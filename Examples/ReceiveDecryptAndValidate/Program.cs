/*
 * Copyright (c) 2021, Norsk Helsenett SF and contributors
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
using Helsenorge.Messaging.ServiceBus;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.Extensions.Logging;

namespace ReceiveDecryptAndValidate
{
    class Program
    {
        private static readonly string _connectionString = "amqp://guest:guest@127.0.0.1:5672";
        private static readonly string _queue = "/amq/queue/test-queue";

        static async Task Main(string[] args)
        {
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<Program>();
            var connection = new ServiceBusConnection(_connectionString, loggerFactory.CreateLogger<ServiceBusConnection>());
            IMessagingReceiver receiver = null;
            try
            {
                var certificateStore = new MockCertificateStore();
                var signatureCertificate = certificateStore.GetCertificate(TestCertificates.CounterpartySigntatureThumbprint);
                var encryptionCertificate = certificateStore.GetCertificate(TestCertificates.CounterpartyEncryptionThumbprint);
                var messageProtection = new SignThenEncryptMessageProtection(signatureCertificate, encryptionCertificate);

                var addressRegistry = new MockAddressRegistry();

                var linkFactory = new LinkFactory(connection, loggerFactory.CreateLogger<LinkFactory>());
                receiver = linkFactory.CreateReceiver(_queue);
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
                            var publicSignatureCertificate = await addressRegistry.GetCertificateDetailsForValidatingSignatureAsync(logger, 123);
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
                    await receiver.Close();
                await connection.CloseAsync();
            }
        }
    }
}
