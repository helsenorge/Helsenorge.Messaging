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
    /// The exception that is thrown when the Quota (Entity Max Size or other Connection etc) allocated to the Entity has exceeded.  Callers should check the
    /// error message to see which of the Quota exceeded and take appropriate action.
    /// </summary>
    public sealed class QuotaExceededException : ServiceBusException
    {
        public QuotaExceededException(string message)
            : this(message, null)
        {
        }

        public QuotaExceededException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
