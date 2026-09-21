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

/// <summary>The part of an uploaded picture to use as the logo.</summary>
public class LogoRequest
{
    /// <summary>
    /// The picture to cut the logo out of, named by the path that `POST api/2.0/files/logos` returned for it. The
    /// path may be used once and only by the account that uploaded it.
    /// </summary>
    /// <example>/temp/logo_a1b2c3.png</example>
    [Required]
    public string TmpFile { get; set; }

    /// <summary>
    /// The left edge of the rectangle cut out of the uploaded picture, counted in pixels from its left side. The
    /// picture itself was already scaled down to fit 1280 by 1280 pixels when it was uploaded.
    /// </summary>
    /// <example>0</example>
    [Range(0, 1280)]
    public int X { get; set; }

    /// <summary>
    /// The top edge of the rectangle cut out of the uploaded picture, counted in pixels from its top.
    /// </summary>
    /// <example>0</example>
    [Range(0, 1280)]
    public int Y { get; set; }

    /// <summary>
    /// How wide a piece of the uploaded picture to cut out, in pixels. It has to be sent together with the height,
    /// and the portal builds the four logo sizes out of the piece.
    /// </summary>
    /// <example>300</example>
    [Range(1, 1280)]
    public uint Width { get; set; }

    /// <summary>
    /// How tall a piece of the uploaded picture to cut out, in pixels. It has to be sent together with the width.
    /// </summary>
    /// <example>300</example>
    [Range(1, 1280)]
    public uint Height { get; set; }
}

/// <summary>
/// The logo request parameters for the specified room.
/// </summary>
public class LogoRequest<T>
{
    /// <summary>The room the logo is set on.</summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>The uploaded picture and the piece of it to use.</summary>
    /// <example>{"tmpFile": "/temp/logo_a1b2c3.png", "x": 0, "y": 0, "width": 300, "height": 300}</example>
    [FromBody]
    public required LogoRequest Logo { get; set; }
}
