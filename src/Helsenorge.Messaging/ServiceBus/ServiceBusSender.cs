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
    internal class ServiceBusSender : CachedAmpqSessionEntity, IMessagingSender
    {
        private readonly string _id;
        private readonly ILogger _logger;
        private SenderLink _link;

        public ServiceBusSender(ServiceBusConnection connection, string id, ILogger logger) : base(connection)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }
            _id = id;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected void EnsureLinkOpen()
        {
            EnsureOpen();
            if(_link == null || _link.IsClosed)
            {
                if(_session != null)
                {
                    _session.Close(TimeSpan.Zero);
                }
                _session = new Session(Connection.Connection);
                OnSessionCreated(_session, Connection.Namespace);
            }
        }

        protected override void OnSessionCreated(Session session, string ns)
        {
            _link = new SenderLink(session, "sender-link", ServiceBusConnection.GetEntityName(_id, ns));
        }

        protected override void OnSessionClosing()
        {
            _link.Close();
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
                EnsureLinkOpen(); //EnsureOpen();
                await _link.SendAsync(originalMessage).ConfigureAwait(false);
            }).PerformAsync().ConfigureAwait(false);
        }
    }
}
