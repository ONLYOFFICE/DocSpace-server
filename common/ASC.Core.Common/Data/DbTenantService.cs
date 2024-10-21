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

namespace ASC.Core.Data;

[Scope]
public class DbTenantService(
    IDbContextFactory<TenantDbContext> dbContextFactory,
    IDbContextFactory<UserDbContext> userDbContextFactory,
    TenantDomainValidator tenantDomainValidator,
    MachinePseudoKeys machinePseudoKeys,
    IMapper mapper)
    : ITenantService
{
    private List<string> _forbiddenDomains;

    public async Task ValidateDomainAsync(string domain)
    {
        // TODO: Why does open transaction?
        //        using var tr = TenantDbContext.Database.BeginTransaction();
        await ValidateDomainAsync(domain, Tenant.DefaultTenant, true);
    }

    public void ValidateTenantName(string name)
    {
        tenantDomainValidator.ValidateTenantName(name);
    }
    
    public async Task<IEnumerable<Tenant>> GetTenantsAsync(DateTime from, bool active = true)
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();
        var q = tenantDbContext.Tenants.AsQueryable();

        if (active)
        {
            q = q.Where(r => r.Status == TenantStatus.Active);
        }

        if (from != default)
        {
            q = q.Where(r => r.LastModified >= from);
        }

        return await q.ProjectTo<Tenant>(mapper.ConfigurationProvider).ToListAsync();
    }
    
    public async Task<IEnumerable<Tenant>> GetTenantsAsync(List<int> ids)
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();

        return await tenantDbContext.Tenants
            .Where(r => ids.Contains(r.Id) && r.Status == TenantStatus.Active)
            .ProjectTo<Tenant>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<IEnumerable<Tenant>> GetTenantsAsync(string login, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrEmpty(login);

        //await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();
        await using var userDbContext = await userDbContextFactory.CreateDbContextAsync();//TODO: remove
        IQueryable<TenantUserSecurity> Query() => userDbContext.Tenants
                
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
                .Where(r => r.User.Status == EmployeeStatus.Active)
                .Where(r => r.DbTenant.Status == TenantStatus.Active)
                .Where(r => !r.User.Removed);

        if (passwordHash == null)
        {
            var q = Query()
                .Where(r => login.Contains('@') ? r.User.Email == login : r.User.Id.ToString() == login);

            return await q.ProjectTo<Tenant>(mapper.ConfigurationProvider).ToListAsync();
        }

        if (Guid.TryParse(login, out var userId))
        {
            var pwdHash = GetPasswordHash(userId, passwordHash);
            var q = Query()
                .Where(r => r.User.Id == userId)
                .Where(r => r.UserSecurity.PwdHash == pwdHash);

            return await q.ProjectTo<Tenant>(mapper.ConfigurationProvider).ToListAsync();
        }
        else
        {
            var usersQuery = await userDbContext.Users
                .Where(r => r.Email == login)
                .Where(r => r.Status == EmployeeStatus.Active)
                .Where(r => !r.Removed)
                .Select(r => r.Id)
                .ToListAsync();

            var passwordHashs = usersQuery.Select(r => GetPasswordHash(r, passwordHash)).ToList();

            var q = Query()
                .Where(r => passwordHashs.Any(p => r.UserSecurity.PwdHash == p) && r.DbTenant.Status == TenantStatus.Active);

            return await q.ProjectTo<Tenant>(mapper.ConfigurationProvider).ToListAsync();
        }
    }

    public async Task<Tenant> RestoreTenantAsync(int oldId, Tenant newTenant, CoreSettings coreSettings)
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();

        _ = await InnerRemoveTenantAsync(tenantDbContext, oldId);

        _ = await InnerSaveTenantAsync(tenantDbContext, coreSettings, newTenant);

        await tenantDbContext.SaveChangesWithValidateAsync();

        return newTenant;
    }

    public async Task<Tenant> GetTenantAsync(int id)
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();
        return await tenantDbContext.Tenants
            .Where(r => r.Id == id)
            .ProjectTo<Tenant>(mapper.ConfigurationProvider)
            .SingleOrDefaultAsync();
    }
    
    public async Task<Tenant> GetTenantAsync(string domain)
    {
        ArgumentException.ThrowIfNullOrEmpty(domain);

        domain = domain.ToLowerInvariant();

        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();

        return mapper.Map<Tenant>(await tenantDbContext.TenantByDomainAsync(domain));
    }
    
    public Tenant GetTenant(string domain)
    {
        ArgumentException.ThrowIfNullOrEmpty(domain);

        domain = domain.ToLowerInvariant();

        using var tenantDbContext = dbContextFactory.CreateDbContext();

        return tenantDbContext.Tenants
            .Where(r => r.Alias == domain || r.MappedDomain == domain)
            .OrderBy(a => a.Status == TenantStatus.Restoring ? TenantStatus.Active : a.Status)
            .ThenByDescending(a => a.Status == TenantStatus.Restoring ? 0 : a.Id)
            .ProjectTo<Tenant>(mapper.ConfigurationProvider)
            .FirstOrDefault();
    }

    public Tenant GetTenantForStandaloneWithoutAlias(string ip)
    {
        using var tenantDbContext = dbContextFactory.CreateDbContext();

        return tenantDbContext.Tenants
             .Where(t => t.Id != -1)
            .OrderBy(a => a.Status)
            .ThenBy(a => a.Id)
            .ProjectTo<Tenant>(mapper.ConfigurationProvider)
            .FirstOrDefault();
    }

    public async Task<Tenant> GetTenantForStandaloneWithoutAliasAsync(string ip)
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();

        return await tenantDbContext.Tenants
            .Where(t => t.Id != -1)
            .Where(t => t.Status != TenantStatus.Suspended && t.Status != TenantStatus.RemovePending)
            .OrderBy(a => a.Status)
            .ThenBy(a => a.Id)
            .ProjectTo<Tenant>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public async Task<Tenant> SaveTenantAsync(CoreSettings coreSettings, Tenant tenant)
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();

        var dbtenant = await InnerSaveTenantAsync(tenantDbContext, coreSettings, tenant);

        if(dbtenant != null)
        {
            await tenantDbContext.SaveChangesWithValidateAsync();
        }

        return tenant;
    }

    public async Task RemoveTenantAsync(int id, bool auto = false)
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();

        var tenant = await InnerRemoveTenantAsync(tenantDbContext, id, auto);

        if (tenant != null)
        {
            await tenantDbContext.SaveChangesWithValidateAsync();
        }
    }

    public async Task PermanentlyRemoveTenantAsync(int id)
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();
        var tenant = await tenantDbContext.Tenants.SingleOrDefaultAsync(r => r.Id == id);
        tenantDbContext.Tenants.Remove(tenant);
        await tenantDbContext.SaveChangesWithValidateAsync();
    }

    public async Task<IEnumerable<TenantVersion>> GetTenantVersionsAsync()
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();
        return await tenantDbContext.TenantVersionsAsync().ToListAsync();
    }


    public async Task<byte[]> GetTenantSettingsAsync(int tenant, string key)
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();
        return await tenantDbContext.SettingValueAsync(tenant, key);
    }

    public byte[] GetTenantSettings(int tenant, string key)
    {
        using var tenantDbContext = dbContextFactory.CreateDbContext();
        return tenantDbContext.SettingValue(tenant, key);
    }


    public async Task SetTenantSettingsAsync(int tenant, string key, byte[] data)
    {
        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();
        if (data == null || data.Length == 0)
        {
            var settings = await tenantDbContext.CoreSettingsAsync(tenant, key);

            if (settings != null)
            {
                tenantDbContext.CoreSettings.Remove(settings);
            }
        }
        else
        {
            var settings = new DbCoreSettings
            {
                Id = key,
                TenantId = tenant,
                Value = data,
                LastModified = DateTime.UtcNow
            };

            await tenantDbContext.AddOrUpdateAsync(q => q.CoreSettings, settings);
        }

        await tenantDbContext.SaveChangesWithValidateAsync();
    }

    private async Task ValidateDomainAsync(string domain, int tenantId, bool validateCharacters)
    {
        // size
        tenantDomainValidator.ValidateDomainLength(domain);

        // characters
        if (validateCharacters)
        {
            tenantDomainValidator.ValidateDomainCharacters(domain);
        }

        await using var tenantDbContext = await dbContextFactory.CreateDbContextAsync();
        // forbidden or exists

        domain = domain.ToLowerInvariant();
        _forbiddenDomains ??= await tenantDbContext.AddressAsync().ToListAsync();

        var exists = tenantId != 0 && _forbiddenDomains.Contains(domain);

        if (!exists)
        {
            exists = await tenantDbContext.AnyTenantsAsync(tenantId, domain);
        }
        if (exists)
        {
            // cut number suffix
            while (true)
            {
                if (tenantDomainValidator.MinLength < domain.Length && char.IsNumber(domain, domain.Length - 1))
                {
                    domain = domain[..^1];
                }
                else
                {
                    break;
                }
            }

            var existsTenants = await tenantDbContext.TenantForbiden.Where(r => r.Address.StartsWith(domain)).Select(r => r.Address).ToListAsync();
            existsTenants.AddRange(await tenantDbContext.Tenants.Where(r => r.Alias.StartsWith(domain) && r.Id != tenantId).Select(r => r.Alias).ToListAsync());
            existsTenants.AddRange(await tenantDbContext.Tenants.Where(r => r.MappedDomain.StartsWith(domain) && r.Id != tenantId && r.Status != TenantStatus.RemovePending).Select(r => r.MappedDomain).ToListAsync());

            throw new TenantAlreadyExistsException("Address busy.", existsTenants.Distinct());
        }
    }

    private string GetPasswordHash(Guid userId, string password)
    {
        return Hasher.Base64Hash(password + userId + Encoding.UTF8.GetString(machinePseudoKeys.GetMachineConstant()), HashAlg.SHA512);
    }

    private async Task<DbTenant> InnerSaveTenantAsync(TenantDbContext tenantDbContext, CoreSettings coreSettings, Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        DbTenant dbTenant;

        if (!string.IsNullOrEmpty(tenant.MappedDomain))
        {
            var baseUrl = coreSettings.GetBaseDomain(tenant.HostedRegion);

            if (baseUrl != null && tenant.MappedDomain.EndsWith("." + baseUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                await ValidateDomainAsync(tenant.MappedDomain[..(tenant.MappedDomain.Length - baseUrl.Length - 1)], tenant.Id, false);
            }
            else
            {
                await ValidateDomainAsync(tenant.MappedDomain, tenant.Id, false);
            }
        }

        if (tenant.Id == Tenant.DefaultTenant)
        {
            tenant.Version = await tenantDbContext.VersionIdAsync();

            tenant.LastModified = DateTime.UtcNow;

            dbTenant = mapper.Map<Tenant, DbTenant>(tenant);
            dbTenant.Id = 0;

            var entity = await tenantDbContext.Tenants.AddAsync(dbTenant);
            dbTenant = entity.Entity;

            await tenantDbContext.SaveChangesWithValidateAsync();
            tenant.Id = dbTenant.Id;
        }
        else
        {
            dbTenant = await tenantDbContext.TenantAsync(tenant.Id);

            if (dbTenant != null)
            {
                dbTenant.Alias = tenant.Alias.ToLowerInvariant();
                dbTenant.MappedDomain = !string.IsNullOrEmpty(tenant.MappedDomain) ? tenant.MappedDomain.ToLowerInvariant() : null;
                dbTenant.Version = tenant.Version;
                dbTenant.VersionChanged = tenant.VersionChanged;
                dbTenant.Name = tenant.Name ?? "";
                dbTenant.Language = tenant.Language;
                dbTenant.TimeZone = tenant.TimeZone;
                dbTenant.TrustedDomainsRaw = tenant.GetTrustedDomains();
                dbTenant.TrustedDomainsEnabled = tenant.TrustedDomainsType;
                dbTenant.CreationDateTime = tenant.CreationDateTime;
                dbTenant.Status = tenant.Status;
                dbTenant.StatusChanged = tenant.StatusChangeDate;
                dbTenant.PaymentId = tenant.PaymentId;
                dbTenant.LastModified = tenant.LastModified = DateTime.UtcNow;
                dbTenant.Industry = tenant.Industry;
                dbTenant.Calls = tenant.Calls;
                dbTenant.OwnerId = tenant.OwnerId;

                tenantDbContext.Update(dbTenant);
            }
        }

        //CalculateTenantDomain(t);
        return dbTenant;
    }

    private async Task<DbTenant> InnerRemoveTenantAsync(TenantDbContext tenantDbContext, int id, bool auto = false)
    {
        var postfix = auto ? "_auto_deleted" : "_deleted";

        var alias = await tenantDbContext.GetAliasAsync(id);

        var count = await tenantDbContext.TenantsCountAsync(alias + postfix);

        var tenant = await tenantDbContext.TenantAsync(id);

        if (tenant != null)
        {
            tenant.Alias = alias + postfix + (count > 0 ? count.ToString() : "");
            tenant.Status = TenantStatus.RemovePending;
            tenant.StatusChanged = DateTime.UtcNow;
            tenant.LastModified = DateTime.UtcNow;

            tenantDbContext.Update(tenant);
        }
        return tenant;
    }
}

public class TenantUserSecurity
{
    public DbTenant DbTenant { get; init; }
    public User User { get; init; }
    public UserSecurity UserSecurity { get; init; }
}