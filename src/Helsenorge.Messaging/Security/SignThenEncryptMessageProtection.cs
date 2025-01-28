/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs;
using Helsenorge.Messaging.Abstractions;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Helsenorge.Messaging.Amqp.Receivers;

namespace Helsenorge.Messaging.Security
{
    /// <summary>
    /// Provides message protection that first signs the message, then encrypts it
    /// </summary>
    public class SignThenEncryptMessageProtection : MessageProtection
    {
        private readonly ILogger _logger;
        private readonly X509IncludeOption? _includeOption;
        private readonly MessagingEncryptionType _messagingEncryptionType;
        private readonly MessagingEncryptionType _rejectMessagingEncryptionTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignThenEncryptMessageProtection"/> class with the required certificates for signing and encrypting data.
        /// </summary>
        /// <param name="signingCertificate">Certificcate that will be used to sign data</param>
        /// <param name="encryptionCertificate">Certificate that will be used to encrypt data</param>
        /// <param name="logger"></param>
        /// <param name="legacyEncryptionCertificate">A legacy certificate that can be used when swapping certificates.</param>
        /// <param name="includeOption">Controls how much of the signer certificate's certificate chain should be
        /// embedded in the signed message. If not specified, the default <see cref="X509IncludeOption.ExcludeRoot"/>
        /// is used.</param>
        /// <param name="messagingEncryptionType">Controls which encryption type the Protect methods use.</param>
        /// <param name="rejectMessagingEncryptionType">Controls which encryption type the Unprotect methods rejects.</param>
        public SignThenEncryptMessageProtection(
            X509Certificate2 signingCertificate,
            X509Certificate2 encryptionCertificate,
            ILogger logger,
            X509Certificate2 legacyEncryptionCertificate = null,
            X509IncludeOption? includeOption = default,
            MessagingEncryptionType messagingEncryptionType = MessagingEncryptionType.AES256,
            MessagingEncryptionType rejectMessagingEncryptionType = MessagingEncryptionType.None)
            : base(signingCertificate, encryptionCertificate, legacyEncryptionCertificate)
        {
            _logger = logger;
            _includeOption = includeOption;
            _messagingEncryptionType = messagingEncryptionType;
            _rejectMessagingEncryptionTypes = rejectMessagingEncryptionType;
        }

        /// <summary>
        /// Gets the content type this protection represents
        /// </summary>
        public override string ContentType => Abstractions.ContentType.SignedAndEnveloped;

        /// <summary>
        /// Signs and then encrypts the contents of <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data that will be signed and then encrypted.</param>
        /// <param name="encryptionCertificate">The public key <see cref="X509Certificate2"/> which will be used to encrypt the data.</param>
        /// <returns>A <see cref="Stream"/> containing the signed and encrypted data.</returns>
        public override Stream Protect(Stream data, X509Certificate2 encryptionCertificate)
        {
            return Protect(data, encryptionCertificate, SigningCertificate);
        }

        /// <inheritdoc/>
        public override Stream Protect(Stream data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (encryptionCertificate == null) throw new ArgumentNullException(nameof(encryptionCertificate));
            if (signingCertificate == null) throw new ArgumentNullException(nameof(signingCertificate));

            byte[] dataAsBytes = new byte[data.Length];
            data.Read(dataAsBytes, 0, (int)data.Length);

            return new MemoryStream(Protect(dataAsBytes, encryptionCertificate, signingCertificate));
        }

        private byte[] Protect(byte[] data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate)
        {
            // first we sign the message
            var signedCms = new SignedCms(new ContentInfo(data));
            var signer = new CmsSigner(signingCertificate);
            if (_includeOption.HasValue)
            {
                signer.IncludeOption = _includeOption.Value;
            }

            signedCms.ComputeSignature(signer);
            byte[] signedData = signedCms.Encode();

            // then we encrypt it
            CmsRecipient cmsRecipient = new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, encryptionCertificate);
            EnvelopedCms envelopedCms = GetEnvelope(signedData);
            envelopedCms.Encrypt(cmsRecipient);

            return envelopedCms.Encode();
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
            X509Certificate2Collection encryptionCertificates = new X509Certificate2Collection(EncryptionCertificate);
            if (LegacyEncryptionCertificate != null)
                encryptionCertificates.Add(LegacyEncryptionCertificate);

            // first we decrypt the data
            EnvelopedCms envelopedCms = new EnvelopedCms();
            envelopedCms.Decode(data);
            try
            {
                var encryptionOid = envelopedCms?.ContentEncryptionAlgorithm?.Oid;
                _logger.LogInformation($"Decrypting EnvelopedCms with ContentEncryptionAlgorithm: {encryptionOid?.FriendlyName ?? "null"} : {encryptionOid?.Value ?? "null"}");

                if ((_rejectMessagingEncryptionTypes.HasFlag(MessagingEncryptionType.DES) && encryptionOid.Value == "1.3.14.3.2.7")
                    || (_rejectMessagingEncryptionTypes.HasFlag(MessagingEncryptionType.TripleDES) && encryptionOid.Value == "1.2.840.113549.3.7"))
                {
                    throw new UnsupportedMessageException($"EnvelopedCms was encrypted with disabled ContentEncryptionAlgorithm: {encryptionOid?.FriendlyName ?? "null"} : {encryptionOid?.Value ?? "null"}");
                }

                envelopedCms.Decrypt(envelopedCms.RecipientInfos[0], encryptionCertificates);
            }
            catch (System.Security.Cryptography.CryptographicException ce)
            {
                var cert = envelopedCms?.RecipientInfos[0]?.RecipientIdentifier?.Value as System.Security.Cryptography.Xml.X509IssuerSerial?;
                if (cert.HasValue)
                    throw new SecurityException($"Message encrypted with certificate SerialNumber {cert.Value.SerialNumber}, IssueName {cert.Value.IssuerName} " +
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
                    throw new CertificateMessagePayloadException(
                        $"Expected signingcertificate: {Environment.NewLine} {signingCertificate} {Environment.NewLine}{Environment.NewLine}" +
                        $"Actual signingcertificate: {Environment.NewLine} {actualSignedCertificate} {Environment.NewLine}{Environment.NewLine}",
                        content);
                }

                signedCms.CheckSignature(new X509Certificate2Collection(signingCertificate), true);
            }
            // Return the raw content (without a signature).
            return signedCms.ContentInfo.Content;
        }

        private EnvelopedCms GetEnvelope(byte[] rawContent)
        {
            if (_messagingEncryptionType.HasFlag(MessagingEncryptionType.AES256))
            {
                return new EnvelopedCms(new ContentInfo(rawContent), new AlgorithmIdentifier(new Oid("2.16.840.1.101.3.4.1.42")));
            }
            else if (_messagingEncryptionType.HasFlag(MessagingEncryptionType.TripleDES))
            {
                return new EnvelopedCms(new ContentInfo(rawContent), new AlgorithmIdentifier(new Oid("1.2.840.113549.3.7")));
            }

            throw new ArgumentException($"MessagingEncryptionType has been set to an unsupported type.: {_messagingEncryptionType}", nameof(_messagingEncryptionType));
        }
    }
}
