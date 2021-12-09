/*
 * Copyright (c) 2021, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.ServiceBus;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.Extensions.Logging;

namespace SignEncryptAndSend
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
            IMessagingSender sender = null;
            var messageCount = 20;
            try
            {
                var certificateStore = new MockCertificateStore();
                var signatureCertificate = certificateStore.GetCertificate(TestCertificates.HelsenorgeSigntatureThumbprint);
                var encryptionCertificate = certificateStore.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint);
                var messageProtection = new SignThenEncryptMessageProtection(signatureCertificate, encryptionCertificate);

                var addressRegistry = new MockAddressRegistry();

                var linkFactory = new LinkFactory(connection, loggerFactory.CreateLogger<LinkFactory>());
                sender = linkFactory.CreateSender(_queue);
                for (var i = 0; i < messageCount; i++)
                {

                    var outgoingMessage = new OutgoingMessage
                    {
                        MessageId = Guid.NewGuid().ToString("N"),
                        ToHerId = 456,
                    };

                    var bodyPlainString = $"Hello world! - {i + 1}";
                    var payloadStream = new MemoryStream(Encoding.UTF8.GetBytes(bodyPlainString));
                    var publicEncryptionCertificate = await addressRegistry.GetCertificateDetailsForEncryptionAsync(logger, 456);
                    var encryptedPayloadStream = messageProtection.Protect(payloadStream, publicEncryptionCertificate.Certificate);

                    var message = await linkFactory.CreateMessageAsync(123, outgoingMessage, encryptedPayloadStream);
                    message.ContentType = ContentType.SignedAndEnveloped;

                    await sender.SendAsync(message);

                    Console.WriteLine($"Message Id: '{message.MessageId}'\nMessage Body: '{bodyPlainString}'\nMessages sent: '{i + 1}'.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: '{e.Message}'.\nStack Trace: {e.StackTrace}");
            }
            finally
            {
                if (sender != null)
                    await sender.Close();
                await connection.CloseAsync();
            }
        }
    }
}
