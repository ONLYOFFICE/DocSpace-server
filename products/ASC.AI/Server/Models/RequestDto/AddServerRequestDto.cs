// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.AI.Models.RequestDto;

/// <summary>
/// Request to register a new custom MCP server.
/// </summary>
public class AddServerRequestDto
{
    /// <summary>
    /// MCP server registration parameters.
    /// </summary>
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