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

using System.Extensions;

namespace ASC.Migration.Core;

[Transient]
public class MigrationOperation(
    ILogger<MigrationOperation> logger,
    MigrationCore migrationCore,
    TenantManager tenantManager,
    SecurityContext securityContext,
    IServiceProvider serviceProvider,
    IDistributedCache cache)
    : DistributedTaskProgress, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1);
    private string _migratorName;
    private Guid _userId;

    private int? _tenantId;
    public int TenantId
    {
        get => _tenantId ?? this[nameof(_tenantId)];
        set
        {
            _tenantId = value;
            this[nameof(_tenantId)] = value;
        }
    }

    private MigrationApiInfo _migrationApiInfo;
    public MigrationApiInfo MigrationApiInfo
    {
        get => _migrationApiInfo ?? JsonSerializer.Deserialize<MigrationApiInfo>(this[nameof(_migrationApiInfo)]);
        set
        {
            _migrationApiInfo = value;
            this[nameof(_migrationApiInfo)] = JsonSerializer.Serialize(value);
        }
    }

    private List<string> _importedUsers;
    public List<string> ImportedUsers
    {
        get => _importedUsers ?? JsonSerializer.Deserialize<List<string>>(this[nameof(_importedUsers)]);
        set
        {
            _importedUsers = value;
            this[nameof(_importedUsers)] = JsonSerializer.Serialize(value);
        }
    }

    private string _logName;
    public string LogName
    {
        get => _logName ?? this[nameof(_logName)];
        set
        {
            _logName = value;
            this[nameof(_logName)] = value;
        }
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
            var onlyParse = _migrationApiInfo == null;
            var copyInfo = _migrationApiInfo.Clone();
            if (onlyParse)
            {
                MigrationApiInfo = new MigrationApiInfo();
            }
            CustomSynchronizationContext.CreateContext();

            await tenantManager.SetCurrentTenantAsync(TenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);
            migrator = migrationCore.GetMigrator(_migratorName);
            migrator.OnProgressUpdateAsync = Migrator_OnProgressUpdateAsync;

            if (migrator == null)
            {
                throw new ItemNotFoundException(MigrationResource.MigrationNotFoundException);
            }

            var folder = await cache.GetStringAsync($"migration folder - {TenantId}");
            await migrator.InitAsync(folder, CancellationToken, onlyParse ? OperationType.Parse : OperationType.Migration);

            await migrator.ParseAsync(onlyParse);
            if (!onlyParse)
            {
                await migrator.MigrateAsync(copyInfo);
            }
        }
        catch (Exception e)
        {
            Exception = e;
            logger.ErrorWithException(e);
            if (migrator != null && migrator.MigrationInfo != null)
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
            if (migrator != null && migrator.MigrationInfo != null)
            {
                MigrationApiInfo = migrator.MigrationInfo.ToApiInfo();
            }
            await PublishChanges();
        }
    }

    public async Task CopyLogsAsync(Stream stream)
    {
        try
        {
            await _semaphore.WaitAsync();
            await using var logger = serviceProvider.GetService<MigrationLogger>();
            logger.Init(LogName);
            await (await logger.GetStreamAsync()).CopyToAsync(stream);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}