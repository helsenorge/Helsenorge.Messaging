/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
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
using System.Collections.Generic;
using System.Linq;
using Code = Helsenorge.Registries.Abstractions.Code;
using Helsenorge.Registries.Configuration;
using System.Net.Http;
using Helsenorge.Registries.HelseId;
using System.Text.Json;
using System.Collections;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Provides access to the address registry
    /// This is designed so it can be used as a singleton
    /// </summary>
    public class AddressRegistryRest : IAddressRegistry
    {
        private readonly AddressRegistryRestSettings _settings;
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;
        private readonly IHelseIdClient _helseIdClient;
        private readonly RestServiceInvoker _restServiceInvoker;

        /// <summary>
        /// Contstructor
        /// </summary>
        /// <param name="settings">Options for this instance</param>
        /// <param name="cache">Cache implementation to use</param>
        /// <param name="logger">The ILogger object used to log diagnostics.</param>

        public AddressRegistryRest(
            AddressRegistryRestSettings settings,
            IDistributedCache cache,
            ILogger logger,
            IHelseIdClient helseIdClient)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _helseIdClient = helseIdClient ?? throw new ArgumentNullException(nameof(helseIdClient));

            _settings = settings;
            _cache = cache;
            _logger = logger;

            var httpClientFactory = new ProxyHttpClientFactory(settings.RestConfiguration);
            _restServiceInvoker = new RestServiceInvoker(_logger, httpClientFactory);
        }

        
        public async Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId)
        {
            return await FindCommunicationPartyDetailsAsync(herId, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns communication details for a specific counterparty
        /// </summary>
        /// <param name="herId">HER-Id of counter party</param>
        /// <returns>Communication details if found, otherwise null</returns>
        public async Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId, bool forceUpdate)
        {
            var key = $"AR_FindCommunicationPartyDetailsAsync_{herId}";

            var communicationPartyDetails = forceUpdate ? null : await CacheExtensions.ReadValueFromCacheAsync<CommunicationPartyDetails>(
                        _logger,
                        _cache,
                        key).ConfigureAwait(false);

            if (communicationPartyDetails == null) { }
            {
                try
                {
                    communicationPartyDetails = await FindCommunicationPartyDetails(herId).ConfigureAwait(false);
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
            }
            return communicationPartyDetails;
        }

        /// <summary>
        /// Makes the actual call to the registry. This is virtual so that it can be mocked by unit tests
        /// </summary>
        /// <param name="herId">Her id of communication party</param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        protected virtual async Task<CommunicationPartyDetails> FindCommunicationPartyDetails(int herId)
        {
            var request = await CreateRequestMessageAsync(HttpMethod.Get, $"/CommunicationParties/{herId}");
            var communicationPartyDetails = await _restServiceInvoker.ExecuteAsync(request, nameof(FindCommunicationPartyDetails));
            return MapCommunicationPartyDetails(communicationPartyDetails);
        }

        public static CommunicationPartyDetails MapCommunicationPartyDetails(string details)
        {
            var communicationPartyDetails = new CommunicationPartyDetails();

            using (var communicationDetails = JsonDocument.Parse(details))
            {
                var root = communicationDetails.RootElement;
                communicationPartyDetails.HerId = root.GetProperty("herId").GetInt32();

                var communicationParty = root.GetProperty("communicationParty");
                communicationPartyDetails.ParentOrganizationNumber = communicationParty.GetProperty("ParentOrganizationNumber").GetInt32();
                communicationPartyDetails.Name = communicationParty.GetProperty("Name").GetString();
                communicationPartyDetails.Active = communicationParty.GetProperty("Active").GetBoolean();
                communicationPartyDetails.ParentHerId = communicationParty.GetProperty("ParentHerId").GetInt32();
                communicationPartyDetails.ParentName = communicationParty.GetProperty("ParentName").GetString();
                communicationPartyDetails.IsValidCommunicationParty = communicationParty.GetProperty("IsValidCommunicationParty").GetBoolean();
                communicationPartyDetails.Type = communicationParty.GetProperty("Type").GetString() 
                switch
                {
                    "Service" => CommunicationPartyTypeEnum.Service,
                    "Person" => CommunicationPartyTypeEnum.Person,
                    "Organization" => CommunicationPartyTypeEnum.Organization,
                    "Department" => CommunicationPartyTypeEnum.Department,
                    "All" => CommunicationPartyTypeEnum.All,
                    "None" => CommunicationPartyTypeEnum.None,
                    _ => CommunicationPartyTypeEnum.None
                };

                var electronicAddress = communicationParty.GetProperty("ElectronicAddresses");
                communicationPartyDetails.SynchronousQueueName = GetQueueName(electronicAddress, "E_SB_SYNC");
                communicationPartyDetails.AsynchronousQueueName = GetQueueName(electronicAddress, "E_SB_ASYNC");
                communicationPartyDetails.ErrorQueueName = GetQueueName(electronicAddress, "E_SB_ERROR");

            }
            return communicationPartyDetails;
        }

        private static string GetQueueName(JsonElement element, string value)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (item.GetProperty("TypeCodeValue").GetString() == value)
                {
                    return item.GetProperty("Address").GetString().Split('/').Last();
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Returns encryption certificate for a specific communication party.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <returns></returns>
        [Obsolete("This method is no longer supported.")]
        public async Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns encryption certificate for a specific communication party.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        [Obsolete("This method is no longer supported.")]
        public async Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId, bool forceUpdate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the signature certificate for a specific communication party.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <returns></returns>
        [Obsolete("This method is no longer supported.")]
        public async Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the signature certificate for a specific communication party.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        [Obsolete("This method is no longer supported.")]
        public async Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId, bool forceUpdate)
        {
            await Task.Delay(1000);
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        [Obsolete("This method is no longer supported.")]
        public async Task<IEnumerable<CommunicationPartyDetails>> SearchByIdAsync(string id, bool forceUpdate = false)
        {
            await Task.Delay(1000);
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IAddressRegistry.GetOrganizationDetailsAsync"/>
        [Obsolete("This method is no longer supported.")]
        public async Task<OrganizationDetails> GetOrganizationDetailsAsync(int herId, bool forceUpdate = false)
        {
            await Task.Delay(1000);
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="PingAsync"/>
        [Obsolete("This method will be replaced in the future.")]
        public async Task PingAsync()
        {
            await Task.Delay(1000);
            throw new NotImplementedException();
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
            var request = await CreateRequestMessageAsync(HttpMethod.Get, $"/CommunicationParties/{herId}");
            await _restServiceInvoker.ExecuteAsync(request, nameof(PingAsync));
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
}