using Helsenorge.Messaging.Abstractions;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Messaging.Tests.Mocks
{
    public class MockCertificateStore : ICertificateStore
    {
        public X509Certificate2 GetCertificate(string thumbprint)
        {
            X509Certificate2 certificate = null;
            switch (thumbprint)
            {
                case TestCertificates.HelsenorgeEncryptionInvalidStartThumbPrint:
                    certificate = TestCertificates.HelsenorgePrivateEncryptionInvalidStart;
                    break;
                case TestCertificates.HelsenorgeEncryptionInvalidEndThumbprint:
                    certificate = TestCertificates.HelsenorgePrivateEncryptionInvalidEnd;
                    break;
                case TestCertificates.HelsenorgeEncryptionThumbprint:
                    certificate = TestCertificates.HelsenorgePrivateEncryption;
                    break;
                case TestCertificates.HelsenorgeSigntatureThumbprint:
                    certificate = TestCertificates.HelsenorgePrivateSigntature;
                    break;
                case TestCertificates.CounterpartyEncryptionThumbprint:
                    certificate = TestCertificates.CounterpartyPrivateEncryption;
                    break;
            }

            return certificate;
        }
    }
}
