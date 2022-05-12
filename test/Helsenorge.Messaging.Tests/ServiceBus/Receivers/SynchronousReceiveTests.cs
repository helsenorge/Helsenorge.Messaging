﻿/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus.Receivers;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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
        public async Task Synchronous_Receive_OK()
        {
            // postition of arguments have been reversed so that we inster the name of the argument without getting a resharper indication
            // makes it easier to read
            await RunReceive(
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

        [TestMethod, Ignore]
        public async Task Synchronous_Receive_InvalidReplyToQueue_SendToErrorQueue()
        {
            await RunReceive(
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
                    Assert.IsTrue(logEntry.First().Message == "An error occurred during Send operation.");
                },
                wait: () => _handledExceptionCalled,
                received: (m) =>
                {
                },
                messageModification: (m) => { m.ReplyTo = "Dialog_" + m.ReplyTo; });
        }


        [TestMethod]
        public async Task Synchronous_ReceiveHerIdMismatch_ErrorQueueWithSpoofingErrorCode()
        {
            await RunReceive(
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
        public async Task Synchronous_Receive_ApplicationThrowsUnsupportedMessageException()
        {
            await RunReceive(
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

        private async Task RunReceive(
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
            Server.RegisterSynchronousMessageReceivedStartingCallbackAsync((m) =>
            {
                _startingCalled = true;
                return Task.CompletedTask;
            });
            Server.RegisterSynchronousMessageReceivedCallbackAsync((m) => 
            {
                received(m);
                _receivedCalled = true;
                return Task.FromResult(GenericResponse);
            });
            Server.RegisterSynchronousMessageReceivedCompletedCallbackAsync((m) =>
            {
                _completedCalled = true;
                return Task.CompletedTask;
            });
            Server.RegisterHandledExceptionCallbackAsync((messagingMessage, exception) =>
            {
                _handledExceptionCalled = true;
                return Task.CompletedTask;
            });

            await Server.Start();

            Wait(15, wait); // we have a high timeout in case we do a bit of debugging. With more extensive debugging (breakpoints), we will get a timeout
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
