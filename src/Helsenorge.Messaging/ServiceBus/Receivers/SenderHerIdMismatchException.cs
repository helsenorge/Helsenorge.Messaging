using System;
using System.Runtime.Serialization;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    [Serializable]
    public class SenderHerIdMismatchException : Exception
    {
        public SenderHerIdMismatchException()
        {
        }

        public SenderHerIdMismatchException(string message) : base(message)
        {
        }

        public SenderHerIdMismatchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SenderHerIdMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}