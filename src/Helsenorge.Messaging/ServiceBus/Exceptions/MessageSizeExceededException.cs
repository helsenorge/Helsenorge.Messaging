using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the message size exceeds the limit.
    /// </summary>
    internal sealed class MessageSizeExceededException : ServiceBusException
    {
        public MessageSizeExceededException(string message)
            : this(message, null)
        {
        }

        public MessageSizeExceededException(string message, Exception innerException)
            : base( message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
