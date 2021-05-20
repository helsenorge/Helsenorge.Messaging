/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    internal static class ServiceBusTestingConstants
    {
        public static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromSeconds(1);

        public static string GetDeadLetterQueueName(string queueName)
        {
            return $"{queueName}/$deadletterqueue";
        }
    }
}
