/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Server.NLog;
using Helsenorge.Registries;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
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

            CreateLogger(configurationRoot);

            // configure caching
            var distributedCache = DistributedCacheFactory.Create();

            // set up address registry
            var addressRegistrySettings = new AddressRegistrySettings();
            configurationRoot.GetSection("AddressRegistrySettings").Bind(addressRegistrySettings);
            var addressRegistry = new AddressRegistry(addressRegistrySettings, distributedCache);

            // set up collaboration registry
            var collaborationProtocolRegistrySettings = new CollaborationProtocolRegistrySettings();
            configurationRoot.GetSection("CollaborationProtocolRegistrySettings").Bind(collaborationProtocolRegistrySettings);

            var collaborationProtocolRegistry = new CollaborationProtocolRegistry(collaborationProtocolRegistrySettings, distributedCache, addressRegistry);

            _serverSettings = new ServerSettings();
            configurationRoot.GetSection("ServerSettings").Bind(_serverSettings);

            // set up messaging
            var messagingSettings = new MessagingSettings();
            configurationRoot.GetSection("MessagingSettings").Bind(messagingSettings);

            messagingSettings.ServiceBus.Synchronous.ReplyQueueMapping.Add(Environment.MachineName, "DUMMY"); // we just need a value, it will never be used
            messagingSettings.LogPayload = true;

            _messagingServer = new MessagingServer(messagingSettings, _loggerFactory, collaborationProtocolRegistry, addressRegistry);

            _messagingServer.RegisterAsynchronousMessageReceivedStartingCallbackAsync((listener, message) =>
            {
                MappedDiagnosticsLogicalContext.Set("correlationId", message.MessageId);
                return Task.CompletedTask;
            });
            _messagingServer.RegisterAsynchronousMessageReceivedCallbackAsync(async (m) =>
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
                
                using var sw = File.CreateText(fileName);
                await m.Payload.SaveAsync(sw, SaveOptions.None, CancellationToken.None);
            });
            _messagingServer.RegisterAsynchronousMessageReceivedCompletedCallbackAsync((m) =>
            {
                MappedDiagnosticsLogicalContext.Set("correlationId", m.MessageId);
                return Task.CompletedTask;
            });

            _messagingServer.RegisterSynchronousMessageReceivedStartingCallbackAsync((m) =>
            {
                MappedDiagnosticsLogicalContext.Set("correlationId", string.Empty);// reset correlation id
                return Task.CompletedTask;
            });
            _messagingServer.RegisterSynchronousMessageReceivedCallbackAsync(async (m) =>
            {
                var path = Path.Combine(_serverSettings.DestinationDirectory, "Synchronous");
                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }
                var fileName = Path.Combine(path, m.MessageId + ".xml");
                using (var sw = File.CreateText(fileName))
                {
                    await m.Payload.SaveAsync(sw, SaveOptions.None, CancellationToken.None);
                }
                return new XDocument(new XElement("DummyResponse"));
            });
            _messagingServer.RegisterSynchronousMessageReceivedCompletedCallbackAsync((m) =>
            {
                MappedDiagnosticsLogicalContext.Set("correlationId", string.Empty); // reset correlation id
                return Task.CompletedTask;
            });
        }

        private static void CreateLogger(IConfigurationRoot configurationRoot)
        {
            LogManager.Configuration = new XmlLoggingConfiguration("nlog.config");
            LogManager.ThrowConfigExceptions = true;
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.AddNLog();
            });
            var provider = serviceCollection.BuildServiceProvider();
            _loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger("TestServer");
        }
    }

    internal class ServerSettings
    {
        public string DestinationDirectory { get; set; }
    }
}
