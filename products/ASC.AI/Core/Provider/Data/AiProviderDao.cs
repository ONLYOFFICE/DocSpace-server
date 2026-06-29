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

[Scope]
public class AiProviderDao(
    IDbContextFactory<AiDbContext> dbContextFactory,
    InstanceCrypto crypto,
    AiGateway gateway,
    AiConfiguration aiConfiguration)
{
    public async Task<AiProvider> AddProviderAsync(
        int tenantId,
        string title,
        string url,
        string key,
        ProviderType type,
        string defaultModel,
        List<AiModelSettings>? modelSettings = null)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        DbAiProvider dbProvider = null!;
        var now = DateTime.UtcNow;
        var isFirstProvider = false;

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            isFirstProvider = !gateway.Configured && !await context.HasProvidersAsync(tenantId);

            dbProvider = new DbAiProvider
            {
                TenantId = tenantId,
                Type = type,
                Url = url,
                Key = await crypto.EncryptAsync(key),
                Title = title,
                CreatedOn = now,
                ModifiedOn = now,
                HasModelSettings = true
            };

            context.Providers.Add(dbProvider);
            await context.SaveChangesAsync();

            if (isFirstProvider)
            {
                var defaultProviderEntity = new DbDefaultAiProvider
                {
                    TenantId = tenantId,
                    ProviderId = dbProvider.Id,
                    DefaultModel = defaultModel
                };

                await context.DefaultProviders.AddOrUpdateAsync(defaultProviderEntity);
                await context.SaveChangesAsync();
            }

            await SaveModelSettingsInternalAsync(context, tenantId, dbProvider.Id, modelSettings);

            await transaction.CommitAsync();
        });

        return new AiProvider
        {
            Id = dbProvider.Id,
            Title = title,
            Url = url,
            Key = key,
            Type = type,
            CreatedOn = now,
            ModifiedOn = now,
            IsDefault = isFirstProvider
        };
    }

    public async Task<AiProvider?> GetProviderAsync(int tenantId, int id, bool forceSystemProvider = false, bool allowLegacyProvider = false)
    {
        if (gateway.Configured)
        {
            if (id == AiGateway.ProviderId)
            {
                return await CreateGatewayProviderAsync(includeCredentials: true, allowEmptyKey: forceSystemProvider);
            }

            if (!allowLegacyProvider)
            {
                return null;
            }
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var provider = await dbContext.GetProviderAsync(tenantId, id);
        if (provider == null)
        {
            return null;
        }

        var (success, decryptedKey) = await TryDecryptKeyAsync(provider.Key);
        provider.Key = decryptedKey;

        var res = provider.Map();
        res.NeedReset = !success;

        return res;
    }

    public async IAsyncEnumerable<AiProvider> GetProvidersAsync(int tenantId, int offset, int limit)
    {
        var defaultProviderId = (await GetDefaultProviderAsync(tenantId))?.ProviderId;

        if (gateway.Configured)
        {
            if (offset > 0)
            {
                yield break;
            }

            var gatewayProvider = await CreateGatewayProviderAsync();
            gatewayProvider.IsDefault = true;
            yield return gatewayProvider;
            yield break;
        }

        if (limit <= 0)
        {
            yield break;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await foreach (var provider in dbContext.GetProvidersAsync(tenantId, offset, limit))
        {
            var (success, decryptedKey) = await TryDecryptKeyAsync(provider.Key);
            provider.Key = decryptedKey;

            var res = provider.Map();
            res.NeedReset = !success;
            res.IsDefault = defaultProviderId == res.Id;

            yield return res;
        }
    }

    public async Task<bool> CanDecryptSomeKeyAsync(int tenantId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var count = 0;

        await foreach (var key in dbContext.GetProviderKeysAsync(tenantId))
        {
            try
            {
                count++;
                _ = await crypto.DecryptAsync(key);
                return true;
            }
            catch (CryptographicException) { }
        }

        return count == 0;
    }

    public async Task<int> GetProvidersTotalCountAsync(int tenantId)
    {
        if (gateway.Configured)
        {
            return 1;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var count = await dbContext.GetProvidersTotalCountAsync(tenantId);

        return count;
    }

    public async Task<bool> IsProviderNameExistsAsync(int tenantId, string title, int excludedProviderId = 0)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.IsProviderNameExistsAsync(tenantId, title, excludedProviderId);
    }

    public async Task<AiProvider> UpdateProviderAsync(int tenantId, AiProvider provider, List<AiModelSettings>? modelSettings = null)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        var key = await crypto.EncryptAsync(provider.Key);
        var now = DateTime.UtcNow;

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            await context.UpdateProviderAsync(tenantId, provider.Id, provider.Title, provider.Url, key, now);
            await context.SaveChangesAsync();

            await SaveModelSettingsInternalAsync(context, tenantId, provider.Id, modelSettings);

            await transaction.CommitAsync();
        });

        provider.ModifiedOn = now;
        provider.NeedReset = false;
        return provider;
    }

    public async Task DeleteProviders(int tenantId, HashSet<int> ids)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            await context.DeleteDefaultProvidersByProviderIdsAsync(tenantId, ids);
            await context.DeleteProvidersAsync(tenantId, ids);
            await context.UpdateRoomSettingsAsync(tenantId, ids);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task<DefaultAiProviderSettings> SetDefaultProviderAsync(int tenantId, AiProvider provider, string defaultModel)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var entity = new DbDefaultAiProvider
            {
                TenantId = tenantId,
                ProviderId = provider.Id,
                DefaultModel = defaultModel
            };

            await context.DefaultProviders.AddOrUpdateAsync(entity);
            await context.SaveChangesAsync();
        });

        return new DefaultAiProviderSettings
        {
            ProviderId = provider.Id,
            ProviderTitle = provider.Title,
            ProviderType = provider.Type,
            DefaultModel = defaultModel,
            HasModelSettings = provider.HasModelSettings
        };
    }

    public async Task<DefaultAiProviderSettings?> GetDefaultProviderAsync(int tenantId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var queryResult = await dbContext.GetDefaultProviderAsync(tenantId);
        if (queryResult == null)
        {
            return null;
        }

        switch (gateway.Configured)
        {
            case true when queryResult.ProviderId != AiGateway.ProviderId:
            case false when queryResult.ProviderId == AiGateway.ProviderId:
                return null;
        }

        var providerType = queryResult.ProviderId == AiGateway.ProviderId
            ? ProviderType.PortalAi
            : queryResult.ProviderType ?? default;

        var result = new DefaultAiProviderSettings
        {
            ProviderId = queryResult.ProviderId,
            DefaultModel = aiConfiguration.ResolveModelId(providerType, queryResult.DefaultModel),
            ProviderTitle = queryResult.ProviderId == AiGateway.ProviderId
                ? AiGateway.ProviderTitle
                : queryResult.ProviderTitle,
            ProviderType = providerType,
            HasModelSettings = queryResult.HasModelSettings
        };

        if (queryResult.DbModelSettings != null)
        {
            result.DbModelSettings = queryResult.DbModelSettings.Map();
        }

        return result;
    }

    public async Task<int?> GetFirstProviderIdAsync(int tenantId)
    {
        if (gateway.Configured && await gateway.IsEnabledAsync())
        {
            return AiGateway.ProviderId;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.GetFirstProviderIdAsync(tenantId)
            ?? (gateway.Configured ? AiGateway.ProviderId : null);
    }

    public async Task<Dictionary<string, AiModelSettings>> GetModelSettingsAsync(int tenantId, int providerId, ProviderType type)
    {
        if (type == ProviderType.PortalAi)
        {
            return [];
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.GetModelSettingsAsync(tenantId, providerId)
            .ToDictionaryAsync(x => x.ModelId, x => x.Map());
    }

    public async Task<AiModelSettings?> GetModelSettingAsync(int tenantId, int providerId, string modelId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var entity = await dbContext.GetModelSettingAsync(tenantId, providerId, modelId);

        return entity?.Map();
    }

    private static async Task SaveModelSettingsInternalAsync(
        AiDbContext context,
        int tenantId,
        int providerId,
        List<AiModelSettings>? modelSettings)
    {
        if (modelSettings is not { Count: > 0 })
        {
            return;
        }

        var modelIds = modelSettings.Select(s => s.ModelId).ToList();

        var existing = await context.GetModelSettingsForUpdateAsync(tenantId, providerId, modelIds)
            .ToDictionaryAsync(x => x.ModelId);

        foreach (var s in modelSettings)
        {
            if (existing.TryGetValue(s.ModelId, out var entity))
            {
                entity.Alias = s.Alias;
                entity.IsEnabled = s.IsEnabled;
                entity.Capabilities = s.Capabilities;
            }
            else
            {
                context.ModelSettings.Add(new DbAiModelSettings
                {
                    TenantId = tenantId,
                    ProviderId = providerId,
                    ModelId = s.ModelId,
                    Alias = s.Alias,
                    IsEnabled = s.IsEnabled,
                    Capabilities = s.Capabilities
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task<AiProvider> CreateGatewayProviderAsync(bool includeCredentials = false, bool allowEmptyKey = false)
    {
        return new AiProvider
        {
            Id = AiGateway.ProviderId,
            Title = AiGateway.ProviderTitle,
            Url = includeCredentials ? gateway.Url : string.Empty,
            Key = includeCredentials ? await gateway.GetKeyAsync(allowEmpty: allowEmptyKey) : string.Empty,
            Type = ProviderType.PortalAi
        };
    }

    private async Task<(bool Success, string Key)> TryDecryptKeyAsync(string encryptedKey)
    {
        try
        {
            var decryptedKey = await crypto.DecryptAsync(encryptedKey);
            return (true, decryptedKey);
        }
        catch (CryptographicException)
        {
            return (false, string.Empty);
        }
    }
}
