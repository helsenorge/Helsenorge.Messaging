using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Helsenorge.Messaging.Server.NLog
{
    internal static class NLogExtensions
    {
#if NET46
        public static ILoggerFactory AddNLog(this ILoggerFactory factory)
        {
            using (var provider = new NLogLoggerProvider())
            {
                factory.AddProvider(provider);
            }
            return factory;
        }
#elif NET471
        public static ILoggingBuilder AddNLog(this ILoggingBuilder builder)
        {
            using (var provider = new NLogLoggerProvider())
            {
                builder.AddProvider(provider);
            }                
            return builder;
        }
#endif
    }
}
