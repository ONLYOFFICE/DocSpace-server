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
    /// Creates a new room group with the specified name, icon, and list of rooms.
    /// </remarks>
    /// <summary>Add a new room group</summary>
    /// <path>api/2.0/files/group</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The newly created room group", typeof(RoomGroupDto))]
    [HttpPost("")]
    public async Task<RoomGroupDto> AddRoomGroup(RoomGroupRequestDto inDto)
    {
        var name = ValidateGroupName(inDto.Name);
        var (roomIntIds, roomStringIds) = ParseRoomIds(inDto.Rooms);

        if (roomIntIds.Count == 0 && roomStringIds.Count == 0)
        {
            throw new InvalidOperationException("At least one room must be provided.");
        }

        await RoomLogoManager.ValidateRoomCover(inDto.Icon);

        // resolved before the group row is written, so a request that resolves nothing leaves nothing behind
        var (intIds, stringIds, anyRejected) = await fileStorageService.ResolveGroupRoomsAsync(roomIntIds, roomStringIds);

        var group = await fileStorageService.SaveRoomGroupAsync(new RoomGroup
        {
            Name = name,
            UserID = authContext.CurrentAccount.ID,
            Icon = inDto.Icon
        });

        await AddRoomsToGroupAsync(intIds, stringIds, group);

        if (anyRejected)
        {
            throw new InvalidOperationException("Some of the rooms could not be added to the group.");
        }

        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <remarks>
    /// Returns detailed information about a room group.
    /// </remarks>
    /// <summary>Get room group info</summary>
    /// <path>api/2.0/files/group/{id}</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room group with the detailed information", typeof(RoomGroupDto))]
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
    [SwaggerResponse(200, "The updated room group", typeof(RoomGroupDto))]
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
            var (intIds, stringIds, anyRejected) = await fileStorageService.ResolveGroupRoomsAsync(addInt, addString);

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
    /// Changes the icon of an existing room group.
    /// </remarks>
    /// <summary>Change group icon</summary>
    /// <path>api/2.0/files/group/{id}/icon</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room group with the updated icon", typeof(RoomGroupDto))]
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
    /// <collection>list</collection>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "List of room groups", typeof(IAsyncEnumerable<RoomGroupDto>))]
    [HttpGet("")]
    public async IAsyncEnumerable<RoomGroupDto> GetRoomGroups(RoomGroupsRequestDto inDto)
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
