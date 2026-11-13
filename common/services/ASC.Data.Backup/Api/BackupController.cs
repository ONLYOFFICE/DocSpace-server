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
    /// Returns the backup schedule of the portal. A portal has at most one schedule. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403.
    /// With `dump`, a caller without the space access permission also gets 403. The call is read-only. If no
    /// schedule is set, the call returns 200 without the `response` field. If the portal subscription has
    /// expired or is not paid, the call returns 402. To set the schedule, use
    /// `POST api/2.0/backup/createbackupschedule`.
    /// </remarks>
    /// <summary>Get the backup schedule</summary>
    /// <path>api/2.0/backup/getbackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The backup schedule, or an empty payload when none is set", typeof(ScheduleDto))]
    [SwaggerResponse(402, "The portal subscription has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("getbackupschedule")]
    public async Task<ScheduleDto> GetBackupSchedule(BackupDumpRequestDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        var response = await backupService.GetScheduleAsync(dto.Dump);
        if (response == null)
        {
            return null;
        }

        var schedule = new ScheduleDto
        {
            StorageType = response.StorageType,
            StorageParams = response.StorageParams ?? new Dictionary<string, string>(),
            CronParams = new CronParams(response.Cron).Map(),
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

    /// <remarks>
    /// Creates the backup schedule of the portal, or replaces the current one. A portal has at most one
    /// schedule. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. With `dump`, a caller without the space access permission also gets 403. The call
    /// changes data. You can repeat it with the same body. It returns `true`. If the free backups of the month
    /// are spent and the paid service is off, the scheduler deletes the schedule. It then sends a notice that
    /// the scheduled backup failed. The call returns 400 if `cronParams` is missing or a value is out of range.
    /// A weekly or monthly schedule without `day` also returns 400. It also returns 400 if `storageParams` has
    /// no key that the storage needs, or repeats a key. It also returns 400 for an unknown `module` or a
    /// `Documents` folder ID that is not a number. If the target folder does not exist, the call returns 404.
    /// It returns 403 if you cannot create files in the folder. The root folders of rooms, the archive, and
    /// room templates also return 403. If the plan has no free backups and the paid backup service is off, the
    /// call returns 402. It also returns 402 if the portal subscription has expired or is not paid.
    /// </remarks>
    /// <summary>Create the backup schedule</summary>
    /// <path>api/2.0/backup/createbackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "True if the schedule was saved", typeof(bool))]
    [SwaggerResponse(400, "The request body cannot be read, `cronParams` is missing, its period is not a defined value, its hour or day is out of range for the period, which includes a weekly or monthly schedule sent without `day`, the number of the stored copies is outside 1-30, `storageType` is not a storage the backups can be written to, a `ThirdPartyConsumer` schedule has no known `module`, the `folderId` or `filePath` key the storage type needs is missing, the `folderId` of a `Documents` schedule is not a number, a dump was requested on a portal that is not a standalone installation, or `storageParams` repeats a key or, on a dump, contains `tenantId`")]
    [SwaggerResponse(402, "The portal subscription does not cover scheduled backups, has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "The target folder was not found")]
    [HttpPost("createbackupschedule")]
    public async Task<bool> CreateBackupSchedule(CreateBackupScheduleRequestDto inDto)
    {
        if (inDto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        var storageType = inDto.StorageType ?? BackupStorageType.Documents;
        var storageParams = inDto.StorageParams == null ? new Dictionary<string, string>() : inDto.StorageParams.ToDictionary(r => r.Key.ToString(), r => r.Value.ToString());
        var backupStored = inDto.BackupsStored ?? 1;
        var cronParams = inDto.CronParams ?? throw new ArgumentException("cronParams is required");
        var cron = new CronParams
        {
            Period = cronParams.Period ?? BackupPeriod.EveryDay,
            Hour = cronParams.Hour,
            Day = cronParams.Day ?? 0
        };
        if (backupStored is > 30 or < 1)
        {
            throw new ArgumentException("backupStored must be 1 - 30");
        }

        if (storageType is BackupStorageType.Documents or BackupStorageType.ThridpartyDocuments)
        {
            var folderId = storageParams.GetValueOrDefault("folderId") ?? throw new ArgumentException("storageParams must contain folderId");

            // A Documents folder has a numeric ID; a string one belongs to a third-party storage, as in startbackup.
            if (storageType == BackupStorageType.Documents && !int.TryParse(folderId, out _))
            {
                throw new ArgumentException("wrong folder type");
            }

            if (int.TryParse(folderId, out var fId))
            {
                await backupService.CheckAccessToFolderAsync(fId);
            }
            else
            {
                await backupService.CheckAccessToFolderAsync(folderId);
            }
        }
        await backupService.CreateScheduleAsync(storageType, storageParams, backupStored, cron, inDto.Dump);
        return true;
    }

    /// <remarks>
    /// Deletes the backup schedule of the portal, so scheduled backups stop. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. With `dump`, a
    /// caller without the space access permission also gets 403. The call changes data. It always returns
    /// `true`, even if there was no schedule, so you can repeat it. The call keeps the backups that the
    /// schedule created. To delete them, use `DELETE api/2.0/backup/deletebackup/{id}`. If the portal
    /// subscription has expired or is not paid, the call returns 402.
    /// </remarks>
    /// <summary>Delete the backup schedule</summary>
    /// <path>api/2.0/backup/deletebackupschedule</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "True once the portal has no backup schedule, whether or not one had to be deleted", typeof(bool))]
    [SwaggerResponse(402, "The portal subscription has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpDelete("deletebackupschedule")]
    public async Task<bool> DeleteBackupSchedule(BackupDumpRequestDto dto)
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
    /// Starts a backup of the portal and returns the queued job. Requires the EditPortalSettings permission, which the
    /// portal owner and DocSpace admins have. Other callers get 403. With `dump`, a caller without the space access
    /// permission also gets 403. The call queues work. To follow the job, poll `GET api/2.0/backup/getbackupprogress`
    /// until `isCompleted` is true. Do not repeat it while a backup runs. A repeat returns the running job. But after
    /// the free backups of the month, it opens another payment session in the wallet. Once the job ends, a repeated
    /// call makes another backup. After the portal spends its free backups of the month, the call charges each backup
    /// to the portal wallet. It then also turns on the paid backup service. If the wallet cannot pay for the backup,
    /// the call returns 402. It also returns 402 for a `Documents` backup if the portal is over its storage quota. The
    /// call returns 400 if `folderId` is missing or does not match `storageType`. It also returns 400 if an item of
    /// `storageParams` has no key or value, or repeats a key. If the target folder does not exist, the call returns
    /// 404. It returns 403 if you cannot create files in the folder. The root folders of rooms, the archive, and room
    /// templates also return 403.
    /// </remarks>
    /// <summary>Start the backup</summary>
    /// <path>api/2.0/backup/startbackup</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The state of the queued backup job", typeof(BackupProgressDto))]
    [SwaggerResponse(400, "The request body cannot be read, the folder ID does not match the storage type or is missing, an item of `storageParams` is null, has no key or no value, or repeats a key, or a dump was requested on a portal that is not a standalone installation")]
    [SwaggerResponse(402, "The portal already uses more storage than its plan allows and the backup goes to `Documents`, or the free backups of the current month are used up and the paid backup service is not available to this portal or cannot be charged")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "The target folder or the backup quota was not found")]
    [AllowNotPayment]
    [HttpPost("startbackup")]
    public async Task<BackupProgressDto> StartBackup(StartBackupRequestDto inDto, [FromServices] TenantQuotaController quotaController)
    {
        await backupService.DemandPermissionsBackupAsync();

        if (inDto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        var storageType = inDto.StorageType ?? BackupStorageType.Documents;
        var storageParams = inDto.StorageParams == null
            ? new Dictionary<string, string>()
            : inDto.StorageParams.ToDictionary(
                r => r?.Key?.ToString() ?? throw new ArgumentException("every item of storageParams must have a key"),
                r => r.Value?.ToString() ?? throw new ArgumentException("every item of storageParams must have a value"));

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

            var folderId = storageParams.GetValueOrDefault("folderId") ?? throw new ArgumentException("storageParams must contain folderId");

            if (int.TryParse(folderId, out var fId))
            {
                await backupService.CheckAccessToFolderAsync(fId);
            }
            else
            {
                await backupService.CheckAccessToFolderAsync(folderId);
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

            return (await backupService.GetBackupProgressAsync(inDto.Dump))?.Map();

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
    /// Cancels the backup job of the portal and removes it from the queue. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call changes
    /// data. It returns `true` if it removed a job and `false` if there was none. A repeated call returns
    /// `false`. After this call, `GET api/2.0/backup/getbackupprogress` returns no job, not a job with the
    /// `Canceled` status. The call cannot cancel a restore.
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
    /// Returns the state of the backup job of the portal. A portal has at most one backup job. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403.
    /// With `dump`, a caller without the space access permission also gets 403. The call is read-only. This is
    /// the call to poll after `POST api/2.0/backup/startbackup`. Repeat it until `isCompleted` is true. If
    /// there is no job, the call returns 200 without the `response` field.
    /// </remarks>
    /// <summary>Get the backup progress</summary>
    /// <path>api/2.0/backup/getbackupprogress</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The state of the backup job, or an empty payload when there is no such job", typeof(BackupProgressDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [AllowNotPayment]
    [HttpGet("getbackupprogress")]
    public async Task<BackupProgressDto> GetBackupProgress(BackupDumpRequestDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        return (await backupService.GetBackupProgressAsync(dto.Dump))?.Map();
    }

    /// <remarks>
    /// Returns the backups of the portal whose archive is still in its storage. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. With `dump`, a
    /// caller without the space access permission also gets 403. The call changes data. It deletes the record
    /// of each backup whose archive is no longer in its storage. It skips backups whose storage it cannot
    /// reach, and keeps their records. You can repeat the call. The list has no set order, so sort it by
    /// `createdOn`. If the portal subscription has expired or is not paid, the call returns 402.
    /// </remarks>
    /// <summary>Get the backup history</summary>
    /// <path>api/2.0/backup/getbackuphistory</path>
    /// <collection>list</collection>
    [Tags("Backup")]
    [SwaggerResponse(200, "The backups whose archive is still stored", typeof(List<BackupHistoryRecordDto>))]
    [SwaggerResponse(402, "The portal subscription has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("getbackuphistory")]
    public async Task<List<BackupHistoryRecordDto>> GetBackupHistory(BackupDumpRequestDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        return (await backupService.GetBackupHistoryAsync(dto.Dump))?.Select(r => r.Map()).ToList();
    }

    /// <remarks>
    /// Deletes a backup. The call first deletes its history record, then its archive in the storage. Requires
    /// the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. For a server-wide backup, a caller without the space access permission also gets 403. The call
    /// changes data and cannot be undone. It returns `true` even if it deletes nothing, for example for a
    /// backup of another portal or a repeated call. If the call cannot reach the storage, the archive stays
    /// there without a record. To check what is left, use `GET api/2.0/backup/getbackuphistory`. If no backup
    /// ever had this ID, the call returns 404. If the portal subscription has expired or is not paid, the call
    /// returns 402.
    /// </remarks>
    /// <summary>Delete the backup</summary>
    /// <path>api/2.0/backup/deletebackup/{id}</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "True once the request has been accepted, whether or not a backup was deleted", typeof(bool))]
    [SwaggerResponse(402, "The portal subscription has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "There is no backup record with this ID")]
    [HttpDelete("deletebackup/{id:guid}")]
    public async Task<bool> DeleteBackup([FromRoute] DeleteBackupRequestDto inDto)
    {
        await backupService.DeleteBackupAsync(inDto.BackupId);
        return true;
    }

    /// <remarks>
    /// Deletes all backups of the portal, both the history records and the archives. The call keeps the backup
    /// schedule. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. With `dump`, a caller without the space access permission also gets 403. The call
    /// changes data and cannot be undone. If an archive fails to delete, the call goes on, and the archive
    /// stays in the storage without a record. It always returns `true`, so you can repeat it. To check what is
    /// left, use `GET api/2.0/backup/getbackuphistory`. If the portal subscription has expired or is not paid,
    /// the call returns 402.
    /// </remarks>
    /// <summary>Delete the backup history</summary>
    /// <path>api/2.0/backup/deletebackuphistory</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "True once every record has been walked, whether or not all of them were deleted", typeof(bool))]
    [SwaggerResponse(402, "The portal subscription has expired or has not been paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpDelete("deletebackuphistory")]
    public async Task<bool> DeleteBackupHistory(BackupDumpRequestDto dto)
    {
        if (dto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }
        await backupService.DeleteAllBackupsAsync(dto.Dump);
        return true;
    }

    /// <remarks>
    /// Starts restoring the portal from a backup and returns the queued job. The restore replaces the current
    /// data of the portal. The source is a backup from the history, or a file in `storageParams` if `backupId`
    /// is not a GUID. An all-zero `backupId` selects no backup, so the job fails. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403.
    /// With `dump`, a caller without the space access permission also gets 403. The call queues work and
    /// cannot be undone. While the restore runs, most API calls return 403. To follow the job, poll
    /// `GET api/2.0/backup/getrestoreprogress` until `isCompleted` is true. You cannot cancel a restore
    /// through the API. A repeated call during the restore returns 403 or the running job. After the restore ends, a
    /// repeated call restores the portal again. If the plan does not allow restoring, the call returns 402. The call
    /// returns 400 if `backupId` is missing, or it is not a GUID and `storageParams` has no `filePath`. It also returns
    /// 400 if an item of `storageParams` has no key or value, or repeats a key. The call returns 404 if the backup, the
    /// file, or its folder does not exist. For `Local` storage, a missing uploaded archive also returns 404. It returns
    /// 403 if you cannot create files in the folder of the backup file.
    /// </remarks>
    /// <summary>Start the restoring process</summary>
    /// <path>api/2.0/backup/startrestore</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The state of the queued restoring job", typeof(BackupProgressDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `backupId`, `backupId` is not a GUID and `storageParams` has no `filePath`, or an item of `storageParams` is null, has no key or no value, or repeats a key")]
    [SwaggerResponse(402, "The pricing plan of this portal does not allow restoring")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "The backup record was not found, the file given in `filePath` or its folder was not found, or, for `Local` storage, no backup archive has been uploaded to the portal")]
    [HttpPost("startrestore")]
    public async Task<BackupProgressDto> StartBackupRestore(StartBackupRestoreRequestDto inDto)
    {
        if (inDto.Dump)
        {
            await tenantExtra.DemandAccessSpacePermissionAsync();
        }

        await backupService.DemandPermissionsRestoreAsync();

        var storageParams = inDto.StorageParams == null
            ? new Dictionary<string, string>()
            : inDto.StorageParams.ToDictionary(
                r => r?.Key?.ToString() ?? throw new ArgumentException("every item of storageParams must have a key"),
                r => r.Value?.ToString() ?? throw new ArgumentException("every item of storageParams must have a value"));

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

        return (await backupService.GetRestoreProgressAsync(inDto.Dump))?.Map();
    }

    /// <remarks>
    /// Returns the state of the restore job of the portal. Anyone can call it without signing in. It also works
    /// while the portal is being restored and other calls are refused. The call is read-only. This is the call
    /// to poll after `POST api/2.0/backup/startrestore`. Repeat it until `isCompleted` is true. Without `dump`,
    /// the call returns any restore job of the portal, including a server-wide one. If there is no job, the
    /// call returns 200 without the `response` field.
    /// </remarks>
    /// <summary>Get the restoring progress</summary>
    /// <path>api/2.0/backup/getrestoreprogress</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Backup")]
    [SwaggerResponse(200, "The state of the restoring job, or an empty payload when there is no such job", typeof(BackupProgressDto))]
    [HttpGet("getrestoreprogress")]  //NOTE: this method doesn't check payment!!!
    [AllowAnonymous]
    [AllowNotPayment]
    public async Task<BackupProgressDto> GetRestoreProgress(RestoreProgressRequestDto dto)
    {
        return (await backupService.GetRestoreProgressAsync(dto.Dump))?.Map();
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
    /// Returns how many free backups the portal created in a period, or paid ones if `paid` is true. Requires
    /// the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. The call is read-only. With no parameters, it returns the count that the free monthly allowance
    /// is checked against. The count includes deleted backups. To list the stored backups, use
    /// `GET api/2.0/backup/getbackuphistory`. The call returns 400 if `from` is later than `to` or a value
    /// cannot be parsed.
    /// </remarks>
    /// <summary>Get the number of backups</summary>
    /// <path>api/2.0/backup/getbackupscount</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The number of backups created within the period", typeof(int))]
    [SwaggerResponse(400, "The start of the period is later than its end, or `from`, `to` or `paid` cannot be parsed")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [AllowNotPayment]
    [HttpGet("getbackupscount")]
    public async Task<int> GetBackupsCountAsync(BackupsCountRequestDto dto)
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
    /// Returns the number of free and of paid backups that the portal created in a period. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403.
    /// The call is read-only. It ignores `paid` and always returns both numbers. The counts include deleted
    /// backups. The call returns 400 if `from` is later than `to` or a value cannot be parsed.
    /// </remarks>
    /// <summary>Get free and paid backup counts</summary>
    /// <path>api/2.0/backup/getbackupscountbypaid</path>
    [Tags("Backup")]
    [SwaggerResponse(200, "The number of free and of paid backups created within the period", typeof(BackupsCountResultDto))]
    [SwaggerResponse(400, "The start of the period is later than its end, or `from`, `to` or `paid` cannot be parsed")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [AllowNotPayment]
    [HttpGet("getbackupscountbypaid")]
    public async Task<BackupsCountResultDto> GetBackupsCountsAsync(BackupsCountRequestDto dto)
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
    /// Returns whether the paid backup service is on for the portal. While it is on, the portal wallet pays for
    /// backups beyond the free monthly allowance. This is a wallet setting, not whether the backup service
    /// works. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. The call is read-only. When `POST api/2.0/backup/startbackup` charges a backup to
    /// the wallet, the portal turns the service on. While the service is off,
    /// `POST api/2.0/backup/createbackupschedule` returns 402 on a plan without free backups. The scheduler
    /// then also deletes the schedule after the free backups of the month are spent.
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
