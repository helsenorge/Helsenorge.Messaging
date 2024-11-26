/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Interface for accessing Collaboration Protocol Registry
    /// </summary>
    public interface ICollaborationProtocolRegistry
    {
        /// <summary>
        /// Finds the collaboration protocol for a communication party
        /// </summary>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> FindProtocolForCounterpartyAsync(int counterpartyHerId);

        /// <summary>
        /// Finds a collaboration agreement based on an id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="myHerId"></param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> FindAgreementByIdAsync(Guid id, int myHerId);

        /// <summary>
        /// Finds a collaboration agreement based on an id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="myHerId"></param>
        /// <param name="forceUpdate">Set to true to force cache update, default value false.</param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> FindAgreementByIdAsync(Guid id, int myHerId, bool forceUpdate);

        /// <summary>
        /// Finds a collaboration agreement based on a communication party
        /// </summary>
        /// <param name="myHerId"></param>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(int myHerId, int counterpartyHerId);

        /// <summary>
        /// Finds a collaboration agreement based on a communication party
        /// </summary>
        /// <param name="myHerId"></param>
        /// <param name="counterpartyHerId"></param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(int myHerId, int counterpartyHerId, bool forceUpdate);

        /// <summary>
        /// Tries to Ping the AddressRegistry Service to verify a connection.
        /// </summary>
        /// <param name="herId">Needs to be a known HER-id</param>
        [Obsolete("This metod will be replaced in the future.")]
        Task PingAsync(int herId);

        /// <summary>
        /// Returns the Collaboration Protocol Profile matching the id argument.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="forceUpdate"></param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> GetCollaborationProtocolProfileAsync(Guid id, bool forceUpdate = false);
    }
}
