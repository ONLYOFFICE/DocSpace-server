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
public class ProfileStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory, InstanceCrypto crypto)
{
    public static string GetLockKey(int tenantId, Guid profileId) => $"ai_integration_profile_{tenantId}_{profileId}";


    public async Task<Profile> CreateAsync(int tenantId, ProfileData profile)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var encryptedKey = await EncryptKeyAsync(profile.Key);
        var entity = ToDbEntity(Guid.CreateVersion7(), tenantId, profile, encryptedKey, DateTime.UtcNow);

        context.Profiles.Add(entity);
        await context.SaveChangesAsync();

        return ToDomainEntity(entity, profile.Key);
    }

    public async Task<IReadOnlyList<Profile>> CreateManyAsync(int tenantId, IReadOnlyList<ProfileData> profiles)
    {
        if (profiles.Count == 0)
        {
            return [];
        }

        await using var context = await dbContextFactory.CreateDbContextAsync();

        var now = DateTime.UtcNow;
        var entities = new List<DbProfile>(profiles.Count);
        foreach (var p in profiles)
        {
            entities.Add(ToDbEntity(Guid.CreateVersion7(), tenantId, p, await EncryptKeyAsync(p.Key), now));
        }

        context.Profiles.AddRange(entities);
        await context.SaveChangesAsync();

        var result = new List<Profile>(entities.Count);
        result.AddRange(entities.Select((t, i) => ToDomainEntity(t, profiles[i].Key)));

        return result;
    }

    public async Task<Profile?> ReadByIdAsync(int tenantId, Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = await context.GetProfileAsync(tenantId, id);
        if (entity == null)
        {
            return null;
        }

        var key = await DecryptKeyAsync(entity.Key);
        return ToDomainEntity(entity, key);
    }

    public async Task<List<Profile>> ReadAllAsync(int tenantId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var result = new List<Profile>();
        await foreach (var entity in context.GetAllProfilesAsync(tenantId))
        {
            var key = await DecryptKeyAsync(entity.Key);
            result.Add(ToDomainEntity(entity, key));
        }

        return result;
    }

    public async Task<Profile> UpdateAsync(int tenantId, Profile profile)
    {
        var encryptedKey = await EncryptKeyAsync(profile.Key);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var entity = await context.GetProfileForUpdateAsync(tenantId, profile.Id);
            if (entity == null)
            {
                return;
            }

            entity.Name = profile.Name;
            entity.ProviderType = profile.ProviderType;
            entity.BaseUrl = profile.BaseUrl;
            entity.Key = encryptedKey;
            entity.ModelId = profile.ModelId;
            entity.Reasoning = profile.Reasoning;
            entity.Capabilities = profile.Capabilities;
            entity.UseResponsesApi = profile.UseResponsesApi;
            entity.CanUseTool = profile.CanUseTool;

            await context.SaveChangesAsync();
        });

        return profile;
    }

    public async Task DeleteAsync(int tenantId, Guid id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            await context.ClearThreadsProfileAsync(tenantId, id);
            await context.DeleteProfileAsync(tenantId, id);

            await transaction.CommitAsync();
        });
    }

    private async Task<string?> EncryptKeyAsync(string? key)
    {
        return string.IsNullOrEmpty(key) ? key : await crypto.EncryptAsync(key);
    }

    private async Task<string?> DecryptKeyAsync(string? encryptedKey)
    {
        if (string.IsNullOrEmpty(encryptedKey))
        {
            return encryptedKey;
        }

        try
        {
            return await crypto.DecryptAsync(encryptedKey);
        }
        catch (CryptographicException)
        {
            return string.Empty;
        }
    }

    private static DbProfile ToDbEntity(Guid id, int tenantId, ProfileData profile, string? encryptedKey, DateTime createdAt)
    {
        return new DbProfile
        {
            Id = id,
            TenantId = tenantId,
            Name = profile.Name,
            ProviderType = profile.ProviderType,
            BaseUrl = profile.BaseUrl,
            Key = encryptedKey,
            ModelId = profile.ModelId,
            Reasoning = profile.Reasoning,
            Capabilities = profile.Capabilities,
            UseResponsesApi = profile.UseResponsesApi,
            CanUseTool = profile.CanUseTool,
            CreatedAt = createdAt
        };
    }

    private static Profile ToDomainEntity(DbProfile entity, string? plainKey)
    {
        return new Profile
        {
            Id = entity.Id,
            Name = entity.Name,
            ProviderType = entity.ProviderType,
            BaseUrl = entity.BaseUrl,
            Key = plainKey,
            ModelId = entity.ModelId,
            Reasoning = entity.Reasoning,
            Capabilities = entity.Capabilities,
            UseResponsesApi = entity.UseResponsesApi,
            CanUseTool = entity.CanUseTool,
            CreatedAt = entity.CreatedAt
        };
    }
}
