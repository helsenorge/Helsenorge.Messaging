using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal class FrameSizeTooSmallException : ServiceBusException
    {
        public FrameSizeTooSmallException(string message)
            : this(message, null)
        {
        }

        public FrameSizeTooSmallException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
