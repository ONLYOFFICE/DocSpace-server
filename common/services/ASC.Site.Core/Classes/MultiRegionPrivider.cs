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

using ASC.Core.Common.EF.Model;
using ASC.FederatedLogin.Profile;

using Microsoft.EntityFrameworkCore;

namespace ASC.Site.Core.Classes
{
    [Scope]
    public class MultiRegionPrivider(
        IDbContextFactory<EuUserDbContext> euUserDbContextFactory,
        IDbContextFactory<UsUserDbContext> usUserDbContextFactory)
    {
        public async Task<List<TenantUser>> FindTenantsByEmailAsync(string email)
        {
            var result = new List<TenantUser>();

            await using var euUserDbContext = await euUserDbContextFactory.CreateDbContextAsync();
            var euTenants = await GetTenantsByEmailAsync(euUserDbContext, email);
            result.AddRange(euTenants);

            await using var usUserDbContext = await usUserDbContextFactory.CreateDbContextAsync();
            var usTenants = await GetTenantsByEmailAsync(usUserDbContext, email);
            result.AddRange(usTenants);

            return result;
        }

        public async Task<List<TenantUser>> FindTenantsBySocialAsync(LoginProfile loginProfile)
        {
            var result = new List<TenantUser>();

            await using var euUserDbContext = await euUserDbContextFactory.CreateDbContextAsync();
            var euTenants = await GetTenantsBySocialAsync(euUserDbContext, loginProfile);
            result.AddRange(euTenants);

            await using var usUserDbContext = await usUserDbContextFactory.CreateDbContextAsync();
            var usTenants = await GetTenantsBySocialAsync(usUserDbContext, loginProfile);
            result.AddRange(usTenants);

            return result;
        }

        private static Task<List<TenantUser>> GetTenantsByEmailAsync(RegionUserDbContext userDbContext, string email)
        {
            return userDbContext.Tenants
                .Where(t => t.Status == TenantStatus.Active)
                .Join(userDbContext.Users, t => t.Id, u => u.TenantId, (tenant, user) => new
                { 
                    tenant,
                    user
                })
                .Where(r => !r.user.Removed && r.user.Status == EmployeeStatus.Active && r.user.Email == email)
                .Select(r => new TenantUser
                {
                    UserId = r.user.Id,
                    UserEmail = r.user.Email,
                    UserFirstName = r.user.FirstName,
                    UserLastName = r.user.LastName,
                    TenantId = r.tenant.Id,
                    TenantAlias = r.tenant.Alias,
                    TenantMappedDomain = r.tenant.MappedDomain
                })
                .ToListAsync();
        }

        private static Task<List<TenantUser>> GetTenantsBySocialAsync(RegionUserDbContext userDbContext, LoginProfile loginProfile)
        {
            return userDbContext.Users
                 .Join(userDbContext.Tenants, u => u.TenantId, t => t.Id, (user, tenant) => new
                 {
                     user,
                     tenant
                 })
                 .Join(userDbContext.AccountLinks, r => r.user.Id.ToString(), a => a.Id, (r, account) => new
                 {
                     r.tenant,
                     r.user,
                     account
                 })
                 .Where(r => r.tenant.Status == TenantStatus.Active)
                 .Where(r => !r.user.Removed && r.user.Status == EmployeeStatus.Active)
                 .Where(r => r.user.Email == loginProfile.EMail || r.account.UId == loginProfile.HashId)
                 .Select(r => new TenantUser
                 {
                     UserId = r.user.Id,
                     UserEmail = r.user.Email,
                     UserFirstName = r.user.FirstName,
                     UserLastName = r.user.LastName,
                     TenantId = r.tenant.Id,
                     TenantAlias = r.tenant.Alias,
                     TenantMappedDomain = r.tenant.MappedDomain
                 })
                 .ToListAsync();
        }
    }


    public class TenantUser
    {
        public Guid UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public int TenantId { get; set; }
        public string TenantAlias { get; set; }
        public string TenantMappedDomain { get; set; }
    }

    public partial class EuUserDbContext(DbContextOptions<EuUserDbContext> dbContextOptions) : RegionUserDbContext(dbContextOptions) { }

    public partial class UsUserDbContext(DbContextOptions<UsUserDbContext> dbContextOptions) : RegionUserDbContext(dbContextOptions) { }

    public class RegionUserDbContext(DbContextOptions options): BaseDbContext(options)
    {
        public DbSet<DbTenant> Tenants { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AccountLinks> AccountLinks { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ModelBuilderWrapper
               .From(modelBuilder, Database)
               .AddDbTenant()
               .AddUser()
               .AddAccountLinks();
        }
    }
}