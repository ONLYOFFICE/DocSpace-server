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
/// The AI module settings.
/// </summary>
public class AiSettingsDto
{
    /// <summary>
    /// Indicates whether web search is enabled for AI chat sessions.
    /// </summary>
    /// <example>true</example>
    public bool WebSearchEnabled { get; init; }

    /// <summary>
    /// Indicates whether the web search API key needs to be reconfigured.
    /// </summary>
    /// <example>false</example>
    public bool WebSearchNeedReset { get; init; }

    /// <summary>
    /// Indicates whether document vectorization is enabled.
    /// </summary>
    /// <example>true</example>
    public bool VectorizationEnabled { get; init; }

    /// <summary>
    /// Indicates whether the embedding provider API key needs to be reconfigured.
    /// </summary>
    /// <example>false</example>
    public bool VectorizationNeedReset { get; init; }

    /// <summary>
    /// Indicates whether the AI subsystem is fully configured and operational.
    /// </summary>
    /// <example>true</example>
    public bool AiReady { get; init; }

    /// <summary>
    /// Indicates whether the AI provider API key needs to be reconfigured.
    /// </summary>
    /// <example>false</example>
    public bool AiReadyNeedReset { get; init; }

    /// <summary>
    /// The unique identifier of the portal-level MCP server, if configured.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid? PortalMcpServerId { get; init; }

    /// <summary>
    /// The name of the embedding model used for document vectorization.
    /// </summary>
    /// <example>text-embedding-3-small</example>
    public required string EmbeddingModel { get; init; }
    
    /// <summary>
    /// Mapping of model identifiers to human-readable aliases.
    /// </summary>
    /// <example>{"gpt-5.2": "GPT-5.2", "claude-sonnet-4-20250514": "Claude Sonnet 4"}</example>
    public required IReadOnlyDictionary<string, string> ModelAliases { get; init; }

    /// <summary>
    /// The tool name used by the AI assistant for knowledge base search.
    /// </summary>
    /// <example>knowledge_search</example>
    public required string KnowledgeSearchToolName { get; init; }

    /// <summary>
    /// The tool name used by the AI assistant for web search.
    /// </summary>
    /// <example>web_search</example>
    public required string WebSearchToolName { get; init; }

    /// <summary>
    /// The tool name used by the AI assistant for web page crawling.
    /// </summary>
    /// <example>web_crawling</example>
    public required string WebCrawlingToolName { get; init; }
    
    /// <summary>
    /// The tool name used by the AI to launch docx creation in the editor.
    /// </summary>
    /// <example>generate_docx</example>
    public required string GenerateDocxToolName { get; init; }
    
    /// <summary>
    /// The tool name used by the AI assistant to launch form creation in the editor.
    /// </summary>
    /// <example>generate_form</example>
    public required string GenerateFormToolName { get; init; }
    
    /// <summary>
    /// The tool name used by the AI assistant to launch presentation creation in the editor.
    /// </summary>
    /// <example>generate_presentation</example>
    public required string GeneratePresentationToolName { get; init; }

    /// <summary>
    /// Indicates whether the system-level AI provider is enabled.
    /// </summary>
    /// <example>true</example>
    public bool SystemAiEnabled { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class AiSettingsDtoMapper
{
    public static partial AiSettingsDto MapToDto(this AiSettings source);
}
