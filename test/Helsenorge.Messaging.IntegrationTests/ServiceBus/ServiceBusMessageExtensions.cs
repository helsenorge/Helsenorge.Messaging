/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using System.IO;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    internal static class ServiceBusMessageExtensions
    {
        public static async Task<string> GetBodyAsStingAsync(this IMessagingMessage message)
        {
            using (var streamReader = new StreamReader(message.GetBody()))
            {
                return await streamReader.ReadToEndAsync();
            }
        }
    }
}
