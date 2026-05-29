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

using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace ASC.AI.Core.Chat.Tool;

public abstract class RedisToolCallResolverBase
{
    protected const string ChannelName = "ai:tool:call";

    protected static readonly TimeSpan ExpirationTimeout = TimeSpan.FromMinutes(5);

    protected static string GetCacheKey(string callId)
    {
        return $"ai:tool:call_data:{callId}";
    }

    protected static RedisChannel GetChannel(string callId)
    {
        return new RedisChannel($"{ChannelName}:{callId}", RedisChannel.PatternMode.Auto);
    }
}

public class RedisToolCallReceiver(
    IRedisClient redisClient,
    IFusionCache cache) : RedisToolCallResolverBase, IToolCallReceiver
{
    public async Task<IToolCallWaiter<T>> SubscribeAsync<T>(CallData callData, CancellationToken cancellationToken)
    {
        await cache.SetAsync(GetCacheKey(callData.CallId), callData, ExpirationTimeout, token: cancellationToken);

        var database = redisClient.GetDefaultDatabase();
        var completionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var channel = GetChannel(callData.CallId);

        await database.SubscribeAsync<T>(channel, payload =>
        {
            completionSource.TrySetResult(payload!);
            return Task.CompletedTask;
        });

        return new RedisToolCallWaiter<T>(database, channel, completionSource);
    }
}

public class RedisToolCallPublisher(
    IRedisClient redisClient,
    IFusionCache cache) : RedisToolCallResolverBase, IToolCallPublisher
{
    public async Task<T?> PublishAsync<T>(string callId, Func<CallData, ValueTask<T>> resultFactory)
    {
        var cacheKey = GetCacheKey(callId);
        var callData = await cache.GetOrDefaultAsync<CallData>(cacheKey);
        if (callData == null)
        {
            return default;
        }

        var payload = await resultFactory(callData);

        var database = redisClient.GetDefaultDatabase();
        await database.PublishAsync(GetChannel(callId), payload);

        await cache.RemoveAsync(cacheKey);

        return payload;
    }
}

internal sealed class RedisToolCallWaiter<T>(
    IRedisDatabase database,
    RedisChannel channel,
    TaskCompletionSource<T> completionSource) : IToolCallWaiter<T>
{
    public Task<T> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return completionSource.Task.WaitAsync(timeout, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(database.UnsubscribeAsync<T>(channel, _ => Task.CompletedTask));
    }
}
