using System;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    /// <summary>
    /// The exception that is thrown when we receive an unexpected identifier type on the AMQP frame
    /// </summary>
    /// <remarks>
    /// The identifier type may be valid within the AMQP specification, but Helsenorge.Messaging does not support the identifier type.
    /// Valid types are <see cref="System.String"/> and <see cref="System.Guid"/>
    /// </remarks>
    internal class UnexpectedMessageIdentifierTypeException : ServiceBusException
    {
        public UnexpectedMessageIdentifierTypeException(string message) : base(message)
        {
        }

        public UnexpectedMessageIdentifierTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public override bool CanRetry => false;
    }
}
