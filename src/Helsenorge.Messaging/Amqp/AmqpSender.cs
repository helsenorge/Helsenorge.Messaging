/* 
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Amqp;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Amqp
{
    [ExcludeFromCodeCoverage]
    internal class AmqpSender : CachedAmpqSessionEntity<SenderLink>, IMessagingSender
    {
        private readonly ILogger _logger;
        private readonly string _id;
        private readonly IDictionary<string, object> _applicationProperties;
        private readonly string _name;

        public AmqpSender(ILogger logger, AmqpConnection connection, string id, IDictionary<string, object> applicationProperties = null) : base(connection)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }
            _id = id;
            _applicationProperties = applicationProperties;

            _name = $"sender-link-{Guid.NewGuid()}";
        }

        public string Name => _name;

        protected override SenderLink CreateLink(ISession session)
        {
            return session.CreateSender(Name, Connection.GetEntityName(_id, LinkRole.Sender)) as SenderLink;
        }

        public void Send(IMessagingMessage message)
            => Send(message, TimeSpan.FromMilliseconds(BusSettings.DefaultTimeoutInMilliseconds));

        public void Send(IMessagingMessage message, TimeSpan serverWaitTime)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (!(message.OriginalObject is Message originalMessage))
                throw new InvalidOperationException("OriginalObject is not a Message");

            new BusOperationBuilder(_logger, "Send").Build(() =>
            {
                EnsureOpen();
                originalMessage.ApplicationProperties.AddApplicationProperties(_applicationProperties);
                originalMessage.ApplicationProperties.Map.Add(BusCore.EnqueuedTimeUtc, DateTime.UtcNow);
                _link.Send(originalMessage, serverWaitTime);
            }).Perform();
        }

        public Task SendAsync(IMessagingMessage message)
            => SendAsync(message, TimeSpan.FromMilliseconds(BusSettings.DefaultTimeoutInMilliseconds));

        public async Task SendAsync(IMessagingMessage message, TimeSpan serverWaitTime)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!(message.OriginalObject is Message originalMessage))
            {
                throw new InvalidOperationException("OriginalObject is not a Message");
            }

            await new BusOperationBuilder(_logger, "SendAsync").Build(async () =>
            {
                await EnsureOpenAsync().ConfigureAwait(false);
                originalMessage.ApplicationProperties.AddApplicationProperties(_applicationProperties);
                originalMessage.ApplicationProperties.Map.Add(BusCore.EnqueuedTimeUtc, DateTime.UtcNow);
                await _link.SendAsync(originalMessage).ConfigureAwait(false);
            }).PerformAsync().ConfigureAwait(false);
        }
    }
}
