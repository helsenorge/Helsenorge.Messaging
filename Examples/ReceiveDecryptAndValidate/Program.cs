/*
 * Copyright (c) 2021-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.Bus;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.Extensions.Logging;

namespace ReceiveDecryptAndValidate
{
    class Program
    {
        private static string HostName = "tb.test.nhn.no";
        private static string Exchange = "NHNTestServiceBus";
        private static string Username = "guest";
        private static string Password = "guest";
        // More information about routing and addressing on RabbitMQ:
        // https://github.com/rabbitmq/rabbitmq-server/tree/main/deps/rabbitmq_amqp1_0#routing-and-addressing
        private static readonly string Queue = "/amq/queue/12345_async";

        static async Task Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            var connectionString = new AmqpConnectionString
            {
                HostName = HostName,
                Exchange = Exchange,
                UserName = Username,
                Password = Password,
            };
            var connection = new BusConnection(connectionString.ToString());
            IMessagingReceiver receiver = null;
            try
            {
                var certificateStore = new MockCertificateStore();
                var signatureCertificate = certificateStore.GetCertificate(TestCertificates.CounterpartySignatureThumbprint);
                var encryptionCertificate = certificateStore.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint);
                var messageProtection = new SignThenEncryptMessageProtection(signatureCertificate, encryptionCertificate);

                var addressRegistry = new MockAddressRegistry();

                var linkFactory = new LinkFactory(connection, loggerFactory.CreateLogger<LinkFactory>());
                receiver = linkFactory.CreateReceiver(Queue);
                int i = 0;
                while (true)
                {
                    var message = await receiver.ReceiveAsync();
                    if(message == null) break;

                    Console.WriteLine($"Message Id: {message.MessageId}");

                    await using var stream = message.GetBody();
                    if (stream != null)
                    {
                        StreamReader streamReader;
                        if (message.ContentType == ContentType.SignedAndEnveloped)
                        {
                            var publicSignatureCertificate = await addressRegistry.GetCertificateDetailsForValidatingSignatureAsync(123);
                            streamReader = new StreamReader(messageProtection.Unprotect(stream, publicSignatureCertificate.Certificate));
                        }
                        else
                        {
                            streamReader = new StreamReader(stream);
                        }
                        var body = await streamReader.ReadToEndAsync();
                        Console.WriteLine($"Message Body: {body}");

                        streamReader.Dispose();
                    }
                    Console.WriteLine($"Messages received: {++i}");

                    await message.CompleteAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: '{e.Message}'.\nStack Trace: {e.StackTrace}");
            }
            finally
            {
                if (receiver != null)
                    await receiver.CloseAsync();
                await connection.CloseAsync();
            }
        }
    }
}
