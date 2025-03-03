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

using ASC.Common.Security;

namespace ASC.People.Api;

/// <summary>
/// People API.
/// </summary>
/// <name>people</name>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("people")]
public abstract class ApiControllerBase : ControllerBase;


[Scope]
public class WebhookGroupAccessChecker(
    AuthContext authContext,
    AuthManager authentication,
    UserManager userManager,
    IPermissionResolver permissionResolver) : IWebhookAccessChecker<GroupInfo>
{
    public async Task<bool> CheckAccessAsync(GroupInfo data, Guid userId)
    {
        if (authContext.CurrentAccount.ID == userId)
        {
            return true;
        }

        var targetUser = await userManager.GetUsersAsync(userId);

        if (await userManager.IsDocSpaceAdminAsync(targetUser))
        {
            return true;
        }

        var account = await authentication.GetAccountByIDAsync(targetUser.TenantId, targetUser.Id);

        return await permissionResolver.CheckAsync(account, Constants.Action_ReadGroups);
    }
}

[Scope]
public class WebhookUserAccessChecker(
    AuthContext authContext,
    UserManager userManager) : IWebhookAccessChecker<UserInfo>
{
    public async Task<bool> CheckAccessAsync(UserInfo data, Guid userId)
    {
        if (authContext.CurrentAccount.ID == userId)
        {
            return true;
        }

        var targetUser = await userManager.GetUsersAsync(userId);
        var targetUserType = await userManager.GetUserTypeAsync(targetUser);

        if (targetUserType is EmployeeType.DocSpaceAdmin)
        {
            return true;
        }

        if (targetUserType is EmployeeType.User or EmployeeType.Guest)
        {
            return false;
        }

        var dataUserType = await userManager.GetUserTypeAsync(data);

        if (dataUserType is EmployeeType.Guest)
        {
            if (data.CreatedBy.HasValue && data.CreatedBy.Value == userId)
            {
                return true;
            }

            var userRelations = await userManager.GetUserRelationsAsync(userId);

            return userRelations.ContainsKey(data.Id);
        }

        return true;
    }
}