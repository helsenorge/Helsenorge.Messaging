/*
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Bus
{
    [ExcludeFromCodeCoverage]
    internal class AmqpReceiver : CachedAmpqSessionEntity<ReceiverLink>, IMessagingReceiver
    {
        private readonly string _id;
        private readonly int _credit;
        private readonly ILogger _logger;
        private readonly string _name;

        public AmqpReceiver(BusConnection connection, string id, int credit, ILogger logger) : base(connection)
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
            var receiver = session.CreateReceiver(Name, Connection.GetEntityName(_id, LinkRole.Receiver)) as ReceiverLink;
            receiver.SetCredit(_credit);
            return receiver;
        }

        public IMessagingMessage Receive()
            => Receive(TimeSpan.FromMilliseconds(BusSettings.DefaultTimeoutInMilliseconds));

        public IMessagingMessage Receive(TimeSpan serverWaitTime)
        {
            var message = new BusOperationBuilder(_logger, "Receive").Build(() =>
            {
                EnsureOpen();
                var amqpMessage = _link.Receive(serverWaitTime);
                return amqpMessage != null ? new BusMessage(amqpMessage) : null;
            }).Perform();

            ConfigureBusOperations(message);

            return message;
        }

        public Task<IMessagingMessage> ReceiveAsync()
            => ReceiveAsync(TimeSpan.FromMilliseconds(BusSettings.DefaultTimeoutInMilliseconds));

        public async Task<IMessagingMessage> ReceiveAsync(TimeSpan serverWaitTime)
        {
            var message = await new BusOperationBuilder(_logger, "Receive").Build(async () =>
            {
                await EnsureOpenAsync().ConfigureAwait(false);
                var amqpMessage = await _link.ReceiveAsync(serverWaitTime).ConfigureAwait(false);
                return amqpMessage != null ? new BusMessage(amqpMessage) : null;
            }).PerformAsync().ConfigureAwait(false);

            ConfigureBusOperations(message);

            return message;
        }

        private void ConfigureBusOperations(BusMessage message)
        {
            if (message != null)
            {
                var amqpMessage = (Message)message.OriginalObject;

                message.CompleteAction = () => new BusOperationBuilder(_logger, "Complete")
                    .Build(() =>
                    {
                        _link.Accept(amqpMessage);
                    }).Perform();
                message.CompleteActionAsync = () => new BusOperationBuilder(_logger, "CompleteAsync")
                    .Build(() =>
                    {
                        _link.Accept(amqpMessage);
                        return Task.CompletedTask;
                    }).PerformAsync();

                message.RejectAction = () => new BusOperationBuilder(_logger, "Reject")
                    .Build(() =>
                    {
                        _link.Reject(amqpMessage);
                    }).Perform();
                message.RejectActionAsync = () => new BusOperationBuilder(_logger, "RejectAsync")
                    .Build(() =>
                    {
                        _link.Reject(amqpMessage);
                        return Task.CompletedTask;
                    }).PerformAsync();

                message.ReleaseAction = () => new BusOperationBuilder(_logger, "Release")
                    .Build(() =>
                    {
                        _link.Release(amqpMessage);
                    }).Perform();
                message.ReleaseActionAsync = () => new BusOperationBuilder(_logger, "ReleaseAsync")
                    .Build(() =>
                    {
                        _link.Release(amqpMessage);
                        return Task.CompletedTask;
                    }).PerformAsync();

                message.DeadLetterAction = () => new BusOperationBuilder(_logger, "DeadLetter")
                    .Build(() =>
                    {
                        if (Connection.MessageBrokerDialect == MessageBrokerDialect.RabbitMQ)
                            throw new NotImplementedException("The method 'DeadLetter()' is not supported by RabbitMQ. Use 'Reject()' or 'RejectAsync' instead.");

                        _link.Reject(amqpMessage);
                    }).Perform();
                message.DeadLetterActionAsync = () => new BusOperationBuilder(_logger, "DeadLetterAsync")
                    .Build(() =>
                    {
                        if (Connection.MessageBrokerDialect == MessageBrokerDialect.RabbitMQ)
                            throw new NotImplementedException("The method 'DeadLetterAsync()' is not supported by RabbitMQ. Use 'RejectAsync()' or 'Reject()'' instead.");

                        _link.Reject(amqpMessage);
                        return Task.CompletedTask;
                    }).PerformAsync();

                message.ModifyAction = (deliveryFailed, undeliverableHere) => new BusOperationBuilder(_logger, "Modify")
                    .Build(() =>
                    {
                        _link.Modify(amqpMessage, deliveryFailed, undeliverableHere);
                    }).Perform();
                message.ModifyActionAsync = (deliveryFailed, undeliverableHere) => new BusOperationBuilder(_logger, "ModifyAsync")
                    .Build(() =>
                    {
                        _link.Modify(amqpMessage, deliveryFailed, undeliverableHere);
                        return Task.CompletedTask;
                    }).PerformAsync();
            }
        }
    }
}
