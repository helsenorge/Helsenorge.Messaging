using Microsoft.Extensions.Caching.Distributed;

namespace Helsenorge.Messaging.Server
{
    public class DistributedCacheFactory
    {
        public static IDistributedCache Create()
        {
            return new MemoryDistributedCache(new MemoryDistributedCacheOptions());
        }

        private class MemoryDistributedCacheOptions : Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions>
        {
            public Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions Value => new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions();
        }
    }
}
