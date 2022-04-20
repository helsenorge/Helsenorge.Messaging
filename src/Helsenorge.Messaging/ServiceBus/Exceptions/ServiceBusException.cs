/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    public abstract class ServiceBusException : Exception
    {
        public abstract bool CanRetry { get; }

        protected ServiceBusException(string message) : base(message)
        {
        }

        protected ServiceBusException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
