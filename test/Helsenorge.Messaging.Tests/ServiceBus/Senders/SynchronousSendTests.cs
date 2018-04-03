using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Schema;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.ServiceBus.Senders
{
    [TestClass]
    public class SynchronousSendTests : BaseTest
    {
        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            Settings.ServiceBus.Synchronous.ReplyQueueMapping = new Dictionary<string, string>
            {
                // machine name is set to lowercase by design, other code uses Environment.MachineName to look things up
                // so this will make things case-insensitive
                {Environment.MachineName.ToLower(), MockFactory.Helsenorge.SynchronousReply.Name}
            };
            Settings.ServiceBus.Synchronous.CallTimeout = TimeSpan.FromSeconds(1);
        }

        private OutgoingMessage CreateMessage()
        {
            return new OutgoingMessage()
            {
                ToHerId = MockFactory.OtherHerId,
                Payload = GenericMessage,
                MessageFunction = "DIALOG_INNBYGGER_EKONTAKT",
                MessageId = Guid.NewGuid().ToString("D"),
                PersonalId = "12345",
            };
        }

        [TestMethod]
        public void Send_Synchronous_OK()
        {
            var message = CreateMessage();
            
            // post a reply on the syncreply queue before posting the actual message
            var mockMessage = CreateMockMessage(message);
            mockMessage.To = MockFactory.Helsenorge.SynchronousReply.Name;
            mockMessage.ReplyTo = MockFactory.OtherParty.Synchronous.Name;
            mockMessage.Queue = MockFactory.Helsenorge.SynchronousReply.Messages;
            MockFactory.Helsenorge.SynchronousReply.Messages.Add(mockMessage);

            var response = RunAndHandleException(Client.SendAndWaitAsync(Logger, message));

            // make sure the content is what we expect
            Assert.AreEqual(GenericResponse.ToString(), response.ToString());
            // message should be gone from our sync reply
            Assert.AreEqual(0, MockFactory.Helsenorge.SynchronousReply.Messages.Count);
        }
        
        [TestMethod]
        [ExpectedException(typeof(MessagingException))]
        public void Send_Synchronous_ErrorQueue()
        {
            var message = CreateMessage();

            // post a reply on the syncreply queue before posting the actual message
            var mockMessage = CreateMockMessage(message);
            mockMessage.To = MockFactory.Helsenorge.SynchronousReply.Name;
            mockMessage.ReplyTo = MockFactory.OtherParty.Synchronous.Name;
            mockMessage.Queue = MockFactory.Helsenorge.SynchronousReply.Messages;
            MockFactory.Helsenorge.SynchronousReply.Messages.Add(mockMessage);
            Client.RegisterSynchronousReplyMessageReceivedCallback(m => { throw new XmlSchemaValidationException(); });

            //This call will timeout while waiting for the sync reply message
            var response = RunAndHandleException(Client.SendAndWaitAsync(Logger, message));

            // message should be moved to the error queue
            Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(MessagingException))]
        public void Send_Synchronous_Timeout()
        {
            var message = CreateMessage();
            
            //fake timeout by not posting any messag on the reply queue
            RunAndHandleMessagingException(Client.SendAndWaitAsync(Logger, message), EventIds.SynchronousCallTimeout);
        }
        [TestMethod]
        public void Send_Synchronous_DelayedReply()
        {
            var message = CreateMessage();
            
            // make first one time out
            try
            {
                RunAndHandleMessagingException(Client.SendAndWaitAsync(Logger, message), EventIds.SynchronousCallTimeout);
            }
            catch (MessagingException) // ignore timeout
            {
            }

            // then we post the message
            var mockMessage = CreateMockMessage(message);
            mockMessage.To = MockFactory.Helsenorge.SynchronousReply.Name;
            mockMessage.ReplyTo = MockFactory.OtherParty.Synchronous.Name;
            mockMessage.Queue = MockFactory.Helsenorge.SynchronousReply.Messages;
            MockFactory.Helsenorge.SynchronousReply.Messages.Add(mockMessage);

            try
            {
                System.Threading.Thread.Sleep(1000);
                // then we post a new one, and this causes the previous one to have timed out
                message = CreateMessage();
                RunAndHandleMessagingException(Client.SendAndWaitAsync(Logger, message), EventIds.SynchronousCallTimeout);
            }
            catch (MessagingException)// ignore timeout
            {
            }
            // make sure it's logged
            Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.SynchronousCallDelayed));
        }

        private static T RunAndHandleException<T>(Task<T> task)
        {
            try
            {
                return task.Result;
            }
            catch (AggregateException ex)
            {

                throw ex.InnerException;
            }
        }
        private static T RunAndHandleMessagingException<T>(Task<T> task, EventId id)
        {
            try
            {
                return task.Result;
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
