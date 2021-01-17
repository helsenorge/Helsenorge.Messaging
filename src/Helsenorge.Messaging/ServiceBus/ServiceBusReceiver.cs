/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Amqp;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    [ExcludeFromCodeCoverage]
    internal class ServiceBusReceiver : CachedAmpqSessionEntity<ReceiverLink>, IMessagingReceiver
    {
        private readonly string _id;
        private readonly ILogger _logger;

        public ServiceBusReceiver(ServiceBusConnection connection, string id, ILogger logger) : base(connection)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }
            _id = id;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override ReceiverLink CreateLink(ISession session)
        {
            return session.CreateReceiver($"receiver-link-{Guid.NewGuid()}", Connection.GetEntityName(_id)) as ReceiverLink;
        }

        public async Task<IMessagingMessage> ReceiveAsync(TimeSpan serverWaitTime)
        {
            var message = await new ServiceBusOperationBuilder(_logger, "Receive").Build(async () =>
            {
                await EnsureOpen();
                var amqpMessage = await _link.ReceiveAsync(serverWaitTime).ConfigureAwait(false);
                return amqpMessage != null ? new ServiceBusMessage(amqpMessage) : null;
            }).PerformAsync().ConfigureAwait(false);

            if (message != null)
            {
                var amqpMessage = (Message)message.OriginalObject;

                message.CompleteAction = () => new ServiceBusOperationBuilder(_logger, "Complete")
                    .Build(async () =>
                    {
                        await EnsureOpen();
                        _link.Accept(amqpMessage);
                    }).PerformAsync();

                message.RejectAction = () => new ServiceBusOperationBuilder(_logger, "Reject")
                    .Build(async () =>
                    {
                        await EnsureOpen();
                        _link.Reject(amqpMessage);
                    }).PerformAsync();

                message.ReleaseAction = () => new ServiceBusOperationBuilder(_logger, "Release")
                    .Build(async () =>
                    {
                        await EnsureOpen();
                        _link.Release(amqpMessage);
                    }).PerformAsync();

                message.DeadLetterAction = () => new ServiceBusOperationBuilder(_logger, "DeadLetter")
                    .Build(async () =>
                    {
                        await EnsureOpen();
                        _link.Reject(amqpMessage);
                    }).PerformAsync();

                message.RenewLockAction = () => new ServiceBusOperationBuilder(_logger, "RenewLock")
                    .Build(async () =>
                    {
                        await EnsureOpen();
                        var lockTimeout = TimeSpan.FromMinutes(1);
                        await Connection.HttpClient.RenewLockAsync(_id, amqpMessage.GetSequenceNumber(), amqpMessage.GetLockToken(), lockTimeout, serverWaitTime).ConfigureAwait(false);
                        return DateTime.UtcNow + lockTimeout;
                    }).PerformAsync();
            }

            return message;
        }
    }
}
