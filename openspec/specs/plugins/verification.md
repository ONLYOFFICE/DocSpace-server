# plugins — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`; `Manager` is
`web/ASC.Web.Core/WebPluginManager.cs`, `Controller` is `web/ASC.Web.Api/Api/Settings/WebPluginsController.cs`,
`ConfigSettings` is `web/ASC.Web.Core/WebPluginSettings.cs` (`WebPluginConfigSettings`, `WebPluginSettings`,
`WebPluginState`), `RequestDto` is `web/ASC.Web.Api/ApiModels/RequestsDto/WebPluginRequestsDto.cs`,
`WebPluginDto` is `web/ASC.Web.Api/ApiModels/ResponseDto/WebPluginDto.cs`, `PluginsDto` is
`web/ASC.Web.Api/ApiModels/ResponseDto/PluginsDto.cs`, `appsettings` is `../buildtools/config/appsettings.json`
(section `plugins`: `enabled`, `upload`, `delete`, `extension`, `maxCount`, `maxSize`, `assetExtensions`,
`assetMaxCount`), `storage.json` is `../buildtools/config/storage.json` (modules `webplugins`, `systemwebplugins`),
`nginx` is `../buildtools/config/nginx/onlyoffice.conf`. Tests: `Tests/` is `web/ASC.Web.Api.Tests/Tests/02_Settings/Webplugins/`,
`TS` is `../tests/api-tests/src/tests/settings/webplugins/webplugins.spec.ts`. Status: **TEST-CONFIRMED** (an
integration test asserts it), **CODE-ONLY** (read from the product code, the configuration or the OpenAPI
document, no test), **NOTES-ONLY** (only the release notes say it — nothing of that kind was written into the
spec). Exception → status mapping: `SecurityException` 403, `InvalidOperationException` 403, `ArgumentException`
400, `ItemNotFoundException` 404 (`common/ASC.Api.Core/Middleware/ResponseWrapper.cs:55-88`); messages are the
values of the `ErrorWebPlugin*` keys of `web/ASC.Web.Core/PublicResources/Resource.resx:924-953`.

The .NET suite for this capability is the two ported tests (both carrying a bug trait) plus the classes
`WebPluginReadTests`, `WebPluginUploadTests`, `WebPluginPackageTests`, `WebPluginReuploadTests`, `WebPluginStateTests`,
`WebPluginCspTests`, `WebPluginRemovalTests`, `WebPluginRefreshTests` and `WebPluginAuditTests` (2026-11-09, run
class by class on the `integration-test` stand: 80 cases, 76 passed, 4 failed — each a `Category=Bug` test of P1, P2,
P8 or P9), the shared base `Tests/WebPluginTestBase.cs` and the package builder. Successful reads in those classes are
raw JSON because of P8. The TS suite the ported tests come from has the same two cases, both marked `test.fail`.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Installation-wide plugin switches** | | |
| Three flags read from the `plugins` section; `getSettings` fills `plugins` for a signed-in caller, false when the keys are missing | `web/ASC.Web.Api/Api/Settings/SettingsController.cs:185-200`; `PluginsDto`; `appsettings:391-400`; yaml `PluginsDto` (97724-97755), `SettingsDto.plugins` (99347-99352); `Tests/WebPluginReadTests.cs:49-77` (signed-in DocSpaceAdmin/User/Guest see all three true; anonymous gets no `plugins`) | TEST-CONFIRMED [run: PASS]; CODE-ONLY: false when the keys are missing (configuration of the stand) |
| 403 "Plugins disabled" on every operation while not enabled; "Forbidden action" for upload / delete while that flag is off | `Manager:92-103` (`DemandWebPlugins`), called at `Manager:141, 280, 410, 545` (update reaches it through `GetWebPluginByNameAsync` → `GetWebPluginsAsync`, `Manager:473-487`); `ConfigSettings:44-46` | CODE-ONLY (unreachable: installation-wide `plugins:*` switches cannot be changed on the stand) |
| No tariff/edition gate; hosted portals upload since v3.5 | no quota or tariff check anywhere in `Manager` / `Controller`; release notes v3.5 line 66 ("upload plugins in the SaaS version"), v2.0 line 32 | CODE-ONLY (unreachable: hosted tariff; the billing suite has no plugin harness; the standalone stand uploads in every test of this folder) |
| **Listing the plugins of a portal** | | |
| Any signed-in member, no permission demand; `enabled` filter applied after mapping; 401 anonymous | `Controller:117-135` (no `DemandPermissionsAsync`); yaml `getWebPlugins` (65626-65716, 401 row); `Tests/WebPluginReadTests.cs:79-129` (DocSpaceAdmin/RoomAdmin/User/Guest list, anonymous 401, `enabled` filter both ways) | TEST-CONFIRMED [run: PASS] |
| System first, portal plugin dropped when a system one has its name, cap at the configured max (100) | `Manager:320-337` (`GetWebPluginsForTenantAsync`); `ConfigSettings:48-52`; `Tests/WebPluginReuploadTests.cs:91-105` (the cap of 100 counts the listed plugins) | TEST-CONFIRMED [run: PASS] (limit only); CODE-ONLY: order and shadowing (unreachable: need an installation-wide plugin on disk) |
| Fields of an entry | `WebPluginDto:41-177`; `WebPluginDto:180-198` (mapper fills `createBy` through `EmployeeDtoHelper`); yaml `WebPluginDto` (103352-103560); `Tests/WebPluginReadTests.cs:129-166`; the generated client cannot read an entry at all, see P8 and ``Tests/WebPluginReadTests.cs:181-196` | TEST-CONFIRMED [run: PASS] (raw read); TEST-CONFIRMED [run: FAIL-BUG] (SDK read, P8) |
| Reads of the suite go through raw JSON (`WebPluginTestBase.GetPluginAsync` / `ListPluginsAsync`) because the generated model cannot read an entry (P8) | `Tests/WebPluginTestBase.cs` | — |
| **Reading one plugin by name** | | |
| Case-insensitive match on `name`; 404 "Plugin not found" | `Manager:473-480`; `Controller:156-165`; yaml `getWebPlugin` (65717-65815); `Tests/WebPluginReadTests.cs:167-213` | TEST-CONFIRMED [run: PASS] |
| **Uploading a plugin package** | | |
| Portal-settings right; no file 400 "No input file"; several 400 "Too many input files"; non-form request → `InvalidOperationException` from `Request.Form` → 403 | `Controller:69-96`; `Resource.resx:945-953`; yaml `addWebPluginFromFile` (65530-65625); `Tests/WebPluginUploadTests.cs:45-160` | TEST-CONFIRMED [run: PASS] |
| Extension must equal the configured one (`.zip`), length > 0, length ≤ configured max (5 MB) | `Manager:148-161`; `ConfigSettings:54-64`; `appsettings:395-397`; `Tests/WebPluginUploadTests.cs:161-182` | TEST-CONFIRMED [run: PASS] |
| Package stored, switched on, audit "Webplugin uploaded", plugin returned with `url` | `Manager:188-211`; `Controller:89-95`; `Tests/DeleteWebPluginTests.cs:58-60` and `Tests/UpdateWebPluginTests.cs:54-56` assert the upload answers success (`IsSuccessStatusCode`); `Tests/WebPluginUploadTests.cs:91-109` (body: enabled, `url`); audit in ``Tests/WebPluginAuditTests.cs:66-95` | TEST-CONFIRMED [run: PASS] |
| **Contents of a plugin package** | | |
| `config.json` and `plugin.js` required, `plugin.css` optional, assets under `assets/`; allowed asset extensions `.jpg .jpeg .png .svg`, at most 10 kept, others skipped | `Manager:624-721` (`SafeExtractPluginToTemp`); `appsettings:398-399`; `ConfigSettings:66-76`; `Tests/WebPluginPackageTests.cs:51-86` | TEST-CONFIRMED [run: PASS] (required files, optional css); CODE-ONLY: which assets are kept and the 10-asset cap (the stored asset files are not readable through the API) |
| Entry count ≤ assets max + 3 (13), unknown/zero size, per-entry size ≤ 5 MB, ratio 100:1, depth 2, unsafe paths; declared-size and running-size checks while extracting | `Manager:171-173, 571-622` (`AnalyzeZip`), `751-778` (`ExtractWithSizeMonitoring`); `Tests/WebPluginPackageTests.cs:86-135` | TEST-CONFIRMED [run: PASS] (count, zero size, entry size, ratio, depth, `..`); CODE-ONLY: declared-size vs running-size checks while extracting (need a forged central directory) |
| Every failure inside the archive handling is wrapped as 400 "Wrong plugin archive. <reason>" | `Manager:213-216`; `Resource.resx:924-926`; `Tests/WebPluginPackageTests.cs:51-135`, ``Tests/WebPluginUploadTests.cs:182-186` (not a zip) | TEST-CONFIRMED [run: PASS] |
| Minimal valid package = `config.json` + `plugin.js` (the shared builder of the suite) | `Tests/WebPluginsTestData.cs:50-75` | TEST-CONFIRMED [run: NO-LINK] |
| `cssUrl` derived from `url` when `plugin.css` exists | `Manager:202-205, 379-382`; `Tests/WebPluginPackageTests.cs:67-84` | TEST-CONFIRMED [run: PASS] |
| **Plugin name and uniqueness** | | |
| `name` regex `^[a-z0-9_.-]+$`, no leading dot; `pluginName` regex `^[0-9a-zA-Z_$]+$`; "Wrong plugin name" | `Manager:119-123, 226-238`; `Resource.resx:942-944`; `Tests/WebPluginPackageTests.cs:137-157` | TEST-CONFIRMED [run: PASS] |
| Portal package: refused when a system plugin has the `name` (any case) or `pluginName`, or another portal plugin with a different name has the `pluginName`; "Plugin already exist" | `Manager:260-275`; `Resource.resx:927-929`; `Tests/WebPluginPackageTests.cs:159-171` (another portal plugin with the same `pluginName`) | TEST-CONFIRMED [run: PASS] (portal half); CODE-ONLY: the system-plugin half (unreachable: no installation-wide plugin on the stand) |
| **Re-uploading a plugin under the same name** | | |
| Folder deleted and rewritten; stored settings for the name read back and kept; switched on; `hash` from `version` | `Manager:179-186, 198-209, 780-795`; `WebPluginDto:143-148` (url text); `Tests/WebPluginReuploadTests.cs:46-64` | TEST-CONFIRMED [run: PASS] |
| Same-version re-upload keeps the `url` (client shows a caching warning, v3.7) | `Manager:198-200`; release notes v3.7 line 73; `Tests/WebPluginReuploadTests.cs:65-77` | TEST-CONFIRMED [run: PASS] |
| **Portal plugin count limit** | | |
| `count + 1 > max` refused unless the name is already installed; "The number of plugins exceeds the limit" wrapped into the archive message (400) | `Manager:179-186, 213-216`; `Resource.resx:939-941`; `ConfigSettings:48-52`; `appsettings:396`; `Tests/WebPluginReuploadTests.cs:91-105` (400, re-upload of an installed name allowed); message in ``Tests/WebPluginReuploadTests.cs:107-119` (P9) | TEST-CONFIRMED [run: PASS] (refusal, exemption); TEST-CONFIRMED [run: FAIL-BUG] (message, P9) |
| **Installation-wide plugins** | | |
| Installation storage module for tenant −1, `system` true, state stored per portal in the portal settings | `Manager:81-82, 105-112, 324-326, 359, 373`; `storage.json:169-185` (`systemwebplugins`, `appendTenantId: false`) | CODE-ONLY (unreachable: an installation-wide upload writes storage shared by every parallel test portal) |
| `createBy` empty / `createOn` zero for a shipped plugin (manifest carries none) | `Manager:395-404` (manifest deserialised as-is); `WebPluginDto:111-121` | CODE-ONLY (unreachable: needs a shipped plugin on disk) |
| `system=true` only on standalone, else 403 "Changes to system plugins are prohibited" | `Manager:143-146`; `RequestDto:102-109`; `Resource.resx:936-938` | CODE-ONLY (unreachable: the 403 needs a hosted stand; the standalone install writes shared storage) |
| System package refused when the uploading portal has the `name` or `pluginName`, or another system plugin has the `pluginName` | `Manager:244-259` | CODE-ONLY (unreachable: installation-wide upload) |
| v3.6: deletable on standalone when the config allows | release notes v3.6 line 45; `Manager:549-552` | CODE-ONLY (unreachable: needs an installation-wide plugin) |
| **Switching a plugin on or off and storing its settings** | | |
| Portal-settings right; body replaces state; `settings` required, `[StringLength(255)]`; encrypted with the instance key; empty → null; 200 empty; audit "Webplugin updated" | `Controller:188-199`; `RequestDto:36-80` (`[StringLength(255)]` at 55, `required` at 56); `Manager:482-487, 510-541`; yaml `updateWebPlugin` (65816-65920), `WebPluginRequest` (103561-103590); `Tests/WebPluginStateTests.cs:49-222`; audit in ``Tests/WebPluginAuditTests.cs:66-95` | TEST-CONFIRMED [run: PASS]; CODE-ONLY: encryption with the instance key (not observable through the API) |
| Owner uploads, then `updateWebPlugin` `enabled:false, settings:"{}"` → 200 (name `files-autotest`) | `Tests/UpdateWebPluginTests.cs:46-70`; `TS:197-217` | TEST-CONFIRMED [run: PASS] |
| Settings decrypted on every read, returned verbatim | `Manager:289-315`; `Tests/WebPluginStateTests.cs:100-116` | TEST-CONFIRMED [run: PASS] |
| **Stored state of a plugin** | | |
| Row kept only while `enabled || settings != null`, otherwise removed; manifest values apply when no row exists | `Manager:510-531` (`UpdateWebPluginAsync`), `289-315` (state applied only when a row exists), `395-404` (manifest deserialised into the same object, `enabled` and `settings` included); `Tests/WebPluginStateTests.cs:117-133, 224-238` (manifest `enabled: true` keeps the plugin on, P1) | TEST-CONFIRMED [run: PASS] (state without row); TEST-CONFIRMED [run: FAIL-BUG] (manifest value, P1) |
| Undecryptable settings → plugin reported off, state rewritten | `Manager:299-312` | CODE-ONLY (unreachable: needs a change of the instance key) |
| **Content Security Policy domains of a plugin** | | |
| `cspDomains` comma-separated; added on upload and on enable, removed on disable and delete; `InvalidOperationException` (403) when the header exceeds `csp:default.maxSize` (15 KB default); state saved before the CSP call | `Controller:91, 198, 264, 267-290`; `products/ASC.Files/Core/Helpers/CspSettingsHelper.cs:119-124, 396`; `Manager:50`; `Tests/WebPluginCspTests.cs:59-121`; refresh in ``Tests/WebPluginRefreshTests.cs:104-128` | TEST-CONFIRMED [run: PASS] (add/remove, 403 over the limit); TEST-CONFIRMED [run: FAIL-BUG] (state saved before the CSP call, P2) |
| **Deleting a plugin** | | |
| Portal-settings right; delete flag; case-insensitive lookup (404); system plugin only on standalone (403); folder missing → 404; folder deleted, cache reset, state removed, audit "Webplugin deleted", CSP domains removed, 200 empty | `Controller:254-265`; `Manager:543-568`; yaml `deleteWebPlugin` (65921-65980); `Tests/WebPluginRemovalTests.cs:44-128` | TEST-CONFIRMED [run: PASS]; CODE-ONLY: delete flag off (configuration), system plugin (installation-wide), folder missing (storage) |
| Owner uploads then deletes `files-autotest` → 200 | `Tests/DeleteWebPluginTests.cs:50-68`; `TS:176-195` | TEST-CONFIRMED [run: PASS] |
| Reserved-prefix names reach the controller: the proxy routes `^/api/2.0/settings/webplugins(?:/|$)` before `/(files|privacyroom)` | `nginx:601-603` (before 605) — the fix for bug 83425; the tests still carry `[Trait("Bug", "83425")]` and the TS suite `test.fail` | TEST-CONFIRMED [run: NO-LINK] (asserts 200) |
| **Refreshing a plugin replaced in storage** | | |
| Portal-settings right; name lower-cased and validated (404); version compared per scope (−1 and portal); cache reset and socket event only when outdated; returns the served plugin | `Controller:221-232`; `Manager:406-471`; `Tests/WebPluginRefreshTests.cs:52-102` (raw HTTP; the pinned SDK package has no operation) | TEST-CONFIRMED [run: PASS] (rights, case, 404, served plugin); CODE-ONLY: per-scope version compare, cache reset and socket event (need files replaced in storage) |
| Refresh leaves stored state, settings and CSP domains untouched (only the served list and the clients are refreshed) | `Manager:406-471` (no write to the settings or the CSP); `Tests/WebPluginRefreshTests.cs:104-128` | TEST-CONFIRMED [run: PASS] |
| Contract (see deviations, P7) | the yaml of the SDK submodule now has `PUT /api/2.0/settings/webplugins/{name}/refresh` (`refreshWebPlugin`, yaml 70055-70082) and the repo SDK source has `RefreshWebPluginAsync`; the pinned `DocSpace.API.SDK` 4.1.0 package used by the tests is treated as lacking it, so the tests call it raw | — |
| **Plugin files served to the browser** | | |
| `url` = storage URI of `<name>/plugin.js` + `?hash=<version>`; per-portal vs installation storage; immutable cache for a year on `storage/(webplugins|systemwebplugins)/...?hash` | `Manager:132-137, 196-205, 363-382`; `storage.json:169-185`; `nginx:61`; `Tests/WebPluginReadTests.cs:129-166`, ``Tests/WebPluginReuploadTests.cs:46-64` | TEST-CONFIRMED [run: PASS] (`<name>/plugin.js?hash=<version>` under `webplugins`); CODE-ONLY: installation storage, nginx cache headers |
| v3.2 caching of plugin requests | release notes v3.2 line 18; `nginx:61`; `Manager:339-353` (memory cache of the manifests, tag reset on storage change) | CODE-ONLY (unreachable: nginx of the router; the tests call the services directly) |
| **Clients are told when a plugin changes** | | |
| `change-web-plugin` with `webPluginName` and `enabled` to the portal's quota room, emitted as `s:change-web-plugin`; sent from update (upload, enable/disable, delete) and from a refresh that found a change | `Manager:435, 538`; `common/ASC.Core.Common/Notify/Signalr/QuotaSocketManager.cs:75-80`; `common/ASC.Socket.IO/app/controllers/files.js:112-113`; `common/ASC.Socket.IO/app/hubs/files.js:424-426` | CODE-ONLY (not done: the Web.Api suite has no socket service or `PortalSocket` wiring, which lives outside this folder) |
| **Audit trail of plugin changes** | | |
| `WebpluginUploaded` / `WebpluginUpdated` / `WebpluginDeleted` with target `pluginName+version` and the plugin name as description; none on reads/refresh | `Controller:89, 196, 262`; yaml audit enum rows 81468-81470 (6085-6087 in the description at 80785); `Tests/WebPluginAuditTests.cs:66-95` (one event each, target `pluginName+version`, none for get/list/refresh) | TEST-CONFIRMED [run: PASS]; CODE-ONLY: the description text (not in `AuditEventDto`) |
| **Plugins in backup and restore** | | |
| `webplugins` is in the allowed storage modules of a portal backup; `systemwebplugins` is not, and is `disableMigrate: true` / `visible: false` in storage; states live in the portal settings table | `common/ASC.Data.Backup.Core/Tasks/PortalTaskBase.cs:123-150`; `storage.json:177-185`; `ConfigSettings:79-91` (`ISettings` → `webstudio_settings`) | CODE-ONLY (not done: backup/restore runs in the backup suite, not on this stand) |

## Where the server differs from the release notes or the OpenAPI document (spec follows the server)

| Release note / document | Server |
|---|---|
| v3.5 notes: "maximum number of plugins per portal 10 including system ones" | The configured maximum is 100 (`appsettings:396`) and the code default is 100 when the key is missing (`ConfigSettings:48-52`); the spec says 100. |
| v2.0 notes: "in SaaS only the system plugins from the CDN"; v2.0: plugin settings "do not work yet, only defaults apply" | Since v3.5 a hosted portal uploads its own plugins when the installation allows it (`Manager:141` checks only the config flag); the per-portal `settings` string is stored encrypted and returned (`Manager:510-541`). Only `system=true` stays standalone-only. |
| yaml `WebPluginDto.system`: "A system plugin cannot be deleted … only switched off"; yaml `PluginsDto.delete`: "plugins that ship with the installation cannot be removed regardless of this flag" | On a standalone installation with the delete flag on, `deleteWebPlugin` removes an installation-wide plugin from storage for every portal (`Manager:549-561`; v3.6 notes line 45). The texts describe the hosted case only. |
| yaml has no `PUT /api/2.0/settings/webplugins/{name}/refresh` | `Controller:214-232` publishes `refreshWebPlugin`; the contract of 2026-11-07 and the SDKs lacked it; the yaml of the SDK submodule now has it (70055-70082), and the integration tests call it raw (`Tests/WebPluginRefreshTests.cs`). Probably the contract was generated before the action was added. |
| yaml `WebPluginDto.name` example `Example Plugin` | A manifest name with a space or a capital letter is refused ("Wrong plugin name", `Manager:120-122`); the example cannot be uploaded. |
| yaml `getWebPlugins` 403 "Web plugins are switched off" only | Also the generic authorization of the Web.Api (401 anonymous) — listed in the yaml as the standard 401 row; no settings right is demanded, so no "Access denied" 403 exists for the reads. Agrees. |
| TS suite: `settings: null` in the PUT body, DELETE case filed as bug 84321, PUT as 83425 | The .NET port sends `"{}"` because the generated client refuses `null`; both .NET tests are filed under 83425. The server accepts `null` (`string.IsNullOrEmpty` → no settings, `Manager:516`). The proxy fix (`nginx:601`) makes both cases pass; the bug traits are stale. |
| `Controller:66` 400 text includes "the portal already holds the maximum number of plugins" and `Controller:67` 403 includes "the request is not a form" | Both match the code paths (`Manager:179-186` wrapped at 213-216; `Request.Form` throws `InvalidOperationException` → 403). Agrees. |
| Test names `DeleteWebPlugin_NameWithFilesPrefix_RemovesPlugin`, `UpdateWebPlugin_NameWithFilesPrefix_DisablesPlugin` | Assertions match the names (200 asserted); the "disables" half is not read back. |

## Left out of the spec, and why

- **The editor add-on list** (`pluginsData` of the document editor configuration, `PluginsConfigDto` in
  `products/ASC.Files/Core/ApiModels/ResponseDto/EditorConfigDto.cs:228-250`): always empty, the building code is
  commented out; named in the Purpose only.
- **What a plugin does in the browser**: scopes, items, context-menu actions, pages/sections, localisation, CSS
  customisation, error display (v3.7, v4.0 notes), the plugin SDK package and the Playwright e2e coverage
  (`../tests/e2e-tests/src/objects/settings/integration/`): client behaviour, not the server.
- **A `settings/plugins` route**: none exists in the Web.Api; the switches are only the `plugins` block of
  `getSettings`. The proxy's `location ~* /plugins` (`nginx:633-635`, `$plugins_service_url`) points at a service
  outside this repository and is not part of `settings/webplugins`.
- **The Content Security Policy mechanics** beyond the domain list a plugin adds (header building, per-domain cache
  keys, standalone base domain): `CspSettingsHelper`, its own concern.
- **Rate-limit headers and 429 rows** of the yaml: generic policy.
- **The in-memory manifest cache and its cross-node reset** (`Manager:125-130, 339-353`): implementation; the
  refresh requirement describes its observable effect only.
- **The exact storage layout** (`$STORAGE_ROOT\Studio\{tenant}\webplugins\<name>\…`, `~/studio/webplugins`): configuration.

## Suspected defects

Re-checked against the merge of release/v4.1.0 (`d58816bece..3533ad8a89`): nothing of this capability changed in that
range — `Manager`, `Controller`, `ConfigSettings`, `RequestDto`, `WebPluginDto`, `PluginsDto`, `CspSettingsHelper.cs`,
`Tests/` and the yaml are byte-identical before and after the merge — so every line reference above still holds and
P1–P7 are all STILL OPEN; no new row. 2026-11-09: P1 and P2 are now reproduced by red `Category=Bug` tests, P8 and P9
were found by the new tests.

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| P1 | Stored state of a plugin | The manifest is deserialised into the same object that carries the portal state, so a `config.json` declaring `"enabled": true` (or `"settings"`) is reported as on for every portal that has no stored row, and switching such a plugin off without settings *removes* the row (`enabled || settings` is false), after which the manifest value is back on the next read: the plugin cannot be switched off. On upload the manifest is re-serialised with the uploader's `createBy`/`createOn` but the `enabled` value it carried is kept. A shipped manifest may also set `createBy`/`createOn` to any value. | `Manager:395-404, 657-671, 510-531, 289-315`; red test `Tests/WebPluginStateTests.cs:224-238` |
| P2 | CSP domains | The plugin state is saved (and the socket event sent) before the CSP save; when the header limit is hit the caller gets 403 while the plugin is installed/enabled and `getWebPlugins` shows it on — upload and update are not atomic with the policy change. | `Controller:87-91, 194-198, 267-290`; `CspSettingsHelper.cs:119-124`; red test `Tests/WebPluginCspTests.cs:104-121` |
| P3 | Deleting a shadowed portal plugin | A portal plugin whose `name` an installation-wide plugin also carries is hidden from every read and lookup, so it can neither be listed nor deleted (the lookup finds the system one; on a hosted portal that is 403); its files stay in the portal storage, counted in the backup but not in the plugin count. | `Manager:320-337, 473-480, 547-552` |
| P4 | Upload error mapping | Every exception raised after the size checks — including storage I/O failures when copying the package or reading the existing settings — is wrapped as 400 "Wrong plugin archive. <message>", so an infrastructure failure is reported as a client error with an internal message. | `Manager:167-216` |
| P5 | Read repairs state | `getWebPlugins`/`getWebPlugin`, callable by any member, rewrite the portal's plugin settings and emit the socket event when a stored settings value fails to decrypt; a GET by a Guest mutates portal settings. | `Manager:299-312, 510-541` |
| P6 | Count limit and the shadow rule | The limit counts the de-duplicated list (system plugins included) but a hidden portal plugin (P3) and a system plugin with the same name together count once, while a re-upload of an installed name bypasses the limit even when the portal is already above it after a config change; the list is silently truncated at the maximum without an error. | `Manager:179-186, 331-334` |
| P7 | Contract | `refreshWebPlugin` is missing from the published OpenAPI document and the SDKs although the controller publishes it (see deviations); agents and generated clients cannot reach it. 2026-11-09: the yaml of the SDK submodule now carries it (70055-70082) and so does the repo SDK source; the pinned 4.1.0 package was not re-checked, the tests call it raw. | `Controller:214-232`; yaml |
| P8 | Contract | The published `WebPluginDto` lists `image`, `cssUrl`, `settings` (and the other optional manifest fields) as required, while the server leaves a null field out of the answer; the generated C# client therefore throws "Required property 'image' not found in JSON" on `getWebPlugin` / `getWebPlugins` for any plugin whose manifest has no image, i.e. nearly every plugin. | `sdk/docspace-api-sdk-csharp/src/DocSpace.API.SDK/Model/WebPluginDto.cs` (16 `IsRequired = true`); red test `Tests/WebPluginReadTests.cs:181-196` |
| P9 | Count limit message | At the limit the upload looks the new name up with `GetWebPluginByNameAsync`, which throws `ItemNotFoundException` instead of returning null, so the caller gets 400 "Wrong plugin archive. Plugin not found"; the `ErrorWebPluginMaxCount` text ("The number of plugins exceeds the limit") is never sent. The refusal itself and the re-upload exemption work. | `Manager:179-186, 473-480`; red test `Tests/WebPluginReuploadTests.cs:107-119` |
