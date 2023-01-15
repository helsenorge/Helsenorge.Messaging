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

namespace Helsenorge.Messaging.Tests.ServiceBus.Senders
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void Send_Asynchronous_NoMessage()
        {
            RunAndHandleException(Client.SendAndContinueAsync(null));
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Send_Asynchronous_Error_Missing_ToHerId()
        {
            var message = CreateMessage();
            message.ToHerId = 0;
            RunAndHandleException(Client.SendAndContinueAsync(message));
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Send_Asynchronous_Error_Missing_MessageId()
        {
            var message = CreateMessage();
            message.MessageId = null;
            RunAndHandleException(Client.SendAndContinueAsync(message));
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Send_Asynchronous_Error_Missing_MessageFunction()
        {
            var message = CreateMessage();
            message.MessageFunction = null;
            RunAndHandleException(Client.SendAndContinueAsync(message));
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Send_Asynchronous_Error_Missing_Payload()
        {
            var message = CreateMessage();
            message.Payload = null;
            RunAndHandleException(Client.SendAndContinueAsync(message));
        }
        
        [TestMethod]
        [ExpectedException(typeof(MessagingException))]
        public void Send_Asynchronous_Error_InvalidMessageFunction()
        {
            var message = CreateMessage();
            message.MessageFunction = "BOB";
            RunAndHandleMessagingException(Client.SendAndContinueAsync(message), EventIds.InvalidMessageFunction);
        }
        [TestMethod]
        [ExpectedException(typeof(MessagingException))]
        public void Send_Asynchronous_InvalidEncryption()
        {
            Settings.IgnoreCertificateErrorOnSend = false;
            CertificateValidator.SetError((c,u)=> (u == X509KeyUsageFlags.KeyEncipherment) ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            RunAndHandleMessagingException(Client.SendAndContinueAsync(message), EventIds.RemoteCertificate);
        }
        [TestMethod]
        public void Send_Asynchronous_InvalidEncryption_Ignore()
        {
            Settings.IgnoreCertificateErrorOnSend = true;
            CertificateValidator.SetError((c, u) => (u == X509KeyUsageFlags.KeyEncipherment) ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            RunAndHandleException(Client.SendAndContinueAsync(message));
        }

        [TestMethod]
        [ExpectedException(typeof(MessagingException))]
        public void Send_Asynchronous_InvalidSignature()
        {
            Settings.IgnoreCertificateErrorOnSend = false;
            CertificateValidator.SetError((c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            RunAndHandleMessagingException(Client.SendAndContinueAsync(message), EventIds.LocalCertificate);
        }

        [TestMethod]
        public void Send_Asynchronous_InvalidSignature_Ignore()
        {
            Settings.IgnoreCertificateErrorOnSend = true;
            CertificateValidator.SetError((c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            RunAndHandleMessagingException(Client.SendAndContinueAsync(message), EventIds.LocalCertificate);
            var errorLog = MockLoggerProvider.Entries.FirstOrDefault(e => e.LogLevel == LogLevel.Error)?.Message;
            Assert.IsTrue(errorLog.Contains("Certificate error(s): StartDate"));
        }

        [TestMethod]
        public void Send_Asynchronous_InvalidEncryptionCertificate()
        {
            Settings.IgnoreCertificateErrorOnSend = false;
            CertificateValidator.SetError((c, u) => {
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
    }
}
