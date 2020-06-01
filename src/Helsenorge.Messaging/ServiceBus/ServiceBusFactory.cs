using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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

        public IMessagingReceiver CreateMessageReceiver(string id)
        {
            return new ServiceBusReceiver(_connection, id, _logger);
        }

        public IMessagingSender CreateMessageSender(string id)
        {
            return new ServiceBusSender(_connection, id, _logger);
        }

        public bool IsClosed => _connection.IsClosedOrClosing;

        public void Close() => _connection.CloseAsync().Wait();

        public IMessagingMessage CreateMessage(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                stream.Close();
                return new ServiceBusMessage(new Message
                {
                    BodySection = new Data { Binary = memoryStream.ToArray() }
                });
            }
        }
    }
}
