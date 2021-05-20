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
    internal interface IServiceBusFactoryPool
    {
        Task<IMessagingFactory> FindNextFactory(ILogger logger);
        void RegisterAlternateMessagingFactory(IMessagingFactory factory);
        Task Shutdown(ILogger logger);
        Task<IMessagingMessage> CreateMessage(ILogger logger, Stream stream);
    }
}
