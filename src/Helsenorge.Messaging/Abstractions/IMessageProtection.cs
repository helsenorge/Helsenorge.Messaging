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
    /// Protects a message using certificate encryption and signing
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
        /// Signs and then encrypts the contents of <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data that will be signed and then encrypted.</param>
        /// <param name="encryptionCertificate">The public key <see cref="X509Certificate2"/> which will be used to encrypt the data.</param>
        /// <returns>A <see cref="Stream"/> containing the signed and encrypted data.</returns>
        Stream Protect(Stream data, X509Certificate2 encryptionCertificate);

        /// <summary>
        /// Decrypts and then verifies the signature of the content in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data which be decrypted and then the signature will be verified.</param>
        /// <param name="signingCertificate">The public key <see cref="X509Certificate2"/> which will be used to validate the signature of the message data.</param>
        /// <returns>A <see cref="Stream"/> containing the data in decrypted form.</returns>
        Stream Unprotect(Stream data, X509Certificate2 signingCertificate);
    }
}
