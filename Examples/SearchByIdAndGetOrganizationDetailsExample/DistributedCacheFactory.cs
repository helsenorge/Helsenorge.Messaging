/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace SearchByIdAndGetOrganizationDetailsExample
{
    public class DistributedCacheFactory
    {
        public static IDistributedCache Create()
        {
            return new MemoryDistributedCache(new MemoryDistributedCacheOptions());
        }

        private class MemoryDistributedCacheOptions : IOptions<Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions>
        {
            public Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions Value => new();
        }
    }
}
