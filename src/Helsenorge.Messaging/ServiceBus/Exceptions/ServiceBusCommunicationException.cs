using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// Exception for signaling general communication errors related to messaging operations.
    /// </summary>
    internal class ServiceBusCommunicationException : ServiceBusException
    {
        public ServiceBusCommunicationException(string message)
            : this(message, null)
        {
        }

        public ServiceBusCommunicationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => true;
    }
}
