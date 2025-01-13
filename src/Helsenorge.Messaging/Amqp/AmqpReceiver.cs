/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
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

namespace Helsenorge.Messaging.Amqp
{
    [ExcludeFromCodeCoverage]
    internal class AmqpReceiver : CachedAmpqSessionEntity<ReceiverLink>, IAmqpReceiver
    {
        private readonly string _id;
        private readonly int _credit;
        private readonly ILogger _logger;
        private readonly string _name;

        public AmqpReceiver(AmqpConnection connection, string id, int credit, ILogger logger) : base(connection)
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

        public IAmqpMessage Receive()
            => Receive(TimeSpan.FromMilliseconds(AmqpSettings.DefaultTimeoutInMilliseconds));

        public IAmqpMessage Receive(TimeSpan serverWaitTime)
        {
            var message = new AmqpOperationBuilder(_logger, "Receive").Build(() =>
            {
                EnsureOpen();
                var amqpMessage = _link.Receive(serverWaitTime);
                return amqpMessage != null ? new AmqpMessage(amqpMessage) : null;
            }).Perform();

            ConfigureBusOperations(message);

            return message;
        }

        public Task<IAmqpMessage> ReceiveAsync()
            => ReceiveAsync(TimeSpan.FromMilliseconds(AmqpSettings.DefaultTimeoutInMilliseconds));

        public async Task<IAmqpMessage> ReceiveAsync(TimeSpan serverWaitTime)
        {
            var message = await new AmqpOperationBuilder(_logger, "Receive").Build(async () =>
            {
                await EnsureOpenAsync().ConfigureAwait(false);
                var amqpMessage = await _link.ReceiveAsync(serverWaitTime).ConfigureAwait(false);
                return amqpMessage != null ? new AmqpMessage(amqpMessage) : null;
            }).PerformAsync().ConfigureAwait(false);

            ConfigureBusOperations(message);

            return message;
        }

        private void ConfigureBusOperations(AmqpMessage message)
        {
            if (message != null)
            {
                var amqpMessage = (Message)message.OriginalObject;

                message.CompleteAction = () => new AmqpOperationBuilder(_logger, "Complete")
                    .Build(() =>
                    {
                        _link.Accept(amqpMessage);
                    }).Perform();
                message.CompleteActionAsync = () => new AmqpOperationBuilder(_logger, "CompleteAsync")
                    .Build(() =>
                    {
                        _link.Accept(amqpMessage);
                        return Task.CompletedTask;
                    }).PerformAsync();

                message.RejectAction = () => new AmqpOperationBuilder(_logger, "Reject")
                    .Build(() =>
                    {
                        _link.Reject(amqpMessage);
                    }).Perform();
                message.RejectActionAsync = () => new AmqpOperationBuilder(_logger, "RejectAsync")
                    .Build(() =>
                    {
                        _link.Reject(amqpMessage);
                        return Task.CompletedTask;
                    }).PerformAsync();

                message.ReleaseAction = () => new AmqpOperationBuilder(_logger, "Release")
                    .Build(() =>
                    {
                        _link.Release(amqpMessage);
                    }).Perform();
                message.ReleaseActionAsync = () => new AmqpOperationBuilder(_logger, "ReleaseAsync")
                    .Build(() =>
                    {
                        _link.Release(amqpMessage);
                        return Task.CompletedTask;
                    }).PerformAsync();

                message.DeadLetterAction = () => new AmqpOperationBuilder(_logger, "DeadLetter")
                    .Build(() =>
                    {
                        if (Connection.MessageBrokerDialect == MessageBrokerDialect.RabbitMQ)
                            throw new NotImplementedException("The method 'DeadLetter()' is not supported by RabbitMQ. Use 'Reject()' or 'RejectAsync' instead.");

                        _link.Reject(amqpMessage);
                    }).Perform();
                message.DeadLetterActionAsync = () => new AmqpOperationBuilder(_logger, "DeadLetterAsync")
                    .Build(() =>
                    {
                        if (Connection.MessageBrokerDialect == MessageBrokerDialect.RabbitMQ)
                            throw new NotImplementedException("The method 'DeadLetterAsync()' is not supported by RabbitMQ. Use 'RejectAsync()' or 'Reject()'' instead.");

                        _link.Reject(amqpMessage);
                        return Task.CompletedTask;
                    }).PerformAsync();

                message.ModifyAction = (deliveryFailed, undeliverableHere) => new AmqpOperationBuilder(_logger, "Modify")
                    .Build(() =>
                    {
                        _link.Modify(amqpMessage, deliveryFailed, undeliverableHere);
                    }).Perform();
                message.ModifyActionAsync = (deliveryFailed, undeliverableHere) => new AmqpOperationBuilder(_logger, "ModifyAsync")
                    .Build(() =>
                    {
                        _link.Modify(amqpMessage, deliveryFailed, undeliverableHere);
                        return Task.CompletedTask;
                    }).PerformAsync();
            }
        }
    }
}
