// (c) Copyright Ascensio System SIA 2009-2026
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
