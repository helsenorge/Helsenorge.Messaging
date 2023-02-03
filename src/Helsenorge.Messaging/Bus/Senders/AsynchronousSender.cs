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
        private readonly BusCore _busCore;
    
        public AsynchronousSender(BusCore busCore)
        {
            _busCore = busCore;
        }

        public async Task SendAsync(ILogger logger, OutgoingMessage message)
        {
            await _busCore.SendAsync(logger, message, QueueType.Asynchronous).ConfigureAwait(false);
        }
    }
}
