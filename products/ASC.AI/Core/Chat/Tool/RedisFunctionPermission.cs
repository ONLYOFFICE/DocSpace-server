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

public abstract class RedisToolPermissionResolverBase
{
    protected const string ChannelName = "ai:function:permission";

    protected static string GetCacheKey(string callId)
    {
        return $"ai:function:call_data:{callId}";
    }
}

public class RedisToolPermissionRequester(
    IRedisClient redisClient,
    IFusionCache cache) : RedisToolPermissionResolverBase, IToolPermissionRequester
{
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _expirationTimeout = _waitTimeout * 1.5;

    public async Task<ToolExecutionDecision> RequestPermissionAsync(CallData callData, CancellationToken cancellationToken)
    {
        var database = redisClient.GetDefaultDatabase();
        var completionSource = new TaskCompletionSource<ToolExecutionDecision>();
        
        var cacheKey = GetCacheKey(callData.CallId);
        
        await cache.SetAsync(cacheKey, callData, _expirationTimeout, token: cancellationToken);
        
        var channel = new RedisChannel($"{ChannelName}:{callData.CallId}", RedisChannel.PatternMode.Auto);

        await database.SubscribeAsync<ToolExecutionDecision>(channel, decision =>
        {
            completionSource.SetResult(decision);
            return Task.CompletedTask;
        });
        
        try
        {
            return await completionSource.Task.WaitAsync(_waitTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            return ToolExecutionDecision.Deny;
        }
        finally
        {
            await database.UnsubscribeAsync<ToolExecutionDecision>(channel, _ => Task.CompletedTask);
        }
    }
}

public class RedisToolPermissionProvider(
    IRedisClient redisClient,
    IFusionCache cache) : RedisToolPermissionResolverBase, IToolPermissionProvider
{
    public async Task<CallData?> ProvidePermissionAsync(string callId, ToolExecutionDecision decision, Func<CallData, ValueTask>? beforeConfirm = null)
    {
        var cacheKey = GetCacheKey(callId);

        var callData = await cache.GetOrDefaultAsync<CallData>(cacheKey);

        if (callData != null && beforeConfirm != null)
        {
            await beforeConfirm(callData);
        }

        var database = redisClient.GetDefaultDatabase();

        var channel = new RedisChannel($"{ChannelName}:{callId}", RedisChannel.PatternMode.Auto);
        await database.PublishAsync(channel, decision);

        await cache.RemoveAsync(cacheKey);

        return callData;
    }
}