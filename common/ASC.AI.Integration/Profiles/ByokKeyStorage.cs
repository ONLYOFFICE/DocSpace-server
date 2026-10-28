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

namespace ASC.AI.Integration.Profiles;

[Scope]
public class ByokKeyStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory, InstanceCrypto crypto, IFusionCache fusionCache)
{
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

    private static string GetCacheKey(int tenantId) => $"ai_integration_byok_keys_{tenantId}";

    public async Task<IReadOnlyList<ByokKey>> ReadAllAsync(int tenantId)
    {
        var entries = await ReadEntriesAsync(tenantId);

        return [.. entries.Select(e => e.Map())];
    }

    public async Task<ByokCredentials?> ReadCredentialsAsync(int tenantId, Guid id)
    {
        var entry = (await ReadEntriesAsync(tenantId)).FirstOrDefault(e => e.Id == id);
        if (entry is null)
        {
            return null;
        }

        var key = await DecryptKeyAsync(entry.Key);

        return string.IsNullOrEmpty(key) ? null : new ByokCredentials(entry.Provider, key);
    }

    public async Task SetAsync(int tenantId, Guid id, string provider, string key)
    {
        var encryptedKey = await crypto.EncryptAsync(key);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var entity = await context.GetByokKeyForUpdateAsync(tenantId, id);
            if (entity is null)
            {
                context.ByokKeys.Add(new DbByokKey
                {
                    TenantId = tenantId,
                    Id = id,
                    Provider = provider,
                    Key = encryptedKey,
                    ModifiedOn = DateTime.UtcNow
                });
            }
            else
            {
                entity.Provider = provider;
                entity.Key = encryptedKey;
                entity.ModifiedOn = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        });

        await fusionCache.RemoveAsync(GetCacheKey(tenantId));
    }

    public async Task<bool> DeleteAsync(int tenantId, Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var deleted = await context.DeleteByokKeyAsync(tenantId, id);

        await fusionCache.RemoveAsync(GetCacheKey(tenantId));

        return deleted > 0;
    }

    private async Task<IReadOnlyList<ByokKeyEntry>> ReadEntriesAsync(int tenantId)
    {
        return await fusionCache.GetOrSetAsync<IReadOnlyList<ByokKeyEntry>>(
            GetCacheKey(tenantId),
            async (_, ctx) =>
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(ctx);

                return await context.GetByokKeysAsync(tenantId)
                    .Select(e => e.Map())
                    .ToListAsync(ctx);
            },
            opt => opt.SetDuration(_cacheDuration).SetFailSafe(true));
    }

    private async Task<string> DecryptKeyAsync(string encryptedKey)
    {
        try
        {
            return await crypto.DecryptAsync(encryptedKey);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return string.Empty;
        }
    }

    internal sealed class ByokKeyEntry
    {
        public Guid Id { get; init; }
        public required string Provider { get; init; }
        public required string Key { get; init; }
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class ByokKeyMapper
{
    public static partial ByokKeyStorage.ByokKeyEntry Map(this DbByokKey source);
    public static partial ByokKey Map(this ByokKeyStorage.ByokKeyEntry source);
}
