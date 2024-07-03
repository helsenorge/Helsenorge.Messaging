/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Threading.Tasks;
using Helsenorge.Registries.Configuration;
using Helsenorge.Registries.HelseId;
using Helsenorge.Registries.Tests.Mocks;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Registries.Tests
{
    [TestClass]
    public class AddressRegistryRestTests
    {
        private AddressRegistryRestMock _registry;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private IHelseIdClient _helseIdClientCpp;


        internal static AddressRegistryRestMock GetDefaultAddressRegistryRestMock(ILogger logger)
        {
            var settings = new AddressRegistryRestSettings()
            {
                RestConfiguration = new RestConfiguration()
                {
                    Address = "https://localhost"
                },
                CachingInterval = TimeSpan.FromSeconds(5),
            };

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var distributedCache = DistributedCacheFactory.CreatePartlyMockedDistributedCache();
            var helseIdClientCpp = new HelseIdClientMock();

            var registry = new AddressRegistryRestMock(settings, distributedCache, logger, helseIdClientCpp);

            registry.SetupFindCommunicationPartyDetails(i =>
            {
                if (i < 0)
                {
                    throw new FaultException(new FaultReason("Her-ID expected to an integer of positive value."), new FaultCode("Client"), string.Empty);
                }
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CommunicationDetailsRest_{i}.json"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
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
            _logger = _loggerFactory.CreateLogger<AddressRegistryRestTests>();
            
            _registry = GetDefaultAddressRegistryRestMock(_logger);
        }

        [TestMethod]
        public void RestAr_Read_CommunicationDetails_Found()
        {
            var result = _registry.FindCommunicationPartyDetailsAsync(8141791).Result;

            Assert.AreEqual("Jonny Karl Rønhovde", result.Name);
            Assert.AreEqual(8141791, result.HerId);
            Assert.AreEqual("tb.test.nhn.no/NHNTESTServiceBus/8141703_async", result.AsynchronousQueueName);
            Assert.AreEqual("tb.test.nhn.no/NHNTESTServiceBus/8141703_sync", result.SynchronousQueueName);
            Assert.AreEqual("tb.test.nhn.no/NHNTESTServiceBus/8141703_error", result.ErrorQueueName);
        }

        [TestMethod]
        public void RestAr_Read_CommunicationDetails_NotFound()
        {
            var result = _registry.FindCommunicationPartyDetailsAsync(1234).Result;

            Assert.IsNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(RegistriesException))]
        public async Task RestAr_CommunicationDetails_Exception()
        {
            await _registry.FindCommunicationPartyDetailsAsync(-4);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestAr_Constructor_Settings_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            new AddressRegistryRest(null, distributedCache, _logger, _helseIdClientCpp);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestAr_Constructor_Cache_Null()
        {
            new AddressRegistryRest(new AddressRegistryRestSettings(), null, _logger, _helseIdClientCpp);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestAr_Constructor_Logger_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            new AddressRegistryRest(new AddressRegistryRestSettings(), distributedCache, null, _helseIdClientCpp);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestAr_Constructor_HelseIdClient_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            new AddressRegistryRest(new AddressRegistryRestSettings(), distributedCache, _logger, null);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task RestAr_GetCertificateDetailsForEncryptionAsync_ShouldThrowNotImplementedException()
        {
            var _registry = GetDefaultAddressRegistryRestMock(_logger);
            await _registry.GetCertificateDetailsForEncryptionAsync(2222);
        }

        [TestMethod]
        public async Task RestAr_GetCertificateDetailsForValidatingSignatureAsync_ShouldThrowNotImplementedException()
        {
            var registry = GetDefaultAddressRegistryRestMock(_logger);
            await Assert.ThrowsExceptionAsync<NotImplementedException>(() => registry.GetCertificateDetailsForValidatingSignatureAsync(2222));
            await Assert.ThrowsExceptionAsync<NotImplementedException>(() => registry.GetCertificateDetailsForValidatingSignatureAsync(2222, false));
        }

        [TestMethod]
        public async Task RestAr_SearchByIdAsync_ShouldThrowNotImplementedException()
        {
            var registry = GetDefaultAddressRegistryRestMock(_logger);
            await Assert.ThrowsExceptionAsync<NotImplementedException>(() => registry.SearchByIdAsync("2222", false));
        }

        [TestMethod]
        public async Task RestAr_GetOrganizationDetailsAsync_ShouldThrowNotImplementedException()
        {
            var registry = GetDefaultAddressRegistryRestMock(_logger);
            await Assert.ThrowsExceptionAsync<NotImplementedException>(() => registry.GetOrganizationDetailsAsync(2222, false));
        }
    }
}
