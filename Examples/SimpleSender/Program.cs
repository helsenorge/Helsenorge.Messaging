/*
 * Copyright (c) 2021-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Bus;
using Microsoft.Extensions.Logging;

namespace SimpleSender
{
    class Program
    {
        private static readonly string _connectionString = "amqps://guest:guest@tb.test.nhn.no:5671";
        // More information about routing and addressing on RabbitMQ:
        // https://github.com/rabbitmq/rabbitmq-server/tree/main/deps/rabbitmq_amqp1_0#routing-and-addressing
        private static readonly string _queue = "/exchange/NHNTESTServiceBus/12345_async";

        static async Task Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            var connection = new BusConnection(_connectionString);
            IMessagingSender sender = null;
            var messageCount = 20;
            try
            {
                var linkFactory = new LinkFactory(connection, loggerFactory.CreateLogger<LinkFactory>());
                sender = linkFactory.CreateSender(_queue);
                for (int i = 0; i < messageCount; i++)
                {
                    var outgoingMessage = new OutgoingMessage
                    {
                        MessageId = Guid.NewGuid().ToString("N"),
                        ToHerId = 456
                    };
                    var bodyString = $"Hello world! - {i + 1}";
                    var body = new MemoryStream(Encoding.UTF8.GetBytes(bodyString));

                    var message = await linkFactory.CreateMessageAsync(123, outgoingMessage, body);

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
                if(sender != null)
                    await sender.CloseAsync();
                if (connection != null)
                    await connection.CloseAsync();
            }

            Console.WriteLine("Press any key to continue. . .");
            Console.ReadKey(true);
        }
    }
}
