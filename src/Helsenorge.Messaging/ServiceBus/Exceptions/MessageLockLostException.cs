using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the lock on the message is lost.  Callers should call Receive and process the message again.
    /// </summary>
    internal sealed class MessageLockLostException : ServiceBusException
    {
        public MessageLockLostException(string message)
            : base(message, null)
        {
        }

        public MessageLockLostException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
