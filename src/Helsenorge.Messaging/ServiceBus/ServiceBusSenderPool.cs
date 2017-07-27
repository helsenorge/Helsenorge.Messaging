using System;
using System.IO;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceBus.Messaging;

namespace Helsenorge.Messaging.ServiceBus
{
	internal class ServiceBusSenderPool : MessagingEntityCache<IMessagingSender>
	{
		private readonly IServiceBusFactoryPool _factoryPool;
		
		public ServiceBusSenderPool(ServiceBusSettings settings,  IServiceBusFactoryPool factoryPool) :
			base("SenderPool", settings.MaxSenders)
		{
			_factoryPool = factoryPool;
		}
		protected override IMessagingSender CreateEntity(ILogger logger, string id)
		{
			var factory = _factoryPool.FindNextFactory(logger);
			return factory.CreateMessageSender(id);
		}

		/// <summary>
		/// Creates a cached messages sender
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="queueName"></param>
		/// <returns></returns>
		public IMessagingSender CreateCachedMessageSender(ILogger logger, string queueName) => Create(logger, queueName);

		/// <summary>
		/// Releases a cached message sender
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="queueName"></param>
		public void ReleaseCachedMessageSender(ILogger logger, string queueName) => Release(logger, queueName);
	}
}
