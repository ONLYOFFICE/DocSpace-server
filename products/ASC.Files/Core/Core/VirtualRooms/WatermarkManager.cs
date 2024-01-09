// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Files.Core.VirtualRooms;

[Flags]
public enum WatermarkAdditions
{
    UserName = 1,
    UserEmail = 2,
    UserIpAdress = 4,
    CurrentDate = 8,
    RoomName = 16
}
public class WatermarkSettings
{
    public bool Enabled { get; set; }      
    public string Text { get; set; }
    public WatermarkAdditions Additions { get; set; }
    public int Rotate { get; set; }
    public double ImageWidth { get; set; }
    public double ImageHeight { get; set; }
    public string ImageUrl { get; set; }
    public int ImageScale { get; set; }
}

[Scope]
public class WatermarkManager
{
    private readonly IDaoFactory _daoFactory;
    private readonly FileSecurity _fileSecurity;
    private readonly RoomLogoManager _roomLogoManager;
    public WatermarkManager(
        IDaoFactory daoFactory,
        FileSecurity fileSecurity,
        RoomLogoManager roomLogoManager)
    {
        _daoFactory = daoFactory;
        _fileSecurity = fileSecurity;
        _roomLogoManager = roomLogoManager;
    }

    public async Task<WatermarkSettings> SetWatermarkAsync<T>(T roomId, WatermarkRequestDto watermarkRequestDto)
    {
        var folderDao = _daoFactory.GetFolderDao<T>();

        var room = await folderDao.GetFolderAsync(roomId);

        if (room == null || !DocSpaceHelper.IsRoom(room.FolderType))
        {
            throw new ItemNotFoundException();
        }

        if (room.RootFolderType == FolderType.Archive || !await _fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        var watermarkSettings = new WatermarkSettings()
        {
            Enabled = watermarkRequestDto.Enabled,
            Text = watermarkRequestDto.Text,
            Additions = watermarkRequestDto.Additions,
            Rotate = watermarkRequestDto.Rotate
        };

        if(!string.IsNullOrEmpty(watermarkRequestDto.ImageUrl))
        {
            watermarkSettings.ImageScale = watermarkRequestDto.ImageScale;
            watermarkSettings.ImageHeight = watermarkRequestDto.ImageHeight;
            watermarkSettings.ImageWidth = watermarkRequestDto.ImageWidth;
            watermarkSettings.ImageUrl = await _roomLogoManager.CreateWatermarkImageAsync(room, watermarkRequestDto.ImageUrl);
        }

        await folderDao.SetWatermarkSettings(watermarkSettings, room);

        return watermarkSettings;
    }

    public async Task<WatermarkSettings> GetWatermarkAsync<T>(Folder<T> room)
    {
        if (room == null || !DocSpaceHelper.IsRoom(room.FolderType))
        {
            throw new ItemNotFoundException();
        }

        if (room.RootFolderType == FolderType.Archive || !await _fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        var folderDao = _daoFactory.GetFolderDao<T>();

        var watermarkSettings = await folderDao.GetWatermarkSettings(room);

        return watermarkSettings;
    }

    public async Task<Folder<T>> DeleteWatermarkAsync<T>(T roomId)
    {
        var folderDao = _daoFactory.GetFolderDao<T>();

        var room = await folderDao.GetFolderAsync(roomId);

        if (room == null || !DocSpaceHelper.IsRoom(room.FolderType))
        {
            throw new ItemNotFoundException();
        }

        if (room.RootFolderType == FolderType.Archive || !await _fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        await folderDao.DeleteWatermarkSettings(room);

        await _roomLogoManager.DeleteWatermarkImageAsync(room);

        return room;
    }
}