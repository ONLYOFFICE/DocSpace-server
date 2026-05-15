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
/// Request to update an existing custom MCP server configuration.
/// </summary>
public class UpdateServerRequestDto
{
    /// <summary>
    /// Unique identifier of the MCP server to update.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "id")]
    public Guid Id { get; init; }

    /// <summary>
    /// Updated server configuration fields.
    /// </summary>
    /// <remarks>This property encapsulates server-related details, such as endpoint, name, headers, and other optional metadata.</remarks>
    /// <example>{"name": "Updated MCP Server", "description": "Updated server description", "endpoint": "https://mcp.example.com/sse"}</example>
    [FromBody]
    public required UpdateServerRequestBody Body { get; init; }
}

/// <summary>
/// Parameters for updating an existing MCP server. All fields are optional — only provided fields will be modified.
/// </summary>
public class UpdateServerRequestBody
{
    /// <summary>
    /// New display name for the server. Only letters, numbers, underscores, and hyphens are allowed. Maximum 128 characters.
    /// </summary>
    /// <example>Updated MCP Server</example>
    [MaxLength(128)]
    public string? Name { get; init; }

    /// <summary>
    /// New human-readable description of the server's purpose. Maximum 255 characters.
    /// </summary>
    /// <example>Updated server description</example>
    [MaxLength(255)]
    public string? Description { get; init; }

    /// <summary>
    /// New base URL of the MCP server endpoint. If changed, the system will re-verify connectivity before saving.
    /// </summary>
    /// <example>https://mcp.example.com/sse</example>
    [Url]
    public string? Endpoint { get; init; }

    /// <summary>
    /// New HTTP headers to include with every request. If changed alongside the endpoint, connectivity is re-verified.
    /// </summary>
    /// <example>{"Authorization": "Bearer token123"}</example>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Set to true to update the server icon. When true, the Icon field value (or null to remove) will be applied.
    /// </summary>
    /// <example>true</example>
    public bool UpdateIcon { get; init; }

    /// <summary>
    /// New Base64-encoded icon image for the server, or null to remove the existing icon. Only applied when UpdateIcon is true.
    /// </summary>
    /// <example>https://example.com/icon.png</example>
    public string? Icon { get; init; }
}