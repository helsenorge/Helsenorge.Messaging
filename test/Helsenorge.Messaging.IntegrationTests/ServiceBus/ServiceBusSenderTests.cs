/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Text;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Helsenorge.Messaging.ServiceBus;
using Xunit;
using Xunit.Abstractions;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    public class ServiceBusSenderTests : IAsyncDisposable
    {
        public string QueueName => nameof(ServiceBusSenderTests);

        private readonly ServiceBusFixture _fixture;
        private readonly ServiceBusSender _sender;

        public ServiceBusSenderTests(ITestOutputHelper output)
        {
            _fixture = new ServiceBusFixture(output);
            _sender = _fixture.CreateSender(QueueName);
            _fixture.PurgeQueueAsync(QueueName).Wait();
        }

        public async ValueTask DisposeAsync()
        {
            await _sender.Close();
            _fixture.Dispose();
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Not_Recreate_Sender_If_No_Underlying_Object_Is_Closed()
        {
            Assert.False(_sender.IsClosed);
            await SendTestMessageAsync();
            Assert.False(_sender.IsClosed);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Recreate_Link_When_Underlying_Connection_Is_Closed()
        {
            Assert.False(_sender.IsClosed);
            var connection = await _fixture.Connection.GetConnection();
            await connection.CloseAsync();
            await SendTestMessageAsync();
            Assert.False(_sender.IsClosed);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Not_Allow_To_Send_Message_When_Connection_Is_Closed()
        {
            var connection = _fixture.Connection;
            Assert.False(_sender.IsClosed);
            await connection.CloseAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(SendTestMessageAsync);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Not_Allow_To_Send_Message_When_Sender_Is_Closed()
        {
            Assert.False(_sender.IsClosed);
            await _sender.Close();
            Assert.True(_sender.IsClosed);
            await Assert.ThrowsAsync<ObjectDisposedException>(SendTestMessageAsync);
        }

        private async Task<string> SendTestMessageAsync()
        {
            var messageText = $"Test message {Guid.NewGuid():N}";
            await _sender.SendAsync(new ServiceBusMessage(new Message
            {
                BodySection = new Data
                {
                    Binary = Encoding.UTF8.GetBytes(messageText)
                }
            }));
            await _fixture.CheckMessageSentAsync(QueueName, messageText);
            return messageText;
        }
    }
}
