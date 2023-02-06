using System;
using System.Threading.Tasks;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries.Abstractions;

namespace ReceiveDecryptAndValidate
{
    public class MockAddressRegistry : IAddressRegistry
    {
        public Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId)
        {
            throw new NotImplementedException();
        }

        public Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId, bool forceUpdate)
        {
            throw new NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId)
        {
            throw new NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId, bool forceUpdate)
        {
            throw new NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId)
        {
            return Task.FromResult(new CertificateDetails
            {
                Certificate = TestCertificates.GetCertificate(TestCertificates.HelsenorgeSignatureThumbprint),
                HerId = herId
            });
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId, bool forceUpdate)
        {
            throw new NotImplementedException();
        }

        public Task PingAsync()
        {
            throw new NotImplementedException();
        }
    }
}
