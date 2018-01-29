using System;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.Threading.Tasks;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.AddressService;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

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
        /// <summary>
        /// Contstructor
        /// </summary>
        /// <param name="settings">Options for this instance</param>
        /// <param name="cache">Cache implementation to use</param>
        public AddressRegistry(
            AddressRegistrySettings settings,
            IDistributedCache cache)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (cache == null) throw new ArgumentNullException(nameof(cache));

            _settings = settings;
            _cache = cache;
            _invoker = new SoapServiceInvoker(settings.WcfConfiguration);
            _invoker.SetClientCredentials(_settings.UserName, _settings.Password);
        }
        /// <summary>
        /// Returns communication details for a specific counterparty
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her id of counterpary</param>
        /// <returns>Communication details if found, otherwise null</returns>
        public async Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(ILogger logger, int herId)
        {
            return await FindCommunicationPartyDetailsAsync(logger, herId, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns communication details for a specific counterparty
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her id of counterpary</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns>Communication details if found, otherwise null</returns>
        public async Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(ILogger logger, int herId, bool forceUpdate)
        {
            var key = $"AR_FindCommunicationPartyDetailsAsync_{herId}";
            var party = forceUpdate ? null : await CacheExtensions.ReadValueFromCache<CommunicationParty>(logger, _cache, key).ConfigureAwait(false);

            if (party == null)
            {
                try
                {
                    party = await FindCommunicationPartyDetails(logger, herId).ConfigureAwait(false);
                }
                catch (FaultException ex)
                {
                    throw new RegistriesException(ex.Message, ex)
                    {
                        EventId = EventIds.CommunicationPartyDetails,
                        Data = { { "HerId", herId } }
                    };
                }
                await CacheExtensions.WriteValueToCache(logger, _cache, key, party, _settings.CachingInterval).ConfigureAwait(false);
            }
            return party == null ? default(CommunicationPartyDetails) : MapCommunicationPartyDetails(party);
        }

        /// <summary>
        /// Returns encryption ceritficate for a specific communcation party.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <returns></returns>
        public async Task<Abstractions.CertificateDetails> GetCertificateDetailsForEncryptionAsync(ILogger logger, int herId)
        {
            return await GetCertificateDetailsForEncryptionAsync(logger, herId, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns encryption ceritficate for a specific communcation party.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        public async Task<Abstractions.CertificateDetails> GetCertificateDetailsForEncryptionAsync(ILogger logger, int herId, bool forceUpdate)
        {
            var key = $"AR_GetCertificateDetailsForEncryption{herId}";
            var certificateDetails = forceUpdate ? null : await CacheExtensions.ReadValueFromCache<AddressService.CertificateDetails>(logger, _cache, key).ConfigureAwait(false);

            if(certificateDetails == null)
            {
                try
                {
                    certificateDetails = await GetCertificateDetailsForEncryptionInternal(logger, herId).ConfigureAwait(false);
                }
                catch(FaultException ex)
                {
                    throw new RegistriesException(ex.Message, ex)
                    {
                        EventId = EventIds.CerificateDetails,
                        Data = { { "HerId", herId } }
                    };
                }
                await CacheExtensions.WriteValueToCache(logger, _cache, key, certificateDetails, _settings.CachingInterval).ConfigureAwait(false);
            }
            return certificateDetails == null ? default(Abstractions.CertificateDetails) : MapCertificateDetails(herId, certificateDetails);
        }

        /// <summary>
        /// Returns the signature certificate for a specific communication party.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <returns></returns>
        public async Task<Abstractions.CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(ILogger logger, int herId)
        {
            return await GetCertificateDetailsForValidatingSignatureAsync(logger, herId, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the signature certificate for a specific communication party.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        public async Task<Abstractions.CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(ILogger logger, int herId, bool forceUpdate)
        {
            var key = $"AR_GetCertificateDetailsForValidationSignature{herId}";
            var certificateDetails = forceUpdate ? null : await CacheExtensions.ReadValueFromCache<AddressService.CertificateDetails>(logger, _cache, key).ConfigureAwait(false);

            if (certificateDetails == null)
            {
                try
                {
                    certificateDetails = await GetCertificateDetailsForValidatingSignatureInternal(logger, herId).ConfigureAwait(false);
                }
                catch (FaultException ex)
                {
                    throw new RegistriesException(ex.Message, ex)
                    {
                        EventId = EventIds.CerificateDetails,
                        Data = { { "HerId", herId } }
                    };
                }
                await CacheExtensions.WriteValueToCache(logger, _cache, key, certificateDetails, _settings.CachingInterval).ConfigureAwait(false);
            }
            return certificateDetails == null ? default(Abstractions.CertificateDetails) : MapCertificateDetails(herId, certificateDetails);
        }
        
        /// <summary>
        /// Makes the actual call to the registry. This is virtual so that it can be mocked by unit tests
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her id of communication party</param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        internal virtual Task<CommunicationParty> FindCommunicationPartyDetails(ILogger logger, int herId)
            => Invoke(logger, x => x.GetCommunicationPartyDetailsAsync(herId), "GetCommunicationPartyDetailsAsync");

        /// <summary>
        /// Makes the actual call to the registry. This is a virtual function so that it can be mocked by unit tests.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        internal virtual Task<AddressService.CertificateDetails> GetCertificateDetailsForEncryptionInternal(ILogger logger, int herId)
            => Invoke(logger, x => x.GetCertificateDetailsForEncryptionAsync(herId), "GetCertificateDetailsForEncryptionAsync");

        /// <summary>
        /// Makes the actual call to the registry. This is a virtual function so that it can be mocked by unit tests.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        internal virtual Task<AddressService.CertificateDetails> GetCertificateDetailsForValidatingSignatureInternal(ILogger logger, int herId)
            => Invoke(logger, x => x.GetCertificateDetailsForValidatingSignatureAsync(herId), "GetCertificateDetailsForValidatingSignatureAsync");

        private static CommunicationPartyDetails MapCommunicationPartyDetails(CommunicationParty communicationParty)
        {
            var details = new CommunicationPartyDetails
            {
                Name = !string.IsNullOrEmpty(communicationParty.DisplayName) ? communicationParty.DisplayName : communicationParty.Name,
                HerId = communicationParty.HerId,
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

        private static Abstractions.CertificateDetails MapCertificateDetails(int herId, AddressService.CertificateDetails certificateDetails)
        {
            return new Abstractions.CertificateDetails
            {
                HerId = herId,
                Certificate = new X509Certificate2(certificateDetails.Certificate),
                LdapUrl = certificateDetails.LdapUrl
            };
        }

        [ExcludeFromCodeCoverage] // requires wire communication
        private Task<T> Invoke<T>(ILogger logger, Func<ICommunicationPartyService, Task<T>> action, string methodName)
            => _invoker.Execute(logger, action, methodName, _settings.EndpointName);
    }
}
