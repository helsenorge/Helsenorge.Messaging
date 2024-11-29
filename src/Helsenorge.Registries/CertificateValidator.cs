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
        private readonly ILogger _logger;
        private readonly bool _useOnlineRevocationCheck;

        /// <summary>
        /// CertificateValidator constructor
        /// </summary>
        /// <param name="logger">Default logger</param>
        /// <param name="useOnlineRevocationCheck">Should online certificate revocation list be used. Optional, default true.</param>
        public CertificateValidator(ILogger logger, bool useOnlineRevocationCheck = true)
        {
            _logger = logger;
            _useOnlineRevocationCheck = useOnlineRevocationCheck;
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

            var chain = new X509Chain
            {
                ChainPolicy =
                {
                    RevocationMode = _useOnlineRevocationCheck ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                    RevocationFlag = X509RevocationFlag.EntireChain,
                    UrlRetrievalTimeout = TimeSpan.FromSeconds(30),
                    VerificationTime = DateTime.Now,
                }
            };

            using (chain)
            {
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
                    }
                }
                if (result != CertificateErrors.None)
                    _logger.LogWarning($"Certificate chain validation failed. Certificate: {certificate.Subject} Result: {result}");

                return result;
            }
        }
    }
}
