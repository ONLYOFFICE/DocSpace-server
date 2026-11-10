# branding-and-licensing — verification

Revision 2026-11-09 (bb406b409b..6fb013e745)

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo root
(`server/`); `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`; `WL` is
`web/ASC.Web.Api/Api/Settings/WhitelabelController.cs`, `Lic` is `web/ASC.Web.Api/Api/Settings/LicenseController.cs`,
`Greet` is `web/ASC.Web.Api/Api/Settings/GreetingSettingsController.cs`, `Settings` is
`web/ASC.Web.Api/Api/Settings/SettingsController.cs`, `Portal` is `web/ASC.Web.Api/Api/PortalController.cs`,
`TWLS` is `web/ASC.Web.Core/WhiteLabel/TenantWhiteLabelSettings.cs` (the settings class and
`TenantWhiteLabelSettingsHelper`), `LogoMgr` is `web/ASC.Web.Core/WhiteLabel/TenantLogoManager.cs`, `LogoType` is
`web/ASC.Web.Core/WhiteLabel/WhiteLabelLogoType.cs`, `Company` is `web/ASC.Web.Core/WhiteLabel/CompanyWhiteLabelSettings.cs`,
`Additional` is `web/ASC.Web.Core/WhiteLabel/AdditionalWhiteLabelSettings.cs`, `InfoSettings` is
`web/ASC.Web.Core/WhiteLabel/TenantInfoSettings.cs`, `Uploader` is `web/ASC.Web.Core/LogoUploader.cs`, `Reader` is
`common/ASC.Core.Common/Billing/License/LicenseReader.cs`, `License` is `common/ASC.Core.Common/Billing/License/License.cs`,
`TariffSvc` is `common/ASC.Core.Common/Billing/TariffService.cs` (`TenantExtraConfig` at 36-46, `CalculateTariffAsync`
at 1151-1229; the service-limit work of hotfix v4.0.1 shifted the lines, the tariff code itself is unchanged), `Extra` is `web/ASC.Web.Core/Utility/TenantExtra.cs`, `TariffSettings` is `web/ASC.Web.Core/TariffSettings.cs`,
`FirstTime` is `web/ASC.Web.Api/Core/FirstTimeTenantSettings.cs`, `DocsLic` is
`products/ASC.Files/Core/Helpers/DocumentServiceLicense.cs`, `RefreshSvc` is
`products/ASC.Files/Worker/Services/RefreshLicenseService.cs`, `Themes` is
`web/ASC.Web.Core/Utility/Settings/CustomColorThemesSettings.cs`, `Quota` is `common/ASC.Core.Common/Tenants/TenantQuota.cs`,
`PayFilter` is `common/ASC.Api.Core/Middleware/PaymentFilter.cs`, `Wrapper` is `common/ASC.Api.Core/Middleware/ResponseWrapper.cs`,
`Core` is `common/ASC.Core.Common/Context/Impl/CoreConfiguration.cs`, `appsettings` is `../buildtools/config/appsettings.json`,
`storage.json` is `../buildtools/config/storage.json`, `nginx` is `../buildtools/config/nginx/onlyoffice.conf`, `Resource` is
`web/ASC.Web.Core/PublicResources/Resource.resx`, `UCResource` is `web/ASC.Web.Core/PublicResources/UserControlsCommonResource.resx`.
Tests: `T/` is `web/ASC.Web.Api.Tests/Tests/`, `TS/` is `../tests/api-tests/src/tests/settings/`. Status: **TEST-CONFIRMED**
(an integration test asserts it; `(TS)` when only the TypeScript suite does, run against a hosted test portal),
**CODE-ONLY** (read from the product code, the configuration or the OpenAPI document), **NOTES-ONLY** (only the release
notes say it — nothing of that kind was written into the spec). Exception → status mapping (`Wrapper:53-131`):
`ArgumentException` (incl. `ArgumentNullException`, `ArgumentOutOfRangeException`) 400, `SecurityException` 403
"Access denied", `InvalidOperationException` 403, `BillingException` and `TenantQuotaException` 402,
`NotSupportedException` 415, anything else 500.

The .NET suite covers the greeting reads and permissions, the colour themes, the portal logo, the payment settings,
the tariff grading and — since hotfix v4.0.1 (commit `fafe467580`) — the rebranding reads, permissions, the two
restores and the gates in front of the saves (`T/02_Settings/Rebranding/`, seven files since release/v4.1.0:
`RebrandingAnonymousTests.cs` and the User cases of the saves and deletes in `RebrandingPermissionsTests.cs`,
`632c2fc262`). That suite runs on a
**standalone** integration host (`APP_HOSTING_STANDALONE=true`) whose default quota has no `customization`, so the
installation gate of the company/additional saves is open and the plan gate (402) is what the owner hits; the TS suite
ran on a hosted portal and hit the installation gate (403) instead. Both are recorded. The two deletes of the company
and additional settings are deliberately not ported: on a standalone host they would reset installation-wide settings
shared by every portal (`T/02_Settings/Rebranding/RebrandingSaveTests.cs:42-49`). The licence operations have .NET
tests only for what the open-source integration host (no license path) can do without replacing the installation's
licence: `T/02_Settings/License/` (access checks incl. L8, the not-Enterprise answers of accept/refresh/upload/required,
the edition flags; 2026-11-10); everything past the edition check needs an Enterprise license path and stays CODE-ONLY
(the TS licence file states the same for the hosted environment, `TS/license/license.spec.ts:1-10`). The greeting save/restore tests exist but are commented out (see L10).

Revision 2026-11-04: re-checked against the merge of `release/v4.1.0` (`d58816bece..3533ad8a89`). Of the files behind
this specification only `TenantLogoManager.cs` (the letter-logo fetch got a ten-second timeout and a five-minute
failure memory, `559b4c1309`), `PaymentFilter.cs` (the `Payment-Info` bypass of the payment gate is gone,
`dbb446ada2`) and `SettingsController.cs` (two lines of AI remarks) changed; `WL`, `TWLS`, `Lic`, `Reader`, `Greet`,
`Company`, `Additional`, `Themes` and the DTOs did not. Line references of `LogoMgr`, `TariffSvc`, `Settings` and
`PayFilter` were refreshed. The yaml was regenerated (`5ea4dc6`); its line references were refreshed, and it still has
no `settings/license/trial` path (L12).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Editions of the installation** | | |
| Hosted = base domain configured; self-hosted = base domain `localhost`; Enterprise = self-hosted + license path; Developer = Enterprise + license type `developer`; open source = self-hosted without license path | `Core:48-63`; `TariffSvc:36-46`; `Reader:43-57` (`license:file:path`, `license:type`); `appsettings:13, 74-78`; `../buildtools/config/appsettings.enterprise.json:7-8`, `appsettings.developer.json:7-8`; `T/02_Settings/License/LicenseOpenSourceTests.cs:103-117` (owner: `openSource` true, `enterprise`/`developer` false on the stand, which has no license path) | TEST-CONFIRMED [run: PASS] (open source), CODE-ONLY (hosted, Enterprise, Developer: need another configuration) |
| `getSettings.standalone`; `getPortalTariff` fills `openSource`/`enterprise`/`developer` only for the portal-settings right | `Settings:119`; `Portal:593-623`; yaml `TariffDto` (105412); `T/02_Settings/License/LicenseOpenSourceTests.cs:90-132` (`standalone` true; owner gets the flags, RoomAdmin/User get null) | TEST-CONFIRMED [run: PASS] |
| Members without the right get `id` 0, `state` Paid, 200 | `T/04_Portal/Quota/PortalTariffPermissionsTests.cs:61-77` (typed SDK call since `632c2fc262`); owner/admin full tariff `T/04_Portal/Quota/PortalTariffTests.cs:53-93` (edition flags not asserted); `T/02_Settings/License/LicenseOpenSourceTests.cs:103-132` (flags) | TEST-CONFIRMED [run: PASS] |
| **Whether branding may be changed** | | |
| Gate = branding section not in `web:hide-settings` AND quota `customization`; `getEnableWhitelabel` needs the portal-settings right | `LogoMgr:56-63, 305-313`; `WL:945-950`; `appsettings:181-189` (WhiteLabel not listed → enabled); yaml `getEnableWhitelabel` (64857); `T/02_Settings/Rebranding/RebrandingReadTests.cs:48-58` (owner on a plan without customization → false); `T/02_Settings/Rebranding/RebrandingPermissionsTests.cs:137-164` (DocSpace admin 200, User 403) | TEST-CONFIRMED [run: PASS] (quota half of the gate, right), CODE-ONLY (`web:hide-settings` half: configuration) |
| Saves answer 402 "Your pricing plan does not support this option" after the permission check | `LogoMgr:315-321` (`BillingException`), `WL:83-96, 164-182, 455-470`; `Resource:861-863`; `T/02_Settings/Rebranding/RebrandingContractTests.cs:105-118` (402 with the message); `T/02_Settings/Rebranding/RebrandingPermissionsTests.cs:220-236` (User 403 before the plan check) | TEST-CONFIRMED [run: PASS] |
| Owner: `getEnableWhitelabel` false, `saveWhiteLabelLogoText` 402, `saveWhiteLabelSettings` 402; User 403 before the plan (`saveWhiteLabelLogoText`, `getEnableWhitelabel`); DocSpace admin 200 on `getEnableWhitelabel`; restores 200 true | `T/02_Settings/Rebranding/RebrandingReadTests.cs:49-58`; `RebrandingSaveTests.cs:53-77`; `RebrandingPermissionsTests.cs:138-163, 221-235`; `RebrandingRestoreTests.cs:43-63`; `TS/rebranding/rebranding.spec.ts:17-28, 151-203`; `TS/rebranding/rebranding.permissions.spec.ts:79-103, 148-176` | TEST-CONFIRMED [run: PASS] |
| The same gate closes `saveCompanyWhiteLabelSettings` and `saveAdditionalWhiteLabelSettings` (402) once the installation check passed | `WL:655-657, 778-780, 952-965` (`DemandRebrandingPermissionAsync` → `DemandWhiteLabelPermissionAsync`); `T/02_Settings/Rebranding/RebrandingSaveTests.cs:91-113` (standalone owner) | TEST-CONFIRMED [run: PASS] |
| **Logo slots and their public listing** | | |
| Sixteen slots; listing skips `Notification`; `[AllowAnonymous, AllowNotPayment, AllowSuspended, AllowBlocked]`; `isDark` fills one theme; `dark` null when equal to `light` | `LogoType:44-93`; `WL:253-322`; yaml `getWhiteLabelLogos` (63579), `WhiteLabelItemDto` (109156); `T/02_Settings/Rebranding/RebrandingReadTests.cs:103-118` (15 listed, no `Notification`, anonymous); `T/02_Settings/Rebranding/RebrandingContractTests.cs:79-105` (`isDark` fills one theme, `dark` null when equal to `light`) | TEST-CONFIRMED [run: PASS], CODE-ONLY (`AllowNotPayment`/`AllowSuspended`/`AllowBlocked`: needs an unpaid/suspended portal) |
| Anonymous gets 200 with 15 entries, none `Notification` | `T/02_Settings/Rebranding/RebrandingReadTests.cs:104-117`; `RebrandingPermissionsTests.cs:125-136`; `TS/rebranding/rebranding.spec.ts:92-106`; `TS/rebranding/rebranding.permissions.spec.ts:68-77` | TEST-CONFIRMED [run: PASS] |
| Sizes 422x48, 772x88, 32x32, 172x40 (all editors), 56x56, 442x48, letter 386x44 | `TWLS:167-206`; `T/02_Settings/Rebranding/RebrandingContractTests.cs:66-79` (every listed slot) | TEST-CONFIRMED [run: PASS] (listed slots), CODE-ONLY (letter 386x44: `Notification` is not listed) |
| **Saving logos from image data** | | |
| Portal-settings right, gate, `logo` key/value list, `key` parsed as int, light/dark data URI (`image/jpeg`, `image/png`, `image/svg+xml`) or temp file name; slot omitted keeps image; dark only for slots that can be dark; login-page save rebuilds `Notification`; `[StringLength(40)]` on `logoText`, not stored; audit; letter-logo cache dropped | `WL:81-140`; `TWLS:671-760, 975-992` (`CanBeDark`); `web/ASC.Web.Api/ApiModels/RequestsDto/WhiteLabelRequestsDto.cs:45-84`; `LogoMgr:371-381`; yaml `saveWhiteLabelSettings` (63365); `T/02_Settings/Rebranding/RebrandingPermissionsTests.cs:250-266` (User 403); `T/02_Settings/Rebranding/RebrandingSaveTests.cs:66-77` (owner 402) | TEST-CONFIRMED [run: PASS] (right, gate), CODE-ONLY (the rest: unreachable, no plan with customization on the stand) |
| Dark image ignored for favicon/editor slots, slot still marked customised | `TWLS:702-708` | CODE-ONLY |
| User 403 on `saveWhiteLabelSettings` and `saveWhiteLabelSettingsFromFiles` before the plan check; anonymous 401 on every save, restore and delete of the branding (nine operations) before any gate | `T/02_Settings/Rebranding/RebrandingPermissionsTests.cs:251-280`; `RebrandingAnonymousTests.cs:44-64` | TEST-CONFIRMED [run: PASS] |
| **Saving logos from uploaded files** | | |
| No file → `InvalidOperationException("No input files")` → 403 before the gate; name `<slot>.<ext>`, `dark` in the name → dark variant | `WL:162-235`; `TWLS:762-778`; `T/02_Settings/Rebranding/RebrandingSaveTests.cs:79-89` (owner, no multipart body → 403); `TS/rebranding/rebranding.spec.ts:205-215` | TEST-CONFIRMED [run: PASS] (no-file), CODE-ONLY (rest) |
| Staging upload `/logoUploader.ashx`: portal-settings right + gate, `logotype`/`width`/`height` form fields, extensions `.bmp .jpg .jpeg .ico .png .svg`, size ≤ slot size, returns the temp file name | `Uploader:384-473`; `TWLS:40`; `nginx:702`; the temp name is consumed by `TWLS:735-748` | CODE-ONLY |
| **Which slots hold a custom image** | | |
| Portal-settings right; 16 entries incl. `Notification` (mapped to the login-page flag); `isDefault` query reads tenant −1 | `WL:344-362`; `TWLS:309-331`; `T/02_Settings/Rebranding/RebrandingPermissionsTests.cs:164-178` (User 403); `T/02_Settings/Rebranding/RebrandingReadTests.cs:118-131` (16 incl. `Notification`) | TEST-CONFIRMED [run: PASS] (right, 16 entries), CODE-ONLY (`isDefault` reads tenant −1) |
| Fresh portal: 16 entries incl. `Notification`, all `default` true; User 403 | `T/02_Settings/Rebranding/RebrandingReadTests.cs:119-130`; `RebrandingPermissionsTests.cs:165-177`; `TS/rebranding/rebranding.spec.ts:108-123`; `TS/rebranding/rebranding.permissions.spec.ts:105-117` | TEST-CONFIRMED [run: PASS] |
| **Restoring the built-in logos** | | |
| Portal-settings right, no gate; deletes `*` in the `whitelabel` storage module; resets the legacy logo (`InfoSettings`); cache dropped; audit; 200 true | `WL:383-426`; `TWLS:585-630`; `InfoSettings:168-183`; `storage.json:92-98`; `T/02_Settings/Rebranding/RebrandingPermissionsTests.cs:236-250` (User 403); `T/02_Settings/Rebranding/RebrandingRestoreTests.cs:53-63` (owner on a plan without customization → 200 true) | TEST-CONFIRMED [run: PASS] (right, no gate, 200 true), CODE-ONLY (storage delete, legacy logo, cache, audit) |
| Owner restores with nothing stored → 200 true | `T/02_Settings/Rebranding/RebrandingRestoreTests.cs:54-63`; `TS/rebranding/rebranding.spec.ts:164-175` | TEST-CONFIRMED [run: PASS] |
| Only 7 of the 15 slot flags are reset by the restore (see L3) | `TWLS:587-614` | — |
| **Saving and clearing the logo text** | | |
| Gate; `[StringLength(40)]` → 400; empty/blank/`ONLYOFFICE` stored as null; restore without gate; audit `WhiteLabelSettingsLogoTextUpdated` | `WL:453-481, 569-593`; `TWLS:134-150, 632-637`; `common/ASC.Core.Common/WhiteLabel/BaseWhiteLabelSettings.cs` (`DefaultLogoText`); `T/02_Settings/Rebranding/RebrandingSaveTests.cs:52-65` (402); `T/02_Settings/Rebranding/RebrandingContractTests.cs:119-132` (41 characters → 400 before the gate); `T/02_Settings/Rebranding/RebrandingRestoreTests.cs:42-53` (restore without gate) | TEST-CONFIRMED [run: PASS] (gate, length, restore), CODE-ONLY (stored as null, audit: need a successful save) |
| Owner restores the wordmark with nothing stored → 200 true; User 403 on the restore | `T/02_Settings/Rebranding/RebrandingRestoreTests.cs:43-52`; `RebrandingPermissionsTests.cs:207-219`; `TS/rebranding/rebranding.spec.ts:151-162` | TEST-CONFIRMED [run: PASS] |
| **Reading the logo text** | | |
| `getWhiteLabelLogoText` returns `LogoText ?? "ONLYOFFICE"` — no fallback to tenant −1 unless `isDefault=true`; `isdefault` = stored text empty; `getSettings.logoText` uses the three-level fallback | `WL:504-547`; `TWLS:136-145`; `LogoMgr:273-282`; `Settings:134`; `T/02_Settings/Rebranding/RebrandingReadTests.cs:131-154` (`ONLYOFFICE`, `isdefault` true on a fresh portal) | TEST-CONFIRMED [run: PASS] (built-in text, `isdefault`), CODE-ONLY (fallback chain: needs a stored text) |
| Owner reads "ONLYOFFICE" and `{logotext, default:true}`; User 403 on both | `T/02_Settings/Rebranding/RebrandingReadTests.cs:132-153`; `RebrandingPermissionsTests.cs:179-205`; `TS/rebranding/rebranding.spec.ts:125-149`; `TS/rebranding/rebranding.permissions.spec.ts:119-146` | TEST-CONFIRMED [run: PASS] |
| **Installation-wide default branding** | | |
| `isDefault=true` → `DemandRebrandingPermissionAsync`: standalone AND not `LimitedAccessSpace` (403), not `core:custom-mode` (403), gate for saves only; stored for tenant −1 in `static_partnerdata`; portals inherit per slot and for the text | `WL:85-90, 171-176, 387-392, 459-464, 575-580, 952-965`; `Extra:108-114`; `Core:63`; `TWLS:143-145, 817-823, 849-868`; `storage.json:101-107`; `T/02_Settings/Rebranding/RebrandingContractTests.cs:151-175` (standalone owner: read 200, save stopped by the gate 402) | TEST-CONFIRMED [run: PASS] (installation check open on standalone, gate for saves), CODE-ONLY (`LimitedAccessSpace`/custom-mode 403, storage, inheritance: other configuration or an installation-wide write) |
| **Company details of the installation** | | |
| Any signed-in member; `[AllowNotPayment]`; 304; `isDefault` via field comparison; `getLicensorData` appends the built-in when replaced and not licensor | `WL:617-631, 712-717`; `Company:537-577`; `web/ASC.Web.Api/ApiModels/ResponseDto/CompanyWhiteLabelSettingsDto.cs:39-111`; `T/02_Settings/Rebranding/RebrandingPermissionsTests.cs:43-124` (User 200, anonymous 401) | TEST-CONFIRMED [run: PASS] (signed-in member), CODE-ONLY (304, `isDefault`, licensor append) |
| Built-in vendor "Ascensio System SIA", `site` `https://www.onlyoffice.com`, `isLicensor` true, `isDefault` true; single-item licensor list; User 200, anonymous 401 on both reads | `T/02_Settings/Rebranding/RebrandingReadTests.cs:60-72, 91-102`; `RebrandingPermissionsTests.cs:44-69, 98-123`; `TS/rebranding/rebranding.spec.ts:30-48, 73-90`; `TS/rebranding/rebranding.permissions.spec.ts:4-25, 51-66` | TEST-CONFIRMED [run: PASS] |
| **Changing and resetting the company details** | | |
| Portal-settings right, installation check (403), gate (402); `settings`/`email`/`site` null → `ArgumentNullException` 400, puny-code → 400, DTO `[Url]`/`[EmailAddress]`/`[Phone]`/`[StringLength(255)]`; `IsLicensor` false; `HideAbout = quota.Branding && input`; saved for tenant −1; delete without gate returns defaults | `WL:653-691, 737-750`; `web/ASC.Web.Api/ApiModels/RequestsDto/WhiteLabelSettingsRequestDto.cs:39-109`; yaml `saveCompanyWhiteLabelSettings` (64385); `T/02_Settings/Rebranding/RebrandingPermissionsTests.cs:281-296` (User 403); `T/02_Settings/Rebranding/RebrandingSaveTests.cs:90-101` (owner 402); `T/02_Settings/Rebranding/RebrandingContractTests.cs:132-149` (`[EmailAddress]` → 400 before the gate) | TEST-CONFIRMED [run: PASS] (right, gate, DTO validation), CODE-ONLY (null/puny-code checks, stored values, delete: behind the gate or installation-wide) |
| Hosted owner: save 403, delete 403 | `TS/rebranding/rebranding.spec.ts:217-237, 262-272` | TEST-CONFIRMED [run: NO-LINK] (TS) |
| Self-hosted owner on a quota without `customization`: save 402 — the gate fires before the body is validated (the test sends no `email`, which would otherwise be 400) | `WL:655-660`; `T/02_Settings/Rebranding/RebrandingSaveTests.cs:91-101` | TEST-CONFIRMED [run: PASS] |
| User 403 on the company and additional save and delete, from the portal-settings demand before the installation check (the owner deletes stay unported) | `WL:655, 739, 778, 838`; `T/02_Settings/Rebranding/RebrandingPermissionsTests.cs:282-310, 315-341` | TEST-CONFIRMED [run: PASS] |
| **Help and community links of the installation** | | |
| Flags + `isDefault`; defaults true when the regional address exists; save replaces the set (`RequiredMappingStrategy.Source` mapper, bools default false); delete returns defaults | `WL:776-849`; `Additional:617-687`; `web/ASC.Web.Api/ApiModels/ResponseDto/AdditionalWhiteLabelSettingsDto.cs:39-114`; `web/ASC.Web.Api/ApiModels/RequestsDto/WhiteLabelSettingsRequestDto.cs:114-176` | CODE-ONLY |
| Five flags true on the test installation (`helpCenter`, `feedbackAndSupport`, `userForum`, `videoGuides`, `licenseAgreements`); the `startDocsEnabled` and `isDefault` assertions were commented out in `bbba22fbf1` (the host now runs a Document Server), so those two are CODE-ONLY; hosted owner save/delete 403; User 200, anonymous 401 | `T/02_Settings/Rebranding/RebrandingReadTests.cs:74-89`; `RebrandingPermissionsTests.cs:71-96`; `TS/rebranding/rebranding.spec.ts:50-71, 239-260, 274-284`; `TS/rebranding/rebranding.permissions.spec.ts:27-49` | TEST-CONFIRMED [run: PASS] |
| Self-hosted owner on a quota without `customization`: save (all six flags true) 402 | `WL:778-782`; `T/02_Settings/Rebranding/RebrandingSaveTests.cs:103-113` | TEST-CONFIRMED [run: PASS] |
| **Where the branding appears and how it is served** | | |
| Editor logo by file type and embed mode; letter logo = uploaded `Notification` → installation default → embedded `logo.png`; letter logo cached one day per culture; `displayAbout` formula | `LogoMgr:233-270, 326-369, 416-429`; `LogoType:95-108`; `Settings:128, 134`; `openspec/specs/notifications/spec.md:594-611` | CODE-ONLY |
| Letter logo as embedded data for the reports: fetched from the portal's own address once per logo path under a 10 s deadline, kept a day; a failed or timed-out fetch is remembered as empty for 5 min and the caller falls back to the logo URL (`559b4c1309`) | `LogoMgr:51-55, 160-212` | CODE-ONLY |
| Logo URLs with `?hash`, built-in with `?hash=<version:number>`; proxy `immutable, max-age=31536000` on `storage/whitelabel/root/*?hash*` and `static_partnerdata`; `whitelabel` module served as attachment (v2.6 notes) | `TWLS:805-847`; `nginx:61`; `storage.json:92-98`; release notes v2.6 (technical) line 33 | CODE-ONLY |
| **Reading the colour themes** | | |
| `[AllowAnonymous, AllowNotPayment, AllowSuspended, AllowBlocked]`; 304; six defaults, `Selected` = min id; `limit` from `core:themelimit` (9 in the dev config, 0 = unlimited) | `Settings:875-886`; `Themes:250-354`; `appsettings:59`; `T/02_Settings/Common/ColorThemeRulesTests.cs:45-58` (anonymous: ids 1-6, selected 1, limit 9) | TEST-CONFIRMED [run: PASS], CODE-ONLY (304, unpaid/suspended) |
| Owner and every member role get 200, non-empty `themes`, `selected` ≥ 0, `limit` ≥ 0 | `T/02_Settings/Common/ColorThemeGetTests.cs:45-79` | TEST-CONFIRMED [run: PASS] |
| **Adding, changing and selecting a colour theme** | | |
| Portal-settings right; built-in id → new theme; existing custom id → colours replaced; add only while `Themes.Count < Limit` (built-ins counted) or `Limit == 0`; id = max+1; `Name` emptied; `selected` only when it exists; audit only on selection | `Settings:904-966`; `web/ASC.Web.Api/ApiModels/RequestsDto/CustomColorThemesSettingsRequestsDto.cs:39-122`; `T/02_Settings/Common/ColorThemeRulesTests.cs:60-131, 179-195` (built-in id → new unnamed theme 7, custom id → colours replaced, selection only of an existing id, three custom themes fit limit 9, RoomAdmin/User/Guest 403) | TEST-CONFIRMED [run: PASS], CODE-ONLY (audit on selection) |
| Owner adds a theme → 200, themes non-empty, new theme has the max id; DocSpace admin adds → 200 | `T/02_Settings/Common/ColorThemeSaveTests.cs:45-86`; `T/02_Settings/Common/ColorThemeDeleteTests.cs:51-59` (max id read back) | TEST-CONFIRMED [run: PASS] |
| Limit reached → silently dropped (see L6) | `Settings:943-955`; `T/02_Settings/Common/ColorThemeRulesTests.cs:133-149` (asserts a client error; fails: 200 with the unchanged list) | TEST-CONFIRMED [run: FAIL-BUG] (L6) |
| **Deleting a colour theme** | | |
| Portal-settings right; built-in id untouched; selected falls back to min id with audit | `Settings:982-1004`; `T/02_Settings/Common/ColorThemeRulesTests.cs:150-178, 196-213` (built-in id untouched, selected falls back to id 1, RoomAdmin/User/Guest 403) | TEST-CONFIRMED [run: PASS], CODE-ONLY (audit) |
| Owner deletes a custom theme: count −1, id gone; DocSpace admin deletes the owner's theme | `T/02_Settings/Common/ColorThemeDeleteTests.cs:45-94` | TEST-CONFIRMED [run: PASS] |
| **Greeting title of the portal** | | |
| `tenant.Name == "" ? PortalName : Name`; `isdefault` = name empty; `getSettings.greetingSettings` same text; caption "Cloud space for your office docs" | `Greet:404-431`; `Settings:117`; `Resource:1146-1148`; `T/02_Settings/Greeting/GreetingDefaultsTests.cs:44-55` (fresh portal: the caption, same text in `getSettings.greetingSettings`) | TEST-CONFIRMED [run: PASS] (empty name), CODE-ONLY (stored name: L10 blocks the save) |
| Every role 200 non-empty; owner/admin `isdefault` true on a fresh portal; anonymous 401 on both | `T/02_Settings/Greeting/GreetingGetTests.cs:7-29`; `GreetingIsDefaultTests.cs:7-23`; `GreetingReadPermissionsTests.cs:7-22` | TEST-CONFIRMED [run: NOT-IN-RUN] |
| **Changing and restoring the greeting title** | | |
| Portal-settings right; `[StringLength(255)] required Title`; hosted free/trial → `ValidateTenantName` (regex `core:tenantname:regex`, hosted only) → 400; audit `GreetingSettingsUpdated`; restore sets `web:portal-name` or "" | `Greet:452-509`; `web/ASC.Web.Api/ApiModels/RequestsDto/GreetingSettingsRequestsDto.cs:48-49`; `common/ASC.Core.Common/Tenants/TenantDomainValidator.cs:95-125`; `InfoSettings:161-166`; `Resource:1200-1202`; `T/02_Settings/Greeting/GreetingDefaultsTests.cs:56-67` (256 characters → 400) | TEST-CONFIRMED [run: PASS] (length), CODE-ONLY (hosted regex: SaaS only; audit, restore value: L10) |
| Room admin / User / Guest 403 "Access denied", anonymous 401 on save and restore | `T/02_Settings/Greeting/GreetingSavePermissionsTests.cs:7-29`; `GreetingRestorePermissionsTests.cs:7-27` | TEST-CONFIRMED [run: NOT-IN-RUN] |
| Owner sets and restores a title | `T/02_Settings/Greeting/GreetingSaveTests.cs:44-72` and `GreetingRestoreTests.cs:44-72` — written but commented out (L10); `TS/greeting/greeting.spec.ts` | CODE-ONLY (.NET), TEST-CONFIRMED [run: NOT-IN-RUN] (TS) |
| **Legacy portal logo** | | |
| Any signed-in member; `notifications/logo.png` when default; 304; reset by the logo restore | `Settings:802-806`; `InfoSettings:213-222`; `WL:408-410`; `T/02_Settings/Common/PortalLogoTests.cs:44-77` (owner and DocSpace admin/RoomAdmin/User/Guest → 200 with a URL) | TEST-CONFIRMED [run: PASS] (signed-in member), CODE-ONLY (`logo.png` path, 304, reset by the restore) |
| Every role 200 with a value | `T/02_Settings/Common/PortalLogoTests.cs:45-76` | TEST-CONFIRMED [run: PASS] |
| **Uploading a license file** | | |
| `[Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard, Administrators")]`; 403 when unauthenticated and wizard completed; not Enterprise → `NotSupportedException` 415; no file → `ArgumentException` 400; staged to `<path>.tmp`; customer id/signature missing → `BillingNotConfiguredException` → generic → 500 "License is not correct"; future start → `ArgumentOutOfRangeException` 400; overdue message with `Update` flag variant | `Lic:264-341`; `Reader:176-240`; `License:419-451`; `Resource:834-836, 1107-1127`; `T/02_Settings/License/LicensePermissionsTests.cs:117-130` (RoomAdmin/User/Guest 403); `T/02_Settings/License/LicenseOpenSourceTests.cs:64-75` (owner, no license path → 415 before the file is read) | TEST-CONFIRMED [run: PASS] (role, edition), CODE-ONLY (the rest: needs an Enterprise license path, unreachable) |
| Edition check (`docspace_dev` vs `license:type`) is disabled since bug 84212 | `Reader:229-237` (commented out) | CODE-ONLY |
| **Activating a license** | | |
| No edition → "Your pricing plan does not support this option" with 200; records `LicenseAccept` for tenant −1; audit `LicenseKeyUploaded`; refresh: backup → move temp → parse → editing-service validation → quota/tariff; failure restores the backup; messages per exception | `Lic:105-147`; `TariffSettings:201-214`; `Reader:108-174`; `DocsLic:83-140`; `UCResource:120-131`; `T/02_Settings/License/LicenseOpenSourceTests.cs:42-52` (owner → 200 with the message, nothing written) | TEST-CONFIRMED [run: PASS] (no edition), CODE-ONLY (the rest: needs an Enterprise license path, unreachable) |
| Wizard completion refreshes the license and records acceptance | `FirstTime:98-104, 139-156`; `Settings` wizard action (`CompleteWizard`) | CODE-ONLY |
| No permission check on `acceptLicense` and `refreshLicense` (L8) | `Lic:73-88, 105-147`; `T/02_Settings/License/LicensePermissionsTests.cs:72-109` (RoomAdmin/User/Guest are expected to get 403; they get 200) | TEST-CONFIRMED [run: FAIL-BUG] (L8) |
| **What the license grants** | | |
| Quota `-1000` "license": `Audit, Ldap, Sso, ThirdParty, Restore, Oauth, ContentSearch, DocsEdition, Statistic` true, `Branding`/`Customization`/`AutomationApi`/`Trial` from the file, `Lifetime = !Trial && !TimeLimited`, `MaxFileSize` from the default quota; tariff `DueDate = license.DueDate`; one tariff for all portals | `Reader:242-279`; `License:332-417`; `TariffSvc:100-106` | CODE-ONLY |
| **Refreshing the license** | | |
| Not Enterprise → false; `BillingNotFoundException`/`BillingNotConfiguredException` → 402, future start → 400; periodic re-read in the Files worker, period `files:refreshLicense:period` (default "1" = one day) | `Lic:77-88`; `RefreshSvc:36-61`; release notes v3.0.1 line 15; `T/02_Settings/License/LicenseOpenSourceTests.cs:53-63` (owner → false) | TEST-CONFIRMED [run: PASS] (not Enterprise), CODE-ONLY (402/400, worker period: Enterprise, unreachable) |
| Periodic re-read only after acceptance, of the license in force only; a staged upload is left in place and not validated (2c268c3041) | `common/Tests/ASC.Core.Common.Tests/LicenseReaderTests.cs:79-82,92-94` (re-read of the active file leaves the staged one); the acceptance gate: `RefreshSvc` | TEST-CONFIRMED (re-read) / CODE-ONLY (acceptance gate) |
| `refreshLicense` still puts a staged file in force first, and restores the active one when the staged one is refused | `LicenseReaderTests.cs:106-109`; `Lic:77-88` | TEST-CONFIRMED |
| **Whether a license is still required** | | |
| `[AllowAnonymous]`; `GetEnableTariffSettings` (tariff settings visible, not `TenantAccessSettings.Anyone`, standalone with license path, no AMI) AND Enterprise AND `!File.Exists(LicensePath)` | `Lic:234-240`; `FirstTime:164-169`; `Extra:48-55`; `T/02_Settings/License/LicenseOpenSourceTests.cs:76-88` (anonymous → false without a license path) | TEST-CONFIRMED [run: PASS] (anonymous, false branch), CODE-ONLY (true branch: Enterprise, unreachable) |
| **Trial license of a self-hosted installation** | | |
| `[ApiExplorerSettings(IgnoreApi = true)]`; not standalone → 415; not DocSpace admin → 403; false when quota tenant ≠ −1, already trial, or due date not max; quota `-1000` "apirequest" trial, 30 days; audit | `Lic:156-215`; `T/02_Settings/License/LicensePermissionsTests.cs:134-150` (raw HTTP, RoomAdmin/User/Guest 403) | TEST-CONFIRMED [run: PASS] (admin check), CODE-ONLY (the rest: an admin call writes the installation quota; 415 needs SaaS) |
| **Tariff state of a licensed installation and the payment gate** | | |
| Standalone → tenant −1; `Paid`; `Delay` + `DelayDueDate = DueDate + core:payment:delay` (10) when past and not lifetime/trial; `NotPaid` after the delay; lifetime stays `Paid`; trial → `Trial` | `TariffSvc:100-106, 1160-1238`; `appsettings:19-20`; `common/ASC.Core.Common/Billing/TariffState.cs:39-52`; `T/02_Settings/License/LicenseOpenSourceTests.cs:103-117` (owner: `Paid`) | TEST-CONFIRMED [run: PASS] (`Paid`), CODE-ONLY (delay, not paid, trial: installation tariff dates, unreachable) |
| `PaymentFilter`: 402 unless `[AllowNotPayment]` — no request header opens the gate since `dbb446ada2`; `IsNotPaidAsync` = tariff settings enabled AND state ≥ NotPaid (the Enterprise clause is subsumed) | `PayFilter:37-56`; `Extra:96-106` | CODE-ONLY |
| Grace period blocks adding rooms/users, after it the portal is blocked (v3.7) | release notes v3.7 lines 59-62; the 402 of the count checks is `openspec/specs/quotas/spec.md` "Count limits of the tariff" | NOTES-ONLY (blocking), CODE-ONLY (402) |
| **Payment settings** | | |
| Portal-settings right; `[AllowNotPayment]`; `buyUrl` = site entry `buy<license:type or enterprise>`; `max` = `core:payment:max-quantity` or 999 | `Settings:1360-1385`; `web/ASC.Web.Api/ApiModels/ResponseDto/PaymentSettingsDto.cs:48-110`; `T/02_Settings/License/LicenseOpenSourceTests.cs:133-160` (owner: `buyUrl` of `buyenterprise`, `max` 999, `standalone`; RoomAdmin/User 403) | TEST-CONFIRMED [run: PASS], CODE-ONLY (`AllowNotPayment`) |
| Owner / DocSpace admin 200 with `salesEmail`, `buyUrl`, `currentLicense` non-null, `max` > 0 | `T/02_Settings/Common/PaymentSettingsTests.cs:46-78` | TEST-CONFIRMED [run: PASS] |
| **Features gated by a quota flag of the edition** | | |
| Flags `branding`, `customization`, `sso`, `ldap`, `audit`, `restore`, `thirdparty`, `statistic`, `docs`, `update`, `lifetime` … are `TenantQuotaFeatureFlag`s; `customization` → white label; `branding` → `HideAbout`/`displayAbout`; `sso` → SSO settings 402 (hosted); `thirdparty` → paid auth keys unless standalone; plugins and themes unchecked | `Quota:545-570`; `LogoMgr:305-313`; `WL:682`; `Settings:128, 1269`; `web/ASC.Web.Api/Api/Settings/SsoController.cs:56-90`; `openspec/specs/plugins/spec.md` (no gate); `Settings:904-1004` (no quota check); `T/02_Settings/Rebranding/RebrandingSaveTests.cs:52-112` (`customization` false → white-label saves 402); `T/02_Settings/Common/ColorThemeRulesTests.cs:60-131` (themes saved on the same plan: unchecked) | TEST-CONFIRMED [run: PASS] (`customization`, themes), CODE-ONLY (other flags: need another plan) |
| Self-hosted installation on the default quota (no `customization`): `getEnableWhitelabel` false, not an edition switch | `T/02_Settings/Rebranding/RebrandingReadTests.cs:41-58` (standalone integration host; the remark says the value follows the plan) | TEST-CONFIRMED [run: PASS] |
| v3.0: enterprise license blocks branding, developer allows it, developer_plus hides About, open source has no branding tab | release notes v3.0 lines 16-22; server: the `customization`/`branding` flags of the applied license (`Reader:263-264`) | NOTES-ONLY (tab), CODE-ONLY (flags) |
| **Audit trail of branding and license changes** | | |
| `WhiteLabelSettingsLogosUpdated` (save, savefromfiles, restore), `WhiteLabelSettingsLogoTextUpdated` (save, restore), `WhiteLabelCompanySettingsUpdated` (save, delete), `WhiteLabelAdditionalSettingsUpdated` (save, delete), `ColorThemeChanged` (selection change, delete of the selected), `GreetingSettingsUpdated` (save only), `LicenseKeyUploaded` (accept, trial, wizard) | `WL:139, 227, 425, 478, 590, 688, 747, 788, 846`; `Settings:963, 999`; `Greet:477`; `Lic:117, 212`; `FirstTime:103` | CODE-ONLY |

## Where the server differs from the release notes or the OpenAPI document (spec follows the server)

| Release note / document | Server |
|---|---|
| v3.0.1 notes: the `docspace_dev` field of the license is checked at upload — a developer license is refused on an Enterprise installation and vice versa | The check is commented out (bug 84212: Docs Developer Edition licenses are issued without the flag); any edition's license file is accepted (`Reader:229-237`). The "License Conflict" message paths in `Lic:132-136, 330-335` and `FirstTime:148-152` are unreachable. |
| v3.0 notes: "in open source the branding tab disappears" | The server has no edition switch: `getEnableWhitelabel` follows the `customization` flag of the installation's quota and the `web:hide-settings` list (`LogoMgr:310-313`); hiding the tab is the client's decision. |
| v3.7 notes: "after the grace period the portal is blocked" | The server answers 402 to every operation without `[AllowNotPayment]` while the tariff is `NotPaid` (`PayFilter`); the tenant status is not changed by the lapse. |
| yaml `uploadLicense` 415: "The installation has no license path configured" | `Lic:277-280` throws `NotSupportedException` when the installation is not Enterprise, which on a self-hosted installation is exactly "no license path" but on a hosted portal as well; an empty path reached inside the reader would be `BillingNotFoundException` → generic → 500 (`Reader:178-181`, `Lic:336-340`). Agrees for the hosted case too. |
| yaml `saveWhiteLabelSettings` 403: "the installation does not allow default branding to be edited"; yaml `refreshLicense` 402 "it was issued for the other edition" | The edition-type path is dead (above); the 403 text is right. |
| yaml `getWhiteLabelLogoText`: "without `isDefault` the portal's own value is returned even when the installation carries a different default" | Agrees (`WL:512`); the spec records it as a requirement because letters and `getSettings.logoText` use the installation default (`LogoMgr:273-282`). |
| yaml `WhiteLabelRequestDto.logoText` on `saveWhiteLabelSettings`: validated, not stored | Agrees (`WL:77`, the DTO attribute fires for both save operations). |
| TS comment `rebranding.spec.ts:4-15`: two distinct gates | Agrees with `WL:952-965` and `LogoMgr:315-321`. |
| .NET test names `GetPortalTariff_Member_ReturnsLimitedTariff`, `DeletePortalColorTheme_DocSpaceAdmin_ThemeIsRemoved` | Assertions match the names. |
| .NET test name `GetAdditionalWhiteLabelSettings_Owner_ReturnsAllResourcesEnabled` | Since `bbba22fbf1` the assertion covers five of the six flags and not `isDefault`; the name overstates it. |

## Left out of the spec, and why

- **The mail white label settings** (`settings/rebranding/mail`, `WL:853-924`): hidden from the contract with
  `[ApiExplorerSettings(IgnoreApi = true)]`, so no SDK or agent reaches them; they gate the letter footer and are the
  `notifications` specification's concern.
- **The logo redirect handler** (`/logo.ashx`, `web/ASC.Web.Core/LogoHandler.cs`, `nginx:706`): a redirect to the same
  URLs `getWhiteLabelLogos` returns, with `logotype`, `default`, `dark`, `culture` query values; implementation detail of
  the client.
- **Culture-specific built-in logos** (`web:logo:custom-cultures`, `TWLS:870-890`, `LogoMgr:431-443`, v2.6 notes):
  configuration of the installation, empty in the shipped config.
- **Portal culture, time zone and the setup wizard**: `getSettings`/`saveSettings` and `completeWizard` are general
  portal settings, not appearance; only the wizard's license step is referenced.
- **The hosted plan catalogue, payments, wallet, `getPaymentQuotas`, `getQuotaPaymentInformation` feature grading**: the
  `billing` and `quotas` specifications.
- **The grading of `getPortalTariff`/`getPortalQuota` by role and the count/storage limits**: `quotas`.
- **The banner setting of Enterprise installations** (v3.2.1 notes, `TenantBannerSettings`, `Settings:160`): a settings
  toggle, not branding.
- **The client-side rebranding through translations and the v4.0 product rename** (v2.6, v4.0 notes): client texts.
- **`LicenseExpiredException` / `LicenseQuotaException` / `LicensePortalException` catch blocks** (`Lic:315-329`): nothing
  in the reader throws them; the messages "The license is not valid for this portal version" etc. are unreachable.
- **The rate-limit rows** of the yaml: generic policy. (The `Payment-Info` header no longer exists in the gate.)

## Suspected defects

Re-checked 2026-10-24 against the hotfix v4.0.1 commits (`183da52625..HEAD`): none of the files behind L1–L12
(`WL`, `TWLS`, `Lic`, `Reader`, `Settings`, `PaymentSettingsDto`, the greeting tests) changed in that range, so all
twelve are **STILL OPEN**. The range touched only `common/ASC.Core.Common/Billing` (accounting service limits —
the `billing` specification) and added the `02_Settings/Rebranding` tests, which confirm the two gates but exercise
none of the defective paths (no logo is ever saved; the company/additional deletes are not ported).

Re-checked 2026-11-04 against `d58816bece..3533ad8a89`: still none of the files behind L1–L12 changed (the only
branding file in the range, `TenantLogoManager.cs`, touched the letter-logo fetch, not the defective paths), the
`PaymentSettingsDto` still never fills `feedbackAndSupportUrl` (L7), and the regenerated yaml (`5ea4dc6`) still has no
`settings/license/trial` path (L12). All twelve are **STILL OPEN**; the new tests of the range exercise none of them.

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| L1 | Saving logos from image data | The per-slot loop `return`s instead of `continue`s when an entry carries neither a light nor a dark image, so every entry after an empty one is silently skipped while the call still answers `true`. | `TWLS:682-685` |
| L2 | Logo slots | The light extension of the embedded spreadsheet-editor slot is read from the normal spreadsheet slot, and that of the embedded PDF slot from the normal PDF slot; when the two were uploaded with different formats the embedded slot's URL names a file that does not exist and the built-in image is served. | `TWLS:398, 402` |
| L3 | Restoring the built-in logos | The restore clears the extension and the default flag of only the seven original slots; the eight editor slots added in v3.3 (spreadsheet, presentation, PDF, diagram, normal and embedded) keep `default` false and their extension after the files were deleted, so `getIsDefaultWhiteLabelLogos` reports them as customised and the next save deletes a file that is not there (logged, not failed). | `TWLS:587-614` vs `TWLS:289-307` |
| L4 | Saving logos from uploaded files | A dark file for a slot without a dark variant stores nothing but still marks the slot as customised; a file whose name does not start with the slot number (`logo.png`) fails `Convert.ToInt32` → 500 instead of 400. | `TWLS:762-778`; `WL:230-235` |
| L5 | Saving logos from image data | A staged file name is resolved with `Path.GetFileName` and deleted from the temp store after use; a `data:` URI with a format other than the three supported ones is treated as a file name, so the call fails with the temp-store error (500), not 400. | `TWLS:733-748` |
| L6 | Adding a colour theme | A new theme beyond `core:themelimit` is dropped and the call answers 200 with the unchanged list; the client cannot tell the refusal from a success. The limit counts the six built-in themes, so `themelimit` 9 allows three custom themes. | `Settings:943-955`; red test `T/02_Settings/Common/ColorThemeRulesTests.cs:133-149` |
| L7 | Payment settings | `feedbackAndSupportUrl` is declared in the response but never filled: it is always null. | `web/ASC.Web.Api/ApiModels/ResponseDto/PaymentSettingsDto.cs:62`; `Settings:1377-1384` |
| L8 | Activating and refreshing the license | `acceptLicense` and `refreshLicense` demand no right at all: any signed-in member of any portal of the installation (a Guest included) can move a staged license file into place, rewrite the installation's quota and tariff and write the "license key uploaded" audit event under their name, or force a re-read; only `uploadLicense` is limited to administrators. | `Lic:73-88, 105-147`; red tests `T/02_Settings/License/LicensePermissionsTests.cs:72-109` |
| L9 | Uploading a license file | A license that is unparsable, lacks a customer id or a signature, or cannot be written is reported as 500 "License is not correct" (generic exception) rather than a client error, while a future start date is 400; the same file is accepted by the upload and refused only on accept when the editing service disagrees. | `Lic:336-340`; `Reader:217-227` |
| L10 | Changing the greeting title | On an installation whose base domain is `localhost` with more than one portal, saving the title fails with 500: saving the tenant rewrites the alias to `localhost` and hits the unique alias index. The .NET save/restore tests were commented out for this reason. | `T/02_Settings/Greeting/GreetingSaveTests.cs:45-47`; `T/02_Settings/Greeting/GreetingRestoreTests.cs:45-47` |
| L11 | Company details | The licensor flag of the request is accepted for compatibility and always stored as `false`, so an installation that is itself the vendor cannot restore `isLicensor` true through the API after a change except by `deleteCompanyWhiteLabelSettings`. | `WL:683`; `api-models` rule §2 |
| L12 | Contract | `POST settings/license/trial` is published by the controller but hidden from the OpenAPI document and the SDKs; agents and generated clients cannot start a trial. | `Lic:156-161` |
| L13 | Refreshing the license | The Files worker's periodic re-read put an uploaded, not yet accepted license in force, bypassing `acceptLicense`. FIXED (2c268c3041): the worker re-reads only the active file and only after acceptance (`LicenseReaderTests.cs:86-94`). | `RefreshSvc`; `LicenseReader` |
