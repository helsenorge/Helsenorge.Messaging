﻿/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

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
