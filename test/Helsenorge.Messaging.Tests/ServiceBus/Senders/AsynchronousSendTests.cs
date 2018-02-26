using System;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries.Abstractions;
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
            RunAndHandleException(Client.SendAndContinueAsync(Logger, message));

            Assert.AreEqual(1, MockFactory.OtherParty.Asynchronous.Messages.Count);
            // message includes CPA id
            Assert.IsNotNull(MockFactory.OtherParty.Asynchronous.Messages[0].CpaId);
        }
        [TestMethod]
        public void Send_Asynchronous_Using_CPP()
        {
            var message = CreateMessage();
            message.ToHerId = MockFactory.HerIdWithOnlyCpp;
            RunAndHandleException(Client.SendAndContinueAsync(Logger, message));

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
            RunAndHandleException(Client.SendAndContinueAsync(Logger, message));

            Assert.AreEqual(1, MockFactory.OtherParty.Asynchronous.Messages.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Send_Asynchronous_NoMessage()
        {
            RunAndHandleException(Client.SendAndContinueAsync(Logger, null));
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Send_Asynchronous_Error_Missing_ToHerId()
        {
            var message = CreateMessage();
            message.ToHerId = 0;
            RunAndHandleException(Client.SendAndContinueAsync(Logger, message));
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Send_Asynchronous_Error_Missing_MessageId()
        {
            var message = CreateMessage();
            message.MessageId = null;
            RunAndHandleException(Client.SendAndContinueAsync(Logger, message));
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Send_Asynchronous_Error_Missing_MessageFunction()
        {
            var message = CreateMessage();
            message.MessageFunction = null;
            RunAndHandleException(Client.SendAndContinueAsync(Logger, message));
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Send_Asynchronous_Error_Missing_Payload()
        {
            var message = CreateMessage();
            message.Payload = null;
            RunAndHandleException(Client.SendAndContinueAsync(Logger, message));
        }
        
        [TestMethod]
        [ExpectedException(typeof(MessagingException))]
        public void Send_Asynchronous_Error_InvalidMessageFunction()
        {
            var message = CreateMessage();
            message.MessageFunction = "BOB";
            RunAndHandleMessagingException(Client.SendAndContinueAsync(Logger, message), EventIds.InvalidMessageFunction);
        }
        [TestMethod]
        [ExpectedException(typeof(MessagingException))]
        public void Send_Asynchronous_InvalidEncryption()
        {
            Settings.IgnoreCertificateErrorOnSend = false;
            CertificateValidator.SetError((c,u)=> (u == X509KeyUsageFlags.DataEncipherment) ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            RunAndHandleMessagingException(Client.SendAndContinueAsync(Logger, message), EventIds.RemoteCertificate);
        }
        [TestMethod]
        public void Send_Asynchronous_InvalidEncryption_Ignore()
        {
            Settings.IgnoreCertificateErrorOnSend = true;
            CertificateValidator.SetError((c, u) => (u == X509KeyUsageFlags.DataEncipherment) ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            RunAndHandleException(Client.SendAndContinueAsync(Logger, message));
        }

        [TestMethod]
        [ExpectedException(typeof(MessagingException))]
        public void Send_Asynchronous_InvalidSignature()
        {
            Settings.IgnoreCertificateErrorOnSend = false;
            CertificateValidator.SetError((c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            RunAndHandleMessagingException(Client.SendAndContinueAsync(Logger, message), EventIds.LocalCertificate);
        }
        [TestMethod]
        public void Send_Asynchronous_InvalidSignature_Ignore()
        {
            Settings.IgnoreCertificateErrorOnSend = true;
            CertificateValidator.SetError((c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            RunAndHandleMessagingException(Client.SendAndContinueAsync(Logger, message), EventIds.LocalCertificate);
        }
    }
}
