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
    DisplayUserSettingsHelper displayUserSettingsHelper) : SocketServiceClient(tariffService, tenantManager, channelWriter, machinePseudoKeys, configuration)
{
    protected override string Hub => "files";

    public async Task ChangeUserTypeAsync(UserInfo userInfo, bool hasPersonalFolder)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var dto = await GetUserDtoAsync(userInfo);
        var currentUser = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var name = currentUser.DisplayUserName(displayUserSettingsHelper);

        await MakeRequest("change-my-type", new { tenantId, user = dto, admin = name, hasPersonalFolder });
    }

    public async Task AddUserAsync(UserInfo userInfo)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("add-user", new { tenantId, user = IdOnly(userInfo.Id) });
    }

    public async Task UpdateUserAsync(UserInfo userInfo)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        // The groups of the account show it among their members, so they are refreshed too.
        foreach (var group in await userManager.GetUserGroupsAsync(userInfo.Id))
        {
            await UpdateGroupAsync(group.ID);
        }

        await MakeRequest("update-user", new { tenantId, user = IdOnly(userInfo.Id) });
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("delete-user", new { tenantId, userId });
    }

    public async Task AddGroupAsync(GroupDto dto)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("add-group", new { tenantId, group = IdOnly(dto.Id) });
    }

    public Task UpdateGroupAsync(GroupDto dto)
    {
        return UpdateGroupAsync(dto.Id);
    }

    public async Task UpdateGroupAsync(Guid groupId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("update-group", new { tenantId, group = IdOnly(groupId) });
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("delete-group", new { tenantId, groupId });
    }

    public async Task AddGuestAsync(UserInfo userInfo, bool notifyAdmins = true)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var dto = IdOnly(userInfo.Id);
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
        var dto = IdOnly(userInfo.Id);
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

    public async Task DeleteGuestAsync(Guid CurrentUserId, Guid userId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await MakeRequest("delete-guest", new { tenantId, room = CurrentUserId, guestId = userId });
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

    public async Task UpdateExternalDbSettingsAsync(int tenantId, bool enabled)
    {
        _ = await _tenantManager.SetCurrentTenantAsync(tenantId);
        await MakeRequest("external-db-settings", new { tenantId, externalDbEnabled = enabled });
    }

    public async Task ConnectTelegram(int tenantId, Guid userId)
    {
        _ = await _tenantManager.SetCurrentTenantAsync(tenantId);
        await MakeRequest("telegram", new { tenantId, userId });
    }

    public async Task UpdateTelegram(int tenantId, Guid userId, string username)
    {
        _ = await _tenantManager.SetCurrentTenantAsync(tenantId);
        await MakeRequest("update-telegram", new { tenantId, userId, username });
    }

    // The profile of the account whose type was changed, for its own client, which takes the whole profile from the
    // event. The AI services limit is left out: clients re-read it on "change-service-limits".
    private async Task<EmployeeFullDto> GetUserDtoAsync(UserInfo userInfo)
    {
        var dto = await employeeFullDtoHelper.GetFullAsync(userInfo);
        dto.AiServicesLimit = null;

        return dto;
    }

    /// <summary>
    /// What an event about an account or a group of a list carries: the id alone. Joining a socket room is not
    /// access-checked, so the event reaches everybody who listens, a user or a guest among them, while a profile or
    /// the members of a group would be built with the rights of whoever made the change. The clients take the id and
    /// re-read through the API, which applies their own rights. The event about the caller's own type is not one of
    /// these: it goes to that account alone, and its client takes the whole profile from it.
    /// </summary>
    private static object IdOnly(Guid id)
    {
        return new { id };
    }
}