﻿/* 
 * Copyright (c) 2020-2021, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Amqp;
using Amqp.Framing;
using Helsenorge.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    internal class ServiceBusFixture : IDisposable
    {
        internal ServiceBusConnection Connection { get; }

        private readonly ILogger _logger;

        public ServiceBusFixture(ITestOutputHelper output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XUnitLoggerProvider(output));
            _logger = loggerFactory.CreateLogger(typeof(ServiceBusFixture));
            Connection = new ServiceBusConnection(GetConnectionString(), _logger);
        }

        public static string GetConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable("HM_IT_CONN_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("HM_IT_CONN_STRING env variable must be set before running integration tests.");
            }
            return connectionString;
        }

        public void Dispose()
        {
            Connection.CloseAsync().Wait();
        }

        public async Task PurgeQueueAsync(string queueName)
        {
            await ReadAllMessagesAsync(queueName, true);
        }

        public ServiceBusReceiver CreateReceiver(string queueName)
        {
            return new ServiceBusReceiver(Connection, queueName, 1, _logger);
        }

        public ServiceBusSender CreateSender(string queueName)
        {
            return new ServiceBusSender(_logger, Connection, queueName);
        }

        public async Task<List<Message>> ReadAllMessagesAsync(string queueName, bool accept = false)
        {
            var list = new List<Message>();
            var connection = new ServiceBusConnection(GetConnectionString(), _logger);
            var rawConnection = await connection.GetInternalConnectionAsync();
            var session = rawConnection.CreateSession();
            var receiverLink = session.CreateReceiver($"test-receiver-link-{Guid.NewGuid()}", connection.GetEntityName(queueName, LinkRole.Receiver));
            Message message;
            while ((message = await receiverLink.ReceiveAsync(ServiceBusTestingConstants.DefaultReadTimeout)) != null)
            {
                if (accept)
                {
                    receiverLink.Accept(message);
                }
                list.Add(message);
            }
            await session.CloseAsync();
            await connection.CloseAsync();
            return list;
        }

        public async Task<string> SendTestMessageAsync(string queueName, string messageText = null)
        {
            if (messageText == null)
            {
                messageText = $"Test message {Guid.NewGuid()}";
            }
            var connection = new ServiceBusConnection(GetConnectionString(), _logger);
            var rawConnection = await connection.GetInternalConnectionAsync();
            var session = rawConnection.CreateSession();
            var senderLink = session.CreateSender($"test-sender-link-{Guid.NewGuid()}", connection.GetEntityName(queueName, LinkRole.Sender));
            await senderLink.SendAsync(new Message
            {
                BodySection = new Data
                {
                    Binary = Encoding.UTF8.GetBytes(messageText)
                }
            }).ConfigureAwait(false);
            await senderLink.CloseAsync();
            await session.CloseAsync();
            await connection.CloseAsync();
            return messageText;
        }

        public async Task CheckMessageSentAsync(string queueName, string text)
        {
            var connection = new ServiceBusConnection(GetConnectionString(), _logger);
            var rawConnection = await connection.GetInternalConnectionAsync();
            var session = rawConnection.CreateSession();
            var receiverLink = session.CreateReceiver($"test-receiver-link-{Guid.NewGuid()}", connection.GetEntityName(queueName, LinkRole.Receiver));
            var message = await receiverLink.ReceiveAsync(ServiceBusTestingConstants.DefaultReadTimeout);
            Assert.NotNull(message);
            receiverLink.Accept(message);
            Assert.Equal(text, message.GetBodyAsString());
            await receiverLink.CloseAsync();
            await session.CloseAsync();
            await connection.CloseAsync();
        }

        public async Task CheckDeadLetterQueueAsync(string queueName, params string[] messages)
        {
            var deadLetterMessages = await ReadAllMessagesAsync(ServiceBusTestingConstants.GetDeadLetterQueueName(queueName));
            Assert.Equal(deadLetterMessages.Count, messages.Length);
            Assert.Equal(deadLetterMessages.Select(m => m.GetBodyAsString()), messages);
        }
    }
}
