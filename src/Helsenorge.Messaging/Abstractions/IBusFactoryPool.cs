/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    internal interface IBusFactoryPool
    {
        Task<IMessagingFactory> FindNextFactoryAsync(ILogger logger);
        void RegisterAlternateMessagingFactoryAsync(IMessagingFactory factory);
        Task ShutdownAsync(ILogger logger);
        Task<IMessagingMessage> CreateMessageAsync(ILogger logger, Stream stream);
    }
}
