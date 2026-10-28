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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Core.Common.AI;

[Singleton]
public class AiGatewayConfiguration(IConfiguration configuration, CoreBaseSettings coreSettings)
{
    public AiGatewaySettings Settings => field ??= configuration.GetSection("ai:gateway").Get<AiGatewaySettings>()
                                                   ?? new AiGatewaySettings();

    public bool Configured => !coreSettings.Standalone
                              && !string.IsNullOrEmpty(Settings.Url)
                              && !string.IsNullOrEmpty(Settings.Secret);

}

[Scope]
public class AiGateway(
    AiGatewayConfiguration aiGatewayConfiguration,
    IHttpClientFactory httpClientFactory,
    TenantManager tenantManager,
    ITariffService tariffService,
    UserManager userManager,
    AuthContext authContext,
    SettingsManager settingsManager,
    IFusionCache fusionCache)
{
    public const int ProviderId = -1;

    private const string ModelsCacheKey = "ai:gateway:models";
    private static readonly TimeSpan _modelsCacheDuration = TimeSpan.FromSeconds(60);

    public string Url => aiGatewayConfiguration.Settings?.Url;

    public TimeSpan ResponseTimeout => aiGatewayConfiguration.Settings.ResponseTimeout;

    public bool Configured => aiGatewayConfiguration.Configured;

    public async Task<bool> IsAiAccessEnabledAsync()
    {
        var settings = await settingsManager.LoadAsync<TenantAiAccessSettings>(tenantManager.GetCurrentTenantId());
        return settings.Enabled;
    }

    public async Task<bool> IsAiEnabledAsync()
    {
        return await IsWalletServiceEnabledAsync(TenantWalletService.AITools);
    }

    public async Task<bool> IsSearchEnabledAsync()
    {
        return await IsWalletServiceEnabledAsync(TenantWalletService.AISearch);
    }

    private async Task<bool> IsWalletServiceEnabledAsync(TenantWalletService service)
    {
        if (!Configured || !await IsAiAccessEnabledAsync())
        {
            return false;
        }

        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>(tenantManager.GetCurrentTenantId());
        return settings.EnabledServices != null && settings.EnabledServices.Contains(service);
    }

    public async Task<string> GetKeyAsync(bool allowEmpty = false)
    {
        if (!Configured)
        {
            return allowEmpty ? string.Empty : throw new AiGatewayNotConfiguredException();
        }

        if (!await IsAiEnabledAsync())
        {
            return allowEmpty ? string.Empty : throw new AiServiceDisabledException();
        }

        return await GenerateKeyAsync(allowEmpty);
    }

    public async Task<string> GetByokKeyAsync(string provider, string apiKey)
    {
        if (!Configured)
        {
            throw new AiGatewayNotConfiguredException();
        }

        if (!await IsAiEnabledAsync())
        {
            throw new AiServiceDisabledException();
        }

        return await GenerateKeyAsync(byok: (provider, apiKey));
    }

    public async Task<bool> VerifyByokKeyAsync(string provider, string apiKey)
    {
        var content = JsonContent.Create(new VerifyByokKeyRequest { Provider = provider, ApiKey = apiKey });
        var key = await GenerateKeyAsync();

        var response = await SendAsync<VerifyByokKeyResponse>(HttpMethod.Post, "/byok/verify", content, key);

        return response.Valid;
    }

    public async Task<AiPricesResponse> GetPricesAsync()
    {
        var key = await GetKeyAsync(allowEmpty: true);
        return await SendAsync<AiPricesResponse>(HttpMethod.Get, "/prices", key: key);
    }

    public async Task<RestrictedModelsResponse> GetRestrictedModelsAsync()
    {
        var key = await GenerateKeyAsync();
        return await SendAsync<RestrictedModelsResponse>(HttpMethod.Get, "/chat/models/restrictions", key: key);
    }

    public async Task<RestrictedModelsResponse> SetRestrictedModelsAsync(HashSet<string> models)
    {
        var content = JsonContent.Create(new SetRestrictedModelsRequest { Models = models });
        var key = await GenerateKeyAsync();

        await fusionCache.RemoveAsync(GetCustomerModelsCacheKey());

        return await SendAsync<RestrictedModelsResponse>(HttpMethod.Put, "/chat/models/restrictions", content, key);
    }

    public async Task<ModelsResponse> GetModelsAsync()
    {
        var key = await GetKeyAsync(allowEmpty: true);
        if (string.IsNullOrEmpty(key))
        {
            return await GetAllModelsAsync();
        }

        return await fusionCache.GetOrSetAsync<ModelsResponse>(
            GetCustomerModelsCacheKey(),
            async (_, _) => await SendAsync<ModelsResponse>(HttpMethod.Get, "/customer/models", key: key),
            opt => opt.SetDuration(_modelsCacheDuration).SetFailSafe(true));
    }

    public async Task<ModelsResponse> GetAllModelsAsync()
    {
        return await fusionCache.GetOrSetAsync<ModelsResponse>(
            ModelsCacheKey,
            async (_, _) => await SendAsync<ModelsResponse>(HttpMethod.Get, "/models"),
            opt => opt.SetDuration(_modelsCacheDuration).SetFailSafe(true));
    }

    private async Task<string> GenerateKeyAsync(bool allowEmpty = false, (string Provider, string ApiKey)? byok = null)
    {
        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantManager.GetCurrentTenantId());
        if (customerInfo == null)
        {
            return allowEmpty ? string.Empty : throw new AccountingPaymentRequiredException();
        }

        var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        if (user == null || user.Removed || user.Status == EmployeeStatus.Terminated || user.Id == Constants.LostUser.Id)
        {
            throw new SecurityException();
        }

        var payload = new
        {
            customerId = customerInfo.PortalId,
            id = user.Id,
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.Add(aiGatewayConfiguration.Settings.TokenExpiration).ToUnixTimeSeconds(),
            byok = byok.HasValue ? new { provider = byok.Value.Provider, apiKey = byok.Value.ApiKey } : null
        };

        return JsonWebToken.Encode(payload, aiGatewayConfiguration.Settings.Secret);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, HttpContent content = null, string key = null)
    {
        using var request = new HttpRequestMessage(method, $"{Url}{path}");

        if (!string.IsNullOrEmpty(key))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }

        request.Content = content;
#pragma warning disable CA2000 // HttpClient is short-lived and disposed by runtime
        var httpClient = httpClientFactory.CreateClient();
#pragma warning restore CA2000
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private string GetCustomerModelsCacheKey()
    {
        return $"{ModelsCacheKey}:{tenantManager.GetCurrentTenantId()}";
    }

    private sealed class VerifyByokKeyRequest
    {
        public required string Provider { get; init; }
        public required string ApiKey { get; init; }
    }

    private sealed record VerifyByokKeyResponse
    {
        public bool Valid { get; init; }
    }
}

public class AiGatewaySettings
{
    public string Url { get; init; }
    public string Secret { get; init; }
    public TimeSpan TokenExpiration { get; init; }
    public TimeSpan ResponseTimeout { get; init; } = TimeSpan.FromMinutes(10);
}

public class AiGatewayNotConfiguredException(string message = "AI gateway is not configured") : Exception(message);

public class AiServiceDisabledException(string message = "AI service is disabled") : Exception(message);

/// <summary>
/// The currency the AI prices are quoted in.
/// </summary>
public record CurrencyInfo
{
    /// <summary>
    /// The ISO 4217 code of the currency the prices are quoted in.
    /// </summary>
    /// <example>USD</example>
    public required string Code { get; init; }

    /// <summary>
    /// The display symbol of the currency.
    /// </summary>
    /// <example>$</example>
    public required string Symbol { get; init; }
}

/// <summary>
/// The AI price list: per-model pricing for every model kind, in a single currency.
/// </summary>
public record AiPricesResponse
{
    /// <summary>
    /// The pricing of every available chat model.
    /// </summary>
    public required IEnumerable<AiChatModelPricing> Chat { get; init; }

    /// <summary>
    /// The pricing of every available embedding model.
    /// </summary>
    public required IEnumerable<AiEmbeddingModelPricing> Embedding { get; init; }

    /// <summary>
    /// The pricing of every available image model.
    /// </summary>
    public required IEnumerable<AiImageModelPricing> Image { get; init; }

    /// <summary>
    /// The pricing of every available web search provider.
    /// </summary>
    public required IEnumerable<AiWebSearchPricing> Search { get; init; }

    public required CurrencyInfo Currency { get; init; }
}

public abstract record AiModelPricing<TPrice>
{
    /// <summary>
    /// The identifier of the model, as the provider expects it on the wire.
    /// </summary>
    /// <example>gpt-4o</example>
    public required string Id { get; init; }

    /// <summary>
    /// The gateway revision of the model, the id BYOK keys are stored under.
    /// </summary>
    public required Guid RevisionId { get; init; }

    /// <summary>
    /// The display name of the model.
    /// </summary>
    /// <example>GPT-4o</example>
    public string Alias { get; init; }

    /// <summary>
    /// The owner of the model, as reported by the provider.
    /// </summary>
    /// <example>openai</example>
    public string OwnedBy { get; init; }

    /// <summary>
    /// The providers the model can be called through: the built-in one first, then the BYOK ones.
    /// </summary>
    public required IReadOnlyList<AiProviderPricing<TPrice>> Providers { get; init; }
}

/// <summary>
/// The price of a model called through one provider.
/// </summary>
public record AiProviderPricing<TPrice>
{
    /// <summary>
    /// The provider type, the value BYOK keys store as their provider.
    /// </summary>
    /// <example>openai</example>
    public required string Id { get; init; }

    /// <summary>
    /// The display name of the provider.
    /// </summary>
    /// <example>OpenAI</example>
    public required string Name { get; init; }

    /// <summary>
    /// Whether the model is called through this provider with the built-in key.
    /// </summary>
    public bool BuiltIn { get; init; }

    public required TPrice Price { get; init; }

    /// <summary>
    /// The model page for the built-in provider, the vendor's pricing page for a BYOK one.
    /// </summary>
    /// <example>https://openai.com/api/pricing</example>
    public string Link { get; init; }
}

/// <summary>
/// The pricing of a single chat model.
/// </summary>
public record AiChatModelPricing : AiModelPricing<AiChatPrice>;

/// <summary>
/// The price of a chat model, per token.
/// </summary>
public record AiChatPrice
{
    /// <summary>
    /// The price of a single prompt token.
    /// </summary>
    /// <example>0.0000025</example>
    public decimal Prompt { get; init; }

    /// <summary>
    /// The price of a single completion token.
    /// </summary>
    /// <example>0.00001</example>
    public decimal Completion { get; init; }

    /// <summary>
    /// The price of a single prompt token read from the cache, if the model supports prompt caching.
    /// </summary>
    /// <example>0.0000002</example>
    [JsonPropertyName("prompt_cache_read")]
    public decimal? PromptCacheRead { get; init; }

    /// <summary>
    /// The price of a single prompt token written to the cache, if the model supports prompt caching.
    /// </summary>
    /// <example>0.0000025</example>
    [JsonPropertyName("prompt_cache_write")]
    public decimal? PromptCacheWrite { get; init; }

    /// <summary>
    /// The price of a single prompt token written to the cache with a one-hour lifetime, if the model supports it.
    /// </summary>
    /// <example>0.000004</example>
    [JsonPropertyName("prompt_cache_write_1h")]
    public decimal? PromptCacheWrite1H { get; init; }
}

/// <summary>
/// The pricing of a single embedding model.
/// </summary>
public record AiEmbeddingModelPricing : AiModelPricing<AiEmbeddingPrice>;

/// <summary>
/// The price of an embedding model, per token.
/// </summary>
public record AiEmbeddingPrice
{
    /// <summary>
    /// The price of a single input token.
    /// </summary>
    /// <example>0.00000002</example>
    public decimal Prompt { get; init; }
}

/// <summary>
/// The pricing of a single image model.
/// </summary>
public record AiImageModelPricing : AiModelPricing<AiImagePrice>;

/// <summary>
/// The price of an image model: per prompt token and per generated image.
/// </summary>
public record AiImagePrice
{
    /// <summary>
    /// The price of a single prompt token.
    /// </summary>
    /// <example>0.00001</example>
    public decimal Prompt { get; init; }

    /// <summary>
    /// The cost associated with the completion of a prompt in an AI model.
    /// </summary>
    /// <example>0.00001</example>
    public decimal Completion { get; init; }

    /// <summary>
    /// The price of a single generated image.
    /// </summary>
    /// <example>0.04</example>
    public decimal Image { get; init; }
}

/// <summary>
/// The pricing of a single web search provider, per request.
/// </summary>
public record AiWebSearchPricing
{
    /// <summary>
    /// The identifier of the web search provider.
    /// </summary>
    /// <example>brave</example>
    public string Id { get; init; }

    /// <summary>
    /// The provider that serves the web search requests.
    /// </summary>
    /// <example>brave</example>
    public string Provider { get; init; }

    /// <summary>
    /// The price of a single web search request.
    /// </summary>
    /// <example>0.005</example>
    public decimal Price { get; init; }

    /// <summary>
    /// The link to the pricing page of the provider.
    /// </summary>
    /// <example>https://brave.com/search/api</example>
    public string Link { get; init; }
}

public class SetRestrictedModelsRequest
{
    public required HashSet<string> Models { get; init; }
}

/// <summary>
/// The AI models the portal is not allowed to use.
/// </summary>
public record RestrictedModelsResponse
{
    /// <summary>
    /// The identifiers of the models the portal is not allowed to use.
    /// </summary>
    /// <example>["gpt-4o", "claude-3-opus"]</example>
    public required List<string> Models { get; init; }
}

[EnumExtensions]
public enum ModelTier
{
    Light,
    Standard,
    Flagship
}

public class ModelTierJsonConverter : JsonConverter<ModelTier?>
{
    public override ModelTier? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return ModelTierExtensions.TryParse(reader.GetString(), true, out var tier) ? tier : null;
        }

        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, ModelTier? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToStringFast().ToLowerInvariant());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public record ModelReasoning
{
    public bool Mandatory { get; init; }

    [JsonPropertyName("default_enabled")]
    public bool DefaultEnabled { get; init; } = true;

    [JsonPropertyName("supported_efforts")]
    public IEnumerable<string> SupportedEfforts { get; init; } = [];

    [JsonPropertyName("default_effort")]
    public string DefaultEffort { get; init; }
}

public record Model
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Alias { get; init; }
    public IEnumerable<string> Capabilities { get; init; }

    [JsonConverter(typeof(ModelTierJsonConverter))]
    public ModelTier? Tier { get; init; }

    public int? Rank { get; init; }

    public ModelReasoning Reasoning { get; init; }

    [JsonPropertyName("revision_id")]
    public required Guid RevisionId { get; init; }

    [JsonPropertyName("input_modalities")]
    public required IEnumerable<string> InputModalities { get; init; }

    [JsonPropertyName("output_modalities")]
    public required IEnumerable<string> OutputModalities { get; init; }

    [JsonPropertyName("byok_providers")]
    public IEnumerable<ModelByokProvider> ByokProviders { get; init; } = [];
}

public record ModelByokProvider
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}


public record ModelsResponse
{
    public required IReadOnlyList<Model> Data { get; init; }
}
