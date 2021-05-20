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
        private readonly int _credit;
        private readonly ILogger _logger;
        private readonly string _name;

        public ServiceBusReceiver(ServiceBusConnection connection, string id, int credit, ILogger logger) : base(connection)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }
            _id = id;
            _credit = credit;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _name = $"receiver-link-{Guid.NewGuid()}";
        }

        public string Name => _name;

        protected override ReceiverLink CreateLink(ISession session)
        {
            var receiver = session.CreateReceiver(Name, Connection.GetEntityName(_id)) as ReceiverLink;
            receiver.SetCredit(_credit);
            return receiver;
        }

        public IMessagingMessage Receive(TimeSpan serverWaitTime)
        {
            var message = new ServiceBusOperationBuilder(_logger, "Receive").Build(() =>
            {
                EnsureOpen();
                var amqpMessage = _link.Receive(serverWaitTime);
                return amqpMessage != null ? new ServiceBusMessage(amqpMessage) : null;
            }).Perform();

            ConfigureServiceBusOperations(serverWaitTime, message);

            return message;
        }

        public async Task<IMessagingMessage> ReceiveAsync(TimeSpan serverWaitTime)
        {
            var message = await new ServiceBusOperationBuilder(_logger, "Receive").Build(async () =>
            {
                await EnsureOpenAsync().ConfigureAwait(false);
                var amqpMessage = await _link.ReceiveAsync(serverWaitTime).ConfigureAwait(false);
                return amqpMessage != null ? new ServiceBusMessage(amqpMessage) : null;
            }).PerformAsync().ConfigureAwait(false);

            ConfigureServiceBusOperations(serverWaitTime, message);

            return message;
        }

        private void ConfigureServiceBusOperations(TimeSpan serverWaitTime, ServiceBusMessage message)
        {
            if (message != null)
            {
                var amqpMessage = (Message)message.OriginalObject;

                message.CompleteActionAsync = () => new ServiceBusOperationBuilder(_logger, "Complete")
                    .Build(async () =>
                    {
                        await EnsureOpenAsync().ConfigureAwait(false);
                        _link.Accept(amqpMessage);
                    }).PerformAsync();

                message.RejectActionAsync = () => new ServiceBusOperationBuilder(_logger, "Reject")
                    .Build(async () =>
                    {
                        await EnsureOpenAsync().ConfigureAwait(false); ;
                        _link.Reject(amqpMessage);
                    }).PerformAsync();

                message.ReleaseActionAsync = () => new ServiceBusOperationBuilder(_logger, "Release")
                    .Build(async () =>
                    {
                        await EnsureOpenAsync().ConfigureAwait(false); ;
                        _link.Release(amqpMessage);
                    }).PerformAsync();

                message.DeadLetterActionAsync = () => new ServiceBusOperationBuilder(_logger, "DeadLetter")
                    .Build(async () =>
                    {
                        await EnsureOpenAsync().ConfigureAwait(false);
                        _link.Reject(amqpMessage);
                    }).PerformAsync();

                message.RenewLockActionAsync = () => new ServiceBusOperationBuilder(_logger, "RenewLock")
                    .Build(async () =>
                    {
                        await EnsureOpenAsync().ConfigureAwait(false);
                        var lockTimeout = TimeSpan.FromMinutes(1);
                        await Connection.HttpClient.RenewLockAsync(_id, amqpMessage.GetSequenceNumber(), amqpMessage.GetLockToken(), lockTimeout, serverWaitTime).ConfigureAwait(false);
                        return DateTime.UtcNow + lockTimeout;
                    }).PerformAsync();

                message.ModifyActionAsync = (deliveryFailed, undeliverableHere) => new ServiceBusOperationBuilder(_logger, "Modify")
                    .Build(async () =>
                    {
                        await EnsureOpenAsync().ConfigureAwait(false);
                        _link.Modify(amqpMessage, deliveryFailed, undeliverableHere);
                    }).PerformAsync();
            }
        }
    }
}
