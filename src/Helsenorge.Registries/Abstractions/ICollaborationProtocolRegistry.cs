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
	}
}
