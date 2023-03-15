﻿/* 
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
    /// The exception that is thrown when AMQP data could not be decoded.
    /// </summary>
    public class DecodeErrorException : AmqpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecodeErrorException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public DecodeErrorException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DecodeErrorException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public DecodeErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <inheritdoc cref="AmqpException.CanRetry"/>
        public override bool CanRetry => false;
    }
}