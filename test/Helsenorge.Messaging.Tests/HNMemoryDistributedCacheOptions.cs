using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Helsenorge.Messaging.Tests
{
    public class HNMemoryDistributedCacheOptions : IOptions<MemoryDistributedCacheOptions>
    {
        public MemoryDistributedCacheOptions Value => new MemoryDistributedCacheOptions();
    }
}
