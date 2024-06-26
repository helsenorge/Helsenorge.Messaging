/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.AddressService;
using Helsenorge.Registries.HelseId;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CertificateDetails = Helsenorge.Registries.AddressService.CertificateDetails;

namespace Helsenorge.Registries.Tests.Mocks
{
    /// <summary>
    /// Provides a mock implementation of AddressRegistry.
    /// This code exists in this assembly so we don't have to make service reference code publicly available
    /// </summary>
    public class AddressRegistryRestMock : AddressRegistryRest
    {
        private Func<int, string> _findCommunicationPartyDetails;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="cache"></param>
        public AddressRegistryRestMock(
            AddressRegistryRestSettings settings,
            IDistributedCache cache,
            ILogger logger,
            IHelseIdClient helseIdClient) : base(settings, cache, logger, helseIdClient)
        {
        }
        /// <summary>
        /// Configures a func to be called when calling the actual method
        /// </summary>
        /// <param name="func"></param>
        public void SetupFindCommunicationPartyDetails(Func<int, string> func)
        {
            _findCommunicationPartyDetails = func;
        }

        protected override async Task<CommunicationPartyDetails> FindCommunicationPartyDetails(int herId)
        {
            var json = _findCommunicationPartyDetails(herId);
            if (json == null)
            {
                return default(CommunicationPartyDetails);
            }
            return await Task.FromResult(MapCommunicationPartyDetails(json)).ConfigureAwait(false);
        }
    }
}
