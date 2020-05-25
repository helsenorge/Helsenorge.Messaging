using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal sealed class RecoverableServiceBusException : ServiceBusException
    {
        public RecoverableServiceBusException(string message) : base(message)
        {
        }

        public RecoverableServiceBusException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public override bool CanRetry => true;
    }
}
