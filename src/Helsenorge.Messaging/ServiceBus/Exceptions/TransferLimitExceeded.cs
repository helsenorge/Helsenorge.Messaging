using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when message transfers exceeds the limit currently allowed on the link
    /// </summary>
    internal class TransferLimitExceeded : ServiceBusException
    {
        public TransferLimitExceeded(string message)
            : this(message, null)
        {
        }

        public TransferLimitExceeded(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
