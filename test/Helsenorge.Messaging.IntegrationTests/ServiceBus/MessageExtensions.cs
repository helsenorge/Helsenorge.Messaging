/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Amqp;
using System.Text;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    internal static class MessageExtensions
    {
        internal static string GetBodyAsString(this Message message)
        {
            return Encoding.UTF8.GetString(message.GetBody<byte[]>());
        }
    }
}
