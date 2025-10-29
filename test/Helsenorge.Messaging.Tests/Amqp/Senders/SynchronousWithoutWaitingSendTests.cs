/*
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Registries.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.Amqp.Senders
{
    [TestClass]
    public class SynchronousWithoutWaitingSendTests : BaseTest
    {
        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            Settings.AmqpSettings.Synchronous.StaticReplyQueue = MockFactory.Helsenorge.SynchronousReply.Name;
            Settings.AmqpSettings.Synchronous.CallTimeout = TimeSpan.FromSeconds(1);
        }

        private OutgoingMessage CreateMessage()
        {
            return new OutgoingMessage
            {
                ToHerId = MockFactory.OtherHerId,
                Payload = GenericMessage,
                MessageFunction = "DIALOG_INNBYGGER_EKONTAKT",
                MessageId = Guid.NewGuid().ToString("D"),
                PersonalId = "12345",
            };
        }


        [TestMethod]
        public void Send_SynchronousWithoutWaiting_Using_CPA()
        {
            var message = CreateMessage();
            message.ToHerId = MockFactory.OtherHerId;
            RunAndHandleException(Client.SendWithoutWaitingAsync(message));

            Assert.HasCount(1, MockFactory.OtherParty.Synchronous.Messages);
            // message includes CPA id
            Assert.IsNotNull(MockFactory.OtherParty.Synchronous.Messages[0].CpaId);
        }

        [TestMethod]
        public void Send_SynchronousWithoutWaiting_NoMessage()
        {
            Assert.Throws<ArgumentNullException>(() => RunAndHandleException(Client.SendWithoutWaitingAsync(null)));
        }

        [TestMethod]
        public void Send_SynchronousWithoutWaiting_Error_Missing_ToHerId()
        {
            var message = CreateMessage();
            message.ToHerId = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RunAndHandleException(Client.SendWithoutWaitingAsync(message)));
        }

        [TestMethod]
        public void Send_SynchronousWithoutWaiting_Error_Missing_MessageFunction()
        {
            var message = CreateMessage();
            message.MessageFunction = null;
            Assert.Throws<ArgumentNullException>(() => RunAndHandleException(Client.SendWithoutWaitingAsync(message)));
        }

        [TestMethod]
        public void Send_SynchronousWithoutWaiting_InvalidEncryption()
        {
            Settings.IgnoreCertificateErrorOnSend = false;
            CertificateValidator.SetError((_, u) =>
                (u == X509KeyUsageFlags.KeyEncipherment) ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            Assert.Throws<MessagingException>(() =>
                RunAndHandleMessagingException(Client.SendWithoutWaitingAsync(message), EventIds.RemoteCertificate));
        }

        [TestMethod]
        public void Send_SynchronousWithoutWaiting_InvalidSignature()
        {
            Settings.IgnoreCertificateErrorOnSend = false;
            CertificateValidator.SetError((_, u) =>
                (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.StartDate : CertificateErrors.None);

            var message = CreateMessage();
            Assert.Throws<MessagingException>(() =>
                RunAndHandleMessagingException(Client.SendWithoutWaitingAsync(message), EventIds.LocalCertificate));
        }
    }
}