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

using ASC.Files.Core.ApiModels;

namespace ASC.AI.Models.RequestDto;

/// <summary>
/// Request to create a new AI agent room.
/// </summary>
public class CreateAgentRequestDto
{
    /// <summary>
    /// The room name.
    /// </summary>
    /// <example>My AI Agent Room</example>
    [StringLength(170)]
    public required string Title { get; set; }

    /// <summary>
    /// The room quota.
    /// </summary>
    /// <example>10485760</example>
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
    /// <example>{"days": 30, "deleteAfter": true}</example>
    public RoomDataLifetimeDto? Lifetime { get; set; }

    /// <summary>
    /// The watermark settings.
    /// </summary>
    /// <example>{"enabled": true, "text": "Confidential"}</example>
    public WatermarkRequestDto? Watermark { get; set; }

    /// <summary>
    /// The room logo.
    /// </summary>
    /// <example>{"tmpFile": "logo.png", "x": 0, "y": 0, "width": 100, "height": 100}</example>
    public LogoRequest? Logo { get; set; }

    /// <summary>
    /// The list of tags.
    /// </summary>
    /// <example>["ai", "assistant"]</example>
    public IEnumerable<string>? Tags { get; set; }

    /// <summary>
    /// The room color.
    /// </summary>
    /// <example>FF6600</example>
    [StringLength(6)]
    public string? Color { get; set; }

    /// <summary>
    /// The room cover.
    /// </summary>
    /// <example>cover1.jpg</example>
    [StringLength(50)]
    public string? Cover { get; set; }

    /// <summary>
    /// Specifies whether the room to be created is private or not.
    /// </summary>
    /// <example>false</example>
    public bool Private { get; set; }

    /// <summary>
    /// The collection of sharing parameters.
    /// </summary>
    /// <example>[{"shareId": "user@example.com", "access": 1}]</example>
    [MaxEmailInvitations]
    public IEnumerable<FileShareParams>? Share { get; set; }
        
    /// <summary>
    /// The chat settings.
    /// </summary>
    /// <example>{"model": "gpt-4", "temperature": 0.7}</example>
    public ChatSettings? ChatSettings { get; set; }

    /// <summary>
    /// Specifies whether to attach default tools to the agent or not.
    /// </summary>
    /// <example>true</example>
    public bool AttachDefaultTools { get; set; } = true;
}