using Helsenorge.Messaging.Abstractions;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Messaging.Tests.Mocks
{
    public class MockCertificateStore : ICertificateStore
    {
        public X509Certificate2 GetCertificate(object thumbprint)
        {
            if (thumbprint == null) throw new ArgumentNullException(nameof(thumbprint));
            if (!(thumbprint is string)) throw new ArgumentException("Argument is expected to be of type string.", nameof(thumbprint));

            string tp = thumbprint.ToString();
            if (string.IsNullOrWhiteSpace(tp)) throw new ArgumentException($"Argument '{nameof(thumbprint)}' must contain a value.", nameof(thumbprint));

            X509Certificate2 certificate = null;
            switch (tp)
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
