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
/// The room logo information.
/// </summary>
public class Logo
{
    /// <summary>
    /// The original logo.
    /// </summary>
    /// <example>https://portal.example.com/logo/original.png</example>
    public required string Original { get; set; }

    /// <summary>
    /// The large logo.
    /// </summary>
    /// <example>https://portal.example.com/logo/large.png</example>
    public required string Large { get; set; }

    /// <summary>
    /// The medium logo.
    /// </summary>
    /// <example>https://portal.example.com/logo/medium.png</example>
    public required string Medium { get; set; }

    /// <summary>
    /// The small logo.
    /// </summary>
    /// <example>https://portal.example.com/logo/small.png</example>
    public required string Small { get; set; }

    /// <summary>
    /// The logo color.
    /// </summary>
    /// <example>#4781D1</example>
    public string Color { get; set; }

    /// <summary>
    /// The logo cover.
    /// </summary>
    /// <example>{"id": "default_cover", "data": "base64-image-data..."}</example>
    public LogoCover Cover { get; set; }

    public bool IsDefault()
    {
        return string.IsNullOrEmpty(Original);
    }
}

/// <summary>
/// The logo cover information.
/// </summary>
public class LogoCover
{
    /// <summary>
    /// The logo cover ID.
    /// </summary>
    /// <example>default_cover</example>
    public required string Id { get; set; }

    /// <summary>
    /// The logo cover data.
    /// </summary>
    /// <example>base64-image-data...</example>
    public required string Data { get; set; }
}

public class MultiSizeLogoCover
{
    /// <summary>
    /// The logo cover ID.
    /// </summary>
    /// <example>default_cover</example>
    public required string Id { get; set; }

    /// <summary>
    /// The logo cover data.
    /// </summary>
    /// <example>
    /// {
    ///   "small": "base64...",
    ///   "medium": "base64...",
    ///   "large": "base64..."
    /// }
    /// </example>
    public required IReadOnlyDictionary<string, string> Data { get; init; }
}