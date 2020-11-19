using System;
using System.Collections.Generic;
using System.Text;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal class InternalErrorException : ServiceBusException
    {
        public InternalErrorException(string message)
            : this(message, null)
        {
        }

        public InternalErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
