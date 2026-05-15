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

public abstract class CachedDictionaryBase<T>
{
    protected string BaseKey { get; init; }
    protected Func<T, bool> Condition { get; init; }

    public void Reset(string key)
    {
        Reset(string.Empty, key);
    }

    public T Get(string key)
    {
        return Get(string.Empty, key, null);
    }

    private T Get(string rootKey, string key, Func<T> defaults)
    {
        var fullKey = BuildKey(key, rootKey);
        var objectCache = GetObjectFromCache(fullKey);

        if (FitsCondition(objectCache))
        {
            OnHit(fullKey);

            return ReturnCached(objectCache);
        }

        if (defaults != null)
        {
            OnMiss(fullKey);
            var newValue = defaults();

            if (Condition == null || Condition(newValue))
            {
                Add(rootKey, key, newValue);
            }

            return newValue;
        }

        return default;
    }

    public void Add(string key, T newValue)
    {
        Add(string.Empty, key, newValue);
    }

    protected abstract void Add(string rootKey, string key, T newValue);

    protected abstract void Reset(string rootKey, string key);

    protected virtual bool FitsCondition(object cached)
    {
        return cached is T;
    }

    protected virtual T ReturnCached(object objectCache)
    {
        return (T)objectCache;
    }

    protected string BuildKey(string key, string rootKey)
    {
        return $"{BaseKey}-{rootKey}-{key}";
    }

    protected abstract object GetObjectFromCache(string fullKey);

    [Conditional("DEBUG")]
    protected virtual void OnHit(string fullKey)
    {
        Debug.Print("cache hit:{0}", fullKey);
    }

    [Conditional("DEBUG")]
    protected virtual void OnMiss(string fullKey)
    {
        Debug.Print("cache miss:{0}", fullKey);
    }
}