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

namespace ASC.Core.Caching;

[Scope(typeof(IQuotaService))]
class CachedQuotaService() : IQuotaService
{
    private readonly DbQuotaService _service;
    private readonly IFusionCache _cache;
    private readonly GeolocationHelper _geolocationHelper;
    private readonly bool _quotaCacheEnabled;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    private const string KeyQuota = "quota";
    private const string KeyQuotaRows = "quotarows";

    public CachedQuotaService(DbQuotaService service, IFusionCacheProvider cacheProvider, GeolocationHelper geolocationHelper, IConfiguration Configuration) : this()
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _geolocationHelper = geolocationHelper;
        _cache = cacheProvider.GetMemoryCache();

        if (Configuration["core:enable-quota-cache"] == null)
        {
            _quotaCacheEnabled = true;
        }
        else
        {
            _quotaCacheEnabled = !bool.TryParse(Configuration["core:enable-quota-cache"], out var enabled) || enabled;
        }
    }

    public async Task<IEnumerable<TenantQuota>> GetTenantQuotasAsync()
    {
        var cacheKey = KeyQuota + (await _geolocationHelper.GetIPGeolocationFromHttpContextAsync()).Key;
        var quotas = await _cache.GetOrDefaultAsync<IEnumerable<TenantQuota>>(cacheKey);
        if (quotas == null)
        {
            quotas = await _service.GetTenantQuotasAsync();
            if (_quotaCacheEnabled)
            {
                await _cache.SetAsync(cacheKey, quotas, _cacheExpiration, [CacheExtention.GetTenantQuotaTag((await _geolocationHelper.GetIPGeolocationFromHttpContextAsync()).Key)]);
            }
        }

        return quotas;
    }

    public async Task<TenantQuota> GetTenantQuotaAsync(int id)
    {
        return (await GetTenantQuotasAsync()).SingleOrDefault(q => q.TenantId == id);
    }

    public async Task<TenantQuota> SaveTenantQuotaAsync(TenantQuota quota)
    {
        var q = await _service.SaveTenantQuotaAsync(quota);

        var tag = CacheExtention.GetTenantQuotaTag((await _geolocationHelper.GetIPGeolocationFromHttpContextAsync()).Key);
        await _cache.RemoveByTagAsync(tag);

        return q;
    }

    public Task RemoveTenantQuotaAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task SetTenantQuotaRowAsync(TenantQuotaRow row, bool exchange)
    {
        await _service.SetTenantQuotaRowAsync(row, exchange);
        var tag = CacheExtention.GetTenantQuotaRowTag(row.TenantId, row.Path);
        await _cache.RemoveByTagAsync(tag);

        if (row.UserId != Guid.Empty)
        {
            tag = CacheExtention.GetTenantQuotaRowTag(row.TenantId, row.Path, row.UserId);
            await _cache.RemoveByTagAsync(tag);
        }
    }

    public async Task<IEnumerable<TenantQuotaRow>> FindTenantQuotaRowsAsync(int tenantId)
    {
        var key = GetKey(tenantId);

        var result = await _cache.GetOrSetAsync<IEnumerable<TenantQuotaRow>>(key, async (ctx, token) =>
        {
            var result = await _service.FindTenantQuotaRowsAsync(tenantId);
            ctx.Tags = result.Select(r => CacheExtention.GetTenantQuotaRowTag(tenantId, r.Path)).ToArray();
            return ctx.Modified(result);
        }, _cacheExpiration);

        return result;
    }

    public async Task<IEnumerable<TenantQuotaRow>> FindUserQuotaRowsAsync(int tenantId, Guid userId)
    {
        var key = GetKey(tenantId, userId);

        var result = await _cache.GetOrSetAsync<IEnumerable<TenantQuotaRow>>(key, async (ctx, token) =>
        {
            var result = await _service.FindUserQuotaRowsAsync(tenantId, userId);
            ctx.Tags = result.Select(r => CacheExtention.GetTenantQuotaRowTag(tenantId, r.Path, userId)).ToArray();
            return ctx.Modified(result);
        }, _cacheExpiration);

        return result;
    }

    private static string GetKey(int tenant)
    {
        return KeyQuotaRows + tenant;
    }

    private static string GetKey(int tenant, Guid userId)
    {
        return KeyQuotaRows + tenant + userId;
    }
}