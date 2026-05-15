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

public class InMemoryToolPermissionRequester(IFusionCache cache) : IToolPermissionRequester
{
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _expirationTimeout = _waitTimeout * 1.5;

    internal static readonly ConcurrentDictionary<string, TaskCompletionSource<ToolExecutionDecision>> PendingRequests = new();

    public async Task<ToolExecutionDecision> RequestPermissionAsync(CallData callData, CancellationToken cancellationToken)
    {
        var cacheKey = $"ai:function:call_data:{callData.CallId}";
        await cache.SetAsync(cacheKey, callData, _expirationTimeout, token: cancellationToken);

        var completionSource = new TaskCompletionSource<ToolExecutionDecision>();
        PendingRequests[callData.CallId] = completionSource;

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
            PendingRequests.TryRemove(callData.CallId, out _);
        }
    }
}

public class InMemoryToolPermissionProvider(IFusionCache cache) : IToolPermissionProvider
{
    public async Task<CallData?> ProvidePermissionAsync(string callId, ToolExecutionDecision decision, Func<CallData, ValueTask>? beforeConfirm = null)
    {
        var cacheKey = $"ai:function:call_data:{callId}";
        var callData = await cache.GetOrDefaultAsync<CallData>(cacheKey);

        if (callData != null && beforeConfirm != null)
        {
            await beforeConfirm(callData);
        }

        if (InMemoryToolPermissionRequester.PendingRequests.TryGetValue(callId, out var completionSource))
        {
            completionSource.TrySetResult(decision);
        }

        await cache.RemoveAsync(cacheKey);

        return callData;
    }
}