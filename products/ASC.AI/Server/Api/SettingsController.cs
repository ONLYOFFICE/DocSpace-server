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

namespace ASC.AI.Api;

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("ai")]
public class SettingsController(AiSettingsService aiSettingsService) : ControllerBase
{
    /// <summary>
    /// Update web search settings
    /// </summary>
    /// <remarks>
    /// Configures the web search integration for AI chat sessions at the portal level.
    /// Allows enabling or disabling web search, selecting the search engine type, and providing the API key for the chosen engine.
    /// Only portal administrators can modify these settings.
    /// </remarks>
    /// <path>api/2.0/ai/config/web-search</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Updated web search settings", typeof(WebSearchSettingsDto))]
    [HttpPut("config/web-search")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    [AiFeature]
    public async Task<WebSearchSettingsDto> SetWebSearchSettingsAsync(SetWebSearchConfigRequestDto inDto)
    {
        var settings = await aiSettingsService.SetWebSearchSettingsAsync(
            inDto.Body.Enabled, 
            inDto.Body.Type, 
            inDto.Body.Key);

        return settings.MapToDto();
    }
    
    /// <summary>
    /// Get web search settings
    /// </summary>
    /// <remarks>
    /// Retrieves the current web search integration settings for AI chat sessions,
    /// including whether web search is enabled, the configured search engine type, and whether the API key needs to be reset.
    /// </remarks>
    /// <path>api/2.0/ai/config/web-search</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Current web search settings", typeof(WebSearchSettingsDto))]
    [HttpGet("config/web-search")]
    [AiFeature]
    public async Task<WebSearchSettingsDto> GetWebSearchSettingsAsync()
    {
        var settings = await aiSettingsService.GetWebSearchSettingsAsync();
        return settings.MapToDto();
    }
    
    /// <summary>
    /// Update vectorization settings
    /// </summary>
    /// <remarks>
    /// Configures the embedding provider used for document vectorization at the portal level.
    /// Vectorization enables semantic search and knowledge retrieval capabilities in AI chat sessions.
    /// Allows selecting the embedding provider type and providing the API key for the chosen provider.
    /// Only portal administrators can modify these settings.
    /// </remarks>
    /// <path>api/2.0/ai/config/vectorization</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Updated vectorization settings", typeof(VectorizationSettingsDto))]
    [HttpPut("config/vectorization")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    [AiFeature]
    public async Task<VectorizationSettingsDto> SetVectorizationSettingsAsync(SetEmbeddingConfigRequestDto inDto)
    {
        var settings = await aiSettingsService.SetVectorizationSettingsAsync(inDto.Body.Type, inDto.Body.Key);

        return settings.MapToDto();
    }
    
    /// <summary>
    /// Get vectorization settings
    /// </summary>
    /// <remarks>
    /// Retrieves the current embedding provider settings used for document vectorization,
    /// including the configured provider type and whether the API key needs to be reset.
    /// </remarks>
    /// <path>api/2.0/ai/config/vectorization</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Current vectorization settings", typeof(VectorizationSettingsDto))]
    [HttpGet("config/vectorization")]
    [AiFeature]
    public async Task<VectorizationSettingsDto> GetVectorizationSettingsAsync()
    {
        var settings = await aiSettingsService.GetVectorizationSettingsAsync();
        return settings.MapToDto();
    }
    
    /// <summary>
    /// Get AI settings
    /// </summary>
    /// <remarks>
    /// Retrieves the combined AI configuration for the current portal, including the status of web search,
    /// vectorization, and AI readiness, along with tool names and the portal MCP server identifier.
    /// </remarks>
    /// <path>api/2.0/ai/config</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Current AI settings", typeof(AiSettingsDto))]
    [HttpGet("config")]
    public async Task<AiSettingsDto> GetAiSettingsAsync()
    {
        var settings = await aiSettingsService.GetAiSettingsAsync();
        return settings.MapToDto();
    }

    /// <summary>
    /// Update per-user AI settings
    /// </summary>
    /// <remarks>
    /// Updates the current user's AI recommended model banner visibility preferences.
    /// Each user's settings are stored independently.
    /// </remarks>
    /// <path>api/2.0/ai/config/user</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Updated per-user AI settings", typeof(AiUserSettingsDto))]
    [HttpPut("config/user")]
    public async Task<AiUserSettingsDto> SetAiUserSettingsAsync(SetAiUserSettingsRequestDto inDto)
    {
        var settings = await aiSettingsService.SetAiUserSettingsAsync(inDto.Body.ChatRecomendedModelVisible);
        return settings.MapToDto();
    }
}