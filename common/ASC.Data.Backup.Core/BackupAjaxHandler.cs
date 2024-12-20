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

using System.Security;

using ASC.Files.Core.Security;

namespace ASC.Data.Backup;

[Scope]
public class BackupAjaxHandler(
    BackupService backupService,
    TenantManager tenantManager,
    MessageService messageService,
    CoreBaseSettings coreBaseSettings,
    CoreConfiguration coreConfiguration,
    PermissionContext permissionContext,
    AuthContext authContext,
    UserManager userManager,
    ConsumerFactory consumerFactory,
    StorageFactory storageFactory,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity)
{
    private const string BackupTempModule = "backup_temp";
    private const string BackupFileName = "backup";

    #region Backup

    public async Task<string> StartBackupAsync(BackupStorageType storageType, Dictionary<string, string> storageParams, string serverBaseUri, bool dump, bool enqueueTask = true, string taskId = null)
    {
        await DemandPermissionsBackupAsync();

        if (!coreBaseSettings.Standalone && dump)
        {
            throw new ArgumentException("backup can not start as dump");
        }

        var backupRequest = new StartBackupRequest
        {
            TenantId = GetCurrentTenantIdAsync(),
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

        return await backupService.StartBackupAsync(backupRequest, enqueueTask, taskId);
    }

    public async Task<BackupProgress> GetBackupProgressAsync()
    {
        await DemandPermissionsBackupAsync();

        return await backupService.GetBackupProgress(GetCurrentTenantIdAsync());
    }
    
    public async Task StopBackupAsync()
    {
        await DemandPermissionsBackupAsync();

        await backupService.StopBackupAsync(await GetCurrentTenantIdAsync());
    }

    public async Task<BackupProgress> GetBackupProgressAsync(int tenantId)
    {
        await DemandPermissionsBackupAsync();

        return await backupService.GetBackupProgress(tenantId);
    }

    public async Task DeleteBackupAsync(Guid id)
    {
        await DemandPermissionsBackupAsync();

        await backupService.DeleteBackupAsync(id);
    }

    public async Task DeleteAllBackupsAsync()
    {
        await DemandPermissionsBackupAsync();

        await backupService.DeleteAllBackupsAsync(GetCurrentTenantIdAsync());
    }

    public async Task<List<BackupHistoryRecord>> GetBackupHistory()
    {
        await DemandPermissionsBackupAsync();

        return await backupService.GetBackupHistoryAsync(GetCurrentTenantIdAsync());
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

        if (folder.FolderType == FolderType.VirtualRooms || folder.FolderType == FolderType.Archive || !await fileSecurity.CanCreateAsync(folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }
    }

    public async Task CheckAccessToFolderAsync<T>(T folderId)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);

        if (folder == null)
        {
            throw new DirectoryNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (folder.FolderType == FolderType.VirtualRooms || folder.FolderType == FolderType.Archive || !await fileSecurity.CanCreateAsync(folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }
    }

    public async Task CreateScheduleAsync(BackupStorageType storageType, Dictionary<string, string> storageParams, int backupsStored, CronParams cronParams, bool dump)
    {
        await DemandPermissionsBackupAsync();
        await DemandPermissionsAutoBackupAsync();

        if (!SetupInfo.IsVisibleSettings("AutoBackup"))
        {
            throw new InvalidOperationException(Resource.ErrorNotAllowedOption);
        }

        if(!coreBaseSettings.Standalone && dump)
        {
            throw new ArgumentException("backup can not start as dump");
        }

        ValidateCronSettings(cronParams);

        var scheduleRequest = new CreateScheduleRequest
        {
            TenantId = tenantManager.GetCurrentTenantId(),
            Cron = cronParams.ToString(),
            NumberOfBackupsStored = backupsStored,
            StorageType = storageType,
            StorageParams = storageParams,
            Dump = dump
        };

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

        await backupService.CreateScheduleAsync(scheduleRequest);
    }

    public async Task<Schedule> GetScheduleAsync()
    {
        await DemandPermissionsBackupAsync();

        var response = await backupService.GetScheduleAsync(GetCurrentTenantIdAsync());
        if (response == null)
        {
            return null;
        }

        var schedule = new Schedule
        {
            StorageType = response.StorageType,
            StorageParams = response.StorageParams ?? new Dictionary<string, string>(),
            CronParams = new CronParams(response.Cron),
            BackupsStored = response.NumberOfBackupsStored.NullIfDefault(),
            LastBackupTime = response.LastBackupTime,
            Dump = response.Dump
        };

        if (response.StorageType == BackupStorageType.CustomCloud)
        {
            var amazonSettings = await coreConfiguration.GetSectionAsync<AmazonS3Settings>();

            var consumer = consumerFactory.GetByKey<DataStoreConsumer>("s3");
            if (!await consumer.GetIsSetAsync())
            {
                await consumer.SetAsync("acesskey", amazonSettings.AccessKeyId);
                await consumer.SetAsync("secretaccesskey", amazonSettings.SecretAccessKey);

                await consumer.SetAsync("bucket", amazonSettings.Bucket);
                await consumer.SetAsync("region", amazonSettings.Region);
            }

            schedule.StorageType = BackupStorageType.ThirdPartyConsumer;
            schedule.StorageParams = new();
            foreach (var r in consumer.AdditionalKeys)
            {
                schedule.StorageParams.Add(r, await consumer.GetAsync(r));
            }
            schedule.StorageParams.Add("module", "S3");

            var scheduleRequest = new CreateScheduleRequest
            {
                TenantId = tenantManager.GetCurrentTenantId(),
                Cron = schedule.CronParams.ToString(),
                NumberOfBackupsStored = schedule.BackupsStored ?? 0,
                StorageType = schedule.StorageType,
                StorageParams = schedule.StorageParams,
                Dump = schedule.Dump
            };

            await backupService.CreateScheduleAsync(scheduleRequest);

        }
        else if (response.StorageType != BackupStorageType.ThirdPartyConsumer)
        {
            schedule.StorageParams["folderId"] = response.StorageBasePath;
        }

        return schedule;
    }

    public async Task DeleteScheduleAsync()
    {
        await DemandPermissionsBackupAsync();

        await backupService.DeleteScheduleAsync(GetCurrentTenantIdAsync());
    }

    private async Task DemandPermissionsBackupAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!coreBaseSettings.Standalone && !SetupInfo.IsVisibleSettings(nameof(ManagementType.Backup)))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "Backup");
        }
    }

    #endregion

    #region restore

    public async Task StartRestoreAsync(string backupId, BackupStorageType storageType, Dictionary<string, string> storageParams, bool notify, string serverBaseUri)
    {
        await DemandPermissionsRestoreAsync();
        var tenantId = GetCurrentTenantIdAsync();
        var restoreRequest = new StartRestoreRequest
        {
            TenantId = tenantId,
            NotifyAfterCompletion = notify,
            StorageParams = storageParams,
            ServerBaseUri = serverBaseUri
        };

        if (Guid.TryParse(backupId, out var guidBackupId))
        {
            restoreRequest.BackupId = guidBackupId;
        }
        else
        {
            restoreRequest.StorageType = storageType;
            restoreRequest.FilePathOrId = storageParams["filePath"];

            if (restoreRequest.StorageType == BackupStorageType.Local)
            {
                var path = await GetTmpFilePathAsync(tenantId);
                path = File.Exists(path + ".tar.gz") ? path + ".tar.gz" : path + ".tar";
                restoreRequest.FilePathOrId = path;
            }
        }

        await backupService.StartRestoreAsync(restoreRequest);
    }

    public async Task<BackupProgress> GetRestoreProgressAsync()
    {
        var tenant = tenantManager.GetCurrentTenant();

        return await backupService.GetRestoreProgress(tenant.Id);
    }

    public async Task DemandPermissionsRestoreAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var quota = await tenantManager.GetTenantQuotaAsync(tenantManager.GetCurrentTenantId());
        if (!SetupInfo.IsVisibleSettings("Restore") || (!coreBaseSettings.Standalone && !quota.AutoBackupRestore))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "Restore");
        }

        if (!coreBaseSettings.Standalone && (!SetupInfo.IsVisibleSettings("Restore") || !quota.AutoBackupRestore))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "Restore");
        }
    }

    private async Task DemandPermissionsAutoBackupAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!SetupInfo.IsVisibleSettings("AutoBackup") || !(await tenantManager.GetTenantQuotaAsync(tenantManager.GetCurrentTenantId())).AutoBackupRestore)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "AutoBackup");
        }
    }

    #endregion

    #region transfer

    public async Task StartTransferAsync(string targetRegion, bool notifyUsers)
    {
        await DemandPermissionsTransferAsync();

        messageService.Send(MessageAction.StartTransferSetting);
        await backupService.StartTransferAsync(
            new StartTransferRequest
            {
                TenantId = GetCurrentTenantIdAsync(),
                TargetRegion = targetRegion,
                NotifyUsers = notifyUsers
            });

    }

    public async Task<BackupProgress> GetTransferProgressAsync()
    {
        return await backupService.GetTransferProgress(GetCurrentTenantIdAsync());
    }

    private async Task DemandPermissionsTransferAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        if (!SetupInfo.IsVisibleSettings(nameof(ManagementType.Migration))
        || !currentUser.IsOwner(tenantManager.GetCurrentTenant())
        || !SetupInfo.IsSecretEmail(currentUser.Email) && !(await tenantManager.GetCurrentTenantQuotaAsync()).AutoBackupRestore)
        {
            throw new InvalidOperationException(Resource.ErrorNotAllowedOption);
        }
    }

    #endregion

    public string GetTmpFolder()
    {
        return backupService.GetTmpFolder();
    }

    private static void ValidateCronSettings(CronParams cronParams)
    {
        new CronExpression(cronParams.ToString());
    }

    private int GetCurrentTenantIdAsync()
    {
        return tenantManager.GetCurrentTenantId();
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

    public class Schedule
    {
        /// <summary>
        /// Storage type
        /// </summary>
        public BackupStorageType StorageType { get; set; }

        /// <summary>
        /// Storage parameters
        /// </summary>
        public Dictionary<string, string> StorageParams { get; set; }

        /// <summary>
        /// Cron parameters
        /// </summary>
        public CronParams CronParams { get; init; }

        /// <summary>
        /// Maximum number of the stored backup copies
        /// </summary>
        public int? BackupsStored { get; init; }

        /// <summary>
        /// Last backup creation time
        /// </summary>
        public DateTime LastBackupTime { get; set; }

        /// <summary>
        /// Dump
        /// </summary>
        [SwaggerSchemaCustom(Example = false)]
        public bool Dump { get; set; }
    }

    public class CronParams
    {
        /// <summary>
        /// Period
        /// </summary>
        public BackupPeriod Period { get; init; }

        /// <summary>
        /// Hour
        /// </summary>
        public int Hour { get; init; }

        /// <summary>
        /// Day
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

    public enum BackupPeriod
    {
        [SwaggerEnum(Description = "Every day")]
        EveryDay = 0,

        [SwaggerEnum(Description = "Every week")]
        EveryWeek = 1,

        [SwaggerEnum(Description = "Every month")]
        EveryMonth = 2
    }
}
