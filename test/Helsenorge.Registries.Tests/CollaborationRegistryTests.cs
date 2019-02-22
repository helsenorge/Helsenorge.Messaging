using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.Mocks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Registries.Tests
{
    [TestClass]
    [DeploymentItem(@"Files", @"Files")]
    public class CollaborationRegistryTests
    {
        private CollaborationProtocolRegistryMock _registry;
        private LoggerFactory _loggerFactory;
        private ILogger _logger;

        [TestInitialize]
        public void Setup()
        {
            var settings = new CollaborationProtocolRegistrySettings()
            {
                UserName = "username",
                Password = "password",
                EndpointName = "BasicHttpBinding_ICommunicationPartyService",
                WcfConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None),
                CachingInterval = TimeSpan.FromSeconds(5),
                MyHerId = 93238 // matches a value in a CPA test file
            };

            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddDebug();
            _logger = _loggerFactory.CreateLogger<CollaborationRegistryTests>();

            var distributedCache = DistributedCacheFactory.Create();

            IAddressRegistry addressRegistry = AddressRegistryTests.GetDefaultAddressRegistryMock();
            _registry = new CollaborationProtocolRegistryMock(settings, distributedCache, addressRegistry);
            _registry.SetupFindAgreementById(i =>
            {
                var file = Path.Combine("Files", $"CPA_{i:D}.xml");
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
            _registry.SetupFindAgreementForCounterparty(i =>
            {
                var file = Path.Combine("Files", $"CPA_{i}.xml");
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
            _registry.SetupFindProtocolForCounterparty(i =>
            {
                if (i < 0)
                {
                    throw new FaultException(new FaultReason("Dummy fault from mock"));
                }
                var file = Path.Combine("Files", $"CPP_{i}.xml");
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_Settings_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();
            IAddressRegistry addressRegistry = new AddressRegistryMock(new AddressRegistrySettings(), distributedCache);

            new CollaborationProtocolRegistry(null, distributedCache, addressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_Cache_Null()
        {
            
            var distributedCache = DistributedCacheFactory.Create();
            IAddressRegistry addressRegistry = new AddressRegistryMock(new AddressRegistrySettings(), distributedCache);

            new CollaborationProtocolRegistry(new CollaborationProtocolRegistrySettings(), null, addressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_AddressRegistry_Null()
        {
            var distributedCache = DistributedCacheFactory.Create();

            new CollaborationProtocolRegistry(new CollaborationProtocolRegistrySettings(), distributedCache, null);
        }

        [TestMethod]
        public void Read_CollaborationProfile()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.AreEqual(profile.CpaId, Guid.Empty);
            Assert.AreEqual("Digitale innbyggertjenester", profile.Name);
            Assert.AreEqual(15, profile.Roles.Count);
            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.IsNotNull(profile.EncryptionCertificate);

            var role = profile.Roles[0];
            Assert.AreEqual("DIALOG_INNBYGGER_DIGITALBRUKERreceiver", role.Name);
            Assert.AreEqual("DIALOG_INNBYGGER_DIGITALBRUKERreceiver", role.RoleName);
            Assert.AreEqual("1.1", role.VersionString);
            Assert.AreEqual(new Version(1, 1), role.Version);
            Assert.AreEqual("1.1", role.ProcessSpecification.VersionString);
            Assert.AreEqual(new Version(1, 1), role.ProcessSpecification.Version);
            Assert.AreEqual("Dialog_Innbygger_Digitalbruker", role.ProcessSpecification.Name);


            Assert.AreEqual(2, role.ReceiveMessages.Count);
            Assert.AreEqual(2, role.SendMessages.Count);
            var message = role.ReceiveMessages[0];
            Assert.AreEqual("DIALOG_INNBYGGER_DIGITALBRUKER", message.Name);
            Assert.AreEqual("sb.test.nhn.no/DigitalDialog/93238_async", message.DeliveryChannel);
            Assert.AreEqual(DeliveryProtocol.Amqp, message.DeliveryProtocol);

            var part = message.Parts.First();
            Assert.AreEqual(1, part.MaxOccurrence);
            Assert.AreEqual(0, part.MinOccurrence);
            Assert.AreEqual("http://www.kith.no/xmlstds/msghead/2006-05-24", part.XmlNamespace);
            Assert.AreEqual("MsgHead-v1_2.xsd", part.XmlSchema);
        }

        [TestMethod]
        public void Read_CollaborationProfile_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 1234).Result;
            Assert.IsNotNull(profile);
            Assert.AreEqual("DummyCollaborationProtocolProfile", profile.Name);
        }

        [TestMethod, Ignore]
        public void Read_CollaborationProfile_FromCache()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile);

            // if it's not found in cache, it will cause an exception
            _registry.SetupFindProtocolForCounterparty(i =>
            {
                throw new NotImplementedException();
            });
            profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile);
        }

        [TestMethod]
        public void Read_CollaborationAgreement_ById()
        {
            var profile = _registry.FindAgreementByIdAsync(_logger, Guid.Parse("49391409-e528-4919-b4a3-9ccdab72c8c1")).Result;

            Assert.AreEqual("Testlege Testlege", profile.Name);
            Assert.AreEqual(93252, profile.HerId);
            Assert.AreEqual(14, profile.Roles.Count);

            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.IsNotNull(profile.EncryptionCertificate);
        }
        [TestMethod]
        public void Read_CollaborationAgreement_ByCounterparty()
        {
            var profile = _registry.FindAgreementForCounterpartyAsync(_logger, 93252).Result;

            Assert.AreEqual(profile.HerId, 93252);
            Assert.AreEqual(profile.Name, "Testlege Testlege");
            Assert.AreEqual(profile.CpaId, new Guid("{9333f3de-e85c-4c26-9066-6800055b1b8e}"));
            Assert.IsNotNull(profile.EncryptionCertificate);
            Assert.IsNotNull(profile.SignatureCertificate);
            Assert.AreEqual(profile.Roles.Count, 14);
        }
        [TestMethod]
        public void Read_CollaborationAgreement_NotFound()
        {
            var profile = _registry.FindAgreementForCounterpartyAsync(_logger, 1234).Result;
            Assert.IsNull(profile);
        }
        [TestMethod, Ignore]
        public void Read_CollaborationAgreement_FromCache()
        {
            var profile = _registry.FindAgreementForCounterpartyAsync(_logger, 93252).Result;
            Assert.IsNotNull(profile);

            // if it's not found in cache, it will cause an exception
            _registry.SetupFindAgreementForCounterparty(i =>
            {
                throw new NotImplementedException();
            });
            profile = _registry.FindAgreementForCounterpartyAsync(_logger, 93252).Result;
            Assert.IsNotNull(profile);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FindMessageForSender_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile.FindMessageForSender(null));
        }
        [TestMethod]
        public void FindMessageForSender_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile.FindMessageForSender("DIALOG_INNBYGGER_EKONTAKT"));
        }
        [TestMethod]
        public void FindMessageForSender_Found_AppRec()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile.FindMessageForSender("APPREC"));
        }
        [TestMethod]
        public void FindMessageForSender_Found_Ny_CPP()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            var collaborationProtocolMessage = profile.FindMessageForReceiver("DIALOG_INNBYGGER_BEHANDLEROVERSIKT");
            Assert.IsNotNull(collaborationProtocolMessage);
            Assert.AreEqual("Svar", collaborationProtocolMessage.Name);
        }
        [TestMethod]
        public void FindMessageForSender_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNull(profile.FindMessageForSender("BOB"));
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FindMessageForReceiver_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile.FindMessageForReceiver(null));
        }
        [TestMethod]
        public void FindMessageForReceiver_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile.FindMessageForReceiver("DIALOG_INNBYGGER_EKONTAKT"));
        }
        [TestMethod]
        public void FindMessageForReceiver_Found_AppRec()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile.FindMessageForReceiver("APPREC"));
        }
        [TestMethod]
        public void FindMessageForReceiver_Found_Ny_CPP()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            var collaborationProtocolMessage = profile.FindMessageForSender("DIALOG_INNBYGGER_BEHANDLEROVERSIKT");
            Assert.IsNotNull(collaborationProtocolMessage);
            Assert.AreEqual("Hent", collaborationProtocolMessage.Name);
        }
        [TestMethod]
        public void FindMessageForReceiver_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNull(profile.FindMessageForReceiver("BOB"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FindMessagePartsForReceiveMessage_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile.FindMessagePartsForReceiveMessage(null));
        }
        [TestMethod]
        public void FindMessagePartsForReceiveMessage_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForReceiveMessage("DIALOG_INNBYGGER_EKONTAKT").Any());
        }
        [TestMethod]
        public void FindMessagePartsForReceiveMessage_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNull(profile.FindMessagePartsForReceiveMessage("BOB"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FindMessagePartsForReceiveAppRec_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile.FindMessagePartsForReceiveAppRec(null));
        }
        [TestMethod]
        public void FindMessagePartsForReceiveAppRec_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForReceiveAppRec("DIALOG_INNBYGGER_EKONTAKT").Any());
        }
        [TestMethod]
        public void FindMessagePartsForReceiveAppRec_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNull(profile.FindMessagePartsForReceiveAppRec("BOB"));
        }

        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FindMessagePartsForSendMessage_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile.FindMessagePartsForSendMessage(null));
        }
        [TestMethod]
        public void FindMessagePartsForSendMessage_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForSendMessage("DIALOG_INNBYGGER_EKONTAKT").Any());
        }
        [TestMethod]
        public void FindMessagePartsForSendMessage_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNull(profile.FindMessagePartsForSendMessage("BOB"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FindMessagePartsForSendAppRec_ArgumentNull()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNotNull(profile.FindMessagePartsForSendAppRec(null));
        }
        [TestMethod]
        public void FindMessagePartsForSendAppRec_Found()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsTrue(profile.FindMessagePartsForSendAppRec("DIALOG_INNBYGGER_EKONTAKT").Any());
        }
        [TestMethod]
        public void FindMessagePartsForSendAppRec_NotFound()
        {
            var profile = _registry.FindProtocolForCounterpartyAsync(_logger, 93238).Result;
            Assert.IsNull(profile.FindMessagePartsForSendAppRec("BOB"));
        }
    }
}