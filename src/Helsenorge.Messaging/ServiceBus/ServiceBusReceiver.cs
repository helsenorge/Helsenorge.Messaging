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
        private AzureCompatibleMessageReceiver _messageReceiver;

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
            _messageReceiver = new AzureCompatibleMessageReceiver(session, ServiceBusConnection.GetEntityName(_id, ns));
        }

        protected override void OnSessionClosing()
        {
            _messageReceiver.CloseAsync().Wait();
        }

        public async Task<IMessagingMessage> ReceiveAsync(TimeSpan serverWaitTime)
        {
            var message = await new ServiceBusOperationBuilder(_logger, "Receive").Build(async () =>
            {
                EnsureOpen();
                return await _messageReceiver.ReceiveAsync(serverWaitTime);
            }).PerformAsync();

            if (message != null)
            {
                var amqpMessage = (Message)message.OriginalObject;

                message.CompleteAction = () => new ServiceBusOperationBuilder(_logger, "Complete")
                    .Build(async () =>
                {
                    EnsureOpen();
                    await _messageReceiver.CompleteAsync(amqpMessage);
                }).PerformAsync();

                message.DeadLetterAction = () => new ServiceBusOperationBuilder(_logger, "DeadLetter")
                    .Build(async () =>
                {
                    EnsureOpen();
                    await _messageReceiver.DeadLetterAsync(amqpMessage);
                }).PerformAsync();

                message.RenewLockAction = (lockTokenGuid, partitionName) => new ServiceBusOperationBuilder(_logger, "RenewLock")
                    .Build(async () =>
                {
                    EnsureOpen();
                    return await _messageReceiver.RenewLockAsync(lockTokenGuid, partitionName, serverWaitTime);
                }).PerformAsync();
            }

            return message;
        }
    }
}
