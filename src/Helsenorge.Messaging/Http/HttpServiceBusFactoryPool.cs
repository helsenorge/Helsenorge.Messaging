using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Http
{
    class HttpServiceBusFactoryPool : MessagingEntityCache<IMessagingFactory>, IServiceBusFactoryPool
    {
        private readonly ServiceBusSettings _settings;
        private readonly object _lock = new object();
        private int _index;
        private IMessagingFactory _alternateMessagingFactor;

        public HttpServiceBusFactoryPool(ServiceBusSettings settings) :
			base("FactoryPool", settings.MaxFactories)
		{
            _settings = settings;
        }

        public void RegisterAlternateMessagingFactory(IMessagingFactory alternateMessagingFactory)
        {
            _alternateMessagingFactor = alternateMessagingFactory;
        }

        protected override IMessagingFactory CreateEntity(ILogger logger, string id)
        {
            if (_alternateMessagingFactor != null) return _alternateMessagingFactor;

            return new HttpServiceBusFactory(_settings.ConnectionString);
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
