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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// The storage one category of a portal module occupies, in the form a statistics page prints it.
/// </summary>
/// <example>
/// {
///   "name": "Collaboration rooms",
///   "icon": "/images/icons/rooms.svg",
///   "disabled": true,
///   "size": "1.5 GB",
///   "url": "/rooms/shared"
/// }
/// </example>
public class UsageSpaceStatItemDto
{
    /// <summary>
    /// The category name in the portal language, HTML-escaped and ready to be rendered as text. What a category
    /// stands for depends on the module asked about - for the Documents module it is a room type.
    /// </summary>
    /// <example>Collaboration rooms</example>
    public string Name { get; set; }

    /// <summary>
    /// The path of the icon to render beside the name, relative to the portal address. It is empty for a category
    /// that ships no icon.
    /// </summary>
    /// <example>/images/icons/rooms.svg</example>
    public string Icon { get; set; }

    /// <summary>
    /// Whether the category is switched off for this portal. A disabled category still reports the space it
    /// occupies, so it is worth showing greyed out rather than dropping.
    /// </summary>
    /// <example>true</example>
    public bool Disabled { get; set; }

    /// <summary>
    /// The occupied space already formatted for display, with its unit and in the portal language - `0 Byte` for
    /// an empty category. It is not a byte count and must not be parsed; the raw numbers live in the quota
    /// reported by `GET api/2.0/portal/quota`.
    /// </summary>
    /// <example>1.5 GB</example>
    public string Size { get; set; }

    /// <summary>
    /// The portal page that lists the contents of this category, relative to the portal address, so a statistics
    /// page can link through to it. It is empty for a category with no page of its own.
    /// </summary>
    /// <example>/rooms/shared</example>
    public string Url { get; set; }
}
