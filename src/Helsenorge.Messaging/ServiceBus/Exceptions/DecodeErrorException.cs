using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal class DecodeErrorException : ServiceBusException
    {
        public DecodeErrorException(string message)
            : this(message, null)
        {
        }

        public DecodeErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
