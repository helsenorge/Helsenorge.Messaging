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
/// Exception being thrown when queue name is empty / not set.
/// </summary>
public class QueueNameNotSetException : Exception
{
    /// <summary>
    /// Constructs an instance of <see cref="QueueNameNotSetException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public QueueNameNotSetException(string message)
        : base(message)
    {
    }
}
