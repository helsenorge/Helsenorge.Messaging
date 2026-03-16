/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
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
using HelseId.Library.Configuration;
using HelseId.Library;
using HelseId.Library.ClientCredentials;
using HelseId.Library.Interfaces.Caching;
using HelseId.Library.Services.Caching;
using Microsoft.IdentityModel.Tokens;

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

            CreateLogger(configurationRoot);

            // configure caching
            var distributedCache = DistributedCacheFactory.Create();

            // set up address registry
            var addressRegistrySettings = new AddressRegistrySettings();
            configurationRoot.GetSection("AddressRegistrySettings").Bind(addressRegistrySettings);
            var addressRegistry = new AddressRegistry(addressRegistrySettings, distributedCache, _logger);

            // set up HelseIdClient
            //var helseidConfiguratrion = new HelseIdConfiguration();
            //configurationRoot.GetSection("HelseIdConfiguration").Bind(helseidConfiguratrion);
            var provider = new SecurityKeyProvider();
            //var helseIdClient = new HelseIdClient(helseidConfiguratrion, provider);

            // set up collaboration rest registry
            var collaborationProtocolRegistryRestSettings = new CollaborationProtocolRegistryRestSettings();
            configurationRoot.GetSection("CollaborationProtocolRegistryRestSettings").Bind(collaborationProtocolRegistryRestSettings);


            //----------------------------------------------HELSEID START
            var serviceCollection = new ServiceCollection();

            //nhn client
            var helseIdConfig_v2 = new HelseIdConfiguration
            {
                ClientId = "39d6ffea-9e66-4681-94ad-109867e553c9",
                Scope = "nhn:cppa/access",
                IssuerUri = "https://helseid-sts.test.nhn.no",
                SelvbetjeningConfiguration = new SelvbetjeningConfiguration { SelvbetjeningScope = " ", UpdateClientSecretEndpoint = "" }
            };
            var helseIdBuilder = serviceCollection.AddHelseIdClientCredentials(helseIdConfig_v2);

            //See HelseIdServiceCollectionExtensions.AddHelseIdDistributedCaching extension
            helseIdBuilder.RemoveServiceRegistrations<ITokenCache>();
            helseIdBuilder.RemoveServiceRegistrations<IDiscoveryDocumentCache>();
            helseIdBuilder.Services.AddSingleton<ITokenCache>(new DistributedTokenCache(distributedCache));
            helseIdBuilder.Services.AddSingleton<IDiscoveryDocumentCache, DistributedDiscoveryDocumentCache>();


            //Register Certificate
            //See HelseIdServiceCollectionExtensions for other auth methods
            helseIdBuilder.AddSigningCredentialForClientAuthentication(new SigningCredentials
                (provider.GetSecurityKey(), SecurityAlgorithms.RsaSha512));


            // Register a service that will call HelseID
            helseIdBuilder.Services.AddHttpClient<Registries.Configuration.ProxyHttpClientFactory>();

            //Nothing for single tenant ??
            serviceCollection.AddSingleton(new HelseId.Library.Models.DetailsFromClient.OrganizationNumbers("parent", "child"));

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            var collaborationProtocolRestRegistry = serviceProvider.GetRequiredService<CollaborationProtocolRegistryRest>();

            //----------------------------------------------HELSEID END

            _serverSettings = new ServerSettings();
            configurationRoot.GetSection("ServerSettings").Bind(_serverSettings);

            // set up messaging
            var messagingSettings = new MessagingSettings();
            configurationRoot.GetSection("MessagingSettings").Bind(messagingSettings);

            messagingSettings.AmqpSettings.Synchronous.ReplyQueueMapping.Add(Environment.MachineName, "DUMMY"); // we just need a value, it will never be used
            messagingSettings.LogPayload = true;

            _messagingServer = new MessagingServer(messagingSettings, _loggerFactory, collaborationProtocolRestRegistry, addressRegistry);

            const string correlationId = "correlationId";
            _messagingServer.RegisterAsynchronousMessageReceivedStartingCallbackAsync((listener, message) =>
            {
                ScopeContext.PushProperty(correlationId, message.MessageId);
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
                ScopeContext.PushProperty(correlationId, m.MessageId);
                return Task.CompletedTask;
            });

            _messagingServer.RegisterSynchronousMessageReceivedStartingCallbackAsync((m) =>
            {
                ScopeContext.PushProperty(correlationId, string.Empty);// reset correlation id
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
                ScopeContext.PushProperty(correlationId, string.Empty); // reset correlation id
                return Task.CompletedTask;
            });
        }

        private static void CreateLogger(IConfigurationRoot configurationRoot)
        {
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");
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
