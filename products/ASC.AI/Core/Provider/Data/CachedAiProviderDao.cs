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

namespace ASC.AI.Core.Provider.Data;

[Scope(typeof(IAiProviderDao))]
public class CachedAiProviderDao(
    AiProviderDao providerDao,
    IFusionCache cache,
    AiGateway gateway) : IAiProviderDao
{
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    public async Task<AiProvider> AddProviderAsync(
        int tenantId,
        string title,
        string url,
        string key,
        ProviderType type,
        string defaultModel)
    {
        var result = await providerDao.AddProviderAsync(tenantId, title, url, key, type, defaultModel);

        if (!result.IsDefault)
        {
            return result;
        }

        await InvalidateDefaultProviderCacheAsync(tenantId);
        await InvalidateFirstProviderCacheAsync(tenantId);

        return result;
    }

    public Task<AiProvider?> GetProviderAsync(int tenantId, int id)
    {
        return providerDao.GetProviderAsync(tenantId, id);
    }

    public IAsyncEnumerable<AiProvider> GetProvidersAsync(int tenantId, int offset, int limit)
    {
        return providerDao.GetProvidersAsync(tenantId, offset, limit);
    }

    public Task<bool> CanDecryptSomeKeyAsync(int tenantId)
    {
        return providerDao.CanDecryptSomeKeyAsync(tenantId);
    }

    public Task<int> GetProvidersTotalCountAsync(int tenantId)
    {
        return providerDao.GetProvidersTotalCountAsync(tenantId);
    }

    public async Task<AiProvider> UpdateProviderAsync(int tenantId, AiProvider provider)
    {
        provider = await providerDao.UpdateProviderAsync(tenantId, provider);

        await InvalidateDefaultProviderCacheAsync(tenantId);

        return provider;
    }

    public async Task DeleteProviders(int tenantId, HashSet<int> ids)
    {
        var defaultProviderId = (await GetDefaultProviderAsync(tenantId))?.ProviderId;

        await providerDao.DeleteProviders(tenantId, ids);

        if (defaultProviderId.HasValue && ids.Contains(defaultProviderId.Value))
        {
            await InvalidateDefaultProviderCacheAsync(tenantId);
        }

        await InvalidateFirstProviderCacheAsync(tenantId);
    }

    public async Task<DefaultAiProvider> SetDefaultProviderAsync(int tenantId, AiProvider provider, string defaultModel)
    {
        var result = await providerDao.SetDefaultProviderAsync(tenantId, provider, defaultModel);

        await InvalidateDefaultProviderCacheAsync(tenantId);

        return result;
    }

    public async Task<DefaultAiProvider?> GetDefaultProviderAsync(int tenantId)
    {
        var cacheKey = GetDefaultProviderCacheKey(tenantId);

        var result = await cache.GetOrSetAsync(cacheKey,
            async _ => await providerDao.GetDefaultProviderAsync(tenantId), _cacheExpiration);

        if (result is not { ProviderId: AiGateway.ProviderId } || await gateway.IsEnabledAsync())
        {
            return result;
        }

        await InvalidateDefaultProviderCacheAsync(tenantId);
        return null;
    }

    public async Task<int?> GetFirstProviderIdAsync(int tenantId)
    {
        var cacheKey = GetFirstProviderCacheKey(tenantId);

        return await cache.GetOrSetAsync(cacheKey,
            async _ => await providerDao.GetFirstProviderIdAsync(tenantId), _cacheExpiration);
    }

    private static string GetDefaultProviderCacheKey(int tenantId)
    {
        return $"ai:default_provider:{tenantId}";
    }

    private static string GetFirstProviderCacheKey(int tenantId)
    {
        return $"ai:first_provider:{tenantId}";
    }

    private async Task InvalidateFirstProviderCacheAsync(int tenantId)
    {
        await cache.RemoveAsync(GetFirstProviderCacheKey(tenantId));
    }

    private async Task InvalidateDefaultProviderCacheAsync(int tenantId)
    {
        await cache.RemoveAsync(GetDefaultProviderCacheKey(tenantId));
    }
}
