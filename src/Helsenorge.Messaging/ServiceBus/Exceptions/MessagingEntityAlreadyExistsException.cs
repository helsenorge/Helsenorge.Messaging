using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an already existing entity is being re created.
    /// </summary>
    internal class MessagingEntityAlreadyExistsException : ServiceBusException
    {
        public MessagingEntityAlreadyExistsException(string message) : this(message, null)
        {
        }

        public MessagingEntityAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
