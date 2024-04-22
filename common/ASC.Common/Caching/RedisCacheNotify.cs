// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Common.Caching;

[Singleton]
public class RedisCacheNotify<T>(IRedisClient redisCacheClient) : ICacheNotify<T>
    where T : new()
{
    private readonly IRedisDatabase _redis = redisCacheClient.GetDefaultDatabase();
    private readonly ConcurrentDictionary<CacheNotifyAction, ConcurrentBag<Action<T>>> _invocationList = new();
    private readonly Guid _instanceId = Guid.NewGuid();


    public void Publish(T obj, CacheNotifyAction action)
    {
        Task.Run(async () => await _redis.PublishAsync(GetChannelName(), new RedisCachePubSubItem<T> { Id = _instanceId, Object = obj, Action = action }))
            .GetAwaiter()
            .GetResult();

        foreach (var handler in GetInvocationList(action))
        {
            handler(obj);
        }
    }

    public async Task PublishAsync(T obj, CacheNotifyAction action)
    {
        await Task.Run(async () => await _redis.PublishAsync(GetChannelName(), new RedisCachePubSubItem<T> { Id = _instanceId, Object = obj, Action = action }));

        foreach (var handler in GetInvocationList(action))
        {
            handler(obj);
        }
    }

    public void Subscribe(Action<T> onChange, CacheNotifyAction action)
    {
        Task.Run(async () => await _redis.SubscribeAsync<RedisCachePubSubItem<T>>(GetChannelName(), i =>
        {
            if (i.Id != _instanceId && (i.Action == action || Enum.IsDefined(typeof(CacheNotifyAction), (i.Action & action))))
            {
                onChange(i.Object);
            }

            return Task.FromResult(true);
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

        foreach (var val in (CacheNotifyAction[])Enum.GetValues(typeof(CacheNotifyAction)))
        {
            if (!(val == action || Enum.IsDefined(typeof(CacheNotifyAction), (val & action))))
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