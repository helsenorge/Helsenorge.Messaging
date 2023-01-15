/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests
{
    [TestClass]
    public class OptionTests : BaseTest
    {
        [TestMethod, Ignore("Currently not able to get this test method working since GetDefaultMessageProtection is invoked and the SignThenEncryptMessageProtection ctor requires certificates which is not necessarily available.")]
        public void MessagingClient_DefaultCerificateStoreIsWindowsCerificateStore()
        {
            MessagingClient messagingClient = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry);
            Assert.IsInstanceOfType(messagingClient.CertificateStore, typeof(WindowsCertificateStore));
        }

        [TestMethod, Ignore("Currently not able to get this test method working since GetDefaultMessageProtection is invoked and the SignThenEncryptMessageProtection ctor requires certificates which is not necessarily available.")]
        public void MessagingServer_DefaultCerificateStoreIsWindowsCerificateStore()
        {
            LoggerFactory loggerFactory = new LoggerFactory();
            MessagingServer messagingServer = new MessagingServer(Settings, loggerFactory, CollaborationRegistry, AddressRegistry);
            Assert.IsInstanceOfType(messagingServer.CertificateStore, typeof(WindowsCertificateStore));
        }

        [TestMethod]
        public void MessagingClient_CerificateStoreIsMockCerificateStore()
        {
            MessagingClient messagingClient = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, new MockCertificateStore());
            Assert.IsInstanceOfType(messagingClient.CertificateStore, typeof(MockCertificateStore));
        }

        [TestMethod]
        public void MessagingServer_CerificateStoreIsMockCerificateStore()
        {
            LoggerFactory loggerFactory = new LoggerFactory();
            MessagingServer messagingServer = new MessagingServer(Settings, loggerFactory, CollaborationRegistry, AddressRegistry, new MockCertificateStore());
            Assert.IsInstanceOfType(messagingServer.CertificateStore, typeof(MockCertificateStore));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Options_NotSet()
        {
            Client = new MessagingClient(null, LoggerFactory, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CollaborationRegistry_NotSet()
        {
            Client = new MessagingClient(Settings, LoggerFactory, null, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddressRegistry_NotSet()
        {
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void MyHerIds_NotSet()
        {
            Settings.MyHerIds.Clear();
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, CertificateStore);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void MyHerIds_InvalidValue()
        {
            Settings.MyHerIds.Clear();
            Settings.MyHerIds.Add(0);
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, CertificateStore);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DecryptionCertificate_NotSet()
        {
            Settings.DecryptionCertificate = null;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SigningCertificate_NotSet()
        {
            Settings.SigningCertificate = null;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConnectionString_NotSet()
        {
            Settings.ServiceBus.ConnectionString = null;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry);
        }

        [TestMethod]
        public void Synchronous_ProcessingTasksEqualsZero_IsAllowed()
        {
            Settings.ServiceBus.Synchronous.ProcessingTasks = 0;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, CertificateStore);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Synchronous_TimeToLive_NotSet()
        {
            Settings.ServiceBus.Synchronous.TimeToLive = TimeSpan.Zero;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Synchronous_ReadTimeout_NotSet()
        {
            Settings.ServiceBus.Synchronous.ReadTimeout = TimeSpan.Zero;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Synchronous_CallTimeout_NotSet()
        {
            Settings.ServiceBus.Synchronous.CallTimeout = TimeSpan.Zero;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Synchronous_ReplyQueue_NotSet()
        {
            Settings.ServiceBus.Synchronous.ReplyQueueMapping = new Dictionary<string, string>();
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry);
        }
        
        [TestMethod]
        public void Asynchronous_ProcessingTasksEqualsZero_IsAllowed()
        {
            Settings.ServiceBus.Asynchronous.ProcessingTasks = 0;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, CertificateStore);
        }
        [TestMethod]
        public void Asynchronous_TimeToLive_NotSet()
        {
            Settings.ServiceBus.Asynchronous.TimeToLive = TimeSpan.Zero;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, CertificateStore);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Asynchronous_ReadTimeout_NotSet()
        {
            Settings.ServiceBus.Asynchronous.ReadTimeout = TimeSpan.Zero;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, CertificateStore);
        }
        [TestMethod]
        public void Error_ProcessingTasksEqualsZero_IsAllowed()
        {
            Settings.ServiceBus.Error.ProcessingTasks = 0;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, CertificateStore);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Error_ReadTimeout_NotSet()
        {
            Settings.ServiceBus.Error.ReadTimeout = TimeSpan.Zero;
            Client = new MessagingClient(Settings, LoggerFactory, CollaborationRegistry, AddressRegistry, CertificateStore);
        }
    }
}
