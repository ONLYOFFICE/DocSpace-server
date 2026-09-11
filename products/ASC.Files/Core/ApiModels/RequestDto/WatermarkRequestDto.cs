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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// The watermark drawn over the documents of a room.
/// </summary>
public class WatermarkRequestDto
{
    /// <summary>
    /// Whether the room draws a watermark at all. Sending the object with this turned off removes the watermark the
    /// room has, and the rest of the fields are then irrelevant.
    /// </summary>
    /// <example>true</example>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Which details of the reader and of the room are stamped into the watermark alongside the text. The values
    /// combine, so several of them can be added together to stamp more than one.
    /// </summary>
    /// <example>3</example>
    public WatermarkAdditions Additions { get; set; }

    /// <summary>
    /// The fixed line drawn over the document, shown before the details selected alongside it. It is the whole
    /// watermark when no details are added.
    /// </summary>
    /// <example>Confidential</example>
    [StringLength(255)]
    public string Text { get; set; }

    /// <summary>
    /// How far the watermark is turned, in degrees, with negative values turning it anticlockwise. Zero draws it
    /// horizontally across the page.
    /// </summary>
    /// <example>-45</example>
    public int Rotate { get; set; }

    /// <summary>
    /// How large the watermark image is drawn, as a percentage of its own size. It applies to the image form of the
    /// watermark only.
    /// </summary>
    /// <example>100</example>
    public int ImageScale { get; set; }

    /// <summary>
    /// The picture to use instead of a text watermark, named by the path that `POST api/2.0/files/logos` returned for
    /// an image uploaded beforehand. The portal copies it into the room when the setting is saved.
    /// </summary>
    /// <example>/temp/watermark_a1b2c3.png</example>
    public string ImageUrl { get; set; }

    /// <summary>
    /// The height the watermark image is drawn with, in pixels, used together with the width to keep its proportions.
    /// </summary>
    /// <example>100.0</example>
    public double ImageHeight { get; set; }

    /// <summary>
    /// The width the watermark image is drawn with, in pixels, used together with the height to keep its proportions.
    /// </summary>
    /// <example>200.0</example>
    public double ImageWidth { get; set; }
}

/// <summary>
/// The request parameters for adding watermarks.
/// </summary>
public class WatermarkRequestDto<T>
{
    /// <summary>
    /// The room ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The watermark settings.
    /// </summary>
    [FromBody]
    public required WatermarkRequestDto Watermark { get; set; }
}