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

using ASC.Core.Configuration;

namespace ASC.Data.Backup.Services;

[Singleton]
public sealed class BackupSchedulerService(
    ILogger<BackupSchedulerService> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    CoreBaseSettings coreBaseSettings,
    IEventBus eventBus)
     : ActivePassiveBackgroundService<BackupSchedulerService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = configuration.GetSection("backup").Get<BackupSettings>().Scheduler.Period;

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        using var serviceScope = _scopeFactory.CreateScope();

        var tariffService = serviceScope.ServiceProvider.GetRequiredService<ITariffService>();
        var backupRepository = serviceScope.ServiceProvider.GetRequiredService<BackupRepository>();
        var backupSchedule = serviceScope.ServiceProvider.GetRequiredService<Schedule>();
        var tenantManager = serviceScope.ServiceProvider.GetRequiredService<TenantManager>();

        logger.DebugStartedToSchedule();

        var backupsToSchedule = await (await backupRepository.GetBackupSchedulesAsync())
            .ToAsyncEnumerable()
            .WhereAwait(async schedule => await backupSchedule.IsToBeProcessedAsync(schedule))
            .ToListAsync(cancellationToken: stoppingToken);

        logger.DebugBackupsSchedule(backupsToSchedule.Count);

        foreach (var schedule in backupsToSchedule)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                if (coreBaseSettings.Standalone || (await tenantManager.GetTenantQuotaAsync(schedule.TenantId)).AutoBackupRestore)
                {
                    var tariff = await tariffService.GetTariffAsync(schedule.TenantId);

                    if (tariff.State < TariffState.Delay)
                    {
                        schedule.LastBackupTime = DateTime.UtcNow;

                        await backupRepository.SaveBackupScheduleAsync(schedule);

                        logger.DebugStartScheduledBackup(schedule.TenantId, schedule.StorageType, schedule.StorageBasePath);

                        _eventBus.Publish(new BackupRequestIntegrationEvent(
                                                 tenantId: schedule.TenantId,
                                                 storageBasePath: schedule.StorageBasePath,
                                                 storageParams: JsonConvert.DeserializeObject<Dictionary<string, string>>(schedule.StorageParams),
                                                 storageType: schedule.StorageType,
                                                 createBy: Constants.CoreSystem.ID,
                                                 isScheduled: true,
                                                 backupsStored: schedule.BackupsStored
                                          ));
                    }
                    else
                    {
                        logger.DebugNotPaid(schedule.TenantId);
                    }
                }
                else
                {
                    logger.DebugHaveNotAccess(schedule.TenantId);
                }
            }
            catch (Exception error)
            {
                logger.ErrorBackups(error);
            }
        }
    }
}
