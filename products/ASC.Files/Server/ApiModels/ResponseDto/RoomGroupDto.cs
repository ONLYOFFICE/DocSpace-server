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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The room security parameters.
/// </summary>
public class RoomGroupDto
{
    /// <summary>
    /// The group ID.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// Group name
    /// </summary>
    /// <example>My Group</example>
    public string Name { get; set; }

    /// <summary>
    /// Group icon
    /// </summary>
    /// <example>{"id":"1","data":{"url":"/temp/logo.png","width":100,"height":100}}</example>
    public MultiSizeLogoCover Icon { get; set; }

    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// The section the group belongs to: Active for Rooms and Forms for Forms.
    /// </summary>
    /// <example>Active</example>
    public SearchArea SearchArea { get; set; }

    /// <summary>
    /// The list of rooms in the group. 
    /// </summary>
    /// <example>[{"id":1,"title":"Room 1"},{"id":2,"title":"Room 2"}]</example>
    public List<FileEntryBaseDto> Rooms { get; set; }

    /// <summary>
    /// Total number of rooms in the group.
    /// </summary>
    /// <example>2</example>
    public int TotalRooms { get; set; }
}

[Scope]
public class RoomGroupDtoHelper(FolderDtoHelper folderWrapperHelper, IDaoFactory daoFactory)
{
    /// <summary>
    /// Builds the response for one group. <paramref name="searchArea"/> is the section the group is
    /// being looked at from: its rooms are then limited to that section, and a group that has no room
    /// there at all is not part of it - <c>null</c> is returned so the listing can skip it. When no
    /// section is given, the group's own one is derived from its rooms.
    /// </summary>
    public async Task<RoomGroupDto> GetAsync(RoomGroup group, bool includeMembers, SearchArea? searchArea = null)
    {
        var roomGroupDao = daoFactory.GetRoomGroupDao<int>();
        var roomGroupRefs = await roomGroupDao.GetRoomsByGroupAsync(group.Id).ToListAsync();

        var fInt = new List<int>();
        var fString = new List<string>();

        foreach (var r in roomGroupRefs)
        {
            if (r.InternalRoomId.HasValue) 
            {
                fInt.Add(r.InternalRoomId.Value);
            } 
            else 
            {
                fString.Add(r.ThirdpartyRoomId);
            }
        }

        var internalFoldersTask = GetFoldersAsync(fInt).ToListAsync().AsTask();
        var thirdPartyFoldersTask = GetFoldersAsync(fString).ToListAsync().AsTask();

        await Task.WhenAll(internalFoldersTask, thirdPartyFoldersTask);

        var internalFolders = internalFoldersTask.Result;
        var thirdPartyFolders = thirdPartyFoldersTask.Result;

        List<FolderType> roomTypes =
        [
            .. internalFolders.Select(r => r.FolderType),
            .. thirdPartyFolders.Select(r => r.FolderType)
        ];

        if (searchArea.HasValue && !RoomGroupArea.BelongsTo(roomTypes, searchArea.Value))
        {
            return null;
        }

        var area = searchArea ?? RoomGroupArea.Derive(roomTypes);

        var result = new RoomGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            UserId = group.UserID,
            SearchArea = area
        };

        var internalRoomsTask = MapAsync(internalFolders, area).ToListAsync().AsTask();
        var thirdPartyRoomsTask = MapAsync(thirdPartyFolders, area).ToListAsync().AsTask();

        await Task.WhenAll(internalRoomsTask, thirdPartyRoomsTask);

        var internalRooms = internalRoomsTask.Result;
        var thirdPartyRooms = thirdPartyRoomsTask.Result;

        result.TotalRooms =
            internalRooms.Count +
            thirdPartyRooms.Count;

        MultiSizeLogoCover cover = null;
        if (!string.IsNullOrEmpty(group.Icon) &&
            (await RoomLogoManager.GetCoversBySizeAsync()).TryGetValue(group.Icon, out var fromDict))
        {
            cover = new MultiSizeLogoCover
            {
                Id = group.Icon,
                Data = fromDict
            };
        }
        result.Icon = cover;

        if (!includeMembers)
        {
            return result;
        }

        result.Rooms = [];
        result.Rooms.AddRange(internalRooms);
        result.Rooms.AddRange(thirdPartyRooms);

        return result;

        IAsyncEnumerable<Folder<T>> GetFoldersAsync<T>(IEnumerable<T> folders)
        {
            return daoFactory.GetFolderDao<T>().GetFoldersAsync(folders);
        }

        async IAsyncEnumerable<FileEntryBaseDto> MapAsync<T>(IEnumerable<Folder<T>> folders, SearchArea roomArea)
        {
            foreach (var folder in folders)
            {
                // an archived room leaves its groups; the reference is kept so that unarchiving restores the membership
                if (folder.RootFolderType == FolderType.Archive)
                {
                    continue;
                }

                // a room of the other section is invisible in this group's listing, so it must not be
                // counted either - otherwise the group shows a room count it never lists
                if (!roomArea.MatchesRoomType(folder.FolderType))
                {
                    continue;
                }

                yield return await folderWrapperHelper.GetAsync(folder);
            }
        }
    }
}
