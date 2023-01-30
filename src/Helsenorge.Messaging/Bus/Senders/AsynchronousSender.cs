/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Bus.Senders
{
    /// <summary>
    /// Handles asynchronous sending
    /// </summary>
    internal class AsynchronousSender
    {
        private readonly ServiceBusCore _core;
    
        public AsynchronousSender(ServiceBusCore core)
        {
            _core = core;
        }

        public async Task SendAsync(ILogger logger, OutgoingMessage message)
        {
            await _core.Send(logger, message, QueueType.Asynchronous).ConfigureAwait(false);
        }
    }
}
