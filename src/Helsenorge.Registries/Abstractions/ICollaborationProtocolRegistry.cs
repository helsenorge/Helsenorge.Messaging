/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
        /// <param name="logger"></param>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> FindProtocolForCounterpartyAsync(ILogger logger, int counterpartyHerId);

        /// <summary>
        /// Finds a collaboration agreement based on an id
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> FindAgreementByIdAsync(ILogger logger, Guid id);

        /// <summary>
        /// Finds a collaboration agreement based on an id
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="id"></param>
        /// <param name="forceUpdate">Set to true to force cache update, default value false.</param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> FindAgreementByIdAsync(ILogger logger, Guid id, bool forceUpdate);

        /// <summary>
        /// Finds a collaboration agreement based on a communication party
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(ILogger logger, int counterpartyHerId);

        /// <summary>
        /// Finds a collaboration agreement based on a communication party
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="counterpartyHerId"></param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(ILogger logger, int counterpartyHerId, bool forceUpdate);

        /// <summary>
        /// Tries to Ping the AddressRegistry Service to verify a connection.
        /// </summary>
        /// <param name="logger">An ILogger object that will be used for logging.</param>
        [Obsolete("This metod will be replaced in the future.")]
        Task PingAsync(ILogger logger, int herId);
    }
}
