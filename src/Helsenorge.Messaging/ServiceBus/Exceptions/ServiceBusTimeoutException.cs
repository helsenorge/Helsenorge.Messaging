using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a time out is encountered.  Callers retry the operation.
    /// </summary>
    internal class ServiceBusTimeoutException : ServiceBusException
    {
        public ServiceBusTimeoutException(string message) : this(message, null)
        {
        }

        public ServiceBusTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public override bool CanRetry => true;
    }
}
