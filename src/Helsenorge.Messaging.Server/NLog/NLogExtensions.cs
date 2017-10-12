using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;

namespace Helsenorge.Messaging.Server.NLog
{
    internal static class NLogExtensions
    {
        /// <summary>
        /// Enable NLog as logging provider in ASP.NET Core.
        /// </summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static ILoggerFactory AddNLog(this ILoggerFactory factory)
        {
            //ignore this
            //LogManager.AddHiddenAssembly(typeof(AspNetExtensions).GetTypeInfo().Assembly);

            using (var provider = new NLogLoggerProvider())
            {
                factory.AddProvider(provider);
            }
            return factory;
        }
        /// <summary>
        /// Apply NLog configuration from XML config.
        /// </summary>
        /// <param name="fileName">absolute path  NLog configuration file.</param>
        private static void ConfigureNLog(string fileName)
        {
            LogManager.Configuration = new XmlLoggingConfiguration(fileName, true);
        }

    }
}
