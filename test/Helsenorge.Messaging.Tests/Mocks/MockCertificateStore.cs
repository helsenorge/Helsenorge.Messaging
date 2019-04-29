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
                case "be26ea7a3eb54e7b00c55782304765777f854192":
                    certificate = TestCertificates.HelsenorgePrivateEncryptionInvalidStart;
                    break;
                case "be26ea7a3eb54e7b00c55782304765777f854191":
                    certificate = TestCertificates.HelsenorgePrivateEncryptionInvalidEnd;
                    break;
                case "fddbcfbb3231f0c66ee2168358229d3cac95e88a":
                    certificate = TestCertificates.HelsenorgePrivateEncryption;
                    break;
                case "bd302b20fcdcf3766bf0bcba485dfb4b2bfe1379":
                    certificate = TestCertificates.HelsenorgePrivateSigntature;
                    break;
                case "b1fae38326a6cefa72708f7633541262e8633b2c":
                    certificate = TestCertificates.CounterpartyPrivateEncryption;
                    break;
            }

            return certificate;
        }
    }
}
