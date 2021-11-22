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
    public class LinkFactory
    {
        private readonly ServiceBusConnection _connection;
        private readonly ILogger<LinkFactory> _logger;

        public LinkFactory(ServiceBusConnection connection, ILogger<LinkFactory> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public IMessagingReceiver CreateReceiver(string queue, int linkCredit = 25)
            => new ServiceBusReceiver(_connection, queue, linkCredit, _logger);

        public IMessagingSender CreateSender(string queue)
            => new ServiceBusSender(_connection, queue, _logger);

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
