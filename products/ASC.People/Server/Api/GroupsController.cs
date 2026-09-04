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

using ASC.Web.Files.Utils;

namespace ASC.People.Api;

/// <remarks>
/// Groups API.
/// </remarks>
/// <name>group</name>
[Scope]
[ApiEndpoint("group")]
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
    /// Returns the groups of the portal, one page at a time, with the summary information about each of them - the
    /// ID, the name and the manager - but without the member list.
    /// The caller needs the permission to read groups.
    /// The call is read-only, and the number of groups that match the filters is reported in the total count of the
    /// response, so a client can page through them with `count` and `startIndex`.
    /// Narrow the result with `filterValue` on the group name, with `userId` to keep only the groups that account
    /// belongs to, and with `manager` set to true to keep only the groups it manages; order it with `sortBy` and
    /// `sortOrder`, and an unknown `sortBy` falls back to sorting by title.
    /// The entries carry no members - read `GET api/2.0/group/{id}` with `includeMembers` for one group, or
    /// `GET api/2.0/group/user/{userid}` to find the groups of a single account.
    /// </remarks>
    /// <summary>
    /// Get groups
    /// </summary>
    /// <path>api/2.0/group</path>
    /// <collection>list</collection>
    [Tags("Group")]
    [SwaggerResponse(200, "The matching groups, with their summary information", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Returns one group by its ID, with its name, its manager and - when asked for - the accounts that belong to
    /// it.
    /// The caller needs the permission to read groups, and the ID has to belong to a group that has not been
    /// deleted, otherwise the operation answers 404.
    /// The call is read-only, and the member list is left out unless `includeMembers` is set to true, so ask for it
    /// only when the members are actually needed.
    /// Use `GET api/2.0/group` to look a group up by name or to page through them all.
    /// </remarks>
    /// <summary>
    /// Get a group
    /// </summary>
    /// <path>api/2.0/group/{id}</path>
    [Tags("Group")]
    [SwaggerResponse(200, "The group, with its members when includeMembers was set", typeof(GroupDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
    [HttpGet("{id:guid}")]
    public async Task<GroupDto> GetGroup(DetailedInformationRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_ReadGroups);

        return await groupFullDtoHelper.Get(await GetGroupInfoAsync(inDto.Id), inDto.IncludeMembers);
    }

    /// <remarks>
    /// Returns every group the account with the ID in the route belongs to, as a flat list of ID and name pairs.
    /// The caller needs the permission to read groups.
    /// The call is read-only, is not paged, and answers an empty list both for an account that belongs to no group
    /// and for an ID that matches no account, so an empty answer does not prove the account exists.
    /// The entries are summaries and carry neither the manager nor the members - read `GET api/2.0/group/{id}` for
    /// the full picture of one of them.
    /// </remarks>
    /// <summary>
    /// Get user groups
    /// </summary>
    /// <path>api/2.0/group/user/{userid}</path>
    /// <collection>list</collection>
    [Tags("Group")]
    [SwaggerResponse(200, "The groups the account belongs to, as ID and name pairs", typeof(IEnumerable<GroupSummaryDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Creates a group with the given name and, optionally, a manager and a first set of members.
    /// The caller needs the permissions to edit groups and to add and remove users.
    /// The name is required and cannot be blank, and unlike the operations that add members later, this one checks
    /// every listed account upfront and rejects the whole call with 400 if any of them is unusable - a guest, a
    /// disabled account or an ID that matches nobody.
    /// The call is not idempotent: names are not unique, so repeating it creates a second group with the same name.
    /// Creating a group raises a `GroupCreated` webhook, and the answer holds the new group with its members
    /// included.
    /// Members can be changed afterwards through `PUT api/2.0/group/{id}` or the dedicated member operations.
    /// </remarks>
    /// <summary>
    /// Add a new group
    /// </summary>
    /// <path>api/2.0/group</path>
    [Tags("Group")]
    [SwaggerResponse(200, "The new group, with its members", typeof(GroupDto))]
    [SwaggerResponse(400, "The group name is empty, or one of the listed accounts is a guest, is disabled or does not exist")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost]
    public async Task<GroupDto> AddGroup(GroupRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        ArgumentException.ThrowIfNullOrWhiteSpace(inDto.GroupName);

        var userIds = inDto.Members?.ToHashSet() ?? [];
        if (inDto.GroupManager != Guid.Empty)
        {
            userIds.Add(inDto.GroupManager);
        }

        foreach (var userId in userIds)
        {
            if (!await ValidateUserAsync(userId))
            {
                throw new ArgumentException(Resource.ErrorUserNotFound);
            }
        }

        var group = await userManager.SaveGroupInfoAsync(new GroupInfo { Name = inDto.GroupName });

        if (inDto.GroupManager != Guid.Empty)
        {
            await TransferUserToDepartmentAsync(inDto.GroupManager, group, true);
        }

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
    /// Changes the name and the manager of a group and adds or removes members, in one call.
    /// The caller needs the permissions to edit groups and to add and remove users, and the ID has to belong to a
    /// group that has not been deleted, otherwise the operation answers 404.
    /// Every field is optional and the ones that are left out are kept: omitting `groupName` keeps the current name,
    /// and omitting `groupManager` keeps the current manager rather than clearing it.
    /// Accounts in `membersToAdd` that cannot be group members - a guest, a disabled account or an ID that matches
    /// nobody - are silently skipped instead of failing the call, so compare the members in the answer with what was
    /// sent to see what was actually applied.
    /// Members are added first and removed afterwards, an account listed in both lists therefore ends up removed,
    /// and removing an account that is not a member changes nothing.
    /// The change raises a `GroupUpdated` webhook, and the answer holds the group as it is after the update.
    /// </remarks>
    /// <summary>
    /// Update a group
    /// </summary>
    /// <path>api/2.0/group/{id}</path>
    [Tags("Group")]
    [SwaggerResponse(200, "The group as it is after the update", typeof(GroupDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
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
    /// Deletes a group and withdraws the access it had been granted to rooms, folders and files.
    /// The caller needs the permissions to edit groups and to add and remove users, and the ID has to belong to a
    /// group that has not been deleted, otherwise the operation answers 404.
    /// The removal is permanent and cannot be undone, and it affects sharing: everything that was shared with the
    /// group loses that share, so members who had access only through this group lose it too.
    /// The accounts themselves are kept - only their membership disappears.
    /// The call answers 204 with no body and raises a `GroupDeleted` webhook; a second call with the same ID answers
    /// 404 rather than succeeding again.
    /// To empty a group without deleting it, move its members away with
    /// `PUT api/2.0/group/{fromId}/members/{toId}` or remove them through `DELETE api/2.0/group/{id}/members`.
    /// </remarks>
    /// <summary>
    /// Delete a group
    /// </summary>
    /// <path>api/2.0/group/{id}</path>
    [Tags("Group")]
    [SwaggerResponse(204, "The group is deleted. No content is returned")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
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
    /// Moves every member of one group into another group, emptying the first one.
    /// The caller needs the permissions to edit groups and to add and remove users, and both IDs have to belong to
    /// groups that have not been deleted, otherwise the operation answers 404.
    /// The source group is kept, only without members, so delete it separately through
    /// `DELETE api/2.0/group/{id}` if it is no longer needed.
    /// Members that cannot be group members any more are silently skipped rather than failing the call, and an
    /// account that already belongs to the destination is simply left there.
    /// The answer is the destination group with its members, not the source one.
    /// To move a chosen few instead of everybody, use `PUT api/2.0/group/{id}/members` and
    /// `DELETE api/2.0/group/{id}/members`.
    /// </remarks>
    /// <summary>
    /// Move group members
    /// </summary>
    /// <path>api/2.0/group/{fromId}/members/{toId}</path>
    [Tags("Group")]
    [SwaggerResponse(200, "The destination group with its members", typeof(GroupDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has one of the specified IDs")]
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
            await RemoveUserFromDepartmentAsync(userInfo.Id, fromGroup);
        }

        return await GetGroup(new DetailedInformationRequestDto { Id = inDto.ToId });
    }

    /// <remarks>
    /// Replaces the whole member list of a group with the accounts given in the request, removing everybody who is
    /// not in that list.
    /// The caller needs the permissions to edit groups and to add and remove users, and the ID has to belong to a
    /// group that has not been deleted, otherwise the operation answers 404.
    /// At least one of the listed accounts has to be usable as a group member, otherwise the call is rejected with
    /// 400 and the group is left untouched; the accounts that cannot be members - a guest, a disabled account or an
    /// ID that matches nobody - are then silently skipped while the rest are applied.
    /// The replacement is not atomic: the current members are removed first and the new ones added afterwards, so a
    /// failure in between can leave the group empty.
    /// The answer is the group with the members it ends up with, which is why it should be read instead of assuming
    /// the request was applied verbatim.
    /// To add or remove a few accounts without touching the others, use `PUT api/2.0/group/{id}/members` and
    /// `DELETE api/2.0/group/{id}/members`.
    /// </remarks>
    /// <summary>
    /// Replace group members
    /// </summary>
    /// <path>api/2.0/group/{id}/members</path>
    [Tags("Group")]
    [SwaggerResponse(200, "The group with the members it ends up with", typeof(GroupDto))]
    [SwaggerResponse(400, "None of the listed accounts can be a group member")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
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
    /// Adds the listed accounts to a group, keeping the members it already has.
    /// The caller needs the permissions to edit groups and to add and remove users, and the ID has to belong to a
    /// group that has not been deleted, otherwise the operation answers 404.
    /// Accounts that cannot be group members - a guest, a disabled account or an ID that matches nobody - are
    /// silently skipped instead of failing the call, so compare the members in the answer with what was sent to see
    /// what was actually applied.
    /// The call is idempotent for an account that is already a member, and it does not change who manages the group;
    /// use `PUT api/2.0/group/{id}/manager` for that.
    /// The answer is the group with its members after the addition.
    /// To replace the whole list instead of extending it, use `POST api/2.0/group/{id}/members`.
    /// </remarks>
    /// <summary>
    /// Add group members
    /// </summary>
    /// <path>api/2.0/group/{id}/members</path>
    [Tags("Group")]
    [SwaggerResponse(200, "The group with its members after the addition", typeof(GroupDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
    [HttpPut("{id:guid}/members")]
    public async Task<GroupDto> AddMembersTo(MembersRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);

        if (inDto.Members.Members != null)
        {
            foreach (var userId in inDto.Members.Members)
            {
                await TransferUserToDepartmentAsync(userId, group, false);
            }
        }

        return await GetGroup(new DetailedInformationRequestDto { Id = group.ID });
    }

    /// <remarks>
    /// Makes an account the manager of a group, replacing whoever managed it before.
    /// Both the group and the account have to exist: the operation answers 404 when the ID in the route matches no
    /// live group and also when `userId` matches no account, so the message of the error says which of the two was
    /// not found.
    /// The account is added to the group at the same time, so a manager does not have to be a member beforehand, and
    /// the previous manager stays in the group as an ordinary member.
    /// A group has one manager, which makes the call idempotent when it names the account that manages it already.
    /// The answer is the group with its new manager.
    /// To change the members rather than the manager, use `PUT api/2.0/group/{id}/members`.
    /// </remarks>
    /// <summary>
    /// Set a group manager
    /// </summary>
    /// <path>api/2.0/group/{id}/manager</path>
    [Tags("Group")]
    [SwaggerResponse(200, "The group with its new manager", typeof(GroupDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID, or no account has the specified userId")]
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
    /// Removes the listed accounts from a group, leaving the rest of its members in place.
    /// The caller needs the permissions to edit groups and to add and remove users, and the ID has to belong to a
    /// group that has not been deleted, otherwise the operation answers 404.
    /// The accounts themselves are kept; only their membership in this group ends, together with the access they had
    /// through it.
    /// The call is idempotent and forgiving: an ID that is not a member, and one that matches no account at all, are
    /// both skipped without an error, and an empty list simply changes nothing.
    /// The answer is the group with the members that remain.
    /// Emptying a group cannot be done through `POST api/2.0/group/{id}/members`, which needs at least one valid
    /// account, so list every member here, or move them away with `PUT api/2.0/group/{fromId}/members/{toId}`.
    /// </remarks>
    /// <summary>
    /// Remove group members
    /// </summary>
    /// <path>api/2.0/group/{id}/members</path>
    [Tags("Group")]
    [SwaggerResponse(200, "The group with the members that remain", typeof(GroupDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
    [HttpDelete("{id:guid}/members")]
    public async Task<GroupDto> RemoveMembersFrom(MembersRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);

        foreach (var userId in inDto.Members?.Members ?? [])
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

    private async Task TransferUserToDepartmentAsync(Guid userId, GroupInfo group, bool setAsManager, bool validate = true)
    {
        if (validate && !await ValidateUserAsync(userId))
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
        return userId != Guid.Empty &&
               userManager.UserExists(user) &&
               user.Status != EmployeeStatus.Terminated &&
               !await userManager.IsGuestAsync(userId);
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

/// <remarks>
/// Groups sharing API.
/// </remarks>
[Scope]
[ApiEndpoint("group")]
public class GroupControllerAdditional<T>(
    ApiContext apiContext,
    IDaoFactory daoFactory,
    FileSharing fileSharing,
    FileSecurity fileSecurity,
    GroupFullDtoHelper groupFullDtoHelper) : ControllerBase
{
    /// <remarks>
    /// Returns the groups that can be given access to the room with the ID given in the route, and reports for each
    /// of them whether it already has access to that room.
    /// The caller has to be allowed to manage the access of that room, and the ID has to belong to an existing room,
    /// so the operation answers 403 for a room the caller cannot share and 404 for an ID that matches nothing.
    /// The call is read-only and, unlike the account search, works without a filter: leaving `filterValue` empty
    /// returns every group instead of nothing, and a value narrows the result by group name.
    /// The result is paged by `count` and `startIndex`, with the number of matching groups in the total count of the
    /// response.
    /// Pass `excludeShared` to keep only the groups that have no access to the room yet, which is the set to offer
    /// when adding new ones; without it every matching group comes back and `shared` tells them apart.
    /// To search users and groups together, use `GET api/2.0/accounts/room/{id}/search`.
    /// </remarks>
    /// <summary>Search groups for a room</summary>
    /// <path>api/2.0/group/room/{id}</path>
    /// <collection>list</collection>
    [Tags("Group / Search")]
    [SwaggerResponse(200, "The matching groups, each with its access state for the room", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No room has the specified ID")]
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
    /// Returns the groups that can be given access to the folder with the ID given in the route, and reports for
    /// each of them whether it already has access to that folder.
    /// The caller has to be allowed to manage the access of that folder, and the ID has to belong to an existing
    /// folder, so the operation answers 403 for a folder the caller cannot share and 404 for an ID that matches
    /// nothing.
    /// The call is read-only and, unlike the account search, works without a filter: leaving `filterValue` empty
    /// returns every group instead of nothing, and a value narrows the result by group name.
    /// The result is paged by `count` and `startIndex`, with the number of matching groups in the total count of the
    /// response.
    /// Pass `excludeShared` to keep only the groups that have no access to the folder yet, which is the set to offer
    /// when adding new ones; without it every matching group comes back and `shared` tells them apart.
    /// To search users and groups together, use `GET api/2.0/accounts/folder/{id}/search`.
    /// </remarks>
    /// <summary>Search groups for a folder</summary>
    /// <path>api/2.0/group/folder/{id}</path>
    /// <collection>list</collection>
    [Tags("Group / Search")]
    [SwaggerResponse(200, "The matching groups, each with its access state for the folder", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No folder has the specified ID")]
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
    /// Returns the groups that can be given access to the file with the ID given in the route, and reports for each
    /// of them whether it already has access to that file.
    /// The caller has to be allowed to manage the access of that file, and the ID has to belong to an existing file,
    /// so the operation answers 403 for a file the caller cannot share and 404 for an ID that matches nothing.
    /// The call is read-only and, unlike the account search, works without a filter: leaving `filterValue` empty
    /// returns every group instead of nothing, and a value narrows the result by group name.
    /// The result is paged by `count` and `startIndex`, with the number of matching groups in the total count of the
    /// response.
    /// Pass `excludeShared` to keep only the groups that have no access to the file yet, which is the set to offer
    /// when adding new ones; without it every matching group comes back and `shared` tells them apart.
    /// To search users and groups together, use `GET api/2.0/accounts/file/{id}/search`.
    /// </remarks>
    /// <summary>Search groups for a file</summary>
    /// <path>api/2.0/group/file/{id}</path>
    /// <collection>list</collection>
    [Tags("Group / Search")]
    [SwaggerResponse(200, "The matching groups, each with its access state for the file", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No file has the specified ID")]
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

        var parentUserIds = await fileSharing.GetPureSharesAsync(fileEntry, ShareFilterType.Group, null, inDto.Text, 0, int.MaxValue).Select(r => r.Id).ToListAsync();
        var securityDao = daoFactory.GetSecurityDao<T>();

        var totalGroups = await securityDao.GetGroupsWithSharedCountAsync(fileEntry, text, inDto.ExcludeShared ?? false, parentUserIds);

        apiContext.SetCount(Math.Min(Math.Max(totalGroups - offset, 0), count)).SetTotalCount(totalGroups);

        await foreach (var item in securityDao.GetGroupsWithSharedAsync(fileEntry, text, inDto.ExcludeShared ?? false, offset, count, parentUserIds))
        {
            yield return await groupFullDtoHelper.Get(item.GroupInfo, false, item.Shared);
        }
    }
}
