/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.Configuration;
using Helsenorge.Registries.Connected_Services.HelseId;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries;

/// <summary>
/// Rest implementation for interacting with Collaboration Protocol registry.
/// This is designed so it can be used as a singleton
/// </summary>
public class CollaborationProtocolRegistryRest : ICollaborationProtocolRegistry
{
    private readonly IDistributedCache _cache;
    private readonly IAddressRegistry _addressRegistry;
    private readonly RestServiceInvoker _restServiceInvoker;
    private readonly ILogger _logger;
    private readonly IHelseIdClient _helseIdClient;
    private readonly XNamespace _ns = "http://www.oasis-open.org/committees/ebxml-cppa/schema/cpp-cpa-2_0.xsd";
    private readonly CollaborationProtocolRegistryRestSettings _settings;

    /// <summary>
    ///     The certificate validator to use
    /// </summary>
    public ICertificateValidator CertificateValidator { get; set; }

    /// <summary>
    /// Contstructor
    /// </summary>
    /// <param name="settings">Options for this instance</param>
    /// <param name="cache">Cache implementation to use</param>
    /// <param name="addressRegistry">AddressRegistry implementation to use</param>
    /// <param name="logger">The ILogger object used to log diagnostics.</param>
    /// <param name="helseIdClient">The HelseIdClient object used to retrive authentication token.</param>
    public CollaborationProtocolRegistryRest(
        CollaborationProtocolRegistryRestSettings settings,
        IDistributedCache cache,
        IAddressRegistry addressRegistry,
        ILogger logger,
        IHelseIdClient helseIdClient)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _addressRegistry = addressRegistry ?? throw new ArgumentNullException(nameof(addressRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _helseIdClient = helseIdClient ?? throw new ArgumentNullException(nameof(helseIdClient));

        var httpClientFactory = new ProxyHttpClientFactory(settings.RestConfiguration);
        _restServiceInvoker = new RestServiceInvoker(_logger, httpClientFactory);
        CertificateValidator = new CertificateValidator(_settings.UseOnlineRevocationCheck);
    }

    /// <inheritdoc cref="FindProtocolForCounterpartyAsync"/>
    public async Task<CollaborationProtocolProfile> FindProtocolForCounterpartyAsync(int counterpartyHerId)
    {
        _logger.LogDebug($"FindProtocolForCounterpartyAsync {counterpartyHerId}");

        var key = $"CPA_FindProtocolForCounterpartyAsync_Rest_{counterpartyHerId}";
        var profile = await CacheExtensions.ReadValueFromCacheAsync<CollaborationProtocolProfile>(_logger, _cache, key).ConfigureAwait(false);
        var xmlString = string.Empty;

        if (profile != null)
        {
            var errors = CertificateErrors.None;
            errors |= CertificateValidator.Validate(profile.EncryptionCertificate, X509KeyUsageFlags.KeyEncipherment);
            errors |= CertificateValidator.Validate(profile.SignatureCertificate, X509KeyUsageFlags.NonRepudiation);
            // if the certificates are valid, only then do we return a value from the cache
            if (errors == CertificateErrors.None)
            {
                return profile;
            }
        }

        try
        {
            xmlString = await FindProtocolForCounterpartyVirtualAsync(counterpartyHerId);
        }
        catch (Exception ex)
        {
            // TODO: Implementere handling for om ingen CPPA er funnet og sørge for at DummyCollaborationProtocolProfileFactory.CreateAsync blir kalt

            throw new RegistriesException(ex.Message, ex)
            {
                EventId = EventIds.CollaborationProfile,
                Data = { { "HerId", counterpartyHerId } }
            };
        }
        if (string.IsNullOrEmpty(xmlString))
        {
            // Fix that enables substitutes and interns without CPP to reply to messages on behalf of the GP
            return await DummyCollaborationProtocolProfileFactory.CreateAsync(_addressRegistry, _logger, counterpartyHerId, null);
        }
        else
        {
            var doc = XDocument.Parse(xmlString);
            profile = doc.Root == null ? null : CollaborationProtocolProfile.CreateFromPartyInfoElement(doc.Root.Element(_ns + "PartyInfo"));
        }

        await CacheExtensions.WriteValueToCacheAsync(_logger, _cache, key, profile, _settings.CachingInterval).ConfigureAwait(false);
        return profile;
    }

    /// <summary>
    /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
    /// </summary>
    /// <param name="counterpartyHerId"></param>
    /// <returns></returns>
    [ExcludeFromCodeCoverage] // requires wire communication
    protected virtual async Task<string> FindProtocolForCounterpartyVirtualAsync(int counterpartyHerId)
    {
        var request = await CreateRequestMessageAsync(HttpMethod.Get, $"/Profiles/{counterpartyHerId}");
        return await _restServiceInvoker.ExecuteAsync(request, nameof(FindProtocolForCounterpartyAsync));
    }

    /// <inheritdoc cref="FindAgreementByIdAsync(System.Guid,int)"/>
    public async Task<CollaborationProtocolProfile> FindAgreementByIdAsync(Guid id, int myHerId)
    {
        return await FindAgreementByIdAsync(id, myHerId, false).ConfigureAwait(false);
    }

    /// <inheritdoc cref="FindAgreementByIdAsync(System.Guid,int,bool)"/>
    public async Task<CollaborationProtocolProfile> FindAgreementByIdAsync(Guid id, int myHerId, bool forceUpdate)
    {
        _logger.LogDebug($"FindAgreementByIdAsync {id}");

        var key = $"CPA_FindAgreementByIdAsync_Rest_{id}";
        var result = forceUpdate ? null 
            : await CacheExtensions.ReadValueFromCacheAsync<CollaborationProtocolProfile>(_logger, _cache, key).ConfigureAwait(false);

        if (result != null)
        {
            var errors = CertificateErrors.None;
            errors |= CertificateValidator.Validate(result.EncryptionCertificate, X509KeyUsageFlags.KeyEncipherment);
            errors |= CertificateValidator.Validate(result.SignatureCertificate, X509KeyUsageFlags.NonRepudiation);
            // if the certificates are valid, only then do we return a value from the cache
            if (errors == CertificateErrors.None)
            {
                return result;
            }
        }

        string xmlString;
        try
        {
            xmlString = await FindAgreementByIdVirtualAsync(id);
        }
        catch (FaultException ex)
        {
            throw new RegistriesException(ex.Message, ex)
            {
                EventId = EventIds.CollaborationAgreement,
                Data = { { "CpaId", id } }
            };
        }

        if (string.IsNullOrEmpty(xmlString)) return null;
        var doc = XDocument.Parse(xmlString);
        if (doc.Root == null) return null;

        var node = (from x in doc.Root.Elements(_ns + "PartyInfo").Elements(_ns + "PartyId")
                    where x.Value != myHerId.ToString()
                    select x.Parent).First();

        result = CollaborationProtocolProfile.CreateFromPartyInfoElement(node);
        result.CpaId = id;

        await CacheExtensions.WriteValueToCacheAsync(_logger, _cache, key, result, _settings.CachingInterval).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [ExcludeFromCodeCoverage] // requires wire communication
    protected virtual async Task<string> FindAgreementByIdVirtualAsync(Guid id)
    {
        var request = await CreateRequestMessageAsync(HttpMethod.Get, $"/Agreements/{id}");
        return await _restServiceInvoker.ExecuteAsync(request, nameof(FindAgreementByIdAsync));
    }

    /// <inheritdoc cref="FindAgreementForCounterpartyAsync(int,int)"/>
    public async Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(int myHerId, int counterpartyHerId)
    {
        return await FindAgreementForCounterpartyAsync(myHerId, counterpartyHerId, false).ConfigureAwait(false);
    }

    /// <inheritdoc cref="FindAgreementForCounterpartyAsync(int,int,bool)"/>
    public async Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(int myHerId, int counterpartyHerId, bool forceUpdate)
    {
        var key = $"CPA_FindAgreementForCounterpartyAsync_Rest_{myHerId}_{counterpartyHerId}";
        var result = forceUpdate ? null : await CacheExtensions.ReadValueFromCacheAsync<CollaborationProtocolProfile>(_logger, _cache, key).ConfigureAwait(false);

        if (result != null)
        {
            var errors = CertificateErrors.None;
            errors |= CertificateValidator.Validate(result.EncryptionCertificate, X509KeyUsageFlags.KeyEncipherment);
            errors |= CertificateValidator.Validate(result.SignatureCertificate, X509KeyUsageFlags.NonRepudiation);
            // if the certificates are valid, only then do we return a value from the cache
            if (errors == CertificateErrors.None)
            {
                return result;
            }
        }

        string details;
        try
        {
            details = await FindAgreementForCounterpartyVirtualAsync(myHerId, counterpartyHerId);
        }
        catch (FaultException ex)
        {
            // if there are error getting a proper CPA, we fallback to getting CPP.
            _logger.LogWarning($"Failed to resolve CPA between {myHerId} and {counterpartyHerId}. {ex.Message}");
            return await FindProtocolForCounterpartyAsync(counterpartyHerId).ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(details)) return null;
        var doc = XDocument.Parse(details);
        if (doc.Root == null) return null;

        var node = (from x in doc.Root.Elements(_ns + "PartyInfo").Elements(_ns + "PartyId")
                    where x.Value != myHerId.ToString()
                    select x.Parent).First();

        result = CollaborationProtocolProfile.CreateFromPartyInfoElement(node);
        result.CpaId = Guid.Parse(doc.Root.Attribute(_ns + "cpaid").Value);

        await CacheExtensions.WriteValueToCacheAsync(_logger, _cache, key, result, _settings.CachingInterval).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
    /// </summary>
    /// <param name="myHerId"></param>
    /// <param name="counterpartyHerId"></param>
    /// <returns></returns>
    [ExcludeFromCodeCoverage] // requires wire communication
    protected virtual async Task<string> FindAgreementForCounterpartyVirtualAsync(int myHerId, int counterpartyHerId)
    {
        var request = await CreateRequestMessageAsync(HttpMethod.Get,
            $"/Agreements/HerIds/{myHerId}/{counterpartyHerId}");
        return await _restServiceInvoker.ExecuteAsync(request, nameof(FindAgreementForCounterpartyVirtualAsync));
    }

    /// <inheritdoc cref="PingAsync"/>
    [Obsolete("This method will be replaced in the future.")]
    public async Task PingAsync(int herId)
    {
        await PingAsyncInternal(herId).ConfigureAwait(false);
    }

    /// <inheritdoc cref="PingAsync"/>
    [ExcludeFromCodeCoverage]
    protected virtual async Task PingAsyncInternal(int herId)
    {
        var request = await CreateRequestMessageAsync(HttpMethod.Get, $"/Profiles/{herId}");
        await _restServiceInvoker.ExecuteAsync(request, nameof(PingAsync));
    }

    /// <inheritdoc cref="GetCollaborationProtocolProfileAsync"/>
    [Obsolete("Method GetCollaborationProtocolProfileAsync is deprecated, and will be removed in the future.")]
    public Task<CollaborationProtocolProfile> GetCollaborationProtocolProfileAsync(Guid id, bool forceUpdate = false)
    {
        throw new NotImplementedException();
    }

    private async Task<RequestParameters> CreateRequestMessageAsync(HttpMethod method, string path)
    {
        var request = new RequestParameters()
        {
            Method = method,
            Path = path,
            BearerToken = await _helseIdClient.CreateJwtAccessTokenAsync(),
            AcceptHeader = "application/xml"
        };
        return request;
    }
}