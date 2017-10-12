using System;
using System.Diagnostics.CodeAnalysis;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.IO;

namespace Helsenorge.Messaging.ServiceBus
{
    internal class ServiceBusFactoryPool : MessagingEntityCache<IMessagingFactory>, IServiceBusFactoryPool
    {
        private readonly ServiceBusSettings _settings;
        private readonly object _lock= new object();
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
        protected override IMessagingFactory CreateEntity(ILogger logger, string id)
        {
            if (_alternateMessagingFactor != null) return _alternateMessagingFactor;

            var factory = MessagingFactory.CreateFromConnectionString(_settings.ConnectionString);
            factory.RetryPolicy = RetryPolicy.Default;
            return new ServiceBusFactory(factory);
        }
        public IMessagingMessage CreateMessage(ILogger logger, Stream stream, OutgoingMessage outgoingMessage)
        {
            var factory = FindNextFactory(logger);
            return factory.CreteMessage(stream, outgoingMessage);
        }
        public IMessagingFactory FindNextFactory(ILogger logger)
        {
            lock (_lock)
            {
                // increas value in a round-robin fashion
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
