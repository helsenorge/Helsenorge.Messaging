/*
 * Copyright (c) 2021-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

namespace Helsenorge.Messaging.Amqp
{
    /// <summary>The kind of Message Broker Dialect.</summary>
    public enum MessageBrokerDialect
    {
        /// <summary>The Message Broker Dialect is MS ServiceBus</summary>
        ServiceBus = 1,
        /// <summary>The Message Broker Dialect is RabbitMQ</summary>
        RabbitMQ = 2,
    }
}
