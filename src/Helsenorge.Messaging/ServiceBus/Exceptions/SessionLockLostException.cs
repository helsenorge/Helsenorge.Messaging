using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the lock on the Session has expired.  Callers should receive the Session again.
    /// </summary>
    internal sealed class SessionLockLostException : ServiceBusException
    {
        public SessionLockLostException(string message)
            : this(message, null)
        {
        }

        public SessionLockLostException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
