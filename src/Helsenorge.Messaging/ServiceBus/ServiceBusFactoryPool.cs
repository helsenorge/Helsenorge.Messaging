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
using Amqp;

namespace Helsenorge.Messaging.ServiceBus
{
    internal class ServiceBusFactoryPool : MessagingEntityCache<IMessagingFactory>, IServiceBusFactoryPool
    {
        private readonly ServiceBusSettings _settings;
        private readonly object _lock= new object();
        private int _index = -1;
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
        protected override IMessagingFactory CreateEntity(ILogger logger, string id)
        {
            if (_alternateMessagingFactor != null) return _alternateMessagingFactor;
            var connection = new ServiceBusConnection(_settings.ConnectionString, _settings.MaxLinksPerSession, _settings.MaxSessionsPerConnection, logger);
            return new ServiceBusFactory(connection, logger);
        }
        public IMessagingMessage CreateMessage(ILogger logger, Stream stream)
        {
            var factory = FindNextFactory(logger);
            return factory.CreateMessage(stream);
        }
        public IMessagingFactory FindNextFactory(ILogger logger)
        {
            lock (_lock)
            {
                // increase value in a round-robin fashion
                _index++;
                if (_index == Capacity)
                {
                    _index = 0;
                }
                var name = string.Format(null, "MessagingFactory{0}", _index);
                return Create(logger, name);
            }
        }
    }
}
