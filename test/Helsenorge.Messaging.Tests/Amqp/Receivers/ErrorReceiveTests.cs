/*
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.Amqp.Receivers
{
    [TestClass]
    public class ErrorReceiveTests : BaseTest
    {
        private bool _errorReceiveCalled;
        private bool _errorStartingCalled;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _errorReceiveCalled = false;
            _errorStartingCalled = false;
        }

        [TestMethod]
        public async Task Error_Receive_Encrypted()
        {
            // postition of arguments have been reversed so that we inster the name of the argument without getting a resharper indication
            // makes it easier to read
            await RunReceive(
                GenericMessage,
                postValidation: () =>
                {
                    Assert.IsTrue(_errorReceiveCalled);
                    Assert.IsEmpty(MockFactory.Helsenorge.Error.Messages);
                    Assert.IsTrue(_errorStartingCalled, "Error message received starting callback not called");
                    Assert.IsNull(MockLoggerProvider.Entries.FirstOrDefault(e => e.Message.Contains("CPA_FindAgreementForCounterpartyAsync_0_93252")));
                    Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.ExternalReportedError));
                    Assert.AreEqual("Label: DIALOG_INNBYGGER_EKONTAKT property1: 1 property2: 1 ", MockLoggerProvider.FindEntry(EventIds.ExternalReportedError).Message);
                },
                wait: () => _errorReceiveCalled,
                messageModification: (m) =>
                {
                    m.SetApplicationPropertyValue("property1", 1);
                    m.SetApplicationPropertyValue("property2", 1);
                });
        }
        [TestMethod]
        public async Task Error_Receive_Soap()
        {
            // postition of arguments have been reversed so that we inster the name of the argument without getting a resharper indication
            // makes it easier to read
            await RunReceive(
                SoapFault,
                postValidation: () =>
                {
                    Assert.IsTrue(_errorReceiveCalled);
                    Assert.IsEmpty(MockFactory.Helsenorge.Error.Messages);
                    Assert.IsTrue(_errorStartingCalled, "Error message received starting callback not called");
                    Assert.IsNotNull(MockLoggerProvider.FindEntry(EventIds.ExternalReportedError));
                },
                wait: () => _errorReceiveCalled,
                messageModification: (m) =>
                {
                    m.ContentType = ContentType.Soap;
                    m.MessageFunction = "AMQP_SOAP_FAULT";
                });
        }

        [TestMethod]
        public async Task Error_Created_By_ReportErrorToExternalSender_Is_Readable_In_ErrorListener()
        {
            // Simulate a message that "we" (Helsenorge, 93238) sent to the other party (93252).
            // The other party fails to process it and reports an error back to our error queue.
            var originalMessageId = Guid.NewGuid().ToString("D");
            var originalMessage = new MockMessage(GenericMessage)
            {
                MessageFunction = "DIALOG_INNBYGGER_EKONTAKT",
                ApplicationTimestamp = DateTime.UtcNow,
                ContentType = ContentType.SignedAndEnveloped,
                MessageId = originalMessageId,
                CorrelationId = originalMessageId,
                FromHerId = MockFactory.HelsenorgeHerId, // we sent the original message
                ToHerId = MockFactory.OtherHerId,        // the other party received it and reports the error
                TimeToLive = TimeSpan.FromSeconds(15),
                Queue = MockFactory.OtherParty.Asynchronous.Messages,
                DeadLetterQueue = MockFactory.OtherParty.DeadLetter.Messages,
            };
            MockFactory.OtherParty.Asynchronous.Messages.Add(originalMessage);

            // The other party reports the error. This creates a brand new error message
            // (SendErrorAsync) and sends it to the original sender's error queue (93238_error).
            await Client.AmqpCore.ReportErrorToExternalSenderAsync(
                Logger,
                EventIds.ApplicationReported,
                originalMessage,
                "transport:internal-error",
                "Something went wrong",
                new[] { "additional-info" });

            // the original message has been completed (removed from the processing queue)
            Assert.AreEqual(0, MockFactory.OtherParty.Asynchronous.Messages.Count);
            // and the error message is now waiting on our error queue
            Assert.AreEqual(1, MockFactory.Helsenorge.Error.Messages.Count);

            // receive the error message with an ErrorListener
            IAmqpMessage receivedErrorMessage = null;
            Server.RegisterErrorMessageReceivedCallbackAsync((m) =>
            {
                receivedErrorMessage = m;
                _errorReceiveCalled = true;
                return Task.CompletedTask;
            });
            Server.RegisterErrorMessageReceivedStartingCallback((m) =>
            {
                _errorStartingCalled = true;
                return Task.CompletedTask;
            });
            await Server.StartAsync();
            Wait(15, () => _errorReceiveCalled);
            await Server.StopAsync();

            Assert.IsTrue(_errorStartingCalled, "Error message received starting callback not called");
            Assert.IsNotNull(receivedErrorMessage, "Error message was not received by the error listener");

            // the error message must pass the header validation - no "missing fields" warnings
            Assert.IsNull(MockLoggerProvider.Entries.FirstOrDefault(e => e.Message.Contains("One or more fields are missing")));

            // the error travels in the opposite direction of the original message
            Assert.AreEqual(MockFactory.OtherHerId, receivedErrorMessage.FromHerId);
            Assert.AreEqual(MockFactory.HelsenorgeHerId, receivedErrorMessage.ToHerId);
            Assert.AreEqual("DIALOG_INNBYGGER_EKONTAKT", receivedErrorMessage.MessageFunction);
            // the error message has its own unique message id, but carries the correlation id of the original message
            Assert.AreNotEqual(originalMessageId, receivedErrorMessage.MessageId);
            Assert.AreEqual(originalMessageId, receivedErrorMessage.CorrelationId);
            // the error details are readable from the application properties
            Assert.AreEqual("transport:internal-error", receivedErrorMessage.Properties["errorCondition"]);
            Assert.AreEqual("Something went wrong", receivedErrorMessage.Properties["errorDescription"]);
            Assert.AreEqual("additional-info;", receivedErrorMessage.Properties["errorConditionData"]);
            Assert.AreEqual(originalMessageId, receivedErrorMessage.Properties["originalMessageId"]);

            // the error listener has logged the reported error with the error properties
            var externalReportedError = MockLoggerProvider.FindEntry(EventIds.ExternalReportedError);
            Assert.IsNotNull(externalReportedError);
            Assert.IsTrue(externalReportedError.Message.Contains("errorCondition: transport:internal-error"));
            Assert.IsTrue(externalReportedError.Message.Contains("errorDescription: Something went wrong"));

            // the error message has been completed (removed from the error queue)
            Assert.AreEqual(0, MockFactory.Helsenorge.Error.Messages.Count);
        }

        private async Task RunReceive(
            XDocument content,
            Action<MockMessage> messageModification,
            Func<bool> wait,
            Action postValidation)
        {
            // create and post message
            var message = CreateMockMessage(content);
            messageModification(message);
            MockFactory.Helsenorge.Error.Messages.Add(message);

            Server.RegisterErrorMessageReceivedCallbackAsync((m) => 
            { 
                _errorReceiveCalled = true;
                return Task.CompletedTask;
            });
            Server.RegisterErrorMessageReceivedStartingCallback((m) =>
            {
                _errorStartingCalled = true;
                return Task.CompletedTask;
            });
            await Server.StartAsync();

            Wait(15, wait); // we have a high timeout in case we do a bit of debugging. With more extensive debugging (breakpoints), we will get a timeout
            await Server.StopAsync();

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
            var max = DateTime.UtcNow.Add(TimeSpan.FromSeconds(timeout));

            while (true)
            {
                if (DateTime.UtcNow > max) throw new TimeoutException();

                if (check()) return;
                Thread.Sleep(50);
            }
        }

        private MockMessage CreateMockMessage(XDocument content = null)
        {
            var messageId = Guid.NewGuid().ToString("D");
            return new MockMessage(content ?? GenericResponse)
            {
                MessageFunction = "DIALOG_INNBYGGER_EKONTAKT",
                ApplicationTimestamp = DateTime.UtcNow,
                ContentType = ContentType.SignedAndEnveloped,
                MessageId = messageId,
                CorrelationId = messageId,
                FromHerId = MockFactory.OtherHerId,
                ToHerId = MockFactory.HelsenorgeHerId,
                TimeToLive = TimeSpan.FromSeconds(15),
                ReplyTo = MockFactory.OtherParty.Synchronous.Name,
                Queue = MockFactory.Helsenorge.Error.Messages,
                DeadLetterQueue = MockFactory.Helsenorge.DeadLetter.Messages
            };
        }
    }
}
