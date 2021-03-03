/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    [ExcludeFromCodeCoverage]
    internal class ServiceBusSender : CachedAmpqSessionEntity<SenderLink>, IMessagingSender
    {
        private readonly string _id;
        private readonly ILogger _logger;
        private readonly string _name;

        public ServiceBusSender(ServiceBusConnection connection, string id, ILogger logger) : base(connection)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }
            _id = id;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _name = $"sender-link-{Guid.NewGuid()}";
        }

        public string Name => _name;

        protected override SenderLink CreateLink(ISession session)
        {
            return session.CreateSender(Name, Connection.GetEntityName(_id)) as SenderLink;
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
                await EnsureOpen();
                await _link.SendAsync(originalMessage);
            }).PerformAsync().ConfigureAwait(false);
        }
    }
}
