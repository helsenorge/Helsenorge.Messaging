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
    public sealed class RecoverableServiceBusException : ServiceBusException
    {
        public RecoverableServiceBusException(string message) : base(message)
        {
        }

        public RecoverableServiceBusException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public override bool CanRetry => true;
    }
}
