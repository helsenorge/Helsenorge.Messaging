using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Helsenorge.Messaging.Server
{
    public class HNMemoryDistributedCacheOptions : IOptions<MemoryDistributedCacheOptions>
    {
        public MemoryDistributedCacheOptions Value => new MemoryDistributedCacheOptions();
    }
}
