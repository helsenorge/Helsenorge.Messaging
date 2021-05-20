/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// Specifies type of queue
    /// </summary>
    public enum QueueType
    {
        /// <summary>
        /// Queue used for asynchronous messages
        /// </summary>
        Asynchronous,
        /// <summary>
        /// Queue used for synchronous messages
        /// </summary>
        Synchronous,
        /// <summary>
        /// Queue used for error messages
        /// </summary>
        Error,
        /// <summary>
        /// Queue used for synchronous reply messages
        /// </summary>
        SynchronousReply
    }
}
