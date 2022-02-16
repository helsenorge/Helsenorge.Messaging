/*
 * Copyright (c) 2021, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Threading.Tasks;
using Helsenorge.Messaging;
using Helsenorge.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace PooledReceiver
{
    class Program
    {
        private static readonly string _connectionString = "amqp://guest:guest@127.0.0.1:5672/NHNTESTServiceBus";
        private static readonly string _queue = "test-queue";

        static async Task Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            var settings = new ServiceBusSettings
            {
                ConnectionString = _connectionString,
                MessageBrokerDialect = MessageBrokerDialect.RabbitMQ,
            };

            await using var linkFactoryPool = new LinkFactoryPool(loggerFactory.CreateLogger<LinkFactoryPool>(), settings);
            try
            {
                var receiver = await linkFactoryPool.CreateCachedMessageReceiver(_queue);
                int i = 0;
                while (true)
                {
                    var message = await receiver.ReceiveAsync();
                    if(message == null) break;

                    Console.WriteLine($"Message Id: {message.MessageId}");

                    var stream = message.GetBody();
                    if (stream != null)
                    {
                        var outputStream = new StreamReader(stream);
                        var body = await outputStream.ReadToEndAsync();

                        Console.WriteLine($"Message Body: {body}");
                    }

                    Console.WriteLine($"Messages received: {i++}");

                    await message.CompleteAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: '{e.Message}'.\nStack Trace: {e.StackTrace}");
            }
            finally
            {
                await linkFactoryPool.ReleaseCachedMessageReceiver(_queue);
            }
        }
    }
}
