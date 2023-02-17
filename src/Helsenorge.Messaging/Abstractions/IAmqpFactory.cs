/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.IO;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Provides an interface for creating messaging entities
    /// </summary>
    public interface IAmqpFactory : ICachedMessagingEntity
    {
        /// <summary>
        /// Creates a receiver
        /// </summary>
        /// <param name="id">Id representing the receiver</param>
        /// <param name="credit">Let's you set the link-credit for the receiver link</param>
        /// <returns></returns>
        IAmqpReceiver CreateMessageReceiver(string id, int credit);
        /// <summary>
        /// Creates a sender
        /// </summary>
        /// <param name="id">Id representing the receiver</param>
        /// <returns></returns>
        IAmqpSender CreateMessageSender(string id);
        /// <summary>
        /// Creates an empty message
        /// </summary>
        /// <param name="stream">Stream containing the information</param>
        /// <returns></returns>
        Task<IAmqpMessage> CreateMessageAsync(Stream stream);
    }
}
