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
/// One day of the entries the caller has not opened yet, the groups running from the most recent day backwards.
/// </summary>
public class NewItemsDto<TItem>
{
    /// <summary>
    /// The day the grouped entries were last changed, written with the offset of the portal time zone. The time part
    /// is the moment of the newest entry of the group.
    /// </summary>
    /// <example>2025-01-01T12:30:00.000+03:00</example>
    public required ApiDateTime Date { get; init; }

    /// <summary>
    /// What changed on that day, the most recent first. Folders are left out of it, so an entry here is always a file
    /// or a room that holds them.
    /// </summary>
    public required IEnumerable<TItem> Items { get; init; }
}

/// <summary>The unseen entries of one room inside a day group.</summary>
public class RoomNewItemsDto
{
    /// <summary>
    /// The room the entries were found in, in its short form: only the identifier, the title, the room type and the
    /// logo are filled in.
    /// </summary>
    public FileEntryBaseDto Room { get; init; }

    /// <summary>
    /// The files of that room the caller has not opened yet, the most recently changed first. Reading them here does
    /// not clear the badges; opening the room itself does.
    /// </summary>
    public IEnumerable<FileEntryBaseDto> Items { get; init; }
}

[Scope]
public class RootNewItemsDtoHelper(FileDtoHelper fileDtoHelper, FolderDtoHelper folderDtoHelper)
{
    private readonly ConcurrentDictionary<string, FileEntryBaseDto> _roomDtoCache = new();

    public async Task<T> GetAsync<T>(
        FileEntry rootEntry, 
        IEnumerable<FileEntry> entries, 
        Func<FileEntryBaseDto, IEnumerable<FileEntryBaseDto>, T> selector)
    {
        var roomKey = GetRoomKey(rootEntry);
        if (!_roomDtoCache.TryGetValue(roomKey, out var roomDto))
        {
            roomDto = await GetShortRoomDtoAsync(rootEntry);
            _roomDtoCache.TryAdd(roomKey, roomDto);
        }

        var files = new List<FileEntryBaseDto>();
        foreach (var entry in entries)
        {
            files.Add(await GetFileEntryDtoAsync(entry));
        }

        return selector(roomDto, files);
    }

    private async Task<FileEntryBaseDto> GetFileEntryDtoAsync(FileEntry entry)
    {
        FileEntryBaseDto dto;
        if (entry.FileEntryType == FileEntryType.Folder)
        {
            dto = entry switch
            {
                Folder<int> fol1 => await folderDtoHelper.GetAsync(fol1),
                Folder<string> fol2 => await folderDtoHelper.GetAsync(fol2),
                _ => null
            };
        }
        else
        {
            dto = entry switch
            {
                File<int> file1 => await fileDtoHelper.GetAsync(file1),
                File<string> file2 => await fileDtoHelper.GetAsync(file2),
                _ => null
            };
        }

        return dto;
    }

    private async Task<FileEntryBaseDto> GetShortRoomDtoAsync(FileEntry entry)
    {
        return entry switch
        {
            Folder<int> folderInt => await folderDtoHelper.GetShortAsync(folderInt),
            Folder<string> folderString => await folderDtoHelper.GetShortAsync(folderString),
            _ => null
        };
    }

    private static string GetRoomKey(FileEntry entry)
    {
        var key = entry switch
        {
            Folder<int> folderInt => folderInt.Id.ToString(),
            Folder<string> folderString => folderString.Id,
            _ => null
        };

        return key;
    }
}