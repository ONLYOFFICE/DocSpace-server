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
/// Backs a portal up and restores it: backups can be started by hand or put on a schedule, they are listed
/// and deleted through a history, and the counters report how many of them the free monthly allowance has
/// covered. Every operation here needs the portal settings permission, and each one that accepts a `dump`
/// parameter switches from the current portal to the whole server, which additionally needs the space access
/// permission and works on a standalone installation only.
/// Backing up and restoring are asynchronous: the operation that starts one queues a job and returns a task
/// ID, and a separate worker service does the work, so the result has to be polled. While a portal is being
/// restored every operation of this API answers 403 except
/// `GET api/2.0/backup/getrestoreprogress`, which is also the only one that needs no authorization.
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
    /// Returns the backup schedule of the current portal. A portal keeps at most one schedule, so no ID is
    /// passed in, and when none is set the call still answers 200 with a body that carries no `response`
    /// member at all. `dump` asks for the schedule of the whole server instead of the one of this portal and
    /// requires the space access permission.
    /// The answer cannot be sent back unchanged: `storageParams` is returned as an object keyed by parameter
    /// name, while `POST api/2.0/backup/createbackupschedule` expects an array of key and value pairs. For
    /// every storage type except `ThirdPartyConsumer` the `folderId` key of the answer is built from the
    /// stored base path rather than read back from the saved parameters, and a schedule that keeps an
    /// unlimited number of copies reports `backupsStored` as null instead of 0.
    /// </remarks>
    /// <summary>Get the backup schedule</summary>
    /// <path>api/2.0/backup/getbackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The backup schedule, or an empty payload when none is set", typeof(ScheduleDto))]
    [SwaggerResponse(402, "The portal subscription has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Sets the backup schedule of the current portal. A portal keeps at most one schedule, so this replaces
    /// the existing one rather than adding a second, and `dump` writes the schedule of the whole server
    /// instead, which requires the space access permission and works on a standalone installation only.
    /// Scheduled backups have to be allowed by the pricing plan of a portal that is not a standalone
    /// installation.
    /// `cronParams` is a period plus a time rather than a cron string: `hour` is the hour of the day from 0
    /// to 23, and `day` has to be given for `EveryWeek`, where it is the day of the week from 1 to 7 with
    /// Sunday as 1, and for `EveryMonth`, where it is the day of the month from 1 to 31. It is left out for
    /// `EveryDay`, and because an omitted `day` is stored as 0, which neither period accepts, a weekly or
    /// monthly schedule sent without it fails instead of falling back to a default.
    /// `backupsStored` is the number of scheduled copies to keep, from 1 to 30, and it defaults to 1. Older
    /// copies are removed by a background cleaner, and only the ones this schedule created: archives made by
    /// `POST api/2.0/backup/startbackup` are not counted and not removed. A portal whose subscription stops
    /// covering backups has its schedule deleted by the scheduler, not suspended, and its administrators are
    /// notified that the scheduled backup failed.
    /// The keys expected in `storageParams` are the same as for `POST api/2.0/backup/startbackup`, except
    /// that they are sent as an array of key and value pairs here and returned as an object by
    /// `GET api/2.0/backup/getbackupschedule`.
    /// </remarks>
    /// <summary>Create the backup schedule</summary>
    /// <path>api/2.0/backup/createbackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "True if the schedule was saved", typeof(bool))]
    [SwaggerResponse(400, "The number of the stored copies is outside 1 - 30, or a dump was requested on a portal that is not a standalone installation")]
    [SwaggerResponse(402, "The portal subscription does not cover scheduled backups, has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "The target folder was not found")]
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
    /// Deletes the backup schedule of the current portal, which stops the scheduled backups; `dump` deletes
    /// the schedule of the whole server instead and requires the space access permission. The archives the
    /// schedule has already produced are kept and stay listed by
    /// `GET api/2.0/backup/getbackuphistory` - delete them through
    /// `DELETE api/2.0/backup/deletebackup/{id}` if they are no longer wanted.
    /// The result is always true, including when there was no schedule to delete, so it confirms that the
    /// portal now has none rather than that anything was removed. The deletion is written to the audit trail
    /// either way.
    /// </remarks>
    /// <summary>Delete the backup schedule</summary>
    /// <path>api/2.0/backup/deletebackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "True once the portal has no backup schedule, whether or not one had to be deleted", typeof(bool))]
    [SwaggerResponse(402, "The portal subscription has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Lists the backups of the current portal whose archive is still present in the storage it was written
    /// to. The records come back in no particular order, so sort them by `createdOn` if the newest one is
    /// wanted. `dump` lists the backups of the whole server instead and requires the space access
    /// permission.
    /// Despite being a read operation, this prunes the history as it goes: a record whose archive is no
    /// longer in its storage is deleted outright, so the list can shrink between two calls without anybody
    /// deleting anything. A record whose storage can no longer be reached at all - a disconnected
    /// third-party account, for instance - is neither returned nor deleted, so it stays invisible while
    /// still occupying the history.
    /// The `id` of a record is the same value as the `taskId` that
    /// `POST api/2.0/backup/startbackup` returned for it, and it is what
    /// `DELETE api/2.0/backup/deletebackup/{id}` and the `backupId` of
    /// `POST api/2.0/backup/startrestore` expect.
    /// </remarks>
    /// <summary>Get the backup history</summary>
    /// <path>api/2.0/backup/getbackuphistory</path>
    /// <collection>list</collection>
    [Tags("Backup")]
    [SwaggerResponse(200, "The backups whose archive is still stored", typeof(List<BackupHistoryRecord>))]
    [SwaggerResponse(402, "The portal subscription has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Deletes one backup: first its history record, then the archive in the storage the record points at.
    /// The ID is the one listed by `GET api/2.0/backup/getbackuphistory`, which is also the `taskId` the
    /// backup was started with.
    /// Deleting a backup of the whole server rather than of one portal additionally requires the space
    /// access permission. A record that belongs to another portal is left untouched and the call still
    /// answers true, so the result confirms that the request was accepted rather than that anything was
    /// deleted - check with `GET api/2.0/backup/getbackuphistory` if it matters.
    /// The record is removed before the archive, so when the storage can no longer be reached the archive
    /// stays behind with nothing pointing at it.
    /// </remarks>
    /// <summary>Delete the backup</summary>
    /// <path>api/2.0/backup/deletebackup/{id}</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "True once the request has been accepted, whether or not a backup was deleted", typeof(bool))]
    [SwaggerResponse(402, "The portal subscription has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpDelete("deletebackup/{id:guid}")]
    public async Task<bool> DeleteBackup([FromRoute] DeleteBackupDto inDto)
    {
        await backupService.DeleteBackupAsync(inDto.BackupId);
        return true;
    }

    /// <remarks>
    /// Deletes every backup of the current portal, both the history records and the archives themselves, and
    /// leaves the backup schedule alone. `dump` clears the backups of the whole server instead and requires
    /// the space access permission.
    /// The records are walked one by one and a failure on any of them is swallowed, so the result is always
    /// true even when some archives could not be deleted: it does not mean the history is now empty. Call
    /// `GET api/2.0/backup/getbackuphistory` afterwards to see what is left.
    /// Each record is removed before its archive, so an archive whose deletion fails stays in the storage
    /// with nothing pointing at it.
    /// </remarks>
    /// <summary>Delete the backup history</summary>
    /// <path>api/2.0/backup/deletebackuphistory</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "True once every record has been walked, whether or not all of them were deleted", typeof(bool))]
    [SwaggerResponse(402, "The portal subscription has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Counts the backups of the current portal that were created within a period, and `paid` chooses which
    /// kind is counted: false, the default, counts the ones covered by the free monthly allowance, and true
    /// counts the ones charged to the portal wallet.
    /// The period defaults to the current calendar month - `from` becomes the first day of the month at
    /// 00:00 UTC and `to` becomes the moment of the call. Both bounds are UTC and inclusive, and a `from`
    /// later than `to` is rejected. Called with no parameters at all, this returns exactly the figure the
    /// free monthly allowance is measured against.
    /// The count is over history records rather than over stored archives, so it includes backups that have
    /// already been deleted; use `GET api/2.0/backup/getbackuphistory` to see what can still be restored.
    /// </remarks>
    /// <summary>Get the number of backups</summary>
    /// <path>api/2.0/backup/getbackupscount</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The number of backups created within the period", typeof(int))]
    [SwaggerResponse(400, "The start of the period is later than its end")]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Counts the backups of the current portal created within a period and splits the result into the ones
    /// covered by the free monthly allowance and the ones charged to the portal wallet, which saves calling
    /// `GET api/2.0/backup/getbackupscount` twice.
    /// The `paid` query parameter is accepted but not read here: the answer always carries both figures. The
    /// period behaves as it does for `GET api/2.0/backup/getbackupscount` - it defaults to the current
    /// calendar month, both bounds are UTC and inclusive, and a `from` later than `to` is rejected.
    /// The counts are over history records rather than over stored archives, so they include backups that
    /// have already been deleted.
    /// </remarks>
    /// <summary>Get free and paid backup counts</summary>
    /// <path>api/2.0/backup/getbackupscountbypaid</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The number of free and of paid backups created within the period", typeof(BackupsCountResultDto))]
    [SwaggerResponse(400, "The start of the period is later than its end")]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Reports whether the paid backup service is switched on for the current portal. This is a wallet
    /// setting of the portal, not the health of the backup service or of the worker that runs the jobs, so a
    /// false answer does not mean backups are unavailable and a true one does not mean they are working.
    /// While it is on, backups beyond the free monthly allowance are charged to the portal wallet. While it
    /// is off and that allowance is used up, `POST api/2.0/backup/startbackup` and
    /// `POST api/2.0/backup/createbackupschedule` answer 402.
    /// Starting a backup once the allowance is used up switches the service on by itself, as soon as a
    /// billing session opens for the portal, so this flag can change without anybody editing the portal
    /// settings.
    /// </remarks>
    /// <summary>Check whether backups are enabled</summary>
    /// <path>api/2.0/backup/getservicestate</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "Whether the paid backup service is switched on for this portal", typeof(BackupServiceStateDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
