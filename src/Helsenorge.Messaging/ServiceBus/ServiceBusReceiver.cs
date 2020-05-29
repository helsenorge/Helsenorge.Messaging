using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Amqp;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus.Exceptions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    [ExcludeFromCodeCoverage]
    internal class ServiceBusReceiver : CachedAmpqSessionEntity, IMessagingReceiver
    {
        private readonly string _id;
        private readonly ILogger _logger;
        private ReceiverLink _receiver;

        public ServiceBusReceiver(ServiceBusConnection connection, string id, ILogger logger) : base(connection)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }
            _id = id;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override void OnSessionCreated(Session session, string ns)
        {
            _receiver = new ReceiverLink(session, $"receiver-link-{Guid.NewGuid()}", ServiceBusConnection.GetEntityName(_id, ns));
        }

        protected override void OnSessionClosing()
        {
            _receiver.CloseAsync().Wait();
        }

        public async Task<IMessagingMessage> ReceiveAsync(TimeSpan serverWaitTime)
        {
            var message = await new ServiceBusOperationBuilder(_logger, "Receive").Build(async () =>
            {
                EnsureOpen();
                var amqpMessage = await _receiver.ReceiveAsync(serverWaitTime);
                return amqpMessage != null ? new ServiceBusMessage(amqpMessage) : null;
            }).PerformAsync();

            if (message != null)
            {
                var amqpMessage = (Message)message.OriginalObject;

                message.CompleteAction = () => new ServiceBusOperationBuilder(_logger, "Complete")
                    .Build(() =>
                {
                    EnsureOpen();
                    _receiver.Accept(amqpMessage);
                    return Task.CompletedTask;
                }).PerformAsync();

                message.DeadLetterAction = () => new ServiceBusOperationBuilder(_logger, "DeadLetter")
                    .Build(() =>
                {
                    EnsureOpen();
                    _receiver.Reject(amqpMessage);
                    return Task.CompletedTask;
                }).PerformAsync();

                message.RenewLockAction = () => new ServiceBusOperationBuilder(_logger, "RenewLock")
                    .Build(async () =>
                {
                    EnsureOpen();
                    var lockTimeout = TimeSpan.FromMinutes(1);
                    await Connection.HttpClient.RenewLockAsync(_id, amqpMessage.GetSequenceNumber(), amqpMessage.GetLockToken(), lockTimeout, serverWaitTime);
                    return DateTime.UtcNow + lockTimeout;
                }).PerformAsync();
            }

            return message;
        }
    }
}
