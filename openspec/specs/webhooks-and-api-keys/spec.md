# webhooks-and-api-keys Specification

## Purpose

Defines the two Developer Tools integrations the server offers to portal members: **webhooks** — subscriptions
that make the portal POST a signed JSON payload to an external URL whenever one of the subscribed events happens
(`settings/webhook*` endpoints), together with the published webhook contract the payloads follow — and **API
keys** — long-lived secrets that authenticate requests as the account that created them, optionally narrowed to
a set of scopes (`keys` endpoints). It also defines the portal-wide Developer Tools access rule
(`settings/devtoolsaccess`) that governs who may use either of them.

Sources: the integration tests in `web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks` (including the delivery
suites that run a receiver in the test process), `06_ApiKeys`, `02_Settings/AccessToDevTools` and
`05_Security/AccessToDevTools`, the unit tests in `common/Tests/ASC.Core.Common.Tests/Webhooks`, the regression
tests in `products/ASC.People/Tests/Tests/00_Regressions`, the OpenAPI document
`sdk/docspace-api-spec/docspace-backend.yaml`, the published webhook contract
`sdk/docspace-api-spec/webhooks/docspace-webhooks.yaml` and the product code where the tests stop (cited in
`verification.md`), checked against the release notes (v2.0, v3.1, v3.7) and revised after the merge of
release/v4.1.0. Where the release notes or the endpoint descriptions and the server disagree, this
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

- **WHEN** a Guest creates a webhook, lists the webhooks, reads the delivery journal, retries a delivery, creates an API key, lists the API keys or reads the permission catalogue
- **THEN** every call answers 403 with "Access denied" (the API-key creation names the guest role instead)

#### Scenario: Anonymous caller is refused

- **WHEN** an unauthenticated request creates or lists webhooks, reads the journal, creates, lists, updates or deletes an API key, or reads the permission catalogue or the current key
- **THEN** the call answers 401

#### Scenario: Restriction switched on

- **WHEN** the DocSpace Owner sets `limitedAccessForUsers` to `true` and a Room admin or a User creates an API key
- **THEN** the call answers 403 with "Access denied" — the restriction applies to Room admins as well as Users, although the setting is named after Users
- **AND** the DocSpace Owner and a DocSpace admin still create a key with 200

#### Scenario: Restriction blocks every webhook endpoint

- **WHEN** `limitedAccessForUsers` is `true` and a User calls `getTenantWebhooks`, `createWebhook`, `updateWebhook`, `enableWebhook`, `removeWebhook`, `getWebhooksLogs`, `retryWebhook` or `retryWebhooks` — even for a subscription and a delivery record of their own created before the restriction
- **THEN** every call answers 403; a Room admin listing the webhooks is refused the same way
- **AND** a DocSpace admin still creates a subscription, lists the webhooks and reads the journal, and the User still reads the trigger catalogue

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
`secretKey`, `enabled`, an optional `ssl`, `triggers` (a bitmask of trigger ids, `0` meaning every event the
creator's role may receive) and an optional `targetId` that limits the subscription to one entity. The
subscription SHALL belong to the caller and the answer SHALL be the stored configuration without the secret
key. Every call adds another subscription. Creation is recorded in the audit trail (`WebhookCreated`) and
announced to the portal's connected clients.

#### Scenario: Owner creates an enabled webhook

- **WHEN** the DocSpace Owner posts a name, a URL that answers a HEAD request, a secret key and `enabled: true`
- **THEN** the call answers 200 with a non-zero `id`, the given `name` and `uri`, `enabled` true, `createdBy` and `createdOn` filled in
- **AND** the answer carries `id`, `name`, `uri`, `enabled`, `ssl`, `triggers`, `targetId`, `createdBy`, `createdOn`, `modifiedBy`, `modifiedOn`, `lastFailureOn`, `lastFailureContent` and `lastSuccessOn`

#### Scenario: Duplicate name and URL

- **WHEN** a second subscription with the same `name` and `uri` is created in the portal
- **THEN** the call answers 400

#### Scenario: Triggers and SSL are stored

- **WHEN** the DocSpace Owner creates a webhook with `triggers: FileCreated`, or one with `ssl: false` and `enabled: false` for an `http` target
- **THEN** the answer carries `triggers` equal to `FileCreated`, and the listing shows `ssl` false and `enabled` false — a false value is stored, not replaced by a default

#### Scenario: SSL left out follows the scheme

- **WHEN** the body has no `ssl`
- **THEN** it is `false` for an `http` target and `true` for an `https` target; `ssl: true` with an `http` target answers 400

#### Scenario: Target of user or group events must exist

- **WHEN** `triggers` holds only user or group events and `targetId` is not the GUID of a user or group the creator may see — a random GUID, a non-GUID, a user id for a group event, or a member hidden from a User
- **THEN** the call answers 400 with the same message in every case, so the answer does not reveal whether the GUID exists
- **AND** an existing user the creator may see is stored as the target; a target of file, folder, room or form events, or of a subscription mixing those with user or group events, is stored as sent without a lookup

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

- **WHEN** the URL answers the HEAD request with 200
- **THEN** the subscription is stored

### Requirement: Trigger catalogue and per-role availability

`GET /api/2.0/settings/webhook/triggers` (`getWebhookTriggers`) SHALL return the fixed catalogue of events in
display order, each with `name` (the event name used in payloads), `id` (the bit value) and `available`
(whether the caller's user type may subscribe to it). A subscription whose `triggers` bitmask contains an event
the subscription creator's type may not subscribe to SHALL be refused with 400 ("Trigger is not available");
`triggers = 0` is accepted from every type that may use webhooks and stands for the events that type may
receive.

#### Scenario: Catalogue is complete and ordered

- **WHEN** any authenticated member requests the catalogue
- **THEN** the call answers 200 with a non-empty list whose first entry has a name — `*`, user, group, file, folder, room, form and agent events in this order with these bit values: `*` 0, `user.created` 1, `user.invited` 2, `user.updated` 4, `user.deleted` 8, `group.created` 16, `group.updated` 32, `group.deleted` 64
- **AND** `file.created` 128, `file.uploaded` 256, `file.updated` 512, `file.trashed` 1024, `file.deleted` 2048, `file.restored` 4096, `file.copied` 8192, `file.moved` 16384, `file.downloaded` 2^34 (listed after `file.moved`)
- **AND** `folder.created` 32768, `folder.updated` 65536, `folder.trashed` 131072, `folder.deleted` 262144, `folder.restored` 524288, `folder.copied` 1048576, `folder.moved` 2097152, `folder.downloaded` 2^35 (listed after `folder.moved`)
- **AND** `room.created` 4194304, `room.updated` 8388608, `room.archived` 16777216, `room.deleted` 33554432, `room.restored` 67108864, `room.copied` 134217728, `form.submit` 268435456, `form.filled.out` 536870912, `form.stopped` 1073741824
- **AND** `agent.created` 2^31, `agent.updated` 2^32, `agent.deleted` 2^33

#### Scenario: Availability per user type

- **WHEN** the catalogue is read by a DocSpace admin (the DocSpace Owner counts as one), a Room admin and a User
- **THEN** every event is `available` to the DocSpace admin and the Room admin
- **AND** for the User every event is available except `user.created`, `user.invited`, `group.created`, `group.updated`, `group.deleted`, `room.created`, `room.copied` and `agent.created`; the catalogue reports `user.created` as not available to a User

#### Scenario: User subscribes to an admin-only event

- **WHEN** a User creates a webhook with `triggers: UserCreated`
- **THEN** the call answers 400 with "Trigger is not available", before the target URL is probed

#### Scenario: Owner subscribes to a file event

- **WHEN** the DocSpace Owner creates a webhook with `triggers: FileCreated`
- **THEN** the call answers 200 and the stored `triggers` equal `FileCreated`

#### Scenario: Catch-all is narrowed to the creator's role

- **WHEN** a User's subscription has `triggers: 0` and a user is added to the portal
- **THEN** no delivery is queued for it, because `user.created` is not available to a User; the same subscription receives `file.created` for a file the User may read

### Requirement: Visibility of webhook configurations

`GET /api/2.0/settings/webhook` (`getTenantWebhooks`) SHALL list the subscriptions of the portal that the caller
may see — every subscription for the DocSpace Owner and DocSpace admins, only the ones the caller created for a
Room admin or a User — each as `configs` (the configuration without the secret) plus `status`, the HTTP status
of its most recent delivery, `0` when nothing has been delivered yet. The list is neither paginated nor ordered.

#### Scenario: Owner sees a created webhook

- **WHEN** the DocSpace Owner creates a webhook and lists the webhooks
- **THEN** exactly one entry has that `configs.id`, with the created `name` and `uri`

#### Scenario: Room admin and User list only their own

- **WHEN** the Owner, another member and a Room admin (or a User) each create a subscription, and the Room admin (or User) lists the webhooks
- **THEN** the list holds exactly the caller's own subscription

#### Scenario: Owner and DocSpace admin list everything

- **WHEN** the Owner and a User each create a subscription, and the Owner or a DocSpace admin lists the webhooks
- **THEN** the list contains both

#### Scenario: Member sees own webhook after another member's attempt on it

- **WHEN** User B creates a webhook and User A fails to remove it, then User B lists the webhooks
- **THEN** User B's list still contains the webhook

### Requirement: Updating a webhook configuration

`PUT /api/2.0/settings/webhook` (`updateWebhook`, the subscription addressed by `id` in the body) SHALL replace
`name`, `uri`, `enabled`, `triggers` and `targetId` with the values of the request — fields left out take their
defaults — and SHALL replace `secretKey` only when a non-empty one is sent and `ssl` only when it is sent. The
subscription is looked up and its ownership checked first; then the same validation as on creation runs,
trigger availability being judged by the creator's type. The answer carries `modifiedOn` and `modifiedBy`;
the update is recorded in the audit trail (`WebhookUpdated`).

#### Scenario: Owner renames and re-targets

- **WHEN** the DocSpace Owner updates a webhook with a new `name`, `uri` and `secretKey`
- **THEN** the call answers 200 with the same `id`, the new `name` and `uri`, and `modifiedOn` set

#### Scenario: Triggers and target replaced

- **WHEN** a subscription to `FileCreated` is updated with `triggers: FileUpdated | FolderCreated` and `targetId: "12345"`
- **THEN** the answer and the listing carry the new triggers and the target; a `targetId` of user or group events that names no user or group the creator may see answers 400 and leaves the stored target unchanged

#### Scenario: Secret and SSL kept when left out

- **WHEN** the update sends an empty `secretKey`, or no `ssl` at all
- **THEN** the next delivery is still signed with the stored secret, and the stored `ssl` stays as it was — a stored `ssl` true with an `http` target makes such an update answer 400 instead of resetting it; `ssl: true` sent for an `http` target answers 400 as well

#### Scenario: Unknown id

- **WHEN** no subscription with that `id` exists in the portal, whatever the body holds beyond the required `name` and `uri`
- **THEN** the call answers 404, even when the body would fail validation

#### Scenario: Another member's subscription

- **WHEN** a Room admin or User updates a subscription created by someone else, with a valid or an invalid body
- **THEN** the call answers 403 with "Access denied" and nothing changes
- **AND** a DocSpace admin may update any subscription; the triggers are then checked against the creator's type, so adding `UserCreated` to a User's subscription answers 400 while adding `FileUpdated` succeeds and leaves `createdBy` unchanged

### Requirement: Switching a webhook on or off

`PUT /api/2.0/settings/webhook/enable` (`enableWebhook`) SHALL set `enabled` of the subscription addressed by
`id` and leave every other stored value unchanged. Switching a subscription **on** SHALL re-run the validation
against the stored values — URL, secret and the triggers against the creator's current type, not the target —
switching **off** is not validated. While a subscription is off, its events SHALL be dropped, not queued. A
DocSpace admin may switch any subscription; anybody else only their own. The change is announced to the
connected clients.

#### Scenario: Owner disables own webhook

- **WHEN** the DocSpace Owner creates an enabled webhook and sends `enabled: false` with its `id`
- **THEN** the call answers 200 with the same `id`, `name` and `uri`, `enabled` false and `modifiedOn` set
- **AND** the body is validated as a full configuration (`name` and `uri` required) although only `id` and `enabled` are read

#### Scenario: Switching on re-validates the stored values

- **WHEN** a subscription is switched on and its stored values no longer pass the URL rules, the HEAD probe, the password rules on the stored secret, or a stored trigger is no longer open to the creator's type
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
anybody else only their own. The removal is recorded in the audit trail (`WebhookDeleted`) and announced to the
connected clients.

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
or contains the event, whose `targetId` (when set) equals the identifier of the entity concerned, whose
creator may still receive the event at that moment — an active member whose current type may subscribe to it,
a DocSpace admin while the Developer Tools are restricted — and whose creator is allowed to see the entity.
The subscription's own creator always receives events about actions they perform themselves.

#### Scenario: Visibility of the entity to the creator

- **WHEN** an event about a file, folder or room, about an account, or about a group is published
- **THEN** the subscription's creator must be able to read the entry, must be allowed to view that account, or must be a DocSpace admin or hold the right to read groups respectively
- **AND** a User's subscription to `file.created` or `folder.created` gets nothing for an entry in a room the User cannot read; a Room admin's subscription to `room.created` gets nothing for a room they are not in; a User gets `user.updated` about themselves and never about another member; a Room admin's subscription to `group.created` gets the event

#### Scenario: Actions that produce the events

- **WHEN** a product action is performed
- **THEN** `user.created` is produced by adding a member through the people API or signing up through a third-party account; `user.invited` by sending an invitation by e-mail, for every address not yet registered; `user.updated` by profile, status, type, culture, photo and activation changes and by closing one's own profile; `user.deleted` by deleting a member or by a reassignment that deletes the source profile; `group.created` and `group.deleted` once per group; `group.updated` only when the name, the manager or the members actually change — a call that repeats the current values publishes nothing
- **AND** `file.*` and `folder.*` by create, upload, edit, lock, rename, reorder, owner change, trash, delete, restore, copy and move of files and folders; `file.downloaded` and `folder.downloaded` by a bulk download, once per listed entry; `folder.restored` and `file.restored` by a move out of the trash
- **AND** `room.created` by creating a room that is not an AI agent, from a template or by an import; `room.updated` by every room update, owner change, quota change, cover and logo change; `room.archived`, `room.deleted` and `room.restored` by room management; `room.copied` by copying or duplicating a room; `agent.created`, `agent.updated` and `agent.deleted` instead of the room events when the room is an AI agent
- **AND** `form.submit`, `form.filled.out` and `form.stopped` as specified in `form-filling-workflow` ("Webhook events of the workflow"); the published contract lists, for every event, the exact REST operations that raise it and under which condition

#### Scenario: Deletion payload describes the entity as it was

- **WHEN** a user, a group, a file or a folder is deleted
- **THEN** the `user.deleted`, `group.deleted`, `file.deleted` or `folder.deleted` payload is built before the deletion and sent after it, so it still carries the user's groups, the group's member count, the file's parent or the folder's file count

#### Scenario: Subscription not covering the event

- **WHEN** an event happens and a subscription's `triggers` mask does not contain it and is not `0`
- **THEN** no delivery is queued for that subscription

#### Scenario: Creator cannot see the entity

- **WHEN** a User's subscription covers `file.created` and another member creates a file the User cannot read
- **THEN** no delivery is queued for that subscription

#### Scenario: Creator no longer entitled

- **WHEN** the creator of a subscription has been terminated, demoted to a type the event is not available to, or is a User or Room admin while the Developer Tools are restricted
- **THEN** no delivery is queued for that subscription, although the creator could subscribe when it was saved

#### Scenario: Target limits the subscription

- **WHEN** a subscription has `targetId` set to the id of one user or one group and another user or group changes
- **THEN** no delivery is queued; the change of the target itself is delivered with `webhook.target` naming the id and the family (`user`, `group`); a subscription without a target carries no `webhook.target`

### Requirement: Payload of an event

Every delivery SHALL carry, in `payload`, the schema of the event's family as the published contract defines
it: a user, a group, a file, a folder, a room (also for `agent.*` events) or a submitted form. The payload
SHALL be built once per event and cut for each subscription creator by the rules the REST API applies to a
reader of the same entity, SHALL carry no reader-dependent state, absolute links and avatars, enums as
numbers and timestamps in UTC whole seconds.

#### Scenario: File, folder and room payloads

- **WHEN** a file, a folder, a room and an AI agent are created
- **THEN** `file.created` carries the file's id, parent, entry type, extension, file type, version, author and absolute view and web links without share tokens; `folder.created` the folder shape without room or file fields; `room.created` the room type, folder type and settings such as indexing and download restriction; `agent.created` the room shape of the AI room and no `room.created` beside it
- **AND** none of them carries `access`, `security`, `shared`, `canShare`, `isFavorite` or `shareRecord`; an entry inside a room carries the type of that room, a room carries none

#### Scenario: Payload is cut per subscriber

- **WHEN** the same event reaches subscriptions of the Owner and of a User who may read the entry
- **THEN** the Owner's `createdBy` carries the author's profile link and the User's does not; the quota of a room goes only to a creator who may edit or delete it (an Editor of the room and the owner of an archived room included); a user payload gives quota and `createdBy` to a DocSpace admin, the quota also to the user themselves, and no contacts to a User or Guest about anybody else

#### Scenario: Lock and group membership in the payload

- **WHEN** a file is locked, or a group is created or updated
- **THEN** `file.updated` carries `locked` true and who locked it; the group payload carries the manager as a summary without e-mail and the member count, never the member list; `folder.downloaded` of a room carries the folder payload with the room's folder type

### Requirement: Published webhook contract

The server SHALL publish the webhook contract — an OpenAPI 3.1 document, version 1.0.0, with one entry per
event naming its payload schema, the REST operations that raise it and the roles that may subscribe, the
headers, the serialization and retry rules and a machine-readable event-to-payload map — at
`/openapi/docspace.webhooks/1.0.yaml`. The document SHALL be generated from the payload models, the trigger
catalogue and the delivery settings, and the committed copy SHALL match what the code generates.

#### Scenario: Contract matches the code

- **WHEN** a payload model, a trigger or a delivery setting changes without the contract being regenerated
- **THEN** the unit test fails; every scalar property of the contract carries an example, and the served document is the committed one

#### Scenario: Signed fixtures of every family

- **WHEN** the webhook SDKs are generated
- **THEN** they are checked against deliveries captured from a running server — `user.*`, `group.*`, `room.created`, `folder.created`, `file.created`, `agent.created` and one automatic retry — each with its signature

### Requirement: Delivery of a webhook

For each queued event the server SHALL write a delivery record and POST a signed JSON body to the subscription
URL, the record and its subscription re-checked right before every attempt. A failed attempt SHALL be retried
on a schedule, under the same record and event id, and the record SHALL store the outcome of the latest
attempt. A subscription that keeps failing SHALL be switched off and one whose target is gone SHALL be
deleted.

#### Scenario: Request headers and body

- **WHEN** a delivery is sent
- **THEN** the request is a POST with `Accept: */*`, `Content-Type: application/json`, `x-docspace-signature-256: sha256=<HMAC-SHA256 of the body, keyed with the secret key, upper-case hex>`, `x-docspace-event-id` (a copy of `event.id`, the id of the event's first delivery record) and `x-docspace-event-timestamp` (a copy of `event.createOn`); redirects are not followed
- **AND** the body is `{ "event": { id, createOn, createBy, trigger, triggerId }, "payload": <the entity>, "webhook": { id, name, url, triggers[], target?, lastFailureOn?, lastFailureContent?, lastSuccessOn?, retryCount, retryOn? } }`, camel-cased, timestamps in UTC whole seconds ending in `Z`, with default values omitted; the receiver verifies the signature of the exact body it got

#### Scenario: Successful delivery

- **WHEN** the target answers 2xx
- **THEN** the record carries that status, the response headers and the first 8 KB of the response body, `delivery` set, `attempts` 1 and no `nextAttemptOn`; the subscription's `lastSuccessOn` is updated
- **AND** a 2xx whose body arrives only after the request timeout is still a delivery; a NUL in the stored body is dropped and a character outside the Basic Multilingual Plane is replaced

#### Scenario: Retries and the final record

- **WHEN** an attempt fails (a non-2xx status, a redirect, a timeout of 30 seconds by default, or a transport error)
- **THEN** it is retried under the same record after 1 minute, 5 minutes, 30 minutes, 2, 5 and 10 hours by default (each with 10 % jitter, configurable), every retry carrying the same `x-docspace-event-id`, an increasing `retryCount` and the subscription's `lastFailureOn`, `lastFailureContent` and `lastSuccessOn` in UTC
- **AND** the record shows the status of the latest attempt, `attempts` and `nextAttemptOn` while another attempt is due; when the retries run out `nextAttemptOn` is empty, `delivery` stays empty and the subscription's `lastFailureOn` and `lastFailureContent` hold the last answer

#### Scenario: Failure is retried before anything is disabled

- **WHEN** a target answers 404 or redirects once and then 200, or answers 500 and then 200
- **THEN** the retry succeeds, the record ends delivered with `attempts` 2, and the subscription stays enabled

#### Scenario: URL re-validated at delivery

- **WHEN** the target URL no longer passes the rules of creation at delivery time
- **THEN** a blacklisted address deletes the subscription with its records, and any other definitive rejection (scheme, `https` demanded by `ssl`) switches it off and ends the record with a note "Not sent: the target address was rejected, and the subscription was disabled: <reason>", `attempts` 0; both are recorded in the audit trail; a host that merely fails to resolve is an ordinary failed attempt

#### Scenario: Subscription disabled or creator no longer entitled

- **WHEN** a record comes due — the first attempt or a retry — for a subscription that is now disabled, or whose creator may no longer receive the event
- **THEN** nothing is sent and no attempt is counted; a record never attempted ends with the note "Not sent: the subscription is disabled" (or the creator note), while a record with an earlier attempt keeps the target's last answer

#### Scenario: Target gone

- **WHEN** the target answers 410
- **THEN** the subscription is removed with its records, nothing is retried, and the audit trail records the deletion with "(HTTP status 410)"

#### Scenario: Failing for longer than the trusted period

- **WHEN** the retries of an event run out and the subscription has had no success (or creation) for more than the trusted period, 3 days by default
- **THEN** the subscription is switched off and the audit trail records it; a first failure on such a subscription is still retried, and a subscription that succeeded an hour earlier stays enabled when an event is given up on

#### Scenario: One sender at a time

- **WHEN** a record is claimed by a sender that dies, or another sender takes the record over while an attempt is in flight
- **THEN** the next poll claims the record once its lease expires and delivers it; an attempt whose record was taken over writes neither the record nor the subscription's bookkeeping; an edit of the subscription during an attempt is kept

### Requirement: Delivery journal

`GET /api/2.0/settings/webhooks/log` (`getWebhooksLogs`) SHALL return the delivery records of the portal,
newest first, paged with `startIndex` and `count` (1–100, default 100; an out-of-range `count` answers 400),
the total number of matching records reported beside the response. For a caller who is not a DocSpace admin
the records SHALL be limited to the deliveries of their own subscriptions. Records whose attempts are over may
be deleted after a configurable retention period; by default they are kept.

#### Scenario: Owner reads the journal

- **WHEN** the DocSpace Owner requests the journal with `count=10`
- **THEN** the call answers 200 with a list
- **AND** each record carries `id`, `eventId`, `retriedBy`, `configName`, `trigger`, `creationTime`, `method` (`POST`), `route` (the subscription's current URL), `requestHeaders`, `requestPayload`, `responseHeaders`, `responsePayload`, `status` (`0` while no answer has come back), `delivery`, `attempts` and `nextAttemptOn`

#### Scenario: Filters combine with AND

- **WHEN** the journal is requested with `deliveryFrom`, `deliveryTo`, `hookUri` (exact match), `configId`, `eventId`, `groupStatus`, `userId` (the subscription's creator) or `trigger` (0 = all)
- **THEN** only the records matching every given filter are returned: `groupStatus` is a bitmask of the not-sent (status 0), 2xx, 3xx, 4xx and 5xx groups combining as a union (0 = no filter); the delivery range matches only delivered records; `hookUri` is compared as a whole; `eventId` returns every record of one event, newest first, and the `id` of a manual retry returns that record alone
- **AND** paging reports `total` as the number of all matching records and pages do not overlap

#### Scenario: Non-admin reads own records only

- **WHEN** a Room admin or a User reads the journal, also with `configId` of another member's subscription or `userId` of the Owner
- **THEN** only the records of their own subscriptions come back — another member's `configId` yields an empty list and `userId` is forced to the caller; a DocSpace admin reads the records of every member

#### Scenario: Guest and anonymous

- **WHEN** a Guest requests the journal
- **THEN** the call answers 403 with "Access denied"
- **AND** an unauthenticated request answers 401

#### Scenario: Clients are told about journal changes

- **WHEN** a record is written, attempted or stopped, or a subscription is created, changed, disabled or removed
- **THEN** the portal's connected clients receive a real-time event naming the subscription (and the record), many records of one subscription written within a second being announced once

### Requirement: Retrying a delivery

`PUT /api/2.0/settings/webhook/{id}/retry` (`retryWebhook`, `id` being a delivery record) SHALL queue a new
delivery record of the same event — same `eventId`, the stored body, the caller in `retriedBy` — and stop the
automatic retries still due for that event. `PUT /api/2.0/settings/webhook/retry` (`retryWebhooks`, body
`ids`, at most 100) SHALL do the same once per event, silently skipping what cannot be queued, and answer the
new records. Both are rate limited (429) and recorded in the audit trail (`WebhookRetried`).

#### Scenario: Single retry

- **WHEN** the DocSpace Owner retries one delivery record
- **THEN** the answer is a new record with its own `id`, `status` 0, no `delivery`, the body of the retried record and `eventId` equal to the first record of the event; the new record is attempted, the retried record keeps its outcome and its `nextAttemptOn` is cleared, and `retriedBy` names the caller
- **AND** an `id` of 0 answers 400, an unknown record 404, and a record of another member's subscription 403 unless the caller is a DocSpace admin; a Guest is refused with 403

#### Scenario: Resend is byte for byte

- **WHEN** a delivered record is retried, or a retry record is retried again
- **THEN** the receiver gets the same body and signature with the `x-docspace-event-id` of the event's first record, and every record of the chain carries that `eventId`

#### Scenario: Nothing to queue

- **WHEN** a delivery of the event is being sent right now, the subscription is disabled, or the record predates the current delivery format (a record never attempted and off the schedule, or a sent record whose attempts were never counted)
- **THEN** the single retry answers 409 — "The event is being sent right now", "The webhook is disabled" or "The delivery was recorded in an earlier webhook format" — and nothing is queued

#### Scenario: Empty batch

- **WHEN** the DocSpace Owner sends `ids: []`, `ids: null` or a body without `ids`
- **THEN** the call answers 200 with an empty list; 100 identifiers are accepted and 101 answer 400

#### Scenario: Batch queues one retry per event

- **WHEN** the batch names two records of different events, or the first record of an event and a retry of it
- **THEN** one new record per event comes back, each with the retried event's `eventId`

#### Scenario: Batch with a foreign record

- **WHEN** a User sends the id of a delivery record of another member's subscription, an unknown id, a record being sent right now or one of a disabled subscription
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
