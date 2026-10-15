# quotas — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); Files test paths omit the `products/ASC.Files/Tests/Tests/` prefix, People test paths the
`products/ASC.People/Tests/Tests/` prefix and Web.Api test paths the `web/ASC.Web.Api.Tests/Tests/` prefix.
`yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`. Status: **TEST-CONFIRMED** (an integration test
asserts it), **CODE-ONLY** (read from the product code or the OpenAPI document, no test), **NOTES-ONLY**
(only the release notes say it — nothing of that kind was written into the spec; such items are listed at
the end as left out).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Portal storage allowance and what is charged against it** | | |
| `maxTotalSize` of the tariff is the allowance; `total_size` feature | `web/ASC.Web.Api/Api/PortalController.cs:718-731`; `common/ASC.Core.Common/Quota/Features/MaxTotalSizeFeature.cs:36-38`; yaml `getPortalQuota`, `getQuotaPaymentInformation` | CODE-ONLY |
| Per-owner quota rows, total = sum of rows with a product tag | `common/ASC.Data.Storage/TenantQuotaController.cs:101-108, 256-267`; `web/ASC.Web.Core/Quota/MaxTotalSizeFeature.cs:47-57`; `PortalController.cs:539-547` | CODE-ONLY |
| Creating a file grows the portal total by `pureContentLength`; ten concurrent files by the sum | `02_Folders/UsedSpace/QuotaUsedSpaceTests.cs:64-130` | TEST-CONFIRMED |
| Trash keeps the total; delete from Trash / empty Trash / batch delete releases it | `QuotaUsedSpaceTests.cs:132-456` | TEST-CONFIRMED |
| Reads and renames do not move the counters (bug 81648) | `02_Folders/UsedSpace/FilesUsedSpaceTests.cs:234-251, 284-299` | TEST-CONFIRMED |
| Thumbnails (`thumb*`) excluded when a stored file's size is summed | `products/ASC.Files/Core/Core/Dao/TeamlabDao/FileDao.cs:1061, 1184, 2327`; `common/ASC.Data.Storage/DiscStorage/DiscDataStore.cs:499-506`; release note v1.1 | CODE-ONLY |
| **Portal storage allowance enforced on uploads** | | |
| Remaining allowance = `maxTotalSize` − used, capped by `maxFileSize`; file above it → `TenantQuotaException` "The maximum file size is exceeded (<remaining>)" | `web/ASC.Web.Core/SetupInfo.cs:156-166`; `FileDao.cs:442-455`; `web/ASC.Web.Core/FileSizeComment.cs:38-41, 60-63`; `web/ASC.Web.Core/PublicResources/Resource.resx:958` | CODE-ONLY |
| `TenantQuotaException` / `BillingException` → 402 | `common/ASC.Api.Core/Middleware/UnhandledExceptionMiddleware.cs:92-95` | CODE-ONLY |
| Chunked upload refused during chunk upload answers 402 with "quota" in the message | `07_Settings/Quota/QuotaTests.cs:591-650` (room quota case; same exception path) | TEST-CONFIRMED |
| Strict for every non-editor write; grace only on the editor save path | `common/ASC.Data.Storage/IQuotaController.cs:38-42`; `TenantQuotaController.cs:232-249`; `FileDao.cs:450-455` | CODE-ONLY |
| **Editor save may overrun once; editor opens read-only** | | |
| Grace = `web:available-file-size`, default 100 MiB; only when used ≤ limit and file ≤ `maxFileSize`; serialized by `tenant_quota_{tenantId}` lock; audit "saved but tenant quota exceeded" | `FileDao.cs:403-436, 442-475`; `common/ASC.Data.Storage/TenantQuotaController.cs:34-44, 237-249`; `common/ASC.Data.Storage/Constants.cs:45`; `products/ASC.Files/Core/Utils/EntryManager.cs:1675-1684`; release notes v3.7, v4.0 | CODE-ONLY |
| `quota_exceeded` scope `tenant` raised by the quota-row update | `TenantQuotaController.cs:109-146`; `common/ASC.Core.Common/Quota/QuotaSocketManager.cs:153-175` | CODE-ONLY |
| Editor opens without edit right with `quotaExceededScope` Tenant / Room / User | `products/ASC.Files/Core/Services/DocumentService/DocumentServiceHelper.cs:114-173`; `products/ASC.Files/Server/Api/EditorController.cs:291-300`; `products/ASC.Files/Server/ApiModels/ResponseDto/ConfigurationDto.cs:135-139`; release note v3.7 | CODE-ONLY |
| **Default user quota of the portal** | | |
| Owner / DocSpace admin read `getUserQuotaSettings` (200, `lastModified` set) | `02_Settings/Quota/UserQuotaSettingsTests.cs:45-74` | TEST-CONFIRMED |
| Room admin / User / Guest 403, anonymous 401 | `UserQuotaSettingsTests.cs:76-106` | TEST-CONFIRMED |
| `POST settings/userquotasettings` hidden from the document (`IgnoreApi`), raw-HTTP write succeeds and `resetUsersQuota` falls back to it | `web/ASC.Web.Api/Api/Settings/SettingsController.cs:309-316`; `07_Quota/QuotaTestBase.cs:53-63`; `07_Quota/ResetUserQuotaTests.cs:42-58` | TEST-CONFIRMED |
| Validation: not a whole number / > `maxTotalSize` / > Standalone quota → 400 (`ArgumentException`); SaaS without `statistic` → 402; ≤ 0 stored as 0; `If-Modified-Since` 304; audit record | `SettingsController.cs:316-367, 386-393, 1545-1554`; yaml `getUserQuotaSettings` | CODE-ONLY |
| **Custom quota of an account** | | |
| Owner sets 100 MB on self and on admin / Room admin / User; reset gives 500 MB default, order kept | `07_Quota/UpdateUserQuotaTests.cs:41-77`; `07_Quota/ResetUserQuotaTests.cs:42-86` | TEST-CONFIRMED |
| DocSpace admin changes and resets the owner's quota | `UpdateUserQuotaTests.cs:79-103`; `ResetUserQuotaTests.cs:88-116` | TEST-CONFIRMED |
| Room admin / User / Guest 403 "Access denied" on self and others; anonymous 401 (update and reset) | `07_Quota/QuotaPermissionsTests.cs:43-245` | TEST-CONFIRMED |
| Quota above the total storage → 400 with the exact message (bug 80301), reproduced via `setTenantQuotaSettings` cap | `QuotaPermissionsTests.cs:255-273`; `QuotaTestBase.cs:89-98` | TEST-CONFIRMED |
| Negative value removes the personal limit; system accounts dropped; `resetUsersQuota` 402 without `statistic` on SaaS; answer carries `quotaLimit`/`usedSpace`/`isCustomQuota` | `products/ASC.People/Server/Api/UserController.cs:2812-2868, 2894-2920`; yaml `updateUserQuota`, `resetUsersQuota` | CODE-ONLY |
| **Behaviour when the user quota is exceeded** | | |
| Limit = custom or portal default, `-1` no limit; used = own quota rows; soft overrun once below 2× with socket + audit, else 402 "User space quota exceeded (<limit>)" | `FileDao.cs:532-587`; `FileSizeComment.cs:54-58, 70-73`; `Resource.resx:1348`; `common/ASC.Core.Common/Users/UserQuotaSettings.cs:36-50` | CODE-ONLY |
| Move / copy / restore into My documents or a room refused outright when the file does not fit; `checkdestfolder` reports the message | `FileDao.cs:1298-1335`; `products/ASC.Files/Core/Utils/EntryManager.cs:1855-1897`; `products/ASC.Files/Core/Services/WCFService/FileOperations/CopyPermissionsCheck.cs:501-520` | CODE-ONLY |
| Move between two accounts' My documents raises the receiver's custom quota instead of refusing | `FileDao.cs:1338-1356` | CODE-ONLY |
| **Default room quota and default AI agent quota** | | |
| Owner / DocSpace admin enable with 1 GB and disable `saveRoomQuotaSettings` | `02_Settings/Quota/RoomQuotaSettingsTests.cs:48-114` | TEST-CONFIRMED |
| Same for `saveAiAgentQuotaSettings` | `02_Settings/Quota/AiAgentQuotaSettingsTests.cs:48-114` | TEST-CONFIRMED |
| Room admin / User / Guest 403, anonymous 401 (both settings) | `RoomQuotaSettingsTests.cs:116-150`; `AiAgentQuotaSettingsTests.cs:116-150` | TEST-CONFIRMED |
| Validation: > `maxTotalSize` / > Standalone quota → plain `Exception` (500); 402 without `statistic`; ≤ 0 stored as 0; audit | `SettingsController.cs:415-460, 482-525, 1545-1554`; yaml `saveRoomQuotaSettings`, `saveAiAgentQuotaSettings` | CODE-ONLY |
| Custom quota on a room/agent refused while the feature is off (`InvalidOperationException` → 403) | `products/ASC.Files/Core/Core/FileStorageService.cs:909-921, 1327-1383`; `07_Settings/Quota/RoomQuotaPermissionsTests.cs`, `RoomQuotaResetTests.cs` (asserted in `room-types`) | TEST-CONFIRMED |
| `aiAgentsUpdateQuota` / `aiAgentsResetQuota` relay to the AI service | yaml l. 5687-5790 | CODE-ONLY |
| `usersQuota` / `roomsQuota` / `aiAgentsQuota` / `tenantCustomQuota` on every quota DTO (Standalone or `statistic`) | `web/ASC.Web.Api/Core/QuotaHelper.cs:144-157` | CODE-ONLY |
| **Portal-wide quota of a Standalone installation** | | |
| Owner caps the test portal at 1 MB → 200, then 100 MB per-user quota → 400 | `07_Quota/QuotaTestBase.cs:89-98`; `QuotaPermissionsTests.cs:255-273` | TEST-CONFIRMED |
| Negative removes; non-admin or SaaS → `NotSupportedException` (415); 402 without `statistic`; socket to admins; audit | `SettingsController.cs:605-644`; yaml `setTenantQuotaSettings` | CODE-ONLY |
| Enforcement: 402 "The used storage size should not exceed <quota>" when already above or above 2×; soft overrun below 2× | `TenantQuotaController.cs:211-230`; `Resource.resx:1279` | CODE-ONLY |
| **Used space by section** | | |
| Four mandatory sections with titles "Files", "Trash", "Rooms", "Archive", values ≥ 0 | `02_Folders/UsedSpace/FilesUsedSpaceTests.cs:61-95, 167-181, 301-318` | TEST-CONFIRMED |
| Forms, AI agents, room templates, default templates sections when the root exists | `products/ASC.Files/Core/Core/Dao/TeamlabDao/FolderDao.cs:430-485`; `FilesUsedSpaceTests.cs:188-203` (AI agents shape when present) | CODE-ONLY |
| Create in My documents / in a room / trash / archive move the right counter; soft delete conserves | `FilesUsedSpaceTests.cs:97-165, 324-346`; `QuotaUsedSpaceTests.cs:64-108, 132-187` | TEST-CONFIRMED |
| Hard delete decreases My documents (bug 81648); pre-existing files counted on first call | `FilesUsedSpaceTests.cs:210-227, 353-368` | TEST-CONFIRMED |
| Deep trees, batch delete and four concurrent deletes return the counters exactly | `QuotaUsedSpaceTests.cs:189-223, 283-456` | TEST-CONFIRMED |
| Form filling room content charged to Forms, not Rooms; archive/unarchive/delete move it | `03_Rooms/FormFilling/FormFillingRoomUsedSpaceTests.cs:51-161` | TEST-CONFIRMED |
| Owner / DocSpace admin 200; Room admin / User / Guest 403; anonymous 401 | `02_Folders/UsedSpace/FilesUsedSpacePermissionsTests.cs:47-121`; `FilesUsedSpaceTests.cs:48-59` | TEST-CONFIRMED |
| Counters incremental, can lag the operation | `02_Folders/UsedSpace/UsedSpaceTestBase.cs:115-140` (polling rationale); yaml `getFilesUsedSpace` | CODE-ONLY |
| **Portal quota and usage statistics** | | |
| `getPortalUsedSpace` ≥ 0 for owner and DocSpace admin; GB rounded to 2 decimals | `04_Portal/Quota/PortalUsedSpaceTests.cs:46-73`; `PortalController.cs:539-547` | TEST-CONFIRMED |
| `getPortalUsersCount` = 5 with one of every role; active state only | `04_Portal/Users/PortalUsersCountTests.cs:52-81`; `PortalController.cs:569-573` | TEST-CONFIRMED |
| 403 "Access denied" for Room admin / User / Guest, 401 anonymous on quota, quota/right, usedspace, userscount | `04_Portal/Quota/PortalQuotaPermissionsTests.cs:45-77` (bug 81340); `PortalRightQuotaPermissionsTests.cs:45-76`; `PortalUsedSpacePermissionsTests.cs:45-76`; `PortalUsersCountTests.cs:83-114` | TEST-CONFIRMED |
| `getRightQuota`: cheapest monthly quota above user count and used space, empty body when none | `PortalController.cs:753-765`; yaml `getRightQuota`; `04_Portal/Quota/PortalRightQuotaTests.cs:52-54` (commented out: returns null on this host) | CODE-ONLY |
| `getPortalTariff` graded by role: owner / DocSpace admin full (`state` Paid, `id` ≥ 0, `quotas` non-empty); Room admin / User / Guest `id` 0; anonymous 401 | `04_Portal/Quota/PortalTariffTests.cs:53-93`; `PortalTariffPermissionsTests.cs:47-84`; `PortalController.cs:596-627` | TEST-CONFIRMED |
| `getQuotaPaymentInformation`: Room admin and User 200 with features, User's only `used` is `total_size`; Guest 403; anonymous 401 (bug 81534) | `04_Portal/Payments/PaymentQuotaInformationTests.cs:86-156`; `QuotaHelper.cs:162-265` | TEST-CONFIRMED |
| **Count limits of the tariff** | | |
| Feature ids `room`, `manager`, `users`, `aiagent`; `-1` unlimited; checkers throw `TenantQuotaException` with "should not exceed" messages | `common/ASC.Core.Common/Quota/ITenantQuotaFeatureChecker.cs:43-90`; `products/ASC.Files/Core/Core/CountRoomFeature.cs:36-123`; `web/ASC.Web.Core/Quota/CountRoomAdminFeature.cs:38-72`; `web/ASC.Web.Core/Quota/CountUserFeature.cs:38-69`; `Resource.resx:1210-1264` | CODE-ONLY |
| Invitation link of a paid role when `manager` is reached → 402 (bug 81564); skipped on an unlimited plan | `04_Portal/Users/InvitationLinkGetTests.cs:115-184`; `PortalController.cs:346-349` | TEST-CONFIRMED (skips itself on the integration host: `manager` is `-1` there) |
| Rooms: create, from template, third-party, copy, restore from archive under the rooms-count lock | `FileStorageService.cs:662-682, 754-758, 1017-1024`; `FileMoveCopyOperation.cs:294-314, 518-535` | CODE-ONLY |
| AI agents: `CreateAiAgentAsync` under the agents-count lock | `FileStorageService.cs:684-701` | CODE-ONLY |
| Paid users: user creation, type change, re-enable, SSO, invitation link; bulk invite refused as a whole with "<asked>/<free>" | `common/ASC.Core.Common/Context/Impl/UserManager.cs:376-389`; `UserController.cs:492-504`; LSP references of `CheckAppend` (`UserManagerWrapper.cs:293-331`, `SsoHandler.cs:301`, `UserController.cs:2346-2470`, `PortalController.cs:282, 348`); `Resource.resx:1315` | CODE-ONLY |
| Free users (`users`) checked when a User or Guest is created | `UserManager.cs:378-383`; `CountUserFeature.cs:62-69` | CODE-ONLY |
| Grace / not-paid tariff → `BillingNotFoundException` "Your pricing plan does not support this option" (402) | `CountRoomFeature.cs:47-55, 93-101`; `CountRoomAdminFeature.cs:51-59`; `common/ASC.Core.Common/Billing/BillingClient.cs:577-588`; `Resource.resx:862` | CODE-ONLY |
| User or Guest FOLLOWING a room invitation link that grants a paid role gets the highest free role (link path only; a direct invite with Room manager is 403 by the type rule — live run 2026-10-04 and `invitations` spec) | `products/ASC.Files/Core/Core/VirtualRooms/InvitationService.cs:328-343`; release note v2.0 | CODE-ONLY |
| **Quota recalculation** | | |
| Hidden endpoints, portal-settings right, one job per portal, reuse / restart, what is recomputed and stamped | `web/ASC.Web.Api/Api/Settings/SettingsController.cs:755-785`; `products/ASC.People/Server/Api/UserController.cs:2758-2785`; `products/ASC.Files/Core/Core/UsersQuotaSyncOperation.cs:36-164`; `products/ASC.Files/Core/Configuration/FilesSpaceUsageStatManager.cs:100-175` | CODE-ONLY |
| **Quota changes are announced on the socket** | | |
| `change-quota-used-value` on every counter change; `change-user-quota-used-value` per account and to admins on settings; `quota_exceeded` with scope | `common/ASC.Data.Storage/BaseStorage.cs:383-413`; `QuotaSocketManager.cs:47-66, 143-175`; `SettingsController.cs:355, 632`; `UserController.cs:2852`; `FileMoveCopyOperation.cs:533-534` | CODE-ONLY |

Counts: 59 rows — TEST-CONFIRMED 28, CODE-ONLY 31, NOTES-ONLY 0 (nothing was written into the spec on the
strength of the release notes alone; the v1.1, v2.0, v3.7 and v4.0 items are each backed by code).
Spec: 13 requirements, 37 scenarios.

## Where the server differs from the release notes or the OpenAPI document (spec follows the server)

| Release note / document | Server |
|---|---|
| v2.5: a custom quota can be set "in the info panel, the table column, the menu and the room dialog" | Client surfaces; the server offers `updateUserQuota` / `resetUsersQuota` for accounts and `updateRoomsQuota` / `resetRoomQuota` / `createRoom.quota` for rooms (the latter in `room-types`). |
| v2.6.1: toasts and banners "almost reached" / "exceeded", a toast before an upload into a full room | Client behaviour. The server only exposes the figures (`getQuotaPaymentInformation`, `getRoomInfo.usedSpace`), the `quota_exceeded` socket event and the refusals; no "almost reached" threshold exists server-side. |
| v3.7: "the document is saved once over the quota, further saving is impossible, a banner appears" | Holds for the user and room quota (below 2× the limit) and, since v4.0, for the portal allowance (within `web:available-file-size`); the banner is the client's reaction to `quota_exceeded`. |
| v4.0: "allow a small overrun of the portal storage quota when saving from the editor" | The overrun is bounded by `web:available-file-size` (100 MiB by default), granted once per portal while the portal is still under the limit, and never for an upload, conversion or copy. |
| yaml `saveRoomQuotaSettings` / `saveAiAgentQuotaSettings` list 403 for "`defaultQuota` is not a JSON number" and 500 for a value above the total storage | The code throws a plain `Exception` in both cases, which the middleware maps to 500; the 403 wording comes from the JSON reader, not from the action. The spec states 500 for the over-limit case and leaves the body-shape errors out. |
| yaml `updateUserQuota` says 400 for a value "larger than the portal-wide quota on a standalone installation" | Matches the code (`ArgumentException`); the test (bug 80301) reproduces it through the Standalone cap because the integration host has no tariff ceiling. |
| yaml `getPortalQuota`: "`maxTotalSize` comes back as 0 when the calling account's own role is user" | Dead branch: a User is refused by `DemandPermissionsAsync(EditPortalSettings)` two lines earlier (`PortalController.cs:720-728`), so no caller ever sees the 0. Left out of the spec. |
| `PortalQuotaTests`, `PortalRightQuotaTests`, `PaymentQuotaInformationTests` (startup-plan assertions) | Commented out in the suite: the integration host runs the single "default" quota without a tariff catalogue, so `getPortalQuota` values and `getRightQuota` (null) cannot be asserted there; the spec states only the role rules and the shape. |
| Test name `UpdateRoomsQuota_ZeroQuota_ShouldResetQuota` | The assertion is `quotaLimit == 0`, i.e. 0 is stored as a limit, not a reset (already recorded in `room-types`). |

## Left out of the spec, and why

- **Per-room quota set/reset, `isCustomQuota`, the 2× soft overrun of a room, archived rooms and the
  feature switch** (`07_Settings/Quota/RoomQuota*Tests.cs`, `QuotaTests.cs`): specified in `room-types`;
  this spec refers to them.
- **Billing, wallet, prices, top-up, `getPaymentQuotas`** (`04_Portal/Payments/*`): out of scope; only the
  role-graded `getQuotaPaymentInformation` and the tariff-limit refusals are used.
- **Free backup count (`free_backup`), `docscloud` count and the DocsCloud tenant quota and quota report**
  (`02_Settings/DocsCloud/DocsCloud*Quota*Tests.cs`, `CountFreeBackupChecker`): tariff features of other
  subsystems; the DocsCloud quota report is a user-storage report for the Docs Cloud offering, not the
  portal quota.
- **Invitation limit per day** (`00_Regressions/InvitationLimitTest.cs`, `web:invitation-limit`): a rate
  limit on invitations, not a tariff count; it is `int.MaxValue` on the integration host.
- **`usersInRoom` feature** (`UsersInRoomChecker`, checked when a member joins a room): a per-room member
  cap of the tariff; no test and no release note; belongs with room membership rather than storage quotas.
- **Quota values of the Startup plan** (`maxTotalSize` 2 GiB, 12 rooms, 3 paid users): asserted only in the
  commented-out tests; they are SaaS seed data, not server behaviour.
- **Upload, UploadCheck, EmptyTrash, Move and Copy suites** (`02_Folders/Upload`, `02_Folders/UploadCheck`,
  `06_Operations/EmptyTrash|Move|Copy`): read for quota assertions; none exist there. The Trash and move
  cases of this spec come from `02_Folders/UsedSpace`.
- **`QuotaFilter` (All / Default / Custom)** of the people and rooms listings: a listing filter, specified
  with the listings.
- **LDAP user quota mapping** (v2.6.1 note): LDAP import settings, out of scope.

## Suspected defects

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| Q1 | Default room / AI agent quota | `saveRoomQuotaSettings` and `saveAiAgentQuotaSettings` refuse a `defaultQuota` above the total storage (and a non-integer JSON number) with a plain `Exception`, which the middleware reports as 500 "Internal Server Error"; the sibling `SaveUserQuotaSettings` and `updateUserQuota` throw `ArgumentException` (400) for the same condition. | `SettingsController.cs:419-441, 486-507` vs `316-343`; `UnhandledExceptionMiddleware.cs:71-95`; yaml documents the 500 |
| Q2 | Move between owners | Moving a file from one account's My documents to another's silently raises the receiver's custom quota by the file size instead of refusing or asking; a Room admin moving a large file into a shared folder of another account can grow that account's limit without the portal-settings right. | `FileDao.cs:1338-1356` |
| Q3 | Standalone portal quota | `setTenantQuotaSettings` reports a caller who is not a DocSpace admin, and a hosted portal, with `NotSupportedException`, which the middleware maps to 415 Unsupported Media Type; a permission refusal is expected to be 403. | `SettingsController.cs:609-612`; yaml `setTenantQuotaSettings` 415 |
| Q4 | Per-room quota change | `FolderQuotaChangeAsync` refuses a quota above `maxTotalSize` twice: first with the "greater than the total storage" message, then (unreachably) with the "folder not found" message; both are `InvalidOperationException` (403), so a caller cannot tell the over-limit case from a missing room by status. | `FileStorageService.cs:1334-1337, 1373-1376` |
| Q5 | Portal allowance refusal message | A file that does not fit the remaining portal allowance is refused with "The maximum file size is exceeded (<remaining>)", quoting the remaining space as if it were a per-file limit; the room and user cases say "space quota exceeded". A client cannot distinguish the portal allowance from `maxFileSize` by the message. | `FileDao.cs:444-455`; `SetupInfo.cs:156-166`; `Resource.resx:958` |
| Q6 | `getPortalQuota` | The `maxTotalSize = 0` branch for a User is unreachable behind the portal-settings demand, while the OpenAPI document describes it as behaviour. | `PortalController.cs:720-728`; yaml `getPortalQuota` |
