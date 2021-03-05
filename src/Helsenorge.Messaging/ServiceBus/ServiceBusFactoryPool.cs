/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Diagnostics.CodeAnalysis;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Helsenorge.Messaging.ServiceBus
{
    internal class ServiceBusFactoryPool : MessagingEntityCache<IMessagingFactory>, IServiceBusFactoryPool
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ServiceBusSettings _settings;
        private int _index;
        private IMessagingFactory _alternateMessagingFactor;

        public ServiceBusFactoryPool(ServiceBusSettings settings) :
            base("FactoryPool", settings.MaxFactories)
        {
            _settings = settings;
        }

        public void RegisterAlternateMessagingFactory(IMessagingFactory alternateMessagingFactory)
        {
            _alternateMessagingFactor = alternateMessagingFactory;
        }

        [ExcludeFromCodeCoverage] // Azure service bus implementation
        protected override Task<IMessagingFactory> CreateEntity(ILogger logger, string id)
        {
            if (_alternateMessagingFactor != null) return Task.FromResult(_alternateMessagingFactor);
            var connection = new ServiceBusConnection(_settings.ConnectionString, _settings.MaxLinksPerSession, _settings.MaxSessionsPerConnection, logger);
            return Task.FromResult<IMessagingFactory>(new ServiceBusFactory(connection, logger));
        }
        public async Task<IMessagingMessage> CreateMessage(ILogger logger, Stream stream)
        {
            var factory = await FindNextFactory(logger).ConfigureAwait(false);
            return await factory.CreateMessage(stream).ConfigureAwait(false);
        }
        public async Task<IMessagingFactory> FindNextFactory(ILogger logger)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // increas value in a round-robin fashion
                _index++;
                if (_index == Capacity)
                {
                    _index = 0;
                }
                var name = string.Format(null, "MessagingFactory{0}", _index);
                return await Create(logger, name).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
