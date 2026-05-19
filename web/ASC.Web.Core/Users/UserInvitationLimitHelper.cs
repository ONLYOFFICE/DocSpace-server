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

namespace ASC.Web.Core.Users;

[Scope]
public class UserInvitationLimitHelper(
    SetupInfo setupInfo,
    TenantManager tenantManager,
    QuotaSocketManager quotaSocketManager,
    IFusionCache hybridCache)
{
    private class InvitationLimitData
    {
        public int Limit { get; set; }
        public DateTime Expiration { get; set; }
    }

    private bool IsLimitEnabled()
    {
        return setupInfo.InvitationLimit != int.MaxValue;
    }

    private string GetCacheKey()
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        return $"invitation_limit:{tenantId}";
    }

    private async Task<InvitationLimitData> GetCacheValue(string cacheKey)
    {
        InvitationLimitData cacheValue = null;

        try
        {
            cacheValue = await hybridCache.GetOrDefaultAsync<InvitationLimitData>(cacheKey);

            if (cacheValue != null && DateTime.UtcNow > cacheValue.Expiration)
            {
                await hybridCache.RemoveAsync(cacheKey);
                cacheValue = null;
            }
        }
        catch
        {
            await hybridCache.RemoveAsync(cacheKey);
        }

        return cacheValue;
    }

    public async Task<int> GetLimit()
    {
        if (!IsLimitEnabled())
        {
            return setupInfo.InvitationLimit;
        }

        var cacheKey = GetCacheKey();

        var cacheValue = await GetCacheValue(cacheKey);

        return cacheValue?.Limit ?? setupInfo.InvitationLimit;
    }

    public async Task IncreaseLimit()
    {
        if (!IsLimitEnabled())
        {
            return;
        }

        var cacheKey = GetCacheKey();

        var cacheValue = await GetCacheValue(cacheKey);

        if (cacheValue == null)
        {
            return;
        }

        var newValue = new InvitationLimitData
        {
            Limit = int.Min(cacheValue.Limit + 1, setupInfo.InvitationLimit),
            Expiration = cacheValue.Expiration
        };

        var options = new FusionCacheEntryOptions(duration: TimeSpan.FromHours(setupInfo.InvitationLimitDuration));

        await hybridCache.SetAsync(cacheKey, newValue, options);

        await quotaSocketManager.ChangeInvitationLimitValue(newValue.Limit);
    }

    public async Task ReduceLimit()
    {
        if (!IsLimitEnabled())
        {
            return;
        }

        var cacheKey = GetCacheKey();

        var cacheValue = await GetCacheValue(cacheKey);

        var options = new FusionCacheEntryOptions(duration: TimeSpan.FromHours(setupInfo.InvitationLimitDuration));

        if (cacheValue != null)
        {
            var newValue = new InvitationLimitData
            {
                Limit = int.Max(cacheValue.Limit - 1, 0),
                Expiration = cacheValue.Expiration
            };

            await hybridCache.SetAsync(cacheKey, newValue, options);

            await quotaSocketManager.ChangeInvitationLimitValue(newValue.Limit);
        }
        else
        {
            var value = new InvitationLimitData
            {
                Limit = int.Max(setupInfo.InvitationLimit - 1, 0),
                Expiration = DateTime.UtcNow.AddHours(setupInfo.InvitationLimitDuration)
            };

            await hybridCache.SetAsync(cacheKey, value, options);

            await quotaSocketManager.ChangeInvitationLimitValue(value.Limit);
        }
    }
}
