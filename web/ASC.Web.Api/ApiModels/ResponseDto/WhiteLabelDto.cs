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
/// One branding logo slot of the portal: the size it is drawn at, and where its images are served from.
/// </summary>
/// <example>
/// {
///   "type": 1,
///   "name": "LightSmall",
///   "size": { "width": 422, "height": 48 },
///   "path": { "light": "/images/logo-light.png", "dark": "/images/logo-dark.png" }
/// }
/// </example>
public class WhiteLabelItemDto
{
    /// <summary>
    /// Which branding slot this entry describes. `Notification` is part of the type but never appears here: that
    /// logo is derived from the login-page one and used only in letters.
    /// </summary>
    /// <example>1</example>
    public WhiteLabelLogoType Type { get; set; }

    /// <summary>
    /// The stable name of the same slot, which is what `GET api/2.0/settings/whitelabel/logos/isdefault` keys its
    /// entries by. It is a name to match on, not a file name.
    /// </summary>
    /// <example>LightSmall</example>
    public string Name { get; set; }

    /// <summary>
    /// The pixel box the slot is drawn in. Only `width` and `height` carry information here; the resize flags and
    /// offsets alongside them are left at their defaults and say nothing about how an uploaded image is treated.
    /// </summary>
    /// <example>{ "width": 422, "height": 48 }</example>
    public WhiteLabelItemSizeDto Size { get; set; }

    /// <summary>
    /// The absolute URLs to render the slot from, one per theme.
    /// </summary>
    /// <example>{ "light": "/images/logo-light.png", "dark": "/images/logo-dark.png" }</example>
    public WhiteLabelItemPathDto Path { get; set; }
}

/// <summary>
/// The image URLs of one logo slot, per interface theme.
/// </summary>
public class WhiteLabelItemPathDto
{
    /// <summary>
    /// The absolute URL of the image to render on a light background. It is filled in unless the request asked
    /// for the dark theme alone with `isDark=true`, in which case only `dark` comes back.
    /// </summary>
    /// <example>/images/logo-light.png</example>
    public string Light { get; set; }

    /// <summary>
    /// The absolute URL of the image to render on a dark background. When both themes are asked for it comes back
    /// empty for a slot that has no separate dark image, meaning the light one is to be used for both; when
    /// `isDark=false` was passed it is left out entirely.
    /// </summary>
    /// <example>/images/logo-dark.png</example>
    public string Dark { get; set; }
}

/// <summary>
/// The pixel box a logo slot is drawn in, in the shape the imaging library reports a geometry.
/// </summary>
public class WhiteLabelItemSizeDto
{
    /// <summary>
    /// Whether the numbers are to be read as an aspect ratio rather than as pixels. Always `false` on the sizes
    /// this API reports.
    /// </summary>
    /// <example>false</example>
    public bool AspectRatio { get; set; }

    /// <summary>
    /// Whether an image would be scaled to cover the box rather than to fit inside it. Always `false` here.
    /// </summary>
    /// <example>false</example>
    public bool FillArea { get; set; }

    /// <summary>
    /// Whether scaling would apply only to an image larger than the box. Always `false` here.
    /// </summary>
    /// <example>false</example>
    public bool Greater { get; set; }

    /// <summary>
    /// The height of the box in pixels - one of the two fields of this object that carry information.
    /// </summary>
    /// <example>48</example>
    public uint Height { get; set; }

    /// <summary>
    /// Whether scaling would be allowed to distort the image. Always `false` here.
    /// </summary>
    /// <example>false</example>
    public bool IgnoreAspectRatio { get; set; }

    /// <summary>
    /// Whether `width` and `height` are to be read as percentages. Always `false` here, so both are pixels.
    /// </summary>
    /// <example>false</example>
    public bool IsPercentage { get; set; }

    /// <summary>
    /// Whether scaling would apply only to an image smaller than the box. Always `false` here.
    /// </summary>
    /// <example>false</example>
    public bool Less { get; set; }

    /// <summary>
    /// Whether the box is to be read as a total pixel-area budget instead of as two dimensions. Always `false`
    /// here.
    /// </summary>
    /// <example>false</example>
    public bool LimitPixels { get; set; }

    /// <summary>
    /// The width of the box in pixels - the other field of this object that carries information.
    /// </summary>
    /// <example>422</example>
    public uint Width { get; set; }

    /// <summary>
    /// The horizontal offset of the box from the origin. Always `0` here.
    /// </summary>
    /// <example>0</example>
    public int X { get; set; }

    /// <summary>
    /// The vertical offset of the box from the origin. Always `0` here.
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
