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
/// The room template parameters.
/// </summary>
public class RoomTemplateDto
{
    /// <summary>
    /// The room template ID.
    /// </summary>
    /// <example>1</example>
    public required int RoomId { get; set; }

    /// <summary>
    /// The room template title.
    /// </summary>
    /// <example>My Document</example>
    public string Title { get; set; }

    /// <summary>
    /// The room template logo.
    /// </summary>
    /// <example>{"tmpFile": "temp_logo.png", "x": 0, "y": 0, "width": 100, "height": 100}</example>
    public LogoRequest Logo { get; set; }

    /// <summary>
    /// Specifies whether to copy room logo or not.
    /// </summary>
    /// <example>true</example>
    public bool CopyLogo { get; set; }

    /// <summary>
    /// The collection of email addresses of users with whom to share a room.
    /// </summary>
    /// <example>["user1@example.com", "user2@example.com"]</example>
    public List<string> Share { get; set; }

    /// <summary>
    /// The collection of groups with whom to share a room.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> Groups { get; set; }

    /// <summary>
    /// Specifies whether the room template is public or not.
    /// </summary>
    /// <example>true</example>
    public bool Public { get; set; }

    /// <summary>
    /// The collection of tags.
    /// </summary>
    /// <example>["tag1", "tag2"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The color of the room template.
    /// </summary>
    /// <example>#FF0000</example>
    [StringLength(6)]
    public string Color { get; set; }

    /// <summary>
    /// The cover of the room template.
    /// </summary>
    /// <example>cover1</example>
    [StringLength(50)]
    public string Cover { get; set; }

    /// <summary>
    /// Room quota
    /// </summary>
    /// <example>10485760</example>
    public long? Quota { get; set; }
}
