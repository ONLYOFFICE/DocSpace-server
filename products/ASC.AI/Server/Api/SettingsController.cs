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

namespace ASC.AI.Api;

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("ai")]
public class SettingsController(AiSettingsService aiSettingsService) : ControllerBase
{
    /// <remarks>
    /// Updates the web search configuration for the AI module, including the search provider type and API key.
    /// </remarks>
    /// <summary>
    /// Set web search settings
    /// </summary>
    /// <path>api/2.0/ai/config/web-search</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Updated web search settings", typeof(WebSearchSettingsDto))]
    [HttpPut("config/web-search")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<WebSearchSettingsDto> SetWebSearchSettingsAsync(SetWebSearchConfigRequestDto inDto)
    {
        var settings = await aiSettingsService.SetWebSearchSettingsAsync(
            inDto.Body.Enabled, 
            inDto.Body.Type, 
            inDto.Body.Key);

        return settings.MapToDto();
    }
    
    /// <remarks>
    /// Returns the current web search configuration for the AI module.
    /// </remarks>
    /// <summary>
    /// Get web search settings
    /// </summary>
    /// <path>api/2.0/ai/config/web-search</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Web search settings", typeof(WebSearchSettingsDto))]
    [HttpGet("config/web-search")]
    public async Task<WebSearchSettingsDto> GetWebSearchSettingsAsync()
    {
        var settings = await aiSettingsService.GetWebSearchSettingsAsync();
        return settings.MapToDto();
    }
    
    /// <remarks>
    /// Updates the vectorization (embedding) configuration for the AI module, including the embedding provider type and API key.
    /// </remarks>
    /// <summary>
    /// Set vectorization settings
    /// </summary>
    /// <path>api/2.0/ai/config/vectorization</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Updated vectorization settings", typeof(VectorizationSettingsDto))]
    [HttpPut("config/vectorization")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<VectorizationSettingsDto> SetVectorizationSettingsAsync(SetEmbeddingConfigRequestDto inDto)
    {
        var settings = await aiSettingsService.SetVectorizationSettingsAsync(inDto.Body.Type, inDto.Body.Key);

        return settings.MapToDto();
    }
    
    /// <remarks>
    /// Returns the current vectorization (embedding) configuration for the AI module.
    /// </remarks>
    /// <summary>
    /// Get vectorization settings
    /// </summary>
    /// <path>api/2.0/ai/config/vectorization</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Vectorization settings", typeof(VectorizationSettingsDto))]
    [HttpGet("config/vectorization")]
    public async Task<VectorizationSettingsDto> GetVectorizationSettingsAsync()
    {
        var settings = await aiSettingsService.GetVectorizationSettingsAsync();
        return settings.MapToDto();
    }
    
    /// <remarks>
    /// Returns the combined AI settings including web search and vectorization configuration.
    /// </remarks>
    /// <summary>
    /// Get AI settings
    /// </summary>
    /// <path>api/2.0/ai/config</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "AI settings", typeof(AiSettingsDto))]
    [HttpGet("config")]
    public async Task<AiSettingsDto> GetAiSettingsAsync()
    {
        var settings = await aiSettingsService.GetAiSettingsAsync();
        return settings.MapToDto();
    }
}