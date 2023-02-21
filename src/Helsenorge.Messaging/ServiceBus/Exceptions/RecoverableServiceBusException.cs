﻿/* 
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
    /// The exception that is thrown when the error reported is a general, but recoverable error.
    /// </summary>
    public sealed class RecoverableServiceBusException : ServiceBusException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecoverableServiceBusException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        public RecoverableServiceBusException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecoverableServiceBusException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for this exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public RecoverableServiceBusException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <inheritdoc cref="ServiceBusException.CanRetry"/>
        public override bool CanRetry => true;
    }
}