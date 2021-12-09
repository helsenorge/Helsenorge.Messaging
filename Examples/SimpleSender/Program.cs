/*
 * Copyright (c) 2021, Norsk Helsenett SF and contributors
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
using Helsenorge.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace SimpleSender
{
    class Program
    {
        // amqps should always be used in production and usually it will be used in the test environments.
        //private static readonly string _connectionString = "amqps://[username]:[password]@127.0.0.1:5671";

        // amqp can be used in your local test environment.
        private static readonly string _connectionString = "amqp://guest:guest@127.0.0.1:5672";
        private static readonly string _queue = "/amq/queue/test-queue";

        static async Task Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            var connection = new ServiceBusConnection(_connectionString, loggerFactory.CreateLogger<ServiceBusConnection>());
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
                    await sender.Close();
                if (connection != null)
                    await connection.CloseAsync();
            }

            Console.WriteLine("Press any key to continue. . .");
            Console.ReadKey(true);
        }
    }
}
