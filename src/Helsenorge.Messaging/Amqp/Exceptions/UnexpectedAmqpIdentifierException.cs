/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.Amqp.Exceptions
{
    /// <summary>
    /// The exception that is thrown when we receive an unexpected identifier type on the AMQP frame
    /// </summary>
    /// <remarks>
    /// The identifier type may be valid within the AMQP specification, but Helsenorge.Messaging does not support the identifier type.
    /// Valid types are <see cref="System.String"/> and <see cref="System.Guid"/>
    /// </remarks>
    public class UnexpectedMessageIdentifierTypeException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedMessageIdentifierTypeException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public UnexpectedMessageIdentifierTypeException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedMessageIdentifierTypeException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public UnexpectedMessageIdentifierTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <inheritdoc cref="AmqpException.CanRetry"/>
        public override bool CanRetry => false;
    }
}
