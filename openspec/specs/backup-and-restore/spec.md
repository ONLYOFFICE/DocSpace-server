# backup-and-restore Specification

## Purpose

Defines how a portal is backed up and restored: the manual backup (start, progress, cancel, the
storage it is written to, the name of the archive, what the archive holds and what it leaves out, the
warning about files missing during the backup, the failure notification), the backup history and the
deletion of backups, the scheduled backup (one schedule per portal, its period, the number of copies
kept, its storage, the validation that protects a stored schedule, the background scheduler and
cleaner), the restore (from a history record or an uploaded archive, who is notified, what the portal
does while it is being restored and afterwards), who may do each of these by user type and tariff, the
download link of a backup and the settings the API exposes. Billing and wallet mechanics are out of
scope except where they decide whether an operation is allowed.

Sources: the integration tests of `common/Tests/ASC.Data.Backup.Tests` (every backup operation except
the restore itself, on a standalone test stack where no tariff limit applies), the unit tests of
`common/Tests/ASC.Data.Backup.Core.Tests` (what the archive covers and how rows are remapped on
restore), the wallet tests of `web/ASC.Web.Api.Tests/Tests/04_Portal/Payments`, the OpenAPI contract
`sdk/docspace-api-spec/docspace-backend.yaml` (operations are named by their `operationId`), the
product code of the backup service and its worker, and the product release notes (v2.5, v3.5, v3.6,
v3.7, v4.0). The hosted-only rules (402 refusals, the free allowance, the paid service, the restore
itself) are confirmed from the code; the evidence for every scenario, the places where the release
notes or the OpenAPI document and the server disagree, and the suspected defects are listed in
`verification.md` next to this file. HTTP status codes are stated as the code produces them.

## Requirements

### Requirement: Who may use the backup operations

Every backup operation SHALL require the portal-settings right (the owner and DocSpace admins): a
Room admin, a User or a Guest is refused with 403 "Access denied", an anonymous caller with 401,
before the body is validated. A hosted (SaaS) portal answers the same 403 while the installation hides
the Backup settings section. Only `getRestoreProgress` is open to every caller. `dump` (the whole
server) additionally requires the space-access right, which exists on a standalone installation only.

#### Scenario: Room admin starts a backup

- **WHEN** a Room admin, a User or a Guest calls `startBackup`, `getBackupHistory`, `createBackupSchedule` or any other backup operation
- **THEN** the system answers 403 "Access denied"

#### Scenario: Anonymous caller

- **WHEN** an anonymous caller calls any backup operation except `getRestoreProgress`
- **THEN** the system answers 401

#### Scenario: Refusal comes before validation

- **WHEN** a User calls `startBackup` with a `folderId` that does not exist, or an anonymous caller sends a `storageType` outside the known values
- **THEN** the system answers 403 (401 for the anonymous caller), not 404 or 400, and nothing is queued

#### Scenario: Server-wide backup on a hosted portal

- **WHEN** the owner of a hosted portal calls `startBackup`, `getBackupSchedule` or `getBackupHistory` with `dump: true`
- **THEN** the system answers 403, because the space-access right does not exist outside a standalone installation

#### Scenario: Server-wide backup on a standalone installation with restricted space

- **WHEN** the owner calls an operation with `dump: true` on a standalone installation whose space access is limited
- **THEN** the system answers 403

### Requirement: Payment state of the portal

While the tariff of a hosted portal is not paid, `getBackupSchedule`, `createBackupSchedule`,
`deleteBackupSchedule`, `getBackupHistory`, `deleteBackup`, `deleteBackupHistory` and
`startBackupRestore` SHALL answer 402. `startBackup`, `cancelBackup`, `getBackupProgress`,
`getRestoreProgress`, `getBackupsCount`, `getBackupsCounts` and `getBackupsServiceState` SHALL stay
reachable whatever the payment state. A standalone installation SHALL never answer 402 for a backup
operation.

#### Scenario: Not-paid portal reads its history

- **WHEN** the owner of a hosted portal whose tariff is not paid calls `getBackupHistory`
- **THEN** the system answers 402

#### Scenario: Not-paid portal starts a backup

- **WHEN** the owner of the same portal calls `startBackup`
- **THEN** the request passes the payment check and is judged by the free-allowance rule below

#### Scenario: Standalone installation

- **WHEN** the owner of a standalone installation calls `startBackup`, `createBackupSchedule` or `startBackupRestore`
- **THEN** the system does not answer 402 whatever the tariff says

### Requirement: Starting a manual backup

`startBackup` SHALL queue one backup job for the portal and answer 200 with the job: `taskId`,
`progress`, `status` Created (Running when the worker was quick) and an empty `error`; the worker
writes the archive later, and the `taskId` becomes the `id` of the history record. `storageType`
defaults to `Documents`. `Documents` needs an integer `folderId`, `ThridpartyDocuments` a
non-integer one, `Local` needs `filePath` and is standalone-only, `ThirdPartyConsumer` needs
`module` plus the consumer keys, `DataStore` nothing.

#### Scenario: Owner backs up into a room

- **WHEN** the owner or a DocSpace admin calls `startBackup` with `storageType: Documents` and `storageParams: [{key: folderId, value: "<room id>"}]`
- **THEN** the system answers 200 with a `taskId` and the job completes without an error
- **AND** the same `taskId` is the `id` of the record `getBackupHistory` lists afterwards

#### Scenario: Backup into the temporary storage

- **WHEN** the owner calls `startBackup` with `storageType: DataStore` and no `storageParams`
- **THEN** the system answers 200 and the job completes with `status` Completed and `progress` 100

#### Scenario: Backup into a third-party storage

- **WHEN** a Nextcloud account is connected for backups and `storageType` is `ThridpartyDocuments` with its folder id, or the S3 keys are saved and `storageType` is `ThirdPartyConsumer` with `module` "s3" and the bucket keys
- **THEN** the job completes without an error

#### Scenario: Second start while a job is queued

- **WHEN** `startBackup` is called while a backup job of the portal is still queued or running
- **THEN** no second job is created and the state of the existing job is returned with its `taskId`
- **AND** when the first job has already finished the start creates a new job with a new `taskId`; either way every job that completed has exactly one history record

### Requirement: Bodies startBackup refuses

`startBackup` SHALL answer 400 and queue nothing when `folderId` is missing, blank, not an integer
or above the integer range for `Documents` (the default type), when it is an integer for
`ThridpartyDocuments`, when `storageParams` repeats a key, when an item of `storageParams` is null
or has no key or no value, when the body is empty or `{}`, or when a field has the wrong JSON type
(the answer then names the field). `Local` without `filePath`, or on a hosted portal, is 500.

#### Scenario: Folder id of the wrong kind

- **WHEN** `storageType` is `Documents` and `folderId` is "invalid_value", "1.5", "" or above the integer range, or `storageType` is `ThridpartyDocuments` and `folderId` is a room id
- **THEN** the system answers 400 and the queue, the history and the counters are unchanged

#### Scenario: Item without a key or a value

- **WHEN** `storageParams` holds a null item, an item with a null key, or an item with a null value, or a `ThridpartyDocuments` request carries no `storageParams`
- **THEN** the system answers 400 and nothing is queued

#### Scenario: Field of the wrong type

- **WHEN** `storageType` is a string, a fraction, a boolean or an array, `storageParams` is an object, a string or a number, or `dump` is a string
- **THEN** the system answers 400 with the name of the field that could not be read (`$.storageType`, `$.storageParams`, `$.dump`)

#### Scenario: Local storage on a hosted portal

- **WHEN** the owner of a hosted portal calls `startBackup` with `storageType: Local`
- **THEN** the system answers 500 "Access denied"

### Requirement: Storages the job cannot write to

`startBackup` SHALL NOT validate the storage itself beyond the folder checks: a `ThirdPartyConsumer`
request without `module` or with an unknown one, the unsupported `CustomCloud` type and a
`storageType` outside the known values are accepted with 200 and the job fails in the worker. The
failed job SHALL report a non-empty `error` through `getBackupProgress`, leave no history record and
SHALL NOT count against the allowance; the next backup SHALL start normally.

#### Scenario: Consumer without a module

- **WHEN** the owner calls `startBackup` with `storageType: ThirdPartyConsumer` and no `module`, or with `module: "no-such-module"`
- **THEN** the system answers 200 with a `taskId`, the job ends with `isCompleted` true, a non-empty `error` and an empty `link`, and `getBackupHistory` stays empty

#### Scenario: Backup after a failed job

- **WHEN** a backup is started after a job failed
- **THEN** it completes, the earlier records are untouched and the counters count only the completed backups

### Requirement: Target folder of a Documents backup

When the archive goes to `Documents` or `ThridpartyDocuments`, the target folder SHALL exist (404
"Folder not found", also for a third-party connection that does not exist), SHALL NOT be the Rooms,
Room templates or Archive root, and the caller SHALL have the right to create content in it (403
otherwise). For `Documents` the portal SHALL also be under its storage allowance: when it already
uses more than `maxTotalSize`, the backup is refused with 402.

#### Scenario: Folder that does not exist

- **WHEN** `folderId` is "0", "-1", "2147483647" or "99999999", or `storageType` is `ThridpartyDocuments` with `folderId` "webdav-99999"
- **THEN** the system answers 404 and nothing is queued

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

#### Scenario: Standalone installation

- **WHEN** the owner of a standalone installation runs several backups in one month
- **THEN** every one of them is counted as free and `getBackupsServiceState.enabled` does not change

### Requirement: Progress and result of a backup job

`getBackupProgress` SHALL report the job of the portal (`dump: true` for the server): 200 with no
`response` member when there is no job; while running `isCompleted` false, `progress` never
decreasing, `error` and `link` empty; afterwards `isCompleted` true, `progress` 100 and `status`
Completed, Canceled or Failted (`error` set). Every poll SHALL name the `taskId` of the start; a
finished job SHALL stay in the queue until a new start or a cancel replaces it. The progress SHALL
also go to the backup socket room, which only admins may join.

#### Scenario: No job for the portal

- **WHEN** the owner calls `getBackupProgress` on a portal that never started a backup
- **THEN** the system answers 200 with an empty payload

#### Scenario: Job finished

- **WHEN** the worker has stored the archive
- **THEN** `getBackupProgress` answers `isCompleted` true, `progress` 100, `status` Completed, `error` "" and the same `taskId` as the start
- **AND** two further polls return the same job, and the account that started the backup receives the "backup created" letter and the audit trail records "backup completed"

#### Scenario: Reads during a backup

- **WHEN** `getBackupHistory`, `getBackupProgress`, `getBackupsCount`, `getBackupsCounts`, `getBackupSchedule` and `getBackupsServiceState` are called at once while a backup runs
- **THEN** every read answers 200, the history holds the earlier records and at most the new one, and once the job is done the history and the counters agree

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

When a backup job fails, the server SHALL e-mail the "backup failed" letter to the account that
started it (to every active DocSpace admin for a scheduled backup), record "backup failed" in the
audit trail and close the paid billing session (v3.6). `cancelBackup` SHALL drop the job of the
portal from the queue, running or finished, and answer true, or false when the queue holds none; a
cancelled job leaves no archive, shows as Canceled or not at all, gets no failure letter (v3.7), and
the archives and counters are untouched.

#### Scenario: Backup fails in the worker

- **WHEN** the worker cannot write the archive
- **THEN** `getBackupProgress` answers `isCompleted` true, `status` Failted and the error message in `error`
- **AND** the starter receives the failure letter and no paid session is completed

#### Scenario: Owner cancels a running backup

- **WHEN** the owner or a DocSpace admin calls `cancelBackup` while the job is Running
- **THEN** the system answers true, the audit trail records "backup cancelled", the job settles as Canceled or disappears from the queue, and `getBackupHistory` never lists it

#### Scenario: Cancel after the job finished

- **WHEN** `cancelBackup` is called while the finished job is still in the queue
- **THEN** the system answers true, `getBackupProgress` reports no job, the archive stays in the history and the counters are unchanged, and a second `cancelBackup` answers false

#### Scenario: Nothing to cancel

- **WHEN** `cancelBackup` is called with no job queued
- **THEN** the system answers 200 with false and the next backup starts normally

#### Scenario: Two cancels at once

- **WHEN** two `cancelBackup` calls arrive together for one job
- **THEN** both answer 200, at least one of them true, and the job settles as Canceled or completes with its record; it is never left running

#### Scenario: Cancel before the worker has the job

- **WHEN** `cancelBackup` is called right after `startBackup`, before the worker has registered the job
- **THEN** the call may answer true while the job still runs to the end and is archived, or false when the queue was empty; either way the queue is not left stuck

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
`id` (the `taskId` of the start), `fileName`, `storageType`, `createdOn` (UTC, no zone designator)
and `expiresOn` (one day after creation for `DataStore`, `0001-01-01` otherwise). A fresh portal
gets an empty list; each completed backup adds one record. A record whose archive is gone SHALL be
dropped by the read; one whose storage cannot be reached SHALL be neither listed nor dropped. The
order is unspecified.

#### Scenario: Two backups listed

- **WHEN** two `DataStore` backups completed and the owner calls `getBackupHistory`
- **THEN** the system answers 200 with `count` 2 and two records whose `id`s are the two task ids, each with a non-empty `fileName`, `storageType` DataStore, `createdOn` within the last day and `expiresOn` after `createdOn`

#### Scenario: Archive deleted by hand

- **WHEN** the archive file of a Documents backup was moved to the Trash or deleted
- **THEN** the next `getBackupHistory` omits the record and marks it removed

#### Scenario: Third-party storage disconnected

- **WHEN** the third-party account a backup was written to is disconnected
- **THEN** the record is not listed and stays in the history

### Requirement: Archive name of a backup

The archive SHALL be named `<alias>_<start time to the second>` with the extension of the storage
(`workspace_...` for the server). When a backup of the same portal already carries that name — two
backups started within one second — the new one SHALL get the first free index, `name(1)`, `name(2)`,
so that no two archives share a path and deleting one never deletes another. A name once used is
never reused, deleted records included.

#### Scenario: Three backups back to back

- **WHEN** three `DataStore` backups are started each as soon as the previous one finished
- **THEN** `getBackupHistory` lists all three with distinct `fileName`s

#### Scenario: Deleting one of them

- **WHEN** the middle one of those three backups is deleted with `deleteBackup`
- **THEN** the other two stay in the history

### Requirement: Deleting backups

`deleteBackup/{id}` SHALL mark the record removed and then delete the archive, answering true; a
record of another portal or one already removed is left alone and the call still answers true; an
unknown id answers 404, as does a route value that is not a GUID. Of several deletes of one backup
at once exactly one deletes the archive. `deleteBackupHistory` SHALL walk every record of the portal
(or of the server with `dump`), remove each record and its archive, swallow failures and always
answer true; the schedule is kept.

#### Scenario: Owner deletes one backup

- **WHEN** the owner or a DocSpace admin calls `deleteBackup` with the id of a record of the portal
- **THEN** the system answers 200 true, the record is no longer listed by `getBackupHistory` and every other record is unchanged

#### Scenario: Unknown id

- **WHEN** the owner calls `deleteBackup` with a GUID that matches no record, the all-zero GUID included
- **THEN** the system answers 404 and the queue, the history and the counters are unchanged

#### Scenario: Id that is not a GUID

- **WHEN** the route value is "abc", "-1", "0", a 5000-character string, "null" or empty
- **THEN** the system answers 404 and nothing changes

#### Scenario: Same backup deleted twice

- **WHEN** `deleteBackup` is called twice with the same id, in a row or at the same time
- **THEN** both calls answer 200 true and the history ends with exactly the other records

#### Scenario: Id of another portal

- **WHEN** the owner calls `deleteBackup` with the id of a record of another portal
- **THEN** the system answers 200 true and nothing is deleted

#### Scenario: Delete while a new backup is written

- **WHEN** a backup is deleted while another one is being written
- **THEN** the new backup completes and the history lists the kept record and the new one

#### Scenario: Owner clears the history

- **WHEN** the owner or a DocSpace admin calls `deleteBackupHistory`
- **THEN** the system answers 200 true, `getBackupHistory` answers an empty list with `count` 0, and the schedule is unchanged

### Requirement: Backup counters and service state

`getBackupsCount` SHALL count the history records (deleted ones included) of the portal created in
the period, `paid` choosing the paid (true) or the free (false, default) ones; `getBackupsCounts`
returns both as `free` and `paid` and ignores `paid`. The period defaults to the first day of the
current month at 00:00 UTC up to now, both bounds inclusive; `from` after `to` or a bound that is
not a date is 400. `getBackupsServiceState` SHALL report `enabled`: whether the paid backup service
is on in the wallet settings.

#### Scenario: Default period

- **WHEN** the owner calls `getBackupsCount` with no parameters after one backup this month
- **THEN** the system answers 1, the same as for the explicit period from the first of the month to tomorrow, and `getBackupsCounts` answers `{free: 1, paid: 0}` whether or not `paid` is sent

#### Scenario: Period around the backup

- **WHEN** the period contains the backup, lies entirely before it, or entirely after it
- **THEN** the counters answer 1, 0 and 0; `from` equal to `to` and a `from` without `to` are accepted

#### Scenario: Inverted or unreadable period

- **WHEN** `from` is later than `to`, `to` is earlier than the default `from`, or `from` or `to` is "not-a-date"
- **THEN** both counting operations answer 400

#### Scenario: Deleted backups still count

- **WHEN** one of two backups is deleted with `deleteBackup`, then the history is cleared with `deleteBackupHistory`
- **THEN** the history shrinks to one record and then to none while `getBackupsCounts` keeps answering `{free: 2, paid: 0}`

#### Scenario: Service state after a paid backup

- **WHEN** the paid service was switched on by a backup beyond the allowance
- **THEN** `getBackupsServiceState` answers `enabled: true`

### Requirement: Creating the backup schedule

`createBackupSchedule` SHALL store the single schedule of the portal, replacing the previous one:
`storageType` and `storageParams` as for `startBackup`, `backupsStored` 1 to 30 (default 1),
`cronParams` (required) with `period` (EveryDay, EveryWeek, EveryMonth), `hour` 0-23 and `day` (1-7
for a week, 1-31 for a month, omitted for a day and stored as 0). The folder is checked as for a
backup. Sending the same schedule again changes nothing; changing one field keeps the others.

#### Scenario: Daily schedule into a room

- **WHEN** the owner or a DocSpace admin posts `storageType: Documents`, `storageParams: [{key: folderId, value: "<room id>"}]`, `backupsStored: 4`, `cronParams: {period: EveryDay, hour: 0}`
- **THEN** the system answers 200 true and `getBackupSchedule` returns the schedule with `backupsStored` 4, `cronParams.day` 0 and `storageParams.folderId` the room id

#### Scenario: Boundary values

- **WHEN** `hour` is 0 or 23, a weekly `day` is 1 or 7, a monthly `day` is 1, 29 or 31, `backupsStored` is 1 or 30, or `backupsStored` and `storageParams` are omitted for `DataStore`
- **THEN** the schedule is stored and read back with those values, `backupsStored` 1 when omitted

#### Scenario: Schedule replaced

- **WHEN** a daily schedule is replaced by a weekly one with `day` 3, then by a monthly one with `day` 20, then by a daily one again
- **THEN** each read returns the latest period, hour and day, and the daily one reports `day` 0

### Requirement: Bodies createBackupSchedule refuses

`createBackupSchedule` SHALL answer 400, keeping the stored schedule, when `cronParams` is missing,
`period` is outside the known values, `hour` is outside 0-23, a weekly or monthly schedule has no
`day` or one outside its range, `backupsStored` is outside 1-30, `storageType` cannot hold a backup,
a `ThirdPartyConsumer` schedule has no known `module`, a `Documents` or `ThridpartyDocuments`
schedule lacks `folderId` or a `Documents` one has a non-integer `folderId`, or a field has the
wrong JSON type. A missing folder is 404.

#### Scenario: Weekly schedule without a day

- **WHEN** `cronParams` is `{period: EveryWeek, hour: 3}`, or a weekly `day` is 0, 8 or -1, or a monthly `day` is 0 or 32
- **THEN** the system answers 400 and `getBackupSchedule` still returns the schedule stored before

#### Scenario: Period or hour out of range

- **WHEN** `period` is 3 or -1, or `hour` is 24 or -1, or the body has no `cronParams`
- **THEN** the system answers 400 and the stored schedule survives

#### Scenario: Too many stored copies

- **WHEN** `backupsStored` is 0, -1, 31 or above the integer range, or is a fraction or a string
- **THEN** the system answers 400 "backupStored must be 1 - 30" (the framework names the field for an unreadable value)

#### Scenario: Storage no schedule can run into

- **WHEN** `storageType` is 99 or -1, or `storageType` is `ThirdPartyConsumer` with `module` "no-such-module" or without `module`
- **THEN** the system answers 400 and the stored schedule survives

#### Scenario: Documents schedule without a usable folder

- **WHEN** a `Documents` or `ThridpartyDocuments` schedule has no `storageParams` or none named `folderId`, or a `Documents` schedule has `folderId` "invalid_value", or the body names neither `storageType` nor `folderId`
- **THEN** the system answers 400; a `folderId` "99999999" that matches no folder answers 404

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
object, `cronParams`, `backupsStored` or null for unlimited, `lastBackupTime` — `0001-01-01` until
the first run — and `dump`) or 200 with no `response` member when none is set. Except for
`ThirdPartyConsumer`, whose saved keys come back as sent, `storageParams.folderId` holds the stored
base path, null for `DataStore`. `deleteBackupSchedule` SHALL delete the schedule, keep the
archives, audit the deletion and answer true even when there was none.

#### Scenario: No schedule

- **WHEN** the owner calls `getBackupSchedule` on a portal without one
- **THEN** the system answers 200 with an empty payload and `count` 0

#### Scenario: Schedule moved between storages

- **WHEN** a `DataStore` schedule is replaced by a `Documents` one into a room and then by a `DataStore` one again
- **THEN** `storageParams.folderId` reads null, then the room id, then null; an S3 `ThirdPartyConsumer` schedule reads back exactly the `module`, `bucket`, `region`, `serviceurl`, `forcepathstyle` and `usehttp` it was saved with

#### Scenario: Schedule deleted twice

- **WHEN** the owner or a DocSpace admin calls `deleteBackupSchedule` twice, or on a portal without a schedule
- **THEN** every call answers 200 true, `getBackupSchedule` answers an empty payload afterwards, and a new schedule can be created

#### Scenario: Manual backups leave the schedule alone

- **WHEN** backups are started, cancelled and deleted and the history is cleared while a schedule is stored
- **THEN** `getBackupSchedule` returns the schedule unchanged

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

`startBackupRestore` SHALL require the portal-settings right (403 "Access denied" before the body
is read), the Restore section not being hidden (403) and, on a hosted portal, the `restore` feature
of the tariff (402). `backupId` names the source: a GUID picks a history record (404 when none),
anything else means `storageParams.filePath` on `storageType` (a file id for Documents, checked like
a backup target; a server path for Local); without `filePath` the call is 400. The answer is the
queued job.

#### Scenario: Restore from a history record

- **WHEN** the owner of a Business portal posts `backupId: "<id from getBackupHistory>"`, `notify: true`
- **THEN** the system answers 200 with the job, the audit trail records "restore started" and the worker picks the job up

#### Scenario: Startup portal

- **WHEN** the owner of a portal whose tariff lacks the `restore` feature calls `startBackupRestore`
- **THEN** the system answers 402; on a standalone installation the same call is never refused for the plan

#### Scenario: Unknown backup id

- **WHEN** `backupId` is a GUID that matches no record
- **THEN** the system answers 404

#### Scenario: Restore from a Documents file

- **WHEN** `backupId` is "file" and `storageParams` is `[{key: filePath, value: "<file id>"}]`
- **THEN** the archive is read from that file; a missing file answers 404

#### Scenario: Member starts a restore

- **WHEN** a Room admin, a User or a Guest calls `startBackupRestore`, or an anonymous caller sends any body or none
- **THEN** the system answers 403 "Access denied" (401 for the anonymous caller), no restore is running and the portal keeps answering its other operations

### Requirement: Bodies startBackupRestore refuses

`startBackupRestore` SHALL answer 400 and queue no restore when `backupId` is missing, null, empty,
or not a GUID while `storageParams` carries no `filePath` (whatever else it carries), when an item
of `storageParams` is null or has no key or no value, when the body is missing, null or `{}`, or
when `backupId`, `storageType`, `notify`, `dump` or `storageParams` has the wrong JSON type — the
answer then names the field. The portal and its rooms SHALL be untouched afterwards.

#### Scenario: Source that is not a backup

- **WHEN** `backupId` is "not-a-guid" or "" — alone, with `storageType: DataStore`, with empty, null or unrelated `storageParams`, or with a null item or one without a key
- **THEN** the system answers 400, `getRestoreProgress` reports no job and `getBackupHistory` still answers

#### Scenario: Field of the wrong type

- **WHEN** `backupId` is a number, an object, an array or a boolean, `storageType` or `dump` is a string, `notify` is a string, a number or null, or `storageParams` is an object or a string
- **THEN** the system answers 400 naming the field (`$.backupId`, `$.storageType`, `$.notify`, `$.dump`, `$.storageParams`), and a room created before the call is intact

### Requirement: Uploading an archive to restore from

A standalone installation SHALL accept an archive in chunks at `backupFileUpload.ashx` for a
`Local` restore: the first request carries `Init=true`, `totalSize` and `extension` (`tar` or
`tar.gz`), later requests one chunk each up to the chunk size the server reports; the upload
replaces an earlier one and must not exceed `maxTotalSize`. The caller needs the restore right;
failures come back as `success: false`. `startBackupRestore` with `storageType: Local` restores the
uploaded file (404 when none).

#### Scenario: Archive larger than the portal allowance

- **WHEN** `totalSize` of the init request exceeds `maxTotalSize`
- **THEN** the answer is `success: false` with "Backup is larger than the total size of the portal"

#### Scenario: Chunks complete the upload

- **WHEN** the appended chunks reach `totalSize`
- **THEN** the answer carries `endUpload: true` and a `Local` restore started afterwards uses that file

### Requirement: The portal while it is being restored

When the worker starts a restore it SHALL e-mail "restore started" to every active user
(`notify: true`) or the owner alone (`notify: false`), activated accounts only, and put the portal
into the restoring state, where every API call but the portal, settings and branding reads and
`getRestoreProgress` answers 403. `getRestoreProgress` SHALL answer every caller, anonymous
included, with 200: an empty payload without a job, whatever `dump` says, otherwise `isCompleted`,
`progress`, `status` and `error`; `link` and `warning` stay empty.

#### Scenario: Member calls the files API during a restore

- **WHEN** any member calls a files, people or backup operation while the portal is in the restoring state
- **THEN** the system answers 403

#### Scenario: Poll without a restore

- **WHEN** an anonymous caller, a Guest, a User, a Room admin or the owner calls `getRestoreProgress` on a portal that never started a restore, with `dump` omitted, false or true
- **THEN** the system answers 200 with an empty payload and `count` 0

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
history over, clear the two-factor settings, set the portal active and e-mail "restore completed";
on failure the original portal is active again.

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
temporary storage (`DataStore`: a pre-signed link valid for one day, carrying the archive name) or a
connected third-party storage (`ThirdPartyConsumer`: an internal link). For `Documents`,
`ThridpartyDocuments` and `Local` the archive is reached through the file or the path and `link`
SHALL be an empty string. The hidden `backup/backuptmp` read SHALL return the temporary folder of
the worker.

#### Scenario: Backup into the temporary storage

- **WHEN** a `DataStore` backup completes
- **THEN** `link` is a pre-signed URL that contains the `fileName` of the history record and expires after one day, and the record expires one day after creation

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
