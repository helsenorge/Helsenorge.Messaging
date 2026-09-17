/*
 * Copyright (c) 2020-2026, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.Amqp
{
    /// <summary>
    /// Tests the different scenarios in AmqpCore.SendErrorAsync (via ReportErrorToExternalSenderAsync).
    /// </summary>
    [TestClass]
    public class SendErrorTests : BaseTest
    {
        private const string UnexpectedContentType = "application/x-something-unexpected";

        /// <summary>
        /// Creates a mock of a message the other party (93252) sent to us (Helsenorge, 93238),
        /// which we fail to process and report an error for.
        /// </summary>
        private MockMessage CreateOriginalMessage()
        {
            var messageId = Guid.NewGuid().ToString("D");
            return new MockMessage(GenericMessage)
            {
                MessageFunction = "DIALOG_INNBYGGER_EKONTAKT",
                ApplicationTimestamp = DateTime.UtcNow,
                ContentType = ContentType.SignedAndEnveloped,
                MessageId = messageId,
                CorrelationId = messageId,
                FromHerId = MockFactory.OtherHerId,
                ToHerId = MockFactory.HelsenorgeHerId,
                TimeToLive = TimeSpan.FromSeconds(15),
                Queue = MockFactory.Helsenorge.Asynchronous.Messages,
                DeadLetterQueue = MockFactory.Helsenorge.DeadLetter.Messages,
            };
        }

        private async Task ReportError(MockMessage originalMessage, string errorCode = "transport:internal-error",
            string errorDescription = "Something went wrong", string[] additionalData = null)
        {
            MockFactory.Helsenorge.Asynchronous.Messages.Add(originalMessage);
            await Client.AmqpCore.ReportErrorToExternalSenderAsync(
                Logger,
                EventIds.ApplicationReported,
                originalMessage,
                errorCode,
                errorDescription,
                additionalData);
        }

        [TestMethod]
        public async Task SendError_UnexpectedContentType_IsCopiedVerbatim_And_ErrorMessageIsStillProcessable()
        {
            var originalMessage = CreateOriginalMessage();
            originalMessage.ContentType = UnexpectedContentType;

            await ReportError(originalMessage);

            // the error message carries the unexpected content type verbatim
            Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
            var errorMessage = MockFactory.OtherParty.Error.Messages.Single();
            Assert.AreEqual(UnexpectedContentType, errorMessage.ContentType);

            // an error message with an unexpected content type can still be processed by an
            // ErrorMessageListener; the error queue skips payload handling for non-text/non-soap
            // content types, so only the application properties are used.
            // Simulate this by moving the message onto our own error queue and consuming it.
            MockFactory.OtherParty.Error.Messages.Clear();
            var mockErrorMessage = (MockMessage)errorMessage;
            mockErrorMessage.Queue = MockFactory.Helsenorge.Error.Messages;
            mockErrorMessage.DeadLetterQueue = MockFactory.Helsenorge.DeadLetter.Messages;
            MockFactory.Helsenorge.Error.Messages.Add(mockErrorMessage);

            var errorReceiveCalled = false;
            Server.RegisterErrorMessageReceivedCallbackAsync(_ =>
            {
                errorReceiveCalled = true;
                return Task.CompletedTask;
            });
            await Server.StartAsync();
            Wait(15, () => errorReceiveCalled);
            await Server.StopAsync();

            Assert.IsEmpty(MockFactory.Helsenorge.Error.Messages);
            var externalReportedError = MockLoggerProvider.FindEntry(EventIds.ExternalReportedError);
            Assert.IsNotNull(externalReportedError);
            Assert.IsTrue(externalReportedError.Message.Contains("errorCondition: transport:internal-error"));
        }

        [TestMethod]
        public async Task SendError_MissingContentType_FallsBackToTextContentType()
        {
            var originalMessage = CreateOriginalMessage();
            originalMessage.ContentType = null;

            await ReportError(originalMessage);

            Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
            var errorMessage = MockFactory.OtherParty.Error.Messages.Single();
            // ContentType is required by the receiving side's header validation,
            // so SendErrorAsync falls back to text/plain
            Assert.AreEqual(ContentType.Text, errorMessage.ContentType);
        }

        [TestMethod]
        public async Task SendError_MissingMessageFunction_ErrorMessageIsSentWithoutLabel()
        {
            var originalMessage = CreateOriginalMessage();
            originalMessage.MessageFunction = null;

            await ReportError(originalMessage);

            // Pitfall: SendErrorAsync copies MessageFunction verbatim without any fallback.
            // The error message is sent without a label and will fail header validation
            // ("Label" missing) on the receiving side.
            Assert.AreEqual(1, MockFactory.OtherParty.Error.Messages.Count);
            var errorMessage = MockFactory.OtherParty.Error.Messages.Single();
            Assert.IsTrue(string.IsNullOrEmpty(errorMessage.MessageFunction));
        }

        [TestMethod]
        public async Task ErrorMessage_WithoutLabel_FailsHeaderValidation_And_TriggersNewErrorMessage()
        {
            // Documents the ping-pong pitfall: an error message without a label fails header
            // validation at the receiver, which reports a *new* error message back to the other
            // party's error queue. If both parties run this library, this can bounce back and forth.
            var messageId = Guid.NewGuid().ToString("D");
            var errorMessageWithoutLabel = new MockMessage(GenericMessage)
            {
                MessageFunction = "", // missing label
                ApplicationTimestamp = DateTime.UtcNow,
                ContentType = ContentType.Text,
                MessageId = messageId,
                CorrelationId = messageId,
                FromHerId = MockFactory.OtherHerId,
                ToHerId = MockFactory.HelsenorgeHerId,
                TimeToLive = TimeSpan.FromSeconds(15),
                Queue = MockFactory.Helsenorge.Error.Messages,
                DeadLetterQueue = MockFactory.Helsenorge.DeadLetter.Messages,
            };
            MockFactory.Helsenorge.Error.Messages.Add(errorMessageWithoutLabel);

            await Server.StartAsync();
            Wait(15, () => MockFactory.OtherParty.Error.Messages.Count == 1);
            await Server.StopAsync();

            // the malformed error message failed header validation and was removed from the queue
            Assert.IsEmpty(MockFactory.Helsenorge.Error.Messages);
            Assert.IsNotNull(MockLoggerProvider.Entries
                .FirstOrDefault(e => e.Message.Contains("One or more fields are missing")));

            // and a new error message has been sent back to the other party - still without a label
            var newErrorMessage = MockFactory.OtherParty.Error.Messages.Single();
            Assert.IsTrue(string.IsNullOrEmpty(newErrorMessage.MessageFunction));
            Assert.AreEqual("transport:invalid-field-value", newErrorMessage.Properties["errorCondition"]);
        }

        [TestMethod]
        public async Task SendError_MissingFromHerId_LogsWarning_And_DoesNotSendErrorMessage()
        {
            var originalMessage = CreateOriginalMessage();
            originalMessage.FromHerId = 0;

            await ReportError(originalMessage);

            // no error message has been sent - we have no idea where to send it
            Assert.IsEmpty(MockFactory.OtherParty.Error.Messages);
            Assert.IsEmpty(MockFactory.Helsenorge.Error.Messages);
            var warning = MockLoggerProvider.FindEntry(EventIds.MissingField);
            Assert.IsNotNull(warning);
            Assert.IsTrue(warning.Message.Contains("FromHerId is missing"));
            // but the original message is still removed from the processing queue
            Assert.IsEmpty(MockFactory.Helsenorge.Asynchronous.Messages);
        }

        [TestMethod]
        public async Task SendError_FromHerIdUnknownInAddressRegistry_ThrowsMessagingException_And_MessageStaysInQueue()
        {
            var originalMessage = CreateOriginalMessage();
            originalMessage.FromHerId = 1234; // no CommunicationDetails_1234.xml exists

            MessagingException messagingException = null;
            try
            {
                await ReportError(originalMessage);
            }
            catch (MessagingException ex)
            {
                messagingException = ex;
            }

            // Pitfall: when the sender cannot be found in the Address Registry, the exception
            // propagates and the original message is *not* removed from the processing queue.
            Assert.IsNotNull(messagingException, "Expected a MessagingException");
            Assert.AreEqual(EventIds.SenderMissingInAddressRegistryEventId.Id, messagingException.EventId.Id);
            Assert.IsEmpty(MockFactory.OtherParty.Error.Messages);
            Assert.AreEqual(1, MockFactory.Helsenorge.Asynchronous.Messages.Count);
        }

        [TestMethod]
        public async Task SendError_MissingCorrelationId_FallsBackToOriginalMessageId()
        {
            var originalMessage = CreateOriginalMessage();
            originalMessage.CorrelationId = null;

            await ReportError(originalMessage);

            var errorMessage = MockFactory.OtherParty.Error.Messages.Single();
            Assert.AreEqual(originalMessage.MessageId, errorMessage.CorrelationId);
        }

        [TestMethod]
        public async Task SendError_ErrorMessageHasItsOwnMessageId_And_ReversedDirection()
        {
            var originalMessage = CreateOriginalMessage();

            await ReportError(originalMessage);

            var errorMessage = MockFactory.OtherParty.Error.Messages.Single();
            Assert.AreNotEqual(originalMessage.MessageId, errorMessage.MessageId);
            // the error travels in the opposite direction of the original message
            Assert.AreEqual(originalMessage.ToHerId, errorMessage.FromHerId);
            Assert.AreEqual(originalMessage.FromHerId, errorMessage.ToHerId);
            Assert.AreEqual(originalMessage.MessageId, errorMessage.Properties["originalMessageId"]);
        }

        [TestMethod]
        public async Task SendError_NoAdditionalData_DoesNotSetErrorConditionDataProperty()
        {
            var originalMessage = CreateOriginalMessage();

            await ReportError(originalMessage, additionalData: null);

            var errorMessage = MockFactory.OtherParty.Error.Messages.Single();
            Assert.AreEqual("transport:internal-error", errorMessage.Properties["errorCondition"]);
            Assert.AreEqual("Something went wrong", errorMessage.Properties["errorDescription"]);
            Assert.IsFalse(errorMessage.Properties.ContainsKey("errorConditionData"));
        }

        [TestMethod]
        public async Task SendError_MissingErrorCode_ThrowsArgumentNullException()
        {
            var originalMessage = CreateOriginalMessage();

            ArgumentNullException argumentNullException = null;
            try
            {
                await ReportError(originalMessage, errorCode: null);
            }
            catch (ArgumentNullException ex)
            {
                argumentNullException = ex;
            }

            Assert.IsNotNull(argumentNullException, "Expected an ArgumentNullException");
            // Pitfall: the exception propagates and the original message is *not* removed from the queue
            Assert.AreEqual(1, MockFactory.Helsenorge.Asynchronous.Messages.Count);
        }

        [TestMethod]
        public async Task SendError_MissingErrorDescription_ThrowsArgumentNullException()
        {
            var originalMessage = CreateOriginalMessage();

            ArgumentNullException argumentNullException = null;
            try
            {
                await ReportError(originalMessage, errorDescription: null);
            }
            catch (ArgumentNullException ex)
            {
                argumentNullException = ex;
            }

            Assert.IsNotNull(argumentNullException, "Expected an ArgumentNullException");
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
                if (DateTime.UtcNow > max)
                {
                    throw new TimeoutException();
                }

                if (check())
                {
                    return;
                }

                Thread.Sleep(50);
            }
        }
    }
}



