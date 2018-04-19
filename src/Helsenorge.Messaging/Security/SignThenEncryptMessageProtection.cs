using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Xml;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus.Receivers;

namespace Helsenorge.Messaging.Security
{
    /// <summary>
    /// Provides message protection that first signs the message, then encrypts it
    /// </summary>
    public class SignThenEncryptMessageProtection : IMessageProtection
    {
        /// <summary>
        /// Gets the content type this protection represents
        /// </summary>
        public string ContentType => Messaging.Abstractions.ContentType.SignedAndEnveloped;

        /// <summary>
        /// Protect the message data
        /// </summary>
        /// <param name="data">Data to protect</param>
        /// <param name="encryptionCertificate">Certificate use for encryption</param>
        /// <param name="signingCertificate">Certificate used for signature</param>
        /// <returns>Data that has been encrypted and signed</returns>
        public MemoryStream Protect(XDocument data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate)
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
        /// Removes protection from the message data
        /// </summary>
        /// <param name="data">Protected data</param>
        /// <param name="encryptionCertificate">Certificate use for encryption</param>
        /// <param name="signingCertificate">Certificate used for signature</param>
        /// <param name="legacyEncryptionCertificate">Old encryption certificate</param>
        /// <returns>Data that has been decrypted and verified</returns>
        public XDocument Unprotect(Stream data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate, X509Certificate2 legacyEncryptionCertificate)
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

            //now we have an unprotected stream reuse the NoMessageProtection code
            var noMessageProtection = new NoMessageProtection();
            return noMessageProtection.Unprotect(ms, null, null, null);
        }
    }
}
