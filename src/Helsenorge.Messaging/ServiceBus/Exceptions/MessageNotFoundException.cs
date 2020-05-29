using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the requested message is not found.
    /// </summary>
    internal sealed class MessageNotFoundException : ServiceBusException
    {
        public MessageNotFoundException(string message)
            : this(message, null)
        {
        }

        public MessageNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
