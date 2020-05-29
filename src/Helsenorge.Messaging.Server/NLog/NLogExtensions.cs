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
