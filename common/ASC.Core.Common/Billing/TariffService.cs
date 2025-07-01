// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Core.Billing;

[Singleton]
public class TenantExtraConfig(CoreBaseSettings coreBaseSettings, LicenseReaderConfig licenseReaderConfig)
{
    public bool Saas
    {
        get { return !coreBaseSettings.Standalone; }
    }

    public bool Enterprise
    {
        get { return coreBaseSettings.Standalone && !string.IsNullOrEmpty(licenseReaderConfig.LicensePath); }
    }

    public bool Developer
    {
        get { return Enterprise && licenseReaderConfig.LicenseType == LicenseType.Developer; }
    }

    public bool Opensource
    {
        get { return coreBaseSettings.Standalone && string.IsNullOrEmpty(licenseReaderConfig.LicensePath); }
    }
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
    IServiceProvider serviceProvider,
    TenantExtraConfig tenantExtraConfig)
    : ITariffService
{
    private static readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _standaloneCacheExpiration = TimeSpan.FromMinutes(15);
    private TimeSpan _cacheExpiration = _defaultCacheExpiration;
    
    private const int DefaultTrialPeriod = 30;

    //private readonly int _activeUsersMin;
    //private readonly int _activeUsersMax;

    private int PaymentDelay => PaymentConfiguration.Delay;
    private bool TrialEnabled => PaymentConfiguration.TrialEnabled;
    
    private PaymentConfiguration _paymentConfiguration;
    private PaymentConfiguration PaymentConfiguration => _paymentConfiguration ??= (configuration.GetSection("core:payment").Get<PaymentConfiguration>() ?? new PaymentConfiguration());

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
                            if (quota.Wallet)
                            {
                                quotaDueDate = currentPayment.EndDate;
                                var existingQuota = tariff.Quotas.FirstOrDefault(x => x.Id == quota.TenantId);
                                if (existingQuota != null && existingQuota.DueDate == currentPayment.EndDate && existingQuota.Quantity == currentPayment.Quantity)
                                {
                                    nextQuantity = existingQuota.NextQuantity;
                                }
                            }
                            else
                            {
                                var paymentEndDate = 9999 <= currentPayment.EndDate.Year ? DateTime.MaxValue : currentPayment.EndDate;
                                asynctariff.DueDate = DateTime.Compare(asynctariff.DueDate, paymentEndDate) < 0 ? asynctariff.DueDate : paymentEndDate;
                            }

                            asynctariff.Quotas = asynctariff.Quotas.Where(r => r.Id != quota.TenantId).ToList();
                            asynctariff.Quotas.Add(new Quota(quota.TenantId, currentPayment.Quantity, quota.Wallet, quotaDueDate, nextQuantity));
                            email = currentPayment.PaymentEmail;
                        }

                        // need sort by wallet
                        asynctariff.Quotas = asynctariff.Quotas.OrderBy(q => q.Wallet).ToList();

                        if (asynctariff.Quotas.All(q => q.Wallet))
                        {
                            if (tariff.Id != 0 && tariff.State >= TariffState.Paid && !await IsFreeTariffAsync(tariff))
                            {
                                throw new BillingNotFoundException($"Payment {tariff.Id} not found. Only wallet payments available");
                            }
                            else
                            {
                                await AddInitialQuotaAsync(asynctariff, tenantId);
                            }
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

    public async Task<bool> PaymentChangeAsync(int tenantId, Dictionary<string, int> quantity, ProductQuantityType productQuantityType, string currency, bool checkQuota)
    {
        if (quantity == null || quantity.Count == 0 || !billingClient.Configured)
        {
            return false;
        }

        var productIds = checkQuota ? await CheckQuotaAndGetProductIds(tenantId, quantity) : await GetProductIds(quantity);

        try
        {
            var changed = await billingClient.ChangePaymentAsync(await coreSettings.GetKeyAsync(tenantId), productIds, quantity.Values, productQuantityType, currency);

            if (!changed)
            {
                return false;
            }

            await ClearCacheAsync(tenantId);
        }
        catch (Exception error)
        {
            logger.ErrorWithException(error);

            return false;
        }

        return true;
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

    public async Task SetTariffAsync(int tenantId, Tariff tariff, List<TenantQuota> quotas = null)
    {
        ArgumentNullException.ThrowIfNull(tariff);

        if (tariff.Quotas == null ||
            (quotas ??= await tariff.Quotas.ToAsyncEnumerable().SelectAwait(async q => await quotaService.GetTenantQuotaAsync(q.Id)).ToListAsync()).Any(q => q == null))
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

    public async Task<Uri> GetShoppingUriAsync(int tenant, string affiliateId, string partnerId, string currency = null, string language = null, string customerEmail = null, Dictionary<string, int> quantity = null, string backUrl = null, bool checkoutSetup = false)
    {
        List<TenantQuota> newQuotas = [];

        if (billingClient.Configured)
        {
            var allQuotas = (await quotaService.GetTenantQuotasAsync()).Where(q => !string.IsNullOrEmpty(q.ProductId) && q.Visible).ToList();
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
                            productIds.ToArray(),
                            affiliateId,
                            partnerId,
                            null,
                            !string.IsNullOrEmpty(currency) ? "__Currency__" : null,
                            !string.IsNullOrEmpty(language) ? "__Language__" : null,
                            !string.IsNullOrEmpty(customerEmail) ? "__CustomerEmail__" : null,
                            hasQuantity ? "__Quantity__" : null,
                            !string.IsNullOrEmpty(backUrl) ? "__BackUrl__" : null
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
                               .Replace("__BackUrl__", HttpUtility.UrlEncode(backUrl ?? "")));
        return result;
    }

    public async Task<IDictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(string partnerId, bool wallet, string[] productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        var def = productIds
            .Select(p => new { ProductId = p, Prices = new Dictionary<string, decimal>() })
            .ToDictionary(e => e.ProductId, e => e.Prices);

        if (productIds.Length == 0)
        {
            return def;
        }

        if (billingClient.Configured)
        {
            try
            {
                var key = $"billing-prices-{partnerId}-{string.Join(",", productIds)}";
                var result = cache.Get<IDictionary<string, Dictionary<string, decimal>>>(key);
                if (result == null)
                {
                    result = await billingClient.GetProductPriceInfoAsync(partnerId, wallet, productIds);
                    cache.Insert(key, result, DateTime.Now.AddHours(1));
                }

                return result;
            }
            catch (Exception error)
            {
                LogError(error);
            }
        }

        return def;
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
        tariff.DueDate = r.Stamp.Year < 9999 ? r.Stamp : DateTime.MaxValue;
        tariff.CustomerId = r.CustomerId;

        var quotas = await coreDbContext.QuotasAsync(r.TenantId, r.Id).ToListAsync();

        foreach (var q in quotas)
        {
            if (q.State.HasValue && q.State.Value == QuotaState.Overdue)
            {
                tariff.OverdueQuotas ??= [];
                tariff.OverdueQuotas.Add(q);
            }
            else
            {
                tariff.Quotas.Add(q);
            }
        }

        if (tariff.Quotas.All(q => q.Wallet))
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

                var stamp = tariffInfo.DueDate;
                if (stamp.Equals(DateTime.MaxValue))
                {
                    stamp = stamp.Date.Add(new TimeSpan(tariffInfo.DueDate.Hour, tariffInfo.DueDate.Minute, tariffInfo.DueDate.Second));
                }

                var efTariff = new DbTariff
                {
                    Id = tariffInfo.Id,
                    TenantId = tenant,
                    Stamp = stamp,
                    CustomerId = tariffInfo.CustomerId,
                    CreateOn = DateTime.UtcNow
                };

                if (efTariff.Id == 0)
                {
                    efTariff.Id = (-tenant);
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
                        DueDate = q.DueDate,
                        NextQuantity = q.NextQuantity,
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

    public async Task<bool> UpdateNextQuantityAsync(int tenant, Tariff tariffInfo, int quotaId, int? nextQuantity)
    {
        try
        {
            if (nextQuantity.HasValue && nextQuantity.Value < 0)
            {
                return false;
            }

            var currentTariff = await GetBillingInfoAsync(tenant);

            await using var dbContext = await coreDbContextManager.CreateDbContextAsync();

            foreach (var q in tariffInfo.Quotas)
            {
                if (q.Id == quotaId)
                {
                    q.NextQuantity = nextQuantity;

                    await dbContext.AddOrUpdateAsync(quota => quota.TariffRows, new DbTariffRow
                    {
                        TariffId = tariffInfo.Id,
                        Quota = q.Id,
                        Quantity = q.Quantity,
                        DueDate = q.DueDate,
                        NextQuantity = nextQuantity,
                        TenantId = tenant
                    });
                }
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
            var trial = await tariff.Quotas.ToAsyncEnumerable().AnyAwaitAsync(async q => (await quotaService.GetTenantQuotaAsync(q.Id)).Trial);
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

    private async Task<bool> IsFreeTariffAsync(Tariff tariff)
    {
        var freeTariff = await tariff.Quotas.ToAsyncEnumerable().FirstOrDefaultAwaitAsync(async tariffRow =>
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

    public async Task<bool> TopUpDepositAsync(int tenantId, decimal amount, string currency, bool waitForChanges = false)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);

        decimal? oldBalanceAmount = 0;

        if (waitForChanges)
        {
            var oldBalance = await GetCustomerBalanceAsync(tenantId);
            oldBalanceAmount = oldBalance?.SubAccounts?.FirstOrDefault(x => x.Currency == currency)?.Amount;
        }

        var result = false;

        try
        {
            result = await billingClient.TopUpDepositAsync(portalId, amount, currency);
        }
        catch (Exception error)
        {
            logger.ErrorWithException(error);
        }

        if (!result || !waitForChanges)
        {
            return result;
        }

        var retryPolicy = Policy
            .HandleResult<bool>(result => result == false)
            .WaitAndRetryAsync(15, retryAttempt => TimeSpan.FromSeconds(1));

        var updated = await retryPolicy.ExecuteAsync(async () =>
        {
            var newBalance = await GetCustomerBalanceAsync(tenantId, true);
            var newBalanceAmount = newBalance?.SubAccounts?.FirstOrDefault(x => x.Currency == currency)?.Amount;

            return oldBalanceAmount != newBalanceAmount;
        });

        if (!updated)
        {
            logger.ErrorBilling(tenantId.ToString(), "Balance value is not updated after replenishment");
            await hybridCache.RemoveAsync(GetAccountingBalanceCacheKey(tenantId));
        }

        return result;
    }

    #region Accounting

    public async Task<Balance> GetCustomerBalanceAsync(int tenantId, bool refresh = false)
    {
        var cacheKey = GetAccountingBalanceCacheKey(tenantId);

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

            if (accountingClient.Configured)
            {
                try
                {
                    var portalId = await coreSettings.GetKeyAsync(tenantId);
                    balance = await accountingClient.GetCustomerBalanceAsync(portalId, true);
                    await hybridCache.SetAsync(cacheKey, balance, TimeSpan.FromMinutes(10));
                }
                catch (Exception error)
                {
                    LogError(error, tenantId.ToString());
                    await hybridCache.SetAsync(cacheKey, new Balance(), TimeSpan.FromMinutes(10));
                }
            }
        }

        return balance;
    }

    public async Task<Session> OpenCustomerSessionAsync(int tenantId, int serviceAccount, string externalRef, int quantity)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);
        return await accountingClient.OpenCustomerSessionAsync(portalId, serviceAccount, externalRef, quantity);
    }

    public async Task<bool> PerformCustomerOperationAsync(int tenantId, int serviceAccount, int sessionId, int quantity)
    {
        var portalId = await coreSettings.GetKeyAsync(tenantId);
        await accountingClient.PerformCustomerOperationAsync(portalId, serviceAccount, sessionId, quantity);
        await hybridCache.RemoveAsync(GetAccountingBalanceCacheKey(tenantId));
        return true;
    }

    public async Task<Report> GetCustomerOperationsAsync(int tenantId, DateTime utcStartDate, DateTime utcEndDate, bool? credit, bool? withdrawal, int? offset, int? limit)
    {
        try
        {
            var portalId = await coreSettings.GetKeyAsync(tenantId);
            return await accountingClient.GetCustomerOperationsAsync(portalId, utcStartDate, utcEndDate, credit, withdrawal, offset, limit);
        }
        catch (Exception error)
        {
            LogError(error, tenantId.ToString());
            return null;
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
            return await hybridCache.GetOrDefaultAsync<T>(key, token: new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
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
