/*
 * Copyright (c) 2021, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Helsenorge.Messaging;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace PooledSender
{
    class Program
    {
        private static readonly string _connectionString = "amqp://guest:guest@127.0.0.1:5672/NHNTESTServiceBus";
        private static readonly string _queue = "test-queue";

        static async Task Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            var settings = new MessagingSettings
            {
                ApplicationProperties = {{ "X-SystemIdentifier", "ExampleSystemIdentifier" }},
                ServiceBus =
                {
                    ConnectionString = _connectionString,
                    MessageBrokerDialect = MessageBrokerDialect.RabbitMQ,
                }
            };

            await using var linkFactoryPool = new LinkFactoryPool(loggerFactory.CreateLogger<LinkFactoryPool>(), settings.ServiceBus, settings.ApplicationProperties);
            try
            {
                var messageCount = 20;
                var sender = await linkFactoryPool.CreateCachedMessageSender(_queue);
                for (int i = 0; i < messageCount; i++)
                {
                    var outgoingMessage = new OutgoingMessage
                    {
                        MessageId = Guid.NewGuid().ToString("N"),
                        ToHerId = 456,
                    };
                    var bodyString = $"Hello world! - {i + 1}";
                    var body = new MemoryStream(Encoding.UTF8.GetBytes(bodyString));

                    var message = await linkFactoryPool.CreateMessageAsync(123, outgoingMessage, body);

                    await sender.SendAsync(message);

                    Console.WriteLine($"Message Id: '{message.MessageId}'\nMessage Body: '{bodyString}'\nMessages sent: '{i + 1}'.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: '{e.Message}'.\nStack Trace: {e.StackTrace}");
            }
            finally
            {
                await linkFactoryPool.ReleaseCachedMessageSender(_queue);
            }
        }
    }
}
