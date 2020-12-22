/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Provides a cache for messaging entities that benefit from not being re-created all the time.
    /// Operations are thread safe
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MessagingEntityCache<T> where T : class, ICachedMessagingEntity
    {
        /// <summary>
        /// Represents an entry in the cache
        /// </summary>
        /// <typeparam name="TE"></typeparam>
        public class CacheEntry<TE> where TE : T
        {
            /// <summary>
            /// Reference to the entity being cached
            /// </summary>
            public TE Entity { get; set; }
            /// <summary>
            /// The time the entry was last accessed
            /// </summary>
            public DateTime LastUsed { get; set; }
            /// <summary>
            /// Number of pending users
            /// </summary>
            public int ActiveCount { get; set; }
            /// <summary>
            /// Entry has ben scheduled for closure
            /// </summary>
            public bool ClosePending { get; set; }
            /// <summary>
            /// Entity Path for this entity
            /// </summary>
            public string Path { get; set; }
        }

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, CacheEntry<T>> _entries = new Dictionary<string, CacheEntry<T>>();
        private readonly string _name;
        private bool _shutdownPending;
        /// <summary>
        /// Gets the maximum number of items the cache can hold
        /// </summary>
        public uint Capacity { get; }
        /// <summary>
        /// Gets a list over all existing entries
        /// </summary>
        public Dictionary<string, CacheEntry<T>> Entries => _entries;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of the cache</param>
        /// <param name="capacity">Max number of items the cache should hold</param>
        protected MessagingEntityCache(string name, uint capacity)
        {
            _name = name;
            Capacity = capacity;
        }

        /// <summary>
        /// Creates the entity contained in the cache
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected abstract Task<T> CreateEntity(ILogger logger, string id);

        /// <summary>
        /// Gets a cached copy. Creates it if it doesn't exist
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<T> Create(ILogger logger, string path)
        {
            logger.LogInformation(EventIds.MessagingEntityCacheProcessor, "Start-MessagingEntityCache::Create: Create entry for {Path}", path);

            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (_shutdownPending) return null;

            await TrimEntries(logger).ConfigureAwait(false); // see if we need to trim entries

            CacheEntry<T> entry;
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {

                // create an entry if it doesn't exist
                if (_entries.ContainsKey(path) == false)
                {
                    // create a new record for this entity
                    logger.LogInformation(EventIds.MessagingEntityCacheProcessor, "MessagingEntityCacheCreate: Creating entry for {Path}", path);
                    entry = new CacheEntry<T>()
                    {
                        ActiveCount = 1,
                        LastUsed = DateTime.Now,
                        Entity = await CreateEntity(logger, path).ConfigureAwait(false),
                        ClosePending = false,
                        Path = path,
                    };
                    _entries.Add(path, entry);

                    logger.LogInformation(EventIds.MessagingEntityCacheProcessor, "End-MessagingEntityCacheCreate: Create entry for {Path}", path);
                    return entry.Entity;
                }

                entry = _entries[path];

                entry.ActiveCount++;
                entry.LastUsed = DateTime.Now;
                logger.LogInformation(EventIds.MessagingEntityCacheProcessor, $"MessagingEntityCacheCreate: Updating entry for {path} with ActiveCount {entry.ActiveCount}");

                // if this entity previously was closed, we need to create a new instance
                if (entry.Entity != null) return entry.Entity;

                logger.LogInformation(EventIds.MessagingEntityCacheProcessor, "MessagingEntityCacheCreate: Creating new entity for {Path}", path);
                entry.Entity = await CreateEntity(logger, path).ConfigureAwait(false);
                entry.ClosePending = false;
            }
            finally
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);
                _semaphore.Release();
                logger.LogInformation(EventIds.MessagingEntityCacheProcessor, "End-MessagingEntityCacheCreate: Create entry for {Path}", path);
            }

            return entry.Entity;
        }

        /// <summary>
        /// Releases a cached copy
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="path"></param>
        public async Task Release(ILogger logger, string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (_shutdownPending) return;

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_entries.TryGetValue(path, out CacheEntry<T> entry) == false)
                {
                    return;
                }
                // under normal conditions, we just decrease the active count
                entry.ActiveCount--;
                logger.LogInformation(EventIds.MessagingEntityCacheProcessor, "MessagingEntityCache: Releasing entry for {Path}", path);

                // under a high volume scenario, this may be the last used entry even if it was just used
                // in those cases we need to close the connection and set respective properties
                if (entry.ClosePending && entry.ActiveCount <= 0)
                {
                    await CloseEntity(logger, entry, path).ConfigureAwait(false);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static async Task CloseEntity(ILogger logger, CacheEntry<T> entry, string path)
        {
            logger.LogInformation(EventIds.MessagingEntityCacheProcessor, "MessagingEntityCache: Closing entity for {Path}", path);

            if (entry.Entity == null) return;
            if (entry.Entity.IsClosed == false)
            {
                try
                {
                    await entry.Entity.Close().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    logger.LogCritical(EventIds.MessagingEntityCacheFailedToCloseEntity, "Failed to close message entity: {Path}", path);
                }
            }
            entry.Entity = null;
            entry.ClosePending = false;
        }
        /// <summary>
        /// Closes all entities
        /// </summary>
        public async Task Shutdown(ILogger logger)
        {
            _shutdownPending = true;
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try {
                logger.LogInformation(EventIds.MessagingEntityCacheProcessor, "Shutting down: {CacheName}", _name);

                foreach (var key in _entries.Keys)
                {
                    var entry = _entries[key];
                    await CloseEntity(logger, entry, key).ConfigureAwait(false);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task TrimEntries(ILogger logger)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // we haven't reached our max capacity yet
                if (_entries.Keys.Count <= Capacity) return;

                logger.LogInformation(EventIds.MessagingEntityCacheProcessor, "MessagingEntityCache: Trimming entries");
                
                int count = (int)(_entries.Keys.Count - Capacity);
                // get the oldest n entries
                var removal = (from v in _entries.Values
                               where v.Entity != null
                               orderby v.LastUsed ascending
                               select v).Take(count);

                foreach (var item in removal)
                {
                    if (item.ClosePending && item.ActiveCount <= 0)
                    {
                        await CloseEntity(logger, item, item.Path).ConfigureAwait(false);
                    }
                    else
                    {
                        item.ClosePending = true; // flad it so that the release function will close the connection
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
