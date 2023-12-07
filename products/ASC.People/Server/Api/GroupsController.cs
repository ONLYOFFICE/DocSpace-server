// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.People.Api;

///<summary>
/// Groups API.
///</summary>
///<name>group</name>
[Scope]
[DefaultRoute]
[ApiController]
public class GroupController(UserManager userManager,
        ApiContext apiContext,
        GroupFullDtoHelper groupFullDtoHelper,
        MessageService messageService,
        MessageTarget messageTarget,
        PermissionContext permissionContext,
        FileSecurity fileSecurity)
    : ControllerBase
{
    /// <summary>
    /// Returns the general information about all the groups, such as group ID and group manager.
    /// </summary>
    /// <short>
    /// Get groups
    /// </short>
    /// <returns type="ASC.Web.Api.Models.GroupSummaryDto, ASC.Api.Core">List of groups</returns>
    /// <remarks>
    /// This method returns partial group information.
    /// </remarks>
    /// <path>api/2.0/groups</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet]
    public async IAsyncEnumerable<GroupDto> GetGroupsAsync(bool withMembers = false)
    {
        var groups = (await userManager.GetDepartmentsAsync()).Select(r => r);
        
        if (!string.IsNullOrEmpty(apiContext.FilterValue))
        {
            groups = groups.Where(r => r.Name!.Contains(apiContext.FilterValue, StringComparison.InvariantCultureIgnoreCase));
        }

        foreach (var g in groups)
        {
            yield return await groupFullDtoHelper.Get(g, withMembers);
        }
    }

    /// <summary>
    /// Returns the detailed information about the selected group.
    /// </summary>
    /// <short>
    /// Get a group
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">Group ID</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the following parameters</returns>
    /// <remarks>
    /// This method returns full group information.
    /// </remarks>
    /// <path>api/2.0/groups/{id}</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("{id:guid}")]
    public async Task<GroupDto> GetGroupAsync(Guid id)
    {
        return await groupFullDtoHelper.Get(await GetGroupInfoAsync(id), true);
    }

    /// <summary>
    /// Returns a list of groups for the user with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Get user groups
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">User ID</param>
    /// <returns type="ASC.Web.Api.Models.GroupSummaryDto, ASC.Api.Core">List of groups</returns>
    /// <path>api/2.0/groups/member/{id}</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("member/{id:guid}")]
    public async Task<IEnumerable<GroupSummaryDto>> GetGroupsByMemberIdAsync(Guid id)
    {
        return (await userManager.GetUserGroupsAsync(id)).Select(x => new GroupSummaryDto(x, userManager));
    }

    /// <summary>
    /// Adds a new group with the group manager, name, and members specified in the request.
    /// </summary>
    /// <short>
    /// Add a new group
    /// </short>
    /// <param type="ASC.People.ApiModels.RequestDto.GroupRequestDto, ASC.People" name="inDto">Group request parameters</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Newly created group with the following parameters</returns>
    /// <path>api/2.0/groups</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost]
    public async Task<GroupDto> AddGroupAsync(GroupRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await userManager.SaveGroupInfoAsync(new GroupInfo { Name = inDto.GroupName });

        await TransferUserToDepartmentAsync(inDto.GroupManager, group, true);

        if (inDto.Members != null)
        {
            foreach (var member in inDto.Members)
            {
                await TransferUserToDepartmentAsync(member, group, false);
            }
        }

        await messageService.SendAsync(MessageAction.GroupCreated, messageTarget.Create(group.ID), group.Name);

        return await groupFullDtoHelper.Get(group, true);
    }

    /// <summary>
    /// Updates the existing group changing the group manager, name, and/or members.
    /// </summary>
    /// <short>
    /// Update a group
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">Group ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.GroupRequestDto, ASC.People" name="inDto">Group request parameters</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Updated group with the following parameters</returns>
    /// <path>api/2.0/groups/{id}</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id:guid}")]
    public async Task<GroupDto> UpdateGroupAsync(Guid id, GroupRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = (await userManager.GetGroupsAsync()).SingleOrDefault(x => x.ID == id);

        if (group == null || group.ID == Constants.LostGroupInfo.ID)
        {
            throw new ItemNotFoundException("group not found");
        }

        group.Name = inDto.GroupName ?? group.Name;
        await userManager.SaveGroupInfoAsync(group);

        await RemoveMembersAsync(id, new GroupRequestDto { Members = (await userManager.GetUsersByGroupAsync(id, EmployeeStatus.All)).Select(u => u.Id).Where(userId => !inDto.Members.Contains(userId)) });

        await TransferUserToDepartmentAsync(inDto.GroupManager, group, true);

        if (inDto.Members != null)
        {
            foreach (var member in inDto.Members)
            {
                await TransferUserToDepartmentAsync(member, group, false);
            }
        }

        await messageService.SendAsync(MessageAction.GroupUpdated, messageTarget.Create(id), group.Name);

        return await GetGroupAsync(id);
    }

    /// <summary>
    /// Deletes a group with the ID specified in the request from the list of groups on the portal.
    /// </summary>
    /// <short>
    /// Delete a group
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">Group ID</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the following parameters</returns>
    /// <path>api/2.0/groups/{id}</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("{id:guid}")]
    public async Task<GroupDto> DeleteGroupAsync(Guid id)
    { 
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(id);

        await userManager.DeleteGroupAsync(id);
        await fileSecurity.RemoveSubjectAsync<int>(id);

        await messageService.SendAsync(MessageAction.GroupDeleted, messageTarget.Create(group.ID), group.Name);

        return await groupFullDtoHelper.Get(group, false);
    }

    /// <summary>
    /// Moves all the members from the selected group to another one specified in the request.
    /// </summary>
    /// <short>
    /// Move group members
    /// </short>
    /// <param type="System.Guid, System" method="url" name="fromId">Group ID to move from</param>
    /// <param type="System.Guid, System" method="url" name="toId">Group ID to move to</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the following parameters</returns>
    /// <path>api/2.0/groups/{fromId}/members/transfer/{toId}</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{fromId:guid}/members/transfer/{toId:guid}")]
    public async Task<GroupDto> TransferMembersToAsync(Guid fromId, Guid toId)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var oldGroup = await GetGroupInfoAsync(fromId);
        var newGroup = await GetGroupInfoAsync(toId);

        var users = await userManager.GetUsersByGroupAsync(oldGroup.ID);
        
        foreach (var userInfo in users)
        {
            await TransferUserToDepartmentAsync(userInfo.Id, newGroup, false);
        }

        return await GetGroupAsync(toId);
    }

    /// <summary>
    /// Replaces the group members with those specified in the request.
    /// </summary>
    /// <short>
    /// Replace group members
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">Group ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.GroupRequestDto, ASC.People" name="inDto">Group request parameters</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the following parameters</returns>
    /// <path>api/2.0/groups/{id}/members</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("{id:guid}/members")]
    public async Task<GroupDto> SetMembersAsync(Guid id, GroupRequestDto inDto)
    {
        await RemoveMembersAsync(id, new GroupRequestDto { Members = (await userManager.GetUsersByGroupAsync(id)).Select(x => x.Id) });
        await AddMembersAsync(id, inDto);

        return await GetGroupAsync(id);
    }

    /// <summary>
    /// Adds new group members to the group with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Add group members
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">Group ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.GroupRequestDto, ASC.People" name="inDto">Group request parameters</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the following parameters</returns>
    /// <path>api/2.0/groups/{id}/members</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id:guid}/members")]
    public async Task<GroupDto> AddMembersAsync(Guid id, GroupRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(id);

        foreach (var userId in inDto.Members)
        {
            await TransferUserToDepartmentAsync(userId, group, false);
        }

        return await GetGroupAsync(group.ID);
    }

    /// <summary>
    /// Sets a user with the ID specified in the request as a group manager.
    /// </summary>
    /// <short>
    /// Set a group manager
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">Group ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.SetManagerRequestDto, ASC.People" name="inDto">Request parameters for setting a group manager</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the following parameters</returns>
    /// <path>api/2.0/groups/{id}/manager</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id:guid}/manager")]
    public async Task<GroupDto> SetManagerAsync(Guid id, SetManagerRequestDto inDto)
    {
        var group = await GetGroupInfoAsync(id);
        
        if (await userManager.UserExistsAsync(inDto.UserId))
        {
            await userManager.SetDepartmentManagerAsync(group.ID, inDto.UserId);
        }
        else
        {
            throw new ItemNotFoundException("user not found");
        }

        return await GetGroupAsync(id);
    }

    /// <summary>
    /// Removes the group members specified in the request from the selected group.
    /// </summary>
    /// <short>
    /// Remove group members
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">Group ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.GroupRequestDto, ASC.People" name="inDto">Group request parameters</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the following parameters</returns>
    /// <path>api/2.0/groups/{id}/members</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("{id:guid}/members")]
    public async Task<GroupDto> RemoveMembersAsync(Guid id, GroupRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(id);

        foreach (var userId in inDto.Members)
        {
            await RemoveUserFromDepartmentAsync(userId, group);
        }

        return await GetGroupAsync(group.ID);
    }

    private async Task<GroupInfo> GetGroupInfoAsync(Guid id)
    {
        var group = (await userManager.GetGroupsAsync()).SingleOrDefault(x => x.ID == id).NotFoundIfNull("group not found");
        
        if (group.ID == Constants.LostGroupInfo.ID)
        {
            throw new ItemNotFoundException("group not found");
        }

        return group;
    }

    private async Task TransferUserToDepartmentAsync(Guid userId, GroupInfo groupInfo, bool setAsManager)
    {
        if (!await userManager.UserExistsAsync(userId) && userId != Guid.Empty)
        {
            return;
        }

        if (setAsManager)
        {
            await userManager.SetDepartmentManagerAsync(group.ID, userId);
        }
        await userManager.AddUserIntoGroupAsync(userId, group.ID);
    }

    private async Task RemoveUserFromDepartmentAsync(Guid userId, GroupInfo group)
    {
        if (!await userManager.UserExistsAsync(userId))
        {
            return;
        }

        var user = await userManager.GetUsersAsync(userId);
        await userManager.RemoveUserFromGroupAsync(user.Id, group.ID);
        await userManager.UpdateUserInfoAsync(user);
    }
}
