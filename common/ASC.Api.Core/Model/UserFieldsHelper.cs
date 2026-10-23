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

namespace ASC.Web.Api.Models;

/// <summary>
/// The user fields that the REST user DTOs and the user.* webhook payload compute the same way.
/// </summary>
/// <remarks>
/// Reads nothing about the current account: what a reader may see is decided by the callers, the REST helpers
/// from AuthContext and the webhook helper per subscription owner.
/// </remarks>
[Scope]
public class UserFieldsHelper(
    UserManager userManager,
    UserPhotoManager userPhotoManager,
    CommonLinkUtility commonLinkUtility,
    SettingsManager settingsManager,
    IQuotaService quotaService,
    TenantManager tenantManager,
    CoreBaseSettings coreBaseSettings)
{
    public async Task<UserRoleFlags> GetRoleFlagsAsync(UserInfo userInfo, Tenant tenant)
    {
        var currentType = await userManager.GetUserTypeAsync(userInfo.Id);
        var isOwner = userInfo.IsOwner(tenant);

        return new UserRoleFlags(
            IsOwner: isOwner,
            IsAdmin: currentType is EmployeeType.DocSpaceAdmin && !isOwner,
            IsRoomAdmin: currentType is EmployeeType.RoomAdmin,
            IsCollaborator: currentType is EmployeeType.User,
            IsVisitor: await userManager.IsGuestAsync(userInfo));
    }

    /// <summary>
    /// The avatar URLs of a user, with the hash that makes a client reload them after the profile changes.
    /// </summary>
    /// <param name="userInfo">The user.</param>
    /// <param name="absolute">Whether to return absolute URLs, as a payload read outside the portal needs.</param>
    public async Task<UserAvatarUrls> GetAvatarUrlsAsync(UserInfo userInfo, bool absolute)
    {
        var cacheKey = GetCacheKey(userInfo);

        return new UserAvatarUrls(
            Avatar: GetAvatarUrl(await userPhotoManager.GetBigPhotoURL(userInfo.Id), cacheKey, absolute),
            AvatarOriginal: GetAvatarUrl(await userPhotoManager.GetPhotoAbsoluteWebPath(userInfo.Id), cacheKey, absolute),
            AvatarMax: GetAvatarUrl(await userPhotoManager.GetMaxPhotoURL(userInfo.Id), cacheKey, absolute),
            AvatarMedium: GetAvatarUrl(await userPhotoManager.GetMediumPhotoURL(userInfo.Id), cacheKey, absolute),
            AvatarSmall: GetAvatarUrl(await userPhotoManager.GetSmallPhotoURL(userInfo.Id), cacheKey, absolute));
    }

    /// <summary>
    /// The big avatar alone, for the places that show a user in short.
    /// </summary>
    public async Task<string> GetAvatarUrlAsync(UserInfo userInfo, bool absolute)
    {
        return GetAvatarUrl(await userPhotoManager.GetBigPhotoURL(userInfo.Id), GetCacheKey(userInfo), absolute);
    }

    /// <summary>
    /// The absolute profile link of a user; null for the empty user ID.
    /// </summary>
    public async Task<string> GetProfileUrlAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        return commonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(userId));
    }

    /// <summary>
    /// The used space and the quota of a user; null when the portal keeps no quota statistics. The quota fields
    /// are null when user quotas are off.
    /// </summary>
    public async Task<UserQuotaInfo> GetQuotaAsync(UserInfo userInfo, Tenant tenant)
    {
        if (!coreBaseSettings.Standalone && !(await tenantManager.GetCurrentTenantQuotaAsync()).Statistic)
        {
            return null;
        }

        var quotaSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();

        var usedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(tenant.Id, userInfo.Id))
            .Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString()))
            .Sum(r => r.Counter));

        if (!quotaSettings.EnableQuota)
        {
            return new UserQuotaInfo(usedSpace, null, null);
        }

        var userQuotaSettings = await settingsManager.LoadAsync<UserQuotaSettings>(userInfo);

        var isCustomQuota = userQuotaSettings != null && userQuotaSettings.UserQuota != userQuotaSettings.GetDefault().UserQuota;

        return new UserQuotaInfo(usedSpace, isCustomQuota ? userQuotaSettings.UserQuota : quotaSettings.DefaultQuota, isCustomQuota);
    }

    private static int GetCacheKey(UserInfo userInfo)
    {
        return Math.Abs(userInfo.LastModified.GetHashCode());
    }

    private string GetAvatarUrl(string path, int cacheKey, bool absolute)
    {
        return (absolute ? commonLinkUtility.GetFullAbsolutePath(path) : path) + $"?hash={cacheKey}";
    }
}

public record UserRoleFlags(bool IsOwner, bool IsAdmin, bool IsRoomAdmin, bool IsCollaborator, bool IsVisitor);

public record UserAvatarUrls(string Avatar, string AvatarOriginal, string AvatarMax, string AvatarMedium, string AvatarSmall);

public record UserQuotaInfo(long UsedSpace, long? QuotaLimit, bool? IsCustomQuota);
