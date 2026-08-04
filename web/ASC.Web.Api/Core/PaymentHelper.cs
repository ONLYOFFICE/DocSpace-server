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

namespace ASC.Web.Api.Core;

[Scope]
public class PaymentHelper(
    ITariffService tariffService,
    IQuotaService quotaService,
    UserManager userManager,
    SecurityContext securityContext,
    TenantManager tenantManager,
    SettingsManager settingsManager,
    MessageService messageService,
    QuotaSocketManager quotaSocketManager,
    AiGateway aiGateway,
    DocsCloudClient docsCloudClient,
    WalletStaticProvider walletStaticProvider,
    TenantWalletSettingsConfig walletSettingsConfig)
{
    public void DemandConfigured()
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }
    }

    public void DemandAiGatewayConfiguration()
    {
        if (!tariffService.IsConfigured() || !aiGateway.Configured)
        {
            throw new InvalidOperationException("Tariff service or AI gateway is not configured");
        }
    }

    public async Task DemandAdminAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }
    }

    private async Task<CustomerInfo> GetCustomerInfoRequiredAsync(int tenantId, bool refresh = false)
    {
        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId, refresh);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        return customerInfo;
    }

    public async Task DemandPayerAsync(CustomerInfo customerInfo)
    {
        if (!await IsPayerAsync(customerInfo))
        {
            throw new SecurityException("Access denied: insufficient permissions for this payment operation");
        }
    }

    public async Task DemandPayerOrOwnerAsync(Tenant tenant, CustomerInfo customerInfo)
    {
        if (!await IsPayerOrOwnerAsync(tenant, customerInfo))
        {
            throw new SecurityException("Access denied: insufficient permissions for this payment operation");
        }
    }

    public async Task<bool> IsPayerOrOwnerAsync(Tenant tenant, CustomerInfo customerInfo)
    {
        return securityContext.CurrentAccount.ID == tenant.OwnerId || await IsPayerAsync(customerInfo);
    }

    /// <summary>
    /// Ensures that the tariff service is configured, the customer exists and the current user is the payer.
    /// </summary>
    /// <returns>The customer information of the current tenant.</returns>
    public async Task<CustomerInfo> DemandCustomerPayerAsync(int tenantId, bool refresh = false)
    {
        DemandConfigured();

        var customerInfo = await GetCustomerInfoRequiredAsync(tenantId, refresh);

        await DemandPayerAsync(customerInfo);

        return customerInfo;
    }

    /// <summary>
    /// Ensures that the tariff service is configured, the current user has administrator rights and the customer exists.
    /// </summary>
    /// <returns>The tenant ID of the validated customer.</returns>
    public async Task<int> EnsureCustomerAndAdminRightsAsync()
    {
        DemandConfigured();

        await DemandAdminAsync();

        var tenantId = tenantManager.GetCurrentTenantId();

        await GetCustomerInfoRequiredAsync(tenantId);

        return tenantId;
    }

    public async Task<string> GetCurrentSubscriptionProductIdAsync(int tenantId)
    {
        var tariff = await tariffService.GetTariffAsync(tenantId);

        if (tariff.State != TariffState.Paid)
        {
            throw new BillingException("Tariff is not paid");
        }

        var mainQuotaRow = tariff.Quotas.FirstOrDefault(q => !q.Additional);
        if (mainQuotaRow == null)
        {
            throw new ItemNotFoundException("Subscription could not be found");
        }

        // Resolve the TenantQuota for the authoritative Wallet flag and ProductId
        var quota = await quotaService.GetTenantQuotaAsync(mainQuotaRow.Id);
        if (quota == null || quota.Wallet || string.IsNullOrEmpty(quota.ProductId))
        {
            throw new ArgumentException("Invalid product");
        }

        return quota.ProductId;
    }

    /// <summary>
    /// Resolves the tenant quota for the given product name, optionally requiring a specific wallet flag.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when no matching product quota is found or its wallet flag does not match.</exception>
    public async Task<TenantQuota> GetQuotaByProductNameAsync(string productName, bool? wallet = null)
    {
        var quota = (await quotaService.GetTenantQuotasAsync())
            .FirstOrDefault(q => !string.IsNullOrEmpty(q.ProductId) && q.Name == productName);

        if (quota == null || (wallet.HasValue && quota.Wallet != wallet.Value))
        {
            throw new ArgumentException("Invalid product");
        }

        return quota;
    }

    /// <summary>
    /// Loads the customer balance and returns the sub-account for the given currency
    /// (defaults to the first supported accounting currency).
    /// </summary>
    /// <exception cref="ItemNotFoundException">Thrown when the balance or the matching sub-account cannot be found.</exception>
    public async Task<SubAccount> GetSubAccountRequiredAsync(int tenantId, string currency = null, bool refresh = false)
    {
        var balance = await tariffService.GetCustomerBalanceAsync(tenantId, refresh);
        if (balance == null)
        {
            throw new ItemNotFoundException("Balance could not be found");
        }

        currency ??= tariffService.GetSupportedAccountingCurrencies().First();

        var subAccount = balance.SubAccounts.FirstOrDefault(x => x.Currency == currency);
        if (subAccount == null)
        {
            throw new ItemNotFoundException("Subaccount could not be found");
        }

        return subAccount;
    }

    /// <summary>
    /// Validates the provided service names against the tenant wallet quotas and returns the corrected service names.
    /// </summary>
    /// <exception cref="ItemNotFoundException">Thrown when a quota with the corresponding service name is hidden or not found in the database.</exception>
    public async Task<List<string>> GetCorrectServiceNamesAsync(List<string> serviceNames)
    {
        if (serviceNames is not { Count: > 0 })
        {
            return serviceNames;
        }

        var quotaList = (await tenantManager.GetTenantQuotasAsync(all: false, wallet: true)).ToList();

        var correctedList = new List<string>();
        foreach (var serviceName in serviceNames.Where(serviceName => !string.IsNullOrEmpty(serviceName)))
        {
            var (_, correctServiceName) = CheckWalletServiceName(quotaList, serviceName);
            correctedList.Add(correctServiceName);
        }

        return correctedList;
    }

    public async Task<bool> PaymentChangeAsync(int tenantId, Dictionary<string, int> quantity, ProductQuantityType productQuantityType, string currency, bool checkQuota, string customerParticipantName)
    {
        var result = await tariffService.PaymentChangeAsync(tenantId, quantity, productQuantityType, currency, checkQuota, customerParticipantName);

        if (result)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, string.Join(", ", quantity.Select(q => $"{q.Key} {q.Value}")));
        }

        return result;
    }

    public async Task<bool> UpdateNextQuantityAsync(int tenantId, Tariff tariff, int quotaId, int? nextQuantity, string productName, int? nextQuota = null)
    {
        var updated = await tariffService.UpdateNextQuantityAsync(tenantId, tariff, quotaId, nextQuantity, nextQuota);

        if (updated)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{productName} {nextQuantity}");
        }

        return updated;
    }

    public async Task<bool> TopUpDepositAsync(int tenantId, decimal amount, string currency, string customerParticipantName, string siteName)
    {
        var result = await tariffService.TopUpDepositAsync(tenantId, amount, currency, customerParticipantName, siteName, null, true);

        if (result)
        {
            messageService.Send(MessageAction.CustomerWalletToppedUp, $"{amount} {currency}");

            await quotaSocketManager.TopUpWallet(false);

            await EnsureLowBalanceThresholdAsync();
        }

        return result;
    }

    /// <summary>
    /// The wallet balance below which a low-balance notification is sent, sourced from config (not user-configurable).
    /// </summary>
    public int GetDefaultLowBalanceThreshold()
    {
        return walletSettingsConfig.LowBalanceThreshold;
    }

    // stamps a non-default TenantWalletSettings row for tenants without auto top-up configured, so the low-balance
    // poller (which only scans persisted wallet-settings rows) can discover them without scanning every active tenant
    private async Task EnsureLowBalanceThresholdAsync()
    {
        var settings = await settingsManager.LoadAsync<TenantWalletSettings>();
        if (settings.Enabled)
        {
            return;
        }

        settings.LowBalanceThreshold = GetDefaultLowBalanceThreshold();
        settings.LowBalanceNotified = false;

        await settingsManager.SaveAsync(settings);
    }

    public async Task<SubscriptionToWalletResult> SubscriptionBalanceToWalletAsync(int tenantId, string productId)
    {
        var transfer = await tariffService.SubscriptionBalanceToWalletAsync(tenantId, productId);
        if (transfer == null)
        {
            throw new BillingException("Failed to move the subscription balance to the wallet");
        }

        messageService.Send(MessageAction.SubscriptionBalanceMovedToWallet, $"{transfer.Amount} {transfer.Currency}");

        await quotaSocketManager.TopUpWallet(false);

        return transfer;
    }

    public async Task<bool> SwitchSubscriptionAsync(int tenantId, string fromProductId, string toProductId, int quantity, string customerParticipantName, string toProductName)
    {
        var result = await tariffService.SwitchSubscriptionAsync(tenantId, fromProductId, toProductId, quantity, customerParticipantName);

        if (result)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{toProductName} {quantity}");
        }

        return result;
    }

    public async Task<bool> GetDocsCloudTrialAsync(int tenantId, string quotaName)
    {
        var result = await tariffService.GetDocsCloudTrialAsync(tenantId);

        if (result)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, quotaName);
        }

        return result;
    }

    public async Task<ServicePayment> MakeAiCreditAsync(int tenantId, decimal amount, string currency, string customerParticipantName, string serviceName)
    {
        var result = await tariffService.MakeAiCreditAsync(tenantId, amount, currency, customerParticipantName, metadata: null);

        if (result != null)
        {
            messageService.Send(MessageAction.CustomerOperationPerformed, null, $"{serviceName} {amount} {currency}");

            await EnableAiToolsServiceAsync();
        }

        return result;
    }

    public async Task<TenantWalletServiceSettings> ChangeWalletServiceStateAsync(TenantWalletService service, bool enabled)
    {
        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>();

        settings.EnabledServices ??= [];

        if (enabled && !settings.EnabledServices.Contains(service))
        {
            if (service == TenantWalletService.AISearch && !settings.EnabledServices.Contains(TenantWalletService.AITools))
            {
                throw new InvalidOperationException("AI Tools service must be enabled before Search");
            }

            settings.EnabledServices.Add(service);
        }

        if (!enabled && settings.EnabledServices.Contains(service))
        {
            settings.EnabledServices.Remove(service);

            if (service == TenantWalletService.AITools && settings.EnabledServices.Contains(TenantWalletService.AISearch))
            {
                settings.EnabledServices.Remove(TenantWalletService.AISearch);
            }
        }

        if (settings.EnabledServices.Count == 0)
        {
            settings.EnabledServices = null;
        }

        var result = await settingsManager.SaveAsync(settings);

        if (!result)
        {
            throw new InvalidOperationException("Failed to save tenant wallet service settings");
        }

        messageService.Send(MessageAction.CustomerWalletServicesSettingsUpdated);

        if (service == TenantWalletService.AITools)
        {
            await quotaSocketManager.ChangeAiConfigAsync();
        }

        return settings;
    }

    public async Task<AiPricesDto> GetAiPricesAsync()
    {
        var aiPrices = await aiGateway.GetPricesAsync();
        var icons = new Dictionary<string, string>();

        var providers = aiPrices.Chat.Select(m => m.OwnedBy.ToLower())
            .Concat(aiPrices.Image.Select(m => m.OwnedBy.ToLower()))
            .Distinct();

        var searchTypes = aiPrices.Search.Select(s => s.Id).Distinct();

        foreach (var provider in providers)
        {
            icons[provider] = await walletStaticProvider.GetImageAsync(provider);
        }

        foreach (var searchType in searchTypes)
        {
            icons[searchType] = await walletStaticProvider.GetImageAsync(searchType);
        }

        var chat = aiPrices.Chat.Select(m => new AiEntryPricingDto<AiChatPriceDto>
        {
            Id = m.Id,
            Image = icons[m.OwnedBy.ToLower()],
            Alias = m.Alias,
            Provider = m.Provider,
            Price = new AiChatPriceDto { Prompt = m.Price.Prompt, Completion = m.Price.Completion },
            Link = m.Link
        }).ToList();

        var embeddingImage = await walletStaticProvider.GetImageAsync("embedding");

        var embedding = aiPrices.Embedding.Select(e => new AiEntryPricingDto<AiEmbeddingPriceDto>
        {
            Id = e.Id,
            Alias = e.Alias,
            Provider = e.Provider,
            Image = embeddingImage,
            Price = new AiEmbeddingPriceDto { Prompt = e.Price.Prompt },
            Link = e.Link
        }).ToList();

        var image = aiPrices.Image.Select(m => new AiEntryPricingDto<AiImagePriceDto>
        {
            Id = m.Id,
            Image = icons[m.OwnedBy.ToLower()],
            Alias = m.Alias,
            Provider = m.Provider,
            Price = new AiImagePriceDto { Prompt = m.Price.Prompt, Image = m.Price.Image },
            Link = m.Link
        }).ToList();

        var search = aiPrices.Search.Select(s => new AiEntryPricingDto<decimal>
        {
            Id = s.Id,
            Alias = Resource.ResourceManager.GetString($"AccountingCustomerOperationServiceDesc_{s.Id}"),
            Image = icons[s.Id],
            Provider = s.Provider,
            Price = s.Price,
            Link = s.Link
        }).ToList();

        return new AiPricesDto
        {
            Chat = chat,
            Embedding = embedding,
            Image = image,
            WebSearch = search,
            Currency = aiPrices.Currency
        };
    }

    public async Task<RestrictedModelsResponse> SetRestrictedAiModelsAsync(HashSet<string> models)
    {
        var result = await aiGateway.SetRestrictedModelsAsync(models);

        messageService.Send(MessageAction.CustomerWalletServicesSettingsUpdated);

        return result;
    }

    public async Task<DocsCloudConfig> UpdateTenantConfigAsync(string portalId, DocsCloudConfig docsCloudConfig)
    {
        var result = await docsCloudClient.UpdateTenantConfigAsync(portalId, docsCloudConfig);

        messageService.Send(MessageAction.DocsCloudConfigUpdated);

        return result;
    }

    private async Task<bool> IsPayerAsync(CustomerInfo customerInfo)
    {
        var payer = await userManager.GetUserByEmailAsync(customerInfo?.Email);

        return payer.Id != ASC.Core.Users.Constants.LostUser.Id && securityContext.CurrentAccount.ID == payer.Id;
    }

    private async Task EnableAiToolsServiceAsync()
    {
        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>();

        if (settings.EnabledServices?.Contains(TenantWalletService.AITools) == true)
        {
            return;
        }

        // Delegate to the canonical enable path so the save-failure guard, list normalization,
        // audit message and AI-config socket signal stay in a single place.
        await ChangeWalletServiceStateAsync(TenantWalletService.AITools, true);
    }

    /// <summary>
    /// Validates the service name and returns the corresponding tenant wallet service with the correct service name
    /// </summary>
    /// <remarks>
    /// Checks if the provided service name matches any tenant quota service name and verifies that the corresponding tenant ID is a valid TenantWalletService enum value.
    /// </remarks>
    /// <param name="quotaList">The tenant wallet quotas to validate against</param>
    /// <param name="serviceName">The service name to validate</param>
    /// <return>The corresponding TenantWalletService enum value and correct service name</return>
    /// <exception cref="ItemNotFoundException">Thrown when the quota with the corresponding service name is hidden or not found in the database.</exception>
    private static (TenantWalletService, string) CheckWalletServiceName(List<TenantQuota> quotaList, string serviceName)
    {
        var selectedQuota = quotaList.FirstOrDefault(x =>
            x.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));

        // for testing purposes
        if (selectedQuota == null)
        {
            serviceName += "-1-hour";
            selectedQuota = quotaList.FirstOrDefault(x =>
                x.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
        }

        if (selectedQuota != null && Enum.IsDefined(typeof(TenantWalletService), selectedQuota.TenantId))
        {
            return ((TenantWalletService)selectedQuota.TenantId, serviceName);
        }

        throw new ItemNotFoundException("Service could not be found");
    }
}
