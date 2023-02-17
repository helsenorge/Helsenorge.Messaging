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
    /// An abstract exception class.
    /// </summary>
    public abstract class AmqpException : Exception
    {
        /// <summary>
        /// Returns true if the exception represents an error on an operation that can be retried, otherwise false.
        /// </summary>
        public abstract bool CanRetry { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        protected AmqpException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        protected AmqpException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
