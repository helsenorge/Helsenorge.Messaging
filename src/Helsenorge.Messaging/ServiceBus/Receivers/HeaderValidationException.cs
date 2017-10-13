using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    [Serializable]
    internal class HeaderValidationException : Exception
    {
        public IEnumerable<string> Fields { get; set; }

        public HeaderValidationException()
        {
        }

        public HeaderValidationException(string message) : base(message)
        {
        }

        public HeaderValidationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected HeaderValidationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
