using Microsoft.Extensions.Caching.Distributed;

namespace Helsenorge.Messaging.Tests
{
    public class DistributedCacheFactory
    {
        public static IDistributedCache Create()
        {
            return CreateForNet471();
        }

        private static IDistributedCache CreateForNet471()
        {
            return new MemoryDistributedCache(new Net471MemoryDistributedCacheOptions());
        }

        private class Net471MemoryDistributedCacheOptions : Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions>
        {
            public Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions Value => new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions();
        }
    }
}
