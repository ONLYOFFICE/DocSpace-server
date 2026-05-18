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
public class RedisCacheNotify<T>(IRedisClient redisCacheClient, ILogger<RedisCacheNotify<T>> logger) : ICacheNotify<T> where T : new()
{
    private readonly IRedisDatabase _redis = redisCacheClient.GetDefaultDatabase();
    private readonly ConcurrentDictionary<CacheNotifyAction, ConcurrentBag<Action<T>>> _invocationList = new();
    private readonly ConcurrentDictionary<CacheNotifyAction, ConcurrentBag<Func<T, Task>>> _invocationListTasks = new();
    private readonly Guid _instanceId = Guid.NewGuid();

    public async Task PublishAsync(T obj, CacheNotifyAction action)
    {
        await _redis.PublishAsync(GetChannelName(), new RedisCachePubSubItem<T> { Id = _instanceId, Object = obj, Action = action });

        foreach (var handler in GetInvocationList(action))
        {
            try
            {
                handler(obj);
            }
            catch (Exception e)
            {
                logger.ErrorRedisCacheNotifyPublish(e);
            }
        }
        
        await foreach (var handler in Task.WhenEach(GetInvocationListTasks(action).Select(handler => handler(obj))))
        {
            try
            {
                await handler;
            }
            catch (Exception e)
            {
                logger.ErrorRedisCacheNotifyPublish(e);
            }
        }
    }

    public void Subscribe(Action<T> onChange, CacheNotifyAction action)
    {
        Task.Run(async () => await _redis.SubscribeAsync<RedisCachePubSubItem<T>>(GetChannelName(), i =>
        {
            if (i.Id != _instanceId && (i.Action == action || Enum.IsDefined(i.Action & action)))
            {
                try
                {
                    onChange(i.Object);
                }
                catch (Exception e)
                {
                    logger.ErrorRedisCacheNotifySubscribe(e);
                }
            }

            return Task.FromResult(true);
        })).GetAwaiter()
          .GetResult();

        AddToInInvocationList(onChange, action);
    }
    public void Subscribe(Func<T, Task> onChange, CacheNotifyAction action)
    {
        Task.Run(async () => await _redis.SubscribeAsync<RedisCachePubSubItem<T>>(GetChannelName(), async i =>
        {
            if (i.Id != _instanceId && (i.Action == action || Enum.IsDefined(i.Action & action)))
            {
                try
                {
                    await onChange(i.Object);
                }
                catch (Exception e)
                {
                    logger.ErrorRedisCacheNotifySubscribe(e);
                }
            }
        })).GetAwaiter()
          .GetResult();

        AddToInInvocationList(onChange, action);
    }

    public void Unsubscribe(CacheNotifyAction action)
    {
        Task.Run(async () => await _redis.UnsubscribeAsync<RedisCachePubSubItem<T>>(GetChannelName(), _ => Task.FromResult(true))).GetAwaiter()
          .GetResult();

        _invocationList.TryRemove(action, out _);
    }

    private static RedisChannel GetChannelName()
    {
        var pattern = $"asc:channel:{typeof(T).FullName}".ToLower(CultureInfo.InvariantCulture);

        var redisChannel = new RedisChannel(pattern, RedisChannel.PatternMode.Pattern);

        return redisChannel;
    }

    private List<Action<T>> GetInvocationList(CacheNotifyAction action)
    {
        var result = new List<Action<T>>();

        foreach (var val in Enum.GetValues<CacheNotifyAction>())
        {
            if (!(val == action || Enum.IsDefined(val & action)))
            {
                continue;
            }

            if (_invocationList.TryGetValue(val, out var handlers))
            {
                result.AddRange(handlers);
            }

        }

        return result;
    }

    private List<Func<T, Task>> GetInvocationListTasks(CacheNotifyAction action)
    {
        var result = new List<Func<T, Task>>();

        foreach (var val in Enum.GetValues<CacheNotifyAction>())
        {
            if (!(val == action || Enum.IsDefined(val & action)))
            {
                continue;
            }

            if (_invocationListTasks.TryGetValue(val, out var handlers))
            {
                result.AddRange(handlers);
            }
        }

        return result;
    }

    private void AddToInInvocationList(Action<T> onChange, CacheNotifyAction action)
    {
        if (onChange != null)
        {
            _invocationList.AddOrUpdate(action,
                [onChange],
                (_, bag) =>
                {
                    bag.Add(onChange);
                    return bag;
                });
        }
        else
        {
            _invocationList.TryRemove(action, out _);
        }
    }
    private void AddToInInvocationList(Func<T, Task> onChange, CacheNotifyAction action)
    {
        if (onChange != null)
        {
            _invocationListTasks.AddOrUpdate(action,
                [onChange],
                (_, bag) =>
                {
                    bag.Add(onChange);
                    return bag;
                });
        }
        else
        {
            _invocationListTasks.TryRemove(action, out _);
        }
    }


    [ProtoContract]
    public record RedisCachePubSubItem<TObject>
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public TObject Object { get; set; }

        [ProtoMember(3)]
        public CacheNotifyAction Action { get; set; }
    }
}