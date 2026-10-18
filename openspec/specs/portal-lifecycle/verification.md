# portal-lifecycle — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`, `apisystem.json` is
`common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/json/apisystem_common.json` (the only OpenAPI
document of the system API; nothing under `sdk/` describes it). Code: `WebPortal` is
`web/ASC.Web.Api/Api/PortalController.cs`, `Owner` is `web/ASC.Web.Api/Api/Settings/OwnerController.cs`,
`Settings` is `web/ASC.Web.Api/Api/Settings/SettingsController.cs`, `FirstTime` is
`web/ASC.Web.Api/Core/FirstTimeTenantSettings.cs`, `SysPortal` is
`common/services/ASC.ApiSystem/Controllers/PortalController.cs`, `RegService` is
`common/services/ASC.ApiSystem/Classes/PortalRegistrationService.cs`, `CommonMethods` is
`common/services/ASC.ApiSystem/Classes/CommonMethods.cs`, `TenantModel` is
`common/services/ASC.ApiSystem/Models/TenantModel.cs`, `Validator` is
`common/ASC.Core.Common/Tenants/TenantDomainValidator.cs`, `DbTenantService` is
`common/ASC.Core.Common/Data/DbTenantService.cs`, `StatusFilter` is
`common/ASC.Api.Core/Middleware/TenantStatusFilter.cs`, `ConfirmAuth` is
`common/ASC.Api.Core/Auth/ConfirmAuthHandler.cs`, `KeyProvider` is
`common/ASC.Core.Common/Security/EmailValidationKeyProvider.cs`, `Removal` is
`web/ASC.Web.Core/RemovePortal/PortalRemovalService.cs`, `RemoveOp` is
`web/ASC.Web.Core/RemovePortal/RemovePortalOperation.cs`, `RetentionJob` is
`web/ASC.Web.Core/Notify/PortalRetentionJob.cs`, `RetentionSchedule` is
`web/ASC.Web.Core/Notify/PortalRetentionSchedule.cs`, `Actions` is `web/ASC.Web.Core/Notify/Actions.cs`.
Tests: `Tests/` is `web/ASC.Web.Api.Tests/Tests/`, `Harness` is
`common/Tests/ASC.Tests.Common/ApiFactories/AspireHostFixture.cs`, `Retention/` is
`common/Tests/ASC.Notify.Tests/Periodic/Retention/`, `TS` is `../tests/api-tests/src/tests/`.
Status: **TEST-CONFIRMED** (an integration or letter-suite test asserts it), **CODE-ONLY** (read from the
product code, the configuration or an OpenAPI document, no test), **NOTES-ONLY** (only the release notes
say it — nothing of that kind was written into the spec). Exception → status mapping of the Web.Api:
`SecurityException` 403, `BillingException` 402, `ArgumentException` 400, `ItemNotFoundException` 404,
plain `Exception` 500 (`common/ASC.Api.Core/Middleware/ResponseWrapper.cs`); the system API answers its
own `ErrorDto` with an explicit status. The `confirm` authentication scheme is `ConfirmAuth`: a wrong or
missing key fails authentication (401); an ordinary bearer token on an action that accepts only the
`confirm` scheme is 403.

The .NET suite for this capability is 25 Web.Api tests (portal information, path, users count, suspend
and delete instructions, owner change permissions) plus the registration every test performs through the
harness, and the retention lifecycle suite of the letter tests (block, unblock, removal). Every state
change through a confirmation link in the Web.Api (suspend, continue, delete, owner change, wizard) is
CODE-ONLY: no test holds a mailed link.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Registering a portal through the system API** | | |
| `register` fields, owner created as the only member and admin, 200 with `reference` and `tenant` (`tenantId`, `portalName`, `domain`, `status`, `ownerId`, `language`) | `SysPortal:91-116`; `RegService:85-144` (`HandleRegisterAsync`), `656-704`; `CommonMethods:51-78` (`ToTenantResponseDto`, `Status = t.Status.ToStringFast()`); `DbTenantService:296-340` (owner, password and admin group in one transaction); `TenantModel:39-229`; apisystem.json `register` | CODE-ONLY |
| Harness registers one portal per test with name `t<12 hex>`, en-US, UTC, `skipWelcome`, reads `tenant.tenantId` and `tenant.ownerId`, then the owner authenticates | `Harness:276-322`; `common/Tests/ASC.Tests.Common/Data/PortalRegistration.cs:39-76`; `common/Tests/ASC.Tests.Common/Data/Initializer.cs` (`Authenticate`); TS `apisystem/apisystem.registration.spec.ts:24-100` (200, `tenant.domain`, `tenant.ownerId`, owner login 200, `@self` id equals `ownerId`) | TEST-CONFIRMED |
| Weak password → 400 "passPolicyError"; empty names → 400 "name is required"; invalid name → "name is incorrect" | `RegService:71-83`, `338-351`, `353-369` | CODE-ONLY |
| 500 "registerNewTenantError" when the tenant cannot be created | `RegService:717-740`; `SysPortal:90` (500 row) | CODE-ONLY |
| **Portal name on a hosted and on a server installation** | | |
| Requested name used on standalone only; empty → generated | `RegService:466-476` (`EnsurePortalNameAsync`: `coreBaseSettings.Standalone ? requested : string.Empty`) | CODE-ONLY |
| Generated `<prefix>-<6 chars>`, 100 retries on "portalNameExist", "portalNameNotAvailable" afterwards; by-email and by-OAuth always generate | `RegService:478-513` (`GetRandomPortalNameAsync`, `DefaultPrefix = "docspace"`, `DefaultRandomLength = 6`, `MaxRetries = 100`), `190`, `259` | CODE-ONLY |
| Test harness relies on the chosen name being kept (standalone launch profile) | `Harness:120-121` (`APP_HOSTING_STANDALONE=true`), `279-301` (`PortalName` reused as the `Origin` host) | TEST-CONFIRMED |
| **Rules a portal name has to pass** | | |
| Trim + lower-case; "portalNameEmpty", "tooShortError", "portalNameIncorrect", "portalNameExist" with `variants`; 200 "portalNameReadyToRegister" | `RegService:515-550` (`CheckExistingNamePortalAsync`); `SysPortal:410-436`; apisystem.json `checkExistingNamePortal` | CODE-ONLY |
| 6–63 characters, `^[a-z0-9]([a-z0-9-]){1,61}[a-z0-9]$`, punycode refused, overridable by configuration | `Validator:71-95` (defaults, `web:alias:min/max/regex`), `102-117` | CODE-ONLY |
| Taken = forbidden address, another alias, or another mapped domain (not of a portal pending removal); `variants` are the names sharing the stem with trailing digits cut | `DbTenantService:414-457` (`ValidateDomainAsync`); hosted with cache: `RegService:788-799` (`ValidateTenantAliasAsync`) | CODE-ONLY |
| **Abuse protection of registration** | | |
| Sliding window per client address, "tooMuchAttempts" 400; sign-in 403 without CAPTCHA / 401 with a wrong one | `CommonMethods:252-302` (`CheckMuchRegistration`), `SysPortal:527-560`; `RegService:395-414` | CODE-ONLY |
| CAPTCHA when required, bypass with a known `appKey`; test addresses bypass both | `RegService:371-393` (`GetRecaptchaErrorAsync`); `CommonMethods:240-250` (`IsTestEmail`, pattern from configuration) | CODE-ONLY |
| The suite's owner address `test@example.com` is never throttled | `Harness:286`, `Initializer.OwnerEmail`; a throttled run would fail every test — the suite passes | TEST-CONFIRMED (indirect) |
| **Trial tariff, cache and welcome of a new portal** | | |
| CSP for the new tenant, hosting cache + billing key on a hosted installation, trial tariff from configuration, welcome through `portal/sendcongratulations`, `reference` with `first=true`, `referenceWelcome` when skipped | `RegService:656-704` (`RegisterPortalFlowAsync`), `706-741`, `743-765` (`ApplyTrialQuotaAsync`); `CommonMethods:80-137` (`CreateReference`, `SendCongratulations`, `CallSendMethod` with `skipAndReturnUrl`); `WebPortal:1276-1311` (`sendcongratulations`) | CODE-ONLY |
| No password → wizard pending (standalone) | `RegService:697-702`, `767-783` (`ConfigureWizardForStandaloneAsync`, `Completed = false`); the default of the wizard setting is completed (`web/ASC.Web.Core/Utility/Settings/WizardSettings.cs:57`) | CODE-ONLY |
| **First-run wizard of a server installation** | | |
| `wizardToken` in `getPortalSettings` for an anonymous caller while not completed | `Settings:212-215`; yaml `SettingsDto.wizardToken` | CODE-ONLY |
| `completeWizard`: Wizard link, settings right, fields, effects, 200 with `completed` | `Settings:825-840` (`[Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard")]`); `FirstTime:58-136` (password, e-mail → `NotActivated` when changed, licence on Enterprise, `Completed = true`, language, time zone, CSP, welcome letters, sign-in cookie); `web/ASC.Web.Api/ApiModels/RequestsDto/WizardRequestsDto.cs`; yaml `completeWizard` (55275-55340) | CODE-ONLY |
| Already completed → 500 "Wizard passed."; empty password 500; e-mail failing the server check 500; malformed/missing `email` or missing `passwordHash` 400 (`[EmailAddress]`, `required`) | `FirstTime:63-67`, `88-96`; `WizardRequestsDto` attributes; yaml 400/500 rows | CODE-ONLY |
| No .NET or TS test exercises the wizard: every test portal is registered with a password, so its wizard is completed from the start | `Harness:281-291`; `TS settings/` has no wizard folder | — |
| **Portal information** | | |
| Full record for the settings right, `tenantId` only otherwise, 401 anonymous; `region` from the hosting cache | `WebPortal:117-134`; `web/ASC.Web.Api/ApiModels/ResponseDto/TenantDto.cs`; yaml `getPortalInformation` (50151-50226), `TenantDto` (101284-101430) | CODE-ONLY |
| Owner and DocSpace admin: 200, `tenantId` > 0, `ownerId`, `language`, `timeZone`, `creationDateTime`, `lastModified` set | `Tests/04_Portal/Settings/PortalInformationTests.cs:47-83`; TS `portal/portalSettings/portalSettings.spec.ts:19-59` (also asserts `region` defined on the hosted test environment) | TEST-CONFIRMED |
| Room admin, User, Guest: 200, `tenantId` > 0, `calls` false, empty `ownerId`, default dates | `Tests/04_Portal/Settings/PortalInformationTests.cs:85-105`; TS `portalSettings.spec.ts:61-116` | TEST-CONFIRMED |
| Anonymous 401 | `Tests/04_Portal/Settings/PortalInformationPermissionsTests.cs:46-57` | TEST-CONFIRMED |
| Keeps answering while not paid | `WebPortal:115` (`[AllowNotPayment]`) | CODE-ONLY |
| **Portal path** | | |
| 200 with absolute URL + `virtualPath`; base URL without; every member; 401 anonymous | `WebPortal:783-787`; `Tests/04_Portal/Settings/PortalPathTests.cs:46-89` (owner `/rooms` regex, owner base, DocSpace admin, Room admin, User, Guest 200); `Tests/04_Portal/Settings/PortalPathPermissionsTests.cs:45-56` (401); TS `portalSettings.spec.ts:131-200` | TEST-CONFIRMED |
| **Looking up a portal account** | | |
| `getUserById`: view check 403 "Access denied", system account 404 "User not found" | `WebPortal:156-172`; yaml `/api/2.0/portal/users/{userId}` (50718-50814) | CODE-ONLY |
| `getPortalUsersCount`: owner and DocSpace admin 200 with 5 on a portal of five members; Room admin, User, Guest 403 "Access denied"; anonymous 401 | `Tests/04_Portal/Users/PortalUsersCountTests.cs:53-114`; `WebPortal:565-571`; detailed in `openspec/specs/quotas/spec.md` ("Portal totals") | TEST-CONFIRMED |
| **Renaming a portal** | | |
| Hidden settings section → 402; hosted without customization → 402 "Your pricing plan does not support this option"; settings right; empty/whitespace alias 400; validation through the system API (hosted) or the tenant service (standalone), message wrapped into 400 | `WebPortal:879-926`; `web/ASC.Web.Core/Helpers/ApiSystemHelper.cs:95-115` (`ValidatePortalNameAsync`, "portalNameExist" → address busy); `DbTenantService:414-457` | CODE-ONLY |
| Alias saved, "Portal renamed" audit, CSP rename, hosting cache update, rename letter to every activated member (not on a localhost installation with a mapped domain), every other active member logged out through the socket, caller's cookies cleared, confirmation sign-in link returned; same alias → empty string | `WebPortal:927-978`; `web/ASC.Web.Core/Notify/StudioNotifyService.cs:673-702` (`PortalRenameNotifyAsync`) | CODE-ONLY |
| Not in the published API document; SDK cannot call it, no test | `WebPortal:875` (`[ApiExplorerSettings(IgnoreApi = true)]`); yaml has no `portalrename` path | — |
| **Status values of a portal** | | |
| Eight statuses including Blocked; `status`/`statusChangeDate` in `getPortalInformation`, `tenantStatus` in `getPortalSettings` | `common/ASC.Core.Common/Tenants/TenantStatus.cs:39-75`; `common/ASC.Core.Common/Tenants/Tenant.cs` (`SetStatus` sets `StatusChangeDate = UtcNow`); `TenantDto.cs`; yaml `SettingsDto.tenantStatus` | CODE-ONLY |
| Suspended / Remove pending → 404 unless allowed-while-suspended; Blocked → 404 unless allowed-while-blocked; Transfering/Restoring/Encryption → 403 unless the path ends in one of the pass-through endings | `StatusFilter:40-100`; `WebPortal:1102` (`[AllowSuspended]` on `continue`), `1137` (`[AllowBlocked]` on `unblock`); `Settings` colour theme `[AllowAnonymous, AllowNotPayment, AllowSuspended, AllowBlocked]` | CODE-ONLY |
| Blocked portal: `getSupportedCultures` 404, `getPortalColorTheme` 200 | `Retention/PortalRetentionLifecycleTests.cs:194-240` | TEST-CONFIRMED |
| Suspended portal answers 404 to `getPortalInformation` | `StatusFilter:57-69`; `WebPortal:115-117` carries no `[AllowSuspended]` | CODE-ONLY |
| **Confirmation links of the lifecycle flows** | | |
| One `confirm` role per action: PortalSuspend, PortalContinue, PortalRemove, PortalOwnerChange, PortalUnblock, Wizard | `WebPortal:1104-1106`, `1139-1141`, `1185-1187`, `1221-1223`; `Owner:134-135`; `Settings:829-830`; `common/ASC.Core.Common/Users/ConfirmTypeEnum.cs:39-112` | CODE-ONLY |
| Key validated with the e-mail-link interval (7 days default), holder signed in as the owner for the call, invalid → 401 | `ConfirmAuth:40-108`; `KeyProvider:78-82` (`email:validinterval`, `FromDays(7)`); yaml `updatePortalOwner` description ("seven days by default") | CODE-ONLY |
| Ordinary token → 403, no token → 401 (owner change) | `Tests/02_Settings/Owner/UpdatePortalOwnerPermissionsTests.cs:52-112` (anonymous 401; Room admin, User, Guest 403 — a token, not the link); TS `settings/owner/owner.permissions.spec.ts:72-121` | TEST-CONFIRMED |
| Ordinary token → 403, no token → 401 (unblock) | `Retention/PortalRetentionLifecycleTests.cs:302-314` | TEST-CONFIRMED |
| Link of another type refused | `ConfirmAuth:71-74` (role claim = the link's type; `[Authorize(Roles=...)]` fails without it) | CODE-ONLY |
| **Asking for the portal to be suspended** | | |
| Settings right, then owner check, then last-space rule; letter with both links; "Owner sent portal deactivation instructions"; `[AllowNotPayment]`; sensitive rate limit | `WebPortal:1021-1038`, `1341-1365` (`DemandPermissionToDeleteTenantAsync`); yaml `sendSuspendInstructions` (50322-50440) | CODE-ONLY |
| Owner → 200 (no body) | `Tests/04_Portal/Settings/SendSuspendInstructionsTests.cs:47-54`; TS `portalSettings.spec.ts:119-128` | TEST-CONFIRMED |
| DocSpace admin → 403 "Access denied" (bug 81374 trait) | `Tests/04_Portal/Settings/SendSuspendInstructionsPermissionsTests.cs:61-76` | TEST-CONFIRMED |
| Room admin, User, Guest → 403 "Access denied"; anonymous 401 | `Tests/04_Portal/Settings/SendSuspendInstructionsPermissionsTests.cs:48-59`, `78-95` | TEST-CONFIRMED |
| **Suspending and continuing a portal** | | |
| `suspendPortal`: owner + last-space checks, Suspended, "Portal deactivated", base domain refreshed | `WebPortal:1181-1198`; yaml `suspendPortal` (50441-50483) | CODE-ONLY |
| `continuePortal`: Active, CSP re-saved, base domain refreshed, `[AllowSuspended]`, no owner re-check | `WebPortal:1100-1115`; yaml `continuePortal` (50650-50717) | CODE-ONLY |
| Repeating the link sets the status again with a new date | `WebPortal:1192-1193` (`SetStatus` unconditionally); `Tenant.SetStatus` | CODE-ONLY |
| **Asking for the portal to be deleted** | | |
| Settings right, owner, last-space rule; auto-renew note when hosted, has payments and not trial; no audit event; `[AllowNotPayment]`; rate limit | `WebPortal:1061-1079`; yaml `sendDeleteInstructions` (50484-50602) | CODE-ONLY |
| Owner → 200 | `Tests/04_Portal/Settings/SendDeleteInstructionsTests.cs:47-54`; TS `portalSettings.spec.ts:203-212` | TEST-CONFIRMED |
| DocSpace admin → 403 "Access denied" (bug 81375 trait); Room admin, User, Guest 403; anonymous 401 | `Tests/04_Portal/Settings/SendDeleteInstructionsPermissionsTests.cs:48-95` | TEST-CONFIRMED |
| **Deleting a portal** | | |
| Owner and last-space checks; OAuth clients deleted; alias `<alias>_deleted` with a free suffix, status Remove pending; hosting cache and CSP cache dropped; deleted letter(s); "Portal deleted"; removal event → purge of storage modules, DB row, search index; returns the "registrationcanceled" page | `WebPortal:1217-1251`; `Removal:66-93`; `DbTenantService:538-570` (`InnerRemoveTenantAsync`); `RemoveOp:70-120` (`DoJob`); `web/ASC.Web.Core/RemovePortal/RemovePortalWorker.cs`; yaml `deletePortal` (50603-50649) | CODE-ONLY |
| Old address 404 at once | `StatusFilter:57-69` (Remove pending → 404) | CODE-ONLY |
| Letter failure does not stop the removal; purge handles a portal whose files carry encryption keys | `Retention/PortalRemovalTests.cs:43-90`; `Retention/PortalPurgeTests.cs:45-...`; `Removal:72-93` (`BeforePurgeAsync` swallows) | TEST-CONFIRMED |
| **Immediate deletion for test portals** | | |
| Owner check, test-address pattern, otherwise 403 "Access denied"; same removal path; not in the API document | `WebPortal:985-1002` (`[ApiExplorerSettings(IgnoreApi = true)]`, `IsSecretEmail`); `web/ASC.Web.Core/SetupInfo.cs:114` (`web:autotest:secret-email`) | CODE-ONLY |
| The TS suite deletes its portals this way on the hosted test environment | `../tests/api-tests/src/services/apisystem.ts:88-110`; `../tests/api-tests/src/helpers/apisystem-registration.ts` (`deleteRegisteredPortal`) | TEST-CONFIRMED (200 asserted by the TS cleanup) |
| **Removing and switching a portal through the system API** | | |
| `remove`: standalone only (403), tenant by `portalName`/`tenantId` (400 "portalNameEmpty"/"portalNameNotFound"), last full-access space 400, wizard incomplete → removed at once else `portal/sendremoveinstructions` on the portal, `removed` flag | `SysPortal:257-333`; `CommonMethods:85-88`, `90-136`; `WebPortal:1315-1336` (`sendremoveinstructions`, standalone + owner + PortalRemove key, 1 h) | CODE-ONLY |
| `status`: Active for Active, Suspended for anything else; standalone only | `SysPortal:346-398` | CODE-ONLY |
| `get`: Active portals only, statistics with owner, usage, wizard state; standalone only | `SysPortal:449-508` | CODE-ONLY |
| v3.0 note: "quick deletion of a portal — a portal with an unfinished wizard is deleted without confirmation" | release notes v3.0.0 line 86; matches `SysPortal:319-331` | CODE-ONLY |
| **Asking for the owner to be changed** | | |
| Settings right; owner e-mail unconfirmed → 400; guest → 403; caller not owner / empty id / inactive → 200 `status` 0 "Access denied"; letter, audit, `status` 1 with `mailto:` | `Owner:77-110`; yaml `sendOwnerChangeInstructions` (59547-59640), `OwnerChangeInstructionsDto` (97255-97285) | CODE-ONLY |
| Room admin, User, Guest → 403; anonymous 401 | `Tests/02_Settings/Owner/OwnerChangeInstructionsPermissionsTests.cs:51-115`; TS `owner.permissions.spec.ts:4-69` | TEST-CONFIRMED |
| **Confirming the owner change** | | |
| PortalOwnerChange link; unknown/guest → 500 "User not found"; inactive → 500 "Access denied"; link address must be the owner's → 500 "Access denied"; promotion to DocSpace admin (402 on seats); `ownerId` saved; "Owner updated"; previous owner keeps the admin type | `Owner:129-189` (`UpdateUserTypeAsync`, `SaveTenantAsync`; nothing demotes the previous owner); yaml `updatePortalOwner` (59641-59700) | CODE-ONLY |
| Room admin, User, Guest with a token → 403; anonymous → 401 | `Tests/02_Settings/Owner/UpdatePortalOwnerPermissionsTests.cs:52-112` | TEST-CONFIRMED |
| **Blocking an idle or unpaid portal on a hosted installation** | | |
| Free: block after 60 days; former paying: 90 days after the due date; with wallet balance: 365; counted from the later of activity / due date / status change / policy start; paid tariff or paid add-on left alone; reserved domain left alone; Blocked + "Portal blocked" + letters | `RetentionSchedule:146-199` (defaults, `core:retention`), `217-260` (`DecideActive`); `RetentionJob:181-200`, `226-262`, `314-336`, `528-560`; `Actions:1912-1959` | CODE-ONLY |
| Schedule: free warned at 30 blocked at 60, former paying warned twice blocked at 90, with balance monthly and blocked after a year; activity restarts the count; policy start counts from the first run | `Retention/PortalRetentionScheduleTests.cs:136-230`, `328-360`; `Retention/PortalRetentionJobTests.cs:144-157`, `431-470`, `614-660`, `801-...` (`PayingPortal_IsLeftAlone`) | TEST-CONFIRMED |
| Idle portal blocked, stops answering, owner gets the blocked letter | `Retention/PortalRetentionLifecycleTests.cs:194-215` | TEST-CONFIRMED |
| **Unblocking a portal with the owner's link** | | |
| PortalUnblock link with the block-day postfix, owner only; not Blocked → 401; Active, retention record reset, "Portal unblocked", CSP re-applied, `[AllowBlocked]` | `WebPortal:1137-1167`; `web/ASC.Web.Core/Utility/CommonLinkUtility.cs:216-230`; `Actions:1938-1958` | CODE-ONLY |
| Owner unblocks → 200, Active, answers again, same link again 401 | `Retention/PortalRetentionLifecycleTests.cs:194-240` | TEST-CONFIRMED |
| Link of another block → 401, portal stays Blocked; current link 200 | `Retention/PortalRetentionLifecycleTests.cs:267-300` | TEST-CONFIRMED |
| Not blocked → 401; ordinary token → 403; no token 401 | `Retention/PortalRetentionLifecycleTests.cs:244-265`, `302-314` | TEST-CONFIRMED |
| Not in the published API document | yaml has no `/api/2.0/portal/unblock` and its `ConfirmType` lacks `PortalUnblock` | — |
| **Removing a blocked portal at the end of its retention** | | |
| Retention 30 (free) / 90 days; final reminder a week before, early reminder a month before for the 90-day cases; late final reminder moves the deletion back; `_auto_deleted` alias; deleted letter; support letter for a lapsed tariff; skipped when the status or its date changed | `RetentionSchedule:146-172`, `262-300` (`DecideBlocked`); `RetentionJob:612-646`; `DbTenantService:540`; `Retention/PortalRetentionScheduleTests.cs:146-155`, `189-230`, `303-326` | TEST-CONFIRMED (schedule) |
| Blocked free portal nobody unblocks → Remove pending, purge started, deleted letter | `Retention/PortalRetentionLifecycleTests.cs:317-356` | TEST-CONFIRMED |
| Unblocked after the run read it → not removed; retention of a block unchanged when the wallet empties | `Retention/PortalRetentionLifecycleTests.cs:379-452` | TEST-CONFIRMED |

## Where the server differs from the release notes or the OpenAPI documents (spec follows the server)

| Release note / document | Server |
|---|---|
| yaml `TenantStatus` lists seven values (Active … Encryption) | The enum has an eighth, `Blocked` (`TenantStatus.cs:74`), which `getPortalInformation` and `getPortalSettings` report for a portal blocked by the retention policy. The published document, and the SDKs, cannot deserialize it. |
| yaml `ConfirmType` ends at `GuestShareLink`; no `PUT /api/2.0/portal/unblock` path | `ConfirmTypeEnum.cs:111` adds `PortalUnblock`, and `WebPortal:1139` publishes `unblockPortal` (not marked as ignored). The contract was generated before the retention policy was added. |
| yaml `TenantDto.status` / `SettingsDto.tenantStatus`: "being transferred, restored, encrypted or removed" | Also suspended and blocked; both answer 404 to almost everything (`StatusFilter`). |
| v3.1 notes: "a portal with no activity for a year … the owner gets a letter, a week later the portal is removed" (PR104); `openspec/specs/notifications/spec.md` "Unused portal warnings and removal": free portal idle three months warned monthly, removed six months and a week | The current policy (`RetentionSchedule:146-172`) warns a free portal after 30 idle days, **blocks** it after 60 and removes it 30 days after the block (free), or 90 days for a lapsed tariff or a wallet balance; the owner can unblock it from the letter. The notes and the sibling spec describe earlier versions of the job; the letter subjects stay in `notifications`. |
| yaml `sendOwnerChangeInstructions` 403: "the caller does not hold the portal-settings right of a DocSpace administrator" | Agrees with `Owner:80`; note that a DocSpace admin who holds the right still gets 200 with `status` 0, not 403 — the tests assert the 403 for Room admin, User and Guest only. |
| yaml `updatePortalOwner` 500: "the email address … no longer belongs to the portal owner" | Agrees (`Owner:169-172`); "User not found" and "Access denied" are both plain exceptions → 500, not 404/403. |
| yaml `deletePortal` description: "its domain stops serving the portal" at once | The row is soft-deleted (alias `_deleted`, Remove pending → 404) immediately; files, rows and index go asynchronously in the removal worker (`RemoveOp`). Agrees in effect. |
| apisystem.json `register` 500: "the `portalName` requested on a server installation is already taken" | On a server installation the chosen name is not checked before `RegisterTenantAsync`; the tenant service throws and the answer is 500 "registerNewTenantError" (`RegService:98-105`, `717-740`) — matches the document, but a hosted installation never reaches this because the name is generated. |
| v3.1.1 notes: by-email registration sets "Admin" as the owner's name | The server sets "Administrator" (`RegService:180`, `291`). |
| TS `apisystem` fixture and the .NET harness both register with `email` + `password`; the TS cleanup deletes through `deleteportalimmediately` | Works only because the test owner address matches the installation's test-address pattern (`WebPortal:996`); on an installation without that pattern every such delete is 403. |
| Test names `SendSuspendInstructions_DocSpaceAdmin_ReturnsAccessDenied` / `SendDeleteInstructions_DocSpaceAdmin_ReturnsAccessDenied` carry bug traits 81374 / 81375 | The assertions (403 "Access denied") match the server (`WebPortal:1341-1345` demands the owner); whatever the bugs reported, the tests are green and the traits are stale. |

## Left out of the spec, and why

- **`portal/thumb`** (`WebPortal:797-817`): hidden from the API document, needs a configured thumbnail service
  and returns `null` (an empty 204/200) without one; no test, no fixed behaviour worth a requirement.
- **`settings/version`** (`web/ASC.Web.Api/Api/Settings/VersionController.cs`): hidden from the API document,
  not tested; the build-info endpoint is anonymous and the portal-version switch is dead product history.
- **`portal/mobile/registration`, `portal/present/mark`, `portal/tenantextra`, `portal/sendcongratulations`**:
  not lifecycle (device registration, a gift banner flag, licence info, the welcome letter trigger — the
  letter is in `notifications`).
- **Registration by e-mail, by OAuth and `provision`** beyond the shared rules: the third-party-profile
  handling (auto-generated addresses, account linking, OAuth consumer configuration) belongs to
  authentication/federated login; only the name generation and the error codes they share are specified.
- **`portal/signin`, `portal/licensequota`, `portal/licensequota/report`** of the system API: sign-in
  discovery and Document Server licence reporting, not the portal object.
- **The system API `settings/get`, `settings/save`, `settings/checkdomain`**: per-tenant key/value settings and
  the DNS check of a mapped domain — custom domains are a settings capability.
- **"Mark as not available" on SaaS**: no operation of that name exists in either API; the only server-side
  "unavailable" state is `Blocked`, set by the retention job and lifted by `unblockPortal`, which is
  specified. The system API `PUT portal/status` switches Active/Suspended on a server installation only.
- **Letter texts, subjects and recipients** of deactivation, deletion, owner change, rename, welcome and the
  retention warnings: `notifications` (its retention row is outdated, see the deviations table).
- **Audit event bodies and the audit-trail API**: `history-and-audit`; the spec names the events only.
- **Billing conditions** (customization tariff for the rename, paid seats for the owner promotion, the
  "former paying" and "wallet balance" categories): named where they gate an operation, mechanics elsewhere.
- **Transfering, Restoring, Migrating, Encryption statuses**: set by the migration, restore and encryption
  jobs (`backup-and-restore`); only their effect on requests is specified here.
- **The retention job's run bookkeeping** (catch-up of at most seven days, first-wave spreading over 30 days,
  accounting time-outs, incomplete runs): implementation of the daily job, covered by
  `Retention/PortalRetentionJobTests.cs`; the spec states the day counts only.

## Suspected defects

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| T1 | Contract | `Blocked` is missing from the published `TenantStatus`, `PortalUnblock` from `ConfirmType`, and `PUT portal/unblock` from the paths, although the controller publishes all three; a generated SDK client fails to read `getPortalInformation`/`getPortalSettings` of a blocked portal (unknown enum value) and cannot call the unblock. | yaml 102001-102020, 92491-92512; `WebPortal:1137-1141`; `TenantStatus.cs:74`; `ConfirmTypeEnum.cs:111` |
| T2 | Confirming the owner change | Every failure is a plain `Exception` → 500 ("User not found" for an unknown id or a guest, "Access denied" for an inactive member or a link whose address is not the owner's). Business refusals are reported as server errors; the sibling `sendOwnerChangeInstructions` uses 400/403 for the same conditions. | `Owner:148-172` |
| T3 | Confirming the owner change | The previous owner is left as a DocSpace admin and nothing records the handover on their account; the new owner is promoted with `UpdateUserTypeAsync` *before* `ownerId` is saved, so a failure in `SaveTenantAsync` leaves a promoted member and the old owner — the two writes are not atomic. | `Owner:176-188` |
| T4 | Suspend / delete instructions, wizard, portal removal | The "last full-access space" rule and the "wizard passed" check throw plain `Exception` → 500 with a resource message; a 403 (rule) or 409/400 (already done) would describe them. The yaml documents the 500s as intended. | `WebPortal:1364`; `FirstTime:66` |
| T5 | Renaming a portal | The alias is saved, the audit written and every other member logged out *before* the hosting cache and the rename letters are updated; a failure in the cache update leaves a hosted portal reachable under neither name until the cache is repaired. The whole action is also absent from the published contract. | `WebPortal:927-963` |
| T6 | Renaming a portal | The response is a confirmation **sign-in** link of type `Auth` for the caller under the new domain, so the rename answers with a credential-bearing URL in the body of a 200; the body is also what the audit log of the proxy sees. | `WebPortal:980-994` |
| T7 | Registering through the system API | On a server installation a requested `portalName` is validated only inside `RegisterTenantAsync`, so a taken or malformed name answers 500 "registerNewTenantError" instead of the 400 codes that `validateportalname` would give for the same input. | `RegService:98-105`, `717-740` |
| T8 | Deleting a portal / immediate deletion | `deleteportalimmediately` lets any owner whose address matches the test-address pattern of the installation delete the portal with an ordinary session and no confirmation; the pattern is a regular expression in configuration, and a loose one (e.g. matching a whole domain) turns every owner of that domain into a one-call deleter. | `WebPortal:985-1002`; `SetupInfo.cs:114` |
| T9 | Status filter | A Suspended portal still answers `getPortalColorTheme` 200 and `continuePortal`; a Remove-pending portal answers 404 to everything including `continuePortal`, so the yaml's claim that `continuePortal` is idempotent holds only for Suspended/Active — fine — but the suspended portal's 404 for `getPortalInformation` means a client cannot read `status` to learn *why* it gets 404 (the yaml tells it to). | `StatusFilter:57-69`; yaml `suspendPortal` description ("read `status` from `GET api/2.0/portal`") |
| T10 | Portal name uniqueness | Uniqueness against mapped domains excludes portals pending removal, but uniqueness against aliases does not: a removed portal keeps `<alias>_deleted` forever (until the purge drops the row), so `variants` of a later `validateportalname` leak the aliases of deleted portals that share the stem. | `DbTenantService:451-453`, `538-570` |
