/*
 * Copyright (c) 2021, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.ServiceBus
{
    [TestClass]
    public class ServiceBusConnectionTests
    {
        private static string ConnectionString = "amqps://guest:guest@messagebroker.nhn.no/NameSpaceTest";

        [TestMethod]
        public void CanGetEnityNameForReceiverLinkForRabbitMQ()
        {
            var serviceBusConnection = new ServiceBusConnection(ConnectionString, MessageBrokerDialect.RabbitMQ, new Logger<ServiceBusConnectionTests>(new NullLoggerFactory()));
            var entityName = serviceBusConnection.GetEntityName("my-queue-name", LinkRole.Receiver);
            Assert.AreEqual("/amq/queue/my-queue-name", entityName);
        }

        [TestMethod]
        public void CanGetEnityNameForSenderLinkForRabbitMQ()
        {
            var serviceBusConnection = new ServiceBusConnection(ConnectionString, MessageBrokerDialect.RabbitMQ, new Logger<ServiceBusConnectionTests>(new NullLoggerFactory()));
            var entityName = serviceBusConnection.GetEntityName("my-queue-name", LinkRole.Sender);
            Assert.AreEqual("/exchange/NameSpaceTest/my-queue-name", entityName);
        }

        [TestMethod]
        public void CanGetEnityNameForReceiverLinkForServiceBus()
        {
            var serviceBusConnection = new ServiceBusConnection(ConnectionString, MessageBrokerDialect.ServiceBus, new Logger<ServiceBusConnectionTests>(new NullLoggerFactory()));
            var entityName = serviceBusConnection.GetEntityName("my-queue-name", LinkRole.Receiver);
            Assert.AreEqual("NameSpaceTest/my-queue-name", entityName);
        }

        [TestMethod]
        public void CanGetEnityNameForSenderLinkForServiceBus()
        {
            var serviceBusConnection = new ServiceBusConnection(ConnectionString, MessageBrokerDialect.ServiceBus, new Logger<ServiceBusConnectionTests>(new NullLoggerFactory()));
            var entityName = serviceBusConnection.GetEntityName("my-queue-name", LinkRole.Sender);
            Assert.AreEqual("NameSpaceTest/my-queue-name", entityName);
        }
    }
}
