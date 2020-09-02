using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Amqp;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus.Exceptions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    [ExcludeFromCodeCoverage]
    internal class ServiceBusSender : CachedAmpqSessionEntity<SenderLink>, IMessagingSender
    {
        private readonly string _id;
        private readonly ILogger _logger;

        public ServiceBusSender(ServiceBusConnection connection, string id, ILogger logger) : base(connection)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }
            _id = id;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override SenderLink CreateLink(ISession session)
        {
            return session.CreateSender($"sender-link-{Guid.NewGuid()}", Connection.GetEntityName(_id)) as SenderLink;
        }

        public async Task SendAsync(IMessagingMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!(message.OriginalObject is Message originalMessage))
            {
                throw new InvalidOperationException("OriginalObject is not a Message");
            }

            await new ServiceBusOperationBuilder(_logger, "Send").Build(async () =>
            {
                EnsureOpen();
                await _link.SendAsync(originalMessage).ConfigureAwait(false);
            }).PerformAsync().ConfigureAwait(false);
        }
    }
}
