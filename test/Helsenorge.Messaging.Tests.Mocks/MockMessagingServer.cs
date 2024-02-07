/*
 * Copyright (c) 2021-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Tests.Mocks
{
    /// <inheritdoc />
    public class MockMessagingServer : MessagingServer
    {
        /// <inheritdoc />
        public MockMessagingServer(MessagingSettings settings, ILoggerFactory loggerFactory, ICollaborationProtocolRegistry collaborationProtocolRegistry, IAddressRegistry addressRegistry)
            : base(settings, loggerFactory, collaborationProtocolRegistry, addressRegistry)
        {
        }

        /// <inheritdoc />
        public MockMessagingServer(MessagingSettings settings, ILoggerFactory loggerFactory, ICollaborationProtocolRegistry collaborationProtocolRegistry, IAddressRegistry addressRegistry, ICertificateStore certificateStore)
            : base(settings, loggerFactory, collaborationProtocolRegistry, addressRegistry, certificateStore)
        {
        }

        /// <inheritdoc />
        public MockMessagingServer(MessagingSettings settings, ILoggerFactory loggerFactory, ICollaborationProtocolRegistry collaborationProtocolRegistry, IAddressRegistry addressRegistry, ICertificateStore certificateStore, ICertificateValidator certificateValidator, IMessageProtection messageProtection)
            : base(settings, loggerFactory, collaborationProtocolRegistry, addressRegistry, certificateStore, certificateValidator, messageProtection)
        {
        }

        /// <inheritdoc />
        protected override Task<bool> CanAuthenticateAgainstMessageBrokerAsync()
        {
            return Task.FromResult(true);
        }
    }
}
