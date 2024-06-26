/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.Configuration;
using Helsenorge.Registries.HelseId;
using Helsenorge.Registries.Tests.Mocks;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Registries.Tests
{
    [TestClass]
    public class CollaborationRegistryRestTests
    {
        private CollaborationProtocolRegistryRestMock _registry;
        private IAddressRegistry addressRegistry;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private IHelseIdClient _helseIdClientCppa;

        [TestInitialize]
        public void Setup()
        {
            var collaborationSettings = new CollaborationProtocolRegistryRestSettings()
            {
                RestConfiguration = new RestConfiguration()
                {
                    Address = "https://localhost"
                },
                CachingInterval = TimeSpan.FromSeconds(5),
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddDebug());
            var loggingProvider = serviceCollection.BuildServiceProvider();
            _loggerFactory = loggingProvider.GetRequiredService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger<CollaborationRegistryRestTests>();

            var distributedCache = DistributedCacheFactory.CreatePartlyMockedDistributedCache();

            addressRegistry = AddressRegistryRestTests.GetDefaultAddressRegistryRestMock(_logger);

            var helseIdConfigCppa = new HelseIdConfiguration()
            {
                ClientId = "client-id",
                TokenEndpoint = "https://localhost",
                ScopeName = "testscope"
            };

            _helseIdClientCppa = new HelseIdClientMock();

            _registry = new CollaborationProtocolRegistryRestMock(collaborationSettings, distributedCache, addressRegistry, _logger, _helseIdClientCppa);
            _registry.SetupFindAgreementById(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_Rest_{i:D}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
            _registry.SetupFindAgreementForCounterparty(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_Rest_{i}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
            _registry.SetupFindProtocolForCounterparty(i =>
            {
                if (i < 0)
                {
                    throw new FaultException(new FaultReason("Dummy fault from mock"), new FaultCode("Client"), string.Empty);
                }
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPP_Rest_{i}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_Constructor_Settings_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();
            addressRegistry = new AddressRegistryRestMock(new AddressRegistryRestSettings(), distributedCache, _logger, _helseIdClientCppa);

            new CollaborationProtocolRegistryRest(null, distributedCache, addressRegistry, _logger, _helseIdClientCppa);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_Constructor_Cache_Null()
        {

            var distributedCache = DistributedCacheFactory.Create();
            addressRegistry = new AddressRegistryRestMock(new AddressRegistryRestSettings(), distributedCache, _logger, _helseIdClientCppa);

            new CollaborationProtocolRegistryRest(new CollaborationProtocolRegistryRestSettings(), null, addressRegistry, _logger, _helseIdClientCppa);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_Constructor_AddressRegistry_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            new CollaborationProtocolRegistryRest(new CollaborationProtocolRegistryRestSettings(), distributedCache, null, _logger, _helseIdClientCppa);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_Constructor_ILogger_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            new CollaborationProtocolRegistryRest(new CollaborationProtocolRegistryRestSettings(), distributedCache, addressRegistry, null, _helseIdClientCppa);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_Constructor_HelseIdClient_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            new CollaborationProtocolRegistryRest(new CollaborationProtocolRegistryRestSettings(), distributedCache, addressRegistry, _logger, null);
        }

        [TestMethod]
        public void RestCpa_Read_CollaborationProfile_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93252).Result;
            Assert.IsNull(profile);
        }

        [TestMethod]
        public void RestCpa_Read_MessageFunctionExceptionProfile_Found()
        {
            var profile = DummyCollaborationProtocolProfileFactory.CreateAsync(addressRegistry, _logger, 93238, "NO_CPA_MESSAGE").Result;
            Assert.IsNull(profile);
        }

        [TestMethod]
        public void RestCpa_Read_DummyCollaborationProfile_Found()
        {
            var profile = DummyCollaborationProtocolProfileFactory.CreateAsync(addressRegistry, _logger, 93238, null).Result;
            Assert.IsNull(profile);
        }

        [TestMethod]
        public void RestCpa_Read_CollaborationProfile_FromCache()
        {
            _registry.CertificateValidator = new MockCertificateValidator();
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile);

            // if it's not found in cache, it will cause an exception
            _registry.SetupFindProtocolForCounterparty(i =>
            {
                throw new NotImplementedException();
            });
            profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile);
        }

        [TestMethod]
        public void RestCpa_Read_CollaborationAgreement_ById()
        {
            var profile = _registry.FindAgreementByIdAsync(Guid.Parse("49391409-e528-4919-b4a3-9ccdab72c8c1"), 93238).Result;

            Assert.AreEqual(93252, profile.HerId);
            Assert.AreEqual("Testlege Testlege", profile.Name);
            Assert.AreEqual(14, profile.Roles.Count);

            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.IsNotNull(profile.EncryptionCertificate);
        }

        [TestMethod]
        public void RestCpa_Read_CollaborationAgreement_ByCounterparty()
        {
            var profile = _registry.FindAgreementForCounterpartyAsync(93238, 93252).Result;

            Assert.AreEqual(93252, profile.HerId);
            Assert.AreEqual("Testlege Testlege", profile.Name);
            Assert.AreEqual(new Guid("{9333f3de-e85c-4c26-9066-6800055b1b8e}"), profile.CpaId);
            Assert.IsNotNull(profile.EncryptionCertificate);
            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.AreEqual(profile.Roles.Count, 14);
        }

        [TestMethod]
        public void RestCpa_Read_CollaborationAgreement_NotFound()
        {
            var profile = _registry.FindAgreementForCounterpartyAsync(5678, 1234).Result;
            Assert.IsNull(profile);
        }

        [TestMethod]
        public void RestCpa_Read_CollaborationAgreement_FromCache()
        {
            _registry.CertificateValidator = new MockCertificateValidator();
            var profile = _registry.FindAgreementForCounterpartyAsync(5678, 93252).Result;
            Assert.IsNotNull(profile);

            // if it's not found in cache, it will cause an exception
            _registry.SetupFindAgreementForCounterparty(i =>
            {
                throw new NotImplementedException();
            });
            profile = _registry.FindAgreementForCounterpartyAsync(5678, 93252).Result;
            Assert.IsNotNull(profile);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_FindMessageForSender_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessageForSender(null));
        }

        [TestMethod]
        public void RestCpa_FindMessageForSender_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessageForSender("DIALOG_INNBYGGER_EKONTAKT"));
        }

        [TestMethod]
        public void RestCpa_FindMessageForSender_Found_AppRec()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessageForSender("APPREC"));
        }

        [TestMethod]
        public void RestCpa_FindMessageForSender_Found_Ny_CPPA()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            var collaborationProtocolMessage = profile.FindMessageForReceiver("DIALOG_INNBYGGER_BEHANDLEROVERSIKT");
            Assert.IsNotNull(collaborationProtocolMessage);
            Assert.AreEqual("DIALOG_INNBYGGER_BEHANDLEROVERSIKT", collaborationProtocolMessage.Name);
            Assert.AreEqual("Svar", collaborationProtocolMessage.Action);
        }

        [TestMethod]
        public void RestCpa_FindMessageForSender_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessageForSender("BOB"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_FindMessageForReceiver_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessageForReceiver(null));
        }

        [TestMethod]
        public void RestCpa_FindMessageForReceiver_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessageForReceiver("DIALOG_INNBYGGER_EKONTAKT"));
        }

        [TestMethod]
        public void RestCpa_FindMessageForReceiver_Found_AppRec()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessageForReceiver("APPREC"));
        }

        [TestMethod]
        public void RestCpa_FindMessageForReceiver_Found_Ny_CPPA()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            var collaborationProtocolMessage = profile.FindMessageForSender("DIALOG_INNBYGGER_BEHANDLEROVERSIKT");
            Assert.IsNotNull(collaborationProtocolMessage);
            Assert.AreEqual("DIALOG_INNBYGGER_BEHANDLEROVERSIKT", collaborationProtocolMessage.Name);
            Assert.AreEqual("Hent", collaborationProtocolMessage.Action);
        }

        [TestMethod]
        public void RestCpa_FindMessageForReceiver_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessageForReceiver("BOB"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_FindMessagePartsForReceiveMessage_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessagePartsForReceiveMessage(null));
        }

        [TestMethod]
        public void RestCpa_FindMessagePartsForReceiveMessage_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForReceiveMessage("DIALOG_INNBYGGER_EKONTAKT").Any());
        }

        [TestMethod]
        public void RestCpa_FindMessagePartsForReceiveMessage_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessagePartsForReceiveMessage("BOB"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_FindMessagePartsForReceiveAppRec_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessagePartsForReceiveAppRec(null));
        }

        [TestMethod]
        public void RestCpa_FindMessagePartsForReceiveAppRec_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForReceiveAppRec("DIALOG_INNBYGGER_EKONTAKT").Any());
        }

        [TestMethod]
        public void RestCpa_FindMessagePartsForReceiveAppRec_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessagePartsForReceiveAppRec("BOB"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_FindMessagePartsForSendMessage_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessagePartsForSendMessage(null));
        }

        [TestMethod]
        public void RestCpa_FindMessagePartsForSendMessage_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForSendMessage("DIALOG_INNBYGGER_EKONTAKT").Any());
        }

        [TestMethod]
        public void RestCpa_FindMessagePartsForSendMessage_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessagePartsForSendMessage("BOB"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RestCpa_FindMessagePartsForSendAppRec_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessagePartsForSendAppRec(null));
        }

        [TestMethod]
        public void RestCpa_FindMessagePartsForSendAppRec_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForSendAppRec("DIALOG_INNBYGGER_EKONTAKT").Any());
        }

        [TestMethod]
        public void RestCpa_FindMessagePartsForSendAppRec_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessagePartsForSendAppRec("BOB"));
        }

        [TestMethod]
        public void RestCpa_Read_CollaborationAgreement_v2_ById()
        {
            _registry.SetupFindAgreementById(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_v2_{i:D}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });

            var profile = _registry.FindAgreementByIdAsync(Guid.Parse("51795e2c-9d39-44e0-9168-5bee38f20819"), 5678).Result;

            Assert.AreEqual("Digitale innbyggertjenester", profile.Name);
            Assert.AreEqual(8093240, profile.HerId);
            Assert.AreEqual(32, profile.Roles.Count);
            Assert.AreEqual(4, profile.Roles[0].SendMessages.Count);
            Assert.AreEqual(5, profile.Roles[0].SendMessages[0].Parts.Count());
            Assert.AreEqual("MsgHead-v1_2.xsd", profile.Roles[0].SendMessages[0].Parts.ToList()[0].XmlSchema);

            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.IsNotNull(profile.EncryptionCertificate);
            Assert.IsTrue(profile.FindMessagePartsForReceiveMessage("DIALOG_INNBYGGER_TEST").Any());
        }

        [TestMethod]
        public void RestCpa_CaseInsensitive_Match_CollaborationAgreement_MessageFunction_To_MessagePart()
        {
            _registry.SetupFindAgreementById(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_v2_{i:D}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });

            var profile = _registry.FindAgreementByIdAsync(Guid.Parse("51795e2c-9d39-44e0-9168-5bee38f20819"), 5678).Result;

            Assert.AreEqual("Digitale innbyggertjenester", profile.Name);
            Assert.AreEqual(8093240, profile.HerId);
            Assert.AreEqual(32, profile.Roles.Count);
            Assert.AreEqual(4, profile.Roles[0].SendMessages.Count);
            Assert.AreEqual(5, profile.Roles[0].SendMessages[0].Parts.Count());
            Assert.AreEqual("MsgHead-v1_2.xsd", profile.Roles[0].SendMessages[0].Parts.ToList()[0].XmlSchema);

            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.IsNotNull(profile.EncryptionCertificate);
            Assert.IsTrue(profile.FindMessagePartsForReceiveMessage("AppRec").Any());
        }

        [TestMethod]
        public void RestCpa_Serialize_CollaborationProtocolProfile()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            var serialized = XmlCacheFormatter.Serialize(profile);
            var deserialized = XmlCacheFormatter.DeserializeAsync<CollaborationProtocolProfile>(serialized).Result;
            Assert.AreEqual(profile.CpaId, profile.CpaId);
            Assert.AreEqual(profile.EncryptionCertificate.Thumbprint, deserialized.EncryptionCertificate.Thumbprint);
        }

        [TestMethod]
        public void RestCpa_Use_Cached_CertificateDetails()
        {
            var key = Guid.NewGuid().ToString();
            var distributedCache = DistributedCacheFactory.Create();
            var data = Encoding.UTF8.GetBytes("Hello, World!");

            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            CacheExtensions.WriteValueToCacheAsync(_logger, distributedCache, key, profile, TimeSpan.FromDays(1)).Wait();
            var cached = CacheExtensions.ReadValueFromCacheAsync<Abstractions.CollaborationProtocolProfile>(_logger, distributedCache, key).Result;
            Assert.IsNotNull(cached);
            using (var rsa = cached.EncryptionCertificate.GetRSAPublicKey())
            {
                var encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
                Assert.IsNotNull(encrypted);
                Assert.IsTrue(encrypted.Length > 0);
            }
        }
    }
}
