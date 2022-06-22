/* 
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the lock on the Session has expired.
    /// </summary>
    public sealed class SessionLockLostException : ServiceBusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionLockLostException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public SessionLockLostException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionLockLostException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public SessionLockLostException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <inheritdoc cref="ServiceBusException.CanRetry"/>
        public override bool CanRetry => false;
    }
}
