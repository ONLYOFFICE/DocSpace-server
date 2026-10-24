# history-and-audit — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); Files test paths omit the `products/ASC.Files/Tests/Tests/` prefix and Web.Api test
paths the `web/ASC.Web.Api.Tests/Tests/05_Security/` prefix. `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml`. Status: **TEST-CONFIRMED** (an integration test asserts
it), **CODE-ONLY** (read from the product code or the OpenAPI document, no test), **NOTES-ONLY** (only
the release notes say it — nothing of that kind was written into the spec; such items are listed at the
end as left out).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Entry history endpoints and the shape of an entry** | | |
| `file/{id}/log`, `folder/{id}/log`, entry fields (`action.id` is the `MessageAction` value), newest first | yaml `getFileHistory`, `getFolderHistory`; `products/ASC.Files/Server/ApiModels/ResponseDto/HistoryDto.cs` (`HistoryDto`, `HistoryApiHelper.ToHistoryAsync` ordering) | CODE-ONLY |
| Third-party file has no history | `HistoryApiHelper.GetEntryEventsAsync` (`ProviderEntry` → empty); yaml `getFileHistory` | CODE-ONLY |
| New room: non-empty, `action.id`, initiator display name, date | `02_Folders/History/FolderHistoryQueryTests.cs` `GetFolderHistory_Room_ReturnsCorrectStructure`; `FolderHistoryPermissionsTests.cs` `GetFolderHistory_Owner_Allowed` | TEST-CONFIRMED |
| Folder id 0 / unknown id → 404 | `FolderHistoryQueryTests.cs` `GetFolderHistory_NonExistentFolderId_Returns404`, `GetFolderHistory_FolderIdZero_Returns404`; `HistoryApiHelper.GetEntryEventsAsync` (`ItemNotFoundException`) | TEST-CONFIRMED |
| Envelope `count` equals the page length | `FolderHistoryQueryTests.cs` `GetFolderHistory_CountField_MatchesArrayLength`; `HistoryApiHelper.GetEntryEventsAsync` (`apiContext.SetCount`) | TEST-CONFIRMED |
| **Entry history paging and date filter** | | |
| `count` 1–100, `startIndex`, `fromDate`/`toDate` in the portal time zone | yaml `getFolderHistory` parameters; `HistoryApiHelper.GetEntryEventsAsync` (`tenantUtil.DateTimeToUtc`) | CODE-ONLY |
| `count=1` → one entry | `FolderHistoryQueryTests.cs` `GetFolderHistory_CountParameter_LimitsReturnedEntries` | TEST-CONFIRMED |
| `startIndex` 0 and 1 give different entries | `FolderHistoryQueryTests.cs` `GetFolderHistory_StartIndex_ShiftsResultSet` | TEST-CONFIRMED |
| `startIndex=99999` → empty list | `FolderHistoryQueryTests.cs` `GetFolderHistory_StartIndexBeyondTotal_ReturnsEmptyArray` | TEST-CONFIRMED |
| Date range around creation contains `RoomCreated` | `FolderHistoryQueryTests.cs` `GetFolderHistory_DateRangeFilter_ContainsEntriesWithinRange` | TEST-CONFIRMED |
| **Who may read an entry history** | | |
| Read right decides; initiator through the account DTO | `HistoryApiHelper.GetEntryEventsAsync` (`fileSecurity.CanReadAsync`), `HistoryDtoHelper.GetAsync` (`EmployeeFullDtoHelper`) | CODE-ONLY |
| Owner, non-member DocSpace admin, Room manager, Read member → 200 on file log and folder log | `01_Files/Log/FileLogPermissionsTests.cs` `GetFileHistory_DocSpaceAdmin_CanReadHistoryInOwnersRoom`, `_RoomAdmin_CanReadHistoryInTheirRoom`, `_UserWithRoomAccess_CanReadHistory`; `FolderHistoryPermissionsTests.cs` `_Owner_Allowed`, `_DocSpaceAdmin_Allowed`, `_UserWithRoomManagerAccess_Allowed`, `_UserWithReadAccess_Allowed` | TEST-CONFIRMED |
| Non-member User / Guest → 403 | `FileLogPermissionsTests.cs` `GetFileHistory_UserWithoutRoomAccess_Returns403`, `_GuestWithoutAccess_Returns403`; `FolderHistoryPermissionsTests.cs` `_UserWithoutRoomAccess_Forbidden`, `_GuestWithoutRoomAccess_Forbidden` | TEST-CONFIRMED |
| Anonymous → 401 | `FileLogPermissionsTests.cs` `GetFileHistory_Unauthenticated_Returns401`; `FolderHistoryPermissionsTests.cs` `GetFolderHistory_Anonymous_Unauthorized` | TEST-CONFIRMED |
| User / Guest with Read: `initiator.profileUrl` empty | `FileLogPermissionsTests.cs` `GetFileHistory_GuestWithRoomAccess_InitiatorHasNoProfileUrl` (bug 81093), `_UserWithRoomAccess_InitiatorHasNoProfileUrl` | TEST-CONFIRMED |
| **Actions that produce a history entry** | | |
| The tracked set and the "nothing else" rule; scenario "The complete tracked set" (the full list) | `products/ASC.Files/Core/Core/History/HistoryService.cs` (`TrackedActions`); `AuditInterpreter.cs` (`_interpreters`, untracked → `null` → dropped by `ToHistoryAsync`) | CODE-ONLY |
| RoomCreated, RoomRenamed, RoomArchived, RoomUnarchived | `FolderHistoryQueryTests.cs` `_NewRoom_ContainsRoomCreated`, `_AfterRename_ContainsRoomRenamed`, `_ArchivedRoom_ContainsRoomArchived`; `FolderHistoryRoomSettingsTests.cs` `_RoomRestoredFromArchive_ContainsRoomUnarchived` | TEST-CONFIRMED |
| FileCreated, FileUploaded (direct and chunked), FileRenamed, FileMoved, FileCopied, FileMovedToTrash, FileDeleted, FileLocked, FileUnlocked, FileIndexChanged | `FolderHistoryFileActionsTests.cs` (one test per action); `FolderHistoryTests.cs` `GetFolderHistory_ContainsFileUploaded_AfterUploadEndpoint` (bug 81623) | TEST-CONFIRMED |
| FolderCreated (room and My documents), FolderRenamed, FolderMoved, FolderCopied, FolderMovedToTrash, FolderDeleted, FolderIndexChanged on the subfolder's own history | `FolderHistoryFileActionsTests.cs`; `FolderHistoryQueryTests.cs` `_SubfolderInMyDocuments_ContainsFolderCreated` | TEST-CONFIRMED |
| Tags, logo, colour, cover, lifetime, indexing, reorder, watermark, deny download | `FolderHistoryRoomSettingsTests.cs` (one test per action, initiator = owner) | TEST-CONFIRMED |
| RoomExternalLinkCreated at Public room creation, Renamed, Revoked (primary), Deleted (additional) | `FolderHistoryRoomSettingsTests.cs` `_RoomCreated_ContainsRoomExternalLinkCreated`, `_LinkRenamed_…`, `_PrimaryLinkRevoked_…`, `_LinkDeleted_…` | TEST-CONFIRMED |
| RoomCreateUser, RoomUpdateAccessForUser, RoomRemoveUser, group actions, RoomChangeOwner, RoomInviteResend | `FolderHistoryMembershipTests.cs` (one test per action); `FolderHistoryQueryTests.cs` `_Sequence_AfterInviteRoleChangeAndRemoval` | TEST-CONFIRMED |
| User type change writes no room entry | `FolderHistoryQueryTests.cs` `GetFolderHistory_UserTypeChange_DoesNotCreateEntry` | TEST-CONFIRMED |
| RoomIndexExportSaved | `FolderHistoryTests.cs` `_ContainsRoomIndexExportSaved_AfterIndexExportCompletes` is **skipped** in the integration-test profile; `HistoryService.TrackedActions` | CODE-ONLY |
| Conversion, FileRestoreVersion, FileVersionRemoved, custom filter, quota actions, AgentCreated/Renamed, form events | `HistoryService.TrackedActions`, `AuditInterpreter._interpreters` | CODE-ONLY |
| **Entries of one action are grouped** | `HistoryApiHelper.ToHistoryAsync` (`GroupBy(x.GetGroupId())`, `Related`, `OrderByDescending(Date)`); yaml `getFolderHistory` ("grouped … `related`") | CODE-ONLY |
| **History of a form-filling folder for a Form filler** | `HistoryApiHelper.GetEntryEventsAsync` (`needFiltering` when `IsFormsFillingFolder` and share is `FillForms`), `HistoryService.FilterFolderActions` / `FilterFileActions` | CODE-ONLY |
| **Folder history report** | | |
| Endpoints, task shape, saved in My documents, same-caller join; scenario "Format and period of the report" (`Xlsx`/`Csv`, portal time zone, whole history when left out) | yaml `createReportFolderHistory`, `getReportFolderHistory`, `terminateReportFolderHistory`; `products/ASC.Files/Server/Api/FoldersController.cs` (`CreateReportFolderHistoryAsync`); `AuditReportWriters.cs` (`SaveToMyDocumentsAsync`) | CODE-ONLY |
| Contents and columns of the report: XLSX limit 200,000 rows (`files:audit-report:xlsx-max-rows`), row-limit note; CSV header row and one row per event | `products/ASC.Files/Core/Services/DocumentBuilderService/AuditReportWriters.cs` (`AuditXlsxReportWriter.MaxRows`, `ReportRowLimitNote`); `AuditReportTask.ProduceAsync` | CODE-ONLY |
| Folder report drops Country, City, Page, Action, Context; IP/Browser/Platform only for a DocSpace admin | `AuditReportWriters.cs` (`AuditReportColumns.Resolve`, `_excludedFromFolderHistory`, `_clientColumns`); `AuditReportTask.ProduceAsync` (`IsDocSpaceAdminAsync`) | CODE-ONLY |
| Room, archived room, subfolder → 200 with task id, status Created/Running | `02_Folders/History/FolderHistoryReportTests.cs` `CreateReport_Room_ReturnsTaskId`, `_ResponseStructure_HasSuccessEnvelopeAndQueuedTask`, `_RoomWithRichHistory_…`, `_ArchivedRoom_…`, `_Subfolder_…` | TEST-CONFIRMED |
| CSV report completes, no error, `resultFileId`, unique rows, file name present | `FolderHistoryReportTests.cs` `CreateReport_Csv_SavesTheRoomHistory` | TEST-CONFIRMED |
| Folder 0 / -1 / unknown → 404 | `FolderHistoryReportTests.cs` `_NonExistentFolderId_Returns404`, `_FolderIdZero_Returns404`, `_NegativeFolderId_Returns404` | TEST-CONFIRMED |
| **Who may export a folder history** | | |
| Read right minus guests; 402 on SaaS when the section is hidden | `products/ASC.Files/Core/Core/Security/FileSecurity.cs` (`HistoryExport` → `Read`, `isGuest` → false, ~l. 1185); `HistoryApiHelper.DemandFolderHistoryReportPermissionAsync` (`BillingException`) | CODE-ONLY |
| Owner, DocSpace admin, Room manager, Editing, Read, Comment → 200 | `02_Folders/History/FolderHistoryReportPermissionsTests.cs` `CreateReport_Owner_Allowed`, `_DocSpaceAdmin_Allowed`, `_RoomAdminWithRoomManagerAccess_Allowed`, `_RoomAdminWithEditingAccess_Allowed`, `_UserWithReadAccess_Allowed`, `_UserWithCommentAccess_Allowed` | TEST-CONFIRMED |
| Guest with Read / without access → 403 | `FolderHistoryReportPermissionsTests.cs` `CreateReport_GuestWithReadAccess_Forbidden` (bug 81592), `_GuestWithoutRoomAccess_Forbidden` | TEST-CONFIRMED |
| Non-member User → 403; anonymous → 401 | `FolderHistoryReportPermissionsTests.cs` `_UserWithoutRoomAccess_Forbidden`, `_Anonymous_Unauthorized` | TEST-CONFIRMED |
| **Version list and editing revisions of a file** | | |
| ReadHistory right of `FileSecurity`; editing and above; third-party excluded | `products/ASC.Files/Core/Core/FileStorageService.cs` (`GetFileHistoryAsync`, `GetEditHistoryAsync`: `CanReadHistoryAsync`, `ProviderEntry`); yaml `getEditHistory`, `getFileVersionInfo` | CODE-ONLY |
| One version, version 1 / group 1, `count` 1; comment "Created", status None; revision fields | `01_Files/History/FileVersionInfoTests.cs` `_NewFile_HasOneVersionInHistory`, `_VersionItem_HasCorrectStructure`, `_FirstVersion_HasCommentCreatedAndFileStatusZero`; `EditHistoryTests.cs` `_Owner_GetsEditHistoryOfNewFile`, `_EditHistoryEntry_HasCorrectStructure`, `_HistoryGrowsAfterVersionIncrement` | TEST-CONFIRMED |
| `GET file/{id}?version=1` returns version 1 | `FileVersionInfoTests.cs` `GetFileInfo_WithVersion_ReturnsSpecificVersion` | TEST-CONFIRMED |
| Revisions: owner, admin / Room admin own room, Editing member → 200; Read, Comment, Guest-Read, non-member → 403 | `01_Files/History/EditHistoryPermissionsTests.cs` (all tests) | TEST-CONFIRMED |
| Version list: own file in My documents for admin, Room admin, User → 200; another's private file, non-member admin on a room file → 403 | `01_Files/History/FileVersionInfoPermissionsTests.cs` | TEST-CONFIRMED |
| Anonymous → 403 on both; unknown file: revisions 404 with message, version list 403 | `EditHistoryPermissionsTests.cs` `_Unauthenticated_CannotGetEditHistory`; `FileVersionInfoPermissionsTests.cs` `_Unauthenticated_Returns403`; `EditHistoryTests.cs` `_NonExistentFile_Returns404` (bug 80962); `FileVersionInfoTests.cs` `_NonExistentFile_Returns403` | TEST-CONFIRMED |
| Archived room: revisions readable | `EditHistoryTests.cs` `GetEditHistory_FileInArchivedRoom_ReturnsSuccess` | TEST-CONFIRMED |
| Viewer opens current version, not `version=1` → 403 | `01_Files/History/OpenFileVersionAccessTests.cs` (bug 80683) | TEST-CONFIRMED |
| **What the audit trail records** | | |
| `MessageAction` `< 2000` = login event, otherwise audit event | `common/ASC.MessagingSystem/Data/MessagesRepository.cs` (`IsLoginEvent`, `AddLoginEventAsync` / `AddAuditEventAsync`, `MessageSenderService`) | CODE-ONLY |
| Event fields (scenario "Audit event shape"), IP port stripped, geolocation, portal time zone; "Special accounts are named": "Anonymous" / "System Account" / "Unknown Account" | `common/services/ASC.AuditTrail/Models/Mappings/EventTypeConverter.cs`; `common/services/ASC.AuditTrail/AuditReportResource.resx` (`GuestAccount` = Anonymous); `common/ASC.Core.Common/GeolocationHelper.cs`; `web/ASC.Web.Api/ApiModels/ResponseDto/AuditEventDto.cs` | CODE-ONLY (naming: release notes v2.6 agree) |
| Event shape after a password-instructions action | `AuditTrail/AuditEventsFilterTests.cs`, `LastAuditEventsTests.cs` (`AssertAuditEventShape`, `TriggerAuditEventAsync`) | TEST-CONFIRMED |
| **Location (`context`) of an audit event** | `EventTypeConverter.Convert(AuditEventQuery…)` (`GetLocationText`, then `Rooms: {RoomTitle}`, `AI agents: {…}`, `RoomOldTitle` for `RoomRenamed`, `RootFolderTitle`); resource keys `*Module`; release notes v3.6 | CODE-ONLY |
| **Reading the audit trail** | | |
| `events/last` 20 within lifetime, limited text; "Filter parameters" (action override, target rule, count default 100, startIndex after filters); "Portal without the audit option" (no audit option → last 20; 402) | `web/ASC.Web.Api/Api/SecurityController.cs` (`GetLastAuditEvents`, `GetAuditEventsByFilter`, `DemandBaseAuditPermission`, `DemandAuditPermissionAsync`); `common/services/ASC.AuditTrail/Repositories/AuditEventsRepository.cs` (`GetByFilterWithActionsAsync`); yaml `getAuditEventsByFilter` | CODE-ONLY |
| Owner / DocSpace admin → 200 on `last` and `filter` | `AuditEventsFilterTests.cs` `_Owner_ReturnsAuditEvents`, `_DocSpaceAdmin_ReturnsAuditEvents`; `LastAuditEventsTests.cs` same pair | TEST-CONFIRMED |
| Room admin / User / Guest → 403 "Access denied" on `last`, `filter`, `types`, `mappers` | `AuditEventsFilterTests.cs`, `LastAuditEventsTests.cs`, `AuditTrailTypesTests.cs`, `AuditTrailMappersPermissionsTests.cs` (bug 81861) `_NonAdminMember_ThrowsAccessDenied` | TEST-CONFIRMED |
| Anonymous → 401 on every audit endpoint | the `_Anonymous_ThrowsUnauthorized` tests of the same classes and of `AuditSettingsGetTests`, `AuditSettingsSetTests`, `AuditTrailReportTests` | TEST-CONFIRMED |
| `types` contains `FileCreated` / `Documents`; mappers non-empty | `AuditTrailTypesTests.cs` `_Owner_ReturnsAllTypeCatalogues`, `_DocSpaceAdmin_…`; `AuditTrailMappersTests.cs` | TEST-CONFIRMED |
| **Audit trail and login history reports** | | |
| Period resolution ("Invalid period"), formats, 402; "Report file and audit event": My documents, 200k limit, `*ReportDownloaded` event | `SecurityController.cs` (`ResolveReportPeriod`, `StartAuditReportAsync`, `GetAuditReportStatusAsync`, `TerminateAuditReportAsync`); `AuditReportTask.cs` (`SendDownloadedMessage`); yaml `createAuditTrailReport`, `createLoginHistoryReport` | CODE-ONLY |
| Owner / admin start either report with no period, 7 days, `from` 10 years ago → 200 with id | `AuditTrail/AuditTrailReportTests.cs`, `LoginHistory/LoginHistoryReportTests.cs` (`_Owner_…`, `_DocSpaceAdmin_…`, `_WithPeriod_…`, `_FromBeforeLifetime_…`) | TEST-CONFIRMED |
| `from` > `to`, or `to` 10 years ago → 400 | same classes, `_PeriodEndsBeforeStart_ReturnsBadRequest`, `_PeriodOutsideLifetime_ReturnsBadRequest` | TEST-CONFIRMED |
| Room admin / User / Guest → 403; anonymous → 401 | same classes, `_NonAdminMember_ThrowsAccessDenied` / `_ByRole_ReturnsAccessDenied`, `_Anonymous_…` | TEST-CONFIRMED |
| **Retention settings of the two logs** | | |
| Default 180, "Value outside the range" (1–180 → 400, nothing saved), `AuditSettingsUpdated`, bounds `events/last` and report periods, UI block removed in v2.6 | `common/ASC.Core.Common/Tenants/TenantAuditSettings.cs` (`MaxLifeTime`, `GetDefault`); `SecurityController.SetAuditSettings`, `GetLastAuditEvents`, `ResolveReportPeriod`; release notes v2.6 | CODE-ONLY |
| GET → 200 with values > 0 and `lastModified`; POST 180/180 → echoed | `AuditTrail/AuditSettingsGetTests.cs`, `AuditSettingsSetTests.cs` (`_Owner_…`, `_DocSpaceAdmin_…`) | TEST-CONFIRMED |
| Room admin / User / Guest → 403; anonymous → 401 | same classes | TEST-CONFIRMED |
| **Login actions that are recorded** | | |
| "Ways in and their actions": ViaPassword (hash) / ViaApi (plain) / ViaApiSocialAccount + provider / AuthLinkActivated (key, e-mail) / ViaOAuth; fail actions; "Logout and closed connections are recorded": `Logout`; `UserLogoutActiveConnection*` | `web/ASC.Web.Api/Api/AuthenticationController.cs` (~l. 330-395, `switch (wrapper.LoginType)`); `common/ASC.Core.Common/Messaging/LoginActions.cs`; `common/services/ASC.AuditTrail/Mappers/LoginActionMapper.cs`; `web/ASC.Web.Api/Api/ConnectionsController.cs`; release notes v3.5 | CODE-ONLY |
| Login event fields, IP port stripped, country/city | `EventTypeConverter.Convert(LoginEventQuery…)`; `LoginEventsRepository.GetByFilterAsync` (`AddGeolocationAsync`); `LoginEventDto.cs`; release notes v2.6 | CODE-ONLY |
| `login/filter?action=LoginSuccessViaPassword` non-empty after the owner signs in | `LoginHistory/LoginEventsFilterTests.cs` `GetLoginEventsByFilter_Owner_FiltersByAction` | TEST-CONFIRMED |
| Login event shape | `LoginHistory/LastLoginEventsTests.cs` `_Owner_ReturnsEvents`, `_DocSpaceAdmin_ReturnsEvents` | TEST-CONFIRMED |
| **Reading the login history** | | |
| `login/last` 20; `filter` parameters; fallback without audit option; 402 | `SecurityController.GetLastLoginEvents`, `GetLoginEventsByFilter`; `LoginEventsRepository.GetByFilterAsync`; yaml `getLoginEventsByFilter` | CODE-ONLY |
| Owner / admin see events of DocSpace admin, Room admin and User | `LastLoginEventsTests.cs` `_Owner_SeesEventsForAllUserTypes`, `_DocSpaceAdmin_SeesEventsForAllUserTypes` | TEST-CONFIRMED |
| Filter by `userId`, `count=1`, `userId` before `count` | `LoginEventsFilterTests.cs` `_Owner_FiltersByUserId`, `_DocSpaceAdmin_FiltersByUserId`, `_Owner_FiltersWithCountPagination`, `_Owner_FiltersByUserIdBeforeCount` | TEST-CONFIRMED |
| Room admin / User / Guest → 403; anonymous → 401 | `LastLoginEventsTests.cs`, `LoginEventsFilterTests.cs` (`_ByRole_ReturnsAccessDenied`, `_Anonymous_ReturnsUnauthorized`) | TEST-CONFIRMED |
| **Listing active connections** | | |
| Own successful logins of the last year, item fields; "Current connection is marked and refreshed": `loginEvent` from cookie, own item refreshed and on top, single current-request item when nothing is stored | `ConnectionsController.GetAllActiveConnections`; `common/ASC.Core.Common/Data/DbLoginEventsManager.cs` (`GetLoginEventsAsync`, `AddYears(-1)`, `LoginActions.Success`); yaml `getAllActiveConnections` | CODE-ONLY |
| Every user type → 200, non-empty items, field checks, valid date | `ActiveConnections/GetActiveConnectionsTests.cs` (all four tests) | TEST-CONFIRMED |
| DocSpace admin's list has no owner item | `ActiveConnections/GetActiveConnectionsPermissionsTests.cs` `_DocSpaceAdmin_DoesNotSeeOwnersConnections` | TEST-CONFIRMED |
| Anonymous → 401 | `GetActiveConnectionsPermissionsTests.cs` `_Anonymous_ThrowsUnauthorized` | TEST-CONFIRMED |
| **Ending one connection** | | |
| Marks inactive, invalidates token, sockets; owner/admin rule; `false` when not active | `ConnectionsController.LogOutActiveConnection`; `DbLoginEventsManager.LogOutEventAsync`; yaml `logOutActiveConnection` | CODE-ONLY |
| Own connection → `true`; User's token 401 afterwards | `ActiveConnections/LogOutActiveConnectionTests.cs` `_Member_LogsOutOwnConnection`, `_User_LogsOutOwnConnection_InvalidatesToken` | TEST-CONFIRMED |
| Owner closes a User's, DocSpace admin closes the owner's → `true` | `LogOutActiveConnectionTests.cs` `_Owner_LogsOutAnotherUsersConnection`, `_DocSpaceAdmin_LogsOutAnotherUsersConnection` | TEST-CONFIRMED |
| Room admin / User / Guest on the owner's connection → 403 "Access denied"; anonymous → 401 | `ActiveConnections/LogOutActiveConnectionPermissionsTests.cs` (bug 81824) | TEST-CONFIRMED |
| **Ending every connection except the current one** | | |
| Cookie decides the survivor; bearer token → all closed; audit event | `ConnectionsController.LogOutAllExceptThisConnection`; `DbLoginEventsManager.LogOutAllActiveConnectionsExceptThisAsync`; yaml `logOutAllExceptThisConnection` | CODE-ONLY |
| Second token keeps working, first token → 401; name, `count` 1 | `ActiveConnections/LogOutAllExceptThisTests.cs` `_OtherSession_IsInvalidatedAfterCall`, `_Owner_Succeeds`, `_Member_Succeeds` | TEST-CONFIRMED |
| Anonymous → 401 | `ActiveConnections/LogOutAllExceptThisPermissionsTests.cs` | TEST-CONFIRMED |
| **Ending every connection of an account** | | |
| Own id or DocSpace admin; cookies reset | `ConnectionsController.LogOutAllActiveConnectionsForUser`, `LogOutAllActiveConnections` (`ResetUserCookieAsync`) | CODE-ONLY |
| Owner / admin → 200; User's token 401 afterwards | `ActiveConnections/LogOutAllForUserTests.cs` (all three tests) | TEST-CONFIRMED |
| Room admin / User / Guest naming the owner → 403; anonymous → 401 | `ActiveConnections/LogOutAllForUserPermissionsTests.cs` | TEST-CONFIRMED |
| **Logging out everywhere with a password reset** | | |
| Closes all, `UserSentPasswordChangeInstructions`, confirmation URL, password unchanged | `ConnectionsController.LogOutAllActiveConnectionsChangePassword`; yaml `logOutAllActiveConnectionsChangePassword` | CODE-ONLY |
| Owner: URL parts, `count` 1, own token 401 afterwards; every type gets the URL | `ActiveConnections/LogOutAllChangePasswordTests.cs` | TEST-CONFIRMED |
| Anonymous → 401 | `ActiveConnections/LogOutAllChangePasswordPermissionsTests.cs` | TEST-CONFIRMED |
| **Forwarding audit and login events to syslog** (scenarios "Collector configured" — fields, "Long values are truncated", "Collector not configured") | logger `ASC.Audit`; `common/ASC.MessagingSystem/Core/Sender/AuditLogSender.cs` (fields, limits, sanitising), `DbMessageSender.SendAsync` (called before `MessagesRepository.AddAsync`); `../buildtools/config/nlog.config` (`syslog` target, `ASC.Audit` rule with the `ASC_SYSLOG_SERVER` filter, UDP 514); `Directory.Packages.props` (`NLog.Targets.Syslog`); release notes v4.0 | CODE-ONLY |
| **Suspicious-login notification** (scenario "The three signs"; thresholds `web:suspiciousLogin:*` — `historyLimit`, `failThreshold`, `suspiciousScore`, `failWindowHours`, `freshLoginWindowMinutes`) | `web/ASC.Web.Core/Notify/SuspiciousLoginNotifier.cs` (`CheckAsync`, `DeviceKey`, `SuspiciousLoginNotifierConfiguration` defaults 30 / 3 / 2 / 24 h / 5 min); `web/ASC.Web.Core/CookiesManager.cs` (`AuthenticateMeAndSetCookiesAsync` publishes `SuspiciousLoginIntegrationEvent`); `web/ASC.Web.Studio/IntegrationEvents/EventHandling/SuspiciousLoginIntegrationEventHandler.cs`; `WebstudioNotifyPatternResource.resx` (`pattern_suspicious_login`, `subject_suspicious_login`); release notes v4.0 | CODE-ONLY |

Counts: 88 rows — TEST-CONFIRMED 58, CODE-ONLY 30, NOTES-ONLY 0 (nothing was written into the spec on
the strength of the release notes alone). Spec: 23 requirements, 84 scenarios.

## Where the server differs from the release notes or the OpenAPI text (spec follows the server)

| Source says | Server |
|---|---|
| v2.6: country and city in the Location column "for SaaS" | `GeolocationHelper` resolves the IP against the `dbip_lookup` table on every installation; there is no SaaS gate in the code. On a standalone portal without the table the fields are simply empty. |
| v4.0: history export "available to all user types except guests and anonymous" in every room type | Matches the server (`HistoryExport` → `Read` unless guest), with one addition: on a SaaS portal with the login history section hidden the report answers 402, and the IP / Browser / Platform columns are written only for a DocSpace admin. |
| v3.5: "no duplicates" in the login history after the Action column split | Nothing in the current code or tests shows a deduplication rule; each `AuthenticateMeAsync` writes one login event. Not written into the spec. |
| yaml `getLoginEventsByFilter`: `startIndex` "is applied to the log before the filters" | `LoginEventsRepository.GetByFilterAsync` applies `Where` (user, action, period) first and `Skip`/`Take` afterwards; the spec says "after the filters". |
| yaml `getFileVersionInfo`: "in a room only its managers and content creators may read the history" | `EditHistoryPermissionsTests` pass for a User invited with the Editing role; the spec says "editing access and above", as the `getEditHistory` text does. |
| yaml `getAllActiveConnections`: items "cover the last year" | Confirmed (`DateTime.UtcNow.AddYears(-1)` in `DbLoginEventsManager.GetLoginEventsAsync`), but only events whose action is in `LoginActions.Success` are listed — a `Logout` row never appears. |

## Left out of the spec, and why

- **Data-layer / analytics events** (client-only, per the task brief).
- **UI items of the release notes**: the empty-state stub of the Audit Trail page (v2.6.1), the 2FA banner
  on Login History (v3.5), the day filter toolbar and export button of the History tab (v4.0 — the
  server side, `fromDate`/`toDate` and the report, is specified), the Location tooltip and the Trash
  "Room → Location" column rename (v3.5), the info panel history loader (v3.0).
- **Portal deletion after a year without login or audit events** (v3.1 SaaS): belongs to portal
  lifecycle; `AuditEventsRepository.GetLastEventAsync` / `LoginEventsRepository.GetLastSuccessEventAsync`
  are its inputs only.
- **`audit/types` and `audit/mappers` content beyond the two asserted values**: the full vocabulary
  changes with the portal version and is not worth freezing in a spec.
- **The exact text of each history entry and of `data`** (`HistoryDataDto`, the per-action interpreters in
  `products/ASC.Files/Core/Core/History/Interpreters`): the tests assert only `action.id` and the initiator.
- **Form events in the entry history** (`FormSubmit`, `FormStartedToFill`, …): listed as tracked actions
  and cross-referenced to `form-filling-workflow`; their triggering rules live there.
- **`messaging:enabled`**: when the configuration switches messaging off, nothing is recorded or
  forwarded (`DbMessageSender`); treated as deployment configuration, not product behaviour.
- **Login-event cache and socket disconnect details** (`LoginEventsCache`, `QuotaSocketManager.LogoutSession`):
  implementation of "the session's sockets are disconnected".
- **The `logoutall/{userId}` audit action choice** (`UserLogoutActiveConnections` when the caller names
  their own id, `…ForUser` otherwise): mentioned once in the recorded-actions requirement, not a scenario.

## Suspected defects (not written into the spec)

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| H1 | `GET security/audit/events/filter` | A `moduleType` that at least one product does not provide (any value other than the default — the Login product has only the `None` location) makes `AuditEventsRepository.GetByFilterWithActionsAsync` dereference a `null` module mapper (`mappers.Mappers.Find(...)` → `moduleMapper.Actions`), so the request answers 500 instead of the filtered list. The OpenAPI text documents `moduleType=Files` as a valid filter. | `common/services/ASC.AuditTrail/Repositories/AuditEventsRepository.cs`, `locationType` branch; `LoginActionMapper.cs` (`Location = LocationType.None` only). No test covers `moduleType`. |
| H2 | `GET files/file/{id}/history`, `GET files/file/{id}/edit/history` | An anonymous caller is answered with 403 instead of 401 (the security failure is raised as `InvalidOperationException` inside `FileStorageService`), and the version list of a file that does not exist answers 403 instead of 404 (`CanReadHistoryAsync(null)`), while the revisions of the same id answer 404. | `01_Files/History/FileVersionInfoPermissionsTests.cs` `_Unauthenticated_Returns403`, `EditHistoryPermissionsTests.cs` `_Unauthenticated_CannotGetEditHistory`, `FileVersionInfoTests.cs` `_NonExistentFile_Returns403` (the tests assert the current behaviour); `FileStorageService.GetFileHistoryAsync` / `GetEditHistoryAsync`. |
| H3 | `PUT security/activeconnections/logout/{loginEventId}` | The owner check runs only when the login event has a `UserId`; an active login event stored without one (`loginEvent.UserId.HasValue` false) can be closed by any signed-in user of the portal. | `web/ASC.Web.Api/Api/ConnectionsController.cs` `LogOutActiveConnection`. |
| H4 | `POST security/audit/settings/lifetime` | The out-of-range check for `loginHistoryLifeTime` also fires when the body or `settings` is missing, so a missing body is reported as "LoginHistoryLifeTime" (400) rather than as a missing body; harmless but misleading. | `SecurityController.SetAuditSettings` (`inDto?.Settings?.LoginHistoryLifeTime is not (> 0 and <= MaxLifeTime)`). |
| H5 | `GET security/audit/login/filter`, `events/filter` | On a portal without the audit option of its plan the filters are silently dropped and the 20 most recent events are returned with 200, so a client cannot tell a filtered empty result from "filtering not available". The OpenAPI text documents it; recorded here as a design smell rather than a defect. | `SecurityController.GetLoginEventsByFilter`, `GetAuditEventsByFilter`. |
