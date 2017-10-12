using System;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    internal class ServiceBusReceiverPool : MessagingEntityCache<IMessagingReceiver>
    {
        private readonly IServiceBusFactoryPool _factoryPool;
        
        public ServiceBusReceiverPool(ServiceBusSettings settings, IServiceBusFactoryPool factoryPool) :
            base("ReceiverPool", settings.MaxReceivers)
        {
            _factoryPool = factoryPool;
        }

        protected override IMessagingReceiver CreateEntity(ILogger logger, string id)
        {
            var factory = _factoryPool.FindNextFactory(logger);
            return factory.CreateMessageReceiver(id);
        }

        /// <summary>
        /// Creates a cached message receiver
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public IMessagingReceiver CreateCachedMessageReceiver(ILogger logger, string queueName) => Create(logger, queueName);

        /// <summary>
        /// Releases a cached message receiver
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueName"></param>
        public void ReleaseCachedMessageReceiver(ILogger logger, string queueName) => Release(logger, queueName);
    }
}
