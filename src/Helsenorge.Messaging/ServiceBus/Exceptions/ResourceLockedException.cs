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
    internal class ResourceLockedException : ServiceBusException
    {
        public ResourceLockedException(string message)
            : this(message, null)
        {
        }

        public ResourceLockedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
