using System;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    [Serializable]
    public class PayloadDecryptionException : Exception
    {
        public PayloadDecryptionException()
        {
        }

        public PayloadDecryptionException(string message) : base(message)
        {
        }

        public PayloadDecryptionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}