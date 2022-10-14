/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.Security
{
    [TestClass]
    public class SignThenEncryptMessageProtectionTests
    {
        public SignThenEncryptMessageProtectionTests()
        {
             partyASignatureCertificate = TestCertificates.GenerateX509Certificate2(X509KeyUsageFlags.NonRepudiation, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddMonths(1));
             partyAEncryptionCertificate = TestCertificates.GenerateX509Certificate2(X509KeyUsageFlags.KeyEncipherment, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddMonths(1));
             helseNorgeEncryptionCertificate = TestCertificates.GenerateX509Certificate2(X509KeyUsageFlags.KeyEncipherment, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddMonths(1));
             helseNorgeSignatureCertificate = TestCertificates.GenerateX509Certificate2(X509KeyUsageFlags.NonRepudiation, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddMonths(1));

        }
        private XDocument _content;
        private X509Certificate2 partyASignatureCertificate;
        private X509Certificate2 partyAEncryptionCertificate;
        private X509Certificate2 helseNorgeEncryptionCertificate;
        private X509Certificate2 helseNorgeSignatureCertificate;

        [TestInitialize]
        public void Setup()
        {
            _content = new XDocument(new XElement("SomeDummyXml"));
        }

        [TestMethod]
        [TestCategory("X509Chain")]
        public void Protect_And_Unprotect_OK()
        {
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));


            var partyAProtection = new SignThenEncryptMessageProtection(partyASignatureCertificate, partyAEncryptionCertificate);
            var stream = partyAProtection.Protect(
                contentStream, 
                helseNorgeEncryptionCertificate);

            var partyBProtection = new SignThenEncryptMessageProtection(helseNorgeSignatureCertificate, helseNorgeEncryptionCertificate);
            var result = partyBProtection.Unprotect(
                stream, 
                partyASignatureCertificate);
            
            Assert.AreEqual(_content.ToString(), result.ToXDocument().ToString());
        }

        [TestMethod]
        [TestCategory("X509Chain")]
        public void Protect_And_Unprotect_UsingLegacy_OK()
        {
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(partyASignatureCertificate, partyAEncryptionCertificate);
            var stream = partyAProtection.Protect(contentStream, helseNorgeEncryptionCertificate);

            var partyBProtection = new SignThenEncryptMessageProtection(
                helseNorgeSignatureCertificate,
                helseNorgeEncryptionCertificate, 
                helseNorgeEncryptionCertificate);  // Legacy certificate
            var result = partyBProtection.Unprotect(stream,partyASignatureCertificate);

            Assert.AreEqual(_content.ToString(), result.ToXDocument().ToString());
        }

        [TestMethod]
        [TestCategory("X509Chain")]
        [ExpectedException(typeof(CertificateMessagePayloadException))]
        public void Protect_And_Unprotect_WrongSigningCertificate()
        {
            const string wrongCertificateBase64 = "MIIE3jCCA8agAwIBAgILCE2BUrKlJGrOxOgwDQYJKoZIhvcNAQELBQAwSzELMAkGA1UEBhMCTk8xHTAbBgNVBAoMFEJ1eXBhc3MgQVMtOTgzMTYzMzI3MR0wGwYDVQQDDBRCdXlwYXNzIENsYXNzIDMgQ0EgMzAeFw0xNjAxMTgwOTA3NTZaFw0xOTAxMTgyMjU5MDBaMHIxCzAJBgNVBAYTAk5PMRswGQYDVQQKDBJOT1JTSyBIRUxTRU5FVFQgU0YxFTATBgNVBAsMDFRFU1RTRU5URVJFVDEbMBkGA1UEAwwSTk9SU0sgSEVMU0VORVRUIFNGMRIwEAYDVQQFEwk5OTQ1OTg3NTkwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCZ34VMBCzmHwmvMWwq0YhtNaEz19PxcEq3ImbCLWZx0zIf2hp8ZSDQy23KpgTumrTebeXEW5b1ig4THXizKzDtwirV5ssO441U7hvTXr+Bm1GYpRc1Q0vzZbKg41Nje5cq+kAovq3H8nnJ3csdjFS5QWKKz1hyUL9V6mZiR1eMVLWbOL2gBR6rjB0OgpoXtF9wmb2Z9So+srAyqnpRy9xBumBFdqvx3+8iZp8G9FH0TPgzeEPreLX5tdKZL0J/Z7+zWXqCx+Fu1PoKMkdw+aYJCVtUJPRXY1t4BpLKO0h6yXf7Rpky+sUQcJmKyagOBPZr9mqqjycYQg6JPSkcTo+XAgMBAAGjggGaMIIBljAJBgNVHRMEAjAAMB8GA1UdIwQYMBaAFMzD+Ae3nG16TvWnKx0F+bNHHJHRMB0GA1UdDgQWBBRpioossQ08OgpOuAl6/58qpAkvajAOBgNVHQ8BAf8EBAMCBkAwFQYDVR0gBA4wDDAKBghghEIBGgEDAjCBpQYDVR0fBIGdMIGaMC+gLaArhilodHRwOi8vY3JsLmJ1eXBhc3Mubm8vY3JsL0JQQ2xhc3MzQ0EzLmNybDBnoGWgY4ZhbGRhcDovL2xkYXAuYnV5cGFzcy5uby9kYz1CdXlwYXNzLGRjPU5PLENOPUJ1eXBhc3MlMjBDbGFzcyUyMDMlMjBDQSUyMDM/Y2VydGlmaWNhdGVSZXZvY2F0aW9uTGlzdDB6BggrBgEFBQcBAQRuMGwwMwYIKwYBBQUHMAGGJ2h0dHA6Ly9vY3NwLmJ1eXBhc3Mubm8vb2NzcC9CUENsYXNzM0NBMzA1BggrBgEFBQcwAoYpaHR0cDovL2NydC5idXlwYXNzLm5vL2NydC9CUENsYXNzM0NBMy5jZXIwDQYJKoZIhvcNAQELBQADggEBALPuCmA93Mi9NZFUFOaQz3PasTFLeLmtSXtt4Qp0TVtJuhqrlDeWYXDCsffMQoCAZXE3569/hdEgHPBVALo8xKS9vdwZR5SgIF+IivsEdC4ZYsq8C5VX4qq2WxW7yHNy3GYU8RBdOaztTfUliv7uaAeooP6EOPa6m+R+dgGfGnb5rM8NRyGgcAKDvC1YUFwdWaIgqO0gBB6WnSkhkyk0iX4tksUkbemQFcyMi2XDog6IFpkYt85MvfBklwjjufCiIcpkzHmuZCcYSLdwqi40Cz4QM5FE8zQYJJLco35A7NVW3MusyFImTleOlL10NH3XnqeLM8loa1Ph7YPl0SpiSjY=";
            var wrongCertificate = new X509Certificate2(Convert.FromBase64String(wrongCertificateBase64));
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(partyASignatureCertificate, partyAEncryptionCertificate);
            var stream = partyAProtection.Protect(
                contentStream,
                helseNorgeEncryptionCertificate);
            
            var partyBProtection = new SignThenEncryptMessageProtection(helseNorgeSignatureCertificate, helseNorgeEncryptionCertificate);
            var result = partyBProtection.Unprotect(stream, wrongCertificate);
        }

        [TestMethod]
        [TestCategory("X509Chain")]
        [ExpectedException(typeof(SecurityException))]
        public void Protect_And_Unprotect_WrongEncryptionCertificate()
        {
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(partyASignatureCertificate, partyAEncryptionCertificate);
            // Random encryption certificate -> TestCertificates.CounterpartyPublicEncryption
            var stream = partyAProtection.Protect(contentStream, partyAEncryptionCertificate);

            var partyBProtection = new SignThenEncryptMessageProtection(helseNorgeSignatureCertificate, helseNorgeEncryptionCertificate);
            var result = partyBProtection.Unprotect(stream, partyASignatureCertificate);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Protect_Data_ArgumentNullException()
        {
            var partyAProtection = new SignThenEncryptMessageProtection(partyASignatureCertificate, partyAEncryptionCertificate);
            partyAProtection.Protect(null, helseNorgeSignatureCertificate);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Protect_Encryption_ArgumentNullException()
        {
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(partyASignatureCertificate, partyAEncryptionCertificate);
            partyAProtection.Protect(contentStream, null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Protect_Signature_ArgumentNullException()
        {
            new SignThenEncryptMessageProtection(null, partyAEncryptionCertificate);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [TestCategory("X509Chain")]
        public void Unprotect_Data_ArgumentNullException()
        {
            var partyBProtection = new SignThenEncryptMessageProtection(partyASignatureCertificate, partyAEncryptionCertificate);
            partyBProtection.Unprotect(null, partyASignatureCertificate);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [TestCategory("X509Chain")]
        public void Unprotect_Encryption_ArgumentNullException()
        {
            new SignThenEncryptMessageProtection(helseNorgeSignatureCertificate, null);
        }
        [TestMethod]
        [TestCategory("X509Chain")]
        public void Unprotect_Signature_MissingPublicKeySignatureCertificate()
        {
            MemoryStream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(_content.ToString()));

            var partyAProtection = new SignThenEncryptMessageProtection(partyASignatureCertificate, partyAEncryptionCertificate);
            var stream = partyAProtection.Protect(contentStream, helseNorgeEncryptionCertificate);

            var partyBProtection = new SignThenEncryptMessageProtection(helseNorgeSignatureCertificate,helseNorgeEncryptionCertificate);
            var result = partyBProtection.Unprotect(stream, null);

            Assert.AreEqual(_content.ToString(), result.ToXDocument().ToString());
        }
    }
}
