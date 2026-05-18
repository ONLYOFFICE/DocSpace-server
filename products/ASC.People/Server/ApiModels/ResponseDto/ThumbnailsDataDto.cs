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

namespace ASC.People.ApiModels.ResponseDto;

///<summary>
/// The thumbnails data parameters.
///</summary>
public class ThumbnailsDataDto
{
    private ThumbnailsDataDto() { }

    public static async Task<ThumbnailsDataDto> Create(UserInfo userInfo, UserPhotoManager userPhotoManager)
    {
        var cacheKey = Math.Abs(userInfo.LastModified.GetHashCode());

        return new ThumbnailsDataDto
        {
            Original = await userPhotoManager.GetPhotoAbsoluteWebPath(userInfo.Id) + $"?hash={cacheKey}",
            Retina = await userPhotoManager.GetRetinaPhotoURL(userInfo.Id) + $"?hash={cacheKey}",
            Max = await userPhotoManager.GetMaxPhotoURL(userInfo.Id) + $"?hash={cacheKey}",
            Big = await userPhotoManager.GetBigPhotoURL(userInfo.Id) + $"?hash={cacheKey}",
            Medium = await userPhotoManager.GetMediumPhotoURL(userInfo.Id) + $"?hash={cacheKey}",
            Small = await userPhotoManager.GetSmallPhotoURL(userInfo.Id) + $"?hash={cacheKey}"
        };
    }

    /// <summary>
    /// The thumbnail original photo.
    /// </summary>
    /// <example>default_user_photo_size_1280-1280.png</example>
    public string Original { get; set; }

    /// <summary>
    /// The thumbnail retina.
    /// </summary>
    /// <example>default_user_photo_size_360-360.png</example>
    public string Retina { get; set; }

    /// <summary>
    /// The thumbnail maximum size photo.
    /// </summary>
    /// <example>default_user_photo_size_200-200.png</example>
    public string Max { get; set; }

    /// <summary>
    /// The thumbnail big size photo.
    /// </summary>
    /// <example>default_user_photo_size_82-82.png</example>
    public string Big { get; set; }

    /// <summary>
    /// The thumbnail medium size photo.
    /// </summary>
    /// <example>default_user_photo_size_48-48.png</example>
    public string Medium { get; set; }

    /// <summary>
    /// The thumbnail small size photo.
    /// </summary>
    /// <example>default_user_photo_size_32-32.png</example>
    public string Small { get; set; }
}