using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the Messaging Entity is disabled. Enable the entity again using Portal.
    /// </summary>
    internal sealed class MessagingEntityDisabledException : ServiceBusException
    {
        public MessagingEntityDisabledException(string message)
            : this(message, null)
        {
        }

        public MessagingEntityDisabledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
