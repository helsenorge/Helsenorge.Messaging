/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the Messaging Entity is not found.  Verify Entity Exists.
    /// </summary>
    internal sealed class MessagingEntityNotFoundException : ServiceBusException
    {
        public MessagingEntityNotFoundException(string message)
            : this(message, null)
        {
        }

        public MessagingEntityNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry { get; }
    }
}
