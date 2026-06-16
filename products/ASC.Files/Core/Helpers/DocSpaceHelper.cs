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

namespace ASC.Files.Core.Helpers;

public static class DocSpaceHelper
{
    public static readonly HashSet<FolderType> RoomTypes =
    [
        FolderType.CustomRoom,
        FolderType.EditingRoom,
        FolderType.FillingFormsRoom,
        FolderType.PublicRoom,
        FolderType.VirtualDataRoom,
        FolderType.AiRoom
    ];

    extension(FolderType folderType)
    {
        public bool IsRoom()
        {
            return RoomTypes.Contains(folderType);
        }

        public bool IsAgent()
        {
            return folderType == FolderType.AiRoom;
        }

        public bool IsPublicSystemFolder()
        {
            return folderType == FolderType.AiAgents;
        }
    }

    public static HashSet<FolderType> FormsFillingSystemFolders => [
        FolderType.FormFillingFolderDone,
        FolderType.FormFillingFolderInProgress,
        FolderType.InProcessFormFolder,
        FolderType.ReadyFormFolder
    ];

    public static bool IsFillFormsRoom(FolderType? roomType) =>
        roomType is FolderType.FillingFormsRoom or FolderType.VirtualDataRoom or FolderType.USER;

    public static bool IsFormsFillingSystemFolder(FolderType folderType)
    {
        return FormsFillingSystemFolders.Contains(folderType);
    }

    public static bool IsFormsFillingFolder<T>(FileEntry<T> entry)
    {
        return entry is Folder<T> f && (f.FolderType == FolderType.FillingFormsRoom || IsFormsFillingSystemFolder(f.FolderType));
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
            FolderType.AiRoom => RoomType.AiRoom,
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
            RoomType.AiRoom => FolderType.AiRoom,
            _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, null)
        };
    }

    public static IEnumerable<FolderType> MapToFolderTypes(IEnumerable<FilterType> filterTypes)
    {
        if (filterTypes == null)
        {
            return null;
        }

        var result = new HashSet<FolderType>();

        foreach (var type in filterTypes)
        {
            var folderType = MapToFolderType(type);
            if (folderType.HasValue)
            {
                result.Add(folderType.Value);
            }
        }

        return result;
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
            FilterType.AiRooms => FolderType.AiRoom,
            _ => null
        };
    }

    public static async Task<bool> LocatedInPrivateRoomAsync<T>(FileEntry<T> file, IFolderDao<T> folderDao)
    {
        var room = await GetParentRoom(file, folderDao);

        return LocatedInPrivateRoom(room);
    }

    public static bool LocatedInPrivateRoom<T>(Folder<T> room)
    {
        return room is { SettingsPrivate: true };
    }

    public static async Task<bool> IsWatermarkEnabled<T>(FileEntry<T> file, IFolderDao<T> folderDao)
    {
        if (file.ProviderEntry || file.RootFolderType is not (FolderType.VirtualRooms or FolderType.Archive))
        {
            return false;
        }

        var room = await GetParentRoom(file, folderDao);

        return IsWatermarkEnabled(room);
    }

    public static bool IsWatermarkEnabled<T>(Folder<T> room)
    {
        return room?.SettingsWatermark != null;
    }

    public static async Task<Folder<T>> GetParentRoom<T>(FileEntry<T> file, IFolderDao<T> folderDao)
    {
        return await folderDao.GetParentFoldersAsync(file.ParentId).FirstOrDefaultAsync(f => f.IsRoom);
    }

    public static async ValueTask<bool> IsFormOrCompletedForm<T>(File<T> file, IDaoFactory daoFactory)
    {
        var extension = FileUtility.GetFileExtension(file.Title);
        if (FileUtility.GetFileTypeByExtention(extension) != FileType.Pdf)
        {
            return false;
        }

        if (file.IsForm)
        {
            return true;
        }

        var roles = await daoFactory.GetCacheFileDao<T>().GetFormRoles(file.Id).ToListAsync();
        return roles.Count != 0 && roles.All(r => r.Submitted);
    }
}
