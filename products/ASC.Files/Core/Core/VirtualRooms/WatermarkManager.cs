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

namespace ASC.Files.Core.VirtualRooms;

/// <summary>
/// The watermark additions.
/// </summary>
[Flags]
public enum WatermarkAdditions
{
    [Description("User name")]
    UserName = 1,

    [Description("User email")]
    UserEmail = 2,

    [Description("User ip adress")]
    UserIpAdress = 4,

    [Description("Current date")]
    CurrentDate = 8,

    [Description("Room name")]
    RoomName = 16
}

/// <summary>
/// The watermark settings information.
/// </summary>
public class WatermarkSettings
{
    /// <summary>
    /// The watermark text.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// The watermark additions.
    /// </summary>
    public WatermarkAdditions Additions { get; set; }

    /// <summary>
    /// The watermark rotate angle.
    /// </summary>
    public int Rotate { get; set; }

    /// <summary>
    /// The watermark image width.
    /// </summary>
    public double ImageWidth { get; set; }

    /// <summary>
    /// The watermark image height.
    /// </summary>
    public double ImageHeight { get; set; }

    /// <summary>
    /// The watermark image URL.
    /// </summary>
    public string ImageUrl { get; set; }

    /// <summary>
    /// The watermark image scale.
    /// </summary>
    public int ImageScale { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class WatermarkSettingsMapper
{
    public static partial WatermarkSettings Map(this WatermarkRequestDto source);
    public static partial DbRoomWatermark Map(this WatermarkSettings source);
    public static partial WatermarkSettings Map(this DbRoomWatermark source);
}

[Scope]
public class WatermarkManager(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    RoomLogoManager roomLogoManager)
{
    public async Task<WatermarkSettings> SetWatermarkAsync<T>(Folder<T> room, WatermarkRequestDto watermarkRequestDto)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        if (watermarkRequestDto == null)
        {
            return new WatermarkSettings();
        }

        if (room is not { IsRoom: true })
        {
            throw new ItemNotFoundException();
        }

        if (room.RootFolderType == FolderType.Archive || !await fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        var watermarkSettings = new WatermarkSettings
        {
            Text = watermarkRequestDto.Text,
            Additions = watermarkRequestDto.Additions,
            Rotate = watermarkRequestDto.Rotate
        };

        var imageUrl = await GetWatermarkImageUrlAsync(room, watermarkRequestDto.ImageUrl);

        if (!string.IsNullOrEmpty(imageUrl))
        {
            watermarkSettings.ImageScale = watermarkRequestDto.ImageScale;
            watermarkSettings.ImageHeight = watermarkRequestDto.ImageHeight;
            watermarkSettings.ImageWidth = watermarkRequestDto.ImageWidth;
            watermarkSettings.ImageUrl = imageUrl;
        }

        await folderDao.SetWatermarkSettings(watermarkSettings, room);

        return watermarkSettings;
    }

    public async Task<string> GetWatermarkImageUrlAsync<T>(Folder<T> folder, string imageUrlFromDto)
    {
        string imageUrl = null;

        if (!string.IsNullOrEmpty(imageUrlFromDto))
        {
            if (Uri.IsWellFormedUriString(imageUrlFromDto, UriKind.Absolute))
            {
                imageUrl = imageUrlFromDto;
            }
            else
            {
                imageUrl = await roomLogoManager.CreateWatermarkImageAsync(folder, imageUrlFromDto);
            }
        }

        return imageUrl;
    }

    public async Task<WatermarkSettings> GetWatermarkAsync<T>(Folder<T> room)
    {
        if (room is not { IsRoom: true } ||
            room.RootFolderType == FolderType.Archive ||
            !await fileSecurity.CanEditRoomAsync(room))
        {
            return null;
        }

        var folderDao = daoFactory.GetFolderDao<T>();

        var watermarkSettings = await folderDao.GetWatermarkSettings(room);

        return watermarkSettings;
    }
}
