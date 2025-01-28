/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Runtime.Serialization;

namespace Helsenorge.Messaging.Amqp.Receivers
{
    /// <summary>
    /// An exception that can be used by the client program to notify that this an unsupported message
    /// was received and could not be handled. The library will then report back to the sender with the
    /// error code 'transport:unsupported-message' + the exception message.
    /// </summary>
    [Serializable]
    public class UnsupportedMessageException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="UnsupportedMessageException" /> class.</summary>
        public UnsupportedMessageException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedMessageException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnsupportedMessageException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedMessageException" /> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.
        /// </param>
        public UnsupportedMessageException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
