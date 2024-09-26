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

namespace ASC.Core;

[Scope]
public class TenantManager(
    ITenantService tenantService,
    IQuotaService quotaService,
    IHttpContextAccessor httpContextAccessor,
    ITariffService tariffService,
    CoreBaseSettings coreBaseSettings,
    CoreSettings coreSettings,
    IDistributedLockProvider distributedLockProvider)
{
    private Tenant _currentTenant;
    public const string CouldNotResolveCurrentTenant = "Could not resolve current tenant :-(.";

    private const string CurrentTenant = "CURRENT_TENANT";

    private static readonly List<string> _thisCompAddresses = [];
    

    static TenantManager()
    {
        _thisCompAddresses.Add("localhost");
        _thisCompAddresses.Add(Dns.GetHostName().ToLowerInvariant());
        _thisCompAddresses.AddRange(Dns.GetHostAddresses("localhost").Select(a => a.ToString()));
        try
        {
            _thisCompAddresses.AddRange(Dns.GetHostAddresses(Dns.GetHostName()).Select(a => a.ToString()));
        }
        catch
        {
            // ignore
        }
    }


    public async Task<List<Tenant>> GetTenantsAsync(bool active = true)
    {
        return (await tenantService.GetTenantsAsync(default, active)).ToList();
    }
    
    public Task<IEnumerable<Tenant>> GetTenantsAsync(List<int> ids)
    {
        return tenantService.GetTenantsAsync(ids);
    }

    public Task<Tenant> GetTenantAsync(int tenantId)
    {
        return tenantService.GetTenantAsync(tenantId);
    }

    public async Task<Tenant> GetTenantAsync(string domain)
    {
        if (string.IsNullOrEmpty(domain))
        {
            return null;
        }

        Tenant t = null;
        if (_thisCompAddresses.Contains(domain, StringComparer.InvariantCultureIgnoreCase))
        {
            t = await tenantService.GetTenantAsync("localhost");
        }
        var isAlias = false;
        if (t == null)
        {
            var baseUrl = coreSettings.BaseDomain;
            if (!string.IsNullOrEmpty(baseUrl) && domain.EndsWith("." + baseUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                isAlias = true;
                t = await tenantService.GetTenantAsync(domain[..(domain.Length - baseUrl.Length - 1)]);
            }
        }
        
        t ??= await tenantService.GetTenantAsync(domain);
        
        if (t == null && coreBaseSettings.Standalone && !isAlias)
        {
            t = await tenantService.GetTenantForStandaloneWithoutAliasAsync(domain);
        }

        return t;
    }

    private Tenant GetTenant(string domain)
    {
        if (string.IsNullOrEmpty(domain))
        {
            return null;
        }

        Tenant t = null;
        if (_thisCompAddresses.Contains(domain, StringComparer.InvariantCultureIgnoreCase))
        {
            t = tenantService.GetTenant("localhost");
        }

        var isAlias = false;
        if (t == null)
        {
            var baseUrl = coreSettings.BaseDomain;
            if (!string.IsNullOrEmpty(baseUrl) && domain.EndsWith("." + baseUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                isAlias = true;
                t = tenantService.GetTenant(domain[..(domain.Length - baseUrl.Length - 1)]);
            }
        }
        
        t ??= tenantService.GetTenant(domain);
        
        if (t == null && coreBaseSettings.Standalone && !isAlias)
        {
            t = tenantService.GetTenantForStandaloneWithoutAlias(domain);
        }

        return t;
    }

    public async Task SetTenantVersionAsync(Tenant tenant, int version)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (tenant.Version != version)
        {
            tenant.Version = version;
            await SaveTenantAsync(tenant);
        }
        else
        {
            throw new ArgumentException("This is current version already");
        }
    }

    public async Task<Tenant> SaveTenantAsync(Tenant tenant)
    {
        var newTenant = await tenantService.SaveTenantAsync(coreSettings, tenant);
        if (CallContext.GetData(CurrentTenant) is Tenant)
        {
            SetCurrentTenant(newTenant);
        }

        return newTenant;
    }
    
    public async Task<Tenant> RestoreTenantAsync(int oldId, Tenant newTenant)
    {
        newTenant = await tenantService.RestoreTenantAsync(oldId, newTenant, coreSettings);
        SetCurrentTenant(newTenant);

        return newTenant;
    }

    public async Task RemoveTenantAsync(int tenantId, bool auto = false)
    {
        await tenantService.RemoveTenantAsync(tenantId, auto);
    }

    public Task<Tenant> GetCurrentTenantAsync(bool throwIfNotFound, HttpContext context)
    {
        return _currentTenant != null ? 
            Task.FromResult(_currentTenant) : 
            GetCurrentTenantFromDbAsync(throwIfNotFound, context);
    }

    private async Task<Tenant> GetCurrentTenantFromDbAsync(bool throwIfNotFound, HttpContext context)
    {
        if (_currentTenant != null)
        {
            return _currentTenant;
        }

        Tenant tenant = null;

        if (context != null)
        {
            tenant = context.Items[CurrentTenant] as Tenant;
            if (tenant == null)
            {
                tenant = await GetTenantAsync(context.Request.Url().Host);
                context.Items[CurrentTenant] = tenant;
            }

            if (tenant == null)
            {
                var origin = context.Request.Headers[HeaderNames.Origin].FirstOrDefault();

                if (!string.IsNullOrEmpty(origin))
                {
                    var originUri = new Uri(origin);

                    tenant = await GetTenantAsync(originUri.Host);
                    context.Items[CurrentTenant] = tenant;
                }
            }
        }

        if (tenant == null && throwIfNotFound)
        {
            throw new Exception(CouldNotResolveCurrentTenant);
        }

        _currentTenant = tenant;

        return tenant;
    }

    public async Task<int> GetCurrentTenantIdAsync()
    {
        return (await GetCurrentTenantAsync()).Id;
    }

    public Task<Tenant> GetCurrentTenantAsync(bool throwIfNotFound = true)
    {
        return GetCurrentTenantAsync(throwIfNotFound, httpContextAccessor?.HttpContext);
    }
    
    public Tenant GetCurrentTenant(bool throwIfNotFound = true)
    {
        if (_currentTenant != null)
        {
            return _currentTenant;
        }

        Tenant tenant = null;

        var context = httpContextAccessor?.HttpContext;
        
        if (context != null)
        {
            tenant = context.Items[CurrentTenant] as Tenant;
            if (tenant == null)
            {
                tenant = GetTenant(context.Request.Url().Host);
                context.Items[CurrentTenant] = tenant;
            }

            if (tenant == null)
            {
                var origin = context.Request.Headers[HeaderNames.Origin].FirstOrDefault();

                if (!string.IsNullOrEmpty(origin))
                {
                    var originUri = new Uri(origin);

                    tenant = GetTenant(originUri.Host);
                    context.Items[CurrentTenant] = tenant;
                }
            }
        }

        if (tenant == null && throwIfNotFound)
        {
            throw new Exception(CouldNotResolveCurrentTenant);
        }

        _currentTenant = tenant;

        return tenant;
    }

    public void SetCurrentTenant(Tenant tenant)
    {
        if (tenant != null)
        {
            _currentTenant = tenant;
            if (httpContextAccessor?.HttpContext != null)
            {
                httpContextAccessor.HttpContext.Items[CurrentTenant] = tenant;
            }

            CultureInfo.CurrentCulture = tenant.GetCulture();
            CultureInfo.CurrentUICulture = tenant.GetCulture();
        }
    }

    public async Task<Tenant> SetCurrentTenantAsync(int tenantId)
    {
        var result = await GetTenantAsync(tenantId);
        SetCurrentTenant(result);

        return result;
    }

    public async Task SetCurrentTenantAsync(string domain)
    {
        var result = await GetTenantAsync(domain);
        SetCurrentTenant(result);
    }

    public async Task CheckTenantAddressAsync(string address)
    {
        await tenantService.ValidateDomainAsync(address);
    }

    public async Task<IEnumerable<TenantVersion>> GetTenantVersionsAsync()
    {
        return await tenantService.GetTenantVersionsAsync();
    }


    public async Task<IEnumerable<TenantQuota>> GetTenantQuotasAsync()
    {
        return await GetTenantQuotasAsync(false);
    }

    public async Task<IEnumerable<TenantQuota>> GetTenantQuotasAsync(bool all)
    {
        return (await quotaService.GetTenantQuotasAsync()).Where(q => q.TenantId < 0 && (all || q.Visible)).OrderByDescending(q => q.TenantId).ToList();
    }

    public async Task<TenantQuota> GetCurrentTenantQuotaAsync(bool refresh = false)
    {
        return await GetTenantQuotaAsync(await GetCurrentTenantIdAsync(), refresh);
    }

    public async Task<TenantQuota> GetTenantQuotaAsync(int tenant, bool refresh = false)
    {
        var defaultQuota = await quotaService.GetTenantQuotaAsync(tenant) ?? await quotaService.GetTenantQuotaAsync(Tenant.DefaultTenant) ?? TenantQuota.Default;
        if (defaultQuota.TenantId != tenant && tariffService != null)
        {
            var tariff = await tariffService.GetTariffAsync(tenant, refresh: refresh);

            TenantQuota currentQuota = null;
            foreach (var tariffRow in tariff.Quotas)
            {
                var qty = tariffRow.Quantity;

                var quota = await quotaService.GetTenantQuotaAsync(tariffRow.Id);

                quota *= qty;
                currentQuota += quota;
            }

            return currentQuota;
        }

        return defaultQuota;
    }

    public async Task<IDictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync()
    {
        var quotas = await GetTenantQuotasAsync(false);
        var productIds = quotas
            .Select(p => p.ProductId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToArray();
        
        var tenant = await GetCurrentTenantAsync(false);
        var prices = await tariffService.GetProductPriceInfoAsync(tenant?.PartnerId, productIds);
        var result = prices.ToDictionary(price => quotas.First(quota => quota.ProductId == price.Key).Name, price => price.Value);
        return result;
    }

    public Dictionary<string, decimal> GetProductPriceInfo(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return null;
        }

        var tenant = GetCurrentTenant(false);
        var prices = tariffService.GetProductPriceInfoAsync(tenant?.PartnerId, productId).Result;
        return prices.TryGetValue(productId, out var price) ? price : null;
    }

    public async Task<TenantQuota> SaveTenantQuotaAsync(TenantQuota quota)
    {
        quota = await quotaService.SaveTenantQuotaAsync(quota);

        return quota;
    }

    public async Task SetTenantQuotaRowAsync(TenantQuotaRow row, bool exchange)
    {
        await using (await distributedLockProvider.TryAcquireFairLockAsync($"quota_{row.TenantId}_{row.Path}_{row.UserId}"))
        {
            await quotaService.SetTenantQuotaRowAsync(row, exchange);
        }
    }

    public async Task<List<TenantQuotaRow>> FindTenantQuotaRowsAsync(int tenantId)
    {
        return (await quotaService.FindTenantQuotaRowsAsync(tenantId)).ToList();
    }

    public void ValidateTenantName(string name)
    {
        tenantService.ValidateTenantName(name);
    }
}
