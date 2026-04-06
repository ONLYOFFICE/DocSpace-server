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

    /// <remarks>
    /// Creates a new room group with the specified name, icon, and list of rooms.
    /// </remarks>
    /// <summary>Add a new room group</summary>
    /// <path>api/2.0/files/group</path>
    [Tags("Rooms / Groups")]
    [HttpPost("")]
    public async Task<RoomGroupDto> AddRoomGroup(RoomGroupRequestDto inDto)
    {
        var (roomIntIds, roomStringIds) = FileOperationsManager.GetIds(inDto.Rooms);

        if (roomIntIds.Count == 0 && roomStringIds.Count == 0)
        {
            throw new InvalidOperationException("At least one room must be provided.");
        }

        await RoomLogoManager.ValidateRoomCover(inDto.Icon);

        var group = await fileStorageService.SaveRoomGroupAsync(new RoomGroup
        {
            Name = inDto.Name,
            UserID = authContext.CurrentAccount.ID,
            Icon = inDto.Icon
        });

        await AddRoomsToGroupAsync(roomIntIds, roomStringIds, group);

        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <remarks>
    /// Returns detailed information about a room group.
    /// </remarks>
    /// <summary>Get room group info</summary>
    /// <path>api/2.0/files/group/{id}</path>
    [Tags("Rooms / Groups")]
    [HttpGet("{id:int}")]
    public async Task<RoomGroupDto> GetRoomGroupInfo(RoomGroupIdRequestDto inDto)
    {
        var group = await fileStorageService.GetGroupInfoAsync(inDto.Id);
        return await roomGroupDtoHelper.GetAsync(group, inDto.IncludeMembers);
    }

    /// <remarks>
    /// Updates room group properties and adds or removes rooms.
    /// </remarks>
    /// <summary>Update room group</summary>
    /// <path>api/2.0/files/group/{id}</path>
    [Tags("Rooms / Groups")]
    [HttpPut("{id:int}")]
    public async Task<RoomGroupDto> UpdateRoomGroup(UpdateRoomGroupRequestDto inDto)
    {
        var group = await fileStorageService.GetGroupInfoAsync(inDto.Id);
        group.Name = inDto.UpdateRoom.GroupName ?? group.Name;

        await fileStorageService.SaveRoomGroupAsync(group);

        if (inDto.UpdateRoom.RoomsToAdd != null)
        {
            var (addInt, addString) = FileOperationsManager.GetIds(inDto.UpdateRoom.RoomsToAdd);
            await AddRoomsToGroupAsync(addInt, addString, group);
        }

        if (inDto.UpdateRoom.RoomsToRemove != null)
        {
            var (removeInt, removeString) = FileOperationsManager.GetIds(inDto.UpdateRoom.RoomsToRemove);
            await RemoveRoomsFromGroupAsync(removeInt, removeString, group);
        }

        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <remarks>
    /// Changes the icon of an existing room group.
    /// </remarks>
    /// <summary>Change group icon</summary>
    /// <path>api/2.0/files/group/{id}/icon</path>
    [Tags("Rooms / Groups")]
    [HttpPost("{id:int}/icon")]
    public async Task<RoomGroupDto> ChangeRoomGroupIcon(RoomGroupIconRequestDto inDto)
    {
        var group = await fileStorageService.ChangeGroupIconAsync(inDto.Id, inDto.Update.Icon);
        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <remarks>
    /// Returns a list of all room groups for the current user.
    /// </remarks>
    /// <summary>List room groups</summary>
    /// <path>api/2.0/files/group</path>
    [Tags("Rooms / Groups")]
    [HttpGet("")]
    public async IAsyncEnumerable<RoomGroupDto> GetRoomGroups(RoomGroupIdRequestDto inDto)
    {
        await foreach (var group in fileStorageService.GetGroupsAsync())
        {
            yield return await roomGroupDtoHelper.GetAsync(group, inDto.IncludeMembers);
        }
    }

    /// <remarks>
    /// Deletes the specified room group.
    /// </remarks>
    /// <summary>Delete group</summary>
    /// <path>api/2.0/files/group/{id}</path>
    [Tags("Rooms / Groups")]
    [HttpDelete("{id:int}")]
    public async Task DeleteRoomGroup(RoomGroupIdRequestDto inDto)
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
        var intTasks = intIds.Select(id => fileStorageService.RemoveRoomFromGroupAsync(id, group.Id));
        var stringTasks = stringIds.Select(id => fileStorageService.RemoveRoomFromGroupAsync(id, group.Id));

        await Task.WhenAll(intTasks.Concat(stringTasks));
    }
}
