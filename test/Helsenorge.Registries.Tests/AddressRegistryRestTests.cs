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
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Registries.Configuration;
using Helsenorge.Registries.HelseId;
using Helsenorge.Registries.Tests.Mocks;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Caching.Distributed;
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
            var _helseIdClientCpp = new HelseIdClientMock();

            var _registry = new AddressRegistryRestMock(settings, distributedCache, logger, _helseIdClientCpp);

            _registry.SetupFindCommunicationPartyDetails(i =>
            {
                if (i < 0)
                {
                    throw new FaultException(new FaultReason("Her-ID expected to an integer of positive value."), new FaultCode("Client"), string.Empty);
                }
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CommunicationDetailsRest_{i}.json"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });

            return _registry;
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
        public void Read_CommunicationDetails_Found()
        {
            var result = _registry.FindCommunicationPartyDetailsAsync(8141791).Result;

            Assert.AreEqual("Jonny Karl Rønhovde", result.Name);
            Assert.AreEqual(8141791, result.HerId);
            Assert.AreEqual("8141703_async", result.AsynchronousQueueName);
            Assert.AreEqual("8141703_sync", result.SynchronousQueueName);
            Assert.AreEqual("8141703_error", result.ErrorQueueName);
        }
        [TestMethod]
        public void Read_CommunicationDetails_NotFound()
        {
            var result = _registry.FindCommunicationPartyDetailsAsync(1234).Result;

            Assert.IsNull(result);
        }
        [TestMethod]
        //[ExpectedException(typeof(RegistriesException))]
        public void Read_CommunicationDetails_Exception()
        {
            var task = _registry.FindCommunicationPartyDetailsAsync(-4);

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

            new AddressRegistryRest(null, distributedCache, _logger, _helseIdClientCpp);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_Cache_Null()
        {
            new AddressRegistryRest(new AddressRegistryRestSettings(), null, _logger, _helseIdClientCpp);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_Logger_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            new AddressRegistryRest(new AddressRegistryRestSettings(), distributedCache, null, _helseIdClientCpp);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_HelseIdClient_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            new AddressRegistryRest(new AddressRegistryRestSettings(), distributedCache, _logger, null);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void GetCertificateDetailsForEncryptionAsync()
        {
            var registry = GetDefaultAddressRegistryRestMock(_logger);
            registry.GetCertificateDetailsForEncryptionAsync(2222);
        }

        
        [TestMethod]
        public void Serialize_AddressService_CommunicationParty()
        {
            var item = new AddressService.CommunicationParty
            {
                Active = true,
                HerId = 1234,
                ParentHerId = 4321,
            };

            var serialized = XmlCacheFormatter.Serialize(item);
            var deserialized = XmlCacheFormatter.DeserializeAsync<AddressService.CommunicationParty>(serialized).Result;
            Assert.AreEqual(item.Active, deserialized.Active);
            Assert.AreEqual(item.HerId, deserialized.HerId);
            Assert.AreEqual(item.ParentHerId, deserialized.ParentHerId);
        }

        [TestMethod]
        public void Serialize_AddressService_CommunicationPartyDetails()
        {
            var item = new Abstractions.CommunicationPartyDetails
            {
                Name = "Name",
                HerId = 1234,
                ParentHerId = 4321,
                ParentName = "Parent Name",
                SynchronousQueueName = "1234_sync",
                AsynchronousQueueName = "1234_async",
                ErrorQueueName = "1234_error",
            };

            var serialized = XmlCacheFormatter.Serialize(item);
            var deserialized =
                XmlCacheFormatter.DeserializeAsync<Abstractions.CommunicationPartyDetails>(serialized).Result;
            Assert.AreEqual(item.Name, deserialized.Name);
            Assert.AreEqual(item.HerId, deserialized.HerId);
            Assert.AreEqual(item.ParentHerId, deserialized.ParentHerId);
            Assert.AreEqual(item.ParentName, deserialized.ParentName);
            Assert.AreEqual(item.SynchronousQueueName, deserialized.SynchronousQueueName);
            Assert.AreEqual(item.AsynchronousQueueName, deserialized.AsynchronousQueueName);
            Assert.AreEqual(item.ErrorQueueName, deserialized.ErrorQueueName);
        }

        [TestMethod]
        public void Serialize_AddressService_CertificateDetails()
        {
            var keys = ECDsa.Create();
            var request = new CertificateRequest("CN=foo", keys, HashAlgorithmName.SHA256);
            var cert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

            var serviceDetails = new AddressService.CertificateDetails
            {
                Certificate = cert.GetRawCertData(),
                LdapUrl = "ldap://CN=foo",
            };

            var serialized = XmlCacheFormatter.Serialize(serviceDetails);
            var deserialized = XmlCacheFormatter.DeserializeAsync<AddressService.CertificateDetails>(serialized).Result;
            var deserializedCert = new X509Certificate2(deserialized.Certificate);
            Assert.AreEqual(serviceDetails.LdapUrl, deserialized.LdapUrl);
            Assert.AreEqual(cert.Thumbprint, deserializedCert.Thumbprint);
        }

        [TestMethod]
        public void Serialize_Abstractions_CertificateDetails()
        {
            var keys = ECDsa.Create();
            var request = new CertificateRequest("CN=foo", keys, HashAlgorithmName.SHA256);
            var cert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

            var serviceDetails = new Abstractions.CertificateDetails
            {
                Certificate = cert,
                HerId = 1234,
                LdapUrl = "ldap://CN=foo",
            };

            var serialized = XmlCacheFormatter.Serialize(serviceDetails);
            var deserialized = XmlCacheFormatter.DeserializeAsync<Abstractions.CertificateDetails>(serialized).Result;
            var deserializedCert = deserialized.Certificate;
            Assert.AreEqual(cert.Thumbprint, deserializedCert.Thumbprint);
            Assert.AreEqual(serviceDetails.HerId, deserialized.HerId);
            Assert.AreEqual(cert.Thumbprint, deserializedCert.Thumbprint);
        }
    }
}
