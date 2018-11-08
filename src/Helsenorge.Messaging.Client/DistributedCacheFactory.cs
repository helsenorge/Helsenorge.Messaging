using Microsoft.Extensions.Caching.Distributed;

namespace Helsenorge.Messaging.Client
{
    public class DistributedCacheFactory
    {
        public static IDistributedCache Create()
        {
            
#if NET46
            return CreateForNet46();
#elif NET471
            return CreateForNet471();
#else
            throw new System.NotImplementedException("Unsupported target framework");
#endif

        }

#if NET46
        private static IDistributedCache CreateForNet46()
        {
            var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            return new MemoryDistributedCache(memoryCache);
        }
#elif NET471
        private static IDistributedCache CreateForNet471()
        {
            return new MemoryDistributedCache(new Net471MemoryDistributedCacheOptions());
        }

        private class Net471MemoryDistributedCacheOptions : Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions>
        {
            public Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions Value => new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions();
        }
#endif
    }
}
