using System;
using System.IO;
using System.ServiceModel;
using System.Threading.Tasks;
using Helsenorge.Registries.CPAService;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Helsenorge.Registries.Abstractions;
using System.Runtime.Serialization;
using System.Xml;

namespace Helsenorge.Registries.Mocks
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
            IAddressRegistry addressRegistry) : base(settings, cache, addressRegistry)
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
        /// <param name="logger"></param>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        protected override Task<string> FindProtocolForCounterparty(ILogger logger, int counterpartyHerId)
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

        internal override Task<CpaXmlDetails> FindAgreementForCounterparty(ILogger logger, int counterpartyHerId)
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

        internal override Task<CpaXmlDetails> FindAgreementById(ILogger logger, Guid id)
        {
            var details = new CpaXmlDetails()
            {
                CollaborationProtocolAgreementXml = _findAgreementById(id)
            };
            return Task.FromResult(details);
        }
    }
}
