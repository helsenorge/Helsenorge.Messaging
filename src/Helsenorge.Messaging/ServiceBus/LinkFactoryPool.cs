﻿/*
 * Copyright (c) 2021-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// A factory for creating pooled Receiver and Sender links.
    /// </summary>
    public class LinkFactoryPool : IAsyncDisposable
    {
        private readonly ILogger _logger;
        private readonly ServiceBusSettings _settings;
        private readonly IDictionary<string, object> _applicationProperties;
        private readonly ServiceBusFactoryPool _factoryPool;
        private readonly ServiceBusReceiverPool _receiverPool;
        private readonly ServiceBusSenderPool _senderPool;

        /// <summary>Initializes a new instance of the <see cref="LinkFactoryPool" /> class with a <see cref="ServiceBusSettings"/> and a <see cref="ILogger"/>.</summary>
        /// <param name="settings">A <see cref="ServiceBusSettings"/> instance that contains the settings.</param>
        /// <param name="logger">An <see cref="ILogger"/> instance which will be used to log errors and information.</param>
        /// <param name="applicationProperties">A Dictionary with additional application properties which will be added to <see cref="Amqp.Message"/>.</param>
        public LinkFactoryPool(ILogger logger, ServiceBusSettings settings, IDictionary<string, object> applicationProperties = null)
        {
            _logger = logger;
            _settings = settings;
            _applicationProperties = applicationProperties;

            _factoryPool = new ServiceBusFactoryPool(_settings, _applicationProperties);
            _receiverPool = new ServiceBusReceiverPool(_settings, _factoryPool);
            _senderPool = new ServiceBusSenderPool(_settings, _factoryPool);
        }

        /// <summary>Creates a pooled receiver link of type <see cref="IMessagingReceiver"/>.</summary>
        /// <param name="queue">The path to the queue you want to receive messages from.</param>
        /// <returns>A <see cref="IMessagingReceiver"/></returns>
        public Task<IMessagingReceiver> CreateCachedMessageReceiver(string queue)
            => _receiverPool.CreateCachedMessageReceiver(_logger, queue);

        /// <summary>Release a pooled receiver link tied to the 'queue'.</summary>
        /// <param name="queue">The queue address for the link we want to release</param>
        public Task ReleaseCachedMessageReceiver(string queue)
            => _receiverPool.ReleaseCachedMessageReceiver(_logger, queue);

        /// <summary>Creates a pooled sender link of type <see cref="IMessagingSender"/>.</summary>
        /// <param name="queue">The path to the queue you want to send messages to.</param>
        /// <returns>A <see cref="IMessagingSender"/></returns>
        public Task<IMessagingSender> CreateCachedMessageSender(string queue)
            => _senderPool.CreateCachedMessageSender(_logger, queue);

        /// <summary>Release a pooled sender link tied to the 'queue'.</summary>
        /// <param name="queue">The queue address for the link we want to release</param>
        public Task ReleaseCachedMessageSender(string queue)
            => _senderPool.ReleaseCachedMessageSender(_logger, queue);

        /// <summary>Creates a <see cref="IMessagingMessage"/>.</summary>
        /// <param name="fromHerId">The HER-id which is the receipient of the message</param>
        /// <param name="message">The outgoing message as an <see cref="OutgoingMessage"/>.</param>
        /// <param name="payload">The payload as a <see cref="Stream"/> object.</param>
        /// <returns>Returns a <see cref="IMessagingMessage"/>.</returns>
        public async Task<IMessagingMessage> CreateMessageAsync(int fromHerId, OutgoingMessage message, Stream payload)
        {
            using var payloadMemoryStream = new MemoryStream();
            await payload.CopyToAsync(payloadMemoryStream);

            var innerMessage = new Message
            {
                BodySection = new Data
                {
                    Binary = payloadMemoryStream.ToArray(),
                }
            };

            return new ServiceBusMessage(innerMessage)
            {
                MessageId = message.MessageId,
                MessageFunction = string.IsNullOrWhiteSpace(message.ReceiptForMessageFunction)
                    ? message.MessageFunction
                    : message.ReceiptForMessageFunction,
                ToHerId = message.ToHerId,
                FromHerId = fromHerId,
                ScheduledEnqueueTimeUtc = message.ScheduledSendTimeUtc,

            };
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await _receiverPool.Shutdown(_logger).ConfigureAwait(false);
            await _senderPool.Shutdown(_logger).ConfigureAwait(false);
            await _factoryPool.Shutdown(_logger).ConfigureAwait(false);
        }
    }
}
