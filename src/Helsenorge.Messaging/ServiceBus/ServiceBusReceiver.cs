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
        private ReceiverLink _link;

        public ServiceBusReceiver(ServiceBusConnection connection, string id, ILogger logger) : base(connection)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }
            _id = id;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected void EnsureLinkOpen()
        {
            EnsureOpen();
            if (_link == null || _link.IsClosed)
            {
                if (_session != null)
                {
                    _session.Close(TimeSpan.Zero);
                }
                _session = new Session(Connection.Connection);
                OnSessionCreated(_session, Connection.Namespace);
            }
        }

        protected override void OnSessionCreated(Session session, string ns)
        {
            _link = new ReceiverLink(session, $"receiver-link-{Guid.NewGuid()}", ServiceBusConnection.GetEntityName(_id, ns));
        }

        protected override void OnSessionClosing()
        {
            _link.CloseAsync().Wait();
        }

        public async Task<IMessagingMessage> ReceiveAsync(TimeSpan serverWaitTime)
        {
            var message = await new ServiceBusOperationBuilder(_logger, "Receive").Build(async () =>
            {
                EnsureLinkOpen();
                var amqpMessage = await _link.ReceiveAsync(serverWaitTime).ConfigureAwait(false);
                return amqpMessage != null ? new ServiceBusMessage(amqpMessage) : null;
            }).PerformAsync().ConfigureAwait(false);

            if (message != null)
            {
                var amqpMessage = (Message)message.OriginalObject;

                message.CompleteAction = () => new ServiceBusOperationBuilder(_logger, "Complete")
                    .Build(() =>
                {
                    EnsureLinkOpen();
                    _link.Accept(amqpMessage);
                    return Task.CompletedTask;
                }).PerformAsync();

                message.DeadLetterAction = () => new ServiceBusOperationBuilder(_logger, "DeadLetter")
                    .Build(() =>
                {
                    EnsureLinkOpen();
                    _link.Reject(amqpMessage);
                    return Task.CompletedTask;
                }).PerformAsync();

                message.RenewLockAction = () => new ServiceBusOperationBuilder(_logger, "RenewLock")
                    .Build(async () =>
                {
                    EnsureLinkOpen();
                    var lockTimeout = TimeSpan.FromMinutes(1);
                    await Connection.HttpClient.RenewLockAsync(_id, amqpMessage.GetSequenceNumber(), amqpMessage.GetLockToken(), lockTimeout, serverWaitTime).ConfigureAwait(false);
                    return DateTime.UtcNow + lockTimeout;
                }).PerformAsync();
            }

            return message;
        }
    }
}
