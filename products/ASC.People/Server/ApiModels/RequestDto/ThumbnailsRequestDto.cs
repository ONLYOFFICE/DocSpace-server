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

namespace ASC.People.ApiModels.RequestDto;

/// <summary>
/// The crop rectangle to apply to an avatar image.
/// </summary>
public class ThumbnailsRequest
{
    /// <summary>
    /// The temporary image to crop, as returned in the `data` of an upload made with `autosave` off. Only the file
    /// name part of the value is used. Omit it to re-crop the photo the profile already has.
    /// </summary>
    /// <example>photo_temp_123.jpg</example>
    public string TmpFile { get; set; }

    /// <summary>
    /// The distance in pixels from the left edge of the original image to the left edge of the crop rectangle.
    /// </summary>
    /// <example>100</example>
    public int X { get; set; }

    /// <summary>
    /// The distance in pixels from the top edge of the original image to the top edge of the crop rectangle.
    /// </summary>
    /// <example>50</example>
    public int Y { get; set; }

    /// <summary>
    /// The width of the crop rectangle in pixels. Passing 0 together with `height` and `tmpFile` keeps the whole
    /// uploaded image instead of cropping it.
    /// </summary>
    /// <example>200</example>
    public uint Width { get; set; }

    /// <summary>
    /// The height of the crop rectangle in pixels. Passing 0 together with `width` and `tmpFile` keeps the whole
    /// uploaded image instead of cropping it.
    /// </summary>
    /// <example>200</example>
    public uint Height { get; set; }
}

/// <summary>
/// The thumbnail request parameters.
/// </summary>
public class ThumbnailsRequestDto
{
    /// <summary>
    /// The profile whose avatar is cropped, taken from the route. Either the ID of the account or its user name is
    /// accepted, and it has to be the calling account, because a profile photo can only be changed by its owner.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required string UserId { get; set; }

    /// <summary>
    /// The crop rectangle, and optionally the temporary image to crop.
    /// </summary>
    /// <example>{"tmpFile":"photo_temp_123.jpg","x":0,"y":0,"width":200,"height":200}</example>
    [FromBody]
    public required ThumbnailsRequest Thumbnails { get; set; }
}