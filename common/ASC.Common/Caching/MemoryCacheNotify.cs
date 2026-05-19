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
public class MemoryCacheNotify<T> : ICacheNotify<T> where T : new()
{
    private readonly ConcurrentDictionary<string, List<Action<T>>> _actions = new();
    private readonly ConcurrentDictionary<string, List<Func<T, Task>>> _funcs = new();

    public async Task PublishAsync(T obj, CacheNotifyAction action)
    {
        if (_actions.TryGetValue(GetKey(action), out var onchange) && onchange != null)
        {
            Parallel.ForEach(onchange, a => a(obj));
        }
        
        if (_funcs.TryGetValue(GetKey(action), out var onchangeFunc) && onchangeFunc != null)
        {
            await Task.WhenAll(onchangeFunc.Select(a => a(obj)));
        }
    }

    public void Subscribe(Action<T> onchange, CacheNotifyAction notifyAction)
    {
        if (onchange != null)
        {
            _actions.GetOrAdd(GetKey(notifyAction), []).Add(onchange);
        }
    }

    public void Subscribe(Func<T, Task> onchange, CacheNotifyAction action)
    {
        if (onchange != null)
        {
            _funcs.GetOrAdd(GetKey(action), []).Add(onchange);
        }
    }

    public void Unsubscribe(CacheNotifyAction notifyAction)
    {
        _actions.TryRemove(GetKey(notifyAction), out _);
        _funcs.TryRemove(GetKey(notifyAction), out _);
    }

    private string GetKey(CacheNotifyAction notifyAction)
    {
        return $"asc:channel:{notifyAction}:{typeof(T).FullName}".ToLower();
    }
}