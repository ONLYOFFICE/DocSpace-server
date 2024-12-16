// (c) Copyright Ascensio System SIA 2009-2024
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

[Singleton]
public class TariffServiceStorage
{
    private static readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _standaloneCacheExpiration = TimeSpan.FromMinutes(15);
    private readonly ICache _cache;
    private readonly CoreBaseSettings _coreBaseSettings;
    private TimeSpan _cacheExpiration;

    public TariffServiceStorage(ICacheNotify<TariffCacheItem> notify, ICache cache, CoreBaseSettings coreBaseSettings, IServiceProvider serviceProvider)
    {
        _cacheExpiration = _defaultCacheExpiration;

        _cache = cache;
        _coreBaseSettings = coreBaseSettings;
        notify.Subscribe(i =>
        {
            _cache.Insert(TariffService.GetTariffNeedToUpdateCacheKey(i.TenantId), "update", _cacheExpiration);

            _cache.Remove(TariffService.GetTariffCacheKey(i.TenantId));
            _cache.Remove(TariffService.GetBillingUrlCacheKey(i.TenantId));
            _cache.Remove(TariffService.GetBillingPaymentCacheKey(i.TenantId)); // clear all payments
        }, CacheNotifyAction.Remove);

        notify.Subscribe(i =>
        {
            using var scope = serviceProvider.CreateScope();
            var tariffService = scope.ServiceProvider.GetService<ITariffService>();
            var tariff = tariffService.GetBillingInfoAsync(i.TenantId, i.TariffId).Result;
            if (tariff != null)
            {
                InsertToCache(i.TenantId, tariff);
            }
        }, CacheNotifyAction.Insert);
    }

    private TimeSpan GetCacheExpiration()
    {
        if (_coreBaseSettings.Standalone && _cacheExpiration < _standaloneCacheExpiration)
        {
            _cacheExpiration = _cacheExpiration.Add(TimeSpan.FromSeconds(30));
        }
        return _cacheExpiration;
    }

    public void InsertToCache(int tenantId, Tariff tariff)
    {
        _cache.Insert(TariffService.GetTariffCacheKey(tenantId), tariff, DateTime.UtcNow.Add(GetCacheExpiration()));
    }

    public void ResetCacheExpiration()
    {
        if (_coreBaseSettings.Standalone)
        {
            _cacheExpiration = _defaultCacheExpiration;
        }
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
    ICacheNotify<TariffCacheItem> notify,
    TariffServiceStorage tariffServiceStorage,
    ILogger<TariffService> logger,
    BillingClient billingClient,
    IServiceProvider serviceProvider,
    TenantExtraConfig tenantExtraConfig)
    : ITariffService
{
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

        var tariff = refresh ? null : cache.Get<Tariff>(GetTariffCacheKey(tenantId));

        if (tariff == null)
        {
            tariff = await GetBillingInfoAsync(tenantId) ?? await CreateDefaultAsync();
            tariff = await CalculateTariffAsync(tenantId, tariff);
            tariffServiceStorage.InsertToCache(tenantId, tariff);

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

                        asynctariff.Id = currentPayment.PaymentId;

                        var paymentEndDate = 9999 <= currentPayment.EndDate.Year ? DateTime.MaxValue : currentPayment.EndDate;
                        asynctariff.DueDate = DateTime.Compare(asynctariff.DueDate, paymentEndDate) < 0 ? asynctariff.DueDate : paymentEndDate;

                        asynctariff.Quotas = asynctariff.Quotas.Where(r => r.Id != quota.TenantId).ToList();
                        asynctariff.Quotas.Add(new Quota(quota.TenantId, currentPayment.Quantity));
                        email = currentPayment.PaymentEmail;
                    }

                    TenantQuota updatedQuota = null;

                    foreach (var quota in asynctariff.Quotas)
                    {
                        var tenantQuota = tenantQuotas.SingleOrDefault(q => q.TenantId == quota.Id);

                        tenantQuota *= quota.Quantity;
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

                    await UpdateCacheAsync(tariff.Id);
                }
                catch (Exception error)
                {
                    if (error is not BillingNotFoundException)
                    {
                        LogError(error, tenantId.ToString());
                    }
                }

                if (tariff.Id == 0)
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

                    var asynctariff = await CreateDefaultAsync();

                    if (freeTariff == null)
                    {
                        asynctariff.DueDate = DateTime.Today.AddDays(-1);
                        asynctariff.State = TariffState.NotPaid;
                    }

                    if (await SaveBillingInfoAsync(tenantId, asynctariff))
                    {
                        asynctariff = await CalculateTariffAsync(tenantId, asynctariff);
                        tariff = asynctariff;
                    }

                    await UpdateCacheAsync(tariff.Id);
                }
            }
            else if (tenantExtraConfig.Enterprise && tariff.Id == 0 && tariff.LicenseDate == DateTime.MaxValue)
            {
                var defaultQuota = await quotaService.GetTenantQuotaAsync(Tenant.DefaultTenant);

                var quota = new TenantQuota(defaultQuota)
                {
                    Name = "start_trial",
                    Trial = true,
                    TenantId = -1000
                };

                await quotaService.SaveTenantQuotaAsync(quota);

                tariff = new Tariff
                {
                    Quotas = [new(quota.TenantId, 1)],
                    DueDate = DateTime.UtcNow.AddDays(DefaultTrialPeriod)
                };

                await SetTariffAsync(Tenant.DefaultTenant, tariff, [quota]);
                await UpdateCacheAsync(tariff.Id);
            }
        }
        else
        {
            tariff = await CalculateTariffAsync(tenantId, tariff);
        }

        return tariff;

        async Task UpdateCacheAsync(int tariffId)
        {
            await notify.PublishAsync(new TariffCacheItem { TenantId = tenantId, TariffId = tariffId }, CacheNotifyAction.Insert);
        }
    }

    public async Task<bool> PaymentChangeAsync(int tenantId, Dictionary<string, int> quantity)
    {
        if (quantity == null || quantity.Count == 0
            || !billingClient.Configured)
        {
            return false;
        }

        var allQuotas = (await quotaService.GetTenantQuotasAsync()).Where(q => !string.IsNullOrEmpty(q.ProductId)).ToList();
        var newQuotas = quantity.Keys.Select(name => allQuotas.Find(q => q.Name == name)).ToList();

        var tariff = await GetTariffAsync(tenantId);

        // update the quantity of present quotas
        TenantQuota updatedQuota = null;
        foreach (var tariffRow in tariff.Quotas)
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
            updatedQuota += quota;
        }

        // add new quotas
        var addedQuotas = newQuotas.Where(q => !tariff.Quotas.Exists(t => t.Id == q.TenantId));
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

        try
        {
            var changed = await billingClient.ChangePaymentAsync(await coreSettings.GetKeyAsync(tenantId), productIds.ToArray(), quantity.Values.ToArray());

            if (!changed)
            {
                return false;
            }

            await ClearCacheAsync(tenantId);
        }
        catch (Exception error)
        {
            logger.ErrorWithException(error);
        }

        return true;
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

    internal static string GetTariffNeedToUpdateCacheKey(int tenantId)
    {
        return $"{tenantId}:update";
    }

    internal static string GetBillingUrlCacheKey(int tenantId)
    {
        return $"{tenantId}:billing:urls";
    }

    internal static string GetBillingPaymentCacheKey(int tenantId)
    {
        return $"{tenantId}:billing:payments";
    }


    private async Task ClearCacheAsync(int tenantId)
    {
        await notify.PublishAsync(new TariffCacheItem { TenantId = tenantId, TariffId = -1 }, CacheNotifyAction.Remove);
    }

    public async Task<IEnumerable<PaymentInfo>> GetPaymentsAsync(int tenantId)
    {
        var key = GetBillingPaymentCacheKey(tenantId);
        var payments = cache.Get<List<PaymentInfo>>(key);
        if (payments == null)
        {
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

            cache.Insert(key, payments, DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
        }

        return payments;
    }

    public async Task<Uri> GetShoppingUriAsync(int tenant, string affiliateId, string partnerId, string currency = null, string language = null, string customerEmail = null, Dictionary<string, int> quantity = null, string backUrl = null)
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
                var productIds = newQuotas.Select(q => q.ProductId);

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

        tariffServiceStorage.ResetCacheExpiration();

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

    public async Task<IDictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(string partnerId, params string[] productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        var def = productIds
            .Select(p => new { ProductId = p, Prices = new Dictionary<string, decimal>() })
            .ToDictionary(e => e.ProductId, e => e.Prices);

        if (billingClient.Configured)
        {
            try
            {
                var key = $"billing-prices-{partnerId}-{string.Join(",", productIds)}";
                var result = cache.Get<IDictionary<string, Dictionary<string, decimal>>>(key);
                if (result == null)
                {
                    result = await billingClient.GetProductPriceInfoAsync(partnerId, productIds);
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
        tariff.Quotas = await coreDbContext.QuotasAsync(r.TenantId, r.Id).ToListAsync();

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

    private async Task AddDefaultQuotaAsync(Tariff tariff)
    {
        var allQuotas = await quotaService.GetTenantQuotasAsync();
        var toAdd = TrialEnabled ? 
            allQuotas.FirstOrDefault(r => r.Trial && !r.Custom) : 
            allQuotas.FirstOrDefault(r => coreBaseSettings.Standalone || r.Free && !r.Custom);

        if (toAdd != null)
        {
            tariff.Quotas.Add(new Quota(toAdd.TenantId, 1));
        }
    }

    private void LogError(Exception error, string tenantId = null)
    {
        if (error is BillingNotFoundException)
        {
            logger.DebugPaymentTenant(tenantId, error.Message);
        }
        else if (error is BillingNotConfiguredException)
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
            var qty = tariffRow.Quantity;

            var quota = await quotaService.GetTenantQuotaAsync(tariffRow.Id);

            quota *= qty;
            result += quota;
        }

        return result;
    }

    public int GetPaymentDelay()
    {
        return PaymentDelay;
    }

    public bool IsConfigured()
    {
        return billingClient.Configured;
    }
}
