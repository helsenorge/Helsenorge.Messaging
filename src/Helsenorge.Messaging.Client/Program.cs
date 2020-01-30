using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Registries;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
#if NET471
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Helsenorge.Messaging.Client
{
    class Program
    {
        private static readonly object SyncRoot = new object();
        private static Stack<string> _files;
    
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
            addressRegistrySettings.WcfConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var addressRegistry = new AddressRegistry(addressRegistrySettings, distributedCache);

            // set up collaboration registry
            var collaborationProtocolRegistrySettings = new CollaborationProtocolRegistrySettings();
            configurationRoot.GetSection("CollaborationProtocolRegistrySettings").Bind(collaborationProtocolRegistrySettings);
            collaborationProtocolRegistrySettings.WcfConfiguration =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var collaborationProtocolRegistry = new CollaborationProtocolRegistry(collaborationProtocolRegistrySettings, 
                distributedCache, addressRegistry);

            _clientSettings = new ClientSettings();
            configurationRoot.GetSection("ClientSettings").Bind(_clientSettings);
            
            // set up messaging
            var messagingSettings = new MessagingSettings();
            configurationRoot.GetSection("MessagingSettings").Bind(messagingSettings);

            messagingSettings.IgnoreCertificateErrorOnSend = ignoreCertificateErrors;
            messagingSettings.LogPayload = true;

            if(noProtection)
                _messagingClient = new MessagingClient(messagingSettings, collaborationProtocolRegistry, addressRegistry, null, null, new NoMessageProtection());
            else
                _messagingClient = new MessagingClient(messagingSettings, collaborationProtocolRegistry, addressRegistry);
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

                if (Directory.Exists(_clientSettings.SourceDirectory) == false)
                {
                    _logger.LogError("Directory does not exist");
                    command.ShowHelp();
                    return 2;
                }
                _files = new Stack<string>(Directory.GetFiles(_clientSettings.SourceDirectory));
                
                var tasks = new List<Task>();
                for (var i = 0; i < _clientSettings.Threads; i++)
                {
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        for (var s = GetNextPath(); !string.IsNullOrEmpty(s); s = GetNextPath())
                        {
                            _logger.LogInformation($"Processing file {s}");
                            Task.WaitAll(_messagingClient.SendAndContinueAsync(_logger, new OutgoingMessage()
                            {
                                MessageFunction = _clientSettings.MessageFunction,
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

                if (Directory.Exists(_clientSettings.SourceDirectory) == false)
                {
                    _logger.LogError("Directory does not exist");
                    command.ShowHelp();
                    return 2;
                }
                _files = new Stack<string>(Directory.GetFiles(_clientSettings.SourceDirectory));

                // since we are synchronous, we don't fire off multiple tasks, we do them sequentially
                for (var s = GetNextPath(); !string.IsNullOrEmpty(s); s = GetNextPath())
                {
                    _logger.LogInformation($"Processing file {s}");
                    var result = _messagingClient.SendAndWaitAsync(_logger, new OutgoingMessage()
                    {
                        MessageFunction = _clientSettings.MessageFunction,
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

        private static void CreateLogger(IConfigurationRoot configurationRoot)
        {            
            ILoggerFactory loggerFactory;
#if NET46
            loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(configurationRoot.GetSection("Logging"));
#elif NET471            
            
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggerConfiguration =>
            {
                loggerConfiguration.AddConsole();
            });
            var provider = serviceCollection.BuildServiceProvider();
            loggerFactory = provider.GetRequiredService<ILoggerFactory>();
#endif
            _logger = loggerFactory.CreateLogger("TestClient");
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
