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
    /// The exception that is thrown when the message has been released by the peer.
    /// </summary>
    public class MessageReleasedException : ServiceBusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReleasedException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public MessageReleasedException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReleasedException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public MessageReleasedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <inheritdoc cref="ServiceBusException"/>
        public override bool CanRetry => false;
    }
}
