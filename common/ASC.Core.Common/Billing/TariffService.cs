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

namespace ASC.Core.Billing;

[Singleton]
public class TenantExtraConfig(CoreBaseSettings coreBaseSettings, LicenseReaderConfig licenseReaderConfig)
{
    public bool Saas => !coreBaseSettings.Standalone;

    public bool Enterprise => coreBaseSettings.Standalone && !string.IsNullOrEmpty(licenseReaderConfig.LicensePath);

    public bool Developer => Enterprise && licenseReaderConfig.LicenseType == LicenseType.Developer;

    public bool Opensource => coreBaseSettings.Standalone && string.IsNullOrEmpty(licenseReaderConfig.LicensePath);
}


[Scope(typeof(ITariffService))]
public class TariffService(
    IQuotaService quotaService,
    ITenantService tenantService,
    CoreBaseSettings coreBaseSettings,
    CoreSettings coreSettings,
    IConfiguration configuration,
    IDbContextFactory<CoreDbContext> coreDbContextManager,
    ICache cache,
    IFusionCache hybridCache,
    IDistributedLockProvider distributedLockProvider,
    ILogger<TariffService> logger,
    BillingClient billingClient,
    AccountingClient accountingClient,
    DocsCloudClient docsCloudClient,
    IServiceProvider serviceProvider,
    ResiliencePipelineProvider<string> resiliencePipelineProvider,
    TenantExtraConfig tenantExtraConfig)
    : ITariffService
{
    private static readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _standaloneCacheExpiration = TimeSpan.FromMinutes(15);
    private TimeSpan _cacheExpiration = _defaultCacheExpiration;

    private const int DefaultTrialPeriod = 30;
    private const int MaxTopUpAttempts = 5;

    private static readonly TimeSpan _participantServiceLimitsCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _participantServiceLimitsFailureCacheDuration = TimeSpan.FromMinutes(1);
    // The user lists wait this long for the limits before they are shown with the last ones known, and at most the hard
    // timeout when none are known yet; the read then finishes in the background and fills the cache.
    private static readonly TimeSpan _serviceLimitsFactorySoftTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _serviceLimitsFactoryHardTimeout = TimeSpan.FromSeconds(3);
    // How long the last limits known stay usable while the accounting service cannot be reached.
    private static readonly TimeSpan _serviceLimitsFailSafeMaxDuration = TimeSpan.FromHours(1);
    // Outlives every entry of a version; when it expires anyway, the next read simply starts a new one.
    private static readonly TimeSpan _serviceLimitsCacheVersionDuration = TimeSpan.FromDays(1);
    private const int ParticipantServiceLimitsPageSize = 100;
    private const int ParticipantServiceLimitsDefaultPageSize = 25;
    private static readonly TimeSpan _subscriptionExtensionTolerance = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _productPriceCacheDuration = TimeSpan.FromHours(1);
    private static readonly TimeSpan _productPriceMemoryCacheDuration = TimeSpan.FromMinutes(5);

    //private readonly int _activeUsersMin;
    //private readonly int _activeUsersMax;

    private int PaymentDelay => PaymentConfiguration.Delay;
    private bool TrialEnabled => PaymentConfiguration.TrialEnabled;

    private PaymentConfiguration PaymentConfiguration => field ??= configuration.GetSection("core:payment").Get<PaymentConfiguration>() ?? new PaymentConfiguration();

    public async Task<Tariff> GetTariffAsync(int tenantId, bool withRequestToPaymentSystem = true, bool refresh = false)
    {
        //single tariff for all portals
        if (coreBaseSettings.Standalone)
        {
            tenantId = -1;
        }

        var key = GetTariffCacheKey(tenantId);
        var tariff = refresh ? null : await GetFromCache<Tariff>(key);

        if (tariff == null)
        {
            await using (await distributedLockProvider.TryAcquireLockAsync($"{key}_lock"))
            {
                tariff = refresh ? null : await GetFromCache<Tariff>(key);
                if (tariff != null)
                {
                    tariff = await CalculateTariffAsync(tenantId, tariff);
                    return tariff;
                }

                tariff = await GetBillingInfoAsync(tenantId) ?? await CreateDefaultAsync();
                tariff = await CalculateTariffAsync(tenantId, tariff);
                await InsertToCache(tenantId, tariff);

                if (billingClient.Configured && withRequestToPaymentSystem)
                {
                    try
                    {
                        var currentPayments = await billingClient.GetCurrentPaymentsAsync(await coreSettings.GetKeyAsync(tenantId), refresh);
                        if (currentPayments.Length == 0)
                        {
                            throw new BillingNotFoundException("Empty PaymentLast");
                        }

                        var asynctariff = await CreateDefaultAsync(true);
                        string email = null;
                        var tenantQuotas = await quotaService.GetTenantQuotasAsync();

                        foreach (var currentPayment in currentPayments.OrderBy(r => r.EndDate))
                        {
                            var quota = tenantQuotas.SingleOrDefault(q => q.ProductId == currentPayment.ProductId.ToString());
                            if (quota == null)
                            {
                                throw new InvalidOperationException($"Quota with id {currentPayment.ProductId} not found for portal {await coreSettings.GetKeyAsync(tenantId)}.");
                            }

                            asynctariff.Id = Math.Max(asynctariff.Id, currentPayment.PaymentId);

                            DateTime? quotaDueDate = null;
                            int? nextQuantity = null;
                            int? nextQuota = null;
                            if (quota.Wallet)
                            {
                                quotaDueDate = currentPayment.EndDate;
                                var existingQuota = tariff.Quotas.FirstOrDefault(x => x.Id == quota.TenantId);
                                if (existingQuota != null && existingQuota.DueDate == currentPayment.EndDate && existingQuota.Quantity == currentPayment.Quantity)
                                {
                                    nextQuantity = existingQuota.NextQuantity;
                                    nextQuota = existingQuota.NextQuota;
                                }
                                if (quota is { Additional: false })
                                {
                                    var paymentEndDate = 9999 <= currentPayment.EndDate.Year ? DateTime.MaxValue : currentPayment.EndDate;
                                    asynctariff.DueDate = DateTime.Compare(asynctariff.DueDate, paymentEndDate) < 0 ? asynctariff.DueDate : paymentEndDate;
                                }
                            }
                            else
                            {
                                var paymentEndDate = 9999 <= currentPayment.EndDate.Year ? DateTime.MaxValue : currentPayment.EndDate;

                                // track the DocsCloudTrial expiration per-quota so it becomes Overdue after the trial ends,
                                // instead of being folded into the tariff-level DueDate (which would expire the whole tariff)
                                if (quota.Additional)
                                {
                                    quotaDueDate = paymentEndDate;
                                }
                                else
                                {
                                    asynctariff.DueDate = DateTime.Compare(asynctariff.DueDate, paymentEndDate) < 0 ? asynctariff.DueDate : paymentEndDate;
                                }
                            }

                            asynctariff.Quotas = asynctariff.Quotas.Where(r => r.Id != quota.TenantId).ToList();
                            asynctariff.Quotas.Add(new Quota(quota.TenantId, currentPayment.Quantity, quota.Additional, quota.Wallet, quotaDueDate, nextQuantity, nextQuota));
                            email = currentPayment.PaymentEmail;
                        }

                        // need sort by additional, then by wallet so the non-wallet base plan is the aggregation base
                        asynctariff.Quotas = asynctariff.Quotas.OrderBy(q => q.Additional).ThenBy(q => q.Wallet).ToList();

                        if (asynctariff.Quotas.All(q => q.Additional))
                        {
                            if (tariff.Id != 0 && tariff.State >= TariffState.Paid && !await IsFreeTariffAsync(tariff))
                            {
                                throw new BillingNotFoundException($"Payment {tariff.Id} not found. Only additional payments available");
                            }

                            await AddInitialQuotaAsync(asynctariff, tenantId);
                        }

                        if (asynctariff.Id == tariff.Id)
                        {
                            asynctariff.OverdueQuotas = tariff.OverdueQuotas;
                        }

                        TenantQuota updatedQuota = null;

                        foreach (var quota in asynctariff.Quotas)
                        {
                            var tenantQuota = tenantQuotas.SingleOrDefault(q => q.TenantId == quota.Id);

                            tenantQuota *= quota.Quantity;

                            tenantQuota.DueDate = quota.DueDate;

                            updatedQuota += tenantQuota;
                        }

                        if (updatedQuota != null)
                        {
                            await updatedQuota.CheckAsync(serviceProvider);
                        }

                        if (!string.IsNullOrEmpty(email))
                        {
                            asynctariff.CustomerId = email;
                        }

                        if (await SaveBillingInfoAsync(tenantId, asynctariff))
                        {
                            asynctariff = await CalculateTariffAsync(tenantId, asynctariff);
                            tariff = asynctariff;
                        }

                        await InsertToCache(tenantId, tariff);
                    }
                    catch (BillingNotFoundException billingNotFoundException)
                    {
                        if (tariff.Id != 0 && tariff.State == TariffState.Paid && !await IsFreeTariffAsync(tariff))
                        {
                            LogError(billingNotFoundException, tenantId.ToString());

                            tariff.DueDate = DateTime.Today.AddDays(-1);

                            if (await SaveBillingInfoAsync(tenantId, tariff))
                            {
                                tariff = await CalculateTariffAsync(tenantId, tariff);
                                await InsertToCache(tenantId, tariff);
                            }
                        }
                    }
                    catch (Exception error)
                    {
                        LogError(error, tenantId.ToString());
                    }

                    if (tariff.Id == 0)
                    {
                        var asynctariff = await CreateDefaultAsync();

                        if (!await IsFreeTariffAsync(tariff))
                        {
                            asynctariff.DueDate = DateTime.Today.AddDays(-1);
                            asynctariff.State = TariffState.NotPaid;
                        }

                        if (await SaveBillingInfoAsync(tenantId, asynctariff))
                        {
                            asynctariff = await CalculateTariffAsync(tenantId, asynctariff);
                            tariff = asynctariff;
                        }

                        await InsertToCache(tenantId, tariff);
                    }
                }
                else if (tenantExtraConfig.Enterprise && tariff.Id == 0 && tariff.LicenseDate == DateTime.MaxValue)
                {
                    var defaultQuota = await quotaService.GetTenantQuotaAsync(Tenant.DefaultTenant);

                    var quota = new TenantQuota(defaultQuota) { Name = "start_trial", Trial = true, TenantId = -1000 };

                    await quotaService.SaveTenantQuotaAsync(quota);

                    tariff = new Tariff { Quotas = [new Quota(quota.TenantId, 1)], DueDate = DateTime.UtcNow.AddDays(DefaultTrialPeriod) };

                    await SetTariffAsync(Tenant.DefaultTenant, tariff, [quota]);
                    await InsertToCache(tenantId, tariff);
                }
            }
        }
        else
        {
            tariff = await CalculateTariffAsync(tenantId, tariff);
        }

        return tariff;
    }

    private async Task<IEnumerable<string>> CheckQuotaAndGetProductIds(int tenantId, Dictionary<string, int> quantity)
    {
        var allQuotas = (await quotaService.GetTenantQuotasAsync()).Where(q => !string.IsNullOrEmpty(q.ProductId)).ToList();
        var newQuotas = quantity.Keys.Select(name => allQuotas.Find(q => q.Name == name)).ToList();

        var tariff = await GetTariffAsync(tenantId);

        var quotas = tariff.Quotas
            .Where(quota => !quota.DueDate.HasValue || quota.DueDate.Value > DateTime.UtcNow)
            .ToList();

        // update the quantity of present quotas
        TenantQuota updatedQuota = null;
        foreach (var tariffRow in quotas)
        {
            var quotaId = tariffRow.Id;
            var qty = tariffRow.Quantity;

            var quota = await quotaService.GetTenantQuotaAsync(quotaId);

            var mustUpdateQuota = newQuotas.FirstOrDefault(q => q.TenantId == quota.TenantId);
            if (mustUpdateQuota != null)
            {
                qty = quantity[mustUpdateQuota.Name];
            }

            quota *= qty;

            quota.DueDate = tariffRow.DueDate;

            updatedQuota += quota;
        }

        // add new quotas
        var addedQuotas = newQuotas.Where(q => !quotas.Exists(t => t.Id == q.TenantId));
        foreach (var addedQuota in addedQuotas)
        {
            var qty = quantity[addedQuota.Name];

            var quota = addedQuota;

            quota *= qty;

            updatedQuota += quota;
        }

        if (updatedQuota != null)
        {
            await updatedQuota.CheckAsync(serviceProvider);
        }

        var productIds = newQuotas.Select(q => q.ProductId);

        return productIds;
    }

    private async Task<IEnumerable<string>> GetProductIds(Dictionary<string, int> quantity)
    {
        var productIds = (await quotaService.GetTenantQuotasAsync())
            .Where(q => !string.IsNullOrEmpty(q.ProductId) && quantity.ContainsKey(q.Name))
            .Select(q => q.ProductId);

        return productIds;
    }

    public async Task<bool> PaymentChangeAsync(int tenantId, Dictionary<string, int> quantity, ProductQuantityType productQuantityType, string currency, bool checkQuota, string customerParticipantName, Dictionary<string, string> metadata = null, bool throwIfFailure = false)
    {
        if (quantity == null || quantity.Count == 0 || !billingClient.Configured)
        {
            return false;
        }

        var productIds = checkQuota ? await CheckQuotaAndGetProductIds(tenantId, quantity) : await GetProductIds(quantity);

        bool changed;

        try
        {
            var portalId = await coreSettings.GetKeyAsync(tenantId);

            changed = await billingClient.ChangePaymentAsync(portalId, productIds, quantity.Values, productQuantityType, currency, customerParticipantName, metadata);

            if (changed)
            {
                await ClearCacheAsync(tenantId);

                await docsCloudClient.ClearCacheAsync(portalId);
            }
        }
        catch (Exception error)
        {
            // a caller that asked for the exception reports it itself, at the level it considers right:
            // the renewal service treats a timeout as an unknown outcome to re-check, not as an error
            if (throwIfFailure)
            {
                throw;
            }

            logger.ErrorWithException(error);

            return false;
        }

        if (!changed && throwIfFailure)
        {
            throw new BillingException("Payment change was declined");
        }

        return changed;
    }

    /// <summary>
    /// Asks the billing service itself, bypassing every cache, whether the subscription to <paramref name="productId"/>
    /// now ends after <paramref name="previousDueDate"/> - i.e. whether a payment change whose response was lost
    /// has actually been applied. Throws <see cref="BillingException"/> when the billing service gives no answer:
    /// the outcome is then still unknown.
    /// </summary>
    public async Task<bool> IsSubscriptionExtendedAsync(int tenantId, string productId, DateTime previousDueDate)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);

        var currentPayments = await billingClient.GetCurrentPaymentsAsync(portalId, true);

        // MySQL datetime keeps no fractional seconds and rounds them, so the stored due date can differ from the
        // billing end date of an unchanged subscription by up to half a second either way. A renewal moves it by a
        // whole subscription period, so anything a second or more later is the renewal.
        var extended = currentPayments.Any(payment =>
            payment.ProductId.ToString() == productId &&
            payment.EndDate - previousDueDate >= _subscriptionExtensionTolerance);

        if (extended)
        {
            await ClearCacheAsync(tenantId);

            await docsCloudClient.ClearCacheAsync(portalId);
        }

        return extended;
    }

    public async Task<PaymentCalculation> PaymentCalculateAsync(int tenantId, Dictionary<string, int> quantity, ProductQuantityType productQuantityType, string currency)
    {
        if (quantity == null || quantity.Count == 0 || !billingClient.Configured)
        {
            return null;
        }

        var productIds = await GetProductIds(quantity);

        try
        {
            var response = await billingClient.CalculatePaymentAsync(await coreSettings.GetKeyAsync(tenantId), productIds, quantity.Values, productQuantityType, currency);

            return response;
        }
        catch (Exception error)
        {
            logger.ErrorWithException(error);

            return null;
        }
    }

    public async Task<SubscriptionBalanceInfo> GetSubscriptionBalanceInfoAsync(int tenantId, string productId)
    {
        if (!billingClient.Configured)
        {
            return null;
        }

        return await billingClient.GetSubscriptionBalanceInfoAsync(await coreSettings.GetKeyAsync(tenantId), productId);
    }

    public async Task<SubscriptionToWalletResult> SubscriptionBalanceToWalletAsync(int tenantId, string productId)
    {
        if (!billingClient.Configured)
        {
            return null;
        }

        var result = await billingClient.SubscriptionBalanceToWalletAsync(await coreSettings.GetKeyAsync(tenantId), productId);

        // Clear the cache to get up-to-date tariff and balance information.
        await ClearCacheAsync(tenantId);

        return result;
    }

    public async Task<bool> SwitchSubscriptionAsync(int tenantId, string fromProductId, string toProductId, int quantity, string customerParticipantName, Dictionary<string, string> metadata = null)
    {
        if (!billingClient.Configured)
        {
            return false;
        }

        try
        {
            var portalId = await coreSettings.GetKeyAsync(tenantId);

            var switched = await billingClient.SwitchSubscriptionAsync(portalId, fromProductId, toProductId, quantity, customerParticipantName, metadata);

            if (!switched)
            {
                return false;
            }

            await ClearCacheAsync(tenantId);

            await docsCloudClient.ClearCacheAsync(portalId);
        }
        catch (Exception error)
        {
            logger.ErrorWithException(error);

            return false;
        }

        return true;
    }

    public async Task<PaymentCalculation> CalculateSwitchSubscriptionAsync(int tenantId, string fromProductId, string toProductId, int quantity)
    {
        if (!billingClient.Configured)
        {
            return null;
        }

        try
        {
            var response = await billingClient.CalculateSwitchSubscriptionAsync(await coreSettings.GetKeyAsync(tenantId), fromProductId, toProductId, quantity);

            return response;
        }
        catch (Exception error)
        {
            logger.ErrorWithException(error);

            return null;
        }
    }

    public async Task<bool> GetDocsCloudTrialAsync(int tenantId, DocsCloudTrialCustomer customer)
    {
        if (!billingClient.Configured)
        {
            return false;
        }

        try
        {
            var portalId = await coreSettings.GetKeyAsync(tenantId);

            var result = await billingClient.GetDocsCloudTrialAsync(portalId, customer);

            await ClearCacheAsync(tenantId);

            await docsCloudClient.ClearCacheAsync(portalId);

            return result;
        }
        catch (Exception error)
        {
            LogError(error, tenantId.ToString());

            return false;
        }
    }

    public async Task SetTariffAsync(int tenantId, Tariff tariff, List<TenantQuota> quotas = null)
    {
        ArgumentNullException.ThrowIfNull(tariff);

        if (tariff.Quotas == null ||
            (quotas ??= await tariff.Quotas
                .ToAsyncEnumerable()
                .Select(async (Quota q, CancellationToken _) => await quotaService.GetTenantQuotaAsync(q.Id)).ToListAsync())
            .Any(q => q == null))
        {
            return;
        }

        await SaveBillingInfoAsync(tenantId, tariff);

        if (quotas.Exists(q => q.Trial) && tenantId != Tenant.DefaultTenant)
        {
            // reset trial date
            var tenant = await tenantService.GetTenantAsync(tenantId);
            if (tenant != null)
            {
                tenant.VersionChanged = DateTime.UtcNow;
                await tenantService.SaveTenantAsync(coreSettings, tenant);
            }
        }
    }

    internal static string GetTariffCacheKey(int tenantId)
    {
        return $"{tenantId}:tariff";
    }


    internal static string GetBillingPaymentCacheKey(int tenantId)
    {
        return $"{tenantId}:billing:payments";
    }

    internal static string GetBillingCustomerCacheKey(int tenantId)
    {
        return $"{tenantId}:billing:customer";
    }

    internal static string GetAccountingBalanceCacheKey(int tenantId)
    {
        return $"{tenantId}:accounting:balance";
    }

    private async Task ClearCacheAsync(int tenantId)
    {
        await hybridCache.RemoveAsync(GetTariffCacheKey(tenantId));
        await hybridCache.RemoveAsync(GetBillingPaymentCacheKey(tenantId));
        await hybridCache.RemoveAsync(GetBillingCustomerCacheKey(tenantId));
        await hybridCache.RemoveAsync(GetAccountingBalanceCacheKey(tenantId));
    }

    public async Task<IEnumerable<PaymentInfo>> GetPaymentsAsync(int tenantId)
    {
        var key = GetBillingPaymentCacheKey(tenantId);
        var payments = await GetFromCache<List<PaymentInfo>>(key);
        if (payments == null)
        {
            await using (await distributedLockProvider.TryAcquireLockAsync($"{key}_lock"))
            {
                payments = await GetFromCache<List<PaymentInfo>>(key);
                if (payments != null)
                {
                    return payments;
                }

                payments = [];
                if (billingClient.Configured)
                {
                    try
                    {
                        var quotas = await quotaService.GetTenantQuotasAsync();
                        foreach (var pi in await billingClient.GetPaymentsAsync(await coreSettings.GetKeyAsync(tenantId)))
                        {
                            var quota = quotas.SingleOrDefault(q => q.ProductId == pi.ProductRef.ToString());
                            if (quota != null)
                            {
                                pi.QuotaId = quota.TenantId;
                            }

                            payments.Add(pi);
                        }
                    }
                    catch (Exception error)
                    {
                        LogError(error, tenantId.ToString());
                    }
                }

                await hybridCache.SetAsync(key, payments, TimeSpan.FromMinutes(10));
            }
        }

        return payments;
    }

    public async Task<Uri> GetShoppingUriAsync(int tenant, string affiliateId, string partnerId, string currency = null, string language = null, string customerEmail = null, Dictionary<string, int> quantity = null, string backUrl = null, string successUrl = null, bool checkoutSetup = false)
    {
        List<TenantQuota> newQuotas = [];

        if (billingClient.Configured)
        {
            var allQuotas = (await quotaService.GetTenantQuotasAsync()).Where(q => !string.IsNullOrEmpty(q.ProductId) && q.Visible && !q.Wallet).ToList();
            newQuotas = quantity.Select(item => allQuotas.Find(q => q.Name == item.Key)).ToList();

            TenantQuota updatedQuota = null;
            foreach (var addedQuota in newQuotas)
            {
                var qty = quantity[addedQuota.Name];

                var quota = addedQuota;

                quota *= qty;

                updatedQuota += quota;
            }

            if (updatedQuota != null)
            {
                await updatedQuota.CheckAsync(serviceProvider);
            }
        }

        var hasQuantity = quantity != null && quantity.Count != 0;
        var keyBuilder = new StringBuilder("shopingurl_");
        keyBuilder.Append(hasQuantity ? string.Join('_', quantity.Keys.ToArray()) : "all");
        if (!string.IsNullOrEmpty(affiliateId))
        {
            keyBuilder.Append($"_{affiliateId}");
        }
        if (!string.IsNullOrEmpty(partnerId))
        {
            keyBuilder.Append($"_{partnerId}");
        }

        var key = keyBuilder.ToString();
        var url = cache.Get<string>(key);
        if (url == null)
        {
            url = string.Empty;
            if (billingClient.Configured)
            {
                var productIds = checkoutSetup ? [] : newQuotas.Select(q => q.ProductId);

                try
                {
                    url =
                        await billingClient.GetPaymentUrlAsync(
                            "__Tenant__",
                            productIds,
                            affiliateId,
                            partnerId,
                            null,
                            !string.IsNullOrEmpty(currency) ? "__Currency__" : null,
                            !string.IsNullOrEmpty(language) ? "__Language__" : null,
                            !string.IsNullOrEmpty(customerEmail) ? "__CustomerEmail__" : null,
                            hasQuantity ? "__Quantity__" : null,
                            !string.IsNullOrEmpty(backUrl) ? "__BackUrl__" : null,
                            !string.IsNullOrEmpty(successUrl) ? "__SuccessUrl__" : null
                            );
                }
                catch (Exception error)
                {
                    logger.ErrorWithException(error);
                }
            }
            cache.Insert(key, url, DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
        }

        ResetCacheExpiration();

        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        var result = new Uri(url
                               .Replace("__Tenant__", HttpUtility.UrlEncode(await coreSettings.GetKeyAsync(tenant)))
                               .Replace("__Currency__", HttpUtility.UrlEncode(currency ?? ""))
                               .Replace("__Language__", HttpUtility.UrlEncode((language ?? "").ToLower()))
                               .Replace("__CustomerEmail__", HttpUtility.UrlEncode(customerEmail ?? ""))
                               .Replace("__Quantity__", hasQuantity ? string.Join(',', quantity.Values) : "")
                               .Replace("__BackUrl__", HttpUtility.UrlEncode(backUrl ?? ""))
                               .Replace("__SuccessUrl__", HttpUtility.UrlEncode(successUrl ?? "")))
            ;
        return result;
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(string partnerId, bool wallet, List<string> productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        if (productIds.Count == 0)
        {
            return [];
        }

        var ids = productIds.Distinct().ToList();

        if (billingClient.Configured)
        {
            try
            {
                var result = new Dictionary<string, Dictionary<string, decimal>>(ids.Count);
                var missing = new List<string>();

                // Every product is a key of its own, and a key missing from process memory is a Redis round trip,
                // so the keys are read, and later written, all at once rather than one after another.
                var cached = await Task.WhenAll(ids.Select(async productId =>
                {
                    var key = GetProductPriceCacheKey(partnerId, wallet, productId);
                    var price = await hybridCache.TryGetAsync<Dictionary<string, decimal>>(key,
                        SetProductPriceCacheOptions);

                    return (ProductId: productId, Price: price);
                }));

                foreach (var (productId, price) in cached)
                {
                    if (price.HasValue)
                    {
                        result.Add(productId, price.Value);
                    }
                    else
                    {
                        missing.Add(productId);
                    }
                }

                if (missing.Count > 0)
                {
                    var prices = await RequestProductPricesAsync(partnerId, wallet, missing);
                    var writes = new List<Task>(missing.Count);

                    foreach (var productId in missing)
                    {
                        if (prices.TryGetValue(productId, out var price))
                        {
                            result.Add(productId, price);
                            var key = GetProductPriceCacheKey(partnerId, wallet, productId);
                            writes.Add(hybridCache.SetAsync(key, price, SetProductPriceCacheOptions).AsTask());
                        }
                        else
                        {
                            // The payment service of this product failed: it goes without a price this time and is
                            // not cached, so the next request asks for it again.
                            result.Add(productId, []);
                        }
                    }

                    await Task.WhenAll(writes);
                }

                return result;
            }
            catch (Exception error)
            {
                LogError(error);
            }
        }

        return ids.ToDictionary(p => p, _ => new Dictionary<string, decimal>());
    }

    // The payment services price a whole list in one request, so the products missing from the cache are asked
    // for together: numeric ids are billing products, the other wallet ids are accounting services.
    // The answer holds every product of a service that answered; the products of a failed service are left out.
    private async Task<Dictionary<string, Dictionary<string, decimal>>> RequestProductPricesAsync(string partnerId,
        bool wallet, List<string> productIds)
    {
        if (!wallet)
        {
            return await RequestServicePricesAsync(productIds,
                ids => billingClient.GetProductPriceInfoAsync(partnerId, false, ids));
        }

        var accountingServices = new List<string>();
        var billingProducts = new List<string>();

        foreach (var productId in productIds)
        {
            if (int.TryParse(productId, out _))
            {
                billingProducts.Add(productId);
            }
            else
            {
                accountingServices.Add(productId);
            }
        }

        // The two services do not depend on each other, so they are asked at the same time.
        var accountingTask = RequestServicePricesAsync(accountingServices, accountingClient.GetProductPriceInfoAsync);
        var billingTask = RequestServicePricesAsync(billingProducts,
            ids => billingClient.GetProductPriceInfoAsync(partnerId, true, ids));

        await Task.WhenAll(accountingTask, billingTask);

        var result = await accountingTask;
        var billingPrices = await billingTask;

        foreach (var (productId, prices) in billingPrices)
        {
            result[productId] = prices;
        }

        return result;
    }

    // One payment service failing must not cost the products of the other one, or those already cached, their
    // prices: the failure is logged and its products are left out of the answer.
    private async Task<Dictionary<string, Dictionary<string, decimal>>> RequestServicePricesAsync(
        List<string> productIds, Func<List<string>, Task<Dictionary<string, Dictionary<string, decimal>>>> request)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        try
        {
            var prices = await request(productIds);

            // A product the service has no price for is answered as empty, so it is cached too and not asked for again.
            return productIds.ToDictionary(productId => productId, productId => prices.GetValueOrDefault(productId) ?? []);
        }
        catch (Exception error)
        {
            LogError(error);

            return [];
        }
    }

    // A price lives an hour in Redis but only minutes in process memory, so a price key deleted in Redis reaches
    // every service without a restart. Reads pass the same options: a value read from Redis with the defaults
    // would be copied into memory for the whole hour.
    private static void SetProductPriceCacheOptions(FusionCacheEntryOptions options)
    {
        options.SetDuration(_productPriceCacheDuration).SetMemoryCacheDuration(_productPriceMemoryCacheDuration);
    }

    // Prices depend on the partner and the payment system, not on the portal: the key carries no tenant id,
    // and one entry serves every portal of the partner.
    private static string GetProductPriceCacheKey(string partnerId, bool wallet, string productId)
    {
        return $"billing-product-price-{partnerId}-{wallet}-{productId}";
    }

    public async Task<Uri> GetAccountLinkAsync(int tenant, string backUrl)
    {
        var key = "accountlink_" + tenant;
        var url = cache.Get<string>(key);
        if (url == null && billingClient.Configured)
        {
            try
            {
                url = await billingClient.GetAccountLinkAsync(await coreSettings.GetKeyAsync(tenant), backUrl);
                cache.Insert(key, url, DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
            }
            catch (Exception error)
            {
                LogError(error);
            }
        }

        return !string.IsNullOrEmpty(url) ? new Uri(url) : null;
    }

    public async Task<Tariff> GetBillingInfoAsync(int? tenant = null, int? id = null)
    {
        await using var coreDbContext = await coreDbContextManager.CreateDbContextAsync();

        var r = await coreDbContext.TariffAsync(tenant, id);

        if (r == null)
        {
            return null;
        }

        var tariff = await CreateDefaultAsync(true);
        tariff.Id = r.Id;
        tariff.DueDate = RestoreMaxValueSentinel(r.Stamp);
        tariff.CustomerId = r.CustomerId;

        var quotas = await coreDbContext.QuotasAsync(r.TenantId, r.Id).ToListAsync();

        foreach (var q in quotas)
        {
            RestoreMaxValueSentinel(q);

            if (q is { Additional: true, State: QuotaState.Overdue })
            {
                tariff.OverdueQuotas ??= [];
                tariff.OverdueQuotas.Add(q);
            }
            else
            {
                tariff.Quotas.Add(q);
            }
        }

        if (tariff.Quotas.All(q => q.Additional))
        {
            await AddInitialQuotaAsync(tariff, tenant.Value);
        }

        return tariff;
    }

    private async Task<bool> SaveBillingInfoAsync(int tenant, Tariff tariffInfo)
    {
        var inserted = false;
        var currentTariff = await GetBillingInfoAsync(tenant);
        if (!tariffInfo.EqualsByParams(currentTariff))
        {
            try
            {
                await using var dbContext = await coreDbContextManager.CreateDbContextAsync();

                var efTariff = new DbTariff
                {
                    Id = tariffInfo.Id,
                    TenantId = tenant,
                    Stamp = TruncateToWholeSeconds(tariffInfo.DueDate),
                    CustomerId = tariffInfo.CustomerId,
                    CreateOn = DateTime.UtcNow
                };

                if (efTariff.Id == 0)
                {
                    efTariff.Id = -tenant;
                    tariffInfo.Id = efTariff.Id;
                }

                efTariff.CustomerId ??= "";
                efTariff = await dbContext.AddOrUpdateAsync(q => q.Tariffs, efTariff);

                foreach (var q in tariffInfo.Quotas)
                {
                    await dbContext.AddOrUpdateAsync(quota => quota.TariffRows, new DbTariffRow
                    {
                        TariffId = efTariff.Id,
                        Quota = q.Id,
                        Quantity = q.Quantity,
                        DueDate = q.DueDate.HasValue ? TruncateToWholeSeconds(q.DueDate.Value) : null,
                        NextQuantity = q.NextQuantity,
                        NextQuota = q.NextQuota,
                        TenantId = tenant
                    });
                }

                await dbContext.SaveChangesAsync();

                inserted = true;
            }
            catch (DbUpdateException)
            {

            }
        }

        if (inserted)
        {
            if (tenant != Tenant.DefaultTenant)
            {
                var t = await tenantService.GetTenantAsync(tenant);
                if (t != null)
                {
                    // update tenant.LastModified to flush cache in documents
                    await tenantService.SaveTenantAsync(coreSettings, t);
                }
            }

            await ClearCacheAsync(tenant);

            await NotifyWebSocketAsync(currentTariff, tariffInfo);
        }

        return inserted;
    }

    // MySQL "datetime" columns have no fractional-second precision; DateTime.MaxValue's sub-second ticks
    // round up to the next (invalid, year 10000) day and get silently stored as a zero-date otherwise.
    private static DateTime TruncateToWholeSeconds(DateTime value)
    {
        return value.Equals(DateTime.MaxValue)
            ? value.Date.Add(new TimeSpan(value.Hour, value.Minute, value.Second))
            : value;
    }

    // Reverses TruncateToWholeSeconds on read, so a due date persisted as the "no expiration" sentinel
    // compares equal (via Quota.Equals) to the in-memory DateTime.MaxValue it originated from.
    private static DateTime RestoreMaxValueSentinel(DateTime value)
    {
        return value.Year < 9999 ? value : DateTime.MaxValue;
    }

    private static void RestoreMaxValueSentinel(Quota q)
    {
        if (q.DueDate.HasValue)
        {
            q.DueDate = RestoreMaxValueSentinel(q.DueDate.Value);
        }
    }

    public async Task<bool> UpdateNextQuantityAsync(int tenant, Tariff tariffInfo, int quotaId, int? nextQuantity, int? nextQuota = null)
    {
        try
        {
            if (nextQuantity is < 0)
            {
                return false;
            }

            // the only supported scheduled quota switch today is reverting from DocsCloudDevPack back to DocsCloud
            if (nextQuota is not null && (quotaId != (int)TenantWalletService.DocsCloudDevPack || nextQuota != (int)TenantWalletService.DocsCloud))
            {
                return false;
            }

            var currentTariff = await GetBillingInfoAsync(tenant);

            await using var dbContext = await coreDbContextManager.CreateDbContextAsync();

            var changed = false;

            foreach (var q in tariffInfo.Quotas)
            {
                if (q.Id == quotaId)
                {
                    q.NextQuantity = nextQuantity;
                    q.NextQuota = nextQuota;

                    await dbContext.AddOrUpdateAsync(quota => quota.TariffRows, new DbTariffRow
                    {
                        TariffId = tariffInfo.Id,
                        Quota = q.Id,
                        Quantity = q.Quantity,
                        DueDate = q.DueDate.HasValue ? TruncateToWholeSeconds(q.DueDate.Value) : null,
                        NextQuantity = nextQuantity,
                        NextQuota = nextQuota,
                        TenantId = tenant
                    });

                    changed = true;
                }
            }

            if (!changed)
            {
                return false;
            }

            await dbContext.SaveChangesAsync();

            await ClearCacheAsync(tenant);

            await NotifyWebSocketAsync(currentTariff, tariffInfo);

            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task DeleteDefaultBillingInfoAsync()
    {
        const int tenant = Tenant.DefaultTenant;

        await using var coreDbContext = await coreDbContextManager.CreateDbContextAsync();
        await coreDbContext.DeleteTariffs(tenant);

        await ClearCacheAsync(tenant);
    }


    private async Task<Tariff> CalculateTariffAsync(int tenantId, Tariff tariff)
    {
        tariff.State = TariffState.Paid;

        if (tariff.Quotas.Count == 0)
        {
            await AddDefaultQuotaAsync(tariff);
        }

        var delay = 0;
        var setDelay = true;
        var lifetime = false;

        if (coreBaseSettings.Standalone)
        {
            foreach (var q in tariff.Quotas)
            {
                var quota = await quotaService.GetTenantQuotaAsync(q.Id);
                if (quota.Lifetime)
                {
                    lifetime = true;
                }
                if (quota.Trial)
                {
                    setDelay = false;
                }
            }
        }

        if (TrialEnabled)
        {
            var trial = await tariff.Quotas
                .ToAsyncEnumerable()
                .AnyAsync(async (q, _) => (await quotaService.GetTenantQuotaAsync(q.Id)).Trial);
            if (trial)
            {
                setDelay = false;
                tariff.State = TariffState.Trial;
                if (tariff.DueDate == DateTime.MinValue || tariff.DueDate == DateTime.MaxValue)
                {
                    var tenant = await tenantService.GetTenantAsync(tenantId);
                    if (tenant != null)
                    {
                        var fromDate = tenant.CreationDateTime < tenant.VersionChanged ? tenant.VersionChanged : tenant.CreationDateTime;
                        var trialPeriod = await GetPeriodAsync("TrialPeriod", DefaultTrialPeriod);
                        if (fromDate == DateTime.MinValue)
                        {
                            fromDate = DateTime.UtcNow.Date;
                        }

                        tariff.DueDate = trialPeriod != 0 ? fromDate.Date.AddDays(trialPeriod) : DateTime.MaxValue;
                    }
                    else
                    {
                        tariff.DueDate = DateTime.MaxValue;
                    }
                }
            }
        }

        if (setDelay && !lifetime)
        {
            delay = PaymentDelay;
        }

        if (tariff.DueDate != DateTime.MinValue && tariff.DueDate.Date < DateTime.UtcNow.Date && delay > 0)
        {
            tariff.State = TariffState.Delay;
            tariff.DelayDueDate = tariff.DueDate.Date.AddDays(delay);
        }

        if (tariff.DueDate == DateTime.MinValue ||
            tariff.DueDate != DateTime.MaxValue && tariff.DueDate.Date < DateTime.UtcNow.Date.AddDays(-delay))
        {
            tariff.State = lifetime ? TariffState.Paid : TariffState.NotPaid;
        }

        return tariff;
    }

    private async Task<int> GetPeriodAsync(string key, int defaultValue)
    {
        var settings = await tenantService.GetTenantSettingsAsync(Tenant.DefaultTenant, key);

        return settings != null ? Convert.ToInt32(Encoding.UTF8.GetString(settings)) : defaultValue;
    }

    private async Task<Tariff> CreateDefaultAsync(bool empty = false)
    {
        var result = new Tariff
        {
            State = TariffState.Paid,
            DueDate = DateTime.MaxValue,
            DelayDueDate = DateTime.MaxValue,
            LicenseDate = DateTime.MaxValue,
            CustomerId = "",
            Quotas = []
        };

        if (!empty)
        {
            await AddDefaultQuotaAsync(result);
        }

        return result;
    }

    private async Task AddInitialQuotaAsync(Tariff tariff, int tenantId)
    {
        await using var coreDbContext = await coreDbContextManager.CreateDbContextAsync();

        var toAdd = await coreDbContext.QuotasAsync(tenantId, -tenantId).FirstOrDefaultAsync();

        if (toAdd != null)
        {
            RestoreMaxValueSentinel(toAdd);
            tariff.Quotas.Insert(0, toAdd);
        }
        else
        {
            await AddDefaultQuotaAsync(tariff);
        }
    }

    private async Task AddDefaultQuotaAsync(Tariff tariff)
    {
        var toAdd = await GetDefaultQuotaAsync();

        if (toAdd != null)
        {
            tariff.Quotas.Insert(0, new Quota(toAdd.TenantId, 1));
        }
    }

    private async Task<TenantQuota> GetDefaultQuotaAsync()
    {
        TenantQuota defaultQuota;
        var allQuotas = await quotaService.GetTenantQuotasAsync();

        if (PaymentConfiguration.DefaultQuota.HasValue)
        {
            defaultQuota = allQuotas.FirstOrDefault(r => r.TenantId == PaymentConfiguration.DefaultQuota.Value);
            if (defaultQuota != null)
            {
                return defaultQuota;
            }
        }

        defaultQuota = TrialEnabled ?
            allQuotas.FirstOrDefault(r => r.Trial && !r.Custom) :
            allQuotas.FirstOrDefault(r => coreBaseSettings.Standalone || r.Free && !r.Custom);

        return defaultQuota;
    }

    private void LogError(Exception error, string tenantId = null)
    {
        if (error is BillingNotFoundException)
        {
            logger.DebugPaymentTenant(tenantId, error.Message);
        }
        else if (error is BillingNotConfiguredException or BillingLicenseTypeException)
        {
            logger.DebugBillingTenant(tenantId, error.Message);
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.ErrorBillingWithException(tenantId, error);
            }
            else
            {
                logger.ErrorBilling(tenantId, error.Message);
            }
        }
    }

    private async Task NotifyWebSocketAsync(Tariff currenTariff, Tariff newTariff)
    {
        var quotaSocketManager = serviceProvider.GetRequiredService<QuotaSocketManager>();

        var updatedQuota = await GetTenantQuotaFromTariffAsync(newTariff);

        var maxTotalSize = updatedQuota.MaxTotalSize;
        var maxTotalSizeFeatureName = updatedQuota.GetFeature<MaxTotalSizeFeature>().Name;

        _ = quotaSocketManager.ChangeQuotaFeatureValueAsync(maxTotalSizeFeatureName, maxTotalSize);

        var maxPaidUsers = updatedQuota.CountRoomAdmin;
        var maxPaidUsersFeatureName = updatedQuota.GetFeature<CountPaidUserFeature>().Name;

        _ = quotaSocketManager.ChangeQuotaFeatureValueAsync(maxPaidUsersFeatureName, maxPaidUsers);

        var maxRoomCount = updatedQuota.CountRoom == int.MaxValue ? -1 : updatedQuota.CountRoom;
        var maxRoomCountFeatureName = updatedQuota.GetFeature<CountRoomFeature>().Name;

        _ = quotaSocketManager.ChangeQuotaFeatureValueAsync(maxRoomCountFeatureName, maxRoomCount);

        if (currenTariff != null)
        {
            var currentQuota = await GetTenantQuotaFromTariffAsync(currenTariff);

            var free = updatedQuota.Free;
            if (currentQuota.Free != free)
            {
                var freeFeatureName = updatedQuota.GetFeature<FreeFeature>().Name;

                _ = quotaSocketManager.ChangeQuotaFeatureValueAsync(freeFeatureName, free);
            }
        }
    }

    private async Task<TenantQuota> GetTenantQuotaFromTariffAsync(Tariff tariff)
    {
        TenantQuota result = null;
        foreach (var tariffRow in tariff.Quotas)
        {
            var quota = await quotaService.GetTenantQuotaAsync(tariffRow.Id);

            quota *= tariffRow.Quantity;

            quota.DueDate = tariffRow.DueDate;

            result += quota;
        }

        return result;
    }

    public async Task<bool> IsFreeTariffAsync(Tariff tariff)
    {
        var freeTariff = await tariff.Quotas.ToAsyncEnumerable().FirstOrDefaultAsync(async (tariffRow, _) =>
        {
            var q = await quotaService.GetTenantQuotaAsync(tariffRow.Id);
            return q == null
                   || (TrialEnabled && q.Trial)
                   || q.Free
                   || q.NonProfit
                   || q.Custom;
        });

        return freeTariff != null;
    }

    public int GetPaymentDelay()
    {
        return PaymentDelay;
    }

    public bool IsConfigured()
    {
        return billingClient.Configured && accountingClient.Configured;
    }

    public async Task<CustomerInfo> GetCustomerInfoAsync(int tenantId, bool refresh = false)
    {
        var cacheKey = GetBillingCustomerCacheKey(tenantId);

        var customerInfo = refresh ? null : await GetFromCache<CustomerInfo>(cacheKey);

        if (customerInfo != null)
        {
            return customerInfo.IsDefault() ? null : customerInfo;
        }

        await using (await distributedLockProvider.TryAcquireLockAsync($"{cacheKey}_lock"))
        {
            customerInfo = refresh ? null : await GetFromCache<CustomerInfo>(cacheKey);

            if (customerInfo != null)
            {
                return customerInfo.IsDefault() ? null : customerInfo;
            }

            if (billingClient.Configured)
            {
                try
                {
                    var portalId = await coreSettings.GetKeyAsync(tenantId);
                    customerInfo = await billingClient.GetCustomerInfoAsync(portalId);
                    await hybridCache.SetAsync(cacheKey, customerInfo, TimeSpan.FromMinutes(10));
                }
                catch (Exception error)
                {
                    LogError(error, tenantId.ToString());
                    await hybridCache.SetAsync(cacheKey, new CustomerInfo(), TimeSpan.FromMinutes(10));
                }
            }
        }

        return customerInfo;
    }

    public async Task<bool> TopUpDepositAsync(int tenantId, decimal amount, string currency, string customerParticipantName, string siteName, Dictionary<string, string> metadata = null, bool waitForChanges = false)
    {
        var (_, result) = await TopUpDepositInternalAsync(tenantId, amount, currency, customerParticipantName, siteName, metadata, waitForChanges);

        return result;
    }

    /// <summary>
    /// Deposits funds and reports whether the outcome could actually be confirmed.
    /// <see cref="TopUpOutcome.Unknown"/> means the billing response was lost (e.g. a timeout) or the
    /// balance did not reflect the change in time - the deposit may or may not have been applied on the
    /// billing side, so callers must not blindly retry with a recomputed amount, or they risk depositing twice.
    /// </summary>
    private async Task<(TopUpOutcome Outcome, bool Result)> TopUpDepositInternalAsync(int tenantId, decimal amount, string currency, string customerParticipantName, string siteName, Dictionary<string, string> metadata, bool waitForChanges)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);

        decimal? oldBalanceAmount = 0;

        if (waitForChanges)
        {
            var oldBalance = await GetCustomerBalanceAsync(tenantId);
            oldBalanceAmount = oldBalance?.SubAccounts?.FirstOrDefault(x => x.Currency == currency)?.Amount;
        }

        var cacheKey = GetAccountingBalanceCacheKey(tenantId);
        var result = false;
        TopUpOutcome outcome;

        try
        {
            result = await billingClient.TopUpDepositAsync(portalId, amount, currency, customerParticipantName, siteName, metadata);
            outcome = result ? TopUpOutcome.Confirmed : TopUpOutcome.Rejected;
        }
        catch (Exception error)
        {
            logger.ErrorWithException(error);
            outcome = TopUpOutcome.Unknown;
        }

        if (!result || !waitForChanges)
        {
            await hybridCache.RemoveAsync(cacheKey);
            return (outcome, result);
        }

        var pipeline = resiliencePipelineProvider.GetPipeline<bool>(AccountingHttpClientExtension.BalanceResiliencePipelineName);

        var updated = await pipeline.ExecuteAsync(async _ =>
        {
            var newBalance = await GetCustomerBalanceAsync(tenantId, true);
            var newBalanceAmount = newBalance?.SubAccounts?.FirstOrDefault(x => x.Currency == currency)?.Amount;

            return oldBalanceAmount != newBalanceAmount;
        });

        if (!updated)
        {
            logger.ErrorBilling(tenantId.ToString(), "Balance value is not updated after replenishment");
            await hybridCache.RemoveAsync(cacheKey);

            // Billing reported success, but we could not confirm it against the balance in time:
            // treat as unknown rather than confirmed so the caller does not act as if it were final.
            outcome = TopUpOutcome.Unknown;
        }

        return (outcome, result);
    }

    public async Task<bool> EnsureWalletBalanceAsync(int tenantId, decimal requiredAmount, string currency, string customerParticipantName, string siteName, bool auto, Dictionary<string, string> metadata = null, bool allowTopUp = true)
    {
        var balanceAmount = await GetWalletBalanceAmountAsync(tenantId, currency);

        // A delayed payment method accepts the deposit right away, but the money is credited only
        // once the transfer settles, so the balance cannot reflect it within this call and the loop below would
        // charge the customer again on every attempt. Such wallets are topped up manually - only report whether
        // what is already there covers the cost.
        if (!allowTopUp)
        {
            return balanceAmount >= requiredAmount;
        }

        // The balance may be consumed concurrently, so the top-up is retried several times.
        for (var attempt = 0; attempt < MaxTopUpAttempts && balanceAmount < requiredAmount; attempt++)
        {
            var topUpAmount = Math.Ceiling((requiredAmount - balanceAmount) * 100) / 100;

            // waitForChanges must stay true: TopUpOutcome.Unknown is the only early exit that stops this loop
            // from firing a second real deposit, and it can only be reported when the balance is polled.
            var (outcome, toppedUp) = await TopUpDepositInternalAsync(tenantId, topUpAmount, currency, customerParticipantName, siteName, metadata, true);
            if (toppedUp)
            {
                var quotaSocketManager = serviceProvider.GetRequiredService<QuotaSocketManager>();
                await quotaSocketManager.TopUpWallet(auto);
            }

            balanceAmount = await GetWalletBalanceAmountAsync(tenantId, currency);

            if (outcome == TopUpOutcome.Unknown)
            {
                // Whether this attempt actually succeeded on the billing side is unconfirmed. Retrying would
                // recompute the shortfall and fire another real deposit, doubling it if the unconfirmed one
                // did land - so stop here and let the caller treat the balance as still insufficient.
                logger.ErrorBilling(tenantId.ToString(), "Top-up outcome is unknown, stopping retries to avoid a duplicate deposit");

                break;
            }
        }

        return balanceAmount >= requiredAmount;
    }

    private enum TopUpOutcome
    {
        Confirmed,
        Rejected,
        Unknown
    }

    private async Task<decimal> GetWalletBalanceAmountAsync(int tenantId, string currency)
    {
        var balance = await GetCustomerBalanceAsync(tenantId, true);
        if (balance == null)
        {
            throw new ItemNotFoundException("Balance could not be found");
        }

        var subAccount = balance.SubAccounts?.FirstOrDefault(x => x.Currency == currency);
        if (subAccount == null)
        {
            throw new ItemNotFoundException("Subaccount could not be found");
        }

        return subAccount.Amount;
    }

    #region Accounting

    public async Task<bool?> HasPositiveBalanceAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        if (!accountingClient.Configured)
        {
            return false;
        }

        // Not through GetCustomerBalanceAsync: it answers null both for a portal without a wallet and for
        // a failed request, and a decision that may block a portal has to tell the two apart. It also
        // asks the service directly, past the cache, since the answer is only needed on a few days.
        try
        {
            var portalId = await coreSettings.GetKeyAsync(tenantId);
            var balance = await accountingClient.GetCustomerBalanceAsync(portalId, cancellationToken);

            return balance?.SubAccounts?.Any(s => s.Amount > 0) ?? false;
        }
        catch (AccountingCustomerNotFoundException)
        {
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller gave up waiting: not knowing is the answer, and the caller says so.
            return null;
        }
        catch (Exception error)
        {
            LogError(error, tenantId.ToString());

            return null;
        }
    }

    public async Task<Balance> GetCustomerBalanceAsync(int tenantId, bool refresh = false)
    {
        if (!accountingClient.Configured)
        {
            return null;
        }

        var cacheKey = GetAccountingBalanceCacheKey(tenantId);

        if (refresh)
        {
            await hybridCache.RemoveAsync(cacheKey);
        }

        var balance = refresh ? null : await GetFromCache<Balance>(cacheKey);

        if (balance != null)
        {
            return balance.IsDefault() ? null : balance;
        }

        await using (await distributedLockProvider.TryAcquireLockAsync($"{cacheKey}_lock"))
        {
            balance = refresh ? null : await GetFromCache<Balance>(cacheKey);

            if (balance != null)
            {
                return balance.IsDefault() ? null : balance;
            }

            try
            {
                var portalId = await coreSettings.GetKeyAsync(tenantId);
                balance = await accountingClient.GetCustomerBalanceAsync(portalId);
            }
            catch (AccountingCustomerNotFoundException exception)
            {
                logger.DebugAccountingTenant(tenantId.ToString(), exception.Message);
                await hybridCache.SetAsync(cacheKey, new Balance(), TimeSpan.FromMinutes(10));
            }
            catch (Exception error)
            {
                LogError(error, tenantId.ToString());
                await hybridCache.SetAsync(cacheKey, new Balance(), TimeSpan.FromMinutes(10));
            }
        }

        return balance;
    }

    public async Task<Session> OpenCustomerSessionAsync(int tenantId, string serviceName, string externalRef, int quantity, int duration)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);
        return await accountingClient.OpenCustomerSessionAsync(portalId, serviceName, externalRef, quantity, duration);
    }

    public async Task<bool> CloseCustomerSessionAsync(int tenantId, int sessionId)
    {
        await accountingClient.CloseCustomerSessionAsync(sessionId);
        await hybridCache.RemoveAsync(GetAccountingBalanceCacheKey(tenantId));
        return true;
    }

    public async Task<Session> ExtendCustomerSessionAsync(int tenantId, int sessionId, int duration)
    {
        var session = await accountingClient.ExtendCustomerSessionAsync(sessionId, duration);
        await hybridCache.RemoveAsync(GetAccountingBalanceCacheKey(tenantId));
        return session;
    }

    public async Task<bool> CompleteCustomerSessionAsync(int tenantId, string serviceName, int sessionId, int quantity, string customerParticipantName, Dictionary<string, string> metadata = null)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);
        await accountingClient.CompleteCustomerSessionAsync(portalId, serviceName, sessionId, quantity, customerParticipantName, metadata);
        await hybridCache.RemoveAsync(GetAccountingBalanceCacheKey(tenantId));
        return true;
    }

    public async Task<OperationResult> CreditBonusAsync(int tenantId, decimal sum, string currency, string externalRef, DateTime? utcExpiresAt = null, Dictionary<string, string> metadata = null)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);
        var result = await accountingClient.CreditBonusAsync(portalId, sum, currency, externalRef, utcExpiresAt, metadata);
        await hybridCache.RemoveAsync(GetAccountingBalanceCacheKey(tenantId));
        return result;
    }

    public async Task<Report> GetCustomerOperationsAsync(int tenantId, OperationFilter filter)
    {
        try
        {
            var portalId = await coreSettings.GetKeyAsync(tenantId);

            return await accountingClient.GetCustomerOperationsAsync(portalId, filter);
        }
        catch (AccountingCustomerNotFoundException exception)
        {
            logger.DebugAccountingTenant(tenantId.ToString(), exception.Message);
        }
        catch (Exception error)
        {
            LogError(error, tenantId.ToString());
        }

        return null;
    }

    public async Task<List<CustomerMonthlyUsage>> GetCustomerMonthlyUsageAsync(int tenantId, MonthlyUsageFilter filter)
    {
        try
        {
            var portalId = await coreSettings.GetKeyAsync(tenantId);

            return await accountingClient.GetCustomerMonthlyUsageAsync(portalId, filter);
        }
        catch (Exception error)
        {
            LogError(error, tenantId.ToString());
            return null;
        }
    }

    public async Task<UsageReport> GetCustomerServiceUsageAsync(int tenantId, UsageFilter filter)
    {
        try
        {
            var portalId = await coreSettings.GetKeyAsync(tenantId);

            return await accountingClient.GetCustomerServiceUsageAsync(portalId, filter);
        }
        catch (Exception error)
        {
            LogError(error, tenantId.ToString());
            return null;
        }
    }

    /// <summary>
    /// Every page of operations in turn, <see cref="PagedFilter.Limit"/> per page; the requests are sent while the
    /// result is enumerated. Failures are not caught: the caller decides, and a report task fails instead of
    /// producing a silently truncated document.
    /// </summary>
    public async IAsyncEnumerable<Report> GetCustomerOperationPagesAsync(int tenantId, OperationFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);

        await foreach (var page in accountingClient.GetCustomerOperationPages(portalId, filter).AsPages().WithCancellation(cancellationToken))
        {
            yield return page;
        }
    }

    /// <summary>
    /// Every page of service usage records in turn, as in <see cref="GetCustomerOperationPagesAsync"/>.
    /// </summary>
    public async IAsyncEnumerable<UsageReport> GetCustomerServiceUsagePagesAsync(int tenantId, UsageFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);

        await foreach (var page in accountingClient.GetCustomerServiceUsagePages(portalId, filter).AsPages().WithCancellation(cancellationToken))
        {
            yield return page;
        }
    }

    public async Task<List<Currency>> GetAllAccountingCurrenciesAsync()
    {
        return await accountingClient.GetAllCurrenciesAsync();
    }

    public List<string> GetSupportedAccountingCurrencies()
    {
        return accountingClient.GetSupportedCurrencies();
    }

    public async Task<List<ServicePriceInfo>> GetAccountingServicePricesAsync(string serviceName, bool active = false)
    {
        return await accountingClient.GetServicePricesAsync(serviceName, active);
    }

    /// <summary>
    /// Returns the service limit only when it is a limit on the service group and belongs to the tenant: the accounting
    /// service addresses a limit by its global ID, so the owner is checked against the account number of the tenant's
    /// customer, and a limit the customer has on another group is left to the operations of that group.
    /// </summary>
    public async Task<ServiceLimit> GetServiceLimitAsync(int tenantId, string groupName, int id)
    {
        ServiceLimit serviceLimit;
        try
        {
            serviceLimit = await accountingClient.GetServiceLimitAsync(id);
        }
        catch (AccountingException exception) when (exception is AccountingNotFoundException or AccountingCustomerNotFoundException)
        {
            logger.DebugAccountingTenant(tenantId.ToString(), exception.Message);
            return null;
        }

        if (serviceLimit == null || !string.Equals(serviceLimit.ServiceGroupName, groupName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var accountNumber = await GetCustomerAccountNumberAsync(tenantId);
        if (accountNumber != serviceLimit.CustomerAccountNumber)
        {
            return null;
        }

        return serviceLimit;
    }

    // The account number of the tenant's customer, null when the portal is not a customer. The cached balance is empty
    // both for a portal that is not a customer and for a while after a failed read, so an empty one is read again
    // straight from the accounting service: a failure then surfaces as one instead of making an existing limit look
    // like one of another portal.
    private async Task<int?> GetCustomerAccountNumberAsync(int tenantId)
    {
        var balance = await GetCustomerBalanceAsync(tenantId);
        if (balance != null)
        {
            return balance.AccountNumber;
        }

        try
        {
            var portalId = await coreSettings.GetKeyAsync(tenantId);

            return (await accountingClient.GetCustomerBalanceAsync(portalId)).AccountNumber;
        }
        catch (AccountingCustomerNotFoundException exception)
        {
            logger.DebugAccountingTenant(tenantId.ToString(), exception.Message);
            return null;
        }
    }

    /// <summary>
    /// Returns the limit of the portal as a whole on the service group, with its spending in the current period, or
    /// null when none is set - including for a portal the accounting service does not know yet.
    /// </summary>
    public async Task<ServiceLimit> GetTotalServiceLimitAsync(int tenantId, string groupName)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);

        try
        {
            return await accountingClient.GetTotalServiceLimitAsync(portalId, groupName, includeUsage: true);
        }
        catch (AccountingException exception) when (exception is AccountingNotFoundException or AccountingCustomerNotFoundException)
        {
            logger.DebugAccountingTenant(tenantId.ToString(), exception.Message);
            return null;
        }
    }

    /// <summary>
    /// Returns the limit for every user on the service group, or null when none is set - including for a portal the
    /// accounting service does not know yet. It carries no spending: that is counted per user and comes with
    /// <see cref="GetParticipantServiceLimitsAsync"/>.
    /// </summary>
    public async Task<ServiceLimit> GetGlobalServiceLimitAsync(int tenantId, string groupName)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);

        try
        {
            return await accountingClient.GetGlobalServiceLimitAsync(portalId, groupName);
        }
        catch (AccountingException exception) when (exception is AccountingNotFoundException or AccountingCustomerNotFoundException)
        {
            logger.DebugAccountingTenant(tenantId.ToString(), exception.Message);
            return null;
        }
    }

    /// <summary>
    /// Returns a page of the limits in force for the users of the tenant on the service group. A portal the accounting
    /// service does not know yet has no limits, so it gets an empty page, as the accounting service answers when no limit
    /// is set.
    /// </summary>
    public async Task<ServiceLimitReport> GetParticipantServiceLimitsAsync(int tenantId, string groupName, ServiceLimitFilter filter)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);

        try
        {
            return await accountingClient.GetParticipantServiceLimitsAsync(portalId, groupName, filter);
        }
        // AccountingNotFoundException is left to fail on purpose: with no limits this route answers an empty page, so a
        // 404 means a route the accounting service does not have, which must not be shown as "no limits".
        catch (AccountingCustomerNotFoundException exception)
        {
            logger.DebugAccountingTenant(tenantId.ToString(), exception.Message);

            return new ServiceLimitReport
            {
                Collection = [],
                Offset = filter?.Offset ?? 0,
                Limit = filter?.Limit ?? ParticipantServiceLimitsDefaultPageSize,
                CurrentPage = 1
            };
        }
    }

    /// <summary>
    /// The limit in force for each user of the tenant on the service group - the personal one, or else the one for every
    /// user - with its spending in the current period, read in full from the accounting service and cached for a few
    /// minutes. It backs per-user lists, which would otherwise ask the accounting service once per user. The service
    /// limit operations of this class drop the cache, but a change made outside the portal, and the spending, show up
    /// only when the cache expires. A user the accounting service has not met yet is not in it. Keyed by the user ID, so
    /// a list of users looks each one up instead of scanning the whole set, and holding only what the lists show, since
    /// the whole set goes to the distributed cache; a switched-off limit is left out. While the
    /// accounting service is slow or cannot be reached, the last list known is used. Empty when the accounting service is
    /// not configured or the portal is not its customer; null when the list cannot be read and none is known yet, since
    /// an empty list would claim that no user has a personal limit.
    /// </summary>
    public async Task<Dictionary<Guid, UserServiceLimit>> GetCachedParticipantServiceLimitsAsync(int tenantId, string groupName)
    {
        if (!accountingClient.Configured)
        {
            return [];
        }

        var version = await GetServiceLimitsCacheVersionAsync(tenantId, groupName);

        return await GetOrSetServiceLimitsAsync<Dictionary<Guid, UserServiceLimit>>(tenantId, GetParticipantServiceLimitsCacheKey(tenantId, groupName, version), async token =>
        {
            try
            {
                var portalId = await coreSettings.GetKeyAsync(tenantId);
                var filter = new ServiceLimitFilter { Limit = ParticipantServiceLimitsPageSize, IncludeUsage = true };

                var serviceLimits = new Dictionary<Guid, UserServiceLimit>();
                await foreach (var page in accountingClient.GetParticipantServiceLimitPages(portalId, groupName, filter).AsPages().WithCancellation(token))
                {
                    // The accounting service leaves switched-off limits out of this list; the check keeps a switched-off
                    // personal limit from hiding the limit for every user should one ever come back in it.
                    foreach (var serviceLimit in page.Collection ?? [])
                    {
                        if (serviceLimit.Enabled && Guid.TryParse(serviceLimit.Participant, out var userId))
                        {
                            serviceLimits.TryAdd(userId, serviceLimit.ToUserServiceLimit());
                        }
                    }
                }

                return serviceLimits;
            }
            catch (AccountingCustomerNotFoundException exception)
            {
                logger.DebugAccountingTenant(tenantId.ToString(), exception.Message);
                return [];
            }
        }, null);
    }

    /// <summary>
    /// The limit in force for the user on the service group: the user's personal limit, or else the one for every user,
    /// both from the caches of <see cref="GetCachedParticipantServiceLimitsAsync"/>. The personal limit and the one for
    /// every user found in the list of the users come with the user's spending; the one for every user applied to a
    /// user the accounting service has not met yet comes without it. Null when no limit applies to the user, and when
    /// the list of the users cannot be read: the user may have a personal limit then, so the one for every user is not
    /// shown in its place.
    /// </summary>
    public async Task<UserServiceLimit> GetCachedUserServiceLimitAsync(int tenantId, string groupName, Guid userId)
    {
        if (!accountingClient.Configured)
        {
            return null;
        }

        // A portal that is not a billing customer cannot have limits - the limit operations refuse it - so the lists of
        // its users do not ask the accounting service for it. Only what is cached decides: the billing service has no
        // short timeout, and every list of users and every sign-in would wait for it. With nothing cached, the limits
        // are read as usual, with the timeouts and fallbacks of their own caches.
        var customerInfo = await GetFromCache<CustomerInfo>(GetBillingCustomerCacheKey(tenantId));
        if (customerInfo != null && customerInfo.IsDefault())
        {
            return null;
        }

        var serviceLimits = await GetCachedParticipantServiceLimitsAsync(tenantId, groupName);
        if (serviceLimits == null)
        {
            return null;
        }

        if (serviceLimits.TryGetValue(userId, out var serviceLimit))
        {
            return serviceLimit;
        }

        var globalServiceLimit = await GetCachedGlobalServiceLimitAsync(tenantId, groupName);

        return globalServiceLimit is { Enabled: true } ? globalServiceLimit.ToUserServiceLimit() : null;
    }

    // The limit for every user, cached like the list of the users: a user missing from that list falls back to it.
    private async Task<ServiceLimit> GetCachedGlobalServiceLimitAsync(int tenantId, string groupName)
    {
        if (!accountingClient.Configured)
        {
            return null;
        }

        // A portal the accounting service does not know, or one without the limit, gets null from the read itself.
        var version = await GetServiceLimitsCacheVersionAsync(tenantId, groupName);

        return await GetOrSetServiceLimitsAsync<ServiceLimit>(tenantId, GetGlobalServiceLimitCacheKey(tenantId, groupName, version),
            _ => GetGlobalServiceLimitAsync(tenantId, groupName), null);
    }

    // The caches behind the user lists, which every page of users and every sign-in reads, so a slow or unreachable
    // accounting service must neither fail nor stall them. A read that fails or runs past the timeouts falls back to the
    // last value known; with none known yet, the lists get the fallback, kept for a minute so that every request does
    // not wait again. A 404 lands here too: a portal without limits gets an empty page, so a 404 means a route the
    // accounting service does not have, which has to be logged as an error rather than cached as "no limits".
    private async Task<T> GetOrSetServiceLimitsAsync<T>(int tenantId, string cacheKey, Func<CancellationToken, Task<T>> factory, T fallback)
    {
        try
        {
            return await hybridCache.GetOrSetAsync<T>(cacheKey, async (_, token) => await factory(token), opt => opt
                .SetDuration(_participantServiceLimitsCacheDuration)
                .SetFailSafe(true, _serviceLimitsFailSafeMaxDuration, _participantServiceLimitsFailureCacheDuration)
                .SetFactoryTimeouts(_serviceLimitsFactorySoftTimeout, _serviceLimitsFactoryHardTimeout));
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            LogError(error, tenantId.ToString());
            await hybridCache.SetAsync(cacheKey, fallback, _participantServiceLimitsFailureCacheDuration);

            return fallback;
        }
    }

    internal static string GetParticipantServiceLimitsCacheKey(int tenantId, string groupName, string version)
    {
        return $"{tenantId}:accounting:servicelimits:{groupName?.ToLowerInvariant()}:{version}:userlimits";
    }

    internal static string GetGlobalServiceLimitCacheKey(int tenantId, string groupName, string version)
    {
        return $"{tenantId}:accounting:servicelimits:{groupName?.ToLowerInvariant()}:{version}:global";
    }

    internal static string GetServiceLimitsCacheVersionKey(int tenantId, string groupName)
    {
        return $"{tenantId}:accounting:servicelimits:{groupName?.ToLowerInvariant()}:version";
    }

    // A change of the limits starts a new version of their caches instead of removing the entries. A read that began
    // before the change and ends after it - a timed-out one completes in the background - stores the old limits under
    // the old version, which nobody reads any more; with the entries removed it would put them back for minutes.
    private async Task<string> GetServiceLimitsCacheVersionAsync(int tenantId, string groupName)
    {
        return await hybridCache.GetOrSetAsync<string>(GetServiceLimitsCacheVersionKey(tenantId, groupName),
            (_, _) => Task.FromResult(Guid.NewGuid().ToString("N")), opt => opt.SetDuration(_serviceLimitsCacheVersionDuration));
    }

    private async Task ClearServiceLimitsCacheAsync(int tenantId, string groupName)
    {
        await hybridCache.SetAsync(GetServiceLimitsCacheVersionKey(tenantId, groupName), Guid.NewGuid().ToString("N"), _serviceLimitsCacheVersionDuration);
    }

    public async Task<ServiceLimit> CreateServiceLimitAsync(int tenantId, string groupName, ServiceLimitScope scope, string customerParticipantName, decimal? amountValue, int? quantityValue, ServiceLimitPeriod period)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);
        var serviceLimit = await accountingClient.CreateServiceLimitAsync(portalId, groupName, scope, customerParticipantName, amountValue, quantityValue, period);

        await ClearServiceLimitsCacheAsync(tenantId, groupName);

        return serviceLimit;
    }

    /// <summary>
    /// Updates the service limit only when it is a limit on the service group and belongs to the tenant; returns null
    /// otherwise.
    /// </summary>
    public async Task<ServiceLimit> UpdateServiceLimitAsync(int tenantId, string groupName, int id, decimal? amountValue, int? quantityValue, bool? enabled)
    {
        var serviceLimit = await GetServiceLimitAsync(tenantId, groupName, id);
        if (serviceLimit == null)
        {
            return null;
        }

        try
        {
            return await accountingClient.UpdateServiceLimitAsync(id, amountValue, quantityValue, enabled);
        }
        catch (AccountingNotFoundException exception)
        {
            // Deleted by a concurrent request between the owner check and the update. A threshold the accounting service
            // rejects is a mistake of the caller and is not caught here.
            logger.DebugAccountingTenant(tenantId.ToString(), exception.Message);
            return null;
        }
        finally
        {
            await ClearServiceLimitsCacheAsync(tenantId, serviceLimit.ServiceGroupName);
        }
    }

    public async Task<int> SetParticipantServiceLimitsAsync(int tenantId, string groupName, List<string> customerParticipantNames, decimal? amountValue, int? quantityValue, ServiceLimitPeriod period)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);
        var count = await accountingClient.SetParticipantServiceLimitsAsync(portalId, groupName, customerParticipantNames, amountValue, quantityValue, period);

        await ClearServiceLimitsCacheAsync(tenantId, groupName);

        return count;
    }

    public async Task<int> DisableParticipantServiceLimitsAsync(int tenantId, string groupName)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);
        var count = await accountingClient.DisableParticipantServiceLimitsAsync(portalId, groupName);

        await ClearServiceLimitsCacheAsync(tenantId, groupName);

        return count;
    }

    /// <summary>
    /// Deletes the service limit only when it is a limit on the service group and belongs to the tenant, and returns
    /// the limit as it was before the deletion; returns null when there is no such limit of the tenant.
    /// </summary>
    public async Task<ServiceLimit> DeleteServiceLimitAsync(int tenantId, string groupName, int id)
    {
        var serviceLimit = await GetServiceLimitAsync(tenantId, groupName, id);
        if (serviceLimit == null)
        {
            return null;
        }

        try
        {
            await accountingClient.DeleteServiceLimitAsync(id);
        }
        catch (AccountingException exception) when (exception is AccountingNotFoundException or AccountingCustomerNotFoundException)
        {
            // Deleted by a concurrent request between the owner check and the deletion.
            logger.DebugAccountingTenant(tenantId.ToString(), exception.Message);
            return null;
        }
        finally
        {
            await ClearServiceLimitsCacheAsync(tenantId, serviceLimit.ServiceGroupName);
        }

        return serviceLimit;
    }

    #endregion


    private TimeSpan GetCacheExpiration()
    {
        if (coreBaseSettings.Standalone && _cacheExpiration < _standaloneCacheExpiration)
        {
            _cacheExpiration = _cacheExpiration.Add(TimeSpan.FromSeconds(30));
        }
        return _cacheExpiration;
    }

    private async Task InsertToCache(int tenantId, Tariff tariff)
    {
        await hybridCache.SetAsync(GetTariffCacheKey(tenantId), tariff, GetCacheExpiration());
    }

    private async Task<T> GetFromCache<T>(string key)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            return await hybridCache.GetOrDefaultAsync<T>(key, token: cts.Token);
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            return default;
        }

    }

    private void ResetCacheExpiration()
    {
        if (coreBaseSettings.Standalone)
        {
            _cacheExpiration = _defaultCacheExpiration;
        }
    }
}
