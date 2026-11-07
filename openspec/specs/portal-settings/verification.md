# portal-settings — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement of `spec.md`, with where it was verified. Nothing was run for this document: no test, build or
service. Paths are relative to the repo root; `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`. Tests:
`Common/`, `Cookies/`, `Banners/` are `web/ASC.Web.Api.Tests/Tests/02_Settings/{Common,Cookies,BannersVisibility}/`;
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
| Other roles 403, unknown mode 400 | `Settings:547-551` | CODE-ONLY |
| **Reading the deep link mode** | | |
| Owner and DocSpace admin 200 with a mode | `Common/DeepLinkGetTests.cs:55-57,71-72` | TEST-CONFIRMED [run: PASS] |
| Anonymous allowed; 304 on unchanged | `Settings:578-583` | CODE-ONLY |
| **Trusted mail domains** | | |
| Right, Custom validation, audit, message; `domains` read only for Custom; tenant saved only after the checks | `Settings:266-298` | CODE-ONLY |
| Type `All` with empty `domains` answers 200 | `../tests/api-tests/src/tests/settings/message/message.settings.spec.ts:154-162` (a TS setup step, not a .NET test) | CODE-ONLY |
| **Time zone list** | | |
| Owner: 200, non-empty, id and name set | `Common/TimeZonesTests.cs:52-54` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin 200; User and Guest 403; anonymous 401 | `Common/TimeZonesTests.cs:68-69,86,100` | TEST-CONFIRMED [run: PASS] |
| UTC added, ordered by offset; confirmation link accepted | `Settings:682,688-697`; `Confirm:53-58` | CODE-ONLY |
| **Host name during setup** | | |
| Wizard role only; a session without a link passes the scheme but holds no such role, so 403; value is the request host | `Settings:721-726`; `Confirm:53-58`; `TS:6-7` (comment) | CODE-ONLY |
| **Custom domain of a self-hosted portal** | | |
| Anonymous 401 | `Common/EmailActivationAndDnsSettingsTests.cs:101` | TEST-CONFIRMED [run: PASS] |
| Self-hosted check first (415), then visibility, then right; normalisation; clearing; refusal; audit | `Dns:46-79,81-122`; `Settings:743` | CODE-ONLY |
| **Content Security Policy follows the domain**: rename called after the save; the rename method was read in part | `Dns:74`; `products/ASC.Files/Core/Helpers/CspSettingsHelper.cs` (`RenameDomainAsync`) | CODE-ONLY |
| **Dismissing the administrator helper** | `Settings:1019-1029`; `TS:9-11` (comment, hosted 415) | CODE-ONLY |
| **Personal start page** | | |
| Owner saves two sections, portal settings follow | `Common/DefaultFolderSaveTests.cs:59-64` | TEST-CONFIRMED [run: PASS] |
| User's choice does not move the owner's | `Common/DefaultFolderSaveTests.cs:87-90` | TEST-CONFIRMED [run: PASS] |
| Room admin and Guest save; anonymous 401 | `Common/DefaultFolderPermissionsTests.cs:73-75,55` | TEST-CONFIRMED [run: PASS] |
| Allowed list, Guest and My documents 400, audit | `Settings:1090-1117` | CODE-ONLY |
| **E-mail confirmation reminder** | | |
| Owner false, User true, anonymous 401 | `Common/EmailActivationAndDnsSettingsTests.cs:54-55,70-71,86` | TEST-CONFIRMED [run: PASS] |
| Stored per member | `Settings:1138-1140` | CODE-ONLY |
| **Reading the socket settings**: asserts a body only, see PS-1 | `Common/SocketSettingsTests.cs:55-56,74-75` | TEST-CONFIRMED [run: PASS] |
| **Configuring the Content Security Policy** | | |
| Owner and DocSpace admin 200, domain and header | `Csp/CspConfigureTests.cs:58-61,77-80` | TEST-CONFIRMED [run: PASS] |
| Anonymous 401; Room admin, User, Guest 403 "Access denied" | `Csp/CspConfigureTests.cs:95,114-115` | TEST-CONFIRMED [run: PASS] |
| **Domains of the Content Security Policy** | `Security:680-703`; size check `products/ASC.Files/Core/Helpers/CspSettingsHelper.cs:119-124`, before the save at 135 | CODE-ONLY |
| **Reading the Content Security Policy** | | |
| All roles and anonymous read configured domain and header | `Csp/CspGetTests.cs:58-60,82-84,101-103` | TEST-CONFIRMED [run: PASS] |
| 304; empty list when nothing is stored | `Security:734-750` | CODE-ONLY |
| **Reading the session lifetime** | | |
| Owner and admin 200, anonymous 401, others 403 "Access denied" | `Cookies/CookieSettingsGetTests.cs:55-56,70-71,85,103-104` | TEST-CONFIRMED [run: PASS] |
| Body carries `lifeTime` and `enabled` | `Cookies/CookieSettingsUpdateTests.cs:140-141`; `Message:115-119` | TEST-CONFIRMED [run: PASS] |
| **Changing the session lifetime** | | |
| Owner, admin 200 with the message; anonymous 401; others 403 | `Cookies/CookieSettingsUpdateTests.cs:58-59,77-78,96,115-116` | TEST-CONFIRMED [run: PASS] |
| Clamp to 9999 read back | `Cookies/CookieSettingsUpdateTests.cs:132-141` | TEST-CONFIRMED [run: PASS] |
| 0 or less clears; 402 when hidden; audit | `Cookies:265-273`; `Message:148-155` | CODE-ONLY |
| **Sessions after a lifetime change**: the tests only re-authenticate and say why in comments, none asserts the old token fails | `Cookies:265-283`; `Cookies/CookieSettingsUpdateTests.cs:61,80,135-137` | CODE-ONLY |
| **Reading the banner flag** | `Banners/BannersVisibilityGetTests.cs:56-58,76-77`; `Banners/BannersVisibilityGetPermissionsTests.cs:56` | TEST-CONFIRMED [run: PASS] |
| **Setting the banner flag** | | |
| Anonymous 401 | `SecBanners/BannersVisibilitySetPermissionsTests.cs:74` | TEST-CONFIRMED [run: PASS] |
| 402 outside Enterprise before the right; 403; save and audit | `Settings:1480-1493`; test class comment at `SecBanners/BannersVisibilitySetPermissionsTests.cs:41-55` | CODE-ONLY |
| **Module key of a module event** | `Module:91-103,140-202` | CODE-ONLY |
| **Addressee of a module event** | `Module:88,105-134`; yaml `sendModuleEvent` (413) | CODE-ONLY |
| **Delivery of a module event** | `Module:61-72,136`; `common/ASC.Core.Common/Notify/Signalr/PluginSocketManager.cs:53-57` | CODE-ONLY |
| **Reading one skill file** | | |
| Foreign file id: 404, file not fetched | `Unit/storage/__tests__/roomSkills.test.ts:104-110` (unit test) | TEST-CONFIRMED [run: NO-LINK] |
| 400 for a missing parameter | `common/ASC.AI.Chat/app/controllers/contextController.ts:65-73` | CODE-ONLY |
| 256 KiB cut | yaml `aiContextGetRoomSkill` | CODE-ONLY |
| **Requests to generate a skill** | | |
| Other folder 400 before the model | `Unit/skills/__tests__/generateSkill.test.ts:146-150` | TEST-CONFIRMED [run: NO-LINK] |
| Control characters only: 400, model not called | `Unit/skills/__tests__/generateSkill.test.ts:138-143` | TEST-CONFIRMED [run: NO-LINK] |
| Access first; field limits; 404; 403 | `SkillsCtl`, `asInput` and `generate`; `Skills:55,59,222-234,238-241` | CODE-ONLY |
| **Result of generating a skill** | | |
| Frontmatter rebuilt, tag dropped, nothing saved | `Unit/skills/__tests__/generateSkill.test.ts:107-118` | TEST-CONFIRMED [run: NO-LINK] |
| Name folding and 64 characters | `Unit/skills/__tests__/generateSkill.test.ts:41-45` | TEST-CONFIRMED [run: NO-LINK] |
| Fallback description | `Unit/skills/__tests__/generateSkill.test.ts:120-124` | TEST-CONFIRMED [run: NO-LINK] |
| **Failures of generating a skill** | | |
| No profile: error type asserted; the 503 comes from that type | `Unit/skills/__tests__/generateSkill.test.ts:152-155`; `Skills:83-84` | TEST-CONFIRMED [run: NO-LINK] |
| `empty_answer` 502; `providerMessage` filter | `Skills:280-292`; `SkillsCtl`, `relaysProviderMessage` | CODE-ONLY |

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
