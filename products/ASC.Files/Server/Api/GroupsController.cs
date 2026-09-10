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
    /// Creates a room group, a personal collection that gathers rooms the caller already works with under one name
    /// and icon; it belongs to the account that created it and is never shown to other members of the portal. Pass
    /// the group name, the identifier of one of the built-in covers offered by `GET api/2.0/files/rooms/covers`, and
    /// a list of at least one room - a number for a room stored in the portal, a string for a room on a connected
    /// third-party account. Any role may create its own group, a guest included: what is checked is read access to
    /// each listed room, not the role of the caller. Repeated identifiers are collapsed, and a value that is not a
    /// room identifier at all is rejected as an invalid request. When none of the listed rooms can be read the group
    /// is not created; when only some of them can, the group is created with those rooms and the call is still
    /// reported as failed, so re-read `GET api/2.0/files/group` before retrying. A room may sit in several groups,
    /// and two groups of the same account may carry the same name. The answer is the stored group with its rooms.
    /// </remarks>
    /// <summary>
    /// Add a new room group
    /// </summary>
    /// <path>api/2.0/files/group</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The created room group with the rooms that were linked to it", typeof(RoomGroupDto))]
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
    /// Returns one room group of the calling account together with the rooms it gathers. Groups are personal: an
    /// identifier that belongs to another member is answered the same way as one that was never created or has
    /// already been deleted, and a portal administrator is no exception to that rule. Take the identifier from
    /// `GET api/2.0/files/group`, which lists the groups the caller owns. Set `includeMembers` to false to get the
    /// group without the `rooms` array, which is the cheaper form when only the name, the icon and the number of
    /// rooms are needed; `totalRooms` is filled either way. A room moved to the archive is left out of both `rooms`
    /// and `totalRooms` while its membership survives, so taking the room out of the archive brings it back into the
    /// group. Rooms stored in the portal are listed before rooms on connected third-party accounts. The call is
    /// read-only and changes nothing about the group or the rooms it refers to.
    /// </remarks>
    /// <summary>
    /// Get room group info
    /// </summary>
    /// <path>api/2.0/files/group/{id}</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room group with the rooms it gathers", typeof(RoomGroupDto))]
    [HttpGet("{id:int}")]
    public async Task<RoomGroupDto> GetRoomGroupInfo(RoomGroupIdRequestDto inDto)
    {
        var group = await fileStorageService.GetGroupInfoAsync(inDto.Id);
        return await roomGroupDtoHelper.GetAsync(group, inDto.IncludeMembers);
    }

    /// <remarks>
    /// Applies changes to one of the caller's own room groups: a new name, rooms to attach, rooms to detach, or any
    /// combination of the three in a single call. A body that carries none of the three (`{}`) is accepted and
    /// changes nothing, while a body that names them and leaves every one of them empty asks for an update that
    /// cannot be performed and is rejected as an invalid request. `roomsToAdd` is resolved the way creation resolves
    /// its list: every identifier has to name a room the caller can read, repeats and rooms already in the group are
    /// collapsed, and when only part of the list resolves the rest is still attached and the call is reported as
    /// failed. `roomsToRemove` works the other way round - a room already in the group is always detached, even when
    /// the caller has since lost access to it, whereas an identifier that is not in the group is resolved first and
    /// refused when it names nothing. The steps are applied in order and are not rolled back when a later one fails.
    /// A group of another account is answered as missing. The answer is the group as stored after the call.
    /// </remarks>
    /// <summary>
    /// Update room group
    /// </summary>
    /// <path>api/2.0/files/group/{id}</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room group as stored after the change", typeof(RoomGroupDto))]
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
    /// Replaces the icon of one of the caller's own room groups and returns the whole group, its name and its rooms
    /// left as they were. Send the identifier of one of the built-in covers offered by
    /// `GET api/2.0/files/rooms/covers`; an empty string strips the icon, after which the group comes back with an
    /// empty `icon`, and any other value - including a word that merely reads like one, such as `none` - is rejected
    /// as an invalid request. An uploaded image cannot be used here, unlike the logo of a room. Leaving `icon` out of
    /// the body or sending it as null is accepted and changes nothing, whereas a request that carries no body at all,
    /// or a body that is not JSON, is refused. Setting the icon the group already has is accepted as well, so
    /// retrying the call is safe. Any role may re-icon its own group, and a group belonging to another account is
    /// answered as missing rather than refused, exactly as reading it would be.
    /// </remarks>
    /// <summary>
    /// Change room group icon
    /// </summary>
    /// <path>api/2.0/files/group/{id}/icon</path>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room group with the new icon", typeof(RoomGroupDto))]
    [HttpPost("{id:int}/icon")]
    public async Task<RoomGroupDto> ChangeRoomGroupIcon(RoomGroupIconRequestDto inDto)
    {
        var group = await fileStorageService.ChangeGroupIconAsync(inDto.Id, inDto.Update.Icon);
        return await roomGroupDtoHelper.GetAsync(group, true);
    }

    /// <remarks>
    /// Returns every room group of the calling account, each with the rooms it gathers. Only groups the caller
    /// created are listed: groups of other members never appear here, and an account that has never made one gets an
    /// empty array back. Set `includeMembers` to false to leave the `rooms` array out of every entry and keep the
    /// name, the icon and `totalRooms` alone, which is the cheaper form when the list is only being shown as a menu.
    /// Archived rooms are skipped in both the `rooms` array and the `totalRooms` count, and reappear once the room is
    /// taken out of the archive. The listing is neither paged nor filtered - it always carries the whole set - and
    /// the order of the entries is not contractual, so sort them on the client when the order matters. The call is
    /// read-only. Use `GET api/2.0/files/group/{id}` when the identifier of a single group is already known, and
    /// `POST api/2.0/files/group` to add one.
    /// </remarks>
    /// <summary>
    /// List room groups
    /// </summary>
    /// <path>api/2.0/files/group</path>
    /// <collection>list</collection>
    [Tags("Rooms / Groups")]
    [SwaggerResponse(200, "The room groups of the calling account", typeof(IAsyncEnumerable<RoomGroupDto>))]
    [HttpGet("")]
    public async IAsyncEnumerable<RoomGroupDto> GetRoomGroups(RoomGroupsRequestDto inDto)
    {
        await foreach (var group in fileStorageService.GetGroupsAsync())
        {
            yield return await roomGroupDtoHelper.GetAsync(group, inDto.IncludeMembers);
        }
    }

    /// <remarks>
    /// Deletes one of the caller's own room groups. Only the collection goes away: the rooms it gathered, their
    /// content and the shares on them are left exactly as they were, and a room that was in no other group simply
    /// stops being grouped. Deleting a group of another account is refused, and an identifier that names nothing -
    /// because it never existed, or because the group has already been deleted - is answered as missing, so repeating
    /// the call after a successful delete does not report success a second time. The operation is destructive and
    /// cannot be undone: there is no trash for groups, and rebuilding one means calling `POST api/2.0/files/group`
    /// again with the same name, icon and rooms, which gives it a new identifier. Nothing is returned in the body.
    /// The `includeMembers` parameter is accepted here because the route shares its contract with
    /// `GET api/2.0/files/group/{id}`, and has no effect on what is deleted. Read the group first when the rooms it
    /// gathers still have to be recorded somewhere.
    /// </remarks>
    /// <summary>
    /// Delete a room group
    /// </summary>
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
