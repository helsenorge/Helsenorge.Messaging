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
    /// The exception that is thrown when a server is busy.  Callers should wait a while and retry the operation.
    /// </summary>
    public sealed class ServerBusyException : ServiceBusException
    {
        public ServerBusyException(string message)
            : this(message, null)
        {
        }

        public ServerBusyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => true;
    }
}
