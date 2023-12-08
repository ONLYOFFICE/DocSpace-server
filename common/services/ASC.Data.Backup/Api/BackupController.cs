// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Data.Backup.Controllers;

/// <summary>
/// Backup API.
/// </summary>
/// <name>backup</name>
[Scope]
[DefaultRoute]
[ApiController]
public class BackupController(
        BackupAjaxHandler backupAjaxHandler,
        TenantManager tenantManager,
        SecurityContext securityContext,
        CoreBaseSettings coreBaseSettings,
        TenantExtra tenantExtra,
        IEventBus eventBus)
    : ControllerBase
    {
    private readonly Guid _currentUserId = securityContext.CurrentAccount.ID;
    private readonly int _tenantId = tenantManager.GetCurrentTenant().Id;

    /// <summary>
    /// Returns the backup schedule of the current portal.
    /// </summary>
    /// <short>Get the backup schedule</short>
    /// <returns type="ASC.Data.Backup.BackupAjaxHandler.Schedule, ASC.Data.Backup">Backup schedule</returns>
    /// <httpMethod>GET</httpMethod>
    /// <path>api/2.0/backup/getbackupschedule</path>
    [HttpGet("getbackupschedule")]
    public async Task<BackupAjaxHandler.Schedule> GetBackupSchedule()
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        return await backupAjaxHandler.GetScheduleAsync();
    }

    /// <summary>
    /// Creates the backup schedule of the current portal with the parameters specified in the request.
    /// </summary>
    /// <short>Create the backup schedule</short>
    /// <param type="ASC.Data.Backup.ApiModels.BackupScheduleDto, ASC.Data.Backup" name="inDto">Backup schedule parameters</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <httpMethod>POST</httpMethod>
    /// <path>api/2.0/backup/createbackupschedule</path>
    [HttpPost("createbackupschedule")]
    public async Task<bool> CreateBackupScheduleAsync(BackupScheduleDto inDto)
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        var storageType = inDto.StorageType == null ? BackupStorageType.Documents : (BackupStorageType)Int32.Parse(inDto.StorageType);
        var storageParams = inDto.StorageParams == null ? new Dictionary<string, string>() : inDto.StorageParams.ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());
        var backupStored = inDto.BackupsStored == null ? 0 : Int32.Parse(inDto.BackupsStored);
        var cron = new CronParams
        {
            Period = inDto.CronParams.Period == null ? BackupPeriod.EveryDay : (BackupPeriod)Int32.Parse(inDto.CronParams.Period),
            Hour = inDto.CronParams.Hour == null ? 0 : Int32.Parse(inDto.CronParams.Hour),
            Day = inDto.CronParams.Day == null ? 0 : Int32.Parse(inDto.CronParams.Day)
        };

        if (storageType == BackupStorageType.Documents)
        {

            if (int.TryParse(storageParams["folderId"], out var fId))
            {
                await backupAjaxHandler.CheckAccessToFolderAsync(fId);
            }
            else
            {
                await backupAjaxHandler.CheckAccessToFolderAsync(storageParams["folderId"]);
            }
        }
        await backupAjaxHandler.CreateScheduleAsync(storageType, storageParams, backupStored, cron, inDto.Dump);
        return true;
    }

    /// <summary>
    /// Deletes the backup schedule of the current portal.
    /// </summary>
    /// <short>Delete the backup schedule</short>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <httpMethod>DELETE</httpMethod>
    /// <path>api/2.0/backup/deletebackupschedule</path>
    [HttpDelete("deletebackupschedule")]
    public async Task<bool> DeleteBackupSchedule()
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        await backupAjaxHandler.DeleteScheduleAsync();

        return true;
    }

    /// <summary>
    /// Starts the backup of the current portal with the parameters specified in the request.
    /// </summary>
    /// <short>Start the backup</short>
    /// <param type="ASC.Data.Backup.ApiModels.BackupDto, ASC.Data.Backup" name="inDto">Backup parameters</param>
    /// <returns type="System.Object, System">Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link</returns>
    /// <httpMethod>POST</httpMethod>
    /// <path>api/2.0/backup/startbackup</path>
    [AllowNotPayment]
    [HttpPost("startbackup")]
    public async Task<BackupProgress> StartBackupAsync(BackupDto inDto)
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        var storageType = inDto.StorageType == null ? BackupStorageType.Documents : (BackupStorageType)Int32.Parse(inDto.StorageType);
        var storageParams = inDto.StorageParams == null ? new Dictionary<string, string>() : inDto.StorageParams.ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());

        if (!coreBaseSettings.Standalone && inDto.Dump)
        {
            throw new ArgumentException("backup can not start as dump");
        }

        if (storageType == BackupStorageType.Documents)
        {

            if (int.TryParse(storageParams["folderId"], out var fId))
            {
                await backupAjaxHandler.CheckAccessToFolderAsync(fId);
            }
            else
            {
                await backupAjaxHandler.CheckAccessToFolderAsync(storageParams["folderId"]);
            }
        }

        var taskId = await backupAjaxHandler.StartBackupAsync(storageType, storageParams, inDto.Dump, false);

        eventBus.Publish(new BackupRequestIntegrationEvent(
             tenantId: _tenantId,
             storageParams: storageParams,
             storageType: storageType,
             createBy: _currentUserId,
             dump: inDto.Dump,
             taskId: taskId
        ));

        return await backupAjaxHandler.GetBackupProgressAsync();
    }

    /// <summary>
    /// Returns the progress of the started backup.
    /// </summary>
    /// <short>Get the backup progress</short>
    /// <returns type="System.Object, System">Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link</returns>
    /// <httpMethod>GET</httpMethod>
    /// <path>api/2.0/backup/getbackupprogress</path>
    [AllowNotPayment]
    [HttpGet("getbackupprogress")]
    public async Task<BackupProgress> GetBackupProgressAsync()
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        return await backupAjaxHandler.GetBackupProgressAsync();
    }

    /// <summary>
    /// Returns the history of the started backup.
    /// </summary>
    /// <short>Get the backup history</short>
    /// <returns type="ASC.Data.Backup.Contracts.BackupHistoryRecord, ASC.Data.Backup.Core">List of backup history records</returns>
    /// <httpMethod>GET</httpMethod>
    /// <path>api/2.0/backup/getbackuphistory</path>
    /// <collection>list</collection>
    [HttpGet("getbackuphistory")]
    public async Task<List<BackupHistoryRecord>> GetBackupHistory()
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        return await backupAjaxHandler.GetBackupHistory();
    }

    /// <summary>
    /// Deletes the backup with the ID specified in the request.
    /// </summary>
    /// <short>Delete the backup</short>
    /// <param type="System.Guid, System" method="url" name="id">Backup ID</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <httpMethod>DELETE</httpMethod>
    /// <path>api/2.0/backup/deletebackup/{id}</path>
    [HttpDelete("deletebackup/{id}")]
    public async Task<bool> DeleteBackup(Guid id)
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        await backupAjaxHandler.DeleteBackupAsync(id);
        return true;
    }

    /// <summary>
    /// Deletes the backup history of the current portal.
    /// </summary>
    /// <short>Delete the backup history</short>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <httpMethod>DELETE</httpMethod>
    /// <path>api/2.0/backup/deletebackuphistory</path>
    [HttpDelete("deletebackuphistory")]
    public async Task<bool> DeleteBackupHistory()
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        await backupAjaxHandler.DeleteAllBackupsAsync();
        return true;
    }

    /// <summary>
    /// Starts the data restoring process of the current portal with the parameters specified in the request.
    /// </summary>
    /// <short>Start the restoring process</short>
    /// <param type="ASC.Data.Backup.ApiModels.BackupRestoreDto, ASC.Data.Backup" name="inDto">Restoring parameters</param>
    /// <returns type="System.Object, System">Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link</returns>
    /// <httpMethod>POST</httpMethod>
    /// <path>api/2.0/backup/startrestore</path>
    [HttpPost("startrestore")]
    public async Task<BackupProgress> StartBackupRestoreAsync(BackupRestoreDto inDto)
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        var storageParams = inDto.StorageParams == null ? new Dictionary<string, string>() : inDto.StorageParams.ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());

        eventBus.Publish(new BackupRestoreRequestIntegrationEvent(
                             tenantId: _tenantId,
                             createBy: _currentUserId,
                             storageParams: storageParams,
                             storageType: (BackupStorageType)Int32.Parse(inDto.StorageType.ToString()),
                             notify: inDto.Notify,
                             backupId: inDto.BackupId
                        ));


        return await backupAjaxHandler.GetRestoreProgressAsync();
    }

    /// <summary>
    /// Returns the progress of the started restoring process.
    /// </summary>
    /// <short>Get the restoring progress</short>
    /// <returns type="System.Object, System">Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link</returns>
    /// <httpMethod>GET</httpMethod>
    /// <path>api/2.0/backup/getrestoreprogress</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [HttpGet("getrestoreprogress")]  //NOTE: this method doesn't check payment!!!
    [AllowAnonymous]
    [AllowNotPayment]
    public async Task<BackupProgress> GetRestoreProgressAsync()
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        return await backupAjaxHandler.GetRestoreProgressAsync();
    }

    /// <summary>
    /// Returns a path to the temporary folder with the stored backup.
    /// </summary>
    /// <short>Get the temporary backup folder</short>
    /// <returns type="System.Object, System">Path to the temporary folder with the stored backup</returns>
    /// <httpMethod>GET</httpMethod>
    /// <path>api/2.0/backup/backuptmp</path>
    ///<visible>false</visible>
    [HttpGet("backuptmp")]
    public async Task<object> GetTempPath()
    {
        if (coreBaseSettings.Standalone)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        return backupAjaxHandler.GetTmpFolder();
    }
}
