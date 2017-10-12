using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

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
        }

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
        protected abstract T CreateEntity(ILogger logger, string id);

        /// <summary>
        /// Gets a cached copy. Creates it if it doesn't exist
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public T Create(ILogger logger, string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (_shutdownPending) return null;

            CacheEntry<T> entry;

            TrimEntries(logger); // see if we need to trim entries

            // create an entry if it doesn't exist
            lock (_entries)
            {
                if (_entries.ContainsKey(path) == false)
                {
                    // create a new record for this entity
                    logger.LogDebug("MessagingEntityCache: Creating entry for {Path}", path);
                    entry = new CacheEntry<T>()
                    {
                        ActiveCount = 1,
                        LastUsed = DateTime.Now,
                        Entity = CreateEntity(logger, path),
                        ClosePending = false
                    };
                    _entries.Add(path, entry);
                    return entry.Entity;
                }
                entry = _entries[path];
            }
            // update information for existing item
            lock (entry)
            {
                entry.ActiveCount++;
                entry.LastUsed = DateTime.Now;
                logger.LogDebug("MessagingEntityCache: Updating entry for {Path}", path);

                // if this entity previously was closed, we need to create a new instance
                if (entry.Entity != null) return entry.Entity;

                logger.LogDebug("MessagingEntityCache: Creating new entity for {Path}", path);
                entry.Entity = CreateEntity(logger, path);
                entry.ClosePending = false;
            }
            return entry.Entity;
        }

        /// <summary>
        /// Releases a cached copy
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="path"></param>
        public void Release(ILogger logger, string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (_shutdownPending) return;

            CacheEntry<T> entry;

            lock (_entries)
            {
                if (_entries.TryGetValue(path, out entry) == false)
                {
                    return;
                }
            }
            lock (entry)
            {
                // under normal conditions, we just decrease the active count
                entry.ActiveCount--;
                logger.LogDebug("MessagingEntityCache: Releasing entry for {Path}", path);

                // under a high volume scenario, this may be the last used entry even if it was just used
                // in those cases we need to close the connection and set respective properties
                if (entry.ClosePending)
                {
                    CloseEntity(logger, entry, path);
                }
            }
        }

        private static void CloseEntity(ILogger logger, CacheEntry<T> entry, string path)
        {
            logger.LogDebug("MessagingEntityCache: Closing entity for {Path}", path);

            if (entry.Entity == null) return;
            if (entry.Entity.IsClosed == false)
            {
                try
                {
                    entry.Entity.Close();
                }
                catch (Exception)
                {
                    logger.LogCritical("Failed to close message entity: {Path}", path);
                }
            }
            entry.Entity = null;
            entry.ClosePending = false;
        }
        /// <summary>
        /// Closes all entities
        /// </summary>
        public void Shutdown(ILogger logger)
        {
            _shutdownPending = true;
            lock (_entries)
            {
                logger.LogInformation("Shutting down: {CacheName}", _name);

                foreach (var key in _entries.Keys)
                {
                    var entry = _entries[key];
                    CloseEntity(logger, entry, key);
                }
            }
        }

        private void TrimEntries(ILogger logger)
        {
            lock (_entries)
            {
                // we haven't reached our max capacity yet
                if (_entries.Keys.Count < Capacity) return;

                logger.LogDebug("MessagingEntityCache: Trimming entries");
                const int count = 10;
                // get the oldest n entries
                var removal = (from v in _entries.Values
                               orderby v.LastUsed ascending
                               select v).Take(count);

                foreach (var item in removal)
                {
                    lock (item) // need to lock each entry so that nobody messes with the reference count and other properties
                    {
                        if (item.ActiveCount == 0)
                        {
                            CloseEntity(logger, item, "");
                        }
                        else
                        {
                            item.ClosePending = true; // flad it so that the release function will close the connection
                        }
                    }
                }
            }
        }
    }
}
