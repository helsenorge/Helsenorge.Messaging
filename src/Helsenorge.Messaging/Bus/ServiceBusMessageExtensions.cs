﻿/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Amqp;
using System;

namespace Helsenorge.Messaging.Bus
{
    internal static class ServiceBusMessageExtensions
    {
        public static long GetSequenceNumber(this Message message)
        {
            return message.MessageAnnotations?.Map.ContainsKey(ServiceBusMessage.SequenceNumberSymbol) == true
                ? (long)message.MessageAnnotations[ServiceBusMessage.SequenceNumberSymbol]
                : 0;
        }

        public static Guid GetLockToken(this Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.DeliveryTag.Length != GuidSize)
            {
                return Guid.Empty;
            }

            var guidBuffer = new byte[GuidSize];
            Buffer.BlockCopy(message.DeliveryTag, 0, guidBuffer, 0, GuidSize);
            return new Guid(guidBuffer);
        }

        private const int GuidSize = 16;

        public static string GetPartitionKey(this Message message)
        {
            return message.MessageAnnotations?.Map.ContainsKey(ServiceBusMessage.PartitionKeySymbol) == true
                ? (string)message.MessageAnnotations[ServiceBusMessage.PartitionKeySymbol]
                : null;
        }
    }
}
