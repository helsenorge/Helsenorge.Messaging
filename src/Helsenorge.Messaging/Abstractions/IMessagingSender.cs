/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Provides an interface for sending a message for a specific implementation
    /// </summary>
    public interface IMessagingSender : ICachedMessagingEntity
    {
        /// <summary>
        /// Sends the message
        /// </summary>
        /// <param name="message">The messag to send</param>
        /// <returns></returns>
        Task SendAsync(IMessagingMessage message);
    }
}
