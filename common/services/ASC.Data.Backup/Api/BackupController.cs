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

using ASC.Common.Threading.DistributedLock.Abstractions;
using ASC.Core.Billing;
using ASC.Core.Common;
using ASC.Core.Tenants;
using ASC.Data.Backup.Core.Quota;
using ASC.Data.Backup.Services;
using ASC.Data.Storage;
using ASC.MessagingSystem;
using ASC.MessagingSystem.Core;
using ASC.MessagingSystem.EF.Model;
using ASC.Web.Core.PublicResources;

using Swashbuckle.AspNetCore.Annotations;

namespace ASC.Data.Backup.Controllers;

/// <remarks>
/// Backup API.
/// </remarks>
/// <name>backup</name>
[Scope]
[ApiEndpoint("backup")]
public class BackupController(
    TenantManager tenantManager,
    AuthContext authContext,
    CoreBaseSettings coreBaseSettings,
    TenantExtra tenantExtra,
    IEventBus eventBus,
    CommonLinkUtility commonLinkUtility,
    CoreSettings coreSettings,
    BackupService backupService,
    MessageService messageService,
    IDistributedLockProvider distributedLockProvider,
    IHttpContextAccessor httpContextAccessor,
    CountFreeBackupChecker freeBackupsChecker)
    : ControllerBase
{
    private Guid CurrentUserId => authContext.CurrentAccount.ID;

    /// <remarks>
    /// Returns the backup schedule of the current portal.
    /// </remarks>
    /// <summary>Get the backup schedule</summary>
    /// <path>api/2.0/backup/getbackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Backup schedule", typeof(ScheduleDto))]
    [SwaggerResponse(403, "Access denied")]
    [HttpGet("getbackupschedule")]
    public async Task<ScheduleDto> GetBackupSchedule(DumpDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        return await backupService.GetScheduleAsync(dto.Dump);
    }

    /// <remarks>
    /// Creates the backup schedule of the current portal with the parameters specified in the request.
    /// </remarks>
    /// <summary>Create the backup schedule</summary>
    /// <path>api/2.0/backup/createbackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(400, "BackupStored must be 1 - 30 or backup can not start as dump")]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(403, "Access denied")]
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
        if (backupStored is > 30 or < 1)
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

    /// <remarks>
    /// Deletes the backup schedule of the current portal.
    /// </remarks>
    /// <summary>Delete the backup schedule</summary>
    /// <path>api/2.0/backup/deletebackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(403, "Access denied")]
    [HttpDelete("deletebackupschedule")]
    public async Task<bool> DeleteBackupSchedule(DumpDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        var tenantId = dto.Dump ? Tenant.DefaultTenant : tenantManager.GetCurrentTenantId();

        await backupService.DeleteScheduleAsync(tenantId);

        await messageService.SendAsync(MessageAction.ScheduledBackupDeleted, MessageTarget.Create(tenantId));

        return true;
    }

    /// <remarks>
    /// Queues a backup of the current portal and returns straight away: the archive itself is written by the
    /// separate backup worker service, which picks the job up from an integration event, so the response
    /// reports a progress of 0 and the `Created` status, and its `taskId` is the handle to poll with
    /// `GET api/2.0/backup/getbackupprogress`. The caller needs the portal settings permission, and
    /// `dump` - a backup of the whole server instead of this one portal - additionally requires the space
    /// access permission and is rejected outside a standalone installation.
    /// The keys expected in `storageParams` depend on `storageType`: `Documents` takes an integer `folderId`,
    /// `ThridpartyDocuments` takes a provider-specific non-integer `folderId`, `Local` takes `filePath` and
    /// works on a standalone installation only, `ThirdPartyConsumer` takes `module` together with the settings
    /// of that consumer, and `DataStore` takes no keys at all; the `subdir` key is added by the operation
    /// itself and must not be sent.
    /// A portal that has already used up the free backups of the current calendar month is charged through the
    /// paid backup service instead, and the call is rejected with 402 when that service is not available to it.
    /// </remarks>
    /// <summary>Start the backup</summary>
    /// <path>api/2.0/backup/startbackup</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The state of the queued backup job", typeof(BackupProgress))]
    [SwaggerResponse(400, "The folder ID does not match the storage type, or a dump was requested on a portal that is not a standalone installation")]
    [SwaggerResponse(402, "The free backups of the current month are used up and the paid backup service is not available to this portal")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "The target folder or the backup quota was not found")]
    [AllowNotPayment]
    [HttpPost("startbackup")]
    public async Task<BackupProgress> StartBackup(BackupDto inDto, [FromServices] TenantQuotaController quotaController)
    {
        await backupService.DemandPermissionsBackupAsync();

        if (inDto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        var storageType = inDto.StorageType ?? BackupStorageType.Documents;
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
                billingSession = await backupService.OpenCustomerSessionForBackupAsync(tenantId);
                if (billingSession == null)
                {
                    throw new BillingException(Resource.ErrorNotAllowedOption);
                }

                await backupService.EnsureBackupServiceEnabledAsync(tenantId);
            }

            var serverBaseUri = coreBaseSettings.Standalone && await coreSettings.GetSettingAsync("BaseDomain") == null
                ? commonLinkUtility.GetFullAbsolutePath("")
                : null;

            var taskId = await backupService.StartBackupAsync(storageType, storageParams, serverBaseUri, inDto.Dump, false);

            var headers = MessageSettings.GetHttpHeaders(httpContextAccessor?.HttpContext?.Request)
                .ToDictionary(x => x.Key, x => x.Value.ToString());

            await eventBus.PublishAsync(new BackupRequestIntegrationEvent(
                 tenantId: tenantId,
                 storageParams: storageParams,
                 storageType: storageType,
                 createBy: CurrentUserId,
                 dump: inDto.Dump,
                 taskId: taskId,
                 serverBaseUri: serverBaseUri,
                 billingSessionId: billingSession?.SessionId ?? 0,
                 billingSessionExpire: billingSession?.Expire ?? default,
                 headers: headers
            ));

            return await backupService.GetBackupProgressAsync(inDto.Dump);

        }
        catch (Exception ex) when (ex is AccountingPaymentRequiredException or AccountingCustomerNotFoundException)
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

    /// <remarks>
    /// Drops the backup job of the current portal from the queue, which cancels it if it is still running.
    /// The caller needs the portal settings permission. It answers false, not an error, when there is nothing
    /// to cancel, so the result says whether a job was actually dropped rather than whether the call
    /// succeeded.
    /// This affects backup jobs only: a restoring job cannot be cancelled through the API. The cancelled job
    /// leaves the queue, so a following `GET api/2.0/backup/getbackupprogress` reports no job at all rather
    /// than a job with the `Canceled` status.
    /// </remarks>
    /// <summary>Cancel the running backup</summary>
    /// <path>api/2.0/backup/cancelbackup</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "True if a backup job was dropped from the queue, false if there was nothing to cancel", typeof(bool))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [AllowNotPayment]
    [HttpPost("cancelbackup")]
    public async Task<bool> CancelBackupAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var result = await backupService.CancelBackupAsync(tenantId);

        if (result)
        {
            await messageService.SendAsync(MessageAction.BackupCancelled, MessageTarget.Create(tenantId));
        }

        return result;
    }

    // /// <remarks>
    // /// Cancel current restore.(Only from saas backup)
    // /// </remarks>
    // /// <summary>Cancel current restore</summary>
    // /// <path>api/2.0/backup/cancelrestore</path>
    // [Tags("Backup")]
    // [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    // [SwaggerResponse(402, "Your pricing plan does not support this option")]
    // [AllowNotPayment]
    // [HttpPost("cancelrestore")]
    // public async Task<bool> CancelRestoreAsync()
    // {
    //     var tenantId = tenantManager.GetCurrentTenantId();
    //
    //     var result = await backupService.CancelRestoreAsync(tenantId);
    //
    //     if (result)
    //     {
    //         await messageService.SendAsync(MessageAction.RestoreCancelled, MessageTarget.Create(tenantId));
    //     }
    //
    //     return result;
    // }

    /// <remarks>
    /// Reports the state of the backup job of the current portal, and is the operation to poll after
    /// `POST api/2.0/backup/startbackup`. The queue holds one job per portal, so no job ID is passed in;
    /// `dump` asks for the state of the server-wide job instead and requires the space access permission.
    /// When there is no such job - none was ever started, or the finished one has already been dropped from
    /// the queue - the call still answers 200, but the body carries no `response` member at all, so a client
    /// has to treat the payload as optional rather than expect an empty object.
    /// While the job runs, `isCompleted` is false, `error` and `link` are empty strings and `progress` grows
    /// from 0 to 100. Once it stops, `isCompleted` turns true and `status` says how it ended: a non-empty
    /// `error` is the only report of a failure, `warning` is set when the archive was written but some files
    /// could not be read or when the job was cancelled, and `link` becomes the download link to the stored
    /// archive.
    /// </remarks>
    /// <summary>Get the backup progress</summary>
    /// <path>api/2.0/backup/getbackupprogress</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The state of the backup job, or an empty payload when there is no such job", typeof(BackupProgress))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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

    /// <remarks>
    /// Returns the history of the started backup.
    /// </remarks>
    /// <summary>Get the backup history</summary>
    /// <path>api/2.0/backup/getbackuphistory</path>
    /// <collection>list</collection>
    [Tags("Backup")]
    [SwaggerResponse(200, "List of backup history records", typeof(List<BackupHistoryRecord>))]
    [SwaggerResponse(403, "Access denied")]
    [HttpGet("getbackuphistory")]
    public async Task<List<BackupHistoryRecord>> GetBackupHistory(DumpDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        return await backupService.GetBackupHistoryAsync(dto.Dump);
    }

    /// <remarks>
    /// Deletes the backup with the ID specified in the request.
    /// </remarks>
    /// <summary>Delete the backup</summary>
    /// <path>api/2.0/backup/deletebackup/{id}</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(403, "Access denied")]
    [HttpDelete("deletebackup/{id:guid}")]
    public async Task<bool> DeleteBackup([FromRoute] DeleteBackupDto inDto)
    {
        await backupService.DeleteBackupAsync(inDto.BackupId);
        return true;
    }

    /// <remarks>
    /// Deletes the backup history from the current portal.
    /// </remarks>
    /// <summary>Delete the backup history</summary>
    /// <path>api/2.0/backup/deletebackuphistory</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(403, "Access denied")]
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

    /// <remarks>
    /// Queues the restoring of the current portal from a backup and returns straight away: the work itself is
    /// done by the separate backup worker service, which picks the job up from an integration event, so the
    /// response reports a progress of 0 and the `Created` status, and the returned `taskId` is the handle to
    /// poll with `GET api/2.0/backup/getrestoreprogress` - the one operation of this service that stays
    /// reachable while the portal is being restored, because every other one answers 403 in that state.
    /// The source is given either by `backupId`, which is the ID of a record from
    /// `GET api/2.0/backup/getbackuphistory`, or, when `backupId` is not a GUID, by the `filePath` key of
    /// `storageParams` together with the matching `storageType`; an all-zero GUID is parsed as a GUID and
    /// therefore reaches neither branch.
    /// The caller needs the portal settings permission, restoring has to be allowed by the pricing plan of a
    /// portal that is not a standalone installation, and `dump` - restoring the whole server rather than this
    /// one portal - additionally requires the space access permission.
    /// </remarks>
    /// <summary>Start the restoring process</summary>
    /// <path>api/2.0/backup/startrestore</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The state of the queued restoring job", typeof(BackupProgress))]
    [SwaggerResponse(402, "The pricing plan of this portal does not allow restoring")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "The backup record was not found, or the file it points to is missing")]
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

        messageService.Send(MessageAction.RestoreStarted, MessageTarget.Create(tenantId), inDto.Dump ? "dump" : string.Empty);

        return await backupService.GetRestoreProgressAsync(inDto.Dump);
    }

    /// <remarks>
    /// Reports the state of the restoring job, and is the operation to poll after
    /// `POST api/2.0/backup/startrestore`. It is the only operation of this service that needs no
    /// authorization and the only one that stays reachable while the portal is being restored, which is
    /// exactly the state a client polls it in - every other operation of the service answers 403 then.
    /// `dump` is read as three states rather than as a flag: omit it to get whichever restoring job concerns
    /// this portal, including a server-wide one, pass false to get the job of this portal only, and pass true
    /// to get the server-wide job; on a portal that is not a standalone installation the value is forced to
    /// false. When there is no matching job the call still answers 200, but the body carries no `response`
    /// member at all.
    /// `isCompleted` is the field to poll, a non-empty `error` is the only report of a failure, and neither
    /// `link` nor `warning` is ever filled in for a restoring job.
    /// </remarks>
    /// <summary>Get the restoring progress</summary>
    /// <path>api/2.0/backup/getrestoreprogress</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Backup")]
    [SwaggerResponse(200, "The state of the restoring job, or an empty payload when there is no such job", typeof(BackupProgress))]
    [HttpGet("getrestoreprogress")]  //NOTE: this method doesn't check payment!!!
    [AllowAnonymous]
    [AllowNotPayment]
    public async Task<BackupProgress> GetRestoreProgress(RestoreDto dto)
    {
        return await backupService.GetRestoreProgressAsync(dto.Dump);
    }

    /// <remarks>
    /// Returns a path to the temporary folder with the stored backup.
    /// </remarks>
    /// <summary>Get the temporary backup folder</summary>
    /// <path>api/2.0/backup/backuptmp</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Backup")]
    [HttpGet("backuptmp")]
    [SwaggerResponse(200, "Path to the temporary folder with the stored backup", typeof(object))]
    public object GetTempPath()
    {
        return backupService.GetTmpFolder();
    }

    /// <remarks>
    /// Returns the number of backups for a period of time. The default is the current calendar month.
    /// </remarks>
    /// <summary>Get the number of backups</summary>
    /// <path>api/2.0/backup/getbackupscount</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Number of backups", typeof(int))]
    [SwaggerResponse(400, "From date must be less than to date")]
    [SwaggerResponse(403, "Access denied")]
    [AllowNotPayment]
    [HttpGet("getbackupscount")]
    public async Task<int> GetBackupsCountAsync(BackupsCountDto dto)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var (defaultFrom, defaultTo) = BackupPeriodHelper.GetCurrentMonthRange();
        var to = dto.To ?? defaultTo;
        var from = dto.From ?? defaultFrom;

        if (from > to)
        {
            throw new ArgumentException("From date must be less than to date");
        }

        var result = await backupService.GetBackupsCountAsync(tenantId, dto.Paid, from, to);
        return result;
    }

    /// <remarks>
    /// Returns the number of free and paid backups for a period of time. The default is the current calendar month.
    /// </remarks>
    /// <summary>Get the number of free and paid backups</summary>
    /// <path>api/2.0/backup/getbackupscountbypaid</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Number of free and paid backups", typeof(BackupsCountResultDto))]
    [SwaggerResponse(400, "From date must be less than to date")]
    [SwaggerResponse(403, "Access denied")]
    [AllowNotPayment]
    [HttpGet("getbackupscountbypaid")]
    public async Task<BackupsCountResultDto> GetBackupsCountsAsync(BackupsCountDto dto)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var (defaultFrom, defaultTo) = BackupPeriodHelper.GetCurrentMonthRange();
        var to = dto.To ?? defaultTo;
        var from = dto.From ?? defaultFrom;

        if (from > to)
        {
            throw new ArgumentException("From date must be less than to date");
        }

        var (free, paid) = await backupService.GetBackupsCountAsync(tenantId, from, to);

        return new BackupsCountResultDto { Free = free, Paid = paid };
    }

    /// <remarks>
    /// Returns the backup service state.
    /// </remarks>
    /// <summary>Get the backup service state</summary>
    /// <path>api/2.0/backup/getservicestate</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Backup service state", typeof(BackupServiceStateDto))]
    [SwaggerResponse(403, "Access denied")]
    [AllowNotPayment]
    [HttpGet("getservicestate")]
    public async Task<BackupServiceStateDto> GetBackupsServiceStateAsync()
    {
        await backupService.DemandPermissionsBackupAsync();

        var tenantId = tenantManager.GetCurrentTenantId();

        var backupServiceEnabled = await backupService.IsBackupServiceEnabledAsync(tenantId);

        return new BackupServiceStateDto { Enabled = backupServiceEnabled };
    }
}
