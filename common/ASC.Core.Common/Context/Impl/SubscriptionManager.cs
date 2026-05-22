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

using AuthConstants = ASC.Common.Security.Authorizing.AuthConstants;

namespace ASC.Core;

[Scope]
public class SubscriptionManager(CachedSubscriptionService service, TenantManager tenantManager, ICache cache)
{
    private readonly CachedSubscriptionService _service = service ?? throw new ArgumentNullException(nameof(service));
    private static readonly SemaphoreSlim _semaphore = new(1);
    public static readonly List<Guid> Groups = Groups =
    [
        AuthConstants.DocSpaceAdmin.ID,
        AuthConstants.Everyone.ID,
        AuthConstants.RoomAdmin.ID,
        AuthConstants.User.ID
    ];

    public async Task SubscribeAsync(string sourceID, string actionID, string objectID, string recipientID)
    {
        var s = new SubscriptionRecord
        {
            Tenant = GetTenant(),
            Subscribed = true
        };

        if (sourceID != null)
        {
            s.SourceId = sourceID;
        }

        if (actionID != null)
        {
            s.ActionId = actionID;
        }

        if (recipientID != null)
        {
            s.RecipientId = recipientID;
        }

        if (objectID != null)
        {
            s.ObjectId = objectID;
        }

        await _service.SaveSubscriptionAsync(s);
    }

    public async Task UnsubscribeAsync(string sourceID, string actionID, string objectID, string recipientID)
    {
        var s = new SubscriptionRecord
        {
            Tenant = GetTenant(),
            Subscribed = false
        };

        if (sourceID != null)
        {
            s.SourceId = sourceID;
        }

        if (actionID != null)
        {
            s.ActionId = actionID;
        }

        if (recipientID != null)
        {
            s.RecipientId = recipientID;
        }

        if (objectID != null)
        {
            s.ObjectId = objectID;
        }

        await _service.SaveSubscriptionAsync(s);
    }

    public async Task UnsubscribeAllAsync(string sourceID, string actionID, string objectID)
    {
        await _service.RemoveSubscriptionsAsync(GetTenant(), sourceID, actionID, objectID);
    }

    public async Task UnsubscribeAllAsync(string sourceID, string actionID)
    {
        await _service.RemoveSubscriptionsAsync(GetTenant(), sourceID, actionID);
    }

    public async Task<string[]> GetSubscriptionMethodAsync(string sourceID, string actionID, string recipientID)
    {
        IEnumerable<SubscriptionMethod> methods;

        if (Groups.Exists(r => r.ToString() == recipientID))
        {
            methods = await GetDefaultSubscriptionMethodsFromCacheAsync(sourceID, actionID, recipientID);
        }
        else
        {
            methods = await _service.GetSubscriptionMethodsAsync(GetTenant(), sourceID, actionID, recipientID);
        }

        var m = methods.FirstOrDefault(x => x.Action.Equals(actionID, StringComparison.OrdinalIgnoreCase)) ??
                methods.FirstOrDefault();

        return m != null ? m.Methods : [];
    }

    public async Task<string[]> GetRecipientsAsync(string sourceID, string actionID, string objectID)
    {
        return await _service.GetRecipientsAsync(GetTenant(), sourceID, actionID, objectID);
    }

    public async Task<object> GetSubscriptionRecordAsync(string sourceID, string actionID, string recipientID, string objectID)
    {
        return await _service.GetSubscriptionAsync(GetTenant(), sourceID, actionID, recipientID, objectID);
    }

    public async Task<string[]> GetSubscriptionsAsync(string sourceID, string actionID, string recipientID, bool checkSubscribe = true)
    {
        return await _service.GetSubscriptionsAsync(GetTenant(), sourceID, actionID, recipientID, checkSubscribe);
    }

    public async Task<bool> IsUnsubscribeAsync(string sourceID, string recipientID, string actionID, string objectID)
    {
        return await _service.IsUnsubscribeAsync(GetTenant(), sourceID, actionID, recipientID, objectID);
    }

    public async Task UpdateSubscriptionMethodAsync(string sourceID, string actionID, string recipientID, string[] senderNames)
    {
        var m = new SubscriptionMethod
        {
            Tenant = GetTenant()
        };

        if (sourceID != null)
        {
            m.Source = sourceID;
        }

        if (actionID != null)
        {
            m.Action = actionID;
        }

        if (recipientID != null)
        {
            m.Recipient = recipientID;
        }

        if (senderNames != null)
        {
            m.Methods = senderNames;
        }


        await _service.SetSubscriptionMethodAsync(m);
    }

    private async Task<IEnumerable<SubscriptionMethod>> GetDefaultSubscriptionMethodsFromCacheAsync(string sourceID, string actionID, string recepient)
    {
        try
        {
            await _semaphore.WaitAsync();
            var key = $"subs|-1{sourceID}{actionID}{recepient}";
            var result = cache.Get<IEnumerable<SubscriptionMethod>>(key);
            if (result == null)
            {
                result = await _service.GetSubscriptionMethodsAsync(-1, sourceID, actionID, recepient);
                cache.Insert(key, result, DateTime.UtcNow.AddDays(1));
            }

            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private int GetTenant()
    {
        return tenantManager.GetCurrentTenantId();
    }
}