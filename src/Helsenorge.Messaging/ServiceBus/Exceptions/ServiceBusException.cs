using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal abstract class ServiceBusException : Exception
    {
        public abstract bool CanRetry { get; }

        protected ServiceBusException(string message) : base(message)
        {
        }

        protected ServiceBusException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
