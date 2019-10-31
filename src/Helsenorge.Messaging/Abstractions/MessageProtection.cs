using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Provides message protection that first signs the message, then encrypts it
    /// </summary>
    public abstract class MessageProtection : IMessageProtection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProtection"/> class with the required certificates for signing and encrypting data.
        /// </summary>
        /// <param name="signingCertificate">Certificcate that will be used to sign data</param>
        /// <param name="encryptionCertificate">Certificate that will be used to encrypt data</param>
        /// <param name="legacyEncryptionCertificate">A legacy certificate that can be used when swapping certificates.</param>
        protected MessageProtection(X509Certificate2 signingCertificate, X509Certificate2 encryptionCertificate, X509Certificate2 legacyEncryptionCertificate = null)
        {
            SigningCertificate = signingCertificate ?? throw new ArgumentNullException(nameof(signingCertificate));
            EncryptionCertificate = encryptionCertificate ?? throw new ArgumentNullException(nameof(encryptionCertificate));
            LegacyEncryptionCertificate = legacyEncryptionCertificate;
        }

        /// <summary>
        /// Gets the content type this protection represents
        /// </summary>
        public virtual string ContentType => Abstractions.ContentType.SignedAndEnveloped;
        /// <summary>
        /// Gets the signing certificate
        /// </summary>
        public X509Certificate2 SigningCertificate { get; private set; }
        /// <summary>
        /// Gets the encryption certificate
        /// </summary>
        public X509Certificate2 EncryptionCertificate { get; private set; }
        /// <summary>
        /// Gets the legacy encryption certificate
        /// </summary>
        public X509Certificate2 LegacyEncryptionCertificate { get; private set; }

        /// <summary>
        /// Protect the message data
        /// </summary>
        /// <param name="data">Data to protect</param>
        /// <param name="encryptionCertificate">Certificate use for encryption</param>
        /// <param name="signingCertificate">Certificate used for signature</param>
        /// <returns>Data that has been encrypted and signed</returns>
        [Obsolete("This method is deprecated and is superseded by MessageProtection.Protect(Stream).")]
        public virtual MemoryStream Protect(XDocument data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Signs and then encrypts the contents of <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data that will be signed and then encrypted.</param>
        /// <param name="encryptionCertificate">The public key <see cref="X509Certificate2"/> which will be used to encrypt the data.</param>
        /// <returns>A <see cref="Stream"/> containing the signed and encrypted data.</returns>
        public virtual Stream Protect(Stream data, X509Certificate2 encryptionCertificate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes protection from the message data
        /// </summary>
        /// <param name="data">Protected data</param>
        /// <param name="encryptionCertificate">Certificate use for encryption</param>
        /// <param name="signingCertificate">Certificate used for signature</param>
        /// <param name="legacyEncryptionCertificate">Old encryption certificate</param>
        /// <returns>Data that has been decrypted and verified</returns>
        [Obsolete("This method is deprecated and is superseded by MessageProtection.Unprotect(Stream).")]
        public virtual XDocument Unprotect(Stream data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate, X509Certificate2 legacyEncryptionCertificate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Decrypts and then verifies the signature of the content in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data which be decrypted and then the signature will be verified.</param>
        /// <param name="signingCertificate">The public key <see cref="X509Certificate2"/> which will be used to validate the signature of the message data.</param>
        /// <returns>A <see cref="Stream"/> containing the data in decrypted form.</returns>
        public virtual Stream Unprotect(Stream data, X509Certificate2 signingCertificate)
        {
            throw new NotImplementedException();
        }
    }
}
