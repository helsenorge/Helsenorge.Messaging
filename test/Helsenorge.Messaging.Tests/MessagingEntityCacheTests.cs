/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Helsenorge.Messaging.Amqp;

namespace Helsenorge.Messaging.Tests
{
    [TestClass]
    public class MessagingEntityCacheTests : BaseTest
    {
        private class EntityItem : ICachedAmqpEntity
        {
            private bool _isClosed;
            readonly string _path;

            public EntityItem(string path)
            {
                _path = path;
            }
            public Task CloseAsync()
            {
                _isClosed = true;
                return Task.CompletedTask;
            }
        
            public bool IsClosed => _isClosed;

            public string Path => _path;
        }

        private class MockCache : AmqpEntityCache<EntityItem>
        {
            public MockCache(string name, uint capacity, ushort timeToLiveInSeconds, ushort maxTrimCountPerRecycle) : base(name, capacity, timeToLiveInSeconds, maxTrimCountPerRecycle)
            {
            }

            protected override Task<EntityItem> CreateEntityAsync(ILogger logger, string id)
            {
                return Task.FromResult(new EntityItem(id));
            }
        }

        private class AmqpFactoryPoolMock : AmqpFactoryPool
        {
            public AmqpFactoryPoolMock(uint capacity, ushort timeToLiveInSeconds, ushort maxTrimCountPerRecycle)
                : base(new BusSettings(new MessagingSettings())
                {
                    MaxFactories = capacity,
                    CacheEntryTimeToLive = timeToLiveInSeconds,
                    MaxCacheEntryTrimCount = maxTrimCountPerRecycle
                })
            {
            }

            protected override Task<IAmqpFactory> CreateEntityAsync(ILogger logger, string id)
            {
                return Task.FromResult<IAmqpFactory>(new AmqpFactoryMock());
            }
        }

        private class AmqpFactoryMock : IAmqpFactory
        {
            public bool IsClosed { get; } = false;

            public Task CloseAsync()
            {
                return Task.CompletedTask;
            }

            public IAmqpReceiver CreateMessageReceiver(string id, int credit)
            {
                return null;
            }

            public IAmqpSender CreateMessageSender(string id)
            {
                return null;
            }

            public Task<IAmqpMessage> CreateMessageAsync(Stream stream)
            {
                return null;
            }
        }

        MockCache _cache;

        [TestInitialize]
        public void Initialize()
        {
            _cache = new MockCache("MockCache", 5, 0, 24);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cache = null;
        }

        [TestMethod]
        public async Task BusFactoryPool_DoNotIncrementActiveCountBeyond1()
        {
            var factoryPool = new AmqpFactoryPoolMock(5, 0, 24);
            for (int i = 0; i < 5; i++)
            {
                await factoryPool.FindNextFactoryAsync(Logger);
            }

            for (int i = 0; i < 5; i++)
            {
                // For other pool types, this would result in ActiveCount being incremented, but for
                // BusFactoryPool.FindNextFactory makes sure that it won't
                await factoryPool.FindNextFactoryAsync(Logger);
                var entry = factoryPool.Entries[$"MessagingFactory{i}"];
                // Even though we have requested the same BusFactory twice, once in the first loop and a second
                // time in this loop, ActiveCount should still be 1.
                Assert.AreEqual(1, entry.ActiveCount);
            }
        }

        /// <summary>
        /// Creates a simple entity
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_Create()
        {
            await _cache.CreateAsync(Logger, "path");

            Assert.AreEqual(1, _cache.Entries.Count, "EntryCount");
            var entry = _cache.Entries.First().Value;

            Assert.AreEqual(1, entry.ActiveCount, "ActiveCount");
            Assert.IsNotNull(entry.Entity, "Entity");
        }

        /// <summary>
        /// Creates an entity and releases it
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_CreateAndRelease()
        {
            var path = "path";
            await _cache.CreateAsync(Logger, path);

            Assert.AreEqual(1, _cache.Entries.Count, "EntryCount");
            var entry = _cache.Entries.First().Value;

            Assert.AreEqual(1, entry.ActiveCount, "ActiveCount");
            Assert.IsNotNull(entry.Entity, "Entity");

            await _cache.ReleaseAsync(Logger, path);

            Assert.AreEqual(0, entry.ActiveCount, "ActiveCount");
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
                await _cache.CreateAsync(Logger, "path");
            }
            Assert.AreEqual(1, _cache.Entries.Count, "EntryCount");
            var entry = _cache.Entries.First().Value;

            Assert.AreEqual(100, entry.ActiveCount, "ActiveCount");
            Assert.IsNotNull(entry.Entity, "Entity");

            for (int i = 0; i < 100; i++)
            {
                await _cache.ReleaseAsync(Logger, "path");
            }
            Assert.AreEqual(1, _cache.Entries.Count, "EntryCount");
            entry = _cache.Entries.First().Value;

            Assert.AreEqual(0, entry.ActiveCount, "ActiveCount");
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
                await _cache.CreateAsync(Logger, "path" + i.ToString());
            }
            Assert.AreEqual(5, _cache.Entries.Count, "EntryCount");

            for (int i = 0; i < _cache.Capacity; i++)
            {
                var entry = _cache.Entries["path" + i.ToString()];

                Assert.AreEqual(1, entry.ActiveCount, "ActiveCount");
                Assert.IsNotNull(entry.Entity, "Entity");
            }
        }

        [TestMethod]
        public async Task MessageClientEntity_EntityClosesOnlyWhenActiveCountEqualToZero()
        {
            for (var i = 1; i < 3001; i++)
            {
                var entry = await _cache.CreateAsync(Logger, $"path{i}");
                if ((i+1) % 5 == 0)
                {
                    await _cache.ReleaseAsync(Logger, entry.Path);
                }
            }

            foreach (var entry in _cache.Entries)
            {
                await _cache.ReleaseAsync(Logger, entry.Value.Path);
            }

            await Task.Delay(4_000);

            for (var i = 3001; i < 3501; i++)
            {
                var entry = await _cache.CreateAsync(Logger, $"path{i}");
                if ((i+1) % 5 == 0)
                {
                    await _cache.ReleaseAsync(Logger, entry.Path);
                }
            }

            await Task.Delay(4_000);

            for (var i = 3501; i < 4001; i++)
            {
                var entry = await _cache.CreateAsync(Logger, $"path{i}");
                if ((i+1) % 5 == 0)
                {
                    await _cache.ReleaseAsync(Logger, entry.Path);
                }
            }

            await Task.Delay(4_000);

            for (var i = 4001; i < 4501; i++)
            {
                var entry = await _cache.CreateAsync(Logger, $"path{i}");
                if ((i+1) % 5 == 0)
                {
                    await _cache.ReleaseAsync(Logger, entry.Path);
                }
            }

            await Task.Delay(10_000);

            for (var i = 4501; i < 5001; i++)
            {
                var entry = await _cache.CreateAsync(Logger, $"path{i}");
                if ((i+1) % 5 == 0)
                {
                    await _cache.ReleaseAsync(Logger, entry.Path);
                }
            }

            var nullEntries = 0;
            var nonNullEntries = 0;
            foreach (var entry in _cache.Entries)
            {
                nullEntries += entry.Value.Entity == null ? 1 : 0;
                nonNullEntries += entry.Value.Entity != null ? 1 : 0;
            }

            Logger.Log(LogLevel.Information, EventIds.MessagingEntityCacheProcessor, $"Null entries: {nullEntries}");
            Logger.Log(LogLevel.Information, EventIds.MessagingEntityCacheProcessor, $"Non-null entries: {nonNullEntries}");

            Assert.AreEqual(3400, nullEntries);
            Assert.AreEqual(1600, nonNullEntries);
        }

        /// <summary>
        /// Creating more than the capacity, open active entrys are markes as close pending
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_EntityGetsClosePending()
        {
            await _cache.CreateAsync(Logger, "path1");
            await _cache.CreateAsync(Logger, "path2");
            await _cache.CreateAsync(Logger, "path3");
            await _cache.CreateAsync(Logger, "path4");
            await _cache.CreateAsync(Logger, "path5");

            // path3 will be the one that will be reclaimed
            var entry = _cache.Entries["path3"];
            entry.LastUsed = DateTime.Now.AddSeconds(-15);

            await _cache.CreateAsync(Logger, "path6");

            Assert.AreEqual(1, entry.ActiveCount, "ActiveCount"); // path3 is still active since we haven't release it
        }

        /// <summary>
        /// Go beyond capacity, then close one of the previously opened
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_CloseEntityWhenBeyondCapacity()
        {
            await _cache.CreateAsync(Logger, "path1");
            await _cache.CreateAsync(Logger, "path2");
            await _cache.CreateAsync(Logger, "path3");
            await _cache.CreateAsync(Logger, "path4");
            await _cache.CreateAsync(Logger, "path5");

            // path3 will be the one that will be reclaimed
            var entry = _cache.Entries["path3"];
            await _cache.ReleaseAsync(Logger, "path3"); // we are done with path 3
            entry.LastUsed = DateTime.Now.AddSeconds(-15);

            await _cache.CreateAsync(Logger, "path6");
            await _cache.CreateAsync(Logger, "path7");

            // entity object for path3 has been reclaimed
            Assert.AreEqual(0, entry.ActiveCount, "ActiveCount is not equal to zero");
            Assert.IsNull(entry.Entity, "Entity is not null");
        }

        /// <summary>
        /// go beyound capacity, then close and re-open an entity
        /// </summary>
        [TestMethod]
        public async Task MessageClientEntity_RecreatePreviouslyClosed()
        {
            await _cache.CreateAsync(Logger, "path1");
            await _cache.CreateAsync(Logger, "path2");
            await _cache.CreateAsync(Logger, "path3");
            await _cache.CreateAsync(Logger, "path4");
            await _cache.CreateAsync(Logger, "path5");

            // path3 will be the one that will be reclaimed
            var entry = _cache.Entries["path3"];
            entry.LastUsed = DateTime.Now.AddSeconds(-15);

            await _cache.CreateAsync(Logger, "path6"); // create a new entry so that we have to free up another
            await _cache.ReleaseAsync(Logger, "path3"); // we are done with path 3
            await _cache.ReleaseAsync(Logger, "path2");

            // path2 will be bumped since it was LastUsed 5 seconds earlier than path3
            _cache.Entries["path2"].LastUsed = DateTime.Now.AddSeconds(-20);

            await _cache.CreateAsync(Logger, "path3");
            Assert.AreEqual(1, entry.ActiveCount, "ActiveCount");
            Assert.IsNotNull(entry.Entity, "Entity");

            //entity object for path2 has been reclaimed
            entry = _cache.Entries["path2"];
            Assert.AreEqual(0, entry.ActiveCount, "ActiveCount");
            Assert.IsNull(entry.Entity, "Entity");
        }

        /// <summary>
        /// Call the shudown command
        /// </summary>
        [TestMethod]
        public async Task Shutdown()
        {
            await _cache.CreateAsync(Logger, "path1");
            await _cache.CreateAsync(Logger, "path2");
            await _cache.CreateAsync(Logger, "path3");
            await _cache.CreateAsync(Logger, "path4");
            await _cache.CreateAsync(Logger, "path5");

            await _cache.ShutdownAsync(Logger);


            // new create commands are ignored in shutdown mode
            await _cache.CreateAsync(Logger, "path6");
            Assert.AreEqual(5, _cache.Entries.Count, "EntryCount");

            foreach (var key in _cache.Entries.Keys)
            {
                var entry = _cache.Entries[key];
                Assert.IsNull(entry.Entity, "Entity");
            }
        }
    }
}
