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

using System.Security;
using System.Text.Json;

using ASC.Common.Web;
using ASC.Core.Common.Settings;
using ASC.Files.Core.Security;

namespace ASC.Data.Backup.Services;

[Scope]
public class BackupService(
        ILogger<BackupService> logger,
        BackupStorageFactory backupStorageFactory,
        BackupWorker backupWorker,
        BackupRepository backupRepository,
        TenantExtra tenantExtra,
        ITariffService tariffService,
        TenantManager tenantManager,
        UserManager userManager,
        SettingsManager settingsManager,
        MessageService messageService,
        CoreBaseSettings coreBaseSettings,
        AuthContext authContext,
        PermissionContext permissionContext,
        IDaoFactory daoFactory,
        FileSecurity fileSecurity,
        StorageFactory storageFactory)
    {
    private const string BackupTempModule = "backup_temp";
    private const string BackupFileName = "backup";
    private const int BackupCustomerSessionDuration = 86400; // 60 * 60 * 24;

    public async Task<string> StartBackupAsync(BackupStorageType storageType, Dictionary<string, string> storageParams, string serverBaseUri, bool dump, bool enqueueTask = true, string taskId = null, int billingSessionId = 0, DateTime billingSessionExpire = default)
    {
        await DemandPermissionsBackupAsync();

        if (!coreBaseSettings.Standalone && dump)
        {
            throw new ArgumentException("backup can not start as dump");
        }

        var backupRequest = new StartBackupRequest
        {
            TenantId = tenantManager.GetCurrentTenantId(),
            UserId = authContext.CurrentAccount.ID,
            StorageType = storageType,
            StorageParams = storageParams,
            Dump = dump,
            ServerBaseUri = serverBaseUri
        };

        switch (storageType)
        {
            case BackupStorageType.ThridpartyDocuments:
            case BackupStorageType.Documents:
                backupRequest.StorageBasePath = storageParams["folderId"];
                break;
            case BackupStorageType.Local:
                if (!coreBaseSettings.Standalone)
                {
                    throw new Exception("Access denied");
                }

                backupRequest.StorageBasePath = storageParams["filePath"];
                break;
        }

        messageService.Send(MessageAction.StartBackupSetting);

        var progress = await backupWorker.StartBackupAsync(backupRequest, enqueueTask, taskId, billingSessionId, billingSessionExpire);
        if (!string.IsNullOrEmpty(progress.Error))
        {
            throw new FaultException();
        }
        return progress.TaskId;
    }

    public async Task CheckAccessToFolderAsync<T>(T folderId)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);

        if (folder == null)
        {
            throw new DirectoryNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (folder.FolderType == FolderType.VirtualRooms || folder.FolderType == FolderType.RoomTemplates || folder.FolderType == FolderType.Archive || !await fileSecurity.CanCreateAsync(folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }
    }

    public async Task DeleteBackupAsync(Guid backupId)
    {
        await DemandPermissionsBackupAsync();

        var backupRecord = await backupRepository.GetBackupRecordAsync(backupId);
        if (backupRecord.TenantId == -1)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        else
        {
            if (backupRecord.TenantId != tenantManager.GetCurrentTenantId())
            {
                return;
            }
        }
        await backupRepository.DeleteBackupRecordAsync(backupRecord.Id);

        var storage = await backupStorageFactory.GetBackupStorageAsync(backupRecord);
        if (storage == null)
        {
            return;
        }

        await storage.DeleteAsync(backupRecord.StoragePath);
    }

    public async Task DeleteAllBackupsAsync(bool dump)
    {
        await DemandPermissionsBackupAsync();

        var tenantId = dump ? -1 : tenantManager.GetCurrentTenantId();

        foreach (var backupRecord in await backupRepository.GetBackupRecordsByTenantIdAsync(tenantId))
        {
            try
            {
                await backupRepository.DeleteBackupRecordAsync(backupRecord.Id);
                var storage = await backupStorageFactory.GetBackupStorageAsync(backupRecord);
                if (storage == null)
                {
                    continue;
                }

                await storage.DeleteAsync(backupRecord.StoragePath);
            }
            catch (Exception error)
            {
                logger.WarningErrorWhileBackupRecord(error);
            }
        }
    }

    public async Task CheckAccessToFileAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new DirectoryNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(file.ParentId);

        if (folder == null)
        {
            throw new DirectoryNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (folder.FolderType == FolderType.VirtualRooms || folder.FolderType == FolderType.RoomTemplates || folder.FolderType == FolderType.Archive || !await fileSecurity.CanCreateAsync(folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }
    }

    public async Task<List<BackupHistoryRecord>> GetBackupHistoryAsync(bool dump)
    {
        await DemandPermissionsBackupAsync();

        var tenantId = dump ? -1 : tenantManager.GetCurrentTenantId();

        var backupHistory = new List<BackupHistoryRecord>();
        foreach (var record in await backupRepository.GetBackupRecordsByTenantIdAsync(tenantId))
        {
            var storage = await backupStorageFactory.GetBackupStorageAsync(record);
            if (storage == null)
            {
                continue;
            }

            if (await storage.IsExistsAsync(record.StoragePath))
            {
                backupHistory.Add(new BackupHistoryRecord
                {
                    Id = record.Id,
                    FileName = record.Name,
                    StorageType = record.StorageType,
                    CreatedOn = record.CreatedOn,
                    ExpiresOn = record.ExpiresOn
                });
            }
            else
            {
                await backupRepository.DeleteBackupRecordAsync(record.Id);
            }
        }
        return backupHistory;
    }

    public async Task StartTransferAsync(StartTransferRequest request)
    {
        var progress = await backupWorker.StartTransferAsync(request.TenantId, request.TargetRegion, request.NotifyUsers);
        if (!string.IsNullOrEmpty(progress.Error))
        {
            throw new FaultException();
        }
    }

    public async Task<string> StartRestoreAsync(string backupId,
        BackupStorageType storageType,
        Dictionary<string, string> storageParams,
        bool notify,
        string serverBaseUri,
        bool dump,
        bool enqueueTask = true,
        string taskId = null)
    {
        await DemandPermissionsRestoreAsync();
        var tenantId = tenantManager.GetCurrentTenantId();
        var request = new StartRestoreRequest
        {
            TenantId = tenantId,
            NotifyAfterCompletion = notify,
            StorageParams = storageParams,
            ServerBaseUri = serverBaseUri,
            Dump = dump
        };

        if (Guid.TryParse(backupId, out var guidBackupId))
        {
            request.BackupId = guidBackupId;
        }
        else
        {
            request.StorageType = storageType;
            request.FilePathOrId = storageParams["filePath"];

            if (request.StorageType == BackupStorageType.Local && enqueueTask)
            {
                var path = await GetTmpFilePathAsync(tenantId);
                path = File.Exists(path + ".tar.gz") ? path + ".tar.gz" : path + ".tar";
                request.FilePathOrId = path;
            }
        }

        if (request.StorageType == BackupStorageType.Local && (string.IsNullOrEmpty(request.FilePathOrId) || !File.Exists(request.FilePathOrId)) && enqueueTask)
        {
            throw new FileNotFoundException();
        }

        if (!request.BackupId.Equals(Guid.Empty))
        {
            var backupRecord = await backupRepository.GetBackupRecordAsync(request.BackupId);
            if (backupRecord == null)
            {
                throw new FileNotFoundException();
            }

            request.FilePathOrId = backupRecord.StoragePath;
            request.StorageType = backupRecord.StorageType;
            request.StorageParams = JsonSerializer.Deserialize<Dictionary<string, string>>(backupRecord.StorageParams);
        }

        var progress = await backupWorker.StartRestoreAsync(request, enqueueTask, taskId);
        if (!string.IsNullOrEmpty(progress.Error))
        {
            throw new FaultException();
        }
        return progress.TaskId;
    }

    public async Task<BackupProgress> GetBackupProgressAsync(bool dump)
    {
        await DemandPermissionsBackupAsync();

        if (dump)
        {
            return await GetDumpBackupProgress();
        }
        else
        {
            return await GetBackupProgressAsync(tenantManager.GetCurrentTenantId());
        }
    }

    public async Task<BackupProgress> GetBackupProgressAsync(int tenantId)
    {
        await DemandPermissionsBackupAsync();

        return await backupWorker.GetBackupProgressAsync(tenantId);
    }

    public async Task<BackupProgress> GetDumpBackupProgress()
    {
        return await backupWorker.GetDumpBackupProgressAsync();
    }

    public async Task<BackupProgress> GetTransferProgress(int tenantId)
    {
        return await backupWorker.GetTransferProgressAsync(tenantId);
    }

    public async Task<BackupProgress> GetRestoreProgressAsync(bool? dump)
    {
        if (!coreBaseSettings.Standalone)
        {
            dump = false;
        }

        if (dump.HasValue)
        {
            if (dump.Value)
            {
                return await backupWorker.GetDumpRestoreProgressAsync();
            }
            else
            {
                var tenantId = tenantManager.GetCurrentTenantId();
                return await backupWorker.GetRestoreProgressAsync(tenantId);
            }
        }
        else
        {
            var tenantId = tenantManager.GetCurrentTenantId();
            return await backupWorker.GetAnyRestoreProgressAsync(tenantId);
        }
    }

    public string GetTmpFolder()
    {
        return backupWorker.TempFolder;
    }

    public async Task CreateScheduleAsync(BackupStorageType storageType, Dictionary<string, string> storageParams, int backupsStored, CronParams cronParams, bool dump)
    {
        await DemandPermissionsBackupAsync();
        await DemandPermissionsAutoBackupAsync();

        if (!coreBaseSettings.Standalone && dump)
        {
            throw new ArgumentException("backup can not start as dump");
        }

        ValidateCronSettings(cronParams);

        var scheduleRequest = new CreateScheduleRequest
        {
            TenantId = dump ? -1 : tenantManager.GetCurrentTenantId(),
            Cron = cronParams.ToString(),
            NumberOfBackupsStored = backupsStored,
            StorageType = storageType,
            StorageParams = storageParams,
            Dump = dump
        };

        if (dump)
        {
            scheduleRequest.StorageParams.Add("tenantId", tenantManager.GetCurrentTenantId().ToString());
        }

        switch (storageType)
        {
            case BackupStorageType.ThridpartyDocuments:
            case BackupStorageType.Documents:
                scheduleRequest.StorageBasePath = storageParams["folderId"];
                break;
            case BackupStorageType.Local:
                if (!coreBaseSettings.Standalone)
                {
                    throw new Exception("Access denied");
                }

                scheduleRequest.StorageBasePath = storageParams["filePath"];
                break;
        }

        await backupRepository.SaveBackupScheduleAsync(
            new BackupSchedule
            {
                TenantId = scheduleRequest.TenantId,
                Cron = scheduleRequest.Cron,
                BackupsStored = scheduleRequest.NumberOfBackupsStored,
                StorageType = scheduleRequest.StorageType,
                StorageBasePath = scheduleRequest.StorageBasePath,
                StorageParams = JsonSerializer.Serialize(scheduleRequest.StorageParams),
                Dump = scheduleRequest.Dump
            });
    }

    public async Task DeleteScheduleAsync(bool dump)
    {
        await DemandPermissionsBackupAsync();

        var tenantId = dump ? -1 : tenantManager.GetCurrentTenantId();
        await backupRepository.DeleteBackupScheduleAsync(tenantId);
    }

    public async Task DeleteScheduleAsync(int tenantId)
    {
        await DemandPermissionsBackupAsync();

        await backupRepository.DeleteBackupScheduleAsync(tenantId);
    }

    public async Task<string> GetTmpFilePathAsync(int tenantId)
    {
        var discStore = await storageFactory.GetStorageAsync(tenantManager.GetCurrentTenantId(), BackupTempModule, (IQuotaController)null) as DiscDataStore;
        var folder = discStore.GetPhysicalPath("", "");

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        return Path.Combine(folder, $"{tenantId}-{BackupFileName}");
    }

    public async Task<ScheduleDto> GetScheduleAsync(bool? dump)
    {
        await DemandPermissionsBackupAsync();
        ScheduleResponse response = null;
        if (dump.HasValue && dump.Value)
        {
            response = await InnerGetScheduleAsync(-1, dump);
        }
        else
        {
            response = await InnerGetScheduleAsync(tenantManager.GetCurrentTenantId(), dump);
        }
        if (response == null)
        {
            return null;
        }

        var schedule = new ScheduleDto
        {
            StorageType = response.StorageType,
            StorageParams = response.StorageParams ?? new Dictionary<string, string>(),
            CronParams = new CronParams(response.Cron),
            BackupsStored = response.NumberOfBackupsStored.NullIfDefault(),
            LastBackupTime = response.LastBackupTime,
            Dump = response.Dump
        };

        if (response.StorageType != BackupStorageType.ThirdPartyConsumer)
        {
            schedule.StorageParams["folderId"] = response.StorageBasePath;
        }

        return schedule;
    }

    public async Task<Session> OpenCustomerSessionForBackupAsync(int tenantId, bool checkPayer = true)
    {
        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId);
        if (customerInfo == null)
        {
            return null;
        }

        if (checkPayer)
        {
            var payer = await userManager.GetUserByEmailAsync(customerInfo?.Email);
            if (authContext.CurrentAccount.ID != payer.Id)
            {
                throw new SecurityException($"payerEmail {customerInfo?.Email}, payerId {payer.Id}, currentId {authContext.CurrentAccount.ID}");
            }
        }

        var serviceAccount = await GetBackupServiceAccountId();
        var externalRef = Guid.NewGuid().ToString();

        var result = await tariffService.OpenCustomerSessionAsync(tenantId, serviceAccount, externalRef, 1, BackupCustomerSessionDuration);

        return result;
    }

    public async Task<bool> CloseCustomerSessionForBackupAsync(int tenantId, int sessionId)
    {
        if (sessionId <= 0 || !tariffService.IsConfigured())
        {
            return false;
        }

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId);
        if (customerInfo == null)
        {
            return false;
        }

        var result = await tariffService.CloseCustomerSessionAsync(tenantId, sessionId);

        return result;
    }

    public async Task<Session> ExtendCustomerSessionForBackupAsync(int tenantId, int sessionId)
    {
        if (sessionId <= 0 || !tariffService.IsConfigured())
        {
            return null;
        }

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId);
        if (customerInfo == null)
        {
            return null;
        }

        var result = await tariffService.ExtendCustomerSessionAsync(tenantId, sessionId, BackupCustomerSessionDuration);

        return result;
    }

    public async Task<bool> CompleteCustomerSessionForBackupAsync(int tenantId, int sessionId, string customerParticipantName)
    {
        if (sessionId <= 0 || !tariffService.IsConfigured())
        {
            return false;
        }

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId);
        if (customerInfo == null)
        {
            return false;
        }

        var serviceAccount = await GetBackupServiceAccountId();

        var result = await tariffService.CompleteCustomerSessionAsync(tenantId, serviceAccount, sessionId, 1, customerParticipantName);

        if (result)
        {
            messageService.Send(MessageAction.CustomerOperationPerformed);
        }

        return result;
    }

    public async Task<int> GetBackupsCountAsync(int tenantId, bool paid, DateTime from, DateTime to)
    {
        return await backupRepository.GetBackupsCountAsync(tenantId, paid, from, to);
    }

    public async Task<bool> IsBackupServiceEnabledAsync(int tenantId)
    {
        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>(tenantId);
        return settings.EnabledServices != null && settings.EnabledServices.Contains(TenantWalletService.Backup);
    }

    private async Task<ScheduleResponse> InnerGetScheduleAsync(int tenantId, bool? dump)
    {
        var schedule = await backupRepository.GetBackupScheduleAsync(tenantId, dump);
        if (schedule != null)
        {
            var tmp = new ScheduleResponse
            {
                StorageType = schedule.StorageType,
                StorageBasePath = schedule.StorageBasePath,
                NumberOfBackupsStored = schedule.BackupsStored,
                Cron = schedule.Cron,
                LastBackupTime = schedule.LastBackupTime,
                StorageParams = JsonSerializer.Deserialize<Dictionary<string, string>>(schedule.StorageParams),
                Dump = schedule.Dump
            };

            return tmp;
        }

        return null;
    }

    private static void ValidateCronSettings(CronParams cronParams)
    {
        new CronExpression(cronParams.ToString());
    }

    private async Task DemandPermissionsBackupAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!coreBaseSettings.Standalone && !SetupInfo.IsVisibleSettings(nameof(ManagementType.Backup)))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }

    private async Task DemandPermissionsAutoBackupAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!SetupInfo.IsVisibleSettings("AutoBackup"))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }

        if (coreBaseSettings.Standalone)
        {
            return;
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        var quota = await tenantManager.GetTenantQuotaAsync(tenantId);

        if (quota.CountFreeBackup == 0 && !await IsBackupServiceEnabledAsync(tenantId))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }

    public async Task DemandPermissionsRestoreAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var quota = await tenantManager.GetTenantQuotaAsync(tenantManager.GetCurrentTenantId());
        if (!SetupInfo.IsVisibleSettings("Restore") || (!coreBaseSettings.Standalone && !quota.Restore))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }

    private async Task<int> GetBackupServiceAccountId()
    {
        var quotaList = await tenantManager.GetTenantQuotasAsync(true, true);

        var backupQuota = quotaList.FirstOrDefault(x => x.TenantId == (int)TenantWalletService.Backup);

        return backupQuota == null ? throw new ItemNotFoundException("Backup quota not found") : int.Parse(backupQuota.ProductId);
    }
}

/// <summary>
/// The backup schedule parameters.
/// </summary>
public class ScheduleDto
{
    /// <summary>
    /// The backup storage type.
    /// </summary>
    public BackupStorageType StorageType { get; set; }

    /// <summary>
    /// The backup storage parameters.
    /// </summary>
    public Dictionary<string, string> StorageParams { get; set; }

    /// <summary>
    /// The backup cron parameters.
    /// </summary>
    public CronParams CronParams { get; init; }

    /// <summary>
    /// The maximum number of the stored backup copies.
    /// </summary>
    public int? BackupsStored { get; init; }

    /// <summary>
    /// The date and time when the last backup was reated.
    /// </summary>
    public DateTime LastBackupTime { get; set; }

    /// <summary>
    /// Specifies if a dump will be created or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool Dump { get; set; }
}

/// <summary>
/// The backup cron parameters.
/// </summary>
public class CronParams
{
    /// <summary>
    /// The backup period type.
    /// </summary>
    public BackupPeriod Period { get; init; }

    /// <summary>
    /// The time of the day to start the backup process.
    /// </summary>
    public int Hour { get; init; }

    /// <summary>
    /// The day of the week to start the backup process.
    /// </summary>
    public int Day { get; init; }

    public CronParams() { }

    public CronParams(string cronString)
    {
        var tokens = cronString.Split(' ');
        Hour = Convert.ToInt32(tokens[2]);
        if (tokens[3] != "?")
        {
            Period = BackupPeriod.EveryMonth;
            Day = Convert.ToInt32(tokens[3]);
        }
        else if (tokens[5] != "*")
        {
            Period = BackupPeriod.EveryWeek;
            Day = Convert.ToInt32(tokens[5]);
        }
        else
        {
            Period = BackupPeriod.EveryDay;
        }
    }

    public override string ToString()
    {
        return Period switch
        {
            BackupPeriod.EveryDay => string.Format("0 0 {0} ? * *", Hour),
            BackupPeriod.EveryMonth => string.Format("0 0 {0} {1} * ?", Hour, Day),
            BackupPeriod.EveryWeek => string.Format("0 0 {0} ? * {1}", Hour, Day),
            _ => base.ToString()
        };
    }
}

/// <summary>
/// The backup period type.
/// </summary>
public enum BackupPeriod
{
    [SwaggerEnum(Description = "Every day")]
    EveryDay = 0,

    [SwaggerEnum(Description = "Every week")]
    EveryWeek = 1,

    [SwaggerEnum(Description = "Every month")]
    EveryMonth = 2
}
