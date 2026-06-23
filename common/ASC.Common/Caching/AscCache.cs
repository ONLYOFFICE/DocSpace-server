// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Common.Caching;

[Singleton]
public class AscCacheNotify
{
    private readonly ICacheNotify<AscCacheItem> _cacheNotify;
    private readonly ICache _cache;

    public AscCacheNotify(ICacheNotify<AscCacheItem> cacheNotify, ICache cache)
    {
        _cacheNotify = cacheNotify;
        _cache = cache;

        _cacheNotify.Subscribe(_ => { OnClearCache(); }, CacheNotifyAction.Any);
    }

    public async Task ClearCacheAsync() => await _cacheNotify.PublishAsync(new AscCacheItem { Id = Guid.NewGuid().ToString() }, CacheNotifyAction.Any);

    public void OnClearCache()
    {
        _cache.Reset();
    }
}

[Singleton(typeof(ICache))]
public sealed class AscCache(IMemoryCache memoryCache) : ICache, IDisposable
{
    private CancellationTokenSource _resetCacheToken = new();
    private bool _disposed;

    public T Get<T>(string key) where T : class
    {
        return memoryCache.Get<T>(key);
    }

    public void Insert(string key, object value, TimeSpan slidingExpiration, Action<object, object, EvictionReason, object> evictionCallback = null)
    {
        Insert(key, value, slidingExpiration, null, evictionCallback);
    }

    public void Insert(string key, object value, DateTime absolutExpiration, Action<object, object, EvictionReason, object> evictionCallback = null)
    {
        Insert(key, value, null, absolutExpiration, evictionCallback);
    }

    public void Remove(string key)
    {
        if (_disposed)
        {
            return;
        }

        memoryCache.Remove(key);
    }

    public void Remove(ConcurrentDictionary<string, object> keys, Regex pattern)
    {
        if (_disposed)
        {
            return;
        }

        var copy = keys.ToDictionary(p => p.Key, p => p.Value);
        var matchedKeys = copy.Select(p => p.Key).Where(k => pattern.IsMatch(k));

        foreach (var key in matchedKeys)
        {
            memoryCache.Remove(key);
        }
    }

    public void Reset()
    {
        if (_disposed)
        {
            return;
        }

        if (_resetCacheToken is { IsCancellationRequested: false, Token.CanBeCanceled: true })
        {
            _resetCacheToken.Cancel();
            _resetCacheToken.Dispose();
        }

        _resetCacheToken = new CancellationTokenSource();
    }

    public ConcurrentDictionary<string, T> HashGetAll<T>(string key) => memoryCache.GetOrCreate(key, _ => new ConcurrentDictionary<string, T>());

    public T HashGet<T>(string key, string field)
    {
        if (_disposed)
        {
            return default;
        }

        if (memoryCache.TryGetValue<ConcurrentDictionary<string, T>>(key, out var dic)
            && dic.TryGetValue(field, out var value))
        {
            return value;
        }

        return default;
    }

    public void HashSet<T>(string key, string field, T value)
    {
        if (_disposed)
        {
            return;
        }

        var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(DateTime.MaxValue)
                .AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));

        var dic = HashGetAll<T>(key);

        if (value != null)
        {
            dic.AddOrUpdate(field, value, (_, _) => value);

            memoryCache.Set(key, dic, options);
        }
        else if (dic != null)
        {
            dic.TryRemove(field, out _);

            if (dic.IsEmpty)
            {
                memoryCache.Remove(key);
            }
            else
            {
                memoryCache.Set(key, dic, options);
            }
        }
    }

    private void Insert(string key, object value, TimeSpan? sligingExpiration = null, DateTime? absolutExpiration = null, Action<object, object, EvictionReason, object> evictionCallback = null)
    {
        if (_disposed)
        {
            return;
        }

        var options = new MemoryCacheEntryOptions()
            .AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));

        if (sligingExpiration.HasValue)
        {
            options = options.SetSlidingExpiration(sligingExpiration.Value);
        }

        if (absolutExpiration.HasValue)
        {
            options = options.SetAbsoluteExpiration(absolutExpiration.Value == DateTime.MaxValue ? DateTimeOffset.MaxValue : new DateTimeOffset(absolutExpiration.Value));
        }

        if (evictionCallback != null)
        {
            options = options.RegisterPostEvictionCallback(new PostEvictionDelegate(evictionCallback));
        }

        memoryCache.Set(key, value, options);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _disposed = true;
            memoryCache?.Dispose();
            _resetCacheToken?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~AscCache()
    {
        Dispose(false);
    }
}