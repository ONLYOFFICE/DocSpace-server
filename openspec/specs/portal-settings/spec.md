# portal-settings Specification

## Purpose

Defines the small portal-wide and per-account settings that have no capability of their own: how the portal
opens links on a mobile device, trusted mail domains, the time zone list, the host name and the custom domain,
the administrator helper tip, a member's start page and e-mail reminder, the socket settings, the Content
Security Policy, the authentication session lifetime and the promotional banner flag. It also defines the
entry through which an integration module pushes an event to the portal's browsers, and two AI operations
around skills: reading one skill file and writing a skill's text. Signing in, two-factor, SSO, IP restrictions
and password rules are not described here. The evidence per claim, the differences from the OpenAPI document
and the suspected defects are in `verification.md` next to this file.

## Requirements

### Requirement: Setting the deep link mode

`configureDeepLink` SHALL store how the portal opens its links on a mobile device, `Web`, `App` or
`ProvideChoice`, for a caller with the portal-settings right (Owner, DocSpace admin; others 403), and answer
200 with the stored `handlingMode` and `lastModified`. A value outside these SHALL answer 400.

#### Scenario: Owner chooses a mode

- **WHEN** the owner posts `deepLinkSettings` with `handlingMode` Web, then ProvideChoice
- **THEN** each call answers 200 with that `handlingMode`, and the first carries a set `lastModified`

#### Scenario: DocSpace admin chooses a mode

- **WHEN** a DocSpace admin posts `handlingMode` App
- **THEN** the system answers 200 with `handlingMode` App

### Requirement: Reading the deep link mode

`getDeepLinkSettings` SHALL answer 200 with the current `handlingMode` to any caller, signed in or not, and
SHALL answer 304 with an empty body when the settings did not change since `If-Modified-Since`.

#### Scenario: Administrator reads the mode

- **WHEN** the owner or a DocSpace admin reads the deep link settings
- **THEN** the system answers 200 with a `handlingMode`

#### Scenario: Anonymous caller reads the mode

- **WHEN** a caller without a session reads the deep link settings
- **THEN** the system answers 200 with a `handlingMode`

### Requirement: Trusted mail domains

`saveMailDomainSettings` SHALL store the trust `type`, `domains` and `inviteUsersAsVisitors` for a caller with
the portal-settings right (others 403), write an audit event and answer 200 with a confirmation message. With
type `Custom` each domain SHALL be trimmed and lower-cased, and an empty list or a malformed domain SHALL answer
400. Other types SHALL not read `domains`.

#### Scenario: Custom type with a malformed domain

- **WHEN** an administrator posts type `Custom` with a domain holding characters a domain cannot have
- **THEN** the system answers 400 and the portal's stored domains stay as they were

#### Scenario: Custom type without domains

- **WHEN** an administrator posts type `Custom` with an empty `domains`
- **THEN** the system answers 400

#### Scenario: Any domain

- **WHEN** an administrator posts type `All` with an empty `domains`
- **THEN** the system answers 200

### Requirement: Time zone list

`getTimeZones` SHALL answer 200 with every time zone of the host, each with a non-empty `id` and `displayName`,
UTC always among them, ordered by UTC offset. An ordinary session of the owner or a DocSpace admin SHALL be
enough; a User or a Guest SHALL get 403 and an anonymous caller 401. A setup confirmation link SHALL also be
accepted.

#### Scenario: Administrator reads the list

- **WHEN** the owner or a DocSpace admin reads the time zones with an ordinary session
- **THEN** the system answers 200 with a non-empty list

#### Scenario: Other members

- **WHEN** a User or a Guest reads the time zones
- **THEN** the system answers 403

#### Scenario: Anonymous caller

- **WHEN** a caller without a session reads the time zones
- **THEN** the system answers 401

### Requirement: Host name during setup

`getPortalHostname` SHALL answer 200 with the host of the request as sent, only to a caller holding a valid
setup wizard confirmation link. An ordinary session SHALL get 403 whatever its role, the owner's included, and
a caller with neither SHALL get 401.

#### Scenario: Ordinary session

- **WHEN** the owner calls `getPortalHostname` with an ordinary session
- **THEN** the system answers 403

#### Scenario: Setup link

- **WHEN** a caller presents a valid setup wizard confirmation link
- **THEN** the system answers 200 with the request's host

### Requirement: Custom domain of a self-hosted portal

`saveDnsSettings` SHALL exist on a self-hosted installation only (415 elsewhere, before any right is checked).
With the portal-settings right (others 403) it SHALL map `dnsName`, trimmed and lower-cased, onto the portal, or
clear the mapping when `enable` is false or the name empty, and write an audit event. A name under the base
domain or already taken SHALL be refused and change nothing.

#### Scenario: Anonymous caller

- **WHEN** a caller without a session posts a custom domain
- **THEN** the system answers 401 and the mapping stays

#### Scenario: Mapping switched off

- **WHEN** an administrator of a self-hosted portal calls with `enable` false and any `dnsName`
- **THEN** the name is ignored, the mapping is cleared and the system answers 200

#### Scenario: Hosted portal

- **WHEN** a member of a hosted portal calls `saveDnsSettings`
- **THEN** the system answers 415

### Requirement: Content Security Policy follows the domain

When `saveDnsSettings` changes the address of the portal, the stored policy SHALL move from the old address to
the new one, so that the portal keeps serving its policy under the new name.

#### Scenario: Domain mapped

- **WHEN** an administrator of a self-hosted portal maps a new custom domain
- **THEN** the policy built for the old address is served for the new one

### Requirement: Dismissing the administrator helper

`closeAdminHelper` SHALL mark the helper tip as viewed for the calling DocSpace admin only, on a self-hosted
installation outside custom mode, and answer 200 with no data. In every other case, a non-administrator
included, it SHALL answer 415 "Not available." and store nothing.

#### Scenario: Administrator of a self-hosted portal

- **WHEN** a DocSpace admin of a self-hosted portal calls `closeAdminHelper`
- **THEN** the system answers 200 and other administrators still see the tip

#### Scenario: Hosted portal

- **WHEN** the owner of a hosted portal calls `closeAdminHelper`
- **THEN** the system answers 415

### Requirement: Personal start page

`saveDefaultFolder` SHALL let any signed-in member choose the section their own account opens into, answer 200
with the saved `defaultFolderType`, write an audit event, and report it in that member's `getPortalSettings`.
Another member's choice SHALL not change. An anonymous caller SHALL get 401; a section that cannot be a start
page, or My documents for a Guest, SHALL answer 400.

#### Scenario: Owner changes the start page

- **WHEN** the owner saves the Rooms section, then Favorites, reading the portal settings after each
- **THEN** each call answers 200 and the portal settings report the section just saved

#### Scenario: Choice stays with the member

- **WHEN** the owner saved the Rooms section and a User saves My documents
- **THEN** the User's portal settings report My documents and the owner's still report the Rooms section

#### Scenario: Room admin and Guest

- **WHEN** a Room admin or a Guest saves the Rooms section
- **THEN** the system answers 200 and their portal settings report it

#### Scenario: Anonymous caller

- **WHEN** a caller without a session saves a start page
- **THEN** the system answers 401

#### Scenario: Guest chooses My documents

- **WHEN** a Guest saves My documents
- **THEN** the system answers 400

### Requirement: E-mail confirmation reminder

`updateEmailActivationSettings` SHALL store for the calling member alone whether the reminder to confirm their
address is shown, and answer 200 with `show` as submitted. Any signed-in role SHALL be allowed; an anonymous
caller SHALL get 401.

#### Scenario: Owner hides the reminder

- **WHEN** the owner sends `show` false
- **THEN** the system answers 200 with `show` false

#### Scenario: User shows the reminder

- **WHEN** a User sends `show` true
- **THEN** the system answers 200 with `show` true

#### Scenario: Anonymous caller

- **WHEN** a caller without a session sends the preference
- **THEN** the system answers 401

### Requirement: Reading the socket settings

`getSocketSettings` SHALL answer 200 with a socket settings object to any signed-in role and needs no
further right.

#### Scenario: Every role reads it

- **WHEN** the owner, a DocSpace admin, a Room admin, a User or a Guest reads the socket settings
- **THEN** the system answers 200 with a body

### Requirement: Configuring the Content Security Policy

`configureCsp` SHALL replace the portal's allowed `domains` for a caller with the portal-settings right, and
answer 200 with the stored `domains` and the `header` built from them. Owner and DocSpace admin SHALL be
allowed; Room admin, User and Guest SHALL get 403 "Access denied", an anonymous caller 401.

#### Scenario: Administrator adds a domain

- **WHEN** the owner or a DocSpace admin posts `domains` with an https address
- **THEN** the system answers 200 with that address in `domains` and a non-empty `header`

#### Scenario: Other members

- **WHEN** a Room admin, a User or a Guest posts `domains`
- **THEN** the system answers 403 with "Access denied"

#### Scenario: Anonymous caller

- **WHEN** a caller without a session posts `domains`
- **THEN** the system answers 401

### Requirement: Domains of the Content Security Policy

`configureCsp` SHALL accept an entry with or without a scheme and with a `*.` wildcard before the host. An entry
that is no valid address once these are removed, or that holds non-ASCII characters, SHALL answer 400 and store
nothing. A resulting `header` larger than the installation allows SHALL answer 403 and store nothing.

#### Scenario: Wildcard domain

- **WHEN** an administrator posts `*.example.com`
- **THEN** the system answers 200 with the entry in `domains`

#### Scenario: Non-ASCII entry

- **WHEN** an administrator posts an entry holding non-ASCII characters
- **THEN** the system answers 400 and the stored domains stay

### Requirement: Reading the Content Security Policy

`getCspSettings` SHALL answer 200 with `domains` and a non-empty `header` to every caller, an anonymous one
included, and SHALL answer 304 with an empty body when the settings did not change since `If-Modified-Since`.
A portal with no stored domains SHALL answer an empty `domains`.

#### Scenario: Every role reads what was configured

- **WHEN** the owner configured a domain and the owner, a DocSpace admin, a Room admin, a User or a Guest reads the policy
- **THEN** the system answers 200 with that domain and a non-empty `header`

#### Scenario: Anonymous caller

- **WHEN** the owner configured a domain and a caller without a session reads the policy
- **THEN** the system answers 200 with that domain and a non-empty `header`

### Requirement: Reading the session lifetime

`getCookieSettings` SHALL answer 200 with `lifeTime` in minutes and `enabled` to the owner and a DocSpace admin.
A Room admin, a User or a Guest SHALL get 403 "Access denied", an anonymous caller 401.

#### Scenario: Administrator reads it

- **WHEN** the owner or a DocSpace admin reads the cookie settings
- **THEN** the system answers 200 with a body

#### Scenario: Other members

- **WHEN** a Room admin, a User or a Guest reads the cookie settings
- **THEN** the system answers 403 with "Access denied"

#### Scenario: Anonymous caller

- **WHEN** a caller without a session reads the cookie settings
- **THEN** the system answers 401

### Requirement: Changing the session lifetime

`updateCookieSettings` SHALL store `lifeTime` and `enabled` for the owner or a DocSpace admin, write an audit
event and answer 200 "Settings have been successfully updated". A `lifeTime` above 9999 SHALL be stored as 9999
without an error; 0 or less SHALL clear the number. Other roles SHALL get 403, an anonymous caller 401, and an
installation hiding the section 402.

#### Scenario: Administrator saves a lifetime

- **WHEN** the owner or a DocSpace admin sends `lifeTime` 720 and `enabled` true
- **THEN** the system answers 200 with "Settings have been successfully updated"

#### Scenario: Lifetime above the maximum

- **WHEN** the owner sends `lifeTime` 100000 and `enabled` true, signs in again and reads the settings
- **THEN** the update answers 200 and the settings report `lifeTime` 9999 and `enabled` true

#### Scenario: Other members

- **WHEN** a Room admin, a User or a Guest sends a lifetime
- **THEN** the system answers 403 with "Access denied"

#### Scenario: Anonymous caller

- **WHEN** a caller without a session sends a lifetime
- **THEN** the system answers 401

### Requirement: Sessions after a lifetime change

A positive `lifeTime` in `updateCookieSettings` SHALL invalidate every session of the portal issued before the
call, also with `enabled` false; with `enabled` true the active connections SHALL be logged out as well. The
caller SHALL be signed in again within the call and get a new session cookie, and a token they held before SHALL
stop working.

#### Scenario: Token held before the call

- **WHEN** the owner changes the lifetime to a positive value and keeps sending the token held before
- **THEN** that token is no longer accepted, and a new sign-in or the cookie from the response is needed

### Requirement: Reading the banner flag

`getTenantBannerSettings` SHALL answer 200 with the promotional banner flag and its `lastModified` to every
signed-in role. An anonymous caller SHALL get 401.

#### Scenario: Every role reads it

- **WHEN** the owner, a DocSpace admin, a Room admin, a User or a Guest reads the banner settings
- **THEN** the system answers 200 with a body

#### Scenario: Anonymous caller

- **WHEN** a caller without a session reads the banner settings
- **THEN** the system answers 401

### Requirement: Setting the banner flag

`setTenantBannerSettings` SHALL store `hidden` only on an Enterprise installation, for a caller with the
portal-settings right, write an audit event and answer 200 with the stored setting. Elsewhere it SHALL answer
402 for any signed-in role, before the right is checked; on Enterprise other roles SHALL get 403. An anonymous
caller SHALL get 401.

#### Scenario: Anonymous caller

- **WHEN** a caller without a session posts `hidden`
- **THEN** the system answers 401

#### Scenario: Not an Enterprise installation

- **WHEN** the owner posts `hidden` on an installation that is not Enterprise
- **THEN** the system answers 402

#### Scenario: Other members on Enterprise

- **WHEN** a User posts `hidden` on an Enterprise installation
- **THEN** the system answers 403

### Requirement: Module key of a module event

`sendModuleEvent` SHALL need no user session: the caller SHALL identify itself with the `X-Module-Key` header,
and the module SHALL be the one the key is configured for, never a value of the body. A missing or unknown key
SHALL answer 401; a key not allowed for the portal named by the request host SHALL answer 403.

#### Scenario: Unknown key

- **WHEN** a caller posts an event with no `X-Module-Key` or a key no module holds
- **THEN** the system answers 401 and nothing is delivered

#### Scenario: Key of another portal

- **WHEN** a module posts an event to a portal its key is not allowed for
- **THEN** the system answers 403 and nothing is delivered

### Requirement: Addressee of a module event

A `sendModuleEvent` body SHALL carry an `event` name and exactly one of `user` and `room`. `event` and `room`
SHALL be 1-128 Latin letters, digits or `_ . : -`; faults answer 400. `user` SHALL be the e-mail of an active
user of this portal, otherwise 404. A body larger than 64 KB SHALL answer 413.

#### Scenario: Both addressees

- **WHEN** a module posts an event with both `user` and `room`, or with neither
- **THEN** the system answers 400

#### Scenario: Address from another portal

- **WHEN** a module posts an event to the e-mail of a user who is not an active user of this portal
- **THEN** the system answers 404

### Requirement: Delivery of a module event

An accepted `sendModuleEvent` SHALL answer 200 and hand the event to the socket service, storing nothing. A
`user` event SHALL reach that user's open sessions without a subscription; a `room` event SHALL reach those
subscribed to `plugin-{module}-{room}`. Browsers SHALL receive `plugin:{module}:{event}` with `{ message }`. A
repeated call SHALL deliver again.

#### Scenario: Event for a user

- **WHEN** a module with a valid key posts `event` new-message and `user` with an active user's e-mail
- **THEN** the system answers 200 and that user's open sessions receive `plugin:{module}:new-message`

#### Scenario: Nobody online

- **WHEN** the addressed user has no open session
- **THEN** the system still answers 200 and the event is not kept for later

### Requirement: Reading one skill file

`aiContextGetRoomSkill` SHALL answer 200 with the Markdown of one skill of an Instructions room as a JSON string,
at most its first 256 KiB. A missing `roomId` or `skillId` SHALL answer 400. A `skillId` that is not one of that
room's skills, or a room that is not an Instructions room the caller can read, SHALL answer 404, and no file is
read.

#### Scenario: Another file of the portal

- **WHEN** a member passes a room they read and the id of a file that is not one of its skills
- **THEN** the system answers 404 without reading that file

#### Scenario: Missing parameter

- **WHEN** a member leaves out `roomId` or `skillId`
- **THEN** the system answers 400

### Requirement: Requests to generate a skill

`aiSkillsGenerate` SHALL check AI access first, then need `destId` (the integer id of an Instructions room),
`title` (1-64 characters after trimming, with a letter or digit) and `description` (1-2000 characters with text
left). Faults SHALL answer 400, another kind of folder 400, an unknown folder 404, a caller who cannot create
files there 403, all before a model runs.

#### Scenario: Not an Instructions room

- **WHEN** a member passes the id of another kind of folder as `destId`
- **THEN** the system answers 400 and no model is called

#### Scenario: Description of control characters only

- **WHEN** a member sends a `description` holding nothing but control characters
- **THEN** the system answers 400 and no model is called

### Requirement: Result of generating a skill

`aiSkillsGenerate` SHALL answer 200 with `name` and `content`, the whole Markdown file, and SHALL save nothing.
`name` SHALL be the `title` lower-cased with other characters folded into single hyphens, at most 64 characters.
The server SHALL write the frontmatter itself, ignoring any name from the model. A description the model left
out SHALL fall back to the caller's `description`.

#### Scenario: Model writes its own name

- **WHEN** a member asks for the title `Sales report` and the model answers a frontmatter with another name and a tag in the description
- **THEN** the file carries `name` `sales-report`, the description without the tag, and nothing is stored

#### Scenario: Model wrote no description

- **WHEN** the model's answer has a body and no description
- **THEN** the frontmatter carries the caller's own words as the description

### Requirement: Failures of generating a skill

When no model profile is bound to the chat action or to the default slot, `aiSkillsGenerate` SHALL answer 503.
A model answer with no body SHALL answer 502 with `code` `empty_answer`. A failed model call SHALL answer with
its status and `code`; `providerMessage` SHALL appear only for a rate limit, a lack of credit or the portal's
spending limit.

#### Scenario: No profile

- **WHEN** a member generates a skill on a portal with no model profile for the chat action and none for the default slot
- **THEN** the system answers 503

#### Scenario: Provider refuses authentication

- **WHEN** the model provider refuses the configured key
- **THEN** the answer carries the `code` and no `providerMessage`
