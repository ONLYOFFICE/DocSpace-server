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
    SettingsManager settingsManager)
{
    public const int ProviderId = -1;
    public const string ProviderTitle = "ONLYOFFICE AI";
    public string Url => Settings?.Url;

    private static AiGatewaySettings _settings;

    private AiGatewaySettings Settings => _settings ??= 
        configuration.GetSection("ai:gateway").Get<AiGatewaySettings>() ?? new AiGatewaySettings();

    public async Task<bool> IsEnabledAsync()
    {
        if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(Settings?.Secret))
        {
            return false;
        }
        
        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>(tenantManager.GetCurrentTenantId());
        return settings.EnabledServices != null && settings.EnabledServices.Contains(TenantWalletService.AITools);
    }
    
    public async Task<string> GetKeyAsync()
    {
        if (!await IsEnabledAsync())
        {
            throw new InvalidOperationException("AI Gateway is not enabled");
        }

        return await GenerateKeyAsync();
    }

    public async Task<AiPricesResponse> GetPricesAsync()
    {
        var key = await GenerateKeyAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{Url}/prices");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AiPricesResponse>();
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
}

public class AiGatewaySettings
{
    public string Url { get; init; }
    public string Secret { get; init; }
    public TimeSpan TokenExpiration { get; init; }
}

public record AiPricesResponse
{
    public required List<AiChatModelPricing> Chat { get; init; }
    public required List<AiEmbeddingModelPricing> Embedding { get; init; }
    public required AiWebSearchPricing WebSearch { get; init; }
}

public record AiChatModelPricing
{
    public required string Id { get; init; }
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
    public required AiEmbeddingPrice Price { get; init; }
}

public record AiEmbeddingPrice
{
    public decimal Prompt { get; init; }
}

public record AiWebSearchPricing
{
    public decimal Search { get; init; }
    public decimal Contents { get; init; }
}