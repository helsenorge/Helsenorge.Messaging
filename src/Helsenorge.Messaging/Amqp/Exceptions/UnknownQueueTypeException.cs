/*
 * Copyright (c) 2023-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.Amqp.Exceptions;

/// <summary>
/// Exception being thrown when the queue type is unknown.
/// </summary>
public class UnknownQueueTypeException : Exception
{
    /// <summary>
    /// Constructs an instance of <see cref="UnknownQueueTypeException"/>.
    /// </summary>
    /// <param name="queueType">The queue type that is unknown.</param>
    public UnknownQueueTypeException(QueueType queueType)
        : base($"Unknown Queue Type: {queueType}")
    {
        QueueType = queueType;
    }

    /// <summary>
    /// Constructs an instance of <see cref="UnknownQueueTypeException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public UnknownQueueTypeException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// The queue type that was reported to be unknown.
    /// </summary>
    public QueueType QueueType { get; }
}
