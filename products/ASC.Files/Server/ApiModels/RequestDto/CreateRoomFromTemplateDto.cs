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
/// The parameters for creating a room from a template.
/// </summary>
public class CreateRoomFromTemplateDto
{
    /// <summary>
    /// The template ID from which the room to be created.
    /// </summary>
    /// <example>1</example>
    public required int TemplateId { get; set; }

    /// <summary>
    /// The room title.
    /// </summary>
    /// <example>My Room From Template</example>
    public required string Title { get; set; }

    /// <summary>
    /// The logo request parameters.
    /// </summary>
    /// <example>{"tmpFile": "/temp/logo.png", "x": 0, "y": 0, "width": 100, "height": 100}</example>
    public LogoRequest Logo { get; set; }

    /// <summary>
    /// Specifies whether to copy a logo or not.
    /// </summary>
    /// <example>false</example>
    public bool CopyLogo { get; set; }

    /// <summary>
    /// The collection of tags.
    /// </summary>
    /// <example>["tag1", "tag2", "tag3"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The color of the room to be created.
    /// </summary>
    /// <example>#FF0000</example>
    [StringLength(6)]
    public string Color { get; set; }

    /// <summary>
    /// The cover of the room to be created.
    /// </summary>
    /// <example>cover1.jpg</example>
    [StringLength(50)]
    public string Cover { get; set; }

    /// <summary>
    /// The room quota.
    /// </summary>
    /// <example>1073741824</example>
    public long? Quota { get; set; }

    /// <summary>
    /// Specifies whether to create a room with indexing.
    /// </summary>
    /// <example>true</example>
    public bool? Indexing { get; set; }

    /// <summary>
    /// Specifies whether to deny downloads from the room.
    /// </summary>
    /// <example>false</example>
    public bool? DenyDownload { get; set; }

    /// <summary>
    /// The room data lifetime information.
    /// </summary>
    /// <example>{"deletePermanently": false, "period": 0, "value": 30, "enabled": true}</example>
    public RoomDataLifetimeDto Lifetime { get; set; }

    /// <summary>
    /// The watermark settings.
    /// </summary>
    /// <example>{"enabled": true, "text": "Confidential", "rotate": -45, "imageScale": 100}</example>
    public WatermarkRequestDto Watermark { get; set; }

    /// <summary>
    /// Specifies whether the room to be created is private or not.
    /// </summary>
    /// <example>false</example>
    public bool? Private { get; set; }
}