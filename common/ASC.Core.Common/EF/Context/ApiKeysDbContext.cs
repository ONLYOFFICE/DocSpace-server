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

namespace ASC.Core.Common.EF;

public class ApiKeysDbContext(DbContextOptions<ApiKeysDbContext> options) : BaseDbContext(options)
{
    public DbSet<ApiKey> DbApiKey { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelBuilderWrapper.From(modelBuilder, Database)
                           .AddDbApiKeys()
                           .AddDbTenant();
    }

    [PreCompileQuery]
    public IAsyncEnumerable<ApiKey> ApiKeysForUserAsync(int tenantId, Guid userId)
    {
        return Queries.ApiKeysForUserAsync(this, tenantId, userId);
    }


    [PreCompileQuery]
    public Task<ApiKey> ValidateApiKeyAsync(int tenantId, string hashedKey)
    {
        return Queries.ValidateApiKeyAsync(this, tenantId, hashedKey);
    }

    [PreCompileQuery]
    public Task<ApiKey> GetApiKeyAsync(int tenantId, Guid keyId)
    {
        return Queries.GetApiKeyAsync(this, tenantId, keyId);
    }

    [PreCompileQuery]
    public Task<ApiKey> GetApiKeyAsync(int tenantId, string hashedKey)
    {
        return Queries.GetApiKeyByHashedKeyAsync(this, tenantId, hashedKey);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<ApiKey> GetAllApiKeyAsync(int tenantId)
    {
        return Queries.AllApiKeysAsync(this, tenantId);
    }


    [PreCompileQuery]
    public Task<int> DeleteApiKeyAsync(int tenantId, Guid keyId)
    {
        return Queries.DeleteApiKeyAsync(this, tenantId, keyId);
    }
}

static file class Queries
{
    public static readonly Func<ApiKeysDbContext, int, Guid, IAsyncEnumerable<ApiKey>> ApiKeysForUserAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (ApiKeysDbContext ctx, int tenantId, Guid userId) =>
                ctx.DbApiKey.Where(r => r.TenantId == tenantId && r.CreateBy == userId)
                            .OrderByDescending(k => k.CreateOn)
                            .AsQueryable()
                            );

    public static readonly Func<ApiKeysDbContext, int, IAsyncEnumerable<ApiKey>> AllApiKeysAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (ApiKeysDbContext ctx, int tenantId) =>
                ctx.DbApiKey.Where(r => r.TenantId == tenantId)
                    .OrderByDescending(k => k.CreateOn)
                    .AsQueryable());

    public static readonly Func<ApiKeysDbContext, int, string, Task<ApiKey>> ValidateApiKeyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (ApiKeysDbContext ctx, int tenantId, string hashedKey) =>
                ctx.DbApiKey.FirstOrDefault(r => r.TenantId == tenantId && r.HashedKey == hashedKey && r.IsActive &&
                                                 (r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow)));

    public static readonly Func<ApiKeysDbContext, int, string, Task<ApiKey>> GetApiKeyByHashedKeyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (ApiKeysDbContext ctx, int tenantId, string hashedKey) =>
                ctx.DbApiKey.FirstOrDefault(r => r.TenantId == tenantId && r.HashedKey == hashedKey));

    public static readonly Func<ApiKeysDbContext, int, Guid, Task<ApiKey>> GetApiKeyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (ApiKeysDbContext ctx, int tenantId, Guid keyId) =>
                ctx.DbApiKey.FirstOrDefault(r => r.TenantId == tenantId && r.Id == keyId));

    public static readonly Func<ApiKeysDbContext, int, Guid, Task<int>> DeleteApiKeyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (ApiKeysDbContext ctx, int tenantId, Guid keyId) =>
                ctx.DbApiKey.Where(r => r.TenantId == tenantId && r.Id == keyId)
                            .ExecuteDelete());
}