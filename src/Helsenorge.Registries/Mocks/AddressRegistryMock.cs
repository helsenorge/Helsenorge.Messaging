using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Registries.AddressService;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries.Mocks
{
    /// <summary>
    /// Provides a mock implementation of AddressRegistry.
    /// This code exists in this assembly so we don't have to make service reference code publicly available
    /// </summary>
    public class AddressRegistryMock : AddressRegistry
    {
        private Func<int, XElement> _findCommunicationPartyDetails;
        private Func<int, XElement> _getCertificateDetailsForEncryption;
        private Func<int, XElement> _getCertificateDetailsForValidatingSignature;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="cache"></param>
        public AddressRegistryMock(
            AddressRegistrySettings settings,
            IDistributedCache cache) : base(settings, cache)
        {
        }
        /// <summary>
        /// Configures a func to be called when calling the actual method
        /// </summary>
        /// <param name="func"></param>
        public void SetupFindCommunicationPartyDetails(Func<int, XElement> func)
        {
            _findCommunicationPartyDetails = func;
        }

        /// <summary>
        /// Receives a delegate which will be exectud when the method GetCerificateDetailsForEncryption is called.
        /// </summary>
        /// <param name="func">The encapsulated method</param>
        public void SetupGetCertificateDetailsForEncryption(Func<int, XElement> func)
        {
            _getCertificateDetailsForEncryption = func;
        }

        /// <summary>
        /// Receives a delegate which will be exectud when the method GetCertificateDetailsForValidatingSignature is called.
        /// </summary>
        /// <param name="func">The encapsulated method</param>
        public void SetupGetCertificateDetailsForValidatingSignature(Func<int, XElement> func)
        {
            _getCertificateDetailsForValidatingSignature = func;
        }

        internal override async Task<CommunicationParty> FindCommunicationPartyDetails(ILogger logger, int herId)
        {
            var xml = _findCommunicationPartyDetails(herId);
            if (xml == null)
            {
                return default(CommunicationParty);
            }
            return await Task.FromResult(Utils.Deserialize<CommunicationParty>(xml)).ConfigureAwait(false);
        }

        internal override async Task<CertificateDetails> GetCertificateDetailsForEncryptionInternal(ILogger logger, int herId)
        {
            var xml = _getCertificateDetailsForEncryption(herId);
            if(xml == null)
            {
                return default(CertificateDetails);
            }
            return await Task.FromResult(Utils.Deserialize<CertificateDetails>(xml)).ConfigureAwait(false);
        }

        internal override async Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureInternal(ILogger logger, int herId)
        {
            var xml = _getCertificateDetailsForValidatingSignature(herId);
            if (xml == null)
            {
                return default(CertificateDetails);
            }
            return await Task.FromResult(Utils.Deserialize<CertificateDetails>(xml)).ConfigureAwait(false);
        }
    }
}
