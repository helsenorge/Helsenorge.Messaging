/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Runtime.Serialization;

namespace Helsenorge.Messaging.Amqp
{
    /// <summary>
    /// Allows application code to send error back to sender
    /// </summary>
    [Serializable]
    public class NotifySenderException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public NotifySenderException() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public NotifySenderException(string message) : base(message) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public NotifySenderException(string message, Exception inner) : base(message, inner) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected NotifySenderException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }

    }
}
