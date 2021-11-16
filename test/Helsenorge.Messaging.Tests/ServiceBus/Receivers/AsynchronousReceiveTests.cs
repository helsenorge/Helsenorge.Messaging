﻿/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

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
using System.Threading.Tasks;

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
        public async Task Asynchronous_Receive_RemoteCertificateMissing()
        {
            CertificateValidator.SetError((c, u) => CertificateErrors.Missing);

            await RunAsynchronousReceive(
                postValidation: () =>
                {
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    var logEntries = MockLoggerProvider.Entries
                        .Where(a =>
                            a.LogLevel == LogLevel.Warning &&
                            a.Message.Contains("Certificate is missing. MessageFunction:"))
                        .ToList();
                    Assert.AreEqual(1, logEntries.Count);
                },
                wait: () => _handledExceptionCalled,
                received: (m) => 
                { 
                    Assert.IsTrue(m.SignatureError == CertificateErrors.Missing);
                    return Task.CompletedTask;
                },
                messageModification: (m) => { });
        }

        [TestMethod, TestCategory("X509Chain")]
        public async Task Asynchronous_Receive_CertificateSignError()
        {
            Exception receiveException = null;

            var partyAProtection = new SignThenEncryptMessageProtection(TestCertificates.CounterpartyPrivateSigntature, TestCertificates.CounterpartyPrivateEncryption);
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, CertificateStore, CertificateValidator, partyAProtection);
            Client.ServiceBus.RegisterAlternateMessagingFactory(MockFactory);

            var partyBProtection = new SignThenEncryptMessageProtection(TestCertificates.HelsenorgePrivateSigntature, TestCertificates.HelsenorgePrivateEncryption);
            Server = new MockMessagingServer(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, CertificateStore, CertificateValidator, partyBProtection);
            Server.ServiceBus.RegisterAlternateMessagingFactory(MockFactory);

            CollaborationRegistry.SetupFindAgreementForCounterparty(i =>
            {
                var file = TestFileUtility.GetFullPathToFile(Path.Combine("Files", $"CPA_{i}_ChangedSignedCertificate.xml"));
                return File.Exists(file) == false ? null : File.ReadAllText(file);
            });

            await RunAsynchronousReceive(
                postValidation: () => {
                    Assert.IsTrue(_startingCalled);
                    Assert.IsFalse(_receivedCalled);
                    Assert.IsTrue(_completedCalled);
                    var error = MockLoggerProvider.FindEntry(EventIds.RemoteCertificate);
                    Assert.IsTrue(error.Message
                        .Contains($"{TestCertificates.HelsenorgePrivateSigntature.Thumbprint}"));
                    Assert.IsTrue(error.Message
                        .Contains($"{TestCertificates.HelsenorgePrivateSigntature.NotBefore}"));
                    var signingException = receiveException as CertificateMessagePayloadException;
                    Assert.IsNotNull(signingException);
                    Assert.IsNotNull(signingException.Payload);
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    Assert.AreEqual(4, MockFactory.OtherParty.Error.Messages[0].Properties.Count);
                    Assert.AreEqual("transport:invalid-certificate", MockFactory.OtherParty.Error.Messages[0].Properties["errorCondition"]);
                },
                wait: () => _completedCalled,
                received: (m) => throw new Exception("Message should not come this far when there is issue with the certificate"),
                messageModification: (m) => { },
                handledException: ((m, e) =>
                {
                    _handledExceptionCalled = true;
                    _completedCalled = true;
                    receiveException = e;
                    return Task.CompletedTask;
                }),
                messageProtected: true);
        }

        [TestMethod]
        public async Task Asynchronous_Receive_NoCpaId()
        {
            // postition of arguments have been reversed so that we inster the name of the argument without getting a resharper indication
            // makes it easier to read
            await RunAsynchronousReceive(
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
                    return Task.CompletedTask;
                },
                messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_WithCpaId()
        {
            await RunAsynchronousReceive(
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
                    return Task.CompletedTask;
                },
                messageModification: (m) => { m.CpaId = "49391409-e528-4919-b4a3-9ccdab72c8c1"; });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_MissingFunction()
        {
            await RunAsynchronousReceive(
                postValidation: () =>
                {
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-field-value", "One or more fields are missing", "Label;");

                },
                wait: () => _handledExceptionCalled,
                received: (m) => { return Task.CompletedTask; },
                messageModification: (m) => { m.MessageFunction = null; });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_MissingToHerId()
        {
            await RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-field-value", "One or more fields are missing", "toHerId;");

            },
            wait: () => _handledExceptionCalled,
            received: (m) => { return Task.CompletedTask; },
            messageModification: (m) => { m.ToHerId = 0; });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_MissingFromHerId()
        {
            await RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                // we don't have enough information to know where to send it back
                Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                var logEntries = MockLoggerProvider.Entries
                .Where(entry => 
                    entry.EventId == EventIds.MissingField && entry.Message.Contains("FromHerId is missing. No idea where to send the error")
                );
                Assert.AreEqual(1, logEntries.Count());
            },
            wait: () => _handledExceptionCalled,
            received: (m) => 
            { 
                return Task.CompletedTask;
            },
            messageModification: (m) => { m.FromHerId = 0; });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_MissingApplicationTimeStamp()
        {
            await RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-field-value", "One or more fields are missing", "applicationTimeStamp;");
            },
            wait: () => _handledExceptionCalled,
            received: (m) => { return Task.CompletedTask; },
            messageModification: (m) => { m.ApplicationTimestamp = DateTime.MinValue; });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_ContentType()
        {
            await RunAsynchronousReceive(
            postValidation: () =>
            {
                Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-field-value", "One or more fields are missing", "ContentType;");
            },
            wait: () => _handledExceptionCalled,
            received: (m) => { return Task.CompletedTask; },
            messageModification: (m) => { m.ContentType = null; });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_XmlSchemaError()
        {
            await RunAsynchronousReceive(
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
        public async Task Asynchronous_Receive_ReceivedDataMismatch()
        {
            await RunAsynchronousReceive(
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
        public async Task Asynchronous_Receive_NotifySender()
        {
            await RunAsynchronousReceive(
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
        public async Task Asynchronous_Receive_GenericException()
        {
            await RunAsynchronousReceive(
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
        public async Task Asynchronous_Receive_LocalCertificateStartDate()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.KeyEncipherment) ? CertificateErrors.StartDate : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                    Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                    Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificateStartDate));
               },
               wait: () => _completedCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.DecryptionError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_LocalCertificateEndDate()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.KeyEncipherment) ? CertificateErrors.EndDate : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificateEndDate));
               },
               wait: () => _completedCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.DecryptionError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_LocalCertificateUsage()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.KeyEncipherment) ? CertificateErrors.Usage : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificateUsage));
               },
               wait: () => _completedCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.DecryptionError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_LocalCertificateRevoked()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.KeyEncipherment) ? CertificateErrors.Revoked : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificateRevocation));
               },
               wait: () => _completedCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.DecryptionError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_LocalCertificateRevokedUnknown()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.KeyEncipherment) ? CertificateErrors.RevokedUnknown : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificateRevocation));
               },
               wait: () => _completedCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.DecryptionError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_LocalCertificateMultiple()
        {
            CertificateValidator.SetError(
                (c, u) =>
                    (u == X509KeyUsageFlags.KeyEncipherment)
                        ? CertificateErrors.StartDate | CertificateErrors.EndDate
                        : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(0, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.LocalCertificate));
               },
               wait: () => _completedCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.DecryptionError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }

        [TestMethod]
        public async Task Asynchronous_Receive_RemoteCertificateStartDate()
        {
            CertificateValidator.SetError(
                (c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.StartDate : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificateStartDate));
                   CheckError(MockFactory.OtherParty.Error.Messages, "transport:expired-certificate", "Invalid start date", string.Empty);
               },
               wait: () => _handledExceptionCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.SignatureError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_ReceiveRemoteCertificateEndDate()
        {
            CertificateValidator.SetError(
            (c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.EndDate : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificateEndDate));
                   CheckError(MockFactory.OtherParty.Error.Messages, "transport:expired-certificate", "Invalid end date", string.Empty);
               },
               wait: () => _handledExceptionCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.SignatureError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_RemoteCertificateUsage()
        {
            CertificateValidator.SetError(
               (c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.Usage : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificateUsage));
                   CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-certificate", "Invalid usage", string.Empty);
               },
               wait: () => _handledExceptionCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.SignatureError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_RemoteCertificateRevoked()
        {
            CertificateValidator.SetError(
                  (c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.Revoked : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificateRevocation));
                   CheckError(MockFactory.OtherParty.Error.Messages, "transport:revoked-certificate", "Certificate has been revoked", string.Empty);
               },
               wait: () => _handledExceptionCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.SignatureError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_RemoteCertificateRevokedUnknown()
        {
            CertificateValidator.SetError(
                  (c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.RevokedUnknown : CertificateErrors.None);

            await RunAsynchronousReceive(
                  postValidation: () =>
                  {
                      Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                      Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                      Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificateRevocation));
                      CheckError(MockFactory.OtherParty.Error.Messages, "transport:revoked-certificate", "Unable to determine revocation status", string.Empty);
                  },
                  wait: () => _handledExceptionCalled,
                  received: (m) => 
                  { 
                      Assert.IsTrue(m.SignatureError != CertificateErrors.None);
                      return Task.CompletedTask;
                  },
                  messageModification: (m) => { });
        }
        [TestMethod]
        public async Task Asynchronous_Receive_RemoteCertificateMultiple()
        {
            CertificateValidator.SetError(
                   (c, u) =>
                       (u == X509KeyUsageFlags.NonRepudiation)
                           ? CertificateErrors.StartDate | CertificateErrors.EndDate
                           : CertificateErrors.None);

            await RunAsynchronousReceive(
               postValidation: () =>
               {
                   Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                   Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                   Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.RemoteCertificate));
                   CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-certificate", "More than one error with certificate", string.Empty);
               },
               wait: () => _handledExceptionCalled,
               received: (m) => 
               { 
                   Assert.IsTrue(m.SignatureError != CertificateErrors.None);
                   return Task.CompletedTask;
               },
               messageModification: (m) => { });
        }

        [TestMethod]
        public async Task Asynchronous_Receive_UseLegacyOK()
        {
            Settings.DecryptionCertificate = new CertificateSettings()
            {
                Thumbprint = "b1fae38326a6cefa72708f7633541262e8633b2c"
            };
            Settings.LegacyDecryptionCertificate = new CertificateSettings()
            {
                Thumbprint = "fddbcfbb3231f0c66ee2168358229d3cac95e88a"
            };

            // postition of arguments have been reversed so that we instert the name of the argument without getting a resharper indication
            // makes it easier to read
            await RunAsynchronousReceive(
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
                    return Task.CompletedTask;
                },
                messageModification: (m) => { });
        }

        [TestMethod]
        public async Task Asynchronous_Receive_SecurityException()
        {
            var signatureCertificate = CertificateStore.GetCertificate(TestCertificates.HelsenorgeSigntatureThumbprint);
            var encryptionCertificate = CertificateStore.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint);
            Server.MessageProtection = new SecurityExceptionMessageProtection(signatureCertificate, encryptionCertificate);

            await RunAsynchronousReceive(
                postValidation: () =>
                {
                    
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    CheckError(MockFactory.OtherParty.Error.Messages, "transport:invalid-certificate", "Invalid certificate", string.Empty);
                },
                wait: () => _handledExceptionCalled,
                received: (m) => { return Task.CompletedTask; },
                messageModification: (m) => { });
        }

        [TestMethod]  
        public async Task Asynchronous_Receive_InvalidXML()
        {  
            var messageAsStream = new MemoryStream();  
            var messageAsString = "Invalid XMLMessage";  
            messageAsStream.Write(new UnicodeEncoding().GetBytes(messageAsString), 0, messageAsString.Length);

            await RunAsynchronousReceive(  
                postValidation: () =>  
                {
                    Assert.IsTrue(_startingCalled);
                    Assert.AreEqual(0, MockFactory.Helsenorge.Asynchronous.Messages.Count);
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    CheckError(MockFactory.OtherParty.Error.Messages, "transport:not-well-formed-xml", "Could not deserialize payload", string.Empty);
                },  
            wait: () => _handledExceptionCalled,  
            received: (m) => { return Task.CompletedTask; },
            messageModification: (m) => { m.SetBody(messageAsStream); });  
        }

        class SecurityExceptionMessageProtection : IMessageProtection
        {
            public SecurityExceptionMessageProtection(X509Certificate2 signingCertificate, X509Certificate2 encryptionCertificate)
            {
                SigningCertificate = signingCertificate;
                EncryptionCertificate = encryptionCertificate;
            }
            /// <summary>
            /// Gets the content type applied to protected data
            /// </summary>
            public string ContentType => Messaging.Abstractions.ContentType.SignedAndEnveloped;
            /// <summary>
            /// Gets the signing certificate, but it's not used in this implementation.
            /// </summary>
            public X509Certificate2 SigningCertificate { get; private set; }
            /// <summary>
            /// Gets the encryption certificate, but it's not used in this implementation.
            /// </summary>
            public X509Certificate2 EncryptionCertificate { get; private set; }
            /// <summary>
            /// Gets the legacy encryption certificate, but it's not used in this implementation.
            /// </summary>
            public X509Certificate2 LegacyEncryptionCertificate => null;

            [Obsolete("This method is deprecated and is superseded by SecurityExceptionMessageProtection.Protect(Stream).")]
            public MemoryStream Protect(XDocument data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate)
            {
                if (data == null) throw new ArgumentNullException(nameof(data));

                var ms = new MemoryStream();
                data.Save(ms);
                return ms;
            }

            public Stream Protect(Stream data, X509Certificate2 encryptionCertificate)
            {
                if (data == null) throw new ArgumentNullException(nameof(data));

                return data;
            }

            [Obsolete("This method is deprecated and is superseded by SecurityExceptionMessageProtection.Unprotect(Stream).")]
            public XDocument Unprotect(Stream data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate, X509Certificate2 legacyEncryptionCertificate)
            {
                throw new SecurityException("Invalid certificate");
            }

            public Stream Unprotect(Stream data, X509Certificate2 signingCertificate)
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

        private async Task RunAsynchronousReceive(
            Action<MockMessage> messageModification, 
            Func<IncomingMessage, Task> received, 
            Func<bool> wait,
            Action postValidation,
            Func<IMessagingMessage, Exception, Task> handledException = null,
            bool messageProtected = false)
        {
            // create and post message
            var message = messageProtected == false ? CreateAsynchronousMessage() : CreateAsynchronousMessageProtected();
            messageModification(message);
            MockFactory.Helsenorge.Asynchronous.Messages.Add(message);

            // configure notifications
            Server.RegisterAsynchronousMessageReceivedStartingCallbackAsync((listener, message) => 
            {
                _startingCalled = true;
                return Task.CompletedTask;
            });
            Server.RegisterAsynchronousMessageReceivedCallbackAsync(async (m) =>
            {
                 await received(m);
                _receivedCalled = true;
            });
            Server.RegisterAsynchronousMessageReceivedCompletedCallbackAsync((m) => 
            {
                _completedCalled = true;
                return Task.CompletedTask;
            });
            Server.RegisterUnhandledExceptionCallbackAsync((m, e) => 
            {
                _unhandledExceptionCalled = true;
                return Task.CompletedTask;
            });
            if (handledException != null) Server.RegisterHandledExceptionCallbackAsync(handledException);
            else Server.RegisterHandledExceptionCallbackAsync((m, e) =>
            {
                _handledExceptionCalled = true;
                return Task.CompletedTask;
            }
            );
            
            await Server.Start();

            Wait(20, wait); // we have a high timeout in case we do a bit of debugging. With more extensive debugging (breakpoints), we will get a timeout
            await Server.Stop();
            
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
            var certificateStore = new MockCertificateStore();
            var messageProtection = new SignThenEncryptMessageProtection(TestCertificates.HelsenorgePrivateSigntature, TestCertificates.HelsenorgePrivateEncryption);
            var messageId = Guid.NewGuid().ToString("D");
            var path = Path.Combine("Files", "Helsenorge_Message.xml");
            var file = File.Exists(path) ? new XDocument(XElement.Load(path)) : null;
            var protect = messageProtection.Protect(file == null ? GenericMessage.ToStream() : file.ToStream(), TestCertificates.HelsenorgePublicEncryption); 
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
