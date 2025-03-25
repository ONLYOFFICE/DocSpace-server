// (c) Copyright Ascensio System SIA 2009-2025
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
        var user = await userManager.GetUsersAsync(userId);

        if (await userManager.IsDocSpaceAdminAsync(user))
        {
            return true;
        }

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
        var user = await userManager.GetUsersAsync(userId);

        return await userManager.CanUserViewAnotherUserAsync(user, data);
    }
}