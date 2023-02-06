/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.ServiceModel;
using System.Threading.Tasks;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.CPAService;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries.Tests.Mocks
{
    /// <summary>
    /// Provides a mock implementation of CollaborationProtocolRegistry.
    /// This code exists in this assembly so we don't have to make service reference code publicly available
    /// </summary>
    public class CollaborationProtocolRegistryMock : CollaborationProtocolRegistry
    {
        private Func<int, string> _findProtocolForCounterparty;
        private Func<int, string> _findAgreementForCounterparty;
        private Func<Guid, string> _findAgreementById;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="cache"></param>
        /// <param name="addressRegistry"></param>
        public CollaborationProtocolRegistryMock(
            CollaborationProtocolRegistrySettings settings,
            IDistributedCache cache,
            IAddressRegistry addressRegistry,
            ILogger logger) : base(settings, cache, addressRegistry, logger)
        {
        }

        /// <summary>
        /// Configures a func to be called when calling the actual method
        /// </summary>
        /// <param name="func"></param>
        public void SetupFindProtocolForCounterparty(Func<int, string> func)
        {
            _findProtocolForCounterparty = func;
        }

        /// <summary>
        /// Configures a func to be called when calling the actual method
        /// </summary>
        /// <param name="func"></param>
        public void SetupFindAgreementById(Func<Guid, string> func)
        {
            _findAgreementById = func;
        }

        /// <summary>
        /// Configures a func to be called when calling the actual method
        /// </summary>
        /// <param name="func"></param>
        public void SetupFindAgreementForCounterparty(Func<int, string> func)
        {
            _findAgreementForCounterparty = func;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        protected override Task<string> FindProtocolForCounterparty(int counterpartyHerId)
        {
            try
            {
                return Task.FromResult(_findProtocolForCounterparty(counterpartyHerId));
            }
            catch (FileNotFoundException)
            {
                throw new FaultException<AddressService.GenericFault>(new AddressService.GenericFault()
                {
                    ErrorCode = "NotFound"
                });
            }
        }

        internal override Task<CpaXmlDetails> FindAgreementForCounterparty(int myHerId, int counterpartyHerId)
        {
            try
            {
                CpaXmlDetails details = null;
                var cpaXml = _findAgreementForCounterparty(counterpartyHerId);
                if(cpaXml != null && !cpaXml.Contains("CpaXmlDetails i:nil=\"true\""))
                {
                    details = new CpaXmlDetails
                    {
                        CollaborationProtocolAgreementXml = cpaXml
                    };
                }

                return Task.FromResult(details);
            }
            catch (FileNotFoundException)
            {
                throw new FaultException<AddressService.GenericFault>(new AddressService.GenericFault()
                {
                    ErrorCode = "NotFound"
                });
            }
        }

        internal override Task<CpaXmlDetails> FindAgreementById(Guid id)
        {
            var details = new CpaXmlDetails()
            {
                CollaborationProtocolAgreementXml = _findAgreementById(id)
            };
            return Task.FromResult(details);
        }

        /// <inheritdoc cref="CollaborationProtocolRegistry.PingAsyncInternal"/>
        protected override Task PingAsyncInternal(int herId)
        {
            return Task.CompletedTask;
        }
    }
}
