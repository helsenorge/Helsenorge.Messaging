/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using Helsenorge.Messaging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.ServiceBus.Senders
{
    [TestClass]
    public class AsynchronousSendTestsWithoutCpp : BaseTest
    {
        private const int CommunicationPartyAHerId = 93238;
        private const int CommunicationPartyBHerId = 93252;

        [TestInitialize]
        public override void Setup()
        {
            SetupInternal(CommunicationPartyBHerId);
        }
        
        private OutgoingMessage CreateMessageForCommunicationPartyWithoutCpp()
        {
            return new OutgoingMessage()
            {
                FromHerId = CommunicationPartyAHerId,
                ToHerId = CommunicationPartyBHerId,
                Payload = GenericMessage,
                MessageFunction = "NO_CPA_MESSAGE",
                MessageId = Guid.NewGuid().ToString("D"),
                PersonalId = "12345"
            };
        }

        [TestMethod]
        public void Send_Asynchronous_CommunicationPartyWithoutCpp_OK()
        {
            var message = CreateMessageForCommunicationPartyWithoutCpp();
            Settings.MessageFunctionsExcludedFromCpaResolve = new List<string> { "NO_CPA_MESSAGE" };

            RunAndHandleException(Client.SendAndContinueAsync(message));

            Assert.AreEqual(1, MockFactory.OtherParty.Asynchronous.Messages.Count);
        }

        [TestMethod]
        public void Send_Asynchronous_CommunicationPartyWithoutCpp_Exception()
        {
            var message = CreateMessageForCommunicationPartyWithoutCpp();

            Assert.ThrowsException<MessagingException>(() => RunAndHandleException(Client.SendAndContinueAsync(message)));

            Assert.AreEqual(0, MockFactory.OtherParty.Asynchronous.Messages.Count);
        }

        private OutgoingMessage CreateApprecMessageForCommunicationPartyWithoutCpp()
        {
            return new OutgoingMessage()
            {
                FromHerId = CommunicationPartyAHerId,
                ToHerId = CommunicationPartyBHerId,
                Payload = GenericMessage,
                MessageFunction = "APPREC",
                MessageId = Guid.NewGuid().ToString("D"),
                PersonalId = "12345",
                ReceiptForMessageFunction = "NO_CPA_MESSAGE",
            };
        }

        [TestMethod]
        public void Send_Asynchronous_Apprec_CommunicationPartyWithoutCpp_OK()
        {
            var message = CreateApprecMessageForCommunicationPartyWithoutCpp();
            Settings.MessageFunctionsExcludedFromCpaResolve = new List<string> { "NO_CPA_MESSAGE" };

            RunAndHandleException(Client.SendAndContinueAsync(message));

            Assert.AreEqual(1, MockFactory.OtherParty.Asynchronous.Messages.Count);
        }

        [TestMethod]
        public void Send_Asynchronous_Apprec_CommunicationPartyWithoutCpp_Exception()
        {
            var message = CreateApprecMessageForCommunicationPartyWithoutCpp();

            Assert.ThrowsException<MessagingException>(() => RunAndHandleException(Client.SendAndContinueAsync(message)));

            Assert.AreEqual(0, MockFactory.OtherParty.Asynchronous.Messages.Count);
        }
    }
}
