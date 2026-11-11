# portal-settings — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement of `spec.md`, with where it was verified. The rows closed on 2026-11-11 cite tests that were run
class by class on the integration-test stack (the `Hosted/` class on integration-test-billing); the rest of the
document was written without a run. Paths are relative to the repo root; `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`. Tests:
`Common/`, `Cookies/`, `Banners/` are `web/ASC.Web.Api.Tests/Tests/02_Settings/{Common,Cookies,BannersVisibility}/`;
`Portal/` is `web/ASC.Web.Api.Tests/Tests/04_Portal/`; `Hosted/` is `web/ASC.Web.Api.Billing.Tests/Tests/Settings/` (the SaaS
stack of profile `integration-test-billing`); `AiSkills/` is `products/ASC.AI/ASC.AI.Tests/Tests/SkillTests/`;
`Csp/`, `SecBanners/` are `web/ASC.Web.Api.Tests/Tests/05_Security/{Csp,BannersVisibility}/`; `Unit/` is
`common/ASC.AI.Chat/app/` (vitest). Code: `Settings` is `web/ASC.Web.Api/Api/Settings/SettingsController.cs`,
`Message` is `web/ASC.Web.Api/Api/Settings/MessageSettingsController.cs`, `Security` is
`web/ASC.Web.Api/Api/SecurityController.cs`, `Dns` is `web/ASC.Web.Api/Core/DnsSettings.cs`, `Cookies` is
`web/ASC.Web.Core/CookiesManager.cs`, `Confirm` is `common/ASC.Api.Core/Auth/ConfirmAuthHandler.cs`, `Module` is
`web/ASC.Web.Api/Api/ModuleEventsController.cs`, `Skills` is `common/ASC.AI.Chat/app/skills/generateSkill.ts`,
`SkillsCtl` is `common/ASC.AI.Chat/app/controllers/skillsController.ts`. `TS` is
`../tests/api-tests/src/tests/settings/common/common.settings.spec.ts` (its comments are claims, not proof).

Status: **TEST-CONFIRMED** (the assertion was read in a test of this repository), **CODE-ONLY** (read from the
code or the yaml, no test). No row is cited by test name alone. Exception mapping from the `billing` verification
(`common/ASC.Api.Core/Middleware/ResponseWrapper.cs:55-115`): `ArgumentException` 400, `SecurityException` and
`InvalidOperationException` 403, `BillingException` 402, anything unmapped 500.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Setting the deep link mode** | | |
| Owner Web, ProvideChoice: 200, mode echoed, `lastModified` set | `Common/DeepLinkConfigureTests.cs:57-60,75-76` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin App: 200 | `Common/DeepLinkConfigureTests.cs:92-93` | TEST-CONFIRMED [run: PASS] |
| Other roles 403, unknown mode 400 | `Common/DeepLinkRulesTests.cs:52,73`; `Settings:547-551` | TEST-CONFIRMED [run: PASS] |
| **Reading the deep link mode** | | |
| Owner and DocSpace admin 200 with a mode | `Common/DeepLinkGetTests.cs:55-57,71-72` | TEST-CONFIRMED [run: PASS] |
| Anonymous allowed; 304 on unchanged | `Common/DeepLinkRulesTests.cs:92,109,131`; `Settings:578-583` | TEST-CONFIRMED [run: PASS] |
| **Trusted mail domains** | | |
| Right, Custom validation, audit, message; `domains` read only for Custom; tenant saved only after the checks | `Common/MailDomainSettingsTests.cs:47,70,91,126,146,162`; `Settings:266-298` | TEST-CONFIRMED [run: FAIL-BUG] — all pass except `:70`: after a refused Custom list the portal reports the entries read before the fault, see PS-7 |
| Type `All` with empty `domains` answers 200 | `Common/MailDomainSettingsTests.cs:110` | TEST-CONFIRMED [run: PASS] |
| **Time zone list** | | |
| Owner: 200, non-empty, id and name set | `Common/TimeZonesTests.cs:52-54` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin 200; User and Guest 403; anonymous 401 | `Common/TimeZonesTests.cs:68-69,86,100` | TEST-CONFIRMED [run: PASS] |
| UTC added, ordered by offset; confirmation link accepted | `Common/TimeZonesRulesTests.cs:48,61,128`; `Settings:682,688-697` | TEST-CONFIRMED [run: PASS] — the link tested is the setup Wizard link of a portal registered without a password; a link with the Administrators role stays CODE-ONLY (`Confirm:53-58`) |
| **Host name during setup** | | |
| Wizard role only; a session without a link passes the scheme but holds no such role, so 403; value is the request host | `Common/TimeZonesRulesTests.cs:82,97,111,145`; `Settings:721-726` | TEST-CONFIRMED [run: PASS] |
| **Custom domain of a self-hosted portal** | | |
| Anonymous 401 | `Common/EmailActivationAndDnsSettingsTests.cs:101` | TEST-CONFIRMED [run: PASS] |
| Self-hosted check first (415), then visibility, then right; normalisation; clearing; refusal; audit | `Hosted/HostedPortalSettingsTests.cs:45,57`; `Common/DnsSettingsTests.cs:65,81,97,114,136,160,186`; `Dns:46-79,81-122` | TEST-CONFIRMED [run: FAIL-BUG] — all pass except `:160`: a name another portal maps is refused and not stored, but the portal then reports it as its mapping, see PS-7. The hidden-section check stays CODE-ONLY (installation configuration) |
| **Content Security Policy follows the domain**: rename called after the save; the rename method was read in part. Not testable here: the moved value is only a Redis cache entry the router reads, no operation reads it back | `Dns:74`; `products/ASC.Files/Core/Helpers/CspSettingsHelper.cs` (`RenameDomainAsync`) | CODE-ONLY |
| **Dismissing the administrator helper** | `Common/AdminHelperTests.cs:56,75,93`; `Hosted/HostedPortalSettingsTests.cs:73`; `Settings:1019-1029`. The stored flag is read from `webstudio_settings` | TEST-CONFIRMED [run: PASS] — custom mode stays CODE-ONLY (installation configuration) |
| **Personal start page** | | |
| Owner saves two sections, portal settings follow | `Common/DefaultFolderSaveTests.cs:59-64` | TEST-CONFIRMED [run: PASS] |
| User's choice does not move the owner's | `Common/DefaultFolderSaveTests.cs:87-90` | TEST-CONFIRMED [run: PASS] |
| Room admin and Guest save; anonymous 401 | `Common/DefaultFolderPermissionsTests.cs:73-75,55` | TEST-CONFIRMED [run: PASS] |
| Allowed list, Guest and My documents 400, audit | `Common/DefaultFolderRulesTests.cs:54,74,91,106` | TEST-CONFIRMED [run: PASS] |
| **E-mail confirmation reminder** | | |
| Owner false, User true, anonymous 401 | `Common/EmailActivationAndDnsSettingsTests.cs:54-55,70-71,86` | TEST-CONFIRMED [run: PASS] |
| Stored per member | `Common/EmailActivationStorageTests.cs:50` (rows of `webstudio_settings`, no operation reads the value back) | TEST-CONFIRMED [run: PASS] |
| **Reading the socket settings**: asserts a body only, see PS-1 | `Common/SocketSettingsTests.cs:55-56,74-75` | TEST-CONFIRMED [run: PASS] |
| **Configuring the Content Security Policy** | | |
| Owner and DocSpace admin 200, domain and header | `Csp/CspConfigureTests.cs:58-61,77-80` | TEST-CONFIRMED [run: PASS] |
| Anonymous 401; Room admin, User, Guest 403 "Access denied" | `Csp/CspConfigureTests.cs:95,114-115` | TEST-CONFIRMED [run: PASS] |
| **Domains of the Content Security Policy** | `Csp/CspDomainsRulesTests.cs:58,77,93` | TEST-CONFIRMED [run: PASS] |
| **Reading the Content Security Policy** | | |
| All roles and anonymous read configured domain and header | `Csp/CspGetTests.cs:58-60,82-84,101-103` | TEST-CONFIRMED [run: PASS] |
| 304; empty list when nothing is stored | `Csp/CspGetRulesTests.cs:48,63` | TEST-CONFIRMED [run: PASS] |
| **Reading the session lifetime** | | |
| Owner and admin 200, anonymous 401, others 403 "Access denied" | `Cookies/CookieSettingsGetTests.cs:55-56,70-71,85,103-104` | TEST-CONFIRMED [run: PASS] |
| Body carries `lifeTime` and `enabled` | `Cookies/CookieSettingsUpdateTests.cs:140-141`; `Message:115-119` | TEST-CONFIRMED [run: PASS] |
| **Changing the session lifetime** | | |
| Owner, admin 200 with the message; anonymous 401; others 403 | `Cookies/CookieSettingsUpdateTests.cs:58-59,77-78,96,115-116` | TEST-CONFIRMED [run: PASS] |
| Clamp to 9999 read back | `Cookies/CookieSettingsUpdateTests.cs:132-141` | TEST-CONFIRMED [run: PASS] |
| 0 or less clears; 402 when hidden; audit | `Cookies/CookieSettingsRulesTests.cs:50,111`; `Message:148-155` | TEST-CONFIRMED [run: PASS] — the 402 of a hidden section stays CODE-ONLY (installation configuration) |
| **Sessions after a lifetime change** | `Cookies/CookieSettingsRulesTests.cs:71,92` (the token held before is refused with 401, for `enabled` true and false and after a DocSpace admin's change; a new sign-in works) | TEST-CONFIRMED [run: PASS] — the logout of active connections with `enabled` true stays CODE-ONLY |
| **Reading the banner flag** | `Banners/BannersVisibilityGetTests.cs:56-58,76-77`; `Banners/BannersVisibilityGetPermissionsTests.cs:56` | TEST-CONFIRMED [run: PASS] |
| **Setting the banner flag** | | |
| Anonymous 401 | `SecBanners/BannersVisibilitySetPermissionsTests.cs:74` | TEST-CONFIRMED [run: PASS] |
| 402 outside Enterprise before the right; 403; save and audit | `SecBanners/BannersVisibilitySetEditionTests.cs:50,66`; `Settings:1480-1493` | TEST-CONFIRMED [run: PASS] for the 402 before the right; the 403, the save and the audit on Enterprise stay CODE-ONLY (the stand has no license file) |
| **Module key of a module event** | `Portal/ModuleEvents/ModuleEventKeyTests.cs:56,71`; `Module:91-103,140-202` | TEST-CONFIRMED [run: PASS] for the 401 of a missing or unknown key; the 403 of another portal's key stays CODE-ONLY (the stand configures no module key) |
| **Addressee of a module event** | `Portal/ModuleEvents/ModuleEventKeyTests.cs:86`; `Module:88,105-134` | TEST-CONFIRMED [run: PASS] for the 413 of a body over 64 KB, refused before the key; the 400 and 404 stay CODE-ONLY (they follow a valid key, which the stand does not configure) |
| **Delivery of a module event**: needs a configured module key, which the stand does not have | `Module:61-72,136`; `common/ASC.Core.Common/Notify/Signalr/PluginSocketManager.cs:53-57` | CODE-ONLY |
| **Reading one skill file** | | |
| Foreign file id: 404, file not fetched | `Unit/storage/__tests__/roomSkills.test.ts:104-110` (unit test) | TEST-CONFIRMED [run: NO-LINK] |
| 400 for a missing parameter | `AiSkills/SkillReadRulesTests.cs:47` | TEST-CONFIRMED [run: PASS] |
| 256 KiB cut | `AiSkills/SkillReadRulesTests.cs:61` | TEST-CONFIRMED [run: PASS] |
| **Requests to generate a skill** | | |
| Other folder 400 before the model | `Unit/skills/__tests__/generateSkill.test.ts:146-150` | TEST-CONFIRMED [run: NO-LINK] |
| Control characters only: 400, model not called | `Unit/skills/__tests__/generateSkill.test.ts:138-143` | TEST-CONFIRMED [run: NO-LINK] |
| Access first; field limits; 404; 403 | `AiSkills/SkillGenerateRulesTests.cs:67,88,104,118` | TEST-CONFIRMED [run: PASS] |
| **Result of generating a skill** | | |
| Frontmatter rebuilt, tag dropped, nothing saved | `Unit/skills/__tests__/generateSkill.test.ts:107-118` | TEST-CONFIRMED [run: NO-LINK] |
| Name folding and 64 characters | `Unit/skills/__tests__/generateSkill.test.ts:41-45` | TEST-CONFIRMED [run: NO-LINK] |
| Fallback description | `Unit/skills/__tests__/generateSkill.test.ts:120-124` | TEST-CONFIRMED [run: NO-LINK] |
| **Failures of generating a skill** | | |
| No profile: error type asserted; the 503 comes from that type | `Unit/skills/__tests__/generateSkill.test.ts:152-155`; `Skills:83-84` | TEST-CONFIRMED [run: NO-LINK] |
| `empty_answer` 502; `providerMessage` filter | `AiSkills/SkillGenerateRulesTests.cs:169,186,205` (a blank answer; a 401 whose text is not relayed; a 429 whose text is) | TEST-CONFIRMED [run: PASS] — the lack-of-credit and spending-limit branches of the filter stay CODE-ONLY |

## Covered by other specifications

| Operation | Covered by |
|---|---|
| `aiPromptsCreate`, `aiPromptsUpdate`, `aiPromptsMove`, `aiPromptsDelete`, `aiPromptsList`, `aiPromptsGetById`, `aiPromptsCreateFolder`, `aiPromptsRenameFolder`, `aiPromptsDeleteFolder`, `aiPromptsListFolders`, `aiPromptsExport` | `ai-agents`, "Prompt library" (it names them as "the `aiPrompts*` operations: create, update, move, delete, get-by-id and list ...") |
| `terminateCustomerOperationsReport`, `createCustomerServiceUsageReport`, `getCustomerServiceUsageReport`, `terminateCustomerServiceUsageReport`, `createCustomerMonthlyUsageReport`, `getCustomerMonthlyUsageReport`, `terminateCustomerMonthlyUsageReport` | `billing`, "Wallet movements, usage and reports" ("Each has a report trio") |
| `getTotalServiceLimit`, `getGlobalServiceLimit`, `getParticipantServiceLimits`, `setParticipantServiceLimits`, `disableParticipantServiceLimits` | `billing`, "Managing the AI spending limits" (the operations under `portal/payment/servicelimit`) |
| `getAuditTrailTypes`, `getAuditTrailMappers` | `history-and-audit`, scenario "Types and mappers catalogues" (`audit/types`, `audit/mappers`) |
| `getLoginEventsByFilter` | `history-and-audit`, "Reading the login history" (`login/filter`) |

These specifications name the operations in words or by route, so `coverage.py`, which looks for the operationId,
keeps listing them.

## Differences between the yaml and the server

| Operation | yaml | Server | Source |
|---|---|---|---|
| `getTimeZones` | "not for a normal logged-in session": needs a confirmation link | An ordinary session of the owner or a DocSpace admin gets 200: a request with no link passes the scheme if signed in, and the role decides | `Settings:669-682`; `Confirm:53-58`; `Common/TimeZonesTests.cs:52,68` |
| `saveDnsSettings` | 200 "Confirmation that the DNS mapping was updated", a string | The code returns null, so there is no confirmation text | `Settings:741`; `Dns:75` |
| `saveDnsSettings` | 500 when the name is "already the alias or mapped domain of a portal, this one included" | The portal's own default address (alias plus base domain) is accepted before that check | `Settings:744`; `Dns:89-92` |
| `getTenantBannerSettings`, `setTenantBannerSettings` | The flag "only takes effect on" a self-hosted installation; on hosted portals banners are always shown | No server code found that reads the flag apart from these two operations, per the CodeGraph index, not LSP. Any such rule would be in the client, which was not read, so the spec does not state it | `Settings:1442-1447,1462-1467` |
| `getSocketSettings` | "Base URL of the portal's real-time notification hub" | See PS-1 | `Settings:1195,1199` |
| `aiContextGetRoomSkill` | 403 mentions "the room named by `entityId`" | The operation has no `entityId`; its parameters are `cloud`, `roomId`, `skillId` | yaml `aiContextGetRoomSkill`; `common/ASC.AI.Chat/app/controllers/contextController.ts:65-73` |
| `sendModuleEvent` | Route spelled `portal/moduleevents`, header parameter `x-Module-Key` | Code spells `portal/moduleEvents` and `X-Module-Key`; assumed to make no difference to a caller, since ASP.NET routes and HTTP headers are case-insensitive | `Module:44,47` |

## Left out

No operation of the owned tags is left out: 19 are in requirements and 26 are listed under "Covered by". Details
left out on purpose: the directives of the policy `header` (installation configuration), which browsers enforce it
(not a server rule), and what the socket service does with an event after the hand-over (it belongs to the
Node service).

## Suspected defects

| # | Where | What the server does | Evidence | Evidence kind |
|---|---|---|---|---|
| PS-1 | `getSocketSettings`, `Settings:1199` | Reads the configuration key `web:hub`, which in the default configuration is a section holding `url` and `internal`, so it has no value of its own and the answer's `url` is empty. The two reads of the same setting for `getPortalSettings` use `web:hub:url`. The test asserts only that a body came back | `Settings:141,157,1199`; `../buildtools/config/appsettings.json:190-193`; `common/ASC.AppHost/Configuration/ProjectConfigurator.cs:209` sets only `web:hub:internal`; `Common/SocketSettingsTests.cs:55-56`. Deployment environments that set a flat `web:hub` were not checked | red by code reading |
| PS-2 | `saveMailDomainSettings`, `Settings:292` | Stores the request's `inviteUsersAsVisitors` in a setting named `InviteAsUsers`, an opposite meaning, and the CodeGraph index shows no reader of that setting (LSP not run) | `Settings:292`; `web/ASC.Web.Core/StudioTrustedDomainSettings.cs:36` | red by code reading |
| PS-3 | `configureDeepLink`, `Settings:548` | A body without `deepLinkSettings` fails on a null reference: 500 for a request fault. The yaml documents the 500 | `Settings:543,548` | red by code reading |
| PS-4 | `configureCsp`, `Security:684` | A `null` entry in `domains` fails on a null reference: 500 for a request fault. The yaml documents the 500 | `Security:671,684` | red by code reading |
| PS-5 | `saveDnsSettings`, `Dns:53,78` | A malformed or taken name and a hidden section answer 500 through plain exceptions, not 400 or 402 as the sibling settings do. The yaml documents the 500 | `Dns:51-54,78`; `Settings:744`; compare `Message:148-151` | red by code reading |
| PS-6 | `closeAdminHelper`, `Settings:1021` | A signed-in non-administrator of a self-hosted portal gets 415 instead of 403, one branch for a role and for an edition. The yaml documents it | `Settings:1017,1021-1024` | red by code reading |
| PS-7 | `saveMailDomainSettings`, `Settings:270-287`; `saveDnsSettings`, `Dns:68-74` | Both change the current tenant object before the check that can refuse the request: the mail domains clear and refill `TrustedDomains`, the DNS save sets `MappedDomain` before `SaveTenantAsync` refuses a name another portal maps. The refusal is answered (400, 500) and the database keeps the old value, but that object is the cached tenant, so the portal then reports the refused value: the entries read before the malformed one, or the other portal's domain as its own mapping | `Common/MailDomainSettingsTests.cs:70`; `Common/DnsSettingsTests.cs:160` (reads `tenants_tenants.mappeddomain`: unchanged) | red in a test run |
