# storage-and-integrations — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Paths are relative to the repo root (`server/`). `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`. Aliases:
`TP/` = `products/ASC.Files/Tests/Tests/03_Rooms/ThirdParty/`; `FS/` = `products/ASC.Files/Tests/Tests/07_Settings/Storage/`;
`ST/` = `web/ASC.Web.Api.Tests/Tests/02_Settings/Storage/`; `DC/` = `web/ASC.Web.Api.Tests/Tests/02_Settings/DocsCloud/`;
`OA/` = `web/ASC.Web.Api.Tests/Tests/03_Authentication/ThirdPartyOAuth/`; `TS-TP` =
`../tests/api-tests/src/tests/rooms/thirdparty/thirdparty.permission.spec.ts`; `FC` =
`products/ASC.Files/Server/Api/ThirdpartyController.cs`; `FSS` = `products/ASC.Files/Core/Core/FileStorageService.cs`;
`PAD` = `products/ASC.Files/Core/Core/Thirdparty/ProviderAccountDao.cs`; `TC` =
`products/ASC.Files/Core/Helpers/ThirdpartyConfiguration.cs`; `SC` = `web/ASC.Web.Api/Api/Settings/StorageController.cs`;
`MSC` = `common/ASC.Data.Storage/Migration/MigrateServiceClient.cs`; `DCC` =
`web/ASC.Web.Api/Api/Settings/DocsCloudController.cs`; `PH` = `web/ASC.Web.Api/Core/PaymentHelper.cs`; `TPC` =
`web/ASC.Web.Api/Api/ThirdPartyController.cs`.

Status: **TEST-CONFIRMED** = a .NET test's assertion was read and states the claim. **CODE-ONLY** = read from the code or
the OpenAPI text, no .NET test; where the TypeScript suite asserts it, `TS-TP:line` is added (only its status line was
read, it is a claim). No test was run for this document: a test-confirmed row says what the test asserts, not that it
passes today. Nextcloud-gated tests (`RequireNextcloud`) run only where the Nextcloud settings are published; the
`integration-test` profile does that. The Docs Connect behind `settings/docscloud` and the billing service are absent
from the test host, so only guards and validation are tested there; `DocsCloudClient` itself was not read, and its
rows rest on `[SwaggerResponse]` and the yaml.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Storage services a portal offers** | | |
| Known names, non-empty name and key | `TP/ThirdPartyProvidersTests.cs:60` | TEST-CONFIRMED [run: PASS] |
| Nextcloud key `WebDav`, oauth false | `TP/ThirdPartyProvidersTests.cs:74` | TEST-CONFIRMED [run: PASS] |
| kDrive `requiredConnectionUrl` true | `TP/ThirdPartyKDriveConnectBugTests.cs:62` | TEST-CONFIRMED [run: PASS] |
| Configured OAuth service has redirect URL and client id (skips where not configured) | `TP/ThirdPartyProvidersTests.cs:89` | TEST-CONFIRMED [run: SKIP] |
| All five presets share key `WebDav`; which require the address | `TC:107-164` | CODE-ONLY |
| **WebDAV family can be left out** | `TP/ThirdPartyProvidersTests.cs:115,129` | TEST-CONFIRMED [run: PASS] |
| **Provider lists need a signed-in non-guest** | | |
| Anonymous 401 on both | `TP/ThirdPartyProvidersTests.cs:143,174` | TEST-CONFIRMED [run: PASS] |
| Guest or switch off: 200 and `[]` | `FC:424-427` | CODE-ONLY |
| **Compact list** | | |
| `WebDav` and `kDrive` as first items | `TP/ThirdPartyProvidersTests.cs:161` | TEST-CONFIRMED [run: PASS] |
| OAuth elements add client id and redirect URL | `TC:170-190` | CODE-ONLY |
| **Connecting a third-party account** | | |
| Credentials checked before the insert | `PAD:150-153` | CODE-ONLY |
| `providerId` > 0 and listed | `TP/ThirdPartyCredentialIsolationTests.cs:89` (arrange assertion), `:136` | TEST-CONFIRMED [run: PASS] |
| Admin and room admin 200; user and guest 403; anonymous 401 | `FSS:3060`; `TS-TP:55,75,95,115,131` | CODE-ONLY |
| Switch off: 403 (the requirement is in `files-settings`) | `FS/ThirdPartyAccessTests.cs:116` | TEST-CONFIRMED [run: PASS] |
| Always room storage: the controller sets it, no request field | `FC:299-311` | CODE-ONLY |
| **Rejected credentials store nothing** | | |
| Wrong password 403 "Access denied" | `TP/ThirdPartyCredentialIsolationTests.cs:54` | TEST-CONFIRMED [run: PASS] |
| No session carried over; title not saved (bug 83399) | `TP/ThirdPartyCredentialIsolationTests.cs:89` | TEST-CONFIRMED [run: PASS] |
| Invalid Box code below 500 (bug 83265) | `TP/ThirdPartyProviderConnectBugTests.cs:52` | TEST-CONFIRMED [run: PASS] |
| **Server address of a new connection** (bug 84672) | `TP/ThirdPartyKDriveConnectBugTests.cs:86` (kDrive only; other WebDAV presets by comment `PAD:500`) | TEST-CONFIRMED [run: PASS] |
| **Re-authenticating and renaming** | | |
| Rename (bug 83303) | `TP/ThirdPartyProviderConnectBugTests.cs:80` | TEST-CONFIRMED [run: PASS] |
| Another member 403 | `FSS:3107-3111` | CODE-ONLY |
| Unknown id 404 | `PAD:71-76` | CODE-ONLY |
| Room account keeps its address | `PAD:201` | CODE-ONLY |
| Account without address not refused for the address | `TP/ThirdPartyKDriveConnectBugTests.cs:112` | TEST-CONFIRMED [run: PASS] |
| **A member's list** | | |
| No `auth_data` property | `TP/ThirdPartyCredentialIsolationTests.cs:136` | TEST-CONFIRMED [run: PASS] |
| Own accounts only, no backup account | `products/ASC.Files/Core/Core/EF/Queries/ProviderAccountQueries.cs:115-125` | CODE-ONLY |
| Empty for a user; anonymous 401 | `TS-TP:148,157` + the query above | CODE-ONLY |
| **Disconnecting** | | |
| Returned folder id, delete rights check, removal of security, tags and group links | `FC:135-146`, `FSS:3241-3276`, `PAD:436-457` | CODE-ONLY |
| Unrelated user 403; anonymous 401 | `FSS:3253`; `TS-TP:193,208` | CODE-ONLY |
| Unknown id 404 | `PAD:71-76` | CODE-ONLY |
| Room group still listed (bug 83264) | `TP/ThirdPartyProviderDeletionBugTests.cs:88` | TEST-CONFIRMED [run: PASS] |
| **Account behind a room** (bug 83305) | `TP/ThirdPartyProviderDeletionBugTests.cs:54` | TEST-CONFIRMED [run: PASS] |
| **Legacy Common section** | `TP/ThirdPartyProvidersTests.cs:188` | TEST-CONFIRMED [run: PASS] |
| **Backup storage account** | | |
| Admin-only | `FC:339-342` | CODE-ONLY |
| Switch off 403, single account, update path checks credentials | `FSS:3185-3228`, `PAD:369-434` | CODE-ONLY |
| **Reading the backup account** | `FSS` `GetBackupThirdPartyAsync` (read 2996-3013 in this revision), `FC:239-249` | CODE-ONLY |
| **Backup account removes its schedule** | `FC:140-143` | CODE-ONLY |
| **Content of a connected account** | | |
| New file fields | `TP/ThirdPartyFileTests.cs:55` | TEST-CONFIRMED [run: PASS] |
| Delete final, then 404 | `TP/ThirdPartyFileTests.cs:186` | TEST-CONFIRMED [run: PASS] |
| Copy from My documents | `TP/ThirdPartyFileTests.cs:211` | TEST-CONFIRMED [run: PASS] |
| **Recent section** (bug 84528) | | |
| Removal | `TP/ThirdPartyRecentDeleteTests.cs:53` | TEST-CONFIRMED [run: PASS] |
| Missing from the service | `TP/ThirdPartyRecentDeleteTests.cs:81` | TEST-CONFIRMED [run: PASS] |
| `openEditFile` not failing | `TP/ThirdPartyRecentMissingFileTests.cs:61` | TEST-CONFIRMED [run: PASS] |
| **OAuth consent URL** | | |
| Owner and user 200 with the consent address; anonymous 401 | `OA/ThirdPartyOAuthGetCodeTests.cs:45`, `OA/ThirdPartyOAuthGetCodePermissionsTests.cs:43,57` | TEST-CONFIRMED [run: PASS] |
| Other provider 200 no URL; out of range 400 | `OA/ThirdPartyOAuthGetCodeTests.cs:60,74` | TEST-CONFIRMED [run: PASS] |
| `desktop` state | `OA/ThirdPartyOAuthGetCodeTests.cs:87` | TEST-CONFIRMED [run: PASS] |
| The seven providers | `TPC:83-104` | CODE-ONLY |
| **Access to the storage settings** | | |
| Anonymous 401 on all six | `ST/StoragePermissionsTests.cs:145` | TEST-CONFIRMED [run: PASS] |
| Hosted portal 403 for the owner | `web/ASC.Web.Core/Utility/TenantExtra.cs:108-114`; remark in `ST/StorageTestBase.cs` | CODE-ONLY |
| **Data storages** | | |
| Owner gets a list (non-null only) | `ST/StorageReadTests.cs:75` | TEST-CONFIRMED [run: PASS] |
| Fields and meaning of no `current` entry | yaml `getAllStorages`, `SC:91-105` | CODE-ONLY |
| **Switching the data storage** | | |
| Not configured 400 | `ST/StorageUpdateTests.cs:43` | TEST-CONFIRMED [run: PASS] |
| Empty body 400, both routes | `ST/StorageUpdateTests.cs:68` | TEST-CONFIRMED [run: PASS] |
| Same module no-op; other: Migrating and request published | `SC:414-423`, `SC:648-655` | CODE-ONLY |
| **Returning to built-in storage** | `SC:452-473`; the test base says it is deliberately not called | CODE-ONLY |
| **Migration progress** | | |
| User 403, anonymous 401 | `ST/StoragePermissionsTests.cs:111,125` | TEST-CONFIRMED [run: PASS] |
| Hosted -1; lapsed portals allowed | `SC:127-136` | CODE-ONLY |
| **CDN storages** | `ST/StorageReadTests.cs:85`; `SC:498-512` | TEST-CONFIRMED [run: PASS] |
| **Selecting a CDN** | | |
| Not configured 400 | `ST/StorageUpdateTests.cs:54` | TEST-CONFIRMED [run: PASS] |
| Empty body 400 | `ST/StorageUpdateTests.cs:68` | TEST-CONFIRMED [run: PASS] |
| Same module no-op; publish | `SC:549-562`, `MSC:101-112` | CODE-ONLY |
| **Clearing CDN** | `SC:592-599` | CODE-ONLY |
| **Amazon S3 regions** | `ST/StorageReadTests.cs:42`; `ST/StoragePermissionsTests.cs:43,56` | TEST-CONFIRMED [run: PASS] |
| **Starting storage encryption** | `SC:167-226`; 415 from yaml `startStorageEncryption` | CODE-ONLY |
| **What a started encryption does** | `SC:228-283` | CODE-ONLY |
| **Reading the encryption settings** | | |
| Owner: empty `password` | `ST/EncryptionSettingsGetTests.cs:46` | TEST-CONFIRMED [run: PASS] |
| Any failure answers 200 empty | `SC:307-334` | CODE-ONLY |
| **Encryption progress** | `SC:356-374`, yaml `getStorageEncryptionProgress` | CODE-ONLY |
| **Access to Docs Connect tenant operations** | | |
| 403 "Access denied" for the three roles, 401 anonymous | `DC/DocsCloudTrialPermissionsTests.cs:50,57`, `DC/DocsCloudTenantGetPermissionsTests.cs:48,55`, `DC/DocsCloudTenantInfoPermissionsTests.cs:48,55`, `DC/DocsCloudTenantConfigGetPermissionsTests.cs:48,55`, `DC/DocsCloudTenantConfigUpdatePermissionsTests.cs:48,56`, `DC/DocsCloudTenantQuotaPermissionsTests.cs:48,55`, `DC/DocsCloudQuotaReportCreatePermissionsTests.cs:48,55`, `DC/DocsCloudQuotaReportGetPermissionsTests.cs:48,55`, `DC/DocsCloudQuotaReportTerminatePermissionsTests.cs:48,55`, `DC/DocsCloudTenantUsagePermissionsTests.cs:48,55`; assertions in `DC/DocsCloudTestBase.cs` | TEST-CONFIRMED [run: PASS] |
| **Docs Connect trial** | `DCC:88-139`, CSP `DCC:555-569` | CODE-ONLY |
| **Moving to Dev Pack** | | |
| Anonymous 401 | `DC/DocsCloudCalculateDevPackPermissionsTests.cs:55`, `DC/DocsCloudSwitchToDevPackPermissionsTests.cs:55` | TEST-CONFIRMED [run: PASS] |
| No billing service 403 for every signed-in caller | remark in `DC/DocsCloudCalculateDevPackPermissionsTests.cs`; `PH:136-140` | CODE-ONLY |
| Preconditions and minimum | `DCC:579-` (`PrepareSwitchAsync`) | CODE-ONLY |
| Serialized switches | `DCC:175-180` | CODE-ONLY |
| **Reading the tenant** | `DCC:244-249`, `DCC:268-300`, `DCC:330-335`, `DCC:398-403`, `DCC:547`, yaml; 400 without a tenant from `[SwaggerResponse]` only | CODE-ONLY |
| **Updating the configuration** | | |
| 256 characters 400 naming the field (bug 83327) | `DC/DocsCloudTenantConfigValidationTests.cs:53,70,87,104` | TEST-CONFIRMED [run: PASS] |
| 9999999999 gives 400 (bug 83326) | `DC/DocsCloudTenantConfigValidationTests.cs:127` | TEST-CONFIRMED [run: PASS] |
| Audit entry | `PH:553-560` | CODE-ONLY |
| **Quota report** | | |
| Start, owner and admin | `DC/DocsCloudQuotaReportCreateTests.cs:48,67` | TEST-CONFIRMED [run: PASS] |
| Cancel, owner and admin | `DC/DocsCloudQuotaReportTerminateTests.cs:47,58` | TEST-CONFIRMED [run: PASS] |
| Own job only; no tenant check at start | `DCC:429-493`; yaml | CODE-ONLY |

## Differences between the yaml and the server

| Operation | Yaml says | Server does | Evidence |
|---|---|---|---|
| `getThirdPartyAccounts` | `auth_data` comes back empty | the property is absent | `TP/ThirdPartyCredentialIsolationTests.cs:136` |
| `updateStorage`, `resetStorageToDefault` | "starts an asynchronous migration" | publishes a request nothing in this repository consumes (SI1) | LSP references, `MSC:94` |
| `updateCdnStorage` | "saved"; upload "handed to the storage service" | not saved, and no consumer (SI2) | `SC:549-572`, `MSC:101-112` |
| `getStorageProgress` | "the call fails" for an unknown migration | yes, by a null dereference: 500 (SI3) | `MSC:114-118`, `MSC` `ServiceClientListener.GetProgress` |
| `startStorageEncryption` | declares 402 "pricing plan does not include" | no code path answers 402 in the action | `SC:167-261` |
| `startStorageEncryption` | 403 only for rights | on a hosted portal the answer is 403 before the rights check | `SC:194-197` |
| `getAllProviders` vs `getCapabilities` | "take `providerKey` from providers" | kDrive and Yandex have key `WebDav` there, own keys in capabilities (SI5) | `TC:107-164`, `TP/ThirdPartyKDriveConnectBugTests.cs` |
| `getThirdPartyCode` | any other value 200, no URL | a value outside the enumeration is 400 | `OA/ThirdPartyOAuthGetCodeTests.cs:74` |

## Left out

| Item | Why |
|---|---|
| `getAllBackupStorages` | owned by `backup-and-restore` |
| Permission switch `changeAccessToThirdparty` | `files-settings` |
| `createRoomThirdParty` and room errors (`TP/ThirdPartyRoomTests.cs`, `TP/ThirdPartyRoomCreationBugTests.cs`, `TP/ThirdPartyPrivateRoomTests.cs`) | `room-types`, `private-rooms` |
| File and folder tests beyond the three scenarios (`TP/ThirdPartyFolderTests.cs`, rename, upload, move; `products/ASC.Files/Tests/Tests/06_Operations/Move/ThirdPartyMoveBugTests.cs`) | same ordinary operations as `file-operations` |
| How encryption runs inside (password, workers, order of files) | the brief limits this to the API |
| Wordpress operations in `FC` and `GetThirdPartyProviderCode` | hidden from the OpenAPI document |

No operation of the owned tags is left out: all 31 are named in a requirement.

## Suspected defects

| # | Where | What the server does | Evidence | Evidence kind |
|---|---|---|---|---|
| SI1 | `updateStorage`, `resetStorageToDefault` | Puts the portal into Migrating and publishes a request no code in this repository subscribes to. The only code that moves data and restores Active has no caller. The portal looks stuck in Migrating and the settings are never saved. | `SC:648-655`; LSP `findReferences` on the message type: only the class and the publishing client (`MSC:82,87,94,123`); on `StorageUploader.StartAsync`: declaration only. A consumer outside this repository cannot be excluded. | red by code reading |
| SI2 | `updateCdnStorage` | Answers with the new `module` and `props`, but the action never saves them, the message omits `props`, and nothing subscribes to the message. | `SC:549-572`; `MSC:101-112`; LSP references on the message type | red by code reading |
| SI3 | `getStorageProgress` on a server installation | With no migration remembered the cache returns null and the value is dereferenced: 500. With SI1 no progress is ever published, so this is every call. | `MSC:114-118` | red by code reading |
| SI4 | Migration operation, if it were started | On failure the task is marked failed but the portal status stays Migrating. | `common/ASC.Data.Storage/StorageUploader.cs:193-204` | red by code reading |
| SI5 | `getAllProviders` Yandex entry | Key `WebDav` without `requiredConnectionUrl`, while a WebDAV connection without an address is refused. A client following the document cannot connect Yandex that way. The refusal itself was read only as a comment. | `TC:107-164`; `PAD:500` comment | red by code reading |
| SI6 | `saveThirdParty` WebDAV `url` | The address is not checked for loopback, link-local or private ranges before the outbound request. Bug 82560; the test is skipped while it is open. | `products/ASC.Files/Tests/Tests/04_Security/Ssrf/ThirdPartySsrfTests.cs:59` | per test comment |
| SI7 | `updateStorage`, `updateCdnStorage` | `props` is not required, but is read unconditionally: a configured module with no `props` gives a null dereference, 500 instead of 400. | `SC:420`, `SC:556`; `web/ASC.Web.Api/ApiModels/RequestsDto/StorageRequestsDto.cs:60` | red by code reading |
| SI8 | `saveThirdPartyBackup` second call | The new title is stored without the invalid-character cleaning a new account gets. | `PAD:412-415` vs `FSS:3204` | red by code reading |
| SI9 | `startStorageEncryption` | A running backup is reported by a bare exception: 500 with no message, though the condition is an expected one. | `SC:225` | red by code reading |
| SI10 | `getStorageEncryptionSettings` | Catches every exception, so missing rights, a hosted portal and real failures all look like 200 with an empty body. The yaml documents this. | `SC:307-334` | red by code reading |

## Not verified

- No test was run. Bug-numbered tests were read, not executed.
- Non-admin 403 on `getAllStorages` and the CDN and update operations: only read in `SC`; the .NET tests cover anonymous callers.
- Re-saving an account of another section through `saveThirdParty`, which passes room storage as the folder type (`FSS:3139`, `PAD:281-366` unread).
- Whether a second DocSpace admin can read the backup folder: if not, `getBackupThirdPartyAccount` answers empty and `saveThirdPartyBackup` would create a second account (`FSS:3196`), and later `SingleOrDefault` calls would fail.
- `DocsCloudClient`, including the 400 without a tenant and the cache times.
- `consumerFactory.GetByKey` for a key that names no consumer at all.
