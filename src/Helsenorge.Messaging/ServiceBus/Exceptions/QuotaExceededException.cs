using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the Quota (Entity Max Size or other Connection etc) allocated to the Entity has exceeded.  Callers should check the
    /// error message to see which of the Quota exceeded and take appropriate action.
    /// </summary>
    internal sealed class QuotaExceededException : ServiceBusException
    {
        public QuotaExceededException(string message)
            : this(message, null)
        {
        }

        public QuotaExceededException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
