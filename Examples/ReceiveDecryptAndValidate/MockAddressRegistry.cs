using System;
using System.Threading.Tasks;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace ReceiveDecryptAndValidate
{
    public class MockAddressRegistry : IAddressRegistry
    {

        public Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(ILogger logger, int herId)
        {
            throw new NotImplementedException();
        }

        public Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(ILogger logger, int herId, bool forceUpdate)
        {
            throw new NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(ILogger logger, int herId)
        {
            throw new NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(ILogger logger, int herId, bool forceUpdate)
        {
            throw new NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(ILogger logger, int herId)
        {
            return Task.FromResult(new CertificateDetails
            {
                Certificate = TestCertificates.GetCertificate(TestCertificates.HelsenorgeSignatureThumbprint),
                HerId = herId
            });
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(ILogger logger, int herId, bool forceUpdate)
        {
            throw new NotImplementedException();
        }

        public Task PingAsync(ILogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
