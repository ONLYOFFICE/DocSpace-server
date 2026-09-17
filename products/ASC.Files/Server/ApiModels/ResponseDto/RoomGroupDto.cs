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
/// A personal collection of rooms: the name and icon it was given, the account that owns it, and the rooms it gathers
/// at the moment it was read.
/// </summary>
public class RoomGroupDto
{
    /// <summary>
    /// The identifier of the group, which addresses it in every other group operation and is kept for as long as the
    /// group exists.
    /// </summary>
    /// <example>42</example>
    public int Id { get; set; }

    /// <summary>
    /// The name its owner gave the group, stored trimmed of surrounding spaces. Names are not unique, so two groups
    /// of the same account can be told apart only by their identifier.
    /// </summary>
    /// <example>Client projects</example>
    public string Name { get; set; }

    /// <summary>
    /// The built-in cover chosen for the group, carrying the cover identifier and its rendering in each available
    /// size. Null when the group has no icon, either because it was never given one or because the icon was cleared
    /// by setting it to an empty value.
    /// </summary>
    /// <example>{"id": "star", "data": {"default": "svg markup", "small": "svg markup"}}</example>
    public MultiSizeLogoCover Icon { get; set; }

    /// <summary>
    /// The account that created the group and the only one able to read, change or delete it; for any other member of
    /// the portal the group does not exist.
    /// </summary>
    /// <example>9a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f9</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// The rooms the group gathers, those stored in the portal first and those on connected third-party accounts
    /// after them. Null when the group was asked for without its members, and an empty array when the group holds no
    /// room the caller can still see. A room moved to the archive is left out until it is taken out of the archive.
    /// </summary>
    /// <example>[{"title": "Client onboarding", "fileEntryType": 1}]</example>
    public List<FileEntryBaseDto> Rooms { get; set; }

    /// <summary>
    /// How many rooms the group shows: the same rooms `rooms` lists, so archived ones are not counted either. It is
    /// filled even when the rooms themselves were not asked for, which makes it the cheap way to tell an empty group
    /// from a populated one.
    /// </summary>
    /// <example>2</example>
    public int TotalRooms { get; set; }
}

[Scope]
public class RoomGroupDtoHelper(FolderDtoHelper folderWrapperHelper, IDaoFactory daoFactory)
{
    public async Task<RoomGroupDto> GetAsync(RoomGroup group, bool includeMembers)
    {
        var result = new RoomGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            UserId = group.UserID
        };

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

        var internalRoomsTask = GetFoldersAsync(fInt).ToListAsync().AsTask();
        var thirdPartyRoomsTask = GetFoldersAsync(fString).ToListAsync().AsTask();

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

        async IAsyncEnumerable<FileEntryBaseDto> GetFoldersAsync<T>(IEnumerable<T> folders)
        {
            var folderDao = daoFactory.GetFolderDao<T>();

            await foreach (var folder in folderDao.GetFoldersAsync(folders))
            {
                // an archived room leaves its groups; the reference is kept so that unarchiving restores the membership
                if (folder.RootFolderType == FolderType.Archive)
                {
                    continue;
                }

                yield return await folderWrapperHelper.GetAsync(folder);
            }
        }
    }
}