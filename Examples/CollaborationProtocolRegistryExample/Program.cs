using System;
using System.Threading.Tasks;
using Helsenorge.Registries;
using Helsenorge.Registries.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollaborationProtocolRegistryExample
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var counterPartyHerId = 8141333;

            var loggerFactory = new NullLoggerFactory();
            var logger = loggerFactory.CreateLogger("CommonLogger");

            var cache = DistributedCacheFactory.Create();

            var addressRegistry = new AddressRegistry(new AddressRegistrySettings
                {
                    WcfConfiguration = new WcfConfiguration
                    {
                        Address = "https://ws-web.test.nhn.no/v1/AR/Basic",
                        MaxBufferSize = 2147483647,
                        MaxBufferPoolSize = 2147483647,
                        MaxReceivedMessageSize = 2147483647,
                        UserName = "Kenneth.Myhra@helsedir.no",
                        Password = "d8JIq7Sa",
                    },
                }, cache);

            var settings = new CollaborationProtocolRegistrySettings
            {
                WcfConfiguration = new WcfConfiguration
                {
                    Address = "https://ws-web.test.nhn.no/v1/CPPA/Basic",
                    MaxBufferSize = 2147483647,
                    MaxBufferPoolSize = 2147483647,
                    MaxReceivedMessageSize = 2147483647,
                    UserName = "Kenneth.Myhra@helsedir.no",
                    Password = "d8JIq7Sa",
                },
                MyHerId = 8093239
            };

            // var communicationPartyDetails = await addressRegistry.FindCommunicationPartyDetailsAsync(logger, counterPartyHerId);
            // Console.WriteLine(communicationPartyDetails.AsynchronousQueueName);
            // Console.WriteLine(communicationPartyDetails.SynchronousQueueName);
            // Console.WriteLine(communicationPartyDetails.ErrorQueueName);

            //var messageFunction = "APPREC";
            var messageFunction = "DIALOG_INNBYGGER_EKONSULTASJON";

            var collaborationProtocolRegistry = new CollaborationProtocolRegistry(settings, cache, addressRegistry);

            var collaborationProtocolAgreement = await collaborationProtocolRegistry.FindAgreementForCounterpartyAsync(logger, counterPartyHerId);
            if (collaborationProtocolAgreement != null)
            {
                Console.WriteLine($"CPA Id: {collaborationProtocolAgreement.CpaId}");

                var collaborationProtocolMessage = collaborationProtocolAgreement.FindMessageForReceiver(messageFunction);
                if (collaborationProtocolMessage != null)
                {
                    Console.WriteLine($"Using message function: {messageFunction} we could find:");
                    Console.WriteLine($"Protocol Message: {collaborationProtocolMessage.Name}");
                    //Console.WriteLine($"Protocol Action: {collaborationProtocolMessage.Action}");
                }
                else
                {
                    Console.WriteLine($"Could not find message for receiver using message function: {messageFunction}");
                }
            }

            var collaborationProtocolProfile = await collaborationProtocolRegistry.FindProtocolForCounterpartyAsync(logger, counterPartyHerId);
            //Console.WriteLine($"CPA Id: {collaborationProtocolProfile.CpaId}");
            Console.WriteLine($"CPP Id: {collaborationProtocolProfile.CppId}");
            Console.WriteLine($"collaborationProtocolProfile.Name: {collaborationProtocolProfile.Name}");

            var collaborationProtocolMessage2 = collaborationProtocolProfile.FindMessageForReceiver(messageFunction);
            if (collaborationProtocolMessage2 != null)
            {
                Console.WriteLine($"Using message function: {messageFunction} we could find:");
                Console.WriteLine($"Protocol Message: {collaborationProtocolMessage2.Name}");
                //Console.WriteLine($"Protocol Action: {collaborationProtocolMessage2.Action}");
            }
            else
            {
                Console.WriteLine($"Could not find message for receiver using message function: {messageFunction}");
            }

            Console.WriteLine("Press any key to continue");
            Console.ReadKey(intercept: true);
        }
    }
}
