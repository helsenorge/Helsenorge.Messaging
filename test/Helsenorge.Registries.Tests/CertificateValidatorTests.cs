/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Security.Cryptography.X509Certificates;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.Tests.Mocks;
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
            var validator = new CertificateValidator(new MockX509Chain(), _logger);
            var error = validator.Validate(null, X509KeyUsageFlags.NonRepudiation);
            Assert.AreEqual(CertificateErrors.Missing, error);
        }
        [TestMethod]
        public void CertificateValidation_None()
        {
            var validator = new CertificateValidator(new MockX509Chain(), _logger);
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignature,
                X509KeyUsageFlags.NonRepudiation);
            Assert.AreEqual(CertificateErrors.None, error);
        }
        [TestMethod]
        public void CertificateValidation_StartDate()
        {
            var validator = new CertificateValidator(new MockX509Chain(), _logger);
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignatureInvalidStart,
                X509KeyUsageFlags.NonRepudiation);
            Assert.AreEqual(CertificateErrors.StartDate, error);
        }
        [TestMethod]
        public void CertificateValidation_EndDate()
        {
            var validator = new CertificateValidator(new MockX509Chain(), _logger);
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignatureInvalidEnd,
                X509KeyUsageFlags.NonRepudiation);
            Assert.AreEqual(CertificateErrors.EndDate, error);
        }
        [TestMethod]
        public void CertificateValidation_Usage()
        {
            var validator = new CertificateValidator(new MockX509Chain(), _logger);
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignature,
                X509KeyUsageFlags.KeyEncipherment);
            Assert.AreEqual(CertificateErrors.Usage, error);
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
            var validator = new CertificateValidator(new MockX509Chain(), _logger);
            var error = validator.Validate(TestCertificates.CounterpartyPublicSignatureInvalidStart,
                X509KeyUsageFlags.KeyEncipherment);
            Assert.AreEqual(CertificateErrors.StartDate | CertificateErrors.Usage, error);
        }

        [TestMethod]
        public void CertificateValidation_RevokeOffline()
        {
            var usage = X509KeyUsageFlags.NonRepudiation;
            var testCertificate = TestCertificates.CounterpartyPublicSignature;

            var mockChain = new MockX509Chain();
            mockChain.SetChainStatus(new[]
            {
                new X509ChainStatus
                {
                    Status = X509ChainStatusFlags.OfflineRevocation,
                    StatusInformation = "Offline revocation"
                }
            });
            var validator = new CertificateValidator(mockChain, _logger);
            var error = validator.Validate(testCertificate, usage);
            Assert.AreEqual(CertificateErrors.RevokedOffline, error);
        }

        [TestMethod]
        public void CertificateValidation_RevokeMultiple()
        {
            var usage = X509KeyUsageFlags.NonRepudiation;
            var testCertificate = TestCertificates.CounterpartyPublicSignature;

            var mockChain = new MockX509Chain();
            mockChain.SetChainStatus(new[]
            {
                new X509ChainStatus
                {
                    Status = X509ChainStatusFlags.OfflineRevocation,
                    StatusInformation = "Offline revocation"
                },
                new X509ChainStatus
                {
                Status = X509ChainStatusFlags.Revoked,
                StatusInformation = "Revoked"
                }
            });
            var validator = new CertificateValidator(mockChain, _logger);
            var error = validator.Validate(testCertificate, usage);
            Assert.AreEqual(CertificateErrors.RevokedOffline | CertificateErrors.Revoked, error);
        }

        [TestMethod]
        public void CertificateValidation_ChainStatusOther()
        {
            var usage = X509KeyUsageFlags.NonRepudiation;
            var testCertificate = TestCertificates.CounterpartyPublicSignature;

            var mockChain = new MockX509Chain();
            mockChain.SetChainStatus(new[]
            {
                new X509ChainStatus
                {
                    Status = X509ChainStatusFlags.HasWeakSignature,
                    StatusInformation = "Has weak signature"
                },
                new X509ChainStatus
                {
                    Status = X509ChainStatusFlags.InvalidExtension,
                    StatusInformation = "Invalid extension"
                }
            });
            var validator = new CertificateValidator(mockChain, _logger);
            var error = validator.Validate(testCertificate, usage);
            Assert.AreEqual(CertificateErrors.None, error);
        }
    }
}
