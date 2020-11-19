using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    class InvalidFieldException : ServiceBusException
    {
        public InvalidFieldException(string message)
            : this(message, null)
        {
        }

        public InvalidFieldException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
