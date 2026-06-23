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
/// The request parameters for adding watermarks.
/// </summary>
public class WatermarkRequestDto
{
    /// <summary>
    /// Specifies whether watermarks are on or off.
    /// </summary>
    /// <example>true</example>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Specifies whether to display the following addditional information or not: username, user email, user IP address, current date and room name.
    /// </summary>
    /// <example>1</example>
    public WatermarkAdditions Additions { get; set; }

    /// <summary>
    /// The watermark text.
    /// </summary>
    /// <example>Confidential</example>
    [StringLength(255)]
    public string Text { get; set; }

    /// <summary>
    /// The watermark text and image rotate angle.
    /// </summary>
    /// <example>-45</example>
    public int Rotate { get; set; }

    /// <summary>
    /// The watermark image scale.
    /// </summary>
    /// <example>100</example>
    public int ImageScale { get; set; }

    /// <summary>
    /// The path to the temporary image file.
    /// </summary>
    /// <example>/tmp/watermark.png</example>
    public string ImageUrl { get; set; }

    /// <summary>
    /// The watermark image height.
    /// </summary>
    /// <example>100.0</example>
    public double ImageHeight { get; set; }

    /// <summary>
    /// The watermark image width.
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