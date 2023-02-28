/*
 * Copyright (c) 2022-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Helsenorge.Messaging;
using Helsenorge.Messaging.Amqp;
using Helsenorge.Registries;
using Helsenorge.Registries.Configuration;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Logging;

namespace SubscriptionExample
{
    internal class Program
    {
        private static string HostName = "tb.test.nhn.no";
        private static string Exchange = "RegisterEvents";
        private static string Username = "guest";
        private static string Password = "guest";

        private static async Task Main(string[] args)
        {
            try
            {
                var loggerFactory = new LoggerFactory();

                var busManagerSettings = new BusManagerSettings
                {
                    WcfConfiguration = new WcfConfiguration
                    {
                        Address = "https://ws-web.test.nhn.no/v2/serviceBusManager/Basic",
                        UserName = Username,
                        Password = Password
                    }
                };
                var busManager = new BusManager(busManagerSettings, loggerFactory.CreateLogger<BusManager>());

                var connectionString = new AmqpConnectionString
                {
                    HostName = HostName,
                    Exchange = Exchange,
                    UserName = Username,
                    Password = Password,
                };
                var serviceBusSettings = new AmqpSettings
                {
                    ConnectionString = connectionString.ToString(),
                };
                await using var linkFactoryPool = new LinkFactoryPool(loggerFactory.CreateLogger("LinkFactoryPool"), serviceBusSettings);

                // Create or fetch an already existing subscription.
                var subscription = await busManager.SubscribeAsync(SubscriptionEventSource.AddressRegister, "SubscriptionExample");

                // Create a receiver that will listen to our subscription queue.
                var receiver = await linkFactoryPool.CreateCachedMessageReceiverAsync(subscription.QueueName);

                Console.WriteLine($"Trying to connect to queue: '{subscription.QueueName}'...");
                if (!await receiver.TryConnectToQueueAsync())
                {
                    Console.WriteLine($"Failed to connect to the queue {subscription.QueueName}. Please try again, this is probably because queue creation took longer than expected.");
                    return;
                }

                Console.WriteLine($"Successfully connected to queue: '{subscription.QueueName}'...");

                while (true)
                {
                    var message = await receiver.ReceiveAsync(TimeSpan.FromSeconds(10));
                    if (message == null)
                        continue;

                    Console.WriteLine($"MessageId: {message.MessageId}");
                    Console.WriteLine("Add code to do something with your message.");
                    Console.WriteLine("-----------------------");

                    await message.CompleteAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
