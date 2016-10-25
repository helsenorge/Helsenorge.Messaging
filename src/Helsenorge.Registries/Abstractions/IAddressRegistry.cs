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
		/// Gets the public encryption certificate in addition to the LDAP Url.
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="herId">Her-ID of the certificate owner</param>
		/// <returns></returns>
        Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(ILogger logger, int herId);

        /// <summary>
        /// Get the public signature certificate in addition to the LDAP Url.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="herId">Her-ID of the certificate owner</param>
        /// <returns></returns>
        Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(ILogger logger, int herId);
    }
}
