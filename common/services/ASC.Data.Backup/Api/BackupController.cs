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

using ASC.Api.Core.Convention;
using ASC.Common.Threading.DistributedLock.Abstractions;
using ASC.Core.Billing;
using ASC.Core.Common;
using ASC.Core.Tenants;
using ASC.Data.Backup.Core.Quota;
using ASC.Data.Backup.Services;
using ASC.Data.Storage;
using ASC.Web.Core.PublicResources;

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
    TenantManager tenantManager,
    AuthContext authContext,
    CoreBaseSettings coreBaseSettings,
    TenantExtra tenantExtra,
    IEventBus eventBus,
    CommonLinkUtility commonLinkUtility,
    CoreSettings coreSettings,
    BackupService backupService,
    IDistributedLockProvider distributedLockProvider,
    CountFreeBackupChecker freeBackupsChecker)
    : ControllerBase
{
    private Guid CurrentUserId => authContext.CurrentAccount.ID;

    /// <summary>
    /// Returns the backup schedule of the current portal.
    /// </summary>
    /// <short>Get the backup schedule</short>
    /// <path>api/2.0/backup/getbackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Backup schedule", typeof(ScheduleDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("getbackupschedule")]
    public async Task<ScheduleDto> GetBackupSchedule(DumpDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        return await backupService.GetScheduleAsync(dto.Dump);
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
    public async Task<bool> CreateBackupSchedule(BackupScheduleDto inDto)
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
            Day = inDto.CronParams.Day ?? 0
        };
        if(backupStored is > 30 or < 1)
        {
            throw new ArgumentException("backupStored must be 1 - 30");
        }

        if (storageType is BackupStorageType.Documents or BackupStorageType.ThridpartyDocuments)
        {

            if (int.TryParse(storageParams["folderId"], out var fId))
            {
                await backupService.CheckAccessToFolderAsync(fId);
            }
            else
            {
                await backupService.CheckAccessToFolderAsync(storageParams["folderId"]);
            }
        }
        await backupService.CreateScheduleAsync(storageType, storageParams, backupStored, cron, inDto.Dump);
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
    public async Task<bool> DeleteBackupSchedule(DumpDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        await backupService.DeleteScheduleAsync(dto.Dump);

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
    public async Task<BackupProgress> StartBackup(BackupDto inDto, [FromServices] TenantQuotaController quotaController)
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
            if (storageType is BackupStorageType.Documents)
            {
                quotaController.Init(tenantManager.GetCurrentTenantId());
                await quotaController.QuotaUsedCheckAsync(0, authContext.CurrentAccount.ID);
            }

            if (int.TryParse(storageParams["folderId"], out var fId))
            {
                await backupService.CheckAccessToFolderAsync(fId);
            }
            else
            {
                await backupService.CheckAccessToFolderAsync(storageParams["folderId"]);
            }
        }
        if (storageType is BackupStorageType.ThirdPartyConsumer)
        {
            storageParams.TryAdd("subdir", "backup");
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        IDistributedLockHandle lockHandle = null;
        Session billingSession = null;

        try
        {
            lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetFreeBackupsCountCheckKey(tenantId));

            try
            {
                await freeBackupsChecker.CheckAppend();
            }
            catch (TenantQuotaException)
            {
                var backupServiceEnabled = await backupService.IsBackupServiceEnabledAsync(tenantId);
                if (!backupServiceEnabled)
                {
                    throw;
                }

                billingSession = await backupService.OpenCustomerSessionForBackupAsync(tenantId);
                if (billingSession == null)
                {
                    throw new BillingException(Resource.ErrorNotAllowedOption);
                }
            }

            var serverBaseUri = coreBaseSettings.Standalone && await coreSettings.GetSettingAsync("BaseDomain") == null
                ? commonLinkUtility.GetFullAbsolutePath("")
                : null;

            var taskId = await backupService.StartBackupAsync(storageType, storageParams, serverBaseUri, inDto.Dump, false);

            await eventBus.PublishAsync(new BackupRequestIntegrationEvent(
                 tenantId: tenantId,
                 storageParams: storageParams,
                 storageType: storageType,
                 createBy: CurrentUserId,
                 dump: inDto.Dump,
                 taskId: taskId,
                 serverBaseUri: serverBaseUri,
                 billingSessionId: billingSession?.SessionId ?? 0,
                 billingSessionExpire: billingSession?.Expire ?? default
            ));

            return await backupService.GetBackupProgressAsync(inDto.Dump);

        }
        catch (Exception ex) when (ex is AccountingPaymentRequiredException || ex is AccountingCustomerNotFoundException)
        {
            throw new BillingException(Resource.ErrorPaymentRequired);
        }
        catch (Exception)
        {
            if (billingSession != null)
            {
                await backupService.CloseCustomerSessionForBackupAsync(tenantId, billingSession.SessionId);
            }

            throw;
        }
        finally
        {
            if (lockHandle != null)
            {
                await lockHandle.ReleaseAsync();
            }
        }
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
    public async Task<BackupProgress> GetBackupProgress(DumpDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        return await backupService.GetBackupProgressAsync(dto.Dump);
    }

    /// <summary>
    /// Returns the history of the started backup.
    /// </summary>
    /// <short>Get the backup history</short>
    /// <path>api/2.0/backup/getbackuphistory</path>
    /// <collection>list</collection>
    [Tags("Backup")]
    [SwaggerResponse(200, "List of backup history records", typeof(List<BackupHistoryRecord>))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("getbackuphistory")]
    public async Task<List<BackupHistoryRecord>> GetBackupHistory(DumpDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        return await backupService.GetBackupHistoryAsync(dto.Dump);
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
        await backupService.DeleteBackupAsync(inDto.BackupId);
        return true;
    }

    /// <summary>
    /// Deletes the backup history from the current portal.
    /// </summary>
    /// <short>Delete the backup history</short>
    /// <path>api/2.0/backup/deletebackuphistory</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpDelete("deletebackuphistory")]
    public async Task<bool> DeleteBackupHistory(DumpDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        await backupService.DeleteAllBackupsAsync(dto.Dump);
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
    public async Task<BackupProgress> StartBackupRestore(BackupRestoreDto inDto)
    {
        if (inDto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        await backupService.DemandPermissionsRestoreAsync();

        var storageParams = inDto.StorageParams == null ? new Dictionary<string, string>() : inDto.StorageParams.ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());

        var serverBaseUri = coreBaseSettings.Standalone && await coreSettings.GetSettingAsync("BaseDomain") == null
            ? commonLinkUtility.GetFullAbsolutePath("")
            : null;
        
        var tenantId = tenantManager.GetCurrentTenantId();

        var storageType = inDto.StorageType ?? BackupStorageType.Documents;
        if (storageType is BackupStorageType.Documents or BackupStorageType.ThridpartyDocuments && storageParams.ContainsKey("filePath"))
        {
            if (int.TryParse(storageParams["filePath"], out var fId))
            {
                await backupService.CheckAccessToFileAsync(fId);
            }
            else
            {
                await backupService.CheckAccessToFileAsync(storageParams["filePath"]);
            }
        }
        if (storageType is BackupStorageType.ThirdPartyConsumer)
        {
            storageParams.TryAdd("subdir", "backup");
        }

        var taskId = await backupService.StartRestoreAsync(inDto.BackupId, storageType, storageParams, inDto.Notify, serverBaseUri, inDto.Dump, false);
        await eventBus.PublishAsync(new BackupRestoreRequestIntegrationEvent(
                             tenantId: tenantId,
                             createBy: CurrentUserId,
                             storageParams: storageParams,
                             storageType: storageType,
                             notify: inDto.Notify,
                             backupId: inDto.BackupId,
                             dump: inDto.Dump,
                             serverBaseUri: serverBaseUri,
                             taskId: taskId
                        ));


        return await backupService.GetRestoreProgressAsync(inDto.Dump);
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
    public async Task<BackupProgress> GetRestoreProgress(RestoreDto dto)
    {
        return await backupService.GetRestoreProgressAsync(dto.Dump);
    }

    /// <summary>
    /// Returns a path to the temporary folder with the stored backup.
    /// </summary>
    /// <short>Get the temporary backup folder</short>
    /// <path>api/2.0/backup/backuptmp</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Backup")]
    [HttpGet("backuptmp")]
    [SwaggerResponse(200, "Path to the temporary folder with the stored backup", typeof(object))]
    public object GetTempPath()
    {
        return backupService.GetTmpFolder();
    }

    /// <summary>
    /// Returns the number of backups for a period of time. The default is one month.
    /// </summary>
    /// <short>Get the number of backups</short>
    /// <path>api/2.0/backup/getbackupscount</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Number of backups", typeof(int))]
    [AllowNotPayment]
    [HttpGet("getbackupscount")]
    public async Task<int> GetBackupsCountAsync(BackupsCountDto dto)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var to = dto.To ?? DateTime.UtcNow.AddSeconds(1);
        var from = dto.From ?? to.AddMonths(-1);

        if (from > to)
        {
            throw new ArgumentException("From date must be less than to date");
        }

        var result = await backupService.GetBackupsCountAsync(tenantId, dto.Paid, from, to);
        return result;
    }

    /// <summary>
    /// Returns the backup service state.
    /// </summary>
    /// <short>Get the backup service state</short>
    /// <path>api/2.0/backup/getservicestate</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Backup service state", typeof(BackupServiceStateDto))]
    [AllowNotPayment]
    [HttpGet("getservicestate")]
    public async Task<BackupServiceStateDto> GetBackupsServiceStateAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var backupServiceEnabled = await backupService.IsBackupServiceEnabledAsync(tenantId);

        return new BackupServiceStateDto { Enabled = backupServiceEnabled };
    }
}
