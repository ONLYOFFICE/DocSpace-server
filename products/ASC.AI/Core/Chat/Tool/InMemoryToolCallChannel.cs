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

namespace ASC.AI.Core.Chat.Tool;

public class InMemoryToolCallReceiver(IFusionCache cache) : IToolCallReceiver
{
    internal static readonly ConcurrentDictionary<string, object> PendingCalls = new();

    private static readonly TimeSpan _expirationTimeout = TimeSpan.FromMinutes(5);

    public async Task<IToolCallWaiter<T>> SubscribeAsync<T>(CallData callData, CancellationToken cancellationToken)
    {
        await cache.SetAsync(GetCacheKey(callData.CallId), callData, _expirationTimeout, token: cancellationToken);

        var completionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        PendingCalls[callData.CallId] = completionSource;

        return new InMemoryToolCallWaiter<T>(callData.CallId, completionSource);
    }

    internal static string GetCacheKey(string callId)
    {
        return $"ai:tool:call_data:{callId}";
    }
}

public class InMemoryToolCallPublisher(IFusionCache cache) : IToolCallPublisher
{
    public async Task<T?> PublishAsync<T>(string callId, Func<CallData, ValueTask<T>> resultFactory)
    {
        var cacheKey = InMemoryToolCallReceiver.GetCacheKey(callId);
        var callData = await cache.GetOrDefaultAsync<CallData>(cacheKey);
        if (callData == null)
        {
            return default;
        }

        var payload = await resultFactory(callData);

        if (InMemoryToolCallReceiver.PendingCalls.TryGetValue(callId, out var pending) &&
            pending is TaskCompletionSource<T> completionSource)
        {
            completionSource.TrySetResult(payload);
        }

        await cache.RemoveAsync(cacheKey);

        return payload;
    }
}

internal sealed class InMemoryToolCallWaiter<T>(
    string callId,
    TaskCompletionSource<T> completionSource) : IToolCallWaiter<T>
{
    public Task<T> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return completionSource.Task.WaitAsync(timeout, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        InMemoryToolCallReceiver.PendingCalls.TryRemove(callId, out _);
        return ValueTask.CompletedTask;
    }
}
