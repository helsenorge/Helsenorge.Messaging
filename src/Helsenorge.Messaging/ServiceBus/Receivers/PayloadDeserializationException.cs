/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    [Serializable]
    public class PayloadDeserializationException : Exception
    {
        public PayloadDeserializationException()
        {
        }

        public PayloadDeserializationException(string message) : base(message)
        {
        }

        public PayloadDeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}