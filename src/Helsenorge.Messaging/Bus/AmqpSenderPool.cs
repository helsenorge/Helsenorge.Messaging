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
    internal class AmqpSenderPool : MessagingEntityCache<IMessagingSender>
    {
        private readonly IBusFactoryPool _factoryPool;
        
        public AmqpSenderPool(ServiceBusSettings settings,  IBusFactoryPool factoryPool) :
            base("SenderPool", settings.MaxSenders, settings.CacheEntryTimeToLive, settings.MaxCacheEntryTrimCount)
        {
            _factoryPool = factoryPool;
        }
        protected override async Task<IMessagingSender> CreateEntity(ILogger logger, string id)
        {
            var factory = await _factoryPool.FindNextFactory(logger).ConfigureAwait(false);
            return factory.CreateMessageSender(id);
        }

        /// <summary>
        /// Creates a cached messages sender
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task<IMessagingSender> CreateCachedMessageSender(ILogger logger, string queueName) => await Create(logger, queueName).ConfigureAwait(false);

        /// <summary>
        /// Releases a cached message sender
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueName"></param>
        public async Task ReleaseCachedMessageSender(ILogger logger, string queueName) => await Release(logger, queueName).ConfigureAwait(false);
    }
}
