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
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Default implementation of <see cref="ICertificateValidator"/>
    /// </summary>
    public class CertificateValidator : ICertificateValidator
    {
        private static readonly TimeSpan DefaultUrlRetrievalTimeout = TimeSpan.FromSeconds(30);

        private readonly ILogger _logger;
        private readonly bool _useOnlineRevocationCheck;
        private readonly TimeSpan _urlRetrievalTimeout;

        /// <summary>
        /// CertificateValidator constructor
        /// </summary>
        /// <param name="logger">Default logger</param>
        /// <param name="useOnlineRevocationCheck">Should online certificate revocation list be used. Optional, default true.</param>
        /// <param name="urlRetrievalTimeout">Timeout used when retrieving the certificate revocation list online. Optional, default 30 seconds.</param>
        public CertificateValidator(ILogger logger, bool useOnlineRevocationCheck = true, TimeSpan? urlRetrievalTimeout = null)
        {
            _logger = logger;
            _useOnlineRevocationCheck = useOnlineRevocationCheck;
            _urlRetrievalTimeout = urlRetrievalTimeout ?? DefaultUrlRetrievalTimeout;
        }

        /// <inheritdoc/>
        public CertificateErrors Validate(X509Certificate2 certificate, X509KeyUsageFlags usage)
            => Validate(certificate, usage, null);

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public CertificateErrors Validate(X509Certificate2 certificate, X509KeyUsageFlags usage, X509Certificate2Collection separateValidationCerts)
        {
            if (certificate == null) return CertificateErrors.Missing;

            var result = CertificateErrors.None;

            if (DateTime.UtcNow < certificate.NotBefore.ToUniversalTime())
            {
                result |= CertificateErrors.StartDate;
            }
            if (DateTime.UtcNow > certificate.NotAfter.ToUniversalTime())
            {
                result |= CertificateErrors.EndDate;
            }

            if (!certificate.HasKeyUsage(usage))
                result |= CertificateErrors.Usage;

            var chain = new X509Chain
            {
                ChainPolicy =
                {
                    RevocationMode = _useOnlineRevocationCheck ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                    RevocationFlag = X509RevocationFlag.EntireChain,
                    UrlRetrievalTimeout = _urlRetrievalTimeout,
                    VerificationTime = DateTime.Now,
                }
            };

            using (chain)
            {
                // User passed other certs to validate against, use these instead of default store
                if (separateValidationCerts != null && separateValidationCerts.Count > 0)
                {
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust; // Different root in use, need to change TrustMode
                    chain.ChainPolicy.CustomTrustStore.AddRange(separateValidationCerts);
                    chain.ChainPolicy.ExtraStore.AddRange(separateValidationCerts); // Precaution, in case some certs are intermediate
                }
                
                if (chain.Build(certificate)) return result;

                foreach (var status in chain.ChainStatus)
                {
                    _logger.LogInformation("Certificate chain validation. " +
                                       $"Status Information: {status.StatusInformation} Status Flag: {status.Status}");
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
                        case X509ChainStatusFlags.UntrustedRoot:
                            result |= CertificateErrors.UntrustedRoot;
                            break;
                        case X509ChainStatusFlags.PartialChain:
                            result |= CertificateErrors.PartialChain;
                            break;
                    }
                }
                if (result != CertificateErrors.None)
                    _logger.LogWarning($"Certificate chain validation failed. Certificate: {certificate.Subject} Result: {result}");

                return result;
            }
        }
    }
}
