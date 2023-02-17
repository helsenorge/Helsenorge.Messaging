/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Messaging.Abstractions;

namespace Helsenorge.Messaging.Security
{
    /// <summary>
    /// Provides no message protection at all
    /// </summary>
    public class NoMessageProtection : IMessageProtection
    {
        /// <summary>
        /// Gets the content type applied to protected data
        /// </summary>
        public string ContentType => Abstractions.ContentType.Text;
        /// <summary>
        /// Gets the signing certificate, but it's not used in this implementation.
        /// </summary>
        public X509Certificate2 SigningCertificate => null;
        /// <summary>
        /// Gets the encryption certificate, but it's not used in this implementation.
        /// </summary>
        public X509Certificate2 EncryptionCertificate => null;
        /// <summary>
        /// Gets the legacy encryption certificate, but it's not used in this implementation.
        /// </summary>
        public X509Certificate2 LegacyEncryptionCertificate => null;

        /// <summary>
        /// Signs and then encrypts the contents of <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data that will be signed and then encrypted.</param>
        /// <param name="encryptionCertificate">Not relevant for this implementation.</param>
        /// <returns>A <see cref="Stream"/> containing the signed and encrypted data.</returns>
        public Stream Protect(Stream data, X509Certificate2 encryptionCertificate)
        {
            return data;
        }

        /// <summary>
        /// Decrypts and then verifies the signature of the content in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">A <see cref="Stream"/> containing the data which be decrypted and then the signature will be verified.</param>
        /// <param name="signingCertificate">Not relevant for this implemenation</param>
        /// <returns>A <see cref="Stream"/> containing the data in decrypted form.</returns>
        public Stream Unprotect(Stream data, X509Certificate2 signingCertificate)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            return data;
        }
    }
}
