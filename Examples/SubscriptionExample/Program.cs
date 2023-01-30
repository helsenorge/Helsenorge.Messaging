/*
 * Copyright (c) 2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Helsenorge.Messaging;
using Helsenorge.Messaging.Bus;
using Helsenorge.Registries;
using Helsenorge.Registries.Configuration;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Logging;

namespace SubscriptionExample
{
    internal class Program
    {
        private static string ConnectionString = "amqps://{0}:{1}@tb.test.nhn.no:5671/RegisterEvents";
        private static string UserName = "<username>";
        private static string Password = "<password>";

        private static async Task Main(string[] args)
        {
            try
            {
                var loggerFactory = new LoggerFactory();

                var serviceManagerBusSettings = new ServiceBusManagerSettings
                {
                    WcfConfiguration = new WcfConfiguration
                    {
                        Address = "https://ws-web.test.nhn.no/v2/serviceBusManager/Basic",
                        UserName = UserName,
                        Password = Password
                    }
                };
                var serviceBusManager = new ServiceBusManager(serviceManagerBusSettings, loggerFactory.CreateLogger<ServiceBusManager>());

                var serviceBusSettings = new ServiceBusSettings
                {
                    ConnectionString = string.Format(ConnectionString, UserName, Password),
                    MessageBrokerDialect = MessageBrokerDialect.RabbitMQ
                };
                await using var linkFactoryPool = new LinkFactoryPool(loggerFactory.CreateLogger("LinkFactoryPool"), serviceBusSettings);

                // Create or fetch an already existing subscription.
                var subscription = await serviceBusManager.SubscribeAsync(SubscriptionEventSource.AddressRegister, "SubscriptionExample");

                // Create a receiver that will listen to our subscription queue.
                var receiver = await linkFactoryPool.CreateCachedMessageReceiver(subscription.QueueName);

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
