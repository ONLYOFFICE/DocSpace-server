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

namespace ASC.Core;

public class MultiRegionHostedSolution
{
    private readonly Dictionary<string, HostedSolution> _regions = new();
    private readonly string _dbid;

    private readonly IConfiguration _configuraion;
    public ConfigurationExtension ConfigurationExtension { get; }
    private readonly CookieStorage _cookieStorage;
    private readonly EFLoggerFactory _loggerFactory;
    private readonly PasswordHasher _passwordHasher;
    private readonly IOptionsSnapshot<HostedSolution> _hostedSolutionOptions;

    public MultiRegionHostedSolution(string dbid,
        IConfiguration configuraion,
        ConfigurationExtension configurationExtension,
        CookieStorage cookieStorage,
        EFLoggerFactory loggerFactory,
        PasswordHasher passwordHasher,
        IOptionsSnapshot<HostedSolution> hostedSolutionOptions)
    {
        _dbid = dbid;
        _configuraion = configuraion;
        ConfigurationExtension = configurationExtension;
        _cookieStorage = cookieStorage;
        _loggerFactory = loggerFactory;
        _passwordHasher = passwordHasher;
        _hostedSolutionOptions = hostedSolutionOptions;

        Initialize();
    }

    public async Task<List<Tenant>> GetTenantsAsync(DateTime from)
    {
        var result = new List<Tenant>();

        foreach (var solution in GetRegionServices())
        {
            result.AddRange(await solution.GetTenantsAsync(from));
        }

        return result;
    }

    public async Task<List<Tenant>> FindTenantsAsync(string login, string password = null, string passwordHash = null)
    {
        var result = new List<Tenant>();
        Exception error = null;

        foreach (var service in GetRegionServices())
        {
            try
            {
                if (string.IsNullOrEmpty(passwordHash) && !string.IsNullOrEmpty(password))
                {
                    passwordHash = _passwordHasher.GetClientPassword(password);
                }

                result.AddRange(await service.FindTenantsAsync(login, passwordHash));
            }
            catch (SecurityException exception)
            {
                error = exception;
            }
        }
        if (result.Count == 0 && error != null)
        {
            throw error;
        }

        return result;
    }

    public async Task<Tenant> RegisterTenantAsync(string region, TenantRegistrationInfo ri)
    {
        ri.HostedRegion = region;
        return await GetRegionService(region).RegisterTenantAsync(ri);
    }

    public async Task<Tenant> GetTenantAsync(string domain)
    {
        foreach (var service in GetRegionServices())
        {
            var tenant = await service.GetTenantAsync(domain);
            if (tenant != null)
            {
                return tenant;
            }
        }

        return null;
    }

    public async Task<Tenant> GetTenantAsync(string region, int tenantId)
    {
        return await GetRegionService(region).GetTenantAsync(tenantId);
    }

    public async Task<Tenant> SaveTenantAsync(string region, Tenant tenant)
    {
        return await GetRegionService(region).SaveTenantAsync(tenant);
    }

    public async Task<string> CreateAuthenticationCookieAsync(string region, int tenantId, Guid userId)
    {
        return await GetRegionService(region).CreateAuthenticationCookieAsync(_cookieStorage, tenantId, userId);
    }

    public async Task<Tariff> GetTariffAsync(string region, int tenantId, bool withRequestToPaymentSystem = true)
    {
        return await GetRegionService(region).GetTariffAsync(tenantId, withRequestToPaymentSystem);
    }

    public async Task SetTariffAsync(string region, int tenant, bool paid)
    {
        await GetRegionService(region).SetTariffAsync(tenant, paid);
    }

    public async Task SetTariffAsync(string region, int tenant, Tariff tariff)
    {
        await GetRegionService(region).SetTariffAsync(tenant, tariff);
    }

    public async Task<TenantQuota> GetTenantQuotaAsync(string region, int tenant)
    {
        return await GetRegionService(region).GetTenantQuotaAsync(tenant);
    }

    public async Task CheckTenantAddressAsync(string address)
    {
        foreach (var service in GetRegionServices())
        {
            await service.CheckTenantAddressAsync(address);
        }
    }

    private IEnumerable<HostedSolution> GetRegionServices()
    {
        return _regions.Where(x => !string.IsNullOrEmpty(x.Key))
               .Select(x => x.Value);
    }

    private HostedSolution GetRegionService(string region)
    {
        return _regions[region];
    }

    private void Initialize()
    {
        var connectionStrings = ConfigurationExtension.GetConnectionStrings();

        if (Convert.ToBoolean(_configuraion["core.multi-hosted.config-only"] ?? "false"))
        {
            foreach (var cs in connectionStrings)
            {
                if (cs.Name.StartsWith(_dbid + "."))
                {
                    var name = cs.Name[(_dbid.Length + 1)..];
                    _regions[name] = _hostedSolutionOptions.Get(cs.Name);
                }
            }

            _regions[_dbid] = _hostedSolutionOptions.Get(_dbid);
            if (!_regions.ContainsKey(string.Empty))
            {
                _regions[string.Empty] = _hostedSolutionOptions.Get(_dbid);
            }
        }
        else
        {

            var find = false;
            foreach (var cs in connectionStrings)
            {
                var dbContextOptionsBuilder = new DbContextOptionsBuilder<CustomDbContext>();
                var options = dbContextOptionsBuilder
                    //.UseMySql(cs.ConnectionString)
                    .UseNpgsql(cs.ConnectionString)
                    .UseLoggerFactory(_loggerFactory)
                    .Options;

                using var dbContext = new CustomDbContext(options);

                if (cs.Name.StartsWith(_dbid + "."))
                {
                    var name = cs.Name[(_dbid.Length + 1)..];
                    _regions[name] = _hostedSolutionOptions.Get(name);
                    find = true;
                }
            }
            if (find)
            {
                _regions[_dbid] = _hostedSolutionOptions.Get(_dbid);
                if (!_regions.ContainsKey(string.Empty))
                {
                    _regions[string.Empty] = _hostedSolutionOptions.Get(_dbid);
                }
            }
            else
            {
                foreach (var connectionString in connectionStrings)
                {
                    try
                    {
                        var dbContextOptionsBuilder = new DbContextOptionsBuilder<CustomDbContext>();
                        var options = dbContextOptionsBuilder
                            //.UseMySql(connectionString.ConnectionString)
                            .UseNpgsql(connectionString.ConnectionString)
                            .UseLoggerFactory(_loggerFactory)
                            .Options;

                        using var dbContext = new CustomDbContext(options);

                        var q = dbContext.Regions.ToList();

                        foreach (var r in q)
                        {
                            var cs = new ConnectionStringSettings(r.Region, r.ConnectionString, r.Provider);

                            if (!_regions.ContainsKey(string.Empty))
                            {
                                _regions[string.Empty] = _hostedSolutionOptions.Get(cs.Name);
                            }

                            _regions[cs.Name] = _hostedSolutionOptions.Get(cs.Name);
                        }
                    }
                    catch (DbException) { }
                }
            }
        }
    }
}