using System.Security;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    ///     Exception related to signing certificate with inheritance to SecurityException
    /// </summary>
    public class CertificateException : SecurityException
    {
        /// <summary>
        ///     Initiate a new instance
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="payload">Payload of message</param>
        public CertificateException(string message, byte[] payload) : base(message)
        {
            Payload = payload;
        }

        /// <summary>
        ///     Payload of message with error signing certificate
        /// </summary>
        public byte[] Payload { get; }
    }
}