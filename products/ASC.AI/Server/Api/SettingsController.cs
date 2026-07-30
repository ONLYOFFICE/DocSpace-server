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
[InternalRoute]
[ApiController]
[ControllerName("ai")]
public class SettingsController(AiSettingsService aiSettingsService) : ControllerBase
{
    /// <remarks>
    /// Configures the document vectorization embedding provider.
    /// </remarks>
    /// <summary>Set the vectorization settings</summary>
    /// <path>api/2.0/ai/config/vectorization</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Vectorization settings", typeof(VectorizationSettingsDto))]
    [HttpPut("config/vectorization")]
    [AiFeature]
    public async Task<VectorizationSettingsDto> SetVectorizationSettingsAsync(SetEmbeddingConfigRequestDto inDto)
    {
        var settings = await aiSettingsService.SetVectorizationSettingsAsync(inDto.Body.Type, inDto.Body.Key);

        return settings.MapToDto();
    }

    /// <remarks>
    /// Returns the document vectorization settings.
    /// </remarks>
    /// <summary>Get the vectorization settings</summary>
    /// <path>api/2.0/ai/config/vectorization</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "Vectorization settings", typeof(VectorizationSettingsDto))]
    [HttpGet("config/vectorization")]
    [AiFeature]
    public async Task<VectorizationSettingsDto> GetVectorizationSettingsAsync()
    {
        var settings = await aiSettingsService.GetVectorizationSettingsAsync();
        return settings.MapToDto();
    }

    /// <remarks>
    /// Returns the AI module settings.
    /// </remarks>
    /// <summary>Get the AI settings</summary>
    /// <path>api/2.0/ai/config</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "AI settings", typeof(AiSettingsDto))]
    [HttpGet("config")]
    public async Task<AiSettingsDto> GetAiSettingsAsync()
    {
        var settings = await aiSettingsService.GetAiSettingsAsync();
        return settings.MapToDto();
    }

    /// <remarks>
    /// Returns the per-user AI settings.
    /// </remarks>
    /// <summary>Get the user AI settings</summary>
    /// <path>api/2.0/ai/config/user</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "User AI settings", typeof(AiUserSettingsDto))]
    [HttpGet("config/user")]
    public async Task<AiUserSettingsDto> GetAiUserSettingsAsync()
    {
        var settings = await aiSettingsService.GetAiUserSettingsAsync();
        return settings.MapToDto();
    }

    /// <remarks>
    /// Updates the per-user AI settings.
    /// </remarks>
    /// <summary>Set the user AI settings</summary>
    /// <path>api/2.0/ai/config/user</path>
    [Tags("AI / Settings")]
    [SwaggerResponse(200, "User AI settings", typeof(AiUserSettingsDto))]
    [HttpPut("config/user")]
    public async Task<AiUserSettingsDto> SetAiUserSettingsAsync([FromBody] SetAiUserSettingsRequestDto inDto)
    {
        var settings = await aiSettingsService.SetAiUserSettingsAsync(inDto.ChatRecommendedModelVisible);
        return settings.MapToDto();
    }
}