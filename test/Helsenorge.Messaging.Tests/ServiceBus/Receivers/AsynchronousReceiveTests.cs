using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.Xml.Schema;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus;
using Helsenorge.Messaging.ServiceBus.Receivers;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Helsenorge.Messaging.Security;
using System.Text;

namespace Helsenorge.Messaging.Tests.ServiceBus.Receivers
{
    [TestClass]
    public class AsynchronousReceiveTests : BaseTest
    {
        private bool _startingCalled;
        private bool _receivedCalled;
        private bool _completedCalled;
        private bool _handledExceptionCalled;
        private bool _unhandledExceptionCalled;


        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _startingCalled = false;
            _receivedCalled = false;
            _completedCalled = false;
        }
        
        [TestMethod]
        public void Asynchronous_Receive_RemoteCertificateMissing()
        {
            CertificateValidator.SetError((c, u) => CertificateErrors.Missing);

            RunAsynchronousReceive(
                postValidation: () =>
                {
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                    Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                    var error = MockLoggerProvider.Entries
                        .Where(a =>
                            a.LogLevel == LogLevel.Warning &&
                            a.Message.Contains("Certificate is missing for message. MessageFunction: DIALOG_INNBYGGER_EKONTAKT"))
                        .ToList();
                    Assert.AreEqual(1, error.Count);
                    Assert.IsTrue(error.Single().Message.Contains("MessageFunction: DIALOG_INNBYGGER_EKONTAKT FromHerId: 93252 ToHerId: 93238"));
                },
                wait: () => _completedCalled,
                received: (m) => { Assert.IsTrue(m.SignatureError == CertificateErrors.Missing); },
                messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_CertificateSignError()
        {
            Exception receiveException = null;

            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry)
            {
                DefaultMessageProtection = new SignThenEncryptMessageProtection(),   // disable protection for most tests
                DefaultCertificateValidator = CertificateValidator
            };
            Client.ServiceBus.RegisterAlternateMessagingFactory(MockFactory);
            
            Server = new MessagingServer(Settings, Logger, LoggerFactory, CollaborationRegistry, AddressRegistry)
            {
                DefaultMessageProtection = new SignThenEncryptMessageProtection(),   // disable protection for most tests
                DefaultCertificateValidator = CertificateValidator
            };
            Server.ServiceBus.RegisterAlternateMessagingFactory(MockFactory);

            CollaborationRegistry.SetupFindAgreementForCounterparty(i =>
            {
                var file = Path.Combine("Files", $"CPA_{i}_ChangedSignedCertificate.xml");
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });

            RunAsynchronousReceive(
                postValidation: () => {
                    Assert.IsTrue(_startingCalled);
                    Assert.IsFalse(_receivedCalled);
                    Assert.IsTrue(_completedCalled);
                    var error = MockLoggerProvider.FindEntry(EventIds.RemoteCertificate);
                    Assert.IsTrue(error.Message
                        .Contains($"{TestCertificates.HelsenorgePrivateSigntature.Thumbprint}"));
                    Assert.IsTrue(error.Message
                        .Contains($"{TestCertificates.HelsenorgePrivateSigntature.NotBefore}"));
                    var signingException = receiveException as CertificateException;
                    Assert.IsNotNull(signingException);
                    Assert.IsNotNull(signingException.Payload);
                },
                wait: () => _completedCalled,
                received: (m) => { },
                messageModification: (m) => { },
                handledException: ((m, e) =>
                {
                    Server.Stop(TimeSpan.FromSeconds(10));
                    _handledExceptionCalled = true;
                    _completedCalled = true;
                    receiveException = e;
                }),
                messageProtected: true);
        }

        [TestMethod]
        public void Asynchronous_Receive_NoCpaId()
        {
            // postition of arguments have been reversed so that we inster the name of the argument without getting a resharper indication
            // makes it easier to read
            RunAsynchronousReceive(
                postValidation: () =>
                {
                    Assert.IsTrue(_startingCalled);
                    Assert.IsTrue(_receivedCalled);
                    Assert.IsTrue(_completedCalled);
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                },
                wait: () => _completedCalled,
                received: (m) =>
                {
                    Assert.AreEqual(MockFactory.HelsenorgeHerId, m.ToHerId);
                    Assert.AreEqual(MockFactory.OtherHerId, m.FromHerId);
                    Assert.AreEqual("DIALOG_INNBYGGER_EKONTAKT", m.MessageFunction);
                },
                messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_WithCpaId()
        {
            RunAsynchronousReceive(
                postValidation: () =>
                {
                    Assert.IsTrue(_startingCalled);
                    Assert.IsTrue(_receivedCalled);
                    Assert.IsTrue(_completedCalled);
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                },
                wait: () => _completedCalled,
                received: (m) =>
                {
                    Assert.AreEqual(MockFactory.HelsenorgeHerId, m.ToHerId);
                    Assert.AreEqual(MockFactory.OtherHerId, m.FromHerId);
                    Assert.AreEqual("DIALOG_INNBYGGER_EKONTAKT", m.MessageFunction);
                },
                messageModification: (m) => { m.CpaId = "49391409-e528-4919-b4a3-9ccdab72c8c1"; });
        }
        [TestMethod]
        public void Asynchronous_Receive_MissingFunction()
        {
            RunAsynchronousReceive(
                postValidation: () =>
                {
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-field-value", "One or more fields are missing", "Label;");

                },
                wait: () => _handledExceptionCalled,
                received: (m) => { },
                messageModification: (m) => { m.MessageFunction = null; });
        }
        [TestMethod]
        public void Asynchronous_Receive_MissingToHerId()
        {
            RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-field-value", "One or more fields are missing", "toHerId;");

            },
            wait: () => _handledExceptionCalled,
            received: (m) => { },
            messageModification: (m) => { m.ToHerId = 0; });
        }
        [TestMethod]
        public void Asynchronous_Receive_MissingFromHerId()
        {
            RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                // we don't have enough information to know where to send it back
                Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
            },
            wait: () => _receivedCalled,
            received: (m) => { Assert.AreEqual(CertificateErrors.Missing, m.SignatureError); },
            messageModification: (m) => { m.FromHerId = 0; });
        }
        [TestMethod]
        public void Asynchronous_Receive_MissingApplicationTimeStamp()
        {
            RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-field-value", "One or more fields are missing", "applicationTimeStamp;");
            },
            wait: () => _handledExceptionCalled,
            received: (m) => { },
            messageModification: (m) => { m.ApplicationTimestamp = DateTime.MinValue; });
        }
        [TestMethod]
        public void Asynchronous_Receive_ContentType()
        {
            RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-field-value", "One or more fields are missing", "ContentType;");
            },
            wait: () => _handledExceptionCalled,
            received: (m) => { },
            messageModification: (m) => { m.ContentType = null; });
        }
        [TestMethod]
        public void Asynchronous_Receive_XmlSchemaError()
        {
            RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                CheckError(MockFactory.OtherParty.Error.Messages, "transport:not-well-formed-xml", "XML-Error", string.Empty);
                var logEntry = MockLoggerProvider.FindEntry(EventIds.NotXml);
                Assert.IsNotNull(logEntry);
                Assert.IsTrue(logEntry.LogLevel == LogLevel.Warning);
            },
            wait: () => _handledExceptionCalled,
            received: (m) => {  throw new XmlSchemaValidationException("XML-Error");},
            messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_ReceivedDataMismatch()
        {
            RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-field-value", "Mismatch", "Expected;Received;");
                var logEntry = MockLoggerProvider.FindEntry(EventIds.DataMismatch);
                Assert.IsNotNull(logEntry);
                Assert.IsTrue(logEntry.LogLevel == LogLevel.Warning);
            },
            wait: () => _handledExceptionCalled,
            received: (m) => { throw new ReceivedDataMismatchException("Mismatch") { ExpectedValue = "Expected", ReceivedValue = "Received"}; },
            messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_NotifySender()
        {
            RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                CheckError(MockFactory.OtherParty.Error.Messages, "transport:internal-error", "NotifySender", string.Empty);
                var logEntry = MockLoggerProvider.FindEntry(EventIds.ApplicationReported);
                Assert.IsNotNull(logEntry);
                Assert.IsTrue(logEntry.LogLevel == LogLevel.Warning);
            },
            wait: () => _handledExceptionCalled,
            received: (m) => { throw new NotifySenderException("NotifySender"); },
            messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_GenericException()
        {
            RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(1, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
            },
            wait: () => _unhandledExceptionCalled,
            received: (m) => { throw new ArgumentOutOfRangeException(); },
            messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_LocalCertificateStartDate()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.DataEncipherment) ? CertificateErrors.StartDate : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                    Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                    Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificateStartDate));
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.DecryptionError != CertificateErrors.None); },
               messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_LocalCertificateEndDate()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.DataEncipherment) ? CertificateErrors.EndDate : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificateEndDate));
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.DecryptionError != CertificateErrors.None); },
               messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_LocalCertificateUsage()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.DataEncipherment) ? CertificateErrors.Usage : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificateUsage));
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.DecryptionError != CertificateErrors.None); },
               messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_LocalCertificateRevoked()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.DataEncipherment) ? CertificateErrors.Revoked : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificateRevocation));
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.DecryptionError != CertificateErrors.None); },
               messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_LocalCertificateRevokedUnknown()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.DataEncipherment) ? CertificateErrors.RevokedUnknown : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificateRevocation));
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.DecryptionError != CertificateErrors.None); },
               messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_LocalCertificateMultiple()
        {
            CertificateValidator.SetError(
                (c, u) =>
                    (u == X509KeyUsageFlags.DataEncipherment)
                        ? CertificateErrors.StartDate | CertificateErrors.EndDate
                        : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificate));
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.DecryptionError != CertificateErrors.None); },
               messageModification: (m) => { });
        }

        [TestMethod]
        public void Asynchronous_Receive_RemoteCertificateStartDate()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.StartDate : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificateStartDate));
                   CheckError(MockFactory.OtherParty.Error.Messages, "transport:expired-certificate", "Invalid start date", string.Empty);
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.SignatureError != CertificateErrors.None); },
               messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_ReceiveRemoteCertificateEndDate()
        {
            CertificateValidator.SetError(
            (c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.EndDate : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificateEndDate));
                   CheckError(MockFactory.OtherParty.Error.Messages, "transport:expired-certificate", "Invalid end date", string.Empty);
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.SignatureError != CertificateErrors.None); },
               messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_RemoteCertificateUsage()
        {
            CertificateValidator.SetError(
               (c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.Usage : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificateUsage));
                   CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-certificate", "Invalid usage", string.Empty);
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.SignatureError != CertificateErrors.None); },
               messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_RemoteCertificateRevoked()
        {
            CertificateValidator.SetError(
                  (c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.Revoked : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificateRevocation));
                   CheckError(MockFactory.OtherParty.Error.Messages, "transport:revoked-certificate", "Certificate has been revoked", string.Empty);
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.SignatureError != CertificateErrors.None); },
               messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_RemoteCertificateRevokedUnknown()
        {
            CertificateValidator.SetError(
                  (c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.RevokedUnknown : CertificateErrors.None);

            RunAsynchronousReceive(
                  postValidation: () =>
                  {
                      Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                      Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                      Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificateRevocation));
                      CheckError(MockFactory.OtherParty.Error.Messages, "transport:revoked-certificate", "Unable to determine revocation status", string.Empty);
                  },
                  wait: () => _completedCalled,
                  received: (m) => { Assert.IsTrue(m.SignatureError != CertificateErrors.None); },
                  messageModification: (m) => { });
        }
        [TestMethod]
        public void Asynchronous_Receive_RemoteCertificateMultiple()
        {
            CertificateValidator.SetError(
                   (c, u) =>
                       (u == X509KeyUsageFlags.NonRepudiation)
                           ? CertificateErrors.StartDate | CertificateErrors.EndDate
                           : CertificateErrors.None);

            RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificate));
                   CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-certificate", "More than one error with certificate", string.Empty);
               },
               wait: () => _completedCalled,
               received: (m) => { Assert.IsTrue(m.SignatureError != CertificateErrors.None); },
               messageModification: (m) => { });
        }

        [TestMethod]
        public void Asynchronous_Receive_UseLegacyOK()
        {
            Settings.DecryptionCertificate = new CertificateSettings()
            {
                Certificate = TestCertificates.CounterpartyPrivateEncryption
            };
            Settings.LegacyDecryptionCertificate = new CertificateSettings()
            {
                Certificate =  TestCertificates.HelsenorgePrivateEncryption
            };

            // postition of arguments have been reversed so that we instert the name of the argument without getting a resharper indication
            // makes it easier to read
            RunAsynchronousReceive(
                postValidation: () =>
                {
                    Assert.IsTrue(_startingCalled);
                    Assert.IsTrue(_receivedCalled);
                    Assert.IsTrue(_completedCalled);
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                },
                wait: () => _completedCalled,
                received: (m) =>
                {
                    Assert.AreEqual(MockFactory.HelsenorgeHerId, m.ToHerId);
                    Assert.AreEqual(MockFactory.OtherHerId, m.FromHerId);
                    Assert.AreEqual("DIALOG_INNBYGGER_EKONTAKT", m.MessageFunction);
                },
                messageModification: (m) => { });
        }

        [TestMethod]
        public void Asynchronous_Receive_SecurityException()
        {
            Server.DefaultMessageProtection = new SecurityExceptionMessageProtection();

            RunAsynchronousReceive(
                postValidation: () =>
                {
                    
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-certificate", "Invalid certificate", string.Empty);
                },
                wait: () => _handledExceptionCalled,
                received: (m) => { },
                messageModification: (m) => { });
        }

        [TestMethod]  
        public void Asynchronous_Receive_InvalidXML()
        {  
            var messageAsStream = new MemoryStream();  
            var messageAsString = "Invalid XMLMessage";  
            messageAsStream.Write(new UnicodeEncoding().GetBytes(messageAsString), 0, messageAsString.Length);

            RunAsynchronousReceive(  
                postValidation: () =>  
                {
                    Assert.IsTrue(_startingCalled);
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    CheckError(MockFactory.OtherParty.Error.Messages, "transport:not-well-formed-xml", "Could not deserialize payload", string.Empty);
                },  
            wait: () => _handledExceptionCalled,  
            received: (m) => { },  
            messageModification: (m) => { m.SetBody(messageAsStream); });  
        }

class SecurityExceptionMessageProtection : IMessageProtection
        {
            public string ContentType => Messaging.Abstractions.ContentType.SignedAndEnveloped;
            
            public MemoryStream Protect(XDocument data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate)
            {
                if (data == null) throw new ArgumentNullException(nameof(data));

                var ms = new MemoryStream();
                data.Save(ms);
                return ms;
            }

            public XDocument Unprotect(Stream data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate, X509Certificate2 legacyEncryptionCertificate)
            {
                throw new SecurityException("Invalid certificate");
            }
        }


        private static void CheckError(IEnumerable<IMessagingMessage> queue, string errorCondition, string errorDescription, string errorConditionData)
        {
            var m = queue.First();
            Assert.AreEqual(errorCondition, m.Properties["errorCondition"].ToString());
            Assert.AreEqual(errorDescription, m.Properties["errorDescription"].ToString());
            if (string.IsNullOrEmpty(errorConditionData) == false)
            {
                Assert.AreEqual(errorConditionData, m.Properties["errorConditionData"].ToString());
            }
        }

        private void RunAsynchronousReceive(
            Action<MockMessage> messageModification, 
            Action<IncomingMessage> received, 
            Func<bool> wait,
            Action postValidation,
            Action<IMessagingMessage, Exception> handledException = null,
            bool messageProtected = false)
        {
            // create and post message
            var message = messageProtected == false ? CreateAsynchronousMessage() : CreateAsynchronousMessageProtected();
            messageModification(message);
            MockFactory.Helsenorge.Asynchronous.Messages.Add(message);

            // configure notifications
            Server.RegisterAsynchronousMessageReceivedStartingCallback((m) => _startingCalled = true);
            Server.RegisterAsynchronousMessageReceivedCallback((m) =>
            {
                received(m);
                _receivedCalled = true;
            });
            Server.RegisterAsynchronousMessageReceivedCompletedCallback((m) => _completedCalled = true);
            Server.RegisterUnhandledExceptionCallback((m, e) => _unhandledExceptionCalled = true);
            if (handledException != null) Server.RegisterHandledExceptionCallback(handledException);
            else Server.RegisterHandledExceptionCallback((m, e) => _handledExceptionCalled = true);
            
            Server.Start();

            Wait(20, wait); // we have a high timeout in case we do a bit of debugging. With more extensive debugging (breakpoints), we will get a timeout
            Server.Stop(TimeSpan.FromSeconds(10));
            
            // check the state of the system
            postValidation();
        }

        /// <summary>
        /// Utility function that waits until a condition is true
        /// </summary>
        /// <param name="timeout">timeout in seconds</param>
        /// <param name="check"></param>
        private static void Wait(int timeout, Func<bool> check)
        {
            var max = DateTime.Now.Add(TimeSpan.FromSeconds(timeout));

            while (true)
            {
                if(DateTime.Now > max) throw new TimeoutException();

                if (check()) return;
                System.Threading.Thread.Sleep(50);
            }
        }

        private MockMessage CreateAsynchronousMessage()
        {
            var messageId = Guid.NewGuid().ToString("D");
            return new MockMessage(GenericResponse)
            {
                MessageFunction = "DIALOG_INNBYGGER_EKONTAKT",
                ApplicationTimestamp = DateTime.Now,
                ContentType = ContentType.SignedAndEnveloped,
                MessageId = messageId,
                CorrelationId = messageId,
                FromHerId = MockFactory.OtherHerId,
                ToHerId = MockFactory.HelsenorgeHerId,
                ScheduledEnqueueTimeUtc = DateTime.UtcNow,
                TimeToLive = TimeSpan.FromSeconds(15),
                ReplyTo = MockFactory.OtherParty.Asynchronous.Name,
                Queue = MockFactory.Helsenorge.Asynchronous.Messages,
            };
        }
        
        private MockMessage CreateAsynchronousMessageProtected()
        {
            var signing = new SignThenEncryptMessageProtection();
            var messageId = Guid.NewGuid().ToString("D");
            var path = Path.Combine("Files", "Helsenorge_Message.xml");
            var file = File.Exists(path) ? new XDocument(XElement.Load(path)) : null;
            var protect = signing.Protect(file ?? GenericMessage, TestCertificates.HelsenorgePublicEncryption,
                TestCertificates.HelsenorgePrivateSigntature); 
            return new MockMessage(protect)
            {
                MessageFunction = "DIALOG_INNBYGGER_EKONTAKT",
                ApplicationTimestamp = DateTime.Now,
                ContentType = ContentType.SignedAndEnveloped,
                MessageId = messageId,
                CorrelationId = messageId,
                FromHerId = MockFactory.OtherHerId,
                ToHerId = MockFactory.HelsenorgeHerId,
                ScheduledEnqueueTimeUtc = DateTime.UtcNow,
                TimeToLive = TimeSpan.FromSeconds(15),
                ReplyTo = MockFactory.OtherParty.Asynchronous.Name,
                Queue = MockFactory.Helsenorge.Asynchronous.Messages,
            };
        }
    }
}
