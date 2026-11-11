# access-settings — verification

Revision 2026-11-09 (bb406b409b..6fb013e745)

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement of `spec.md` with where it was checked. Paths are relative to the repo root (`server/`). `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml`. Code: `Sec` = `web/ASC.Web.Api/Api/Settings/SecurityController.cs`,
`Ip` = `web/ASC.Web.Api/Api/Settings/IpRestrictionsController.cs`, `Tfa` = `web/ASC.Web.Api/Api/Settings/TfaappController.cs`,
`Sso` = `web/ASC.Web.Api/Api/Settings/SsoController.cs`, `Set` = `web/ASC.Web.Api/Api/Settings/SettingsController.cs`,
`Auth` = `web/ASC.Web.Api/Api/AuthenticationController.cs`, `User` = `products/ASC.People/Server/Api/UserController.cs`,
`Wrap` = `common/ASC.Api.Core/Middleware/ResponseWrapper.cs`. (`web/ASC.Web.Api/Api/SsoSettingsV2Controller.cs` is excluded
from compilation by the csproj and was ignored.) Tests: `S/` = `web/ASC.Web.Api.Tests/Tests/02_Settings/`,
`A/` = `web/ASC.Web.Api.Tests/Tests/03_Authentication/`, `P/` = `products/ASC.People/Tests/Tests/05_Password/`,
`B/` = `web/ASC.Web.Api.Billing.Tests/Tests/` (SaaS stack, profile `integration-test-billing`, portal on the free plan).

No test was run for this document. **TEST-CONFIRMED** means a test asserts the claim, not that it was seen passing;
**CODE-ONLY** means read from code or the yaml. Assertions were read from a dump of every assertion line of those folders,
and from the method body where a row needed the request; test line numbers point at the test method.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Reading module access** | | |
| 200 for owner, user; empty list with no ids on this build | `S/Security/WebItemSecurityReadTests.cs:49,64` | TEST-CONFIRMED [run: PASS] |
| Unknown id reads disabled | `S/Security/WebItemSecurityReadTests.cs:80` | TEST-CONFIRMED [run: PASS] |
| Anonymous 401; malformed id 400 (bug 83186) | `S/Security/WebItemSecurityReadTests.cs:96,115` | TEST-CONFIRMED [run: PASS] |
| `users` filtered by what the caller may view (bugs 83192, 83193) | `S/Security/WebItemSecurityReadTests.cs:136,162`; `Sec:109-122` | TEST-CONFIRMED [run: PASS] |
| Absent `ids` = all registered modules | `Sec:80-83`. Unreachable: this build registers no module in `WebItemManager`, so "all" is the empty list the row above already asserts | CODE-ONLY |
| **Checking a module and listing enabled modules** | | |
| Unknown id false; user may call | `S/Security/WebItemSecurityReadTests.cs:185,199` | TEST-CONFIRMED [run: PASS] |
| Enabled modules empty on this build; anonymous 401 | `S/Security/EnabledModulesTests.cs:47,61` | TEST-CONFIRMED [run: PASS] |
| True only for a registered, readable module | `Sec:150-152`. Unreachable: no module is registered on this build, so the true branch has no id to ask about | CODE-ONLY |
| **Changing module access** | | |
| Owner, admin 200, state read back | `S/Security/WebItemSecurityWriteTests.cs:46,65` | TEST-CONFIRMED [run: PASS] |
| Bulk set | `S/Security/WebItemSecurityWriteTests.cs:120` | TEST-CONFIRMED [run: PASS] |
| Malformed id 400 (bugs 83187, 83190) | `S/Security/WebItemSecurityWriteTests.cs:85,146` | TEST-CONFIRMED [run: PASS] |
| Roles below admin 403 | `S/Security/WebItemSecurityWriteTests.cs:103,161` | TEST-CONFIRMED [run: PASS] |
| 403 on an open portal | `web/ASC.Web.Core/WebItemSecurity.cs:160`; yaml `setWebItemSecurity`. Unreachable: no API sets `TenantAccessSettings.Anyone` | CODE-ONLY |
| **Reading product administrators** | | |
| Owner alone by default; grows on promotion; member 403 | `S/Security/ProductAdministratorTests.cs:51,65,86` | TEST-CONFIRMED [run: PASS] |
| Administrator flag for owner, user, promoted; user caller 403 | `S/Security/ProductAdministratorTests.cs:102,117,133,152` | TEST-CONFIRMED [run: PASS] |
| **Changing product administrators** | | |
| Admin may not demote admin (bug 80586) or promote to admin | `S/Security/ProductAdministratorTests.cs:174,194`; `web/ASC.Web.Core/WebItemSecurity.cs:229-245,296` | TEST-CONFIRMED [run: PASS] |
| Owner promotes | `S/Security/ProductAdministratorTests.cs:65` | TEST-CONFIRMED [run: PASS] |
| 402 on a hosted free plan | `B/Settings/FreePlanSettingsTests.cs:57` | TEST-CONFIRMED [run: PASS] |
| **Password policy** | | |
| Defaults 8/false; user reads; anonymous 401 | `S/Security/PasswordSettingsTests.cs:47,64,79` | TEST-CONFIRMED [run: PASS] |
| 304 | `S/Security/PasswordSettingsRangeTests.cs:86` | TEST-CONFIRMED [run: PASS] |
| **Changing the password policy** | | |
| Owner, admin update; read back | `S/Security/PasswordSettingsTests.cs:93,114` | TEST-CONFIRMED [run: PASS] |
| 999 and -5 give 400 naming `MinLength`; roles below admin 403 | `S/Security/PasswordSettingsTests.cs:133,152` | TEST-CONFIRMED [run: PASS] |
| Lower end from the installation key `web:password:min` (default 8), upper end 30 | `S/Security/PasswordSettingsRangeTests.cs:52,70` (8 and 30 stored, 7 and 31 refused). A non-default `web:password:min` stays CODE-ONLY: the stand cannot change the key | TEST-CONFIRMED [run: PASS] |
| **Failed sign-in limits** | | |
| Defaults, read roles, anonymous, member refused | `S/Login/LoginSettingsGetTests.cs:46,64,82,99` | TEST-CONFIRMED [run: PASS] |
| Update persisted, `isDefault` false; roles | `S/Login/LoginSettingsUpdateTests.cs:47,78,112,130` | TEST-CONFIRMED [run: PASS] |
| Reset; roles | `S/Login/LoginSettingsResetTests.cs:46,75,102,119` | TEST-CONFIRMED [run: PASS] |
| 304 | `S/Login/LoginSettingsConditionalGetTests.cs:48,68` | TEST-CONFIRMED [run: PASS] |
| **Range of the sign-in limits** | `S/Login/LoginSettingsUpdateTests.cs:150` (message asserted) | TEST-CONFIRMED [run: PASS] |
| **Sign-in block** | `S/Login/LoginLockoutTests.cs:50` (401 per attempt, 403 and message, owner unaffected, login-history event) | TEST-CONFIRMED [run: PASS] |
| **Reading IP restrictions** | | |
| Anonymous 401, members 403 for both | `S/IpRestrictions/IpRestrictionsGetPermissionsTests.cs:47,64,79,96` | TEST-CONFIRMED [run: PASS] |
| Owner, admin 200; `lastModified` present | `S/IpRestrictions/IpRestrictionsGetTests.cs:50,64,79,94` | TEST-CONFIRMED [run: PASS] |
| 304 on `If-None-Match` (first) and `If-Modified-Since` (second) | `S/IpRestrictions/IpRestrictionsRulesTests.cs:120,165`. The first also answers 304 after the list changed (AC-11): `S/IpRestrictions/IpRestrictionsRulesTests.cs:144` [run: FAIL-BUG] | TEST-CONFIRMED [run: PASS] |
| **Saving IP restrictions** | | |
| Anonymous 401, members 403 for both | `S/IpRestrictions/IpRestrictionsSavePermissionsTests.cs:47,65,81,99` | TEST-CONFIRMED [run: PASS] |
| Owner, admin save `enable` false; echo | `S/IpRestrictions/IpRestrictionsSaveTests.cs:50,66` | TEST-CONFIRMED [run: PASS] |
| Explicit null list binds to an empty array; absent property 400 (comment at :84-85) | `S/IpRestrictions/IpRestrictionsSaveTests.cs:87,108` | TEST-CONFIRMED [run: PASS] |
| Omitted `enable`; `enable` true with empty list 400; entry not an address 400 | `S/IpRestrictions/IpRestrictionsRulesTests.cs:52,67,84,104` (both routes; a CIDR block and a range are refused too) | TEST-CONFIRMED [run: PASS] |
| **Enforcing IP restrictions** | `S/IpRestrictions/IpRestrictionsEnforcementTests.cs:56,73,89,103,127` (caller address set with `X-Forwarded-For`, trusted from 127.0.0.1) | TEST-CONFIRMED [run: PASS] |
| **Listing two-factor methods** | `S/Tfa/TfaGetSettingsTests.cs:46`; `S/Tfa/TfaSettingsPermissionsTests.cs:83`; fields `Tfa:91-116` | TEST-CONFIRMED [run: PASS] |
| **Changing the two-factor policy** | | |
| Owner, admin enable and disable | `S/Tfa/TfaSettingsUpdateTests.cs:46,57,178,193` | TEST-CONFIRMED [run: PASS] |
| SMS without provider 405 (bug 82970) | `S/Tfa/TfaSettingsUpdateTests.cs:71` | TEST-CONFIRMED [run: PASS] |
| Anonymous 401, roles below admin 403 | `S/Tfa/TfaSettingsPermissionsTests.cs:48,66` | TEST-CONFIRMED [run: PASS] |
| Enabling one method switches the other off | `Tfa:271-274,293-296`. Unreachable: SMS cannot be switched on without a provider, so neither direction can be observed | CODE-ONLY |
| **Validation of the two-factor policy** | | |
| Malformed `mandatoryUsers` 400 | `S/Tfa/TfaSettingsUpdateTests.cs:121` | TEST-CONFIRMED [run: PASS] |
| Malformed `trustedIps` 400, sign-in keeps working (bug 82994) | `S/Tfa/TfaSettingsUpdateTests.cs:142` | TEST-CONFIRMED [run: PASS] |
| Nonexistent user and group ids accepted | `S/Tfa/TfaSettingsUpdateTests.cs:97,108` | TEST-CONFIRMED [run: PASS] |
| Owner named while `id` is not the caller's: 403 | `S/Tfa/TfaOwnerGuardTests.cs:55,71,89,101` (the owner listing themselves without `id` is refused too, AC-8) | TEST-CONFIRMED [run: PASS] |
| **Switching a two-factor method on ends sessions** | `S/Tfa/TfaSettingsUpdateTests.cs:208,225`; `Tfa:318-321` | TEST-CONFIRMED [run: PASS] |
| **Who must pass the second factor** | `S/Tfa/TfaSettingsUpdateTests.cs:161`; `S/Tfa/TfaTrustedIpsOverrideTests.cs:70,98`; `web/ASC.Web.Core/Sms/TfaSettingsBase.cs:64-105` | TEST-CONFIRMED [run: PASS] |
| **Policy change with a confirmation link** | | |
| Link returned; 405; refused callers | `S/Tfa/TfaSettingsLinkTests.cs:49,63`; `S/Tfa/TfaSettingsPermissionsTests.cs:97,115` | TEST-CONFIRMED [run: PASS] |
| Empty string when off or exempt | `S/Tfa/TfaOwnerGuardTests.cs:112,123` | TEST-CONFIRMED [run: PASS] |
| **Confirmation data of the second factor** | | |
| Linked account; anonymous 401 | `S/Tfa/TfaConfirmDataTests.cs:46`; `S/Tfa/TfaSettingsPermissionsTests.cs:161` | TEST-CONFIRMED [run: PASS] |
| Empty body when nothing applies | `S/Tfa/TfaOwnerGuardTests.cs:134,144` | TEST-CONFIRMED [run: PASS] |
| Activation link otherwise; SMS branch has no cookie | `Tfa:183-212`. Unreachable: a signed-in session of an account that must link an application cannot exist (switching the method on and unlinking both end sessions), and SMS cannot be switched on | CODE-ONLY |
| **Authenticator setup and code validation take a confirmation link** | | |
| Ordinary token 403 (bug 82972); anonymous 401 | `S/Tfa/TfaAppGenerateSetupCodeTests.cs:55,66`; `S/Tfa/TfaValidateAuthCodeTests.cs:48`; `S/Tfa/TfaSettingsPermissionsTests.cs:132,146` | TEST-CONFIRMED [run: PASS] |
| Through the link: 200, 405, 400 | `S/Tfa/TfaActivationLinkTests.cs:52,85,67` (link query in the `confirm` header, key from the sign-in answer's cookie) | TEST-CONFIRMED [run: PASS] |
| **Backup codes** | | |
| Owner, admin read and regenerate; every lower role | `S/Tfa/TfaAppCodesTests.cs:47,87,135,150`; `S/Tfa/TfaAppCodesRoleTests.cs:51` | TEST-CONFIRMED [run: PASS] |
| Not linked 405 (bugs 82976, 82978); anonymous 401 | `S/Tfa/TfaAppCodesTests.cs:63,76`; `S/Tfa/TfaSettingsPermissionsTests.cs:175,189` | TEST-CONFIRMED [run: PASS] |
| **A backup code signs in once** | `S/Tfa/TfaAppCodesTests.cs:102` | TEST-CONFIRMED [run: PASS] |
| **Unlinking an authenticator application** | | |
| Owner unlinks another, session 401 | `S/Tfa/TfaUnlinkAppTests.cs:97` | TEST-CONFIRMED [run: PASS] |
| Other roles 403 with the message; anonymous 401 | `S/Tfa/TfaUnlinkAppPermissionsTests.cs:48,67` | TEST-CONFIRMED [run: PASS] |
| No `id` 403; malformed and empty 400; not linked 405 (bug 82983) | `S/Tfa/TfaUnlinkAppTests.cs:120,67,82,49` | TEST-CONFIRMED [run: PASS] |
| Caller's own `id` gives an activation link; another person's gives an empty string | `S/Tfa/TfaActivationLinkTests.cs:105,119` | TEST-CONFIRMED [run: PASS] |
| Letter to the person | `Tfa:556-564`. Unreachable: the suite cannot read the letter | CODE-ONLY |
| **Reading single sign-on settings** | | |
| Owner, anonymous, members | `S/Sso/SsoReadTests.cs:65,76`; `S/Sso/SsoPermissionsTests.cs:115` | TEST-CONFIRMED [run: PASS] |
| 402 for an administrator; anonymous `false` without the plan | `B/Settings/FreePlanSettingsTests.cs:74,85` | TEST-CONFIRMED [run: PASS] |
| **Single sign-on template and constants** | `S/Sso/SsoReadTests.cs:43,55`; `S/Sso/SsoPermissionsTests.cs:45,59,72,86,99` | TEST-CONFIRMED [run: PASS] |
| **Saving single sign-on settings** | | |
| Stored and read back; label cut; type normalised (unrecognised to 4, 1 and 3 kept); empty 400; roles | `S/Sso/SsoSaveTests.cs:47,64,75,88,99`; `S/Sso/SsoPermissionsTests.cs:131,145` | TEST-CONFIRMED [run: PASS] |
| **Required single sign-on fields** | | |
| 400 for missing entity ID, SSO URL, field mapping, IdP settings, malformed SLO URL, invalid JSON (f3a52c35dd) | `S/Sso/SsoSaveTests.cs:125,143,161,182,196,210,220` | TEST-CONFIRMED |
| Stored settings do not change | `S/Sso/SsoRefusedSaveTests.cs:46,68` | TEST-CONFIRMED [run: PASS] |
| **Switching single sign-on off** | | |
| Reset restores defaults; roles | `S/Sso/SsoResetTests.cs:41`; `S/Sso/SsoPermissionsTests.cs:161,175` | TEST-CONFIRMED [run: PASS] |
| Accounts converted to ordinary | `Sso:247-251,293,300-318`. Unreachable: a single sign-on account is created only by a sign-in through an identity provider, which the stack has none of | CODE-ONLY |
| **Third-party service keys** | `S/Authorization/GetAuthServicesTests.cs:51,66`; `S/Authorization/GetAuthServicesPermissionsTests.cs:47,64` | TEST-CONFIRMED [run: PASS] |
| **Saving third-party service keys** | | |
| Owner, admin 200 true; roles | `S/Authorization/SaveAuthKeysTests.cs:49,64`; `S/Authorization/SaveAuthKeysPermissionsTests.cs:47,65` | TEST-CONFIRMED [run: PASS] |
| Clear on all-empty; 403 unknown provider; 402 | `S/Authorization/AuthServiceRulesTests.cs:53,72`; `B/Settings/FreePlanSettingsTests.cs:99` | TEST-CONFIRMED [run: PASS] |
| Rejected keys cleared and 400; 403 not settable | `Set:1263-1308`; yaml. Rejected keys unreachable: every key-validating provider asks its outside service. Not settable: not attempted, it needs a consumer with preset keys in the installation config | CODE-ONLY |
| **Testing an external database connection** | | |
| Failure answer; roles | `S/Authorization/TestExternalDatabaseConnectionTests.cs:48,63`; `S/Authorization/TestExternalDatabaseConnectionPermissionsTests.cs:47,66` | TEST-CONFIRMED [run: PASS] |
| SQLite refused on hosted; success path | `B/Settings/FreePlanSettingsTests.cs:115`; `S/Authorization/AuthServiceRulesTests.cs:89` (the stack's MySQL), `:125` (standalone tries SQLite) | TEST-CONFIRMED [run: PASS] |
| **Sign-in with a second-factor code** | `A/AuthenticateWithCodeTests.cs:49,68,84,101,116,137`; every exception becomes 401 at `Auth:220-226` | TEST-CONFIRMED [run: PASS] |
| **Registering a mobile phone** | | |
| Anonymous 401; ordinary token 403, phone unchanged | `A/SaveMobilePhonePermissionsTests.cs:49,67` | TEST-CONFIRMED [run: PASS] |
| 200 body through the link | `Auth:534-548`. Unreachable: SMS cannot be enabled on a test portal (no provider) | CODE-ONLY |
| **Resending the SMS code** | | |
| 401 in the four cases | `A/SendSmsCodeTests.cs:50,68,85,103` | TEST-CONFIRMED [run: PASS] |
| 200 body with SMS on | `Auth:590-597`. Unreachable: SMS cannot be enabled on a test portal (no provider) | CODE-ONLY |
| 405 for valid credentials with SMS off (f5f2735e30) | `A/SendSmsCodeTests.cs:146` | TEST-CONFIRMED |
| **Password reminder for a signed-in caller** | | |
| Own account and admin for member | `P/PasswordReminderTests.cs:60` with the pairs at :45-55 | TEST-CONFIRMED [run: PASS] |
| Refused pairs (bug 80157) | `P/PasswordReminderPermissionsTests.cs:57,89` | TEST-CONFIRMED [run: PASS] |
| Owner for an administrator | `P/PasswordReminderRulesTests.cs:44` | TEST-CONFIRMED [run: PASS] |
| **Password reminder from the sign-in page** | | |
| Conditional message | `P/PasswordReminderTests.cs:76` | TEST-CONFIRMED [run: PASS] |
| Same form whether or not the account exists | `P/PasswordReminderRulesTests.cs:61` | TEST-CONFIRMED [run: PASS] |
| CAPTCHA 403 | `User:2066-2090`. Unreachable: the stand configures no CAPTCHA keys | CODE-ONLY |
| **Changing a password through a confirmation link** | `User:594-662`; yaml `changeUserPassword`. Unreachable: the `PasswordChange` key exists only in the letter, which the suite cannot read | CODE-ONLY |

## Differences between the yaml and the server

| Operation | yaml | Server |
|---|---|---|
| `tfaAppGenerateSetupCode` | 200, 405, 401, no 403 | An ordinary token gets 403 before the action (`TfaAppGenerateSetupCodeTests.cs:55,66`) |
| `saveMobilePhone` | 200, 401, no 403 | An ordinary token gets 403 (`SaveMobilePhonePermissionsTests.cs:67`) |
| `getTfaAppCodes`, `updateTfaAppCodes` | no 403 | `Tfa:439-442,481-484` can answer 403 for an outsider account |
| `authenticateMeFromBodyWithCode`, `sendSmsCode` | 404 for no matching user | With a password, an unknown account gets 401 (`SendSmsCodeTests.cs:68`, `BruteForceLoginManager.cs:162-165`); 404 was not traced to a reachable path |
| Routes | `settings/security/loginsettings` | Code declares `loginSettings`; tests reach it through the SDK with the yaml spelling |

A comment at `S/Tfa/TfaAuditTrailTests.cs:125` says that disabling two-factor invalidates the session. `Tfa:318-321` resets
sessions only when a method was switched on, so the spec states only that. The test name
`UnlinkTfaApp_Owner_CannotUnlinkTheirOwnAppThisWay` (`TfaUnlinkAppTests.cs:120`) sends no `id`; the spec describes what it
asserts, the request without an identifier.

## Left out

| Item | Reason |
|---|---|
| Operations of the owned tags | None left out: all 39 are in a requirement |
| `authenticateMe`, `checkConfirm`, `logout`, the check of the current session | Not in the absent list; `authenticateMe` is named only for the block and the second-factor answer |
| The audit events of two-factor actions | `TfaAuditTrailTests.cs` asserts them for `history-and-audit`; mostly commented out there |
| `03_Authentication/ThirdPartyOAuth` | Third-party account linking, `people-and-groups` |

## Suspected defects

No test was run. "Red by code reading" means the code as read would fail that test's assertion.

| # | Where | What the server does | Evidence | Evidence kind |
|---|---|---|---|---|
| AC-1 | `saveSsoSettingsV2` | No IdP entity ID, no SSO URL, a malformed SLO URL or an incomplete field mapping answers 500; the tests expect 400 (bug 84685) | `SsoSaveTests.cs:111`; `Sso:206-209`; `Wrap` default branch | FIXED (f3a52c35dd): refused as a bad request; also missing `idpSettings` and invalid JSON |
| AC-2 | `saveSsoSettingsV2` | Missing or invalid SSO URL answers 500; the test expects 400 (bug 84686) | `SsoSaveTests.cs:129`; `Sso:211-214` | FIXED (f3a52c35dd): refused as a bad request |
| AC-3 | `saveSsoSettingsV2` | Incomplete field mapping answers 500; the test expects 400 (bug 84687) | `SsoSaveTests.cs:147`; `Sso:221-226` | FIXED (f3a52c35dd): refused as a bad request, missing `fieldMapping` too |
| AC-4 | `saveSsoSettingsV2` | Malformed SLO URL answers 500; the test expects 400 (bug 84688) | `SsoSaveTests.cs:165`; `Sso:216-219` | FIXED (f3a52c35dd): refused as a bad request |
| AC-5 | `sendSmsCode` | Valid credentials with SMS off for the account answer 500: the thrown exception type has no mapping; the test requires anything but 500 (bug 84503) | `SendSmsCodeTests.cs:125`; `web/ASC.Web.Core/Sms/SmsManager.cs:100-103`; `Wrap` default branch | FIXED (f5f2735e30): 405 "SMS not available", asserted at `SendSmsCodeTests.cs:146` |
| AC-6 | `updateTfaSettings` | A numeric `type` outside the known values takes the `None` branch: two-factor is switched off and the answer is 200 false. The test name says the value is ignored; it asserts only false | `TfaSettingsUpdateTests.cs:86`; `Tfa:252,302-315` | by code reading |
| AC-7 | `getTfaSettings` | No role check: any signed-in role receives `trustedIps`, `mandatoryUsers` and `mandatoryGroups` | `Tfa:80-119`; tests cover only owner and anonymous | by code reading |
| AC-8 | `updateTfaSettings` | The 403 for naming the owner depends on the `id` field, not on the caller's role. An owner listing themselves in `mandatoryUsers` with no `id` gets 403 | `Tfa:241-246`; `S/Tfa/TfaOwnerGuardTests.cs:89` | confirmed by test (the spec states this rule) |
| AC-9 | `unlinkTfaApp` | The branch that treats an empty `id` as the caller is not reached: an empty `id` resolves to no account and answers 403 first | `Tfa:520-528`; `TfaUnlinkAppTests.cs:120` | by code reading |
| AC-10 | `updateTfaAppCodes` | Does not check that the method is on at portal level, while `getTfaAppCodes` does; with the method off and an application still linked the two disagree | `Tfa:432-434` vs `Tfa:476` | by code reading |
| AC-11 | `getIpRestrictions` | The ETag is the MD5 of zero bytes for every list: `HttpContextExtension.CalculateEtagAsync` hashes its `MemoryStream` without rewinding it. A client holding any earlier ETag gets 304 after the list changed | `S/IpRestrictions/IpRestrictionsRulesTests.cs:144`; `common/ASC.Common/Caching/HttpContextExtension.cs:87-99` (the same unrewound stream in `IPRestrictionsService.CalculateEtagAsync`) | test red [run: FAIL-BUG] |
