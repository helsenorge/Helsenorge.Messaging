using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
	/// <summary>
	/// Provides an interface for creating IMessageReceivers
	/// </summary>
	public interface IMessageReceiverFactory
	{
		/// <summary>
		/// Creates a new message receiver
		/// </summary>
		/// <param name="herId">Her if of communication party we are receiving from</param>
		/// <returns></returns>
		Task<IMessageReceiver> CreateAsync(int herId);
	}
}
