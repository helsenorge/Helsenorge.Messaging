using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the Messaging Entity is not found.  Verify Entity Exists.
    /// </summary>
    internal sealed class MessagingEntityNotFoundException : ServiceBusException
    {
        public MessagingEntityNotFoundException(string message)
            : this(message, null)
        {
        }

        public MessagingEntityNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry { get; }
    }
}
