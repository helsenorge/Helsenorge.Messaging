/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries;
using Helsenorge.Registries.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Messaging.Amqp;
using Helsenorge.Registries.Tests.Mocks;

namespace Helsenorge.Messaging.Tests
{
    [TestClass]
    public class BaseTest
    {
        private X509Certificate2 _encryptionCertificate;
        private X509Certificate2 _signatureCertificate;

        public Guid CpaId = new Guid("49391409-e528-4919-b4a3-9ccdab72c8c1");
        public const int DefaultOtherHerId = 93252;

        protected AddressRegistryMock AddressRegistry { get; private set; }
        protected CollaborationProtocolRegistryMock CollaborationRegistry { get; private set; }
        protected ILoggerFactory LoggerFactory { get; private set; }
        internal ILogger Logger { get; private set; }

        protected MessagingClient Client { get; set; }
        protected MockMessagingServer Server { get; set; }
        protected MessagingSettings Settings { get; set; }
        internal MockFactory MockFactory { get; set; }

        internal MockLoggerProvider MockLoggerProvider { get; set; }

        internal MockCertificateValidator CertificateValidator { get; set; }

        internal MockCertificateStore CertificateStore { get; set; }

        internal MockMessageProtection MessageProtection { get; set; }

        protected XDocument GenericMessage => new XDocument(new XElement("SomeDummyXmlUsedForTesting"));

        protected XDocument GenericResponse => new XDocument(new XElement("SomeDummyXmlResponseUsedForTesting"));

        protected XDocument SoapFault => XDocument.Load(File.OpenRead(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}SoapFault.xml")));

        [TestInitialize]
        public virtual void Setup()
        {
            SetupInternal(DefaultOtherHerId);
        }

        internal void SetupInternal(int otherHerId)
        {
            _encryptionCertificate = TestCertificates.GetCertificate(TestCertificates.HelsenorgeLegacyEncryptionThumbprint); //TestCertificates.GenerateSelfSignedCertificate("ActorA", X509KeyUsageFlags.KeyEncipherment, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddMonths(1));
            _signatureCertificate = TestCertificates.GetCertificate(TestCertificates.HelsenorgeSignatureThumbprint); //TestCertificates.GenerateSelfSignedCertificate("ActorA", X509KeyUsageFlags.NonRepudiation, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddMonths(1));

            var addressRegistrySettings = new AddressRegistrySettings()
            {
                WcfConfiguration = new WcfConfiguration
                {
                    UserName = "username",
                    Password = "password",
                },
                CachingInterval = TimeSpan.FromSeconds(5)
            };
            var collaborationRegistrySettings = new CollaborationProtocolRegistrySettings()
            {
                WcfConfiguration = new WcfConfiguration
                {
                    UserName = "username",
                    Password = "password",
                },
                CachingInterval = TimeSpan.FromSeconds(5)
            };

            LoggerFactory = new LoggerFactory();

            MockLoggerProvider = new MockLoggerProvider(null);
            LoggerFactory.AddProvider(MockLoggerProvider);
            Logger = LoggerFactory.CreateLogger<BaseTest>();

            var distributedCache = DistributedCacheFactory.Create();

            AddressRegistry = new AddressRegistryMock(addressRegistrySettings, distributedCache, Logger);
            AddressRegistry.SetupFindCommunicationPartyDetails(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CommunicationDetails_{i}.xml"));
                return File.Exists(file) == false ? null : XElement.Load(file);
            });
            AddressRegistry.SetupGetCertificateDetailsForEncryption(i =>
            {
                var path = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"GetCertificateDetailsForEncryption_{i}.xml"));
                return File.Exists(path) == false ? null : XElement.Load(path);
            });
            AddressRegistry.SetupGetCertificateDetailsForValidatingSignature(i =>
            {
                var path = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"GetCertificateDetailsForValidatingSignature_{i}.xml"));
                return File.Exists(path) == false ? null : XElement.Load(path);
            });

            CollaborationRegistry = new CollaborationProtocolRegistryMock(collaborationRegistrySettings, distributedCache, AddressRegistry, Logger);
            CollaborationRegistry.SetupFindProtocolForCounterparty(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPP_{i}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
            CollaborationRegistry.SetupFindAgreementForCounterparty(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_{i}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });
            CollaborationRegistry.SetupFindAgreementById(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_{i:D}.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });

            Settings = new MessagingSettings()
            {
                MyHerIds = new List<int> { MockFactory.HelsenorgeHerId },
                SigningCertificate = new CertificateSettings()
                {
                    StoreName = StoreName.My,
                    StoreLocation = StoreLocation.LocalMachine,
                    Thumbprint = TestCertificates.HelsenorgeSignatureThumbprint,
                },
                DecryptionCertificate = new CertificateSettings()
                {
                    StoreName = StoreName.My,
                    StoreLocation = StoreLocation.LocalMachine,
                    Thumbprint = TestCertificates.HelsenorgeEncryptionThumbprint,
                }
            };

            Settings.BusSettings.ConnectionString = new AmqpConnectionString
            {
                HostName = "blabla",
            };
            Settings.BusSettings.Synchronous.ReplyQueueMapping.Add(Environment.MachineName.ToLower(), "RepliesGoHere");
            // make things easier by only having one processing task per queue
            Settings.BusSettings.Asynchronous.ProcessingTasks = 1;
            Settings.BusSettings.Synchronous.ProcessingTasks = 1;
            Settings.BusSettings.Error.ProcessingTasks = 1;

            MockFactory = new MockFactory(otherHerId);
            CertificateValidator = new MockCertificateValidator();
            CertificateStore = new MockCertificateStore();
            MessageProtection = new MockMessageProtection(_signatureCertificate, _encryptionCertificate);

            Client = new MessagingClient(
                Settings,
                LoggerFactory,
                CollaborationRegistry,
                AddressRegistry,
                CertificateStore,
                CertificateValidator,
                MessageProtection
            ); ;
            Client.BusCore.RegisterAlternateMessagingFactory(MockFactory);

            Server = new MockMessagingServer(
                Settings, 
                LoggerFactory, 
                CollaborationRegistry, 
                AddressRegistry, 
                CertificateStore, 
                CertificateValidator, 
                MessageProtection
            );
            Server.BusCore.RegisterAlternateMessagingFactory(MockFactory);
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
                if ((ex.InnerException is MessagingException messagingException) && (messagingException.EventId.Id == id.Id))
                {
                    throw ex.InnerException;
                }

                throw new InvalidOperationException("Expected a messaging exception");
            }
        }
    }
}
