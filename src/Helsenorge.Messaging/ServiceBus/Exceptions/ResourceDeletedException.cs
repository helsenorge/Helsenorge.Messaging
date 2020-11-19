using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal class ResourceDeletedException : ServiceBusException
    {
        public ResourceDeletedException(string message)
            : this(message, null)
        {
        }

        public ResourceDeletedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
