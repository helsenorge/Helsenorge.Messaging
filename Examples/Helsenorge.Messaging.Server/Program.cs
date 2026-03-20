/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using HelseId.Library;
using HelseId.Library.ClientCredentials;
using HelseId.Library.Configuration;
using HelseId.Library.Models.DetailsFromClient;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Server.NLog;
using Helsenorge.Registries;
using Helsenorge.Registries.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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
        private static IMessagingServer _messagingServer;
        private static ServerSettings _serverSettings;

        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.HelpOption("-?|-h|--help");

            var profileArgument = app.Argument("[profile]", "The name of the json profile file to use (excluded file extension)");
            app.OnExecuteAsync(async (cancellationToken) =>
            {
                if (string.IsNullOrEmpty(profileArgument.Value))
                {
                    app.ShowHelp();
                    return 2;
                }

                Configure(profileArgument.Value);

                await _messagingServer.StartAsync();

                string input;
                do
                {
                    Console.WriteLine("Type 'q' to exit.");
                    input = Console.ReadLine();
                }
                while (input != "q");

                await _messagingServer.StopAsync(TimeSpan.FromSeconds(10));
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
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"{profile}.json", false);
            var configurationRoot = builder.Build();

            var serviceCollection = new ServiceCollection();

            CreateLogger(serviceCollection);

            // configure caching
            var distributedCache = DistributedCacheFactory.Create();

            // set up address registry
            var addressRegistrySettings = new AddressRegistrySettings();
            configurationRoot.GetSection("AddressRegistrySettings").Bind(addressRegistrySettings);
            var addressRegistry = new AddressRegistry(addressRegistrySettings, distributedCache, _logger);

            // set up collaboration rest registry
            var collaborationProtocolRegistryRestSettings = new CollaborationProtocolRegistryRestSettings();
            configurationRoot.GetSection("CollaborationProtocolRegistryRestSettings").Bind(collaborationProtocolRegistryRestSettings);

            // set up HelseIdClient
            var helseidConfiguratrion = HelseIdConfiguration.ConfigurationFromAppSettings(configurationRoot.GetSection("HelseIdConfiguration"));

            //setup OrganizationNumbers according to your tenant style. This is a tipycall single-tenant org
            var organizationNumbers = new OrganizationNumbers();

            serviceCollection.AddSingleton(addressRegistrySettings);
            serviceCollection.AddSingleton(collaborationProtocolRegistryRestSettings);
            serviceCollection.AddSingleton(organizationNumbers);
            serviceCollection.AddSingleton(distributedCache);
            serviceCollection.AddSingleton<IAddressRegistry>(addressRegistry);

            //See HelseId.Library for other ways to register Auth
            var provider = new SecurityKeyProvider();
            var jsonWebKey = provider.GetSecurityKey() as JsonWebKey;

            var helseIdBuilder = serviceCollection.AddHelseIdClientCredentials(helseidConfiguratrion)
                .AddHelseIdDistributedCaching() //See HelseId.Library.Interfaces.Caching.ITokenCache for other options
                .AddSigningCredentialForClientAuthentication(new SigningCredentials(jsonWebKey, jsonWebKey.Alg));

            serviceCollection.AddSingleton<ICollaborationProtocolRegistry, CollaborationProtocolRegistryRest>();

            // Register a service that will call HelseID
            helseIdBuilder.Services.AddHttpClient();

            _serverSettings = new ServerSettings();
            configurationRoot.GetSection("ServerSettings").Bind(_serverSettings);

            // set up messaging
            var messagingSettings = new MessagingSettings();
            configurationRoot.GetSection("MessagingSettings").Bind(messagingSettings);

            messagingSettings.AmqpSettings.Synchronous.ReplyQueueMapping.Add(Environment.MachineName, "DUMMY"); // we just need a value, it will never be used
            messagingSettings.LogPayload = true;

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger("TestServer");

            _messagingServer = new MessagingServer(messagingSettings, loggerFactory, serviceProvider.GetRequiredService<ICollaborationProtocolRegistry>(), addressRegistry);

            const string propKeyCorrelationId = "correlationId";
            _messagingServer.RegisterAsynchronousMessageReceivedStartingCallbackAsync((listener, message) =>
            {
                ScopeContext.PushProperty(propKeyCorrelationId, message.MessageId);
                return Task.CompletedTask;
            });
            _messagingServer.RegisterAsynchronousMessageReceivedCallbackAsync(async (m) =>
            {
                if (m.Payload.ToString().Contains("ThrowException"))
                {
                    throw new InvalidOperationException();
                }

                var path = Path.Combine(_serverSettings.DestinationDirectory, "Asynchronous");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                var fileName = Path.Combine(path, m.MessageId + ".xml");

                await using var sw = File.CreateText(fileName);
                await m.Payload.SaveAsync(sw, SaveOptions.None, CancellationToken.None);
            });
            _messagingServer.RegisterAsynchronousMessageReceivedCompletedCallbackAsync((m) =>
            {
                ScopeContext.PushProperty(propKeyCorrelationId, m.MessageId);
                return Task.CompletedTask;
            });

            _messagingServer.RegisterSynchronousMessageReceivedStartingCallbackAsync((m) =>
            {
                ScopeContext.PushProperty(propKeyCorrelationId, string.Empty);// reset correlation id
                return Task.CompletedTask;
            });
            _messagingServer.RegisterSynchronousMessageReceivedCallbackAsync(async (m) =>
            {
                var path = Path.Combine(_serverSettings.DestinationDirectory, "Synchronous");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                var fileName = Path.Combine(path, m.MessageId + ".xml");
                await using (var sw = File.CreateText(fileName))
                {
                    await m.Payload.SaveAsync(sw, SaveOptions.None, CancellationToken.None);
                }
                return new XDocument(new XElement("DummyResponse"));
            });
            _messagingServer.RegisterSynchronousMessageReceivedCompletedCallbackAsync((m) =>
            {
                ScopeContext.PushProperty(propKeyCorrelationId, string.Empty); // reset correlation id
                return Task.CompletedTask;
            });
        }

        private static void CreateLogger(ServiceCollection serviceCollection)
        {
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");
            LogManager.ThrowConfigExceptions = true;
            serviceCollection.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.AddNLog();
            });
        }
    }

    internal class ServerSettings
    {
        public string DestinationDirectory { get; set; }
    }
}
