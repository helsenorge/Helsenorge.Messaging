/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

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
