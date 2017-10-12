using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Validates a certificate
    /// </summary>
    public interface ICertificateValidator
    {
        /// <summary>
        /// Validates the provided certificate
        /// </summary>
        /// <param name="certificate">The certificate to validate</param>
        /// <param name="usage">The type of usage the certificate is specified for</param>
        /// <returns>A bitcoded status indicating if the certificate is valid or not</returns>
        CertificateErrors Validate(X509Certificate2 certificate, X509KeyUsageFlags usage);
    }
}
