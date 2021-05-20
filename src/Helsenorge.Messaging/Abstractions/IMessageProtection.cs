/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
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
        /// Gets the encryption certificate
        /// </summary>
        X509Certificate2 EncryptionCertificate { get; }

        /// <summary>
        /// Gets the signing certificate
        /// </summary>
        X509Certificate2 SigningCertificate { get; }

        /// <summary>
        /// Gets the legacy encryption certificate
        /// </summary>
        X509Certificate2 LegacyEncryptionCertificate { get; }

        /// <summary>
        /// Protect the message data
        /// </summary>
        /// <param name="data">Data to protect</param>
        /// <param name="encryptionCertificate">Certificate use for encryption</param>
        /// <param name="signingCertificate">Certificate used for signature</param>
        /// <returns>Data that has been encrypted and signed</returns>
        [Obsolete("This method is deprecated and is superseded by IMessageProtection.Protect(Stream).")]
        MemoryStream Protect(XDocument data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate);

        /// <summary>
        /// Signs and then encrypts the contents of <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data that will be signed and then encrypted.</param>
        /// <param name="encryptionCertificate">The public key <see cref="X509Certificate2"/> which will be used to encrypt the data.</param>
        /// <returns>A <see cref="Stream"/> containing the signed and encrypted data.</returns>
        Stream Protect(Stream data, X509Certificate2 encryptionCertificate);

        /// <summary>
        /// Removes protection from the message data
        /// </summary>
        /// <param name="data">Protected data</param>
        /// <param name="encryptionCertificate">Certificate use for encryption</param>
        /// <param name="signingCertificate">Certificate used for signature</param>
        /// <param name="legacyEncryptionCertificate">Old encryption certificate that is no longer i use</param>
        /// <returns>Data that has been decrypted and verified</returns>
        [Obsolete("This method is deprecated and is superseded by IMessageProtection.Unprotect(Stream).")]
        XDocument Unprotect(Stream data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate, X509Certificate2 legacyEncryptionCertificate);

        /// <summary>
        /// Decrypts and then verifies the signature of the content in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data which be decrypted and then the signature will be verified.</param>
        /// <param name="signingCertificate">The public key <see cref="X509Certificate2"/> which will be used to validate the signature of the message data.</param>
        /// <returns>A <see cref="Stream"/> containing the data in decrypted form.</returns>
        Stream Unprotect(Stream data, X509Certificate2 signingCertificate);
    }
}
