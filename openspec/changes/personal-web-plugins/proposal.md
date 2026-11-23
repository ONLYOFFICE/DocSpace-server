# Proposal

## Why

Only a portal owner or DocSpace admin can install a web plugin, and every plugin runs for the whole portal. A
member who wants a plugin for their own work, or a plugin an AI chat generated for them, has to ask an
administrator to install it for everyone. Personal plugins let a member install a plugin that runs for them
alone, while the administrator keeps control of what runs for the portal.

## What Changes

- New `personal` query flag on `addWebPluginFromFile`, `getWebPlugins`, `getWebPlugin`, `updateWebPlugin` and
  `deleteWebPlugin`. With it, a member who is not a guest installs, lists, reads, switches and deletes plugins
  of their own, without the portal-settings right. The plugin runs for that member only, and its state and
  settings are kept for that member only.
- The installation can switch personal plugins off; `getSettings` reports the switch as `plugins.personal`.
- A member holds at most 10 personal plugins by default.
- A name belongs to the higher level. A personal upload that takes the `name` or `pluginName` of a portal or
  installation-wide plugin is refused with 400. A portal upload is never blocked by personal copies. They are
  kept but no longer listed for their owner (shadowed), and they come back when the portal plugin is deleted.
- **BREAKING** `getWebPlugins` without `personal` returns everything available to the caller: the
  installation-wide plugins, then the portal plugins, then the caller's personal plugins that are not
  shadowed. Each entry carries `personal`.
- The domains a personal plugin declares in `cspDomains` never reach the portal Content Security Policy. A
  signed-in member whose request carries the `asc_personal_csp` cookie gets them in the `header` that
  `getCspSettings` returns. The cookie is set and cleared by `getWebPlugins` and by the personal uploads,
  updates and deletions.
- The files of a personal plugin are served to their owner only.
- A personal change sends no real-time event to the portal's clients.
- `refreshWebPlugin` gets a request model of its own and does not take `personal`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `plugins`: found by `addWebPluginFromFile`, `getWebPlugins`, `getWebPlugin`, `updateWebPlugin`,
  `deleteWebPlugin`, `refreshWebPlugin` and `getSettings` (`plugins`). Requirements modified:
  - "Installation-wide plugin switches" (the personal switch);
  - "Listing the plugins of a portal" (personal plugins in the list, `personal`);
  - "Reading one plugin by name" (`personal`);
  - "Uploading a plugin package" (personal upload without the settings right);
  - "Switching a plugin on or off and storing its settings" (personal state);
  - "Content Security Policy domains of a plugin" (personal domains stay out of the portal policy);
  - "Deleting a plugin" (personal deletion);
  - "Clients are told when a plugin changes" (no event for personal changes).

  Requirements added: personal plugins and their access, the name rule between the levels, the personal count
  limit, personal plugin files served to their owner.
- `portal-settings`: found by `getCspSettings`. Requirement modified: "Reading the Content Security Policy"
  (the owner of a personal plugin gets its domains and no 304).

## Breaks current behaviour

- **Listing the plugins of a portal** (`getWebPlugins`). A member with personal plugins gets them after the
  portal plugins, with `personal` true. The list can now hold more than 100 entries: the portal plugins plus
  the member's own. Affected:
  - the web client (`../client`) loads exactly this list and shows the portal plugins in the admin settings
    through `portalPluginList`; its branch `feature/personal-plugins` handles `personal`;
  - SDKs and the OpenAPI document: a new `personal` field on `WebPluginDto` and new query parameters;
  - webhooks, backup and restore, portal migration, stored data: none for portal plugins.

  For a member without personal plugins nothing changes.
- **Reading one plugin by name** (`getWebPlugin`). It no longer searches "the same set `getWebPlugins`
  returns": without `personal` it searches the portal and installation-wide plugins only, so a personal entry
  of the list answers 404 unless `personal=true` is sent.
- **Reading the Content Security Policy** (`getCspSettings`). A request with the `asc_personal_csp` cookie
  from a signed-in member with enabled personal plugins that declare domains gets a longer `header` and never
  304. Every other caller sees no change. The proxy (buildtools `feature/personal-plugins`) asks the API
  instead of its shared cache for such a request.
- **Installation-wide plugin switches** (`getSettings`): a new `plugins.personal` field, true unless the
  installation switched personal plugins off. No existing field changes.
- **Portal plugin count limit**: an upload of a new name at the limit now answers 400 "Wrong plugin archive.
  The number of plugins exceeds the limit", as the requirement says (see P9 below).
- Socket events: a personal change sends none, so the owner's other open pages pick it up on their next load.
- Backup and restore: personal plugin files are in the portal's `webplugins` storage and their state in the
  member's settings row, so both are part of a portal backup. The files sit under a folder named by the
  member's id; whether a restore that maps user ids keeps them attached is not verified (design, open
  questions).

### Deviations

- **P9** (count limit answers "Plugin not found"): fixed. The limit check now looks the new name up in the
  list it already holds instead of calling the lookup that throws.
- **P4** (every failure after the size checks becomes 400 "Wrong plugin archive. …"): kept. The new name
  refusal for personal uploads is one more such 400.
- **P3, P6** (shadowed portal plugin, de-duplicated count): kept for portal plugins. The personal count limit
  counts the member's own plugins only.
- **P1** (manifest `enabled` wins over a missing state row): kept, and it applies to personal plugins the
  same way.
