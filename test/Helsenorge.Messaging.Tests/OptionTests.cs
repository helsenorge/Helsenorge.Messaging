using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests
{
    [TestClass]
    public class OptionTests : BaseTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Options_NotSet()
        {
            Client = new MessagingClient(null, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CollaborationRegistry_NotSet()
        {
            Client = new MessagingClient(Settings, null, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddressRegistry_NotSet()
        {
            Client = new MessagingClient(Settings, CollaborationRegistry, null);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void MyHerId_NotSet()
        {
            Settings.MyHerId = 0;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DecryptionCertificate_NotSet()
        {
            Settings.DecryptionCertificate = null;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SigningCertificate_NotSet()
        {
            Settings.SigningCertificate = null;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConnectionString_NotSet()
        {
            Settings.ServiceBus.ConnectionString = null;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }

        [TestMethod]
        public void Synchronous_ProcessingTasksEqualsZero_IsAllowed()
        {
            Settings.ServiceBus.Synchronous.ProcessingTasks = 0;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Syncrhonous_TimeToLive_NotSet()
        {
            Settings.ServiceBus.Synchronous.TimeToLive = TimeSpan.Zero;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Syncrhonous_ReadTimeout_NotSet()
        {
            Settings.ServiceBus.Synchronous.ReadTimeout = TimeSpan.Zero;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Syncrhonous_CallTimeout_NotSet()
        {
            Settings.ServiceBus.Synchronous.CallTimeout = TimeSpan.Zero;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Syncrhonous_ReplyQueue_NotSet()
        {
            Settings.ServiceBus.Synchronous.ReplyQueueMapping = new Dictionary<string, string>();
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Asynchronous_ProcessingTasks_NotSet()
        {
            Settings.ServiceBus.Asynchronous.ProcessingTasks = 0;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        public void Asynchronous_TimeToLive_NotSet()
        {
            Settings.ServiceBus.Asynchronous.TimeToLive = TimeSpan.Zero;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Asynchronous_ReadTimeout_NotSet()
        {
            Settings.ServiceBus.Asynchronous.ReadTimeout = TimeSpan.Zero;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Error_ProcessingTasks_NotSet()
        {
            Settings.ServiceBus.Error.ProcessingTasks = 0;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Error_TimeToLive_NotSet()
        {
            Settings.ServiceBus.Error.TimeToLive = TimeSpan.Zero;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Error_ReadTimeout_NotSet()
        {
            Settings.ServiceBus.Error.ReadTimeout = TimeSpan.Zero;
            Client = new MessagingClient(Settings, CollaborationRegistry, AddressRegistry);
        }
    }
}
