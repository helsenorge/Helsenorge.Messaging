/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Default implementation for interacting with Collaboration Protocol registry.
    /// This is designed so it can be used as a singleton
    /// </summary>
    public class CollaborationProtocolRegistry : ICollaborationProtocolRegistry
    {
        XNamespace _ns = "http://www.oasis-open.org/committees/ebxml-cppa/schema/cpp-cpa-2_0.xsd";

        private readonly SoapServiceInvoker _invoker;
        private readonly CollaborationProtocolRegistrySettings _settings;
        private readonly IDistributedCache _cache;
        private readonly IAddressRegistry _addressRegistry;
        private readonly ILogger _logger;

        /// <summary>
        /// The certificate validator to use
        /// </summary>
        public ICertificateValidator CertificateValidator { get; set; }

        /// <summary>
        /// Contstructor
        /// </summary>
        /// <param name="settings">Options for this instance</param>
        /// <param name="cache">Cache implementation to use</param>
        /// <param name="addressRegistry">AddressRegistry implementation to use</param>
        /// <param name="logger">The ILogger object used to log diagnostics.</param>
        public CollaborationProtocolRegistry(
            CollaborationProtocolRegistrySettings settings,
            IDistributedCache cache,
            IAddressRegistry addressRegistry,
            ILogger logger)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (addressRegistry == null) throw new ArgumentNullException(nameof(addressRegistry));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _settings = settings;
            _cache = cache;
            _addressRegistry = addressRegistry;
            _logger = logger;
            _invoker = new SoapServiceInvoker(settings.WcfConfiguration);
            CertificateValidator = new CertificateValidator(_settings.UseOnlineRevocationCheck);
        }

        /// <inheritdoc cref="FindProtocolForCounterpartyAsync"/>
        public async Task<CollaborationProtocolProfile> FindProtocolForCounterpartyAsync(int counterpartyHerId)
        {
            _logger.LogDebug($"FindProtocolForCounterpartyAsync {counterpartyHerId}");

            var key = $"CPA_FindProtocolForCounterpartyAsync_{counterpartyHerId}";
            var result = await CacheExtensions.ReadValueFromCacheAsync<CollaborationProtocolProfile>(_logger, _cache, key).ConfigureAwait(false);
            var xmlString = string.Empty;

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
            try
            {
                xmlString = await FindProtocolForCounterparty(counterpartyHerId).ConfigureAwait(false);
            }
            catch (FaultException<CPAService.GenericFault> ex)
            {
                // if this happens, we fall back to the dummy profile further down
                _logger.LogWarning($"Could not find or resolve protocol for counterparty when using HerId {counterpartyHerId}. ErrorCode: {ex.Detail.ErrorCode} Message: {ex.Detail.Message}");
            }
            catch (Exception ex)
            {
                throw new RegistriesException(ex.Message, ex)
                {
                    EventId = EventIds.CollaborationProfile,
                    Data = { { "HerId", counterpartyHerId } }
                };
            }
            if (string.IsNullOrEmpty(xmlString))
            {
                return await DummyCollaborationProtocolProfileFactory.CreateAsync(_addressRegistry, _logger, counterpartyHerId, null);
            }
            else
            {
                var doc = XDocument.Parse(xmlString);
                result = doc.Root == null ? null : CollaborationProtocolProfile.CreateFromPartyInfoElement(doc.Root.Element(_ns + "PartyInfo"));
            }

            await CacheExtensions.WriteValueToCacheAsync(_logger, _cache, key, result, _settings.CachingInterval).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
        /// </summary>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        protected virtual Task<string> FindProtocolForCounterparty(int counterpartyHerId)
            => Invoke(_logger, x => x.GetCppXmlForCommunicationPartyAsync(counterpartyHerId),"GetCppXmlForCommunicationPartyAsync");

        /// <inheritdoc cref="FindAgreementByIdAsync(System.Guid,int)"/>
        public async Task<CollaborationProtocolProfile> FindAgreementByIdAsync(Guid id, int myHerId)
        {
            return await FindAgreementByIdAsync(id, myHerId, false).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindAgreementByIdAsync(System.Guid,int,bool)"/>
        public async Task<CollaborationProtocolProfile> FindAgreementByIdAsync(Guid id, int myHerId, bool forceUpdate)
        {
            _logger.LogDebug($"FindAgreementByIdAsync {id}");

            var key = $"CPA_FindAgreementByIdAsync_{id}";
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

            CPAService.CpaXmlDetails details;

            try
            {
                details = await FindAgreementById(id).ConfigureAwait(false);
            }
            catch (FaultException ex)
            {
                throw new RegistriesException(ex.Message, ex)
                {
                    EventId = EventIds.CollaborationAgreement,
                    Data = { { "CpaId", id } }
                };
            }

            if (string.IsNullOrEmpty(details?.CollaborationProtocolAgreementXml)) return null;
            var doc = XDocument.Parse(details.CollaborationProtocolAgreementXml);
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
        internal virtual Task<CPAService.CpaXmlDetails> FindAgreementById(Guid id)
            => Invoke(_logger, x => x.GetCpaXmlAsync(id), "GetCpaXmlAsync");

        /// <inheritdoc cref="FindAgreementForCounterpartyAsync(int,int)"/>
        public async Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(int myHerId, int counterpartyHerId)
        {
            return await FindAgreementForCounterpartyAsync(myHerId, counterpartyHerId, false).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindAgreementForCounterpartyAsync(int,int,bool)"/>
        public async Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(int myHerId, int counterpartyHerId, bool forceUpdate)
        {

            var key = $"CPA_FindAgreementForCounterpartyAsync_{myHerId}_{counterpartyHerId}";
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

            CPAService.CpaXmlDetails details;

            try
            {
                details = await FindAgreementForCounterparty(myHerId, counterpartyHerId).ConfigureAwait(false);
            }
            catch (FaultException ex)
            {
                // if there are error getting a proper CPA, we fallback to getting CPP.
                _logger.LogWarning($"Failed to resolve CPA between {myHerId} and {counterpartyHerId}. {ex.Message}");
                return await FindProtocolForCounterpartyAsync(counterpartyHerId).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(details?.CollaborationProtocolAgreementXml)) return null;
            var doc = XDocument.Parse(details.CollaborationProtocolAgreementXml);
            if (doc.Root == null) return null;

            var node = (from x in doc.Root.Elements(_ns + "PartyInfo").Elements(_ns + "PartyId")
                        where x.Value != myHerId.ToString()
                        select x.Parent).First();

            result = CollaborationProtocolProfile.CreateFromPartyInfoElement(node);
            result.CpaId = Guid.Parse(doc.Root.Attribute(_ns + "cpaid").Value);

            await CacheExtensions.WriteValueToCacheAsync(_logger, _cache, key, result, _settings.CachingInterval).ConfigureAwait(false);
            return result;
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
            _ = await Invoke(_logger, service => service.GetCppForCommunicationPartyAsync(herId), "GetCppForCommunicationPartyAsync").ConfigureAwait(false);
        }

        /// <inheritdoc cref="GetCollaborationProtocolProfileAsync"/>
        public async Task<CollaborationProtocolProfile> GetCollaborationProtocolProfileAsync(Guid id, bool forceUpdate = false)
        {
            var key = $"CPA_GetCollaborationProtocolProfileAsync_{id}";
            var result = forceUpdate ? null : await CacheExtensions.ReadValueFromCacheAsync<CollaborationProtocolProfile>(_logger, _cache, key).ConfigureAwait(false);

            if (result != null)
            {
                var errors = CertificateErrors.None;
                errors |= CertificateValidator.Validate(result.EncryptionCertificate, X509KeyUsageFlags.KeyEncipherment);
                errors |= CertificateValidator.Validate(result.SignatureCertificate, X509KeyUsageFlags.NonRepudiation);
                // If the certificates are valid, only then do we return a value from the cache
                if (errors == CertificateErrors.None)
                {
                    return result;
                }
            }

            string collaborationProtocolProfileXml;
            try
            {
                collaborationProtocolProfileXml = await GetCollaborationProtocolProfileAsXmlAsyncInternal(id).ConfigureAwait(false);
            }
            catch (FaultException ex)
            {
                // if there are error getting a proper CPP, we have only the option to log that.
                _logger.LogError($"Could not find or resolve protocol for counterparty when retrieving by id: '{id}'.  ErrorCode: {ex.Code} Message: {ex.Message}");
                throw new RegistriesException(ex.Message, ex)
                {
                    EventId = EventIds.CollaborationProfile,
                    Data = { { "CppId", id } }
                };
            }

            if (string.IsNullOrEmpty(collaborationProtocolProfileXml))
                return null;

            var document = XDocument.Parse(collaborationProtocolProfileXml);
            result = document.Root == null ? null : CollaborationProtocolProfile.CreateFromPartyInfoElement(document.Root.Element(_ns + "PartyInfo"));
            if (result != null)
                result.CppId = id;

            await CacheExtensions.WriteValueToCacheAsync(_logger, _cache, key, result, _settings.CachingInterval).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc cref="GetCollaborationProtocolProfileAsync"/>
        protected virtual Task<string> GetCollaborationProtocolProfileAsXmlAsyncInternal(Guid id)
            => Invoke(_logger, x => x.GetCppXmlAsync(id), "GetCppXmlAsync");

        /// <summary>
        /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
        /// </summary>
        /// <param name="myHerId"></param>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        internal virtual Task<CPAService.CpaXmlDetails> FindAgreementForCounterparty(int myHerId, int counterpartyHerId)
            => Invoke(_logger, x => x.GetCpaForCommunicationPartiesXmlAsync(myHerId, counterpartyHerId), "GetCpaForCommunicationPartiesXmlAsync");

        [ExcludeFromCodeCoverage] // requires wire communication
        private Task<T> Invoke<T>(ILogger logger, Func<CPAService.ICPPAService, Task<T>> action, string methodName)
            => _invoker.ExecuteAsync(logger, action, methodName);
    }
}
