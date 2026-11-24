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
  settings are kept in the member's own settings row.
- For a personal upload, the plugin switches, the personal switch and the guest rule are checked before the
  `system` flag and before the form is read: `personal=true&system=true` answers a member 400 and a guest 403.
- The installation can switch personal plugins off; `getPortalSettings` reports the switch as `plugins.personal`.
  `getPortalSettings` now builds `plugins` through a mapper, with no change in what it answers.
- A member holds at most 10 personal plugins by default.
- A name belongs to the higher level. A personal upload that takes the `name` or `pluginName` of a portal or
  installation-wide plugin is refused with 400, a re-upload of a held personal name included. A portal upload is
  never blocked by personal copies. They are kept but no longer listed for their owner (shadowed), and they come
  back when the portal plugin is deleted.
- **BREAKING** `getWebPlugins` without `personal` returns everything available to the caller: the
  installation-wide plugins, then the portal plugins, then the caller's personal plugins that are not
  shadowed. Each entry carries `personal`, which `WebPluginDto` now declares required, so the regenerated
  schema and SDKs make it non-optional.
- Personal files go to the member's folder `.personal/<userId>/<name>/`, with the manifest stored as
  `personal.json`, so no scan for `config.json` lists them.
- The server owns three manifest fields: a portal or personal upload stores the manifest without its `enabled`,
  `settings` and `cssUrl`. An installation-wide upload (`system=true`) keeps them, so its manifest stays the
  default state of the other portals.
- A personal plugin's `cspDomains` are checked at upload: an entry that is no valid address (the check of
  `POST security/csp`) or that holds whitespace, `;` or quotes, and a value longer than 15360 characters, answer
  400 "The cspDomains value lists an invalid address or is too long".
- The domains a personal plugin declares in `cspDomains` never reach the portal Content Security Policy. A
  signed-in member whose request carries the `asc_personal_csp` cookie gets them in the `header` that
  `getCspSettings` returns, unless the header would pass its size limit; if reading them fails, the portal
  header is served. A personal upload, or a personal update with `enabled` true, after which the member's header
  would pass the size limit keeps its change and answers 403 "CSP header size exceeds maximum allowed size of N
  bytes."; a personal deletion or switch-off, the plugin list and `getCspSettings` fall back to the portal
  header without an error. The cookie is set and cleared by
  `getWebPlugins` without `personal` and by the personal uploads, updates and deletions, and every response that
  clears the session cookie, `logout` among them, deletes it too. It carries the key under which the member's
  header is cached for the proxy, next to the portal's, for a day from its last write; a header over the size
  limit is not cached. Saving the portal policy drops the cached personal headers of the portal after the new
  domains are stored, and renaming the portal domain or `deletePortal` drops those under the old host; those of
  a portal the retention job removes, and all of them while plugins are switched off, expire within a day.
- The cached plugin manifests of a portal now expire after an hour, so plugin files replaced in storage show up
  within an hour even without `refreshWebPlugin`.
- The files of a personal plugin are served to their owner only.
- A personal upload, switch or deletion sends the socket event `s:change-personal-web-plugin` with
  `webPluginName` and `enabled` to the owner's private room only. The web client does not subscribe to it yet.
- A restore moves a member's personal plugin folder to the member's new user id.
- `refreshWebPlugin` gets a request model of its own and does not take `personal`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `plugins`: found by `addWebPluginFromFile`, `getWebPlugins`, `getWebPlugin`, `updateWebPlugin`,
  `deleteWebPlugin`, `refreshWebPlugin` and `getPortalSettings` (`plugins`). `logout` and the other responses
  that clear the session cookie now also delete the `asc_personal_csp` cookie; no requirement governs `logout`,
  so the cookie requirement of `plugins` states it. The 403 texts of the delta quote what the API sends,
  "Access denied", where the main spec quotes the exception text. Requirements modified:
  - "Installation-wide plugin switches" (the personal switch; the main spec names `GET api/2.0/settings`
    `getSettings`, which is the operationId of `GET api/2.0/apps/{id}/settings`, so the delta uses the published
    `getPortalSettings`);
  - "Listing the plugins of a portal" (personal plugins in the list, `personal`);
  - "Reading one plugin by name" (`personal`);
  - "Uploading a plugin package" (personal upload without the settings right, manifest fields the server owns);
  - "Re-uploading a plugin under the same name" (personal settings, a name a portal plugin took);
  - "Portal plugin count limit" (the caller's personal plugins come on top of the 100);
  - "Installation-wide plugins" (`system=true` with `personal=true`);
  - "Switching a plugin on or off and storing its settings" (personal state);
  - "Stored state of a plugin" (the member's settings row, manifest values an upload drops);
  - "Content Security Policy domains of a plugin" (personal domains stay out of the portal policy);
  - "Deleting a plugin" (personal deletion);
  - "Refreshing a plugin replaced in storage" (files show up within an hour without the call);
  - "Clients are told when a plugin changes" (the personal event);
  - "Audit trail of plugin changes" (personal changes write the same events);
  - "Plugins in backup and restore" (personal folders follow their owner's new id).

  Requirements added: personal plugins and their switch, the name rule between the levels, the personal count
  limit, personal plugin files served to their owner, the checks of personal `cspDomains`, the size limit of a
  member's header, the `asc_personal_csp` cookie, the header cached for a member.
- `portal-settings`: found by `getCspSettings`. Requirement modified: "Reading the Content Security Policy"
  (no 304 for a request that gets personal domains); requirement added: "Personal domains in the policy
  header". `configureCsp` and `saveDnsSettings` now also drop cached personal headers; "Configuring the Content
  Security Policy" and "Content Security Policy follows the domain" still hold as written.
- `portal-lifecycle`: reached by `deletePortal` and `PUT portal/portalrename`, which now also drop the personal
  headers cached under the old domain. Its requirements still hold as written; no delta.

## Breaks current behaviour

"Before the branch" below means `release/v4.1.0`, where the five operations had no `personal` parameter, so a
`personal=true` in the query was ignored.

- **Installation-wide plugin switches** (`getPortalSettings`): a new `plugins.personal` field, true unless the
  installation switched personal plugins off. No existing field changes. Affected: the SDKs and the OpenAPI
  document get a new optional `personal` on `PluginsDto`; the web client (`../client`, branch
  `feature/personal-plugins`) reads it. Webhooks, socket events, backup and restore, the portal migration and
  stored data: none.
- **Listing the plugins of a portal** (`getWebPlugins`). A member with personal plugins gets them after the
  portal plugins, with `personal` true. The list can now hold more than 100 entries: the portal plugins plus
  the member's own. The answer can set or delete the `asc_personal_csp` cookie. Affected:
  - the web client (`../client`) loads exactly this list and shows the portal plugins in the admin settings
    through `portalPluginList`; its branch `feature/personal-plugins` handles `personal`;
  - SDKs and the OpenAPI document: new `personal` and `cspDomains` fields on `WebPluginDto`, required in the schema, so the
    SDKs will type them non-optional, and a new `personal` query parameter;
  - webhooks, socket events, backup and restore, portal migration, stored data: none for portal plugins.

  For a member without personal plugins the entries only gain `personal` false, and a request that carries a
  left-over `asc_personal_csp` cookie gets it deleted.
- **Reading one plugin by name** (`getWebPlugin`). It no longer searches "the same set `getWebPlugins`
  returns": without `personal` it searches the portal and installation-wide plugins only, so a personal entry
  of the list answers 404 unless `personal=true` is sent. Affected: scripts and SDK users that read an entry of
  the list by its name; the web client does not call `getWebPlugin`. SDKs and the OpenAPI document: a new
  `personal` query parameter.
- **Uploading a plugin package** (`addWebPluginFromFile`). With `personal=true`, a member who is not a guest
  installs a plugin for themselves; before the branch an administrator's call with it installed a portal
  plugin, and anyone else's was refused with 403 "Access denied". An uploaded manifest's `enabled`, `settings`
  and `cssUrl` are no longer kept, for portal uploads too, though still for `system=true`: a manifest `cssUrl`
  without a shipped `plugin.css` no longer shows in `cssUrl` (for `enabled` and `settings` see "Stored state of
  a plugin"). A personal upload after which the member's policy header would pass the size limit answers 403
  "CSP header size exceeds maximum allowed size of N bytes." with the plugin installed and on. The answer of a
  personal upload can set or delete the `asc_personal_csp` cookie. Affected: the web client, which uploads
  personal plugins from the profile's Plugins tab; plugin packages whose manifest carries a `cssUrl`; the SDKs
  and the OpenAPI document (the new query parameter). Manifests stored before keep these fields.
- **Plugin name and uniqueness** (`addWebPluginFromFile`). A refusal because an installed plugin holds the
  `name` or `pluginName`, or because of the plugin limit, answers 400 with its reason alone, such as "Plugin
  already exist"; before the branch the reason came after "Wrong plugin archive. ", which a fault of the archive
  or its manifest keeps. Affected: API clients and scripts that match the whole text of these errors; the web
  client shows the text as it comes. SDKs, webhooks, socket events, backup and stored data: none.
- **Re-uploading a plugin under the same name** (`addWebPluginFromFile`). A personal re-upload keeps the
  settings stored in the member's row and answers 400 once a portal or installation-wide plugin has the
  `name` or `pluginName`. A portal re-upload changes only as every upload does (see "Uploading a plugin
  package"). Affected: members re-uploading a shadowed personal plugin, in the web client among others.
- **Portal plugin count limit**: the 100 still caps the portal and installation-wide plugins; the list may hold
  more because the caller's personal plugins come on top. An upload of a new name at the limit now answers 400
  "The number of plugins exceeds the limit" (see P9 below), without the "Wrong plugin archive. " the requirement
  put before it (see "Plugin name and uniqueness").
  Affected: callers of `getWebPlugins` that assumed at most 100 entries.
- **Installation-wide plugins** (`addWebPluginFromFile` with `system=true`). With `personal=true` as well the
  call now answers 400 "Changes to system plugins are prohibited", and a guest 403; before the branch an
  administrator's call installed the plugin installation-wide on a self-hosted installation and got 403 on a
  hosted one, and anyone else's got 403 "Access denied". Affected: a caller sending both flags; the web client
  never does.
- **Switching a plugin on or off and storing its settings** (`updateWebPlugin`). With `personal=true`, a member
  switches their own plugin and stores its settings in their own row; before the branch an administrator's call
  with it changed the portal plugin of that name, and anyone else's got 403 "Access denied". A personal call with
  `enabled` true after which the member's policy header would pass the size limit answers 403 "CSP header size
  exceeds maximum allowed size of N bytes." with the new state saved; a personal switch-off answers 200. The
  answer of a personal call can set or delete the cookie. Calls without `personal=true` answer as before. Affected: the web client (profile tab), the SDKs and
  the OpenAPI document (the new query parameter).
- **Stored state of a plugin**. A personal plugin's state lives in its owner's settings row. A portal or
  personal upload no longer keeps the manifest's `enabled` and `settings`, so such a plugin uploaded from now on
  and switched off without settings reads as off (P1 below); an installation-wide upload keeps them. Affected:
  portal plugins whose manifest declares `"enabled": true` or `settings`, once uploaded again; manifests stored
  before keep their values. Backup and restore: the member's row is restored with the member.
- **Content Security Policy domains of a plugin**. A personal plugin's domains never reach the portal's domains;
  before the branch an administrator's `personal=true` upload installed a portal plugin whose domains did. A
  personal upload whose `cspDomains` fails the new checks answers 400. Portal plugins are unchanged. Affected:
  the authors of packages with invalid `cspDomains`, which can no longer be installed as personal plugins.
- **Deleting a plugin** (`deleteWebPlugin`). With `personal=true` a member deletes their own plugin, without the
  delete switch; before the branch an administrator's call with it deleted the portal plugin of that name, and
  anyone else's got 403 "Access denied". Calls without `personal=true` answer as before. Affected: the web
  client (profile tab), the SDKs and the OpenAPI document (the new query parameter).
- **Refreshing a plugin replaced in storage** (`refreshWebPlugin`). Before, the cached manifests never expired;
  now plugin files replaced in storage show up within an hour without the call, and no socket event announces
  such a change. The call itself is unchanged and takes no `personal`. Affected: deployments that replace
  plugin files in storage.
- **Clients are told when a plugin changes**. A personal upload, switch or deletion sends
  `s:change-personal-web-plugin` with `webPluginName` and `enabled` to the owner's private room, and the
  portal's clients get no event for it; before the branch an administrator's `personal=true` call sent the
  portal event for the portal plugin it changed. Affected: socket events (a new one); the web client does not
  subscribe to it yet, so until it does the owner's other open pages pick a change up on their next load.
- **Audit trail of plugin changes**. A personal upload, update or deletion writes "Webplugin uploaded",
  "Webplugin updated" or "Webplugin deleted" in the portal's audit trail, with no mark that the plugin is
  personal. Affected: administrators who read the audit trail (`history-and-audit`); webhooks: none.
- **Plugins in backup and restore**. The portal backup already carried the whole `webplugins` storage, personal
  folders included. A restore now moves `.personal/<oldUserId>/` to the user's new id through the user mapping
  and skips the files of a user it does not restore, except a dump restore, which keeps them as they are; a
  transfer of the portal to another region does the same. Affected: backup and restore and the portal
  migration; existing stored data: none.
- **Reading the Content Security Policy** (`getCspSettings`). A request with the `asc_personal_csp` cookie
  from a signed-in member with enabled, unshadowed personal plugins that declare domains gets a longer `header`
  and never 304; when the longer header would pass the size limit, it gets the portal header, still without 304
  (requirement "Personal domains in the policy header"). Every other caller sees no change. The proxy
  (buildtools `feature/personal-plugins`) reads such a member's header from the cache under the key the cookie
  carries, and asks the API only when it is missing. Affected: the proxy; the SDKs and the OpenAPI document:
  none (no field changes).

### Deviations

- **P9** (count limit answers "Plugin not found"): fixed. The limit check now looks the new name up in the
  list it already holds instead of calling the lookup that throws.
- **P1** (manifest `enabled` wins over a missing state row): partly fixed. A portal or personal upload no
  longer keeps the manifest's `enabled` and `settings`, so such a plugin uploaded from now on can be switched
  off. An installation-wide manifest, shipped or uploaded with `system=true`, and one an earlier upload stored
  still carry them, and a shipped manifest's `createBy` and `createOn` are still trusted.
- **P2** (state saved before the CSP save, then 403): kept, and extended to personal plugins. A personal upload,
  or a personal update with `enabled` true, after which the member's policy header would pass the size limit
  answers 403 after its change was saved, as a portal call does.
- **P4** (every failure after the size checks becomes 400 "Wrong plugin archive. …"): narrowed. A refusal
  because an installed plugin holds the `name` or `pluginName`, because of the plugin limit or, for a personal
  upload, because of its `cspDomains` now answers 400 with its reason alone; a fault of the archive or its
  manifest and a storage failure are still wrapped, so P4 stays open for the storage failures.
- **P3, P6** (shadowed portal plugin, de-duplicated count): kept for portal plugins. The personal count limit
  counts the member's own plugins only.
- **P8** (required fields the server leaves out): kept. `personal` is always sent, so marking it required adds
  nothing to it.
