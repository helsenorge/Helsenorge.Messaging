using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Helsenorge.Registries.Tests.Mocks
{
    public class PartlyMockedDistributedCache : IDistributedCache
    {
        private readonly IDistributedCache _distributedCache;

        public PartlyMockedDistributedCache(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public byte[] Get(string key)
        {
            return _distributedCache.Get(key);
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = new CancellationToken())
        {
            if (key.EndsWith("93253"))
            {
                var fullPath = TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}CPA_93253_Cache.bin");
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"File not found {fullPath}");

                return Task.FromResult(File.ReadAllBytes(fullPath));
            }

            if (key.EndsWith("93239"))
            {
                var fullPath = TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}CPP_93239_Cache.bin");
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"File not found {fullPath}");

                return Task.FromResult(File.ReadAllBytes(fullPath));
            }

            return _distributedCache.GetAsync(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _distributedCache.Set(key, value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = new CancellationToken())
        {
            if (key.EndsWith("93253"))
            {
                return Task.CompletedTask;
            }

            if (key.EndsWith("93239"))
            {
                return Task.CompletedTask;
            }

            return _distributedCache.SetAsync(key, value, options, token);
        }

        public void Refresh(string key)
        {
            _distributedCache.Refresh(key);
        }

        public Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
        {
            return _distributedCache.RefreshAsync(key, token);
        }

        public void Remove(string key)
        {
            _distributedCache.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
        {
            return _distributedCache.RemoveAsync(key, token);
        }
    }
}
