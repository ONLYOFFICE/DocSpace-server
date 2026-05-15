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

using ASC.AI.Core.Chat.Tool;

namespace ASC.AI.Models.ResponseDto;

/// <summary>
/// The tool call message content.
/// </summary>
public class ToolContentDto : MessageContentDto
{
    /// <summary>
    /// The content type.
    /// </summary>
    /// <example>1</example>
    public override MessageContentType Type => MessageContentType.Tool;

    /// <summary>
    /// The name of the tool that was invoked by the AI assistant.
    /// </summary>
    /// <example>search_documents</example>
    public required string Name { get; init; }

    /// <summary>
    /// The key-value pairs of arguments passed to the tool, or null if the tool accepts no arguments.
    /// </summary>
    public IDictionary<string, object?>? Arguments { get; init; }

    /// <summary>
    /// The result returned by the tool after execution, or null if the tool has not yet completed.
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// The metadata about the MCP server that provides this tool, or null for built-in tools.
    /// </summary>
    public McpServerInfo? McpServerInfo { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class ToolContentDtoMapper
{
    public static partial ToolContentDto MapToDto(this ToolCallMessageContent source);
}
