using System.Threading.Tasks;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace SignEncryptAndSend
{
    public class MockAddressRegistry : IAddressRegistry
    {
        public Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(ILogger logger, int herId)
        {
            throw new System.NotImplementedException();
        }

        public Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(ILogger logger, int herId, bool forceUpdate)
        {
            throw new System.NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(ILogger logger, int herId)
        {
            return Task.FromResult(new CertificateDetails
            {
                Certificate = TestCertificates.CounterpartyPublicEncryption,
                HerId = herId,
            });
        }

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(ILogger logger, int herId, bool forceUpdate)
        {
            throw new System.NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(ILogger logger, int herId)
        {
            throw new System.NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(ILogger logger, int herId, bool forceUpdate)
        {
            throw new System.NotImplementedException();
        }

        public Task PingAsync(ILogger logger)
        {
            throw new System.NotImplementedException();
        }
    }
}
