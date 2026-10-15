# backup-and-restore — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`). `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`; `BackupController` is
`common/services/ASC.Data.Backup/Api/BackupController.cs`, `BackupService` is
`common/ASC.Data.Backup.Core/Service/BackupService.cs`, `BackupProgressItem` / `RestoreProgressItem` are in
`common/ASC.Data.Backup.Core/Service/ProgressItems/`, the worker services in
`common/services/ASC.Data.Backup.Worker/Services/`. Status: **TEST-CONFIRMED** (a test asserts it),
**CODE-ONLY** (read from the product code or the OpenAPI document, no test), **NOTES-ONLY** (only the release
notes say it). Exception → status mapping used throughout: `SecurityException` 403, `ArgumentException` 400,
`BillingException` / `TenantQuotaException` / `AccountingPaymentRequiredException` 402, `ItemNotFoundException` /
`FileNotFoundException` / `DirectoryNotFoundException` 404, `NotSupportedException` 415, any other exception 500
(`CustomExceptionHandler` in `common/ASC.Api.Core/Middleware/ResponseWrapper.cs:53-114`, registered by
`common/ASC.Api.Core/Core/BaseStartup.cs:129`).

No integration test calls the backup endpoints: `web/ASC.Web.Api.Tests`, `products/ASC.Files/Tests` and
`products/ASC.People/Tests` contain no `/backup` request (the only hits are TFA "backup codes", the wallet
service named `backup` and a commented-out quota assertion). `products/ASC.Files/Tests/Tests/07_Settings/Storage`
covers the files settings (auto-cleanup, force save, display extension, third-party access), not backup
storage. The unit tests in `common/Tests/ASC.Data.Backup.Core.Tests` cover the archive coverage and the row
remapping on restore.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Who may use the backup operations** | | |
| Portal-settings right on every operation; SaaS also requires the Backup section not hidden (`web:hide-settings`) → `SecurityException` 403 | `BackupService.cs:599-607` (`DemandPermissionsBackupAsync`), called from every service method; `web/ASC.Web.Core/SetupInfo.cs:127,153`; `../buildtools/config/appsettings.json:181-189` (Backup is not hidden by default) | CODE-ONLY |
| Anonymous 401 on every operation except `getRestoreProgress` (`[AllowAnonymous]`, `requiresAuthorization=false`) | `BackupController.cs:633-642`; yaml `getRestoreProgress` security `{}` | CODE-ONLY |
| `dump` demands the space-access right: 403 on a hosted portal and on a standalone with `LimitedAccessSpace` | `BackupController.cs:100-103, 160-163, 214-217, 258-261, 451-454, 483-486, 534-537, 568-571`; `web/ASC.Web.Core/Utility/TenantExtra.cs:108-114` | CODE-ONLY |
| v3.5 / v4.0: "all portal admins may back up"; only the payer connects the service | release notes v3.5, v4.0; server: `EditPortalSettings` only; the service is switched on by `startBackup` itself (`BackupService.cs:554-570`) | CODE-ONLY (the payer restriction is a wallet rule, out of scope) |
| **Payment state of the portal** | | |
| `[AllowNotPayment]` on `startBackup`, `cancelBackup`, `getBackupProgress`, `getRestoreProgress`, `getBackupsCount`, `getBackupsCounts`, `getBackupsServiceState`; the others answer 402 when not paid | `BackupController.cs:252, 389, 447, 638, 675, 710, 746`; `common/ASC.Api.Core/Middleware/PaymentFilter.cs:39-59`; `TenantExtra.cs:96-106`; yaml 402 rows of `getBackupSchedule`, `createBackupSchedule`, `deleteBackupSchedule`, `getBackupHistory`, `deleteBackup`, `deleteBackupHistory` | CODE-ONLY |
| **Starting a manual backup** | | |
| Queues one job, returns `taskId`, progress 0, status Created; worker picks the integration event up | `BackupController.cs:254-373`; `BackupService.cs:65-107`; `common/ASC.Data.Backup.Core/Service/BackupWorker.cs:106-148`; `common/services/ASC.Data.Backup.Worker/IntegrationEvents/EventHandling/BackupRequestedIntegrationEventHandler.cs:49-81`; yaml `startBackup` | CODE-ONLY |
| `storageType` default Documents; keys per type; `subdir` added for ThirdPartyConsumer; Local on standalone only (`Exception` "Access denied" → 500) | `BackupController.cs:263-302`; `BackupService.cs:85-99`; yaml `StartBackupRequestDto` | CODE-ONLY |
| Folder id kind mismatch → `ArgumentException` "wrong folder type" (400) | `BackupController.cs:266-275` | CODE-ONLY |
| Second start while a job is queued returns the existing job | `BackupWorker.cs:110-146` (new item only when none, or the same taskId still Created) | CODE-ONLY |
| **Target folder of a Documents backup** | | |
| Folder must exist (404), not Rooms/RoomTemplates/Archive root, caller must `CanCreate` (403) | `BackupService.cs:109-123`; `BackupController.cs:282-298` | CODE-ONLY |
| Documents target checks the portal storage allowance (`QuotaUsedCheckAsync(0)`) → 402 | `BackupController.cs:284-288`; yaml `startBackup` 402 | CODE-ONLY |
| **Free monthly allowance and the paid backup service** | | |
| `countFreeBackup` feature (`free_backup`), checker counts free records since the first of the month UTC; message "The number of free backups should not exceed {0} within a month" | `common/ASC.Data.Backup.Core/Quota/CountFreeBackupFeature.cs:40-99`; `common/ASC.Core.Common/Tenants/TenantQuota.cs:439-449, 564`; `web/ASC.Web.Core/PublicResources/Resource.resx:1248` | CODE-ONLY |
| Values per plan: 0 on Startup / Non-profit, 2 on Business | release notes v3.5 (lines 53, 56); the server reads the number from the tariff catalogue (SaaS seed data, no test) | NOTES-ONLY |
| Allowance used up → billing session opened under a per-portal lock; none → `BillingException` "Your pricing plan does not support this option"; payment errors → "Payment required"; service switched on (`EnsureBackupServiceEnabledAsync`); session closed on failure | `BackupController.cs:306-372`; `BackupService.cs:457-476, 554-570`; `Resource.resx:861, 894` | CODE-ONLY |
| Standalone and not-paid tariff skip the check (v3.5 "blocked portal keeps free unlimited backups") | `CountFreeBackupFeature.cs:59-73` | CODE-ONLY |
| Record marked `Paid` when a session id was set; free/paid counters | `BackupProgressItem.cs:212-228`; `common/ASC.Data.Backup.Core/Storage/BackupRepository.cs:136-152` | CODE-ONLY |
| **Progress and result of a backup job** | | |
| Empty payload when no job; fields while running / after; `Failted` with `error` | `BackupController.cs:449-456`; `BackupWorker.cs:204-218`; `common/ASC.Data.Backup.Core/Service/ProgressItems/BaseBackupProgressItem.cs:65-87`; yaml `BackupProgressDto` | CODE-ONLY |
| Socket `backup-progress` / `end-backup` to `{tenant}-backup` (`backup` for dump); non-admins filtered out of the `backup` room | `products/ASC.Files/Core/Utils/SocketManager.cs:148-164`; `common/ASC.Socket.IO/app/hubs/files.js:187-199, 229-241, 454-467, 543-552` | CODE-ONLY |
| Completion letter `backup_created` to the starter (manual only); audit "backup completed"; Documents file marked new and announced | `BackupProgressItem.cs:232-237, 249, 261-272, 350-355`; `common/ASC.Data.Backup.Core/Core/NotifyHelper.cs:137-148, 229-256` | CODE-ONLY |
| **Warning about files missing during the backup** | | |
| Unreadable file that no longer exists in `files_file` → listed in `storage/missing_info`, job completes with `warning` "Some files were not included in the backup. For more details, please check {0}"; file still in the DB → rethrow (job fails) (v3.6) | `common/ASC.Data.Backup.Core/Tasks/BackupPortalTask.cs:609-676`; `BackupProgressItem.cs:239-243`; `common/ASC.Data.Backup.Core/Tasks/KeyHelper.cs:83-86`; `common/ASC.Data.Backup.Core/BackupResource.resx:129-131`; release note v3.6 line 48 | CODE-ONLY |
| **Failed and cancelled backups** | | |
| Failure: `backup_failed` letter to the starter (all active admins when started by the scheduler), audit "backup failed", billing session closed (v3.6) | `BackupProgressItem.cs:274-312`; `NotifyHelper.cs:150-166, 258-286`; release note v3.6 line 44 | CODE-ONLY |
| `cancelBackup` dequeues the job(s) of the portal, true/false, audit "backup cancelled"; cancelled job leaves the queue; no failure letter when cancelled (v3.7) | `BackupController.cs:391-403`; `BackupWorker.cs:172-184`; `BackupProgressItem.cs:278-283, 302`; yaml `cancelBackup`; release note v3.7 line 70 | CODE-ONLY |
| **Contents of a portal archive** | | |
| Declared modules: Tenants, Audit, AiProviders, Files, Files2, WebStudio, Core, Identity, Ai, AiIntegration; storage modules allowed list (files, logo, whitelabel, customnavigation, room_logos, webplugins, mcp_icons and legacy names) | `common/ASC.Data.Backup.Core/Tasks/Modules/ModuleProvider.cs:39-51`; `common/ASC.Data.Backup.Core/Tasks/PortalTaskBase.cs:121-151`; `.claude/rules/backup-tables.md` | CODE-ONLY |
| Thumbnails and earlier Documents archives excluded; stored quota rows corrected by their size | `BackupPortalTask.cs:534-545, 684-757` | CODE-ONLY |
| Every tenant table is declared, intentionally skipped (short_links, account_links, backup_backup, tariff rows, notify/event queues, instance registration, …) or a known gap | `common/Tests/ASC.Data.Backup.Core.Tests/BackupCoverageTests.cs:65-167` | TEST-CONFIRMED |
| Rows are remapped to the new tenant, folder, user and provider ids on restore (files_security, files_group, files_roomgroup, files_order, metadata tables) | `common/Tests/ASC.Data.Backup.Core.Tests/FilesModuleSpecificsTests.cs:93-369` | TEST-CONFIRMED |
| **Backup history** | | |
| Lists records whose archive exists; prunes records whose archive is gone; unreachable storage neither listed nor pruned; fields and `expiresOn` one day for DataStore | `BackupService.cs:202-227`; `common/ASC.Data.Backup.Core/Storage/BackupStorageFactory.cs:45-59`; `BackupProgressItem.cs:206-228`; `common/services/ASC.Data.Backup/ApiModels/BackupHistoryRecordDto.cs`; yaml `getBackupHistory`, `BackupHistoryRecordDto` | CODE-ONLY |
| A Documents archive in the Trash counts as gone | `common/ASC.Data.Backup.Core/Storage/DocumentsBackupStorage.cs:202-215` | CODE-ONLY |
| **Deleting backups** | | |
| `deleteBackup`: record first (soft `Removed`), then the archive; other portal's record ignored with true; dump record needs the space-access right; unknown id → 500 | `BackupService.cs:125-150`; `BackupRepository.cs:93-104`; `BackupController.cs:509-513`; yaml `deleteBackup` | CODE-ONLY |
| `deleteBackupHistory`: walks records, swallows failures, always true, schedule untouched | `BackupService.cs:152-176`; `BackupController.cs:532-540` | CODE-ONLY |
| **Backup counters and service state** | | |
| Default period first-of-month UTC to now; inclusive bounds; `from > to` → 400; `paid` filter; counts include removed records | `BackupController.cs:677-728`; `CountFreeBackupFeature.cs:90-99`; `BackupRepository.cs:231-242` (no `Removed` filter) | CODE-ONLY |
| `getBackupsServiceState.enabled` = wallet setting contains the Backup service | `BackupController.cs:748-757`; `BackupService.cs:548-552` | CODE-ONLY |
| **Creating the backup schedule** | | |
| One schedule per portal (replace); `backupsStored` 1-30 default 1 (400 outside); cron built from period/hour/day and validated by `CronExpression` (500); folder checked as for a backup; Local only standalone | `BackupController.cs:158-193`; `BackupService.cs:369-423, 594-597, 661-713`; `BackupRepository.cs:106-111`; yaml `createBackupSchedule`, `BackupCronRequest` | CODE-ONLY |
| **Scheduled backups require the allowance or the paid service** | | |
| `AutoBackup` section hidden → 403; SaaS with `countFreeBackup == 0` and service off → `BillingException` 402; standalone exempt | `BackupService.cs:609-630` | CODE-ONLY |
| **Reading and deleting the schedule** | | |
| Empty payload when none; `storageParams` as object with `folderId` = base path except ThirdPartyConsumer; `backupsStored` null for 0; delete always true with audit "scheduled backup deleted" | `BackupController.cs:98-126, 212-226`; `BackupService.cs:425-433, 448-455, 572-592`; yaml `getBackupSchedule`, `ScheduleDto`, `deleteBackupSchedule` | CODE-ONLY |
| **Running the schedule** | | |
| Scheduler period 15 min; next run = cron after last run in the portal time zone; only active portals; tariff state must be below Delay (grace) | `BackupSchedulerService.cs:60, 80-104`; `common/ASC.Data.Backup.Core/Core/Schedule.cs:39-70`; `BackupRepository.cs:209-217`; `../buildtools/config/backup.json` | CODE-ONLY |
| Allowance check under the lock; paid session only when the service is on; `lastBackupTime` stamped; job queued as scheduled under the system account | `BackupSchedulerService.cs:104-150`; `BackupWorker.cs:150-170`; `BackupProgressItem.cs:75-90, 127-135` | CODE-ONLY |
| On quota / billing / payment failure the schedule is deleted and `scheduled_backup_failed` is sent to active admins | `BackupSchedulerService.cs:157-164`; `NotifyHelper.cs:168-183, 288-316`; yaml `createBackupSchedule` description | CODE-ONLY |
| Scheduled run: audit by the backup service initiator, no completion letter | `BackupProgressItem.cs:232-237, 338-348` | CODE-ONLY |
| **Stored copies of a schedule are pruned** | | |
| Cleaner period 15 min; scheduled records beyond `backupsStored` newest removed; manual ones untouched; expired DataStore records; records whose archive is gone; provider refusal → kept until 6 months old | `BackupCleanerService.cs:44-125`; `BackupRepository.cs:171-185` | CODE-ONLY |
| **Who may restore and from what** | | |
| `Restore` section hidden → 403; SaaS without `quota.Restore` → 402; `backupId` GUID vs `filePath`; record not found → `FileNotFoundException`; Documents file checked through `CheckAccessToFileAsync` (404 / 403); audit "restore started" | `BackupController.cs:566-616`; `BackupService.cs:178-200, 238-299, 632-646`; `TenantQuota.cs:379-389, 558` (`restore` feature flagged for DocSpace admins); yaml `startBackupRestore`, `StartBackupRestoreRequestDto` | CODE-ONLY |
| v3.5: restore unavailable on Startup | release note v3.5 line 55; server: the `restore` feature of the tariff (catalogue data) | CODE-ONLY (mechanism) / NOTES-ONLY (which plan) |
| **Uploading an archive to restore from** | | |
| `backupFileUpload.ashx`: `Init=true` + `totalSize` + `extension`, size ≤ `MaxTotalSize` ("Backup is larger than the total size of the portal"), chunk ≤ `ChunkUploadSize`, upload info cached 20 min, earlier file replaced, `endUpload` when complete; restore right demanded; errors as `success:false` | `common/ASC.Data.Backup.Core/BackupFileUploadHandler.cs:46-168`; `common/services/ASC.Data.Backup/Extensions/BackupServiceExtensions.cs:62-69`; `BackupResource.resx:156-158` | CODE-ONLY |
| `Local` restore uses `<tenant>-backup.tar.gz` / `.tar` in the `backup_temp` store; missing → 404 | `BackupService.cs:264-278, 435-446` | CODE-ONLY |
| **The portal while it is being restored** | | |
| `restore_started` to active users or the owner (activated accounts only); tenant status Restoring; progress 10 → 75 → 100 | `RestoreProgressItem.cs:147-189, 226, 275`; `NotifyHelper.cs:185-200, 220-226, 318-343` | CODE-ONLY |
| 403 on every request while Restoring except paths ending in `portal`, `settings`, `getrestoreprogress`, `cancelrestore`, `colortheme`, `logos`, `build`, `@self`, `encryption/progress`, `rebranding/*` | `common/ASC.Api.Core/Middleware/TenantStatusFilter.cs:40-83` | CODE-ONLY |
| `getRestoreProgress`: anonymous, `dump` three-state, forced false on SaaS, empty payload when none, `link`/`warning` never set | `BackupController.cs:639-642`; `BackupService.cs:344-362`; `BackupWorker.cs:228-250`; `BaseBackupProgressItem.cs:77-84` | CODE-ONLY |
| Socket `restore-progress` / `end-restore` to `{tenant}-restore` | `SocketManager.cs:159-164`; `files.js:530-540` | CODE-ONLY |
| No cancel-restore operation is published (`cancelrestore` action commented out) | `BackupController.cs:405-427`; `BackupWorker.cs:186-202` (dump restore never cancellable) | CODE-ONLY |
| **What a restore produces** | | |
| SaaS: SHA-256 (or multipart MD5) of the archive must match a record of the portal, else "The backup file is invalid. Please use the file created in the same portal."; unreadable archive → "The backup file is invalid." | `RestoreProgressItem.cs:131-145, 278-289`; `BackupWorker.cs:330-364`; `BackupResource.resx:126-134` | CODE-ONLY |
| Dump archive on SaaS, or dump flag mismatch → invalid | `common/ASC.Data.Backup.Core/Tasks/RestorePortalTask.cs:88-101` | CODE-ONLY |
| New tenant with remapped ids; alias, mapped domain and payment id kept; history records migrated; status Active; TFA / SMS settings cleared; default template settings restored; `restore_completed_v115` with a set-password link | `RestoreProgressItem.cs:169-261`; `RestorePortalTask.cs:110-127`; `BackupRepository.cs:76-91`; `NotifyHelper.cs:203-218, 345-371` | CODE-ONLY |
| Failure: original tenant (all tenants for a dump) set Active again; cancelled non-dump restore removes the half-restored tenant | `RestoreProgressItem.cs:278-331` | CODE-ONLY |
| **Download link of a backup** | | |
| `link` = pre-signed URI (1 day) for DataStore, internal URI for ThirdPartyConsumer, "" for Documents / ThridpartyDocuments / Local; DataStore record expires after 1 day | `common/ASC.Data.Backup.Core/Storage/ConsumerBackupStorage.cs:99-107`; `DocumentsBackupStorage.cs:120-123`; `common/ASC.Data.Backup.Core/Storage/LocalBackupStorage.cs:75-78`; `BackupProgressItem.cs:204, 223` | CODE-ONLY |
| Hidden `GET backup/backuptmp` returns the worker temp folder | `BackupController.cs:644-656` (`IgnoreApi`) | CODE-ONLY |
| **Storage targets offered for backups** | | |
| `getAllBackupStorages`: consumer list, `current` only for a ThirdPartyConsumer schedule; portal-settings right; SaaS Backup section check through `GetScheduleAsync` | `web/ASC.Web.Api/Api/Settings/StorageController.cs:601-646`; yaml `getAllBackupStorages` | CODE-ONLY |
| Third-party targets "only on Business" | release note v3.5 line 54; server: no tariff check on `ThirdPartyConsumer` in `startBackup` / `createBackupSchedule` (`BackupController.cs:158-193, 254-373`) | NOTES-ONLY (client rule; spec says the server does not check it) |
| **Backup features exposed by the tariff** | | |
| `free_backup` (count), `backup` (wallet flag), `restore`, `thirdparty` features, flagged `EmployeeType.DocSpaceAdmin` | `TenantQuota.cs:439-461, 558, 561, 564-565`; `web/ASC.Web.Api/Core/QuotaHelper.cs` (grading, see `quotas` spec) | CODE-ONLY |
| Wallet service `backup` is priced and listed | `web/ASC.Web.Api.Tests/Tests/04_Portal/Payments/PaymentWalletServiceTests.cs:67-82`; `PaymentWalletServicesTests.cs:63-79` | TEST-CONFIRMED |
| Features marked for DocSpace admins are dropped for a User | `quotas` spec, `04_Portal/Payments/PaymentQuotaInformationTests.cs:86-156` | TEST-CONFIRMED |

Counts: 57 rows — TEST-CONFIRMED 4, CODE-ONLY 50, NOTES-ONLY 3 (the per-plan values of `countFreeBackup`,
which plan has `restore`, and the Business-only third-party targets; the spec states the mechanism and names
the catalogue values as what the hosted offering sets). Spec: 24 requirements, 64 scenarios.

## Where the server differs from the release notes or the OpenAPI document (spec follows the server)

| Release note / document | Server |
|---|---|
| v3.5: "ThirdParty section is available only on the Business tariff" | Client rule. `startBackup` and `createBackupSchedule` accept `ThirdPartyConsumer` on any tariff; only the free allowance / paid service decides whether the backup may start. |
| v3.5: "only the payer may connect the service" | The wallet settings endpoint decides that (out of scope). `startBackup` by any DocSpace admin switches the service on by itself once a billing session opens (v4.0 "backup without directly enabling the service"). |
| v3.5 / v3.7: wallet warnings, "top up wallet" dialog, backups-possible counter in the card | Client surfaces. The server exposes `getBackupsCounts`, `getBackupsServiceState` and the 402 refusals. |
| v3.1: error block in the backup pages | Client behaviour. |
| v2.5: Space management "back up / restore all portals" | The `dump` parameter of the same operations on a standalone installation with unrestricted space access. |
| yaml `startBackup` / `createBackupSchedule`: 400 for "a dump was requested on a portal that is not a standalone installation" | Unreachable: `DemandAccessSpacePermissionAsync` runs first and throws `SecurityException` (403) on every non-standalone portal (`BackupController.cs:258-261, 277-280`; `TenantExtra.cs:108-114`). The spec states 403. |
| yaml `getRestoreProgress` "the only operation that needs no authorization" | Also exempt from the payment filter and from the restoring-state filter (`TenantStatusFilter.cs:41`). |
| Test name `GetWalletService_Owner_Backup_ReturnsPricedService` | Assertion matches the name (price > 0, `serviceName` "backup"). |

## Left out of the spec, and why

- **Wallet, prices, top-up, enabling the service through the wallet settings, transaction history**
  (`04_Portal/Payments/*`, `PaymentsController.SetTenantWalletSettings`): out of scope; only "is the
  backup allowed" is used.
- **Portal transfer between regions** (`TransferProgressItem`, `StartTransferAsync`, the migration
  letters): a hosted-only operation without an API endpoint in this document.
- **Server-wide dump internals** (`DoDump`, `mysql` upgrade scripts, `RestoreFromDump`, license reset
  after a dump restore): the dump is covered only as the `dump` parameter and the space-access rule.
- **Backup worker sizing** (`backup.json`: `Limit` 100000 rows per select, 4 backup workers, 1
  scheduler worker, 7-day temp-file cleaner): configuration, not behaviour.
- **Audit event names and letter texts** beyond the ones named (`BackupStarted`, `ScheduledBackupStarted`,
  `BackupCompleted`, `RestoreStarted`, `CustomerOperationPerformed`): see `history-and-audit`.
- **Which plan carries which feature** (Startup 0 / Business 2 free backups, `restore` on Business): catalogue
  data of the hosted offering, stated only as what the notes say.
- **`DistributedTaskStatus` numeric values, `BackupStorageType` numbers**: enum numbers are kept out of the
  spec by convention; the yaml lists them.

## Suspected defects

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| B1 | `deleteBackup/{id}` | An id that matches no record dereferences a null record (`backupRecord.TenantId`) and answers 500 instead of 404; the yaml documents the 500. | `BackupService.cs:129-130`; yaml `deleteBackup` 500 |
| B2 | `startBackup` / `createBackupSchedule` with `dump` | The documented 400 "dump on a non-standalone portal" is dead: the space-access demand two lines earlier already answers 403 on every hosted portal. | `BackupController.cs:258-261, 277-280`; `TenantExtra.cs:110` |
| B3 | `startBackupRestore` | `backupId` equal to the all-zero GUID parses as a GUID, so neither the record branch nor the `filePath` branch runs; the job is queued with no source and fails only in the worker (reported through `getRestoreProgress.error`). | `BackupService.cs:258-291`; yaml `StartBackupRestoreRequestDto.backupId` |
| B4 | `startBackupRestore` from a Documents file | The archive file is checked with the create right on its folder (`CanCreateAsync`), although reading the file is all a restore needs; an admin who can read but not write the folder is refused. | `BackupService.cs:178-200` |
| B5 | `backupFileUpload.ashx` | Every failure, a permission refusal included, is answered 200 with `success:false`; an anonymous or unprivileged caller cannot tell a 403 from a validation error. | `BackupFileUploadHandler.cs:53-60, 140-148` |
| B6 | `cancelBackup` | The cancelled job is dequeued, so the `Canceled` status and the "backup cancelled" warning that the job sets are never observable through `getBackupProgress`; a client only sees the job vanish. The paid billing session of a cancelled job is closed by the job itself only if it was already running. | `BackupWorker.cs:172-184`; `BackupProgressItem.cs:278-283` |
| B7 | `getBackupsCount` / free allowance | The counters count records including removed ones (`Removed` is not filtered), so deleting a backup does not give the free allowance back; consistent with the document, but a deleted failed-upload record also counts. | `BackupRepository.cs:231-242` |
| B8 | Free allowance checker | `CheckUsed` is a no-op left from "the transition period from free to paid backup" with a TODO to remove it; the tariff self-check therefore never reports the feature. | `CountFreeBackupFeature.cs:52-57` |
| B9 | Scheduler | Any billing or quota failure deletes the schedule outright (not suspended), so a temporarily empty wallet silently ends the automatic backups; the admins get one letter. The yaml describes it. | `BackupSchedulerService.cs:157-164` |
| B10 | `getBackupHistory` | A read deletes records (soft `Removed`) and may skip unreachable storages, so the list can shrink between two calls without anybody deleting anything; a disconnected third-party record stays invisible but still counts in the allowance. | `BackupService.cs:202-227` |
| B11 | `backup/backuptmp` | A hidden read returns the absolute temporary folder of the worker to any DocSpace admin (path disclosure, no functional use in the client). | `BackupController.cs:644-656` |
| B12 | Restore | The portal-wide 403 while restoring is a path-suffix allowlist (`portal`, `settings`, …); any route ending in one of those words passes, and the lists carry a TODO. | `TenantStatusFilter.cs:40-44, 72-83` |
