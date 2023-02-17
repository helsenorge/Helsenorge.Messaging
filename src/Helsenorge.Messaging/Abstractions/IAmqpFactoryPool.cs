/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
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
    internal interface IAmqpFactoryPool
    {
        Task<IAmqpFactory> FindNextFactoryAsync(ILogger logger);
        void RegisterAlternateMessagingFactoryAsync(IAmqpFactory factory);
        Task ShutdownAsync(ILogger logger);
        Task<IAmqpMessage> CreateMessageAsync(ILogger logger, Stream stream);
    }
}
