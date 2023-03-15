/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.Threading.Tasks;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Registries.AddressService;
using CertificateDetails = Helsenorge.Registries.Abstractions.CertificateDetails;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Provides access to the address registry
    /// This is designed so it can be used as a singleton
    /// </summary>
    public class AddressRegistry : IAddressRegistry
    {
        private readonly SoapServiceInvoker _invoker;
        private readonly AddressRegistrySettings _settings;
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;

        /// <summary>
        /// Contstructor
        /// </summary>
        /// <param name="settings">Options for this instance</param>
        /// <param name="cache">Cache implementation to use</param>
        /// <param name="logger">The ILogger object used to log diagnostics.</param>
        public AddressRegistry(
            AddressRegistrySettings settings,
            IDistributedCache cache,
            ILogger logger)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _settings = settings;
            _cache = cache;
            _logger = logger;
            _invoker = new SoapServiceInvoker(settings.WcfConfiguration);
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

        /// <summary>
        /// Returns communication details for a specific counterparty
        /// </summary>
        /// <param name="herId">Her id of counterpary</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns>Communication details if found, otherwise null</returns>
        public async Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId, bool forceUpdate)
        {
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
                    communicationParty = await FindCommunicationPartyDetails(herId).ConfigureAwait(false);
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

                communicationPartyDetails = MapCommunicationPartyDetails(communicationParty);

                // Cache the mapped CommunicationPartyDetails.
                await CacheExtensions.WriteValueToCacheAsync(_logger, _cache, key, communicationPartyDetails, _settings.CachingInterval).ConfigureAwait(false);
            }

            return communicationPartyDetails;
        }

        /// <summary>
        /// Returns encryption certificate for a specific communication party.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <returns></returns>
        public async Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId)
        {
            return await GetCertificateDetailsForEncryptionAsync(herId, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns encryption certificate for a specific communication party.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        public async Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId, bool forceUpdate)
        {
            var key = $"AR_GetCertificateDetailsForEncryption{herId}";

            var certificateDetails = forceUpdate ? null : await CacheExtensions.ReadValueFromCacheAsync<CertificateDetails>(
                        _logger,
                        _cache,
                        key).ConfigureAwait(false);
            if (certificateDetails == null)
            {
                AddressService.CertificateDetails certificateDetailsRegistry;
                try
                {
                    certificateDetailsRegistry = await GetCertificateDetailsForEncryptionInternal(herId).ConfigureAwait(false);
                }
                catch (FaultException ex)
                {
                    throw new RegistriesException(ex.Message, ex)
                    {
                        EventId = EventIds.CerificateDetails,
                        Data = { { "HerId", herId } }
                    };
                }

                certificateDetails = MapCertificateDetails(herId, certificateDetailsRegistry);
                // Cache the mapped CertificateDetails.
                await CacheExtensions.WriteValueToCacheAsync(_logger, _cache, key, certificateDetails, _settings.CachingInterval).ConfigureAwait(false);
            }

            return certificateDetails;
        }

        /// <summary>
        /// Returns the signature certificate for a specific communication party.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <returns></returns>
        public async Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId)
        {
            return await GetCertificateDetailsForValidatingSignatureAsync(herId, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the signature certificate for a specific communication party.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        public async Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId, bool forceUpdate)
        {
            var key = $"AR_GetCertificateDetailsForValidationSignature{herId}";

            var certificateDetails = forceUpdate ? null : await CacheExtensions.ReadValueFromCacheAsync<CertificateDetails>(
                        _logger,
                        _cache,
                        key).ConfigureAwait(false);
            if (certificateDetails == null)
            {
                AddressService.CertificateDetails certificateDetailsRegistry;
                try
                {
                    certificateDetailsRegistry = await GetCertificateDetailsForValidatingSignatureInternal(herId).ConfigureAwait(false);
                }
                catch (FaultException ex)
                {
                    throw new RegistriesException(ex.Message, ex)
                    {
                        EventId = EventIds.CerificateDetails,
                        Data = { { "HerId", herId } }
                    };
                }

                certificateDetails = MapCertificateDetails(herId, certificateDetailsRegistry);
                // Cache the mapped CertificateDetails.
                await CacheExtensions.WriteValueToCacheAsync(
                    _logger,
                    _cache,
                    key,
                    certificateDetails,
                    _settings.CachingInterval).ConfigureAwait(false);
            }

            return certificateDetails;
        }

        /// <inheritdoc cref="IAddressRegistry.PingAsync"/>
        public Task PingAsync()
            => PingAsyncInternal();

        /// <summary>
        /// Makes the actual call to the registry. This is virtual so that it can be mocked by unit tests
        /// </summary>
        /// <param name="herId">Her id of communication party</param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        internal virtual Task<CommunicationParty> FindCommunicationPartyDetails(int herId)
            => Invoke(_logger, x => x.GetCommunicationPartyDetailsAsync(herId), "GetCommunicationPartyDetailsAsync");

        /// <summary>
        /// Makes the actual call to the registry. This is a virtual function so that it can be mocked by unit tests.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        internal virtual Task<AddressService.CertificateDetails> GetCertificateDetailsForEncryptionInternal(int herId)
            => Invoke(_logger, x => x.GetCertificateDetailsForEncryptionAsync(herId), "GetCertificateDetailsForEncryptionAsync");

        /// <summary>
        /// Makes the actual call to the registry. This is a virtual function so that it can be mocked by unit tests.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        internal virtual Task<AddressService.CertificateDetails> GetCertificateDetailsForValidatingSignatureInternal(int herId)
            => Invoke(_logger, x => x.GetCertificateDetailsForValidatingSignatureAsync(herId), "GetCertificateDetailsForValidatingSignatureAsync");

        /// <inheritdoc cref="PingAsync"/>
        /// <remarks>Makes the actual call to the registry. It's a virtual method to make it mockable for unit tests.</remarks>
        [ExcludeFromCodeCoverage]
        internal virtual Task PingAsyncInternal()
            => Invoke(_logger, x => x.PingAsync(), "PingAsync");

        private static CommunicationPartyDetails MapCommunicationPartyDetails(CommunicationParty communicationParty)
        {
            if (communicationParty == null)
                return null;

            var details = new CommunicationPartyDetails
            {
                Name = !string.IsNullOrEmpty(communicationParty.DisplayName) ? communicationParty.DisplayName : communicationParty.Name,
                HerId = communicationParty.HerId,
                ParentHerId = communicationParty.ParentHerId,
                ParentName = communicationParty.ParentName
            };

            foreach (var address in communicationParty.ElectronicAddresses)
            {
                switch (address.Type.CodeValue)
                {
                    case "E_SB_ASYNC":
                        details.AsynchronousQueueName = address.Address;
                        break;
                    case "E_SB_SYNC":
                        details.SynchronousQueueName = address.Address;
                        break;
                    case "E_SB_ERROR":
                        details.ErrorQueueName = address.Address;
                        break;
                }
            }
            return details;
        }

        private static CertificateDetails MapCertificateDetails(int herId, AddressService.CertificateDetails certificateDetails)
        {
            if (certificateDetails == null)
                return null;

            return new CertificateDetails
            {
                HerId = herId,
                Certificate = new X509Certificate2(certificateDetails.Certificate),
                LdapUrl = certificateDetails.LdapUrl
            };
        }

        [ExcludeFromCodeCoverage] // requires wire communication
        private Task<T> Invoke<T>(ILogger logger, Func<ICommunicationPartyService, Task<T>> action, string methodName)
            => _invoker.ExecuteAsync(logger, action, methodName);
    }
}
