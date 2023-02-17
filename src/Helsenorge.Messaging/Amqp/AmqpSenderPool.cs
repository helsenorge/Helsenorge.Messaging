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
    internal class AmqpSenderPool : AmqpEntityCache<IAmqpSender>
    {
        private readonly IAmqpFactoryPool _factoryPool;
        
        public AmqpSenderPool(BusSettings settings,  IAmqpFactoryPool factoryPool) :
            base("SenderPool", settings.MaxSenders, settings.CacheEntryTimeToLive, settings.MaxCacheEntryTrimCount)
        {
            _factoryPool = factoryPool;
        }
        protected override async Task<IAmqpSender> CreateEntityAsync(ILogger logger, string id)
        {
            var factory = await _factoryPool.FindNextFactoryAsync(logger).ConfigureAwait(false);
            return factory.CreateMessageSender(id);
        }

        /// <summary>
        /// Creates a cached messages sender
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public async Task<IAmqpSender> CreateCachedMessageSenderAsync(ILogger logger, string queueName) => await CreateAsync(logger, queueName).ConfigureAwait(false);

        /// <summary>
        /// Releases a cached message sender
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queueName"></param>
        public async Task ReleaseCachedMessageSenderAsync(ILogger logger, string queueName) => await ReleaseAsync(logger, queueName).ConfigureAwait(false);
    }
}
