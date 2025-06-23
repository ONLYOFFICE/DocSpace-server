﻿// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.AI.Core.Common.Database;

[Scope]
public class AiProviderDao(
    IDbContextFactory<AiDbContext> dbContextFactory, 
    InstanceCrypto crypto,
    ProviderSettings settings)
{
    public async Task<AiProvider> AddProviderAsync(int tenantId, AiProvider provider)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        DbAiProvider dbProvider = null!;

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            var now = DateTime.UtcNow;

            dbProvider = new DbAiProvider
            {
                TenantId = tenantId,
                Type = provider.Type,
                Url = provider.Url?.ToString(),
                Key = await crypto.EncryptAsync(provider.Key),
                Title = provider.Title,
                CreatedOn = now,
                ModifiedOn = now
            };
            
            await context.Providers.AddAsync(dbProvider);
            await context.SaveChangesAsync();
        });
        
        provider.Id = dbProvider.Id;
        return provider;
    }

    public async Task<AiProvider?> GetProviderAsync(int tenantId, int id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var provider = await dbContext.GetProviderAsync(tenantId, id);
        if (provider == null)
        {
            return null;
        }

        return await ToProviderAsync(provider);
    }
    
    public async IAsyncEnumerable<AiProvider> GetProvidersAsync(int tenantId, int offset, int limit)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync();
        await foreach (var provider in dbContext.GetProvidersAsync(tenantId, offset, limit))
        {
            yield return await ToProviderAsync(provider);
        }
    }

    public async Task<int> GetProvidersTotalCountAsync(int tenantId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetProvidersTotalCountAsync(tenantId);
    }
    
    public async Task<AiProvider> UpdateProviderAsync(AiProvider provider)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            await context.UpdateProviderAsync(provider.Id, provider.Title, provider.Url?.ToString(), provider.Key, provider.ModifiedOn);
            
            await context.SaveChangesAsync();
        });
        
        return provider;
    }
    
    public async Task DeleteProviders(int tenantId, IEnumerable<int> ids)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            await context.DeleteProvidersAsync(tenantId, ids);
            
            await context.SaveChangesAsync();
        });
    }

    private async Task<AiProvider> ToProviderAsync(DbAiProvider provider)
    {
        var providerSettings = settings.Get(provider.Type);
        var url = !string.IsNullOrEmpty(provider.Url) ? provider.Url : providerSettings?.Url;

        return new AiProvider
        {
            Id = provider.Id,
            Type = provider.Type,
            Url = !string.IsNullOrEmpty(url) ? new Uri(url) : null,
            Key = await crypto.DecryptAsync(provider.Key),
            Title = provider.Title,
            CreatedOn = provider.CreatedOn,
            ModifiedOn = provider.ModifiedOn
        };
    }
}