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
    /// The exception that is thrown when we encounter and unknown or uncategorized error condition.
    /// </summary>
    public sealed class UncategorizedAmqpException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UncategorizedAmqpException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public UncategorizedAmqpException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UncategorizedAmqpException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public UncategorizedAmqpException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UncategorizedAmqpException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="condition">The error condition that occurred.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public UncategorizedAmqpException(string message, string condition, Exception innerException) : base($"{message} Condition: {condition}", innerException)
        {
        }

        /// <inheritdoc cref="AmqpException.CanRetry"/>
        public override bool CanRetry => false;
    }
}
