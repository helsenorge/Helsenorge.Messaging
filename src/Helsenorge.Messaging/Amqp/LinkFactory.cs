/*
 * Copyright (c) 2021-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Amqp
{
    /// <summary>
    /// A Factory for creating Receiver and Sender links
    /// </summary>
    public class LinkFactory
    {
        private readonly AmqpConnection _connection;
        private readonly ILogger<LinkFactory> _logger;
        private readonly IDictionary<string, object> _applicationProperties;

        /// <summary>Initializes a new instance of the <see cref="LinkFactory" /> class with a <see cref="AmqpConnection"/> and a <see cref="ILogger{LinkFactory}"/>.</summary>
        /// <param name="connection">A <see cref="AmqpConnection"/> that represents the connection to ServiecBus.</param>
        /// <param name="logger">A <see cref="ILogger{LinkFactory}"/> which will be used to log errors and information.</param>
        /// <param name="applicationProperties">A Dictionary with additional application properties which will be added to <see cref="Amqp.Message"/>.</param>
        public LinkFactory(AmqpConnection connection, ILogger<LinkFactory> logger, IDictionary<string, object> applicationProperties = null)
        {
            _connection = connection;
            _logger = logger;
            _applicationProperties = applicationProperties ?? new Dictionary<string, object>();
        }

        /// <summary>Creates a Receiver Link of type <see cref="IAmqpReceiver"/>.</summary>
        /// <param name="queue">The path to the queue you want to receive messages from.</param>
        /// <param name="linkCredit">How many messages should the client buffer client-side.</param>
        /// <returns>A <see cref="IAmqpReceiver"/></returns>
        public IAmqpReceiver CreateReceiver(string queue, int linkCredit = 25)
            => new AmqpReceiver(_connection, queue, linkCredit, _logger);

        /// <summary>Creates a Sender Link of type <see cref="IMessagingSender"/>.</summary>
        /// <param name="queue">The path to the queue you want to receive messages from.</param>
        /// <returns>A <see cref="IMessagingSender"/></returns>
        public IMessagingSender CreateSender(string queue)
            => new AmqpSender(_logger, _connection, queue, _applicationProperties);

        /// <summary>Creates a <see cref="IAmqpMessage"/>.</summary>
        /// <param name="fromHerId">The HER-id which is the receipient of the message</param>
        /// <param name="message">The outgoing message as an <see cref="OutgoingMessage"/>.</param>
        /// <param name="payload">The payload as a <see cref="Stream"/> object.</param>
        /// <returns>Returns a <see cref="IAmqpMessage"/>.</returns>
        public async Task<IAmqpMessage> CreateMessageAsync(int fromHerId, OutgoingMessage message, Stream payload)
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

            return new AmqpMessage(innerMessage)
            {
                MessageId = message.MessageId,
                MessageFunction = string.IsNullOrWhiteSpace(message.ReceiptForMessageFunction)
                    ? message.MessageFunction
                    : message.ReceiptForMessageFunction,
                ToHerId = message.ToHerId,
                FromHerId = fromHerId,
            };
        }
    }
}
