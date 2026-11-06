# access-settings — verification

Every requirement of `spec.md` with where it was checked. Paths are relative to the repo root (`server/`). `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml`. Code: `Sec` = `web/ASC.Web.Api/Api/Settings/SecurityController.cs`,
`Ip` = `web/ASC.Web.Api/Api/Settings/IpRestrictionsController.cs`, `Tfa` = `web/ASC.Web.Api/Api/Settings/TfaappController.cs`,
`Sso` = `web/ASC.Web.Api/Api/Settings/SsoController.cs`, `Set` = `web/ASC.Web.Api/Api/Settings/SettingsController.cs`,
`Auth` = `web/ASC.Web.Api/Api/AuthenticationController.cs`, `User` = `products/ASC.People/Server/Api/UserController.cs`,
`Wrap` = `common/ASC.Api.Core/Middleware/ResponseWrapper.cs`. (`web/ASC.Web.Api/Api/SsoSettingsV2Controller.cs` is excluded
from compilation by the csproj and was ignored.) Tests: `S/` = `web/ASC.Web.Api.Tests/Tests/02_Settings/`,
`A/` = `web/ASC.Web.Api.Tests/Tests/03_Authentication/`, `P/` = `products/ASC.People/Tests/Tests/05_Password/`.

No test was run for this document. **TEST-CONFIRMED** means a test asserts the claim, not that it was seen passing;
**CODE-ONLY** means read from code or the yaml. Assertions were read from a dump of every assertion line of those folders,
and from the method body where a row needed the request; test line numbers point at the test method.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Reading module access** | | |
| 200 for owner, user; empty list with no ids on this build | `S/Security/WebItemSecurityReadTests.cs:49,64` | TEST-CONFIRMED |
| Unknown id reads disabled | `S/Security/WebItemSecurityReadTests.cs:80` | TEST-CONFIRMED |
| Anonymous 401; malformed id 400 (bug 83186) | `S/Security/WebItemSecurityReadTests.cs:96,115` | TEST-CONFIRMED |
| `users` filtered by what the caller may view (bugs 83192, 83193) | `S/Security/WebItemSecurityReadTests.cs:136,162`; `Sec:109-122` | TEST-CONFIRMED |
| Absent `ids` = all registered modules | `Sec:80-83` | CODE-ONLY |
| **Checking a module and listing enabled modules** | | |
| Unknown id false; user may call | `S/Security/WebItemSecurityReadTests.cs:185,199` | TEST-CONFIRMED |
| Enabled modules empty on this build; anonymous 401 | `S/Security/EnabledModulesTests.cs:47,61` | TEST-CONFIRMED |
| True only for a registered, readable module | `Sec:150-152` | CODE-ONLY |
| **Changing module access** | | |
| Owner, admin 200, state read back | `S/Security/WebItemSecurityWriteTests.cs:46,65` | TEST-CONFIRMED |
| Bulk set | `S/Security/WebItemSecurityWriteTests.cs:120` | TEST-CONFIRMED |
| Malformed id 400 (bugs 83187, 83190) | `S/Security/WebItemSecurityWriteTests.cs:85,146` | TEST-CONFIRMED |
| Roles below admin 403 | `S/Security/WebItemSecurityWriteTests.cs:103,161` | TEST-CONFIRMED |
| 403 on an open portal | `web/ASC.Web.Core/WebItemSecurity.cs:160`; yaml `setWebItemSecurity` | CODE-ONLY |
| **Reading product administrators** | | |
| Owner alone by default; grows on promotion; member 403 | `S/Security/ProductAdministratorTests.cs:51,65,86` | TEST-CONFIRMED |
| Administrator flag for owner, user, promoted; user caller 403 | `S/Security/ProductAdministratorTests.cs:102,117,133,152` | TEST-CONFIRMED |
| **Changing product administrators** | | |
| Admin may not demote admin (bug 80586) or promote to admin | `S/Security/ProductAdministratorTests.cs:174,194`; `web/ASC.Web.Core/WebItemSecurity.cs:229-245,296` | TEST-CONFIRMED |
| Owner promotes | `S/Security/ProductAdministratorTests.cs:65` | TEST-CONFIRMED |
| 402 on a hosted free plan | `Sec:463-468` | CODE-ONLY |
| **Password policy** | | |
| Defaults 8/false; user reads; anonymous 401 | `S/Security/PasswordSettingsTests.cs:47,64,79` | TEST-CONFIRMED |
| 304 | `Sec:207-209`; yaml | CODE-ONLY |
| **Changing the password policy** | | |
| Owner, admin update; read back | `S/Security/PasswordSettingsTests.cs:93,114` | TEST-CONFIRMED |
| 999 and -5 give 400 naming `MinLength`; roles below admin 403 | `S/Security/PasswordSettingsTests.cs:133,152` | TEST-CONFIRMED |
| Lower end from the installation key `web:password:min` (default 8), upper end 30 | `web/ASC.Web.Core/Utility/PasswordSettings.cs:89,143-151` | CODE-ONLY |
| **Failed sign-in limits** | | |
| Defaults, read roles, anonymous, member refused | `S/Login/LoginSettingsGetTests.cs:46,64,82,99` | TEST-CONFIRMED |
| Update persisted, `isDefault` false; roles | `S/Login/LoginSettingsUpdateTests.cs:47,78,112,130` | TEST-CONFIRMED |
| Reset; roles | `S/Login/LoginSettingsResetTests.cs:46,75,102,119` | TEST-CONFIRMED |
| 304 | `Sec:561-563`; yaml | CODE-ONLY |
| **Range of the sign-in limits** | `S/Login/LoginSettingsUpdateTests.cs:150` (message asserted) | TEST-CONFIRMED |
| **Sign-in block** | `S/Login/LoginLockoutTests.cs:50` (401 per attempt, 403 and message, owner unaffected, login-history event) | TEST-CONFIRMED |
| **Reading IP restrictions** | | |
| Anonymous 401, members 403 for both | `S/IpRestrictions/IpRestrictionsGetPermissionsTests.cs:47,64,79,96` | TEST-CONFIRMED |
| Owner, admin 200; `lastModified` present | `S/IpRestrictions/IpRestrictionsGetTests.cs:50,64,79,94` | TEST-CONFIRMED |
| 304 on `If-None-Match` (first) and `If-Modified-Since` (second) | `Ip:72-75,159-161`; yaml | CODE-ONLY |
| **Saving IP restrictions** | | |
| Anonymous 401, members 403 for both | `S/IpRestrictions/IpRestrictionsSavePermissionsTests.cs:47,65,81,99` | TEST-CONFIRMED |
| Owner, admin save `enable` false; echo | `S/IpRestrictions/IpRestrictionsSaveTests.cs:50,66` | TEST-CONFIRMED |
| Explicit null list binds to an empty array; absent property 400 (comment at :84-85) | `S/IpRestrictions/IpRestrictionsSaveTests.cs:87,108` | TEST-CONFIRMED |
| Omitted `enable`; `enable` true with empty list 400; entry not an address 400 | `Ip:102-123,188-209` | CODE-ONLY |
| **Enforcing IP restrictions** | `common/ASC.IPSecurity/Utils/IPSecurity.cs:82-151`; `common/ASC.Api.Core/Middleware/IpSecurityFilter.cs:43-59`. Tests never save `enable` true (`IpRestrictionsSaveTests.cs:40-42`) | CODE-ONLY |
| **Listing two-factor methods** | `S/Tfa/TfaGetSettingsTests.cs:46`; `S/Tfa/TfaSettingsPermissionsTests.cs:83`; fields `Tfa:91-116` | TEST-CONFIRMED |
| **Changing the two-factor policy** | | |
| Owner, admin enable and disable | `S/Tfa/TfaSettingsUpdateTests.cs:46,57,178,193` | TEST-CONFIRMED |
| SMS without provider 405 (bug 82970) | `S/Tfa/TfaSettingsUpdateTests.cs:71` | TEST-CONFIRMED |
| Anonymous 401, roles below admin 403 | `S/Tfa/TfaSettingsPermissionsTests.cs:48,66` | TEST-CONFIRMED |
| Enabling one method switches the other off | `Tfa:271-274,293-296` | CODE-ONLY |
| **Validation of the two-factor policy** | | |
| Malformed `mandatoryUsers` 400 | `S/Tfa/TfaSettingsUpdateTests.cs:121` | TEST-CONFIRMED |
| Malformed `trustedIps` 400, sign-in keeps working (bug 82994) | `S/Tfa/TfaSettingsUpdateTests.cs:142` | TEST-CONFIRMED |
| Nonexistent user and group ids accepted | `S/Tfa/TfaSettingsUpdateTests.cs:97,108` | TEST-CONFIRMED |
| Owner named while `id` is not the caller's: 403 | `Tfa:241-246`; yaml `updateTfaSettings` | CODE-ONLY |
| **Switching a two-factor method on ends sessions** | `S/Tfa/TfaSettingsUpdateTests.cs:208,225`; `Tfa:318-321` | TEST-CONFIRMED |
| **Who must pass the second factor** | `S/Tfa/TfaSettingsUpdateTests.cs:161`; `S/Tfa/TfaTrustedIpsOverrideTests.cs:70,98`; `web/ASC.Web.Core/Sms/TfaSettingsBase.cs:64-105` | TEST-CONFIRMED |
| **Policy change with a confirmation link** | | |
| Link returned; 405; refused callers | `S/Tfa/TfaSettingsLinkTests.cs:49,63`; `S/Tfa/TfaSettingsPermissionsTests.cs:97,115` | TEST-CONFIRMED |
| Empty string when off or exempt | `Tfa:358-366` | CODE-ONLY |
| **Confirmation data of the second factor** | | |
| Linked account; anonymous 401 | `S/Tfa/TfaConfirmDataTests.cs:46`; `S/Tfa/TfaSettingsPermissionsTests.cs:161` | TEST-CONFIRMED |
| Activation link otherwise; empty body when nothing applies; SMS branch has no cookie | `Tfa:183-212` | CODE-ONLY |
| **Authenticator setup and code validation take a confirmation link** | | |
| Ordinary token 403 (bug 82972); anonymous 401 | `S/Tfa/TfaAppGenerateSetupCodeTests.cs:55,66`; `S/Tfa/TfaValidateAuthCodeTests.cs:48`; `S/Tfa/TfaSettingsPermissionsTests.cs:132,146` | TEST-CONFIRMED |
| Through the link: 405, 400 | `Tfa:144,388,394-399`; yaml. The link path needs a browser and is not covered here (`TfaSettingsLinkTests.cs:37-41`) | CODE-ONLY |
| **Backup codes** | | |
| Owner, admin read and regenerate; every lower role | `S/Tfa/TfaAppCodesTests.cs:47,87,135,150`; `S/Tfa/TfaAppCodesRoleTests.cs:51` | TEST-CONFIRMED |
| Not linked 405 (bugs 82976, 82978); anonymous 401 | `S/Tfa/TfaAppCodesTests.cs:63,76`; `S/Tfa/TfaSettingsPermissionsTests.cs:175,189` | TEST-CONFIRMED |
| **A backup code signs in once** | `S/Tfa/TfaAppCodesTests.cs:102` | TEST-CONFIRMED |
| **Unlinking an authenticator application** | | |
| Owner unlinks another, session 401 | `S/Tfa/TfaUnlinkAppTests.cs:97` | TEST-CONFIRMED |
| Other roles 403 with the message; anonymous 401 | `S/Tfa/TfaUnlinkAppPermissionsTests.cs:48,67` | TEST-CONFIRMED |
| No `id` 403; malformed and empty 400; not linked 405 (bug 82983) | `S/Tfa/TfaUnlinkAppTests.cs:120,67,82,49` | TEST-CONFIRMED |
| Letter to the person; caller's own `id` gives an activation link | `Tfa:556-564` | CODE-ONLY |
| **Reading single sign-on settings** | | |
| Owner, anonymous, members | `S/Sso/SsoReadTests.cs:65,76`; `S/Sso/SsoPermissionsTests.cs:115` | TEST-CONFIRMED |
| 402 for an administrator; anonymous `false` without the plan | `Sso:77-96,325-337` | CODE-ONLY |
| **Single sign-on template and constants** | `S/Sso/SsoReadTests.cs:43,55`; `S/Sso/SsoPermissionsTests.cs:45,59,72,86,99` | TEST-CONFIRMED |
| **Saving single sign-on settings** | | |
| Stored and read back; label cut; type normalised (unrecognised to 4, 1 and 3 kept); empty 400; roles | `S/Sso/SsoSaveTests.cs:47,64,75,88,99`; `S/Sso/SsoPermissionsTests.cs:131,145` | TEST-CONFIRMED |
| **Required single sign-on fields** | `Sso:206-226` (thrown before the save at 242). The status is AC-1 to AC-4 | CODE-ONLY |
| **Switching single sign-on off** | | |
| Reset restores defaults; roles | `S/Sso/SsoResetTests.cs:41`; `S/Sso/SsoPermissionsTests.cs:161,175` | TEST-CONFIRMED |
| Accounts converted to ordinary | `Sso:247-251,293,300-318` | CODE-ONLY |
| **Third-party service keys** | `S/Authorization/GetAuthServicesTests.cs:51,66`; `S/Authorization/GetAuthServicesPermissionsTests.cs:47,64` | TEST-CONFIRMED |
| **Saving third-party service keys** | | |
| Owner, admin 200 true; roles | `S/Authorization/SaveAuthKeysTests.cs:49,64`; `S/Authorization/SaveAuthKeysPermissionsTests.cs:47,65` | TEST-CONFIRMED |
| Clear on all-empty; rejected keys cleared and 400; 403 unknown or not settable; 402 | `Set:1263-1308`; yaml | CODE-ONLY |
| **Testing an external database connection** | | |
| Failure answer; roles | `S/Authorization/TestExternalDatabaseConnectionTests.cs:48,63`; `S/Authorization/TestExternalDatabaseConnectionPermissionsTests.cs:47,66` | TEST-CONFIRMED |
| SQLite refused on hosted; success path | `Set:1348-1353`. The positive path has no test (`TestExternalDatabaseConnectionTests.cs:37-40`) | CODE-ONLY |
| **Sign-in with a second-factor code** | `A/AuthenticateWithCodeTests.cs:49,68,84,101,116,137`; every exception becomes 401 at `Auth:220-226` | TEST-CONFIRMED |
| **Registering a mobile phone** | | |
| Anonymous 401; ordinary token 403, phone unchanged | `A/SaveMobilePhonePermissionsTests.cs:49,67` | TEST-CONFIRMED |
| 200 body through the link | `Auth:534-548`. SMS cannot be enabled on a test portal | CODE-ONLY |
| **Resending the SMS code** | | |
| 401 in the four cases | `A/SendSmsCodeTests.cs:50,68,85,103` | TEST-CONFIRMED |
| 200 body with SMS on | `Auth:590-597`. Not reachable on a test portal | CODE-ONLY |
| **Password reminder for a signed-in caller** | | |
| Own account and admin for member | `P/PasswordReminderTests.cs:60` with the pairs at :45-55 | TEST-CONFIRMED |
| Refused pairs (bug 80157) | `P/PasswordReminderPermissionsTests.cs:57,89` | TEST-CONFIRMED |
| Owner for an administrator | `User:2051-2063` | CODE-ONLY |
| **Password reminder from the sign-in page** | | |
| Conditional message | `P/PasswordReminderTests.cs:76` | TEST-CONFIRMED |
| CAPTCHA 403; same form whether or not the account exists | `User:2066-2090` | CODE-ONLY |
| **Changing a password through a confirmation link** | `User:594-662`; yaml `changeUserPassword`. No test in `products/ASC.People/Tests` mentions the route or the confirm role | CODE-ONLY |

## Differences between the yaml and the server

| Operation | yaml | Server |
|---|---|---|
| `tfaAppGenerateSetupCode` | 200, 405, 401, no 403 | An ordinary token gets 403 before the action (`TfaAppGenerateSetupCodeTests.cs:55,66`) |
| `saveMobilePhone` | 200, 401, no 403 | An ordinary token gets 403 (`SaveMobilePhonePermissionsTests.cs:67`) |
| `getTfaAppCodes`, `updateTfaAppCodes` | no 403 | `Tfa:439-442,481-484` can answer 403 for an outsider account |
| `authenticateMeFromBodyWithCode`, `sendSmsCode` | 404 for no matching user | With a password, an unknown account gets 401 (`SendSmsCodeTests.cs:68`, `BruteForceLoginManager.cs:162-165`); 404 was not traced to a reachable path |
| `sendSmsCode` | no entry for SMS being off | Answers 500, see AC-5 |
| `saveSsoSettingsV2` | Documents 500 for invalid fields | The tests expect 400, see AC-1 to AC-4 |
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
| AC-1 | `saveSsoSettingsV2` | No IdP entity ID answers 500; the test expects 400 (bug 84685) | `SsoSaveTests.cs:111`; `Sso:206-209`; `Wrap` default branch | red by code reading |
| AC-2 | `saveSsoSettingsV2` | Missing or invalid SSO URL answers 500; the test expects 400 (bug 84686) | `SsoSaveTests.cs:129`; `Sso:211-214` | red by code reading |
| AC-3 | `saveSsoSettingsV2` | Incomplete field mapping answers 500; the test expects 400 (bug 84687) | `SsoSaveTests.cs:147`; `Sso:221-226` | red by code reading |
| AC-4 | `saveSsoSettingsV2` | Malformed SLO URL answers 500; the test expects 400 (bug 84688) | `SsoSaveTests.cs:165`; `Sso:216-219` | red by code reading |
| AC-5 | `sendSmsCode` | Valid credentials with SMS off for the account answer 500: the thrown exception type has no mapping; the test requires anything but 500 (bug 84503) | `SendSmsCodeTests.cs:125`; `web/ASC.Web.Core/Sms/SmsManager.cs:100-103`; `Wrap` default branch | red by code reading |
| AC-6 | `updateTfaSettings` | A numeric `type` outside the known values takes the `None` branch: two-factor is switched off and the answer is 200 false. The test name says the value is ignored; it asserts only false | `TfaSettingsUpdateTests.cs:86`; `Tfa:252,302-315` | by code reading |
| AC-7 | `getTfaSettings` | No role check: any signed-in role receives `trustedIps`, `mandatoryUsers` and `mandatoryGroups` | `Tfa:80-119`; tests cover only owner and anonymous | by code reading |
| AC-8 | `updateTfaSettings` | The 403 for naming the owner depends on the `id` field, not on the caller's role. An owner listing themselves in `mandatoryUsers` with no `id` gets 403 | `Tfa:241-246` | by code reading |
| AC-9 | `unlinkTfaApp` | The branch that treats an empty `id` as the caller is not reached: an empty `id` resolves to no account and answers 403 first | `Tfa:520-528`; `TfaUnlinkAppTests.cs:120` | by code reading |
| AC-10 | `updateTfaAppCodes` | Does not check that the method is on at portal level, while `getTfaAppCodes` does; with the method off and an application still linked the two disagree | `Tfa:432-434` vs `Tfa:476` | by code reading |
