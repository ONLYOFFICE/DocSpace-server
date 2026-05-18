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

namespace ASC.AI.Models.RequestDto;

/// <summary>
/// Request to register a new custom MCP server.
/// </summary>
public class AddServerRequestDto
{
    /// <summary>
    /// MCP server registration parameters.
    /// </summary>
    /// <example>{"name": "my-custom-server", "description": "Custom MCP server for project management tools", "endpoint": "https://mcp.example.com/sse"}</example>
    [FromBody]
    public required AddMcpServerRequestBody Body { get; init; }
}

/// <summary>
/// Parameters for creating a new custom MCP server.
/// </summary>
public class AddMcpServerRequestBody
{
    /// <summary>
    /// Unique display name for the server. Only letters, numbers, underscores, and hyphens are allowed. Maximum 128 characters.
    /// </summary>
    /// <example>my-custom-server</example>
    [MaxLength(128)]
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of the server's purpose and capabilities. Maximum 255 characters.
    /// </summary>
    /// <example>Custom MCP server for project management tools</example>
    [MaxLength(255)]
    public required string Description { get; init; }

    /// <summary>
    /// Base URL of the MCP server endpoint. Must be a valid, reachable URL. The system will verify connectivity during registration.
    /// </summary>
    /// <example>https://mcp.example.com/sse</example>
    [Url]
    public required string Endpoint { get; init; }

    /// <summary>
    /// Optional HTTP headers to include with every request to the MCP server (e.g., authentication tokens or API keys).
    /// </summary>
    /// <example>{"Authorization": "Bearer token123"}</example>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Optional Base64-encoded icon image for the server. Used as the visual identifier in the UI.
    /// </summary>
    /// <example>https://example.com/icon.png</example>
    public string? Icon { get; init; }
}