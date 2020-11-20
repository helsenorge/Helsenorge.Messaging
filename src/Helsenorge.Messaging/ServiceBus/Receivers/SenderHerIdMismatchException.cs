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
    [Serializable]
    public class SenderHerIdMismatchException : Exception
    {
        public SenderHerIdMismatchException()
        {
        }

        public SenderHerIdMismatchException(string message) : base(message)
        {
        }

        public SenderHerIdMismatchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SenderHerIdMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}