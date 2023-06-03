/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Helsenorge.Registries.AddressService;
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
        /// <param name="herId">Her id of detail owner</param>
        /// <returns></returns>
        Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId);

        /// <summary>
        /// Finds communication details
        /// </summary>
        /// <param name="herId">Her id of detail owner</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId, bool forceUpdate);

        /// <summary>
        /// Gets the public encryption certificate in addition to the LDAP Url.
        /// </summary>
        /// <param name="herId">Her-ID of the certificate owner</param>
        /// <returns></returns>
        Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId);

        /// <summary>
        /// Returns encryption ceritficate for a specific communcation party.
        /// </summary>
        /// <param name="herId">Her-ID of the communication party</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId, bool forceUpdate);

        /// <summary>
        /// Get the public signature certificate in addition to the LDAP Url.
        /// </summary>
        /// <param name="herId">Her-ID of the certificate owner</param>
        /// <returns></returns>
        Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId);

        /// <summary>
        /// Get the public signature certificate in addition to the LDAP Url.
        /// </summary>
        /// <param name="herId">Her-ID of the certificate owner</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId, bool forceUpdate);

        /// <summary>
        /// Tries to Ping the AddressRegistry Service to verify a connection.
        /// </summary>
        Task PingAsync();

        /// <summary>
        /// Searches for Communication Parties using organization number or HER-id.
        /// </summary>
        /// <param name="id">HER-id or Organization number.</param>
        /// <param name="forceUpdate">Set to true to force an update of the cache.</param>
        /// <returns>Returns a list of CommunicationPartyDetails.</returns>
        Task<IEnumerable<CommunicationPartyDetails>> SearchByIdAsync(string id, bool forceUpdate = false);

        /// <summary>
        /// Returns information about an organization.
        /// </summary>
        /// <param name="herId">HER-id of the organization.</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        Task<OrganizationDetails> GetOrganizationDetailsAsync(int herId, bool forceUpdate = false);
    }
}
