/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Amqp;
using System;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Amqp
{
    internal class AzureCompatibleMessageReceiver
    {
        private readonly ReceiverLink _receiver;

        public AzureCompatibleMessageReceiver(Session session, string id)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _receiver = new ReceiverLink(session, $"receiver-link-{Guid.NewGuid()}", id);
        }

        public async Task CloseAsync()
        {
            await _receiver.CloseAsync().ConfigureAwait(false);
        }
    }
}
