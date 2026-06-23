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

namespace ASC.AI.Models.ResponseDto;

/// <summary>
/// MCP server status within a room, reflecting the current user's connection state for OAuth-based servers.
/// </summary>
public class McpServerStatusDto
{
    /// <summary>
    /// Unique identifier of the MCP server.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; init; }

    /// <summary>
    /// Display name of the MCP server.
    /// </summary>
    /// <example>DocSpace Tools</example>
    public required string Name { get; init; }

    /// <summary>
    /// Type of the MCP server (Custom, DocSpace).
    /// </summary>
    /// <example>0</example>
    public ServerType ServerType { get; init; }

    /// <summary>
    /// Indicates whether the current user has an active connection to this server. For direct-connection servers this is always true; for OAuth-based servers it reflects whether the user has completed authorization.
    /// </summary>
    /// <example>true</example>
    public bool Connected { get; init; }

    /// <summary>
    /// Server icon in multiple resolutions for UI display.
    /// </summary>
    /// <example>{"icon48": "/img/icon48.png", "icon32": "/img/icon32.png", "icon24": "/img/icon24.png", "icon16": "/img/icon16.png"}</example>
    public Icon? Icon { get; init; }

    /// <summary>
    /// Indicates whether the server requires a configuration reset due to connectivity or credential issues.
    /// </summary>
    /// <example>false</example>
    public bool NeedReset { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class McpServerStatusDtoMapper
{
    public static partial McpServerStatusDto MapToStatusDto(this McpServerStatus source);
}
