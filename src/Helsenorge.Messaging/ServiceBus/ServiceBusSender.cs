/*
 * Copyright (c) 2020-2021, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
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
        private readonly Dictionary<string,string> _systemInfoProperties;

        public ServiceBusSender(ServiceBusConnection connection, string id, ILogger logger) : base(connection)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }
            _id = id;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _name = $"sender-link-{Guid.NewGuid()}";
            _systemInfoProperties = GetSystemInfoProperties(connection);
        }

        public string Name => _name;

        protected override SenderLink CreateLink(ISession session)
        {
            return session.CreateSender(Name, Connection.GetEntityName(_id, LinkRole.Sender)) as SenderLink;
        }

        public void Send(IMessagingMessage message)
            => Send(message, TimeSpan.FromMilliseconds(ServiceBusSettings.DefaultTimeoutInMilliseconds));

        public void Send(IMessagingMessage message, TimeSpan serverWaitTime)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (!(message.OriginalObject is Message originalMessage))
                throw new InvalidOperationException("OriginalObject is not a Message");

            new ServiceBusOperationBuilder(_logger, "Send").Build(() =>
            {
                EnsureOpen();
                AddSystemHeaders(originalMessage.ApplicationProperties);
                _link.Send(originalMessage, serverWaitTime);
            }).Perform();
        }

        public Task SendAsync(IMessagingMessage message)
            => SendAsync(message, TimeSpan.FromMilliseconds(ServiceBusSettings.DefaultTimeoutInMilliseconds));

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

            await new ServiceBusOperationBuilder(_logger, "SendAsync").Build(async () =>
            {
                await EnsureOpenAsync().ConfigureAwait(false);
                AddSystemHeaders(originalMessage.ApplicationProperties);
                await _link.SendAsync(originalMessage).ConfigureAwait(false);
            }).PerformAsync().ConfigureAwait(false);
        }

        private void AddSystemHeaders(ApplicationProperties applicationProperties)
        {
            foreach (var systemInfoProperty in _systemInfoProperties)
            {
                applicationProperties[systemInfoProperty.Key] = systemInfoProperty.Value;
            }
        }

        private static Dictionary<string, string> GetSystemInfoProperties(ServiceBusConnection connection)
        {
            var systemInfoProperties = new Dictionary<string, string>();
            try
            {
                systemInfoProperties.Add("x-system-identifier", connection.SystemIdent);

                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                systemInfoProperties.Add("x-system-assembly", assemblyName.ToString());
                systemInfoProperties.Add("x-system-assembly-version", assemblyName.Version.ToString());

                var frameworkName = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
                systemInfoProperties.Add("x-system-framework", frameworkName);

                var hostname = Dns.GetHostName();
                systemInfoProperties["x-system-hostname"] = hostname;
            }
            catch (Exception)
            {
                // ignored, properties are not yet required
            }

            return systemInfoProperties;
        }
    }
}
