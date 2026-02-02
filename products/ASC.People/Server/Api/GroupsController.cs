// (c) Copyright Ascensio System SIA 2009-2026
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

using ASC.Web.Files.Utils;

namespace ASC.People.Api;

///<remarks>
/// Groups API.
///</remarks>
///<name>group</name>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("group")]
public class GroupController(
    GroupSummaryDtoHelper groupSummaryDtoHelper,
    UserManager userManager,
    ApiContext apiContext,
    GroupFullDtoHelper groupFullDtoHelper,
    MessageService messageService,
    PermissionContext permissionContext,
    FileSecurity fileSecurity,
    UserSocketManager socketManager,
    UserWebhookManager webhookManager)
    : ControllerBase
{
    /// <remarks>
    /// Returns the general information about all the groups, such as group ID and group manager.
    /// </remarks>
    /// <summary>
    /// Get groups
    /// </summary>
    /// <remarks>
    /// This method returns partial group information.
    /// </remarks>
    /// <path>api/2.0/group</path>
    /// <collection>list</collection>
    [Tags("Group")]
    [SwaggerResponse(200, "List of groups", typeof(IAsyncEnumerable<GroupDto>))]
    [HttpGet]
    public async IAsyncEnumerable<GroupDto> GetGroups(GeneralInformationRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_ReadGroups);

        var offset = inDto.StartIndex;
        var count = inDto.Count;
        var text = inDto.Text;

        var memberId = inDto.UserId ?? Guid.Empty;
        var asManager = inDto.Manager ?? false;

        if (!GroupSortTypeExtensions.TryParse(inDto.SortBy, true, out var sortBy))
        {
            sortBy = GroupSortType.Title;
        }

        var totalCount = await userManager.GetGroupsCountAsync(text, memberId, asManager);

        apiContext.SetCount(Math.Min(Math.Max(totalCount - offset, 0), count)).SetTotalCount(totalCount);

        await foreach (var g in userManager.GetGroupsAsync(text, memberId, asManager, sortBy, inDto.SortOrder == SortOrder.Ascending, offset, count))
        {
            yield return await groupFullDtoHelper.Get(g, false);
        }
    }

    /// <remarks>
    /// Returns the detailed information about the selected group.
    /// </remarks>
    /// <summary>
    /// Get a group
    /// </summary>
    /// <remarks>
    /// This method returns full group information.
    /// </remarks>
    /// <path>api/2.0/group/{id}</path>
    [Tags("Group")]
    [SwaggerResponse(200, "Group with the detailed information", typeof(GroupDto))]
    [SwaggerResponse(404, "Group not found")]
    [HttpGet("{id:guid}")]
    public async Task<GroupDto> GetGroup(DetailedInformationRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_ReadGroups);

        return await groupFullDtoHelper.Get(await GetGroupInfoAsync(inDto.Id), inDto.IncludeMembers);
    }

    /// <remarks>
    /// Returns a list of groups for the user with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Get user groups
    /// </summary>
    /// <path>api/2.0/group/user/{userid}</path>
    /// <collection>list</collection>
    [Tags("Group")]
    [SwaggerResponse(200, "List of groups", typeof(IEnumerable<GroupSummaryDto>))]
    [HttpGet("user/{userid:guid}")]
    public async Task<IEnumerable<GroupSummaryDto>> GetGroupByUserId(GetGroupByUserIdRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_ReadGroups);
        var groups = await userManager.GetUserGroupsAsync(inDto.UserId);
        List<GroupSummaryDto> result = new(groups.Count);

        foreach (var g in groups)
        {
            result.Add(await groupSummaryDtoHelper.GetAsync(g));
        }

        return result;
    }

    /// <remarks>
    /// Adds a new group with the group manager, name, and members specified in the request.
    /// </remarks>
    /// <summary>
    /// Add a new group
    /// </summary>
    /// <path>api/2.0/group</path>
    [Tags("Group")]
    [SwaggerResponse(200, "Newly created group with the detailed information", typeof(GroupDto))]
    [HttpPost]
    public async Task<GroupDto> AddGroup(GroupRequestDto inDto)
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

        messageService.Send(MessageAction.GroupCreated, MessageTarget.Create(group.ID), group.Name);

        var dto = await groupFullDtoHelper.Get(group, true);

        await socketManager.AddGroupAsync(dto);

        await webhookManager.PublishAsync(WebhookTrigger.GroupCreated, group);

        return dto;
    }

    /// <remarks>
    /// Updates the existing group changing the group manager, name, and/or members.
    /// </remarks>
    /// <summary>
    /// Update a group
    /// </summary>
    /// <path>api/2.0/group/{id}</path>
    [Tags("Group")]
    [SwaggerResponse(200, "Updated group with the detailed information", typeof(GroupDto))]
    [SwaggerResponse(404, "Group not found")]
    [HttpPut("{id:guid}")]
    public async Task<GroupDto> UpdateGroup(UpdateGroupRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);

        group.Name = inDto.Update.GroupName ?? group.Name;
        await userManager.SaveGroupInfoAsync(group);

        await TransferUserToDepartmentAsync(inDto.Update.GroupManager, group, true);

        if (inDto.Update.MembersToAdd != null)
        {
            foreach (var memberToAdd in inDto.Update.MembersToAdd)
            {
                await TransferUserToDepartmentAsync(memberToAdd, group, false);
            }
        }

        if (inDto.Update.MembersToRemove != null)
        {
            foreach (var memberToRemove in inDto.Update.MembersToRemove)
            {
                await RemoveUserFromDepartmentAsync(memberToRemove, group);
            }
        }

        messageService.Send(MessageAction.GroupUpdated, MessageTarget.Create(inDto.Id), group.Name);

        var dto = await GetGroup(new DetailedInformationRequestDto { Id = inDto.Id });

        await socketManager.UpdateGroupAsync(dto);

        await webhookManager.PublishAsync(WebhookTrigger.GroupUpdated, group);

        return dto;
    }

    /// <remarks>
    /// Deletes a group with the ID specified in the request from the list of groups on the portal.
    /// </remarks>
    /// <summary>
    /// Delete a group
    /// </summary>
    /// <path>api/2.0/group/{id}</path>
    [Tags("Group")]
    [SwaggerResponse(200, "No content", typeof(NoContentResult))]
    [SwaggerResponse(404, "Group not found")]
    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> DeleteGroup(GetGroupByIdRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);

        await userManager.DeleteGroupAsync(inDto.Id);
        await fileSecurity.RemoveSubjectAsync(inDto.Id, false);

        messageService.Send(MessageAction.GroupDeleted, MessageTarget.Create(group.ID), group.Name);

        await socketManager.DeleteGroupAsync(inDto.Id);

        await webhookManager.PublishAsync(WebhookTrigger.GroupDeleted, group);

        return NoContent();
    }

    /// <remarks>
    /// Moves all the members from the selected group to another one specified in the request.
    /// </remarks>
    /// <summary>
    /// Move group members
    /// </summary>
    /// <path>api/2.0/group/{fromId}/members/{toId}</path>
    [Tags("Group")]
    [SwaggerResponse(200, "Group with the detailed information", typeof(GroupDto))]
    [SwaggerResponse(404, "Group not found")]
    [HttpPut("{fromId:guid}/members/{toId:guid}")]
    public async Task<GroupDto> MoveMembersTo(MoveGroupMemebersRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var fromGroup = await GetGroupInfoAsync(inDto.FromId);
        var toGroup = await GetGroupInfoAsync(inDto.ToId);

        var users = await userManager.GetUsersByGroupAsync(fromGroup.ID);

        foreach (var userInfo in users)
        {
            await TransferUserToDepartmentAsync(userInfo.Id, toGroup, false);
        }

        return await GetGroup(new DetailedInformationRequestDto { Id = inDto.ToId });
    }

    /// <remarks>
    /// Replaces the group members with those specified in the request.
    /// </remarks>
    /// <summary>
    /// Replace group members
    /// </summary>
    /// <path>api/2.0/group/{id}/members</path>
    [Tags("Group")]
    [SwaggerResponse(200, "Group with the detailed information", typeof(GroupDto))]
    [HttpPost("{id:guid}/members")]
    public async Task<GroupDto> SetMembersTo(MembersRequestDto inDto)
    {
        var anyValidMembers = await inDto.Members.Members
            .ToAsyncEnumerable()
            .AnyAsync(async (userId, _) => await ValidateUserAsync(userId));
        
        if (!anyValidMembers)
        {
            throw new ArgumentException(nameof(inDto.Members.Members));
        }
        
        await RemoveMembersFrom(new MembersRequestDto { Id = inDto.Id, Members = new MembersRequest { Members = (await userManager.GetUsersByGroupAsync(inDto.Id)).Select(x => x.Id) } });
        await AddMembersTo(inDto);

        return await GetGroup(new DetailedInformationRequestDto { Id = inDto.Id });
    }

    /// <remarks>
    /// Adds new group members to the group with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Add group members
    /// </summary>
    /// <path>api/2.0/group/{id}/members</path>
    [Tags("Group")]
    [SwaggerResponse(200, "Group with the detailed information", typeof(GroupDto))]
    [SwaggerResponse(404, "Group not found")]
    [HttpPut("{id:guid}/members")]
    public async Task<GroupDto> AddMembersTo(MembersRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);

        foreach (var userId in inDto.Members.Members)
        {
            await TransferUserToDepartmentAsync(userId, group, false);
        }

        return await GetGroup(new DetailedInformationRequestDto { Id = group.ID });
    }

    /// <remarks>
    /// Sets a user with the ID specified in the request as a group manager.
    /// </remarks>
    /// <summary>
    /// Set a group manager
    /// </summary>
    /// <path>api/2.0/group/{id}/manager</path>
    [Tags("Group")]
    [SwaggerResponse(200, "Group with the detailed information", typeof(GroupDto))]
    [SwaggerResponse(404, "User not found")]
    [HttpPut("{id:guid}/manager")]
    public async Task<GroupDto> SetGroupManager(SetManagerRequestDto inDto)
    {
        var group = await GetGroupInfoAsync(inDto.Id);

        if (await userManager.UserExistsAsync(inDto.SetManager.UserId))
        {
            await TransferUserToDepartmentAsync(inDto.SetManager.UserId, group, true);
        }
        else
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        return await GetGroup(new DetailedInformationRequestDto { Id = inDto.Id });
    }

    /// <remarks>
    /// Removes the group members specified in the request from the selected group.
    /// </remarks>
    /// <summary>
    /// Remove group members
    /// </summary>
    /// <path>api/2.0/group/{id}/members</path>
    [Tags("Group")]
    [SwaggerResponse(200, "Group with the detailed information", typeof(GroupDto))]
    [SwaggerResponse(404, "Group not found")]
    [HttpDelete("{id:guid}/members")]
    public async Task<GroupDto> RemoveMembersFrom(MembersRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);

        foreach (var userId in inDto.Members.Members)
        {
            await RemoveUserFromDepartmentAsync(userId, group);
        }

        return await GetGroup(new DetailedInformationRequestDto { Id = group.ID });
    }

    private async Task<GroupInfo> GetGroupInfoAsync(Guid id)
    {
        var group = await userManager.GetGroupInfoAsync(id);
        if (group == null || group.Removed || group.ID == Constants.LostGroupInfo.ID)
        {
            throw new ItemNotFoundException(Resource.ErrorGroupNotFound);
        }

        return group;
    }

    private async Task TransferUserToDepartmentAsync(Guid userId, GroupInfo group, bool setAsManager)
    {
        if (await ValidateUserAsync(userId))
        {
            return;
        }

        if (setAsManager)
        {
            await userManager.SetDepartmentManagerAsync(group.ID, userId);
        }

        await userManager.AddUserIntoGroupAsync(userId, group.ID, notifyWebSocket: false);
    }

    private async Task<bool> ValidateUserAsync(Guid userId)
    {
        var user = await userManager.GetUsersAsync(userId);
        return userId == Guid.Empty || 
               !userManager.UserExists(user) || 
               user.Status == EmployeeStatus.Terminated || 
               await userManager.IsGuestAsync(userId);
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
    FileSharing fileSharing,
    FileSecurity fileSecurity,
    GroupFullDtoHelper groupFullDtoHelper)
    : GroupControllerAdditional<int>(apiContext, daoFactory, fileSharing, fileSecurity, groupFullDtoHelper);

public class GroupControllerThirdParty(
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSharing fileSharing,
    FileSecurity fileSecurity,
    GroupFullDtoHelper groupFullDtoHelper)
    : GroupControllerAdditional<string>(apiContext, daoFactory, fileSharing, fileSecurity, groupFullDtoHelper);

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("group")]
public class GroupControllerAdditional<T>(
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSharing fileSharing,
    FileSecurity fileSecurity,
    GroupFullDtoHelper groupFullDtoHelper) : ControllerBase
{
    /// <remarks>
    /// Returns groups with their sharing settings in a room with the ID specified in request.
    /// </remarks>
    /// <summary>Get groups with room sharing settings</summary>
    /// <path>api/2.0/group/room/{id}</path>
    /// <collection>list</collection>
    [Tags("Group / Search")]
    [SwaggerResponse(200, "Ok", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("room/{id}")]
    public async IAsyncEnumerable<GroupDto> GetGroupsWithRoomsShared(GetGroupsWithSharedRequestDto<T> inDto)
    {
        var room = (await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetGroups(inDto, room))
        {
            yield return p;
        }
    }

    /// <remarks>
    /// Returns groups with their sharing settings in a folder with the ID specified in request.
    /// </remarks>
    /// <summary>Get groups with folder sharing settings</summary>
    /// <path>api/2.0/group/folder/{id}</path>
    /// <collection>list</collection>
    [Tags("Group / Search")]
    [SwaggerResponse(200, "Ok", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("folder/{id}")]
    public async IAsyncEnumerable<GroupDto> GetGroupsWithFoldersShared(GetGroupsWithSharedRequestDto<T> inDto)
    {
        var folder = (await daoFactory.GetFolderDao<T>().GetFolderAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetGroups(inDto, folder))
        {
            yield return p;
        }
    }

    /// <remarks>
    /// Returns groups with their sharing settings for a file with the ID specified in request.
    /// </remarks>
    /// <summary>Get groups with file sharing settings</summary>
    /// <path>api/2.0/group/file/{id}</path>
    /// <collection>list</collection>
    [Tags("Group / Search")]
    [SwaggerResponse(200, "Ok", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("file/{id}")]
    public async IAsyncEnumerable<GroupDto> GetGroupsWithFilesShared(GetGroupsWithSharedRequestDto<T> inDto)
    {
        var file = (await daoFactory.GetFileDao<T>().GetFileAsync(inDto.Id)).NotFoundIfNull();

        await foreach (var p in GetGroups(inDto, file))
        {
            yield return p;
        }
    }

    private async IAsyncEnumerable<GroupDto> GetGroups(GetGroupsWithSharedRequestDto<T> inDto, FileEntry<T> fileEntry)
    {
        if (!await fileSecurity.CanEditAccessAsync(fileEntry))
        {
            throw new SecurityException();
        }

        var offset = inDto.StartIndex;
        var count = inDto.Count;
        var text = inDto.Text;
        
        var parentUserIds = await fileSharing.GetPureSharesAsync(fileEntry, ShareFilterType.Group, null, inDto.Text, 0, int.MaxValue).Select(r=> r.Id).ToListAsync();
        var securityDao = daoFactory.GetSecurityDao<T>();

        var totalGroups = await securityDao.GetGroupsWithSharedCountAsync(fileEntry, text, inDto.ExcludeShared ?? false, parentUserIds);

        apiContext.SetCount(Math.Min(Math.Max(totalGroups - offset, 0), count)).SetTotalCount(totalGroups);

        await foreach (var item in securityDao.GetGroupsWithSharedAsync(fileEntry, text, inDto.ExcludeShared ?? false, offset, count, parentUserIds))
        {
            yield return await groupFullDtoHelper.Get(item.GroupInfo, false, item.Shared);
        }
    }
}