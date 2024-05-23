/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.Utilities;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Default implementation of <see cref="ICertificateValidator"/>
    /// </summary>
    public class CertificateValidator : ICertificateValidator
    {
        private readonly bool _useOnlineRevocationCheck;
        private readonly IX509Chain _chain;

        /// <summary>
        /// CertificateValidator constructor
        /// </summary>
        /// <param name="useOnlineRevocationCheck">Should online certificate revocation list be used. Optional, default true.</param>
        public CertificateValidator(bool useOnlineRevocationCheck = true)
        {
            _useOnlineRevocationCheck = useOnlineRevocationCheck;
            _chain = new X509ChainWrapper();
        }

        /// <summary>
        /// CertificateValidator constructor
        /// </summary>
        /// <param name="chain">You can set your own X509Chain.</param>
        /// <param name="useOnlineRevocationCheck">Should online certificate revocation list be used. Optional, default true.</param>
        internal CertificateValidator(IX509Chain chain, bool useOnlineRevocationCheck = true)
        {
            _useOnlineRevocationCheck = useOnlineRevocationCheck;
            _chain = chain;
        }

        /// <summary>
        /// Validates the provided certificate
        /// </summary>
        /// <param name="certificate">The certificate to validate</param>
        /// <param name="usage">The type of usage the certificate is specified for</param>
        /// <returns>A bitcoded status indicating if the certificate is valid or not</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public CertificateErrors Validate(X509Certificate2 certificate, X509KeyUsageFlags usage)
        {
            if (certificate == null) return CertificateErrors.Missing;

            var result = CertificateErrors.None;

            if (DateTime.Now < certificate.NotBefore)
            {
                result |= CertificateErrors.StartDate;
            }
            if (DateTime.Now > certificate.NotAfter)
            {
                result |= CertificateErrors.EndDate;
            }

            if (!certificate.HasKeyUsage(usage))
                result |= CertificateErrors.Usage;


            _chain.ChainPolicy.RevocationMode = _useOnlineRevocationCheck ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
            _chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
            _chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(30);
            _chain.ChainPolicy.VerificationTime = DateTime.Now;

            using (_chain)
            {
                if (_chain.Build(certificate)) return result;

                foreach (var status in _chain.ChainStatus)
                {
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (status.Status)
                    {
                        case X509ChainStatusFlags.OfflineRevocation:
                            result |= CertificateErrors.RevokedOffline;
                            break;
                        case X509ChainStatusFlags.RevocationStatusUnknown:
                            result |= CertificateErrors.RevokedUnknown;
                            break;
                        case X509ChainStatusFlags.Revoked:
                            result |= CertificateErrors.Revoked;
                            break;
                    }
                }
                return result;
            }
        }
    }
}
