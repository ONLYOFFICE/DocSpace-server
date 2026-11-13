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
    UserWebhookManager webhookManager,
    ILogger<GroupController> logger)
    : ControllerBase
{
    /// <remarks>
    /// Returns the groups of the portal, one page at a time. Each entry has the ID, the name, and the manager, but no
    /// members. Requires a DocSpace admin or a room admin. Other callers get 403. The call is read-only. The
    /// response reports the total number of matching groups, so you can page with `count` and `startIndex`. The
    /// call matches `filterValue` against any part of the group name and ignores case. With `userId`, it keeps only
    /// the groups of that account. With `manager` set to true, it keeps only the groups that account manages. An
    /// unknown `sortBy` value orders the groups by title. The call returns 400 if `count` is out of range or a
    /// parameter has the wrong type. To read the members of one group, use `GET api/2.0/group/{id}`.
    /// </remarks>
    /// <summary>
    /// Get groups
    /// </summary>
    /// <path>api/2.0/group</path>
    /// <collection>list</collection>
    [Tags("Group")]
    [SwaggerResponse(200, "The matching groups, with their summary information", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
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
    /// Returns one group by its ID, with its name, its manager, and its members. Requires a DocSpace admin or a room
    /// admin. Other callers get 403. The call is read-only. Set `includeMembers` to false to leave out the members
    /// of a large group. If no group has this ID or the group is deleted, the call returns 404. To find a group by
    /// name, use `GET api/2.0/group`.
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
    /// Returns all groups that an account belongs to, as ID and name pairs. Requires a DocSpace admin or a room
    /// admin. Other callers get 403, even for their own groups. The call is read-only and does not page the list.
    /// The entries have no manager and no members. An empty list means the account is in no group or does not
    /// exist. To read one group in full, use `GET api/2.0/group/{id}`.
    /// </remarks>
    /// <summary>
    /// Get user groups
    /// </summary>
    /// <path>api/2.0/group/user/{userId}</path>
    /// <collection>list</collection>
    [Tags("Group")]
    [SwaggerResponse(200, "The groups the account belongs to, as ID and name pairs", typeof(IEnumerable<GroupSummaryDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("user/{userId:guid}")]
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
    /// Creates a group with a name and, optionally, a manager and members. The manager also becomes a member.
    /// Requires a DocSpace admin. Other callers get 403. A room admin can read groups but also gets 403 here. The
    /// call changes data. Do not repeat it. Group names are not unique, so each call creates another group. The
    /// response is the new group with its members. The call returns 400 if the name is missing, blank, or too long.
    /// It also returns 400 if the manager or any member is a guest, a disabled account, or an unknown ID. In that
    /// case, the call creates nothing.
    /// </remarks>
    /// <summary>
    /// Add a new group
    /// </summary>
    /// <path>api/2.0/group</path>
    [RaisesWebhook(WebhookTrigger.GroupCreated, When = "once for the new group, without `user.*` webhooks for its members")]
    [Tags("Group")]
    [SwaggerResponse(200, "The new group, with its members", typeof(GroupDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `groupName`, the group name is `null`, blank or longer than 128 characters, or one of the listed members or the manager is a guest, is disabled or does not exist")]
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
    /// Changes the name and the manager of a group, and adds or removes members, in one call. Requires a DocSpace
    /// admin. Other callers get 403. A room admin can read groups but also gets 403 here. The call changes data. You
    /// can repeat it with the same body. A field that you leave out keeps its current value. The call cannot clear the
    /// manager through `groupManager`. If `membersToRemove` lists the manager, the group is left without one. It skips
    /// a guest, a disabled account, or an unknown ID in `groupManager` and `membersToAdd`. It also skips an ID in
    /// `membersToRemove` that is not a member. The call adds members first and then removes them. So an account in both
    /// lists ends up removed. The response is the group after the update. Compare it with the request to see what the
    /// call skipped. If no group has this ID, the call returns 404. The call returns 400 if `groupName` is too long.
    /// </remarks>
    /// <summary>
    /// Update a group
    /// </summary>
    /// <path>api/2.0/group/{id}</path>
    [RaisesWebhook(WebhookTrigger.GroupUpdated, When = "only when the name, the manager, or the members change, without `user.*` webhooks")]
    [Tags("Group")]
    [SwaggerResponse(200, "The group as it is after the update", typeof(GroupDto))]
    [SwaggerResponse(400, "The request body cannot be read, or `groupName` is longer than 128 characters")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
    [HttpPut("{id:guid}")]
    public async Task<GroupDto> UpdateGroup(UpdateGroupRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);

        // group.updated goes out only for a real change: the name is compared here, the manager and the members by
        // PublishIfChangedAsync, as the membership calls do
        var nameBefore = group.Name;
        var membershipBefore = await GetMembershipAsync(group);

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

        if (!string.Equals(group.Name, nameBefore, StringComparison.Ordinal))
        {
            await webhookManager.PublishAsync(WebhookTrigger.GroupUpdated, group);
        }
        else
        {
            await PublishIfChangedAsync(group, membershipBefore);
        }

        return dto;
    }

    /// <remarks>
    /// Deletes a group and removes its access to rooms, folders, and files. Members who had access only through the
    /// group lose it. The call keeps the accounts of the members. Requires a DocSpace admin. Other callers get 403.
    /// The deletion cannot be undone. A repeated call returns 404. The call returns 204 with no body. If no group
    /// has this ID, the call returns 404. To empty a group without deleting it, use
    /// `DELETE api/2.0/group/{id}/members`.
    /// </remarks>
    /// <summary>
    /// Delete a group
    /// </summary>
    /// <path>api/2.0/group/{id}</path>
    [RaisesWebhook(WebhookTrigger.GroupDeleted, When = "once for the deleted group, without `user.*` webhooks for its former members")]
    [Tags("Group")]
    [SwaggerResponse(204, "The group is deleted. No content is returned")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
    [HttpDelete("{id:guid}")]
    public async Task<NoContentResult> DeleteGroup(GetGroupByIdRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);

        await webhookManager.PublishAfterAsync(WebhookTrigger.GroupDeleted, group, async () =>
        {
            await userManager.DeleteGroupAsync(inDto.Id);
            await fileSecurity.RemoveSubjectAsync(inDto.Id, false);

            messageService.Send(MessageAction.GroupDeleted, MessageTarget.Create(group.ID), group.Name);

            await socketManager.DeleteGroupAsync(inDto.Id);
        });

        return NoContent();
    }

    /// <remarks>
    /// Moves the members of one group to another group. The source group stays, without these members. Requires a
    /// DocSpace admin. Other callers get 403. A room admin can read groups but also gets 403 here. The call changes
    /// data, and you can repeat it. If both IDs are the same, the call changes nothing. The destination group keeps
    /// its manager. If the manager of the source group leaves it, the source group has no manager after the call.
    /// The call skips disabled members, so they stay in the source group. A guest member leaves the source group
    /// but does not join the destination. The response is the destination group with its members. If no group has
    /// one of the IDs, the call returns 404. To move only some members, use `PUT api/2.0/group/{id}/members` and
    /// `DELETE api/2.0/group/{id}/members`. To delete the source group, use `DELETE api/2.0/group/{id}`.
    /// </remarks>
    /// <summary>
    /// Move group members
    /// </summary>
    /// <path>api/2.0/group/{fromId}/members/{toId}</path>
    [RaisesWebhook(WebhookTrigger.GroupUpdated, When = "for the source group if the two IDs differ and the source has enabled members")]
    [RaisesWebhook(WebhookTrigger.GroupUpdated, When = "for the destination group only when its members change, without `user.*` webhooks")]
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

        // Moving a group onto itself is a no-op. Without this guard the loop below adds each member
        // back into the same group and then removes them from it, leaving the group empty.
        if (fromGroup.ID == toGroup.ID)
        {
            return await GetGroup(new DetailedInformationRequestDto { Id = inDto.ToId });
        }

        var users = await userManager.GetUsersByGroupAsync(fromGroup.ID);

        if (users.Length == 0)
        {
            return await GetGroup(new DetailedInformationRequestDto { Id = inDto.ToId });
        }

        var toGroupBefore = await GetMembershipAsync(toGroup);

        foreach (var userInfo in users)
        {
            await TransferUserToDepartmentAsync(userInfo.Id, toGroup, false);
            await RemoveUserFromDepartmentAsync(userInfo.Id, fromGroup);
        }

        await webhookManager.PublishAsync(WebhookTrigger.GroupUpdated, fromGroup);
        await PublishIfChangedAsync(toGroup, toGroupBefore);

        return await GetGroup(new DetailedInformationRequestDto { Id = inDto.ToId });
    }

    /// <remarks>
    /// Replaces the members of a group with the listed accounts. Requires a DocSpace admin. Other callers get 403. A
    /// room admin can read groups but also gets 403 here. The call changes data. You can repeat it with the same
    /// body. First, the call removes the current members, except disabled ones. Then it adds the listed accounts. It
    /// skips a guest, a disabled account, or an unknown ID. These two steps are not atomic. So a failure between
    /// them can leave the group empty. The response is the group with the members it ends up with. If no group has
    /// this ID, the call returns 404. The call returns 400 if the list is missing, empty, or has no account that can
    /// be a member. In that case, the group does not change. To add or remove a few accounts, use
    /// `PUT api/2.0/group/{id}/members` or `DELETE api/2.0/group/{id}/members`.
    /// </remarks>
    /// <summary>
    /// Replace group members
    /// </summary>
    /// <path>api/2.0/group/{id}/members</path>
    [RaisesWebhook(WebhookTrigger.GroupUpdated, When = "only when the members or the manager change, without `user.*` webhooks for them")]
    [Tags("Group")]
    [SwaggerResponse(200, "The group with the members it ends up with", typeof(GroupDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `members` list, or none of the listed accounts can be a group member")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
    [HttpPost("{id:guid}/members")]
    public async Task<GroupDto> SetMembersTo(MembersRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        // Resolved here, before the payload is validated, so that an unknown ID answers 404 instead of the 400 of
        // the member check below. The removals and the additions then reuse this instance and go through the private
        // helpers rather than through AddMembersTo/RemoveMembersFrom: those are endpoints of their own and would
        // repeat the permission demand, the lookup and the response DTO for every call.
        var group = await GetGroupInfoAsync(inDto.Id);

        var anyValidMembers = await inDto.Members.Members
            .ToAsyncEnumerable()
            .AnyAsync(async (userId, _) => await ValidateUserAsync(userId));

        if (!anyValidMembers)
        {
            throw new ArgumentException(nameof(inDto.Members.Members));
        }

        var before = await GetMembershipAsync(group);

        foreach (var user in await userManager.GetUsersByGroupAsync(group.ID))
        {
            await RemoveUserFromDepartmentAsync(user.Id, group);
        }

        foreach (var userId in inDto.Members.Members)
        {
            await TransferUserToDepartmentAsync(userId, group, false);
        }

        await PublishIfChangedAsync(group, before);

        return await GetGroup(new DetailedInformationRequestDto { Id = group.ID });
    }

    /// <remarks>
    /// Adds the listed accounts to a group and keeps its current members. Requires a DocSpace admin. Other callers
    /// get 403. A room admin can read groups but also gets 403 here. The call changes data. You can repeat it with
    /// the same body. The call skips a guest, a disabled account, an unknown ID, and a current member. An empty list
    /// changes nothing. The response is the group with its members after the call. If no group has this ID, the call
    /// returns 404. This call does not change the manager. To set the manager, use
    /// `PUT api/2.0/group/{id}/manager`. To replace all members, use `POST api/2.0/group/{id}/members`.
    /// </remarks>
    /// <summary>
    /// Add group members
    /// </summary>
    /// <path>api/2.0/group/{id}/members</path>
    [RaisesWebhook(WebhookTrigger.GroupUpdated, When = "only when the call adds an account, without `user.*` webhooks for it")]
    [Tags("Group")]
    [SwaggerResponse(200, "The group with its members after the addition", typeof(GroupDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
    [HttpPut("{id:guid}/members")]
    public async Task<GroupDto> AddMembersTo(MembersRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);

        if (inDto.Members.Members?.Any() == true)
        {
            var before = await GetMembershipAsync(group);

            foreach (var userId in inDto.Members.Members)
            {
                await TransferUserToDepartmentAsync(userId, group, false);
            }

            await PublishIfChangedAsync(group, before);
        }

        return await GetGroup(new DetailedInformationRequestDto { Id = group.ID });
    }

    /// <remarks>
    /// Makes an account the manager of a group and replaces the previous manager. The account also becomes a member.
    /// The previous manager stays a member. Requires a DocSpace admin. Other callers get 403. A room admin can read
    /// groups but also gets 403 here. The call changes data. You can repeat it with the same body. If the account is
    /// a guest or disabled, the call changes nothing. The response is the group with its manager. The call returns
    /// 404 if no group has this ID or no account has `userId`. It also returns 400 if `userId` is missing. To
    /// change the members, use `PUT api/2.0/group/{id}/members`.
    /// </remarks>
    /// <summary>
    /// Set a group manager
    /// </summary>
    /// <path>api/2.0/group/{id}/manager</path>
    [RaisesWebhook(WebhookTrigger.GroupUpdated, When = "only when the manager or the members change, without `user.*` webhooks for them")]
    [Tags("Group")]
    [SwaggerResponse(200, "The group with its new manager", typeof(GroupDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `userId`")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID, or no account has the specified userId")]
    [HttpPut("{id:guid}/manager")]
    public async Task<GroupDto> SetGroupManager(SetManagerRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);

        if (!await userManager.UserExistsAsync(inDto.SetManager.UserId))
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        var before = await GetMembershipAsync(group);

        await TransferUserToDepartmentAsync(inDto.SetManager.UserId, group, true);

        await PublishIfChangedAsync(group, before);

        return await GetGroup(new DetailedInformationRequestDto { Id = inDto.Id });
    }

    /// <remarks>
    /// Removes the listed accounts from a group and keeps the other members. The accounts stay, but they lose the
    /// access they had through the group. Requires a DocSpace admin. Other callers get 403. A room admin can read
    /// groups but also gets 403 here. The call changes data. You can repeat it with the same body. The call skips
    /// an ID that is not a member or matches no account. If the list includes the manager, the group is left without
    /// one. An empty list changes nothing. The response is the group
    /// with the remaining members. If no group has this ID, the call returns 404. To remove all members, list them
    /// all here.
    /// </remarks>
    /// <summary>
    /// Remove group members
    /// </summary>
    /// <path>api/2.0/group/{id}/members</path>
    [RaisesWebhook(WebhookTrigger.GroupUpdated, When = "only when the call removes a member, without `user.*` webhooks for it")]
    [Tags("Group")]
    [SwaggerResponse(200, "The group with the members that remain", typeof(GroupDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "No group has the specified ID")]
    [HttpDelete("{id:guid}/members")]
    public async Task<GroupDto> RemoveMembersFrom(MembersRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(Constants.Action_EditGroups, Constants.Action_AddRemoveUser);

        var group = await GetGroupInfoAsync(inDto.Id);
        var userIds = inDto.Members?.Members ?? [];

        if (userIds.Any())
        {
            var before = await GetMembershipAsync(group);

            foreach (var userId in userIds)
            {
                await RemoveUserFromDepartmentAsync(userId, group);
            }

            await PublishIfChangedAsync(group, before);
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

    /// <summary>
    /// The members and the manager of a group, taken before a membership call so that <see cref="PublishIfChangedAsync"/>
    /// can tell whether the call changed anything: `group.updated` is sent only for a real change. Null when no
    /// subscription listens to `group.updated`: the snapshot reads every member of the group, which only an event pays for.
    /// Null as well when it cannot be read: the webhook is best effort and must not stop the membership call.
    /// </summary>
    private async Task<(Guid Manager, HashSet<Guid> Members)?> GetMembershipAsync(GroupInfo group)
    {
        try
        {
            if (!await webhookManager.HasSubscribersAsync(WebhookTrigger.GroupUpdated))
            {
                return null;
            }

            return await ReadMembershipAsync(group);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.ErrorReadGroupMembership(group.ID, e);

            return null;
        }
    }

    private async Task<(Guid Manager, HashSet<Guid> Members)> ReadMembershipAsync(GroupInfo group)
    {
        var members = (await userManager.GetUsersByGroupAsync(group.ID, EmployeeStatus.All)).Select(u => u.Id).ToHashSet();

        return (await userManager.GetDepartmentManagerAsync(group.ID), members);
    }

    private async Task PublishIfChangedAsync(GroupInfo group, (Guid Manager, HashSet<Guid> Members)? before)
    {
        if (before is not { } snapshot)
        {
            return;
        }

        // the change is made by now: a failure to read it back drops the webhook rather than failing the request
        (Guid Manager, HashSet<Guid> Members) after;

        try
        {
            after = await ReadMembershipAsync(group);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.ErrorReadGroupMembership(group.ID, e);

            return;
        }

        if (after.Manager == snapshot.Manager && after.Members.SetEquals(snapshot.Members))
        {
            return;
        }

        await webhookManager.PublishAsync(WebhookTrigger.GroupUpdated, group);
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
    /// Returns the portal groups for sharing the room. The `shared` field tells whether a group already has access.
    /// The list also has the system group Everyone. Requires the right to change access to the room. Other callers
    /// get 403. If no room has the ID, the call returns 404. The call is read-only. It sorts the groups by name and
    /// pages them by `count` and `startIndex`, with a total count. An empty `filterValue` returns every group.
    /// Otherwise, the call keeps the groups whose name contains the value, in any case. With `excludeShared`, the
    /// call returns only the groups without access. The call returns 400 if `count` is outside 1 to 100. To search
    /// users and groups together, use `GET api/2.0/accounts/room/{id}/search`.
    /// </remarks>
    /// <summary>Search groups for a room</summary>
    /// <path>api/2.0/group/room/{id}</path>
    /// <collection>list</collection>
    [Tags("Group / Search")]
    [SwaggerResponse(200, "The matching groups, each with its access state for the room", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
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
    /// Returns the portal groups for sharing the folder. The `shared` field tells whether a group already has access.
    /// A group with access to a parent folder also counts. The list also has the system group Everyone. Requires the
    /// right to change access to the folder. Other callers get 403. If no folder has the ID, the call returns 404.
    /// The call is read-only. It sorts the groups by name and pages them by `count` and `startIndex`, with a total
    /// count. An empty `filterValue` returns every group. Otherwise, the call keeps the groups whose name contains
    /// the value, in any case. With `excludeShared`, the call returns only the groups without access. The call
    /// returns 400 if `count` is outside 1 to 100. To search users and groups together, use
    /// `GET api/2.0/accounts/folder/{id}/search`.
    /// </remarks>
    /// <summary>Search groups for a folder</summary>
    /// <path>api/2.0/group/folder/{id}</path>
    /// <collection>list</collection>
    [Tags("Group / Search")]
    [SwaggerResponse(200, "The matching groups, each with its access state for the folder", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
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
    /// Returns the portal groups for sharing the file. The `shared` field tells whether a group already has access.
    /// A group with access to a parent folder also counts. The list also has the system group Everyone. Requires the
    /// right to change access to the file. Other callers get 403. If no file has the ID, the call returns 404. The
    /// call is read-only. It sorts the groups by name and pages them by `count` and `startIndex`, with a total count.
    /// An empty `filterValue` returns every group. Otherwise, the call keeps the groups whose name contains the
    /// value, in any case. With `excludeShared`, the call returns only the groups without access. The call returns
    /// 400 if `count` is outside 1 to 100. To search users and groups together, use
    /// `GET api/2.0/accounts/file/{id}/search`.
    /// </remarks>
    /// <summary>Search groups for a file</summary>
    /// <path>api/2.0/group/file/{id}</path>
    /// <collection>list</collection>
    [Tags("Group / Search")]
    [SwaggerResponse(200, "The matching groups, each with its access state for the file", typeof(IAsyncEnumerable<GroupDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
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
