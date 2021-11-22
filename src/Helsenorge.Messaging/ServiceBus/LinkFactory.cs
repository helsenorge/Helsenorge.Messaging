/*
 * Copyright (c) 2021, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.IO;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// A Factory for creating Receiver and Sender links
    /// </summary>
    public class LinkFactory
    {
        private readonly ServiceBusConnection _connection;
        private readonly ILogger<LinkFactory> _logger;

        /// <summary>Initializes a new instance of the <see cref="LinkFactory" /> class with a <see cref="ServiceBusConnection"/> and a <see cref="ILogger{LinkFactory}"/>.</summary>
        /// <param name="connection">A <see cref="ServiceBusConnection"/> that represents the connection to ServiecBus.</param>
        /// <param name="logger">A <see cref="ILogger{LinkFactory}"/> which will be used to log errors and information.</param>
        public LinkFactory(ServiceBusConnection connection, ILogger<LinkFactory> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        /// <summary>Creates a Receiver Link of type <see cref="IMessagingReceiver"/>.</summary>
        /// <param name="queue">The path to the queue you want to receive messages from.</param>
        /// <param name="linkCredit">How many messages should the client buffer client-side.</param>
        /// <returns>A <see cref="IMessagingReceiver"/></returns>
        public IMessagingReceiver CreateReceiver(string queue, int linkCredit = 25)
            => new ServiceBusReceiver(_connection, queue, linkCredit, _logger);

        /// <summary>Creates a Sender Link of type <see cref="IMessagingSender"/>.</summary>
        /// <param name="queue">The path to the queue you want to receive messages from.</param>
        /// <returns>A <see cref="IMessagingSender"/></returns>
        public IMessagingSender CreateSender(string queue)
            => new ServiceBusSender(_connection, queue, _logger);

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
    }
}
