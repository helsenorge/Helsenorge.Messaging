using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal class MessageReleasedException : ServiceBusException
    {
        public MessageReleasedException(string message)
            : this(message, null)
        {
        }

        public MessageReleasedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
