# data-import — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`; `Controller` is
`web/ASC.Web.Api/Api/MigrationController.cs`, `StatusDto` is `web/ASC.Web.Api/ApiModels/ResponseDto/MigrationStatusDto.cs`,
`FinishDto` is `web/ASC.Web.Api/ApiModels/RequestsDto/FinishDto.cs`, `RouteDto` is
`web/ASC.Web.Api/ApiModels/RequestsDto/IdRequestDto.cs` (`MigratorNameRequestDto`, lines 131-143), `Core` is
`common/ASC.Migration/ApiModels/MigrationCore.cs`, `Worker` is `common/ASC.Migration/Core/MigrationWorker.cs`,
`Operation` is `common/ASC.Migration/Core/MigrationOperation.cs`, `Upload` is
`common/ASC.Migration/Core/MigrationFileUploadHandler.cs`, `Logger` is `common/ASC.Migration/Core/MigrationLogger.cs`,
`Handler` is `common/ASC.Migration/Core/MigrationIntegrationEventHandler.cs`, `Migrator` is
`common/ASC.Migration/Migrators/Migrator.cs`, `Info` is `common/ASC.Migration/Migrators/Model/MigrationInfo.cs`,
`User` is `common/ASC.Migration/Migrators/Model/MigrationUser.cs`, `Workspace` is
`common/ASC.Migration/Migrators/Provider/WorkspaceMigrator.cs`, `Google` is
`common/ASC.Migration/Migrators/Provider/Google/GoogleWorkspaceMigrator.cs`, `Nextcloud` is
`common/ASC.Migration/Migrators/Provider/NC/NextcloudWorkspaceMigrator.cs`, `ApiInfo` is
`common/ASC.Migration/ApiModels/MigrationApiInfo.cs`, `ApiUser` is `common/ASC.Migration/ApiModels/MigratingApiUser.cs`,
`Resx` is `common/ASC.Migration/Resources/MigrationResource.resx`, `UserManager` is
`common/ASC.Core.Common/Context/Impl/UserManager.cs`, `PaidChecker` is `web/ASC.Web.Core/Quota/CountRoomAdminFeature.cs`,
`storage.json` / `appsettings.json` / `nginx` are `../buildtools/config/storage.json`, `../buildtools/config/appsettings.json`,
`../buildtools/config/nginx/onlyoffice.conf`. Tests: `Tests/` is `web/ASC.Web.Api.Tests/Tests/07_Migration/`
(`MigrationTests.cs`, `MigrationPermissionsTests.cs`), `TS` is `../tests/api-tests/src/tests/migration/`
(`migration.spec.ts`, `migration.permissions.spec.ts`). Status: **TEST-CONFIRMED** (an integration test asserts
it), **CODE-ONLY** (read from the product code, the configuration or the OpenAPI document, no test), **NOTES-ONLY**
(only the release notes say it — nothing of that kind was written into the spec). Exception → status mapping:
`SecurityException` 403 "Access denied", `ArgumentException` 400, `ItemNotFoundException` 404, `InvalidOperationException`
403, `TenantQuotaException` 402, anything else 500 (`common/ASC.Api.Core/Middleware/ResponseWrapper.cs:55-95`).

The .NET suite for this capability is 6 functional tests (list, status, logs × owner / DocSpace admin) and 12
permission cases (anonymous, Room admin, User, Guest × the same three reads) — exactly what the TS suite has.
Both suites say in a comment why init, migrate, finish, cancel and clear are not covered: they need a real
backup and a worker that has run. Everything about the jobs themselves is therefore CODE-ONLY.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Who may use data import** | | |
| DocSpace administrator right on every controller action; 403 "Access denied" | `Controller:356-362` (`DemandPermissionAsync`, called at 77, 105, 134, 178, 204, 237, 291, 333); `Upload:54-57` | CODE-ONLY |
| Owner and DocSpace admin get 200 on list/status, 404 on logs of a fresh portal | `Tests/MigrationTests.cs:54-141`; `TS/migration.spec.ts` | TEST-CONFIRMED |
| Room admin, User, Guest → 403 "Access denied" on list, status, logs | `Tests/MigrationPermissionsTests.cs:61-78, 94-111, 127-144`; `TS/migration.permissions.spec.ts` | TEST-CONFIRMED |
| Anonymous → 401 on list, status, logs | `Tests/MigrationPermissionsTests.cs:47-59, 80-92, 113-125` | TEST-CONFIRMED |
| No edition/tariff gate | no quota, tariff or standalone check in `Controller`, `Core`, `Worker`, `Operation`; the `migration.enabled` key of `appsettings.json:272-274` is read nowhere in the server (see deviations) | CODE-ONLY |
| **Listing the import sources** | | |
| Names from the registered migrators, registration order, case-insensitive match later | `Core:44-49`; `Workspace:59`, `Google:72`, `Nextcloud:63` (the three names) | CODE-ONLY |
| List contains `Workspace`, `Nextcloud`, `GoogleWorkspace`; `count` = length; `links[0].action` GET | `Tests/MigrationTests.cs:54-83`; yaml `listMigrations` (45430-45501) | TEST-CONFIRMED |
| **Uploading the source backup** | | |
| Route `migrationFileUpload.ashx` served by the Web.Api; proxied by nginx | `web/ASC.Web.Api/Extensions/WebApiServiceExtensions.cs:87-89`; `nginx:698-700` | CODE-ONLY |
| `Init=true`: previous folder deleted, random new folder in the `migration` storage module, path cached for a day, chunk size from `files:uploader:chunk-size` (10 MB default) | `Upload:61-86`; `appsettings.json:123-124`; `storage.json:126-133` | CODE-ONLY |
| Later POST: `Form.Files[0]` appended to `Path.GetFileName(Name)` in the cached folder | `Upload:87-97` | CODE-ONLY |
| JSON `{Success, Message, ChunkSize}` always, HTTP 200; "Access denied." for non-admins; no size check | `Upload:51-57, 99-134` | CODE-ONLY |
| **Archive requirements per source** | | |
| Workspace: first `.gz`/`.tar`, else "Archive must be .gz or .tar" with files in `failedArchives`; `databases/` tables and `files/` content read | `Workspace:70-78, 97-109, 216, 318-319` | CODE-ONLY |
| Nextcloud: first `.zip`, else "Archive must be .zip"; needs a `.bak`/`.sql` dump ("*.bak file not found" wrapped as "Couldn't parse <name> archive"); `data/<uid>/files` | `Nextcloud:73-87, 141-146, 225-231, 386-389`; `Resx:138-140` | CODE-ONLY |
| Google: every `.zip`, name order, root `Takeout` required; per-archive failure listed, all failing → "Couldn't parse archives" | `Google:82-92, 145-150, 205-213`; `Resx:141-143` | CODE-ONLY |
| **Parsing the uploaded backup** | | |
| Queue only, 200 empty body; event published to the bus | `Controller:103-108`; `Core:51-57`; yaml `uploadAndInitializeMigration` (45502-45595) | CODE-ONLY |
| Worker runs the job: migrator resolved by name, "No such migration provider" when unknown | `Handler:46-54`; `Worker:45-48, 55-83`; `Operation:84-121`; `Resx:192-194` | CODE-ONLY |
| `parseResult` fields (`operation` lower-cased, users of the three lists, groups, `files`, `failedArchives`) | `Info:53-69`; `User:44-56`; `ApiInfo`; yaml `MigrationApiInfo` (96575-96711), `MigratingApiUser` (96527-96574) | CODE-ONLY |
| Progress 100 and `isCompleted` at the end; upload kept after a successful parse, deleted after a failure | `Workspace:119-122`, `Google:222-225`, `Nextcloud:232-235`; `Operation:143-183` (`clearMigrationFolder` only on error or after an import) | CODE-ONLY |
| **Classifying the users found** | | |
| New / existing / without e-mail; disabled (Terminated) portal account skipped; duplicates merged | `Workspace:281-311`, `Google:175-199, 230-236`, `Nextcloud:192-211` | CODE-ONLY |
| Default `userType` User, `shouldImport` false | `User:38, 40` | CODE-ONLY |
| **Backup size against the storage quota** | | |
| `MaxTotalSize` of the tenant quota compared with the folder size (archives excluded for an import); "Backup is larger than the total size of the portal" | `Operation:123-141`; `Resx:180-182`; release notes v3.0 line 31 (client warning dialog) | CODE-ONLY |
| **Reading the job state** | | |
| Null job → empty 200; `progress`, `error` (exception message or ""), `isCompleted`, `parseResult` | `Controller:132-155`; `StatusDto`; yaml `getMigrationStatus` (45596-45675), `MigrationStatusDto` (96712-96752) | CODE-ONLY |
| 200 on a fresh portal (owner, DocSpace admin) | `Tests/MigrationTests.cs:85-107` | TEST-CONFIRMED |
| **Starting the import** | | |
| Body required (`ArgumentNullException` → 400); `[EmailAddress]` on `email`; `userType` is a string enum (unknown name → 400); 200 empty body; queued | `Controller:233-266`; `ApiUser:54-55, 83`; `common/ASC.Core.Common/Core/EmployeeType.cs:40`; yaml `startMigration` (45816-45901) | CODE-ONLY |
| Only `apiInfo.Users` is read for the selection; `existUsers`/`withoutEmailUsers` of the parse are merged into the pool and matched by `key`; an empty source e-mail takes the request's | `Info:71-98` | CODE-ONLY |
| Users-only mode = all `import...` flags false (storage `ShouldImport` false → nothing created) | `Info:95-112`; `Migrator:330-333` | CODE-ONLY |
| **Importing a user as DocSpace admin** | | |
| Owner bypasses; otherwise `DocSpaceAdmin` e-mails in `users` must be a subset of the portal's admins, else 403 before queueing | `Controller:239-265` | CODE-ONLY |
| **Creating the accounts** | | |
| Lost or removed e-mail → new user: last name falls back, unique user name, Pending, saved with the type, type group added, photo saved; existing → `Info` replaced, not created | `Migrator:197-268, 270-274`; `UserManager:361-401` | CODE-ONLY |
| `successedUsers` = selected − failed | `Migrator:192-193` | CODE-ONLY |
| **Failures and the paid-user quota** | | |
| RoomAdmin/DocSpaceAdmin → paid-user checker; User/Guest → active-user checker; exception per user caught → `failedUsers`, "Couldn't import user", user removed from the pool | `UserManager:378-389`; `PaidChecker:39-60`; `Migrator:260-266, 145` | CODE-ONLY |
| v3.6 notes: admin limit moved from "Select users" to "Select user types" step | release notes v3.6 lines 22-26 — client behaviour; the server check is per user at creation | NOTES-ONLY |
| **Importing groups** | | |
| `importGroups` sets every group; per-group `shouldImport` not read; name reuse; members from the import pool; manager set | `Info:109-112`; `Migrator:276-326`; `Workspace:578-631`; `Google:237-265`; `Nextcloud:239-263` | CODE-ONLY |
| **Importing personal files and their shares** | | |
| Folder "ASC migration files dd.MM.yyyy" in My documents as the user; tree by level; file versions/dates; shares re-applied with fallback to an allowed right; owner and unknown subjects skipped | `Migrator:328-556`; `Workspace:545-576` (1→Editing, 2→Read, 6→Comment), `Google:369-416`, `Nextcloud:492-528` | CODE-ONLY |
| `importPersonalFiles` false → storage not imported; shares need the storage | `Info:95-97`; `Migrator:330-333, 433-436` | CODE-ONLY |
| **Importing common and project files of a Workspace portal** | | |
| Common → Public room "ASC migration common files dd.MM.yyyy" as the caller; projects → Custom / Editing (private) room per project, owner changed, participants Content creator, Everyone Read for open projects | `Migrator:159-185, 346-379`; `Workspace:145-181, 348-382, 491-543`; `Info:100-107` | CODE-ONLY |
| Only the Workspace migrator fills common/project storage | `Google`, `Nextcloud` never set `CommonStorage`/`ProjectStorage` | CODE-ONLY |
| **Progress and end of the import** | | |
| 30 / 20 / 30 / 85 / 90 / 100; per-item try/catch; upload folder deleted after an import | `Migrator:128-195, 143-157`; `Operation:144-148, 179-182` | CODE-ONLY |
| **One job per portal** | | |
| Semaphore; existing completed job dequeued and replaced; running job kept and only republished | `Worker:55-83` | CODE-ONLY |
| **Cancelling a parse** | | |
| Dequeues only jobs with `operation` "parse" for the tenant, then deletes the upload folder; 200 always | `Controller:176-181`; `Core:67-70`; `Worker:85-95`; yaml `cancelMigration` (45676-45745) | CODE-ONLY |
| **Clearing a finished import** | | |
| Dequeues completed jobs with `operation` "migration"; folder deleted; 200 always | `Controller:202-207`; `Worker:97-107`; yaml `clearMigration` (45746-45815) | CODE-ONLY |
| **Finishing the import** | | |
| `isSendWelcomeEmail` `required` (400); true → activation mail for each imported, not active user; no job → `Exception` → 500 "No migration is in progress"; then clear | `Controller:331-354`; `FinishDto:46`; `Resx:195-197`; yaml `finishMigration` (45976-46053), `FinishDto` (94990-95004) | CODE-ONLY |
| **Downloading the log** | | |
| 404 "No migration is in progress" without a job | `Controller:289-297`; `Tests/MigrationTests.cs:113-141` (bug trait 81653, asserts 404); `TS/migration.spec.ts` ("BUG 81653") | TEST-CONFIRMED |
| `text/plain; charset=UTF-8`, attachment `migration.log`, per-job name, lines `yyyy-MM-ddTHH:mm:ss: msg` + exception message; stored in the `migration_log` module | `Controller:298-305`; `Logger:48-100`; `storage.json:134-141`; yaml `getMigrationLogs` (45902-45975) | CODE-ONLY |
| **Jobs run in the background** | | |
| Four integration events published by the Web.Api, subscribed by the Web.Studio host; job authenticates as the caller, tenant culture set | `Core:51-75`; `web/ASC.Web.Studio/Extensions/WebStudioServiceExtensions.cs:98-114`; `Operation:105-112` | CODE-ONLY |

## Where the server differs from the release notes or the OpenAPI document (spec follows the server)

| Release note / document | Server |
|---|---|
| v2.5 notes: migration of "users with files, groups and shared files/folders" | Also Workspace Common documents and project files into rooms (`Migrator:159-185`), user photos and profile contacts (`Google:580-686`, `Nextcloud:302-362`). |
| v3.6 notes: "no admin limit on the Select users step, everyone is a free User; the limit moved to Select user types" | Client logic. The server has no up-front limit at all: every user is proposed as `User` (`User:38`), and a RoomAdmin/DocSpaceAdmin that does not fit the paid-user quota fails individually at creation while the import continues (`Migrator:260-266`). |
| v3.0 notes: "a warning dialog appears before data migration when the quota is on" | Client. The server rejects a backup bigger than the portal's total storage quota inside the job with "Backup is larger than the total size of the portal" (`Operation:123-141`), not at the API call. |
| yaml `MigrationApiInfo.users` example `{"id": "...", "email": ..., "shouldImport": true}`, `groups` example `{"id": "1", "name": "Group1", ...}` | The wire fields are `key` (not `id`) for a user and `groupName` / `userUidList` (no `id`, no `name`) for a group (`ApiUser:47`, `common/ASC.Migration/ApiModels/MigratingApiGroup.cs:48-62`). A body built from the example would match no parsed user. |
| yaml `ImportableApiEntity.shouldImport` on a group: "Specifies whether the API entity should be imported" | The per-group flag is never read; `importGroups` applies to every group (`Info:109-112`). |
| yaml `uploadAndInitializeMigration` lists a generic 400 | The action binds only a route string; no 400 is produced by the code. |
| yaml `startMigration`: "a call made while the job runs is ignored" | Agrees (`Worker:60-77`); in addition a *completed* job of either kind is silently replaced by the new one, so `clearMigration` is not needed before a second import. |
| yaml `cancelMigration`: "cancelling when nothing is running still answers 200" | Agrees. The upload folder is deleted even then (`Worker:94`), so a backup uploaded but not yet parsed is lost by a stray cancel. |
| yaml `migrationFileUpload.ashx` is described only in prose | The handler is not in the OpenAPI document and not in any SDK; the client calls it directly (`../client/packages/client/src/store/ImportAccountsStore.ts:446`). |
| `appsettings.json:272-274` `"migration": { "enabled": "false" }` | No server code reads the key (string search over `common/`, `web/`, `products/`); it gates nothing. |
| `nginx:618` routes `/api/2.0/migration` to the Web.Api; `nginx:642-644` also has `location ~* /migration` → `$migration_service_url` (port 5034) | The API block at 618 wins for `api/2.0/migration/*`; the 5034 service is not part of this repository and not part of this feature. |
| Test names `GetMigrationStatus_*_ReturnsStatusWhenNothingIsRunning` | The assertion is only `StatusCode == 200`; the empty body is not read back (the SDK wraps a null response). |

## Left out of the spec, and why

- **The wizard** (steps, the "Select user types" screen, the quota warning dialog, the 4-days-until-cleanup
  hint): client behaviour (`../client/packages/client/src/store/ImportAccountsStore.ts`), not the server.
- **LDAP import of users and groups** (v2.6 notes line 51): a different feature with its own endpoints.
- **The database-migration tools** (`common/Tools/ASC.Migration.Runner`, `ASC.Migration.Creator`,
  `ASC.MigrationPersonalToDocspace`): schema and personal-portal migrations, unrelated to data import.
- **The storage migration** (`common/ASC.Data.Storage/Migration`): moving files between storage providers.
- **How each migrator reads its format** beyond what the caller sees (Workspace XML table dumps and
  `files/folder_<n>/file_<id>/v<n>/content`, Nextcloud `INSERT INTO` regex parsing of `oc_accounts`,
  `oc_accounts_data`, `oc_storages`, `oc_filecache`, `oc_share`, `oc_groups`, Google `*-info.json` share
  files, localized Takeout folder names, profile and account HTML): implementation; the spec names only the
  archive requirements and the resulting lists.
- **Nextcloud permission bit mapping** (`Nextcloud:520-528`: bits 1/17/… → Read, else Editing) and the
  Workspace project-folder discovery: implementation detail of "the right the source had".
- **Rate-limit headers and 429/5xx rows** of the yaml: generic policy.
- **The distributed task queue mechanics** (Redis-backed task list, 1-day TTL of the task and of the upload
  folder key, cancellation by notify, the 10-second cleaner): implementation; see M4/M5 for their visible
  consequences.
- **Audit trail**: no audit event is written by any migration operation; nothing to specify.
- **Socket events**: the user-created event is sent per imported account (`Migrator:217`) and the paid-user
  counter update is dead code (M6); the real-time behaviour belongs to the users feature.

## Suspected defects

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| M1 | Uploading the source backup | The handler answers HTTP 200 for every outcome, including "Access denied." and an upload sent without a prior `Init` (the cached folder path is null, so `Path.Combine` throws and the raw exception message is returned as `Message`). No upload size limit exists; the quota check happens only later in the job. A client that checks the HTTP status instead of `Success` sees nothing wrong. | `Upload:51-108` |
| M2 | Reading the job state | `getMigrationStatus` swallows every exception from the queue (`catch { }`) and returns the empty result, so a cache/queue outage looks exactly like "no job". | `Controller:135-154` |
| M3 | Finishing the import | With `isSendWelcomeEmail` true after a parse-only job (or an import that failed before the user step) `ImportedUsers` is null and the loop throws `NullReferenceException` → 500 with an internal message instead of the documented "No migration is in progress". | `Controller:335-352`; `Operation:169` (set only when a migrator ran) |
| M4 | Cancelling / clearing | `Stop` and `Clear` read `MigrationApiInfo.Operation` of every queued task of the tenant; a parse task that was enqueued but has not yet reported progress has `MigrationApiInfo` null (NRE in the event handler, the request is lost) or an `Operation` that is still null (the task is not matched and the parse keeps running while its upload folder is deleted underneath it). | `Worker:87, 99`; `Operation:69-74, 97-102` |
| M5 | Uploading / parsing | The upload folder path is cached for one day; a parse started later than that finds no folder and ends with the generic "file not found" message, and the folder is never cleaned. Folder deletion is fire-and-forget (`Task.Factory.StartNew`) with no error handling. | `Upload:75`; `Operation:119-121, 150-154, 209-223` |
| M6 | Creating the accounts | The `else if (user.UserType == EmployeeType.RoomAdmin)` branch after the type switch is unreachable (the switch already matches RoomAdmin), so the paid-user counter update over the socket never fires for imported Room admins; a `Guest` `userType` is accepted by the contract but no type group is added for it (`Guid.Empty`). | `Migrator:218-234` |
| M7 | Importing groups | `MigrateGroupAsync` uses `return` instead of `continue` for a group whose `ShouldImport` is false, which would end the whole group stage; harmless today only because every group shares the `importGroups` value. The per-group `shouldImport` published in the contract is dead. | `Migrator:286-289`; `Info:109-112` |
| M8 | Progress and end of the import | Progress steps are computed with integer division (`30 / count`, `20 / count`), so with more than 30 users or 20 groups the step is 0 and `progress` does not move during that stage. | `Migrator:143, 200, 279` |
| M9 | Classifying the users found | A source user whose e-mail belongs to a disabled portal account is dropped from every list without a log line or an `errors` entry, so the caller cannot tell why the user is missing. | `Workspace:282-285`, `Google:182-185`, `Nextcloud:199-202` |
| M10 | Starting the import | The non-owner admin check inspects only `users`; since `Merge` also reads only `users`, this is consistent — but `migratorName` of the body is trusted as the job's migrator without comparing it with the parsed job, so a body with another `migratorName` makes the import re-parse the upload with the wrong migrator and fail late with its error. | `Controller:248-265`; `Operation:76-82`; `Info:82-98` |
