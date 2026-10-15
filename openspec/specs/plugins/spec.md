# plugins Specification

## Purpose

Defines the web plugin system of a portal: the installation-wide switches that decide whether plugins
run at all and whether an administrator may upload or delete them (reported by `getSettings` as
`plugins`), the plugin package an administrator uploads (`POST /api/2.0/settings/webplugins`) and the
rules it has to pass, the two kinds of plugin — the ones that ship with the installation ("system",
installation-wide) and the ones uploaded into one portal — the per-portal state of a plugin (`enabled`
and the opaque `settings` string), the listing and lookup, the deletion, the refresh of a plugin
replaced in storage, the Content Security Policy domains a plugin declares, the audit-trail and
real-time events the changes produce, and the backup of plugin files. What a plugin does in the browser
(scopes, items, pages, the plugin SDK) is the client's business and is out of scope; so is the editor
add-on list of the document editor configuration, which the server always leaves empty.

Sources: the integration tests of `web/ASC.Web.Api.Tests` (`02_Settings/Webplugins`) and the TypeScript
suite they were ported from, the OpenAPI contract `sdk/docspace-api-spec/docspace-backend.yaml`
(operations named by `operationId`), the product code of the plugin manager, controller and settings
where the tests stop, the runtime configuration of the installation, and the release notes (v2.0, v2.5,
v3.2, v3.5, v3.6, v3.7, v4.0). Where the notes or the OpenAPI document and the server disagree this
specification follows the server; the evidence per scenario, the differences and the suspected defects
are in `verification.md` next to this file.

## Requirements

### Requirement: Installation-wide plugin switches

The installation SHALL carry three switches — plugins enabled, upload allowed, delete allowed — that
`getSettings` SHALL report to a signed-in caller as `plugins.enabled`, `plugins.upload` and
`plugins.delete` (false when not configured). While plugins are not enabled every `settings/webplugins`
operation SHALL answer 403 "Plugins disabled"; an upload while upload is off and a deletion while delete
is off SHALL answer 403 "Forbidden action". No tariff or edition gates plugins.

#### Scenario: Signed-in member reads the switches

- **WHEN** any signed-in member calls `getSettings`
- **THEN** the answer carries `plugins` with `enabled`, `upload` and `delete` as the installation configured them

#### Scenario: Plugins switched off for the installation

- **WHEN** plugins are not enabled and a member lists, reads, uploads, updates, refreshes or deletes a plugin
- **THEN** the system answers 403 "Plugins disabled"

#### Scenario: Upload or delete switched off

- **WHEN** upload is off and the owner uploads a package, or delete is off and the owner deletes a plugin
- **THEN** the system answers 403 "Forbidden action"

### Requirement: Listing the plugins of a portal

`getWebPlugins` SHALL return, to any signed-in member without a settings right, the installation-wide
plugins first and then the portal's own, dropping a portal plugin whose name an installation-wide plugin
already uses, capped at 100 entries and not paginated. `enabled=true` or `enabled=false` SHALL keep only
the plugins in that state. Each entry SHALL carry the manifest data, `createBy`, `createOn`, the portal
state (`enabled`, `settings`), `system`, `url` and `cssUrl`. Anonymous: 401.

#### Scenario: Member lists an empty portal

- **WHEN** a User calls `getWebPlugins` on a portal where nothing is installed and the installation ships no plugins
- **THEN** the system answers 200 with an empty list

#### Scenario: Fields of an entry

- **WHEN** a plugin is listed
- **THEN** the entry carries `name`, `version`, `minDocSpaceVersion`, `description`, `license`, `author`, `homePage`, `pluginName`, `scopes`, `image`, `nameLocale`, `descriptionLocale` and `runtime` from its manifest

#### Scenario: Installation-wide plugin shadows a portal plugin

- **WHEN** an installation-wide plugin and a portal plugin carry the same `name`
- **THEN** only the installation-wide one is listed, with `system` true

#### Scenario: Filter by state

- **WHEN** the owner calls `getWebPlugins` with `enabled=false`
- **THEN** only the plugins whose stored state is off are returned

### Requirement: Reading one plugin by name

`getWebPlugin` SHALL look the plugin up over the same set `getWebPlugins` returns, by its manifest `name`
matched without regard to case — not by the display name and not by `pluginName` — and SHALL answer 404
"Plugin not found" for a name that is not installed. Any signed-in member may read it; anonymous 401.

#### Scenario: Name in another case

- **WHEN** a member reads `Example-Plugin` while the installed manifest name is `example-plugin`
- **THEN** the system answers 200 with that plugin

#### Scenario: Unknown name

- **WHEN** a member reads a name no installed plugin carries
- **THEN** the system answers 404 "Plugin not found"

### Requirement: Uploading a plugin package

`addWebPluginFromFile` SHALL require the portal-settings right (owner or DocSpace admin; others 403
"Access denied", anonymous 401) and a form request with exactly one non-empty `.zip` file of at most 5 MB
(400 "No input file", "Too many input files", "Wrong file extension" or "File size exceeds limit"; 403
when the body is not a form). It SHALL store the package, switch the plugin on, record the upload in the
audit trail and answer 200 with the plugin as `getWebPlugin` would return it.

#### Scenario: Owner uploads a valid package

- **WHEN** the owner posts one `.zip` holding a manifest and the entry script as `multipart/form-data`
- **THEN** the system answers 200 with the plugin, `enabled` true, `system` false, `createBy` the owner and a `url` ending in `plugin.js?hash=<version>`

#### Scenario: Package too large

- **WHEN** the uploaded file is larger than 5 MB
- **THEN** the system answers 400 "File size exceeds limit"

#### Scenario: Two files in one request

- **WHEN** the form carries two files
- **THEN** the system answers 400 "Too many input files"

### Requirement: Contents of a plugin package

The archive SHALL contain a manifest `config.json` and an entry script `plugin.js` at its root; a
stylesheet `plugin.css` and images under `assets/` are optional. The system SHALL refuse with 400 "Wrong
plugin archive. <reason>" more than 13 entries, an entry of unknown or zero size, one over 5 MB or beyond
its declared size, a compression ratio over 100:1, a path deeper than two levels or an unsafe path.
Images outside `.jpg`, `.jpeg`, `.png`, `.svg` SHALL be skipped and at most 10 kept.

#### Scenario: Manifest missing

- **WHEN** the archive has no `config.json`
- **THEN** the system answers 400 "Wrong plugin archive. The required entry config.json not found."

#### Scenario: Entry script missing

- **WHEN** the archive has a manifest but no `plugin.js`
- **THEN** the system answers 400 "Wrong plugin archive. The required entry plugin.js not found."

#### Scenario: Path traversal in the archive

- **WHEN** an entry is named `../plugin.js`
- **THEN** the system answers 400 "Wrong plugin archive. Unsafe path detected: ../plugin.js"

#### Scenario: Stylesheet shipped

- **WHEN** the archive also holds `plugin.css`
- **THEN** the plugin's `cssUrl` is the `url` with `plugin.js` replaced by `plugin.css`; otherwise `cssUrl` is empty

### Requirement: Plugin name and uniqueness

The manifest `name` SHALL consist of lower-case letters, digits, `_`, `.` and `-` and not start with a
dot (it is also the storage folder); `pluginName` SHALL be a JavaScript identifier. Either failing SHALL
answer 400 "Wrong plugin archive. Wrong plugin name". A portal package SHALL be refused with 400 "Wrong
plugin archive. Plugin already exist" when an installation-wide plugin has its `name` (any case) or its
`pluginName`, or another portal plugin with a different `name` has its `pluginName`.

#### Scenario: Upper-case name

- **WHEN** the manifest says `"name": "MyPlugin"`
- **THEN** the system answers 400 "Wrong plugin archive. Wrong plugin name"

#### Scenario: Same JavaScript object as another plugin

- **WHEN** a package named `b` declares the `pluginName` an installed plugin `a` already registers
- **THEN** the system answers 400 "Wrong plugin archive. Plugin already exist"

#### Scenario: Name taken by an installation-wide plugin

- **WHEN** a portal package carries the `name` of a plugin that ships with the installation
- **THEN** the system answers 400 "Wrong plugin archive. Plugin already exist"

### Requirement: Re-uploading a plugin under the same name

A package whose `name` is already installed in the same scope SHALL replace the stored files of that
plugin, keep the `settings` string the portal stored for the name, switch the plugin on, and answer the
new manifest; `url` SHALL change with the `version` through its `hash` query. The replacement SHALL be
accepted even when the portal is at its plugin limit.

#### Scenario: New version uploaded

- **WHEN** the owner uploads `example-plugin` version 1.0.0, saves settings for it, and uploads version 1.1.0 under the same name
- **THEN** the second answer carries version 1.1.0, `enabled` true, the saved `settings` and a `url` ending in `?hash=1.1.0`

#### Scenario: Same version uploaded again

- **WHEN** the same version is uploaded twice
- **THEN** the files are replaced and the `url` is unchanged, so a browser may keep serving the cached script

### Requirement: Portal plugin count limit

A portal SHALL hold at most 100 plugins, the installation-wide ones counted in: an upload that would add
a new name beyond that SHALL answer 400 "Wrong plugin archive. The number of plugins exceeds the limit",
while a re-upload of an installed name is accepted. The list SHALL never return more than that number.

#### Scenario: Limit reached

- **WHEN** 100 plugins are available to the portal and the owner uploads a package with a new name
- **THEN** the system answers 400 "Wrong plugin archive. The number of plugins exceeds the limit"

### Requirement: Installation-wide plugins

A plugin in the installation-wide storage SHALL be available to every portal with `system` true, an
empty `createBy` and a zero `createOn`, and SHALL win its `name` over a portal plugin; its `enabled`
state and `settings` SHALL be stored per portal. `addWebPluginFromFile` with `system=true` SHALL store
the package installation-wide on a self-hosted installation only, else 403 "Changes to system plugins
are prohibited", and SHALL refuse a `name` or `pluginName` the uploading portal has (400).

#### Scenario: Installation-wide plugin on two portals

- **WHEN** portal A switches an installation-wide plugin off and portal B does not
- **THEN** `getWebPlugin` answers `enabled` false on A and the plugin's own state on B

#### Scenario: Hosted portal asks for an installation-wide install

- **WHEN** the owner of a hosted portal uploads with `system=true`
- **THEN** the system answers 403 "Changes to system plugins are prohibited" and stores nothing

#### Scenario: Self-hosted installation-wide install

- **WHEN** the owner of a self-hosted portal uploads with `system=true`
- **THEN** the system answers 200 with `system` true and the plugin appears on every portal of the installation

### Requirement: Switching a plugin on or off and storing its settings

`updateWebPlugin` SHALL require the portal-settings right (403 "Access denied", anonymous 401), find the
plugin by `name` without regard to case (404 "Plugin not found") and replace the stored state with the
body: `enabled` and the required `settings` string of at most 255 characters (400 otherwise). `settings`
SHALL be stored encrypted for the portal and returned verbatim; an empty string SHALL mean no settings.
The call SHALL answer 200 with no body and record the change in the audit trail.

#### Scenario: Owner disables an uploaded plugin

- **WHEN** the owner uploads a plugin and calls `updateWebPlugin` with `enabled` false and `settings` `{}`
- **THEN** the system answers 200 and `getWebPlugin` reports `enabled` false

#### Scenario: Settings saved and read back

- **WHEN** the owner saves `settings` `{"theme":"dark"}` with `enabled` true
- **THEN** `getWebPlugin` and `getWebPlugins` return `settings` `{"theme":"dark"}` exactly

#### Scenario: Settings too long

- **WHEN** `settings` is 256 characters long
- **THEN** the system answers 400 and the stored state is unchanged

#### Scenario: Room admin tries to change a plugin

- **WHEN** a Room admin, a User or a Guest calls `updateWebPlugin`
- **THEN** the system answers 403 "Access denied"

### Requirement: Stored state of a plugin

The portal SHALL keep one state row per plugin name only while the plugin is on or has settings; a plugin
switched off with no settings SHALL have its row removed. A plugin with no stored row SHALL be reported
with the `enabled` and `settings` values of its manifest (normally off and empty). A stored `settings`
value that cannot be decrypted SHALL make the read report the plugin as off with empty settings and store
that state.

#### Scenario: Installation-wide plugin never touched

- **WHEN** a portal reads an installation-wide plugin it never switched
- **THEN** `enabled` and `settings` are whatever the plugin's manifest declares, off and empty for a manifest that declares nothing

#### Scenario: Undecryptable settings

- **WHEN** the stored settings of a plugin were encrypted by another installation key
- **THEN** the plugin is listed with `enabled` false and empty `settings`, and that state replaces the broken one

### Requirement: Content Security Policy domains of a plugin

A plugin manifest MAY declare `cspDomains`, a comma-separated list of origins. Uploading or switching the
plugin on SHALL add them to the portal's Content Security Policy domains; switching it off or deleting it
SHALL remove them. When adding them would push the policy header over 15 KB the call SHALL answer 403
"CSP header size exceeds maximum allowed size of 15360 bytes." after the plugin state was already saved.

#### Scenario: Plugin with external domains enabled

- **WHEN** a plugin declaring `cspDomains` `https://a.example,https://b.example` is uploaded
- **THEN** both origins appear in the portal's CSP settings and are gone again after the plugin is switched off

#### Scenario: Header limit exceeded

- **WHEN** switching a plugin on would make the CSP header longer than 15 KB
- **THEN** the system answers 403 with the size message and the plugin is nevertheless stored as on

### Requirement: Deleting a plugin

`deleteWebPlugin` SHALL require the portal-settings right (403 "Access denied", anonymous 401) and the
delete switch, find the plugin by `name` without regard to case (404 "Plugin not found", also when its
folder is missing from storage), refuse an installation-wide plugin on a hosted installation with 403
"Changes to system plugins are prohibited", and otherwise delete the folder, drop the state and
settings, remove the plugin's CSP domains, write the audit event and answer 200.

#### Scenario: Owner deletes an uploaded plugin

- **WHEN** the owner uploads a plugin and calls `deleteWebPlugin` with its name
- **THEN** the system answers 200 and `getWebPlugin` answers 404 "Plugin not found"
- **AND** a second `deleteWebPlugin` on the same name answers 404 "Plugin not found"

#### Scenario: Installation-wide plugin on a self-hosted installation

- **WHEN** the owner of a self-hosted portal deletes a plugin with `system` true while delete is allowed
- **THEN** the system answers 200 and the plugin disappears from every portal

#### Scenario: Installation-wide plugin on a hosted portal

- **WHEN** the owner of a hosted portal deletes a plugin with `system` true
- **THEN** the system answers 403 "Changes to system plugins are prohibited"

#### Scenario: Name starting with a reserved route word

- **WHEN** the owner uploads a plugin named `files-autotest` and then deletes it or switches it off by that name
- **THEN** the system answers 200

### Requirement: Refreshing a plugin replaced in storage

`refreshWebPlugin` (`PUT /api/2.0/settings/webplugins/{name}/refresh`) SHALL require the portal-settings
right and compare, in the installation-wide and the portal scope, the served plugin with the manifest in
storage: when the versions differ or the plugin appeared or vanished, it SHALL re-read that scope's list
on every server and notify the portal's clients. It SHALL answer 200 with the plugin as served
afterwards, or 404 "Plugin not found"; stored state, settings and CSP domains are untouched.

#### Scenario: Files replaced during a deployment

- **WHEN** a new version of an installation-wide plugin is put into storage and the owner calls `refreshWebPlugin`
- **THEN** the system answers 200 with the new `version` and `url`, and every portal lists the new version

#### Scenario: Nothing changed

- **WHEN** the owner calls `refreshWebPlugin` while the stored manifest matches the served one
- **THEN** the system answers 200 with the plugin unchanged and no client is notified

### Requirement: Plugin files served to the browser

The system SHALL serve a plugin's files from the storage folder named after it: `url` SHALL be the
absolute address of `plugin.js` with a `hash` query equal to the manifest `version` (none without a
version), and `cssUrl` the same address for `plugin.css` when shipped. A portal plugin's files SHALL be
stored per portal, an installation-wide plugin's once; the proxy marks both immutable for a year, so a
changed version needs a new `hash`.

#### Scenario: Script address

- **WHEN** a plugin `draw.io` version 1.2.0 is installed
- **THEN** its `url` ends in `/draw.io/plugin.js?hash=1.2.0`

### Requirement: Clients are told when a plugin changes

After a plugin is uploaded, switched, deleted or found changed by a refresh, the system SHALL send the
portal's connected clients a real-time event naming the plugin and whether it is enabled, so an open
interface reloads or unloads the plugin without a page reload. A failure to send SHALL NOT fail the call.

#### Scenario: Plugin switched off

- **WHEN** the owner switches a plugin off
- **THEN** every connected client of the portal receives the event with the plugin's `name` and `enabled` false

### Requirement: Audit trail of plugin changes

An upload, an update and a deletion SHALL each write one audit event — "Webplugin uploaded", "Webplugin
updated", "Webplugin deleted" — naming the plugin and carrying its `pluginName` and `version` as the
target. Reads, the refresh and the stored-state repair of undecryptable settings SHALL write none.

#### Scenario: Upload audited

- **WHEN** the owner uploads a plugin
- **THEN** the audit trail of `history-and-audit` shows "Webplugin uploaded" by the owner with the plugin's name

### Requirement: Plugins in backup and restore

A portal backup SHALL include the portal's uploaded plugin files and the stored plugin states (they are
portal settings), so a restore brings the uploaded plugins back with their state. Installation-wide plugin
files SHALL NOT be part of a portal backup or of a storage migration; after a restore on another
installation the portal's state rows for installation-wide plugins apply to whatever that installation
ships.

#### Scenario: Restore on the same installation

- **WHEN** a portal with an uploaded, enabled plugin is backed up and restored
- **THEN** `getWebPlugins` lists the plugin with `enabled` true and its settings
