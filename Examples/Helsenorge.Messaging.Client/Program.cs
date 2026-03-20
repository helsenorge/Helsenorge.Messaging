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
using Helsenorge.Messaging.Security;
using Helsenorge.Registries;
using Helsenorge.Registries.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Client
{
    class Program
    {
        private static readonly object SyncRoot = new object();
        private static Stack<string> _files;
        private static ILoggerFactory _loggerFactory;
        private static ILogger _logger;
        private static MessagingClient _messagingClient;
        private static ClientSettings _clientSettings;

        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.HelpOption("-?|-h|--help");

            app.Command("sendasync", HandleAsyncMessage);
            app.Command("sendsync", HandleSyncMessage);
            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 2;
            });

            int exitCode = app.Execute(args);

#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Press any key to continue. . .");
            Console.ReadKey(true);
#endif
            return exitCode;
        }

        private static void Configure(string profile, bool ignoreCertificateErrors, bool noProtection)
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
            var helseidConfiguratrion = HelseIdConfiguration.ConfigurationFromAppSettings(configurationRoot.GetSection("CppaHelseIdConfiguration"));

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

            _clientSettings = new ClientSettings();
            configurationRoot.GetSection("ClientSettings").Bind(_clientSettings);
            
            // set up messaging
            var messagingSettings = new MessagingSettings();
            configurationRoot.GetSection("MessagingSettings").Bind(messagingSettings);

            messagingSettings.IgnoreCertificateErrorOnSend = ignoreCertificateErrors;
            messagingSettings.LogPayload = true;

            var serviceProvider = serviceCollection.BuildServiceProvider();
            _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger("TestServer");

            if (noProtection)
                _messagingClient = new MessagingClient(messagingSettings, _loggerFactory, serviceProvider.GetRequiredService<ICollaborationProtocolRegistry>(), addressRegistry, null, null, new NoMessageProtection());
            else
                _messagingClient = new MessagingClient(messagingSettings, _loggerFactory, serviceProvider.GetRequiredService<ICollaborationProtocolRegistry>(), addressRegistry);
        }

        private static void HandleAsyncMessage(CommandLineApplication command)
        {
            var profileArgument = command.Argument("[profile]", "The name of the json profile file to use (excluded file extension)");
            var noProtection = command.Option("--noprotection", "Don't sign or encrypt message", CommandOptionType.NoValue);

            command.HelpOption("-?|-h|--help");
            command.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(profileArgument.Value))
                {
                    command.ShowHelp();
                    return 2;
                }

                Configure(profileArgument.Value, noProtection.HasValue(), noProtection.HasValue());

                if (!Directory.Exists(_clientSettings.SourceDirectory))
                {
                    _logger.LogError("Directory does not exist");
                    command.ShowHelp();
                    return 2;
                }
                _files = new Stack<string>(Directory.GetFiles(_clientSettings.SourceDirectory));

                // For the sake of the example we just fetch the first HER-id of MyHerIds configured in ClientSample.json.
                var fromHerId = _messagingClient.Settings?.MyHerIds?.FirstOrDefault();
                if (!fromHerId.HasValue)
                    throw new MessagingException("At least one HER-id must be set for the setting MyHerIds in ClientSample.json.");

                var tasks = new List<Task>();
                for (var i = 0; i < _clientSettings.Threads; i++)
                {
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        for (var s = GetNextPath(); !string.IsNullOrEmpty(s); s = GetNextPath())
                        {
                            _logger.LogInformation("Processing file {S}", s);
                            Task.WaitAll(_messagingClient.SendAndContinueAsync(new OutgoingMessage()
                            {
                                MessageFunction = _clientSettings.MessageFunction,
                                FromHerId = fromHerId.Value,
                                ToHerId = _clientSettings.ToHerId,
                                MessageId = Guid.NewGuid().ToString("D"),
                                PersonalId = "99999999999",
                                Payload = XDocument.Load(File.OpenRead(s))
                            }));
                        }
                    }));
                }
                tasks.ForEach((w) => w.Wait());
                return 0;
            });
        }

        private static void HandleSyncMessage(CommandLineApplication command)
        {
            var profileArgument = command.Argument("[profile]", "The name of the json profile file to use (excluded file extension)");
            var noProtection = command.Option("--noprotection", "Don't sign or encrypt message", CommandOptionType.NoValue);

            command.HelpOption("-?|-h|--help");
            command.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(profileArgument.Value))
                {
                    command.ShowHelp();
                    return 2;
                }
                Configure(profileArgument.Value, noProtection.HasValue(), noProtection.HasValue());

                if (!Directory.Exists(_clientSettings.SourceDirectory))
                {
                    _logger.LogError("Directory does not exist");
                    command.ShowHelp();
                    return 2;
                }
                _files = new Stack<string>(Directory.GetFiles(_clientSettings.SourceDirectory));

                // For the sake of the example we just fetch the first HER-id of MyHerIds configured in ClientSample.json.
                var fromHerId = _messagingClient.Settings?.MyHerIds?.FirstOrDefault();
                if (!fromHerId.HasValue)
                    throw new MessagingException("At least one HER-id must be set for the setting MyHerIds in ClientSample.json.");

                // since we are synchronous, we don't fire off multiple tasks, we do them sequentially
                for (var s = GetNextPath(); !string.IsNullOrEmpty(s); s = GetNextPath())
                {
                    _logger.LogInformation("Processing file {S}", s);
                    var result = _messagingClient.SendAndWaitAsync(new OutgoingMessage()
                    {
                        MessageFunction = _clientSettings.MessageFunction,
                        FromHerId = fromHerId.Value,
                        ToHerId = _clientSettings.ToHerId,
                        MessageId = Guid.NewGuid().ToString("D"),
                        PersonalId = "99999999999",
                        Payload = XDocument.Load(File.OpenRead(s))

                    }).Result;
                    _logger.LogInformation(result.ToString());
                }
                return 0;
            });

        }

        private static string GetNextPath()
        {
            lock (SyncRoot)
            {
                if (_files.Count > 0)
                {
                    return _files.Pop();
                }
            }
            return null;
        }

        private static void CreateLogger(ServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(loggerConfiguration =>
            {
                loggerConfiguration.AddConsole();
            });
        }
    }

    internal class ClientSettings
    {
        public int ToHerId { get; set; }
        public string MessageFunction { get; set; }
        public string SourceDirectory { get; set; }
        public int Threads { get; set; }
    }
}
