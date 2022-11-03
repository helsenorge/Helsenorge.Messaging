/*
 * Copyright (c) 2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Text;
using Helsenorge.Messaging.ServiceBus;

namespace Helsenorge.Messaging.AdminLib;

internal static class QueueUtilities
{
    internal static string ConstructQueueName(int herId, QueueType queueType)
    {
        return queueType switch
        {
            QueueType.Asynchronous => $"{herId}_async",
            QueueType.Synchronous => $"{herId}_sync",
            QueueType.Error => $"{herId}_error",
            QueueType.DeadLetter => $"{herId}_dl",
            QueueType.SynchronousReply => $"{herId}_syncreply",
            _ => throw new ArgumentOutOfRangeException(nameof(queueType), queueType, $"The queue type: '{queueType}' is not supported."),
        };
    }

    internal static string GetByteHeaderAsString(IDictionary<string, object> headers, string name)
    {
        if (!headers.ContainsKey(name))
            throw new MissingHeaderException(name);

        var bytes = headers[name] as byte[];
        if (bytes == null)
            throw new HeaderNotOfExpectedTypeException(name, typeof(byte[]));

        return Encoding.UTF8.GetString(bytes);
    }
}
