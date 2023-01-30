/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.Bus.Receivers
{
    /// <summary>
    /// Represents an error that we failed to deserialize the payload
    /// </summary>
    [Serializable]
    public class PayloadDeserializationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the PayloadDeserializationException class
        /// </summary>
        public PayloadDeserializationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the PayloadDeserializationException class
        /// with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PayloadDeserializationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PayloadDeserializationException class
        /// with a specified error message and a reference to the inner exception that 
        /// is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a null reference 
        /// if no inner exception is specified.
        /// </param>
        public PayloadDeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
