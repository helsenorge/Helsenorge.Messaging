/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.Amqp.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the Quota (Entity Max Size or other Connection etc) allocated to the Entity has exceeded. Callers should check the
    /// error message to see which of the Quota exceeded and take appropriate action.
    /// </summary>
    public sealed class QuotaExceededException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuotaExceededException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public QuotaExceededException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuotaExceededException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public QuotaExceededException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <inheritdoc cref="AmqpException.CanRetry"/>
        public override bool CanRetry => false;
    }
}
