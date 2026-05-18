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
/// The request parameters for updating a room.
/// </summary>
public class UpdateRoomRequest
{
    /// <summary>
    /// The room title.
    /// </summary>
    /// <example>My Document</example>
    [StringLength(170)]
    public string Title { get; set; }

    /// <summary>
    /// The room quota.
    /// </summary>
    /// <example>10485760</example>
    public long? Quota { get; set; }

    /// <summary>
    /// Specifies whether to create a third-party room with indexing.
    /// </summary>
    /// <example>true</example>
    public bool? Indexing { get; set; }

    /// <summary>
    /// Specifies whether to deny downloads from the third-party room.
    /// </summary>
    /// <example>true</example>
    public bool? DenyDownload { get; set; }

    /// <summary>
    /// The room data lifetime information.
    /// </summary>
    /// <example>{"value": 12, "deletePermanently": false}</example>
    public RoomDataLifetimeDto Lifetime { get; set; }

    /// <summary>
    /// The watermark settings.
    /// </summary>
    /// <example>{"enabled": false}</example>
    public WatermarkRequestDto Watermark { get; set; }

    /// <summary>
    /// The room logo.
    /// </summary>
    /// <example>{"tmpFile": "temp_logo.png", "x": 0, "y": 0, "width": 100, "height": 100}</example>
    public LogoRequest Logo { get; set; }

    /// <summary>
    /// The list of tags.
    /// </summary>
    /// <example>["tag1", "tag2"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The room color.
    /// </summary>
    /// <example>#FF5733</example>
    [StringLength(6)]
    public string Color { get; set; }

    /// <summary>
    /// The room cover.
    /// </summary>
    /// <example>cover1</example>
    [StringLength(50)]
    public string Cover { get; set; }
    
    /// <summary>
    /// The chat settings.
    /// </summary>
    /// <example>{"providerId": 1, "modelId": "gpt-4", "prompt": "You are a helpful assistant."}</example>
    public ChatSettings ChatSettings { get; set; }

    /// <summary>
    /// Specifies whether to send form data to external database.
    /// </summary>
    /// <example>false</example>
    public bool? SendFormToExternalDB { get; set; }

    /// <summary>
    /// Specifies whether to save form data as XLSX file.
    /// </summary>
    /// <example>false</example>
    public bool? SaveFormAsXLSX { get; set; }
}

/// <summary>
/// The request parameters for updating a room.
/// </summary>
public class UpdateRoomRequestDto<T>
{
    /// <summary>
    /// The room ID.
    /// </summary>
    /// <example>file-id</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The request parameters for updating a room.
    /// </summary>
    [FromBody]
    public required UpdateRoomRequest UpdateRoom { get; set; }
}