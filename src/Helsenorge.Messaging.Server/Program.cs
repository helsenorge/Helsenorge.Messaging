using System;
using System.Configuration;
using System.IO;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Server.NLog;
using Helsenorge.Registries;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Helsenorge.Messaging.Server
{
    class Program
    {
        private static ILogger _logger;
        private static ILoggerFactory _loggerFactory;
        private static IMessagingServer _messagingServer;
        private static ServerSettings _serverSettings;

        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.HelpOption("-?|-h|--help");

            var profileArgument = app.Argument("[profile]", "The name of the json profile file to use (excluded file extension)");
            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(profileArgument.Value))
                {
                    app.ShowHelp();
                    return 2;
                }

                Configure(profileArgument.Value);

                _messagingServer.Start();

                string input;
                do
                {
                    Console.WriteLine("Type 'q' to exit.");
                    input = Console.ReadLine();
                }
                while (input != "q");

                _messagingServer.Stop(TimeSpan.FromSeconds(10));
                return 0;
            });

            int exitCode = app.Execute(args);

#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Press any key to continue. . .");
            Console.ReadKey(true);
#endif
            return exitCode;
        }

        private static void Configure(string profile)
        {
            // read configuration values
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"{profile}.json", false);
            var configurationRoot = builder.Build();

            // configure logging
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddConsole(configurationRoot.GetSection("Logging"));
            _loggerFactory.AddNLog();

            LogManager.Configuration = new XmlLoggingConfiguration("nlog.config", true);
            
            _logger = _loggerFactory.CreateLogger("TestServer");

            // configure caching
            var distributedCache = DistributedCacheFactory.Create();

            // set up address registry
            var addressRegistrySettings = new AddressRegistrySettings();
            configurationRoot.GetSection("AddressRegistrySettings").Bind(addressRegistrySettings);
            addressRegistrySettings.WcfConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var addressRegistry = new AddressRegistry(addressRegistrySettings, distributedCache);

            // set up collaboration registry
            var collaborationProtocolRegistrySettings = new CollaborationProtocolRegistrySettings();
            configurationRoot.GetSection("CollaborationProtocolRegistrySettings").Bind(collaborationProtocolRegistrySettings);
            collaborationProtocolRegistrySettings.WcfConfiguration =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var collaborationProtocolRegistry = new CollaborationProtocolRegistry(collaborationProtocolRegistrySettings, distributedCache, addressRegistry);

            _serverSettings = new ServerSettings();
            configurationRoot.GetSection("ServerSettings").Bind(_serverSettings);

            // set up messaging
            var messagingSettings = new MessagingSettings();
            configurationRoot.GetSection("MessagingSettings").Bind(messagingSettings);

            messagingSettings.ServiceBus.Synchronous.ReplyQueueMapping.Add(Environment.MachineName, "DUMMY"); // we just need a value, it will never be used
            messagingSettings.LogPayload = true;

            _messagingServer = new MessagingServer(messagingSettings, _logger, _loggerFactory, collaborationProtocolRegistry, addressRegistry);

            _messagingServer.RegisterAsynchronousMessageReceivedStartingCallback((m) =>
            {
                MappedDiagnosticsLogicalContext.Set("correlationId", m.MessageId);
            });
            _messagingServer.RegisterAsynchronousMessageReceivedCallback((m) =>
            {
                if (m.Payload.ToString().Contains("ThrowException"))
                {
                    throw new InvalidOperationException();
                }

                var path = Path.Combine(_serverSettings.DestinationDirectory, "Asynchronous");
                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }
                var fileName = Path.Combine(path, m.MessageId + ".xml");
                using (var sw = File.CreateText(fileName))
                {
                    m.Payload.Save(sw);
                }
            });
            _messagingServer.RegisterAsynchronousMessageReceivedCompletedCallback((m) =>
            {
                MappedDiagnosticsLogicalContext.Set("correlationId", m.MessageId);
            });

            _messagingServer.RegisterSynchronousMessageReceivedStartingCallback((m) =>
            {
                MappedDiagnosticsLogicalContext.Set("correlationId", string.Empty);// reset correlation id
            });
            _messagingServer.RegisterSynchronousMessageReceivedCallback((m) =>
            {
                var path = Path.Combine(_serverSettings.DestinationDirectory, "Synchronous");
                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }
                var fileName = Path.Combine(path, m.MessageId + ".xml");
                using (var sw = File.CreateText(fileName))
                {
                    m.Payload.Save(sw);
                }
                return new XDocument(new XElement("DummyResponse"));
            });
            _messagingServer.RegisterSynchronousMessageReceivedCompletedCallback((m) =>
            {
                MappedDiagnosticsLogicalContext.Set("correlationId", string.Empty); // reset correlation id
            });
        }
    }

    internal class ServerSettings
    {
        public string DestinationDirectory { get; set; }
    }
}
