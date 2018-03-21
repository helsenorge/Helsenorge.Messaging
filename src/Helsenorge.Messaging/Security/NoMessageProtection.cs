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
        public string ContentType => Messaging.Abstractions.ContentType.Text;

        /// <summary>
        /// Protects the information based on the certificates
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encryptionCertificate">Not relevant for this implementation</param>
        /// <param name="signingCertificate">Not relevant for this implementation</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public MemoryStream Protect(XDocument data, X509Certificate2 encryptionCertificate, X509Certificate2 signingCertificate)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var ms = new MemoryStream();
            data.Save(ms);
            ms.Position = 0;
            return ms;
        }

        /// <summary>
        /// Unprotects the information based on the certificates
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encryptionCertificate">Not relevant for this implementation</param>
        /// <param name="signingCertificate">Not relevant for this implementation</param>
        /// <param name="legacyEncryptionCertificate">Not relevant for this implementation</param>
        /// <returns></returns>
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
    }
}
