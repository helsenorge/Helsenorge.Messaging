/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Helsenorge.Messaging.Server.NLog
{
    internal static class NLogExtensions
    {
        public static ILoggingBuilder AddNLog(this ILoggingBuilder builder)
        {
            using (var provider = new NLogLoggerProvider())
            {
                builder.AddProvider(provider);
            }
            return builder;
        }
    }
}
