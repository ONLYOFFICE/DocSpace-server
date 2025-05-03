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
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<ApiKey> ApiKeysForUserAsync(int tenantId, Guid userId)
    {
        return Queries.ApiKeysForUserAsync(this, tenantId, userId);
    }
    

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<ApiKey> ValidateApiKeyAsync(int tenantId, string hashedKey)
    {
        return Queries.ValidateApiKeyAsync(this, tenantId,hashedKey);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<ApiKey> GetApiKeyAsync(int tenantId, Guid keyId)
    {
        return Queries.GetApiKeyAsync(this, tenantId, keyId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<ApiKey> GetApiKeyAsync(int tenantId, string hashedKey)
    {
        return Queries.GetApiKeyByHashedKeyAsync(this, tenantId, hashedKey);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<ApiKey> GetAllApiKeyAsync(int tenantId)
    {
        return  Queries.AllApiKeysAsync(this, tenantId);
    }

    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> DeleteApiKeyAsync(int tenantId, Guid keyId)
    {
        return Queries.DeleteApiKeyAsync(this, tenantId,keyId);
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