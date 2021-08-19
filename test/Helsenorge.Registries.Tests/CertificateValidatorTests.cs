/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Security.Cryptography.X509Certificates;
using Helsenorge.Registries.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Registries.Tests
{
    [TestClass]
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
        [TestCategory("X509Chain"), Ignore]
        public void CertificateValidation_StartDate()
        {
            var validator = new CertificateValidator();
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignatureInvalidStart,
                X509KeyUsageFlags.NonRepudiation);
            Assert.AreEqual(CertificateErrors.StartDate, error);
        }
        [TestMethod]
        [TestCategory("X509Chain"), Ignore]
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
                X509KeyUsageFlags.KeyEncipherment);
            Assert.AreEqual(CertificateErrors.Usage, error);
        }
        [TestMethod]
        [TestCategory("X509Chain")]
        public void X509Certificate2Extensions_KeyUsage()
        {
            Assert.IsTrue(TestCertificates.CounterpartyPublicSignature.HasKeyUsage(X509KeyUsageFlags.NonRepudiation));
            Assert.IsFalse(TestCertificates.CounterpartyPublicSignature.HasKeyUsage(X509KeyUsageFlags.DataEncipherment));
        }
        // don't have a certificate with multiple errors
        //[TestMethod]
        //public void CertificateValidation_Multiple()
        //{
        //	var validator = new CertificateValidator();
        //	var error = validator.Validate(TestCertificates.HelsenorgePublicEncryptionInvalid,
        //		X509KeyUsageFlags.KeyEncipherment);
        //	Assert.AreEqual(CertificateErrors.StartDate | CertificateErrors.Usage, error);
        //}

        // missing certificate that has been revoked
    }
}
