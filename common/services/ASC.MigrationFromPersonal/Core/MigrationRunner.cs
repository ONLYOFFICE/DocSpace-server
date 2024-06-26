﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.MigrationFromPersonal.Core;


[Transient]
public class MigrationRunner
{
    private readonly DbFactory _dbFactory;
    private readonly StorageFactory _storageFactory;
    private readonly StorageFactoryConfig _storageFactoryConfig;
    private readonly ModuleProvider _moduleProvider;
    private readonly ILogger<RestoreDbModuleTask> _logger;
    private readonly CreatorDbContext _creatorDbContext;
    private long _totalSize;

    private string _backupFile;
    private string _region;
    private List<IModuleSpecifics> _modules;
    private readonly List<ModuleName> _namesModules = new List<ModuleName>()
    {
        ModuleName.Core,
        ModuleName.Files,
        ModuleName.Files2,
        ModuleName.Tenants,
        ModuleName.WebStudio
    };

    public MigrationRunner(
        DbFactory dbFactory,
        StorageFactory storageFactory,
        StorageFactoryConfig storageFactoryConfig,
        ModuleProvider moduleProvider,
        ILogger<RestoreDbModuleTask> logger,
        CreatorDbContext creatorDbContext)
    {
        _dbFactory = dbFactory;
        _storageFactory = storageFactory;
        _storageFactoryConfig = storageFactoryConfig;
        _moduleProvider = moduleProvider;
        _logger = logger;
        _creatorDbContext = creatorDbContext;
    }

    public async Task<(string, int)> RunAsync(string backupFile, string region, string fromAlias, string toAlias, long totalSize)
    {
        _totalSize = totalSize;
        _region = region;
        _modules = _moduleProvider.AllModules.Where(m => _namesModules.Contains(m.ModuleName)).ToList();
        _backupFile = backupFile;
        var columnMapper = new ColumnMapper();
        if (!string.IsNullOrEmpty(toAlias))
        {
            using var dbContextTenant = _creatorDbContext.CreateDbContext<TenantDbContext>();
            var fromTenant = dbContextTenant.Tenants.SingleOrDefault(q => q.Alias == fromAlias);

            using var dbContextToTenant = _creatorDbContext.CreateDbContext<TenantDbContext>(region);
            var toTenant = dbContextToTenant.Tenants.SingleOrDefault(q => q.Alias == toAlias);

            columnMapper.SetMapping("tenants_tenants", "id", fromTenant.Id, toTenant.Id);
            columnMapper.Commit();
        }

        using (var dataReader = new ZipReadOperator(_backupFile))
        {
            foreach (var module in _modules)
            {
                _logger.Debug($"start restore module: {module}");
                var restoreTask = new RestoreDbModuleTask(_logger, module, dataReader, columnMapper, _dbFactory, false, false, _region, _storageFactory, _storageFactoryConfig, _moduleProvider);

                await restoreTask.RunJob();
                _logger.Debug($"end restore module: {module}");
            }

            await DoRestoreStorage(dataReader, columnMapper);

            SetQuotarow(columnMapper.GetTenantMapping());
            SetTenantActiveaAndTenantOwner(columnMapper.GetTenantMapping());
            SetAdmin(columnMapper.GetTenantMapping());
            await SetTariffAsync(columnMapper.GetTenantMapping());
        }
        using var dbContextTenantRegion = _creatorDbContext.CreateDbContext<TenantDbContext>(_region);
        var tenantId = columnMapper.GetTenantMapping();
        var tenant = await dbContextTenantRegion.Tenants.SingleAsync(t => t.Id == tenantId);
        return (tenant.Alias, tenant.Id);
    }

    private async Task DoRestoreStorage(IDataReadOperator dataReader, ColumnMapper columnMapper)
    {
        var fileGroups = GetFilesToProcess(dataReader).GroupBy(file => file.Module).ToList();

        foreach (var group in fileGroups)
        {
            _logger.Debug($"start restore fileGroup: {group.Key}");
            foreach (var file in group)
            {
                var storage = await _storageFactory.GetStorageAsync(columnMapper.GetTenantMapping(), group.Key, _region);
                var quotaController = storage.QuotaController;
                storage.SetQuotaController(null);

                try
                {
                    var adjustedPath = file.Path;
                    var module = _moduleProvider.GetByStorageModule(file.Module, file.Domain);
                    if (module == null || module.TryAdjustFilePath(false, columnMapper, ref adjustedPath))
                    {
                        var key = file.GetZipKey();
                        using var stream = dataReader.GetEntry(key);

                        await storage.SaveAsync(file.Domain, adjustedPath, module != null ? module.PrepareData(key, stream, columnMapper) : stream);
                    }
                }
                finally
                {
                    if (quotaController != null)
                    {
                        storage.SetQuotaController(quotaController);
                    }
                }
            }
            _logger.Debug($"end restore fileGroup: {group.Key}");
        }
    }

    private IEnumerable<BackupFileInfo> GetFilesToProcess(IDataReadOperator dataReader)
    {
        using var stream = dataReader.GetEntry(KeyHelper.GetStorageRestoreInfoZipKey());
        if (stream == null)
        {
            return Enumerable.Empty<BackupFileInfo>();
        }

        var restoreInfo = XElement.Load(new StreamReader(stream));

        return restoreInfo.Elements("file").Select(BackupFileInfo.FromXElement).ToList();
    }

    private void SetQuotarow(int tenantId)
    {
        using var coreDbContext = _creatorDbContext.CreateDbContext<CoreDbContext>(_region);

        var row = new DbQuotaRow();
        row.TenantId = tenantId;
        row.Path = "/files/";
        row.UserId = Guid.Empty;
        row.Counter = _totalSize;
        row.Tag = "e67be73d-f9ae-4ce1-8fec-1880cb518cb4";
        row.LastModified = DateTime.UtcNow;
        coreDbContext.AddOrUpdate(coreDbContext.QuotaRows, row);
        coreDbContext.SaveChanges();
    }

    private void SetTenantActiveaAndTenantOwner(int tenantId)
    {
        using var dbContextTenant = _creatorDbContext.CreateDbContext<TenantDbContext>(_region);
        using var dbContextUser = _creatorDbContext.CreateDbContext<UserDbContext>(_region);

        var tenant = dbContextTenant.Tenants.Single(t => t.Id == tenantId);
        tenant.Status = TenantStatus.Active;
        _logger.Debug("set tenant status");
        tenant.CreationDateTime = DateTime.UtcNow;
        tenant.LastModified = DateTime.UtcNow;
        tenant.StatusChanged = DateTime.UtcNow;
        tenant.PaymentId = string.Empty;
        if (!dbContextUser.Users.Any(q => q.Id == tenant.OwnerId))
        {

            var user = dbContextUser.Users.Single(u => u.TenantId == tenantId);
            tenant.OwnerId = user.Id;
            _logger.Debug($"set ownerId {user.Id}");
        }
        dbContextTenant.Tenants.Update(tenant);
        dbContextTenant.SaveChanges();
    }

    private async Task SetTariffAsync(int tenantId)
    {
        await using var coreDbContext = _creatorDbContext.CreateDbContext<CoreDbContext>(_region);
        var stamp = DateTime.MaxValue;
        stamp = stamp.Date.Add(new TimeSpan(DateTime.MaxValue.Hour, DateTime.MaxValue.Minute, DateTime.MaxValue.Second));

        var tariff = new DbTariff();
        tariff.TenantId = tenantId;
        tariff.Id = -tenantId;
        tariff.Stamp = stamp;
        tariff.CreateOn = DateTime.UtcNow;
        tariff.CustomerId = "";

        await coreDbContext.AddOrUpdateAsync(q => q.Tariffs, tariff);

        var tariffs = coreDbContext.TariffRows.Where(t => t.TenantId == tenantId);
        coreDbContext.TariffRows.RemoveRange(tariffs);

        var tariffRow = new DbTariffRow();
        tariffRow.TenantId = tenantId;
        tariffRow.TariffId = -tenantId;
        tariffRow.Quota = -3;
        tariffRow.Quantity = 1;

        await coreDbContext.AddOrUpdateAsync(q => q.TariffRows, tariffRow);

        await coreDbContext.SaveChangesAsync();
    }
    
    private void SetAdmin(int tenantId)
    {
        using var dbContextTenant = _creatorDbContext.CreateDbContext<TenantDbContext>(_region);
        var tenant = dbContextTenant.Tenants.Single(t => t.Id == tenantId);
        using var dbContextUser = _creatorDbContext.CreateDbContext<UserDbContext>(_region);

        if (!dbContextUser.UserGroups.Any(q => q.TenantId == tenantId))
        {
            var userGroup = new UserGroup()
            {
                TenantId = tenantId,
                LastModified = DateTime.UtcNow,
                RefType = ASC.Core.UserGroupRefType.Contains,
                Removed = false,
                UserGroupId = ASC.Common.Security.Authorizing.Constants.DocSpaceAdmin.ID,
                Userid = tenant.OwnerId.Value
            };

            dbContextUser.UserGroups.Add(userGroup);
            dbContextUser.SaveChanges();
        }
    }
}