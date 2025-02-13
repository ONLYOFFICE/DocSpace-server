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

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Api.Core.Socket;

public class UserSocketManager(ITariffService tariffService,
    TenantManager tenantManager, 
    ChannelWriter<SocketData> channelWriter, 
    MachinePseudoKeys machinePseudoKeys,
    IConfiguration configuration,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    SecurityContext securityContext,
    UserManager userManager,
    GroupFullDtoHelper groupFullDtoHelper) : SocketServiceClient(tariffService, tenantManager, channelWriter, machinePseudoKeys, configuration)
{
    protected override string Hub => "files";

    public async Task AddUserAsync(UserInfo userInfo)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var dto = await employeeFullDtoHelper.GetFullAsync(userInfo);
        await MakeRequest("add-user", new { tenantId, user = dto });
    }

    public async Task UpdateUserAsync(UserInfo userInfo)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var dto = await employeeFullDtoHelper.GetFullAsync(userInfo);
        foreach (var group in dto.Groups)
        {
            var groupInfo = await userManager.GetGroupInfoAsync(group.Id);
            var groupDto = await groupFullDtoHelper.Get(groupInfo, true);
            await UpdateGroupAsync(groupDto);
        }
        await MakeRequest("update-user", new { tenantId, user = dto });
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("delete-user", new { tenantId, userId });
    }

    public async Task AddGroupAsync(GroupDto dto)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("add-group", new { tenantId, group = dto });
    }

    public async Task UpdateGroupAsync(GroupDto dto)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("update-group", new { tenantId, group = dto });
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("delete-group", new { tenantId, groupId });
    }

    public async Task AddGuestAsync(UserInfo userInfo, bool notifyAdmins = true)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var dto = await employeeFullDtoHelper.GetFullAsync(userInfo);
        var currentUser = securityContext.CurrentAccount.ID;
        await MakeRequest("add-guest", new { tenantId, room = currentUser, guest = dto });
        if (notifyAdmins)
        {
            var admins = await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID);
            foreach (var admin in admins.Where(a => currentUser != a.Id))
            {
                await MakeRequest("add-guest", new { tenantId, room = admin.Id, guest = dto });
            }
        }
    }

    public async Task UpdateGuestAsync(UserInfo userInfo)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var dto = await employeeFullDtoHelper.GetFullAsync(userInfo);
        var relations = await userManager.GetUserRelationsByTargetAsync(userInfo.Id);
        var admins = await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID);
        foreach (var relation in relations)
        {
            await MakeRequest("update-guest", new { tenantId, room = relation.Key, guest = dto });
        }
        foreach (var admin in admins.Where(a => !relations.ContainsKey(a.Id)))
        {
            await MakeRequest("update-guest", new { tenantId, room = admin.Id, guest = dto });
        }
    }

    public async Task DeleteGuestAsync(Guid userId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var relations = await userManager.GetUserRelationsByTargetAsync(userId);
        var admins = await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID);
        foreach (var relation in relations)
        {
            await MakeRequest("delete-guest", new { tenantId, room = relation.Key, guestId = userId });
        }
        foreach (var admin in admins.Where(a => !relations.ContainsKey(a.Id)))
        {
            await MakeRequest("delete-guest", new { tenantId, room = admin.Id, guestId = userId });
        }
    }
}
