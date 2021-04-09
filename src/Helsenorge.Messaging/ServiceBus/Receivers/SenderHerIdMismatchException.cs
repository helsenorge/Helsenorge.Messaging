/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Runtime.Serialization;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    /// <summary>
    /// Represents an error that we failed to deserialize the payload
    /// </summary>
    [Serializable]
    public class SenderHerIdMismatchException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the SenderHerIdMismatchException class
        /// </summary>
        public SenderHerIdMismatchException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SenderHerIdMismatchException class
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SenderHerIdMismatchException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SenderHerIdMismatchException class
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a null reference 
        /// if no inner exception is specified.
        /// </param>
        public SenderHerIdMismatchException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}