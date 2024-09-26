// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Core.Helpers;

public static class DocSpaceHelper
{
    public static bool IsRoom(FolderType folderType)
    {
        return folderType is 
            FolderType.CustomRoom or 
            FolderType.EditingRoom or 
            FolderType.FillingFormsRoom or
            FolderType.PublicRoom or 
            FolderType.VirtualDataRoom;
    }

    public static bool IsFormsFillingSystemFolder(FolderType folderType)
    {
        return folderType is
            FolderType.FormFillingFolderDone or
            FolderType.FormFillingFolderInProgress or
            FolderType.InProcessFormFolder or
            FolderType.ReadyFormFolder;
    }

    public static RoomType? MapToRoomType(FolderType folderType)
    {
        return folderType switch
        {
            FolderType.FillingFormsRoom => RoomType.FillingFormsRoom,
            FolderType.EditingRoom => RoomType.EditingRoom,
            FolderType.CustomRoom => RoomType.CustomRoom,
            FolderType.PublicRoom => RoomType.PublicRoom,
            FolderType.VirtualDataRoom => RoomType.VirtualDataRoom,
            _ => null
        };
    }

    public static FolderType MapToFolderType(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.FillingFormsRoom => FolderType.FillingFormsRoom,
            RoomType.EditingRoom => FolderType.EditingRoom,
            RoomType.CustomRoom => FolderType.CustomRoom,
            RoomType.PublicRoom => FolderType.PublicRoom,
            RoomType.VirtualDataRoom => FolderType.VirtualDataRoom,
            _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, null)
        };
    }

    public static FolderType? MapToFolderType(FilterType filterType)
    {
        return filterType switch
        {
            FilterType.FillingFormsRooms => FolderType.FillingFormsRoom,
            FilterType.EditingRooms => FolderType.EditingRoom,
            FilterType.CustomRooms => FolderType.CustomRoom,
            FilterType.PublicRooms => FolderType.PublicRoom,
            FilterType.VirtualDataRooms => FolderType.VirtualDataRoom,
            _ => null
        };
    }

    public static async Task<bool> LocatedInPrivateRoomAsync<T>(File<T> file, IFolderDao<T> folderDao)
    {
        var parents = await folderDao.GetParentFoldersAsync(file.ParentId).ToListAsync();
        var room = parents.Find(f => IsRoom(f.FolderType));

        return room is { SettingsPrivate: true };
    }

    public static async Task<bool> IsWatermarkEnabled<T>(File<T> file, IFolderDao<T> folderDao)
    {
        var (watermarkSettings, _) = await GetWatermarkSettings(file, folderDao);

        return watermarkSettings != null;
    }

    public static async Task<(WatermarkSettings, Folder<T>)> GetWatermarkSettings<T>(File<T> file, IFolderDao<T> folderDao)
    {
        var parents = await folderDao.GetParentFoldersAsync(file.ParentId).ToListAsync();
        var room = parents.Find(f => IsRoom(f.FolderType));

        if (room != null)
        {
            var watermarkSettings = string.IsNullOrEmpty(room.SettingsWatermark) ? null : JsonSerializer.Deserialize<WatermarkSettings>(room.SettingsWatermark);
            return (watermarkSettings, room);
        }
        return (null, null);
    }
}