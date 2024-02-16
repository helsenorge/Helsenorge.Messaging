/*
 * Copyright (c) 2022-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;

namespace SubscriptionExample
{
    internal static class IMessagingReceiverExtensions
    {
        // NOTE: A very hacky way of trying to connect to a queue. This can probably be done in a much better way.
        public static async Task<bool> TryConnectToQueueAsync(this IAmqpReceiver receiver)
        {
            var i = 0;
            while (true)
            {
                try
                {
                    var message = await receiver.ReceiveAsync(new TimeSpan(0, 0, 0, 10));
                    if (message != null)
                        await message.RelaseAsync();
                    // If we get here we have successfully connected to the queue.
                    return true;
                }
                catch (Exception)
                {
                    if (i == 10)
                        // Just exit the application since we cannot connect.
                        return false;
                }

                i++;
                // Sleep for 10 seconds before we try again.
                Thread.Sleep(10000);
            }
        }
    }
}
