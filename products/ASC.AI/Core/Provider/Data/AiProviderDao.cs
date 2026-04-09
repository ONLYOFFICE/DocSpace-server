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

[Scope]
public class AiProviderDao(
    IDbContextFactory<AiDbContext> dbContextFactory,
    InstanceCrypto crypto,
    AiGateway gateway,
    AiConfiguration aiConfiguration) : IAiProviderDao
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
                ModifiedOn = now
            };

            await context.Providers.AddAsync(dbProvider);
            await context.SaveChangesAsync();

            if (isFirstProvider)
            {
                var defaultProviderEntity = new DbDefaultAiProvider
                {
                    TenantId = tenantId,
                    ProviderId = dbProvider.Id,
                    DefaultModel = defaultModel
                };
                await context.DefaultProviders.AddAsync(defaultProviderEntity);
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

    public async Task<AiProvider?> GetProviderAsync(int tenantId, int id, bool forceSystemProvider = false)
    {
        if (gateway.Configured && id == AiGateway.ProviderId)
        {
            return await CreateGatewayProviderAsync(includeCredentials: true, force: forceSystemProvider);
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

        if (gateway.Configured && offset == 0)
        {
            var gatewayProvider = await CreateGatewayProviderAsync();
            gatewayProvider.IsDefault = defaultProviderId == gatewayProvider.Id;
            yield return gatewayProvider;
            limit--;
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
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var count = await dbContext.GetProvidersTotalCountAsync(tenantId);

        if (gateway.Configured)
        {
            count++;
        }

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
            var transaction = await context.Database.BeginTransactionAsync();

            await context.DeleteDefaultProvidersByProviderIdsAsync(tenantId, ids);
            await context.DeleteProvidersAsync(tenantId, ids);
            await context.UpdateRoomSettingsAsync(tenantId, ids);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task<DefaultAiProvider> SetDefaultProviderAsync(int tenantId, AiProvider provider, string defaultModel)
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

        return new DefaultAiProvider
        {
            ProviderId = provider.Id,
            ProviderTitle = provider.Title,
            DefaultModel = defaultModel
        };
    }

    public async Task<DefaultAiProvider?> GetDefaultProviderAsync(int tenantId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var result = await dbContext.GetDefaultProviderAsync(tenantId);
        if (result == null)
        {
            return null;
        }

        if (result.ProviderId == AiGateway.ProviderId)
        {
            result.ProviderTitle = AiGateway.ProviderTitle;
            result.ProviderType = ProviderType.PortalAi;
        }

        if (result.ProviderType.HasValue)
        {
            result.DefaultModel = aiConfiguration.ResolveModelId(result.ProviderType.Value, result.DefaultModel);
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

    public async Task SaveModelSettingsAsync(int tenantId, int providerId, AiModelSettings settings)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var entity = new DbAiModelSettings
            {
                TenantId = tenantId,
                ProviderId = providerId,
                ModelId = settings.ModelId,
                Alias = settings.Alias,
                IsEnabled = settings.IsEnabled,
                Capabilities = settings.Capabilities
            };

            await context.ModelSettings.AddOrUpdateAsync(entity);
            await context.SaveChangesAsync();
        });
    }

    public async Task DeleteModelSettingsAsync(int tenantId, int providerId, string modelId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.DeleteModelSettingsAsync(tenantId, providerId, modelId);
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

        var existing = await context.GetModelSettingsByIdsAsync(tenantId, providerId, modelIds)
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
                await context.ModelSettings.AddAsync(new DbAiModelSettings
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

    private async Task<AiProvider> CreateGatewayProviderAsync(bool includeCredentials = false, bool force = false)
    {
        return new AiProvider
        {
            Id = AiGateway.ProviderId,
            Title = AiGateway.ProviderTitle,
            Url = includeCredentials ? gateway.Url : string.Empty,
            Key = includeCredentials ? await gateway.GetKeyAsync(force) : string.Empty,
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
