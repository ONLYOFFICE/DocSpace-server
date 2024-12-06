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

using ASC.Api.Core.Convention;

using Swashbuckle.AspNetCore.Annotations;

namespace ASC.Data.Backup.Controllers;

/// <summary>
/// Backup API.
/// </summary>
/// <name>backup</name>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("backup")]
public class BackupController(
    BackupAjaxHandler backupAjaxHandler,
    TenantManager tenantManager,
    AuthContext authContext,
    CoreBaseSettings coreBaseSettings,
    TenantExtra tenantExtra,
    IEventBus eventBus,
    CommonLinkUtility commonLinkUtility,
    CoreSettings coreSettings)
    : ControllerBase
{
    private Guid CurrentUserId => authContext.CurrentAccount.ID;

    /// <summary>
    /// Returns the backup schedule of the current portal.
    /// </summary>
    /// <short>Get the backup schedule</short>
    /// <path>api/2.0/backup/getbackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Backup schedule", typeof(BackupAjaxHandler.Schedule))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("getbackupschedule")]
    public async Task<BackupAjaxHandler.Schedule> GetBackupSchedule()
    {
        return await backupAjaxHandler.GetScheduleAsync();
    }

    /// <summary>
    /// Creates the backup schedule of the current portal with the parameters specified in the request.
    /// </summary>
    /// <short>Create the backup schedule</short>
    /// <path>api/2.0/backup/createbackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(400, "BackupStored must be 1 - 30 or backup can not start as dump")]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpPost("createbackupschedule")]
    public async Task<bool> CreateBackupScheduleAsync(BackupScheduleDto inDto)
    {
        if (inDto.Dump) 
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        var storageType = inDto.StorageType ?? BackupStorageType.Documents;
        var storageParams = inDto.StorageParams == null ? new Dictionary<string, string>() : inDto.StorageParams.ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());
        var backupStored = inDto.BackupsStored ?? 1;
        var cron = new CronParams
        {
            Period = inDto.CronParams.Period ?? BackupPeriod.EveryDay,
            Hour = inDto.CronParams.Hour,
            Day = inDto.CronParams.Day.HasValue ? inDto.CronParams.Day.Value : 0
        };
        if(backupStored is > 30 or < 1)
        {
            throw new ArgumentException("backupStored must be 1 - 30");
        }

        if (storageType is BackupStorageType.Documents or BackupStorageType.ThridpartyDocuments)
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
    /// <path>api/2.0/backup/deletebackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpDelete("deletebackupschedule")]
    public async Task<bool> DeleteBackupSchedule()
    {
        await backupAjaxHandler.DeleteScheduleAsync();

        return true;
    }

    /// <summary>
    /// Starts the backup of the current portal with the parameters specified in the request.
    /// </summary>
    /// <short>Start the backup</short>
    /// <path>api/2.0/backup/startbackup</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link", typeof(BackupProgress))]
    [SwaggerResponse(400, "Wrong folder type or backup can`t start as dump")]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "The required folder was not found")]
    [AllowNotPayment]
    [HttpPost("startbackup")]
    public async Task<BackupProgress> StartBackupAsync(BackupDto inDto)
    {
        if (inDto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        var storageType =  inDto.StorageType ?? BackupStorageType.Documents;
        var storageParams = inDto.StorageParams == null ? new Dictionary<string, string>() : inDto.StorageParams.ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());

        var canParse = false;
        if (storageParams.TryGetValue("folderId", out var param))
        {
            canParse = int.TryParse(param, out _);
        }
        if (storageType == BackupStorageType.Documents && !canParse
            || storageType == BackupStorageType.ThridpartyDocuments && canParse)
        {
            throw new ArgumentException("wrong folder type");
        }

        if (!coreBaseSettings.Standalone && inDto.Dump)
        {
            throw new ArgumentException("backup can`t start as dump");
        }

        if (storageType is BackupStorageType.Documents or BackupStorageType.ThridpartyDocuments)
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
        if (storageType is BackupStorageType.ThirdPartyConsumer && !storageParams.ContainsKey("subdir"))
        {
            storageParams.Add("subdir", "backup");
        }
        

        var serverBaseUri = coreBaseSettings.Standalone && await coreSettings.GetSettingAsync("BaseDomain") == null
            ? commonLinkUtility.GetFullAbsolutePath("")
            : default;
        
        var taskId = await backupAjaxHandler.StartBackupAsync(storageType, storageParams, serverBaseUri, inDto.Dump, false);
        var tenantId = tenantManager.GetCurrentTenantId();
        
        await eventBus.PublishAsync(new BackupRequestIntegrationEvent(
             tenantId: tenantId,
             storageParams: storageParams,
             storageType: storageType,
             createBy: CurrentUserId,
             dump: inDto.Dump,
             taskId: taskId,
             serverBaseUri: serverBaseUri
        ));

        return await backupAjaxHandler.GetBackupProgressAsync();
    }

    /// <summary>
    /// Returns the progress of the started backup.
    /// </summary>
    /// <short>Get the backup progress</short>
    /// <path>api/2.0/backup/getbackupprogress</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link", typeof(BackupProgress))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [AllowNotPayment]
    [HttpGet("getbackupprogress")]
    public async Task<BackupProgress> GetBackupProgressAsync()
    {
        return await backupAjaxHandler.GetBackupProgressAsync();
    }

    /// <summary>
    /// Returns the history of the started backup.
    /// </summary>
    /// <short>Get the backup history</short>
    /// <path>api/2.0/backup/getbackuphistory</path>
    /// <collection>list</collection>
    [Tags("Backup")]
    [SwaggerResponse(200, "List of backup history records", typeof(BackupHistoryRecord))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("getbackuphistory")]
    public async Task<List<BackupHistoryRecord>> GetBackupHistory()
    {
        return await backupAjaxHandler.GetBackupHistory();
    }

    /// <summary>
    /// Deletes the backup with the ID specified in the request.
    /// </summary>
    /// <short>Delete the backup</short>
    /// <path>api/2.0/backup/deletebackup/{id}</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpDelete("deletebackup/{id:guid}")]
    public async Task<bool> DeleteBackup([FromRoute] DeleteBackupDto inDto)
    {
        await backupAjaxHandler.DeleteBackupAsync(inDto.BackupId);
        return true;
    }

    /// <summary>
    /// Deletes the backup history of the current portal.
    /// </summary>
    /// <short>Delete the backup history</short>
    /// <path>api/2.0/backup/deletebackuphistory</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful")]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpDelete("deletebackuphistory")]
    public async Task<bool> DeleteBackupHistory()
    {
        await backupAjaxHandler.DeleteAllBackupsAsync();
        return true;
    }

    /// <summary>
    /// Starts the data restoring process of the current portal with the parameters specified in the request.
    /// </summary>
    /// <short>Start the restoring process</short>
    /// <path>api/2.0/backup/startrestore</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link", typeof(BackupProgress))]
    [SwaggerResponse(400, "Backup can not start as dump")]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "The required file or folder was not found")]
    [HttpPost("startrestore")]
    public async Task<BackupProgress> StartBackupRestoreAsync(BackupRestoreDto inDto)
    {
        await backupAjaxHandler.DemandPermissionsRestoreAsync();

        var storageParams = inDto.StorageParams == null ? new Dictionary<string, string>() : inDto.StorageParams.ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());

        var serverBaseUri = coreBaseSettings.Standalone && await coreSettings.GetSettingAsync("BaseDomain") == null
            ? commonLinkUtility.GetFullAbsolutePath("")
            : default;
        
        var tenantId = tenantManager.GetCurrentTenantId();

        var storageType = inDto.StorageType ?? BackupStorageType.Documents;
        if (storageType is BackupStorageType.Documents or BackupStorageType.ThridpartyDocuments && storageParams.ContainsKey("filePath"))
        {
            if (int.TryParse(storageParams["filePath"], out var fId))
            {
                await backupAjaxHandler.CheckAccessToFileAsync(fId);
            }
            else
            {
                await backupAjaxHandler.CheckAccessToFileAsync(storageParams["filePath"]);
            }
        }
        if (storageType is BackupStorageType.ThirdPartyConsumer && !storageParams.ContainsKey("subdir"))
        {
            storageParams.Add("subdir", "backup");
        }

        await eventBus.PublishAsync(new BackupRestoreRequestIntegrationEvent(
                             tenantId: tenantId,
                             createBy: CurrentUserId,
                             storageParams: storageParams,
                             storageType: storageType,
                             notify: inDto.Notify,
                             backupId: inDto.BackupId,
                             serverBaseUri: serverBaseUri
                        ));


        return await backupAjaxHandler.GetRestoreProgressAsync();
    }

    /// <summary>
    /// Returns the progress of the started restoring process.
    /// </summary>
    /// <short>Get the restoring progress</short>
    /// <path>api/2.0/backup/getrestoreprogress</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Backup")]
    [SwaggerResponse(200, "Backup progress: completed or not, progress percentage, error, tenant ID, backup progress item (Backup, Restore, Transfer), link", typeof(BackupProgress))]
    [HttpGet("getrestoreprogress")]  //NOTE: this method doesn't check payment!!!
    [AllowAnonymous]
    [AllowNotPayment]
    public async Task<BackupProgress> GetRestoreProgressAsync()
    {
        return await backupAjaxHandler.GetRestoreProgressAsync();
    }

    /// <summary>
    /// Returns a path to the temporary folder with the stored backup.
    /// </summary>
    /// <short>Get the temporary backup folder</short>
    /// <path>api/2.0/backup/backuptmp</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Backup")]
    [HttpGet("backuptmp")]
    [SwaggerResponse(200, "Path to the temporary folder with the stored backup")]
    public object GetTempPath()
    {
        return backupAjaxHandler.GetTmpFolder();
    }
}
