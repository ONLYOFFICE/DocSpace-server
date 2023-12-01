// (c) Copyright Ascensio System SIA 2010-2022
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

public class WatermarkJson
{
    public bool Enabled { get; set; }      
    public List<string> Text { get; set; }
    public int Rotate { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string ImageUrl { get; set; }
    public int Scale { get; set; }
}

[Scope]
public class WaterMarksManager
{
    private readonly IDaoFactory _daoFactory;
    private readonly FileSecurity _fileSecurity;
    private readonly RoomLogoManager _roomLogoManager;
    public WaterMarksManager(
        IDaoFactory daoFactory,
        FileSecurity fileSecurity,
        RoomLogoManager roomLogoManager)
    {
        _daoFactory = daoFactory;
        _fileSecurity = fileSecurity;
        _roomLogoManager = roomLogoManager;
    }

    public async Task<Folder<T>> AddRoomWaterMarksAsync<T>(T roomId, WatermarksRequestDto watermarksRequestDto)
    {
        var room = await _daoFactory.GetFolderDao<T>().GetFolderAsync(roomId);
        var folderDao = _daoFactory.GetFolderDao<T>();

        if (room.RootFolderType == FolderType.Archive || !await _fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        if (room == null || !DocSpaceHelper.IsRoom(room.FolderType))
        {
            throw new ItemNotFoundException();
        }

        if (room.RootFolderType == FolderType.Archive || !await _fileSecurity.CanEditRoomAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        var textArray = GetWaterMarkText(watermarksRequestDto);
        var watermarkSetings = new WatermarkJson()
        {
            Text = textArray,
            Rotate = watermarksRequestDto.Rotate,
            Height = watermarksRequestDto.Height,
            Width = watermarksRequestDto.Width,
            ImageUrl = string.Empty,
            Scale = watermarksRequestDto.Scale
        };

        await folderDao.WatermarksSaveToDbAsync(watermarkSetings, room);

        return room;
    }
    
    internal List<string> GetWaterMarkText(WatermarksRequestDto watermarksRequestDto)
    {
        var text = new List<string> ();
        if (watermarksRequestDto.UserName)
        {
            text.Add(("${UserName}"));
        }

        if (watermarksRequestDto.UserEmail)
        {
            text.Add(("${UserEmail}"));
        }

        if (watermarksRequestDto.UserIpAdress)
        {
            text.Add(("${UserIpAdress}"));
        }

        if (watermarksRequestDto.CurrentDate)
        {
            text.Add(("${CurrentDate}"));
        }

        if (watermarksRequestDto.RoomName)
        {
            text.Add(("${RoomName}"));
        }
        if (watermarksRequestDto.Text != string.Empty)
        {
            text.Add((watermarksRequestDto.Text));
        }

        return text;
    }

    public async Task<WatermarksRequestDto> GetWatermarkInformation<T>(Folder<T> room)
    {
        var folderDao = _daoFactory.GetFolderDao<T>();

        if (room.RootFolderType == FolderType.Archive || !await _fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        if (room == null || !DocSpaceHelper.IsRoom(room.FolderType))
        {
            throw new ItemNotFoundException();  
        }

        if (room.RootFolderType == FolderType.Archive || !await _fileSecurity.CanEditRoomAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        var watermarkData  = await folderDao.GetWatermarkInfo(room);
        var watermarkRequestDto = new WatermarksRequestDto();

        watermarkRequestDto.Enabled = watermarkData.Enabled;

        var userName = @"\${UserName}";
        var userEmail = @"\${UserEmail}";
        var userIpAdress = @"\${UserIpAdress}";
        var currentDate = @"\${CurrentDate}";
        var roomName = @"\${RoomName}";
        var text = @"\$\{(.*?)\}";

        if (watermarkData.Text.Count != 0)
        {
            foreach (var watermark in watermarkData.Text)
            {
                if (Regex.IsMatch(watermark, userName))
                {
                    watermarkRequestDto.UserName = true;
                }
                if (Regex.IsMatch(watermark, userEmail))
                {
                    watermarkRequestDto.UserEmail = true;
                }
                if (Regex.IsMatch(watermark, userIpAdress))
                {
                    watermarkRequestDto.UserIpAdress = true;
                }
                if (Regex.IsMatch(watermark, currentDate))
                {
                    watermarkRequestDto.CurrentDate = true;
                }
                if (Regex.IsMatch(watermark, roomName))
                {
                    watermarkRequestDto.RoomName = true;
                }
                if(!Regex.IsMatch(watermark, text))
                {
                    watermarkRequestDto.Text = watermark;
                }
            }
        }
        watermarkRequestDto.Rotate = watermarkData.Rotate;
        watermarkRequestDto.Scale = watermarkData.Scale;
        watermarkRequestDto.UrlImage = watermarkData.ImageUrl;
        watermarkRequestDto.Height = watermarkData.Height;
        watermarkRequestDto.Width = watermarkData.Width;

        return watermarkRequestDto;
    }

    public async Task<Folder<T>> RemoveRoomWaterMarksAsync<T>(T roomId)
    {
        var room = await _daoFactory.GetFolderDao<T>().GetFolderAsync(roomId);
        var folderDao = _daoFactory.GetFolderDao<T>();

        await _roomLogoManager.DeleteWatermarkImageAsync(room);
        if (room.RootFolderType == FolderType.Archive || !await _fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        if (room == null || !DocSpaceHelper.IsRoom(room.FolderType))
        {
            throw new ItemNotFoundException();
        }

        if (room.RootFolderType == FolderType.Archive || !await _fileSecurity.CanEditRoomAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }
        await folderDao.DeleteWatermarkFromDbAsync(room);

        return room;
    }
}