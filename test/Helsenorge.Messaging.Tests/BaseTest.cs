using System;
using System.IO;
using Helsenorge.Registries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Helsenorge.Registries.Mocks;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Helsenorge.Registries.Abstractions;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Tests
{
    [TestClass]
    [DeploymentItem(@"Files", @"Files")]
    public class BaseTest
    {
        //public const int MyHerId = 93238;
        ///public const int OhterHerId = 93252;
        public Guid CpaId = new Guid("49391409-e528-4919-b4a3-9ccdab72c8c1");
        public const int DefaultOtherHerId = 93252;

        protected AddressRegistryMock AddressRegistry { get; private set; }
        protected CollaborationProtocolRegistryMock CollaborationRegistry { get; private set; }
        protected ILoggerFactory LoggerFactory { get; private set; }
        internal ILogger Logger { get; private set; }

        protected MessagingClient Client { get; set; }
        protected MessagingServer Server { get; set; }
        protected MessagingSettings Settings { get; set; }
        internal MockFactory MockFactory { get; set; }

        internal MockLoggerProvider MockLoggerProvider { get; set; }

        internal MockCertificateValidator CertificateValidator { get; set; }

        protected XDocument GenericMessage => new XDocument(new XElement("SomeDummyXmlUsedForTesting"));

        protected XDocument GenericResponse => new XDocument(new XElement("SomeDummyXmlResponseUsedForTesting"));

        protected XDocument SoapFault => XDocument.Load(File.OpenRead(@"Files\SoapFault.xml"));


        [TestInitialize]
        public virtual void Setup()
        {
            SetupInternal(DefaultOtherHerId);
        }

        internal void SetupInternal(int otherHerId)
		{
			var addressRegistrySettings = new AddressRegistrySettings()
			{
				UserName = "username",
				Password = "password",
				EndpointName = "SomeEndpointName",
				WcfConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None),
				CachingInterval = TimeSpan.FromSeconds(5)
			};
			var collaborationRegistrySettings = new CollaborationProtocolRegistrySettings()
			{
				UserName = "username",
				Password = "password",
				EndpointName = "SomeEndpointName",
				WcfConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None),
				CachingInterval = TimeSpan.FromSeconds(5)
			};

			LoggerFactory = new LoggerFactory();

			MockLoggerProvider = new MockLoggerProvider(null);
			LoggerFactory.AddProvider(MockLoggerProvider);

			//LoggerFactory.AddDebug();
			//Logger = new LoggerMock(LoggerFactory.CreateLogger<BaseTest>());
			Logger = LoggerFactory.CreateLogger<BaseTest>();

			var memoryCache = new MemoryCache(new MemoryCacheOptions());
			var distributedCache = new MemoryDistributedCache(memoryCache);

			AddressRegistry = new AddressRegistryMock(addressRegistrySettings, distributedCache);
			AddressRegistry.SetupFindCommunicationPartyDetails(i =>
			{
				var file = Path.Combine("Files", $"CommunicationDetails_{i}.xml");
				return File.Exists(file) == false ? null : XElement.Load(file);
			});
            AddressRegistry.SetupGetCertificateDetailsForEncryption(i =>
            {
                var path = Path.Combine("Files", $"GetCertificateDetailsForEncryption_{i}.xml");
                return File.Exists(path) == false ? null : XElement.Load(path);
            });
            AddressRegistry.SetupGetCertificateDetailsForValidatingSignature(i =>
            {
                var path = Path.Combine("Files", $"GetCertificateDetailsForValidatingSignature_{i}.xml");
                return File.Exists(path) == false ? null : XElement.Load(path);
            });

			CollaborationRegistry = new CollaborationProtocolRegistryMock(collaborationRegistrySettings, distributedCache, AddressRegistry);
			CollaborationRegistry.SetupFindProtocolForCounterparty(i => 
            {
                var path = Path.Combine("Files", $"CPP_{i}.xml");
                return File.Exists(path) == false ? null : File.ReadAllText(path);
            });
			CollaborationRegistry.SetupFindAgreementForCounterparty(i =>
            {
                var path = Path.Combine("Files", $"CPA_{i}.xml");
                return File.Exists(path) == false ? null : File.ReadAllText(path);
            });
            CollaborationRegistry.SetupFindAgreementById(i =>
            {
                var path = Path.Combine("Files", $"CPA_{i:D}.xml");
                return File.Exists(path) == false ? null : File.ReadAllText(path);
            });

			Settings = new MessagingSettings()
			{
				MyHerId = MockFactory.HelsenorgeHerId,
				SigningCertificate = new CertificateSettings()
				{
					Certificate = TestCertificates.HelsenorgePrivateSigntature
				},
				DecryptionCertificate = new CertificateSettings()
				{
					Certificate = TestCertificates.HelsenorgePrivateEncryption
				},
                DefaultDeliveryProtocol = DeliveryProtocol.Amqp
			};
			
			Settings.ServiceBus.ConnectionString = "connection string";
			Settings.ServiceBus.Synchronous.ReplyQueueMapping.Add(Environment.MachineName.ToLower(), "RepliesGoHere");
			// make things easier by only having one processing task per queue
			Settings.ServiceBus.Asynchronous.ProcessingTasks = 1;
			Settings.ServiceBus.Synchronous.ProcessingTasks = 1;
			Settings.ServiceBus.Error.ProcessingTasks = 1;

			MockFactory = new MockFactory(otherHerId);
			CertificateValidator = new MockCertificateValidator();

			Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry)
			{
				DefaultMessageProtection = new NoMessageProtection(),   // disable protection for most tests
				DefaultCertificateValidator = CertificateValidator
			};
			Client.ServiceBus.RegisterAlternateMessagingFactory(MockFactory);

			Server = new MessagingServer(Settings, Logger, LoggerFactory, CollaborationRegistry, AddressRegistry)
			{
				DefaultMessageProtection = new NoMessageProtection(),   // disable protection for most tests
				DefaultCertificateValidator = CertificateValidator
			};
			Server.ServiceBus.RegisterAlternateMessagingFactory(MockFactory);
		}

		internal MockMessage CreateMockMessage(OutgoingMessage message)
		{
			return new MockMessage(GenericResponse)
			{
				MessageFunction = message.MessageFunction,
				ApplicationTimestamp = DateTime.Now,
				ContentType = ContentType.SignedAndEnveloped,
				MessageId = Guid.NewGuid().ToString("D"),
				CorrelationId = message.MessageId,
				FromHerId = MockFactory.OtherHerId,
				ToHerId = MockFactory.HelsenorgeHerId,
				ScheduledEnqueueTimeUtc = DateTime.UtcNow,
				TimeToLive = TimeSpan.FromSeconds(15),
			};
		}

        protected void RunAndHandleException(Task task)
        {
            try
            {
                Task.WaitAll(task);
            }
            catch (AggregateException ex)
            {

                throw ex.InnerException;
            }
        }

        protected void RunAndHandleMessagingException(Task task, EventId id)
        {
            try
            {
                Task.WaitAll(task);
            }
            catch (AggregateException ex)
            {
                var messagingException = ex.InnerException as MessagingException;
                if ((messagingException != null) && (messagingException.EventId.Id == id.Id))
                {
                    throw ex.InnerException;
                }

                throw new InvalidOperationException("Expected a messaging exception");
            }
        }
    }
}
