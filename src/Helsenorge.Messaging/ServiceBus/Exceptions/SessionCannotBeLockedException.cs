using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a session cannot be locked.
    /// </summary>
    internal sealed class SessionCannotBeLockedException : ServiceBusException
    {
        public SessionCannotBeLockedException(string message)
            : this(message, null)
        {
        }

        public SessionCannotBeLockedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
