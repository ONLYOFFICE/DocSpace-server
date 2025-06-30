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
using ASC.Core.Data;
using ASC.FederatedLogin.Profile;

using Microsoft.EntityFrameworkCore;

namespace ASC.Site.Core.Classes
{
    [Scope]
    public class MultiRegionPrivider(
        MachinePseudoKeys machinePseudoKeys,
        IDbContextFactory<EuUserDbContext> euUserDbContextFactory,
        IDbContextFactory<UsUserDbContext> usUserDbContextFactory)
    {
        public async Task<DbTenant> FindTenantByDomainAsync(string domain)
        {
            await using var euUserDbContext = await euUserDbContextFactory.CreateDbContextAsync();
            var euTask = GetTenantByDomainAsync(euUserDbContext, domain);

            await using var usUserDbContext = await usUserDbContextFactory.CreateDbContextAsync();
            var usTask = GetTenantByDomainAsync(usUserDbContext, domain);

            await Task.WhenAll(euTask, usTask);

            return euTask.Result ?? usTask.Result;
        }

        public async Task<List<TenantUser>> FindTenantsByEmailAsync(string email)
        {
            var result = new List<TenantUser>();

            await using var euUserDbContext = await euUserDbContextFactory.CreateDbContextAsync();
            var euTask = GetTenantsByEmailAsync(euUserDbContext, email);

            await using var usUserDbContext = await usUserDbContextFactory.CreateDbContextAsync();
            var usTask = GetTenantsByEmailAsync(usUserDbContext, email);

            await Task.WhenAll(euTask, usTask);

            return [.. euTask.Result, .. usTask.Result];
        }

        public async Task<List<TenantUser>> FindTenantsByEmailPasswordAsync(string email, string passwordHash)
        {
            await using var euUserDbContext = await euUserDbContextFactory.CreateDbContextAsync();
            var euTask = GetTenantsByEmailPasswordAsync(euUserDbContext, email, passwordHash);

            await using var usUserDbContext = await usUserDbContextFactory.CreateDbContextAsync();
            var usTask = GetTenantsByEmailPasswordAsync(usUserDbContext, email, passwordHash);

            await Task.WhenAll(euTask, usTask);

            return [.. euTask.Result, .. usTask.Result];
        }

        public async Task<List<TenantUser>> FindTenantsBySocialAsync(LoginProfile loginProfile)
        {
            await using var euUserDbContext = await euUserDbContextFactory.CreateDbContextAsync();
            var euTask = GetTenantsBySocialAsync(euUserDbContext, loginProfile);

            await using var usUserDbContext = await usUserDbContextFactory.CreateDbContextAsync();
            var usTask = GetTenantsBySocialAsync(usUserDbContext, loginProfile);

            await Task.WhenAll(euTask, usTask);

            return [.. euTask.Result, .. usTask.Result];
        }


        private static Task<DbTenant> GetTenantByDomainAsync(RegionUserDbContext userDbContext, string domain)
        {
            return userDbContext.Tenants
                .Where(t => t.Alias == domain || t.MappedDomain == domain)
                .Where(t => t.Status != TenantStatus.Suspended && t.Status != TenantStatus.RemovePending)
                .OrderBy(a => a.Status == TenantStatus.Restoring ? TenantStatus.Active : a.Status)
                .ThenByDescending(a => a.Status == TenantStatus.Restoring ? 0 : a.Id)
                .FirstOrDefaultAsync();
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

        private string GetPasswordHash(Guid userId, string password)
        {
            return Hasher.Base64Hash(password + userId + Encoding.UTF8.GetString(machinePseudoKeys.GetMachineConstant()), HashAlg.SHA512);
        }

        private async Task<List<TenantUser>> GetTenantsByEmailPasswordAsync(RegionUserDbContext userDbContext, string email, string passwordHash)
        {
            var usersQuery = await userDbContext.Tenants
                .Where(r => r.Status == TenantStatus.Active)
                .Join(userDbContext.Users, r => r.Id, r => r.TenantId, (tenant, user) => new
                {
                    tenant,
                    user
                })
                .Where(r => r.user.Status == EmployeeStatus.Active && !r.user.Removed && r.user.Email == email)
                .Select(r => r.user.Id)
                .ToListAsync();

            var passwordHashs = usersQuery.Select(r => GetPasswordHash(r, passwordHash)).ToList();

            return await userDbContext.Tenants
                .Where(r => r.Status == TenantStatus.Active)
                .Join(userDbContext.Users, r => r.Id, r => r.TenantId, (tenant, user) => new
                {
                    tenant,
                    user
                })
                .Join(userDbContext.UserSecurity, r => r.user.Id, r => r.UserId, (tenantUser, security) => new TenantUserSecurity
                {
                    DbTenant = tenantUser.tenant,
                    User = tenantUser.user,
                    UserSecurity = security
                })
                .Where(r => r.User.Status == EmployeeStatus.Active && !r.User.Removed && r.User.Email == email)
                .Where(r => passwordHashs.Any(p => r.UserSecurity.PwdHash == p))
                .Select(r => new TenantUser
                 {
                     UserId = r.User.Id,
                     UserEmail = r.User.Email,
                     UserFirstName = r.User.FirstName,
                     UserLastName = r.User.LastName,
                     TenantId = r.DbTenant.Id,
                     TenantAlias = r.DbTenant.Alias,
                     TenantMappedDomain = r.DbTenant.MappedDomain
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
        public DbSet<UserSecurity> UserSecurity { get; set; }
        public DbSet<AccountLinks> AccountLinks { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ModelBuilderWrapper
               .From(modelBuilder, Database)
               .AddDbTenant()
               .AddUser()
               .AddUserSecurity()
               .AddAccountLinks();
        }
    }
}