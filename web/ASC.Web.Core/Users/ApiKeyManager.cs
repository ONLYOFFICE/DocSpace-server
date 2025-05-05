// (c) Copyright Ascensio System SIA 2009-2024
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
        
        if (permissions is { Count: > 0 } && permissions.Exists(x=> x == "*"))
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

        var apiKeys =  context.ApiKeysForUserAsync(tenantId, userId);

        await foreach (var apiKey in apiKeys)
        {
            yield return apiKey;
        }
    }
}