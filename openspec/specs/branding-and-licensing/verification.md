# branding-and-licensing — verification

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
at 1039-1117), `Extra` is `web/ASC.Web.Core/Utility/TenantExtra.cs`, `TariffSettings` is `web/ASC.Web.Core/TariffSettings.cs`,
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
notes say it — nothing of that kind was written into the spec). Exception → status mapping (`Wrapper:53-114`):
`ArgumentException` (incl. `ArgumentNullException`, `ArgumentOutOfRangeException`) 400, `SecurityException` 403
"Access denied", `InvalidOperationException` 403, `BillingException` and `TenantQuotaException` 402,
`NotSupportedException` 415, anything else 500.

The .NET suite covers the greeting reads and permissions, the colour themes, the portal logo, the payment settings
and the tariff grading; the white-label, rebranding and licence operations have no .NET test (the TS suite covers
rebranding reads, permissions and the two gates; the TS licence file states that licence management cannot be
automated on the hosted test environment, `TS/license/license.spec.ts:1-10`). The greeting save/restore tests exist
but are commented out (see L10).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Editions of the installation** | | |
| Hosted = base domain configured; self-hosted = base domain `localhost`; Enterprise = self-hosted + license path; Developer = Enterprise + license type `developer`; open source = self-hosted without license path | `Core:48-63`; `TariffSvc:36-46`; `Reader:43-57` (`license:file:path`, `license:type`); `appsettings:13, 74-78`; `../buildtools/config/appsettings.enterprise.json:7-8`, `appsettings.developer.json:7-8` | CODE-ONLY |
| `getSettings.standalone`; `getPortalTariff` fills `openSource`/`enterprise`/`developer` only for the portal-settings right | `Settings:119`; `Portal:593-623`; yaml `TariffDto` (100715-100750) | CODE-ONLY |
| Members without the right get `id` 0, `state` Paid, 200 | `T/04_Portal/Quota/PortalTariffPermissionsTests.cs:61-84`; owner/admin full tariff `T/04_Portal/Quota/PortalTariffTests.cs:53-93` (edition flags not asserted) | TEST-CONFIRMED (grading), CODE-ONLY (flags) |
| **Whether branding may be changed** | | |
| Gate = branding section not in `web:hide-settings` AND quota `customization`; `getEnableWhitelabel` needs the portal-settings right | `LogoMgr:51-58, 286-294`; `WL:945-950`; `appsettings:181-189` (WhiteLabel not listed → enabled); yaml `getEnableWhitelabel` (61535-61615) | CODE-ONLY |
| Saves answer 402 "Your pricing plan does not support this option" after the permission check | `LogoMgr:296-302` (`BillingException`), `WL:83-96, 164-182, 455-470`; `Resource:861-863` | CODE-ONLY |
| Owner: `getEnableWhitelabel` false, `saveWhiteLabelLogoText` 402, `saveWhiteLabelSettings` 402; User 403 before the plan; restores 200 true | `TS/rebranding/rebranding.spec.ts:17-28, 151-203`; `TS/rebranding/rebranding.permissions.spec.ts:79-103, 148-176` | TEST-CONFIRMED (TS) |
| **Logo slots and their public listing** | | |
| Sixteen slots; listing skips `Notification`; `[AllowAnonymous, AllowNotPayment, AllowSuspended, AllowBlocked]`; `isDark` fills one theme; `dark` null when equal to `light` | `LogoType:44-93`; `WL:253-322`; yaml `getWhiteLabelLogos` (60232-60327), `WhiteLabelItemDto` (104227-104260) | CODE-ONLY |
| Anonymous gets 15 entries, none `Notification` | `TS/rebranding/rebranding.spec.ts:92-106`; `TS/rebranding/rebranding.permissions.spec.ts:68-77` | TEST-CONFIRMED (TS) |
| Sizes 422x48, 772x88, 32x32, 172x40 (all editors), 56x56, 442x48, letter 386x44 | `TWLS:167-206` | CODE-ONLY |
| **Saving logos from image data** | | |
| Portal-settings right, gate, `logo` key/value list, `key` parsed as int, light/dark data URI (`image/jpeg`, `image/png`, `image/svg+xml`) or temp file name; slot omitted keeps image; dark only for slots that can be dark; login-page save rebuilds `Notification`; `[StringLength(40)]` on `logoText`, not stored; audit; letter-logo cache dropped | `WL:81-140`; `TWLS:671-760, 975-992` (`CanBeDark`); `web/ASC.Web.Api/ApiModels/RequestsDto/WhiteLabelRequestsDto.cs:45-84`; `LogoMgr:352-362`; yaml `saveWhiteLabelSettings` (60012-60124) | CODE-ONLY |
| Dark image ignored for favicon/editor slots, slot still marked customised | `TWLS:702-708` | CODE-ONLY |
| **Saving logos from uploaded files** | | |
| No file → `InvalidOperationException("No input files")` → 403 before the gate; name `<slot>.<ext>`, `dark` in the name → dark variant | `WL:162-235`; `TWLS:762-778`; `TS/rebranding/rebranding.spec.ts:205-215` (403 asserted) | TEST-CONFIRMED (TS, no-file), CODE-ONLY (rest) |
| Staging upload `/logoUploader.ashx`: portal-settings right + gate, `logotype`/`width`/`height` form fields, extensions `.bmp .jpg .jpeg .ico .png .svg`, size ≤ slot size, returns the temp file name | `Uploader:384-473`; `TWLS:40`; `nginx:702`; the temp name is consumed by `TWLS:735-748` | CODE-ONLY |
| **Which slots hold a custom image** | | |
| Portal-settings right; 16 entries incl. `Notification` (mapped to the login-page flag); `isDefault` query reads tenant −1 | `WL:344-362`; `TWLS:309-331` | CODE-ONLY |
| Fresh portal: 16 entries, all `default` true; User 403 | `TS/rebranding/rebranding.spec.ts:108-123`; `TS/rebranding/rebranding.permissions.spec.ts:105-117` | TEST-CONFIRMED (TS) |
| **Restoring the built-in logos** | | |
| Portal-settings right, no gate; deletes `*` in the `whitelabel` storage module; resets the legacy logo (`InfoSettings`); cache dropped; audit; 200 true | `WL:383-426`; `TWLS:585-630`; `InfoSettings:168-183`; `storage.json:92-98` | CODE-ONLY |
| Owner restores with nothing stored → 200 true | `TS/rebranding/rebranding.spec.ts:164-175` | TEST-CONFIRMED (TS) |
| Only 7 of the 15 slot flags are reset by the restore (see L3) | `TWLS:587-614` | — |
| **Saving and clearing the logo text** | | |
| Gate; `[StringLength(40)]` → 400; empty/blank/`ONLYOFFICE` stored as null; restore without gate; audit `WhiteLabelSettingsLogoTextUpdated` | `WL:453-481, 569-593`; `TWLS:134-150, 632-637`; `common/ASC.Core.Common/WhiteLabel/BaseWhiteLabelSettings.cs` (`DefaultLogoText`) | CODE-ONLY |
| Owner restores the wordmark with nothing stored → 200 true | `TS/rebranding/rebranding.spec.ts:151-162` | TEST-CONFIRMED (TS) |
| **Reading the logo text** | | |
| `getWhiteLabelLogoText` returns `LogoText ?? "ONLYOFFICE"` — no fallback to tenant −1 unless `isDefault=true`; `isdefault` = stored text empty; `getSettings.logoText` uses the three-level fallback | `WL:504-547`; `TWLS:136-145`; `LogoMgr:254-263`; `Settings:134` | CODE-ONLY |
| Owner reads "ONLYOFFICE" and `{logotext, default:true}`; User 403 on both | `TS/rebranding/rebranding.spec.ts:125-149`; `TS/rebranding/rebranding.permissions.spec.ts:119-146` | TEST-CONFIRMED (TS) |
| **Installation-wide default branding** | | |
| `isDefault=true` → `DemandRebrandingPermissionAsync`: standalone AND not `LimitedAccessSpace` (403), not `core:custom-mode` (403), gate for saves only; stored for tenant −1 in `static_partnerdata`; portals inherit per slot and for the text | `WL:85-90, 171-176, 387-392, 459-464, 575-580, 952-965`; `Extra:108-114`; `Core:63`; `TWLS:143-145, 817-823, 849-868`; `storage.json:101-107` | CODE-ONLY |
| **Company details of the installation** | | |
| Any signed-in member; `[AllowNotPayment]`; 304; `isDefault` via field comparison; `getLicensorData` appends the built-in when replaced and not licensor | `WL:617-631, 712-717`; `Company:537-577`; `web/ASC.Web.Api/ApiModels/ResponseDto/CompanyWhiteLabelSettingsDto.cs:39-111` | CODE-ONLY |
| Built-in vendor "Ascensio System SIA", `isLicensor` true, `isDefault` true; single-item licensor list; User 200, anonymous 401 | `TS/rebranding/rebranding.spec.ts:30-48, 73-90`; `TS/rebranding/rebranding.permissions.spec.ts:4-25, 51-66` | TEST-CONFIRMED (TS) |
| **Changing and resetting the company details** | | |
| Portal-settings right, installation check (403), gate (402); `settings`/`email`/`site` null → `ArgumentNullException` 400, puny-code → 400, DTO `[Url]`/`[EmailAddress]`/`[Phone]`/`[StringLength(255)]`; `IsLicensor` false; `HideAbout = quota.Branding && input`; saved for tenant −1; delete without gate returns defaults | `WL:653-691, 737-750`; `web/ASC.Web.Api/ApiModels/RequestsDto/WhiteLabelSettingsRequestDto.cs:39-109`; yaml `saveCompanyWhiteLabelSettings` (61038-61106) | CODE-ONLY |
| Hosted owner: save 403, delete 403 | `TS/rebranding/rebranding.spec.ts:217-237, 262-272` | TEST-CONFIRMED (TS) |
| **Help and community links of the installation** | | |
| Flags + `isDefault`; defaults true when the regional address exists; save replaces the set (`RequiredMappingStrategy.Source` mapper, bools default false); delete returns defaults | `WL:776-849`; `Additional:617-687`; `web/ASC.Web.Api/ApiModels/ResponseDto/AdditionalWhiteLabelSettingsDto.cs:39-114`; `web/ASC.Web.Api/ApiModels/RequestsDto/WhiteLabelSettingsRequestDto.cs:114-176` | CODE-ONLY |
| All six flags true and `isDefault` true on the test installation; hosted owner save/delete 403; User 200, anonymous 401 | `TS/rebranding/rebranding.spec.ts:50-71, 239-260, 274-284`; `TS/rebranding/rebranding.permissions.spec.ts:27-49` | TEST-CONFIRMED (TS) |
| **Where the branding appears and how it is served** | | |
| Editor logo by file type and embed mode; letter logo = uploaded `Notification` → installation default → embedded `logo.png`; letter logo cached one day per culture; `displayAbout` formula | `LogoMgr:214-251, 307-350, 397-410`; `LogoType:95-108`; `Settings:128, 134`; `openspec/specs/notifications/spec.md:594-611` | CODE-ONLY |
| Logo URLs with `?hash`, built-in with `?hash=<version:number>`; proxy `immutable, max-age=31536000` on `storage/whitelabel/root/*?hash*` and `static_partnerdata`; `whitelabel` module served as attachment (v2.6 notes) | `TWLS:805-847`; `nginx:61`; `storage.json:92-98`; release notes v2.6 (technical) line 33 | CODE-ONLY |
| **Reading the colour themes** | | |
| `[AllowAnonymous, AllowNotPayment, AllowSuspended, AllowBlocked]`; 304; six defaults, `Selected` = min id; `limit` from `core:themelimit` (9 in the dev config, 0 = unlimited) | `Settings:875-886`; `Themes:250-354`; `appsettings:59` | CODE-ONLY |
| Owner and every member role get 200, non-empty `themes`, `selected` ≥ 0, `limit` ≥ 0 | `T/02_Settings/Common/ColorThemeGetTests.cs:45-79` | TEST-CONFIRMED |
| **Adding, changing and selecting a colour theme** | | |
| Portal-settings right; built-in id → new theme; existing custom id → colours replaced; add only while `Themes.Count < Limit` (built-ins counted) or `Limit == 0`; id = max+1; `Name` emptied; `selected` only when it exists; audit only on selection | `Settings:904-966`; `web/ASC.Web.Api/ApiModels/RequestsDto/CustomColorThemesSettingsRequestsDto.cs:39-122` | CODE-ONLY |
| Owner adds a theme → 200, themes non-empty, new theme has the max id; DocSpace admin adds → 200 | `T/02_Settings/Common/ColorThemeSaveTests.cs:45-86`; `T/02_Settings/Common/ColorThemeDeleteTests.cs:51-59` (max id read back) | TEST-CONFIRMED |
| Limit reached → silently dropped (see L6) | `Settings:943-955` | CODE-ONLY |
| **Deleting a colour theme** | | |
| Portal-settings right; built-in id untouched; selected falls back to min id with audit | `Settings:982-1004` | CODE-ONLY |
| Owner deletes a custom theme: count −1, id gone; DocSpace admin deletes the owner's theme | `T/02_Settings/Common/ColorThemeDeleteTests.cs:45-94` | TEST-CONFIRMED |
| **Greeting title of the portal** | | |
| `tenant.Name == "" ? PortalName : Name`; `isdefault` = name empty; `getSettings.greetingSettings` same text; caption "Cloud space for your office docs" | `Greet:404-431`; `Settings:117`; `Resource:1146-1148` | CODE-ONLY |
| Every role 200 non-empty; owner/admin `isdefault` true on a fresh portal; anonymous 401 on both | `T/02_Settings/Greeting/GreetingGetTests.cs:7-29`; `GreetingIsDefaultTests.cs:7-23`; `GreetingReadPermissionsTests.cs:7-22` | TEST-CONFIRMED |
| **Changing and restoring the greeting title** | | |
| Portal-settings right; `[StringLength(255)] required Title`; hosted free/trial → `ValidateTenantName` (regex `core:tenantname:regex`, hosted only) → 400; audit `GreetingSettingsUpdated`; restore sets `web:portal-name` or "" | `Greet:452-509`; `web/ASC.Web.Api/ApiModels/RequestsDto/GreetingSettingsRequestsDto.cs:48-49`; `common/ASC.Core.Common/Tenants/TenantDomainValidator.cs:95-125`; `InfoSettings:161-166`; `Resource:1200-1202` | CODE-ONLY |
| Room admin / User / Guest 403 "Access denied", anonymous 401 on save and restore | `T/02_Settings/Greeting/GreetingSavePermissionsTests.cs:7-29`; `GreetingRestorePermissionsTests.cs:7-27` | TEST-CONFIRMED |
| Owner sets and restores a title | `T/02_Settings/Greeting/GreetingSaveTests.cs:44-72` and `GreetingRestoreTests.cs:44-72` — written but commented out (L10); `TS/greeting/greeting.spec.ts` | CODE-ONLY (.NET), TEST-CONFIRMED (TS) |
| **Legacy portal logo** | | |
| Any signed-in member; `notifications/logo.png` when default; 304; reset by the logo restore | `Settings:802-806`; `InfoSettings:213-222`; `WL:408-410` | CODE-ONLY |
| Every role 200 with a value | `T/02_Settings/Common/PortalLogoTests.cs:45-76` | TEST-CONFIRMED |
| **Uploading a license file** | | |
| `[Authorize(AuthenticationSchemes = "confirm", Roles = "Wizard, Administrators")]`; 403 when unauthenticated and wizard completed; not Enterprise → `NotSupportedException` 415; no file → `ArgumentException` 400; staged to `<path>.tmp`; customer id/signature missing → `BillingNotConfiguredException` → generic → 500 "License is not correct"; future start → `ArgumentOutOfRangeException` 400; overdue message with `Update` flag variant | `Lic:264-341`; `Reader:176-240`; `License:419-451`; `Resource:834-836, 1107-1127` | CODE-ONLY |
| Edition check (`docspace_dev` vs `license:type`) is disabled since bug 84212 | `Reader:229-237` (commented out) | CODE-ONLY |
| **Activating a license** | | |
| No edition → "Your pricing plan does not support this option" with 200; records `LicenseAccept` for tenant −1; audit `LicenseKeyUploaded`; refresh: backup → move temp → parse → editing-service validation → quota/tariff; failure restores the backup; messages per exception | `Lic:105-147`; `TariffSettings:201-214`; `Reader:108-174`; `DocsLic:83-140`; `UCResource:120-131` | CODE-ONLY |
| Wizard completion refreshes the license and records acceptance | `FirstTime:98-104, 139-156`; `Settings` wizard action (`CompleteWizard`) | CODE-ONLY |
| No permission check on `acceptLicense` and `refreshLicense` (L8) | `Lic:73-88, 105-147` | — |
| **What the license grants** | | |
| Quota `-1000` "license": `Audit, Ldap, Sso, ThirdParty, Restore, Oauth, ContentSearch, DocsEdition, Statistic` true, `Branding`/`Customization`/`AutomationApi`/`Trial` from the file, `Lifetime = !Trial && !TimeLimited`, `MaxFileSize` from the default quota; tariff `DueDate = license.DueDate`; one tariff for all portals | `Reader:242-279`; `License:332-417`; `TariffSvc:85-91` | CODE-ONLY |
| **Refreshing the license** | | |
| Not Enterprise → false; `BillingNotFoundException`/`BillingNotConfiguredException` → 402, future start → 400; periodic re-read in the Files worker, period `files:refreshLicense:period` (default "1" = one day) | `Lic:77-88`; `RefreshSvc:36-61`; release notes v3.0.1 line 15 | CODE-ONLY |
| **Whether a license is still required** | | |
| `[AllowAnonymous]`; `GetEnableTariffSettings` (tariff settings visible, not `TenantAccessSettings.Anyone`, standalone with license path, no AMI) AND Enterprise AND `!File.Exists(LicensePath)` | `Lic:234-240`; `FirstTime:164-169`; `Extra:48-55` | CODE-ONLY |
| **Trial license of a self-hosted installation** | | |
| `[ApiExplorerSettings(IgnoreApi = true)]`; not standalone → 415; not DocSpace admin → 403; false when quota tenant ≠ −1, already trial, or due date not max; quota `-1000` "apirequest" trial, 30 days; audit | `Lic:156-215` | CODE-ONLY |
| **Tariff state of a licensed installation and the payment gate** | | |
| Standalone → tenant −1; `Paid`; `Delay` + `DelayDueDate = DueDate + core:payment:delay` (10) when past and not lifetime/trial; `NotPaid` after the delay; lifetime stays `Paid`; trial → `Trial` | `TariffSvc:85-91, 1039-1117`; `appsettings:19-20`; `common/ASC.Core.Common/Billing/TariffState.cs:39-52` | CODE-ONLY |
| `PaymentFilter`: 402 unless `[AllowNotPayment]` or `Payment-Info: false`; `IsNotPaidAsync` = tariff settings enabled AND state ≥ NotPaid (the Enterprise clause is subsumed) | `PayFilter:37-60`; `Extra:96-106` | CODE-ONLY |
| Grace period blocks adding rooms/users, after it the portal is blocked (v3.7) | release notes v3.7 lines 59-62; the 402 of the count checks is `openspec/specs/quotas/spec.md` "Count limits of the tariff" | NOTES-ONLY (blocking), CODE-ONLY (402) |
| **Payment settings** | | |
| Portal-settings right; `[AllowNotPayment]`; `buyUrl` = site entry `buy<license:type or enterprise>`; `max` = `core:payment:max-quantity` or 999 | `Settings:1360-1385`; `web/ASC.Web.Api/ApiModels/ResponseDto/PaymentSettingsDto.cs:48-110` | CODE-ONLY |
| Owner / DocSpace admin 200 with `salesEmail`, `buyUrl`, `currentLicense` non-null, `max` > 0 | `T/02_Settings/Common/PaymentSettingsTests.cs:46-78` | TEST-CONFIRMED |
| **Features gated by a quota flag of the edition** | | |
| Flags `branding`, `customization`, `sso`, `ldap`, `audit`, `restore`, `thirdparty`, `statistic`, `docs`, `update`, `lifetime` … are `TenantQuotaFeatureFlag`s; `customization` → white label; `branding` → `HideAbout`/`displayAbout`; `sso` → SSO settings 402 (hosted); `thirdparty` → paid auth keys unless standalone; plugins and themes unchecked | `Quota:546-565`; `LogoMgr:286-294`; `WL:682`; `Settings:128, 1269`; `web/ASC.Web.Api/Api/Settings/SsoController.cs:56-90`; `openspec/specs/plugins/spec.md` (no gate); `Settings:904-1004` (no quota check) | CODE-ONLY |
| v3.0: enterprise license blocks branding, developer allows it, developer_plus hides About, open source has no branding tab | release notes v3.0 lines 16-22; server: the `customization`/`branding` flags of the applied license (`Reader:263-264`) | NOTES-ONLY (tab), CODE-ONLY (flags) |
| **Audit trail of branding and license changes** | | |
| `WhiteLabelSettingsLogosUpdated` (save, savefromfiles, restore), `WhiteLabelSettingsLogoTextUpdated` (save, restore), `WhiteLabelCompanySettingsUpdated` (save, delete), `WhiteLabelAdditionalSettingsUpdated` (save, delete), `ColorThemeChanged` (selection change, delete of the selected), `GreetingSettingsUpdated` (save only), `LicenseKeyUploaded` (accept, trial, wizard) | `WL:139, 227, 425, 478, 590, 688, 747, 788, 846`; `Settings:963, 999`; `Greet:477`; `Lic:117, 212`; `FirstTime:103` | CODE-ONLY |

## Where the server differs from the release notes or the OpenAPI document (spec follows the server)

| Release note / document | Server |
|---|---|
| v3.0.1 notes: the `docspace_dev` field of the license is checked at upload — a developer license is refused on an Enterprise installation and vice versa | The check is commented out (bug 84212: Docs Developer Edition licenses are issued without the flag); any edition's license file is accepted (`Reader:229-237`). The "License Conflict" message paths in `Lic:132-136, 330-335` and `FirstTime:148-152` are unreachable. |
| v3.0 notes: "in open source the branding tab disappears" | The server has no edition switch: `getEnableWhitelabel` follows the `customization` flag of the installation's quota and the `web:hide-settings` list (`LogoMgr:291-294`); hiding the tab is the client's decision. |
| v3.7 notes: "after the grace period the portal is blocked" | The server answers 402 to every operation without `[AllowNotPayment]` while the tariff is `NotPaid` (`PayFilter`); the tenant status is not changed by the lapse. |
| yaml `uploadLicense` 415: "The installation has no license path configured" | `Lic:277-280` throws `NotSupportedException` when the installation is not Enterprise, which on a self-hosted installation is exactly "no license path" but on a hosted portal as well; an empty path reached inside the reader would be `BillingNotFoundException` → generic → 500 (`Reader:178-181`, `Lic:336-340`). Agrees for the hosted case too. |
| yaml `saveWhiteLabelSettings` 403: "the installation does not allow default branding to be edited"; yaml `refreshLicense` 402 "it was issued for the other edition" | The edition-type path is dead (above); the 403 text is right. |
| yaml `getWhiteLabelLogoText`: "without `isDefault` the portal's own value is returned even when the installation carries a different default" | Agrees (`WL:512`); the spec records it as a requirement because letters and `getSettings.logoText` use the installation default (`LogoMgr:254-263`). |
| yaml `WhiteLabelRequestDto.logoText` on `saveWhiteLabelSettings`: validated, not stored | Agrees (`WL:77`, the DTO attribute fires for both save operations). |
| TS comment `rebranding.spec.ts:4-15`: two distinct gates | Agrees with `WL:952-965` and `LogoMgr:296-302`. |
| .NET test names `GetPortalTariff_Member_ReturnsLimitedTariff`, `DeletePortalColorTheme_DocSpaceAdmin_ThemeIsRemoved` | Assertions match the names. |

## Left out of the spec, and why

- **The mail white label settings** (`settings/rebranding/mail`, `WL:853-924`): hidden from the contract with
  `[ApiExplorerSettings(IgnoreApi = true)]`, so no SDK or agent reaches them; they gate the letter footer and are the
  `notifications` specification's concern.
- **The logo redirect handler** (`/logo.ashx`, `web/ASC.Web.Core/LogoHandler.cs`, `nginx:706`): a redirect to the same
  URLs `getWhiteLabelLogos` returns, with `logotype`, `default`, `dark`, `culture` query values; implementation detail of
  the client.
- **Culture-specific built-in logos** (`web:logo:custom-cultures`, `TWLS:870-890`, `LogoMgr:412-424`, v2.6 notes):
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
- **The `Payment-Info` header and the rate-limit rows** of the yaml: generic policy.

## Suspected defects

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| L1 | Saving logos from image data | The per-slot loop `return`s instead of `continue`s when an entry carries neither a light nor a dark image, so every entry after an empty one is silently skipped while the call still answers `true`. | `TWLS:682-685` |
| L2 | Logo slots | The light extension of the embedded spreadsheet-editor slot is read from the normal spreadsheet slot, and that of the embedded PDF slot from the normal PDF slot; when the two were uploaded with different formats the embedded slot's URL names a file that does not exist and the built-in image is served. | `TWLS:398, 402` |
| L3 | Restoring the built-in logos | The restore clears the extension and the default flag of only the seven original slots; the eight editor slots added in v3.3 (spreadsheet, presentation, PDF, diagram, normal and embedded) keep `default` false and their extension after the files were deleted, so `getIsDefaultWhiteLabelLogos` reports them as customised and the next save deletes a file that is not there (logged, not failed). | `TWLS:587-614` vs `TWLS:289-307` |
| L4 | Saving logos from uploaded files | A dark file for a slot without a dark variant stores nothing but still marks the slot as customised; a file whose name does not start with the slot number (`logo.png`) fails `Convert.ToInt32` → 500 instead of 400. | `TWLS:762-778`; `WL:230-235` |
| L5 | Saving logos from image data | A staged file name is resolved with `Path.GetFileName` and deleted from the temp store after use; a `data:` URI with a format other than the three supported ones is treated as a file name, so the call fails with the temp-store error (500), not 400. | `TWLS:733-748` |
| L6 | Adding a colour theme | A new theme beyond `core:themelimit` is dropped and the call answers 200 with the unchanged list; the client cannot tell the refusal from a success. The limit counts the six built-in themes, so `themelimit` 9 allows three custom themes. | `Settings:943-955` |
| L7 | Payment settings | `feedbackAndSupportUrl` is declared in the response but never filled: it is always null. | `web/ASC.Web.Api/ApiModels/ResponseDto/PaymentSettingsDto.cs:62`; `Settings:1377-1384` |
| L8 | Activating and refreshing the license | `acceptLicense` and `refreshLicense` demand no right at all: any signed-in member of any portal of the installation (a Guest included) can move a staged license file into place, rewrite the installation's quota and tariff and write the "license key uploaded" audit event under their name, or force a re-read; only `uploadLicense` is limited to administrators. | `Lic:73-88, 105-147` |
| L9 | Uploading a license file | A license that is unparsable, lacks a customer id or a signature, or cannot be written is reported as 500 "License is not correct" (generic exception) rather than a client error, while a future start date is 400; the same file is accepted by the upload and refused only on accept when the editing service disagrees. | `Lic:336-340`; `Reader:217-227` |
| L10 | Changing the greeting title | On an installation whose base domain is `localhost` with more than one portal, saving the title fails with 500: saving the tenant rewrites the alias to `localhost` and hits the unique alias index. The .NET save/restore tests were commented out for this reason. | `T/02_Settings/Greeting/GreetingSaveTests.cs:45-47`; `T/02_Settings/Greeting/GreetingRestoreTests.cs:45-47` |
| L11 | Company details | The licensor flag of the request is accepted for compatibility and always stored as `false`, so an installation that is itself the vendor cannot restore `isLicensor` true through the API after a change except by `deleteCompanyWhiteLabelSettings`. | `WL:683`; `api-models` rule §2 |
| L12 | Contract | `POST settings/license/trial` is published by the controller but hidden from the OpenAPI document and the SDKs; agents and generated clients cannot start a trial. | `Lic:156-161` |
