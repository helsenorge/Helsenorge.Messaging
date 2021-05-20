/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus.Receivers;

namespace Helsenorge.Messaging.Security
{
    /// <summary>
    /// Provices no message protection at all
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
        /// Protects the information based on the certificates
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encryptionCertificate">Not relevant for this implementation</param>
        /// <param name="signingCertificate">Not relevant for this implementation</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [Obsolete("This method is deprecated and is superseded by NoMessageProtection.Protect(Stream).")]
        public MemoryStream Protect(XDocument data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var ms = new MemoryStream();
            data.Save(ms);
            ms.Position = 0;
            return ms;
        }

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
        /// Unprotects the information based on the certificates
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encryptionCertificate">Not relevant for this implementation</param>
        /// <param name="signingCertificate">Not relevant for this implementation</param>
        /// <param name="legacyEncryptionCertificate">Not relevant for this implementation</param>
        /// <returns></returns>
        [Obsolete("This method is deprecated and is superseded by NoMessageProtection.Unprotect(Stream).")]
        public XDocument Unprotect(Stream data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate, X509Certificate2 legacyEncryptionCertificate)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                return XDocument.Load(data);
            }
            catch (XmlException)
            {
                // some parties have chose to use string instead of stream when sending unecrypted XML (soap faults)
                // since the GetBody<Stream>() always returns a valid stream, it causes a problem if the original data was string

                // the general XDocument.Load() fails, then we try a fallback to a manually deserialize the content
                try
                { 
                    data.Position = 0;
                    var serializer = new DataContractSerializer(typeof(string));
                    var dictionary = XmlDictionaryReader.CreateBinaryReader(data, XmlDictionaryReaderQuotas.Max);
                    var xmlContent = serializer.ReadObject(dictionary);

                    return XDocument.Parse(xmlContent as string);
                }
                catch (Exception ex)
                {
                    throw new PayloadDeserializationException("Could not deserialize payload", ex);
                }
            }
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
