/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Xml.Linq;
using System.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

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
                    Assert.AreEqual(0, MockFactory.Helsenorge.Error.Messages.Count);
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
                    Assert.AreEqual(0, MockFactory.Helsenorge.Error.Messages.Count);
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
                System.Threading.Thread.Sleep(50);
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
