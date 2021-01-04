/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.ServiceBus
{
    [TestClass]
    public class ServiceBusFactoryPoolTests : BaseTest
    {
        [TestMethod]
        public void ServiceBusFactoryPool_RounTripTest()
        {
            var settings = new ServiceBusSettings(new MessagingSettings())
            {
                ConnectionString = "amqps://username:password@sb.test.nhn.no:5671/NHNTestServiceBus",
                MaxFactories = 5
            };
            var factoryPool = new ServiceBusFactoryPool(settings);

            var factory1 = factoryPool.FindNextFactory(Logger);
            var factory2 = factoryPool.FindNextFactory(Logger);
            var factory3 = factoryPool.FindNextFactory(Logger);
            var factory4 = factoryPool.FindNextFactory(Logger);
            var factory5 = factoryPool.FindNextFactory(Logger);
            var factory6 = factoryPool.FindNextFactory(Logger);

            Assert.AreNotSame(factory1, factory5);
            Assert.AreSame(factory1, factory6);
        }

        [TestMethod]
        public void ServiceBusFactoryPool_ClosePendingFalseAfterOneRoundTrip()
        {
            var settings = new ServiceBusSettings(new MessagingSettings())
            {
                ConnectionString = "amqps://username:password@sb.test.nhn.no:5671/NHNTestServiceBus",
                MaxFactories = 5
            };
            var factoryPool = new ServiceBusFactoryPool(settings);

            var factory1 = factoryPool.FindNextFactory(Logger);
            var factory2 = factoryPool.FindNextFactory(Logger);
            var factory3 = factoryPool.FindNextFactory(Logger);
            var factory4 = factoryPool.FindNextFactory(Logger);
            var factory5 = factoryPool.FindNextFactory(Logger);
            // when we reach the sixth FindNextFactory it round-trips back to the first factory
            var factory6 = factoryPool.FindNextFactory(Logger);

            var entry = factoryPool.Entries["MessagingFactory0"];
            // because of the round-tripping we should not reach Capacity and therefore ClosePending is still false
            Assert.IsFalse(entry.ClosePending);
        }
    }
}
