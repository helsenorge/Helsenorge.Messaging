/* 
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
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
        private readonly ILogger _logger;
        private readonly ServiceBusConnection _connection;
        private readonly IDictionary<string, object> _applicationProperties;

        public ServiceBusFactory(ILogger logger, ServiceBusConnection connection, IDictionary<string, object> applicationProperties)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _applicationProperties = applicationProperties;
        }

        public IMessagingReceiver CreateMessageReceiver(string id, int credit)
        {
            return new ServiceBusReceiver(_connection, id, credit, _logger);
        }

        public IMessagingSender CreateMessageSender(string id)
        {
            return new ServiceBusSender(_logger, _connection, id, _applicationProperties);
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
