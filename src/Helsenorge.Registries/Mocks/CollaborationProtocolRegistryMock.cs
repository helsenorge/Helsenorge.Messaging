using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Helsenorge.Registries.CPAService;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries.Mocks
{
	/// <summary>
	/// Provides a mock implementation of CollaborationProtocolRegistry.
	/// This code exists in this assembly so we don't have to make service reference code publicly available
	/// </summary>
	public class CollaborationProtocolRegistryMock : CollaborationProtocolRegistry
	{
		private Func<int, string> _findProtocolForCounterparty;
		private Func<int, string> _findAgreementForCounterparty;
		private Func<Guid, string> _findAgreementById;
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="cache"></param>
		public CollaborationProtocolRegistryMock(
			CollaborationProtocolRegistrySettings settings,
			IDistributedCache cache) : base(settings, cache)
		{
		}
		/// <summary>
		/// Configures a func to be called when calling the actual method
		/// </summary>
		/// <param name="func"></param>
		public void SetupFindProtocolForCounterparty(Func<int, string> func)
		{
			_findProtocolForCounterparty = func;
		}
		/// <summary>
		/// Configures a func to be called when calling the actual method
		/// </summary>
		/// <param name="func"></param>
		public void SetupFindAgreementById(Func<Guid, string> func)
		{
			_findAgreementById = func;
		}
		/// <summary>
		/// Configures a func to be called when calling the actual method
		/// </summary>
		/// <param name="func"></param>
		public void SetupFindAgreementForCounterparty(Func<int, string> func)
		{
			_findAgreementForCounterparty = func;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="counterpartyHerId"></param>
		/// <returns></returns>
		protected override Task<string> FindProtocolForCounterparty(ILogger logger, int counterpartyHerId)
		{
			return Task.FromResult(_findProtocolForCounterparty(counterpartyHerId));
		}

		internal override Task<CpaXmlDetails> FindAgreementForCounterparty(ILogger logger, int counterpartyHerId)
		{
			var details = new CpaXmlDetails()
			{
				CollaborationProtocolAgreementXml = _findAgreementForCounterparty(counterpartyHerId)
			};
			return Task.FromResult(details);
		}

		internal override Task<CpaXmlDetails> FindAgreementById(ILogger logger, Guid id)
		{
			var details = new CpaXmlDetails()
			{
				CollaborationProtocolAgreementXml = _findAgreementById(id)
			};
			return Task.FromResult(details);
		}
	}
}
