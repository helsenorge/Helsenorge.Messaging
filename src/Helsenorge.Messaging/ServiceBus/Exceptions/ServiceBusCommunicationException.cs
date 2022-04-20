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
    /// <summary>
    /// Exception for signaling general communication errors related to messaging operations.
    /// </summary>
    public class ServiceBusCommunicationException : ServiceBusException
    {
        public ServiceBusCommunicationException(string message)
            : this(message, null)
        {
        }

        public ServiceBusCommunicationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => true;
    }
}
