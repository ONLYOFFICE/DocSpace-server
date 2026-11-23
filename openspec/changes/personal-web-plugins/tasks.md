# Tasks

The code of groups 1-5 is on `feature/personal-plugins` (`410d6a20f6`, `36c35c030c`, `2dfeea03fc`, `7a0fb27433`,
`e9a85ab1ea`). A task is ticked once its test has run green on the `integration-test` stand.

## 1. Personal level: switch, storage and state

- [ ] 1.1 Report `plugins.personal` in `getSettings` and answer 403 "Personal plugins disabled" to a guest and while personal plugins are off; verified by `PersonalWebPluginTests.UploadPersonalWebPlugin_Guest_Forbidden` and a new case that lists, reads, updates and deletes with `personal=true` as a Guest
- [ ] 1.2 Store personal files under the member's folder of the `webplugins` module and their state in the member's settings row; verified by `PersonalWebPluginTests.UploadPersonalWebPlugin_User_ListedForOwnerOnly` (listed for the owner, not for another User, empty `personal=true` list for the other User)
- [ ] 1.3 Refuse `personal=true` with `system=true` with 400 "Changes to system plugins are prohibited"; add a case to `PersonalWebPluginTests`
- [ ] 1.4 Switch and delete a personal plugin without the portal-settings right, the delete switch not needed; add cases to `PersonalWebPluginTests`: `updateWebPlugin` with `personal=true` and `enabled` false, then `getWebPlugin` with `personal=true` reports `enabled` false; `deleteWebPlugin` with `personal=true` answers 200 and a second one 404 "Plugin not found"

## 2. Name rule between the levels

- [ ] 2.1 Refuse a personal upload that takes the `name` or `pluginName` of a portal plugin with 400 "Wrong plugin archive. A plugin with the same name is already installed for the whole portal"; verified by `PersonalWebPluginTests.UploadPersonalWebPlugin_NameOfPortalPlugin_Refused`, plus a case for a `pluginName`-only clash
- [ ] 2.2 Refuse a second personal plugin of the same member with the same `pluginName` with 400 "Wrong plugin archive. Plugin already exist"; add a case to `PersonalWebPluginTests`
- [ ] 2.3 Shadow a personal plugin when a portal plugin takes its name, and list it again after the portal plugin is deleted; verified by `PersonalWebPluginTests.UploadPortalWebPlugin_NameOfPersonalPlugin_ShadowsPersonalCopy`, plus a case that deletes the portal plugin and finds the personal one listed with its stored state
- [ ] 2.4 Delete a shadowed personal plugin without touching the portal plugin of the same name; add a case to `PersonalWebPluginTests` (portal plugin still listed, `enabled` unchanged)

## 3. Personal count limit

- [ ] 3.1 Refuse a new personal name beyond 10 with 400 "Wrong plugin archive. The number of plugins exceeds the limit" and accept a re-upload at the limit; add both boundary cases (10 held + new name, 10 held + new version of a held name) to `PersonalWebPluginTests` or a class of their own if the suite would pass 24 cases
- [ ] 3.2 Confirm that personal plugins do not count toward the portal limit: a member with personal plugins does not change the result of `WebPluginReuploadTests` at the portal limit

## 4. Files, CSP and events

- [ ] 4.1 Serve a personal plugin's `url` to its owner only (403 to another User, the portal owner and an anonymous caller; 200 to the owner); add a test that requests the `url` of a personal plugin as each of them
- [ ] 4.2 Keep personal `cspDomains` out of the portal CSP and give them to their owner through `getCspSettings` with the `asc_personal_csp` cookie, without 304; add tests: the portal's CSP settings after a personal upload with `cspDomains`, the owner's `header` with the cookie and `If-Modified-Since`, another User's `header` with the cookie
- [ ] 4.3 Set and clear `asc_personal_csp` in `getWebPlugins` and the personal upload, update and deletion; add a test that reads `Set-Cookie` after a personal upload with `cspDomains` and after switching that plugin off
- [ ] 4.4 Send no plugin event for a personal change; CODE-ONLY until a suite runs the socket service (the Web.Api stand has none)

## 5. Changed requirements and deviations

- [ ] 5.1 Re-run the tests the `verification.md` of `plugins` cites for the modified requirements ("Installation-wide plugin switches", "Listing the plugins of a portal", "Reading one plugin by name", "Uploading a plugin package", "Switching a plugin on or off and storing its settings", "Content Security Policy domains of a plugin", "Deleting a plugin", "Clients are told when a plugin changes"): the classes `WebPluginReadTests`, `WebPluginUploadTests`, `WebPluginPackageTests`, `WebPluginReuploadTests`, `WebPluginStateTests`, `WebPluginCspTests`, `WebPluginRemovalTests`, `WebPluginRefreshTests`, `WebPluginAuditTests`, `DeleteWebPluginTests`, `UpdateWebPluginTests`; update a case only where the requirement text changed for portal plugins (none expected)
- [ ] 5.2 Add the case "Personal plugin read without the flag" (`getWebPlugin` without `personal=true` on a personal name → 404) and "Personal plugins after the portal ones" (order and `personal` of the entries) to `PersonalWebPluginTests`
- [ ] 5.3 Re-run the tests the `verification.md` of `portal-settings` cites for "Reading the Content Security Policy" and confirm they still pass without the cookie
- [ ] 5.4 P9: run `WebPluginReuploadTests.Upload_PortalAtThePluginLimit_SaysTheLimitIsReached`; once it is green, drop its `Category=Bug` trait and the P9 comment, and close P9 in `openspec/deviations.md` with this change
- [ ] 5.5 Add the new rows to `openspec/specs/plugins/verification.md` and `openspec/specs/portal-settings/verification.md` with the tests of groups 1-4

## 6. Specs and checks

- [ ] 6.1 Run `openspec validate --specs --no-interactive` and `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec plugins` and `--spec portal-settings`. The change adds no operation, only the `personal` query parameter and field, so every operation of the change is already in the published document; check that each one is called by a test with `personal=true`: `addWebPluginFromFile`, `getWebPlugins`, `getWebPlugin`, `updateWebPlugin`, `deleteWebPlugin` (raw HTTP in `PersonalWebPluginTests` until the SDK has the parameter)

## Workflow follow-up

- Regenerate the OpenAPI documents and the SDKs (`generate-sdk`) so `personal` reaches them, then move the raw HTTP calls of `PersonalWebPluginTests` to the SDK.
- Archive the change after the branch is merged.
