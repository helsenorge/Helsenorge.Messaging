/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Amqp
{
    internal class AmqpReceiverPool : AmqpEntityCache<IAmqpReceiver>
    {
        private readonly IAmqpFactoryPool _factoryPool;
        private readonly int _credit;
        
        public AmqpReceiverPool(AmqpSettings settings, IAmqpFactoryPool factoryPool) :
            base("ReceiverPool", settings.MaxReceivers, settings.CacheEntryTimeToLive, settings.MaxCacheEntryTrimCount)
        {
            _factoryPool = factoryPool;
            _credit = settings.LinkCredits;
        }

        protected override async Task<IAmqpReceiver> CreateEntityAsync(ILogger logger, string id)
        {
            var factory = await _factoryPool.FindNextFactoryAsync(logger).ConfigureAwait(false);
            return factory.CreateMessageReceiver(id, _credit);
        }

        /// <summary>
        /// Creates a cached message receiver
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task<IAmqpReceiver> CreateCachedMessageReceiverAsync(ILogger logger, string queueName) => await CreateAsync(logger, queueName).ConfigureAwait(false);

        /// <summary>
        /// Releases a cached message receiver
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueName"></param>
        public async Task ReleaseCachedMessageReceiverAsync(ILogger logger, string queueName) => await ReleaseAsync(logger, queueName).ConfigureAwait(false);
    }
}
