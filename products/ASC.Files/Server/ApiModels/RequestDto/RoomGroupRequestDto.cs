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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The name, the icon and the rooms of a room group to create.
/// </summary>
public class RoomGroupRequestDto
{
    /// <summary>
    /// The name to show the group under. Surrounding spaces are trimmed before it is stored, a name that is blank
    /// once trimmed is refused, and the name does not have to differ from the names of the caller's other groups.
    /// </summary>
    /// <example>Client projects</example>
    [Required]
    [StringLength(128)]
    public string Name { get; set; }

    /// <summary>
    /// The icon of the group, given as the identifier of one of the built-in covers listed by
    /// `GET api/2.0/files/rooms/covers`. An uploaded image cannot be used, and any value that is not one of those
    /// identifiers is refused.
    /// </summary>
    /// <example>star</example>
    [Required]
    [StringLength(50)]
    public string Icon { get; set; }

    /// <summary>
    /// The rooms to gather in the group, each given as a number for a room stored in the portal or as a string for a
    /// room on a connected third-party account. Every identifier has to name a room the caller can read; repeats are
    /// collapsed, and an element of any other shape - a decimal number, a number sent as a string, null - is refused.
    /// </summary>
    /// <example>[12, 15, "folder-123-abc"]</example>
    [Required]
    public List<JsonElement> Rooms { get; set; }
}