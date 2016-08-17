using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helsenorge.Registries.Abstractions;

namespace Helsenorge.Messaging.Abstractions
{
	/// <summary>
	/// Provides an interface for creating IMessageSenders
	/// </summary>
	public interface IMessageSenderFactory
	{
		/// <summary>
		/// Creates a message sender
		/// </summary>
		/// <param name="profile">Collaboration profile of communication party</param>
		/// <returns></returns>
		Task<IMessageSender> CreateAsync(CollaborationProtocolProfile profile);
	}
}
