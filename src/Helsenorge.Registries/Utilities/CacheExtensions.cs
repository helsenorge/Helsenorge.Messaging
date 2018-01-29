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
        public static async Task<T> ReadValueFromCache<T>(ILogger logger, IDistributedCache cache, string key) where T : class
        {
            var cached = cache.Get(key);
            try
            {
                return cached != null ? ByteArrayToObject<T>(cached) : default(T);
            }
            catch (Exception ex)
            {
                logger.LogWarning(1, ex, $"Failed reading value {key} from cache");
                return default(T);
            }
        }

        public static async Task WriteValueToCache(ILogger logger, IDistributedCache cache, string key, object value, TimeSpan expires)
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

            var formatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, value);
                return memoryStream.ToArray();
            }
        }
        private static T ByteArrayToObject<T>(byte[] value) where T : class
        {
            if (typeof(T) == typeof(byte[])) return value as T;

            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                memoryStream.Write(value, 0, value.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(memoryStream) as T;
            }
        }
    }
}
