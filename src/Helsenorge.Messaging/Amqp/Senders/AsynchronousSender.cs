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

namespace Helsenorge.Messaging.Amqp.Senders
{
    /// <summary>
    /// Handles asynchronous sending
    /// </summary>
    internal class AsynchronousSender
    {
        private readonly AmqpCore _amqpCore;
    
        public AsynchronousSender(AmqpCore amqpCore)
        {
            _amqpCore = amqpCore;
        }

        public async Task SendAsync(ILogger logger, OutgoingMessage message)
        {
            await _amqpCore.SendAsync(logger, message, QueueType.Asynchronous).ConfigureAwait(false);
        }
    }
}
