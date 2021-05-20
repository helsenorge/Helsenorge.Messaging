/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Tests
{
    [TestClass]
    public class MessagingEntityCacheTests : BaseTest
    {
        private class EntityItem : ICachedMessagingEntity
        {
            private bool _isClosed;
            readonly string _path;

            public EntityItem(string path)
            {
                _path = path;
            }
            public Task Close()
            {
                _isClosed = true;
                return Task.CompletedTask;
            }
        
            public bool IsClosed => _isClosed;

            public string Path => _path;
        }

        private class MockCache : MessagingEntityCache<EntityItem>
        {
            public MockCache(string name, uint capacity) : base(name, capacity)
            {
            }

            protected override Task<EntityItem> CreateEntity(ILogger logger, string id)
            {
                return Task.FromResult(new EntityItem(id));
            }
        }


        MockCache _cache;

        [TestInitialize]
        public void Initialize()
        {
            _cache = new MockCache("MockCache", 5);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cache = null;
        }

        /// <summary>
        /// Creates a simple entity
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_Create()
        {
            await _cache.Create(Logger, "path");

            Assert.AreEqual(1, _cache.Entries.Count, "EntryCount");
            var entry = _cache.Entries.First().Value;

            Assert.AreEqual(1, entry.ActiveCount, "ActiveCount");
            Assert.IsFalse(entry.ClosePending, "ClosePending");
            Assert.IsNotNull(entry.Entity, "Entity");
        }

        /// <summary>
        /// Creates an entity and releases it
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_CreateAndRelease()
        {
            var path = "path";
            await _cache.Create(Logger, path);

            Assert.AreEqual(1, _cache.Entries.Count, "EntryCount");
            var entry = _cache.Entries.First().Value;

            Assert.AreEqual(1, entry.ActiveCount, "ActiveCount");
            Assert.IsFalse(entry.ClosePending, "ClosePending");
            Assert.IsNotNull(entry.Entity, "Entity");

            await _cache.Release(Logger, path);

            Assert.AreEqual(0, entry.ActiveCount, "ActiveCount");
            Assert.IsFalse(entry.ClosePending, "ClosePending");
            Assert.IsNotNull(entry.Entity, "Entity");
        }

        /// <summary>
        /// Creates 10 entities witht the same path
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_CreateAndRelase100WithSamePath()
        {
            for (int i = 0; i < 100; i++)
            {
                await _cache.Create(Logger, "path");
            }
            Assert.AreEqual(1, _cache.Entries.Count, "EntryCount");
            var entry = _cache.Entries.First().Value;

            Assert.AreEqual(100, entry.ActiveCount, "ActiveCount");
            Assert.IsFalse(entry.ClosePending, "ClosePending");
            Assert.IsNotNull(entry.Entity, "Entity");

            for (int i = 0; i < 100; i++)
            {
                await _cache.Release(Logger, "path");
            }
            Assert.AreEqual(1, _cache.Entries.Count, "EntryCount");
            entry = _cache.Entries.First().Value;

            Assert.AreEqual(0, entry.ActiveCount, "ActiveCount");
            Assert.IsFalse(entry.ClosePending, "ClosePending");
            Assert.IsNotNull(entry.Entity, "Entity");
        }

        /// <summary>
        /// Creates 10 entities with different paths. We are within the threshold, so no one should be closed
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_CreateFullCapacity()
        {
            for (int i = 0; i < _cache.Capacity; i++)
            {
                await _cache.Create(Logger, "path" + i.ToString());
            }
            Assert.AreEqual(5, _cache.Entries.Count, "EntryCount");

            for (int i = 0; i < _cache.Capacity; i++)
            {
                var entry = _cache.Entries["path" + i.ToString()];

                Assert.AreEqual(1, entry.ActiveCount, "ActiveCount");
                Assert.IsFalse(entry.ClosePending, "ClosePending");
                Assert.IsNotNull(entry.Entity, "Entity");
            }
        }

        /// <summary>
        /// Creating more than the capacity, open active entrys are markes as close pending
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_EntityGetsClosePending()
        {
            await _cache.Create(Logger, "path1");
            await _cache.Create(Logger, "path2");
            await _cache.Create(Logger, "path3");
            await _cache.Create(Logger, "path4");
            await _cache.Create(Logger, "path5");

            // path3 will be the one that will be reclaimed
            var entry = _cache.Entries["path3"];
            entry.LastUsed = DateTime.Now.AddSeconds(-15);

            await _cache.Create(Logger, "path6");

            Assert.AreEqual(1, entry.ActiveCount, "ActiveCount"); // path3 is still active since we haven't release it
            Assert.IsTrue(entry.ClosePending, "ClosePending");
        }

        /// <summary>
        /// Go beyond capacity, then close one of the previously opened
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_CloseEntityWhenBeyondCapacity()
        {
            await _cache.Create(Logger, "path1");
            await _cache.Create(Logger, "path2");
            await _cache.Create(Logger, "path3");
            await _cache.Create(Logger, "path4");
            await _cache.Create(Logger, "path5");

            // path3 will be the one that will be reclaimed
            var entry = _cache.Entries["path3"];
            entry.LastUsed = DateTime.Now.AddSeconds(-15);

            await _cache.Create(Logger, "path6");
            await _cache.Release(Logger, "path3"); // we are done with path 3

            // entity object for path3 has been reclaimed
            Assert.AreEqual(0, entry.ActiveCount, "ActiveCount is not equal to zero");
            Assert.IsNull(entry.Entity, "Entity is not null");
            Assert.IsFalse(entry.ClosePending, "ClosePending is not false");
        }

        /// <summary>
        /// go beyound capacity, then close and re-open an entity
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_RecreatePreviouslyClosed()
        {
            await _cache.Create(Logger, "path1");
            await _cache.Create(Logger, "path2");
            await _cache.Create(Logger, "path3");
            await _cache.Create(Logger, "path4");
            await _cache.Create(Logger, "path5");

            // path3 will be the one that will be reclaimed
            var entry = _cache.Entries["path3"];
            entry.LastUsed = DateTime.Now.AddSeconds(-15);

            await _cache.Create(Logger, "path6"); // create a new entry so that we have to free up another
            await _cache.Release(Logger, "path3"); // we are done with path 3
            await _cache.Release(Logger, "path2");

            //we need to use path3 again, so path 2 becomes the one being bumped
            _cache.Entries["path2"].LastUsed = DateTime.Now.AddSeconds(-10);

            await _cache.Create(Logger, "path3");
            Assert.AreEqual(1, entry.ActiveCount, "ActiveCount");
            Assert.IsNotNull(entry.Entity, "Entity");
            Assert.IsFalse(entry.ClosePending, "ClosePending");

            //entity object for path2 has been reclaimed
            entry = _cache.Entries["path2"];
            Assert.AreEqual(0, entry.ActiveCount, "ActiveCount");
            Assert.IsNull(entry.Entity, "Entity");
            Assert.IsFalse(entry.ClosePending, "ClosePending");
        }

        /// <summary>
        /// Call the shudown command
        /// </summary>
        [TestMethod]
        public async Task Shutdown()
        {
            await _cache.Create(Logger, "path1");
            await _cache.Create(Logger, "path2");
            await _cache.Create(Logger, "path3");
            await _cache.Create(Logger, "path4");
            await _cache.Create(Logger, "path5");

            _cache.Entries["path2"].ClosePending = true;

            await _cache.Shutdown(Logger);


            // new create commands are ignored in shutdown mode
            await _cache.Create(Logger, "path6");
            Assert.AreEqual(5, _cache.Entries.Count, "EntryCount");

            foreach (var key in _cache.Entries.Keys)
            {
                var entry = _cache.Entries[key];
                Assert.IsNull(entry.Entity, "Entity");
                Assert.IsFalse(entry.ClosePending, "ClosePending");
            }
        }
    }
}
