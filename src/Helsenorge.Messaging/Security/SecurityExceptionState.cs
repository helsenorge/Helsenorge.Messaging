using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Security
{
    /// <summary>
    /// Used to spesify state when security exception is thrown
    /// </summary>
    public static class SecurityExceptionState
    {
        /// <summary>
        /// When there is a issue with the signing of the certificate
        /// </summary>
        public static readonly string CertificateSigningError = "Error signing the certificate";

        /// <summary>
        /// When there is a issue with decrypting the encrypted message
        /// </summary>
        public static readonly string CertificateDecryptingError = "Error decrypting the encryption";
    }
}
