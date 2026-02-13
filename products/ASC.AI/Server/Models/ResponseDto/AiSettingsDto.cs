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
/// The AI module settings.
/// </summary>
public class AiSettingsDto
{
    /// <summary>
    /// Specifies whether the web search feature is enabled.
    /// </summary>
    /// <example>true</example>
    public bool WebSearchEnabled { get; init; }

    /// <summary>
    /// Specifies whether the web search settings need to be reset.
    /// </summary>
    /// <example>false</example>
    public bool WebSearchNeedReset { get; init; }

    /// <summary>
    /// Specifies whether the vectorization feature is enabled.
    /// </summary>
    /// <example>true</example>
    public bool VectorizationEnabled { get; init; }

    /// <summary>
    /// Specifies whether the vectorization settings need to be reset.
    /// </summary>
    /// <example>false</example>
    public bool VectorizationNeedReset { get; init; }

    /// <summary>
    /// Specifies whether the AI module is ready for use.
    /// </summary>
    /// <example>true</example>
    public bool AiReady { get; init; }

    /// <summary>
    /// Specifies whether the AI readiness settings need to be reset.
    /// </summary>
    /// <example>false</example>
    public bool AiReadyNeedReset { get; init; }

    /// <summary>
    /// The portal MCP server ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid? PortalMcpServerId { get; init; }

    /// <summary>
    /// The name of the embedding model used for vectorization.
    /// </summary>
    /// <example>text-embedding-3-small</example>
    public required string EmbeddingModel { get; init; }

    /// <summary>
    /// The name of the knowledge search tool.
    /// </summary>
    /// <example>knowledge_search</example>
    public required string KnowledgeSearchToolName { get; init; }

    /// <summary>
    /// The name of the web search tool.
    /// </summary>
    /// <example>web_search</example>
    public required string WebSearchToolName { get; init; }

    /// <summary>
    /// The name of the web crawling tool.
    /// </summary>
    /// <example>web_crawling</example>
    public required string WebCrawlingToolName { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class AiSettingsDtoMapper
{
    public static partial AiSettingsDto MapToDto(this AiSettings source);
}
