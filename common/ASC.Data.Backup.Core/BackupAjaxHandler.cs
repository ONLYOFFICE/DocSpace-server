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

using ASC.Files.Core.Security;

namespace ASC.Data.Backup;

[Scope]
public class BackupAjaxHandler(
    BackupService backupService,
    TenantManager tenantManager,
    MessageService messageService,
    CoreBaseSettings coreBaseSettings,
    PermissionContext permissionContext,
    AuthContext authContext,
    UserManager userManager,
    StorageFactory storageFactory,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity)
{
    private const string BackupTempModule = "backup_temp";
    private const string BackupFileName = "backup";

    #region Backup

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
            TenantId =  dump ? -1 : tenantManager.GetCurrentTenantId(),
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

        await backupService.CreateScheduleAsync(scheduleRequest);
    }

    public async Task DeleteScheduleAsync(bool dump)
    {
        await DemandPermissionsBackupAsync();

        if (dump)
        {
            await backupService.DeleteScheduleAsync(-1);
        }
        else 
        {
            await backupService.DeleteScheduleAsync(GetCurrentTenantIdAsync());
        }
    }

    private async Task DemandPermissionsBackupAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!coreBaseSettings.Standalone && !SetupInfo.IsVisibleSettings(nameof(ManagementType.Backup)))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }

    #endregion

    #region restore

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
        var tenantId = GetCurrentTenantIdAsync();
        var restoreRequest = new StartRestoreRequest
        {
            TenantId = tenantId,
            NotifyAfterCompletion = notify,
            StorageParams = storageParams,
            ServerBaseUri = serverBaseUri,
            Dump =  dump
        };

        if (Guid.TryParse(backupId, out var guidBackupId))
        {
            restoreRequest.BackupId = guidBackupId;
        }
        else
        {
            restoreRequest.StorageType = storageType;
            restoreRequest.FilePathOrId = storageParams["filePath"];

            if (restoreRequest.StorageType == BackupStorageType.Local && enqueueTask)
            {
                var path = await GetTmpFilePathAsync(tenantId);
                path = File.Exists(path + ".tar.gz") ? path + ".tar.gz" : path + ".tar";
                restoreRequest.FilePathOrId = path;
            }
        }

        return await backupService.StartRestoreAsync(restoreRequest, enqueueTask, taskId);
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
                return await backupService.GetDumpRestoreProgressAsync();
            }
            else
            {
                var tenant = tenantManager.GetCurrentTenant();
                return await backupService.GetRestoreProgressAsync(tenant.Id);
            }
        }
        else
        {
            var tenant = tenantManager.GetCurrentTenant();
            return await backupService.GetAnyRestoreProgressAsync(tenant.Id);
        }
    }

    public async Task DemandPermissionsRestoreAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var quota = await tenantManager.GetTenantQuotaAsync(tenantManager.GetCurrentTenantId());
        if (!SetupInfo.IsVisibleSettings("Restore") || (!coreBaseSettings.Standalone && !quota.AutoBackupRestore))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }

        if (!coreBaseSettings.Standalone && (!SetupInfo.IsVisibleSettings("Restore") || !quota.AutoBackupRestore))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }

    private async Task DemandPermissionsAutoBackupAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!SetupInfo.IsVisibleSettings("AutoBackup") || !(await tenantManager.GetTenantQuotaAsync(tenantManager.GetCurrentTenantId())).AutoBackupRestore)
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
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
}
