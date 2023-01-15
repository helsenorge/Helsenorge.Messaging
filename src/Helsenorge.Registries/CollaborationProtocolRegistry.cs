/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
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
using Helsenorge.Registries.AddressService;

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
        private readonly IAddressRegistry _adressRegistry;
        /// <summary>
        /// The certificate validator to use
        /// </summary>
        public ICertificateValidator CertificateValidator { get; set; }

        /// <summary>
        /// Contstructor
        /// </summary>
        /// <param name="settings">Options for this instance</param>
        /// <param name="cache">Cache implementation to use</param>
        /// <param name="adressRegistry">AdressRegistry implementation to use</param>
        public CollaborationProtocolRegistry(
            CollaborationProtocolRegistrySettings settings,
            IDistributedCache cache,
            IAddressRegistry adressRegistry)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (adressRegistry == null) throw new ArgumentNullException(nameof(adressRegistry));

            _settings = settings;
            _cache = cache;
            _adressRegistry = adressRegistry;
            _invoker = new SoapServiceInvoker(settings.WcfConfiguration);
            CertificateValidator = new CertificateValidator(_settings.UseOnlineRevocationCheck);
        }

        /// <inheritdoc cref="FindProtocolForCounterpartyAsync"/>
        public async Task<CollaborationProtocolProfile> FindProtocolForCounterpartyAsync(ILogger logger, int counterpartyHerId)
        {
            logger.LogDebug($"FindProtocolForCounterpartyAsync {counterpartyHerId}");

            var key = $"CPA_FindProtocolForCounterpartyAsync_{counterpartyHerId}";
            var result = await CacheExtensions.ReadValueFromCache<CollaborationProtocolProfile>(logger, _cache, key, _settings.CachingFormatter).ConfigureAwait(false);
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
                xmlString = await FindProtocolForCounterparty(logger, counterpartyHerId).ConfigureAwait(false);
            }
            catch (FaultException<CPAService.GenericFault> ex)
            {
                // if this happens, we fall back to the dummy profile further down
                logger.LogWarning($"Could not find or resolve protocol for counterparty when using HerId {counterpartyHerId}. ErrorCode: {ex.Detail.ErrorCode} Message: {ex.Detail.Message}");
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
                return await DummyCollaborationProtocolProfileFactory.CreateAsync(_adressRegistry, logger, counterpartyHerId, null);
            }
            else
            {
                var doc = XDocument.Parse(xmlString);
                result = doc.Root == null ? null : CollaborationProtocolProfile.CreateFromPartyInfoElement(doc.Root.Element(_ns + "PartyInfo"));
            }

            await CacheExtensions.WriteValueToCache(logger, _cache, key, result, _settings.CachingInterval, _settings.CachingFormatter).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        protected virtual Task<string> FindProtocolForCounterparty(ILogger logger, int counterpartyHerId)
            => Invoke(logger, x => x.GetCppXmlForCommunicationPartyAsync(counterpartyHerId),"GetCppXmlForCommunicationPartyAsync");

        /// <inheritdoc cref="FindAgreementByIdAsync(Microsoft.Extensions.Logging.ILogger,System.Guid,int)"/>
        public async Task<CollaborationProtocolProfile> FindAgreementByIdAsync(ILogger logger, Guid id, int myHerId)
        {
            return await FindAgreementByIdAsync(logger, id, myHerId, false).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindAgreementByIdAsync(Microsoft.Extensions.Logging.ILogger,System.Guid,int,bool)"/>
        public async Task<CollaborationProtocolProfile> FindAgreementByIdAsync(ILogger logger, Guid id, int myHerId, bool forceUpdate)
        {
            logger.LogDebug($"FindAgreementByIdAsync {id}");

            var key = $"CPA_FindAgreementByIdAsync_{id}";
            var result = forceUpdate ? null : await CacheExtensions.ReadValueFromCache<CollaborationProtocolProfile>(logger, _cache, key, _settings.CachingFormatter).ConfigureAwait(false);

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
                details = await FindAgreementById(logger, id).ConfigureAwait(false);
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

            await CacheExtensions.WriteValueToCache(logger, _cache, key, result, _settings.CachingInterval, _settings.CachingFormatter).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        internal virtual Task<CPAService.CpaXmlDetails> FindAgreementById(ILogger logger, Guid id)
            => Invoke(logger, x => x.GetCpaXmlAsync(id), "GetCpaXmlAsync");

        /// <inheritdoc cref="FindAgreementForCounterpartyAsync(Microsoft.Extensions.Logging.ILogger,int,int)"/>
        public async Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(ILogger logger, int myHerId, int counterpartyHerId)
        {
            return await FindAgreementForCounterpartyAsync(logger, myHerId, counterpartyHerId, false).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindAgreementForCounterpartyAsync(Microsoft.Extensions.Logging.ILogger,int,int,bool)"/>
        public async Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(ILogger logger, int myHerId, int counterpartyHerId, bool forceUpdate)
        {

            var key = $"CPA_FindAgreementForCounterpartyAsync_{myHerId}_{counterpartyHerId}";
            var result = forceUpdate ? null : await CacheExtensions.ReadValueFromCache<CollaborationProtocolProfile>(logger, _cache, key, _settings.CachingFormatter).ConfigureAwait(false);

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
                details = await FindAgreementForCounterparty(logger, myHerId, counterpartyHerId).ConfigureAwait(false);
            }
            catch (FaultException ex)
            {
                // if there are error getting a proper CPA, we fallback to getting CPP.
                logger.LogWarning($"Failed to resolve CPA between {myHerId} and {counterpartyHerId}. {ex.Message}");
                return await FindProtocolForCounterpartyAsync(logger, counterpartyHerId).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(details?.CollaborationProtocolAgreementXml)) return null;
            var doc = XDocument.Parse(details.CollaborationProtocolAgreementXml);
            if (doc.Root == null) return null;

            var node = (from x in doc.Root.Elements(_ns + "PartyInfo").Elements(_ns + "PartyId")
                        where x.Value != myHerId.ToString()
                        select x.Parent).First();

            result = CollaborationProtocolProfile.CreateFromPartyInfoElement(node);
            result.CpaId = Guid.Parse(doc.Root.Attribute(_ns + "cpaid").Value);
            
            await CacheExtensions.WriteValueToCache(logger, _cache, key, result, _settings.CachingInterval, _settings.CachingFormatter).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc cref="PingAsync"/>
        [Obsolete("This metod will be replaced in the future.")]
        public async Task PingAsync(ILogger logger, int herId)
        {
            await PingAsyncInternal(logger, herId).ConfigureAwait(false);
        }

        /// <inheritdoc cref="PingAsync"/>
        [ExcludeFromCodeCoverage]
        protected virtual async Task PingAsyncInternal(ILogger logger, int herId)
        {
            _ = await Invoke(logger, service => service.GetCppForCommunicationPartyAsync(herId), "GetCppForCommunicationPartyAsync").ConfigureAwait(false);
        }

        /// <inheritdoc cref="GetCollaborationProtocolProfileAsync"/>
        public async Task<CollaborationProtocolProfile> GetCollaborationProtocolProfileAsync(ILogger logger, Guid id, bool forceUpdate = false)
        {
            var key = $"CPA_GetCollaborationProtocolProfileAsync_{id}";
            var result = forceUpdate ? null : await CacheExtensions.ReadValueFromCache<CollaborationProtocolProfile>(logger, _cache, key, _settings.CachingFormatter).ConfigureAwait(false);

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
                collaborationProtocolProfileXml = await GetCollaborationProtocolProfileAsXmlAsyncInternal(logger, id).ConfigureAwait(false);
            }
            catch (FaultException ex)
            {
                // if there are error getting a proper CPP, we have only the option to log that.
                logger.LogError($"Could not find or resolve protocol for counterparty when retrieving by id: '{id}'.  ErrorCode: {ex.Code} Message: {ex.Message}");
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

            await CacheExtensions.WriteValueToCache(logger, _cache, key, result, _settings.CachingInterval, _settings.CachingFormatter).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc cref="GetCollaborationProtocolProfileAsync"/>
        protected virtual Task<string> GetCollaborationProtocolProfileAsXmlAsyncInternal(ILogger logger, Guid id)
            => Invoke(logger, x => x.GetCppXmlAsync(id), "GetCppXmlAsync");
        
        /// <summary>
        /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="myHerId"></param>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        internal virtual Task<CPAService.CpaXmlDetails> FindAgreementForCounterparty(ILogger logger, int myHerId, int counterpartyHerId)
            => Invoke(logger, x => x.GetCpaForCommunicationPartiesXmlAsync(myHerId, counterpartyHerId), "GetCpaForCommunicationPartiesXmlAsync");

        [ExcludeFromCodeCoverage] // requires wire communication
        private Task<T> Invoke<T>(ILogger logger, Func<CPAService.ICPPAService, Task<T>> action, string methodName)
            => _invoker.Execute(logger, action, methodName);

        [ExcludeFromCodeCoverage] // requires wire communication
        private Task<T> Invoke<T>(ILogger logger, Func<ICommunicationPartyService, Task<T>> action, string methodName)
            => _invoker.Execute(logger, action, methodName);
    }
}
