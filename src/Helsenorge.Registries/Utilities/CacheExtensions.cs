/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries.Utilities
{
    /// <summary>
    /// Extension methods for IDistributedCache
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// Reads a value with the specified key from cache.
        /// </summary>
        public static async Task<T> ReadValueFromCacheAsync<T>(
            ILogger logger,
            IDistributedCache cache,
            string key)
            where T : class
        {
            try
            {
                var cached = await cache.GetAsync(key).ConfigureAwait(false);

                if (cached is null)
                    return default;

                return await ByteArrayToObjectAsync<T>(cached).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(1, ex, $"Failed reading value {key} from cache");
                return default;
            }
        }

        /// <summary>
        /// Writes the supplied value to cache using the specified key.
        /// </summary>
        public static async Task WriteValueToCacheAsync(
            ILogger logger,
            IDistributedCache cache,
            string key, object value,
            TimeSpan expires)
        {
            if (expires == TimeSpan.Zero) return;
            if (value == null) return;

            var options = new DistributedCacheEntryOptions();
            options.SetAbsoluteExpiration(expires);

            logger.LogDebug("WriteValueToCache key {0}", key);

            try
            {
                await cache.SetAsync(key, ObjectToByteArray(value), options).ConfigureAwait(false);
                logger.LogDebug("WriteValueToCache key {0} complete", key);
            }
            catch (Exception ex)
            {
                logger.LogWarning(1, ex, $"Failed writing value {key} to cache.");
            }
        }

        private static byte[] ObjectToByteArray(object value)
        {
            if (value.GetType() == typeof(byte[])) return value as byte[];

            return XmlCacheFormatter.Serialize(value);
        }

        private static async Task<T> ByteArrayToObjectAsync<T>(byte[] value)
            where T : class
        {
            if (typeof(T) == typeof(byte[])) return value as T;

            return await XmlCacheFormatter.DeserializeAsync<T>(value);
        }
    }
}
