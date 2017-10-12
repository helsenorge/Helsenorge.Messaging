using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Microsoft.ServiceBus.Messaging;

namespace Helsenorge.Messaging.ServiceBus
{
    [ExcludeFromCodeCoverage] // Azure service bus implementation
    internal class ServiceBusReceiver : IMessagingReceiver
    {
        private readonly MessageReceiver _implementation;

        public ServiceBusReceiver(MessageReceiver implementation)
        {
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            _implementation = implementation;
        }
        public async Task<IMessagingMessage> ReceiveAsync(TimeSpan serverWaitTime)
        {
            var message = await _implementation.ReceiveAsync(serverWaitTime).ConfigureAwait(false);
            return message != null ? new ServiceBusMessage(message) : null;
        }
        bool ICachedMessagingEntity.IsClosed => _implementation.IsClosed;
        void ICachedMessagingEntity.Close() => _implementation.Close();
    }
}
