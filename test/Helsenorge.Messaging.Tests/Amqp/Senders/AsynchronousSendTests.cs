/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.Amqp.Senders
{
    [TestClass]
    public class AsynchronousSendTests : BaseTest
    {
        private OutgoingMessage CreateMessage()
        {
            return  new OutgoingMessage()
            {
                FromHerId = MockFactory.HelsenorgeHerId,
                ToHerId = MockFactory.OtherHerId,
                Payload = GenericMessage,
                MessageFunction = "DIALOG_INNBYGGER_EKONTAKT",
                MessageId = Guid.NewGuid().ToString("D"),
                PersonalId = "12345"
            };
        }

        [TestMethod]
        public void Send_Asynchronous_Using_CPA()
        {
            var message = CreateMessage();
            message.ToHerId = MockFactory.OtherHerId;
            RunAndHandleException(Client.SendAndContinueAsync(message));

            Assert.AreEqual(1, MockFactory.OtherParty.Asynchronous.Messages.Count);
            // message includes CPA id
            Assert.IsNotNull(MockFactory.OtherParty.Asynchronous.Messages[0].CpaId);
        }
        [TestMethod]
        public void Send_Asynchronous_Using_CPP()
        {
            var message = CreateMessage();
            message.ToHerId = MockFactory.HerIdWithOnlyCpp;
            RunAndHandleException(Client.SendAndContinueAsync(message));

            Assert.AreEqual(1, MockFactory.OtherPartyWithOnlyCpp.Asynchronous.Messages.Count);
            // no CPA id specified
            Assert.IsNull(MockFactory.OtherPartyWithOnlyCpp.Asynchronous.Messages[0].CpaId);
        }

        [TestMethod]
        public void Send_Asynchronous_Receipt()
        {
            var message = CreateMessage();
            message.ReceiptForMessageFunction = message.MessageFunction;
            message.MessageFunction = "APPREC";
            RunAndHandleException(Client.SendAndContinueAsync(message));

            Assert.AreEqual(1, MockFactory.OtherParty.Asynchronous.Messages.Count);
        }

        [TestMethod]
        public void Send_Asynchronous_NoMessage()
        {
            Assert.Throws<ArgumentNullException>(() => RunAndHandleException(Client.SendAndContinueAsync(null)));
        }

        [TestMethod]
        public void Send_Asynchronous_Error_Missing_ToHerId()
        {
            var message = CreateMessage();
            message.ToHerId = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RunAndHandleException(Client.SendAndContinueAsync(message)));
        }

        [TestMethod]
        public void Send_Asynchronous_Error_Missing_MessageId()
        {
            var message = CreateMessage();
            message.MessageId = null;
            Assert.Throws<ArgumentNullException>(() => RunAndHandleException(Client.SendAndContinueAsync(message)));
        }

        [TestMethod]
        public void Send_Asynchronous_Error_Missing_MessageFunction()
        {
            var message = CreateMessage();
            message.MessageFunction = null;
            Assert.Throws<ArgumentNullException>(() => RunAndHandleException(Client.SendAndContinueAsync(message)));
        }

        [TestMethod]
        public void Send_Asynchronous_Error_Missing_Payload()
        {
            var message = CreateMessage();
            message.Payload = null;
            Assert.Throws<ArgumentNullException>(() => RunAndHandleException(Client.SendAndContinueAsync(message)));
        }
        
        [TestMethod]
        public void Send_Asynchronous_Error_InvalidMessageFunction()
        {
            var message = CreateMessage();
            message.MessageFunction = "BOB";
            Assert.Throws<MessagingException>(() =>
                RunAndHandleMessagingException(Client.SendAndContinueAsync(message), EventIds.InvalidMessageFunction));
        }

        [TestMethod]
        public void Send_Asynchronous_InvalidEncryption()
        {
            Settings.IgnoreCertificateErrorOnSend = false;
            CertificateValidator.SetError((_,u)=> u == X509KeyUsageFlags.KeyEncipherment ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            Assert.Throws<MessagingException>(() =>
                RunAndHandleMessagingException(Client.SendAndContinueAsync(message), EventIds.RemoteCertificate));
        }

        [TestMethod]
        public void Send_Asynchronous_InvalidEncryption_Ignore()
        {
            Settings.IgnoreCertificateErrorOnSend = true;
            CertificateValidator.SetError((_, u) => u == X509KeyUsageFlags.KeyEncipherment ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            RunAndHandleException(Client.SendAndContinueAsync(message));
        }

        [TestMethod]
        public void Send_Asynchronous_InvalidSignature()
        {
            Settings.IgnoreCertificateErrorOnSend = false;
            CertificateValidator.SetError((_, u) => u == X509KeyUsageFlags.NonRepudiation ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            Assert.Throws<MessagingException>(() =>
                RunAndHandleMessagingException(Client.SendAndContinueAsync(message), EventIds.LocalCertificate));
        }

        [TestMethod]
        public void Send_Asynchronous_InvalidSignature_Ignore()
        {
            Settings.IgnoreCertificateErrorOnSend = true;
            CertificateValidator.SetError((_, u) => u == X509KeyUsageFlags.NonRepudiation ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            RunAndHandleMessagingException(Client.SendAndContinueAsync(message), EventIds.LocalCertificate);
            var errorLog = MockLoggerProvider.Entries.FirstOrDefault(e => e.LogLevel == LogLevel.Error)?.Message;
            Assert.IsTrue(errorLog?.Contains("Certificate error(s): StartDate"));
        }

        [TestMethod]
        public void Send_Asynchronous_InvalidEncryptionCertificate()
        {
            Settings.IgnoreCertificateErrorOnSend = false;
            CertificateValidator.SetError((_, u) => {
                if (u != X509KeyUsageFlags.KeyEncipherment) return CertificateErrors.None;

                return CertificateErrors.Revoked | CertificateErrors.EndDate;
                });

            var message = CreateMessage();
            try
            {
                RunAndHandleMessagingException(Client.SendAndContinueAsync(message), EventIds.RemoteCertificate);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Certificate error(s): EndDate, Revoked."));
            }
            
        }

        [DataRow(2, null)]
        [DataRow(2, false)]
        [DataRow(0, true)]
        [TestMethod]
        public void Send_AsyncMessage_SkipAddingPayloadMetadata(int expectedLogElements, bool? configValue)
        {
            var message = CreateMessage();
            if (configValue.HasValue)
            {
                Settings.SkipAddingPayloadMetadataIntoApplicationProperties = configValue.Value;
            }
            RunAndHandleException(Client.SendAndContinueAsync(message));

            Assert.AreEqual(expectedLogElements, MockLoggerProvider.Entries.Count(x=>x.Message.Contains("AddingPayloadMetadata")));
        }
    }
}
