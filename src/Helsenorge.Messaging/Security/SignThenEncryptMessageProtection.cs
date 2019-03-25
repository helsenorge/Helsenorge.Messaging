using System;
using System.IO;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using Helsenorge.Messaging.Abstractions;

namespace Helsenorge.Messaging.Security
{
    /// <summary>
    /// Provides message protection that first signs the message, then encrypts it
    /// </summary>
    public class SignThenEncryptMessageProtection : MessageProtection
    {
        private readonly X509Certificate2 _signingCertificate;
        private readonly X509Certificate2 _encryptionCertificate;
        private readonly X509Certificate2 _legacyEncryptionCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignThenEncryptMessageProtection"/> class with the required certificates for signing and encrypt data.
        /// </summary>
        /// <param name="signingCertificate">Certificcate that will be used to sign data</param>
        /// <param name="encryptionCertificate">Certificate that will be used to encrypt data</param>
        /// <param name="legacyEncryptionCertificate">A legacy certificate that can be used when swapping certificates.</param>
        public SignThenEncryptMessageProtection(X509Certificate2 signingCertificate, X509Certificate2 encryptionCertificate, X509Certificate2 legacyEncryptionCertificate = null)
        { 
            _signingCertificate = signingCertificate ?? throw new ArgumentNullException(nameof(signingCertificate));
            _encryptionCertificate = encryptionCertificate ?? throw new ArgumentNullException(nameof(encryptionCertificate));
            _legacyEncryptionCertificate = legacyEncryptionCertificate;
        }

        /// <summary>
        /// Gets the content type this protection represents
        /// </summary>
        public override string ContentType => Abstractions.ContentType.SignedAndEnveloped;

        /// <summary>
        /// Protect the message data
        /// </summary>
        /// <param name="data">Data to protect</param>
        /// <param name="encryptionCertificate">Certificate use for encryption</param>
        /// <param name="signingCertificate">Certificate used for signature</param>
        /// <returns>Data that has been encrypted and signed</returns>
        [Obsolete("This method is deprecated and is superseded by MessageProtection.Protect(Stream).")]
        public override MemoryStream Protect(XDocument data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (encryptionCertificate == null) throw new ArgumentNullException(nameof(encryptionCertificate));
            if (signingCertificate == null) throw new ArgumentNullException(nameof(signingCertificate));

            // convert xml to a byte array the CMS API's can use
            var raw = Encoding.UTF8.GetBytes(data.ToString());

            // first we sign the message
            var signed = new SignedCms(new ContentInfo(raw));
            signed.ComputeSignature(new CmsSigner(signingCertificate));
            raw = signed.Encode();

            // then we encrypt it
            var recipient = new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, encryptionCertificate);
            var envelope = new EnvelopedCms(new ContentInfo(raw));

            envelope.Encrypt(recipient);
            raw = envelope.Encode();

            // convert back to a memory stream other systems can leverage
            return new MemoryStream(raw);
        }

        /// <summary>
        /// Signs and then encrypts the contents of <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data that will be signed and then encrypted.</param>
        /// <param name="encryptionCertificate">The public key <see cref="X509Certificate2"/> which will be used to encrypt the data.</param>
        /// <returns>A <see cref="Stream"/> containing the signed and encrypted data.</returns>
        public override Stream Protect(Stream data, X509Certificate2 encryptionCertificate)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (encryptionCertificate == null) throw new ArgumentNullException(nameof(encryptionCertificate));

            byte[] dataAsBytes = new byte[data.Length];
            data.Read(dataAsBytes, 0, (int)data.Length);

            return new MemoryStream(Protect(dataAsBytes, encryptionCertificate));
        }

        private byte[] Protect(byte[] data, X509Certificate2 encryptionCertificate)
        {
            // first we sign the message
            SignedCms signedCms = new SignedCms(new ContentInfo(data));
            signedCms.ComputeSignature(new CmsSigner(_signingCertificate));
            byte[] signedData = signedCms.Encode();

            // then we encrypt it
            CmsRecipient cmsRecipient = new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, encryptionCertificate);
            EnvelopedCms envelopedCms = new EnvelopedCms(new ContentInfo(signedData));
            envelopedCms.Encrypt(cmsRecipient);

            return envelopedCms.Encode();
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
        public override XDocument Unprotect(Stream data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate, X509Certificate2 legacyEncryptionCertificate)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (encryptionCertificate == null) throw new ArgumentNullException(nameof(encryptionCertificate));

            var ms = new MemoryStream();
            data.CopyTo(ms);

            var encryptionCertificates = new X509Certificate2Collection(encryptionCertificate);
            if (legacyEncryptionCertificate != null)
            {
                encryptionCertificates.Add(legacyEncryptionCertificate);
            }

            var raw = ms.ToArray();
            // first we decrypt the data
            var envelopedCm = new EnvelopedCms();
            envelopedCm.Decode(raw);
            try
            {
                envelopedCm.Decrypt(envelopedCm.RecipientInfos[0], encryptionCertificates);
            }
            catch (System.Security.Cryptography.CryptographicException ce)
            {
                var cert = envelopedCm?.RecipientInfos[0]?.RecipientIdentifier?.Value as System.Security.Cryptography.Xml.X509IssuerSerial?;
                if (cert.HasValue)
                    throw new SecurityException($"Message encrypted with certificate SerialNumber {cert.Value.SerialNumber}, IssueName {cert.Value.IssuerName } " +
                                            $"could not be decrypted. Certification details: {cert} Exception: {ce.Message}");
                throw new SecurityException($"Encryption certificate not found. Exception: {ce.Message}");
            }

            raw = envelopedCm.ContentInfo.Content;

            // then we validate the signature
            var signed = new SignedCms();
            signed.Decode(raw);

            // there have been cases when the sender doesn't specify a FromHerId; makes it impossible to find the signature certificate
            // the decryption certificate will be ours, so we since we sign first, we can decrypt the data and see if that gives us any clues
            // if no decryption certicate has been provided, we assume we don't have valid certificate
            if (signingCertificate != null)
            {
                // check if the certificate is in the list of certificates used to sign the package
                // there may be more than one; have seen the root certificate being included some times
                if (signed.Certificates.Find(X509FindType.FindBySerialNumber, signingCertificate.SerialNumber, false).Count == 0)
                {
                    var actualSignedCertificate = signed.Certificates.Count > 0
                        ? signed.Certificates[signed.Certificates.Count - 1] : null;
                    
                    // it looks like that last certificate in the collection is the one at the end of the chain
                    throw new CertificateException(
                        $"Expected signingcertificate: {Environment.NewLine} {signingCertificate} {Environment.NewLine}{Environment.NewLine}" +
                        $"Actual signingcertificate: {Environment.NewLine} {actualSignedCertificate} {Environment.NewLine}{Environment.NewLine}",
                        raw);
                }

                signed.CheckSignature(new X509Certificate2Collection(signingCertificate), true);
            }
            raw = signed.ContentInfo.Content;
            // convert from byte array to XDocument
            ms = new MemoryStream(raw);
            return ms.ToXDocument();
        }

        /// <summary>
        /// Decrypts and then verifies the signature of the content in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data which be decrypted and then the signature will be verified.</param>
        /// <param name="signingCertificate">The public key <see cref="X509Certificate2"/> which will be used to validate the signature of the message data.</param>
        /// <returns>A <see cref="Stream"/> containing the data in decrypted form.</returns>
        public override Stream Unprotect(Stream data, X509Certificate2 signingCertificate)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            byte[] dataAsBytes = new byte[data.Length];
            data.Read(dataAsBytes, 0, (int)data.Length);

            return new MemoryStream(Unprotect(dataAsBytes, signingCertificate));
        }

        private byte[] Unprotect(byte[] data, X509Certificate2 signingCertificate)
        {
            X509Certificate2Collection encryptionCertificates = new X509Certificate2Collection(_encryptionCertificate);
            if (_legacyEncryptionCertificate != null)
                encryptionCertificates.Add(_legacyEncryptionCertificate);

            // first we decrypt the data
            EnvelopedCms envelopedCms = new EnvelopedCms();
            envelopedCms.Decode(data);
            try
            {
                envelopedCms.Decrypt(envelopedCms.RecipientInfos[0], encryptionCertificates);
            }
            catch (System.Security.Cryptography.CryptographicException ce)
            {
                var cert = envelopedCms?.RecipientInfos[0]?.RecipientIdentifier?.Value as System.Security.Cryptography.Xml.X509IssuerSerial?;
                if (cert.HasValue)
                    throw new SecurityException($"Message encrypted with certificate SerialNumber {cert.Value.SerialNumber}, IssueName {cert.Value.IssuerName } " +
                                            $"could not be decrypted. Certification details: {cert} Exception: {ce.Message}");

                throw new SecurityException($"Encryption certificate not found. Exception: {ce.Message}");
            }
            // Retrieve the decrypted content.
            byte[] content = envelopedCms.ContentInfo.Content;

            // then we validate the signature
            SignedCms signedCms = new SignedCms();
            signedCms.Decode(content);

            // there have been cases when the sender doesn't specify a FromHerId; makes it impossible to find the signature certificate
            // the decryption certificate will be ours, so we since we sign first, we can decrypt the data and see if that gives us any clues
            // if no decryption certicate has been provided, we assume we don't have valid certificate
            if (signingCertificate != null)
            {
                // check if the certificate is in the list of certificates used to sign the package
                // there may be more than one; have seen the root certificate being included some times
                if (signedCms.Certificates.Find(X509FindType.FindBySerialNumber, signingCertificate.SerialNumber, false).Count == 0)
                {
                    var actualSignedCertificate = signedCms.Certificates.Count > 0
                        ? signedCms.Certificates[signedCms.Certificates.Count - 1] : null;

                    // it looks like that last certificate in the collection is the one at the end of the chain
                    throw new CertificateException(
                        $"Expected signingcertificate: {Environment.NewLine} {signingCertificate} {Environment.NewLine}{Environment.NewLine}" +
                        $"Actual signingcertificate: {Environment.NewLine} {actualSignedCertificate} {Environment.NewLine}{Environment.NewLine}",
                        content);
                }

                signedCms.CheckSignature(new X509Certificate2Collection(signingCertificate), true);
            }
            // Return the raw content (without a signature).
            return signedCms.ContentInfo.Content;
        }
    }
}
