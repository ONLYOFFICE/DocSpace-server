# data-import Specification

## Purpose

Defines the import of a portal from another product ("Data import" in the settings): the source
products the server can read — ONLYOFFICE Workspace, Nextcloud and Google Workspace — the upload of
the source backup through `migrationFileUpload.ashx`, the parse pass that inspects the backup
(`POST /api/2.0/migration/init/{migratorName}`) and reports the users, groups and archives it holds,
the import itself (`POST /api/2.0/migration/migrate`) with the chosen users, their types, the groups
and the file categories, the progress and result read from `GET /api/2.0/migration/status`, the log
download, the cancellation of a parse, the clean-up of a finished import and its closing with the
activation e-mails, who may run all of this, the one-job-per-portal rule, how existing e-mails, user
types, the storage quota and the paid-user quota are handled, and the way the jobs run in the
background. The wizard screens of the client and the LDAP import are out of scope.

Sources: the integration tests of `web/ASC.Web.Api.Tests` (`07_Migration`) and the TypeScript suite
they were ported from (only the list, status and log operations on a portal without a job, plus the
access matrix), the OpenAPI contract `sdk/docspace-api-spec/docspace-backend.yaml` (operations named
by `operationId`), the product code of the migration controller, the upload handler, the worker and
the three migrators where the tests stop, the storage and proxy configuration, and the release notes
(v2.5, v3.0, v3.6). Where the notes or the OpenAPI document and the server disagree this specification
follows the server; the evidence per scenario, the differences and the suspected defects are in
`verification.md` next to this file.

## Requirements

### Requirement: Who may use data import

Every `migration/*` operation and the backup upload SHALL require the DocSpace administrator right
(the owner or a DocSpace admin): a Room admin, a User or a Guest is refused with 403 "Access denied",
an anonymous caller with 401. No edition or tariff SHALL gate the operations themselves; the only
limits are the portal's storage quota and its user quotas, applied while the job runs.

#### Scenario: Owner and DocSpace admin read the list

- **WHEN** the owner or a DocSpace admin calls `listMigrations`, `getMigrationStatus` or `getMigrationLogs`
- **THEN** the system answers 200 (or 404 for the log of a portal without a job), never 403

#### Scenario: Room admin, User or Guest

- **WHEN** a Room admin, a User or a Guest calls `listMigrations`, `getMigrationStatus` or `getMigrationLogs`
- **THEN** the system answers 403 "Access denied"

#### Scenario: Anonymous caller

- **WHEN** an anonymous caller calls `listMigrations`, `getMigrationStatus` or `getMigrationLogs`
- **THEN** the system answers 401

### Requirement: Listing the import sources

`listMigrations` SHALL return the names of the sources the installation can import from — `Workspace`,
`Nextcloud` and `GoogleWorkspace` — as a plain list in registration order, with `count` equal to the
length of the list; the names are not localized and do not depend on the portal. The name is passed
back as `migratorName` to the parse operation, matched without regard to case.

#### Scenario: Owner lists the sources

- **WHEN** the owner calls `listMigrations`
- **THEN** the system answers 200 with a list containing `Workspace`, `Nextcloud` and `GoogleWorkspace` and `count` equal to its length

### Requirement: Uploading the source backup

The backup SHALL be uploaded to `migrationFileUpload.ashx` of the portal: a POST with `Init=true`
SHALL open a fresh upload folder, remove the previous one and answer `Success` true with the
configured `ChunkSize` (10 MB by default); every later POST SHALL append the form file to the file
named by the `Name` query (base name only) in that folder. Errors SHALL be reported as `Success`
false with a `Message`, always with HTTP 200. No upload size limit is applied here.

#### Scenario: Administrator starts an upload

- **WHEN** the owner posts to `migrationFileUpload.ashx?Init=true`
- **THEN** the answer is `Success` true with `ChunkSize` 10485760, and any folder left by an earlier upload is deleted

#### Scenario: Chunk appended

- **WHEN** the owner posts a form file to `migrationFileUpload.ashx?Name=backup.zip` after `Init`
- **THEN** the bytes are appended to `backup.zip` in the portal's upload folder and the answer is `Success` true

#### Scenario: Member without the right

- **WHEN** a User posts to `migrationFileUpload.ashx?Init=true`
- **THEN** the answer is `Success` false with `Message` "Access denied." and nothing is created

### Requirement: Archive requirements per source

The parse SHALL pick the backup from the upload folder by extension: `Workspace` takes the first `.gz`
or `.tar` file (a Workspace backup); `Nextcloud` takes the first `.zip`, which must hold a `.bak` or
`.sql` database dump and the users' `data/` folder; `GoogleWorkspace` takes every `.zip` (one or more
Takeout archives with a root `Takeout` folder), in name order. An upload without a file of the right
extension SHALL fail the job and list the uploaded files in `failedArchives`.

#### Scenario: Wrong extension for the source

- **WHEN** the owner uploads `backup.zip` and parses it with `migratorName` `Workspace`
- **THEN** the job ends with `isCompleted` true, `error` "Archive must be .gz or .tar" and `backup.zip` in `parseResult.failedArchives`

#### Scenario: Nextcloud archive without a database dump

- **WHEN** the uploaded `.zip` holds no `.bak` or `.sql` file
- **THEN** the job ends with `error` "Couldn't parse <archive> archive" and the archive in `failedArchives`

#### Scenario: Several Google Takeouts

- **WHEN** three Takeout `.zip` files are uploaded and parsed with `GoogleWorkspace`
- **THEN** every archive is read; an archive that cannot be read is listed in `failedArchives` and the others are still parsed, and only when every archive fails the job ends with "Couldn't parse archives"

### Requirement: Parsing the uploaded backup

`uploadAndInitializeMigration` SHALL only queue a parse job with the migrator named in the route and
answer 200 with no body. The job SHALL read the backup without changing the portal and report, through
`getMigrationStatus`, `parseResult` with `operation` `parse`, then `progress` 100 and `isCompleted`
true. An unknown name SHALL end the job with `error` "No such migration provider". A successful parse
keeps the uploaded files; a failed one deletes them.

#### Scenario: Parse queued

- **WHEN** the owner calls `uploadAndInitializeMigration` with `migratorName` `Nextcloud` after the upload
- **THEN** the system answers 200 immediately and `getMigrationStatus` soon reports a job with `parseResult.operation` `parse` and a rising `progress`

#### Scenario: Unknown migrator

- **WHEN** the owner calls `uploadAndInitializeMigration` with `migratorName` `Dropbox`
- **THEN** the system answers 200 and the job ends with `isCompleted` true and `error` "No such migration provider"

#### Scenario: Parse finished

- **WHEN** the parse of a readable backup ends
- **THEN** `parseResult` lists every user found with `key`, `email`, `displayName`, `firstName`, `lastName`, `userType` and `migratingFiles` (`foldersCount`, `filesCount`, `bytesTotal`), every group with `groupName` and `userUidList`, and the uploaded files stay in place for the import

### Requirement: Classifying the users found

The parse SHALL sort each source user by e-mail: a user whose e-mail no portal account carries goes to
`users`; one whose e-mail an active portal account carries goes to `existUsers`; one without an e-mail
goes to `withoutEmailUsers`. A user whose e-mail belongs to a disabled portal account SHALL be dropped
silently; two source users with one e-mail SHALL be merged with their files combined. Every user is
proposed with `userType` `User` and `shouldImport` false.

#### Scenario: E-mail already on the portal

- **WHEN** the backup holds a user with the e-mail of an existing active member
- **THEN** the user appears in `existUsers`, not in `users`

#### Scenario: User without e-mail

- **WHEN** a Nextcloud account or a Google Takeout carries no e-mail address
- **THEN** the user appears in `withoutEmailUsers` with an empty `email`, and the caller may supply one at import time

#### Scenario: Disabled member

- **WHEN** the backup holds a user whose e-mail belongs to a disabled portal account
- **THEN** the user is in none of the three lists

### Requirement: Backup size against the storage quota

When the portal has a total storage quota, the job SHALL compare the size of the uploaded data (for an
import: the extracted content, without the archive itself) with that quota before parsing or importing
and SHALL end with `error` "Backup is larger than the total size of the portal" when it is bigger. A
portal without a storage limit is not checked.

#### Scenario: Backup bigger than the quota

- **WHEN** the owner of a portal with a 1 GB storage quota parses a 2 GB upload
- **THEN** the job ends with `isCompleted` true and `error` "Backup is larger than the total size of the portal", and the upload is deleted

### Requirement: Reading the job state

`getMigrationStatus` SHALL answer 200 with the portal's one job — `progress` (0 to 100), `error` (empty
while nothing failed, else the message that ended the job), `isCompleted` and `parseResult` — or with
an empty result when the portal holds no job (never started, cancelled, cleared or finished). The
`parseResult.operation` value, `parse` or `migration`, SHALL tell the two kinds of job apart.

#### Scenario: Fresh portal

- **WHEN** the owner calls `getMigrationStatus` on a portal where nothing was ever started
- **THEN** the system answers 200 with an empty result

#### Scenario: Job failed

- **WHEN** a parse or import ended with an exception
- **THEN** `isCompleted` is true and `error` carries the message, in the portal language

### Requirement: Starting the import

`startMigration` SHALL take the edited `parseResult` as body — `migratorName`, the `users` with
`shouldImport`, `email` and `userType`, and the `import...` flags for personal, shared, common and
project files and for groups — queue the import and answer 200 with no body. A missing body, an unknown
`userType` or an invalid `email` SHALL answer 400. Only `users` is read for the selection, matched by
`key`: an entry of `existUsers` or `withoutEmailUsers` must be moved into `users` to be imported.

#### Scenario: Import queued

- **WHEN** the owner posts the parse result with two users marked `shouldImport` true and `importPersonalFiles` true
- **THEN** the system answers 200 and `getMigrationStatus` reports a job with `parseResult.operation` `migration`

#### Scenario: Users only

- **WHEN** the owner posts users marked `shouldImport` true with every `import...` flag false
- **THEN** only the accounts are created; no folder, room or group is created

#### Scenario: Invalid e-mail

- **WHEN** a user in the body carries `email` `not-an-address`
- **THEN** the system answers 400 and nothing is queued

#### Scenario: User without e-mail given one

- **WHEN** an entry from `withoutEmailUsers` is sent in `users` with an `email` filled in and `shouldImport` true
- **THEN** the account is created with that e-mail

### Requirement: Importing a user as DocSpace admin

A DocSpace admin who is not the owner SHALL be refused with 403 "Access denied" when any user marked
`shouldImport` carries `userType` `DocSpaceAdmin` unless an account with that e-mail is already a
DocSpace admin of the portal; the owner may import any type. The check SHALL run before the job is
queued, so nothing is imported on refusal.

#### Scenario: Admin promotes a new user

- **WHEN** a DocSpace admin posts a user with `userType` `DocSpaceAdmin` whose e-mail is not an administrator of the portal
- **THEN** the system answers 403 "Access denied" and no job is queued

#### Scenario: Owner promotes a new user

- **WHEN** the owner posts the same body
- **THEN** the system answers 200 and the user is created as a DocSpace admin

### Requirement: Creating the accounts

For each selected user whose e-mail has no portal account the import SHALL create a pending account
with the source names (the last name falls back to the first), a unique user name derived from the
e-mail, the chosen `userType` and its type group, the source photo and profile data when present. A
selected user whose e-mail exists SHALL NOT be created again; the existing account receives the
imported files instead. Every selected user SHALL be counted in `successedUsers`.

#### Scenario: New account

- **WHEN** a user `john@example.com` with `userType` `RoomAdmin` is imported
- **THEN** a Room admin account with activation pending exists afterwards, user name `john` (or `john1` when taken) and the source photo

#### Scenario: Existing account selected

- **WHEN** a user from `existUsers` is sent in `users` with `shouldImport` true
- **THEN** no account is created, the account keeps its type, and the user's files are imported into that account

### Requirement: Failures and the paid-user quota

Creating a user as `RoomAdmin` or `DocSpaceAdmin` SHALL count against the portal's paid-user quota and
a `User` against the user quota, as any invitation does; a user whose creation fails for that or any
other reason SHALL be counted in `failedUsers`, recorded as "<key> - Couldn't import user" in
`errors`, and skipped for groups and files, while the import continues with the next user.
`successedUsers` is the number of selected users minus `failedUsers`.

#### Scenario: Paid-user limit reached

- **WHEN** the portal has one paid seat left and two users are imported as `RoomAdmin`
- **THEN** the first is created, the second is counted in `failedUsers` with "Couldn't import user" in `errors`, and the job still ends with `isCompleted` true and an empty `error`

### Requirement: Importing groups

`importGroups` SHALL decide for every group at once (the `shouldImport` of a single group is ignored).
An imported group whose name a portal group already carries SHALL reuse that group; otherwise a group
of that name is created. The members found in the source that were selected for import SHALL be added
to the group; a Workspace department head becomes the group's manager. A member that cannot be added
is recorded as "Couldn't add user <key> to group <name>" in `errors`.

#### Scenario: Group with members

- **WHEN** `importGroups` is true and the group `Marketing` lists two selected users and one not selected
- **THEN** `Marketing` exists with the two imported users as members

#### Scenario: Group name taken

- **WHEN** the portal already has a group named `Marketing`
- **THEN** no second group is created and the members are added to the existing one

### Requirement: Importing personal files and their shares

With `importPersonalFiles` true the files of each selected user SHALL be placed, acting as that user,
in a new folder "ASC migration files <dd.MM.yyyy>" in the user's My documents, keeping the folder tree.
With `importSharedFiles` and `importSharedFolders` the source shares SHALL be re-applied to imported
users and groups as editing, read or comment, downgraded to the nearest right the entry allows; shares
to unknown subjects or to the owner are skipped. Failures are recorded in `errors`.

#### Scenario: Personal files imported

- **WHEN** a user with 3 folders and 10 files is imported with `importPersonalFiles` true
- **THEN** the user's My documents holds "ASC migration files <today>" with the 3 folders and 10 files, created by that user

#### Scenario: Shares re-applied

- **WHEN** a Nextcloud file was shared with another imported user and `importSharedFiles` is true
- **THEN** the imported file is shared with that user's new account with the matching right

#### Scenario: Files switched off

- **WHEN** `importPersonalFiles` is false
- **THEN** no folder is created for any user and `importSharedFiles`/`importSharedFolders` have no effect

### Requirement: Importing common and project files of a Workspace portal

For a `Workspace` backup `importCommonFiles` SHALL copy the Common documents into a new Public room
"ASC migration common files <dd.MM.yyyy>" owned by the caller, and `importProjectFiles` SHALL create
one room per project — Custom, or Editing for a private project — owned by the project manager when
imported as a Room admin or DocSpace admin, with the participants as Content creators and read access
for Everyone on a non-private project. The other sources have no common or project files.

#### Scenario: Common files

- **WHEN** `importCommonFiles` is true for a Workspace backup
- **THEN** a Public room "ASC migration common files <today>" holds the Common documents tree

#### Scenario: Private project

- **WHEN** a private project with two participants is imported with `importProjectFiles` true
- **THEN** an Editing room with the project's title exists, its participants have the Content creator role and Everyone has no access

### Requirement: Progress and end of the import

The import SHALL report `progress` through the stages — accounts up to 30, groups to 50, personal
files to 80, common files 85, project files 90, done 100 — with `parseResult.operation` `migration`,
`successedUsers`, `failedUsers` and `errors`. One failing user, file, share or group SHALL NOT stop the
job; an unexpected failure SHALL end it with `error` set. When the job ends the uploaded files SHALL
be deleted, so a new import needs a new upload and parse.

#### Scenario: Import completed

- **WHEN** the import ends
- **THEN** `isCompleted` is true, `progress` 100, `error` empty, `successedUsers` and `failedUsers` filled, and the upload folder is gone

### Requirement: One job per portal

A portal SHALL hold at most one job. `uploadAndInitializeMigration` or `startMigration` called while a
job is still running SHALL answer 200 and be ignored; called while the portal's job is completed it
SHALL replace that job. The job of one portal never affects another portal.

#### Scenario: Second parse while running

- **WHEN** the owner calls `uploadAndInitializeMigration` twice while the first parse is still running
- **THEN** both calls answer 200 and `getMigrationStatus` keeps reporting the first parse

#### Scenario: Import after parse

- **WHEN** the owner calls `startMigration` after the parse reported `isCompleted`
- **THEN** the finished parse job is replaced by the import job

### Requirement: Cancelling a parse

`cancelMigration` SHALL stop the portal's parse job, if any, delete the uploaded files and answer 200
with no body, also when nothing is running. An import job SHALL NOT be stopped by it, and nothing
already imported is undone.

#### Scenario: Parse cancelled

- **WHEN** the owner calls `cancelMigration` during a parse
- **THEN** the system answers 200, the parse stops, `getMigrationStatus` soon returns an empty result and the upload folder is deleted

#### Scenario: Nothing running

- **WHEN** the owner calls `cancelMigration` on a portal without a job
- **THEN** the system answers 200

### Requirement: Clearing a finished import

`clearMigration` SHALL remove the portal's completed import job, delete the uploaded files and answer
200 with no body, also when there is nothing to clear; a running job or a parse job is not affected.
Afterwards `getMigrationStatus` SHALL return an empty result and `getMigrationLogs` 404.

#### Scenario: Import cleared

- **WHEN** the owner calls `clearMigration` after the import reported `isCompleted`
- **THEN** the system answers 200, `getMigrationStatus` returns an empty result and the imported users and files stay

### Requirement: Finishing the import

`finishMigration` SHALL require `isSendWelcomeEmail` (400 when missing). With true it SHALL send the
activation e-mail to every account the import created that is not yet active, skipping active ones,
and SHALL answer 500 "No migration is in progress" when the portal holds no job. It SHALL then do what
`clearMigration` does. The call is not idempotent: each call sends the e-mails again.

#### Scenario: Welcome e-mails sent

- **WHEN** the owner calls `finishMigration` with `isSendWelcomeEmail` true after an import that created three pending accounts
- **THEN** three activation e-mails are sent, the system answers 200 and the job is removed

#### Scenario: Quiet finish

- **WHEN** the owner calls `finishMigration` with `isSendWelcomeEmail` false
- **THEN** no e-mail is sent, the job is removed and the system answers 200

#### Scenario: No job

- **WHEN** the owner calls `finishMigration` with `isSendWelcomeEmail` true on a portal without a job
- **THEN** the system answers 500 "No migration is in progress"

### Requirement: Downloading the log

`getMigrationLogs` SHALL answer 404 "No migration is in progress" when the portal holds no job, and
otherwise 200 with the log of the current job as a `text/plain; charset=UTF-8` attachment named
`migration.log`: one timestamped line per step and progress report, with the message of every
recorded failure. Each job writes its own log; while the job runs the download returns what was
written so far.

#### Scenario: Portal without a job

- **WHEN** the owner calls `getMigrationLogs` on a fresh portal
- **THEN** the system answers 404 "No migration is in progress"

#### Scenario: Log of a finished import

- **WHEN** the owner calls `getMigrationLogs` after an import
- **THEN** the system answers 200 with `migration.log` listing the created users, groups, folders, files and the failures

### Requirement: Jobs run in the background

The parse, import, cancel and clear requests SHALL be handed to a background worker over the event
bus and run with the identity of the caller and the language of the portal; the API answers before
any work is done, so every result is read from `getMigrationStatus`. The uploaded backup and the
logs SHALL be stored per portal in the installation's storage.

#### Scenario: Caller identity

- **WHEN** a DocSpace admin starts an import
- **THEN** the common-files room and the accounts are created on behalf of that admin, and the error messages are in the portal language
