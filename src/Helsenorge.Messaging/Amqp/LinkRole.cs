/*
 * Copyright (c) 2021-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

namespace Helsenorge.Messaging.Amqp
{
    /// <summary>Indicates the the role of the AMQP link.</summary>
    internal enum LinkRole
    {
        /// <summary>The link has the sender role.</summary>
        Sender = 1,
        /// <summary>The link has the receiver role.</summary>
        Receiver = 2,
    }
}
