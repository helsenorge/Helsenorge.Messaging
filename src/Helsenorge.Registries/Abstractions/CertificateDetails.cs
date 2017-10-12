using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Represents information about the Certificate
    /// </summary>
    public class CertificateDetails
    {
        /// <summary>
        /// The HER-ID of the communication party. This is identifies this party in the Address Registry
        /// </summary>
        public int HerId { get; set; }

        /// <summary>
        /// The certificate of the communcation party represented as a byte array.
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// The complete url for the certificate.
        /// </summary>
        public string LdapUrl { get; set; }
    }
}
