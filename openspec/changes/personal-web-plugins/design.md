# Design

## Context

See proposal.md for the why. The design builds on these requirements:
- `plugins` "Installation-wide plugins": the installation-wide plugin already wins its name over a portal plugin.
- `plugins` "Stored state of a plugin": one state row per name, encrypted `settings`.
- `plugins` "Plugin name and uniqueness": `name` is the storage folder; `pluginName` is the global JavaScript
  object the client registers.
- `plugins` "Plugin files served to the browser": files are served from the `webplugins` storage module.
- `plugins` "Plugins in backup and restore": a portal backup carries the whole `webplugins` module.
- `portal-settings` "Configuring the Content Security Policy": `configureCsp` stores the portal domains and
  caches the header the proxy serves.
- `portal-settings` "Domains of the Content Security Policy": the address check that personal `cspDomains`
  reuse.
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
- No new storage module, no schema change, no migration.

**Non-Goals:**
- An administrator view or removal of members' personal plugins, a "promote to portal" action.
- Cleaning up the plugins of a deleted user, charging them to the user's quota.
- Switching between a personal copy and a portal plugin without a page reload.

## Decisions

### Storage: a folder in the existing module

Personal files go to `webplugins/.personal/<userId>/<name>/`, and their manifest is stored as `personal.json`
instead of `config.json`. The folder starts with a dot because a manifest name cannot, so no portal plugin
folder can be `.personal`; a folder named `personal` could also be a portal plugin's, and deleting that plugin
would delete every member's files with it. The portal scan looks for `config.json` and keeps only
`<name>/config.json`, one level deep, so it never reads a personal manifest; the listing itself still walks the
personal folders, because the disc and the S3 storage both list recursively.

Alternatives: a new storage module (needs `storage.json` changes on every installation and a backup
declaration); the user's documents (mixes code with documents); browser storage (bound to one device).

### State: the member's own settings row

The state of personal plugins is the same settings object as the portal's, saved for the current user. The
`settings` encryption, the row removal for "off and empty", and the settings a re-upload keeps behave the same
way.

### Manifest fields the server owns

A portal or personal upload stores the manifest with the server's `createBy` and `createOn` and without
`enabled`, `settings` and `cssUrl`: the state lives in the settings rows, and `cssUrl` is derived on every read
from a shipped `plugin.css`. A plugin uploaded that way from now on can therefore be switched off (P1). An
installation-wide upload (`system=true`) keeps the three fields, so its manifest stays the default state of the
other portals, as a shipped manifest does; a manifest stored by an earlier upload also keeps its values.

### Order of the checks

For a personal upload the controller checks the plugin switches, the personal switch and the guest rule before
it reads the form, and the manager repeats them before it looks at `system`. A guest who sends
`personal=true&system=true` gets 403; a member gets 400 "Changes to system plugins are prohibited".

### Shadowing is computed at read time

`getWebPlugins` builds installation-wide → portal → personal and drops a personal plugin whose `name` or
`pluginName` is already in the list. Nothing is stored for the shadow, so deleting the portal plugin brings
the personal copy back with its own state. A portal upload never looks at personal copies, so a member cannot
block an administrator.

Alternatives:
- Personal wins: an administrator could not impose a version or switch a plugin off.
- Block the portal upload: members would block the administrator, who cannot see their copies.
- Delete the personal copies on a portal upload: settings are lost, and the copy may be another plugin with
  the same name.

### Personal CSP through a cached header and a key in a cookie

The portal CSP header is cached per host (`csp:{host}`) and read by the proxy for everyone, so the personal
domains cannot go there. The member's own header (portal domains plus their personal ones) is cached next to
it, under `csp:{host}:{token}`, written through the same cache so the proxy reads it the same way. The token is
an HMAC of the tenant and user id with the installation key: the server can rebuild it for any member without
storing a mapping, and no one can derive another member's token from an id.

- A personal upload checks `cspDomains`: every entry, trimmed, must pass the address check of `configureCsp`
  (`CspSettings.IsValidDomain`) and hold no whitespace, `;` or quote, and the whole value is at most 15360
  characters, a cap that bounds the work of building the header. Otherwise the upload answers 400.
- `getWebPlugins` without `personal`, and the personal POST, PUT and DELETE, set the HttpOnly, `SameSite=Lax`
  cookie `asc_personal_csp` (a year; `Secure` when the request is HTTPS) to the token while the member has
  enabled personal plugins with domains that no portal or installation-wide plugin shadows. They write the entry
  when its header changed or the token is missing from the tenant's token list.
- When that header would pass `csp:default:MaxSize`, the member's entry is removed and nothing is cached. A
  personal POST, and a personal PUT with `enabled` true, then answer 403 "CSP header size exceeds maximum allowed
  size of N bytes." after the change was saved, as the portal path does (P2); `SyncPersonalCspAsync` refuses
  only for them (`refuseTooLarge`). A personal DELETE, a PUT with `enabled` false and the plugin list log a
  warning and answer as usual, with the cookie set, and `getCspSettings` falls back to the portal header, so the
  proxy serves that member the portal policy.
- When the member has no such domains, a personal POST, PUT or DELETE removes the member's entry, with or
  without the cookie, and deletes the cookie if the request carries it. `getWebPlugins` without `personal`
  does both only when the request carries the cookie, so the list of a member who never had personal domains
  writes nothing to the cache.
- The entries and the token list live a day from their last write. An unchanged header that the token list
  still holds is not written again, so a day after its last write the entry expires and the proxy asks
  `getCspSettings` until the next call that sets the cookie writes it again.
- The proxy reads `csp:{host}:{token}` for a page request whose cookie is a well-formed token (64 lowercase hex
  characters). On a miss it asks `getCspSettings`, which adds the caller's domains to `header` and skips 304;
  if reading them fails, it logs a warning and answers the portal header. Every other request keeps the portal
  path.
- Every save of the portal CSP (for example `configureCsp`, a portal plugin with domains uploaded, switched or
  deleted, a re-cache of a missing portal header) removes the entries of the tenant's token list after the new
  domains are stored, since each entry repeats the portal domains. A domain change (`saveDnsSettings`, the
  portal rename) and `deletePortal` remove the entries under the old host, and the token list with them. A
  portal that the retention job removes (`PortalRemovalService`) loses only its portal key; its personal entries
  expire within a day. While plugins are switched off for the installation, no plugin call runs far enough to
  touch the entries, so they too expire within a day unless a save of the portal CSP removes them.
- Every response that clears the session cookie (`CookiesManager.ClearCookies` for the auth key) also deletes
  `asc_personal_csp` when the request carries it: sign-out (`logout`), an expired or invalid session cookie or
  token on an operation that needs a session, a sign-in or confirmation link of another user, the cleanup after
  a failed third-party sign-in, and the portal rename.

Alternatives: the domains themselves in the cookie (the proxy would have to rebuild every directive); an
administrator approval or allow-list of personal domains (left out of the MVP); refusing `cspDomains` on
personal plugins (plugins that call external services could not work).

### Files served to their owner only

The storage handler of `webplugins` serves a `.personal/<id>/…` path to the signed-in user `<id>` only, and it
accepts no first path segment other than `.personal` or a valid plugin name.

### A socket event for the owner only

The portal event `s:change-web-plugin` goes to the whole portal and names the plugin, so a personal change cannot
use it. A personal upload, switch or deletion sends `s:change-personal-web-plugin` with `webPluginName` and
`enabled` to the private room `{tenantId}-private-{userId}` of the owner, which only the owner's sockets join.
A failure to send is logged and does not fail the call. The web client does not subscribe to it yet; it is to
subscribe to that event in that room, so the owner's other open pages follow a change without a reload. Until
then they see it after a reload.

### Restore of the personal folders

A portal backup already carries the whole `webplugins` module, personal folders included, and the members'
settings rows, which the restore moves to the members' new ids. `ModuleProvider` now returns
`WebPluginsModuleSpecifics` for the `webplugins` storage module; on a restore and on a transfer to another
region it moves `.personal/<oldUserId>/` to the user's new id through the user mapping, and skips a file of a
user who is not restored, except in a dump restore, which keeps the ids. The `createBy` inside a restored
`personal.json` keeps the old user id, as the `config.json` of a portal plugin already does.

### Configuration

Two installation keys: personal plugins on (default true) and the per-member limit (default 10). The whole
`plugins` section is read with `TryParse` and a default, never with a binder that throws: `getPortalSettings`
reads it on every page, so a typo must not take the portal down. A count or size of 0 or less falls back to its
default. This deliberately does not follow the repository's helper with bounds and warnings, by the team's
decision. `getPortalSettings` fills `plugins` from these settings through a mapper; what it answers does not
change.

## Risks / Trade-offs

- [The `pluginName` of a personal plugin can collide with a portal plugin added later] → the personal copy is
  shadowed, never loaded twice.
- [Personal files count against no quota, since the `webplugins` module is counted in none, and stay after the
  user is deleted] → follow-up; the per-member limit bounds their number and the package size limit each one.
- [Parallel uploads of one member can pass the count limit] → stated in the requirement; follow-up.
- [Storage access of personal files outside the file handler] → follow-up.
- [Binding the personal CSP cookie to the session] → follow-up.
- [`asc_personal_csp` is `SameSite=Lax`: a frame on another site (an SDK frame) never sends it and runs without
  the personal CSP; a top-level navigation from another site sends it, and the proxy then serves the cached
  personal header whenever the entry exists, or else the `getCspSettings` answer, which holds the personal
  domains only for a signed-in request] → follow-up for frames.
- [Another device, or a browser without the cookie, gets no personal CSP until a page loaded after its first
  plugin list] → that list sets the cookie.
- [A member whose personal header would pass the size limit gets 403 from a personal upload or switch-on and the
  portal policy on their pages, and the cookie the plugin list sets makes the proxy ask `getCspSettings` for
  each of those pages] → the 15360-character cap bounds one plugin, not the member's sum; follow-up.
- [The tenant's token list is kept by read-modify-write: two writes at once can lose a token, and the entry of a
  lost token survives a save of the portal CSP] → the member's next plugin list rewrites a changed header, and
  the entry expires a day after its last write.
- [The personal entries of a portal removed by the retention job, and all personal entries while plugins are
  switched off, stay until they expire, a day after their last write] → follow-up.
- [The portal scan still enumerates the `.personal` keys, more of them as members add plugins] → follow-up: list
  the top-level folders only.
- [A member installs a malicious plugin from someone else] → it runs with that member's rights only; an
  installation can switch personal plugins off.

## Migration Plan

No data migration. Existing portal plugins, states and CSP domains are untouched, so the requirements listed
under "Breaks current behaviour" hold for all existing data: without personal plugins `getWebPlugins`,
`getWebPlugin` and `getCspSettings` answer as before, apart from the new `personal` field. A manifest stored by
an earlier upload keeps its `enabled`, `settings` and `cssUrl` until the plugin is uploaded again without
`system=true`, so P1 stays for it.

Personal plugins written by earlier builds of this branch keep a `config.json` manifest under `.personal` and
are no longer listed. Development stands remove the `.personal` folders those builds wrote.

Rollback: deploy the previous version. It scans `config.json` recursively, so it does not list personal plugins,
whose manifests are `personal.json`; their states stay unused in the members' settings rows, and the cached
personal headers expire within a day. Personal files on a rollback: follow-up.

## Open Questions

- Whether personal plugins should become an administrator toggle of the portal instead of an installation key.
