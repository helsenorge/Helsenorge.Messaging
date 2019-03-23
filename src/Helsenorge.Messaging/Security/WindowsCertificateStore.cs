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
        /// 
        /// </summary>
        /// <param name="thumbprint"></param>
        /// <returns></returns>
        public X509Certificate2 GetCertificate(string thumbprint)
        {
            if (string.IsNullOrEmpty(thumbprint)) throw new ArgumentException($"Argument '{nameof(thumbprint)}' must contain a value.", nameof(thumbprint));

            var store = new X509Store(_storeName.Value, _storeLocation.Value);
            store.Open(OpenFlags.ReadOnly);

            var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
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
