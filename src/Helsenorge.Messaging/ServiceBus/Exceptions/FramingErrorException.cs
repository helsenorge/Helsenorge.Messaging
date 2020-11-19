using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal class FramingErrorException : ServiceBusException
    {
        public FramingErrorException(string message)
            : this(message, null)
        {
        }

        public FramingErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}