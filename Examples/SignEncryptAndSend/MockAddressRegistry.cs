using System.Collections.Generic;
using System.Threading.Tasks;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries.Abstractions;

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

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId, ICertificateValidator certificateValidator = null)
        {
            return Task.FromResult(new CertificateDetails
            {
                Certificate = TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint),
                HerId = herId,
            });
        }

        public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId, bool forceUpdate, ICertificateValidator certificateValidator = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId, ICertificateValidator certificateValidator = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId, bool forceUpdate, ICertificateValidator certificateValidator = null)
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

        public Task<OrganizationDetails> GetOrganizationDetailsAsync(int herId, bool forceUpdate = false)
        {
            throw new System.NotImplementedException();
        }
    }
}
