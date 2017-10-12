using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Protectes a message using certificate encryption and signing
    /// </summary>
    public interface IMessageProtection
    {
        /// <summary>
        /// Gets the content type this protection represents
        /// </summary>
        string ContentType { get; }
        /// <summary>
        /// Protect the message data
        /// </summary>
        /// <param name="data">Data to protect</param>
        /// <param name="encryptionCertificate">Certificate use for encryption</param>
        /// <param name="signingCertificate">Certificate used for signature</param>
        /// <returns>Data that has been encrypted and signed</returns>
        MemoryStream Protect(XDocument data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate);

        /// <summary>
        /// Removes protection from the message data
        /// </summary>
        /// <param name="data">Protected data</param>
        /// <param name="encryptionCertificate">Certificate use for encryption</param>
        /// <param name="signingCertificate">Certificate used for signature</param>
        /// <param name="legacyEncryptionCertificate">Old encryption certificate that is no longer i use</param>
        /// <returns>Data that has been decrypted and verified</returns>
        XDocument Unprotect(Stream data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate, X509Certificate2 legacyEncryptionCertificate);
    }
}
