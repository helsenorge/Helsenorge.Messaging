using System.Threading.Tasks;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ReceiveDecryptAndValidate
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
            throw new System.NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(ILogger logger, int herId, bool forceUpdate)
        {
            throw new System.NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(ILogger logger, int herId)
        {
            return Task.FromResult(new CertificateDetails
            {
                Certificate = TestCertificates.HelsenorgePublicSignature,
                HerId = herId
            });
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
