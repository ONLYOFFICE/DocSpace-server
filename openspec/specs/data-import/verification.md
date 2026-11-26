# data-import — verification

> **Test run 80268dcdbe** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-14), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Revision 2026-11-18 (c8bb15c779, no product change): line citations that no longer pointed at the code their row describes were moved to it (most shifted with the longer controller remarks); no row status or claim changed, except where a quoted remark or a removed attribute is noted in the row. The `yaml` line numbers were moved to the submodule commit release/v4.1.0 pins (`ce37da4`); where the header names an older submodule commit, that stays the version the yaml texts were read from.

Revision 2026-11-14 (09d4de0768..6fb6dfb17a)


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
403, `TenantQuotaException` 402, anything else 500 (`common/ASC.Api.Core/Middleware/ResponseWrapper.cs:42-150`).

The .NET suite for this capability is 6 functional tests (list, status, logs × owner / DocSpace admin) and 12
permission cases (anonymous, Room admin, User, Guest × the same three reads) — exactly what the TS suite has —
plus, since the merge of `release/v4.1.0` (`3533ad8a89`, previous state `d58816bece`), `Tests/MigrationWriteTests.cs`
(cancel, clear, finish, migrate and init on a portal without an upload, two of them with bug traits) and
`Tests/MigrationWritePermissionsTests.cs` (anonymous, Room admin, User, Guest × the five write operations). Since 2026-11-10
`Tests/MigrationUploadTests.cs`, `MigrationParseTests.cs`, `MigrationImportTests.cs` and `MigrationStartValidationTests.cs`
upload a hand-made Nextcloud backup (`Tests/MigrationTestBase.cs`, `NextcloudBackup`: a `.zip` with an SQL dump and
`data/<uid>/files`) through `migrationFileUpload.ashx`, parse and import it into the test's own portal; the Workspace
and Google Workspace rows that need a valid archive of their format, the welcome e-mails and the quota limit stay CODE-ONLY. In the
`release/v4.1.0` merge range the product side changed only in `Controller:216-218` (three `RaisesWebhook` declarations on
`startMigration`); `common/ASC.Migration` is untouched, so every line reference below still holds.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Who may use data import** | | |
| DocSpace administrator right on every controller action; 403 "Access denied" | `Controller:341-347` (`DemandPermissionAsync`, called at 77, 105, 134, 178, 204, 237, 291, 333); `Upload:54-57` | TEST-CONFIRMED [run: FAIL-BUG] (403 on the eight actions: `Tests/MigrationPermissionsTests.cs:65-145`, `Tests/MigrationWritePermissionsTests.cs:63-111`) |
| Owner and DocSpace admin get 200 on list/status, 404 on logs of a fresh portal | `Tests/MigrationTests.cs:54-141`; `TS/migration.spec.ts` | TEST-CONFIRMED [run: PASS] |
| Room admin, User, Guest → 403 "Access denied" on list, status, logs | `Tests/MigrationPermissionsTests.cs:61-78, 94-111, 127-144`; `TS/migration.permissions.spec.ts` | TEST-CONFIRMED [run: PASS] |
| Anonymous → 401 on list, status, logs | `Tests/MigrationPermissionsTests.cs:47-59, 80-92, 113-125` | TEST-CONFIRMED [run: PASS] |
| Room admin, User, Guest → 403 "Access denied" and anonymous → 401 on cancel, clear, finish, migrate, init | `Tests/MigrationWritePermissionsTests.cs:48-72` (`Endpoints` theory data at 46) | TEST-CONFIRMED [run: PASS] |
| No edition/tariff gate | no quota, tariff or standalone check in `Controller`, `Core`, `Worker`, `Operation`; the `migration.enabled` key of `appsettings.json:272-274` is read nowhere in the server (see deviations) | TEST-CONFIRMED [run: PASS] (parse and import run on the standalone stand with `migration.enabled` false: `Tests/MigrationImportTests.cs:54`) / CODE-ONLY (that the key is read nowhere) |
| **Listing the import sources** | | |
| Names from the registered migrators, registration order, case-insensitive match later | `Core:44-49`; `Workspace:59`, `Google:72`, `Nextcloud:63` (the three names) | TEST-CONFIRMED [run: PASS] (all three names resolve, case-insensitively: `Tests/MigrationParseTests.cs:96, 141, 156`) / CODE-ONLY (registration order) |
| List contains `Workspace`, `Nextcloud`, `GoogleWorkspace`; `count` = length; `links[0].action` GET | `Tests/MigrationTests.cs:54-83`; yaml `listMigrations` (47782-47849) | TEST-CONFIRMED [run: PASS] |
| **Uploading the source backup** | | |
| Route `migrationFileUpload.ashx` served by the Web.Api; proxied by nginx | `web/ASC.Web.Api/Extensions/WebApiServiceExtensions.cs:87-89`; `nginx:698-700` | TEST-CONFIRMED [run: PASS] (served by the Web.Api: `Tests/MigrationUploadTests.cs:48`) / CODE-ONLY (nginx proxy) |
| `Init=true`: previous folder deleted, random new folder in the `migration` storage module, path cached for a day, chunk size from `files:uploader:chunk-size` (10 MB default) | `Upload:61-86`; `appsettings.json:123-124`; `storage.json:126-133` | TEST-CONFIRMED [run: PASS-ON-RERUN] (10 MB default chunk: `Tests/MigrationUploadTests.cs:48`; previous upload discarded by a new `Init`: `Tests/MigrationUploadTests.cs:134`) / CODE-ONLY (random folder name, one-day cache) |
| Later POST: `Form.Files[0]` appended to `Path.GetFileName(Name)` in the cached folder | `Upload:87-97` | TEST-CONFIRMED [run: PASS] (three chunks appended into one archive the parse reads: `Tests/MigrationUploadTests.cs:116`) |
| JSON `{Success, Message, ChunkSize}`; "Access denied." for non-admins | `Upload:51-57, 99-134` | CODE-ONLY |
| **Archive requirements per source** | | |
| Workspace: first `.gz`/`.tar`, else "Archive must be .gz or .tar" with files in `failedArchives`; `databases/` tables and `files/` content read | `Workspace:70-78, 102-114, 266, 370-371` | TEST-CONFIRMED [run: PASS] ("Archive must be .gz or .tar", file in `failedArchives`: `Tests/MigrationParseTests.cs:141`) / CODE-ONLY (reading `databases/` and `files/`: no Workspace archive built, its tables are `DataTable` XML dumps that need an inline schema) |
| Nextcloud: first `.zip`, else "Archive must be .zip"; needs a `.bak`/`.sql` dump ("*.bak file not found" wrapped as "Couldn't parse <name> archive"); `data/<uid>/files` | `Nextcloud:73-87, 141-146, 225-231, 386-389`; `Resx:138-140` | TEST-CONFIRMED [run: PASS-ON-RERUN] ("Archive must be .zip": `Tests/MigrationParseTests.cs:111`; no dump → "Couldn't parse nc archive": `Tests/MigrationParseTests.cs:126`; `data/<uid>/files` counted: `Tests/MigrationParseTests.cs:47`) |
| Google: every `.zip`, name order, root `Takeout` required; per-archive failure listed, all failing → "Couldn't parse archives" | `Google:82-92, 144-149, 222-230`; `Resx:141-143` | TEST-CONFIRMED [run: PASS] (no root `Takeout`, all failing → "Couldn't parse archives" and `failedArchives`: `Tests/MigrationParseTests.cs:156`) / CODE-ONLY (several archives, name order, a successful Takeout) |
| **Parsing the uploaded backup** | | |
| Queue only, 200 empty body; event published to the bus — for a known and an unknown name alike, with nothing uploaded | `Controller:100-105`; `Core:51-57`; yaml `uploadAndInitializeMigration` (47850-47941); `Tests/MigrationWriteTests.cs:106-119` | TEST-CONFIRMED [run: PASS] (`ClearMigration_FinishedFailedJob_RemovesTheJob` beside it is M13, next section) |
| Worker runs the job: migrator resolved by name; an unknown name ends the job with `isCompleted` true and a non-empty `error` | `Handler:46-54`; `Worker:45-48, 55-83`; `Operation:84-121`; `Tests/MigrationWriteTests.cs:106-119` (asserts `isCompleted` and `error` not empty for `TotallyNotARealMigrator`) — the message is not the documented "No such migration provider", see M11 | TEST-CONFIRMED [run: PASS] (outcome) / CODE-ONLY (message) (`ClearMigration_FinishedFailedJob_RemovesTheJob` beside it is M13, next section) |
| A parse without an upload ends with `isCompleted` true and a non-empty `error` (the cached folder is null) | `Operation:120`; `Nextcloud:66-73` (`Directory.GetFiles(null)` throws after the logger was initialised); `Tests/MigrationWriteTests.cs:106-119` (`Nextcloud`) | TEST-CONFIRMED [run: PASS] (`ClearMigration_FinishedFailedJob_RemovesTheJob` beside it is M13, next section) |
| `parseResult` fields (`operation` lower-cased, users of the three lists, groups, `files`, `failedArchives`) | `Info:53-69`; `User:44-56`; `ApiInfo`; yaml `MigrationApiInfo` (100692-100828), `MigratingApiUser` (100644-100691) | TEST-CONFIRMED [run: PASS] (`Tests/MigrationParseTests.cs:47`) |
| Progress 100 and `isCompleted` at the end; upload kept after a successful parse, deleted after a failure | `Workspace:135-138`, `Google:239-242`, `Nextcloud:231-234`; `Operation:148-188` (`clearMigrationFolder` only on error or after an import) | TEST-CONFIRMED [run: PASS-ON-RERUN] (progress 100 and `isCompleted`: `Tests/MigrationParseTests.cs:47`; upload kept after a parse, read again by the import: `Tests/MigrationImportTests.cs:54`; deleted after a failed parse: `Tests/MigrationParseTests.cs:171`) |
| **Classifying the users found** | | |
| New / existing / without e-mail; disabled (Terminated) portal account skipped; duplicates merged | `Workspace:333-363`, `Google:174-198, 247-253`, `Nextcloud:180-199` | TEST-CONFIRMED [run: PASS] for Nextcloud (new / existing / without e-mail, Terminated account skipped: `Tests/MigrationParseTests.cs:47`) / CODE-ONLY (Workspace and Google, duplicate merge) |
| Default `userType` User, `shouldImport` false | `User:38, 40` | TEST-CONFIRMED [run: PASS] (`Tests/MigrationParseTests.cs:47`) |
| **Backup size against the storage quota** | | |
| `MaxTotalSize` of the tenant quota compared with the folder size (archives excluded for an import); "Backup is larger than the total size of the portal" | `Operation:123-141`; `Resx:180-182`; release notes v3.0 line 31 (client warning dialog) | CODE-ONLY (unreachable: the standalone stand's tenant quota has no `MaxTotalSize` limit, and the billing stack runs no Web.Studio job host for this) |
| **Reading the job state** | | |
| Null job → empty 200; `progress`, `error` (exception message or ""), `isCompleted`, `parseResult` | `Controller:123-146`; `StatusDto`; yaml `getMigrationStatus` (47942-48009), `MigrationStatusDto` (100829-100869) | TEST-CONFIRMED [run: PASS-ON-RERUN] (null job: `Tests/MigrationWriteTests.cs:49-67`; fields after a parse: `Tests/MigrationParseTests.cs:47`; `error` = exception message: `Tests/MigrationParseTests.cs:111`) |
| 200 on a fresh portal (owner, DocSpace admin) | `Tests/MigrationTests.cs:85-107` | TEST-CONFIRMED [run: PASS] |
| **Starting the import** | | |
| Body required (`ArgumentNullException` → 400); `[EmailAddress]` on `email`; `userType` is a string enum (unknown name → 400); 200 empty body; queued | `Controller:224-257`; `ApiUser:54-55, 83`; `common/ASC.Core.Common/Core/EmployeeType.cs:40`; yaml `startMigration` (48142-48232) | TEST-CONFIRMED [run: PASS] (missing body: `Tests/MigrationStartValidationTests.cs:134`; invalid e-mail: `Tests/MigrationStartValidationTests.cs:105`; unknown `userType`: `Tests/MigrationStartValidationTests.cs:117`; 200 and queued: `Tests/MigrationStartValidationTests.cs:95`, `Tests/MigrationImportTests.cs:54`) |
| An empty parse result (`{}`) is accepted with 200 on a portal where nothing was parsed | `Tests/MigrationWriteTests.cs:96-104` (no exception); the queued job then resolves no migrator (`migratorName` null, `Core:46-49`) and ends with an error (`Operation:108`, see M11) | TEST-CONFIRMED [run: PASS] (200) / CODE-ONLY (job outcome) |
| `startMigration` declares `room.created`, `folder.created` and `room.updated` for the rooms and folders the import creates and hands over | `Controller:216-218` (`RaisesWebhook`); `Migrator:385-411, 580-613` | CODE-ONLY |
| Only `apiInfo.Users` is read for the selection; `existUsers`/`withoutEmailUsers` of the parse are merged into the pool and matched by `key`; an empty source e-mail takes the request's | `Info:71-98` | TEST-CONFIRMED [run: PASS] (a `withoutEmailUsers` user created with the request's e-mail: `Tests/MigrationImportTests.cs:54`; an `existUsers` user matched by `key`: `Tests/MigrationImportTests.cs:106`) |
| Users-only mode = all `import...` flags false (storage `ShouldImport` false → nothing created) | `Info:95-112`; `Migrator:564-567` | TEST-CONFIRMED [run: PASS] (`Tests/MigrationImportTests.cs:121`) |
| **Importing a user as DocSpace admin** | | |
| Owner bypasses; otherwise `DocSpaceAdmin` e-mails in `users` must be a subset of the portal's admins, else 403 before queueing | `Controller:230-256` | TEST-CONFIRMED [run: PASS] (`Tests/MigrationStartValidationTests.cs:48, 64, 79, 95`) |
| **Creating the accounts** | | |
| Lost or removed e-mail → new user: last name falls back, unique user name, Pending, saved with the type, type group added, photo saved; existing → `Info` replaced, not created | `Migrator:423-502, 504-508`; `UserManager:361-401` | TEST-CONFIRMED [run: PASS] (new users Pending, saved with the type, last name falls back to the first name; existing user not created again: `Tests/MigrationImportTests.cs:54, 106`) / CODE-ONLY (unique user name, photo, removed account) |
| `successedUsers` = selected − failed | `Migrator:418-419` | TEST-CONFIRMED [run: PASS] with no failures (`Tests/MigrationImportTests.cs:54, 121`) / CODE-ONLY (a failed user) |
| **Failures and the paid-user quota** | | |
| RoomAdmin/DocSpaceAdmin → paid-user checker; User/Guest → active-user checker; exception per user caught → `failedUsers`, "Couldn't import user", user removed from the pool | `UserManager:378-389`; `PaidChecker:39-60`; `Migrator:494-500, 371` | TEST-CONFIRMED [run: PASS] (RoomAdmin imported: `Tests/MigrationImportTests.cs:54`) / CODE-ONLY (a user refused by the checker: the stand has no paid-user limit to hit) |
| v3.6 notes: admin limit moved from "Select users" to "Select user types" step | release notes v3.6 lines 22-26 — client behaviour; the server check is per user at creation | NOTES-ONLY |
| **Importing groups** | | |
| `importGroups` sets every group; per-group `shouldImport` not read; name reuse; members from the import pool; manager set | `Info:109-112`; `Migrator:510-560`; `Workspace:630-683`; `Google:254-282`; `Nextcloud:270-294` | TEST-CONFIRMED [run: PASS] (group created, members only from the import pool: `Tests/MigrationImportTests.cs:88`) / CODE-ONLY (name reuse, manager) |
| **Importing personal files and their shares** | | |
| Folder "ASC migration files dd.MM.yyyy" in My documents as the user; tree by level; file versions/dates; shares re-applied with fallback to an allowed right; owner and unknown subjects skipped | `Migrator:562-791`; `Workspace:597-628` (1→Editing, 2→Read, 6→Comment), `Google:387-434`, `Nextcloud:607-643` | TEST-CONFIRMED [run: PASS] ("ASC migration files …" in My documents of the user, sub-folder tree and files: `Tests/MigrationImportTests.cs:106`) / CODE-ONLY (versions, dates, shares) |
| `importPersonalFiles` false → storage not imported; shares need the storage | `Info:95-97`; `Migrator:564-567, 668-671` | TEST-CONFIRMED [run: PASS] (`Tests/MigrationImportTests.cs:121`) |
| **Importing common and project files of a Workspace portal** | | |
| Common → Public room "ASC migration common files dd.MM.yyyy" as the caller; projects → Custom / Editing (private) room per project, owner changed, participants Content creator, Everyone Read for open projects | `Migrator:385-411, 580-613`; `Workspace:195-231, 400-434, 543-595`; `Info:100-107` | CODE-ONLY |
| Only the Workspace migrator fills common/project storage | `Google`, `Nextcloud` never set `CommonStorage`/`ProjectStorage` | CODE-ONLY |
| **Progress and end of the import** | | |
| 30 / 20 / 30 / 85 / 90 / 100; per-item try/catch; upload folder deleted after an import | `Migrator:354-421, 369-383`; `Operation:149-153, 184-187` | TEST-CONFIRMED [run: PASS] (100 at the end, upload deleted after an import: `Tests/MigrationImportTests.cs:54, 139`) / CODE-ONLY (intermediate steps, per-item catch) |
| **One job per portal** | | |
| Semaphore; existing completed job dequeued and replaced; running job kept and only republished | `Worker:55-83` | TEST-CONFIRMED [run: PASS] (a completed job replaced by a new parse: `Tests/MigrationParseTests.cs:187`) / CODE-ONLY (a running job kept) |
| **Cancelling a parse** | | |
| Dequeues only jobs with `operation` "parse" for the tenant, then deletes the upload folder; 200 always | `Controller:164-169`; `Core:67-70`; `Worker:85-95`; yaml `cancelMigration` (48010-48073) | TEST-CONFIRMED [run: PASS] (finished parse dequeued, upload deleted: `Tests/MigrationParseTests.cs:204`; 200 with nothing queued: `Tests/MigrationWriteTests.cs:49`) |
| Cancel with nothing running, twice → 200 both times, status stays empty | `Tests/MigrationWriteTests.cs:47-56` | TEST-CONFIRMED [run: PASS] |
| **Clearing a finished import** | | |
| Dequeues completed jobs with `operation` "migration"; folder deleted; 200 always | `Controller:189-194`; `Worker:97-107`; yaml `clearMigration` (48074-48141) | TEST-CONFIRMED [run: PASS] (`Tests/MigrationImportTests.cs:154`) |
| Clear with nothing to clear, twice → 200 both times, status stays empty | `Tests/MigrationWriteTests.cs:58-67` | TEST-CONFIRMED [run: PASS] |
| A finished *parse* job is not cleared (the filter is `operation == "migration"`); the test with bug trait 84792 demands that a failed parse is removed — see M13 | `Worker:99`; `Tests/MigrationWriteTests.cs:125-140` | CODE-ONLY (the test asserts the opposite) |
| **Finishing the import** | | |
| `isSendWelcomeEmail` `required` (400); true → activation mail for each imported, not active user; no job → `Exception` → 500 "No migration is in progress"; then clear | `Controller:316-339`; `FinishDto:46-53`; `Resx:195-197`; yaml `finishMigration` (48308-48381), `FinishDto` (99055-99069) | TEST-CONFIRMED [run: FAIL-BUG] (no job → 500 "No migration is in progress": `Tests/MigrationParseTests.cs:266`; after an import 200 and job cleared: `Tests/MigrationImportTests.cs:168`) / (after a parse-only job: NRE, `Tests/MigrationParseTests.cs:250`, M3) / CODE-ONLY (the activation letters) |
| Empty body → 400; `isSendWelcomeEmail: false` without a job → 200 | `Tests/MigrationWriteTests.cs:73-94` | TEST-CONFIRMED [run: PASS] |
| **Downloading the log** | | |
| 404 "No migration is in progress" without a job | `Controller:277-285`; `Tests/MigrationTests.cs:113-141` (bug trait 81653, asserts 404); `TS/migration.spec.ts` ("BUG 81653") | TEST-CONFIRMED [run: PASS] |
| A failed parse that initialised its logger answers 200 with its (near-empty) log; the test with bug trait 84791 demands 404 — see M12 | `Nextcloud:68` (`MigrationLogger.Init()` before the failure); `Operation:175-176` (`LogName` and `SaveLogAsync` in `finally`); `Controller:278-290`; `Tests/MigrationWriteTests.cs:146-160` | CODE-ONLY (the test asserts the opposite) |
| `text/plain; charset=UTF-8`, attachment `migration.log`, per-job name, lines `yyyy-MM-ddTHH:mm:ss: msg` + exception message; stored in the `migration_log` module | `Controller:286-293`; `Logger:48-100`; `storage.json:134-141`; yaml `getMigrationLogs` (48240-48307) | TEST-CONFIRMED [run: PASS] (`text/plain; charset=utf-8`, attachment `migration.log`, `yyyy-MM-ddTHH:mm:ss: ` lines: `Tests/MigrationParseTests.cs:222`, raw HTTP, see M14) / CODE-ONLY (the `migration_log` module) |
| **Jobs run in the background** | | |
| Four integration events published by the Web.Api, subscribed by the Web.Studio host; job authenticates as the caller, tenant culture set | `Core:51-75`; `web/ASC.Web.Studio/Extensions/WebStudioServiceExtensions.cs:105-121`; `Operation:105-112` | TEST-CONFIRMED [run: PASS] (the Web.Studio host runs the jobs, files land in the importing user's My documents: `Tests/MigrationImportTests.cs:106`) / CODE-ONLY (tenant culture) |

## Where the server differs from the release notes or the OpenAPI document (spec follows the server)

| Release note / document | Server |
|---|---|
| v2.5 notes: migration of "users with files, groups and shared files/folders" | Also Workspace Common documents and project files into rooms (`Migrator:385-411`), user photos and profile contacts (`Google:598-704`, `Nextcloud:418-476`). |
| v3.6 notes: "no admin limit on the Select users step, everyone is a free User; the limit moved to Select user types" | Client logic. The server has no up-front limit at all: every user is proposed as `User` (`User:38`), and a RoomAdmin/DocSpaceAdmin that does not fit the paid-user quota fails individually at creation while the import continues (`Migrator:494-500`). |
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
| yaml `uploadAndInitializeMigration` and `Resx:192-194`: an unknown `migratorName` ends the job with "No such migration provider" | Unreachable: `Operation:108` assigns `migrator.OnProgressUpdateAsync` before the null check at `Operation:114-116`, so the job ends with a null-reference message instead (M11). `Tests/MigrationWriteTests.cs:106-119` asserts only that `error` is not empty. The spec says "a non-empty `error`". |
| Test name `GetMigrationLogs_JobFailedBeforeWritingLog_ThrowsNotFound` (bug 84791) | The job does initialise and save its log (`Nextcloud:68`, `Operation:165-171`), so the server answers 200; the name describes the expected, not the observed, behaviour (M12); `Operation:175-176`. |
| Test name `ClearMigration_FinishedFailedJob_RemovesTheJob` (bug 84792) | `Worker:99` clears only completed jobs whose `operation` is "migration"; a failed parse stays (M13). |

## Left out of the spec, and why

- **The wizard** (steps, the "Select user types" screen, the quota warning dialog, the 4-days-until-cleanup
  hint): client behaviour (`../client/packages/client/src/store/ImportAccountsStore.ts`), not the server.
- **LDAP import of users and groups** (v2.6 notes line 51): a different feature with its own endpoints.
- **The database-migration tools** (`common/Tools/ASC.Migration.Runner`, `ASC.Migration.Creator`,
  `ASC.MigrationPersonalToDocspace`): schema and personal-portal migrations, unrelated to data import.
- **The storage migration** (`common/ASC.Data.Storage/Migration`): moving files between storage providers.
- **How each migrator reads its format** beyond what the caller sees (Workspace XML table dumps and
  `files/folder_<n>/file_<id>/v<n>/content`, Nextcloud `INSERT INTO` and `COPY` parsing of `oc_accounts`,
  `oc_accounts_data`, `oc_storages`, `oc_filecache`, `oc_share`, `oc_groups`, Google `*-info.json` share
  files, localized Takeout folder names, profile and account HTML): implementation; the spec names only the
  archive requirements and the resulting lists.
- **Nextcloud permission bit mapping** (`Nextcloud:635-643`: bits 1/17/… → Read, else Editing) and the
  Workspace project-folder discovery: implementation detail of "the right the source had".
- **Rate-limit headers and 429/5xx rows** of the yaml: generic policy.
- **The distributed task queue mechanics** (Redis-backed task list, 1-day TTL of the task and of the upload
  folder key, cancellation by notify, the 10-second cleaner): implementation; see M4/M5 for their visible
  consequences.
- **Audit trail**: no audit event is written by any migration operation; nothing to specify.
- **Socket events**: the user-created event is sent per imported account (`Migrator:443`) and the paid-user
  counter update is dead code (M6); the real-time behaviour belongs to the users feature.

## Suspected defects

Re-checked against the merge of `release/v4.1.0` (`d58816bece..3533ad8a89`): `common/ASC.Migration` did not change and
the controller gained only the `RaisesWebhook` declarations, so M2–M10 are all STILL OPEN; M4 is now observed by a test
(`Tests/MigrationWriteTests.cs:125-140`, bug 84792 — M13 below is its concrete form). Three new rows came out of the new
tests.

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| M11 | Parsing the uploaded backup | An unknown `migratorName` does not produce the documented "No such migration provider": `migrator.OnProgressUpdateAsync` is assigned before the null check, so the job ends with the null-reference message as `error`, and `parseResult` stays an empty object whose `operation` is null (which also keeps M13 from matching the job). The same happens to `startMigration` with an empty body. | `Operation:97-116`; `Tests/MigrationWriteTests.cs:106-119` (asserts only non-empty) |
| M12 | Downloading the log | A parse that failed before doing anything (no upload) still answers 200 with its log, because the migrator initialises the logger before touching the folder and the operation saves the log in `finally`; the test with bug trait 84791 expects 404. | `Nextcloud:68-73`; `Operation:175-176`; `Tests/MigrationWriteTests.cs:146-160` — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bug 84791) |
| M13 | Clearing a finished import | OPEN, red test, run 365d87fe60 (failure text: the job is still answered, with `error`). `clearMigration` removes only completed jobs whose `operation` is "migration", so a failed parse (an unknown migrator, a wrong archive) stays in the queue until a new parse replaces it; `cancelMigration` matches `operation == "parse"` and therefore skips the unknown-migrator job too (its operation is null). The test with bug trait 84792 expects the failed job to be removed. | `Worker:87, 99`; `Operation:97-101`; `Tests/MigrationWriteTests.cs:125-140` — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bug 84792) |
| M14 | Downloading the log | The OpenAPI document does not describe the `text/plain` body of `getMigrationLogs`, so the generated .NET SDK (4.1.0) parses the log as JSON and throws `ApiException` ("Unexpected character encountered while parsing number") on every successful download; the test reads it over raw HTTP. A contract/SDK defect, the server answers correctly. | `Tests/MigrationParseTests.cs:222` |
| M2 | Reading the job state | `getMigrationStatus` swallows every exception from the queue (`catch { }`) and returns the empty result, so a cache/queue outage looks exactly like "no job". | `Controller:126-145` |
| M3 | Finishing the import | With `isSendWelcomeEmail` true after a parse-only job (or an import that failed before the user step) `ImportedUsers` is null and the loop throws `NullReferenceException` → 500 with an internal message instead of the documented "No migration is in progress". | `Controller:320-337`; `Operation:174` (set only when a migrator ran); OPEN, red test (2026-11-10): `Tests/MigrationParseTests.cs:250` answers 500 `System.NullReferenceException` "Object reference not set to an instance of an object." — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) |
| M4 | Cancelling / clearing | `Stop` and `Clear` read `MigrationApiInfo.Operation` of every queued task of the tenant; a parse task that was enqueued but has not yet reported progress has `MigrationApiInfo` null (NRE in the event handler, the request is lost) or an `Operation` that is still null (the task is not matched and the parse keeps running while its upload folder is deleted underneath it). | `Worker:87, 99`; `Operation:69-74, 97-102` — red test, run 2026-11-18 (`2a64d461e6`, repeated on `c245ce0160`) (bug 84792; M13 is its concrete form) |
| M5 | Uploading / parsing | The upload folder path is cached for one day; a parse started later than that finds no folder and ends with the generic "file not found" message, and the folder is never cleaned. Folder deletion is fire-and-forget (`Task.Factory.StartNew`) with no error handling. | `Upload:75`; `Operation:119-121, 155-159, 259-273` |
| M6 | Creating the accounts | The `else if (user.UserType == EmployeeType.RoomAdmin)` branch after the type switch is unreachable (the switch already matches RoomAdmin), so the paid-user counter update over the socket never fires for imported Room admins; a `Guest` `userType` is accepted by the contract but no type group is added for it (`Guid.Empty`). | `Migrator:444-460` |
| M7 | Importing groups | `MigrateGroupAsync` uses `return` instead of `continue` for a group whose `ShouldImport` is false, which would end the whole group stage; harmless today only because every group shares the `importGroups` value. The per-group `shouldImport` published in the contract is dead. | `Migrator:520-523`; `Info:109-112` |
| M8 | Progress and end of the import | Progress steps are computed with integer division (`30 / count`, `20 / count`), so with more than 30 users or 20 groups the step is 0 and `progress` does not move during that stage. | `Migrator:369, 426, 513` |
| M9 | Classifying the users found | A source user whose e-mail belongs to a disabled portal account is dropped from every list without a log line or an `errors` entry, so the caller cannot tell why the user is missing. | `Workspace:334-337`, `Google:181-184`, `Nextcloud:187-190` |
| M10 | Starting the import | The non-owner admin check inspects only `users`; since `Merge` also reads only `users`, this is consistent — but `migratorName` of the body is trusted as the job's migrator without comparing it with the parsed job, so a body with another `migratorName` makes the import re-parse the upload with the wrong migrator and fail late with its error. | `Controller:239-256`; `Operation:76-82`; `Info:82-98` |

## Revision 2026-11-14 (`09d4de0768..6fb6dfb17a`)

| Claim | Source | Status |
|---|---|---|
| Nextcloud takes `.zip`, `.tar.gz`, `.tgz` in any case; otherwise "Archive must be .zip, .tar.gz or .tgz" (supersedes "Archive must be .zip" in the row above) | `cef7b8b357`; `NextcloudWorkspaceMigrator` | TEST-CONFIRMED [run: PASS] (`web/ASC.Web.Api.Tests/Tests/07_Migration/NextcloudParseTests.cs:47-71` `Parse_SupportedArchive_ReadsUsersAndFiles`, `:320-331` `Parse_UnsupportedArchive_ReportsAcceptedFormats`) |
| Broken archive: cut short, no end-of-archive block, entry outside the folder → "Couldn't parse", archive in `failedArchives` | `5e9bcf0184`, `3ff6b37802`, `f55c8884a0` | TEST-CONFIRMED [run: PASS] (`web/ASC.Web.Api.Tests/Tests/07_Migration/NextcloudUnpackTests.cs:134-151`, `:162-176`; `WorkspaceParseTests.cs:148-160`) |
| Links and repeated entries: symbolic link skipped, hard link copied or skipped with a log line, later copy of a repeated path kept | `2f5d73b358`, `75591b0f19` | TEST-CONFIRMED [run: PASS] (`web/ASC.Web.Api.Tests/Tests/07_Migration/NextcloudUnpackTests.cs:54`, `:79`, `:105`; `NextcloudParseTests.cs:134-153`) |
| Paths from the dump outside the user's folder left out | `f55c8884a0` | TEST-CONFIRMED [run: PASS] (`web/ASC.Web.Api.Tests/Tests/07_Migration/NextcloudParseTests.cs:80-94`, `:106-122`) |
| Unpack bound: quota capped by the installation ceiling (100 GB default, 1 MB–1 PB), entries (1,000,000 default); same LargeBackup error; Google names the failing Takeout | `7eef459e01`, `67b50ac0ea`, `ba7ce9581a`, `8ab63e709e`; `MigrationOperation.GetMaxExtractedSize`, `GetMaxExtractedEntries` | TEST-CONFIRMED [run: PASS] (`web/ASC.Web.Api.Tests/Tests/07_Migration/MigrationSizeLimitTests.cs:60-199`, error text "larger than the total size of the portal"; defaults and bounds: `MigrationExtractionLimitsTests.cs:61-150`) |
| Unpacking progress at most once per percent | `e344a1bf33`, `75591b0f19` | TEST-CONFIRMED [run: PASS] (`web/ASC.Web.Api.Tests/Tests/07_Migration/NextcloudParseTests.cs:165-186`, `WorkspaceParseTests.cs:86-98`, `GoogleWorkspaceParseTests.cs:80-92`) |
| A parse leaves the unpacked tree; a later parse or the import reads it, an import without a parse unpacks the archive itself | `5e9bcf0184` | TEST-CONFIRMED [run: PASS] (`web/ASC.Web.Api.Tests/Tests/07_Migration/NextcloudParseTests.cs:200-217`, `:231-261`, `:295-314`); no spec text changed (the uploaded files still stay for the import) |
| Stopped while unpacking ends with the cancellation | `8a7c2b12a0`, `8ab63e709e` | CODE-ONLY |
| Oversized profile photo skipped with a log line | `c4951746da`; resource `PhotoTooLarge` | TEST-CONFIRMED [run: PASS] (`web/ASC.Web.Api.Tests/Tests/07_Migration/WorkspaceParseTests.cs:111-127` `Migrate_UserPhoto_SkipsPhotoOverPixelLimit`) |
| M3 | `c096f23fa9` removed the documented 500 of `finishMigration`, but `FinishMigration` (`MigrationController.cs:316-339`) still dereferences `ImportedUsers` unguarded, so the 500 remains, now undeclared | STILL OPEN |
| M2–M14 (others) | the range changed the migrators and `MigrationOperation` (limits); the controller only in remarks; the upload handler, the queue reads, `Stop`/`Clear`, the user and group stages and the progress arithmetic are untouched | STILL OPEN |

## Revision 2026-11-26 (bugs 85259, 85267)

| Claim | Source | Status |
|---|---|---|
| A Nextcloud dump that writes an `INSERT` over several lines (a line break after `VALUES`, one row per line; `\n` or `\r\n`) is read like one with the rows on the line of the `INSERT`. Before the fix no table was found in such a dump and the parse ended without an error and without users. FIXED `97b4b72dbd` | `Nextcloud:306-322` (`GetInsertRows`: the statement is read up to the `;` that ends a line) | TEST-CONFIRMED (`web/ASC.Web.Api.Tests/Tests/07_Migration/NextcloudParseTests.cs:344-366` `Parse_DumpWithRowPerLine_ReadsUsersAndFiles`, bug 85259) |
| A Nextcloud dump written by pg_dump — no `INSERT`, the rows of a table after `COPY schema.table (columns) FROM stdin;`, one per line, values separated by tabs, NULL as `\N`, up to a line `\.` — is read like a MySQL one. Before the fix no table was found in such a dump and the parse ended without an error and without users. FIXED (uncommitted) | `Nextcloud:301-304` (`GetDumpChunk`: the `INSERT` first, else the `COPY` block), `:324-393` (`GetCopyRows`, `UnescapeCopyValue`) | TEST-CONFIRMED (`web/ASC.Web.Api.Tests/Tests/07_Migration/NextcloudParseTests.cs:377-412` `Parse_PostgreSqlDump_ReadsUsersAndFiles`, bug 85267) |
