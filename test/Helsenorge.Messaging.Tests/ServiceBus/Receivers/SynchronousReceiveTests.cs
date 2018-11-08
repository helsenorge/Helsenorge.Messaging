using System;
using System.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus.Receivers;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Tests.ServiceBus.Receivers
{
    [TestClass]
    public class SynchronousReceiveTests : BaseTest
    {
        // the errro handling and decryption stuff is common between all listeners, so that is tested in the async tests
        private bool _startingCalled;
        private bool _receivedCalled;
        private bool _completedCalled;
        private bool _handledExceptionCalled;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _startingCalled = false;
            _receivedCalled = false;
            _completedCalled = false;
            _handledExceptionCalled = false;
        }

        [TestMethod]
        public void Synchronous_Receive_OK()
        {
            // postition of arguments have been reversed so that we inster the name of the argument without getting a resharper indication
            // makes it easier to read
            RunReceive(
                postValidation: () =>
                {
                    Assert.IsTrue(_startingCalled);
                    Assert.IsTrue(_receivedCalled);
                    Assert.IsTrue(_completedCalled);
                    Assert.AreEqual(0, MockFactory.Helsenorge.Synchronous.Messages.Count);
                    Assert.AreEqual(1, MockFactory.OtherParty.SynchronousReply.Messages.Count);
                },
                wait: () => _completedCalled,
                received: (m) =>
                {
                },
                messageModification: (m) => { });
        }

        [TestMethod]
        public void Synchronous_Receive_InvalidReplyToQueue_SendToErrorQueue()
        {
            RunReceive(
                postValidation: () =>
                {
                    Assert.IsTrue(_startingCalled);
                    Assert.IsTrue(_handledExceptionCalled);
                    Assert.AreEqual(0, MockFactory.Helsenorge.Synchronous.Messages.Count);
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    Assert.AreEqual("transport:invalid-field-value", MockFactory.OtherParty.Error.Messages.First().Properties["errorCondition"]);
                    Assert.AreEqual("Invalid value in field: 'ReplyTo'", MockFactory.OtherParty.Error.Messages.First().Properties["errorDescription"]);
                    var logEntry = MockLoggerProvider.Entries.Where(l => l.LogLevel == LogLevel.Critical);
                    Assert.AreEqual(1, logEntry.Count());
                    Assert.IsTrue(logEntry.First().Message == "Cannot send message to service bus. Invalid endpoint.");
                },
                wait: () => _handledExceptionCalled,
                received: (m) =>
                {
                },
                messageModification: (m) => { m.ReplyTo = "Dialog_" + m.ReplyTo; });
        }


        [TestMethod]
        public void Synchronous_ReceiveHerIdMismatch_ErrorQueueWithSpoofingErrorCode()
        {
            RunReceive(

                postValidation: () =>
                {
                    Assert.IsTrue(_startingCalled);
                    Assert.IsTrue(_handledExceptionCalled);
                    Assert.AreEqual(0, MockFactory.Helsenorge.Synchronous.Messages.Count);
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    Assert.AreEqual("abuse:spoofing-attack", MockFactory.OtherParty.Error.Messages.First().Properties["errorCondition"]);
                },
                wait: () => _handledExceptionCalled,
                received: m => { throw new SenderHerIdMismatchException(); },
                messageModification: m => { });
        }

        [TestMethod]
        public void Synchronous_Receive_ApplicationThrowsUnsupportedMessageException()
        {
            RunReceive(

                postValidation: () =>
                {
                    Assert.IsTrue(_startingCalled);
                    Assert.IsTrue(_handledExceptionCalled);
                    Assert.AreEqual(0, MockFactory.Helsenorge.Synchronous.Messages.Count);
                    Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
                    Assert.AreEqual("transport:unsupported-message", MockFactory.OtherParty.Error.Messages.First().Properties["errorCondition"]);
                },
                wait: () => _handledExceptionCalled,
                received: m => { throw new UnsupportedMessageException(); },
                messageModification: m => { });
        }

        private void RunReceive(
            Action<MockMessage> messageModification,
            Action<IncomingMessage> received,
            Func<bool> wait,
            Action postValidation)
        {
            // create and post message
            var message = CreateMockMessage();
            messageModification(message);
            MockFactory.Helsenorge.Synchronous.Messages.Add(message);

            // configure notifications
            Server.RegisterSynchronousMessageReceivedStartingCallback((m) => _startingCalled = true);
            Server.RegisterSynchronousMessageReceivedCallback((m) => 
            {
                received(m);
                _receivedCalled = true;
                return GenericResponse;
            });
            Server.RegisterSynchronousMessageReceivedCompletedCallback((m) => _completedCalled = true);
            Server.RegisterHandledExceptionCallback((messagingMessage, exception) => _handledExceptionCalled = true);

            Server.Start();

            Wait(15, wait); // we have a high timeout in case we do a bit of debugging. With more extensive debugging (breakpoints), we will get a timeout
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
                if (DateTime.Now > max) throw new TimeoutException();

                if (check()) return;
                System.Threading.Thread.Sleep(50);
            }
        }

        private MockMessage CreateMockMessage()
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
                ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(2),
                TimeToLive = TimeSpan.FromSeconds(15),
                ReplyTo = MockFactory.OtherParty.SynchronousReply.Name,
                To = MockFactory.OtherParty.Synchronous.Name,
                Queue = MockFactory.Helsenorge.Synchronous.Messages,
                DeadLetterQueue = MockFactory.DeadLetterQueue
            };
        }
    }
}
