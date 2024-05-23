/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.Connected_Services.HelseId;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using CertificateDetails = Helsenorge.Registries.Abstractions.CertificateDetails;
using System.Xml.Linq;
using Helsenorge.Registries.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Helsenorge.Registries.AddressService;
using System.ServiceModel;

namespace Helsenorge.Registries;
/// <summary>
/// Rest implementation for interacting with Collaboration Protocol registry.
/// This is designed so it can be used as a singleton
/// </summary>
public class AddressRegistryRest : IAddressRegistry
{
    private readonly IDistributedCache _cache;
    private readonly RestServiceInvoker _restServiceInvokerCommunicationParty;
    private readonly RestServiceInvoker _restServiceInvokerCertificate;
    private readonly ILogger _logger;
    private readonly AddressRegistryRestSettings _settings;
    private readonly HelseIdClient _helseIdClient;
    private readonly XNamespace _ns = "http://www.oasis-open.org/committees/ebxml-cppa/schema/cpp-cpa-2_0.xsd";

    /// <summary>
    ///     The certificate validator to use
    /// </summary>
    public ICertificateValidator CertificateValidator { get; set; }

    public AddressRegistryRest(
        AddressRegistryRestSettings settings,
        IDistributedCache cache,
        ILogger logger,
        HelseIdClient helseIdClient)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _helseIdClient = helseIdClient ?? throw new ArgumentNullException(nameof(helseIdClient));

        var httpClientFactoryCommunicationParty = new ProxyHttpClientFactory(settings.RestConfigurationAr);
        _restServiceInvokerCommunicationParty = new RestServiceInvoker(_logger, httpClientFactoryCommunicationParty);
        var httpClientFactoryCertificate = new ProxyHttpClientFactory(settings.RestConfigurationCertificate);
        _restServiceInvokerCertificate = new RestServiceInvoker(_logger, httpClientFactoryCertificate);

        CertificateValidator = new CertificateValidator(_settings.UseOnlineRevocationCheck);
    }

    /// <summary>
    /// Returns communication details for a specific counterparty
    /// </summary>
    /// <param name="herId">HER-Id of counter party</param>
    /// <returns>Communication details if found, otherwise null</returns>
    public async Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId)
    {
        return await FindCommunicationPartyDetailsAsync(herId, false).ConfigureAwait(false);
    }

    public async Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId, bool forceUpdate)
    {
        _logger.LogDebug($"FindCommunicationPartyDetailsAsync {herId}");

        var key = $"AR_FindCommunicationPartyDetailsAsync_{herId}";
        var communicationPartyDetails = forceUpdate ? null : await CacheExtensions.ReadValueFromCacheAsync<CommunicationPartyDetails>(
                        _logger,
                        _cache,
                        key).ConfigureAwait(false);

        if (communicationPartyDetails == null)
        {
            CommunicationParty communicationParty = null;
            try
            {
                var communicationParties = await FindCommunicationPartyDetailsVirtualAsync(herId);
            }
            catch (FaultException<GenericFault> ex)
            {
                if (ex.Detail.ErrorCode == "InvalidHerIdSupplied")
                    throw new InvalidHerIdException(herId, ex);
            }
            catch (FaultException ex)
            {
                throw new RegistriesException(ex.Message, ex)
                {
                    EventId = EventIds.CommunicationPartyDetails,
                    Data = { { "HerId", herId } }
                };
            }

            //communicationPartyDetails = MapCommunicationPartyTo<CommunicationPartyDetails>(communicationParty);
            
        }
        return null;
    }

    /// <summary>
    /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
    /// </summary>
    /// <param name="herId"></param>
    /// <returns></returns>
    [ExcludeFromCodeCoverage] // requires wire communication
    protected virtual async Task<string> FindCommunicationPartyDetailsVirtualAsync(int herId)
    {
        var request = await CreateRequestMessageAsync(HttpMethod.Get, $"/CommunicationParties/{herId}");
        return await _restServiceInvokerCommunicationParty.ExecuteAsync(request, nameof(FindCommunicationPartyDetailsVirtualAsync));
    }

    public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId)
    {
        //return GetCertificateDetailsForEncryptionAsync(herId, false);
        throw new NotImplementedException();
    }

    public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(string herId)
    {
        return GetCertificateDetailsForEncryptionAsync(herId, false);
    }

    public async Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId, bool forceUpdate)
    {
        throw new NotSupportedException();
    }
    public async Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(string herId, bool forceUpdate)
    {
        var key = $"AR_GetCertificateDetailsForEncryptionAsync{herId}";

        var certificateDetails = forceUpdate ? null : await CacheExtensions.ReadValueFromCacheAsync<CertificateDetails>(
                    _logger,
                    _cache,
                    key).ConfigureAwait(false);
        if (certificateDetails == null)
        {
            //AddressService.CertificateDetails certificateDetailsRegistry;
            try
            {
                 var certificateDetailsRegistry = await GetCertificateDetailsForEncryptionVirtualAsync(herId).ConfigureAwait(false);
            }
            catch (FaultException ex)
            {
                throw new RegistriesException(ex.Message, ex)
                {
                    EventId = EventIds.CerificateDetails,
                    Data = { { "HerId", herId } }
                };
            }

            //certificateDetails = MapCertificateDetails(herId, certificateDetailsRegistry);
            //if (_certificateValidator != null && certificateDetails?.Certificate != null)
            //{
            //    var error = _certificateValidator.Validate(certificateDetails.Certificate, X509KeyUsageFlags.KeyEncipherment);
            //    if (error != CertificateErrors.None)
            //    {
            //        throw new CouldNotVerifyCertificateException($"Could not verify HerId: {herId} certificate", herId);
            //    }
            //}

            // Cache the mapped CertificateDetails.
            await CacheExtensions.WriteValueToCacheAsync(_logger, _cache, key, certificateDetails, _settings.CachingInterval).ConfigureAwait(false);
        }

        return certificateDetails;
    }

    protected virtual async Task<string> GetCertificateDetailsForEncryptionVirtualAsync(string herId)
    {
        var request = await CreateRequestMessageAsync(HttpMethod.Get, $"/CertificateMetadata/{herId}");
        return await _restServiceInvokerCertificate.ExecuteAsync(request, nameof(GetCertificateDetailsForEncryptionVirtualAsync));
    }

    public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId)
    {
        throw new NotImplementedException();
    }

    public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId, bool forceUpdate)
    {
        throw new NotImplementedException();
    }

    public Task PingAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<CommunicationPartyDetails>> SearchByIdAsync(string id, bool forceUpdate = false)
    {
        throw new NotImplementedException();
    }

    public Task<OrganizationDetails> GetOrganizationDetailsAsync(int herId, bool forceUpdate = false)
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
            AcceptHeader = "application/json"
        };
        return request;
    }
}