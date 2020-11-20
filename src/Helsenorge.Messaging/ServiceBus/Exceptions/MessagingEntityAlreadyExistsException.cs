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
    /// The exception that is thrown when an already existing entity is being re created.
    /// </summary>
    internal class MessagingEntityAlreadyExistsException : ServiceBusException
    {
        public MessagingEntityAlreadyExistsException(string message) : this(message, null)
        {
        }

        public MessagingEntityAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
