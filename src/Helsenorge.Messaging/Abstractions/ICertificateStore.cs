using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Abstraction for the certificate store
    /// </summary>
    public interface ICertificateStore
    {
        /// <summary>
        /// Returns a certificate from a certificate store
        /// </summary>
        /// <returns></returns>
        X509Certificate2 GetCertificate(object identifier);
    }
}
