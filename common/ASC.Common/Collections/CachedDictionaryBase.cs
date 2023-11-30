// (c) Copyright Ascensio System SIA 2010-2023
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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