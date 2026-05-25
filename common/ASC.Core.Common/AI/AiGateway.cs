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

[Scope]
public class AiGateway(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    TenantManager tenantManager,
    ITariffService tariffService,
    UserManager userManager,
    AuthContext authContext,
    SettingsManager settingsManager,
    CoreBaseSettings coreSettings)
{
    public const int ProviderId = -1;
    public const string ProviderTitle = "ONLYOFFICE AI";
    public string Url => Settings?.Url;

    private static AiGatewaySettings _settings;

    private AiGatewaySettings Settings => _settings ??= 
        configuration.GetSection("ai:gateway").Get<AiGatewaySettings>() ?? new AiGatewaySettings();
    
    public bool Configured => !coreSettings.Standalone && !string.IsNullOrEmpty(Settings.Url) && !string.IsNullOrEmpty(Settings.Secret);

    public async Task<bool> IsEnabledAsync()
    {
        if (!Configured)
        {
            return false;
        }
        
        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>(tenantManager.GetCurrentTenantId());
        return settings.EnabledServices != null && settings.EnabledServices.Contains(TenantWalletService.AITools);
    }
    
    public async Task<string> GetKeyAsync(bool force = false)
    {
        if (!force && !await IsEnabledAsync())
        {
            throw new InvalidOperationException("AI Gateway is not enabled");
        }

        return await GenerateKeyAsync();
    }

    public async Task<AiPricesResponse> GetPricesAsync()
    {
        return await SendAsync<AiPricesResponse>(HttpMethod.Get, "/prices", authorize: false);
    }

    public async Task<RestrictedModelsResponse> GetRestrictedModelsAsync()
    {
        return await SendAsync<RestrictedModelsResponse>(HttpMethod.Get, "/chat/models/restrictions");
    }

    public async Task<RestrictedModelsResponse> SetRestrictedModelsAsync(HashSet<string> models)
    {
        var content = JsonContent.Create(new SetRestrictedModelsRequest { Models = models });
        return await SendAsync<RestrictedModelsResponse>(HttpMethod.Put, "/chat/models/restrictions", content);
    }

    private async Task<string> GenerateKeyAsync()
    {
        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantManager.GetCurrentTenantId());
        if (customerInfo == null)
        {
            throw new AccountingPaymentRequiredException();
        }

        var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        if (user == null || user.Removed ||  user.Status == EmployeeStatus.Terminated || user.Id == Constants.LostUser.Id)
        {
            throw new SecurityException();
        }

        var payload = new
        {
            customerId = customerInfo.PortalId,
            id = user.Id,
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.Add(Settings.TokenExpiration).ToUnixTimeSeconds()
        };

        return JsonWebToken.Encode(payload, Settings.Secret);
    }
    
    private async Task<T> SendAsync<T>(HttpMethod method, string path, HttpContent content = null, bool authorize = true)
    {
        using var request = new HttpRequestMessage(method, $"{Url}{path}");

        if (authorize)
        {
            var key = await GenerateKeyAsync();
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
}

public class AiGatewaySettings
{
    public string Url { get; init; }
    public string Secret { get; init; }
    public TimeSpan TokenExpiration { get; init; }
}

public record CurrencyInfo
{
    public required string Code { get; init; }
    public required string Symbol { get; init; }
}

public record AiPricesResponse
{
    public required List<AiChatModelPricing> Chat { get; init; }
    public required List<AiEmbeddingModelPricing> Embedding { get; init; }
    public required AiWebSearchPricing WebSearch { get; init; }
    public required CurrencyInfo Currency { get; init; } = new() { Code = "USD", Symbol = "$" };
}

public record AiChatModelPricing
{
    public required string Id { get; init; }
    public string Alias { get; init; } = "GPT-5.2";
    public string OwnedBy { get; init; } = "openai";
    public string Provider { get; init; } = "OpenRouter";
    public required AiChatPrice Price { get; init; }
}

public record AiChatPrice
{
    public decimal Prompt { get; init; }
    public decimal Completion { get; init; }
}

public record AiEmbeddingModelPricing
{
    public required string Id { get; init; }
    public string Alias { get; init; } = "GPT-5.2";
    public string OwnedBy { get; init; } = "openai";
    public string Provider { get; init; } = "OpenRouter";
    public required AiEmbeddingPrice Price { get; init; }
}

public record AiEmbeddingPrice
{
    public decimal Prompt { get; init; }
}

public record AiWebSearchPricing
{
    public string Provider { get; init; } = "Exa";
    public decimal Search { get; init; }
    public decimal Contents { get; init; }
}

public class SetRestrictedModelsRequest
{
    public required HashSet<string> Models { get; init; }
}

public record RestrictedModelsResponse
{
    public required List<string> Models { get; init; }
}