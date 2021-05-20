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
    /// The exception that is thrown when the lock on the message is lost.  Callers should call Receive and process the message again.
    /// </summary>
    internal sealed class MessageLockLostException : ServiceBusException
    {
        public MessageLockLostException(string message)
            : base(message, null)
        {
        }

        public MessageLockLostException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
