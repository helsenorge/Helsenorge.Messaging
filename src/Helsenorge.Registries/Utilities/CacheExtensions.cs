/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries.Utilities
{
    internal static class CacheExtensions
    {
        public static async Task<T> ReadValueFromCacheAsync<T>(
            ILogger logger,
            IDistributedCache cache,
            string key,
            CacheFormatterType formatter = CacheFormatterType.BinaryFormatter)
            where T : class
        {
            try
            {
                var cached = await cache.GetAsync(key).ConfigureAwait(false);

                if (cached is null)
                    return default;

                return await ByteArrayToObjectAsync<T>(cached, formatter).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(1, ex, $"Failed reading value {key} from cache");
                return default;
            }
        }

        public static async Task WriteValueToCacheAsync(
            ILogger logger,
            IDistributedCache cache,
            string key, object value,
            TimeSpan expires,
            CacheFormatterType formatter = CacheFormatterType.BinaryFormatter)
        {
            if (expires == TimeSpan.Zero) return;
            if (value == null) return;

            var options = new DistributedCacheEntryOptions();
            options.SetAbsoluteExpiration(expires);

            logger.LogDebug("WriteValueToCache key {0}", key);

            try
            {
                await cache.SetAsync(key, ObjectToByteArray(value, formatter), options).ConfigureAwait(false);
                logger.LogDebug("WriteValueToCache key {0} complete", key);
            }
            catch (Exception ex)
            {
                logger.LogWarning(1, ex, $"Failed writing value {key} to cache.");
            }
        }

        private static byte[] ObjectToByteArray(object value, CacheFormatterType formatter)
        {
            if (value.GetType() == typeof(byte[])) return value as byte[];

            switch (formatter)
            {
                case CacheFormatterType.BinaryFormatter:
                    return ObjectToByteArray(value);
                case CacheFormatterType.XmlFormatter:
                    return XmlCacheFormatter.Serialize(value);
                default:
                    throw new ArgumentException("Invalid cache formatter");
            }
        }

        private static async Task<T> ByteArrayToObjectAsync<T>(byte[] value, CacheFormatterType formatter)
            where T : class
        {
            if (typeof(T) == typeof(byte[])) return value as T;

            switch (formatter)
            {
                case CacheFormatterType.BinaryFormatter:
                    return await ByteArrayToObjectAsync<T>(value);
                case CacheFormatterType.XmlFormatter:
                    return await XmlCacheFormatter.DeserializeAsync<T>(value);
                default:
                    throw new ArgumentException("Invalid cache formatter");
            }
        }

        private static byte[] ObjectToByteArray(object value)
        {
            var formatter = new BinaryFormatter();
            using var memoryStream = new MemoryStream();
#pragma warning disable SYSLIB0011
            formatter.Serialize(memoryStream, value);
#pragma warning restore SYSLIB0011
            return memoryStream.ToArray();
        }

        private static async Task<T> ByteArrayToObjectAsync<T>(byte[] value) where T : class
        {
            using var memoryStream = new MemoryStream();
            var formatter = new BinaryFormatter();
            await memoryStream.WriteAsync(value, 0, value.Length).ConfigureAwait(false);
            memoryStream.Seek(0, SeekOrigin.Begin);
#pragma warning disable SYSLIB0011
            return formatter.Deserialize(memoryStream) as T;
#pragma warning restore SYSLIB0011
        }
    }
}
