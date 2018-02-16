using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Exception related to signing certificate with inheritance to SecurityException
    /// </summary>
    public class SigningCertificateException : SecurityException
    {
        /// <summary>
        /// Payload of message with error signing certificate
        /// </summary>
        public string Payload { get; }

        /// <summary>
        /// Initiate a new instance
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="payload">Payload of message</param>
        public SigningCertificateException(string message, string payload) : base(message)
        {
            Payload = payload;
        }
    }
}
