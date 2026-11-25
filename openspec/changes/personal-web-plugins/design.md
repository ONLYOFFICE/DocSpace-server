# Design

## Context

See proposal.md for the why. The design builds on these requirements:
- `plugins` "Installation-wide plugins": the installation-wide plugin already wins its name over a portal plugin.
- `plugins` "Stored state of a plugin": one state row per name, encrypted `settings`.
- `plugins` "Plugin name and uniqueness": `name` is the storage folder; `pluginName` is the global JavaScript
  object the client registers.
- `plugins` "Plugin files served to the browser": files are served from the `webplugins` storage module.
- `plugins` "Plugins in backup and restore": a portal backup carries the `webplugins` module.
- `portal-settings` "Configuring the Content Security Policy": `configureCsp` stores the portal domains and
  caches the header the proxy serves.
- `portal-settings` "Domains of the Content Security Policy": the address check that personal `cspDomains`
  narrow further.
- `portal-settings` "Reading the Content Security Policy": `getCspSettings`, which the proxy asks when it has no
  cached header.

The web client loads exactly what `getWebPlugins` returns and keys every plugin by `name` and `pluginName`,
so one page must never see two plugins with the same `name` or `pluginName`.

## Goals / Non-Goals

**Goals:**
- A third level below installation-wide and portal plugins, with the same rule that already holds between the
  two: a name belongs to the higher level.
- A personal plugin cannot change the portal state, the portal Content Security Policy or the socket events of
  the portal. Its changes do write the portal's audit events, as a portal plugin's do.
- The administrators of a portal decide whether its members may install plugins, see what they installed, and
  can switch a member's plugin off or delete it.
- No schema change, no migration.

**Non-Goals:**
- A "promote to portal" action, or a download of a member's plugin code by an administrator.
- Cleaning up the plugins of a deleted user, charging them to the user's quota.
- Switching between a personal copy and a portal plugin without a page reload.

## Decisions

### Two switches: the installation's and the portal's

The installation key `plugins:personal` (on unless set) decides whether personal plugins exist at all;
`getPortalSettings` reports it as `plugins.personalAvailable`. A portal setting of its own
(`PersonalWebPluginsSettings`, off for every portal until an administrator switches it on with
`setPersonalWebPlugins`) decides whether they run on the portal; `plugins.personal` reports both together. Off
hides the personal plugins of all members at once: they are neither listed nor loaded, their domains leave the
member headers, and the calls with `personal=true` answer 403. Files and states stay, so switching on brings
them back. The switch sends `s:change-personal-web-plugins` with `enabled` to the quota room every open client
joins, so open pages load or unload the personal plugins without a reload.

There is no state "blocked by the administrator" for one plugin: a member would get round it by uploading the
plugin under another name. The administrator's levers are the portal switch, a member's plugin switched off or
deleted, and the member's account itself.

### Storage: a module of its own

Personal files go to the storage module `personalwebplugins`, under `<userId>/<name>/`, with the manifest as
`config.json`. The module is declared in `storage.json` with a private ACL, so a cloud storage keeps the files
private, and a restore or a transfer, which saves with the module's default ACL, keeps them private as well.
The portal scan of `webplugins` no longer passes personal files at all. The storage handler of the module
serves a path only to the signed-in user whose id is its first segment.

Alternatives: a `.personal` folder inside `webplugins` (the first builds of this branch: the module ACL of
`webplugins` is public-read, so a restore to a cloud storage made the files public, and the handler had to tell
personal paths apart by name); the user's documents (mixes code with documents); browser storage (bound to one
device).

### State: the member's own settings row

The state of personal plugins is the same settings object as the portal's, saved for the user. The `settings`
encryption, the row removal for "off and empty", and the settings a re-upload keeps behave the same way. An
administrator's switch-off writes the member's row and keeps the member's settings.

### Manifest fields the server owns

A portal or personal upload stores the manifest with the server's `createBy` and `createOn` and without
`enabled`, `settings` and `cssUrl`, and every read of the storage drops them again for a portal or personal
plugin, so a manifest stored by an earlier upload applies none of them either: the state lives in the settings
rows, and `cssUrl` is derived on every read from a shipped `plugin.css`. An installation-wide plugin keeps the
three fields, so its manifest stays the default state of the other portals (P1 stays open for it).

### Order of the checks

For a personal upload the controller checks the plugin switches, the personal switches and the guest rule before
it reads the form, and the manager repeats them before it looks at `system`. A guest who sends
`personal=true&system=true` gets 403; a member gets 400 "Changes to system plugins are prohibited".

### Shadowing is computed at read time

`getWebPlugins` builds installation-wide → portal → personal and drops a personal plugin whose `name` or
`pluginName` is already in the list. Nothing is stored for the shadow, so deleting the portal plugin brings
the personal copy back with its own state. A portal upload never looks at personal copies, so a member cannot
block an administrator.

Alternatives:
- Personal wins: an administrator could not impose a version or switch a plugin off.
- Block the portal upload: members would block the administrator.
- Delete the personal copies on a portal upload: settings are lost, and the copy may be another plugin with
  the same name.

### The personal plugins of members for an administrator

`getMembersWebPlugins` lists the top-level folders of the `personalwebplugins` module, one per member who ever
uploaded, and reads each member's plugins through the per-member manifest cache, so a repeated call costs no
storage reads. Each entry carries the member as `createBy` and the member's `enabled`, and never the member's
`settings`, which may hold the member's own keys. `setMemberWebPlugin` writes the member's settings row, keeping
the member's settings; `deleteMemberWebPlugin` deletes the folder and the state. Both send
`s:change-personal-web-plugin` to the member's private room and write an audit event with the member as the
target. The list includes the plugins of deleted members, so an administrator can remove them.

### Personal domains: checked as hosts, kept out of frame-ancestors

Every entry of a personal `cspDomains`, trimmed, has to be an http, https, ws or wss host, with an optional
`*.` wildcard below a name of two labels or more, besides passing the address check of `configureCsp`; the
whole value is at most 15360 characters. A bare scheme (`https:`), a lone `*`, a wildcard over a top-level
domain and a keyword are refused with 400, since each would open the member's pages to any site. The domains go
into every fetch directive of the member's header but not into `frame-ancestors`: a plugin that shows another
site in a frame does not need that site to frame the portal.

### Personal CSP: a header per session, under a key the proxy derives itself

The portal CSP header is cached per host (`csp:{host}`) and read by the proxy for everyone, so the personal
domains cannot go there. A member's header (portal domains plus their personal ones) is cached for a day under
`csp:{host}:personal:{generation}:{hash}`, where `hash` is the SHA-256 of the session cookie `asc_auth_key` of
the request, as the proxy receives it once percent-decoded. The proxy computes the same key itself from the
session cookie of the page request, so a cookie set from a neighbouring site can point a browser at no header but
its own session's.

- `asc_personal_csp` is only a hint that the session has personal domains: `1`, HttpOnly, `SameSite=Lax`, a year,
  `Secure` over HTTPS. `getWebPlugins` without `personal`, and the personal POST, PUT and DELETE, set it while the
  member has enabled, unshadowed personal domains whose header stays within `csp:default:maxSize`, and delete it
  otherwise. A page request without it keeps the portal path, so a member without personal domains never costs
  the proxy more than before.
- The calls that set the hint write the session's header when it changed. When the request carries no session
  cookie (a script signed in with a token), nothing is cached; when it also carries an `Authorization` header,
  the session cookie may not be what signed it in, so nothing is cached either.
- On a miss the proxy asks `getCspSettings`, which adds the caller's personal domains to `header`, skips 304, and
  caches the header it answers for the session, so the next page of that session needs no API call.
- The generation, a random value per host key at `csp:{host}:personal`, replaces dropping entries one by one. A
  change that can alter a member's personal domains starts a new one: a personal upload, switch or deletion, a
  portal plugin upload or deletion (it can shadow a personal one), `setPersonalWebPlugins`, an administrator's
  switch or deletion of a member's plugin, and a save of the portal policy, which every member header repeats.
  The headers under the old generation are no longer found and expire within a day. A re-cache of a missing
  portal header writes the same domains and starts none.
- When the header would pass `csp:default:maxSize`, the session caches nothing and the hint is not set. A
  personal POST, and a personal PUT with `enabled` true, then answer 403 "CSP header size exceeds maximum allowed
  size of N bytes." after the change was saved, as the portal path does (P2); the refusal is recognised by its
  own exception type. A personal DELETE, a PUT with `enabled` false and the plugin list log a warning and answer
  as usual.
- Every response that clears the session cookie also deletes the hint. The session's header stays until it
  expires, but no request can reach it without that session cookie.

Alternatives: an HMAC of the tenant and user id as the cookie and the key (the first builds of this branch: a
cookie set from a neighbouring site could carry another member's token, it outlived the session on a shared
computer, and it was computable from the installation key); FusionCache tags to drop a member's entries (a tag is
expired lazily by the cache that reads it, while the proxy reads Redis directly and would still find the
entries); an index of the tenant's tokens (a read-modify-write that loses tokens under concurrent writes).

### Socket events

The portal event `s:change-web-plugin` goes to the whole portal and names the plugin, so a personal change cannot
use it. A personal upload, switch or deletion, and an administrator's switch or deletion of a member's plugin,
send `s:change-personal-web-plugin` with `webPluginName` and `enabled` to the private room
`{tenantId}-private-{userId}` of the member, which only the member's sockets join; the web client re-reads the
member's own list on it and loads or unloads the plugin in every open tab. `setPersonalWebPlugins` sends
`s:change-personal-web-plugins` to the quota room. A failure to send is logged and does not fail the call.

### Restore of the personal folders

A portal backup carries the `personalwebplugins` module and the members' settings rows, which the restore moves
to the members' new ids. `ModuleProvider` returns `PersonalWebPluginsModuleSpecifics` for that module; on a
restore and on a transfer to another region it moves `<oldUserId>/` to the user's new id through the user
mapping, and skips a file of a user who is not restored, except in a dump restore, which keeps the ids. The
`createBy` inside a restored manifest keeps the old user id, as the `config.json` of a portal plugin already
does.

### Configuration

The `plugins` section is read once, through the repository's configuration helpers, with a default and bounds
for every value and a warning for one that cannot be read or lies out of bounds; the list of asset extensions is
read element by element. `csp:default:maxSize` is read the same way, once. `getPortalSettings` fills `plugins`
from these settings through a mapper and adds the portal switch.

## Risks / Trade-offs

- [The `pluginName` of a personal plugin can collide with a portal plugin added later] → the personal copy is
  shadowed, never loaded twice.
- [Personal files count against no quota and stay after the user is deleted] → the administrator's list shows
  the plugins of deleted members, which the administrator can delete; the per-member limit bounds their number
  and the package size limit each one; quota and automatic cleanup are follow-ups.
- [Parallel uploads of one member can pass the count limit] → stated in the requirement; follow-up.
- [Every personal change starts a new generation for the whole portal, so each member with personal domains
  costs the proxy one `getCspSettings` call on the next page] → personal changes are rare, and the call caches
  the header again.
- [`asc_personal_csp` is `SameSite=Lax`: a frame on another site (an SDK frame) never sends it and runs without
  the personal CSP] → follow-up for frames.
- [Another device, or a browser without the hint, gets no personal CSP until a page loaded after its first
  plugin list] → that list sets the hint.
- [A member whose personal header would pass the size limit gets 403 from a personal upload or switch-on and the
  portal policy on their pages] → the 15360-character cap bounds one plugin, not the member's sum; follow-up.
- [A member installs a malicious plugin from someone else] → it runs with that member's rights only; personal
  plugins are off on a portal until an administrator switches them on, and the administrator sees and can remove
  every member's plugin.

## Migration Plan

No data migration. Existing portal plugins, states and CSP domains are untouched; personal plugins are off on
every portal until an administrator switches them on. A manifest stored by an earlier portal upload no longer
applies its `enabled`, `settings` and `cssUrl`, so a portal plugin that had no state row and a manifest saying
`"enabled": true` reads as off until an administrator switches it on.

The installation declares the `personalwebplugins` module in `storage.json` (buildtools `feature/personal-plugins`;
other deployments add the same entry). Personal plugins written by earlier builds of this branch under
`webplugins/.personal` are no longer listed; development stands remove those folders.

Rollback: not planned. An earlier version does not know the module and lists no personal plugin; see the pull
request for what a rollback leaves behind.

## Open Questions

None.
