/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Security.Cryptography;
using System;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Helsenorge.Registries.Tests
{
    [TestClass]
    public class CertificateValidatorTests
    {
        private ILogger _logger;

        [TestInitialize]
        public void Setup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddDebug());
            var provider = serviceCollection.BuildServiceProvider();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<CertificateValidatorTests>();
        }

        [TestMethod]
        public void CertificateValidation_ArgumentNullException()
        {
            var validator = new CertificateValidator(_logger);
            var error = validator.Validate(null, X509KeyUsageFlags.NonRepudiation);
            Assert.AreEqual(CertificateErrors.Missing, error);
        }
        [TestMethod]
        public void CertificateValidation_None()
        {
            var validator = new CertificateValidator(_logger);
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignature,
                X509KeyUsageFlags.NonRepudiation);
            // Added RevokedUnknown as build server add this error
            Assert.IsTrue(error == CertificateErrors.None
                || error == CertificateErrors.RevokedUnknown);
        }
        [TestMethod]
        public void CertificateValidation_StartDate()
        {
            var validator = new CertificateValidator(_logger);
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignatureInvalidStart,
                X509KeyUsageFlags.NonRepudiation);
            // Added RevokedUnknown as build server add this error
            Assert.IsTrue(error == CertificateErrors.StartDate
                || error == (CertificateErrors.Usage | CertificateErrors.RevokedUnknown));
        }
        [TestMethod]
        public void CertificateValidation_EndDate()
        {
            var validator = new CertificateValidator(_logger);
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignatureInvalidEnd,
                X509KeyUsageFlags.NonRepudiation);
            // Added RevokedUnknown as build server add this error
            Assert.IsTrue(error == CertificateErrors.EndDate
                || error == (CertificateErrors.Usage | CertificateErrors.RevokedUnknown));
        }
        [TestMethod]
        public void CertificateValidation_Usage()
        {
            var validator = new CertificateValidator(_logger);
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignature,
                X509KeyUsageFlags.KeyEncipherment);
            // Added RevokedUnknown as build server add this error
            Assert.IsTrue(error == CertificateErrors.Usage
                || error == (CertificateErrors.Usage | CertificateErrors.RevokedUnknown));
        }
        [TestMethod]
        public void X509Certificate2Extensions_KeyUsage()
        {
            Assert.IsTrue(TestCertificates.CounterpartyPublicSignature.HasKeyUsage(X509KeyUsageFlags.NonRepudiation));
            Assert.IsFalse(TestCertificates.CounterpartyPublicSignature.HasKeyUsage(X509KeyUsageFlags.KeyEncipherment));
        }

        [TestMethod]
        public void CertificateValidation_Multiple()
        {
            var validator = new CertificateValidator(_logger);
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignatureInvalidStart,
                X509KeyUsageFlags.KeyEncipherment);
            // Added RevokedUnknown as build server add this error
            Assert.IsTrue(error == (CertificateErrors.StartDate | CertificateErrors.Usage) 
                || error == (CertificateErrors.StartDate | CertificateErrors.Usage | CertificateErrors.RevokedUnknown));
        }
    }
}
