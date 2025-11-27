// (c) Copyright Ascensio System SIA 2009-2025
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

[Singleton(typeof(IToolPermissionRequester))]
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

[Singleton(typeof(IToolPermissionProvider))]
public class RedisToolPermissionProvider(
    IRedisClient redisClient,
    IFusionCache cache) : RedisToolPermissionResolverBase, IToolPermissionProvider
{
    public async Task<CallData?> ProvidePermissionAsync(string callId, ToolExecutionDecision decision)
    {
        var database = redisClient.GetDefaultDatabase();
        
        var channel = new RedisChannel($"{ChannelName}:{callId}", RedisChannel.PatternMode.Auto);
        await database.PublishAsync(channel, decision);
        
        var cacheKey = GetCacheKey(callId);

        var callData = await cache.GetOrDefaultAsync<CallData>(cacheKey);

        await cache.RemoveAsync(cacheKey);
        
        return callData;
    }
}