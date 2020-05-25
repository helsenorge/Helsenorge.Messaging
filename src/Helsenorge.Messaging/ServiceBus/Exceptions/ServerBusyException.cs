using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a server is busy.  Callers should wait a while and retry the operation.
    /// </summary>
    internal sealed class ServerBusyException : ServiceBusException
    {
        public ServerBusyException(string message)
            : this(message, null)
        {
        }

        public ServerBusyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => true;
    }
}
