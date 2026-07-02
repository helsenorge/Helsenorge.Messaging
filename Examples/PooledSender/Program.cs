/*
 * Copyright (c) 2021-2024, Norsk Helsenett SF and contributors
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
using Helsenorge.Messaging.Amqp;
using Microsoft.Extensions.Logging;

namespace PooledSender
{
    class Program
    {
        private static string HostName = "tb.test.nhn.no";
        private static string Exchange = "NHNTESTServiceBus";
        private static string Username = "guest";
        private static string Password = "guest";
        // More information about routing and addressing on RabbitMQ:
        // https://github.com/rabbitmq/rabbitmq-server/tree/main/deps/rabbitmq_amqp1_0#routing-and-addressing
        // NOTE: The address below uses the deprecated v1 address format. RabbitMQ 4.0 and later also
        // supports the new v2 address format, and v1 will be removed in a future RabbitMQ release:
        // https://www.rabbitmq.com/docs/amqp#target-address-v2
        // To use the v2 address format (requires RabbitMQ 4.0 or later), enable it on the settings
        // (settings.AmqpSettings.UseAmqpAdressV2 = true) and pass the plain queue name instead:
        // private static readonly string Queue = "12345_async";
        private static readonly string Queue = "/exchange/NHNTESTServiceBus/12345_async";

        static async Task Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            var connectionString = new AmqpConnectionString
            {
                HostName = HostName,
                Exchange = Exchange,
                UserName = Username,
                Password = Password,
            };
            var settings = new MessagingSettings
            {
                ApplicationProperties = {{ "X-SystemIdentifier", "ExampleSystemIdentifier" }},
                AmqpSettings =
                {
                    ConnectionString = connectionString.ToString(),
                }
            };

            await using var linkFactoryPool = new LinkFactoryPool(loggerFactory.CreateLogger<LinkFactoryPool>(), settings.AmqpSettings, settings.ApplicationProperties);
            try
            {
                var messageCount = 20;
                var sender = await linkFactoryPool.CreateCachedMessageSenderAsync(Queue);
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
                await linkFactoryPool.ReleaseCachedMessageSenderAsync(Queue);
            }
        }
    }
}
