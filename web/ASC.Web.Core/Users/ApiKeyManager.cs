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

using ASC.Core.Common.EF;

namespace ASC.Web.Core.Users;

[Scope]
public class ApiKeyManager(
    IDbContextFactory<ApiKeysDbContext> dbContextFactory,
    AuthContext authContext,
    PasswordHasher passwordHasher,
    TenantManager tenantManager)
{
    private readonly string _keyPrefix = "sk-";

    private string GenerateApiKey()
    {
        var keyBytes = new byte[32];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        return _keyPrefix + Convert.ToHexString(keyBytes).ToLower();
    }

    private string HashApiKey(string apiKey)
    {
        // Use same hash alg as for client password 
        return passwordHasher.GetClientPassword(apiKey);
    }

    public async Task<(string apiKey, ApiKey keyData)> CreateApiKeyAsync(string name, List<string> permissions = null,
        TimeSpan? expiresIn = null)
    {
        var currentUserId = authContext.CurrentAccount.ID;
        var newKey = GenerateApiKey();
        var hashedKey = HashApiKey(newKey);

        if (permissions is { Count: > 0 } && permissions.Exists(x => x == "*"))
        {
            permissions = null;
        }

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = name,
            KeyPostfix = newKey.Substring(newKey.Length - 4, 4),
            HashedKey = hashedKey,
            CreateBy = currentUserId,
            CreateOn = DateTime.UtcNow,
            Permissions = permissions ?? [],
            ExpiresAt = expiresIn.HasValue ? DateTime.UtcNow.Add(expiresIn.Value) : null,
            TenantId = tenantManager.GetCurrentTenantId(),
            IsActive = true
        };

        await using var context = await dbContextFactory.CreateDbContextAsync();

        context.DbApiKey.Add(apiKey);

        await context.SaveChangesAsync();

        return (newKey, apiKey);
    }

    public async Task<ApiKey> GetApiKeyAsync(Guid keyId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.GetApiKeyAsync(tenantId, keyId);
    }

    public async Task<ApiKey> GetApiKeyAsync(string apiKey)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var hashedKey = HashApiKey(apiKey);

        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.GetApiKeyAsync(tenantId, hashedKey);
    }

    public async Task<ApiKey> ValidateApiKeyAsync(string apiKey)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        if (string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        var hashedKey = HashApiKey(apiKey);

        await using var context = await dbContextFactory.CreateDbContextAsync();

        var keyData = await context.ValidateApiKeyAsync(tenantId, hashedKey);

        if (keyData != null)
        {
            keyData.LastUsed = DateTime.UtcNow;

            await context.AddOrUpdateAsync(q => q.DbApiKey, keyData);
            await context.SaveChangesAsync();
        }

        return keyData;
    }

    public async Task<bool> UpdateApiKeyAsync(Guid keyId,
                                              List<string> permissions,
                                              string name,
                                              bool? isActive)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var context = await dbContextFactory.CreateDbContextAsync();

        var apiKey = await context.GetApiKeyAsync(tenantId, keyId);

        if (apiKey == null)
        {
            return false;
        }

        if (isActive.HasValue)
        {
            apiKey.IsActive = isActive.Value;
        }

        if (permissions is { Count: > 0 })
        {
            if (permissions.Count == 1 && permissions[0] == "*")
            {
                permissions = null;
            }

            apiKey.Permissions = permissions;
        }

        if (!string.IsNullOrEmpty(name))
        {
            apiKey.Name = name;
        }

        await context.AddOrUpdateAsync(q => q.DbApiKey, apiKey);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteApiKeyAsync(Guid keyId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var context = await dbContextFactory.CreateDbContextAsync();

        var result = await context.DeleteApiKeyAsync(tenantId, keyId);

        return result >= 0;
    }

    public async IAsyncEnumerable<ApiKey> GetAllApiKeysAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var context = await dbContextFactory.CreateDbContextAsync();

        var apiKeys = context.GetAllApiKeyAsync(tenantId);

        await foreach (var apiKey in apiKeys)
        {
            yield return apiKey;
        }
    }

    public async IAsyncEnumerable<ApiKey> GetApiKeysAsync(Guid userId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var context = await dbContextFactory.CreateDbContextAsync();

        var apiKeys = context.ApiKeysForUserAsync(tenantId, userId);

        await foreach (var apiKey in apiKeys)
        {
            yield return apiKey;
        }
    }
}