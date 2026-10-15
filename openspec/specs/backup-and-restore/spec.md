# backup-and-restore Specification

## Purpose

Defines how a portal is backed up and restored: the manual backup (start, progress, cancel, the
storage it is written to, what the archive holds and what it leaves out, the warning about files
missing during the backup, the failure notification), the backup history and the deletion of
backups, the scheduled backup (one schedule per portal, its period, the number of copies kept, its
storage, the background scheduler and cleaner), the restore (from a history record or an uploaded
archive, who is notified, what the portal does while it is being restored and afterwards), who may
do each of these by user type and tariff, the download link of a backup and the settings the API
exposes. Billing and wallet mechanics are out of scope except where they decide whether an operation
is allowed.

Sources: the unit tests of `common/Tests/ASC.Data.Backup.Core.Tests` (what the archive covers and
how rows are remapped on restore), the wallet tests of `web/ASC.Web.Api.Tests/Tests/04_Portal/Payments`,
the OpenAPI contract `sdk/docspace-api-spec/docspace-backend.yaml` (operations are named by their
`operationId`), the product code of the backup service and its worker, and the product release notes
(v2.5, v3.5, v3.6, v3.7, v4.0). No integration test exercises the backup endpoints, so most rows
are confirmed from the code; the evidence for every scenario, the places where the release notes or
the OpenAPI document and the server disagree, and the suspected defects are listed in
`verification.md` next to this file. HTTP status codes are stated as the code produces them.

## Requirements

### Requirement: Who may use the backup operations

Every backup operation SHALL require the portal-settings right (the owner and DocSpace admins): a
Room admin, a User or a Guest is refused with 403 "No permissions to perform this action", an
anonymous caller with 401. On a hosted (SaaS) portal the same 403 applies while the installation
hides the Backup settings section. Only `getRestoreProgress` is open to anonymous callers. `dump`
(the whole server) additionally requires the space-access right, which exists on a standalone
installation only.

#### Scenario: Room admin starts a backup

- **WHEN** a Room admin, a User or a Guest calls `startBackup`, `getBackupHistory` or `createBackupSchedule`
- **THEN** the system answers 403 "No permissions to perform this action"

#### Scenario: Server-wide backup on a hosted portal

- **WHEN** the owner of a hosted portal calls `startBackup`, `getBackupSchedule` or `getBackupHistory` with `dump: true`
- **THEN** the system answers 403, because the space-access right does not exist outside a standalone installation

#### Scenario: Server-wide backup on a standalone installation with restricted space

- **WHEN** the owner calls an operation with `dump: true` on a standalone installation whose space access is limited
- **THEN** the system answers 403

### Requirement: Payment state of the portal

While the tariff of a hosted portal is in the not-paid state, the operations that read or change
the schedule and the history (`getBackupSchedule`, `createBackupSchedule`, `deleteBackupSchedule`,
`getBackupHistory`, `deleteBackup`, `deleteBackupHistory`, `startBackupRestore`) SHALL answer 402.
`startBackup`, `cancelBackup`, `getBackupProgress`, `getRestoreProgress`, `getBackupsCount`,
`getBackupsCounts` and `getBackupsServiceState` SHALL stay reachable regardless of the payment
state.

#### Scenario: Not-paid portal reads its history

- **WHEN** the owner of a hosted portal whose tariff is not paid calls `getBackupHistory`
- **THEN** the system answers 402

#### Scenario: Not-paid portal starts a backup

- **WHEN** the owner of the same portal calls `startBackup`
- **THEN** the request passes the payment check and is judged by the free-allowance rule below

### Requirement: Starting a manual backup

`startBackup` SHALL queue one backup job for the portal and answer 200 with the job: `taskId`,
`progress` 0, `status` Created; the worker writes the archive later. `storageType` defaults to
`Documents`. `Documents` needs an integer `folderId`, `ThridpartyDocuments` a non-integer one (a
mismatch is 400 "wrong folder type"), `Local` needs `filePath` and is standalone-only,
`ThirdPartyConsumer` needs `module` plus the consumer keys, `DataStore` needs nothing.

#### Scenario: Owner backs up into My documents

- **WHEN** the owner calls `startBackup` with `storageType: Documents` and `storageParams: [{key: folderId, value: "<folder id>"}]`
- **THEN** the system answers 200 with a `taskId`, `progress` 0 and `status` Created
- **AND** the same `taskId` becomes the `id` of the history record once the archive is stored

#### Scenario: Folder id of the wrong kind

- **WHEN** `storageType` is `Documents` and `folderId` is not an integer, or `storageType` is `ThridpartyDocuments` and `folderId` is an integer
- **THEN** the system answers 400 "wrong folder type"

#### Scenario: Local storage on a hosted portal

- **WHEN** the owner of a hosted portal calls `startBackup` with `storageType: Local`
- **THEN** the system answers 500 "Access denied"

#### Scenario: Second start while a job is queued

- **WHEN** `startBackup` is called while a backup job of the portal is still queued or running
- **THEN** no second job is created and the state of the existing job is returned

### Requirement: Target folder of a Documents backup

When the archive goes to `Documents` or `ThridpartyDocuments`, the target folder SHALL exist (404
"Folder not found" otherwise), SHALL NOT be the Rooms, Room templates or Archive root, and the caller
SHALL have the right to create content in it (403 "You don't have enough permission to create"
otherwise). For `Documents` the portal SHALL also be under its storage allowance: when the portal
already uses more than `maxTotalSize`, the backup is refused with 402.

#### Scenario: Backup into the Rooms root

- **WHEN** the owner names the Rooms root, the Archive root or the Room templates root as `folderId`
- **THEN** the system answers 403

#### Scenario: Backup into a folder the caller cannot write

- **WHEN** a DocSpace admin names a folder in another member's My documents where they cannot create files
- **THEN** the system answers 403

#### Scenario: Portal already above its storage allowance

- **WHEN** the portal total exceeds `maxTotalSize` and the backup goes to `Documents`
- **THEN** the system answers 402

### Requirement: Free monthly allowance and the paid backup service

On a hosted portal the tariff SHALL carry a number of free backups per calendar month
(`countFreeBackup`; the hosted catalogue gives 0 to Startup and Non-profit, 2 to Business). Beyond
it `startBackup` SHALL open a paid billing session and switch the backup service on; with no
billing customer it answers 402 "Your pricing plan does not support this option", with a wallet
that cannot be charged 402 "Payment required". A standalone installation and a not-paid tariff
SHALL have no limit.

#### Scenario: Free allowance available

- **WHEN** fewer backups than `countFreeBackup` were recorded for the portal since the first day of the current month (UTC)
- **THEN** the backup is started as a free one and `getBackupsCounts.free` grows by one when it completes

#### Scenario: Allowance used up and the service cannot be charged

- **WHEN** the allowance of the month is used up and the portal has no billing customer, or the wallet cannot cover one backup
- **THEN** the system answers 402 and nothing is queued

#### Scenario: Allowance used up and the wallet covers it

- **WHEN** the allowance is used up and a billing session opens
- **THEN** the backup is queued as a paid one, `getBackupsServiceState.enabled` becomes true, and the paid counter grows when the backup completes

#### Scenario: Portal blocked for non-payment

- **WHEN** the tariff of the portal is in the not-paid state
- **THEN** the allowance is not checked and the backup is free

### Requirement: Progress and result of a backup job

`getBackupProgress` SHALL report the job of the portal (`dump: true` for the server): 200 with no
`response` member when there is no job; while running `isCompleted` false, `progress` up to 100 and
`error` empty; afterwards `isCompleted` true and `status` Completed, Canceled or Failted with a
non-empty `error`. The progress SHALL also go to the backup socket room, which only admins may
join. A completed manual backup e-mails its starter.

#### Scenario: No job for the portal

- **WHEN** the owner calls `getBackupProgress` on a portal that never started a backup
- **THEN** the system answers 200 with an empty payload

#### Scenario: Job finished

- **WHEN** the worker has stored the archive
- **THEN** `getBackupProgress` answers `isCompleted` true, `progress` 100, `status` Completed, `error` ""
- **AND** the account that started the backup receives the "backup created" letter and the audit trail records "backup completed"

#### Scenario: Progress on the socket

- **WHEN** the worker advances the job
- **THEN** `backup-progress` with the percentage and finally `end-backup` with the result are emitted to the `{tenant}-backup` room (`backup` for a server-wide job)
- **AND** a member who is not an admin is not joined to that room

### Requirement: Warning about files missing during the backup

When a stored file cannot be read while the archive is written and it no longer exists in the
database (deleted during the backup), the backup SHALL finish and report it: the archive carries a
`storage/missing_info` entry listing those files and `getBackupProgress.warning` reads "Some files
were not included in the backup. For more details, please check storage/missing_info" (v3.6). A
file still in the database but unreadable SHALL fail the backup.

#### Scenario: File deleted from the Trash during the backup

- **WHEN** a file is deleted permanently while the backup is copying the storage
- **THEN** the job completes with `warning` set and the file is listed in `storage/missing_info` of the archive

#### Scenario: File present but unreadable

- **WHEN** a file that still exists in the database cannot be read from the storage
- **THEN** the job fails and `error` is set

### Requirement: Failed and cancelled backups

When a backup job fails, the server SHALL e-mail the "backup failed" letter with the error message
to the account that started it (to every active DocSpace admin for a scheduled backup), record
"backup failed" in the audit trail and close the paid billing session (v3.6). `cancelBackup` SHALL
drop the job of the portal from the queue and answer true, or false when there is nothing to cancel;
a cancelled job is no longer reported by `getBackupProgress` and no failure letter is sent (v3.7).

#### Scenario: Backup fails in the worker

- **WHEN** the worker cannot write the archive
- **THEN** `getBackupProgress` answers `isCompleted` true, `status` Failted and the error message in `error`
- **AND** the starter receives the failure letter and no paid session is completed

#### Scenario: Owner cancels a running backup

- **WHEN** the owner calls `cancelBackup` while a job runs
- **THEN** the system answers true, the audit trail records "backup cancelled", and a following `getBackupProgress` reports no job

#### Scenario: Nothing to cancel

- **WHEN** `cancelBackup` is called with no job queued
- **THEN** the system answers 200 with false

### Requirement: Contents of a portal archive

A portal archive SHALL hold the rows of the portal from every table declared by a backup module
(tenants, audit, files, web studio settings, core users and groups, identity, AI) and the files of
the allowed storage modules (documents, logos, white label, room logos, web plugins, MCP icons).
It SHALL leave out thumbnails, earlier archives stored in Documents, and tables tied to the source
installation or transient (short links, account links, tariff rows, notify and event queues).

#### Scenario: Every tenant table is accounted for

- **WHEN** the tables with a tenant column of the schema are compared with the declared ones
- **THEN** each is either declared, intentionally skipped with a reason, or listed as a known gap

#### Scenario: Earlier backups are not backed up again

- **WHEN** the portal keeps earlier archives in a Documents folder
- **THEN** those files are excluded from the new archive and their size is subtracted from the stored quota rows inside it

### Requirement: Backup history

`getBackupHistory` SHALL list the backups of the portal whose archive still exists in its storage:
`id` (the `taskId` of the start), `fileName` (alias plus start time; `workspace` for the server),
`storageType`, `createdOn` (UTC) and `expiresOn` (one day after creation for `DataStore`,
`0001-01-01` otherwise). A record whose archive is gone SHALL be dropped by the read; one whose
storage cannot be reached SHALL be neither listed nor dropped. The order is unspecified.

#### Scenario: Archive deleted by hand

- **WHEN** the archive file of a Documents backup was moved to the Trash or deleted
- **THEN** the next `getBackupHistory` omits the record and marks it removed

#### Scenario: Third-party storage disconnected

- **WHEN** the third-party account a backup was written to is disconnected
- **THEN** the record is not listed and stays in the history

### Requirement: Deleting backups

`deleteBackup/{id}` SHALL mark the record removed and then delete the archive, answering true; a
record of another portal is left untouched and the call still answers true; an unknown id answers
500. `deleteBackupHistory` SHALL walk every record of the portal (or of the server with `dump`),
remove each record and its archive, swallow failures and always answer true; the schedule is kept.

#### Scenario: Owner deletes one backup

- **WHEN** the owner calls `deleteBackup` with the id of a record of the portal
- **THEN** the system answers 200 true and the record is no longer listed by `getBackupHistory`

#### Scenario: Id of another portal

- **WHEN** the owner calls `deleteBackup` with the id of a record of another portal
- **THEN** the system answers 200 true and nothing is deleted

#### Scenario: Owner clears the history

- **WHEN** the owner calls `deleteBackupHistory`
- **THEN** the system answers 200 true, the records and archives are deleted and the schedule is unchanged

### Requirement: Backup counters and service state

`getBackupsCount` SHALL count the history records (deleted ones included) of the portal created in
the period, `paid` choosing the paid (true) or the free (false, default) ones; `getBackupsCounts`
returns both as `free` and `paid`. The period defaults to the first day of the current month at
00:00 UTC up to now, both bounds inclusive; `from` after `to` is 400. `getBackupsServiceState`
SHALL report `enabled`: whether the paid backup service is on in the wallet settings of the portal.

#### Scenario: Default period

- **WHEN** the owner calls `getBackupsCount` with no parameters after two free backups this month
- **THEN** the system answers 2

#### Scenario: Inverted period

- **WHEN** `from` is later than `to`
- **THEN** the system answers 400

#### Scenario: Service state after a paid backup

- **WHEN** the paid service was switched on by a backup beyond the allowance
- **THEN** `getBackupsServiceState` answers `enabled: true`

### Requirement: Creating the backup schedule

`createBackupSchedule` SHALL store the single schedule of the portal, replacing the previous one:
`storageType` and `storageParams` as for `startBackup`, `backupsStored` 1 to 30 (default 1; 400
outside), `cronParams` with `period` (EveryDay, EveryWeek, EveryMonth), `hour` 0-23 and `day` (1-7
for a week, 1-31 for a month, omitted for a day). An invalid period, hour or day, including a
weekly or monthly schedule without `day`, is refused with 500. The folder is checked as for a
backup.

#### Scenario: Daily schedule into a Documents folder

- **WHEN** the owner posts `storageType: Documents`, `storageParams: [{key: folderId, value: "<id>"}]`, `backupsStored: 5`, `cronParams: {period: EveryDay, hour: 2}`
- **THEN** the system answers 200 true and `getBackupSchedule` returns the schedule with `backupsStored` 5 and `cronParams.day` 0

#### Scenario: Too many stored copies

- **WHEN** `backupsStored` is 0 or 31
- **THEN** the system answers 400 "backupStored must be 1 - 30"

#### Scenario: Weekly schedule without a day

- **WHEN** `cronParams` is `{period: EveryWeek, hour: 2}`
- **THEN** the system answers 500, because the omitted day is stored as 0 which a weekly schedule does not accept

### Requirement: Scheduled backups require the allowance or the paid service

On a hosted portal `createBackupSchedule` SHALL be refused with 402 "Your pricing plan does not
support this option" when the tariff has no free backups (`countFreeBackup` 0) and the paid backup
service is not switched on; the installation may also hide automatic backups entirely (403). A
standalone installation has no such limit.

#### Scenario: Startup portal without the service

- **WHEN** the owner of a portal with `countFreeBackup` 0 and the backup service off calls `createBackupSchedule`
- **THEN** the system answers 402

#### Scenario: Business portal

- **WHEN** the owner of a portal with `countFreeBackup` 2 calls `createBackupSchedule`
- **THEN** the schedule is saved

### Requirement: Reading and deleting the schedule

`getBackupSchedule` SHALL answer 200 with the schedule (`storageType`, `storageParams` as an
object, `cronParams`, `backupsStored` or null for unlimited, `lastBackupTime`, `dump`) or 200 with
no `response` member when none is set; except for `ThirdPartyConsumer` the `folderId` of the answer
is the stored base path. `deleteBackupSchedule` SHALL delete the schedule, keep the archives it
produced, record the deletion in the audit trail and answer true even when there was none.

#### Scenario: No schedule

- **WHEN** the owner calls `getBackupSchedule` on a portal without one
- **THEN** the system answers 200 with an empty payload

#### Scenario: Schedule deleted twice

- **WHEN** the owner calls `deleteBackupSchedule` twice
- **THEN** both calls answer 200 true and the history is unchanged

### Requirement: Running the schedule

The scheduler SHALL run every 15 minutes and start the backup of every active portal whose next
run time (period and hour in the portal time zone, counted from the last run) has passed, unless
the tariff is in the grace or not-paid state. The run counts against the free allowance; beyond it
a paid session is opened only when the backup service is on. When the backup is not allowed, the
schedule SHALL be deleted and every active DocSpace admin e-mailed "scheduled backup failed".

#### Scenario: Scheduled run on a Business portal

- **WHEN** the hour of a daily schedule passes and the allowance of the month is not used up
- **THEN** a backup is queued as scheduled, `lastBackupTime` is updated, the audit trail records "scheduled backup started" by the backup service, and no completion letter is sent

#### Scenario: Allowance used up, service off

- **WHEN** the scheduler finds the allowance used up and the paid service switched off
- **THEN** the schedule is deleted, the admins receive the failure letter and `getBackupSchedule` answers an empty payload afterwards

#### Scenario: Portal in the grace period

- **WHEN** the tariff of the portal is in the grace or not-paid state
- **THEN** the scheduled backup is skipped and the schedule is kept

### Requirement: Stored copies of a schedule are pruned

The cleaner SHALL run every 15 minutes and delete, per portal with a schedule, the scheduled
archives beyond the newest `backupsStored` ones; manual backups are neither counted nor deleted.
It SHALL also delete `DataStore` backups past their `expiresOn` (one day) and drop records whose
archive no longer exists. A record whose storage provider refuses the deletion SHALL be kept and
dropped only when it is older than six months.

#### Scenario: Sixth scheduled copy

- **WHEN** a schedule keeps 5 copies and a sixth scheduled backup completes
- **THEN** the oldest scheduled archive is deleted on the next cleaner run and the manual backups of the portal stay

#### Scenario: Temporary-storage backup expires

- **WHEN** a `DataStore` backup is one day old
- **THEN** the cleaner deletes it and it disappears from the history

### Requirement: Who may restore and from what

`startBackupRestore` SHALL require the portal-settings right, the Restore section not being hidden
(403) and, on a hosted portal, the `restore` feature of the tariff (402 "Your pricing plan does not
support this option"). `backupId` names the source: a GUID picks a history record (404 when none),
anything else means `storageParams.filePath` on `storageType` (a file id for Documents, checked
like a backup target; a server path for Local). The answer is the queued job.

#### Scenario: Restore from a history record

- **WHEN** the owner of a Business portal posts `backupId: "<id from getBackupHistory>"`, `notify: true`
- **THEN** the system answers 200 with the job, the audit trail records "restore started" and the worker picks the job up

#### Scenario: Startup portal

- **WHEN** the owner of a portal whose tariff lacks the `restore` feature calls `startBackupRestore`
- **THEN** the system answers 402

#### Scenario: Unknown backup id

- **WHEN** `backupId` is a GUID that matches no record
- **THEN** the system answers 404

#### Scenario: Restore from a Documents file

- **WHEN** `backupId` is "file" and `storageParams` is `[{key: filePath, value: "<file id>"}]`
- **THEN** the archive is read from that file; a missing file answers 404

### Requirement: Uploading an archive to restore from

A standalone installation SHALL accept an archive in chunks at `backupFileUpload.ashx` for a
`Local` restore: the first request carries `Init=true`, `totalSize` and `extension` (`tar` or
`tar.gz`), later requests one chunk each up to the chunk size the server reports; the upload
replaces an earlier one and must not exceed `maxTotalSize`. The caller needs the restore right;
failures come back as `success: false`. `startBackupRestore` with `storageType: Local` SHALL
restore the uploaded file (404 when none).

#### Scenario: Archive larger than the portal allowance

- **WHEN** `totalSize` of the init request exceeds `maxTotalSize`
- **THEN** the answer is `success: false` with "Backup is larger than the total size of the portal"

#### Scenario: Chunks complete the upload

- **WHEN** the appended chunks reach `totalSize`
- **THEN** the answer carries `endUpload: true` and a `Local` restore started afterwards uses that file

### Requirement: The portal while it is being restored

When the worker starts a restore it SHALL e-mail "restore started" to every active user
(`notify: true`) or the owner alone (`notify: false`), activated accounts only, and put the portal
into the restoring state, in which every API call answers 403 except the portal and settings reads,
`getRestoreProgress` and a few branding reads. `getRestoreProgress` SHALL answer without
authentication: an empty payload without a job, otherwise `isCompleted`, `progress`, `status` and
`error`; `link` and `warning` stay empty.

#### Scenario: Member calls the files API during a restore

- **WHEN** any member calls a files, people or backup operation while the portal is in the restoring state
- **THEN** the system answers 403

#### Scenario: Anonymous poll of the progress

- **WHEN** an anonymous caller calls `getRestoreProgress` during the restore
- **THEN** the system answers 200 with `isCompleted` false and a `progress` between 10 and 75
- **AND** the same percentages are emitted as `restore-progress` to the `{tenant}-restore` socket room

#### Scenario: Restore cannot be cancelled

- **WHEN** a client looks for an operation to cancel a running restore
- **THEN** none is published; the job runs to completion or failure

### Requirement: What a restore produces

On a hosted portal the archive SHALL be one of this portal's own backups (its hash matches a
history record), otherwise the job fails with "The backup file is invalid. Please use the file
created in the same portal."; an unreadable archive fails with "The backup file is invalid." The
restore SHALL load the archive into a new tenant, keep the alias and mapped domain, carry the
history over, clear the two-factor settings, set the portal active and e-mail "restore completed".
On failure the original portal SHALL be active again.

#### Scenario: Restore completes

- **WHEN** the worker finishes a portal restore
- **THEN** `getRestoreProgress` reports `isCompleted` true and `progress` 100, the portal is active under its alias, the backups of the old portal are listed by `getBackupHistory`, and the two-factor authentication settings are reset

#### Scenario: Archive of another portal on a hosted installation

- **WHEN** the archive hash matches no record of the portal
- **THEN** the job fails with "The backup file is invalid. Please use the file created in the same portal." and the portal returns to the active state

#### Scenario: Server-wide archive on a hosted portal

- **WHEN** an archive of a whole server is restored on a hosted portal, or a portal archive is restored with `dump: true`
- **THEN** the job fails with the invalid-backup message

### Requirement: Download link of a backup

`getBackupProgress.link` SHALL be the download link of the stored archive for a backup into the
temporary storage (`DataStore`: a pre-signed link valid for one day) or a connected third-party
storage (`ThirdPartyConsumer`: an internal link). For `Documents`, `ThridpartyDocuments` and
`Local` the archive is reached through the file or the path and `link` SHALL be an empty string.
The hidden `backup/backuptmp` read SHALL return the temporary folder of the worker.

#### Scenario: Backup into the temporary storage

- **WHEN** a `DataStore` backup completes
- **THEN** `link` is a pre-signed URL that expires after one day and the record expires one day after creation

#### Scenario: Backup into Documents

- **WHEN** a `Documents` backup completes
- **THEN** `link` is "" and the archive is a new file in the target folder, marked as new

### Requirement: Storage targets offered for backups

`getAllBackupStorages` SHALL list the third-party storage consumers that can hold backups, each
with id, title, the authentication keys it expects, `isSet` and `current`; `current` is set only on
the consumer the saved schedule writes to (`ThirdPartyConsumer`), with the keys of that schedule.
It SHALL require the portal-settings right and, on a hosted portal, the Backup section not being
hidden (403). The release notes limit third-party targets to Business; the server does not check
the tariff.

#### Scenario: Schedule into Amazon S3

- **WHEN** the schedule stores backups in a third-party consumer and the owner calls `getAllBackupStorages`
- **THEN** that consumer is `current` with the keys of the schedule and every other entry is not current

#### Scenario: Schedule into Documents

- **WHEN** the schedule stores backups in Documents, or there is no schedule
- **THEN** no entry is current

### Requirement: Backup features exposed by the tariff

`getPortalQuota` SHALL expose `countFreeBackup` (free backups per month), `backup` (the paid
backup service as a wallet service), `restore` and `thirdparty` as features of the tariff, graded
for DocSpace admins in `getQuotaPaymentInformation`; `getWalletService` for the `backup` service
SHALL return a priced service.

#### Scenario: Owner reads the backup wallet service

- **WHEN** the owner reads the wallet service named `backup`
- **THEN** the system answers 200 with `serviceName` "backup", a price above 0 and a non-empty feature list

#### Scenario: Features graded by role

- **WHEN** a User reads `getQuotaPaymentInformation`
- **THEN** the backup features marked for DocSpace admins are not in the list
