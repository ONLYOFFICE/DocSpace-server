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

namespace ASC.Collections;

public sealed class HttpRequestDictionary<T> : CachedDictionaryBase<T>
{
    private const string StoreKey = "__HttpRequestDictionaryStore";

    private readonly ConcurrentDictionary<string, object> _store;

    public HttpRequestDictionary(HttpContext httpContext, string baseKey)
    {
        Condition = _ => true;
        BaseKey = baseKey;
        _store = GetStore(httpContext);
    }

    /// <summary>
    /// <see cref="HttpContext.Items"/> is a plain dictionary and is not thread-safe, while a single request
    /// may build its response from several threads at once. So the entries are kept in a concurrent dictionary
    /// stored under a single key. Only the creation of that key is locked, once per request; every later
    /// lookup is a lock-free read.
    /// </summary>
    private static ConcurrentDictionary<string, object> GetStore(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            return null;
        }

        var items = httpContext.Items;

        if (items.TryGetValue(StoreKey, out var store))
        {
            return (ConcurrentDictionary<string, object>)store;
        }

        lock (items)
        {
            if (items.TryGetValue(StoreKey, out store))
            {
                return (ConcurrentDictionary<string, object>)store;
            }

            var newStore = new ConcurrentDictionary<string, object>();
            items[StoreKey] = newStore;

            return newStore;
        }
    }

    protected override void Reset(string rootKey, string key)
    {
        _store?.TryRemove(BuildKey(key, rootKey), out _);
    }

    protected override void Add(string rootKey, string key, T newValue)
    {
        if (_store != null)
        {
            _store[BuildKey(key, rootKey)] = new CachedItem(newValue);
        }
    }

    protected override object GetObjectFromCache(string fullKey)
    {
        return _store != null && _store.TryGetValue(fullKey, out var cached) ? cached : null;
    }

    protected override bool FitsCondition(object cached)
    {
        return cached is CachedItem;
    }

    protected override T ReturnCached(object objectCache)
    {
        return ((CachedItem)objectCache).Value;
    }

    protected override void OnHit(string fullKey) { }

    protected override void OnMiss(string fullKey) { }

    private sealed class CachedItem
    {
        internal T Value { get; set; }

        internal CachedItem(T value)
        {
            Value = value;
        }
    }
}