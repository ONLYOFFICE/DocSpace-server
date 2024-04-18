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

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace ASC.Web.Core.Users;

[Scope]
public class UserInvitationLimitHelper(
    SetupInfo setupInfo,
    TenantManager tenantManager,
    QuotaSocketManager quotaSocketManager,
    IRedisClient redisClient)
{
    private bool IsLimitEnabled()
    {
        return setupInfo.InvitationLimit != int.MaxValue;
    }

    private async Task<string> GetCacheKey()
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();

        return $"invitation_limit:{tenantId}";
    }

    public async Task<int> GetLimit()
    {
        if (!IsLimitEnabled())
        {
            return setupInfo.InvitationLimit;
        }

        var cacheKey = await GetCacheKey();

        var redisDatabase = redisClient.GetDefaultDatabase().Database;

        var cacheKeyExist = await redisDatabase.KeyExistsAsync(cacheKey);

        return cacheKeyExist ? (int)(await redisDatabase.StringGetAsync(cacheKey)) : setupInfo.InvitationLimit;
    }

    public async Task IncreaseLimit()
    {
        if (!IsLimitEnabled())
        {
            return;
        }

        var cacheKey = await GetCacheKey();

        var redisDatabase = redisClient.GetDefaultDatabase().Database;

        var cacheKeyExist = await redisDatabase.KeyExistsAsync(cacheKey);

        if (!cacheKeyExist)
        {
            return;
        }

        var oldValue = (int)(await redisDatabase.StringGetAsync(cacheKey));

        var newValue = int.Min(oldValue + 1, setupInfo.InvitationLimit);

        _ = await redisDatabase.StringSetAsync(cacheKey, newValue);

        await quotaSocketManager.ChangeInvitationLimitValue(newValue);
    }

    public async Task ReduceLimit()
    {
        if (!IsLimitEnabled())
        {
            return;
        }

        var cacheKey = await GetCacheKey();

        var redisDatabase = redisClient.GetDefaultDatabase().Database;

        var cacheKeyExist = await redisDatabase.KeyExistsAsync(cacheKey);

        if (cacheKeyExist)
        {
            var oldValue = (int)(await redisDatabase.StringGetAsync(cacheKey));

            var newValue = int.Max(oldValue - 1, 0);

            _ = await redisDatabase.StringSetAsync(cacheKey, newValue);

            await quotaSocketManager.ChangeInvitationLimitValue(newValue);
        }
        else
        {
            var value = int.Max(setupInfo.InvitationLimit - 1, 0);

            _ = await redisDatabase.StringSetAsync(cacheKey, value);

            await quotaSocketManager.ChangeInvitationLimitValue(value);
        }
    }
}
