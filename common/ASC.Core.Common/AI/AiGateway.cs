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

    public async Task<bool> IsAiEnabledAsync()
    {
        if (!Configured)
        {
            return false;
        }

        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>(tenantManager.GetCurrentTenantId());
        return settings.EnabledServices != null && settings.EnabledServices.Contains(TenantWalletService.AITools);
    }

    public async Task<bool> IsSearchEnabledAsync()
    {
        if (!Configured)
        {
            return false;
        }

        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>(tenantManager.GetCurrentTenantId());
        return settings.EnabledServices != null && settings.EnabledServices.Contains(TenantWalletService.AISearch);
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
        var path = string.IsNullOrEmpty(key) ? "/models" : "/customer/models";

        var cacheKey = string.IsNullOrEmpty(key)
            ? ModelsCacheKey
            : GetCustomerModelsCacheKey();

        return await fusionCache.GetOrSetAsync<ModelsResponse>(
            cacheKey,
            async (_, _) => await SendAsync<ModelsResponse>(HttpMethod.Get, path, key: key),
            opt => opt.SetDuration(_modelsCacheDuration).SetFailSafe(true));
    }

    private async Task<string> GenerateKeyAsync(bool allowEmpty = false)
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
            exp = DateTimeOffset.UtcNow.Add(aiGatewayConfiguration.Settings.TokenExpiration).ToUnixTimeSeconds()
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
    /// The provider that serves the model.
    /// </summary>
    /// <example>openai</example>
    public string Provider { get; init; }

    /// <summary>
    /// The link to the pricing page of the model.
    /// </summary>
    /// <example>https://openai.com/api/pricing</example>
    public string Link { get; init; }

    public required TPrice Price { get; init; }
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

public record Model
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Alias { get; init; }
    public IEnumerable<string> Capabilities { get; init; }

    [JsonConverter(typeof(ModelTierJsonConverter))]
    public ModelTier? Tier { get; init; }

    public int? Rank { get; init; }

    [JsonPropertyName("revision_id")]
    public required Guid RevisionId { get; init; }

    [JsonPropertyName("input_modalities")]
    public required IEnumerable<string> InputModalities { get; init; }

    [JsonPropertyName("output_modalities")]
    public required IEnumerable<string> OutputModalities { get; init; }
}


public record ModelsResponse
{
    public required IEnumerable<Model> Data { get; init; }
}
