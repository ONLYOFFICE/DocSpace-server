# webhooks-and-api-keys Specification

## Purpose

Defines the two Developer Tools integrations the server offers to portal members: **webhooks** — subscriptions
that make the portal POST a signed JSON payload to an external URL whenever one of the subscribed events happens
(`settings/webhook*` endpoints) — and **API keys** — long-lived secrets that authenticate requests as the account
that created them, optionally narrowed to a set of scopes (`keys` endpoints). It also defines the portal-wide
Developer Tools access rule (`settings/devtoolsaccess`) that governs who may use either of them.

Sources: the integration tests in `web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks`, `06_ApiKeys`,
`02_Settings/AccessToDevTools` and `05_Security/AccessToDevTools`, the regression test
`products/ASC.People/Tests/Tests/00_Regressions/WebhookTest.cs`, the OpenAPI document and a static reading of the
product code (`common/ASC.Webhooks.Core`, `web/ASC.Web.Studio/WebHooks`, `web/ASC.Web.Api/Api/Settings/WebhooksController.cs`,
`products/ASC.People/Server/Api/ApiKeysController.cs`, `common/ASC.Api.Core/Auth`), checked against the release
notes (v2.0, v3.1, v3.7). Where the release notes or the endpoint descriptions and the server disagree, this
specification follows the server; the differences are listed in `verification.md`. The webhook events of the
form-filling workflow (`form.submit`, `form.filled.out`, `form.stopped`) are specified in
`form-filling-workflow` and only cross-referenced here.

## Requirements

### Requirement: Developer Tools access rule

The server SHALL decide whether a caller may use the Developer Tools — webhook configurations, the webhook
delivery journal and API keys — from the caller's user type and the portal setting `limitedAccessForUsers`:

| Caller                              | Developer Tools                                                  |
|-------------------------------------|------------------------------------------------------------------|
| DocSpace Owner, DocSpace admin      | always                                                           |
| Room admin, User                    | only while `limitedAccessForUsers` is `false` (the default)      |
| Guest                               | never                                                            |
| anonymous                           | never (401)                                                      |

A refused member SHALL be answered with 403 and the message "Access denied". The read-only trigger catalogue
(`GET /api/2.0/settings/webhook/triggers`) and the API-key permission catalogue are exceptions: the trigger
catalogue is readable by every authenticated member, a Guest included, and the permission catalogue refuses
only Guests. The restriction applies to Room admins as well as Users, even though the setting is named after
Users.

#### Scenario: Guest is refused everywhere

- **WHEN** a Guest creates a webhook, lists the webhooks, reads the delivery journal, creates an API key, lists the API keys or reads the permission catalogue
- **THEN** every call answers 403 with "Access denied" (the API-key creation names the guest role instead)

#### Scenario: Anonymous caller is refused

- **WHEN** an unauthenticated request creates or lists webhooks, reads the journal, creates, lists, updates or deletes an API key, or reads the permission catalogue or the current key
- **THEN** the call answers 401

#### Scenario: Restriction switched on

- **WHEN** the DocSpace Owner sets `limitedAccessForUsers` to `true` and a Room admin or a User creates an API key
- **THEN** the call answers 403 with "Access denied"
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
SHALL require the portal-settings right — the DocSpace Owner and DocSpace admins — persist the value for the
whole portal, record the change in the audit trail and echo the saved setting. The default value is `false`.

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

`POST /api/2.0/settings/webhook` (`createWebhook`) SHALL create a subscription with `name` (required, at most
50 characters), `uri` (required), `secretKey` (required on creation, at most 50 characters, and it MUST satisfy
the portal password rules), `enabled`, `ssl`, `triggers` (a bitmask of `WebhookTrigger`, `0` meaning every
event) and an optional `targetId` (at most 255 characters) that limits the subscription to one entity. The
subscription SHALL belong to the caller (`createdBy`) and SHALL be stored with `createdOn`. The answer SHALL be
the stored configuration (`id`, `name`, `uri`, `enabled`, `ssl`, `triggers`, `targetId`, `createdBy`,
`createdOn`, `modifiedBy`, `modifiedOn`, `lastFailureOn`, `lastFailureContent`, `lastSuccessOn`) without the
secret key. Every call adds another subscription, but a second subscription with the same `name` and `uri` in the
portal SHALL be refused with 400. Creation is recorded in the audit trail (`WebhookCreated`).

#### Scenario: Owner creates an enabled webhook

- **WHEN** the DocSpace Owner posts a name, an `https` URL that answers a HEAD request, a secret key and `enabled: true`
- **THEN** the call answers 200 with a non-zero `id`, the given `name` and `uri`, `enabled` true, `createdBy` and `createdOn` filled in

#### Scenario: Triggers and SSL are stored

- **WHEN** the DocSpace Owner creates a webhook with `ssl: true` and `triggers: FileCreated`
- **THEN** the answer carries `ssl` true and `triggers` equal to `FileCreated`

#### Scenario: Room admin and User create their own

- **WHEN** a Room admin or a User (the restriction being off) creates a webhook
- **THEN** the call answers 200 with a non-zero `id`

#### Scenario: Missing name, URL or secret

- **WHEN** the request has no `name` or no `uri`, or is a creation without `secretKey`, or the secret does not pass the password rules, or `name`/`secretKey` exceed 50 characters
- **THEN** the call answers 400

### Requirement: Validation of the target URL

Before a subscription is stored, switched on or delivered to, the server SHALL validate its URL: it MUST be an
absolute `http` or `https` address; with `ssl: true` it MUST be `https`; a host given as an IP address, and every
address the host name resolves to (DNS lookup limited to 3 seconds), MUST NOT fall into the blacklist
(`webhooks:blacklist`, by default the loopback, link-local, private and carrier-grade NAT ranges of IPv4 and
IPv6: `0.0.0.0/8`, `10.0.0.0/8`, `100.64.0.0/10`, `127.0.0.0/8`, `169.254.0.0/16`, `172.16.0.0/12`,
`192.168.0.0/16`, `::/128`, `::1/128`, `fc00::/8`, `fd00::/8`, `fe80::/10`). On creation, update and
switching on, the server SHALL additionally send a HEAD request to the URL — connecting to the already resolved
address so that DNS rebinding cannot redirect it, without following redirects, and verifying the TLS certificate
only when `ssl` is `true` — and refuse the subscription unless the answer is a success status. A URL that fails
the rules SHALL be refused with 400; a URL that does not answer the HEAD request at all (connection failure,
timeout, invalid certificate under `ssl`) SHALL be refused with 500.

#### Scenario: Private address is refused

- **WHEN** the DocSpace Owner creates a webhook whose URL is `http://localhost`
- **THEN** the call answers 400

#### Scenario: Target that redirects is refused

- **WHEN** the URL answers the HEAD request with a redirect (3xx) or any non-success status
- **THEN** the call answers 400 ("webhook URL not available")

#### Scenario: Public target that answers

- **WHEN** the URL is `https://example.com/?id=...`, which answers the HEAD request with 200
- **THEN** the subscription is stored

### Requirement: Trigger catalogue and per-role availability

`GET /api/2.0/settings/webhook/triggers` (`getWebhookTriggers`) SHALL return the fixed catalogue of events in
display order — `*`, user, group, file (including `file.downloaded`), folder (including `folder.downloaded`),
room, form and agent events — each with `name` (the event name used in payloads), `id` (the bit value) and
`available` (whether the caller's user type may subscribe to it). The catalogue, with the roles allowed to
subscribe, is:

| Event name          | Bit (`id`) | Member name       | DocSpace admin | Room admin | User |
|---------------------|-----------:|-------------------|:--:|:--:|:--:|
| `*` (all events)    | 0          | `All`             | ✔ | ✔ | ✔ |
| `user.created`      | 1          | `UserCreated`     | ✔ | ✔ |   |
| `user.invited`      | 2          | `UserInvited`     | ✔ | ✔ |   |
| `user.updated`      | 4          | `UserUpdated`     | ✔ | ✔ | ✔ |
| `user.deleted`      | 8          | `UserDeleted`     | ✔ | ✔ | ✔ |
| `group.created`     | 16         | `GroupCreated`    | ✔ | ✔ |   |
| `group.updated`     | 32         | `GroupUpdated`    | ✔ | ✔ |   |
| `group.deleted`     | 64         | `GroupDeleted`    | ✔ | ✔ |   |
| `file.created`      | 128        | `FileCreated`     | ✔ | ✔ | ✔ |
| `file.uploaded`     | 256        | `FileUploaded`    | ✔ | ✔ | ✔ |
| `file.updated`      | 512        | `FileUpdated`     | ✔ | ✔ | ✔ |
| `file.trashed`      | 1024       | `FileTrashed`     | ✔ | ✔ | ✔ |
| `file.deleted`      | 2048       | `FileDeleted`     | ✔ | ✔ | ✔ |
| `file.restored`     | 4096       | `FileRestored`    | ✔ | ✔ | ✔ |
| `file.copied`       | 8192       | `FileCopied`      | ✔ | ✔ | ✔ |
| `file.moved`        | 16384      | `FileMoved`       | ✔ | ✔ | ✔ |
| `folder.created`    | 32768      | `FolderCreated`   | ✔ | ✔ | ✔ |
| `folder.updated`    | 65536      | `FolderUpdated`   | ✔ | ✔ | ✔ |
| `folder.trashed`    | 131072     | `FolderTrashed`   | ✔ | ✔ | ✔ |
| `folder.deleted`    | 262144     | `FolderDeleted`   | ✔ | ✔ | ✔ |
| `folder.restored`   | 524288     | `FolderRestored`  | ✔ | ✔ | ✔ |
| `folder.copied`     | 1048576    | `FolderCopied`    | ✔ | ✔ | ✔ |
| `folder.moved`      | 2097152    | `FolderMoved`     | ✔ | ✔ | ✔ |
| `room.created`      | 4194304    | `RoomCreated`     | ✔ | ✔ |   |
| `room.updated`      | 8388608    | `RoomUpdated`     | ✔ | ✔ | ✔ |
| `room.archived`     | 16777216   | `RoomArchived`    | ✔ | ✔ | ✔ |
| `room.deleted`      | 33554432   | `RoomDeleted`     | ✔ | ✔ | ✔ |
| `room.restored`     | 67108864   | `RoomRestored`    | ✔ | ✔ | ✔ |
| `room.copied`       | 134217728  | `RoomCopied`      | ✔ | ✔ |   |
| `form.submit`       | 268435456  | `FormSubmit`      | ✔ | ✔ | ✔ |
| `form.filled.out`   | 536870912  | `FormFilledOut`   | ✔ | ✔ | ✔ |
| `form.stopped`      | 1073741824 | `FormStopped`     | ✔ | ✔ | ✔ |
| `agent.created`     | 2^31       | `AgentCreated`    | ✔ | ✔ |   |
| `agent.updated`     | 2^32       | `AgentUpdated`    | ✔ | ✔ | ✔ |
| `agent.deleted`     | 2^33       | `AgentDeleted`    | ✔ | ✔ | ✔ |
| `file.downloaded`   | 2^34       | `FileDownloaded`  | ✔ | ✔ | ✔ |
| `folder.downloaded` | 2^35       | `FolderDownloaded`| ✔ | ✔ | ✔ |

The DocSpace Owner counts as a DocSpace admin. Creating or updating a subscription whose `triggers` bitmask
contains an event the caller's type may not subscribe to SHALL be refused with 400 ("Trigger is not available");
`triggers = 0` (all events) is accepted from every type that may use webhooks.

#### Scenario: Catalogue is complete and ordered

- **WHEN** any authenticated member requests the catalogue
- **THEN** the call answers 200 with a non-empty list whose first entry has a name, ordered as in the table

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
their defaults — and SHALL replace `secretKey` only when a non-empty one is sent, keeping the stored secret
otherwise. The same validation as on creation (trigger availability, password rules for a new secret, URL rules
and the HEAD probe) SHALL run, and it runs before the subscription is looked up. A DocSpace admin may update any
subscription; anybody else only their own. The answer SHALL be the updated configuration with `modifiedOn` and
`modifiedBy` set; the update is recorded in the audit trail (`WebhookUpdated`).

#### Scenario: Owner renames and re-targets

- **WHEN** the DocSpace Owner updates a webhook with a new `name`, `uri` and `secretKey`
- **THEN** the call answers 200 with the same `id`, the new `name` and `uri`, and `modifiedOn` set

#### Scenario: Unknown id

- **WHEN** the body passes validation but no subscription with that `id` exists in the portal
- **THEN** the call answers 404

#### Scenario: Another member's subscription

- **WHEN** a Room admin or User updates a subscription created by someone else
- **THEN** the call answers 403 with "Access denied"

### Requirement: Switching a webhook on or off

`PUT /api/2.0/settings/webhook/enable` (`enableWebhook`) SHALL set `enabled` of the subscription addressed by
`id` and leave every other stored value unchanged, although the body is validated as a full configuration
(`name` and `uri` required). Switching a subscription **on** SHALL re-run the validation against the stored
values (URL rules, HEAD probe, password rules on the stored secret) and refuse with 400 when they no longer pass;
switching **off** is not validated. While a subscription is off, its events SHALL be dropped, not queued. A
DocSpace admin may switch any subscription; anybody else only their own.

#### Scenario: Owner disables own webhook

- **WHEN** the DocSpace Owner creates an enabled webhook and sends `enabled: false` with its `id`
- **THEN** the call answers 200 with the same `id`, `name` and `uri`, `enabled` false and `modifiedOn` set

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
creator is allowed to see the entity: for files, folders and rooms the creator must be able to read the entry;
for users the creator must be allowed to view that account; for groups the creator must be a DocSpace admin or
hold the right to read groups. The subscription's own creator always receives events about actions they perform
themselves. Events are produced by the product actions below:

| Event                         | Produced by                                                                                     |
|-------------------------------|-------------------------------------------------------------------------------------------------|
| `user.created`                | adding a member through the people API, confirming an invitation, signing up through a third-party account |
| `user.invited`                | sending an invitation by e-mail                                                                 |
| `user.updated`, `user.deleted`| profile and type changes, deletion/reassignment of a member                                     |
| `group.created/updated/deleted` | group management                                                                              |
| `file.*`, `folder.*`          | create, upload, edit, trash, delete, restore, copy, move of files and folders                   |
| `file.downloaded`, `folder.downloaded` | download through the file handler or a download operation                              |
| `room.created`                | creating a room that is not an AI agent                                                         |
| `room.updated/archived/deleted/restored` | room management                                                                      |
| `room.copied`                 | copying a room (including creating one from a template)                                         |
| `agent.created`               | creating an AI agent room                                                                       |
| `agent.updated`, `agent.deleted` | AI agent management                                                                          |
| `form.submit`, `form.filled.out`, `form.stopped` | see `form-filling-workflow` ("Webhook events of the workflow")                |

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

For each queued event the server SHALL write a delivery record and POST a JSON body to the subscription URL with
`Accept: */*`, `Content-Type: application/json` and the header `x-docspace-signature-256: sha256=<HMAC-SHA256
of the body, keyed with the secret key, upper-case hex>`. The body SHALL be
`{ "event": { id, createOn, createBy, trigger, triggerId }, "payload": <the entity>, "webhook": { id, name,
url, triggers[], target?, lastFailureOn?, lastFailureContent?, lastSuccessOn?, retryCount, retryOn? } }`,
camel-cased, dates in UTC, with default values omitted. The target URL SHALL be validated again at delivery
time (same rules as on creation): a blacklisted address deletes the subscription, any other validation failure
switches it off, both recorded in the audit trail. A delivery that fails (transport error, timeout or a
non-success status) SHALL be retried up to `webhooks:repeatcount` times (default 5) with exponential back-off
starting at one second, each retry carrying `retryCount`, `retryOn` and the last failure in `webhook`. When the
target answers `410 Gone` the subscription SHALL be deleted. After the attempts the record SHALL store the final
HTTP status (`500` for a non-HTTP failure), the request and response headers and bodies, and `delivery` — set
only on success. A subscription whose last success (or creation) lies more than `webhooks:trustedDaysCount`
days back (default 3) when a delivery fails SHALL be switched off.

#### Scenario: Successful delivery

- **WHEN** the target answers 2xx
- **THEN** the record carries that status, the response headers and body and a `delivery` time, and the subscription's `lastSuccessOn` is updated

#### Scenario: Target gone

- **WHEN** the target answers 410
- **THEN** the subscription is removed and the audit trail records the deletion with "(HTTP status 410)"

#### Scenario: Failing for longer than the trusted period

- **WHEN** a delivery fails and the subscription has had no success for more than 3 days
- **THEN** the subscription is switched off and the audit trail records it

### Requirement: Delivery journal

`GET /api/2.0/settings/webhooks/log` (`getWebhooksLogs`) SHALL return the delivery records of the portal,
newest first, paged with `startIndex` and `count` (1–100, default 100; an out-of-range `count` answers 400),
the total number of matching records reported beside the response. Each record carries `id`, `configName`,
`trigger`, `creationTime`, `method` (`POST`), `route`, `requestHeaders`, `requestPayload`, `responseHeaders`,
`responsePayload`, `status` (`0` while pending) and `delivery`. Filters SHALL combine with AND: `deliveryFrom`,
`deliveryTo`, `hookUri` (exact match), `configId`, `eventId`, `groupStatus` (bitmask of `NotSent` 1,
`Status2xx` 2, `Status3xx` 4, `Status4xx` 8, `Status5xx` 16; 0 = no filter), `userId` (the subscription's
creator) and `trigger` (0 = all). For a caller who is not a DocSpace admin `userId` SHALL be forced to the
caller, so they only ever see deliveries of their own subscriptions.

#### Scenario: Owner reads the journal

- **WHEN** the DocSpace Owner requests the journal with `count=10`
- **THEN** the call answers 200 with a list

#### Scenario: Guest and anonymous

- **WHEN** a Guest requests the journal
- **THEN** the call answers 403 with "Access denied"
- **AND** an unauthenticated request answers 401

### Requirement: Retrying a delivery

`PUT /api/2.0/settings/webhook/{id}/retry` (`retryWebhook`, `id` being a delivery record) SHALL create a new
delivery record with the stored payload of the given one and queue it; the answer is the new record with
`status` 0 and no `delivery` yet. An `id` of 0 answers 400, an unknown record 404, and a record of another
member's subscription 403 unless the caller is a DocSpace admin. `PUT /api/2.0/settings/webhook/retry`
(`retryWebhooks`, body `ids`) SHALL do the same for a batch, silently skipping unknown records and — for a
non-admin — records of other members' subscriptions, and answering the list of new records. Both endpoints are
rate limited (429 on a burst).

#### Scenario: Empty batch

- **WHEN** the DocSpace Owner sends `ids: []`
- **THEN** the call answers 200 with an empty list

#### Scenario: Batch with a foreign record

- **WHEN** a User sends the id of a delivery record of another member's subscription
- **THEN** the record is skipped and the answer is shorter than `ids`

### Requirement: Creating an API key

`POST /api/2.0/keys` (`createApiKey`) SHALL issue a key for the calling account: `name` (required, 1–30
characters, stored verbatim including HTML), optional `permissions` (scope values from the catalogue; `*` or an
omitted field means unrestricted; an empty array or an unknown scope answers 400 "Permissions are not valid.")
and optional `expiresInDays` (1–365; omitted means no expiry). The secret SHALL have the form `sk-` followed by
64 lower-case hex characters, SHALL be stored only as a hash and SHALL be returned in full in `key` only in this
answer; every later read SHALL show `sk-***` followed by the last four characters (`keyPostfix`). The answer also
carries `id`, `name`, `permissions` (an empty list for an unrestricted key), `createBy` (the caller),
`createOn`, `expiresAt` (set only when `expiresInDays` was given), `lastUsed` (empty) and `isActive` true. The
DocSpace Owner, DocSpace admins, Room admins and Users may create keys (subject to the access rule); creation is
recorded in the audit trail and rate limited.

#### Scenario: Every type creates a key, with or without scopes and expiry

- **WHEN** the DocSpace Owner, a DocSpace admin, a Room admin or a User creates a key with no permissions, with `*:read`, or with a list of per-area scopes, each with and without `expiresInDays: 7`
- **THEN** the call answers 200 with a non-empty `id` and `key`, the given `name`, `permissions` equal to what was sent (empty when omitted), `createBy` equal to the caller, and `expiresAt` set exactly when `expiresInDays` was sent

#### Scenario: Name kept verbatim

- **WHEN** a member creates a key named `<script>alert('xss')</script>`
- **THEN** the call answers 200 and `name` is echoed unchanged

#### Scenario: Blank, missing or long name

- **WHEN** `name` is empty or whitespace, the body has no `name` property, or `name` is 31 characters long
- **THEN** the call answers 400 ("The Name field is required." / "missing required properties including: 'name'" / "Incorrect name. Length must be less than 30")

#### Scenario: Empty permissions array

- **WHEN** `permissions` is `[]`
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
fields left out keep their values, `name` when sent MUST be 1–30 characters and not whitespace only (400,
nothing changed), `permissions` when sent MUST be a non-empty list of catalogue values (`["*"]` makes the key
unrestricted). The caller may update their own keys and a DocSpace admin any key; an unknown or already deleted
`keyId` answers 404. The answer is `true` when the key was changed and `false` when it was left untouched
because it has already expired. The change is recorded in the audit trail.

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
answered 401. On success the request SHALL run as the account that created the key, the key's `lastUsed` SHALL
be set to now, and the key's scopes SHALL become the request's `scope` claims — a key without scopes (created
with no `permissions` or with `*`) carries the single scope `*:write`.

#### Scenario: Valid key

- **WHEN** a request carries a valid, active key
- **THEN** it is processed as the key's creator and `lastUsed` is updated

#### Scenario: Deactivated or expired key

- **WHEN** a request carries a key whose `isActive` is false or whose `expiresAt` has passed
- **THEN** the call answers 401

### Requirement: Scope enforcement for API-key requests

The server SHALL derive the scopes an endpoint demands from its route and method, and refuse an API-key request
with 403 unless the key holds at least one of them. The route map is:

| Route                                   | GET accepts                                    | POST/PUT/DELETE accept        |
|-----------------------------------------|------------------------------------------------|-------------------------------|
| `api/2.0/files/rooms...`                | `rooms:read`, `rooms:write`, `*:read`, `*:write` | `rooms:write`, `*:write`    |
| `api/2.0/files...` (other)              | `files:read`, `files:write`, `*:read`, `*:write` | `files:write`, `*:write`    |
| `api/2.0/people/@self...`               | `accounts.self:read`, `accounts.self:write`, `*:read`, `*:write` | `accounts.self:write`, `*:write` |
| `api/2.0/people...` (other)             | `accounts:read`, `accounts:write`, `*:read`, `*:write` | `accounts:write`, `*:write` |
| `api/2.0/group...`                      | `accounts:read`, `*:read`, `*:write`           | `accounts:write`, `*:write`   |
| `api/2.0/keys/@self`                    | any key                                        | —                             |
| every other route                       | `*:read`, `*:write`                            | `*:write`                     |

The check applies to anonymous-allowed endpoints too. Scopes never widen the account's own rights: an endpoint
the creating account may not call stays refused for the key.

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
