/*
 * Copyright (c) 2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.Amqp.Exceptions;

public class UnknownQueueTypeException : Exception
{
    public UnknownQueueTypeException(QueueType queueType)
        : base($"Unknown Queue Type: {queueType}")
    {
        QueueType = queueType;
    }

    public UnknownQueueTypeException(string message)
        : base(message)
    {
    }

    public QueueType QueueType { get; }
}
