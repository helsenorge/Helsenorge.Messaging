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
    public sealed class UncategorizedServiceBusException : ServiceBusException
    {
        public UncategorizedServiceBusException(string message) : base(message)
        {
        }

        public UncategorizedServiceBusException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public UncategorizedServiceBusException(string message, string condition, Exception innerException) : base($"{message} Condition: {condition}", innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
