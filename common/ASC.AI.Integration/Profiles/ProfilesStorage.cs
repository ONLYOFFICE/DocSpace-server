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

namespace ASC.AI.Integration.Profiles;

[Scope]
public class ProfilesStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory, InstanceCrypto crypto)
{
    public async Task<Profile> CreateAsync(int tenantId, ProfileData profile)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var encryptedKey = await EncryptKeyAsync(profile.Key);
        var entity = ToDbEntity(tenantId, profile, encryptedKey, DateTime.UtcNow);

        await context.Profiles.AddAsync(entity);
        await context.SaveChangesAsync();

        return ToDomainEntity(entity, profile.Key);
    }

    public async Task<IReadOnlyList<Profile>> CreateManyAsync(int tenantId, IReadOnlyList<ProfileData> profiles)
    {
        if (profiles.Count == 0)
        {
            return [];
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        var now = DateTime.UtcNow;
        var entities = new List<DbProfile>(profiles.Count);
        foreach (var p in profiles)
        {
            entities.Add(ToDbEntity(tenantId, p, await EncryptKeyAsync(p.Key), now));
        }

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            await context.Profiles.AddRangeAsync(entities);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        });

        var result = new List<Profile>(entities.Count);
        result.AddRange(entities.Select((t, i) => ToDomainEntity(t, profiles[i].Key)));

        return result;
    }

    public async Task<Profile?> ReadByIdAsync(int tenantId, int id)
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
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var encryptedKey = await EncryptKeyAsync(profile.Key);

        await context.UpdateProfileAsync(
            tenantId,
            profile.Id,
            profile.Name,
            profile.ProviderType,
            profile.BaseUrl,
            encryptedKey,
            profile.ModelId,
            profile.Reasoning,
            profile.Capabilities);

        return profile;
    }

    public async Task DeleteAsync(int tenantId, int id)
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

    private static DbProfile ToDbEntity(int tenantId, ProfileData profile, string? encryptedKey, DateTime createdAt)
    {
        return new DbProfile
        {
            TenantId = tenantId,
            Name = profile.Name,
            ProviderType = profile.ProviderType,
            BaseUrl = profile.BaseUrl,
            Key = encryptedKey,
            ModelId = profile.ModelId,
            Reasoning = profile.Reasoning,
            Capabilities = profile.Capabilities,
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
            CreatedAt = entity.CreatedAt
        };
    }
}
