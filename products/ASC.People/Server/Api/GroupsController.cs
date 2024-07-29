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

namespace ASC.People.Api;

///<summary>
/// Groups API.
///</summary>
///<name>group</name>
[Scope]
[DefaultRoute]
[ApiController]
public class GroupController(
    GroupSummaryDtoHelper groupSummaryDtoHelper,
    UserManager userManager,
    ApiContext apiContext,
    GroupFullDtoHelper groupFullDtoHelper,
    MessageService messageService,
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
    /// <param type="System.Nullable{System.Guid}, System" name="userId">User ID</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="manager">Specifies if the user is a manager or not</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">List of groups</returns>
    /// <remarks>
    /// This method returns partial group information.
    /// </remarks>
    /// <path>api/2.0/groups</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet]
    public async IAsyncEnumerable<GroupDto> GetGroupsAsync(Guid? userId, bool? manager)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_ReadGroups);
        
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);
        var text = apiContext.FilterValue;

        var memberId = userId ?? Guid.Empty;
        var asManager = manager ?? false;

        if (!GroupSortTypeExtensions.TryParse(apiContext.SortBy, true, out var sortBy))
        {
            sortBy = GroupSortType.Title;
        }

        var totalCount = await userManager.GetGroupsCountAsync(text, memberId, asManager);

        apiContext.SetCount(Math.Min(Math.Max(totalCount - offset, 0), count)).SetTotalCount(totalCount);

        await foreach (var g in userManager.GetGroupsAsync(text, memberId, asManager, sortBy, !apiContext.SortDescending, offset, count))
        {
            yield return await groupFullDtoHelper.Get(g, false);
        }
    }

    /// <summary>
    /// Returns the detailed information about the selected group.
    /// </summary>
    /// <short>
    /// Get a group
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">Group ID</param>
    /// <param type="System.Boolean, System" name="includeMembers">Include members</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the detailed information</returns>
    /// <remarks>
    /// This method returns full group information.
    /// </remarks>
    /// <path>api/2.0/groups/{id}</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("{id:guid}")]
    public async Task<GroupDto> GetGroupAsync(Guid id, bool includeMembers = true)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_ReadGroups);
        
        return await groupFullDtoHelper.Get(await GetGroupInfoAsync(id), includeMembers);
    }

    /// <summary>
    /// Returns a list of groups for the user with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Get user groups
    /// </short>
    /// <param type="System.Guid, System" method="url" name="userid">User ID</param>
    /// <returns type="ASC.Web.Api.Models.GroupSummaryDto, ASC.Api.Core">List of groups</returns>
    /// <path>api/2.0/groups/user/{userid}</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("user/{userid:guid}")]
    public async Task<IEnumerable<GroupSummaryDto>> GetByUserIdAsync(Guid userid)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_ReadGroups);
        var groups = await userManager.GetUserGroupsAsync(userid);
        List<GroupSummaryDto> result = new(groups.Count);
        
        foreach (var g in groups)
        {
            result.Add(await groupSummaryDtoHelper.GetAsync(g));
        }

        return result;
    }

    /// <summary>
    /// Adds a new group with the group manager, name, and members specified in the request.
    /// </summary>
    /// <short>
    /// Add a new group
    /// </short>
    /// <param type="ASC.People.ApiModels.RequestDto.GroupRequestDto, ASC.People" name="inDto">Group request parameters</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Newly created group with the detailed information</returns>
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

        await messageService.SendAsync(MessageAction.GroupCreated, MessageTarget.Create(group.ID), group.Name);

        return await groupFullDtoHelper.Get(group, true);
    }

    /// <summary>
    /// Updates the existing group changing the group manager, name, and/or members.
    /// </summary>
    /// <short>
    /// Update a group
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">Group ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateGroupRequestDto, ASC.People" name="inDto">Group request parameters</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Updated group with the detailed information</returns>
    /// <path>api/2.0/groups/{id}</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id:guid}")]
    public async Task<GroupDto> UpdateGroupAsync(Guid id, UpdateGroupRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(id);

        group.Name = inDto.GroupName ?? group.Name;
        await userManager.SaveGroupInfoAsync(group);
        
        await TransferUserToDepartmentAsync(inDto.GroupManager, group, true);

        if (inDto.MembersToAdd != null)
        {
            foreach (var memberToAdd in inDto.MembersToAdd)
            {
                await TransferUserToDepartmentAsync(memberToAdd, group, false);
            }
        }
        
        if (inDto.MembersToRemove != null)
        {
            foreach (var memberToRemove in inDto.MembersToRemove)
            {
                await RemoveUserFromDepartmentAsync(memberToRemove, group);
            }
        }

        await messageService.SendAsync(MessageAction.GroupUpdated, MessageTarget.Create(id), group.Name);

        return await GetGroupAsync(id);
    }

    /// <summary>
    /// Deletes a group with the ID specified in the request from the list of groups on the portal.
    /// </summary>
    /// <short>
    /// Delete a group
    /// </short>
    /// <param type="System.Guid, System" method="url" name="id">Group ID</param>
    /// <returns type="Microsoft.AspNetCore.Mvc.NoContentResult, Microsoft.AspNetCore.Mvc">No content</returns>
    /// <path>api/2.0/groups/{id}</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> DeleteGroupAsync(Guid id)
    { 
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(id);

        await userManager.DeleteGroupAsync(id);
        await fileSecurity.RemoveSubjectAsync(id, false);

        await messageService.SendAsync(MessageAction.GroupDeleted, MessageTarget.Create(group.ID), group.Name);

        return NoContent();
    }

    /// <summary>
    /// Moves all the members from the selected group to another one specified in the request.
    /// </summary>
    /// <short>
    /// Move group members
    /// </short>
    /// <param type="System.Guid, System" method="url" name="fromId">Group ID to move from</param>
    /// <param type="System.Guid, System" method="url" name="toId">Group ID to move to</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the detailed information</returns>
    /// <path>api/2.0/groups/{fromId}/members/{toId}</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{fromId:guid}/members/{toId:guid}")]
    public async Task<GroupDto> TransferMembersToAsync(Guid fromId, Guid toId)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var fromGroup = await GetGroupInfoAsync(fromId);
        var toGroup = await GetGroupInfoAsync(toId);

        var users = await userManager.GetUsersByGroupAsync(fromGroup.ID);
        
        foreach (var userInfo in users)
        {
            await TransferUserToDepartmentAsync(userInfo.Id, toGroup, false);
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
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the detailed information</returns>
    /// <path>api/2.0/groups/{id}/members</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("{id:guid}/members")]
    public async Task<GroupDto> SetMembersToAsync(Guid id, GroupRequestDto inDto)
    {
        await RemoveMembersFromAsync(id, new GroupRequestDto { Members = (await userManager.GetUsersByGroupAsync(id)).Select(x => x.Id) });
        await AddMembersToAsync(id, inDto);
        
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
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the detailed information</returns>
    /// <path>api/2.0/groups/{id}/members</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id:guid}/members")]
    public async Task<GroupDto> AddMembersToAsync(Guid id, GroupRequestDto inDto)
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
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the detailed information</returns>
    /// <path>api/2.0/groups/{id}/manager</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id:guid}/manager")]
    public async Task<GroupDto> SetManagerAsync(Guid id, SetManagerRequestDto inDto)
    {
        var group = await GetGroupInfoAsync(id);
        
        if (await userManager.UserExistsAsync(inDto.UserId))
        {
            await TransferUserToDepartmentAsync(inDto.UserId, group, true);
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
    /// <returns type="ASC.People.ApiModels.ResponseDto.GroupDto, ASC.People">Group with the detailed information</returns>
    /// <path>api/2.0/groups/{id}/members</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("{id:guid}/members")]
    public async Task<GroupDto> RemoveMembersFromAsync(Guid id, GroupRequestDto inDto)
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
        var group = await userManager.GetGroupInfoAsync(id);
        if (group == null || group.ID == Constants.LostGroupInfo.ID)
        {
            throw new ItemNotFoundException("group not found");
        }

        return group;
    }

    private async Task TransferUserToDepartmentAsync(Guid userId, GroupInfo group, bool setAsManager)
    {
        var user = await userManager.GetUsersAsync(userId);
        if (userId == Guid.Empty || !userManager.UserExists(user) || user.Status != EmployeeStatus.Active)
        {
            return;
        }

        if (setAsManager)
        {
            await userManager.SetDepartmentManagerAsync(group.ID, userId);
        }
        
        await userManager.AddUserIntoGroupAsync(userId, group.ID, notifyWebSocket: false);
    }

    private async Task RemoveUserFromDepartmentAsync(Guid userId, GroupInfo group)
    {
        if (userId == Guid.Empty || !await userManager.UserExistsAsync(userId))
        {
            return;
        }

        var user = await userManager.GetUsersAsync(userId);
        await userManager.RemoveUserFromGroupAsync(user.Id, group.ID);
        await userManager.UpdateUserInfoAsync(user, notifyWebSocket: false);
    }
}

[ConstraintRoute("int")]
public class GroupControllerInternal(
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    GroupFullDtoHelper groupFullDtoHelper)
    : GroupControllerAdditional<int>(apiContext, daoFactory, fileSecurity, groupFullDtoHelper);

public class GroupControllerThirdParty(
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    GroupFullDtoHelper groupFullDtoHelper)
    : GroupControllerAdditional<string>(apiContext, daoFactory, fileSecurity, groupFullDtoHelper);

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("group")]
public class GroupControllerAdditional<T>(
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    GroupFullDtoHelper groupFullDtoHelper) : ControllerBase
{
    [HttpGet("room/{id}")]
    public async IAsyncEnumerable<GroupDto> GetGroupsWithSharedAsync(T id, bool? excludeShared)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(id)).NotFoundIfNull();

        if (!await fileSecurity.CanEditAccessAsync(room))
        {
            throw new SecurityException();
        }
        
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);
        var text = apiContext.FilterValue;
        
        var securityDao = daoFactory.GetSecurityDao<T>();

        var totalGroups = await securityDao.GetGroupsWithSharedCountAsync(room, text, excludeShared ?? false);

        apiContext.SetCount(Math.Min(Math.Max(totalGroups - offset, 0), count)).SetTotalCount(totalGroups);

        await foreach (var item in securityDao.GetGroupsWithSharedAsync(room, text, excludeShared ?? false, offset, count))
        {
            yield return await groupFullDtoHelper.Get(item.GroupInfo, false, item.Shared);
        }
    }
}