using System;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    [Serializable]
    public class PayloadDeserializationException : Exception
    {
        public PayloadDeserializationException()
        {
        }

        public PayloadDeserializationException(string message) : base(message)
        {
        }

        public PayloadDeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}