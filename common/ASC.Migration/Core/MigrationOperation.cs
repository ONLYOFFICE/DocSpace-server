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

using System.Extensions;
using System.Globalization;

namespace ASC.Migration.Core;

[Transient]
public class MigrationOperation : DistributedTaskProgress
{
    private string _migratorName;
    private Guid _userId;

    public int TenantId { get; set; }
    public MigrationApiInfo MigrationApiInfo { get; set; }
    public List<string> ImportedUsers { get; set; }
    public string LogName { get; set; }

    private readonly ILogger<MigrationOperation> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IFusionCache _hybridCache;

    public MigrationOperation()
    {

    }

    public MigrationOperation(ILogger<MigrationOperation> logger,
        IServiceScopeFactory serviceScopeFactory,
        IFusionCache hybridCache)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _hybridCache = hybridCache;
    }


    public void InitParse(int tenantId, Guid userId, string migratorName)
    {
        TenantId = tenantId;
        _migratorName = migratorName;
        _userId = userId;
    }

    public void InitMigrate(int tenantId, Guid userId, MigrationApiInfo migrationApiInfo)
    {
        TenantId = tenantId;
        MigrationApiInfo = migrationApiInfo;
        _migratorName = migrationApiInfo.MigratorName;
        _userId = userId;
    }

    protected override async Task DoJob()
    {
        var clearMigrationFolder = false;
        string folder = null;
        Migrator migrator = null;

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
        var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
        var migrationCore = scope.ServiceProvider.GetRequiredService<MigrationCore>();

        try
        {
            var onlyParse = MigrationApiInfo == null;
            var copyInfo = MigrationApiInfo.Clone();
            if (onlyParse)
            {
                MigrationApiInfo = new MigrationApiInfo();
            }
            CustomSynchronizationContext.CreateContext();

            var tenant = await tenantManager.SetCurrentTenantAsync(TenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);
            migrator = migrationCore.GetMigrator(_migratorName);
            migrator.OnProgressUpdateAsync = Migrator_OnProgressUpdateAsync;

            var culture = tenant.GetCulture();
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            if (migrator == null)
            {
                throw new ItemNotFoundException(MigrationResource.MigrationNotFoundException);
            }

            var key = GetMigrationFolderCacheKey(TenantId);
            folder = await _hybridCache.GetOrDefaultAsync<string>(key);
            await migrator.InitAsync(folder, onlyParse ? OperationType.Parse : OperationType.Migration, CancellationToken);

            var tenantQuota = await tenantManager.GetTenantQuotaAsync(TenantId);
            var maxTotalSize = tenantQuota?.MaxTotalSize ?? long.MaxValue;

            if (maxTotalSize > 0 && maxTotalSize != long.MaxValue)
            {
                var sourceFiles = onlyParse
                    ? []
                    : migrator.MigrationInfo.Files.Select(x=>Path.Combine(folder, x));

                var size = Directory
                    .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                    .Where(x => !sourceFiles.Contains(x))
                    .Sum(file => new FileInfo(file).Length);

                if (size > maxTotalSize)
                {
                    throw new Exception(MigrationResource.LargeBackup);
                }
            }

            await migrator.ParseAsync(onlyParse);
            if (!onlyParse)
            {
                await migrator.MigrateAsync(copyInfo);
                clearMigrationFolder =  true;
            }
        }
        catch (DirectoryNotFoundException)
        {
            Exception = new Exception(FilesCommonResource.ErrorMessage_FileNotFound);
            clearMigrationFolder =  true;
        }
        catch (Exception e)
        {
            Exception = e;
            clearMigrationFolder =  true;
            _logger.ErrorWithException(e);
            if (migrator is { MigrationInfo: not null })
            {
                MigrationApiInfo = migrator.MigrationInfo.ToApiInfo();
            }
        }
        finally
        {
            if (migrator != null)
            {
                ImportedUsers = migrator.GetGuidImportedUsers();
                LogName = migrator.GetLogName();
                await migrator.SaveLogAsync();
                await migrator.DisposeAsync();
            }
            if (!CancellationToken.IsCancellationRequested)
            {
                IsCompleted = true;
                await PublishChanges();
            }
            if (clearMigrationFolder || CancellationToken.IsCancellationRequested)
            {
                ClearMigrationFolder(folder);
            }
        }

        async Task Migrator_OnProgressUpdateAsync(double arg1, string arg2)
        {
            Percentage = arg1;
            if (migrator is { MigrationInfo: not null })
            {
                MigrationApiInfo = migrator.MigrationInfo.ToApiInfo();
            }
            await PublishChanges();
        }
    }

    public static string GetMigrationFolderCacheKey(int tenantId)
    {
        return $"migrationFolder_{tenantId}";
    }

    public static async Task ClearMigrationFolder(IServiceProvider serviceProvider, int tenantId)
    {
        var hybridCache = serviceProvider.GetService<IFusionCache>();
        var key = GetMigrationFolderCacheKey(tenantId);
        var path = await hybridCache.GetOrDefaultAsync<string>(key);
        ClearMigrationFolder(path);
    }

    public static void ClearMigrationFolder(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        _ = Task.Factory.StartNew(() =>
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        });
    }
}