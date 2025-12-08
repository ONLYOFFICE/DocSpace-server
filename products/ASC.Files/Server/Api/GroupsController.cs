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

namespace ASC.Files.Api;

[DefaultRoute("group")]
public class GroupsController(
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileStorageService fileStorageService,
    RoomGroupDtoHelper roomGroupDtoHelper,
    AuthContext authContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{

    /// <summary>
    /// Creates a new room group with the specified name, icon, and list of rooms.
    /// </summary>
    /// <short>Add a new room group</short>
    /// <path>api/2.0/files/group</path>
    [HttpPost("")]
    public async Task<RoomGroupDto> AddGroup(RoomGroupRequestDto inDto)
    {
        var (roomIntIds, roomStringIds) = FileOperationsManager.GetIds(inDto.Rooms);

        if (roomIntIds.Count == 0 && roomStringIds.Count == 0)
        {
            throw new InvalidOperationException("At least one room must be provided.");
        }

        var group = await fileStorageService.SaveRoomGroupAsync(new RoomGroup
        {
            Name = inDto.Name,
            UserID = authContext.CurrentAccount.ID,
            Icon = inDto.Icon
        });

        await AddRoomsToGroupAsync(roomIntIds, roomStringIds, group);

        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <summary>
    /// Returns detailed information about a room group.
    /// </summary>
    /// <short>Get room group info</short>
    /// <path>api/2.0/files/group/{id}</path>
    [HttpGet("{id:int}")]
    public async Task<RoomGroupDto> GetGroupInfo(GroupIdRequestDto inDto)
    {
        var group = await GetGroupInfoAsync(inDto.Id).NotFoundIfNull("Group not found");
        return await roomGroupDtoHelper.GetAsync(group, inDto.IncludeMembers);
    }

    /// <summary>
    /// Updates room group properties and adds or removes rooms.
    /// </summary>
    /// <short>Update room group</short>
    /// <path>api/2.0/files/group/{id}</path>
    [HttpPut("{id:int}")]
    public async Task<RoomGroupDto> UpdateGroup(UpdateGroupRequestDto inDto)
    {
        var group = await GetGroupInfoAsync(inDto.Id);

        group.Name = inDto.Update.GroupName ?? group.Name;
        await fileStorageService.SaveRoomGroupAsync(group);

        if (inDto.Update.RoomsToAdd != null)
        {
            var (addInt, addString) = FileOperationsManager.GetIds(inDto.Update.RoomsToAdd);
            await AddRoomsToGroupAsync(addInt, addString, group);
        }

        if (inDto.Update.RoomsToRemove != null)
        {
            var (removeInt, removeString) = FileOperationsManager.GetIds(inDto.Update.RoomsToRemove);
            await RemoveRoomsFromGroupAsync(removeInt, removeString, group);
        }

        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <summary>
    /// Changes the icon of an existing room group.
    /// </summary>
    /// <short>Change group icon</short>
    /// <path>api/2.0/files/group/{id}/icon</path>
    [HttpPost("{id:int}/icon")]
    public async Task<RoomGroupDto> ChangeRoomIcon(IconRequestDto inDto)
    {
        var group = await fileStorageService.ChangeGroupIconAsync(inDto.Id, inDto.Update.Icon);
        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <summary>
    /// Returns a list of all room groups for the current user.
    /// </summary>
    /// <short>List room groups</short>
    /// <path>api/2.0/files/group</path>
    [HttpGet("")]
    public async IAsyncEnumerable<RoomGroupDto> GetGroups(GroupIdRequestDto inDto)
    {
        await foreach (var group in fileStorageService.GetGroupsAsync())
        {
            yield return await roomGroupDtoHelper.GetAsync(group, inDto.IncludeMembers);
        }
    }

    /// <summary>
    /// Deletes the specified room group.
    /// </summary>
    /// <short>Delete group</short>
    /// <path>api/2.0/files/group/{id}</path>
    [HttpDelete("{id:int}")]
    public async Task DeleteGroup(GroupIdRequestDto inDto)
    {
        await fileStorageService.DeleteGroup(inDto.Id);
    }

    private async Task AddRoomsToGroupAsync(List<int> intIds, List<string> stringIds, RoomGroup group)
    {
        var intTasks = intIds.Select(id => fileStorageService.AddRoomToGroupAsync(id, group.Id));
        var stringTasks = stringIds.Select(id => fileStorageService.AddRoomToGroupAsync(id, group.Id));

        await Task.WhenAll(intTasks.Concat(stringTasks));
    }

    private async Task RemoveRoomsFromGroupAsync(List<int> intIds, List<string> stringIds, RoomGroup group)
    {
        var totalRooms = await fileStorageService.GetGroupRoomsCountAsync(group.Id);

        var toRemoveCount = intIds.Count + stringIds.Count;

        if (toRemoveCount >= totalRooms)
        {
            throw new InvalidOperationException("Cannot remove all rooms from the group. At least one room must remain.");
        }

        var intTasks = intIds.Select(id => fileStorageService.RemoveRoomFromGroupAsync(id, group.Id));
        var stringTasks = stringIds.Select(id => fileStorageService.RemoveRoomFromGroupAsync(id, group.Id));

        await Task.WhenAll(intTasks.Concat(stringTasks));
    }

    private async Task<RoomGroup> GetGroupInfoAsync(int id)
    {
        var group = await fileStorageService.GetGroupInfoAsync(id);
        if (group == null)
        {
            throw new ItemNotFoundException(Resource.ErrorGroupNotFound);
        }

        return group;
    }
}