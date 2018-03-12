using System;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.Security
{
    [TestClass]
    [DeploymentItem(@"Files", @"Files")]
    public class SignThenEncryptMessageProtectionTests
    {
        private XDocument _content;
        private SignThenEncryptMessageProtection _protection;
        [TestInitialize]
        public void Setup()
        {
            _content = new XDocument(new XElement("SomeDummyXml"));
            _protection = new SignThenEncryptMessageProtection();
        }

        [TestMethod]
        [TestCategory("X509Chain")]
        public void Protect_And_Unprotect_OK()
        {
            var stream = _protection.Protect(
                _content, 
                TestCertificates.HelsenorgePublicEncryption,
                TestCertificates.CounterpartyPrivateSigntature);

            var result = _protection.Unprotect(
                stream, 
                TestCertificates.HelsenorgePrivateEncryption,
                TestCertificates.CounterpartyPublicSignature, null);

            Assert.AreEqual(_content.ToString(), result.ToString());
        }

        [TestMethod]
        [TestCategory("X509Chain")]
        public void Protect_And_Unprotect_UsingLegacy_OK()
        {
            var stream = _protection.Protect(
                _content,
                TestCertificates.HelsenorgePublicEncryption,
                TestCertificates.CounterpartyPrivateSigntature);

            var result = _protection.Unprotect(
                stream,
                TestCertificates.CounterpartyPrivateEncryption, // set some random as the primary
                TestCertificates.CounterpartyPublicSignature,
                TestCertificates.HelsenorgePrivateEncryption); // set the actual as legacy

            Assert.AreEqual(_content.ToString(), result.ToString());
        }

        [TestMethod]
        [TestCategory("X509Chain")]
        [ExpectedException(typeof(CertificateException))]
        public void Protect_And_Unprotect_WrongSigningCertificate()
        {
            var stream = _protection.Protect(
                _content,
                TestCertificates.HelsenorgePublicEncryption,
                TestCertificates.CounterpartyPrivateSigntature);

            const string raw = "MIIE3jCCA8agAwIBAgILCE2BUrKlJGrOxOgwDQYJKoZIhvcNAQELBQAwSzELMAkGA1UEBhMCTk8xHTAbBgNVBAoMFEJ1eXBhc3MgQVMtOTgzMTYzMzI3MR0wGwYDVQQDDBRCdXlwYXNzIENsYXNzIDMgQ0EgMzAeFw0xNjAxMTgwOTA3NTZaFw0xOTAxMTgyMjU5MDBaMHIxCzAJBgNVBAYTAk5PMRswGQYDVQQKDBJOT1JTSyBIRUxTRU5FVFQgU0YxFTATBgNVBAsMDFRFU1RTRU5URVJFVDEbMBkGA1UEAwwSTk9SU0sgSEVMU0VORVRUIFNGMRIwEAYDVQQFEwk5OTQ1OTg3NTkwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCZ34VMBCzmHwmvMWwq0YhtNaEz19PxcEq3ImbCLWZx0zIf2hp8ZSDQy23KpgTumrTebeXEW5b1ig4THXizKzDtwirV5ssO441U7hvTXr+Bm1GYpRc1Q0vzZbKg41Nje5cq+kAovq3H8nnJ3csdjFS5QWKKz1hyUL9V6mZiR1eMVLWbOL2gBR6rjB0OgpoXtF9wmb2Z9So+srAyqnpRy9xBumBFdqvx3+8iZp8G9FH0TPgzeEPreLX5tdKZL0J/Z7+zWXqCx+Fu1PoKMkdw+aYJCVtUJPRXY1t4BpLKO0h6yXf7Rpky+sUQcJmKyagOBPZr9mqqjycYQg6JPSkcTo+XAgMBAAGjggGaMIIBljAJBgNVHRMEAjAAMB8GA1UdIwQYMBaAFMzD+Ae3nG16TvWnKx0F+bNHHJHRMB0GA1UdDgQWBBRpioossQ08OgpOuAl6/58qpAkvajAOBgNVHQ8BAf8EBAMCBkAwFQYDVR0gBA4wDDAKBghghEIBGgEDAjCBpQYDVR0fBIGdMIGaMC+gLaArhilodHRwOi8vY3JsLmJ1eXBhc3Mubm8vY3JsL0JQQ2xhc3MzQ0EzLmNybDBnoGWgY4ZhbGRhcDovL2xkYXAuYnV5cGFzcy5uby9kYz1CdXlwYXNzLGRjPU5PLENOPUJ1eXBhc3MlMjBDbGFzcyUyMDMlMjBDQSUyMDM/Y2VydGlmaWNhdGVSZXZvY2F0aW9uTGlzdDB6BggrBgEFBQcBAQRuMGwwMwYIKwYBBQUHMAGGJ2h0dHA6Ly9vY3NwLmJ1eXBhc3Mubm8vb2NzcC9CUENsYXNzM0NBMzA1BggrBgEFBQcwAoYpaHR0cDovL2NydC5idXlwYXNzLm5vL2NydC9CUENsYXNzM0NBMy5jZXIwDQYJKoZIhvcNAQELBQADggEBALPuCmA93Mi9NZFUFOaQz3PasTFLeLmtSXtt4Qp0TVtJuhqrlDeWYXDCsffMQoCAZXE3569/hdEgHPBVALo8xKS9vdwZR5SgIF+IivsEdC4ZYsq8C5VX4qq2WxW7yHNy3GYU8RBdOaztTfUliv7uaAeooP6EOPa6m+R+dgGfGnb5rM8NRyGgcAKDvC1YUFwdWaIgqO0gBB6WnSkhkyk0iX4tksUkbemQFcyMi2XDog6IFpkYt85MvfBklwjjufCiIcpkzHmuZCcYSLdwqi40Cz4QM5FE8zQYJJLco35A7NVW3MusyFImTleOlL10NH3XnqeLM8loa1Ph7YPl0SpiSjY=";
            var cert = new X509Certificate2(Convert.FromBase64String(raw));

            var result = _protection.Unprotect(
                stream,
                TestCertificates.HelsenorgePrivateEncryption,
                cert, 
                null);
        }

        [TestMethod]
        [TestCategory("X509Chain")]
        [ExpectedException(typeof(SecurityException))]
        public void Protect_And_Unprotect_WrongEncryptionCertificate()
        {
            var stream = _protection.Protect(
                _content,
                TestCertificates.CounterpartyPublicEncryption, //random encryption certificate
                TestCertificates.CounterpartyPrivateSigntature);

            var result = _protection.Unprotect(
                stream,
                TestCertificates.HelsenorgePrivateEncryption,
                TestCertificates.CounterpartyPublicSignature,
                null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Protect_Data_ArgumentNullException()
        {
            _protection.Protect(null, TestCertificates.HelsenorgePublicEncryption, TestCertificates.CounterpartyPrivateSigntature);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Protect_Encryption_ArgumentNullException()
        {
            _protection.Protect(_content, null, TestCertificates.CounterpartyPrivateSigntature);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Protect_Signature_ArgumentNullException()
        {
            _protection.Protect(_content, TestCertificates.HelsenorgePublicEncryption, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [TestCategory("X509Chain")]
        public void Unprotect_Data_ArgumentNullException()
        {
            var stream = _protection.Protect(
                _content,
                TestCertificates.HelsenorgePublicEncryption,
                TestCertificates.CounterpartyPrivateSigntature);

            _protection.Unprotect(null, TestCertificates.HelsenorgePrivateEncryption, TestCertificates.CounterpartyPublicSignature, null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [TestCategory("X509Chain")]
        public void Unprotect_Encryption_ArgumentNullException()
        {
            var stream = _protection.Protect(
                _content,
                TestCertificates.HelsenorgePublicEncryption,
                TestCertificates.CounterpartyPrivateSigntature);

            _protection.Unprotect(stream, null, TestCertificates.CounterpartyPublicSignature, null);
        }
        [TestMethod]
        [TestCategory("X509Chain")]
        public void Unprotect_Signature_ArgumentNullException()
        {
            var stream = _protection.Protect(
                _content,
                TestCertificates.HelsenorgePublicEncryption,
                TestCertificates.CounterpartyPrivateSigntature);

            var result = _protection.Unprotect(stream, TestCertificates.HelsenorgePrivateEncryption, null, null);
            Assert.AreEqual(_content.ToString(), result.ToString());
        }
    }
}
