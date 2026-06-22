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
        string defaultModel,
        List<AiModelSettings>? modelSettings = null)
    {
        var result = await providerDao.AddProviderAsync(tenantId, title, url, key, type, defaultModel, modelSettings);

        if (!result.IsDefault)
        {
            return result;
        }

        await InvalidateDefaultProviderCacheAsync(tenantId);
        await InvalidateFirstProviderCacheAsync(tenantId);

        return result;
    }

    public Task<AiProvider?> GetProviderAsync(int tenantId, int id, bool forceSystemProvider = false, bool allowLegacyProvider = false)
    {
        return providerDao.GetProviderAsync(tenantId, id, forceSystemProvider, allowLegacyProvider);
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

    public Task<bool> IsProviderNameExistsAsync(int tenantId, string title, int excludedProviderId = 0)
    {
        return providerDao.IsProviderNameExistsAsync(tenantId, title, excludedProviderId);
    }

    public async Task<AiProvider> UpdateProviderAsync(int tenantId, AiProvider provider, List<AiModelSettings>? modelSettings = null)
    {
        provider = await providerDao.UpdateProviderAsync(tenantId, provider, modelSettings);

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

    public async Task<DefaultAiProviderSettings> SetDefaultProviderAsync(int tenantId, AiProvider provider, string defaultModel)
    {
        var result = await providerDao.SetDefaultProviderAsync(tenantId, provider, defaultModel);

        await InvalidateDefaultProviderCacheAsync(tenantId);

        return result;
    }

    public async Task<DefaultAiProviderSettings?> GetDefaultProviderAsync(int tenantId)
    {
        var cacheKey = GetDefaultProviderCacheKey(tenantId);

        var cached = await cache.TryGetAsync<DefaultAiProviderSettings>(cacheKey);
        DefaultAiProviderSettings? result;

        if (cached.HasValue)
        {
            result = cached.Value;
        }
        else
        {
            result = await providerDao.GetDefaultProviderAsync(tenantId);
            if (result != null)
            {
                await cache.SetAsync(cacheKey, result, _cacheExpiration);
            }
        }

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

        var cached = await cache.TryGetAsync<int>(cacheKey);
        if (cached.HasValue)
        {
            if (cached.Value != AiGateway.ProviderId || await gateway.IsEnabledAsync())
            {
                return cached.Value;
            }

            await InvalidateFirstProviderCacheAsync(tenantId);
        }

        var result = await providerDao.GetFirstProviderIdAsync(tenantId);
        if (result != null)
        {
            await cache.SetAsync(cacheKey, result, _cacheExpiration);
        }

        return result;
    }

    public Task<Dictionary<string, AiModelSettings>> GetModelSettingsAsync(int tenantId, int providerId, ProviderType type)
    {
        return providerDao.GetModelSettingsAsync(tenantId, providerId, type);
    }

    public Task<AiModelSettings?> GetModelSettingAsync(int tenantId, int providerId, string modelId)
    {
        return providerDao.GetModelSettingAsync(tenantId, providerId, modelId);
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
