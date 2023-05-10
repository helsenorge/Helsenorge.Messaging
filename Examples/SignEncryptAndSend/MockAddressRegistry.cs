using System.Collections.Generic;
using System.Threading.Tasks;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace SignEncryptAndSend
{
    public class MockAddressRegistry : IAddressRegistry
    {
        public Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId, bool forceUpdate)
        {
            throw new System.NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId)
        {
            return Task.FromResult(new CertificateDetails
            {
                Certificate = TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint),
                HerId = herId,
            });
        }

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId, bool forceUpdate)
        {
            throw new System.NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId)
        {
            throw new System.NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId, bool forceUpdate)
        {
            throw new System.NotImplementedException();
        }

        public Task PingAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<CommunicationPartyDetails>> SearchByIdAsync(string id, bool forceUpdate = false)
        {
            throw new System.NotImplementedException();
        }
    }
}
