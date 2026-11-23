# Design

## Context

See proposal.md for the why. The design builds on these requirements of `plugins`:
- "Installation-wide plugins": the installation-wide plugin already wins its name over a portal plugin.
- "Stored state of a plugin": one state row per name, encrypted `settings`.
- "Plugin name and uniqueness": `name` is the storage folder; `pluginName` is the global JavaScript object the
  client registers.
- "Plugin files served to the browser": files are served from the `webplugins` storage module.

The web client loads exactly what `getWebPlugins` returns and keys every plugin by `name` and `pluginName`,
so one page must never see two plugins with the same `name` or `pluginName`.

## Goals / Non-Goals

**Goals:**
- A third level below installation-wide and portal plugins, with the same rule that already holds between the
  two: a name belongs to the higher level.
- A personal plugin cannot change anything shared: the portal state, the portal Content Security Policy, the
  socket events of the portal.
- No new storage module, no schema change, no migration.

**Non-Goals:**
- An administrator view or removal of members' personal plugins, a "promote to portal" action.
- Cleaning up the plugins of a deleted user, charging them to the user's quota.
- Switching between a personal copy and a portal plugin without a page reload.

## Decisions

### Storage: a folder in the existing module

Personal files go to `webplugins/.personal/<userId>/<name>/`, saved with a private ACL so a public-read S3
module does not expose them past the storage handler. A manifest name cannot start with a dot, so no plugin
folder can be `.personal`; a name like `personal` could be, and deleting such a portal plugin would delete
every member's folder with it. The portal scan keeps only `<name>/config.json` entries, one level deep, so it
never reads a personal manifest; the listing itself still walks the personal folders (S3 lists recursively).

Alternatives: a new storage module (needs `storage.json` changes on every installation and a backup
declaration); the user's documents (mixes code with documents); browser storage (bound to one device).

### State: the member's own settings row

The state of personal plugins is the same settings object as the portal's, saved for the current user. The
`settings` encryption, the row removal for "off and empty", and P1 behave the same way.

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

- `getWebPlugins` without `personal`, and the personal POST, PUT and DELETE, write the entry and set the HttpOnly
  cookie `asc_personal_csp` to the token while the member has enabled personal plugins with domains; otherwise
  they remove the entry and clear the cookie. The entries live a day and are rewritten on every plugin list.
- The proxy reads `csp:{host}:{token}` for a request whose cookie is a well-formed token. On a miss it asks
  `getCspSettings`, which adds the caller's domains to `header` and skips 304. Every other request keeps the
  portal path.
- Saving the portal CSP removes the personal entries of the tenant (their tokens are kept in a list next to
  them), since each of them repeats the portal domains.

Alternatives: the domains themselves in the cookie (the proxy would have to rebuild every directive, and a
cookie written by a sibling site could widen anyone's policy); an administrator approval or allow-list of
personal domains (left out of the MVP); refusing `cspDomains` on personal plugins (plugins that call external
services could not work).

### Files served to their owner only

The storage handler of `webplugins` refuses a `.personal/<id>/…` path unless `<id>` is the signed-in user. The
first path segment has to be either exactly `.personal` or a valid plugin name, so no other spelling of the
personal folder that a file system resolves to the same place gets through.

### No socket event for personal changes

The plugin event goes to the whole portal and names the plugin. A personal change sends nothing, and the
owner's other pages see the change after a reload.

### Configuration

Two installation keys: personal plugins on (default true) and the per-member limit (default 10). The whole
`plugins` section is read with `TryParse` and a default, never with a binder that throws: `getSettings` reads it
on every page, so a typo must not take the portal down. A count or size of 0 or less falls back to its default.
This deliberately does not follow the repository's helper with bounds and warnings, by the team's decision.

## Risks / Trade-offs

- [The `pluginName` of a personal plugin can collide with a portal plugin added later] → the personal copy is
  shadowed, never loaded twice.
- [Personal files count against the portal quota and stay after the user is deleted] → follow-up; the
  per-member limit bounds the size.
- [Parallel uploads of one member can pass the count limit] → stated in the requirement; follow-up.
- [The personal `cspDomains` are not validated like `POST api/2.0/security/csp` and the header size is not
  capped on the personal path] → only the owner's pages are affected; follow-up.
- [`asc_personal_csp` is `SameSite=Lax` and the session cookie `Strict`: the first page after sign-in, a page
  opened from a link on another site, and SDK frames on another site run without the personal CSP] → the next
  page load has it; follow-up for frames.
- [A personal cache entry outlives a lost token in the tenant's token list (two writes at once)] → the next plugin
  list rewrites it, and it expires after a day.
- [The portal scan lists every member's personal files, more of them as members add plugins] → follow-up: list
  the top-level folders only.
- [A member installs a malicious plugin from someone else] → it runs with that member's rights only; an
  installation can switch personal plugins off.

## Migration Plan

No data migration. Existing portal plugins, states and CSP domains are untouched, so the requirements listed
under "Breaks current behaviour" hold for all existing data: without personal plugins `getWebPlugins`,
`getWebPlugin` and `getCspSettings` answer as before. Rollback: deploy the previous version; personal folders
and states stay in storage unused.

## Open Questions

- Whether a restore that maps user ids keeps the personal folders, which are named by user id, attached to
  their owners.
- Whether personal plugins should become an administrator toggle of the portal instead of an installation key.
