/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Main interface for sending messages
    /// </summary>
    public interface IMessagingClient
    {
        /// <summary>
        /// Send a message and continue with other work (asynchronous messaging)
        /// </summary>
        /// <param name="message">Details about the message being sent</param>
        /// <returns></returns>
        Task SendAndContinueAsync(OutgoingMessage message);

        /// <summary>
        /// Send a message and wait for a reply (synchronous messaging)
        /// </summary>
        /// <param name="message">Details about the message being sent</param>
        /// <returns></returns>
        Task<XDocument> SendAndWaitAsync(OutgoingMessage message);

        /// <summary>
        /// Closes down links, sessions and connections.
        /// </summary>
        Task Close();
    }
}
