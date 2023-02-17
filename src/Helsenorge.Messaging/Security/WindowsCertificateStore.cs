/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Messaging.Security
{
    /// <summary>
    /// 
    /// </summary>
    internal class WindowsCertificateStore : ICertificateStore
    {
        private readonly StoreName? _storeName;
        private readonly StoreLocation? _storeLocation;

        /// <summary>
        /// Retrieves a certificate from the Windows Certificate Store
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="storeLocation"></param>
        public WindowsCertificateStore(StoreName? storeName, StoreLocation? storeLocation)
        {
            _storeName = storeName ?? throw new ArgumentNullException(nameof(storeName));
            _storeLocation = storeLocation ?? throw new ArgumentNullException(nameof(storeLocation));
        }

        /// <summary>
        /// Retrieves the Certificate from Windows Cerificate Store using the thumbprint as identifier
        /// </summary>
        /// <param name="thumbprint">The certificate's thumbprint</param>
        /// <returns>Returns the <seealso cref="System.Security.Cryptography.X509Certificates"/> matching the thumbprint.</returns>
        public X509Certificate2 GetCertificate(object thumbprint)
        {
            if (thumbprint == null) throw new ArgumentNullException(nameof(thumbprint));
            if (!(thumbprint is string)) throw new ArgumentException("Argument is expected to be of type string.", nameof(thumbprint));

            string tp = thumbprint.ToString();
            if (string.IsNullOrWhiteSpace(tp)) throw new ArgumentException($"Argument '{nameof(thumbprint)}' must contain a value.", nameof(thumbprint));

            var store = new X509Store(_storeName.Value, _storeLocation.Value);
            store.Open(OpenFlags.ReadOnly);

            var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, tp, false);
            var enumerator = certCollection.GetEnumerator();
            X509Certificate2 certificate = null;
            while (enumerator.MoveNext())
            {
                certificate = enumerator.Current;
            }
            store.Close();
            
            return certificate;
        }
    }
}
