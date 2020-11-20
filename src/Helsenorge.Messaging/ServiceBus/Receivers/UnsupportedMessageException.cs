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
    public class UnsupportedMessageException : Exception
    {
        public UnsupportedMessageException()
        {
        }

        public UnsupportedMessageException(string message) : base(message)
        {
        }

        public UnsupportedMessageException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnsupportedMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}