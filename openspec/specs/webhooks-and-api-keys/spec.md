# webhooks-and-api-keys Specification

## Purpose

Defines the two Developer Tools integrations the server offers to portal members: **webhooks** — subscriptions
that make the portal POST a signed JSON payload to an external URL whenever one of the subscribed events happens
(`settings/webhook*` endpoints) — and **API keys** — long-lived secrets that authenticate requests as the account
that created them, optionally narrowed to a set of scopes (`keys` endpoints). It also defines the portal-wide
Developer Tools access rule (`settings/devtoolsaccess`) that governs who may use either of them.

Sources: the integration tests in `web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks`, `06_ApiKeys`,
`02_Settings/AccessToDevTools` and `05_Security/AccessToDevTools`, the regression tests in
`products/ASC.People/Tests/Tests/00_Regressions`, the OpenAPI document
`sdk/docspace-api-spec/docspace-backend.yaml` and the product code where the tests stop (cited in
`verification.md`), checked against the release notes (v2.0, v3.1, v3.7). Where the release notes or the endpoint descriptions and the server disagree, this
specification follows the server; the differences are listed in `verification.md`. The webhook events of the
form-filling workflow (`form.submit`, `form.filled.out`, `form.stopped`) are specified in
`form-filling-workflow` and only cross-referenced here.

## Requirements

### Requirement: Developer Tools access rule

The server SHALL decide whether a caller may use the Developer Tools — webhook configurations, the webhook
delivery journal and API keys — from the caller's user type and the portal setting `limitedAccessForUsers`:
the DocSpace Owner and DocSpace admins always may, a Room admin or a User only while the setting is `false`
(the default), a Guest never; anonymous is 401. A refused member SHALL be answered with 403 and the message
"Access denied". The two read-only catalogues are exceptions.

#### Scenario: Catalogues are the exceptions

- **WHEN** a member reads the trigger catalogue (`GET /api/2.0/settings/webhook/triggers`) or the API-key permission catalogue
- **THEN** the trigger catalogue is returned to every authenticated member, a Guest included, and the permission catalogue refuses only Guests

#### Scenario: Guest is refused everywhere

- **WHEN** a Guest creates a webhook, lists the webhooks, reads the delivery journal, creates an API key, lists the API keys or reads the permission catalogue
- **THEN** every call answers 403 with "Access denied" (the API-key creation names the guest role instead)

#### Scenario: Anonymous caller is refused

- **WHEN** an unauthenticated request creates or lists webhooks, reads the journal, creates, lists, updates or deletes an API key, or reads the permission catalogue or the current key
- **THEN** the call answers 401

#### Scenario: Restriction switched on

- **WHEN** the DocSpace Owner sets `limitedAccessForUsers` to `true` and a Room admin or a User creates an API key
- **THEN** the call answers 403 with "Access denied" — the restriction applies to Room admins as well as Users, although the setting is named after Users
- **AND** the DocSpace Owner and a DocSpace admin still create a key with 200

#### Scenario: Restriction switched off again

- **WHEN** `limitedAccessForUsers` is set to `true` and then back to `false`, and a Room admin or a User creates an API key
- **THEN** the call answers 200

#### Scenario: Trigger catalogue stays readable

- **WHEN** a Guest requests `GET /api/2.0/settings/webhook/triggers`
- **THEN** the call answers 200 with a non-empty list

### Requirement: Reading and changing the Developer Tools restriction

`GET /api/2.0/settings/devtoolsaccess` (`getTenantAccessDevToolsSettings`) SHALL return
`limitedAccessForUsers` and `lastModified` to every authenticated member, whatever their type.
`POST /api/2.0/settings/devtoolsaccess` (`setTenantDevToolsAccessSettings`, body `limitedAccessForUsers`)
SHALL require the portal-settings right, persist the value for the whole portal, record the change in the
audit trail and echo the saved setting. The default value is `false`.

#### Scenario: Every member reads the setting

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin, a User or a Guest reads the setting
- **THEN** the call answers 200 with the current value

#### Scenario: Anonymous read

- **WHEN** an unauthenticated request reads the setting
- **THEN** the call answers 401

#### Scenario: Owner and DocSpace admin change the setting

- **WHEN** the DocSpace Owner sets `limitedAccessForUsers` to `true`, then to `false`
- **THEN** each call answers 200 echoing the value, and a later read — by the Owner or by a DocSpace admin — returns the value last set
- **AND** a DocSpace admin changing it is answered 200 as well

#### Scenario: Other members cannot change it

- **WHEN** a Room admin, a User or a Guest posts the setting
- **THEN** the call answers 403
- **AND** an unauthenticated request answers 401

### Requirement: Creating a webhook configuration

`POST /api/2.0/settings/webhook` (`createWebhook`) SHALL create a subscription with a `name`, a `uri`, a
`secretKey`, `enabled`, `ssl`, `triggers` (a bitmask of trigger ids, `0` meaning every event) and an optional
`targetId` that limits the subscription to one entity. The subscription SHALL belong to the caller and the
answer SHALL be the stored configuration without the secret key. Every call adds another subscription.
Creation is recorded in the audit trail (`WebhookCreated`).

#### Scenario: Owner creates an enabled webhook

- **WHEN** the DocSpace Owner posts a name, an `https` URL that answers a HEAD request, a secret key and `enabled: true`
- **THEN** the call answers 200 with a non-zero `id`, the given `name` and `uri`, `enabled` true, `createdBy` and `createdOn` filled in
- **AND** the answer carries `id`, `name`, `uri`, `enabled`, `ssl`, `triggers`, `targetId`, `createdBy`, `createdOn`, `modifiedBy`, `modifiedOn`, `lastFailureOn`, `lastFailureContent` and `lastSuccessOn`

#### Scenario: Duplicate name and URL

- **WHEN** a second subscription with the same `name` and `uri` is created in the portal
- **THEN** the call answers 400

#### Scenario: Triggers and SSL are stored

- **WHEN** the DocSpace Owner creates a webhook with `ssl: true` and `triggers: FileCreated`
- **THEN** the answer carries `ssl` true and `triggers` equal to `FileCreated`

#### Scenario: Room admin and User create their own

- **WHEN** a Room admin or a User (the restriction being off) creates a webhook
- **THEN** the call answers 200 with a non-zero `id`

#### Scenario: Missing name, URL or secret

- **WHEN** the request has no `name` or no `uri`, or is a creation without `secretKey`, or the secret does not pass the portal password rules, or `name`/`secretKey` exceed 50 characters or `targetId` 255
- **THEN** the call answers 400

### Requirement: Validation of the target URL

Before a subscription is stored, switched on or delivered to, the server SHALL validate its URL: it MUST be an
absolute `http` or `https` address; with `ssl: true` it MUST be `https`; a host given as an IP address, and
every address the host name resolves to, MUST NOT fall into the configurable address blacklist. On creation,
update and switching on, a HEAD request to the URL MUST additionally answer a success status. A URL that
fails the rules SHALL be refused with 400.

#### Scenario: Private address is refused

- **WHEN** the DocSpace Owner creates a webhook whose URL is `http://localhost`
- **THEN** the call answers 400
- **AND** by default the blacklist holds the loopback, link-local, private and carrier-grade NAT ranges of IPv4 and IPv6 (`0.0.0.0/8`, `10.0.0.0/8`, `100.64.0.0/10`, `127.0.0.0/8`, `169.254.0.0/16`, `172.16.0.0/12`, `192.168.0.0/16`, `::/128`, `::1/128`, `fc00::/8`, `fd00::/8`, `fe80::/10`), and the DNS lookup is limited to 3 seconds

#### Scenario: HEAD probe is pinned and does not follow redirects

- **WHEN** the HEAD request is sent
- **THEN** it connects to the already resolved address so that DNS rebinding cannot redirect it, does not follow redirects, and verifies the TLS certificate only when `ssl` is `true`

#### Scenario: Target that does not answer

- **WHEN** the URL does not answer the HEAD request at all (connection failure, timeout, invalid certificate under `ssl`)
- **THEN** the call answers 500

#### Scenario: Target that redirects is refused

- **WHEN** the URL answers the HEAD request with a redirect (3xx) or any non-success status
- **THEN** the call answers 400 ("webhook URL not available")

#### Scenario: Public target that answers

- **WHEN** the URL is `https://example.com/?id=...`, which answers the HEAD request with 200
- **THEN** the subscription is stored

### Requirement: Trigger catalogue and per-role availability

`GET /api/2.0/settings/webhook/triggers` (`getWebhookTriggers`) SHALL return the fixed catalogue of events in
display order, each with `name` (the event name used in payloads), `id` (the bit value) and `available`
(whether the caller's user type may subscribe to it). A subscription whose `triggers` bitmask contains an event
the caller's type may not subscribe to SHALL be refused with 400 ("Trigger is not available"); `triggers = 0`
(all events) is accepted from every type that may use webhooks.

#### Scenario: Catalogue is complete and ordered

- **WHEN** any authenticated member requests the catalogue
- **THEN** the call answers 200 with a non-empty list whose first entry has a name — `*`, user, group, file, folder, room, form and agent events in this order with these bit values: `*` 0, `user.created` 1, `user.invited` 2, `user.updated` 4, `user.deleted` 8, `group.created` 16, `group.updated` 32, `group.deleted` 64
- **AND** `file.created` 128, `file.uploaded` 256, `file.updated` 512, `file.trashed` 1024, `file.deleted` 2048, `file.restored` 4096, `file.copied` 8192, `file.moved` 16384
- **AND** `folder.created` 32768, `folder.updated` 65536, `folder.trashed` 131072, `folder.deleted` 262144, `folder.restored` 524288, `folder.copied` 1048576, `folder.moved` 2097152
- **AND** `room.created` 4194304, `room.updated` 8388608, `room.archived` 16777216, `room.deleted` 33554432, `room.restored` 67108864, `room.copied` 134217728, `form.submit` 268435456, `form.filled.out` 536870912, `form.stopped` 1073741824
- **AND** `agent.created` 2^31, `agent.updated` 2^32, `agent.deleted` 2^33, `file.downloaded` 2^34, `folder.downloaded` 2^35

#### Scenario: Availability per user type

- **WHEN** the catalogue is read by a DocSpace admin (the DocSpace Owner counts as one), a Room admin and a User
- **THEN** every event is `available` to the DocSpace admin and the Room admin
- **AND** for the User every event is available except `user.created`, `user.invited`, `group.created`, `group.updated`, `group.deleted`, `room.created`, `room.copied` and `agent.created`

#### Scenario: User subscribes to an admin-only event

- **WHEN** a User creates a webhook with `triggers: UserCreated`
- **THEN** the call answers 400

#### Scenario: Owner subscribes to a file event

- **WHEN** the DocSpace Owner creates a webhook with `triggers: FileCreated`
- **THEN** the call answers 200 and the stored `triggers` equal `FileCreated`

### Requirement: Visibility of webhook configurations

`GET /api/2.0/settings/webhook` (`getTenantWebhooks`) SHALL list the subscriptions of the portal that the caller
may see — every subscription for the DocSpace Owner and DocSpace admins, only the ones the caller created for a
Room admin or a User — each as `configs` (the configuration without the secret) plus `status`, the HTTP status
of its most recent delivery, `0` when nothing has been delivered yet. The list is neither paginated nor ordered.

#### Scenario: Owner sees a created webhook

- **WHEN** the DocSpace Owner creates a webhook and lists the webhooks
- **THEN** exactly one entry has that `configs.id`, with the created `name` and `uri`

#### Scenario: Room admin and User list

- **WHEN** a Room admin or a User lists the webhooks
- **THEN** the call answers 200 with a list (their own subscriptions)

#### Scenario: Member sees own webhook after another member's attempt on it

- **WHEN** User B creates a webhook and User A fails to remove it, then User B lists the webhooks
- **THEN** User B's list still contains the webhook

### Requirement: Updating a webhook configuration

`PUT /api/2.0/settings/webhook` (`updateWebhook`, the subscription addressed by `id` in the body) SHALL replace
`name`, `uri`, `enabled`, `ssl`, `triggers` and `targetId` with the values of the request — fields left out take
their defaults — and SHALL replace `secretKey` only when a non-empty one is sent. The same validation as on
creation SHALL run. The answer SHALL be the updated configuration with `modifiedOn` and `modifiedBy` set; the
update is recorded in the audit trail (`WebhookUpdated`).

#### Scenario: Owner renames and re-targets

- **WHEN** the DocSpace Owner updates a webhook with a new `name`, `uri` and `secretKey`
- **THEN** the call answers 200 with the same `id`, the new `name` and `uri`, and `modifiedOn` set

#### Scenario: Unknown id

- **WHEN** the body passes validation but no subscription with that `id` exists in the portal
- **THEN** the call answers 404, because the validation (trigger availability, password rules for a new secret, URL rules and the HEAD probe) runs before the subscription is looked up

#### Scenario: Another member's subscription

- **WHEN** a Room admin or User updates a subscription created by someone else
- **THEN** the call answers 403 with "Access denied"
- **AND** a DocSpace admin may update any subscription

### Requirement: Switching a webhook on or off

`PUT /api/2.0/settings/webhook/enable` (`enableWebhook`) SHALL set `enabled` of the subscription addressed by
`id` and leave every other stored value unchanged. Switching a subscription **on** SHALL re-run the validation
against the stored values; switching **off** is not validated. While a subscription is off, its events SHALL
be dropped, not queued. A DocSpace admin may switch any subscription; anybody else only their own.

#### Scenario: Owner disables own webhook

- **WHEN** the DocSpace Owner creates an enabled webhook and sends `enabled: false` with its `id`
- **THEN** the call answers 200 with the same `id`, `name` and `uri`, `enabled` false and `modifiedOn` set
- **AND** the body is validated as a full configuration (`name` and `uri` required) although only `id` and `enabled` are read

#### Scenario: Switching on re-validates the stored values

- **WHEN** a subscription is switched on and its stored values no longer pass the URL rules, the HEAD probe or the password rules on the stored secret
- **THEN** the call answers 400

#### Scenario: Another member's webhook

- **WHEN** User A sends `enabled: false` for a webhook created by User B
- **THEN** the call answers 403 with "Access denied"

#### Scenario: Unknown id

- **WHEN** no subscription with the given `id` exists
- **THEN** the call answers 404

### Requirement: Removing a webhook configuration

`DELETE /api/2.0/settings/webhook/{id}` (`removeWebhook`) SHALL delete the subscription for good — further
events are not queued for it and the delivery records referencing it disappear from the journal — and SHALL
answer with the configuration as it was just before the removal. A DocSpace admin may remove any subscription;
anybody else only their own. The removal is recorded in the audit trail (`WebhookDeleted`).

#### Scenario: Owner removes own webhook

- **WHEN** the DocSpace Owner removes a webhook they created
- **THEN** the call answers 200 with the removed `id`, and the list no longer contains it

#### Scenario: Another member's webhook

- **WHEN** User A removes a webhook created by User B
- **THEN** the call answers 403 with "Access denied" and User B still sees the webhook in the list

#### Scenario: Unknown id

- **WHEN** no subscription with the given `id` exists, or it was already removed
- **THEN** the call answers 404

### Requirement: Which actions emit which events, and to whom

The server SHALL publish an event to every subscription of the portal that is enabled, whose `triggers` is `0`
or contains the event, whose `targetId` (when set) equals the identifier of the entity concerned, and whose
creator is allowed to see the entity. The subscription's own creator always receives events about actions
they perform themselves.

#### Scenario: Visibility of the entity to the creator

- **WHEN** an event about a file, folder or room, about an account, or about a group is published
- **THEN** the subscription's creator must be able to read the entry, must be allowed to view that account, or must be a DocSpace admin or hold the right to read groups respectively

#### Scenario: Actions that produce the events

- **WHEN** a product action is performed
- **THEN** `user.created` is produced by adding a member through the people API, confirming an invitation or signing up through a third-party account; `user.invited` by sending an invitation by e-mail; `user.updated` and `user.deleted` by profile and type changes and by deletion or reassignment of a member; `group.created`, `group.updated` and `group.deleted` by group management
- **AND** `file.*` and `folder.*` by create, upload, edit, trash, delete, restore, copy and move of files and folders; `file.downloaded` and `folder.downloaded` by a download through the file handler or a download operation
- **AND** `room.created` by creating a room that is not an AI agent; `room.updated`, `room.archived`, `room.deleted` and `room.restored` by room management; `room.copied` by copying a room (including creating one from a template); `agent.created` by creating an AI agent room; `agent.updated` and `agent.deleted` by AI agent management
- **AND** `form.submit`, `form.filled.out` and `form.stopped` as specified in `form-filling-workflow` ("Webhook events of the workflow")

#### Scenario: Subscription not covering the event

- **WHEN** an event happens and a subscription's `triggers` mask does not contain it and is not `0`
- **THEN** no delivery is queued for that subscription

#### Scenario: Creator cannot see the entity

- **WHEN** a User's subscription covers `file.created` and another member creates a file the User cannot read
- **THEN** no delivery is queued for that subscription

#### Scenario: Target limits the subscription

- **WHEN** a subscription has `targetId` set to the id of one folder and a file is created elsewhere
- **THEN** no delivery is queued for that subscription

### Requirement: Delivery of a webhook

For each queued event the server SHALL write a delivery record and POST a signed JSON body to the subscription
URL. The target URL SHALL be validated again at delivery time. A delivery that fails SHALL be retried with
exponential back-off; after the attempts the record SHALL store the outcome. A subscription that keeps failing
SHALL be switched off, and one whose target is gone SHALL be deleted.

#### Scenario: Request headers and body

- **WHEN** a delivery is sent
- **THEN** the request carries `Accept: */*`, `Content-Type: application/json` and the header `x-docspace-signature-256: sha256=<HMAC-SHA256 of the body, keyed with the secret key, upper-case hex>`
- **AND** the body is `{ "event": { id, createOn, createBy, trigger, triggerId }, "payload": <the entity>, "webhook": { id, name, url, triggers[], target?, lastFailureOn?, lastFailureContent?, lastSuccessOn?, retryCount, retryOn? } }`, camel-cased, dates in UTC, with default values omitted

#### Scenario: URL re-validated at delivery

- **WHEN** the target URL no longer passes the rules of creation at delivery time
- **THEN** a blacklisted address deletes the subscription and any other validation failure switches it off, both recorded in the audit trail

#### Scenario: Retries and the final record

- **WHEN** a delivery fails (transport error, timeout or a non-success status)
- **THEN** it is retried up to a configurable number of times (5 by default) with exponential back-off starting at one second, each retry carrying `retryCount`, `retryOn` and the last failure in `webhook`
- **AND** after the attempts the record stores the final HTTP status (`500` for a non-HTTP failure), the request and response headers and bodies, and `delivery` — set only on success

#### Scenario: Successful delivery

- **WHEN** the target answers 2xx
- **THEN** the record carries that status, the response headers and body and a `delivery` time, and the subscription's `lastSuccessOn` is updated

#### Scenario: Target gone

- **WHEN** the target answers 410
- **THEN** the subscription is removed and the audit trail records the deletion with "(HTTP status 410)"

#### Scenario: Failing for longer than the trusted period

- **WHEN** a delivery fails and the subscription has had no success (or creation) for more than the trusted period, 3 days by default
- **THEN** the subscription is switched off and the audit trail records it

### Requirement: Delivery journal

`GET /api/2.0/settings/webhooks/log` (`getWebhooksLogs`) SHALL return the delivery records of the portal,
newest first, paged with `startIndex` and `count` (1–100, default 100; an out-of-range `count` answers 400),
the total number of matching records reported beside the response. For a caller who is not a DocSpace admin
the records SHALL be limited to the deliveries of their own subscriptions.

#### Scenario: Owner reads the journal

- **WHEN** the DocSpace Owner requests the journal with `count=10`
- **THEN** the call answers 200 with a list
- **AND** each record carries `id`, `configName`, `trigger`, `creationTime`, `method` (`POST`), `route`, `requestHeaders`, `requestPayload`, `responseHeaders`, `responsePayload`, `status` (`0` while pending) and `delivery`

#### Scenario: Filters combine with AND

- **WHEN** the journal is requested with `deliveryFrom`, `deliveryTo`, `hookUri` (exact match), `configId`, `eventId`, `groupStatus`, `userId` (the subscription's creator) or `trigger` (0 = all)
- **THEN** only the records matching every given filter are returned
- **AND** `groupStatus` is a bitmask combining not-sent, 2xx, 3xx, 4xx and 5xx groups (0 = no filter), and for a non-admin `userId` is forced to the caller

#### Scenario: Guest and anonymous

- **WHEN** a Guest requests the journal
- **THEN** the call answers 403 with "Access denied"
- **AND** an unauthenticated request answers 401

### Requirement: Retrying a delivery

`PUT /api/2.0/settings/webhook/{id}/retry` (`retryWebhook`, `id` being a delivery record) SHALL create a new
delivery record with the stored payload of the given one and queue it. `PUT /api/2.0/settings/webhook/retry`
(`retryWebhooks`, body `ids`) SHALL do the same for a batch, silently skipping unknown records and — for a
non-admin — records of other members' subscriptions, and answering the list of new records. Both endpoints are
rate limited (429 on a burst).

#### Scenario: Single retry

- **WHEN** the DocSpace Owner retries one delivery record
- **THEN** the answer is the new record with `status` 0 and no `delivery` yet
- **AND** an `id` of 0 answers 400, an unknown record 404, and a record of another member's subscription 403 unless the caller is a DocSpace admin

#### Scenario: Empty batch

- **WHEN** the DocSpace Owner sends `ids: []`
- **THEN** the call answers 200 with an empty list

#### Scenario: Batch with a foreign record

- **WHEN** a User sends the id of a delivery record of another member's subscription
- **THEN** the record is skipped and the answer is shorter than `ids`

### Requirement: Creating an API key

`POST /api/2.0/keys` (`createApiKey`) SHALL issue a key for the calling account: `name` (required, 1–30
characters, stored verbatim including HTML), `permissions` (catalogue scopes; `*` or an omitted field
means unrestricted) and optional `expiresInDays` (1–365; omitted means no expiry). The secret SHALL be returned
in full only in this answer and stored as a hash. Every type but Guest may create keys (subject to the
access rule); creation is recorded in the audit trail and rate limited.

#### Scenario: Every type creates a key, with or without scopes and expiry

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User creates a key with no permissions, with `*:read`, or with a list of per-area scopes, each with and without `expiresInDays: 7`
- **THEN** the call answers 200 with a non-empty `id` and `key`, the given `name`, `permissions` equal to what was sent (empty when omitted), `createBy` equal to the caller, and `expiresAt` set exactly when `expiresInDays` was sent
- **AND** the answer also carries `createOn`, `lastUsed` (empty) and `isActive` true

#### Scenario: Shape of the secret

- **WHEN** a key is created
- **THEN** `key` is `sk-` followed by 64 lower-case hex characters, and every later read shows `sk-***` followed by the last four characters (`keyPostfix`)

#### Scenario: Name kept verbatim

- **WHEN** a member creates a key named `<script>alert('xss')</script>`
- **THEN** the call answers 200 and `name` is echoed unchanged

#### Scenario: Blank, missing or long name

- **WHEN** `name` is empty or whitespace, the body has no `name` property, or `name` is 31 characters long
- **THEN** the call answers 400 ("The Name field is required." / "missing required properties including: 'name'" / "Incorrect name. Length must be less than 30")

#### Scenario: Empty permissions array

- **WHEN** `permissions` is `[]`, or names a scope that is not in the catalogue
- **THEN** the call answers 400 with "Permissions are not valid."

#### Scenario: Guest

- **WHEN** a Guest creates a key
- **THEN** the call answers 403 and the message names the guest role

### Requirement: Permission catalogue of API keys

`GET /api/2.0/keys/permissions` (`getAllPermissions`) SHALL return the fixed, alphabetically sorted list of
scope values a key may be granted: `*`, `*:read`, `*:write`, `accounts:read`, `accounts:write`,
`accounts.self:read`, `accounts.self:write`, `files:read`, `files:write`, `rooms:read`, `rooms:write` — the
same for every caller — with `count` 11. Guests are refused with 403.

#### Scenario: Catalogue for every type

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User reads the catalogue
- **THEN** the call answers 200 with exactly those eleven values in that order

### Requirement: Listing API keys

`GET /api/2.0/keys` (`getApiKeys`) SHALL return every key of the portal to the DocSpace Owner and DocSpace
admins and only the caller's own keys to a Room admin or a User, newest first, without secrets (`key` masked,
`keyPostfix`), including expired and deactivated keys with their `expiresAt` and `isActive`. Guests are refused
with 403.

#### Scenario: Owner and DocSpace admin see everybody's keys

- **WHEN** the Owner, a DocSpace admin and a User each create a key, and the Owner or the DocSpace admin lists the keys
- **THEN** the list contains all of them

#### Scenario: Room admin and User see only their own

- **WHEN** the Owner and a Room admin (or a User) each create a key, and the Room admin (or User) lists the keys
- **THEN** the list contains their own key and not the Owner's

#### Scenario: Guest

- **WHEN** a Guest lists the keys
- **THEN** the call answers 403 with "Access denied"

### Requirement: Reading the key that authenticates the request

`GET /api/2.0/keys/@self` (`getApiKey`) SHALL return the key carried in the request's `Authorization: Bearer
sk-...` header — `id`, `name`, masked `key`, `keyPostfix`, `permissions`, `isActive`, `expiresAt` — whatever
scopes the key holds. A request authenticated by a cookie or a user token has no key to report and SHALL be
answered 400.

#### Scenario: Key reads itself

- **WHEN** a member of any type creates a key and calls the endpoint with that key as bearer
- **THEN** the call answers 200 with the created `id` and `name`, `key` matching `^sk-\*{3}`, `keyPostfix` equal to the last four characters of the secret, empty `permissions` and `isActive` true

#### Scenario: Scoped key reads itself

- **WHEN** a key limited to `files:read` and `accounts:read` calls the endpoint
- **THEN** the call answers 200 and `permissions` contains both scopes

#### Scenario: User token instead of a key

- **WHEN** the Owner calls the endpoint with a user session
- **THEN** the call answers 400

### Requirement: Updating an API key

`PUT /api/2.0/keys/{keyId}` (`updateApiKey`) SHALL change `name`, `permissions` and/or `isActive` of the key;
fields left out keep their values, `name` when sent MUST be 1–30 characters and not whitespace only,
`permissions` when sent MUST be a non-empty list of catalogue values (`["*"]` makes the key unrestricted). The
caller may update their own keys and a DocSpace admin any key; an unknown or already deleted `keyId` answers
404. The change is recorded in the audit trail.

#### Scenario: Expired key is left untouched

- **WHEN** a key whose `expiresAt` has passed is updated
- **THEN** the call answers `false` and nothing is changed; a changed key answers `true`

#### Scenario: Every type updates own key

- **WHEN** the Owner, a DocSpace admin, a Room admin or a User renames their key and sets `permissions` to `files:read, rooms:read`
- **THEN** the call answers 200 with `true`, `count` 1 and a `PUT` link to the key

#### Scenario: DocSpace admin updates the Owner's key

- **WHEN** a DocSpace admin renames a key created by the Owner
- **THEN** the call answers 200 with `true`

#### Scenario: Deactivate and reactivate

- **WHEN** the Owner sends `isActive: false` for their key, then `isActive: true`
- **THEN** each call answers `true` and the listing shows `isActive` false, then true

#### Scenario: Room admin, User or Guest on the Owner's key

- **WHEN** a Room admin, a User or a Guest renames a key created by the Owner
- **THEN** the call answers 403 with "Access denied"

#### Scenario: Empty or whitespace name

- **WHEN** the Owner sends `name` `""` or `"   "`
- **THEN** the call answers 400 and the listing still shows the old name

#### Scenario: Unknown or deleted key

- **WHEN** `keyId` is the empty GUID or the id of a key already deleted
- **THEN** the call answers 404

### Requirement: Deleting an API key

`DELETE /api/2.0/keys/{keyId}` (`deleteApiKey`) SHALL remove the key permanently, so it stops authenticating
at once, and answer `true`. The caller may delete their own keys and a DocSpace admin any key; an unknown or
already deleted `keyId` answers 404. The deletion is recorded in the audit trail.

#### Scenario: Every type deletes own key

- **WHEN** the Owner, a DocSpace admin, a Room admin or a User deletes a key they created
- **THEN** the call answers 200 with `true`, `count` 1 and a `DELETE` link to the key

#### Scenario: DocSpace admin deletes the Owner's key

- **WHEN** a DocSpace admin deletes a key created by the Owner
- **THEN** the call answers 200 with `true`

#### Scenario: Room admin, User or Guest on the Owner's key

- **WHEN** a Room admin, a User or a Guest deletes a key created by the Owner
- **THEN** the call answers 403 with "Access denied"

#### Scenario: Unknown or deleted key

- **WHEN** `keyId` is the empty GUID or the key was already deleted
- **THEN** the call answers 404

### Requirement: Authenticating with an API key

A request SHALL be authenticated by an API key when its `Authorization` header is `Bearer <token>` and the
token is not a JWT and starts with `sk-`. The key SHALL be looked up by its hash within the current portal and
accepted only when it is active (`isActive`) and not expired; an unknown, deactivated or expired key SHALL be
answered 401. On success the request SHALL run as the account that created the key.

#### Scenario: Valid key

- **WHEN** a request carries a valid, active key
- **THEN** it is processed as the key's creator and `lastUsed` is set to now
- **AND** the key's scopes become the request's `scope` claims — a key without scopes (created with no `permissions` or with `*`) carries the single scope `*:write`

#### Scenario: Deactivated or expired key

- **WHEN** a request carries a key whose `isActive` is false or whose `expiresAt` has passed
- **THEN** the call answers 401

### Requirement: Scope enforcement for API-key requests

The server SHALL derive the scopes an endpoint demands from its route and method, and refuse an API-key request
with 403 unless the key holds at least one of them. The check applies to anonymous-allowed endpoints too.
Scopes never widen the account's own rights: an endpoint the creating account may not call stays refused for
the key.

#### Scenario: Route map

- **WHEN** an API-key request reaches an endpoint
- **THEN** a GET under `api/2.0/files/rooms...` accepts `rooms:read`, `rooms:write`, `*:read` or `*:write` and a write there `rooms:write` or `*:write`; any other `api/2.0/files...` GET accepts `files:read`, `files:write`, `*:read` or `*:write` and a write `files:write` or `*:write`
- **AND** a GET under `api/2.0/people/@self...` accepts `accounts.self:read`, `accounts.self:write`, `*:read` or `*:write` and a write `accounts.self:write` or `*:write`; any other `api/2.0/people...` GET accepts `accounts:read`, `accounts:write`, `*:read` or `*:write` and a write `accounts:write` or `*:write`; a GET under `api/2.0/group...` accepts `accounts:read`, `*:read` or `*:write` and a write `accounts:write` or `*:write`
- **AND** `api/2.0/keys/@self` accepts any key, and every other route accepts `*:read` or `*:write` on GET and `*:write` on a write

#### Scenario: Read-only key cannot create another key

- **WHEN** a key limited to `files:read` calls `POST /api/2.0/keys`
- **THEN** the call answers 403

#### Scenario: Account rights still apply

- **WHEN** a User's key holding `accounts:read` and `accounts:write` lists the people
- **THEN** the call answers 403, because a User has no Accounts section

#### Scenario: Scoped key reads itself

- **WHEN** a key holding only `files:read` and `accounts:read` calls `GET /api/2.0/keys/@self`
- **THEN** the call answers 200

### Requirement: Expiry of an API key

A background job (every 10 minutes) SHALL deactivate every active key whose `expiresAt` has passed and send the
"Your API key is expired" notice (`api_key_expired`) to the key's creator when that account is active and
activated, naming the key; the key name SHALL be HTML-escaped in the letter. A key with `expiresAt` in the past
SHALL already be refused for authentication before the job runs.

#### Scenario: Expired key is deactivated and announced

- **WHEN** a key's `expiresAt` passes and the job runs
- **THEN** the key's `isActive` becomes false and its creator receives the notice with the key name

#### Scenario: Key name with markup in the letter

- **WHEN** the expired key is named `<a href=//evil.com>LINK</a>`
- **THEN** the letter body contains `&lt;a href=//evil.com&gt;LINK&lt;/a&gt;` and not the raw markup
