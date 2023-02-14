/*
 * Copyright (c) 2021-2023, Norsk Helsenett SF and contributors
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
using Helsenorge.Messaging.Bus;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.Extensions.Logging;

namespace SignEncryptAndSend
{
    class Program
    {
        private static string HostName = "tb.test.nhn.no";
        private static string Exchange = "NHNTestServiceBus";
        private static string Username = "guest";
        private static string Password = "guest";
        // More information about routing and addressing on RabbitMQ:
        // https://github.com/rabbitmq/rabbitmq-server/tree/main/deps/rabbitmq_amqp1_0#routing-and-addressing
        private static readonly string Queue = "/exchange/NHNTESTServiceBus/12345_async";

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
            IMessagingSender sender = null;
            var messageCount = 20;
            try
            {
                var certificateStore = new MockCertificateStore();
                var signatureCertificate = certificateStore.GetCertificate(TestCertificates.HelsenorgeSignatureThumbprint);
                var encryptionCertificate = certificateStore.GetCertificate(TestCertificates.HelsenorgeEncryptionThumbprint);
                var messageProtection = new SignThenEncryptMessageProtection(signatureCertificate, encryptionCertificate);

                var addressRegistry = new MockAddressRegistry();

                var linkFactory = new LinkFactory(connection, loggerFactory.CreateLogger<LinkFactory>());
                sender = linkFactory.CreateSender(Queue);
                for (var i = 0; i < messageCount; i++)
                {

                    var outgoingMessage = new OutgoingMessage
                    {
                        MessageId = Guid.NewGuid().ToString("N"),
                        ToHerId = 456,
                    };

                    var bodyPlainString = $"Hello world! - {i + 1}";
                    var payloadStream = new MemoryStream(Encoding.UTF8.GetBytes(bodyPlainString));
                    var publicEncryptionCertificate = await addressRegistry.GetCertificateDetailsForEncryptionAsync(456);
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
                    await sender.CloseAsync();
                await connection.CloseAsync();
            }
        }
    }
}
