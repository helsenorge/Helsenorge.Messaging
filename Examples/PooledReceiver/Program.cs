/*
 * Copyright (c) 2021-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Threading.Tasks;
using Helsenorge.Messaging;
using Helsenorge.Messaging.Amqp;
using Microsoft.Extensions.Logging;

namespace PooledReceiver
{
    class Program
    {
        private static string HostName = "tb.test.nhn.no";
        private static string Exchange = "NHNTestServiceBus";
        private static string Username = "guest";
        private static string Password = "guest";

        // More information about routing and addressing on RabbitMQ:
        // https://github.com/rabbitmq/rabbitmq-server/tree/main/deps/rabbitmq_amqp1_0#routing-and-addressing
        private static readonly string Queue = "/amq/queue/12345_async";

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
            var settings = new AmqpSettings
            {
                ConnectionString = connectionString.ToString(),
            };

            await using var linkFactoryPool = new LinkFactoryPool(loggerFactory.CreateLogger<LinkFactoryPool>(), settings);
            try
            {
                var receiver = await linkFactoryPool.CreateCachedMessageReceiverAsync(Queue);
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
                await linkFactoryPool.ReleaseCachedMessageReceiverAsync(Queue);
            }
        }
    }
}
