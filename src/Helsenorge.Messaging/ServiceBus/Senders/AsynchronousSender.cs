using System;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus.Senders
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
