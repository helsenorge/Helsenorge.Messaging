using Amqp;
using Amqp.Framing;
using Helsenorge.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    public class ServiceBusMessageTests : IDisposable
    {
        private readonly ServiceBusFixture _fixture;
        private readonly ServiceBusSender _sender;
        private readonly ServiceBusReceiver _receiver;

        public string QueueName => nameof(ServiceBusMessageTests);

        public ServiceBusMessageTests(ITestOutputHelper output)
        {
            _fixture = new ServiceBusFixture(output);
            _sender = _fixture.CreateSender(QueueName);
            _receiver = _fixture.CreateReceiver(QueueName);
            _fixture.PurgeQueueAsync(QueueName).Wait();
            _fixture.PurgeQueueAsync(ServiceBusTestingConstants.GetDeadLetterQueueName(QueueName)).Wait();
        }

        public void Dispose()
        {
            _receiver.Close();
            _fixture.Dispose();
        }

        [Fact]
        public async Task Received_Message_Should_Have_Valid_Properties()
        {
            var messageId = Guid.NewGuid().ToString("N");
            var messageText = $"Test message {messageId}";
            var timestamp = DateTime.UtcNow;

            const int toHerId = 1001;
            const int fromHerId = 2001;
            const string contentType = "plain/text";
            const string cpaId = "testCpaId";
            const string correlationId = "testCorrelationId";
            const string replyTo = "testReplyTo";
            const string to = "testTo";
            const string messageFunction = "testMessageFunction";

            await _sender.SendAsync(new ServiceBusMessage(new Message
            {
                BodySection = new Data
                {
                    Binary = Encoding.UTF8.GetBytes(messageText)
                }
            })
            {
                ToHerId = toHerId,
                FromHerId = fromHerId,
                ContentType = contentType,
                CpaId = cpaId,
                CorrelationId = correlationId,
                MessageId = messageId,
                MessageFunction = messageFunction,
                ReplyTo = replyTo,
                To = to,
                ApplicationTimestamp = timestamp,
                Properties = new Dictionary<string, object>
                {
                    {"some-test-property", "some-test-property-value"}
                }
            });

            var incomingMessage = await _receiver.ReceiveAsync(ServiceBusTestingConstants.DefaultReadTimeout);
            Assert.NotNull(incomingMessage);
            await incomingMessage.CompleteAsync();

            Assert.Equal(messageText, await incomingMessage.GetBodyAsStingAsync());
            Assert.Equal(messageText.Length, incomingMessage.Size);
            Assert.Equal(0, incomingMessage.DeliveryCount); // First delivery
            Assert.Equal(fromHerId, incomingMessage.FromHerId);
            Assert.Equal(toHerId, incomingMessage.ToHerId);
            Assert.Equal(timestamp.TrimMillis().ToUniversalTime(), incomingMessage.ApplicationTimestamp.ToUniversalTime());
            Assert.Equal(cpaId, incomingMessage.CpaId);
            Assert.True(incomingMessage.EnqueuedTimeUtc > DateTime.UtcNow.AddMinutes(-1)); // FIXME: fragile check? can break on computers having wrong time?
            Assert.True(incomingMessage.ScheduledEnqueueTimeUtc > DateTime.UtcNow.AddMinutes(-1)); // FIXME: fragile check? can break on computers having wrong time?
            Assert.True(incomingMessage.ExpiresAtUtc > DateTime.UtcNow);
            Assert.NotNull(incomingMessage.Properties);
            Assert.Contains("some-test-property", incomingMessage.Properties.Keys);
            Assert.Equal("some-test-property-value", incomingMessage.Properties["some-test-property"]);
            Assert.Equal(contentType, incomingMessage.ContentType);
            Assert.Equal(correlationId, incomingMessage.CorrelationId);
            Assert.Equal(messageFunction, incomingMessage.MessageFunction);
            Assert.Equal(messageId, incomingMessage.MessageId);
            Assert.Equal(replyTo, incomingMessage.ReplyTo);
            Assert.Equal(replyTo, incomingMessage.ReplyTo);
            Assert.True(incomingMessage.TimeToLive > TimeSpan.Zero);
            Assert.Equal(to, incomingMessage.To);
        }

        [Fact]
        public async Task Should_Mark_Message_As_Accepted_Upon_Completion()
        {
            var messageText = await _fixture.SendTestMessageAsync(QueueName);
            var message = await _receiver.ReceiveAsync(ServiceBusTestingConstants.DefaultReadTimeout);
            Assert.NotNull(message);
            Assert.Equal(messageText, await message.GetBodyAsStingAsync());
            await message.CompleteAsync();
            Assert.Empty(await _fixture.ReadAllMessagesAsync(QueueName));
            await _fixture.CheckDeadLetterQueueAsync(QueueName);
        }

        [Fact]
        public async Task Should_Mark_Message_As_Rejected_When_Marking_As_Dead_Letter()
        {
            var messageText = await _fixture.SendTestMessageAsync(QueueName);
            var message = await _receiver.ReceiveAsync(ServiceBusTestingConstants.DefaultReadTimeout);
            Assert.NotNull(message);
            Assert.Equal(messageText, await message.GetBodyAsStingAsync());
            message.DeadLetter();

            await _fixture.CheckDeadLetterQueueAsync(QueueName, messageText);
        }

        [Fact]
        public async Task Should_Renew_Message_Lock()
        {
            var messageText = await _fixture.SendTestMessageAsync(QueueName);
            var message = (ServiceBusMessage)await _receiver.ReceiveAsync(ServiceBusTestingConstants.DefaultReadTimeout);
            Assert.NotNull(message);
            Assert.Equal(messageText, await message.GetBodyAsStingAsync());
            var wasLockedUntil = message.LockedUntil;
            await Task.Delay(TimeSpan.FromSeconds(1));
            message.RenewLock();
            Assert.True(message.LockedUntil > wasLockedUntil);
            Assert.True(message.LockedUntil - wasLockedUntil > TimeSpan.FromSeconds(1));
            await message.CompleteAsync();
        }
    }
}
