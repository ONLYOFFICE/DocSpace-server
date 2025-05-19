// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Core.Common.EF.Context;

public partial class TenantDbContext
{
    [PreCompileQuery([null])]
    public Task<DbTenant> TenantByDomainAsync(string domain)
    {
        return Queries.TenantByDomainAsync(this, domain);
    }
    
    [PreCompileQuery([])]
    public Task<int> VersionIdAsync()
    {
        return Queries.VersionIdAsync(this);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<DbTenant> TenantAsync(int tenantId)
    {
        return Queries.TenantAsync(this, tenantId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<string> GetAliasAsync(int tenantId)
    {
        return Queries.GetAliasAsync(this, tenantId);
    }
    
    [PreCompileQuery([null])]
    public Task<int> TenantsCountAsync(string startAlias)
    {
        return Queries.TenantsCountAsync(this, startAlias);
    }
    
    [PreCompileQuery([])]
    public IAsyncEnumerable<TenantVersion> TenantVersionsAsync()
    {
        return Queries.TenantVersionsAsync(this);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<byte[]> SettingValueAsync(int tenantId, string id)
    {
        return Queries.SettingValueAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public byte[] SettingValue(int tenantId, string id)
    {
        return Queries.SettingValue(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<DbCoreSettings> CoreSettingsAsync(int tenantId, string id)
    {
        return Queries.CoreSettingsAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([])]
    public IAsyncEnumerable<string> AddressAsync()
    {
        return Queries.AddressAsync(this);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<bool> AnyTenantsAsync(int tenantId, string domain)
    {
        return Queries.AnyTenantsAsync(this, tenantId, domain);
    }
}

static file class Queries
{
    public static readonly Func<TenantDbContext, string, Task<DbTenant>> TenantByDomainAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TenantDbContext ctx, string domain) =>
                ctx.Tenants
                    .Where(r => r.Alias == domain || r.MappedDomain == domain)
                    .OrderBy(a => a.Status == TenantStatus.Restoring ? TenantStatus.Active : a.Status)
                    .ThenByDescending(a => a.Status == TenantStatus.Restoring ? 0 : a.Id)
                    .FirstOrDefault());
    
    
    public static readonly Func<TenantDbContext, Task<int>> VersionIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TenantDbContext ctx) =>
                ctx.TenantVersion
                    .Where(r => r.DefaultVersion == 1 || r.Id == 0)
                    .OrderByDescending(r => r.Id)
                    .Select(r => r.Id)
                    .FirstOrDefault());

    public static readonly Func<TenantDbContext, int, Task<DbTenant>> TenantAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TenantDbContext ctx, int tenantId) =>
                ctx.Tenants.AsTracking().FirstOrDefault(r => r.Id == tenantId));

    public static readonly Func<TenantDbContext, int, Task<string>> GetAliasAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TenantDbContext ctx, int tenantId) =>
                ctx.Tenants
                    .Where(r => r.Id == tenantId)
                    .Select(r => r.Alias)
                    .FirstOrDefault());

    public static readonly Func<TenantDbContext, string, Task<int>> TenantsCountAsync = 
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
    (TenantDbContext ctx, string startAlias) =>
        ctx.Tenants
            .Count(r => r.Alias.StartsWith(startAlias)));

    public static readonly Func<TenantDbContext, IAsyncEnumerable<TenantVersion>> TenantVersionsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TenantDbContext ctx) =>
                ctx.TenantVersion
                    .Where(r => r.Visible)
                    .Select(r => new TenantVersion(r.Id, r.Version)));

    public static readonly Func<TenantDbContext, int, string, Task<byte[]>> SettingValueAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TenantDbContext ctx, int tenantId, string id) =>
                ctx.CoreSettings
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == id)
                    .Select(r => r.Value)
                    .FirstOrDefault());

    public static readonly Func<TenantDbContext, int, string, byte[]> SettingValue =
        Microsoft.EntityFrameworkCore.EF.CompileQuery(
            (TenantDbContext ctx, int tenantId, string id) =>
                ctx.CoreSettings
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id == id)
                    .Select(r => r.Value)
                    .FirstOrDefault());

    public static readonly Func<TenantDbContext, int, string, Task<DbCoreSettings>> CoreSettingsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TenantDbContext ctx, int tenantId, string id) =>
                ctx.CoreSettings.FirstOrDefault(r => r.TenantId == tenantId && r.Id == id));

    public static readonly Func<TenantDbContext, IAsyncEnumerable<string>> AddressAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TenantDbContext ctx) => ctx.TenantForbiden.Select(r => r.Address));

    public static readonly Func<TenantDbContext, int, string, Task<bool>> AnyTenantsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TenantDbContext ctx, int tenantId, string domain) =>
                ctx.Tenants
                    .Any(r => (r.Alias == domain ||
                              r.MappedDomain == domain && !(r.Status == TenantStatus.RemovePending ||
                                                           r.Status == TenantStatus.Restoring))
                                                       && r.Id != tenantId));
}