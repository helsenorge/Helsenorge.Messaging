using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal sealed class UncategorizedServiceBusException : ServiceBusException
    {
        public UncategorizedServiceBusException(string message) : base(message)
        {
        }

        public UncategorizedServiceBusException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
