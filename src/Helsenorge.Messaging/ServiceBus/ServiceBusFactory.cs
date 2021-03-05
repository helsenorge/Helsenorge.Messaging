/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    [ExcludeFromCodeCoverage]
    internal class ServiceBusFactory : IMessagingFactory
    {
        private readonly ServiceBusConnection _connection;
        private readonly ILogger _logger;

        public ServiceBusFactory(ServiceBusConnection connection, ILogger logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IMessagingReceiver CreateMessageReceiver(string id, int credit)
        {
            return new ServiceBusReceiver(_connection, id, credit, _logger);
        }

        public IMessagingSender CreateMessageSender(string id)
        {
            return new ServiceBusSender(_connection, id, _logger);
        }

        public bool IsClosed => _connection.IsClosedOrClosing;

        public async Task Close() => await _connection.CloseAsync().ConfigureAwait(false);

        public async Task<IMessagingMessage> CreateMessage(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            stream.Close();
            return new ServiceBusMessage(new Message
            {
                BodySection = new Data { Binary = memoryStream.ToArray() }
            });
        }
    }
}
