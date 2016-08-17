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
	}
}
