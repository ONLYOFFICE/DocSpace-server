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

using ImageMagick;

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The white label item parameters.
/// </summary>
/// <example>
/// {
///   "type": 1,
///   "name": "example value",
///   "size": {},
///   "path": {}
/// }
/// </example>
public class WhiteLabelItemDto
{
    /// <summary>
    /// The white label logo type.
    /// </summary>
    /// <example>1</example>
    public WhiteLabelLogoType Type { get; set; }

    /// <summary>
    /// The white label file name.
    /// </summary>
    /// <example>Example Name</example>
    public string Name { get; set; }

    /// <summary>
    /// The white label file size.
    /// </summary>
    /// <example>{}</example>
    public WhiteLabelItemSizeDto Size { get; set; }

    /// <summary>
    /// The white label file path.
    /// </summary>
    /// <example>{}</example>
    public WhiteLabelItemPathDto Path { get; set; }
}

/// <summary>
/// The white label item path parameters.
/// </summary>
public class WhiteLabelItemPathDto
{
    /// <summary>
    /// The path to the light theme logo.
    /// </summary>
    /// <example>/images/logo-light.png</example>
    public string Light { get; set; }

    /// <summary>
    /// The path to the dark theme logo.
    /// </summary>
    /// <example>/images/logo-dark.png</example>
    public string Dark { get; set; }
}

/// <summary>
/// The white label logo size parameters.
/// </summary>
public class WhiteLabelItemSizeDto
{
    /// <summary>
    /// Specifies whether the size is an aspect ratio.
    /// </summary>
    /// <example>false</example>
    public bool AspectRatio { get; set; }

    /// <summary>
    /// Specifies whether the logo is resized based on the smallest fitting dimension.
    /// </summary>
    /// <example>false</example>
    public bool FillArea { get; set; }

    /// <summary>
    /// Specifies whether the logo is resized only if it is greater than the size.
    /// </summary>
    /// <example>false</example>
    public bool Greater { get; set; }

    /// <summary>
    /// The logo height, in pixels.
    /// </summary>
    /// <example>48</example>
    public uint Height { get; set; }

    /// <summary>
    /// Specifies whether the logo is resized without preserving the aspect ratio.
    /// </summary>
    /// <example>false</example>
    public bool IgnoreAspectRatio { get; set; }

    /// <summary>
    /// Specifies whether the width and height are expressed as percentages.
    /// </summary>
    /// <example>false</example>
    public bool IsPercentage { get; set; }

    /// <summary>
    /// Specifies whether the logo is resized only if it is less than the size.
    /// </summary>
    /// <example>false</example>
    public bool Less { get; set; }

    /// <summary>
    /// Specifies whether the logo is resized using a pixel area count limit.
    /// </summary>
    /// <example>false</example>
    public bool LimitPixels { get; set; }

    /// <summary>
    /// The logo width, in pixels.
    /// </summary>
    /// <example>422</example>
    public uint Width { get; set; }

    /// <summary>
    /// The X offset from the origin, in pixels.
    /// </summary>
    /// <example>0</example>
    public int X { get; set; }

    /// <summary>
    /// The Y offset from the origin, in pixels.
    /// </summary>
    /// <example>0</example>
    public int Y { get; set; }

    /// <summary>
    /// Creates the white label logo size from the image geometry.
    /// </summary>
    /// <param name="geometry">The image geometry.</param>
    /// <returns>The white label logo size parameters.</returns>
    public static WhiteLabelItemSizeDto FromGeometry(IMagickGeometry geometry)
    {
        return new WhiteLabelItemSizeDto
        {
            AspectRatio = geometry.AspectRatio,
            FillArea = geometry.FillArea,
            Greater = geometry.Greater,
            Height = geometry.Height,
            IgnoreAspectRatio = geometry.IgnoreAspectRatio,
            IsPercentage = geometry.IsPercentage,
            Less = geometry.Less,
            LimitPixels = geometry.LimitPixels,
            Width = geometry.Width,
            X = geometry.X,
            Y = geometry.Y
        };
    }
}
