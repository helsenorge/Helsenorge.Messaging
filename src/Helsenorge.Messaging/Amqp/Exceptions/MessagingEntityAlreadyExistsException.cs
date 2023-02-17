/* 
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.Amqp.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an already existing entity is being re-created.
    /// </summary>
    public class MessagingEntityAlreadyExistsException : BusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingEntityAlreadyExistsException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public MessagingEntityAlreadyExistsException(string message) : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingEntityAlreadyExistsException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public MessagingEntityAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <inheritdoc cref="BusException.CanRetry"/>
        public override bool CanRetry => false;
    }
}
