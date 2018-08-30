using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Helsenorge.Registries.Tests
{
    public class HNMemoryDistributedCacheOptions : IOptions<MemoryDistributedCacheOptions>
    {
        public MemoryDistributedCacheOptions Value => new MemoryDistributedCacheOptions();
    }
}
