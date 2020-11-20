/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
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
