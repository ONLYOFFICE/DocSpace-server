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

namespace ASC.Files.Api;

[ApiEndpoint(Template = "group")]
public class GroupsController(
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileStorageService fileStorageService,
    RoomGroupDtoHelper roomGroupDtoHelper,
    AuthContext authContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{

    /// <remarks>
    /// Creates a room group, a personal collection of rooms with a name and an icon. Other members of the portal
    /// never see the group. Any signed-in user can call it, including a guest. The call checks read access to each
    /// listed room. It changes data. Do not repeat it. Each call creates another group. The call returns 400 for a
    /// blank or too long name, or an unknown icon. It also returns 400 if `searchArea` is not `Active` or `Forms`.
    /// It also returns 400 for a malformed room identifier or a disconnected storage account. An empty
    /// `rooms` list returns 403. If no listed room can be added, the call creates no group. It returns 403 if a
    /// listed identifier exists but is not a room the caller can read. If all existing rooms are readable but in
    /// the other section, it returns 400. If no listed room exists, it returns 404. A string identifier that names
    /// no third-party storage also returns 404. If only some rooms can be added, the call creates the group with
    /// them but returns 403. So read `GET api/2.0/files/group` before you retry.
    /// </remarks>
    /// <summary>
    /// Add a new room group
    /// </summary>
    /// <path>api/2.0/files/group</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The created room group with the rooms that were linked to it", typeof(RoomGroupDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `name`, `icon` or `rooms`, the name is blank or longer than 128 characters, the icon is not a built-in cover, `searchArea` names a section other than `Active` and `Forms`, a room identifier is neither a positive number nor a non-numeric string, a third-party room identifier refers to a storage account that is not connected, or none of the listed rooms can be added and the existing ones all belong to the other section than `searchArea`")]
    [SwaggerResponse(403, "The `rooms` list is empty, the listed rooms exist but none of them is a room the caller can read, or only some of them could be added - the group is then created with those")]
    [SwaggerResponse(404, "None of the listed rooms exists, or a string room identifier does not name a third-party storage")]
    [SwaggerResponse(500, "A third-party room identifier carries a storage account number beyond the 32-bit range")]
    [HttpPost("")]
    public async Task<RoomGroupDto> AddRoomGroup(RoomGroupRequestDto inDto)
    {
        var name = ValidateGroupName(inDto.Name);
        var searchArea = ValidateSearchArea(inDto.SearchArea);
        var (roomIntIds, roomStringIds) = ParseRoomIds(inDto.Rooms);

        if (roomIntIds.Count == 0 && roomStringIds.Count == 0)
        {
            throw new InvalidOperationException("At least one room must be provided.");
        }

        await RoomLogoManager.ValidateRoomCover(inDto.Icon);

        // resolved before the group row is written, so a request that resolves nothing leaves nothing behind
        var (intIds, stringIds, anyRejected) = await fileStorageService.ResolveGroupRoomsAsync(roomIntIds, roomStringIds, searchArea);

        var group = await fileStorageService.SaveRoomGroupAsync(new RoomGroup
        {
            Name = name,
            UserID = authContext.CurrentAccount.ID,
            Icon = inDto.Icon,
            FolderType = searchArea.ToFolderType()
        });

        await AddRoomsToGroupAsync(intIds, stringIds, group);

        if (anyRejected)
        {
            throw new InvalidOperationException("Some of the rooms could not be added to the group.");
        }

        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <remarks>
    /// Returns one room group of the caller with its rooms. Any signed-in user can call it, including a guest. A
    /// missing group or a group of another account returns 404, even for a DocSpace admin. The call is read-only. With `includeMembers` set to false, `rooms` is null, but `totalRooms` is still filled. The
    /// `rooms` list skips archived rooms, rooms of the other section, and rooms the caller can no longer read. The
    /// `totalRooms` count uses the same rule. A skipped room returns after it leaves the archive or the caller gets
    /// access again. Rooms stored in the portal come before rooms of third-party storages.
    /// </remarks>
    /// <summary>
    /// Get room group info
    /// </summary>
    /// <path>api/2.0/files/group/{id}</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room group with the rooms it gathers", typeof(RoomGroupDto))]
    [SwaggerResponse(404, "The group does not exist or belongs to another account")]
    [HttpGet("{id:int}")]
    public async Task<RoomGroupDto> GetRoomGroupInfo(RoomGroupIdRequestDto inDto)
    {
        var group = await fileStorageService.GetGroupInfoAsync(inDto.Id);
        return await roomGroupDtoHelper.GetAsync(group, inDto.IncludeMembers);
    }

    /// <remarks>
    /// Updates a room group of the caller. It can rename the group, add rooms, and remove rooms in one call. Any
    /// signed-in user can call it, including a guest. A missing group or a group of another account returns 404, even
    /// for a DocSpace admin. The call changes data. You can repeat it with the same body. It
    /// applies the name first, then `roomsToAdd`, then `roomsToRemove`. A later failure does not undo the earlier
    /// steps. An empty body changes nothing. If every field the body sends is null, the call returns 400. The call
    /// always removes a room that is in the group, even if the caller lost access to it. If no room of `roomsToAdd`
    /// can be added, the call fails. It returns 403 if a listed identifier exists but is not a
    /// room the caller can read. If all existing rooms are readable but in the other section, it returns 400. If no
    /// listed room exists, it returns 404. These rules also apply to the identifiers of `roomsToRemove` that are not
    /// in the group, without the section check. If only some rooms can be applied, the call applies them but
    /// returns 403. A string identifier that names no third-party storage also returns 404. The call returns 400 for
    /// a blank or too long name, a malformed room identifier, or a disconnected storage account.
    /// </remarks>
    /// <summary>
    /// Update room group
    /// </summary>
    /// <path>api/2.0/files/group/{id}</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room group as stored after the change", typeof(RoomGroupDto))]
    [SwaggerResponse(400, "The request body cannot be read or names its members with every one of them null, the new name is blank or longer than 128 characters, a room identifier is neither a positive number nor a non-numeric string, a third-party room identifier refers to a storage account that is not connected, or none of the rooms of `roomsToAdd` can be added and the existing ones all belong to the other section than the group")]
    [SwaggerResponse(403, "The rooms of `roomsToAdd`, or the rooms of `roomsToRemove` that are not in the group, exist but none of them is a room the caller can read, or only some of the rooms could be applied - the earlier steps stay applied")]
    [SwaggerResponse(404, "The group does not exist or belongs to another account, none of the rooms of `roomsToAdd` or of the rooms of `roomsToRemove` that are not in the group exists, or a string room identifier does not name a third-party storage")]
    [SwaggerResponse(500, "A third-party room identifier carries a storage account number beyond the 32-bit range")]
    [HttpPut("{id:int}")]
    public async Task<RoomGroupDto> UpdateRoomGroup(UpdateRoomGroupRequestDto inDto)
    {
        var update = inDto.UpdateRoom;

        if (update.HasPayload && update.GroupName == null && update.RoomsToAdd == null && update.RoomsToRemove == null)
        {
            throw new ArgumentException("The request does not contain anything to update.");
        }

        var group = await fileStorageService.GetGroupInfoAsync(inDto.Id);

        if (update.GroupName != null)
        {
            group.Name = ValidateGroupName(update.GroupName);
            await fileStorageService.SaveRoomGroupAsync(group);
        }

        var rejected = false;

        if (update.RoomsToAdd is { Count: > 0 })
        {
            var (addInt, addString) = ParseRoomIds(update.RoomsToAdd);
            var (intIds, stringIds, anyRejected) = await fileStorageService.ResolveGroupRoomsAsync(addInt, addString, group.FolderType.ToSearchArea());

            await AddRoomsToGroupAsync(intIds, stringIds, group);
            rejected |= anyRejected;
        }

        if (update.RoomsToRemove is { Count: > 0 })
        {
            var (removeInt, removeString) = ParseRoomIds(update.RoomsToRemove);
            var (intIds, stringIds, anyRejected) = await fileStorageService.ResolveGroupRoomsForRemovalAsync(group.Id, removeInt, removeString);

            await RemoveRoomsFromGroupAsync(intIds, stringIds, group);
            rejected |= anyRejected;
        }

        if (rejected)
        {
            throw new InvalidOperationException("Some of the rooms could not be applied to the group.");
        }

        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <remarks>
    /// Changes the icon of a room group of the caller. Any signed-in user can call it, including a guest. A missing
    /// group or a group of another account returns 404, even for a DocSpace admin. The call changes data. You can repeat it with the same body. An empty `icon` removes the icon, and a null or missing
    /// `icon` changes nothing. The call returns 400 if `icon` is neither a built-in cover nor an empty string. It
    /// also returns 400 if the body is missing or is not JSON.
    /// </remarks>
    /// <summary>
    /// Change room group icon
    /// </summary>
    /// <path>api/2.0/files/group/{id}/icon</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room group with the new icon", typeof(RoomGroupDto))]
    [SwaggerResponse(400, "The request body cannot be read, or the icon is neither a built-in cover nor an empty string")]
    [SwaggerResponse(404, "The group does not exist or belongs to another account")]
    [HttpPost("{id:int}/icon")]
    public async Task<RoomGroupDto> ChangeRoomGroupIcon(RoomGroupIconRequestDto inDto)
    {
        var group = await fileStorageService.ChangeGroupIconAsync(inDto.Id, inDto.Update.Icon);
        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <remarks>
    /// Returns the room groups of the caller in the Rooms or Forms section, each with its rooms. Groups of other
    /// members never appear, even for a DocSpace admin. Any signed-in user can call it, including a guest. The call
    /// is read-only. The list has no paging or total, and its order is not fixed. An empty array means the caller
    /// has no groups in this section. With `includeMembers` set to false, `rooms` is null in every entry, but
    /// `totalRooms` is still filled. Each `rooms` list skips archived rooms, rooms of the other section, and rooms
    /// the caller can no longer read. The `totalRooms` count uses the same rule. A skipped room returns after it
    /// leaves the archive or the caller gets access again. If `searchArea` is not `Active` or `Forms`, the call
    /// returns 400. To get one group, use `GET api/2.0/files/group/{id}`.
    /// </remarks>
    /// <summary>
    /// List room groups
    /// </summary>
    /// <path>api/2.0/files/group</path>
    /// <collection>list</collection>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room groups of the calling account", typeof(IAsyncEnumerable<RoomGroupDto>))]
    [SwaggerResponse(400, "The `searchArea` names a section other than `Active` and `Forms`")]
    [HttpGet("")]
    public async IAsyncEnumerable<RoomGroupDto> GetRoomGroups(RoomGroupsRequestDto inDto)
    {
        var searchArea = ValidateSearchArea(inDto.SearchArea);

        await foreach (var group in fileStorageService.GetGroupsAsync(searchArea))
        {
            yield return await roomGroupDtoHelper.GetAsync(group, inDto.IncludeMembers);
        }
    }

    /// <remarks>
    /// Deletes a room group of the caller. The rooms of the group, their content, and their shares stay unchanged.
    /// Any signed-in user can call it, including a guest. If the group belongs to another account, the call returns
    /// 403, even for a DocSpace admin. The call cannot be undone, and a repeated call returns 404. If the group does
    /// not exist, the call returns 404. The response body is empty. The call ignores `includeMembers`. To keep the
    /// list of rooms, read the group first with `GET api/2.0/files/group/{id}`.
    /// </remarks>
    /// <summary>
    /// Delete a room group
    /// </summary>
    /// <path>api/2.0/files/group/{id}</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room group no longer exists; the body is empty and the rooms it gathered are left as they were")]
    [SwaggerResponse(403, "The group belongs to another account")]
    [SwaggerResponse(404, "The group does not exist")]
    [HttpDelete("{id:int}")]
    public async Task DeleteRoomGroup(RoomGroupIdRequestDto inDto)
    {
        await fileStorageService.DeleteGroup(inDto.Id);
    }

    private async Task AddRoomsToGroupAsync(List<int> intIds, List<string> stringIds, RoomGroup group)
    {
        // sequential: the same room may legitimately appear twice in one request, and parallel
        // inserts of the same reference race each other into a duplicate-key failure
        foreach (var id in intIds)
        {
            await fileStorageService.AddRoomToGroupAsync(id, group.Id);
        }

        foreach (var id in stringIds)
        {
            await fileStorageService.AddRoomToGroupAsync(id, group.Id);
        }
    }

    private async Task RemoveRoomsFromGroupAsync(List<int> intIds, List<string> stringIds, RoomGroup group)
    {
        foreach (var id in intIds)
        {
            await fileStorageService.RemoveRoomFromGroupAsync(id, group.Id);
        }

        foreach (var id in stringIds)
        {
            await fileStorageService.RemoveRoomFromGroupAsync(id, group.Id);
        }
    }

    /// <summary>
    /// A group lives either in the Rooms section or in the Forms one, so only those two areas are
    /// accepted; any other value of the shared <see cref="SearchArea"/> dictionary is a bad request.
    /// A missing value means Active, which is what a client that predates the split sends.
    /// </summary>
    private static SearchArea ValidateSearchArea(SearchArea? searchArea)
    {
        return searchArea switch
        {
            null or SearchArea.Active => SearchArea.Active,
            SearchArea.Forms => SearchArea.Forms,
            _ => throw new ArgumentException($"'{searchArea}' is not a valid room group area.", nameof(searchArea))
        };
    }

    private static string ValidateGroupName(string name)
    {
        var trimmed = name?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException("The group name must not be empty.", nameof(name));
        }

        return trimmed;
    }

    /// <summary>
    /// Turns the raw <c>rooms</c> payload into internal and third-party room ids, rejecting
    /// anything that is not a room id at all: a null element, a fractional or non-positive number,
    /// a number sent as a string, or a nested value. Duplicates are collapsed, so repeating a room
    /// in one request is a no-op rather than a conflict.
    /// </summary>
    private static (List<int> IntIds, List<string> StringIds) ParseRoomIds(List<JsonElement> rooms)
    {
        var intIds = new List<int>();
        var stringIds = new List<string>();

        foreach (var room in rooms ?? [])
        {
            switch (room.ValueKind)
            {
                case JsonValueKind.Number when room.TryGetInt32(out var id) && id > 0:
                    if (!intIds.Contains(id))
                    {
                        intIds.Add(id);
                    }

                    break;
                // a third-party room id is never numeric — a numeric string is a wrong-typed element
                case JsonValueKind.String when room.GetString() is { Length: > 0 } value && !int.TryParse(value, out _):
                    if (!stringIds.Contains(value))
                    {
                        stringIds.Add(value);
                    }

                    break;
                default:
                    throw new ArgumentException($"'{room}' is not a valid room id.", nameof(rooms));
            }
        }

        return (intIds, stringIds);
    }
}
