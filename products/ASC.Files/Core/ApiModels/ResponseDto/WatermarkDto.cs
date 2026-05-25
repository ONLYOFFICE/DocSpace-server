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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The watermark settings.
/// </summary>
public class WatermarkDto
{
    /// <summary>
    /// Specifies whether to display in the watermark: username, user email, user ip-adress, current date, and room name.
    /// </summary>
    /// <example>0</example>
    public required WatermarkAdditions Additions { get; set; }

    /// <summary>
    /// The watermark text.
    /// </summary>
    /// <example>Confidential</example>
    public string Text { get; set; }

    /// <summary>
    /// The watermark text and image rotate.
    /// </summary>
    /// <example>45</example>
    public required int Rotate { get; set; }

    /// <summary>
    /// The watermark image scale.
    /// </summary>
    /// <example>100</example>
    public required int ImageScale { get; set; }

    /// <summary>
    /// The watermark image url.
    /// </summary>
    /// <example>http://localhost/watermark.png</example>
    public string ImageUrl { get; set; }

    /// <summary>
    /// The watermark image height.
    /// </summary>
    /// <example>100.0</example>
    public required double ImageHeight { get; set; }

    /// <summary>
    /// The watermark image width.
    /// </summary>
    /// <example>200.0</example>
    public required double ImageWidth { get; set; }
}
[Scope]
public class WatermarkDtoHelper
{
    public WatermarkDto Get(WatermarkSettings watermarkSettings)
    {
        if (watermarkSettings == null)
        {
            return null;
        }

        return new WatermarkDto
        {
            Additions = watermarkSettings.Additions,
            Text = watermarkSettings.Text,
            Rotate = watermarkSettings.Rotate,
            ImageScale = watermarkSettings.ImageScale,
            ImageUrl = watermarkSettings.ImageUrl,
            ImageHeight = watermarkSettings.ImageHeight,
            ImageWidth = watermarkSettings.ImageWidth
        };
    }
}