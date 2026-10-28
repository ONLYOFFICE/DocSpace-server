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

namespace ASC.Api.Core.Webhook.Payloads;

/// <summary>
/// Builds the payload of a user.* webhook.
/// </summary>
/// <remarks>
/// Deliberately not EmployeeFullDtoHelper, which reads the role of the current account: a webhook is built once
/// per event, while it is published, and has no reader at that point. GetAsync builds the whole user once;
/// RestrictForSubscriberAsync then cuts it for each subscription owner by the rules EmployeeFullDtoHelper applies
/// to a reader. WebhookUserAccessChecker decides whether the owner gets the event at all.
/// </remarks>
[Scope]
public class UserWebhookDtoHelper(
    UserManager userManager,
    UserPhotoManager userPhotoManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    UserFieldsHelper userFieldsHelper,
    WebItemSecurity webItemSecurity,
    WebItemManager webItemManager,
    TenantManager tenantManager,
    CoreBaseSettings coreBaseSettings,
    GroupSummaryDtoHelper groupSummaryDtoHelper)
{
    public async Task<UserWebhookDto> GetAsync(UserInfo userInfo)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var result = userInfo.MapToWebhookDto();

        result.DisplayName = displayUserSettingsHelper.GetFullUserName(userInfo);

        var roles = await userFieldsHelper.GetRoleFlagsAsync(userInfo, tenant);
        result.IsOwner = roles.IsOwner;
        result.IsAdmin = roles.IsAdmin;
        result.IsRoomAdmin = roles.IsRoomAdmin;
        result.IsCollaborator = roles.IsCollaborator;
        result.IsVisitor = roles.IsVisitor;

        if (!string.IsNullOrEmpty(userInfo.CultureName))
        {
            result.CultureName = coreBaseSettings.GetRightCultureName(userInfo.GetCulture());
        }

        await FillGroupsAsync(result, userInfo);
        await FillAvatarsAsync(result, userInfo);
        await FillQuotaAsync(result, userInfo, tenant);

        var listAdminModules = await userInfo.GetListAdminModulesAsync(webItemSecurity, webItemManager);
        if (listAdminModules.Count > 0)
        {
            result.ListAdminModules = listAdminModules;
        }

        if (userInfo.CreatedBy.HasValue)
        {
            result.CreatedBy = await GetSummaryAsync(userInfo.CreatedBy.Value);
        }

        return result;
    }

    public async Task<UserWebhookDto> RestrictForSubscriberAsync(UserWebhookDto payload, UserInfo userInfo, Guid? subscriberId)
    {
        if (!subscriberId.HasValue || await userManager.IsDocSpaceAdminAsync(subscriberId.Value))
        {
            return payload;
        }

        var isSelf = subscriberId.Value == userInfo.Id;
        var result = payload.Clone();

        result.CreatedBy = null;

        if (!isSelf)
        {
            result.UsedSpace = null;
            result.QuotaLimit = null;
            result.IsCustomQuota = null;

            if (await IsWithoutContactsAccessAsync(subscriberId))
            {
                result.Groups = null;
                result.Department = null;
                result.ProfileUrl = null;
            }
        }

        return result;
    }

    /// <summary>
    /// Whether the subscription owner may not see the contacts of another user (see
    /// <see cref="UserExtensions.IsWithoutContactsAccessAsync"/>). A subscription without an owner is not restricted.
    /// </summary>
    public async Task<bool> IsWithoutContactsAccessAsync(Guid? subscriberId)
    {
        return subscriberId.HasValue && await userManager.IsWithoutContactsAccessAsync(subscriberId.Value);
    }

    /// <summary>
    /// The summary as a subscriber without contacts access may receive it: without the profile link and without the
    /// user name, which is the local part of the email address, unless the summary is the subscriber themselves.
    /// The REST API shows such a reader neither of them. The summary is shared by every subscriber of the event, so it
    /// is copied rather than changed.
    /// </summary>
    public static UserSummaryWebhookDto RestrictSummaryForSubscriber(UserSummaryWebhookDto summary, Guid subscriberId)
    {
        if (summary == null || summary.Id == subscriberId || (summary.ProfileUrl == null && summary.UserName == null))
        {
            return summary;
        }

        var result = summary.Clone();
        result.ProfileUrl = null;
        result.UserName = null;

        return result;
    }

    public async Task<UserSummaryWebhookDto> GetSummaryAsync(Guid userId)
    {
        var userInfo = await userManager.GetUsersAsync(userId);

        return await GetSummaryAsync(userInfo);
    }

    public async Task<UserSummaryWebhookDto> GetSummaryAsync(UserInfo userInfo)
    {
        if (userInfo == null || userInfo.Equals(Constants.LostUser))
        {
            return null;
        }

        var result = userInfo.MapToSummaryWebhookDto();

        result.DisplayName = displayUserSettingsHelper.GetFullUserName(userInfo);
        result.Avatar = await userFieldsHelper.GetAvatarUrlAsync(userInfo, absolute: true);
        result.ProfileUrl = await userFieldsHelper.GetProfileUrlAsync(userInfo.Id);

        return result;
    }

    private async Task FillGroupsAsync(UserWebhookDto result, UserInfo userInfo)
    {
        var groupsFromDb = await userManager.GetUserGroupsAsync(userInfo.Id);

        List<GroupSummaryWebhookDto> groups = [];

        // the same summary the REST API puts into a user's groups; its lookups are served by the user cache
        foreach (var g in groupsFromDb)
        {
            groups.Add((await groupSummaryDtoHelper.GetAsync(g)).MapToWebhookDto());
        }

        if (groups.Count > 0)
        {
            result.Groups = groups;
            result.Department = string.Join(", ", groups.Select(d => d.Name));
        }
        else
        {
            result.Department = "";
        }
    }

    private async Task FillAvatarsAsync(UserWebhookDto result, UserInfo userInfo)
    {
        var avatars = await userFieldsHelper.GetAvatarUrlsAsync(userInfo, absolute: true);

        result.HasAvatar = await userPhotoManager.UserHasAvatar(userInfo.Id);
        result.Avatar = avatars.Avatar;
        result.AvatarOriginal = avatars.AvatarOriginal;
        result.AvatarMax = avatars.AvatarMax;
        result.AvatarMedium = avatars.AvatarMedium;
        result.AvatarSmall = avatars.AvatarSmall;
        result.ProfileUrl = await userFieldsHelper.GetProfileUrlAsync(userInfo.Id);
    }

    private async Task FillQuotaAsync(UserWebhookDto result, UserInfo userInfo, Tenant tenant)
    {
        var quota = await userFieldsHelper.GetQuotaAsync(userInfo, tenant);

        if (quota == null)
        {
            return;
        }

        result.UsedSpace = quota.UsedSpace;
        result.QuotaLimit = quota.QuotaLimit;
        result.IsCustomQuota = quota.IsCustomQuota;
    }
}
