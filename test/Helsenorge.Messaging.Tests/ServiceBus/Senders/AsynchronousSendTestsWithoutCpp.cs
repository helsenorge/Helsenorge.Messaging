using System;
using Helsenorge.Messaging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.ServiceBus.Senders
{
    [TestClass]
    public class AsynchronousSendTestsWithoutCpp : BaseTest
    {
        private const int CommunicationPartyWithoutCppHerId = 94867;

        [TestInitialize]
        public override void Setup()
        {
            SetupInternal(CommunicationPartyWithoutCppHerId);
        }
        
        private OutgoingMessage CreateMessageForCommunicationPartyWithoutCpp()
        {
            return new OutgoingMessage()
            {
                ToHerId = CommunicationPartyWithoutCppHerId,
                Payload = GenericMessage,
                MessageFunction = "DIALOG_INNBYGGER_EKONTAKT",
                MessageId = Guid.NewGuid().ToString("D"),
                PersonalId = "12345"
            };
        }

        [TestMethod, Ignore]
        public void Send_Asynchronous_CommunicationPartyWithoutCpp_OK()
        {
            var message = CreateMessageForCommunicationPartyWithoutCpp();

            RunAndHandleException(Client.SendAndContinueAsync(Logger, message));

            Assert.AreEqual(1, MockFactory.OtherParty.Asynchronous.Messages.Count);
        }
    }
}
