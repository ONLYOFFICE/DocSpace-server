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

public class AiSettingsDto
{
    /// <summary>
    /// Indicates whether web search is enabled for AI chat sessions.
    /// </summary>
    public bool WebSearchEnabled { get; init; }

    /// <summary>
    /// Indicates whether the web search API key needs to be reconfigured.
    /// </summary>
    public bool WebSearchNeedReset { get; init; }

    /// <summary>
    /// Indicates whether document vectorization is enabled.
    /// </summary>
    public bool VectorizationEnabled { get; init; }

    /// <summary>
    /// Indicates whether the embedding provider API key needs to be reconfigured.
    /// </summary>
    public bool VectorizationNeedReset { get; init; }

    /// <summary>
    /// Indicates whether the AI subsystem is fully configured and operational.
    /// </summary>
    public bool AiReady { get; init; }

    /// <summary>
    /// Indicates whether the AI provider API key needs to be reconfigured.
    /// </summary>
    public bool AiReadyNeedReset { get; init; }

    /// <summary>
    /// The unique identifier of the portal-level MCP server, if configured.
    /// </summary>
    public Guid? PortalMcpServerId { get; init; }

    /// <summary>
    /// The name of the embedding model used for document vectorization.
    /// </summary>
    public required string EmbeddingModel { get; init; }

    /// <summary>
    /// The tool name used by the AI assistant for knowledge base search.
    /// </summary>
    public required string KnowledgeSearchToolName { get; init; }

    /// <summary>
    /// The tool name used by the AI assistant for web search.
    /// </summary>
    public required string WebSearchToolName { get; init; }

    /// <summary>
    /// The tool name used by the AI assistant for web page crawling.
    /// </summary>
    public required string WebCrawlingToolName { get; init; }
    
    /// <summary>
    /// The tool name used by the AI to launch docx creation in the editor.
    /// </summary>
    public required string GenerateDocxToolName { get; init; }
    
    /// <summary>
    /// The tool name used by the AI assistant to launch form creation in the editor.
    /// </summary>
    public required string GenerateFormToolName { get; init; }
    
    /// <summary>
    /// The tool name used by the AI assistant to launch presentation creation in the editor.
    /// </summary>
    public required string GeneratePresentationToolName { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class AiSettingsDtoMapper
{
    public static partial AiSettingsDto MapToDto(this AiSettings source);
}
