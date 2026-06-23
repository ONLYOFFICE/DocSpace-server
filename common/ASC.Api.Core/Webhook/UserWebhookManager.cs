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

using ASC.Common.Security;

namespace ASC.Api.Core.Webhook;

[Scope]
public class UserWebhookManager(
    IWebhookPublisher webhookPublisher,
    WebhookGroupAccessChecker webhookGroupAccessChecker,
    WebhookUserAccessChecker webhookUserAccessChecker)
{
    public async Task PublishAsync(WebhookTrigger trigger, UserInfo userInfo)
    {
        await webhookPublisher.PublishAsync(trigger, webhookUserAccessChecker, userInfo, userInfo.Id);
    }

    public async Task PublishAsync(WebhookTrigger trigger, ASC.Core.Users.GroupInfo groupInfo)
    {
        await webhookPublisher.PublishAsync(trigger, webhookGroupAccessChecker, groupInfo, groupInfo.ID);
    }
}

[Scope]
public class WebhookGroupAccessChecker(
    AuthManager authentication,
    UserManager userManager,
    IPermissionResolver permissionResolver) : IWebhookAccessChecker<ASC.Core.Users.GroupInfo>
{
    public bool CheckIsTarget(ASC.Core.Users.GroupInfo data, string targetId)
    {
        return data.ID.ToString() == targetId;
    }

    public async Task<bool> CheckAccessAsync(ASC.Core.Users.GroupInfo data, Guid userId)
    {
        if (await userManager.IsDocSpaceAdminAsync(userId))
        {
            return true;
        }
        
        var user = await userManager.GetUsersAsync(userId);
        var account = await authentication.GetAccountByIDAsync(user.TenantId, user.Id);

        return await permissionResolver.CheckAsync(account, Constants.Action_ReadGroups);
    }
}

[Scope]
public class WebhookUserAccessChecker(UserManager userManager) : IWebhookAccessChecker<UserInfo>
{
    public bool CheckIsTarget(UserInfo data, string targetId)
    {
        return data.Id.ToString() == targetId;
    }

    public async Task<bool> CheckAccessAsync(UserInfo data, Guid userId)
    {
        return await userManager.CanUserViewAnotherUserAsync(userId, data.Id);
    }
}