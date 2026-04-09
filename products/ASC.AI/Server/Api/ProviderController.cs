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
[AiFeature]
[ControllerName("ai")]
public class ProviderController(
    AiProviderService providerService,
    ApiContext apiContext,
    MessageService messageService,
    ProviderMapper providerMapper) : ControllerBase
{
    /// <summary>
    /// Add an AI provider
    /// </summary>
    /// <remarks>
    /// Registers a new AI provider for the current tenant by specifying its type, display title, API endpoint URL, and authentication key.
    /// The provider becomes available for AI chat conversations after creation. This action is rate-limited.
    /// </remarks>
    /// <path>api/2.0/ai/providers</path>
    [Tags("AI / Providers")]
    [SwaggerResponse(200, "Created AI provider details", typeof(AiProviderDto))]
    [SwaggerResponse(400, "Invalid connection data or provider with this name already exists")]
    [SwaggerResponse(403, "You don't have enough permission to manage providers")]
    [HttpPost("providers")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<AiProviderDto> AddProviderAsync(CreateProviderRequestDto inDto)
    {
        var provider = await providerService.AddProviderAsync(
            inDto.Title,
            inDto.Url,
            inDto.Key,
            inDto.Type,
            MapModelSettings(inDto.ModelSettings));

        messageService.Send(MessageAction.AIProviderCreated, MessageTarget.Create(provider.Id), provider.Title);

        return providerMapper.MapToDto(provider);
    }

    /// <summary>
    /// Get AI providers
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of AI providers configured for the current tenant.
    /// Supports pagination via the startIndex and count query parameters. The total number of providers is included in the response metadata.
    /// </remarks>
    /// <path>api/2.0/ai/providers</path>
    /// <collection>list</collection>
    [Tags("AI / Providers")]
    [SwaggerResponse(200, "Paginated list of AI providers", typeof(List<AiProviderDto>))]
    [HttpGet("providers")]
    public async Task<List<AiProviderDto>> GetProvidersAsync(PaginatedRequestDto inDto)
    {
        var totalCountTask = providerService.GetProvidersTotalCountAsync();

        var providers = await providerService.GetProvidersAsync(inDto.StartIndex, inDto.Count)
            .Select(providerMapper.MapToDto)
            .ToListAsync();

        var totalCount = await totalCountTask;

        apiContext.SetCount(providers.Count).SetTotalCount(totalCount);

        return providers;
    }

    /// <summary>
    /// Update an AI provider
    /// </summary>
    /// <remarks>
    /// Updates the configuration of an existing AI provider, including its display title, API endpoint URL, and authentication key.
    /// Only the fields provided in the request body will be updated. This action is rate-limited.
    /// </remarks>
    /// <path>api/2.0/ai/providers/{id}</path>
    [Tags("AI / Providers")]
    [SwaggerResponse(200, "Updated AI provider details", typeof(AiProviderDto))]
    [SwaggerResponse(400, "Invalid connection data or provider with this name already exists")]
    [SwaggerResponse(403, "You don't have enough permission to manage providers")]
    [SwaggerResponse(404, "The provider with the specified ID was not found")]
    [HttpPut("providers/{id}")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<AiProviderDto> UpdateProviderAsync(UpdateProviderRequestDto inDto)
    {
        var modelSettings = MapModelSettings(inDto.Body.ModelSettings);

        var provider = await providerService.UpdateProviderAsync(inDto.Id, inDto.Body.Title, inDto.Body.Url, inDto.Body.Key, modelSettings);

        messageService.Send(MessageAction.AIProviderUpdated, MessageTarget.Create(provider.Id), provider.Title);

        return providerMapper.MapToDto(provider);
    }

    /// <summary>
    /// Delete AI providers
    /// </summary>
    /// <remarks>
    /// Permanently deletes one or more AI providers by their identifiers.
    /// All specified providers are removed from the current tenant. This action cannot be undone.
    /// </remarks>
    /// <path>api/2.0/ai/providers</path>
    [Tags("AI / Providers")]
    [SwaggerResponse(204, "The providers were successfully deleted")]
    [SwaggerResponse(403, "You don't have enough permission to manage providers")]
    [HttpDelete("providers")]
    public async Task<NoContentResult> DeleteProvidersAsync(RemoveProviderRequestDto inDto)
    {
        var providers = new List<AiProvider>();

        foreach (var id in inDto.Ids)
        {
            var provider = await providerService.GetProviderAsync(id);
            if (provider != null)
            {
                providers.Add(provider);
            }
        }

        await providerService.DeleteProvidersAsync(inDto.Ids);

        foreach (var provider in providers)
        {
            messageService.Send(MessageAction.AIProviderDeleted, MessageTarget.Create(provider.Id), provider.Title);
        }

        return NoContent();
    }

    /// <summary>
    /// Get available AI provider types
    /// </summary>
    /// <remarks>
    /// Returns the list of AI provider types that are available for configuration on the current instance.
    /// Each entry includes the provider type identifier and the default API endpoint URL.
    /// </remarks>
    /// <path>api/2.0/ai/providers/available</path>
    /// <collection>list</collection>
    [Tags("AI / Providers")]
    [SwaggerResponse(200, "List of available AI provider types", typeof(List<ProviderSettingsDto>))]
    [HttpGet("providers/available")]
    public async Task<List<ProviderSettingsDto>> GetAvailableProvidersAsync()
    {
        var providers = await providerService.GetAvailableProvidersAsync();

        return providers.Select(x => x.MapToDto()).ToList();
    }

    /// <summary>
    /// Preview models for a new AI provider
    /// </summary>
    /// <remarks>
    /// Connects to the specified AI provider using the provided credentials and returns the available models
    /// with their default settings. This is used to preview models before saving the provider.
    /// Recommended models are enabled by default with configuration-defined settings.
    /// Additional models are disabled by default with empty capabilities.
    /// </remarks>
    /// <path>api/2.0/ai/providers/models/preview</path>
    /// <collection>list</collection>
    [Tags("AI / Providers")]
    [SwaggerResponse(200, "List of models with default settings", typeof(List<ModelSettingsDto>))]
    [SwaggerResponse(400, "Invalid connection data or unsupported provider type")]
    [SwaggerResponse(403, "You don't have enough permission to manage providers")]
    [HttpPost("providers/models/preview")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<List<ModelSettingsDto>> PreviewProviderModelsAsync(PreviewProviderModelsRequestDto inDto)
    {
        var models = await providerService.GetPreviewModelsAsync(inDto.Type, inDto.Url, inDto.Key);

        return models.Select(x => x.MapToDto()).ToList();
    }

    /// <summary>
    /// Get all models for a provider with their settings
    /// </summary>
    /// <remarks>
    /// Returns the full list of AI models available from a provider, including both recommended and additional models.
    /// Each model includes its current settings: enabled state, display alias, and capabilities (vision, tool calling, thinking).
    /// Recommended models are enabled by default and their alias and capabilities come from configuration.
    /// Additional models are disabled by default and can be configured by the admin.
    /// </remarks>
    /// <path>api/2.0/ai/providers/{providerId}/models</path>
    /// <collection>list</collection>
    [Tags("AI / Providers")]
    [SwaggerResponse(200, "List of models with settings", typeof(List<ModelSettingsDto>))]
    [SwaggerResponse(403, "You don't have enough permission to manage providers")]
    [SwaggerResponse(404, "Provider not found")]
    [HttpGet("providers/{providerId}/models")]
    public async Task<List<ModelSettingsDto>> GetProviderModelsAsync(GetProviderModelsRequestDto inDto)
    {
        var models = await providerService.GetModelsWithSettingsAsync(inDto.ProviderId);

        return models.Select(x => x.MapToDto()).ToList();
    }

    /// <summary>
    /// Update model settings
    /// </summary>
    /// <remarks>
    /// Updates the settings for a specific model on a provider.
    /// For recommended models, only the enabled state can be changed; alias and capabilities are managed by configuration.
    /// For additional (non-recommended) models, the enabled state, display alias, and capabilities can be configured.
    /// </remarks>
    /// <path>api/2.0/ai/providers/{providerId}/models/{modelId}</path>
    [Tags("AI / Providers")]
    [SwaggerResponse(200, "Model settings updated successfully")]
    [SwaggerResponse(403, "You don't have enough permission to manage providers")]
    [SwaggerResponse(404, "Provider not found")]
    [HttpPut("providers/{providerId}/models/{modelId}")]
    public async Task UpdateModelSettingsAsync(UpdateModelSettingsRequestDto inDto)
    {
        await providerService.UpdateModelSettingsAsync(
            inDto.ProviderId,
            inDto.ModelId,
            inDto.Body.IsEnabled,
            inDto.Body.Alias,
            inDto.Body.Capabilities);
    }

    /// <summary>
    /// Set the default AI provider
    /// </summary>
    /// <remarks>
    /// Sets the default AI provider and model for the current tenant.
    /// The specified provider and model will be used as the default for all new AI chat sessions within the tenant.
    /// </remarks>
    /// <path>api/2.0/ai/providers/default</path>
    [Tags("AI / Providers")]
    [SwaggerResponse(200, "Default provider information", typeof(DefaultProviderDto))]
    [SwaggerResponse(403, "You don't have enough permission to manage providers")]
    [SwaggerResponse(404, "Provider not found")]
    [HttpPut("providers/default")]
    public async Task<DefaultProviderDto> SetDefaultProviderAsync(SetDefaultProviderRequestDto inDto)
    {
        var result = await providerService.SetDefaultProviderAsync(inDto.ProviderId, inDto.DefaultModel);

        return result.MapToDto();
    }

    /// <summary>
    /// Get the default AI provider
    /// </summary>
    /// <remarks>
    /// Returns the default AI provider and model configured for the current tenant.
    /// Returns null if the tenant does not have any registered providers.
    /// </remarks>
    /// <path>api/2.0/ai/providers/default</path>
    [Tags("AI / Providers")]
    [SwaggerResponse(200, "Default provider information or null if not set", typeof(DefaultProviderDto))]
    [HttpGet("providers/default")]
    public async Task<DefaultProviderDto?> GetDefaultProviderAsync()
    {
        var result = await providerService.GetDefaultProviderAsync();

        return result?.MapToDto();
    }

    private static List<AiModelSettings>? MapModelSettings(HashSet<ModelSettingsItemDto>? items)
    {
        return items is not { Count: > 0 } ? null : items.Select(x => x.Map()).ToList();
    }
}
