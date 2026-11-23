# Spec Delta

## ADDED Requirements

### Requirement: Switch for personal plugins

`getSettings` SHALL report to a signed-in caller `plugins.personal`, true unless the installation switched
personal plugins off. While it is off, and for a guest, every `settings/webplugins` operation sent with
`personal=true` SHALL answer 403 "Personal plugins disabled", and `getWebPlugins` without it SHALL list no
personal plugin.

#### Scenario: Default installation

- **WHEN** a signed-in member calls `getSettings` on an installation that did not configure personal plugins
- **THEN** the answer carries `plugins.personal` true

#### Scenario: Guest asks for personal plugins

- **WHEN** a Guest uploads a package with `personal=true`
- **THEN** the system answers 403 "Personal plugins disabled"

#### Scenario: Personal plugins switched off

- **WHEN** the installation switched personal plugins off and a User lists, reads, uploads, updates or deletes with `personal=true`
- **THEN** the system answers 403 "Personal plugins disabled"

### Requirement: Personal plugins

With `personal=true`, a signed-in member other than a guest SHALL upload, list, read, switch and delete
plugins of their own (`addWebPluginFromFile`, `getWebPlugins`, `getWebPlugin`, `updateWebPlugin`,
`deleteWebPlugin`) without the portal-settings right. A personal plugin SHALL carry `personal` true, be listed
for its owner only, and keep its state and `settings` for its owner only. `system=true` with `personal=true`
SHALL answer 400 "Changes to system plugins are prohibited".

#### Scenario: User installs a plugin for themselves

- **WHEN** a User uploads a package with `personal=true`
- **THEN** the system answers 200 with `personal` true and `enabled` true
- **AND** the User's `getWebPlugins` lists the plugin with `personal` true
- **AND** another User's `getWebPlugins` does not list it and their `getWebPlugins` with `personal=true` is empty

#### Scenario: Personal and installation-wide together

- **WHEN** a member uploads a package with `personal=true` and `system=true`
- **THEN** the system answers 400 "Changes to system plugins are prohibited" and stores nothing

#### Scenario: Personal plugin switched off

- **WHEN** a User calls `updateWebPlugin` with `personal=true`, `enabled` false and `settings` `{}` for their plugin
- **THEN** the system answers 200, the User's `getWebPlugin` with `personal=true` reports `enabled` false, and the portal's state of a plugin with that name is unchanged

#### Scenario: Unknown personal name

- **WHEN** a User reads, switches or deletes with `personal=true` a name none of their personal plugins carries
- **THEN** the system answers 404 "Plugin not found"

### Requirement: A plugin name belongs to the higher level

A personal upload SHALL answer 400 "Wrong plugin archive. A plugin with the same name or pluginName is already
installed for the whole portal" when a portal or installation-wide plugin has its `name` (any case) or `pluginName`, and 400
"Wrong plugin archive. Plugin already exist" when another personal plugin of the same member has its
`pluginName`. A portal upload SHALL NOT be refused for a personal plugin; that plugin SHALL be left out of its
owner's list until the portal plugin is deleted.

#### Scenario: Name of a portal plugin

- **WHEN** the owner installed a portal plugin and a User uploads a package with the same `name` and `personal=true`
- **THEN** the system answers 400 "Wrong plugin archive. A plugin with the same name or pluginName is already installed for the whole portal"

#### Scenario: Portal plugin shadows a personal one

- **WHEN** a User installed a personal plugin and the owner then uploads a portal plugin with the same `name`
- **THEN** the owner's upload answers 200
- **AND** the User's `getWebPlugins` lists that name once, with `personal` false
- **AND** the User's `getWebPlugins` with `personal=true` still returns the personal plugin

#### Scenario: Portal plugin deleted

- **WHEN** the owner deletes the portal plugin that shadowed a User's personal plugin
- **THEN** the User's `getWebPlugins` lists the personal plugin again, with the state and settings the User stored

### Requirement: Personal plugin count limit

A member SHALL hold at most 10 personal plugins: an upload of a new name beyond that SHALL answer 400 "Wrong
plugin archive. The number of plugins exceeds the limit", while a re-upload of a name the member holds is
accepted. Personal plugins SHALL NOT count toward the portal limit. Uploads of the same member that run at the
same time can together pass the limit.

#### Scenario: Limit reached

- **WHEN** a User holds 10 personal plugins and uploads a package with a new name and `personal=true`
- **THEN** the system answers 400 "Wrong plugin archive. The number of plugins exceeds the limit"

#### Scenario: Re-upload at the limit

- **WHEN** a User holds 10 personal plugins and uploads a new version of one of them with `personal=true`
- **THEN** the system answers 200 with the new version

### Requirement: Personal plugin files served to their owner

The files of a personal plugin SHALL be served to its owner only: its `url`, `cssUrl` and assets SHALL answer
403 to any other caller, an administrator or an anonymous one included. The `url` of a personal plugin SHALL
point to `plugin.js` in a folder of its owner, with the same `hash` query as a portal plugin.

#### Scenario: Another member requests the script

- **WHEN** a User installed a personal plugin and another User, or the portal owner, requests its `url`
- **THEN** the system answers 403

#### Scenario: Owner requests the script

- **WHEN** the User who installed the personal plugin requests its `url`
- **THEN** the system answers 200 with the script

## MODIFIED Requirements

### Requirement: Installation-wide plugin switches

The installation SHALL carry three switches — plugins enabled, upload allowed, delete allowed — that
`getSettings` SHALL report to a signed-in caller as `plugins.enabled`, `plugins.upload` and
`plugins.delete` (false when not configured). While plugins are not enabled every `settings/webplugins`
operation SHALL answer 403 "Plugins disabled"; an upload while upload is off and a portal plugin deletion
while delete is off SHALL answer 403 "Forbidden action". No tariff or edition gates plugins.

#### Scenario: Signed-in member reads the switches

- **WHEN** any signed-in member calls `getSettings`
- **THEN** the answer carries `plugins` with `enabled`, `upload` and `delete` as the installation configured them

#### Scenario: Plugins switched off for the installation

- **WHEN** plugins are not enabled and a member lists, reads, uploads, updates, refreshes or deletes a plugin
- **THEN** the system answers 403 "Plugins disabled"

#### Scenario: Upload or delete switched off

- **WHEN** upload is off and the owner uploads a package, or delete is off and the owner deletes a plugin
- **THEN** the system answers 403 "Forbidden action"

#### Scenario: Delete switched off for a personal plugin

- **WHEN** delete is off and a User deletes their personal plugin with `personal=true`
- **THEN** the system answers 200

### Requirement: Listing the plugins of a portal

`getWebPlugins` SHALL return, to any signed-in member without a settings right, the installation-wide
plugins first, then the portal's own, dropping a portal plugin whose name an installation-wide plugin already
uses (100 at most), then the caller's personal ones; no paging. A set `enabled` SHALL keep only the plugins
in that state. Each entry SHALL carry the manifest data, `createBy`, `createOn`, the state
(`enabled`, `settings`), `system`, `personal`, `url` and `cssUrl`. Anonymous: 401.

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

#### Scenario: Personal plugins after the portal ones

- **WHEN** a User with a personal plugin calls `getWebPlugins` on a portal with a portal plugin
- **THEN** the portal plugin comes first with `personal` false and the personal plugin last with `personal` true

### Requirement: Reading one plugin by name

`getWebPlugin` SHALL look the plugin up over the portal and installation-wide plugins `getWebPlugins` returns,
or with `personal=true` over the caller's personal plugins, by its manifest `name` matched without regard to
case — not by the display name and not by `pluginName` — and SHALL answer 404 "Plugin not found" for a name
that is not installed there. Any signed-in member may read it; anonymous 401.

#### Scenario: Name in another case

- **WHEN** a member reads `Example-Plugin` while the installed manifest name is `example-plugin`
- **THEN** the system answers 200 with that plugin

#### Scenario: Unknown name

- **WHEN** a member reads a name no installed plugin carries
- **THEN** the system answers 404 "Plugin not found"

#### Scenario: Personal plugin read without the flag

- **WHEN** a User reads the name of their personal plugin, which no portal plugin carries, without `personal=true`
- **THEN** the system answers 404 "Plugin not found"

### Requirement: Uploading a plugin package

`addWebPluginFromFile` SHALL require the portal-settings right unless `personal=true` (owner or DocSpace admin;
others 403 "Access denied", anonymous 401) and a form with exactly one non-empty `.zip` file of at most 5 MB
(400 "No input file", "Too many input files", "Wrong file extension" or "File size exceeds limit"; 403 when
the body is not a form). It SHALL store the package, switch the plugin on, audit the upload and answer 200 with
the plugin as `getWebPlugin` returns it.

#### Scenario: Owner uploads a valid package

- **WHEN** the owner posts one `.zip` holding a manifest and the entry script as `multipart/form-data`
- **THEN** the system answers 200 with the plugin, `enabled` true, `system` false, `createBy` the owner and a `url` ending in `plugin.js?hash=<version>`

#### Scenario: Package too large

- **WHEN** the uploaded file is larger than 5 MB
- **THEN** the system answers 400 "File size exceeds limit"

#### Scenario: Two files in one request

- **WHEN** the form carries two files
- **THEN** the system answers 400 "Too many input files"

### Requirement: Switching a plugin on or off and storing its settings

`updateWebPlugin` SHALL require the portal-settings right unless `personal=true` (403 "Access denied",
anonymous 401), find the plugin by `name` in any case (404 "Plugin not found") and replace the stored state
with the body: `enabled` and the required `settings` string of at most 4096 characters (400 otherwise).
`settings` SHALL be stored encrypted and returned verbatim; an empty string SHALL mean no settings. The call
SHALL answer 200 with no body and record the change in the audit trail.

#### Scenario: Owner disables an uploaded plugin

- **WHEN** the owner uploads a plugin and calls `updateWebPlugin` with `enabled` false and `settings` `{}`
- **THEN** the system answers 200 and `getWebPlugin` reports `enabled` false

#### Scenario: Settings saved and read back

- **WHEN** the owner saves `settings` `{"theme":"dark"}` with `enabled` true
- **THEN** `getWebPlugin` and `getWebPlugins` return `settings` `{"theme":"dark"}` exactly

#### Scenario: Settings too long

- **WHEN** `settings` is 4097 characters long
- **THEN** the system answers 400 and the stored state is unchanged

#### Scenario: Room admin tries to change a plugin

- **WHEN** a Room admin, a User or a Guest calls `updateWebPlugin` without `personal=true`
- **THEN** the system answers 403 "Access denied"

### Requirement: Content Security Policy domains of a plugin

A plugin manifest MAY declare `cspDomains`, a comma-separated list of origins. Uploading or switching a portal
plugin on SHALL add them to the portal's Content Security Policy domains; switching it off or deleting it
SHALL remove them. A personal plugin's domains SHALL never reach the portal's domains. When adding them would
push the policy header over 15 KB the call SHALL answer 403 "CSP header size exceeds maximum allowed size of
15360 bytes." after the plugin state was already saved.

#### Scenario: Plugin with external domains enabled

- **WHEN** a plugin declaring `cspDomains` `https://a.example,https://b.example` is uploaded
- **THEN** both origins appear in the portal's CSP settings and are gone again after the plugin is switched off

#### Scenario: Header limit exceeded

- **WHEN** switching a plugin on would make the CSP header longer than 15 KB
- **THEN** the system answers 403 with the size message and the plugin is nevertheless stored as on

#### Scenario: Personal plugin with external domains

- **WHEN** a User uploads a plugin declaring `cspDomains` `https://a.example` with `personal=true`
- **THEN** the portal's CSP settings do not contain `https://a.example`

### Requirement: Deleting a plugin

`deleteWebPlugin` SHALL require the portal-settings right (403 "Access denied", anonymous 401) and the
delete switch unless `personal=true`, find the plugin by `name` in any case (404 "Plugin not found", also
when its folder is missing from storage), refuse an installation-wide plugin on a hosted installation with
403 "Changes to system plugins are prohibited", and otherwise delete the folder, drop the state and settings,
remove the plugin's CSP domains, write the audit event and answer 200.

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

#### Scenario: Personal plugin deleted under a portal plugin of the same name

- **WHEN** a User deletes with `personal=true` their personal plugin that a portal plugin of the same name shadows
- **THEN** the system answers 200 and the portal plugin stays installed with its state

### Requirement: Portal plugin count limit

A portal SHALL hold at most 100 plugins, the installation-wide ones counted in: an upload that would add
a new name beyond that SHALL answer 400 "Wrong plugin archive. The number of plugins exceeds the limit",
while a re-upload of an installed name is accepted. The portal and installation-wide part of the list SHALL
never return more than that number; the caller's personal plugins come on top.

#### Scenario: Limit reached

- **WHEN** 100 plugins are available to the portal and the owner uploads a package with a new name
- **THEN** the system answers 400 "Wrong plugin archive. The number of plugins exceeds the limit"

#### Scenario: Personal plugins on top of a full portal

- **WHEN** 100 plugins are available to the portal and a User with a personal plugin calls `getWebPlugins`
- **THEN** the list holds the 100 portal and installation-wide plugins and the User's personal plugin

### Requirement: Clients are told when a plugin changes

After a portal or installation-wide plugin is uploaded, switched, deleted or found changed by a refresh, the
system SHALL send the portal's connected clients a real-time event naming the plugin and whether it is enabled,
so an open interface reloads or unloads the plugin without a page reload. A failure to send SHALL NOT fail the
call. A personal plugin change SHALL send no event; the owner's other pages see it after a reload.

#### Scenario: Plugin switched off

- **WHEN** the owner switches a plugin off
- **THEN** every connected client of the portal receives the event with the plugin's `name` and `enabled` false

#### Scenario: Personal plugin switched off

- **WHEN** a User switches their personal plugin off with `personal=true`
- **THEN** no connected client of the portal receives an event for it
