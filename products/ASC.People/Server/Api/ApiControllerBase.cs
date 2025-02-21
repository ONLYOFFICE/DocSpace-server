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

using System.Text.Json.Nodes;

using ASC.Common.Security;
using ASC.Webhooks.Core;

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
public class WebhookPeopleAccessChecker(
    AuthManager authentication,
    UserManager userManager,
    SecurityContext securityContext,
    IPermissionResolver permissionResolver) : IWebhookAccessChecker
{
    public async Task<bool> CheckAccessAsync(WebhookData webhookData)
    {
        if (securityContext.CurrentAccount.ID == webhookData.TargetUserId)
        {
            return true;
        }

        if (typeof(EmployeeDto).IsAssignableFrom(webhookData.ResponseType))
        {
            var entryNode = JsonNode.Parse(webhookData.ResponseString)["response"];
            return await CheckAccessByResponse(entryNode, false, webhookData.TargetUserId);
        }

        if (typeof(IEnumerable<EmployeeDto>).IsAssignableFrom(webhookData.ResponseType) ||
            typeof(IAsyncEnumerable<EmployeeDto>).IsAssignableFrom(webhookData.ResponseType))
        {
            var entryNodes = JsonNode.Parse(webhookData.ResponseString)["response"].AsArray();
            foreach (var entryNode in entryNodes)
            {
                if (!await CheckAccessByResponse(entryNode, false, webhookData.TargetUserId))
                {
                    return false;
                }
            }

            return true;
        }

        if (typeof(GroupDto).IsAssignableFrom(webhookData.ResponseType))
        {
            var entryNode = JsonNode.Parse(webhookData.ResponseString)["response"];
            return await CheckAccessByResponse(entryNode, true, webhookData.TargetUserId);
        }

        if (typeof(IEnumerable<GroupDto>).IsAssignableFrom(webhookData.ResponseType) ||
            typeof(IAsyncEnumerable<GroupDto>).IsAssignableFrom(webhookData.ResponseType))
        {
            var entryNodes = JsonNode.Parse(webhookData.ResponseString)["response"].AsArray();
            foreach (var entryNode in entryNodes)
            {
                if (!await CheckAccessByResponse(entryNode, true, webhookData.TargetUserId))
                {
                    return false;
                }
            }

            return true;
        }

        var accessByFileId = await CheckAccessByRouteAsync(webhookData.RouteData, "userid", false, webhookData.TargetUserId);
        if (accessByFileId.HasValue)
        {
            return accessByFileId.Value;
        }

        return false;
    }

    private async Task<bool?> CheckAccessByRouteAsync(Dictionary<string, string> routeData, string param, bool isGroup, Guid userId)
    {
        if (routeData.TryGetValue(param, out var idStr) && Guid.TryParse(idStr, out var id))
        {
            return await CheckAccessAsync(id, isGroup, userId);
        }

        return null;
    }

    private async Task<bool> CheckAccessByResponse(JsonNode entryNode, bool isGroup, Guid userId)
    {
        if (entryNode == null)
        {
            return false;
        }

        var entryIdNode = entryNode["id"];

        if (Guid.TryParse(entryIdNode.GetValue<string>(), out var id))
        {
            return await CheckAccessAsync(id, isGroup, userId);
        }

        return false;
    }

    async Task<bool> CheckAccessAsync(Guid id, bool isGroup, Guid userId)
    {
        var targetUser = await userManager.GetUsersAsync(userId);

        if (await userManager.IsDocSpaceAdminAsync(targetUser))
        {
            return true;
        }

        if (!isGroup)
        {
            var user = await userManager.GetUsersAsync(id);

            if (await userManager.IsGuestAsync(user))
            {
                return user.CreatedBy == userId;
            }
        }

        var account = await authentication.GetAccountByIDAsync(targetUser.TenantId, targetUser.Id);

        return await permissionResolver.CheckAsync(account, Constants.Action_ReadGroups);
    }
}