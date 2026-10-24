# apps — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`; `Controller` is
`web/ASC.Web.Api/Api/AppsController.cs`, `Service` is `common/ASC.Core.Common/Data/AppSettingsService.cs`,
`Config` is `common/ASC.Core.Common/Context/Impl/CoreConfiguration.cs` (`CoreBaseSettings.DefaultApps`, line 73,
reading the `apps` section), `AppItem` is `common/ASC.Core.Common/Configuration/AppItem.cs`, `Entity` is
`common/ASC.Core.Common/EF/Model/DbAppSettings.cs` (table `app_settings`, key `tenant_id` + `id`), `RequestDto`
is `web/ASC.Web.Api/ApiModels/RequestsDto/AppRequestsDto.cs` (`SetAppEnabledRequest`, `SetAppSettingsRequest`,
`GetAppRequestDto`), `AppDto` is `web/ASC.Web.Api/ApiModels/ResponseDto/AppDto.cs`, `Socket` is
`common/ASC.Core.Common/Data/AppsSocketManager.cs` plus `common/ASC.Core.Common/Notify/Signalr/SocketServiceClient.cs`,
`appsettings` is `../buildtools/config/appsettings.json` (section `apps`, lines 3-11: `ai-files`, `ai-rooms`,
`ai-forms`, `ai-agents`, `docs-cloud`, `ai-arbiter`, `e2e-rooms`, all `enabled: true`). Tests: `Tests/` is
`web/ASC.Web.Api.Tests/Tests/09_Apps/`, `TS` is `../tests/api-tests/src/tests/apps/` (`apps.read.spec.ts`,
`apps.write.spec.ts`, `apps.permission.spec.ts`, `apps.permission.matrix.spec.ts`, `apps.scenarios.spec.ts`).
Status: **TEST-CONFIRMED** (an integration test asserts it), **CODE-ONLY** (read from the product code, the
configuration or the OpenAPI document, no test), **NOTES-ONLY** (only the release notes say it — nothing of that
kind was written into the spec). Exception → status mapping: `ItemNotFoundException` 404, `ArgumentException`
400, `SecurityException` 403 "Access denied" (`common/ASC.Api.Core/Middleware/ResponseWrapper.cs:55-74`); 415 and
400 for an absent or unbindable body come from the MVC pipeline before the action runs.

The .NET suite is 13 files / 12 test classes (one of them, `SetAppEnabledTests`, is the bug-84498 regression
written before the port); the TS suite has the same cases plus two that cannot be ported (a fresh-session read
and an `APPS_REQUIRED_MODULES` pin). The feature itself landed in May 2026 (`f5bf80901c`, "Add app settings
management with database table and API endpoints"); the controller texts were rewritten for AI consumers in
`0eae64a955`.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Catalogue of portal applications** | | |
| 200, non-empty unique ids, `count` = length | `Tests/AppsGetAllTests.cs` `GetAll_Owner_ReturnsApplications`; `TS apps.read.spec.ts:16` | TEST-CONFIRMED |
| Fresh portal: every entry without `settings` | `Tests/AppsGetAllTests.cs` `GetAll_PortalThatNeverSavedSettings_ReportsNone` | TEST-CONFIRMED |
| Repeated calls identical, change nothing | `Tests/AppsGetAllTests.cs` `GetAll_RepeatedCalls_ReturnSameListAndChangeNothing` | TEST-CONFIRMED |
| Unknown query parameters ignored | `Tests/AppsGetAllTests.cs` `GetAll_UnknownQueryParameters_AreIgnored` | TEST-CONFIRMED |
| List shows the new flag and document, same length | `Tests/AppsGetAllTests.cs` `GetAll_AfterChange_ShowsEnabledAndSettings` | TEST-CONFIRMED |
| Order of the configuration, no paging/filter, default `enabled` from config, override per portal | `Service:41-60` (`GetAppsAsync` iterates `defaults`, `o?.Enabled ?? d.Enabled`); `Config:73`; `Controller:70-82`; yaml `getAll` (45248-45321), `AppArrayWrapper` (92709) | CODE-ONLY |
| Anonymous 401 | `Tests/AppsAnonymousAccessTests.cs` `GetAll_Anonymous_ReturnsUnauthorized`; yaml 401 row | TEST-CONFIRMED |
| **Reading one application** | | |
| 200, `count` 1, equals the list entry | `Tests/AppGetTests.cs` `Get_Owner_ReturnsApplicationById`; `Tests/AppsGetAllTests.cs` `GetAll_EveryListedApplication_MatchesGetAndGetSettings` | TEST-CONFIRMED |
| Reading changes nothing | `Tests/AppGetTests.cs` `Get_Reading_DoesNotChangeEnabledOrSettings` | TEST-CONFIRMED |
| Reports the enabled and the disabled state, the saved document | `Tests/AppGetTests.cs` `Get_ReturnsEnabledAndDisabledApplication`, `Get_ReturnsSavedSettingsDocument` | TEST-CONFIRMED |
| 404 with the id in the message | `Tests/AppGetTests.cs` `Get_UnknownId_ReturnsNotFound` (asserts the body contains `no-such-app`); `Controller:102-103` (`App '{id}' not found`) | TEST-CONFIRMED |
| Blanks, non-ASCII, markup, `a/b`, `../settings`, 2000 chars → 404 | `Tests/AppGetTests.cs` `Get_IdThatCannotBeAnApplication_ReturnsNotFound`, `Get_VeryLongId_ReturnsNotFound` | TEST-CONFIRMED |
| Case-sensitive match | `Tests/AppGetTests.cs` `Get_IdIsCaseSensitive`; `Service:68-72` (`x.Id == id`, ordinal) | TEST-CONFIRMED |
| Empty id routes to the list | `Tests/AppGetTests.cs` `Get_EmptyId_IsRoutedToTheListNotToAnApplication` | TEST-CONFIRMED |
| Anonymous 401 | `Tests/AppsAnonymousAccessTests.cs` `Get_Anonymous_ReturnsUnauthorized` | TEST-CONFIRMED |
| **Reading the settings document** | | |
| Never saved → empty `response`, `count` 0 | `Tests/AppSettingsGetTests.cs` `GetSettings_NeverSaved_ComesBackEmpty`; `Controller:124-134, 208-212` (`ParseSettings` → null); yaml `JsonValueWrapper` (97623) | TEST-CONFIRMED |
| Falsy documents kept, `count` 1 | `Tests/AppSettingsGetTests.cs` `GetSettings_FalsyDocument_IsKeptNotTreatedAsEmpty` | TEST-CONFIRMED |
| Readable while disabled | `Tests/AppSettingsGetTests.cs` `GetSettings_WhileApplicationIsDisabled_StillReadable` | TEST-CONFIRMED |
| 404 unknown / unusable id; anonymous 401 | `Tests/AppSettingsGetTests.cs` `GetSettings_UnknownId_ReturnsNotFound`, `GetSettings_IdThatCannotBeAnApplication_ReturnsNotFound`; `Tests/AppsAnonymousAccessTests.cs` `GetSettings_Anonymous_ReturnsUnauthorized` | TEST-CONFIRMED |
| **Who may read and who may write** | | |
| Owner reads and writes through all five operations | `Tests/AppsRolePermissionsTests.cs` `Owner_ReadsAndWritesThroughAllFiveMethods` | TEST-CONFIRMED |
| DocSpace admin writes; Room admin, User, Guest read but get 403 on both writes and on the reset, enabled and disabled application alike | `Tests/AppsRolePermissionsTests.cs` `Member_ReadsEnabledApplication_AndWritesOnlyWithPortalSettingsRight`, `Member_ReadsDisabledApplication_AndWritesOnlyWithPortalSettingsRight`, `User_WriteIsDenied_ForEnabledAndDisabledApplicationAlike`; `Controller:159, 193` (`EditPortalSettings`), no demand on the three reads | TEST-CONFIRMED |
| Anonymous write 401 and nothing changes | `Tests/AppsAnonymousAccessTests.cs` `SetEnabled_Anonymous_ReturnsUnauthorizedAndChangesNothing`, `SetSettings_Anonymous_ReturnsUnauthorizedAndChangesNothing`, `SetSettings_AnonymousRawRequest_ReturnsUnauthorized` | TEST-CONFIRMED |
| **Order of checks on a write** | | |
| 403 for an unknown id, same message as for a real one | `Tests/AppsRolePermissionsTests.cs` `Member_WithoutRight_GetsForbiddenBeforeTheIdIsLookedUp` (compares `error.message`) | TEST-CONFIRMED |
| Unbindable bodies → 400 for a caller without the right; bindable-but-empty → 403 | same test, the two body loops | TEST-CONFIRMED |
| 415 first (no body), then binding, then permission, then lookup | `Tests/SetAppEnabledStateTests.cs` `SetEnabled_RequestWithoutBody_ReturnsUnsupportedMediaTypeAndChangesNothing`; `Controller:159-160, 193-201` (demand before the service call; the service throws 404) | TEST-CONFIRMED |
| **Switching an application on or off** | | |
| 200 with `id` and new `enabled`; `get` and `getAll` agree | `Tests/SetAppEnabledStateTests.cs` `SetEnabled_Owner_FlipsTheFlagAndItIsReadBack` | TEST-CONFIRMED |
| Idempotent; last request decides | `Tests/SetAppEnabledStateTests.cs` `SetEnabled_RepeatedRequest_IsIdempotent`, `SetEnabled_Sequence_LastRequestDecidesTheFinalState` | TEST-CONFIRMED |
| Document untouched by a switch | `Tests/SetAppEnabledStateTests.cs` `SetEnabled_Switching_DoesNotTouchTheSettingsDocument`; `Service:89-104` (mutates `Enabled` only) | TEST-CONFIRMED |
| Other applications and the count untouched | `Tests/SetAppEnabledStateTests.cs` `SetEnabled_OneApplication_LeavesTheOthersAndTheCountAlone` | TEST-CONFIRMED |
| Unknown id 404, nothing created | `Tests/SetAppEnabledStateTests.cs` `SetEnabled_UnknownId_ReturnsNotFoundAndCreatesNothing`; `Service:91-93` | TEST-CONFIRMED |
| **Body rules of the switch** | | |
| `{}` → 400, state kept (bug 84498) | `Tests/SetAppEnabledTests.cs` `SetAppEnabled_BodyWithoutEnabled_ReturnsBadRequestAndKeepsState` (`[Trait("Bug","84498")]`); `RequestDto:52` (`required bool`), fix commit `c8e3cb2543`; `TS apps.write.spec.ts:201` | TEST-CONFIRMED |
| `null`, strings, numbers, array, object, malformed, bare `null` → 400 | `Tests/SetAppEnabledStateTests.cs` `SetEnabled_UnusableBody_ReturnsBadRequestAndChangesNothing` | TEST-CONFIRMED |
| No body → 415 | `Tests/SetAppEnabledStateTests.cs` `SetEnabled_RequestWithoutBody_ReturnsUnsupportedMediaTypeAndChangesNothing` | TEST-CONFIRMED |
| **Saving the settings document** | | |
| 200 with the document; `getSettings`, `get`, `getAll` agree | `Tests/SetAppSettingsTests.cs` `SetSettings_Owner_ResponseCarriesTheStoredDocumentAndEveryReaderAgrees` | TEST-CONFIRMED |
| Every JSON kind round-trips (20 documents incl. Unicode, control characters, nested null, odd keys) | `Tests/AppSettingsRoundTripTests.cs` + `Tests/AppsTestData.cs` `JsonValues`; `Controller:197-200` (`GetRawText`), `Entity:46-48, 70-71` (`json` / `jsonb` column) | TEST-CONFIRMED |
| Idempotent save; deep nesting stored whole | `Tests/SetAppSettingsTests.cs` `SetSettings_SameDocumentTwice_IsIdempotent`, `SetSettings_DeeplyNestedDocument_IsStoredWhole` | TEST-CONFIRMED |
| Saving does not change `enabled`; saved while disabled applies after enabling | `Tests/SetAppSettingsTests.cs` `SetSettings_Saving_DoesNotChangeEnabled`, `SetSettings_SavedWhileDisabled_ApplyAfterEnabling` | TEST-CONFIRMED |
| Replace, never merge (object→object, object→array, array→object, →`{}`) | `Tests/SetAppSettingsTests.cs` `SetSettings_NewDocument_ReplacesTheOldOneNeverMerges` | TEST-CONFIRMED |
| Two applications independent | `Tests/SetAppSettingsTests.cs` `SetSettings_OneApplication_LeavesAnothersAlone` | TEST-CONFIRMED |
| Unknown id 404, nothing created | `Tests/SetAppSettingsTests.cs` `SetSettings_UnknownId_ReturnsNotFoundAndCreatesNothing`; `Service:110-112` | TEST-CONFIRMED |
| **Dropping the settings document** | | |
| `null` drops, repeatable, `count` 0, `enabled` kept, re-save works | `Tests/SetAppSettingsTests.cs` `SetSettings_Null_DropsTheDocumentAndIsRepeatable`; `Controller:196-200` (Undefined/Null → null) | TEST-CONFIRMED |
| Body without the member resets like null | `Tests/SetAppSettingsInvalidBodyTests.cs` `SetSettings_BodyWithoutSettingsMember_ResetsTheDocumentLikeNull` | TEST-CONFIRMED |
| **Body rules of the settings write** | | |
| Nine malformed bodies → 400, document survives | `Tests/SetAppSettingsInvalidBodyTests.cs` `SetSettings_UnusableBody_ReturnsBadRequestAndTheSavedDocumentSurvives` | TEST-CONFIRMED |
| No body → 415 | `Tests/SetAppSettingsInvalidBodyTests.cs` `SetSettings_RequestWithoutBody_ReturnsUnsupportedMediaTypeAndTheSavedDocumentSurvives` | TEST-CONFIRMED |
| **Consistency of the three readers** | | |
| Lifecycle save → off → on → restore → drop returns to the original snapshot | `Tests/AppsScenariosTests.cs` `Lifecycle_SettingsThenDisableThenEnableThenRestore`, `GetGetSettingsAndGetAll_StayConsistentAfterEveryChange` | TEST-CONFIRMED |
| Unknown application rejected by all four id operations, never appears | `Tests/AppsScenariosTests.cs` `UnknownApplication_IsRejectedEverywhereAndNeverAppears` | TEST-CONFIRMED |
| Every configured application reachable, switchable, configurable | `Tests/AppsScenariosTests.cs` `EveryModule_IsReachableAndCanBeSwitchedAndConfigured` | TEST-CONFIRMED |
| **Per-portal storage and configured defaults** | | |
| Set and defaults from the `apps` section, read once, not writable through the API | `Config:73`; `appsettings:3-11`; `Service` (every method starts from `DefaultApps`); no write path to the configuration | CODE-ONLY |
| No row until the first write; the first write copies the configured `enabled` into the new row; rows keyed by portal + id; a drop keeps the row | `Service:138-167` (`UpsertAsync`: `Enabled = defaults...?.Enabled ?? false`, `SaveChangesAsync`); `Entity:40-44` (key `TenantId`,`Id`); no delete anywhere in `Service` | CODE-ONLY |
| Default flag visible before any write (tests toggle "the opposite of the current value", so the default is only read, never asserted to a value) | `Tests/AppsScenariosTests.cs`, `Tests/SetAppEnabledStateTests.cs` (snapshots `before`) | CODE-ONLY |
| Two portals independent | `Service:46-48, 73-74` (every query filtered by `TenantId`); every .NET test owns one portal, no cross-portal test | CODE-ONLY |
| **Clients are told when an application is switched** | | |
| `change-app-enabled` with `{room: "<tenant>-apps", id, enabled}` queued after the save; delivered as `s:change-app-enabled` `{id, enabled}` to that room; nothing on `setSettings` | `Controller:164` (after `SetEnabledAsync`, nothing in `SetSettingsAsync`); `Socket` (`AppsSocketManager:46-57`); `SocketServiceClient.MakeRequest:65-81` (channel write, no awaited HTTP); `common/ASC.Socket.IO/app/controllers/files.js:117`, `hubs/files.js:429-432` | CODE-ONLY |
| Delivery failure does not fail the call | `SocketServiceClient:132-180` (`SocketService` background loop catches and logs) | CODE-ONLY |
| No audit event | `Controller` calls no `MessageService`; no `MessageAction` for apps | CODE-ONLY |
| **Scope of the flag and the document** | | |
| Nothing server-side reads the flag or the document: `DefaultApps` is referenced only by the service, the service only by the controller | LSP `findReferences` on `CoreBaseSettings.DefaultApps` (7 refs, `Config` + `Service`) and on `AppSettingsService` (13 refs, `Service` + `Controller:207`); `AppItem` used by `Config`, `Service`, `Controller` only | CODE-ONLY |
| Validation beyond "valid JSON" is absent | `Service:113-123` (`JsonDocument.Parse` only); `Controller:197-200` | CODE-ONLY |

## Where the server differs from the release notes or the OpenAPI document (spec follows the server)

| Release note / document | Server |
|---|---|
| v3.3 / v3.5 notes: "hide applications in the Article through a config on the server" | That v3.x switch predates this feature (`f5bf80901c`, May 2026) and is a client-side read of a static config; the per-portal flag with `PUT /api/2.0/apps/{id}/enabled` and the `app_settings` table exist only since v4.0. The notes do not mention the API; the spec is written from the server. |
| v4.0 notes: rebranding "ONLYOFFICE DocSpace → ONLYOFFICE Apps", Article split by modules, Overview section, promo modals and tours per module | Client behaviour; the server side of "modules" is exactly the catalogue described here (`ai-files`, `ai-rooms`, `ai-forms`, `ai-agents`, `docs-cloud`, `ai-arbiter`, `e2e-rooms` in the dev config). The server ships no title, icon or description for an id (`AppDto:24-30` says so). |
| yaml operationIds are the generic `get`, `getAll`, `getSettings`, `setEnabled`, `setSettings` | The SDK exposes them as `AppsApi.GetAsync` etc.; there is no `getApp`/`getApps` naming. Agrees, but the ids collide in a flat namespace and the TS/C# clients disambiguate only by the `Apps` class. |
| yaml `get` and `getSettings` declare a generic `400 Bad Request` | No path of the two GETs produces 400 (an unusable id is 404, see tests); the row is the document-wide default. |
| yaml `AppDto.id` is `["null","string"]` | The server always fills `id` from the configuration (`Controller:204`); never null. |
| yaml `SetAppSettingsRequest.settings` has no `type` and is not `required` | Matches the server: any JSON value, and an absent member means "drop" exactly like `null` (`Controller:197-199`, tests `SetSettings_BodyWithoutSettingsMember_ResetsTheDocumentLikeNull`). |
| Controller text: "a body that is not valid JSON fails with 400 and stores nothing" | The 400 comes from model binding (`JsonElement` cannot be built from malformed input), not from the service's own `JsonDocument.Parse` guard, which is unreachable through HTTP (see AP4). Observable behaviour agrees. |
| TS `apps.scenarios.spec.ts:168` "A fresh owner session sees the stored state" and `:233` `APPS_REQUIRED_MODULES` | Not ported: the .NET harness has no second session, and the module set is the installation's (`AppsTestBase` takes "whatever the list reports"). |
| TS helper restores every application after a test (`withAppRestored`) | The .NET suite needs none of it (one portal per test); so a residual-state bug would show only in TS. |

## Left out of the spec, and why

- **Client behaviour**: which module an id stands for, titles, icons, the Article/Overview layout, promo modals,
  tours, hiding a disabled module (`../client/packages/shared/api/apps/index.ts` consumes the five operations;
  the socket event is handled in the client bundle).
- **The AI product behind `ai-*`**: `ai-agents`; the apps catalogue does not gate it (see "Scope of the flag").
- **Backup and restore of `app_settings`**: the table is NOT backed up — it is a declared coverage gap
  (`common/Tests/ASC.Data.Backup.Core.Tests/BackupCoverageTests.cs:99`, `_knownGaps["app_settings"] = "T6"`),
  so a restored portal loses its flags and documents and falls back to the configured defaults. Not a
  requirement; listed as AP1.
- **Rate-limit headers and 429/500/502/503 rows** of the yaml: generic policy.
- **The `last_modified` column** (`Entity:49-51, 72-74`): written on every upsert, never exposed by any operation.
- **The 64-character limit of an identifier** (`AppItem:37`, `Entity:38-41`): a configuration constraint, see AP2.
- **The migration** that created the table (`migrations/mysql/SaaS/MigrationContext/20260622084927_MigrationContext_Upgrade85.cs:210`):
  schema history.

## Suspected defects

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| AP1 | Per-portal storage | `app_settings` is in no backup module: a portal backup silently drops every application flag and settings document; after a restore the portal reads the configured defaults and `getSettings` answers empty. Declared as coverage debt "T6", no ticket landed. | `BackupCoverageTests.cs:99`; no `TableInfo("app_settings", …)` in `common/ASC.Data.Backup.Core/Tasks/Modules/` |
| AP2 | Configuration | An `apps` entry whose `id` is longer than 64 characters is listed and read normally but every write fails at the database (`varchar(64)` / `MaxLength(64)`), surfacing as 500; nothing validates the configuration at start-up. | `AppItem:37`; `Entity:38-41`; `Config:73` (no validation) |
| AP3 | Per-portal storage | The first write of a portal copies the configured `enabled` of that moment into the row (`Enabled = defaults…?.Enabled`). A portal that only ever saved a settings document therefore freezes the default: a later change of the installation's `enabled` default no longer reaches it, while a portal that never wrote anything follows the new default. The two portals report different flags for the same configuration. | `Service:138-167` (`UpsertAsync`); `Service:41-60` (`o?.Enabled ?? d.Enabled`) |
| AP4 | Saving the settings document | `SetSettingsAsync` re-parses the JSON and throws `ArgumentException` ("Settings must be valid JSON", 400) — unreachable, because the controller hands it `JsonElement.GetRawText()`, which is always valid; the documented 400 comes from model binding instead. Dead code that describes a different contract. | `Service:113-123`; `Controller:197-200` |
| AP5 | Dropping the settings document | A body that binds but has no `settings` member (`{}`, `{"setting":{…}}` — a typo) drops the stored document with 200, exactly like an explicit `null`. The client cannot distinguish "reset" from "I mistyped the field"; the tests pin the behaviour (`SetSettings_BodyWithoutSettingsMember_ResetsTheDocumentLikeNull`). A design smell rather than a crash. | `Controller:196-200` (`ValueKind is Undefined or Null`) |
| AP6 | Clients are told when an application is switched | The socket event is queued only after `setEnabled`; a switch and a settings save in a row leave clients with the new flag and the old document until they re-read, which the controller text documents as intended ("clients pick the new settings up on their next read"). Noted here so the asymmetry is visible; no spec change. | `Controller:164, 191-202` |
