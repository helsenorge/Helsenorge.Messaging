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
using Helsenorge.Registries.Tests.Mocks;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Registries.Tests
{
    [TestClass]
    public class CollaborationRegistryTests
    {
        private CollaborationProtocolRegistryMock _registry;
        private IAddressRegistry _addressRegistry;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;

        [TestInitialize]
        public void Setup()
        {
            var settings = new CollaborationProtocolRegistrySettings()
            {
                WcfConfiguration = new WcfConfiguration
                {
                    UserName = "username",
                    Password = "password",
                },
                CachingInterval = TimeSpan.FromSeconds(5),
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddDebug());
            var provider = serviceCollection.BuildServiceProvider();
            _loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger<CollaborationRegistryTests>();

            var distributedCache = DistributedCacheFactory.CreatePartlyMockedDistributedCache();

            _addressRegistry = AddressRegistryTests.GetDefaultAddressRegistryMock(_logger);
            _registry = new CollaborationProtocolRegistryMock(settings, distributedCache, _addressRegistry, _logger);
            _registry.SetupFindAgreementById(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_{i:D}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
            _registry.SetupFindAgreementForCounterparty(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_{i}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
            _registry.SetupFindProtocolForCounterparty(i =>
            {
                if (i < 0)
                {
                    throw new FaultException(new FaultReason("Dummy fault from mock"), new FaultCode("Client"), string.Empty);
                }
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPP_{i}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
        }

        [TestMethod]
        public void Constructor_Settings_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();
            IAddressRegistry addressRegistry = new AddressRegistryMock(
                new AddressRegistrySettings { WcfConfiguration = new WcfConfiguration() }, distributedCache, _logger);

            Assert.Throws<ArgumentNullException>(() =>
                new CollaborationProtocolRegistry(null, distributedCache, addressRegistry, _logger));
        }

        [TestMethod]
        public void Constructor_Cache_Null()
        {

            var distributedCache = DistributedCacheFactory.Create();
            IAddressRegistry addressRegistry = new AddressRegistryMock(
                new AddressRegistrySettings { WcfConfiguration = new WcfConfiguration() }, distributedCache, _logger);

            Assert.Throws<ArgumentNullException>(() =>
                new CollaborationProtocolRegistry(new CollaborationProtocolRegistrySettings(), null, addressRegistry,
                    _logger));
        }

        [TestMethod]
        public void Constructor_AddressRegistry_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
                new CollaborationProtocolRegistry(new CollaborationProtocolRegistrySettings(), distributedCache, null,
                    _logger));
        }

        [TestMethod]
        public void Constructor_ILogger_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
                new CollaborationProtocolRegistry(new CollaborationProtocolRegistrySettings(), distributedCache,
                    _addressRegistry, null));
        }

        [TestMethod]
        public void Read_CollaborationProfile_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93252).Result;
            Assert.IsNotNull(profile);
            Assert.AreEqual("DummyCollaborationProtocolProfile", profile.Name);
        }

        [TestMethod]
        public void Read_MessageFunctionExceptionProfile_Found()
        {
            var profile = DummyCollaborationProtocolProfileFactory.CreateAsync(_addressRegistry, _logger, 93238, "NO_CPA_MESSAGE").Result;
            Assert.IsNotNull(profile);
            Assert.AreEqual("MessageFunctionExceptionProfile", profile.Name);
            Assert.AreEqual("NO_CPA_MESSAGE", profile.Roles.First().SendMessages.First().Name);
            Assert.AreEqual("NO_CPA_MESSAGE", profile.Roles.First().SendMessages.First().Action);
        }

        [TestMethod]
        public void Read_DummyCollaborationProfile_Found()
        {
            var profile = DummyCollaborationProtocolProfileFactory.CreateAsync(_addressRegistry, _logger, 93238, null).Result;
            Assert.IsNotNull(profile);
            Assert.AreEqual("DummyCollaborationProtocolProfile", profile.Name);
            Assert.AreEqual("APPREC", profile.Roles.First().SendMessages.First().Name);
            Assert.AreEqual("APPREC", profile.Roles.First().SendMessages.First().Action);
        }

        [TestMethod]
        public void Read_CollaborationProfile_FromCache()
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
        public void Read_CollaborationAgreement_ById()
        {
            var profile = _registry.FindAgreementByIdAsync(Guid.Parse("49391409-e528-4919-b4a3-9ccdab72c8c1"), 93238).Result;

            Assert.AreEqual(93252, profile.HerId);
            Assert.AreEqual("Testlege Testlege", profile.Name);
            Assert.HasCount(14, profile.Roles);

            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.IsNotNull(profile.EncryptionCertificate);
        }

        [TestMethod]
        public void Read_CollaborationAgreement_ByCounterparty()
        {
            var profile = _registry.FindAgreementForCounterpartyAsync(93238, 93252).Result;

            Assert.AreEqual(93252, profile.HerId);
            Assert.AreEqual("Testlege Testlege", profile.Name);
            Assert.AreEqual(new Guid("{9333f3de-e85c-4c26-9066-6800055b1b8e}"), profile.CpaId);
            Assert.IsNotNull(profile.EncryptionCertificate);
            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.HasCount(14, profile.Roles);
        }

        [TestMethod]
        public void Read_CollaborationAgreement_NotFound()
        {
            var profile = _registry.FindAgreementForCounterpartyAsync(5678, 1234).Result;
            Assert.IsNull(profile);
        }

        [TestMethod]
        public void Read_CollaborationAgreement_FromCache()
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
        public void FindMessageForSender_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.Throws<ArgumentNullException>(() => profile.FindMessageForSender(null));
        }

        [TestMethod]
        public void FindMessageForSender_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessageForSender("DIALOG_INNBYGGER_EKONTAKT"));
        }

        [TestMethod]
        public void FindMessageForSender_Found_AppRec()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessageForSender("APPREC"));
        }

        [TestMethod]
        public void FindMessageForSender_Found_Ny_CPPA()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            var collaborationProtocolMessage = profile.FindMessageForReceiver("DIALOG_INNBYGGER_BEHANDLEROVERSIKT");
            Assert.IsNotNull(collaborationProtocolMessage);
            Assert.AreEqual("DIALOG_INNBYGGER_BEHANDLEROVERSIKT", collaborationProtocolMessage.Name);
            Assert.AreEqual("Svar", collaborationProtocolMessage.Action);
        }

        [TestMethod]
        public void FindMessageForSender_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessageForSender("BOB"));
        }

        [TestMethod]
        public void FindMessageForReceiver_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.Throws<ArgumentNullException>(() => profile.FindMessageForReceiver(null));
        }

        [TestMethod]
        public void FindMessageForReceiver_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessageForReceiver("DIALOG_INNBYGGER_EKONTAKT"));
        }

        [TestMethod]
        public void FindMessageForReceiver_Found_AppRec()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNotNull(profile.FindMessageForReceiver("APPREC"));
        }

        [TestMethod]
        public void FindMessageForReceiver_Found_Ny_CPPA()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            var collaborationProtocolMessage = profile.FindMessageForSender("DIALOG_INNBYGGER_BEHANDLEROVERSIKT");
            Assert.IsNotNull(collaborationProtocolMessage);
            Assert.AreEqual("DIALOG_INNBYGGER_BEHANDLEROVERSIKT", collaborationProtocolMessage.Name);
            Assert.AreEqual("Hent", collaborationProtocolMessage.Action);
        }

        [TestMethod]
        public void FindMessageForReceiver_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessageForReceiver("BOB"));
        }

        [TestMethod]
        public void FindMessagePartsForReceiveMessage_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.Throws<ArgumentNullException>(() => profile.FindMessagePartsForReceiveMessage(null));
        }

        [TestMethod]
        public void FindMessagePartsForReceiveMessage_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForReceiveMessage("DIALOG_INNBYGGER_EKONTAKT").Any());
        }

        [TestMethod]
        public void FindMessagePartsForReceiveMessage_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessagePartsForReceiveMessage("BOB"));
        }

        [TestMethod]
        public void FindMessagePartsForReceiveAppRec_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.Throws<ArgumentNullException>(() => profile.FindMessagePartsForReceiveAppRec(null));
        }

        [TestMethod]
        public void FindMessagePartsForReceiveAppRec_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForReceiveAppRec("DIALOG_INNBYGGER_EKONTAKT").Any());
        }

        [TestMethod]
        public void FindMessagePartsForReceiveAppRec_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessagePartsForReceiveAppRec("BOB"));
        }

        [TestMethod]
        public void FindMessagePartsForSendMessage_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.Throws<ArgumentNullException>(() => profile.FindMessagePartsForSendMessage(null));
        }

        [TestMethod]
        public void FindMessagePartsForSendMessage_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForSendMessage("DIALOG_INNBYGGER_EKONTAKT").Any());
        }

        [TestMethod]
        public void FindMessagePartsForSendMessage_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessagePartsForSendMessage("BOB"));
        }

        [TestMethod]
        public void FindMessagePartsForSendAppRec_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.Throws<ArgumentNullException>(() => profile.FindMessagePartsForSendAppRec(null));
        }

        [TestMethod]
        public void FindMessagePartsForSendAppRec_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForSendAppRec("DIALOG_INNBYGGER_EKONTAKT").Any());
        }

        [TestMethod]
        public void FindMessagePartsForSendAppRec_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            Assert.IsNull(profile.FindMessagePartsForSendAppRec("BOB"));
        }

        [TestMethod]
        public void Read_CollaborationAgreement_v2_ById()
        {
            _registry.SetupFindAgreementById(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_v2_{i:D}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });

            var profile = _registry.FindAgreementByIdAsync(Guid.Parse("51795e2c-9d39-44e0-9168-5bee38f20819"), 5678).Result;

            Assert.AreEqual("Digitale innbyggertjenester", profile.Name);
            Assert.AreEqual(8093240, profile.HerId);
            Assert.HasCount(32, profile.Roles);
            Assert.HasCount(4, profile.Roles[0].SendMessages);
            Assert.AreEqual(5, profile.Roles[0].SendMessages[0].Parts.Count());
            Assert.AreEqual("MsgHead-v1_2.xsd", profile.Roles[0].SendMessages[0].Parts.ToList()[0].XmlSchema);

            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.IsNotNull(profile.EncryptionCertificate);
            Assert.IsTrue(profile.FindMessagePartsForReceiveMessage("DIALOG_INNBYGGER_TEST").Any());
        }

        [TestMethod]
        public void CaseInsensitive_Match_CollaborationAgreement_MessageFunction_To_MessagePart()
        {
            _registry.SetupFindAgreementById(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_v2_{i:D}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });

            var profile = _registry.FindAgreementByIdAsync(Guid.Parse("51795e2c-9d39-44e0-9168-5bee38f20819"), 5678).Result;

            Assert.AreEqual("Digitale innbyggertjenester", profile.Name);
            Assert.AreEqual(8093240, profile.HerId);
            Assert.HasCount(32, profile.Roles);
            Assert.HasCount(4, profile.Roles[0].SendMessages);
            Assert.AreEqual(5, profile.Roles[0].SendMessages[0].Parts.Count());
            Assert.AreEqual("MsgHead-v1_2.xsd", profile.Roles[0].SendMessages[0].Parts.ToList()[0].XmlSchema);

            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.IsNotNull(profile.EncryptionCertificate);
            Assert.IsTrue(profile.FindMessagePartsForReceiveMessage("AppRec").Any());
        }

        [TestMethod]
        public void Serialize_CollaborationProtocolProfile()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(93238).Result;
            var serialized = XmlCacheFormatter.Serialize(profile);
            var deserialized = XmlCacheFormatter.DeserializeAsync<CollaborationProtocolProfile>(serialized).Result;
            Assert.AreEqual(profile.CpaId, profile.CpaId);
            Assert.AreEqual(profile.EncryptionCertificate.Thumbprint, deserialized.EncryptionCertificate.Thumbprint);
        }

        [TestMethod]
        public void Use_Cached_CertificateDetails()
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
                Assert.IsNotEmpty(encrypted);
            }
        }
    }
}
