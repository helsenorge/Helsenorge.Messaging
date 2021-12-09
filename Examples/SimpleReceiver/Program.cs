﻿/*
 * Copyright (c) 2021, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace SimpleReceiver
{
    class Program
    {
        private static readonly string _connectionString = "amqp://guest:guest@127.0.0.1:5672";
        private static readonly string _queue = "/amq/queue/kenneth-test-queue";

        static async Task Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            var connection = new ServiceBusConnection(_connectionString, loggerFactory.CreateLogger<ServiceBusConnection>());
            IMessagingReceiver receiver = null;
            try
            {
                var linkFactory = new LinkFactory(connection, loggerFactory.CreateLogger<LinkFactory>());
                receiver = linkFactory.CreateReceiver(_queue);
                int i = 0;
                while (true)
                {
                    var message = await receiver.ReceiveAsync();
                    if (message == null) break;

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

                await receiver.Close().ConfigureAwait(false);
                await connection.CloseAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: '{e.Message}'.\nStack Trace: {e.StackTrace}");
            }
            finally
            {
                if (receiver != null)
                    await receiver.Close();
                await connection.CloseAsync();
            }

            Console.WriteLine("Press any key to continue. . .");
            Console.ReadKey(true);
        }
    }    
}


