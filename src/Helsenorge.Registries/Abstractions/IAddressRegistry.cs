/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Interface for accessing the address registry
    /// </summary>
    public interface IAddressRegistry
    {
        /// <summary>
        /// Finds communication details
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her id of detail owner</param>
        /// <returns></returns>
        Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(ILogger logger, int herId);

        /// <summary>
        /// Finds communication details
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her id of detail owner</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(ILogger logger, int herId, bool forceUpdate);

        /// <summary>
        /// Gets the public encryption certificate in addition to the LDAP Url.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the certificate owner</param>
        /// <returns></returns>
        Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(ILogger logger, int herId);

        /// <summary>
        /// Returns encryption ceritficate for a specific communcation party.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(ILogger logger, int herId, bool forceUpdate);

        /// <summary>
        /// Get the public signature certificate in addition to the LDAP Url.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the certificate owner</param>
        /// <returns></returns>
        Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(ILogger logger, int herId);

        /// <summary>
        /// Get the public signature certificate in addition to the LDAP Url.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the certificate owner</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(ILogger logger, int herId, bool forceUpdate);

        /// <summary>
        /// Tries to Ping the AddressRegistry Service to verify a connection.
        /// </summary>
        /// <param name="logger">An ILogger object that will be used for logging.</param>
        Task PingAsync(ILogger logger);
    }
}
