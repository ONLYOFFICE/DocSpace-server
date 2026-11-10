# storage-and-integrations Specification

## Purpose

Defines how a portal reaches storage outside itself: the third-party storage accounts a member connects
(`files/thirdparty`), the portal's single backup storage account, the OAuth consent URL that starts a connection
(`thirdparty/{provider}`), the data storage and the CDN of a portal on a server installation (`settings/storage`),
storage encryption as the API shows it (`settings/encryption`), and the Docs Connect tenant of a portal
(`settings/docscloud`).

Referred to, not repeated: rooms made from a connected account are in `room-types`, and a private room over one is
in `private-rooms`; the permission switch for third-party storage is in `files-settings`; what is written to the
backup account and `getAllBackupStorages` are in `backup-and-restore`; storage limits are in `quotas`; the wallet,
the tariff and the Docs Connect wallet services are in `billing`; the portal statuses and what they block are in
`portal-lifecycle`.

Evidence per scenario, the differences from the OpenAPI document, the operations left out and the suspected defects
are in `verification.md` next to this file.

## Requirements

### Requirement: Storage services a portal offers

`getAllProviders` SHALL list the storage services the installation enables, each with `name`, `key`, `oauth`,
`connected` and, for OAuth services whose application is configured, `redirectUrl` and `clientId`. The WebDAV presets
Nextcloud, ownCloud, kDrive, Yandex and WebDav share the key `WebDav`; `requiredConnectionUrl` marks those that need a
server address.

#### Scenario: Known services

- **WHEN** the owner calls `getAllProviders`
- **THEN** the list contains Box, Dropbox, OneDrive, kDrive, WebDav, Nextcloud and ownCloud, every entry with a non-empty `name` and `key`

#### Scenario: Nextcloud is a WebDAV preset

- **WHEN** the owner reads the Nextcloud entry
- **THEN** its `key` is `WebDav` and `oauth` is false

#### Scenario: kDrive asks for an address

- **WHEN** the owner reads the kDrive entry
- **THEN** `requiredConnectionUrl` is true

#### Scenario: Configured OAuth service

- **WHEN** an OAuth application is configured for Box, Dropbox or OneDrive
- **THEN** its entry has `oauth` true and a non-empty `redirectUrl` and `clientId`

### Requirement: WebDAV family can be left out of the providers list

`getAllProviders` with `excludewebdav=true` SHALL return the OAuth services only, and with `excludewebdav=false` the
same list as without the parameter.

#### Scenario: Excluded

- **WHEN** the owner calls `getAllProviders` with `excludewebdav=true`
- **THEN** every entry has `oauth` true, none has the key `WebDav`, and Box, Dropbox and OneDrive are present

#### Scenario: Not excluded

- **WHEN** the owner calls `getAllProviders` with `excludewebdav=false`
- **THEN** the names equal those of a call without the parameter

### Requirement: Provider lists need a signed-in member who is not a guest

`getAllProviders` and `getCapabilities` SHALL answer 401 to an anonymous caller, and an empty array with 200 to a guest
and to everyone while the portal-wide third-party permission is off.

#### Scenario: Anonymous

- **WHEN** an anonymous caller calls `getAllProviders` or `getCapabilities`
- **THEN** the answer is 401

#### Scenario: Guest or permission off

- **WHEN** a guest calls either operation, or anyone does while the third-party permission is off
- **THEN** the answer is 200 with an empty array

### Requirement: Compact list of connectable services

`getCapabilities` SHALL return one array per enabled service whose first item is the provider key; an OAuth service
adds its client id and redirect URL and appears only when its application is configured. Unlike `getAllProviders` it
names each service by its own key, such as `kDrive` or `Yandex`.

#### Scenario: WebDAV family keys

- **WHEN** the owner calls `getCapabilities`
- **THEN** the first items of the elements include `WebDav` and `kDrive`

### Requirement: Connecting a third-party account

`saveThirdParty` without `providerId` SHALL verify the credentials with the service first and only then store a new
account as room storage, returning the folder for its root with a string `id` and a `providerId`. The caller needs
the right to create rooms: the owner, a DocSpace admin and a room admin may; a user and a guest get 403.

#### Scenario: Roles

- **WHEN** a DocSpace admin or a room admin connects an account with valid credentials
- **THEN** the answer is 200
- **AND** a user or a guest gets 403 and an anonymous caller 401

#### Scenario: New account

- **WHEN** the owner connects a Nextcloud account and calls `getThirdPartyAccounts`
- **THEN** the answer of `saveThirdParty` has a `providerId` greater than 0 and the account is listed

### Requirement: Rejected credentials store nothing

A connection whose credentials the service rejects SHALL be refused with 403 "Access denied" and SHALL NOT be stored.
A successful connection to the same host MUST NOT make a later wrong password succeed.

#### Scenario: Wrong password

- **WHEN** `saveThirdParty` is called for a Nextcloud host with a wrong password
- **THEN** the answer is 403 and contains "Access denied"

#### Scenario: Wrong password after a correct connection

- **WHEN** an account on a host is connected and then the same host is connected twice with a wrong password
- **THEN** both answers are 403 and the title of the wrong attempt is not in `getThirdPartyAccounts`

#### Scenario: Invalid OAuth code

- **WHEN** `saveThirdParty` is called for Box with a `token` that is no valid authorization code
- **THEN** the answer is a status below 500

### Requirement: Server address of a new connection

A new connection to a service with a required server address SHALL be refused with 400 and not stored when `url` is
missing, blank, unparsable or not an HTTP address.

#### Scenario: Invalid address

- **WHEN** `saveThirdParty` is called with `providerKey` kDrive and `url` empty, blank, "not a url" or an `ftp://` address
- **THEN** the answer is 400 and the title is not in `getThirdPartyAccounts`

### Requirement: Re-authenticating and renaming an account

`saveThirdParty` with `providerId` SHALL update that account instead of adding one, and only for the member who
connected it. A non-empty `customerTitle` renames it, and a room account keeps its stored address. Another member gets
403 and an unknown id 404.

#### Scenario: Rename

- **WHEN** the owner re-saves an account with its `providerId` and a new `customerTitle`
- **THEN** `getThirdPartyAccounts` shows the new title for that `providerId`

#### Scenario: Another member's account

- **WHEN** a member calls `saveThirdParty` with the `providerId` of an account someone else connected
- **THEN** the answer is 403

#### Scenario: Account saved without an address

- **WHEN** a kDrive account stored with no address is re-authenticated with a rejected password
- **THEN** the refusal is not a 400 and does not report an invalid connection URL

### Requirement: A member's list of connected accounts

`getThirdPartyAccounts` SHALL return only the accounts the caller connected, never the backup account, as a plain array
that is empty when there are none. Credentials MUST NOT appear in it.

#### Scenario: No credentials

- **WHEN** the owner has a connected account and reads `files/thirdparty` as raw JSON
- **THEN** the answer is 200 and no element has an `auth_data` property

#### Scenario: Another member's accounts

- **WHEN** a user who connected nothing calls the operation after the owner connected an account
- **THEN** the answer is 200 with an empty array
- **AND** an anonymous caller gets 401

### Requirement: Disconnecting an account

`deleteThirdParty` SHALL remove the account with its credentials, sharing records and tags and return the id of the
folder that stood for it; files at the service stay. A member without delete rights on that folder gets 403, an id no
account has gets 404, and an anonymous caller 401. Group links of the account's rooms are removed with it.

#### Scenario: Another member's account

- **WHEN** an unrelated user calls `deleteThirdParty` for an account another member connected
- **THEN** the answer is 403

#### Scenario: Room group after the account went away

- **WHEN** a room over a connected account belongs to a room group and the room is deleted, which removes the account
- **THEN** `getRoomGroups` still answers without error

### Requirement: An account a room stands on cannot be disconnected

`deleteThirdParty` SHALL refuse with 403 an account that backs a room, leaving the room and the account in place. The
account goes away by deleting the room.

#### Scenario: Provider behind a room

- **WHEN** the owner calls `deleteThirdParty` for an account a room was created on
- **THEN** the answer is 403, `getRoomInfo` still returns the room and `getThirdPartyAccounts` still lists the account

### Requirement: Accounts of the legacy Common section

`getCommonThirdPartyFolders` SHALL return as folder entries the accounts attached to the Common section. Accounts made
by `saveThirdParty` are room storage, so they do not appear there.

#### Scenario: No legacy connections

- **WHEN** the owner of a portal with no such accounts calls the operation
- **THEN** the answer is an empty array

### Requirement: Backup storage account of the portal

`saveThirdPartyBackup` SHALL hold one account per portal: a DocSpace admin connects it, and a second call updates the
existing account instead of adding another, checking new credentials first. Other callers, and everyone while the
third-party permission is off, get 403. What is written to the account is in `backup-and-restore`.

#### Scenario: Not an administrator

- **WHEN** a room admin, user or guest calls `saveThirdPartyBackup`
- **THEN** the answer is 403

#### Scenario: Second call

- **WHEN** a backup account exists and a DocSpace admin calls `saveThirdPartyBackup` with other credentials the service accepts
- **THEN** the same account now holds them and no second one appears

### Requirement: Reading the backup storage account

`getBackupThirdPartyAccount` SHALL return the root folder of the backup account to any signed-in member who may read it,
and 200 with an empty body when there is none or the caller may not read it. `getThirdPartyAccounts` MUST NOT list it.

#### Scenario: Nothing to return

- **WHEN** no backup account is connected, or the caller has no read access to its folder
- **THEN** the answer is 200 with an empty body

### Requirement: Disconnecting the backup account removes its schedule

`deleteThirdParty` for the backup account SHALL also delete the backup schedule that writes to it.

#### Scenario: Schedule goes with the account

- **WHEN** the backup account is disconnected successfully
- **THEN** the schedule stored for that account's folder is deleted

### Requirement: Content of a connected account

Folders and files of a connected account SHALL be handled by the ordinary Files operations with string identifiers,
and report `providerKey` and the `rootFolderType` of the section. Deleting a file there is final: the service has no
Trash for it, so reading it afterwards answers 404.

#### Scenario: New file

- **WHEN** the owner creates a `.docx` in a folder of a connected room
- **THEN** the file has a string `id`, the given `title`, that `folderId`, `rootFolderType` VirtualRooms and `providerKey` `WebDav`

#### Scenario: Delete

- **WHEN** a file of a connected account is deleted with `deleteFile` and the operation finishes without error
- **THEN** the folder no longer lists it and `getFileInfo` answers 404

#### Scenario: Copy into a connected account

- **WHEN** a file from My documents is copied into a connected folder with `copyBatchItems`
- **THEN** the operation finishes without error, the folder lists the file and the source stays

### Requirement: Recent section with a third-party file

A third-party file in the caller's Recent section SHALL be removable with `deleteRecent`, and a file that has since
disappeared from the service MUST NOT fail `deleteRecent` or the opening of another file with `openEditFile`.

#### Scenario: Remove from Recent

- **WHEN** a third-party file was added to Recent and `deleteRecent` is called with its id
- **THEN** the section stops listing it

#### Scenario: File missing from the service

- **WHEN** the file was deleted at the service and `deleteRecent` is called with its id
- **THEN** the call succeeds and the section lists without it

### Requirement: OAuth consent URL

`getThirdPartyCode` SHALL return as a string the consent URL of Google, Dropbox, Docusign, Box, OneDrive, Wordpress or
Github to any signed-in member, with `response_type=code` and a `state` pointing at this portal's
`thirdparty/{provider}/code`. It stores nothing. An anonymous caller gets 401.

#### Scenario: Google for any member

- **WHEN** the owner or a user calls `getThirdPartyCode` for Google
- **THEN** the answer is 200 with the provider's consent address and `response_type=code`
- **AND** an anonymous caller gets 401

#### Scenario: Other providers

- **WHEN** the provider is a known one outside these seven
- **THEN** the answer is 200 with no URL
- **AND** a value outside the provider list is 400

#### Scenario: Desktop client

- **WHEN** the call carries `desktop=true` and other query parameters
- **THEN** the query string is appended to `state` after `thirdparty/google/code`, which a call without `desktop` ends with

### Requirement: Access to the storage settings

`getAllStorages`, `updateStorage`, `resetStorageToDefault`, `getAllCdnStorages`, `updateCdnStorage` and
`resetCdnToDefault` SHALL require the right to edit portal settings on a server installation whose access space is not
limited. Anyone else signed in gets 403, on a hosted portal the owner too, and an anonymous caller 401.

#### Scenario: Anonymous

- **WHEN** an anonymous caller calls any of these operations
- **THEN** the answer is 401

#### Scenario: Hosted portal

- **WHEN** the owner of a hosted portal calls any of these operations
- **THEN** the answer is 403

### Requirement: Data storages of a portal

`getAllStorages` SHALL list the storage providers of the installation with their keys, whether they are configured
(`isSet`) and which one the portal uses (`current`). The built-in storage is not an entry: no `current` entry means the
portal uses it.

#### Scenario: Owner on a server installation

- **WHEN** the owner calls `getAllStorages`
- **THEN** the answer is 200 with a list

### Requirement: Switching the data storage

`updateStorage` SHALL refuse with 400 a missing body or a `module` not configured on the installation. The module in use
SHALL return the saved settings unchanged. Any other module SHALL put the portal into the Migrating status, request a
migration and answer with the `module` and `props` sent.

#### Scenario: Module not configured

- **WHEN** the owner calls `updateStorage` with a module the installation has not configured
- **THEN** the answer is 400 and nothing changes

#### Scenario: Empty body

- **WHEN** `settings/storage` receives a PUT with an empty body
- **THEN** the answer is 400

#### Scenario: Another configured module

- **WHEN** the owner sends a configured module the portal does not use
- **THEN** `getPortalInformation` reports `status` Migrating and the migration is requested

### Requirement: Returning to the built-in data storage

`resetStorageToDefault` SHALL clear the module and its keys from the request it hands over, put the portal into the
Migrating status and request a migration, with no response body.

#### Scenario: Reset

- **WHEN** the owner on a server installation calls `resetStorageToDefault`
- **THEN** the answer is 200 with no body and the portal status is Migrating

### Requirement: Storage migration progress

`getStorageProgress` SHALL require the right to edit portal settings and answer on a lapsed portal too. A hosted portal
gets -1; on a server installation the answer is the last percentage reported for the portal's migration.

#### Scenario: Refused callers

- **WHEN** a user calls `getStorageProgress`
- **THEN** the answer is 403
- **AND** an anonymous caller gets 401

#### Scenario: Hosted portal

- **WHEN** an administrator of a hosted portal calls the operation
- **THEN** the answer is -1

### Requirement: CDN storages of a portal

`getAllCdnStorages` SHALL list the providers that offer a CDN, in the shape of `getAllStorages`, and mark the one in use
as `current`. No `current` entry means the portal serves its static content itself.

#### Scenario: Owner on a server installation

- **WHEN** the owner calls `getAllCdnStorages`
- **THEN** the answer is 200 with a list

### Requirement: Selecting a CDN

`updateCdnStorage` SHALL refuse with 400 a missing body or a `module` not configured on the installation, return the
saved settings unchanged for the module in use, and otherwise publish the upload request and answer with the `module`
and `props` sent. The portal status does not change.

#### Scenario: Module not configured

- **WHEN** the owner calls `updateCdnStorage` with a module the installation has not configured
- **THEN** the answer is 400

#### Scenario: Empty body

- **WHEN** `settings/storage/cdn` receives a PUT with an empty body
- **THEN** the answer is 400

### Requirement: Clearing the CDN settings

`resetCdnToDefault` SHALL clear the saved CDN settings with no response body and no migration. Repeating it is harmless.

#### Scenario: Reset

- **WHEN** an administrator on a server installation calls `resetCdnToDefault`
- **THEN** the answer is 200 with no body and `getAllCdnStorages` has no `current` entry

### Requirement: Amazon S3 regions

`getAmazonS3Regions` SHALL return to any signed-in member the Amazon regions compiled into the server, each with
`systemName`, `displayName` and the partition details. An anonymous caller gets 401.

#### Scenario: Any member

- **WHEN** the owner or a user calls `getAmazonS3Regions`
- **THEN** the list is not empty and its first entry has a `systemName` and a `displayName`

#### Scenario: Anonymous

- **WHEN** an anonymous caller calls `getAmazonS3Regions`
- **THEN** the answer is 401

### Requirement: Starting storage encryption

`startStorageEncryption` SHALL need a server installation, the right to edit portal settings and an unlimited access
space, or answer 403. It answers false where the build has encryption off, 415 where the feature is not offered or the
storage or CDN of the portal is third-party, and 500 while a portal's backup runs.

#### Scenario: Hosted portal

- **WHEN** the feature is available but the installation is not a server one
- **THEN** the answer is 403

#### Scenario: Third-party storage in use

- **WHEN** `getAllStorages` or `getAllCdnStorages` of the calling portal has a `current` entry
- **THEN** the answer is 415 and nothing is started

### Requirement: What a started encryption does

A successful `startStorageEncryption` SHALL flip the direction by the saved state, encrypt a decrypted installation and
decrypt an encrypted one, drop the backup schedules of all portals, put every portal into the Encryption status, write
an audit entry, mail users when `notifyUsers` is true and answer true.

#### Scenario: All portals are affected

- **WHEN** an administrator of one portal starts encryption on an installation with several portals
- **THEN** every portal reports the Encryption status and has no backup schedule

### Requirement: Reading the encryption settings

`getStorageEncryptionSettings` SHALL return `status` and `notifyUsers` with `password` always an empty string. Where
the settings cannot be read, for lack of the right, on a hosted portal or a limited access space, it SHALL answer 200
with an empty body rather than an error.

#### Scenario: Owner

- **WHEN** the owner calls `getStorageEncryptionSettings`
- **THEN** `password` is present and empty

#### Scenario: Caller without the right

- **WHEN** a user calls the operation
- **THEN** the answer is 200 with an empty body

### Requirement: Encryption progress

`getStorageEncryptionProgress` SHALL answer any signed-in member with no portal-settings check: a percentage during a
run, an empty body when no run is remembered, -1 where the build has encryption off, and 415 on an installation that
is not a server one or does not offer the feature.

#### Scenario: Ordinary member during a run

- **WHEN** a user calls the operation during a run on a server installation
- **THEN** the answer is the percentage

### Requirement: Access to Docs Connect tenant operations

`startDocsCloudTrial`, `getTenant`, `getTenantInfo`, `getTenantConfig`, `updateTenantConfig`, `getTenantQuota`,
`createTenantQuotaReport`, `getTenantQuotaReport`, `terminateTenantQuotaReport` and `getTenantUsage` SHALL require the
right to edit portal settings. A room admin, user or guest gets 403 "Access denied", an anonymous caller 401.

#### Scenario: Other role

- **WHEN** a room admin, user or guest calls any of these operations with a valid request
- **THEN** the answer is 403 and contains "Access denied"

#### Scenario: Anonymous

- **WHEN** an anonymous caller calls any of them
- **THEN** the answer is 401

### Requirement: Docs Connect trial

`startDocsCloudTrial` SHALL need a configured billing service (403), a trial quota on the installation (404), a tariff
not delayed or unpaid (402) and no Docs Connect subscription on the portal (400). It answers true when the trial is
granted, adding the tenant's address to the Content Security Policy domains, and false when billing declines or its
answer cannot be read; after an unreadable answer the cached tariff is dropped, so the next check reads billing again.

#### Scenario: Subscription already there

- **WHEN** the tariff holds a Docs Connect trial, Docs Connect or Docs Connect Dev Pack quota
- **THEN** the answer is 400

#### Scenario: Granted

- **WHEN** the billing service grants the trial
- **THEN** the answer is true and the address of the assigned tenant is among the allowed domains

#### Scenario: Unreadable billing answer

- **WHEN** the billing service answers with something that is not a readable trial result
- **THEN** the answer is false and the cached tariff is cleared

### Requirement: Moving to Docs Connect Dev Pack

`calculateDevPack` SHALL price and `switchToDevPack` SHALL perform the move from an active Docs Connect subscription to
Dev Pack for `quantity` users. Both need a configured billing service and an administrator (403), a billing customer
(404) and a tariff in good standing (402); a low `quantity`, no subscription or an existing Dev Pack give 400.

#### Scenario: Anonymous

- **WHEN** an anonymous caller calls either operation
- **THEN** the answer is 401

#### Scenario: No billing service

- **WHEN** any signed-in caller calls either on an installation without a billing service
- **THEN** the answer is 403

#### Scenario: Quantity below the minimum

- **WHEN** `quantity` is below the current number of Docs Connect users or the Dev Pack minimum, by default 10
- **THEN** the answer is 400 and contains "Invalid quantity", and nothing is asked of the billing service

#### Scenario: No Docs Connect subscription

- **WHEN** the portal holds no active Docs Connect subscription
- **THEN** the answer is 400 and contains "DocsCloud subscription is not active"

#### Scenario: DocSpace admin who is not the owner

- **WHEN** a DocSpace admin who is not the owner calls `calculateDevPack` or `switchToDevPack` on a subscribed portal
- **THEN** the switch is priced or performed, and `switchToDevPack` names the caller as the paying participant

#### Scenario: Not an administrator

- **WHEN** a signed-in caller who is not an administrator calls either operation on an installation with a billing service
- **THEN** the answer is 403 and contains "Access denied", and nothing is charged

#### Scenario: Simultaneous switches

- **WHEN** two `switchToDevPack` calls for one portal overlap
- **THEN** they run one after the other and the second gets 400 because Dev Pack is already set

### Requirement: Reading the Docs Connect tenant

`getTenant` SHALL return the tenant of the portal, or 200 with an empty body when it has none, from a cache unless
`refresh=true`. `getTenantInfo`, `getTenantConfig`, `getTenantQuota` and `getTenantUsage` return the license and usage,
the configuration, the user lists and the active-user count, and answer 400 without an activated tenant.

#### Scenario: Refresh

- **WHEN** an administrator calls one of these with `refresh=true`
- **THEN** the data is requested from Docs Connect again and replaces the cached copy

#### Scenario: Trial license of a paid portal

- **WHEN** the license says trial and the tariff holds a Docs Connect or Dev Pack quota
- **THEN** `getTenantInfo` reports `license.trial` false

#### Scenario: Never activated

- **WHEN** the portal has no activated Docs Connect tenant
- **THEN** `getTenant` answers 200 with an empty body and `getTenantInfo` answers 400 containing "DocsCloud resource not found"

### Requirement: Updating the Docs Connect configuration

`updateTenantConfig` SHALL refuse with 400, before contacting Docs Connect, a `tenantName`, `security.secret`,
`security.header` or `ipFilter.rules` address over 255 characters and a `server.fileSizeLimit` outside its range. A valid
update is sent to Docs Connect, recorded in the audit trail and answered with the stored configuration.

#### Scenario: Text too long

- **WHEN** the owner sends a `tenantName`, secret, header or filter address of 256 characters
- **THEN** the answer is 400 naming the field

#### Scenario: File size limit

- **WHEN** the owner sends `server.fileSizeLimit` 9999999999
- **THEN** the answer is 400

### Requirement: Docs Connect quota report

`createTenantQuotaReport` SHALL queue a report job for the caller and answer with its initial state. `getTenantQuotaReport`
returns the caller's own job or an empty body, and `terminateTenantQuotaReport` asks to cancel the caller's own job,
accepted even without one. A tenant is not checked when the job starts.

#### Scenario: Start

- **WHEN** the owner or a DocSpace admin calls `createTenantQuotaReport`
- **THEN** the answer is 200 with a non-empty `id`, empty `error`, `isCompleted` false, `percentage` 0 and `status` Created

#### Scenario: Cancel

- **WHEN** the owner or a DocSpace admin calls `terminateTenantQuotaReport` after starting a report
- **THEN** the answer is a success
