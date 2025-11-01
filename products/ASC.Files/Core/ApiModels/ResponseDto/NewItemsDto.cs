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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The new item parameters.
/// </summary>
public class NewItemsDto<TItem>
{
    /// <summary>
    /// The date and time when the new item was created.
    /// </summary>
    public required ApiDateTime Date { get; init; }

    /// <summary>
    /// The list of items.
    /// </summary>
    public required IEnumerable<TItem> Items { get; init; }
}

/// <summary>
/// The room new items information.
/// </summary>
public class RoomNewItemsDto
{
    /// <summary>
    /// The room file entry.
    /// </summary>
    public FileEntryBaseDto Room { get; init; }

    /// <summary>
    /// The list of file entry items.
    /// </summary>
    public IEnumerable<FileEntryBaseDto> Items { get; init; }
}

[Scope]
public class RoomNewItemsDtoHelper(FileDtoHelper fileDtoHelper, FolderDtoHelper folderDtoHelper)
{
    private readonly ConcurrentDictionary<string, FileEntryBaseDto> _roomDtoCache = new();

    public async Task<RoomNewItemsDto> GetAsync(FileEntry roomEntry, IEnumerable<FileEntry> entries)
    {
        var roomKey = GetRoomKey(roomEntry);
        if (!_roomDtoCache.TryGetValue(roomKey, out var roomDto))
        {
            roomDto = await GetShortRoomDtoAsync(roomEntry);
            _roomDtoCache.TryAdd(roomKey, roomDto);
        }

        var files = new List<FileEntryBaseDto>();
        foreach (var entry in entries)
        {
            files.Add(await GetFileEntryDtoAsync(entry));
        }
        
        return new RoomNewItemsDto
        {
            Room = roomDto,
            Items = files
        };
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