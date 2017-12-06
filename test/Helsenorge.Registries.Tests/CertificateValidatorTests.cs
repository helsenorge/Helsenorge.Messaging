using System;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Registries.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Registries.Tests
{
    [TestClass]
    [DeploymentItem(@"Files", @"Files")]
    public class CertificateValidatorTests
    {
        [TestMethod]
        public void CertificateValidation_ArgumentNullException()
        {
            var validator = new CertificateValidator();
            var error = validator.Validate(null, X509KeyUsageFlags.NonRepudiation);
            Assert.AreEqual(CertificateErrors.Missing, error);
        }
        [TestMethod]
        [TestCategory("X509Chain"), Ignore]
        public void CertificateValidation_None()
        {
            var validator = new CertificateValidator();
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignature,
                X509KeyUsageFlags.NonRepudiation);
            Assert.AreEqual(CertificateErrors.None, error);
        }
        [TestMethod]
        public void CertificateValidation_StartDate()
        {
            var validator = new CertificateValidator();
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignatureInvalidStart,
                X509KeyUsageFlags.NonRepudiation);
            Assert.AreEqual(CertificateErrors.StartDate, error);
        }
        [TestMethod]
        public void CertificateValidation_EndDate()
        {
            var validator = new CertificateValidator();
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignatureInvalidEnd,
                X509KeyUsageFlags.NonRepudiation);
            Assert.AreEqual(CertificateErrors.EndDate, error);
        }
        [TestMethod]
        [TestCategory("X509Chain"), Ignore]
        public void CertificateValidation_Usage()
        {
            var validator = new CertificateValidator();
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignature,
                X509KeyUsageFlags.DataEncipherment);
            Assert.AreEqual(CertificateErrors.Usage, error);
        }
        // don't have a certificate with multiple errors
        //[TestMethod]
        //public void CertificateValidation_Multiple()
        //{
        //	var validator = new CertificateValidator();
        //	var error = validator.Validate(TestCertificates.HelsenorgePublicEncryptionInvalid,
        //		X509KeyUsageFlags.DataEncipherment);
        //	Assert.AreEqual(CertificateErrors.StartDate | CertificateErrors.Usage, error);
        //}

        // missing certificate that has been revoked
    }
}
