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
using CertificateDetails = Helsenorge.Registries.Abstractions.CertificateDetails;
using System.Collections.Generic;
using Helsenorge.Registries.Configuration;
using System.Net.Http;
using Helsenorge.Registries.HelseId;
using System.Text.Json;

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
        /// <param name="helseIdClient">The HelseIdClient object used to retrive authentication token.</param>


        public AddressRegistryRest(
            AddressRegistryRestSettings settings,
            IDistributedCache cache,
            ILogger logger,
            IHelseIdClient helseIdClient)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _helseIdClient = helseIdClient ?? throw new ArgumentNullException(nameof(helseIdClient));

            var httpClientFactory = new ProxyHttpClientFactory(settings.RestConfiguration);
            _restServiceInvoker = new RestServiceInvoker(_logger, httpClientFactory);
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
        /// <param name="herId">HER-Id of counter party</param>
        /// /// <param name="forceUpdate">Set to true to force cache update.</param>
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
                try
                {
                    var details = await FindCommunicationPartyDetails(herId).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(details))
                    {
                        communicationPartyDetails = MapCommunicationPartyDetails(details);
                    }
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
                await CacheExtensions.WriteValueToCacheAsync(_logger, _cache, key, communicationPartyDetails, _settings.CachingInterval).ConfigureAwait(false);
            }
            return communicationPartyDetails;
        }

        /// <summary>
        /// Makes the actual call to the registry. This is virtual so that it can be mocked by unit tests
        /// </summary>
        /// <param name="herId">Her id of communication party</param>
        /// <returns>Communication party details as json</returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        internal virtual async Task<string> FindCommunicationPartyDetails(int herId)
        {
            var request = await CreateRequestMessageAsync(HttpMethod.Get, $"/CommunicationParties/{herId}");
            return await _restServiceInvoker.ExecuteAsync(request, nameof(FindCommunicationPartyDetails));
        }

        private static CommunicationPartyDetails MapCommunicationPartyDetails(string details)
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
                    return item.GetProperty("Address").GetString();
                }
            }
            return string.Empty;
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

        /// <inheritdoc cref="IAddressRegistry.GetCertificateDetailsForEncryptionAsync(int)"/>
        [Obsolete("This method is no longer supported.")]
        public async Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId)
        {
            return await GetCertificateDetailsForEncryptionAsync(herId, false);
        }

        /// <inheritdoc cref="IAddressRegistry.GetCertificateDetailsForEncryptionAsync(int, bool)"/>
        [Obsolete("This method is no longer supported.")]
        public async Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId, bool forceUpdate)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IAddressRegistry.GetCertificateDetailsForValidatingSignatureAsync(int)"/>
        [Obsolete("This method is no longer supported.")]
        public async Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IAddressRegistry.GetCertificateDetailsForValidatingSignatureAsync(int, bool)"/>
        [Obsolete("This method is no longer supported.")]
        public async Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId, bool forceUpdate)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IAddressRegistry.SearchByIdAsync"/>/>
        [Obsolete("This method is no longer supported.")]
        public async Task<IEnumerable<CommunicationPartyDetails>> SearchByIdAsync(string id, bool forceUpdate = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IAddressRegistry.GetOrganizationDetailsAsync"/>
        [Obsolete("This method is no longer supported.")]
        public async Task<OrganizationDetails> GetOrganizationDetailsAsync(int herId, bool forceUpdate = false)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IAddressRegistry.PingAsync"/>
        [Obsolete("This method is no longer supported.")]
        public async Task PingAsync()
        {
            throw new NotImplementedException();
        }
    }
}