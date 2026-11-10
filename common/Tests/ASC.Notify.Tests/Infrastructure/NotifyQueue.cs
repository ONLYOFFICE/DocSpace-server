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

namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// The letter host's notify queue: what a notify client hands over to the engine, one request per recipient. The
/// host is never run, so nothing else drains it; a case takes the requests of its own object and puts every other
/// request back for the case it belongs to. Cases that read the queue share <see cref="NotifyQueueCollection"/>.
/// </summary>
internal static class NotifyQueue
{
    private static readonly SemaphoreSlim _gate = new(1, 1);

    public static async Task<List<NotifyRequest>> TakeAsync(IServiceProvider services, string objectId)
    {
        var reader = services.GetRequiredService<ChannelReader<NotifyRequest>>();
        var writer = services.GetRequiredService<ChannelWriter<NotifyRequest>>();

        var ours = new List<NotifyRequest>();
        var foreign = new List<NotifyRequest>();

        await _gate.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            while (reader.TryRead(out var request))
            {
                (request.ObjectID == objectId ? ours : foreign).Add(request);
            }

            foreach (var request in foreign)
            {
                writer.TryWrite(request);
            }
        }
        finally
        {
            _gate.Release();
        }

        return ours;
    }

    /// <summary>
    /// What <c>NotifyEngine.SendGroupNotify</c> asks before a request reaches a channel: a request that checks
    /// subscriptions goes out unless the recipient unsubscribed from the request's own action.
    /// </summary>
    public static async Task<bool> PassesEngineSubscriptionCheckAsync(IServiceProvider services, NotifyRequest request)
    {
        if (!request._isNeedCheckSubscriptions)
        {
            return true;
        }

        using var engineScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        await engineScope.ServiceProvider.GetRequiredService<TenantManager>().SetCurrentTenantAsync(request._tenantId);

        var subscriptions = request.GetSubscriptionProvider(engineScope);

        return !await subscriptions.IsUnsubscribeAsync((IDirectRecipient)request.Recipient, request.NotifyAction, request.ObjectID);
    }
}

[CollectionDefinition(nameof(NotifyQueueCollection))]
public sealed class NotifyQueueCollection;
