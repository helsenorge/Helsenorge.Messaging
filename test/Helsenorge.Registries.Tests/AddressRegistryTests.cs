/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel;
using System.Xml.Linq;
using Helsenorge.Registries.Configuration;
using Helsenorge.Registries.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;

namespace Helsenorge.Registries.Tests
{
    [TestClass]
    public class AddressRegistryTests
    {
        private AddressRegistryMock _registry;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;

        internal static AddressRegistryMock GetDefaultAddressRegistryMock()
        {
            var settings = new AddressRegistrySettings()
            {
                WcfConfiguration = new WcfConfiguration
                {
                    UserName = "username",
                    Password = "password",
                },
                CachingInterval = TimeSpan.FromSeconds(5)
            };

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var distributedCache = DistributedCacheFactory.Create();

            var registry = new AddressRegistryMock(settings, distributedCache);

            registry.SetupFindCommunicationPartyDetails(i =>
            {
                if (i < 0)
                {
                    throw new FaultException(new FaultReason("Her-ID expected to an integer of positive value."), new FaultCode("Client"), string.Empty);
                }
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CommunicationDetails_{i}.xml"));
                return File.Exists(file) == false ? null : XElement.Load(file);
            });

            registry.SetupGetCertificateDetailsForEncryption(i =>
            {
                if (i < 0)
                {
                    throw new FaultException(new FaultReason("Her-ID expected to an integer of positive value."), new FaultCode("Client"), string.Empty);
                }
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"GetCertificateDetailsForEncryption_{i}.xml"));
                return File.Exists(file) == false ? null : XElement.Load(file);
            });

            registry.SetupGetCertificateDetailsForValidatingSignature(i =>
            {
                if (i < 0)
                {
                    throw new FaultException(new FaultReason("Her-ID expected to an integer of positive value."), new FaultCode("Client"), string.Empty);
                }
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"GetCertificateDetailsForValidatingSignature_{i}.xml"));
                return File.Exists(file) == false ? null : XElement.Load(file);
            });

            return registry;
        }

        [TestInitialize]
        public void Setup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddDebug());
            var provider = serviceCollection.BuildServiceProvider();
            _loggerFactory = provider.GetRequiredService<ILoggerFactory>();            
            _logger = _loggerFactory.CreateLogger<AddressRegistryTests>();
            
            _registry = GetDefaultAddressRegistryMock();
        }
        [TestMethod]
        public void Read_CommunicationDetails_Found()
        {
            var result = _registry.FindCommunicationPartyDetailsAsync(_logger, 93252).Result;

            Assert.AreEqual("Alexander Dahl", result.Name);
            Assert.AreEqual(93252, result.HerId);
            Assert.AreEqual("sb.test.nhn.no/DigitalDialog/93252_async", result.AsynchronousQueueName);
            Assert.AreEqual("sb.test.nhn.no/DigitalDialog/93252_sync", result.SynchronousQueueName);
            Assert.AreEqual("sb.test.nhn.no/DigitalDialog/93252_error", result.ErrorQueueName);
        }
        [TestMethod]
        public void Read_CommunicationDetails_NotFound()
        {
            var result = _registry.FindCommunicationPartyDetailsAsync(_logger, 1234).Result;

            Assert.IsNull(result);
        }
        [TestMethod]
        //[ExpectedException(typeof(RegistriesException))]
        public void Read_CommunicationDetails_Exception()
        {
            var task = _registry.FindCommunicationPartyDetailsAsync(_logger, -4);

            try
            {
                Task.WaitAll(task);
            }
            catch (AggregateException ex )
            {
                var x = ex.InnerException;
            }
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_Settings_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            new AddressRegistry(null, distributedCache);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_Cache_Null()
        {
            new AddressRegistry(new AddressRegistrySettings(), null);
        }

        [TestMethod, Ignore]
        public void Read_GetCertificateDetailsForEncryption_Found()
        {
            var certificateDetails = _registry.GetCertificateDetailsForEncryptionAsync(_logger, 93252).Result;

            Assert.IsNotNull(certificateDetails);
            Assert.AreEqual(93252, certificateDetails.HerId);
            Assert.IsNotNull(certificateDetails.Certificate);
        }

        [TestMethod]
        public void Read_GetCertificateDetailsForEncryption_NotFound()
        {
            var certificateDetails = _registry.GetCertificateDetailsForEncryptionAsync(_logger, 2234).Result;

            Assert.IsNull(certificateDetails);
        }

        [TestMethod]
        public void Read_GetCertificateDetailsForEncryption_ExpectRegistriesException()
        {
            try
            {
                var certificateDetails = _registry.GetCertificateDetailsForEncryptionAsync(_logger, -1).Result;
            }
            catch (AggregateException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(RegistriesException));
            }
        }

        [TestMethod, Ignore]
        public void Read_GetCertificateDetailsForValidatingSignature_Found()
        {
            var certificateDetails = _registry.GetCertificateDetailsForValidatingSignatureAsync(_logger, 93252).Result;

            Assert.IsNotNull(certificateDetails);
            Assert.AreEqual(93252, certificateDetails.HerId);
            Assert.IsNotNull(certificateDetails.Certificate);
        }

        [TestMethod]
        public void Read_GetCertificateDetailsForValidatingSignature_NotFound()
        {
            var certificateDetails = _registry.GetCertificateDetailsForValidatingSignatureAsync(_logger, 1234).Result;

            Assert.IsNull(certificateDetails);
        }

        [TestMethod]
        public void Read_GetCertificateDetailsForValidatingSignature_ExpectRegistriesException()
        {
            try
            {
                var certificateDetails = _registry.GetCertificateDetailsForValidatingSignatureAsync(_logger, -1).Result;
            }
            catch (AggregateException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(RegistriesException));
            }
        }
    } 
}
