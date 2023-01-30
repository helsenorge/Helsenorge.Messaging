/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Bus
{
    internal class ServiceBusReceiverPool : MessagingEntityCache<IMessagingReceiver>
    {
        private readonly IBusFactoryPool _factoryPool;
        private readonly int _credit;
        
        public ServiceBusReceiverPool(ServiceBusSettings settings, IBusFactoryPool factoryPool) :
            base("ReceiverPool", settings.MaxReceivers, settings.CacheEntryTimeToLive, settings.MaxCacheEntryTrimCount)
        {
            _factoryPool = factoryPool;
            _credit = settings.LinkCredits;
        }

        protected override async Task<IMessagingReceiver> CreateEntity(ILogger logger, string id)
        {
            var factory = await _factoryPool.FindNextFactory(logger).ConfigureAwait(false);
            return factory.CreateMessageReceiver(id, _credit);
        }

        /// <summary>
        /// Creates a cached message receiver
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task<IMessagingReceiver> CreateCachedMessageReceiver(ILogger logger, string queueName) => await Create(logger, queueName).ConfigureAwait(false);

        /// <summary>
        /// Releases a cached message receiver
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueName"></param>
        public async Task ReleaseCachedMessageReceiver(ILogger logger, string queueName) => await Release(logger, queueName).ConfigureAwait(false);
    }
}
