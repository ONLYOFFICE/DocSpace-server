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
