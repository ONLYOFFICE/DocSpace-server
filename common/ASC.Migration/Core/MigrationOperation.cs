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

using System.Extensions;

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
    private readonly MigrationCore _migrationCore;
    private readonly TenantManager _tenantManager;
    private readonly SecurityContext _securityContext;
    private readonly IFusionCache _hybridCache;

    public MigrationOperation()
    {

    }

    public MigrationOperation(ILogger<MigrationOperation> logger,
        MigrationCore migrationCore,
        TenantManager tenantManager,
        SecurityContext securityContext,
        IFusionCache hybridCache)
    {
        _logger = logger;
        _migrationCore = migrationCore;
        _tenantManager = tenantManager;
        _securityContext = securityContext;
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
        Migrator migrator = null;
        try
        {
            var onlyParse = MigrationApiInfo == null;
            var copyInfo = MigrationApiInfo.Clone();
            if (onlyParse)
            {
                MigrationApiInfo = new MigrationApiInfo();
            }
            CustomSynchronizationContext.CreateContext();

            var tenant = await _tenantManager.GetTenantAsync(TenantId);
            _tenantManager.SetCurrentTenant(tenant);
            await _securityContext.AuthenticateMeWithoutCookieAsync(_userId);
            migrator = _migrationCore.GetMigrator(_migratorName);
            migrator.OnProgressUpdateAsync = Migrator_OnProgressUpdateAsync;

            if (migrator == null)
            {
                throw new ItemNotFoundException(MigrationResource.MigrationNotFoundException);
            }

            var folder = await _hybridCache.GetOrDefaultAsync<string>($"migration folder - {TenantId}");
            await migrator.InitAsync(folder, onlyParse ? OperationType.Parse : OperationType.Migration, CancellationToken);

            var tenantQuota = await _tenantManager.GetTenantQuotaAsync(TenantId);
            var maxTotalSize = tenantQuota?.MaxTotalSize ?? long.MaxValue;

            if (maxTotalSize > 0 && maxTotalSize != long.MaxValue)
            {
                var size = Directory
                    .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
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
            }
        }
        catch (DirectoryNotFoundException)
        {
            Exception = new Exception(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        catch (Exception e)
        {
            Exception = e;
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
                await migrator.DisposeAsync();
            }
            if (!CancellationToken.IsCancellationRequested)
            {
                IsCompleted = true;
                await PublishChanges();
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
}