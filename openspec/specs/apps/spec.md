# apps Specification

## Purpose

Defines the portal applications of an installation: the feature modules the installation declares in its
configuration (such as `ai-rooms` or `docs-cloud`), each of which a portal can switch on or off and can
hold an opaque JSON settings document for. The specification covers the catalogue (`GET /api/2.0/apps`),
the lookup of one application and of its settings document (`GET /api/2.0/apps/{id}`,
`GET /api/2.0/apps/{id}/settings`), the two writes (`PUT /api/2.0/apps/{id}/enabled`,
`PUT /api/2.0/apps/{id}/settings`), who may read and write, the order in which a write is checked, the
per-portal storage of the flag and the document, and the real-time event a switch produces. What an
application does in the browser, how the client maps an identifier to a title and an icon, and which
modules a given installation ships are the client's and the installation's business and are out of scope.
The web plugins an administrator uploads are the `plugins` specification; the AI product behind the `ai-*`
identifiers is `ai-agents`.

Sources: the integration tests of `web/ASC.Web.Api.Tests` (`09_Apps`) and the TypeScript suite they were
ported from (`apps/`), the OpenAPI contract `sdk/docspace-api-spec/docspace-backend.yaml` (operations
named by `operationId`: `getAll`, `get`, `getSettings`, `setEnabled`, `setSettings`), the product code of
the controller and the settings service where the tests stop, the runtime configuration of the
installation, and the release notes (v3.3, v3.5, v4.0). Where the notes or the OpenAPI document and the
server disagree this specification follows the server; the evidence per scenario, the differences and the
suspected defects are in `verification.md` next to this file. HTTP status codes are stated where the tests
assert them.

## Requirements

### Requirement: Catalogue of portal applications

`getAll` (`GET /api/2.0/apps`) SHALL return, to any signed-in member, every application the installation
configuration declares, in the order of the configuration, without paging or filtering; query parameters
SHALL be ignored. Each entry SHALL carry `id`, `enabled` (the portal's own flag where saved, else the
configured default) and `settings` (the saved document, or none); `count` SHALL equal the number of
entries. An empty list means no applications are configured. Anonymous: 401.

#### Scenario: Owner lists the applications

- **WHEN** the owner calls `getAll`
- **THEN** the system answers 200 with a list whose identifiers are non-empty and unique and whose `count` equals its length

#### Scenario: Portal that never saved anything

- **WHEN** a fresh portal calls `getAll`
- **THEN** every entry has no `settings`

#### Scenario: Unknown query parameters

- **WHEN** `getAll` is called with `?foo=bar&count=-1&startIndex=%FF`
- **THEN** the answer is identical to the call without parameters

#### Scenario: List reflects a change

- **WHEN** the owner switches one application and saves a settings document for it
- **THEN** the next `getAll` has the same number of entries and that entry carries the new flag and the document

### Requirement: Reading one application

`get` (`GET /api/2.0/apps/{id}`) SHALL return the application whose configured identifier equals `id`
exactly (case-sensitive), with the same `id`, `enabled` and `settings` the list reports, and SHALL answer
404 naming the identifier when no configured application has it — including blanks, non-ASCII, markup,
slashes or a very long value. A request to `/api/2.0/apps/` with an empty identifier SHALL be routed to
the list. Reading SHALL change nothing. Anonymous: 401.

#### Scenario: Owner reads an application

- **WHEN** the owner calls `get` with an identifier from the list
- **THEN** the system answers 200 with `count` 1 and the same `id`, `enabled` and `settings` the list has for it

#### Scenario: Unknown identifier

- **WHEN** a member calls `get` with `no-such-app`
- **THEN** the system answers 404 with a message naming `no-such-app`

#### Scenario: Identifier in another case

- **WHEN** a member reads an installed identifier spelled in upper case
- **THEN** the system answers 404

#### Scenario: Empty identifier

- **WHEN** a member requests `/api/2.0/apps/`
- **THEN** the system answers 200 with the same array `getAll` returns

### Requirement: Reading the settings document

`getSettings` (`GET /api/2.0/apps/{id}/settings`) SHALL return the saved document alone, as it was
saved, with `count` 1; while the portal has never saved one or has dropped it, the response SHALL be empty
with `count` 0. A falsy document (`0`, `false`, `""`, `[]`, `{}`) SHALL be returned as such, never as
"nothing saved". The document SHALL be readable while the application is switched off. Unknown
identifier: 404. Anonymous: 401.

#### Scenario: Never saved

- **WHEN** a member reads the settings of an application the portal never configured
- **THEN** the system answers 200 with an empty `response` and `count` 0

#### Scenario: Falsy document kept

- **WHEN** the owner saves `0` (or `false`, `""`, `[]`, `{}`) and a member reads the settings
- **THEN** the system answers 200 with exactly that value and `count` 1

#### Scenario: Application switched off

- **WHEN** a document is saved and the application is then switched off
- **THEN** `getSettings` still returns the document and `get` reports `enabled` false

### Requirement: Who may read and who may write

Every signed-in member — owner, DocSpace admin, Room admin, User and Guest — SHALL read the list, an
application and its settings, whether the application is on or off. `setEnabled` and `setSettings` SHALL
require the portal-settings right: the owner and a DocSpace admin succeed, a Room admin, a User and a
Guest SHALL be refused with 403 "Access denied" and nothing changes. Without credentials every operation
SHALL answer 401, and a refused write SHALL leave the state untouched.

#### Scenario: Guest reads a configured application

- **WHEN** a Guest calls `getAll`, `get` and `getSettings` for an application with a saved document
- **THEN** all three answer 200 and report the same flag and document the owner sees

#### Scenario: DocSpace admin writes

- **WHEN** a DocSpace admin switches an application, saves a document and then drops it
- **THEN** each call answers 200 and the reads reflect the change

#### Scenario: Room admin, User or Guest writes

- **WHEN** a Room admin, a User or a Guest calls `setEnabled` or `setSettings` on an enabled or a disabled application
- **THEN** the system answers 403 and the flag and the document are unchanged

#### Scenario: Anonymous write

- **WHEN** an anonymous caller sends `setEnabled` or `setSettings`
- **THEN** the system answers 401 and the application is unchanged

### Requirement: Order of checks on a write

A write SHALL be checked in this order: the request must carry a JSON body (else 415), the body must bind
to the request model (else 400), the caller must hold the portal-settings right (else 403), and the
identifier must be configured (else 404). A caller without the right therefore SHALL get 403 for an
unknown identifier, with the same message as for a real one, so a refusal never reveals whether an
application exists.

#### Scenario: Member without the right and an unknown identifier

- **WHEN** a User calls `setEnabled` or `setSettings` with `no-such-app`
- **THEN** the system answers 403 with the same message it gives for a configured identifier

#### Scenario: Unbindable body from a member without the right

- **WHEN** a User sends `{"enabled":"true"}` or `{}` to `setEnabled`, or `{"settings":{"a":` or `5` to `setSettings`
- **THEN** the system answers 400, not 403

#### Scenario: Bindable but empty body from a member without the right

- **WHEN** a User sends `{"a":5}` or `{"settings":null}` to `setSettings`
- **THEN** the system answers 403

### Requirement: Switching an application on or off

`setEnabled` (`PUT /api/2.0/apps/{id}/enabled`) with body `{"enabled": <bool>}` SHALL store the flag for
the current portal, overriding the configured default, and answer 200 with the application in its new
state, the saved document untouched. Repeating the same value SHALL change nothing; the last request
decides the final state. Switching one application SHALL leave every other application and the list
length alone. Unknown identifier: 404 and nothing is created.

#### Scenario: Owner flips the flag

- **WHEN** the owner sends the opposite of the current `enabled`
- **THEN** the system answers 200 with `id` and the new `enabled`, and `get` and `getAll` report it

#### Scenario: Idempotent switch

- **WHEN** the owner sends the value the application already has
- **THEN** the system answers 200 and the state is identical to before

#### Scenario: Switching keeps the document

- **WHEN** a document is saved and the application is switched off and on again
- **THEN** `getSettings` returns the document unchanged after each switch

#### Scenario: Unknown identifier

- **WHEN** the owner calls `setEnabled` with `no-such-app`
- **THEN** the system answers 404 and the list is unchanged

### Requirement: Body rules of the switch

The `enabled` field of `setEnabled` SHALL be mandatory and a JSON boolean: a body without it (`{}`), with
`null`, a string (`"true"`), a number, an array, an object, malformed JSON or a bare `null` body SHALL
answer 400 and change nothing. A request without a body SHALL answer 415 and change nothing.

#### Scenario: Missing field

- **WHEN** the owner sends `{}` to `setEnabled` for an enabled application
- **THEN** the system answers 400 and the application stays enabled

#### Scenario: Wrong type

- **WHEN** the owner sends `{"enabled":"true"}`, `{"enabled":1}` or `{"enabled":null}`
- **THEN** the system answers 400 and the flag and the document are unchanged

#### Scenario: No body

- **WHEN** the owner sends `setEnabled` with no body
- **THEN** the system answers 415 and the application is unchanged

### Requirement: Saving the settings document

`setSettings` (`PUT /api/2.0/apps/{id}/settings`) with body `{"settings": <json>}` SHALL store any valid
JSON value verbatim for the current portal, replacing the whole stored document rather than merging into
it, without changing `enabled`. It SHALL answer 200 with the application carrying the stored document, and
`getSettings`, `get` and `getAll` SHALL return the same value. Saving twice is idempotent. Unknown
identifier: 404 and nothing is created.

#### Scenario: Any JSON value round-trips

- **WHEN** the owner saves `{"a":1}`, `[1,"two",false]`, `"plain string"`, `42`, `-7`, `3.14159`, `true`, a 20-level nested object or a document with Unicode and control characters
- **THEN** the answer, `getSettings`, `get` and `getAll` all return that value unchanged

#### Scenario: Replace, never merge

- **WHEN** `{"a":1,"b":2}` is saved and then `{"a":3}`
- **THEN** `getSettings` returns `{"a":3}` with no `b`

#### Scenario: Saving does not switch

- **WHEN** the owner saves a document while the application is off (or on)
- **THEN** the answer and `get` report the flag as it was, and the document applies once the application is switched on

#### Scenario: Two applications are independent

- **WHEN** a document is saved for one application
- **THEN** the flag and document of another application are unchanged

### Requirement: Dropping the settings document

`settings` `null`, and a body that binds but has no `settings` member (`{}`, `{"survives":false}`), SHALL
drop the stored document: the call answers 200 with no `settings`, `getSettings` answers an empty response
with `count` 0, `get` and `getAll` report no `settings`, and `enabled` is untouched. Dropping is
repeatable and a new document can be saved afterwards.

#### Scenario: Null drops the document

- **WHEN** the owner saves `{"settings":null}` twice after saving a document
- **THEN** both calls answer 200 with no `settings`, every reader reports none and `enabled` is unchanged

#### Scenario: Body without the settings member

- **WHEN** the owner sends `{}` or `{"survives":false}` to `setSettings`
- **THEN** the system answers 200 with the `id` and the stored document is gone

### Requirement: Body rules of the settings write

A body that is not a JSON object with an optional `settings` member — truncated or malformed JSON, a
trailing comma, an unquoted key, a bare number, string or array, a bare `null`, or an empty string — SHALL
answer 400 and leave the saved document and the flag untouched. A request without a body SHALL answer 415
and leave the document untouched.

#### Scenario: Malformed body

- **WHEN** the owner sends `{"settings":{"a":`, `{"settings":{"a":1,}}`, `5`, `"text"`, `[1,2]`, `null` or an empty body string
- **THEN** the system answers 400 and `getSettings` still returns the previously saved document

#### Scenario: No body

- **WHEN** the owner sends `setSettings` with no body
- **THEN** the system answers 415 and the document survives

### Requirement: Consistency of the three readers

After every change — a switch, a save, a drop — `get`, `getSettings` and the matching entry of `getAll`
SHALL agree on `enabled` and `settings`, and repeated reads SHALL return identical answers without
changing anything. Every configured application SHALL be reachable by `get`, switchable and configurable
in the same way.

#### Scenario: Lifecycle

- **WHEN** the owner saves a document, switches off, switches on, restores the original flag and drops the document
- **THEN** after each step the three readers agree, and at the end the state equals the original snapshot

#### Scenario: Every configured application

- **WHEN** the owner, for each listed application, saves `{"probe":true}`, flips the flag and flips it back
- **THEN** each application reports the restored flag and the probe document

### Requirement: Per-portal storage and configured defaults

The set of applications and the default `enabled` of each SHALL come from the installation configuration
and SHALL NOT be changeable through the API. A portal SHALL hold no stored row for an application until
its first write; the first write SHALL create the row with the configured default for the field it does
not set. A stored flag or document SHALL belong to one portal only. Dropping the document SHALL keep the
row with the flag; no operation removes a row.

#### Scenario: Default flag before any write

- **WHEN** a fresh portal reads an application the configuration declares as enabled
- **THEN** `enabled` is true and `settings` is empty although the portal stored nothing

#### Scenario: Two portals

- **WHEN** portal A switches an application off
- **THEN** portal B still reads the configured default for it

### Requirement: Clients are told when an application is switched

After `setEnabled` stores the flag the system SHALL queue a real-time event to the portal's connected
clients naming the application `id` and its new `enabled`, so an open interface shows or hides the module
without a reload; a failure to deliver SHALL NOT fail the call. `setSettings` SHALL send no event; clients
pick a new document up on their next read. No audit-trail event is written for either write.

#### Scenario: Application switched off

- **WHEN** the owner switches an application off
- **THEN** the connected clients of that portal receive the event with the `id` and `enabled` false

#### Scenario: Document saved

- **WHEN** the owner saves a settings document
- **THEN** no real-time event is sent

### Requirement: Scope of the flag and the document

The `enabled` flag and the settings document SHALL be data for the clients only: no other server operation
SHALL be refused, hidden or altered because an application is switched off, and the server SHALL never
read or validate the shape of the document beyond its being valid JSON.

#### Scenario: Application switched off

- **WHEN** `ai-rooms` is switched off for a portal
- **THEN** every other API operation of the portal behaves exactly as before
