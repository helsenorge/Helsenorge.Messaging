/*
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.Security
{
    [TestClass]
    public class SignThenEncryptMessageProtectionTests
    {
        private XDocument _content;
        private ILogger _logger;
        [TestInitialize]
        public void Setup()
        {
            _content = new XDocument(new XElement("SomeDummyXml"));
            var mockProvider = new MockLoggerProvider(null);
            _logger = mockProvider.CreateLogger("Test logger");
        }

        [TestMethod]
        public void Protect_And_Unprotect_OK()
        {
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint), _logger);
            var stream = partyAProtection.Protect(contentStream, TestCertificates.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint));

            var partyBProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.HelsenorgeSignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint), _logger);
            var result = partyBProtection.Unprotect(
                stream, 
                TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint));
            
            Assert.AreEqual(_content.ToString(), result.ToXDocument().ToString());
        }

        [TestMethod]
        public void Protect_And_Unprotect_UsingLegacy_OK()
        {
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint), _logger);
            var stream = partyAProtection.Protect(contentStream, TestCertificates.GetCertificate(TestCertificates.HelsenorgeLegacyEncryptionThumbprint));

            var partyBProtection = new SignThenEncryptMessageProtection(
                TestCertificates.GetCertificate(TestCertificates.HelsenorgeSignatureThumbprint),
                TestCertificates.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint),
                _logger,
                TestCertificates.GetCertificate(TestCertificates.HelsenorgeLegacyEncryptionThumbprint));  // Legacy certificate
            var result = partyBProtection.Unprotect(stream, TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint));

            Assert.AreEqual(_content.ToString(), result.ToXDocument().ToString());
        }

        [TestMethod]
        public void Protect_And_Unprotect_WrongSigningCertificate()
        {
            const string wrongCertificateBase64 = "MIIE3jCCA8agAwIBAgILCE2BUrKlJGrOxOgwDQYJKoZIhvcNAQELBQAwSzELMAkGA1UEBhMCTk8xHTAbBgNVBAoMFEJ1eXBhc3MgQVMtOTgzMTYzMzI3MR0wGwYDVQQDDBRCdXlwYXNzIENsYXNzIDMgQ0EgMzAeFw0xNjAxMTgwOTA3NTZaFw0xOTAxMTgyMjU5MDBaMHIxCzAJBgNVBAYTAk5PMRswGQYDVQQKDBJOT1JTSyBIRUxTRU5FVFQgU0YxFTATBgNVBAsMDFRFU1RTRU5URVJFVDEbMBkGA1UEAwwSTk9SU0sgSEVMU0VORVRUIFNGMRIwEAYDVQQFEwk5OTQ1OTg3NTkwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCZ34VMBCzmHwmvMWwq0YhtNaEz19PxcEq3ImbCLWZx0zIf2hp8ZSDQy23KpgTumrTebeXEW5b1ig4THXizKzDtwirV5ssO441U7hvTXr+Bm1GYpRc1Q0vzZbKg41Nje5cq+kAovq3H8nnJ3csdjFS5QWKKz1hyUL9V6mZiR1eMVLWbOL2gBR6rjB0OgpoXtF9wmb2Z9So+srAyqnpRy9xBumBFdqvx3+8iZp8G9FH0TPgzeEPreLX5tdKZL0J/Z7+zWXqCx+Fu1PoKMkdw+aYJCVtUJPRXY1t4BpLKO0h6yXf7Rpky+sUQcJmKyagOBPZr9mqqjycYQg6JPSkcTo+XAgMBAAGjggGaMIIBljAJBgNVHRMEAjAAMB8GA1UdIwQYMBaAFMzD+Ae3nG16TvWnKx0F+bNHHJHRMB0GA1UdDgQWBBRpioossQ08OgpOuAl6/58qpAkvajAOBgNVHQ8BAf8EBAMCBkAwFQYDVR0gBA4wDDAKBghghEIBGgEDAjCBpQYDVR0fBIGdMIGaMC+gLaArhilodHRwOi8vY3JsLmJ1eXBhc3Mubm8vY3JsL0JQQ2xhc3MzQ0EzLmNybDBnoGWgY4ZhbGRhcDovL2xkYXAuYnV5cGFzcy5uby9kYz1CdXlwYXNzLGRjPU5PLENOPUJ1eXBhc3MlMjBDbGFzcyUyMDMlMjBDQSUyMDM/Y2VydGlmaWNhdGVSZXZvY2F0aW9uTGlzdDB6BggrBgEFBQcBAQRuMGwwMwYIKwYBBQUHMAGGJ2h0dHA6Ly9vY3NwLmJ1eXBhc3Mubm8vb2NzcC9CUENsYXNzM0NBMzA1BggrBgEFBQcwAoYpaHR0cDovL2NydC5idXlwYXNzLm5vL2NydC9CUENsYXNzM0NBMy5jZXIwDQYJKoZIhvcNAQELBQADggEBALPuCmA93Mi9NZFUFOaQz3PasTFLeLmtSXtt4Qp0TVtJuhqrlDeWYXDCsffMQoCAZXE3569/hdEgHPBVALo8xKS9vdwZR5SgIF+IivsEdC4ZYsq8C5VX4qq2WxW7yHNy3GYU8RBdOaztTfUliv7uaAeooP6EOPa6m+R+dgGfGnb5rM8NRyGgcAKDvC1YUFwdWaIgqO0gBB6WnSkhkyk0iX4tksUkbemQFcyMi2XDog6IFpkYt85MvfBklwjjufCiIcpkzHmuZCcYSLdwqi40Cz4QM5FE8zQYJJLco35A7NVW3MusyFImTleOlL10NH3XnqeLM8loa1Ph7YPl0SpiSjY=";
#if NET9_0_OR_GREATER
            var wrongCertificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(wrongCertificateBase64));
#else
            var wrongCertificate = new X509Certificate2(Convert.FromBase64String(wrongCertificateBase64));
#endif
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint), _logger);
            var stream = partyAProtection.Protect(contentStream, TestCertificates.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint));
            
            var partyBProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.HelsenorgeSignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint), _logger);
            Assert.Throws<CertificateMessagePayloadException>(() =>
                partyBProtection.Unprotect(stream, wrongCertificate));
        }

        [TestMethod]
        public void Protect_And_Unprotect_WrongEncryptionCertificate()
        {
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint), _logger);
            // Random encryption certificate -> TestCertificates.CounterpartyPublicEncryption
            var stream = partyAProtection.Protect(contentStream, TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint));

            var partyBProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.HelsenorgeSignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint), _logger);
            Assert.Throws<SecurityException>(() => partyBProtection.Unprotect(stream,
                TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint)));
        }

        [TestMethod]
        public void Protect_Data_ArgumentNullException()
        {
            var partyAProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint), _logger);
            Assert.Throws<ArgumentNullException>(() => partyAProtection.Protect(null,
                TestCertificates.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint)));
        }

        [TestMethod]
        public void Protect_Encryption_ArgumentNullException()
        {
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint), _logger);
            Assert.Throws<ArgumentNullException>(() => partyAProtection.Protect(contentStream, null));
        }

        [TestMethod]
        public void Protect_Signature_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SignThenEncryptMessageProtection(null,
                TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint), _logger));
        }

        [TestMethod]
        public void Unprotect_Data_ArgumentNullException()
        {
            var partyBProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint), _logger);
            Assert.Throws<ArgumentNullException>(() => partyBProtection.Unprotect(null,
                TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint)));
        }

        [TestMethod]
        public void Unprotect_Encryption_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SignThenEncryptMessageProtection(
                    TestCertificates.GetCertificate(TestCertificates.HelsenorgeSignatureThumbprint), null, _logger));
        }

        [TestMethod]
        public void Unprotect_Signature_MissingPublicKeySignatureCertificate()
        {
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.CounterpartySignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint), _logger);
            var stream = partyAProtection.Protect(contentStream, TestCertificates.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint));

            var partyBProtection = new SignThenEncryptMessageProtection(TestCertificates.GetCertificate(TestCertificates.HelsenorgeSignatureThumbprint), TestCertificates.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint), _logger);
            var result = partyBProtection.Unprotect(stream, null);

            Assert.AreEqual(_content.ToString(), result.ToXDocument().ToString());
        }

        [TestMethod]
        public void IsCertificateAuthority_WithBasicConstraintsCaTrue_ReturnsTrue()
        {
            using var caCertificate = CreateCertificate("CN=Test Buypass Class 3 CA 3", isCertificateAuthority: true);

            Assert.IsTrue(SignThenEncryptMessageProtection.IsCertificateAuthority(caCertificate));
        }

        [TestMethod]
        public void IsCertificateAuthority_WithBasicConstraintsCaFalse_ReturnsFalse()
        {
            using var leafCertificate = CreateCertificate("CN=Test Norsk Helsenett", isCertificateAuthority: false);

            Assert.IsFalse(SignThenEncryptMessageProtection.IsCertificateAuthority(leafCertificate));
        }

        [TestMethod]
        public void IsCertificateAuthority_SelfIssuedWithoutBasicConstraints_ReturnsTrue()
        {
            // A self-issued certificate (Subject == Issuer) without a BasicConstraints extension
            // should be treated as a root/CA certificate.
            using var selfIssued = CreateCertificate("CN=Test Root", isCertificateAuthority: null);

            Assert.IsTrue(SignThenEncryptMessageProtection.IsCertificateAuthority(selfIssued));
        }

        [TestMethod]
        public void ResolveActualSigningCertificate_UsesSignerInfo_WhenRootIsBundled()
        {
            // The message is signed by the leaf certificate, but the sender also bundles the root CA certificate. The old logic returned the last certificate in the collection (the root); the new logic must return the actual signer (the leaf).
            using var leafCertificate = CreateCertificate("CN=Test Norsk Helsenett Signing", isCertificateAuthority: false);
            using var rootCertificate = CreateCertificate("CN=Test Buypass Class 3 CA 3", isCertificateAuthority: true);

            var signedCms = new SignedCms(new ContentInfo(Encoding.UTF8.GetBytes(_content.ToString())));
            var signer = new CmsSigner(leafCertificate) { IncludeOption = X509IncludeOption.EndCertOnly };
            signedCms.ComputeSignature(signer);
            // Simulate the sender bundling the root certificate in addition to the leaf.
            signedCms.AddCertificate(rootCertificate);

            var actual = SignThenEncryptMessageProtection.ResolveActualSigningCertificate(signedCms);

            Assert.IsNotNull(actual);
            Assert.AreEqual(leafCertificate.Thumbprint, actual.Thumbprint);
        }

        [TestMethod]
        public void ResolveActualSigningCertificate_FallsBackToNonCaCertificate_WhenSignerNotEmbedded()
        {
            // When the signer certificate is not embedded in the message (SignerInfo.Certificate is null)
            // and both a CA and an end-entity certificate are bundled, we should report the non-CA (leaf) one.
            using var signingCertificate = CreateCertificate("CN=Test Signer Not Embedded", isCertificateAuthority: false);
            using var otherLeafCertificate = CreateCertificate("CN=Test Other Leaf", isCertificateAuthority: false);
            using var rootCertificate = CreateCertificate("CN=Test Buypass Class 3 CA 3", isCertificateAuthority: true);

            var signedCms = new SignedCms(new ContentInfo(Encoding.UTF8.GetBytes(_content.ToString())));
            var signer = new CmsSigner(signingCertificate) { IncludeOption = X509IncludeOption.None };
            signedCms.ComputeSignature(signer);
            // The signer certificate is intentionally NOT added; only the root and an unrelated leaf are bundled.
            signedCms.AddCertificate(rootCertificate);
            signedCms.AddCertificate(otherLeafCertificate);

            var actual = SignThenEncryptMessageProtection.ResolveActualSigningCertificate(signedCms);

            Assert.IsNotNull(actual);
            Assert.AreEqual(otherLeafCertificate.Thumbprint, actual.Thumbprint);
            Assert.AreNotEqual(rootCertificate.Thumbprint, actual.Thumbprint);
        }

        private static X509Certificate2 CreateCertificate(string subjectName, bool? isCertificateAuthority)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // isCertificateAuthority == null means "do not add a BasicConstraints extension at all".
            if (isCertificateAuthority.HasValue)
            {
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(isCertificateAuthority.Value, false, 0, true));
            }

            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }
    }
}
