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

namespace ASC.Core.Caching;

[Singleton]
internal class TenantServiceCache
{
    private const string Key = "tenants";
    private readonly TimeSpan _cacheExpiration;
    internal readonly ICache Cache;
    internal readonly ICacheNotify<TenantCacheItem> CacheNotifyItem;

    public TenantServiceCache(
        CoreBaseSettings coreBaseSettings,
        ICacheNotify<TenantCacheItem> cacheNotifyItem,
        ICache cache)
    {
        CacheNotifyItem = cacheNotifyItem;
        Cache = cache;
        _cacheExpiration = TimeSpan.FromMinutes(2);

        cacheNotifyItem.Subscribe(t =>
        {
            var tenants = GetTenantStore();
            tenants.Remove(t.TenantId);
            tenants.Clear(coreBaseSettings);
        }, CacheNotifyAction.InsertOrUpdate);
    }

    internal TenantStore GetTenantStore()
    {
        var store = Cache.Get<TenantStore>(Key);
        if (store == null)
        {
            store = new TenantStore();
            Cache.Insert(Key, store, DateTime.UtcNow.Add(_cacheExpiration));
        }

        return store;
    }

    internal class TenantStore
    {
        private readonly Dictionary<int, Tenant> _byId = new();
        private readonly Dictionary<string, Tenant> _byDomain = new();
        private readonly Lock _locker = new();

        public Tenant Get(int id)
        {
            Tenant t;
            lock (_locker)
            {
                _byId.TryGetValue(id, out t);
            }

            return t;
        }

        public Tenant Get(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return null;
            }

            Tenant t;
            lock (_locker)
            {
                _byDomain.TryGetValue(domain, out t);
            }

            return t;
        }

        public void Insert(Tenant t, string ip = null)
        {
            if (t == null)
            {
                return;
            }

            Remove(t.Id);
            lock (_locker)
            {
                _byId[t.Id] = t;
                _byDomain[t.Alias] = t;
                if (!string.IsNullOrEmpty(t.MappedDomain))
                {
                    _byDomain[t.MappedDomain] = t;
                }

                if (!string.IsNullOrEmpty(ip))
                {
                    _byDomain[ip] = t;
                }
            }
        }

        public void Remove(int id)
        {
            var t = Get(id);
            if (t != null)
            {
                lock (_locker)
                {
                    _byId.Remove(id);
                    _byDomain.Remove(t.Alias);
                    if (!string.IsNullOrEmpty(t.MappedDomain))
                    {
                        _byDomain.Remove(t.MappedDomain);
                    }
                }
            }
        }

        internal void Clear(CoreBaseSettings coreBaseSettings)
        {
            if (!coreBaseSettings.Standalone)
            {
                return;
            }

            lock (_locker)
            {
                _byId.Clear();
                _byDomain.Clear();
            }
        }
    }
}

[Scope(typeof(ITenantService))]
internal class CachedTenantService : ITenantService
{
    private readonly DbTenantService _service;
    private readonly ICacheNotify<TenantCacheItem> _cacheNotifyItem;
    private readonly TenantServiceCache _tenantServiceCache;
    private static readonly TimeSpan _settingsExpiration = TimeSpan.FromMinutes(2);
    private readonly IFusionCache _fusionCache;

    public CachedTenantService(DbTenantService service, TenantServiceCache tenantServiceCache, IFusionCacheProvider cacheProvider)
    {
        _fusionCache = cacheProvider.GetMemoryCache();
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _tenantServiceCache = tenantServiceCache;
        _cacheNotifyItem = tenantServiceCache.CacheNotifyItem;
    }

    public async Task ValidateDomainAsync(string domain)
    {
        await _service.ValidateDomainAsync(domain);
    }

    public async Task<bool> IsForbiddenDomainAsync(string domain)
    {
        return await _service.IsForbiddenDomainAsync(domain);
    }

    public void ValidateTenantName(string name)
    {
        _service.ValidateTenantName(name);
    }

    public async Task<IEnumerable<Tenant>> GetTenantsAsync(string login, string passwordHash)
    {
        return await _service.GetTenantsAsync(login, passwordHash);
    }

    public async Task<IEnumerable<Tenant>> GetTenantsAsync(DateTime from, bool active = true)
    {
        return await _service.GetTenantsAsync(from, active);
    }

    public async Task<IEnumerable<Tenant>> GetTenantsAsync(List<int> ids)
    {
        return await _service.GetTenantsAsync(ids);
    }

    public async Task<Tenant> RestoreTenantAsync(Tenant oldTenant, Tenant newTenant, CoreSettings coreSettings)
    {
        newTenant = await _service.RestoreTenantAsync(oldTenant, newTenant, coreSettings);
        await _cacheNotifyItem.PublishAsync(new TenantCacheItem { TenantId = oldTenant.Id }, CacheNotifyAction.InsertOrUpdate);
        await _cacheNotifyItem.PublishAsync(new TenantCacheItem { TenantId = newTenant.Id }, CacheNotifyAction.InsertOrUpdate);
        return newTenant;
    }

    public async Task<Tenant> GetTenantAsync(int id)
    {
        var tenants = _tenantServiceCache.GetTenantStore();
        var t = tenants.Get(id);
        if (t == null)
        {
            t = await _service.GetTenantAsync(id);
            if (t != null)
            {
                tenants.Insert(t);
            }
        }

        return t;
    }

    public async Task<Tenant> GetTenantAsync(string domain)
    {
        var tenants = _tenantServiceCache.GetTenantStore();
        var t = tenants.Get(domain);
        if (t == null)
        {
            t = await _service.GetTenantAsync(domain);
            if (t != null)
            {
                tenants.Insert(t);
            }
        }

        return t;
    }

    public Tenant GetTenant(string domain)
    {
        var tenants = _tenantServiceCache.GetTenantStore();
        var t = tenants.Get(domain);
        if (t == null)
        {
            t = _service.GetTenant(domain);
            if (t != null)
            {
                tenants.Insert(t);
            }
        }

        return t;
    }

    public Tenant GetTenantForStandaloneWithoutAlias(string ip)
    {
        var tenants = _tenantServiceCache.GetTenantStore();
        var t = tenants.Get(ip);
        if (t == null)
        {
            t = _service.GetTenantForStandaloneWithoutAlias(ip);
            if (t != null)
            {
                tenants.Insert(t, ip);
            }
        }

        return t;
    }

    public async Task<Tenant> GetTenantForStandaloneWithoutAliasAsync(string ip)
    {
        var tenants = _tenantServiceCache.GetTenantStore();
        var t = tenants.Get(ip);
        if (t == null)
        {
            t = await _service.GetTenantForStandaloneWithoutAliasAsync(ip);
            if (t != null)
            {
                tenants.Insert(t, ip);
            }
        }

        return t;
    }

    public async Task<Tenant> SaveTenantAsync(CoreSettings coreSettings, Tenant tenant)
    {
        tenant = await _service.SaveTenantAsync(coreSettings, tenant);
        await _cacheNotifyItem.PublishAsync(new TenantCacheItem { TenantId = tenant.Id }, CacheNotifyAction.InsertOrUpdate);

        return tenant;
    }

    public async Task RemoveTenantAsync(Tenant tenant, bool auto = false)
    {
        await _service.RemoveTenantAsync(tenant, auto);
        await _cacheNotifyItem.PublishAsync(new TenantCacheItem { TenantId = tenant.Id }, CacheNotifyAction.InsertOrUpdate);
    }

    public async Task PermanentlyRemoveTenantAsync(int id)
    {
        await _service.PermanentlyRemoveTenantAsync(id);
        await _cacheNotifyItem.PublishAsync(new TenantCacheItem { TenantId = id }, CacheNotifyAction.Remove);
    }

    public async Task<IEnumerable<TenantVersion>> GetTenantVersionsAsync()
    {
        return await _service.GetTenantVersionsAsync();
    }

    public async Task<byte[]> GetTenantSettingsAsync(int tenant, string key)
    {
        var cacheKey = GetCacheKey(tenant, key);

        var data = await _fusionCache.GetOrSetAsync<byte[]>(cacheKey, async (ctx, token) =>
        {
            var data = await _service.GetTenantSettingsAsync(tenant, key);

            return ctx.Modified(data);
        }, _settingsExpiration, [CacheExtention.GetTenantSettingsTag(tenant, key)]);

        return data == null ? null : data.Length == 0 ? null : data;
    }

    public byte[] GetTenantSettings(int tenant, string key)
    {
        var cacheKey = GetCacheKey(tenant, key);
        var data = _fusionCache.GetOrSet<byte[]>(cacheKey, (ctx, token) =>
        {
            var data = _service.GetTenantSettings(tenant, key);

            return ctx.Modified(data);
        }, _settingsExpiration, [CacheExtention.GetTenantSettingsTag(tenant, key)]);

        return data == null ? null : data.Length == 0 ? null : data;
    }

    public async Task SetTenantSettingsAsync(int tenant, string key, byte[] data)
    {
        await _service.SetTenantSettingsAsync(tenant, key, data);
        var tag = CacheExtention.GetTenantSettingsTag(tenant, key);
        await _fusionCache.RemoveByTagAsync(tag);
    }

    private string GetCacheKey(int tenant, string key)
    {
        return $"settings/{tenant}/{key.ToLowerInvariant()}";
    }
}