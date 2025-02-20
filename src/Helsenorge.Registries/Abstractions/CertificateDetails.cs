/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Represents information about the Certificate
    /// </summary>
    [Serializable]
    public class CertificateDetails
    {
        // These are used during serialization and deserialization since X509Certificate2 and X509Certificate are no longer serializable in .net core.
        private string _certificateBase64String;

        /// <summary>
        /// The HER-ID of the communication party. This is identifies this party in the Address Registry
        /// </summary>
        public int HerId { get; set; }

        /// <summary>
        /// The certificate of the communication party represented as a byte array.
        /// </summary>
        [field: NonSerialized]
        [JsonIgnore]
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// The complete url for the certificate.
        /// </summary>
        public string LdapUrl { get; set; }

        /// <summary>
        /// Called on serializing the object, exports the certificate to a base64-encoded string.
        /// </summary>
        [OnSerializing]
        internal void OnSerializing(StreamingContext context)
        {
            _certificateBase64String = Certificate == null
                ? null
                : Convert.ToBase64String(Certificate.Export(X509ContentType.Cert));
        }

        /// <summary>
        /// Called when object is deserialized, imports the certificates from the previously serialized base64-encoded string.
        /// </summary>
        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            Certificate = string.IsNullOrWhiteSpace(_certificateBase64String)
                ? null
#if NET9_0_OR_GREATER
                : X509CertificateLoader.LoadCertificate(Convert.FromBase64String(_certificateBase64String));
#else
                : new X509Certificate2(Convert.FromBase64String(_certificateBase64String));
#endif
        }
    }
}
