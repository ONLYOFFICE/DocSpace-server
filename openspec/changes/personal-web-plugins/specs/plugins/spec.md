# Spec Delta

## ADDED Requirements

### Requirement: Switch for personal plugins

`getPortalSettings` SHALL report to a signed-in caller `plugins.personalAvailable`, true unless the installation
switched personal plugins off, and `plugins.personal`, true only while they are available and an administrator switched them on
for the portal, which a new portal has not. While plugins are on and `personal` is false, a guest's or member's
call with `personal=true` SHALL answer 403 "Access denied", and `getWebPlugins` SHALL list no personal plugin.

#### Scenario: New portal

- **WHEN** a signed-in member calls `getPortalSettings` on a new portal of an installation that did not configure personal plugins
- **THEN** the answer carries `plugins.personalAvailable` true and `plugins.personal` false

#### Scenario: Guest asks for personal plugins

- **WHEN** plugins, their upload and personal plugins are on and a Guest uploads a package, lists, reads, updates with a valid body or deletes with `personal=true`
- **THEN** the system answers 403 "Access denied"

#### Scenario: Personal plugins switched off for the portal

- **WHEN** plugins and their upload are on, an administrator switched personal plugins off, and a User uploads a package, lists, reads, updates with a valid body or deletes with `personal=true`
- **THEN** the system answers 403 "Access denied"
- **AND** the User's `getWebPlugins` without `personal` lists none of their personal plugins

#### Scenario: Personal plugins switched off for the installation

- **WHEN** the installation switched personal plugins off
- **THEN** `getPortalSettings` carries `plugins.personalAvailable` false and `plugins.personal` false, and `setPersonalWebPlugins` answers 403 "Access denied"

### Requirement: Switching personal plugins for a portal

`setPersonalWebPlugins` SHALL need the portal-settings right, and answer 403 "Access denied" without it, while
plugins are off or while the installation switched personal plugins off. It SHALL store `enabled` for the portal
and answer the plugin switches as `getPortalSettings` reports them. Switching off SHALL keep the files and states
of personal plugins, so switching on again brings them back.

#### Scenario: Member asks to switch

- **WHEN** a User or a Room admin calls `setPersonalWebPlugins`
- **THEN** the system answers 403 "Access denied" and `getPortalSettings` reports `plugins.personal` unchanged

#### Scenario: Off and on again

- **WHEN** a User holds an enabled personal plugin and a DocSpace admin switches personal plugins off and then on again
- **THEN** each call answers `personal` as switched
- **AND** while they are off the User's `getWebPlugins` lists none of the User's personal plugins, and `getMembersWebPlugins` still lists the plugin
- **AND** afterwards it lists the plugin with `enabled` true

### Requirement: Personal plugins of members for an administrator

`getMembersWebPlugins`, `setMemberWebPlugin` and `deleteMemberWebPlugin` SHALL need the portal-settings right
(403 "Access denied" otherwise, and while plugins are off). The list SHALL return the personal plugins of all
members, each with its member in `createBy`, `createByType` and `createByIsOwner`, and with empty `settings`.
Switching SHALL keep the member's settings, deleting SHALL drop the files and the state, and both SHALL answer
404 "Plugin not found" for a name the member lacks.

#### Scenario: DocSpace admin lists the plugins of members

- **WHEN** a User holds a personal plugin with saved settings and a DocSpace admin calls `getMembersWebPlugins`
- **THEN** the list holds the plugin with `createBy` the User, `createByType` `User`, `createByIsOwner` false, `enabled` true and empty `settings`

#### Scenario: Owner lists a plugin of their own

- **WHEN** the owner holds a personal plugin and calls `getMembersWebPlugins`
- **THEN** the list holds the plugin with `createBy` the owner, `createByType` `DocSpaceAdmin` and `createByIsOwner` true

#### Scenario: DocSpace admin switches a member's plugin off and on

- **WHEN** a DocSpace admin calls `setMemberWebPlugin` with `enabled` false, or with `enabled` true after false, for that plugin
- **THEN** the system answers 200 and the User's `getWebPlugin` with `personal=true` reports that `enabled` with the settings the User saved

#### Scenario: DocSpace admin deletes a member's plugin

- **WHEN** a DocSpace admin calls `deleteMemberWebPlugin` for that plugin twice
- **THEN** the first call answers 200, the second 404 "Plugin not found", and the User's `getWebPlugins` with `personal=true` no longer lists the plugin

#### Scenario: Plugin of a deleted member

- **WHEN** a User holding a personal plugin is deleted and the owner calls `getMembersWebPlugins`
- **THEN** the list holds the plugin with `createBy` carrying the User's id and `createByType` `Guest`
- **AND** `deleteMemberWebPlugin` with that id answers 200

#### Scenario: Unknown plugin of a member

- **WHEN** the owner calls `setMemberWebPlugin` for a name the User holds no plugin under
- **THEN** the system answers 404 "Plugin not found"

#### Scenario: Member asks for the plugins of members

- **WHEN** a User or a Room admin calls `getMembersWebPlugins`, `setMemberWebPlugin` or `deleteMemberWebPlugin`
- **THEN** the system answers 403 "Access denied" and the member's plugin stays on with its settings

### Requirement: Personal plugins

With `personal=true`, a signed-in member other than a guest SHALL upload, list, read, switch and delete
plugins of their own (`addWebPluginFromFile`, `getWebPlugins`, `getWebPlugin`, `updateWebPlugin`,
`deleteWebPlugin`) without the portal-settings right. A personal plugin SHALL carry `personal` true, and its
`settings` SHALL reach its owner only. With `system=true` too, an upload SHALL answer 400
"Changes to system plugins are prohibited" once the personal checks pass.

#### Scenario: User installs a plugin for themselves

- **WHEN** a User uploads a package with `personal=true`
- **THEN** the system answers 200 with `personal` true and `enabled` true
- **AND** the User's `getWebPlugins` lists the plugin with `personal` true
- **AND** another User's `getWebPlugins` does not list it and their `getWebPlugins` with `personal=true` is empty

#### Scenario: Personal and installation-wide together

- **WHEN** plugins, their upload and personal plugins are on and a User uploads a package with `personal=true` and `system=true`
- **THEN** the system answers 400 "Changes to system plugins are prohibited" and stores nothing

#### Scenario: Guest asks for a personal installation-wide plugin

- **WHEN** plugins and their upload are on and a Guest uploads a package with `personal=true` and `system=true`
- **THEN** the system answers 403 "Access denied" and stores nothing

#### Scenario: Personal plugin switched off

- **WHEN** a User calls `updateWebPlugin` with `personal=true`, `enabled` false and `settings` `{}` for their plugin
- **THEN** the system answers 200, the User's `getWebPlugin` with `personal=true` reports `enabled` false, and the portal's state of a plugin with that name is unchanged

#### Scenario: Unknown personal name

- **WHEN** a User reads, switches or deletes with `personal=true` a name none of their own personal plugins carries, the name of another User's personal plugin included
- **THEN** the system answers 404 "Plugin not found"

### Requirement: A plugin name belongs to the higher level

A personal upload SHALL answer 400 "A plugin with the same name or pluginName is already
installed for the whole portal" when a portal or installation-wide plugin has its `name` (any case) or
`pluginName`, and 400 "Plugin already exist" when another personal plugin of the same
member has its `pluginName`. A portal upload SHALL NOT be refused for a personal plugin; that plugin SHALL be
left out of its owner's list until the portal plugin is deleted.

#### Scenario: Name of a portal plugin

- **WHEN** the owner installed a portal plugin and a User uploads a package with the same `name` and `personal=true`
- **THEN** the system answers 400 "A plugin with the same name or pluginName is already installed for the whole portal"

#### Scenario: JavaScript object of a portal plugin

- **WHEN** the owner installed a portal plugin and a User uploads with `personal=true` a package with another `name` and the same `pluginName`
- **THEN** the system answers 400 "A plugin with the same name or pluginName is already installed for the whole portal"

#### Scenario: JavaScript object of another personal plugin

- **WHEN** a User holds a personal plugin and uploads with `personal=true` a package with another `name` and the same `pluginName`
- **THEN** the system answers 400 "Plugin already exist"

#### Scenario: Portal plugin shadows a personal one

- **WHEN** a User installed a personal plugin and the owner then uploads a portal plugin with the same `name`
- **THEN** the owner's upload answers 200
- **AND** the User's `getWebPlugins` lists that name once, with `personal` false
- **AND** the User's `getWebPlugins` with `personal=true` still returns the personal plugin

#### Scenario: Portal plugin deleted

- **WHEN** the owner deletes the portal plugin that shadowed a User's personal plugin
- **THEN** the User's `getWebPlugins` lists the personal plugin again, with the state and settings the User stored

### Requirement: Personal plugin count limit

A member SHALL hold at most 10 personal plugins by default: an upload of a new name beyond that SHALL answer
400 "The number of plugins exceeds the limit", while a re-upload of a name the member
holds is accepted unless a portal or installation-wide plugin has taken its `name` or `pluginName`. Personal
plugins SHALL NOT count toward the portal limit. Uploads of the same member that run at the same time can
together pass the limit.

#### Scenario: Limit reached

- **WHEN** a User holds 10 personal plugins and uploads a package with a new name and `personal=true`
- **THEN** the system answers 400 "The number of plugins exceeds the limit"

#### Scenario: Re-upload at the limit

- **WHEN** a User holds 10 personal plugins and uploads a new version of one of them with `personal=true`
- **THEN** the system answers 200 with the new version

### Requirement: Personal plugin files served to their owner

The files of a personal plugin SHALL be stored in its owner's folder of the `personalwebplugins` storage module,
private in a cloud storage, and served to the owner only: its `url`, `cssUrl` and assets SHALL answer 403 to any
other caller, an administrator or an anonymous one included. The `url` SHALL point to `plugin.js` in that
folder, with the same `hash` query as a portal plugin.

#### Scenario: Another member requests the script

- **WHEN** a User installed a personal plugin and another User, or the portal owner, requests its `url`
- **THEN** the system answers 403

#### Scenario: Owner requests the script

- **WHEN** the User who installed the personal plugin requests its `url`
- **THEN** the system answers 200 with the script

### Requirement: Content Security Policy domains of a personal plugin

A personal upload SHALL answer 400 "The cspDomains value lists an invalid address or is too long" when the
manifest's `cspDomains` is longer than 15360 characters, or one of its comma-separated entries, trimmed, is not an
http, https, ws or wss host, optionally with a `*.` wildcard below a name of two labels or more. An empty value
SHALL declare no domain. The domains SHALL reach only their owner's policy header, outside its
`frame-ancestors`, and never the portal's domains.

#### Scenario: Invalid entry

- **WHEN** a User uploads with `personal=true` a manifest whose `cspDomains` is `https://a.example,https://b.example; script-src *`
- **THEN** the system answers 400 "The cspDomains value lists an invalid address or is too long" and stores nothing

#### Scenario: Bare scheme or wildcard over a top-level domain

- **WHEN** a User uploads with `personal=true` a manifest whose `cspDomains` is `https:`, `*` or `*.com`
- **THEN** the system answers 400 "The cspDomains value lists an invalid address or is too long" and stores nothing

#### Scenario: Domains kept out of frame-ancestors

- **WHEN** a User's enabled personal plugin declares `https://a.example` and the User calls `getCspSettings` with `asc_personal_csp`
- **THEN** the `header` allows `https://a.example` in `frame-src` and does not list it in `frame-ancestors`

#### Scenario: Value at the length limit

- **WHEN** a User uploads with `personal=true` a manifest whose `cspDomains` lists one valid address repeated to exactly 15360 characters
- **THEN** the system answers 200

#### Scenario: Value over the length limit

- **WHEN** a User uploads with `personal=true` a manifest whose `cspDomains` lists valid addresses in 15361 characters
- **THEN** the system answers 400 "The cspDomains value lists an invalid address or is too long"

#### Scenario: Empty value

- **WHEN** a User uploads with `personal=true` a manifest whose `cspDomains` is empty
- **THEN** the system answers 200 and the plugin adds no domain to any policy

### Requirement: Size limit of a member's policy header

A personal upload, or a personal update with `enabled` true, after which the caller's personal header (the
portal's domains plus the caller's personal domains) would pass the size limit, 15 KB by default, SHALL keep its
change, answer 403 "CSP header size exceeds maximum allowed size of 15360 bytes." and set no
`asc_personal_csp`. A personal deletion or switch-off, and `getWebPlugins` without `personal`, SHALL still answer
200, and `getCspSettings` SHALL fall back to the portal's header.

#### Scenario: Personal upload over the limit

- **WHEN** a User uploads with `personal=true` a plugin whose `cspDomains`, together with the portal's domains and the User's other enabled, unshadowed personal domains, push the policy header over its size limit
- **THEN** the system answers 403 "CSP header size exceeds maximum allowed size of 15360 bytes." and sets no `asc_personal_csp`
- **AND** the User's `getWebPlugins` with `personal=true` lists the plugin with `enabled` true

#### Scenario: Plugin list over the limit

- **WHEN** the enabled personal plugins of a User that no higher-level plugin shadows declare domains that push the policy header over its size limit, and the User calls `getWebPlugins` without `personal`
- **THEN** the system answers 200 and sets no `asc_personal_csp`, deleting one the request carries, and `getCspSettings` with the cookie answers the portal's `header`

#### Scenario: Personal deletion over the limit

- **WHEN** a User whose personal header is over the size limit deletes one of their personal plugins with `personal=true`, and the rest still push the header over the limit
- **THEN** the system answers 200, the plugin is deleted and the answer sets no `asc_personal_csp`

### Requirement: Cookie of the personal Content Security Policy

`getWebPlugins` without `personal`, and an upload, update or deletion with `personal=true` that answers 200,
SHALL set the HttpOnly cookie `asc_personal_csp` to `1` while the caller has personal domains (of their enabled
personal plugins that no higher-level plugin shadows) within the size limit, and otherwise delete it from a
request that carries it, unless reading the caller's plugins fails. A response that clears the session cookie,
`logout` among others, SHALL delete it too.

#### Scenario: Cookie set by the plugin list

- **WHEN** a User with an enabled personal plugin that no higher-level plugin shadows, declaring `cspDomains` `https://a.example`, calls `getWebPlugins` without `personal`
- **THEN** the answer sets `asc_personal_csp` to `1`

#### Scenario: Cookie deleted after switching off

- **WHEN** that User switches the plugin off with `personal=true` and `settings` `{}` on a request that carries the cookie
- **THEN** the answer deletes `asc_personal_csp`

#### Scenario: Sign-out

- **WHEN** the User calls `logout` on a request that carries the cookie
- **THEN** the answer deletes `asc_personal_csp`

#### Scenario: Session no longer valid

- **WHEN** a request that carries the cookie and an expired or invalid session cookie calls an operation that needs a session
- **THEN** the answer deletes `asc_personal_csp` together with the session cookie

#### Scenario: Another device

- **WHEN** the User opens the portal in a browser that has no `asc_personal_csp` cookie
- **THEN** its pages carry no personal domain in their policy until a page loaded after that browser's first `getWebPlugins`

### Requirement: Policy header cached for a member

The calls that set `asc_personal_csp`, and `getCspSettings` for a session-cookie caller carrying it, SHALL
cache for the proxy, for a day, the header `getCspSettings` gives the caller, under a key of the session cookie
and the portal's member-header generation. Personal uploads, updates and deletions, portal plugin
uploads and deletions, the member-plugin calls, the personal switch and a policy save SHALL start a new
generation; a header cached before another change SHALL last at most a day.

#### Scenario: Unchanged header

- **WHEN** a member calls `getWebPlugins` again and the header to cache did not change
- **THEN** the copy is not written again and still expires a day after its last write

#### Scenario: Cookie set for another session

- **WHEN** a request carries `asc_personal_csp` and the session cookie of one member
- **THEN** the proxy serves the header cached for that session or asks `getCspSettings`, whatever the value of `asc_personal_csp`

#### Scenario: Session cookie sent twice

- **WHEN** a request carries `asc_auth_key` twice, in any letter case, with `asc_personal_csp`
- **THEN** the proxy asks `getCspSettings`, which answers for the session it authenticated and caches nothing for it

#### Scenario: New generation

- **WHEN** a member uploads, switches or deletes a personal plugin, the owner uploads or deletes a portal plugin, switches personal plugins or a member's personal plugin, or saves the portal's domains with `configureCsp`
- **THEN** the proxy finds no member header cached before, and takes each member's policy from `getCspSettings`, which caches it under the new generation

#### Scenario: Portal removed by the retention job

- **WHEN** the retention job removes a portal
- **THEN** the headers cached for its members expire a day after their last write

#### Scenario: Plugins switched off for the installation

- **WHEN** plugins are switched off for the installation
- **THEN** the plugin calls answer 403 and start no new generation, so a cached member header expires a day after its last write unless a new generation starts first

## MODIFIED Requirements

### Requirement: Installation-wide plugin switches

`getPortalSettings` SHALL report to a signed-in caller the installation switches `plugins.enabled`, `plugins.upload`,
`plugins.delete` (false unless set) and `plugins.personalAvailable` (true unless set), and `plugins.personal`.
While plugins are off, every `settings/webplugins` and `settings/personalwebplugins` operation SHALL answer 403
"Access denied", and so SHALL an upload while upload is off and a portal plugin deletion while delete is off.
No tariff or edition gates plugins.

#### Scenario: Signed-in member reads the switches

- **WHEN** any signed-in member calls `getPortalSettings`
- **THEN** the answer carries `plugins` with `enabled`, `upload`, `delete` and `personalAvailable` as the installation configured them, and `personal` as the portal set it

#### Scenario: Plugins switched off for the installation

- **WHEN** plugins are not enabled and the owner lists, reads, uploads a package, updates with a valid body, refreshes or deletes a plugin, with or without `personal=true` where the operation takes it
- **THEN** the system answers 403 "Access denied"

#### Scenario: Upload or delete switched off

- **WHEN** plugins are on and upload is off and the owner uploads a package, with or without `personal=true`, or delete is off and the owner deletes a portal plugin
- **THEN** the system answers 403 "Access denied"

#### Scenario: Delete switched off for a personal plugin

- **WHEN** plugins and personal plugins are on, delete is off and a User deletes their personal plugin with `personal=true`
- **THEN** the system answers 200

### Requirement: Listing the plugins of a portal

`getWebPlugins` SHALL return, to any signed-in member without a settings right, the installation-wide
plugins, then the portal's own that none of them shadows (100 at most in all), then the caller's personal ones
that no plugin above shadows; no paging. A set `enabled` SHALL keep only the plugins in that state. Each entry
SHALL carry the manifest data, `createBy`, `createOn`, the state (`enabled`, `settings`), `system`, `personal`,
`url` and `cssUrl`. Anonymous: 401.

#### Scenario: Member lists an empty portal

- **WHEN** a User calls `getWebPlugins` on a portal where nothing is installed and the installation ships no plugins
- **THEN** the system answers 200 with an empty list

#### Scenario: Fields of an entry

- **WHEN** a plugin is listed
- **THEN** the entry carries `name`, `version`, `minDocSpaceVersion`, `description`, `license`, `author`, `homePage`, `pluginName`, `scopes`, `image`, `cspDomains`, `nameLocale`, `descriptionLocale` and `runtime` from its manifest

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

`addWebPluginFromFile` SHALL require the portal-settings right unless `personal=true` (owner, DocSpace admin;
others 403 "Access denied", anonymous 401) and a form with one non-empty `.zip` of at most 5 MB (400 "No
input file", "Too many input files", "Wrong file extension" or "File size exceeds limit"; 403 when the body is
not a form). It SHALL store the package, without the manifest's `enabled`, `settings` and `cssUrl` unless
`system=true`, switch it on and answer 200 with it.

#### Scenario: Owner uploads a valid package

- **WHEN** the owner posts one `.zip` holding a manifest and the entry script as `multipart/form-data`
- **THEN** the system answers 200 with the plugin, `enabled` true, `system` false, `createBy` the owner and a `url` ending in `plugin.js?hash=<version>`

#### Scenario: Package too large

- **WHEN** the uploaded file is larger than 5 MB
- **THEN** the system answers 400 "File size exceeds limit"

#### Scenario: Two files in one request

- **WHEN** the form carries two files
- **THEN** the system answers 400 "Too many input files"

#### Scenario: Manifest declares a stylesheet address

- **WHEN** the owner uploads a package without `plugin.css` whose manifest declares a `cssUrl`, without `system=true`
- **THEN** the system answers 200 with no `cssUrl`

#### Scenario: Personal upload over the header size limit

- **WHEN** a User uploads with `personal=true` a plugin whose domains push the User's policy header over its size limit
- **THEN** the system answers 403 "CSP header size exceeds maximum allowed size of 15360 bytes." and the plugin stays installed and on

### Requirement: Plugin name and uniqueness

The manifest `name` SHALL consist of lower-case letters, digits, `_`, `.` and `-` and not start with a
dot (it is also the storage folder); `pluginName` SHALL be a JavaScript identifier. Either failing SHALL
answer 400 "Wrong plugin archive. Wrong plugin name". A portal package SHALL be refused with 400 "Plugin
already exist" when an installation-wide plugin has its `name` (any case) or its `pluginName`, or another
portal plugin with a different `name` has its `pluginName`.

#### Scenario: Upper-case name

- **WHEN** the manifest says `"name": "MyPlugin"`
- **THEN** the system answers 400 "Wrong plugin archive. Wrong plugin name"

#### Scenario: Same JavaScript object as another plugin

- **WHEN** a package named `b` declares the `pluginName` an installed plugin `a` already registers
- **THEN** the system answers 400 "Plugin already exist"

#### Scenario: Name taken by an installation-wide plugin

- **WHEN** a portal package carries the `name` of a plugin that ships with the installation
- **THEN** the system answers 400 "Plugin already exist"

### Requirement: Re-uploading a plugin under the same name

A package whose `name` is already installed in the same scope SHALL replace the stored files of that plugin,
keep the `settings` string stored for the name (a personal plugin's in its owner's settings), switch the plugin
on, and answer the new manifest; `url` SHALL change with the `version` through its `hash` query. The
replacement SHALL be accepted even at the plugin limit, but a personal one SHALL answer 400 once a portal or
installation-wide plugin has its `name` or `pluginName`.

#### Scenario: New version uploaded

- **WHEN** the owner uploads `example-plugin` version 1.0.0, saves settings for it, and uploads version 1.1.0 under the same name
- **THEN** the second answer carries version 1.1.0, `enabled` true, the saved `settings` and a `url` ending in `?hash=1.1.0`

#### Scenario: Same version uploaded again

- **WHEN** the same version is uploaded twice
- **THEN** the files are replaced and the `url` is unchanged, so a browser may keep serving the cached script

#### Scenario: Personal plugin re-uploaded after a portal plugin took its name

- **WHEN** a User holds a personal plugin `x`, the owner then installs a portal plugin `x`, and the User uploads `x` again with `personal=true`
- **THEN** the system answers 400 "A plugin with the same name or pluginName is already installed for the whole portal"

### Requirement: Portal plugin count limit

A portal SHALL hold at most 100 plugins, the installation-wide ones counted in: an upload that would add
a new name beyond that SHALL answer 400 "The number of plugins exceeds the limit",
while a re-upload of an installed name is accepted. The portal and installation-wide part of the list SHALL
never return more than that number; the caller's personal plugins come on top.

#### Scenario: Limit reached

- **WHEN** 100 plugins are available to the portal and the owner uploads a package with a new name
- **THEN** the system answers 400 "The number of plugins exceeds the limit"

#### Scenario: Personal plugins on top of a full portal

- **WHEN** 100 plugins are available to the portal and a User with a personal plugin calls `getWebPlugins`
- **THEN** the list holds the 100 portal and installation-wide plugins and the User's personal plugin

### Requirement: Installation-wide plugins

An installation-wide plugin SHALL be available to every portal with `system` true, an empty `createBy` and
a zero `createOn`, and SHALL win its `name` over a portal plugin; its state and `settings` SHALL be stored per
portal. `addWebPluginFromFile` with `system=true`, unless `personal=true`, SHALL store the package
installation-wide on a self-hosted installation only, else 403 "Changes to system plugins are prohibited", and
SHALL refuse a `name` or `pluginName` the uploading portal has (400).

#### Scenario: Installation-wide plugin on two portals

- **WHEN** portal A switches an installation-wide plugin off and portal B does not
- **THEN** `getWebPlugin` answers `enabled` false on A and the plugin's own state on B

#### Scenario: Hosted portal asks for an installation-wide install

- **WHEN** the owner of a hosted portal uploads with `system=true`
- **THEN** the system answers 403 "Changes to system plugins are prohibited" and stores nothing

#### Scenario: Self-hosted installation-wide install

- **WHEN** the owner of a self-hosted portal uploads with `system=true`
- **THEN** the system answers 200 with `system` true and the plugin appears on every portal of the installation

#### Scenario: Installation-wide install asked for with personal

- **WHEN** plugins, their upload and personal plugins are on and the owner of a self-hosted or a hosted portal uploads a package with `system=true` and `personal=true`
- **THEN** the system answers 400 "Changes to system plugins are prohibited" and stores nothing

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

#### Scenario: Personal plugin switched on over the header size limit

- **WHEN** a User switches on with `personal=true` a personal plugin whose domains push the User's policy header over its size limit
- **THEN** the system answers 403 "CSP header size exceeds maximum allowed size of 15360 bytes." and the User's `getWebPlugin` with `personal=true` reports `enabled` true

### Requirement: Stored state of a plugin

The portal, and for a personal plugin its owner's settings row, SHALL keep one state per plugin name only while
the plugin is on or has settings; switching it off with no settings SHALL remove the state. With no stored state,
an installation-wide plugin SHALL be reported with its manifest's `enabled` and `settings`, a portal or personal
one off with no settings. Settings that cannot be decrypted SHALL make the read report the plugin off with empty
settings.

#### Scenario: Installation-wide plugin never touched

- **WHEN** a portal reads an installation-wide plugin it never switched
- **THEN** `enabled` and `settings` are whatever the plugin's manifest declares, off and empty for a manifest that declares nothing

#### Scenario: Undecryptable settings

- **WHEN** the stored settings of a plugin were encrypted by another installation key
- **THEN** the plugin is listed with `enabled` false and empty `settings`

#### Scenario: Uploaded manifest declares the plugin on

- **WHEN** the owner uploads, without `system=true`, a package whose manifest declares `"enabled": true` and switches the plugin off with empty `settings`
- **THEN** `getWebPlugin` reports `enabled` false

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

### Requirement: Refreshing a plugin replaced in storage

`refreshWebPlugin` SHALL require the portal-settings right and compare, in the installation-wide and the
portal scope, the served plugin with the manifest in storage: when the versions differ or the plugin appeared
or vanished, it SHALL re-read that scope's list on every server and notify the portal's clients. It SHALL
answer 200 with the plugin as served afterwards, or 404 "Plugin not found". Without it, each server SHALL show
files replaced in storage within an hour, when its cache expires.

#### Scenario: Files replaced during a deployment

- **WHEN** a new version of an installation-wide plugin is put into storage and the owner calls `refreshWebPlugin`
- **THEN** the system answers 200 with the new `version` and `url`, and every portal lists the new version
- **AND** the plugin's stored state, settings and CSP domains are untouched

#### Scenario: Nothing changed

- **WHEN** the owner calls `refreshWebPlugin` while the stored manifest matches the served one
- **THEN** the system answers 200 with the plugin unchanged and no client is notified

#### Scenario: Files replaced without a refresh

- **WHEN** a new version of a plugin is put into storage and nobody calls `refreshWebPlugin`
- **THEN** `getWebPlugins` lists the new version within an hour, and no client is notified

### Requirement: Clients are told when a plugin changes

After a plugin is uploaded, switched, deleted or found changed by a refresh, the system SHALL send a real-time
event naming the plugin and whether it is enabled: for a portal or installation-wide plugin to the portal's
connected clients, for a personal plugin `s:change-personal-web-plugin` to its owner's connected clients only.
`setPersonalWebPlugins` SHALL send `s:change-personal-web-plugins` with `enabled` to the portal's connected
clients. A failure to send SHALL NOT fail the call.

#### Scenario: Plugin switched off

- **WHEN** the owner switches a plugin off
- **THEN** every connected client of the portal receives the event with the plugin's `name` and `enabled` false

#### Scenario: Personal plugin switched off

- **WHEN** a User switches their personal plugin off with `personal=true`
- **THEN** the User's connected clients receive `s:change-personal-web-plugin` with the plugin's `name` as `webPluginName` and `enabled` false
- **AND** no other client of the portal receives an event for it

#### Scenario: Personal plugins switched off for the portal

- **WHEN** the owner switches personal plugins off with `setPersonalWebPlugins`
- **THEN** every connected client of the portal receives `s:change-personal-web-plugins` with `enabled` false

### Requirement: Audit trail of plugin changes

An upload, an update and a deletion, personal ones included, SHALL each write "Webplugin uploaded", "Webplugin
updated" or "Webplugin deleted", naming the plugin, with its `pluginName` and `version` as the target.
`setPersonalWebPlugins` SHALL write "Personal web plugins enabled" or "disabled", and `setMemberWebPlugin` and
`deleteMemberWebPlugin` "Personal web plugin of a user updated" or "deleted", with the member as the target.
Reads and the refresh SHALL write none.

#### Scenario: Upload audited

- **WHEN** the owner uploads a plugin
- **THEN** the audit trail of `history-and-audit` shows "Webplugin uploaded" by the owner with the plugin's name

#### Scenario: Personal upload audited

- **WHEN** a User uploads a plugin with `personal=true`
- **THEN** the audit trail shows "Webplugin uploaded" by that User with the plugin's name, as for a portal plugin

### Requirement: Plugins in backup and restore

A portal backup SHALL include the uploaded plugin files, personal ones included, and the plugin states, so a
restore brings them back. A restore or transfer SHALL keep a member's personal files private under the member's
new user id, and skip those of a member it does not restore unless it restores a dump.
Installation-wide plugin files SHALL NOT be in a portal backup or a storage migration; restored elsewhere, the
portal's states for them apply to whatever that installation ships.

#### Scenario: Restore on the same installation

- **WHEN** a portal with an uploaded, enabled plugin is backed up and restored
- **THEN** `getWebPlugins` lists the plugin with `enabled` true and its settings

#### Scenario: Restore that gives a member a new id

- **WHEN** a portal whose User holds an enabled personal plugin is backed up and restored on the same installation, and the restore gives the User a new user id
- **THEN** that User's `getWebPlugins` with `personal=true` lists the plugin with `enabled` true and its settings
