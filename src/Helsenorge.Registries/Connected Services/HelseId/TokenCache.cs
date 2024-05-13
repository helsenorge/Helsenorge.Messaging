/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Concurrent;

namespace Helsenorge.Registries.Connected_Services.HelseId;

public static class TokenCache
{
    private static readonly ConcurrentDictionary<string, (string Token, DateTime Expiry)> _cache = new();

    public static bool TryGetToken(string key, out string token)
    {
        if (_cache.TryGetValue(key, out var cacheEntry) && DateTime.UtcNow < cacheEntry.Expiry)
        {
            token = cacheEntry.Token;
            return true;
        }

        token = null;
        return false;
    }

    public static void SetToken(string key, string token, int seconds)
    {
        var expiry = DateTime.UtcNow.AddSeconds(seconds);
        _cache[key] = (token, expiry);
    }
}