using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal class ResourceLockedException : ServiceBusException
    {
        public ResourceLockedException(string message)
            : this(message, null)
        {
        }

        public ResourceLockedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
