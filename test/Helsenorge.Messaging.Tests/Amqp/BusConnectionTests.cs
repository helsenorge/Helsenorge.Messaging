/*
 * Copyright (c) 2021, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Amqp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.Amqp
{
    [TestClass]
    public class BusConnectionTests
    {
        private static string ConnectionString = "amqps://guest:guest@messagebroker.nhn.no/NameSpaceTest";

        [TestMethod]
        public void CanGetEnityNameForReceiverLinkForRabbitMQ()
        {
            var connection = new AmqpConnection(ConnectionString, MessageBrokerDialect.RabbitMQ);
            var entityName = connection.GetEntityName("my-queue-name", LinkRole.Receiver);
            Assert.AreEqual("/amq/queue/my-queue-name", entityName);
        }

        [TestMethod]
        public void CanGetEnityNameForSenderLinkForRabbitMQ()
        {
            var connection = new AmqpConnection(ConnectionString, MessageBrokerDialect.RabbitMQ);
            var entityName = connection.GetEntityName("my-queue-name", LinkRole.Sender);
            Assert.AreEqual("/exchange/NameSpaceTest/my-queue-name", entityName);
        }

        [TestMethod]
        public void CanGetEnityNameForReceiverLinkForServiceBus()
        {
            var connection = new AmqpConnection(ConnectionString, MessageBrokerDialect.ServiceBus);
            var entityName = connection.GetEntityName("my-queue-name", LinkRole.Receiver);
            Assert.AreEqual("NameSpaceTest/my-queue-name", entityName);
        }

        [TestMethod]
        public void CanGetEnityNameForSenderLinkForServiceBus()
        {
            var connection = new AmqpConnection(ConnectionString, MessageBrokerDialect.ServiceBus);
            var entityName = connection.GetEntityName("my-queue-name", LinkRole.Sender);
            Assert.AreEqual("NameSpaceTest/my-queue-name", entityName);
        }
    }
}
