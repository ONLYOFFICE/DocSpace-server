// (c) Copyright Ascensio System SIA 2010-2022
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

public sealed class HttpRequestDictionary<T> : CachedDictionaryBase<T>
{
    private readonly HttpContext _httpContext;

    public HttpRequestDictionary(HttpContext httpContext, string baseKey)
    {
        Condition = (_) => true;
        BaseKey = baseKey;
        _httpContext = httpContext;
    }

    protected override void Reset(string rootKey, string key)
    {
        if (_httpContext != null)
        {
            var builtkey = BuildKey(key, rootKey);
            _httpContext.Items[builtkey] = null;
        }
    }

    protected override void Add(string rootKey, string key, T newValue)
    {
        if (_httpContext != null)
        {
            var builtkey = BuildKey(key, rootKey);
            _httpContext.Items[builtkey] = new CachedItem(newValue);
        }
    }

    protected override object GetObjectFromCache(string fullKey)
    {
        return _httpContext?.Items[fullKey];
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