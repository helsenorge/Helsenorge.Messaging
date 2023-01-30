/* 
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.Bus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the lock on the message is lost. Callers should call Receive and process the message again.
    /// </summary>
    public sealed class MessageLockLostException : BusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageLockLostException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public MessageLockLostException(string message)
            : base(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageLockLostException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public MessageLockLostException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <inheritdoc cref="BusException.CanRetry"/>
        public override bool CanRetry => false;
    }
}
