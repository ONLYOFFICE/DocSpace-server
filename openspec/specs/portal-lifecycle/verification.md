# portal-lifecycle — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

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
harness, and the retention lifecycle suite of the letter tests (block, unblock, removal).

Revision 2026-11-10: the CODE-ONLY rows were worked through with new tests. `Tests/04_Portal/Lifecycle/`
calls the system API raw (registration, `validateportalname`, `status`, `get`, `remove`; the harness gained
`CreateApiSystemClient` and a `CreatePortalAsync` overload that registers without a password), the hidden
`portalrename` and `deleteportalimmediately`, and the wizard through the `wizardToken` an anonymous caller is
given. The confirmation links of suspend and continue are signed in-process by the letter suite
(`common/Tests/ASC.Notify.Tests/Lifecycle/PortalConfirmLinkTests.cs`, written `Lifecycle/` below), the way
the unblock link already was. Still out of reach: the PortalRemove link (the letter stack runs no identity
service, so the deletion fails on the OAuth clients with 500), the PortalOwnerChange link (the letter stack has
no People service to create the new owner, and no portal of the Web.Api stack has an activated owner address),
anything hosted (hosting cache, customization tariff, billing key, region), reCAPTCHA and throttling (the
suite's owner address is the test address that bypasses both), letter bodies, audit events and the rate
limits.

Revision 2026-11-04: re-checked against the merge of `release/v4.1.0` (`d58816bece..3533ad8a89`). None of the
portal, owner, wizard, system-API, registration, removal, status-filter or retention code changed in the range
(`SysPortal` gained one `[RaisesWebhook]` line at 708, below every cited line), and none of the lifecycle tests
changed; the specification text is unchanged. The yaml was regenerated (`5ea4dc6`): `TenantStatus` now lists
`Blocked`, `ConfirmType` `PortalUnblock`, and `PUT /api/2.0/portal/unblock` is published as `unblockPortal`
(T1 fixed); `PUT portal/portalrename` and `DELETE portal/deleteportalimmediately` stay hidden. Yaml line
references were refreshed to the regenerated document.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Registering a portal through the system API** | | |
| `register` fields, owner created as the only member and admin, 200 with `reference` and `tenant` (`tenantId`, `portalName`, `domain`, `status`, `ownerId`, `language`) | `SysPortal:91-116`; `RegService:85-144` (`HandleRegisterAsync`), `656-704`; `CommonMethods:51-78` (`ToTenantResponseDto`, `Status = t.Status.ToStringFast()`); `DbTenantService:296-340` (owner, password and admin group in one transaction); `TenantModel:39-229`; apisystem.json `register`; `Tests/04_Portal/Lifecycle/SystemApiRegistrationTests.cs:60` | TEST-CONFIRMED [run: PASS] (response fields; "owner is the only member and admin" stays CODE-ONLY) |
| Scenario "Fields of the registration body": `portalName`, `firstName`, `lastName`, `email`, `password`/`passwordHash`, `language`, `timeZoneName`, `region`/`awsRegion`, `spam`, `calls`, `skipWelcome` | `TenantModel:39-229`; `RegService:85-144`; apisystem.json `register`; `Tests/04_Portal/Lifecycle/SystemApiRegistrationTests.cs:60`, `79` | TEST-CONFIRMED [run: PASS] for `portalName`, `firstName`, `lastName`, `email`, `password`, `language`, `timeZoneName`, `skipWelcome`; `passwordHash`, `region`/`awsRegion`, `spam`, `calls` CODE-ONLY |
| Harness registers one portal per test with name `t<12 hex>`, en-US, UTC, `skipWelcome`, reads `tenant.tenantId` and `tenant.ownerId`, then the owner authenticates | `Harness:276-322`; `common/Tests/ASC.Tests.Common/Data/PortalRegistration.cs:39-76`; `common/Tests/ASC.Tests.Common/Data/Initializer.cs` (`Authenticate`); TS `apisystem/apisystem.registration.spec.ts:24-100` (200, `tenant.domain`, `tenant.ownerId`, owner login 200, `@self` id equals `ownerId`) | TEST-CONFIRMED [run: NO-LINK] |
| Weak password → 400 "passPolicyError"; empty names → 400 "name is required"; invalid name → "name is incorrect" | `RegService:71-83`, `338-351`, `353-369`; `Tests/04_Portal/Lifecycle/SystemApiRegistrationTests.cs:97`, `106`, `115` | TEST-CONFIRMED [run: PASS] |
| 500 "registerNewTenantError" when the tenant cannot be created | `RegService:717-740`; `SysPortal:90` (500 row); `Tests/04_Portal/Lifecycle/SystemApiRegistrationTests.cs:124` (a taken name on a server installation, T7) | TEST-CONFIRMED [run: PASS] |
| **Portal name on a hosted and on a server installation** | | |
| Requested name used on standalone only; empty → generated | `RegService:466-476` (`EnsurePortalNameAsync`: `coreBaseSettings.Standalone ? requested : string.Empty`); `Tests/04_Portal/Lifecycle/SystemApiRegistrationTests.cs:60` (kept), `88` (empty → generated) | TEST-CONFIRMED [run: PASS] (the hosted side CODE-ONLY) |
| Generated `<prefix>-<6 chars>`, 100 retries on "portalNameExist", "portalNameNotAvailable" afterwards; by-email and by-OAuth always generate | `RegService:478-513` (`GetRandomPortalNameAsync`, `DefaultPrefix = "docspace"`, `DefaultRandomLength = 6`, `MaxRetries = 100`), `190`, `259`; `Tests/04_Portal/Lifecycle/SystemApiRegistrationTests.cs:88` | TEST-CONFIRMED [run: PASS] for the `docspace-` + 6 characters format; the retries and the by-email / OAuth generation CODE-ONLY |
| Test harness relies on the chosen name being kept (standalone launch profile) | `Harness:120-121` (`APP_HOSTING_STANDALONE=true`), `279-301` (`PortalName` reused as the `Origin` host) | TEST-CONFIRMED [run: NO-LINK] |
| **Rules a portal name has to pass** | | |
| Trim + lower-case; "portalNameEmpty", "tooShortError", "portalNameIncorrect", "portalNameExist" with `variants`; 200 "portalNameReadyToRegister" | `RegService:515-550` (`CheckExistingNamePortalAsync`); `SysPortal:410-436`; apisystem.json `checkExistingNamePortal`; `Tests/04_Portal/Lifecycle/SystemApiPortalNameTests.cs:44`, `53`, `70` | TEST-CONFIRMED [run: PASS] |
| Scenario "Rule codes": empty → "portalNameEmpty"; <6 or >63 → "tooShortError"; pattern or punycode → "portalNameIncorrect"; portal / mapped domain / reserved address → "portalNameExist" with `variants` | `RegService:515-550`; `Validator:71-117`; `DbTenantService:414-457`; `Tests/04_Portal/Lifecycle/SystemApiPortalNameTests.cs:53`, `70` | TEST-CONFIRMED [run: PASS] for empty, too short, too long, pattern, punycode and a taken portal with `variants`; mapped domain and reserved address CODE-ONLY |
| 6–63 characters, `^[a-z0-9]([a-z0-9-]){1,61}[a-z0-9]$`, punycode refused, overridable by configuration | `Validator:71-95` (defaults, `web:alias:min/max/regex`), `102-117`; `Tests/04_Portal/Lifecycle/SystemApiPortalNameTests.cs:70`, `79`; `../buildtools/config/appsettings.json` `web:alias:min` 3 | TEST-CONFIRMED [run: PASS] with the override: the stack configures a minimum of 3, so 2 is "tooShortError" and 3 passes; 64 refused, 63 passes; the default 6 CODE-ONLY |
| Taken = forbidden address, another alias, or another mapped domain (not of a portal pending removal); `variants` are the names sharing the stem with trailing digits cut | `DbTenantService:414-457` (`ValidateDomainAsync`); hosted with cache: `RegService:788-799` (`ValidateTenantAliasAsync`); `Tests/04_Portal/Lifecycle/SystemApiPortalNameTests.cs:53`; `Tests/04_Portal/Lifecycle/PortalImmediateDeletionTests.cs:44` (a deleted portal's name is free) | TEST-CONFIRMED [run: PASS] for another alias and its `variants`; forbidden address and mapped domain CODE-ONLY |
| **Abuse protection of registration** | | |
| Sliding window per client address, "tooMuchAttempts" 400; sign-in 403 without CAPTCHA / 401 with a wrong one | `CommonMethods:252-302` (`CheckMuchRegistration`), `SysPortal:527-560`; `RegService:395-414` | CODE-ONLY |
| CAPTCHA when required, bypass with a known `appKey`; test addresses bypass both | `RegService:371-393` (`GetRecaptchaErrorAsync`); `CommonMethods:240-250` (`IsTestEmail`, pattern from configuration) | CODE-ONLY |
| The suite's owner address `test@example.com` is never throttled | `Harness:286`, `Initializer.OwnerEmail`; a throttled run would fail every test — the suite passes | TEST-CONFIRMED [run: NO-LINK] (indirect) |
| **Trial tariff, cache and welcome of a new portal** | | |
| CSP for the new tenant, hosting cache + billing key on a hosted installation, trial tariff from configuration, welcome through `portal/sendcongratulations`, `reference` with `first=true`, `referenceWelcome` when skipped | `RegService:656-704` (`RegisterPortalFlowAsync`), `706-741`, `743-765` (`ApplyTrialQuotaAsync`); `CommonMethods:80-137` (`CreateReference`, `SendCongratulations`, `CallSendMethod` with `skipAndReturnUrl`); `WebPortal:1276-1311` (`sendcongratulations`) | CODE-ONLY |
| Scenario "Welcome mail of a registration with a password": welcome through the portal, `reference` with `first=true`; `skipWelcome` → address returned as `referenceWelcome` | `CommonMethods:80-137`; `RegService:656-704`; `WebPortal:1276-1311`; `Tests/04_Portal/Lifecycle/SystemApiRegistrationTests.cs:79` | TEST-CONFIRMED [run: PASS] for `referenceWelcome` when skipped; the welcome letter CODE-ONLY |
| No password → wizard pending (standalone) | `RegService:697-702`, `767-783` (`ConfigureWizardForStandaloneAsync`, `Completed = false`); the default of the wizard setting is completed (`web/ASC.Web.Core/Utility/Settings/WizardSettings.cs:57`); `Tests/04_Portal/Lifecycle/PortalWizardTests.cs:66` | TEST-CONFIRMED [run: PASS] |
| **First-run wizard of a server installation** | | |
| `wizardToken` in `getPortalSettings` for an anonymous caller while not completed | `Settings:212-215`; yaml `SettingsDto.wizardToken`; `Tests/04_Portal/Lifecycle/PortalWizardTests.cs:66`, `74`, `82` | TEST-CONFIRMED [run: PASS] |
| `completeWizard`: Wizard link, settings right, fields, effects, 200 with `completed` | `Settings:825-840` (`[Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard")]`); `FirstTime:58-136` (password, e-mail → `NotActivated` when changed, licence on Enterprise, `Completed = true`, language, time zone, CSP, welcome letters, sign-in cookie); `web/ASC.Web.Api/ApiModels/RequestsDto/WizardRequestsDto.cs`; yaml `completeWizard` (58616); `Tests/04_Portal/Lifecycle/PortalWizardTests.cs:82`, `113` | TEST-CONFIRMED [run: PASS] for the Wizard link (401 without), `completed` true and the password the owner then signs in with; the other effects (address change, licence, language, CSP, letters, cookie) CODE-ONLY |
| Scenario "Body of the wizard call": `email`, `passwordHash`, `lng`, `timeZone`, `amiId`, `subscribeFromSite`; changed address → unconfirmed; licence accepted on Enterprise | `FirstTime:58-136`; `web/ASC.Web.Api/ApiModels/RequestsDto/WizardRequestsDto.cs`; `Tests/04_Portal/Lifecycle/PortalWizardTests.cs:82` | TEST-CONFIRMED [run: PASS] for `email`, `passwordHash`, `lng`, `timeZone`; `amiId`, `subscribeFromSite`, changed address and licence CODE-ONLY |
| Already completed → 500 "Wizard passed."; empty password 500; e-mail failing the server check 500; malformed/missing `email` or missing `passwordHash` 400 (`[EmailAddress]`, `required`) | `FirstTime:63-67`, `88-96`; `WizardRequestsDto` attributes; yaml 400/500 rows; `Tests/04_Portal/Lifecycle/PortalWizardTests.cs:82`, `124`, `142` | TEST-CONFIRMED [run: PASS] for "Wizard passed." 500, empty password 500, malformed `email` 400; missing fields and the server-side e-mail check CODE-ONLY |
| The harness registers with a password, so a test portal's wizard is completed from the start; `Tests/04_Portal/Lifecycle/PortalWizardTests.cs` registers without one (`CreatePortalAsync(configure)`) to reach the wizard | `Harness:281-291`; `Tests/04_Portal/Lifecycle/PortalWizardTests.cs:66`, `74` | TEST-CONFIRMED [run: PASS] |
| **Portal information** | | |
| Full record for the settings right, `tenantId` only otherwise, 401 anonymous; `region` from the hosting cache | `WebPortal:117-134`; `web/ASC.Web.Api/ApiModels/ResponseDto/TenantDto.cs`; yaml `getPortalInformation` (53428), `TenantDto` (106009) | CODE-ONLY |
| Scenario "Fields of the whole record": `tenantId`, `tenantAlias`, `name`, `ownerId`, `language`, `timeZone`, `industry`, `status`, `statusChangeDate`, `creationDateTime`, `lastModified`, `version`, `mappedDomain`, trusted domains; `region` with the hosting cache on | `WebPortal:117-134`; `web/ASC.Web.Api/ApiModels/ResponseDto/TenantDto.cs`; yaml `TenantDto` (106009) | CODE-ONLY |
| Owner and DocSpace admin: 200, `tenantId` > 0, `ownerId`, `language`, `timeZone`, `creationDateTime`, `lastModified` set | `Tests/04_Portal/Settings/PortalInformationTests.cs:47-83`; TS `portal/portalSettings/portalSettings.spec.ts:19-59` (also asserts `region` defined on the hosted test environment) | TEST-CONFIRMED [run: PASS] |
| Room admin, User, Guest: 200, `tenantId` > 0, `calls` false, empty `ownerId`, default dates | `Tests/04_Portal/Settings/PortalInformationTests.cs:85-105`; TS `portalSettings.spec.ts:61-116` | TEST-CONFIRMED [run: PASS] |
| Anonymous 401 | `Tests/04_Portal/Settings/PortalInformationPermissionsTests.cs:46-57` | TEST-CONFIRMED [run: PASS] |
| Keeps answering while not paid | `WebPortal:115` (`[AllowNotPayment]`) | CODE-ONLY |
| **Portal path** | | |
| 200 with absolute URL + `virtualPath`; base URL without; every member; 401 anonymous | `WebPortal:783-787`; `Tests/04_Portal/Settings/PortalPathTests.cs:46-89` (owner `/rooms` regex, owner base, DocSpace admin, Room admin, User, Guest 200); `Tests/04_Portal/Settings/PortalPathPermissionsTests.cs:45-56` (401); TS `portalSettings.spec.ts:131-200` | TEST-CONFIRMED [run: PASS] |
| **Looking up a portal account** | | |
| `getUserById`: view check 403 "Access denied", system account 404 "User not found" | `WebPortal:156-172`; yaml `getUserById` (54036-54061) | CODE-ONLY |
| `getPortalUsersCount`: owner and DocSpace admin 200 with 5 on a portal of five members; Room admin, User, Guest 403 "Access denied"; anonymous 401 | `Tests/04_Portal/Users/PortalUsersCountTests.cs:53-114`; `WebPortal:565-571`; detailed in `openspec/specs/quotas/spec.md` ("Portal totals") | TEST-CONFIRMED [run: PASS] |
| **Renaming a portal** | | |
| Hidden settings section → 402; hosted without customization → 402 "Your pricing plan does not support this option"; settings right; empty/whitespace alias 400; validation through the system API (hosted) or the tenant service (standalone), message wrapped into 400 | `WebPortal:879-926`; `web/ASC.Web.Core/Helpers/ApiSystemHelper.cs:95-115` (`ValidatePortalNameAsync`, "portalNameExist" → address busy); `DbTenantService:414-457`; `Tests/04_Portal/Lifecycle/PortalRenameTests.cs:51`, `74`, `98`, `112` | TEST-CONFIRMED [run: PASS] for the settings right (Room admin, User 403; DocSpace admin passes), empty / whitespace 400, taken and too short 400 (validation message wrapped); both 402 paths CODE-ONLY (not reachable on the standalone stack) |
| Alias saved, "Portal renamed" audit, CSP rename, hosting cache update, rename letter to every activated member (not on a localhost installation with a mapped domain), every other active member logged out through the socket, caller's cookies cleared, confirmation sign-in link returned; same alias → empty string | `WebPortal:927-978`; `web/ASC.Web.Core/Notify/StudioNotifyService.cs:673-702` (`PortalRenameNotifyAsync`); `Tests/04_Portal/Lifecycle/PortalRenameTests.cs:61`, `124` | TEST-CONFIRMED [run: PASS] for the alias saved lower-cased, the Auth sign-in link under the new domain, same alias → empty string; audit, CSP, cache, letters, logout and cookies CODE-ONLY |
| Scenario "What follows a rename": "Portal renamed" audit, CSP and hosting cache follow the new domain; same alias → empty string, nothing changes | `WebPortal:927-978`; `Tests/04_Portal/Lifecycle/PortalRenameTests.cs:61` | TEST-CONFIRMED [run: PASS] for the same alias → empty string, nothing changes; audit, CSP and cache CODE-ONLY |
| Not in the published API document; SDK cannot call it, no test | `WebPortal:875` (`[ApiExplorerSettings(IgnoreApi = true)]`); yaml `5ea4dc6` still has no `portalrename` path (the route is only named in the `TenantDto.tenantAlias` text, 103894) | — |
| **Status values of a portal** | | |
| Eight statuses including Blocked; `status`/`statusChangeDate` in `getPortalInformation`, `tenantStatus` in `getPortalSettings` | `common/ASC.Core.Common/Tenants/TenantStatus.cs:39-75`; `common/ASC.Core.Common/Tenants/Tenant.cs` (`SetStatus` sets `StatusChangeDate = UtcNow`); `TenantDto.cs`; yaml `SettingsDto.tenantStatus` | CODE-ONLY |
| Suspended / Remove pending → 404 unless allowed-while-suspended; Blocked → 404 unless allowed-while-blocked; Transfering/Restoring/Encryption → 403 unless the path ends in one of the pass-through endings | `StatusFilter:40-100`; `WebPortal:1102` (`[AllowSuspended]` on `continue`), `1137` (`[AllowBlocked]` on `unblock`); `Settings` colour theme `[AllowAnonymous, AllowNotPayment, AllowSuspended, AllowBlocked]`; `Tests/04_Portal/Lifecycle/SystemApiPortalStatusTests.cs:55`; `Lifecycle/PortalConfirmLinkTests.cs:109` | TEST-CONFIRMED [run: PASS] for Suspended (404, colour theme and `continuePortal` pass, `suspendPortal` 404); Blocked by the retention rows; Remove pending (see the deletion row: on a server installation the old address resolves to the default portal) and Transfering/Restoring/Encryption CODE-ONLY |
| Scenario "What each status lets through": Suspended / Remove pending 404 but `continuePortal` and the colour theme; Blocked 404 but `unblockPortal` and the colour theme; Transfering / Restoring / Encryption 403 but progress, settings, theme, logo, build, `@self` | `StatusFilter:40-100`; `WebPortal:1102`, `1137`; `Tests/04_Portal/Lifecycle/SystemApiPortalStatusTests.cs:55`; `Lifecycle/PortalConfirmLinkTests.cs:109` | TEST-CONFIRMED [run: PASS] for the Suspended and (retention suite) Blocked parts; Transfering / Restoring / Encryption CODE-ONLY |
| Blocked portal: `getSupportedCultures` 404, `getPortalColorTheme` 200 | `Retention/PortalRetentionLifecycleTests.cs:194-240` | TEST-CONFIRMED [run: PASS] |
| Suspended portal answers 404 to `getPortalInformation` | `StatusFilter:57-69`; `WebPortal:115-117` carries no `[AllowSuspended]`; `Tests/04_Portal/Lifecycle/SystemApiPortalStatusTests.cs:55` | TEST-CONFIRMED [run: PASS] |
| **Confirmation links of the lifecycle flows** | | |
| One `confirm` role per action: PortalSuspend, PortalContinue, PortalRemove, PortalOwnerChange, PortalUnblock, Wizard | `WebPortal:1104-1106`, `1139-1141`, `1185-1187`, `1221-1223`; `Owner:134-135`; `Settings:829-830`; `common/ASC.Core.Common/Users/ConfirmTypeEnum.cs:39-112`; `Lifecycle/PortalConfirmLinkTests.cs:109`, `145`; `Tests/04_Portal/Lifecycle/PortalWizardTests.cs:82` | TEST-CONFIRMED [run: PASS] for PortalSuspend, PortalContinue, Wizard (and PortalUnblock in the retention suite); PortalRemove and PortalOwnerChange links CODE-ONLY (see the deletion and owner-change rows) |
| Scenario "One link type per operation": `suspendPortal` PortalSuspend, `continuePortal` PortalContinue, `deletePortal` PortalRemove, `updatePortalOwner` PortalOwnerChange, `unblockPortal` PortalUnblock, `completeWizard` Wizard | `WebPortal:1104-1106`, `1139-1141`, `1185-1187`, `1221-1223`; `Owner:134-135`; `Settings:829-830`; `Lifecycle/PortalConfirmLinkTests.cs:109`, `145`; `Tests/04_Portal/Lifecycle/PortalWizardTests.cs:82` | TEST-CONFIRMED [run: PASS] for `suspendPortal`, `continuePortal`, `completeWizard` (`unblockPortal` in the retention suite); `deletePortal`, `updatePortalOwner` CODE-ONLY |
| Key validated with the e-mail-link interval (7 days default), holder signed in as the owner for the call, invalid → 401 | `ConfirmAuth:40-108`; `KeyProvider:78-82` (`email:validinterval`, `FromDays(7)`); yaml `updatePortalOwner` description ("seven days by default"); `Lifecycle/PortalConfirmLinkTests.cs:109`, `161` | TEST-CONFIRMED [run: PASS] for a forged key → 401 and the holder acting as the owner (the owner check of `suspendPortal` passes); the 7-day interval CODE-ONLY |
| Ordinary token → 403, no token → 401 (owner change) | `Tests/02_Settings/Owner/UpdatePortalOwnerPermissionsTests.cs:52-112` (anonymous 401; Room admin, User, Guest 403 — a token, not the link); TS `settings/owner/owner.permissions.spec.ts:72-121` | TEST-CONFIRMED [run: PASS] |
| Ordinary token → 403, no token → 401 (unblock) | `Retention/PortalRetentionLifecycleTests.cs:302-314` | TEST-CONFIRMED [run: PASS] |
| Link of another type refused | `ConfirmAuth:71-74` (role claim = the link's type; `[Authorize(Roles=...)]` fails without it); `Lifecycle/PortalConfirmLinkTests.cs:145` (PortalContinue on `suspendPortal`, PortalSuspend on `deletePortal` → 403) | TEST-CONFIRMED [run: PASS] |
| **Asking for the portal to be suspended** | | |
| Settings right, then owner check, then last-space rule; letter with both links; "Owner sent portal deactivation instructions"; `[AllowNotPayment]`; sensitive rate limit | `WebPortal:1021-1038`, `1341-1365` (`DemandPermissionToDeleteTenantAsync`); yaml `sendSuspendInstructions` (53597) | CODE-ONLY |
| Scenario "Audit, lapsed payment and rate limit of the request" (suspend): "Owner sent portal deactivation instructions"; `[AllowNotPayment]`; sensitive rate limit 5 per 15 min → 429 | `WebPortal:1021-1038`; yaml `sendSuspendInstructions` (53597, 429 row) | CODE-ONLY |
| Owner → 200 (no body) | `Tests/04_Portal/Settings/SendSuspendInstructionsTests.cs:47-54`; TS `portalSettings.spec.ts:119-128` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin → 403 "Access denied" (bug 81374 trait) | `Tests/04_Portal/Settings/SendSuspendInstructionsPermissionsTests.cs:61-76` | TEST-CONFIRMED [run: PASS] |
| Room admin, User, Guest → 403 "Access denied"; anonymous 401 | `Tests/04_Portal/Settings/SendSuspendInstructionsPermissionsTests.cs:48-59`, `78-95` | TEST-CONFIRMED [run: PASS] |
| **Suspending and continuing a portal** | | |
| `suspendPortal`: owner + last-space checks, Suspended, "Portal deactivated", base domain refreshed | `WebPortal:1181-1198`; yaml `suspendPortal` (53691); `Lifecycle/PortalConfirmLinkTests.cs:109` | TEST-CONFIRMED [run: PASS] for the Suspended status set through the link; owner / last-space checks, audit and base domain CODE-ONLY |
| `continuePortal`: Active, CSP re-saved, base domain refreshed, `[AllowSuspended]`, no owner re-check | `WebPortal:1100-1115`; yaml `continuePortal` (53923); `Lifecycle/PortalConfirmLinkTests.cs:109` | TEST-CONFIRMED [run: PASS] for Active on a suspended portal (`[AllowSuspended]`); CSP and base domain CODE-ONLY |
| Scenario "Side effects of suspending and continuing": "Portal deactivated" audit and base domain refreshed on suspend; CSP re-applied on continue | `WebPortal:1181-1198`, `1100-1115` | CODE-ONLY |
| Repeating the link sets the status again with a new date | `WebPortal:1192-1193` (`SetStatus` unconditionally); `Tenant.SetStatus`; `Lifecycle/PortalConfirmLinkTests.cs:109` | TEST-CONFIRMED [run: PASS] after a correction: the link sets the status again with a new date once the portal is active again; on a still suspended portal `suspendPortal` answers 404 (spec scenario corrected) |
| **Asking for the portal to be deleted** | | |
| Settings right, owner, last-space rule; auto-renew note when hosted, has payments and not trial; no audit event; `[AllowNotPayment]`; rate limit | `WebPortal:1061-1079`; yaml `sendDeleteInstructions` (53759) | CODE-ONLY |
| Scenario "Auto-renew note, lapsed payment and rate limit of the request" (delete): auto-renew note when hosted, has payments and not trial; `[AllowNotPayment]`; 5 per 15 min | `WebPortal:1061-1079`; yaml `sendDeleteInstructions` (53759) | CODE-ONLY |
| Owner → 200 | `Tests/04_Portal/Settings/SendDeleteInstructionsTests.cs:47-54`; TS `portalSettings.spec.ts:203-212` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin → 403 "Access denied" (bug 81375 trait); Room admin, User, Guest 403; anonymous 401 | `Tests/04_Portal/Settings/SendDeleteInstructionsPermissionsTests.cs:48-95` | TEST-CONFIRMED [run: PASS] |
| **Deleting a portal** | | |
| Owner and last-space checks; OAuth clients deleted; alias `<alias>_deleted` with a free suffix, status Remove pending; hosting cache and CSP cache dropped; deleted letter(s); "Portal deleted"; removal event → purge of storage modules, DB row, search index; returns the "registrationcanceled" page | `WebPortal:1217-1251`; `Removal:66-93`; `DbTenantService:538-570` (`InnerRemoveTenantAsync`); `RemoveOp:70-120` (`DoJob`); `web/ASC.Web.Core/RemovePortal/RemovePortalWorker.cs`; yaml `deletePortal` (53853); `Tests/04_Portal/Lifecycle/PortalImmediateDeletionTests.cs:44` (the same removal path through `deleteportalimmediately`) | TEST-CONFIRMED [run: PASS] for the removal path freeing the name; the PortalRemove link itself cannot be followed on the letter stack (no identity service: the OAuth-client deletion answers 500), so the alias, audit, caches and letters stay CODE-ONLY |
| Scenario "What else the deletion does": OAuth clients removed; hosting cache and CSP cache dropped; "has been deleted" letter to the owner and, for a paying hosted portal, to support; "Portal deleted" audit | `WebPortal:1217-1251`; `Removal:66-93` | CODE-ONLY |
| Old address 404 at once | `StatusFilter:57-69` (Remove pending → 404); `Tests/04_Portal/Lifecycle/PortalImmediateDeletionTests.cs:44` | TEST-CONFIRMED [run: PASS] after a correction: on a server installation the renamed alias leaves the old address to the default portal (200, not 404); the portal is no longer listed and its name is free at once (spec scenario corrected, T11) |
| Letter failure does not stop the removal; purge handles a portal whose files carry encryption keys | `Retention/PortalRemovalTests.cs:43-90`; `Retention/PortalPurgeTests.cs:45-...`; `Removal:72-93` (`BeforePurgeAsync` swallows) | TEST-CONFIRMED [run: PASS] |
| **Immediate deletion for test portals** | | |
| Owner check, test-address pattern, otherwise 403 "Access denied"; same removal path; not in the API document | `WebPortal:985-1002` (`[ApiExplorerSettings(IgnoreApi = true)]`, `IsSecretEmail`); `web/ASC.Web.Core/SetupInfo.cs:114` (`web:autotest:secret-email`); `Tests/04_Portal/Lifecycle/PortalImmediateDeletionTests.cs:44`, `61` | TEST-CONFIRMED [run: PASS] for the owner with the test address (200, removed) and a DocSpace admin (403); an owner without the test address CODE-ONLY (the stack's pattern is the suite's owner address) |
| The TS suite deletes its portals this way on the hosted test environment | `../tests/api-tests/src/services/apisystem.ts:88-110`; `../tests/api-tests/src/helpers/apisystem-registration.ts` (`deleteRegisteredPortal`) | TEST-CONFIRMED [run: NO-LINK] (200 asserted by the TS cleanup) |
| **Removing and switching a portal through the system API** | | |
| `remove`: standalone only (403), tenant by `portalName`/`tenantId` (400 "portalNameEmpty"/"portalNameNotFound"), last full-access space 400, wizard incomplete → removed at once else `portal/sendremoveinstructions` on the portal, `removed` flag | `SysPortal:257-333`; `CommonMethods:85-88`, `90-136`; `WebPortal:1315-1336` (`sendremoveinstructions`, standalone + owner + PortalRemove key, 1 h); `Tests/04_Portal/Lifecycle/SystemApiPortalStatusTests.cs:127`, `136`, `145`, `156` | TEST-CONFIRMED [run: PASS] for `tenantId`, 400 "portalNameEmpty"/"portalNameNotFound", wizard incomplete → `removed` true, completed → instructions and `removed` false; the 403 of a hosted installation and the last-space 400 CODE-ONLY (the shared stack always holds other full-access portals) |
| Scenario "Refusals of remove and statistics of get": last full-access space → 400 "The last full access space cannot be deleted."; missing / unknown name → 400; `get` with `statistics` → owner, usage, wizard state | `SysPortal:257-333`, `449-508`; `Tests/04_Portal/Lifecycle/SystemApiPortalStatusTests.cs:98`, `127`, `136` | TEST-CONFIRMED [run: PASS] for missing / unknown → 400 and `get` with `statistics` (owner, usage, wizard state); the last-space 400 CODE-ONLY |
| `status`: Active for Active, Suspended for anything else; standalone only | `SysPortal:346-398`; `Tests/04_Portal/Lifecycle/SystemApiPortalStatusTests.cs:55`, `76` | TEST-CONFIRMED [run: PASS] (the hosted 403 CODE-ONLY) |
| `get`: Active portals only, statistics with owner, usage, wizard state; standalone only | `SysPortal:449-508`; `Tests/04_Portal/Lifecycle/SystemApiPortalStatusTests.cs:98`, `114` | TEST-CONFIRMED [run: PASS] (the hosted 403 CODE-ONLY) |
| v3.0 note: "quick deletion of a portal — a portal with an unfinished wizard is deleted without confirmation" | release notes v3.0.0 line 86; matches `SysPortal:319-331`; `Tests/04_Portal/Lifecycle/SystemApiPortalStatusTests.cs:156` | TEST-CONFIRMED [run: PASS] |
| **Asking for the owner to be changed** | | |
| Settings right; owner e-mail unconfirmed → 400; guest → 403; caller not owner / empty id / inactive → 200 `status` 0 "Access denied"; letter, audit, `status` 1 with `mailto:` | `Owner:77-110`; yaml `sendOwnerChangeInstructions` (62896), `OwnerChangeInstructionsDto` (101433); `Tests/02_Settings/Owner/OwnerChangeInstructionsTests.cs:49`, `63` | TEST-CONFIRMED [run: PASS] for the unconfirmed owner address → 400 (owner and DocSpace admin callers); the `status` 0/1 answers CODE-ONLY: no portal of the stack has an activated owner address |
| Scenario "Owner starts the transfer": PortalOwnerChange letter to the owner's address, "Owner sent change owner instructions", 200 with `status` 1 and `mailto:` in `message` | `Owner:77-110`; yaml `OwnerChangeInstructionsDto` (101433) | CODE-ONLY |
| Room admin, User, Guest → 403; anonymous 401 | `Tests/02_Settings/Owner/OwnerChangeInstructionsPermissionsTests.cs:51-115`; TS `owner.permissions.spec.ts:4-69` | TEST-CONFIRMED [run: PASS] |
| **Confirming the owner change** | | |
| PortalOwnerChange link; unknown/guest → 500 "User not found"; inactive → 500 "Access denied"; link address must be the owner's → 500 "Access denied"; promotion to DocSpace admin (402 on seats); `ownerId` saved; "Owner updated"; previous owner keeps the admin type | `Owner:129-189` (`UpdateUserTypeAsync`, `SaveTenantAsync`; nothing demotes the previous owner); yaml `updatePortalOwner` (62981) | CODE-ONLY |
| Scenario "Promotion, audit and the previous owner": non-admin promoted first (402 on seats), "Owner updated" audit, previous owner keeps the admin type | `Owner:129-189` | CODE-ONLY |
| Room admin, User, Guest with a token → 403; anonymous → 401 | `Tests/02_Settings/Owner/UpdatePortalOwnerPermissionsTests.cs:52-112` | TEST-CONFIRMED [run: PASS] |
| **Blocking an idle or unpaid portal on a hosted installation** | | |
| Free: block after 60 days; former paying: 90 days after the due date; with wallet balance: 365; counted from the later of activity / due date / status change / policy start; paid tariff or paid add-on left alone; reserved domain left alone; Blocked + "Portal blocked" + letters | `RetentionSchedule:146-199` (defaults, `core:retention`), `217-260` (`DecideActive`); `RetentionJob:181-200`, `226-262`, `314-336`, `528-560`; `Actions:1912-1959` | CODE-ONLY |
| Scenario "Where the count starts": later of last activity, due date, last status change, policy start | `RetentionSchedule:217-260`; `RetentionJob:226-262`; `Retention/PortalRetentionScheduleTests.cs:328-360` | TEST-CONFIRMED [run: PASS] |
| Schedule: free warned at 30 blocked at 60, former paying warned twice blocked at 90, with balance monthly and blocked after a year; activity restarts the count; policy start counts from the first run | `Retention/PortalRetentionScheduleTests.cs:136-230`, `328-360`; `Retention/PortalRetentionJobTests.cs:144-157`, `431-470`, `614-660`, `801-...` (`PayingPortal_IsLeftAlone`) | TEST-CONFIRMED [run: PASS] |
| Idle portal blocked, stops answering, owner gets the blocked letter | `Retention/PortalRetentionLifecycleTests.cs:194-215` | TEST-CONFIRMED [run: PASS] |
| **Unblocking a portal with the owner's link** | | |
| PortalUnblock link with the block-day postfix, owner only; not Blocked → 401; Active, retention record reset, "Portal unblocked", CSP re-applied, `[AllowBlocked]` | `WebPortal:1137-1167`; `web/ASC.Web.Core/Utility/CommonLinkUtility.cs:216-230`; `Actions:1938-1958` | CODE-ONLY |
| Owner unblocks → 200, Active, answers again, same link again 401 | `Retention/PortalRetentionLifecycleTests.cs:194-240` | TEST-CONFIRMED [run: PASS] |
| Link of another block → 401, portal stays Blocked; current link 200 | `Retention/PortalRetentionLifecycleTests.cs:267-300` | TEST-CONFIRMED [run: PASS] |
| Not blocked → 401; ordinary token → 403; no token 401 | `Retention/PortalRetentionLifecycleTests.cs:244-265`, `302-314` | TEST-CONFIRMED [run: PASS] |
| Published as `unblockPortal` since the contract was regenerated (`5ea4dc6`); no SDK-based test yet | yaml `/api/2.0/portal/unblock` (53968-53993), `ConfirmType` `PortalUnblock` (96330) | — |
| **Removing a blocked portal at the end of its retention** | | |
| Retention 30 (free) / 90 days; final reminder a week before, early reminder a month before for the 90-day cases; late final reminder moves the deletion back; `_auto_deleted` alias; deleted letter; support letter for a lapsed tariff; skipped when the status or its date changed | `RetentionSchedule:146-172`, `262-300` (`DecideBlocked`); `RetentionJob:612-646`; `DbTenantService:540`; `Retention/PortalRetentionScheduleTests.cs:146-155`, `189-230`, `303-326` | TEST-CONFIRMED [run: PASS] (schedule) |
| Scenario "What the removal does": same path as `deletePortal`, alias `<alias>_auto_deleted`, deleted letter, support letter for a lapsed tariff | `RetentionJob:612-646`; `DbTenantService:540`; `Retention/PortalRetentionLifecycleTests.cs:317-356` | TEST-CONFIRMED [run: PASS] (letters CODE-ONLY) |
| Blocked free portal nobody unblocks → Remove pending, purge started, deleted letter | `Retention/PortalRetentionLifecycleTests.cs:317-356` | TEST-CONFIRMED [run: PASS] |
| Unblocked after the run read it → not removed; retention of a block unchanged when the wallet empties | `Retention/PortalRetentionLifecycleTests.cs:379-452` | TEST-CONFIRMED [run: PASS] |

## Where the server differs from the release notes or the OpenAPI documents (spec follows the server)

| Release note / document | Server |
|---|---|
| yaml before `5ea4dc6`: `TenantStatus` with seven values, `ConfirmType` ending at `GuestShareLink`, no `PUT /api/2.0/portal/unblock` path | Resolved by the regenerated contract: `TenantStatus` (106729-106751) lists `Blocked`, `ConfirmType` (96308-96330) `PortalUnblock`, and the path (53968) is published as `unblockPortal`. |
| yaml `TenantDto.status` (106152): "being transferred, restored, encrypted or removed" | Also suspended and blocked; both answer 404 to almost everything (`StatusFilter`). The sibling `SettingsDto.tenantStatus` text (103878-103881) was updated in `5ea4dc6` and names the deactivation and the retention block; the `TenantDto.status` text was not. |
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

Re-checked 2026-11-04 against `d58816bece..3533ad8a89`: T1 is fixed by the regenerated contract; T2–T10 are still open —
none of the code behind them changed in the range.

| # | Where | What the server does | Evidence | 2026-11-04 |
|---|-------|----------------------|----------|------------|
| T1 | Contract | `Blocked` was missing from the published `TenantStatus`, `PortalUnblock` from `ConfirmType`, and `PUT portal/unblock` from the paths, although the controller publishes all three. | yaml 106729-106751, 96308-96330, 53968-53993; `WebPortal:1137-1141`; `TenantStatus.cs:74`; `ConfirmTypeEnum.cs:111` | FIXED (contract regenerated, submodule `5ea4dc6`) |
| T2 | Confirming the owner change | Every failure is a plain `Exception` → 500 ("User not found" for an unknown id or a guest, "Access denied" for an inactive member or a link whose address is not the owner's). Business refusals are reported as server errors; the sibling `sendOwnerChangeInstructions` uses 400/403 for the same conditions. | `Owner:148-172` | STILL OPEN |
| T3 | Confirming the owner change | The previous owner is left as a DocSpace admin and nothing records the handover on their account; the new owner is promoted with `UpdateUserTypeAsync` *before* `ownerId` is saved, so a failure in `SaveTenantAsync` leaves a promoted member and the old owner — the two writes are not atomic. | `Owner:176-188` | STILL OPEN |
| T4 | Suspend / delete instructions, wizard, portal removal | The "last full-access space" rule and the "wizard passed" check throw plain `Exception` → 500 with a resource message; a 403 (rule) or 409/400 (already done) would describe them. The yaml documents the 500s as intended. | `WebPortal:1364`; `FirstTime:66` | STILL OPEN |
| T5 | Renaming a portal | The alias is saved, the audit written and every other member logged out *before* the hosting cache and the rename letters are updated; a failure in the cache update leaves a hosted portal reachable under neither name until the cache is repaired. The whole action is also absent from the published contract. | `WebPortal:927-963` | STILL OPEN (still no `portalrename` path in `5ea4dc6`) |
| T6 | Renaming a portal | The response is a confirmation **sign-in** link of type `Auth` for the caller under the new domain, so the rename answers with a credential-bearing URL in the body of a 200; the body is also what the audit log of the proxy sees. | `WebPortal:980-994` | STILL OPEN |
| T7 | Registering through the system API | On a server installation a requested `portalName` is validated only inside `RegisterTenantAsync`, so a taken or malformed name answers 500 "registerNewTenantError" instead of the 400 codes that `validateportalname` would give for the same input. | `RegService:98-105`, `717-740` | STILL OPEN |
| T8 | Deleting a portal / immediate deletion | `deleteportalimmediately` lets any owner whose address matches the test-address pattern of the installation delete the portal with an ordinary session and no confirmation; the pattern is a regular expression in configuration, and a loose one (e.g. matching a whole domain) turns every owner of that domain into a one-call deleter. | `WebPortal:985-1002`; `SetupInfo.cs:114` | STILL OPEN |
| T9 | Status filter | A Suspended portal still answers `getPortalColorTheme` 200 and `continuePortal`; a Remove-pending portal answers 404 to everything including `continuePortal`, so the yaml's claim that `continuePortal` is idempotent holds only for Suspended/Active — fine — but the suspended portal's 404 for `getPortalInformation` means a client cannot read `status` to learn *why* it gets 404 (the yaml tells it to). | `StatusFilter:57-69`; yaml `suspendPortal` description ("read `status` from `GET api/2.0/portal`") | STILL OPEN |
| T10 | Portal name uniqueness | Uniqueness against mapped domains excludes portals pending removal, but uniqueness against aliases does not: a removed portal keeps `<alias>_deleted` forever (until the purge drops the row), so `variants` of a later `validateportalname` leak the aliases of deleted portals that share the stem. | `DbTenantService:451-453`, `538-570` | STILL OPEN |
| T11 | Deleting a portal on a server installation | The deletion renames the alias at once, and a server installation resolves an address it does not know to its default portal: every request a client still sends to the deleted portal's address is answered by the default portal (200), not refused with 404, so a stale client or link silently works against another portal. | `DbTenantService:538-570`; `StatusFilter:57-69`; `Tests/04_Portal/Lifecycle/PortalImmediateDeletionTests.cs` (the old address answered 200 within 15 s) | NEW 2026-11-10 |
