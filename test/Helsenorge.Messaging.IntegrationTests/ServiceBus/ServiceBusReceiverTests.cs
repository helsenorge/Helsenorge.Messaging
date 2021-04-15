/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.ServiceBus;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    public class ServiceBusReceiverTests : IAsyncDisposable
    {
        public string QueueName => nameof(ServiceBusReceiverTests);

        private readonly ServiceBusFixture _fixture;
        private readonly ServiceBusReceiver _receiver;

        public ServiceBusReceiverTests(ITestOutputHelper output)
        {
            _fixture = new ServiceBusFixture(output);
            _receiver = _fixture.CreateReceiver(QueueName);
            _fixture.PurgeQueueAsync(QueueName).Wait();
        }

        public async ValueTask DisposeAsync()
        {
            await _receiver.Close();
            _fixture.Dispose();
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Not_Recreate_Receiver_If_No_Underlying_Object_Is_Closed()
        {
            Assert.False(_receiver.IsClosed);
            await ReceiveTestMessageAsync();
            Assert.False(_receiver.IsClosed);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Recreate_Link_When_Underlying_Connection_Is_Closed()
        {
            Assert.False(_receiver.IsClosed);
            var connection = await _fixture.Connection.GetConnection();
            await connection.CloseAsync();
            await ReceiveTestMessageAsync();
            Assert.False(_receiver.IsClosed);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Not_Allow_To_Receive_Message_When_Connection_Is_Closed()
        {
            var connection = _fixture.Connection;
            Assert.False(_receiver.IsClosed);
            await connection.CloseAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await _receiver.ReceiveAsync(TimeSpan.Zero));
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Not_Allow_To_Receive_Message_When_Receiver_Is_Closed()
        {
            Assert.False(_receiver.IsClosed);
            await _receiver.Close();
            Assert.True(_receiver.IsClosed);
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await _receiver.ReceiveAsync(TimeSpan.Zero));
        }

        private async Task ReceiveTestMessageAsync()
        {
            var messageText = await _fixture.SendTestMessageAsync(QueueName);
            var message = await _receiver.ReceiveAsync(ServiceBusTestingConstants.DefaultReadTimeout);
            Assert.NotNull(message);
            await message.CompleteAsync();
            Assert.Equal(messageText, await message.GetBodyAsStingAsync());
        }
    }
}
