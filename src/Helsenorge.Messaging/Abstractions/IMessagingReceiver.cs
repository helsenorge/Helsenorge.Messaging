using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Provides an interface for receiving messages for a specific implementation
    /// </summary>
    public interface IMessagingReceiver : ICachedMessagingEntity
    {
        /// <summary>
        /// Receives a message
        /// </summary>
        /// <param name="serverWaitTime">Timeout applied to receive operation</param>
        /// <returns></returns>
        Task<IMessagingMessage> ReceiveAsync(TimeSpan serverWaitTime);
    }
}
