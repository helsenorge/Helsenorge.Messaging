/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Registries.Abstractions;

namespace Helsenorge.Messaging.Tests.Mocks
{
    /// <summary>
    /// These certificates are not in test or production use, they have only been generated for test purposes.
    /// </summary>
    public static class TestCertificates
    {
        private static IDictionary<string, X509Certificate2> _certificates = new Dictionary<string, X509Certificate2>();

        public const string CounterpartyEncryptionThumbprint = "b1fae38326a6cefa72708f7633541262e8633b2c";
        public const string CounterpartySignatureThumbprint = "76b0195ba41374d5f372f4b70f907e7f9725fc02";

        public const string HelsenorgeEncryptionThumbprint = "fddbcfbb3231f0c66ee2168358229d3cac95e88a";
        public const string HelsenorgeSignatureThumbprint = "bd302b20fcdcf3766bf0bcba485dfb4b2bfe1379";

        public const string HelsenorgeLegacyEncryptionThumbprint = "0d9b26fd099d4e72a5efc9f9d8e8fc75ac95e88a";

        public static X509Certificate2 GetCertificate(string thumbnail)
        {
            if (_certificates.Count == 0)
                GenerateKnownCertificates();

            if (!_certificates.ContainsKey(thumbnail))
                throw new CertificateException(CertificateErrors.Missing, $"The certificate with thumbnail {thumbnail} is not a known certificate.");

            return _certificates[thumbnail];;
        }

        private static void GenerateKnownCertificates()
        {
            var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
            var notAfter = DateTimeOffset.UtcNow.AddMonths(1);
            _certificates.Add(CounterpartyEncryptionThumbprint, GenerateSelfSignedCertificate(CounterpartyEncryptionThumbprint, X509KeyUsageFlags.KeyEncipherment, notBefore, notAfter));
            _certificates.Add(CounterpartySignatureThumbprint, GenerateSelfSignedCertificate(CounterpartySignatureThumbprint, X509KeyUsageFlags.NonRepudiation, notBefore, notAfter));
            _certificates.Add(HelsenorgeEncryptionThumbprint, GenerateSelfSignedCertificate(HelsenorgeEncryptionThumbprint, X509KeyUsageFlags.KeyEncipherment, notBefore, notAfter));
            _certificates.Add(HelsenorgeSignatureThumbprint, GenerateSelfSignedCertificate(HelsenorgeSignatureThumbprint, X509KeyUsageFlags.NonRepudiation, notBefore, notAfter));
            _certificates.Add(HelsenorgeLegacyEncryptionThumbprint, GenerateSelfSignedCertificate(HelsenorgeLegacyEncryptionThumbprint, X509KeyUsageFlags.KeyEncipherment,notBefore, notAfter));
        }

        public static X509Certificate2 GenerateSelfSignedCertificate(string commonName, X509KeyUsageFlags keyUsage, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            using var rsa = RSA.Create(2048);
            var certificateRequest = new CertificateRequest($"CN={commonName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            certificateRequest.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, true));
            var selfSignedCertificate = certificateRequest.CreateSelfSigned(notBefore, notAfter);
            return selfSignedCertificate;
        }
    }
}
