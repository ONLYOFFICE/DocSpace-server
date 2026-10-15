# quotas Specification

## Purpose

Defines the storage and count limits of a portal and how the server accounts for them: the storage allowance
of the tariff (`maxTotalSize`) and what is charged against it, the portal-level quota settings (the default
per-user, per-room and per-AI-agent quota and their switches, the Standalone portal-wide quota), the custom
quota of an account, how a write is refused once a quota is used up and the soft overrun the server allows,
the used-space statistics of the sections and of the portal, the count limits of the tariff (rooms, paid
users, free users, AI agents), the quota recalculation job and the socket events that announce quota changes.
The per-room quota itself — setting, resetting and the soft overrun of a room — is specified in
`room-types` ("Per-room storage quota", "Behaviour when the room quota is exceeded") and is only referred to
here. Billing, wallet and payments are out of scope except where a tariff limit is the direct cause of a
refusal.

Sources: the integration tests of `products/ASC.Files/Tests/Tests/07_Settings/Quota`, `02_Folders/UsedSpace`,
`03_Rooms/FormFilling/FormFillingRoomUsedSpaceTests.cs`, `products/ASC.People/Tests/Tests/07_Quota`,
`web/ASC.Web.Api.Tests/Tests/02_Settings/Quota`, `04_Portal/Quota`, `04_Portal/Users` and the tariff-limit
parts of `04_Portal/Payments`; the OpenAPI contract `sdk/docspace-api-spec/docspace-backend.yaml` (operations
are named by their `operationId`); the product code where the tests stop (`TenantQuotaController`,
`FileDao`, `DocumentServiceHelper`, `QuotaHelper`, the `*Checker` classes); and the product release notes
(v1.1 to v4.0), which say what was shipped and when. Where the release notes and the server disagree this
specification follows the server; the differences and the evidence for every scenario are listed in
`verification.md` next to this file. HTTP status codes are stated where the tests assert them.

## Requirements

### Requirement: Portal storage allowance and what is charged against it

The portal SHALL have one storage allowance, `maxTotalSize` of the tariff quota (`getPortalQuota`, feature
`total_size` of `getQuotaPaymentInformation`), in bytes. The server SHALL keep the used space as per-owner
counters (`tenants_quotarow`) that are updated by every write and delete of file content, and SHALL report
the portal total as the sum of the rows that carry a product tag; rows without a tag are not counted. The
counters follow the content: moving an entry to the Trash moves its space to the Trash section and frees
nothing, and the space is released only when the entry leaves the Trash (a hard delete, emptying the Trash).
Operations that do not change stored content (reading metadata, renaming) SHALL NOT move the counters.
Thumbnails SHALL NOT be charged against the quota (v1.1): the files whose name starts with `thumb` are
excluded when the stored size of a file is summed.

#### Scenario: Creating a file charges the portal total

- **WHEN** a `.docx` is created in My documents of the owner
- **THEN** `total_size.used` of `getQuotaPaymentInformation` grows by the file's `pureContentLength`
- **AND** ten files created concurrently grow it by the sum of their sizes

#### Scenario: Trash keeps the space, leaving the Trash releases it

- **WHEN** a file, a folder or a three-level tree is moved to the Trash
- **THEN** the portal total is unchanged
- **AND** when the entry is deleted from the Trash, the Trash is emptied, or a batch delete removes a tree and a file at once, the total returns to its value before the content was created

#### Scenario: Reads and renames do not move the counters

- **WHEN** a file is read three times by id, or renamed
- **THEN** the My documents counter is unchanged

### Requirement: Portal storage allowance enforced on uploads

A write of file content SHALL be refused when the file does not fit into the remaining allowance of the
portal (`maxTotalSize` minus the portal total) or exceeds the per-file limit of the tariff
(`maxFileSize`). The refusal is a `TenantQuotaException` and SHALL answer 402 with the message "The maximum
file size is exceeded (<remaining size>)" — the remaining size, not the file size, is quoted. The check is
strict for uploads, conversions, copies and every other write; only the editor save path is allowed the
overrun described in the next requirement. A chunked upload answers 402 with a message naming the quota
when the write is refused during the chunk upload.

#### Scenario: File larger than the remaining allowance

- **WHEN** the remaining allowance of the portal is smaller than the file being uploaded
- **THEN** the system answers 402 with the "maximum file size is exceeded" message and the remaining allowance in brackets

#### Scenario: Refusal does not consume the allowance

- **WHEN** an upload is refused by the portal allowance
- **THEN** nothing is written and the counters are unchanged

### Requirement: Editor save may overrun the portal allowance once; the editor opens read-only when it is exhausted

A document saved from the editor (`EntryManager.SaveEditingAsync`) SHALL be allowed to take the portal above
`maxTotalSize` once, by at most the grace size `web:available-file-size` (100 MiB by default), when the
portal is not yet above the limit and the file does not exceed `maxFileSize` (v3.7, v4.0). The grant is
serialized per portal, so under concurrency it is consumed once; the save is recorded in the audit trail as
"saved but tenant quota exceeded" and the socket event `quota_exceeded` with scope `tenant` is raised.
Once the portal total is above `maxTotalSize` (or above the Standalone portal-wide quota), opening a document
SHALL set `quotaExceededScope` to `Tenant` in the editor configuration and open it without the edit right, so
further saves are not attempted. The same read-only opening SHALL apply when the room quota of the room
(`quotaExceededScope: Room`) or the user quota of the caller (`quotaExceededScope: User`) is at or above its
limit.

#### Scenario: First save over the limit is accepted, the next is refused

- **WHEN** a document is saved from the editor and the portal is under `maxTotalSize` but the saved size takes it over by less than the grace size
- **THEN** the save is accepted, the audit trail records it, and the `tenant` quota-exceeded socket event is sent
- **AND** a further save while the portal is above the limit is refused with 402

#### Scenario: Editor opens for viewing when a quota is exhausted

- **WHEN** a document is opened for editing while the portal total is above `maxTotalSize`, the room is at its room quota or the caller is at their user quota
- **THEN** the editor configuration carries `quotaExceededScope` (`Tenant`, `Room` or `User`) and the document is opened without the edit right

### Requirement: Default user quota of the portal

The portal SHALL carry a per-user quota setting (`TenantUserQuotaSettings`: `enableQuota`, `defaultQuota`
in bytes, `lastRecalculateDate`, `lastModified`), read with `getUserQuotaSettings` and written with
`POST /api/2.0/settings/userquotasettings` (hidden from the OpenAPI document and the SDKs). Both calls
SHALL require the portal-settings right (the owner and DocSpace admins): a Room admin, a User and a Guest are
refused with 403, an anonymous caller with 401. Writing SHALL refuse a `defaultQuota` that is not a whole
number or is larger than `maxTotalSize` of the tariff (400, "Failed to set quota per user. The entered value is
greater than the total storage."), and on a Standalone installation also larger than the portal-wide quota
when that is on; a hosted portal whose tariff lacks the `statistic` feature answers 402. A value of 0 or less
is stored as 0. The read supports `If-Modified-Since` (304 when unchanged) and the write is recorded in the
audit trail.

#### Scenario: Owner and DocSpace admin read the settings

- **WHEN** the owner or a DocSpace admin calls `getUserQuotaSettings`
- **THEN** the system answers 200 with the settings and a non-default `lastModified`

#### Scenario: Other roles are refused

- **WHEN** a Room admin, a User or a Guest calls `getUserQuotaSettings`, or an anonymous caller does
- **THEN** the system answers 403 (401 for anonymous)

#### Scenario: Default quota is enabled over raw HTTP

- **WHEN** the owner posts `{"enableQuota":true,"defaultQuota":524288000}` to `settings/userquotasettings`
- **THEN** the system answers 2xx and `resetUsersQuota` afterwards reports 500 MB as the limit of every reset account

### Requirement: Custom quota of an account

`updateUserQuota` SHALL give the listed accounts their own storage limit (`quota` in bytes; 0 or more becomes
the personal limit, a negative value removes it and the account follows the portal default) and
`resetUsersQuota` SHALL return them to the portal default. Both SHALL require the portal-settings right
(403 "Access denied" for a Room admin, a User and a Guest — on their own account as well as on others —,
401 for anonymous), and the owner and a DocSpace admin may change any account including the owner's.
`updateUserQuota` SHALL refuse a value larger than the portal's total storage (400, "Failed to set quota per
user. The entered value is greater than the total storage."), on Standalone also one larger than the
portal-wide quota when it is on; `resetUsersQuota` on a hosted portal whose tariff lacks `statistic` answers
402. System accounts are dropped from the list silently; the answer lists the accounts that were reached, each
with `quotaLimit`, `usedSpace` and `isCustomQuota`. Setting a limit frees nothing: an account already above
its new limit simply cannot add more.

#### Scenario: Owner sets and resets a custom quota

- **WHEN** the owner sets 100 MB on their own account, then on a DocSpace admin, a Room admin and a User, and resets all of them with the 500 MB default enabled
- **THEN** every answer lists the accounts with `quotaLimit` 100 MB after the update and 500 MB after the reset, in the order given

#### Scenario: DocSpace admin changes the owner's quota

- **WHEN** a DocSpace admin updates and resets the quota of the owner, a Room admin and a User
- **THEN** the system answers 200 with the three accounts

#### Scenario: Room admin, User, Guest and anonymous are refused

- **WHEN** a Room admin, a User or a Guest calls `updateUserQuota` or `resetUsersQuota` for themselves or for other accounts, or an anonymous caller does
- **THEN** the system answers 403 with "Access denied" (401 for anonymous)

#### Scenario: Quota above the total storage is refused

- **WHEN** the portal-wide quota is capped at 1 MB and the owner asks for 100 MB on an account
- **THEN** the system answers 400 with "Failed to set quota per user. The entered value is greater than the total storage."

### Requirement: Behaviour when the user quota is exceeded

While the user quota feature is on, the limit of an account SHALL be its custom quota or, without one, the
portal default (`-1` means no limit); its used space is the sum of its own quota rows. Saving a file into
My documents of the account whose remaining space is smaller than the file SHALL be allowed once as a soft
overrun — when the account is not yet above its limit and the result stays below twice the limit — with the
socket event `quota_exceeded` (scope `user`) and an audit record; otherwise it SHALL be refused with 402
"User space quota exceeded (<limit>)". A move or copy into My documents, and a restore from the Trash into
My documents, SHALL refuse the file outright when it does not fit (402 from a move; the advisory
`checkdestfolder` reports the same message as an error). A move or copy into a room is checked the same way
against the room quota ("Room space quota exceeded (<limit>)"). Moving a file from the My documents of one
account to the My documents of another SHALL NOT be refused: when the receiving account lacks the space its
custom limit is raised by the file size and the change is announced on the socket.

#### Scenario: Soft overrun of the user quota

- **WHEN** the user quota is on and an account below its limit saves a file that takes it above the limit but below twice the limit
- **THEN** the file is saved, the `user` quota-exceeded socket event is sent and the audit trail records "saved but user quota exceeded"
- **AND** the next file is refused with 402 "User space quota exceeded"

#### Scenario: File more than twice the limit

- **WHEN** an account saves a file that would take it above twice its limit
- **THEN** the system refuses with 402 and records "not saved due to user quota"

#### Scenario: Move between owners raises the receiver's quota

- **WHEN** a file is moved from one account's My documents to another's and the receiver's remaining space is smaller than the file
- **THEN** the receiver's custom quota becomes its limit plus the file size and the move succeeds

### Requirement: Default room quota and default AI agent quota of the portal

`saveRoomQuotaSettings` SHALL set the per-room default (`TenantRoomQuotaSettings`) and
`saveAiAgentQuotaSettings` the per-AI-agent default (`TenantAiAgentQuotaSettings`, v3.6), each with
`enableQuota` and `defaultQuota` in bytes, applied to rooms and agents created afterwards. Both SHALL require
the portal-settings right (403 for a Room admin, a User and a Guest, 401 for anonymous) and on a hosted
portal the `statistic` feature of the tariff (402 otherwise). A `defaultQuota` larger than `maxTotalSize`,
or on Standalone larger than the portal-wide quota when it is on, is refused (500 — the error is a plain
exception); a value of 0 or less is stored as 0; `enableQuota:false` keeps the stored size but enforces
nothing. The answer is the saved settings (`enableQuota`, `defaultQuota`, `lastRecalculateDate`,
`lastModified`). While a feature is off, setting a custom quota on a room or agent is refused (403, see
`room-types`); `aiAgentsUpdateQuota` and `aiAgentsResetQuota` relay the same room calls for AI agents
through the AI service. The three defaults and the Standalone portal quota are also reported on every quota
of `getQuotaPaymentInformation` as `usersQuota`, `roomsQuota`, `aiAgentsQuota` and `tenantCustomQuota`
(Standalone or `statistic` only).

#### Scenario: Owner and DocSpace admin switch the room quota on and off

- **WHEN** the owner or a DocSpace admin calls `saveRoomQuotaSettings` with `enableQuota:true, defaultQuota:1073741824`, then with `enableQuota:false, defaultQuota:-1`
- **THEN** the first answer reports `enableQuota:true` and `defaultQuota` 1 GB, the second `enableQuota:false`

#### Scenario: AI agent quota behaves the same

- **WHEN** the owner or a DocSpace admin calls `saveAiAgentQuotaSettings` with the same two bodies
- **THEN** the answers mirror the room case

#### Scenario: Other roles are refused

- **WHEN** a Room admin, a User or a Guest calls `saveRoomQuotaSettings` or `saveAiAgentQuotaSettings`, or an anonymous caller does
- **THEN** the system answers 403 (401 for anonymous)

### Requirement: Portal-wide quota of a Standalone installation

`setTenantQuotaSettings` SHALL set the storage quota of one tenant of a self-hosted installation
(`tenantId`, `quota` in bytes; a negative value removes the limit). It SHALL require the portal-settings
right and the `statistic` feature (402), and SHALL be refused on a hosted portal and for a caller who is not a
DocSpace admin (`NotSupportedException`, 415). The saved state is announced to the DocSpace admins on the
socket (`change-user-quota-used-value`) and recorded in the audit trail. While the quota is on, a write
SHALL be refused with 402 "The used storage size should not exceed <quota>" when the portal is already above
the quota or the write would take it above twice the quota; a write that takes it above the quota but below
twice is accepted and marked as exceeded (socket event, scope `tenant`). The quota also caps every default
and custom quota (user, room, AI agent) set afterwards.

#### Scenario: Owner caps the portal of a test host

- **WHEN** the owner calls `setTenantQuotaSettings` with the portal's own `tenantId` and 1 MB
- **THEN** the system answers 200 and a per-user quota of 100 MB is refused afterwards with 400

#### Scenario: Write above twice the portal quota

- **WHEN** the portal-wide quota is on and a write would take the portal above twice the quota, or the portal is already above it
- **THEN** the system answers 402 with "The used storage size should not exceed <quota>"

### Requirement: Used space by section

`getFilesUsedSpace` SHALL report, for the whole portal, the space in bytes taken by each root section with
its localized title: `myDocumentsUsedSpace` ("Files"), `trashUsedSpace` ("Trash"), `roomsUsedSpace`
("Rooms"), `archiveUsedSpace` ("Archive"), and, when the root exists, `formsUsedSpace`, `aiAgentsUsedSpace`,
the room templates and the default templates. Every value is a non-negative whole number. The call SHALL
require the portal-settings right: the owner and a DocSpace admin read it, a Room admin, a User and a Guest
answer 403, an anonymous caller 401. The counters are maintained incrementally as content is created, deleted
and moved, and a change can lag the operation that caused it by a moment. Content of a Form filling room is
charged to the Forms section, not to Rooms, although the room lives under the rooms tree.

#### Scenario: Sections are present with titles and non-negative values

- **WHEN** the owner has a file in My documents and an archived room with a file, and calls `getFilesUsedSpace`
- **THEN** the four mandatory sections are present with the titles "Files", "Trash", "Rooms", "Archive" and values of 0 or more

#### Scenario: Each operation moves the counter of its section

- **WHEN** a file is created in My documents, a file is created in a Custom room, a file is moved to the Trash, a room with a file is archived
- **THEN** My documents, Rooms, Trash and Archive grow respectively by the file size, and a soft delete removes from My documents exactly what it adds to the Trash

#### Scenario: Hard delete releases the section

- **WHEN** a file in My documents is deleted immediately
- **THEN** the My documents counter decreases

#### Scenario: Deep trees and concurrent deletes keep the counters exact

- **WHEN** a tree of depth 3, breadth 2 with two files per folder is created in My documents or in a room, moved to the Trash and deleted from it, or four such trees are deleted from the Trash at once
- **THEN** My documents (or Rooms), Trash and the portal total return exactly to their values before the tree was created

#### Scenario: Form filling rooms are charged to the Forms section

- **WHEN** a PDF is uploaded into a Form filling room, the room is archived, restored and deleted
- **THEN** the Forms counter grows by the upload, moves to Archive and back, and returns to its initial value on deletion, while the Rooms counter never changes
- **AND** an upload into a Custom room grows Rooms and leaves Forms unchanged

#### Scenario: Who may read the statistics

- **WHEN** a DocSpace admin, a Room admin, a User, a Guest or an anonymous caller calls `getFilesUsedSpace`
- **THEN** the DocSpace admin gets 200, the Room admin, User and Guest 403, the anonymous caller 401

### Requirement: Portal quota and usage statistics

`getPortalQuota` SHALL return the allowance of the tariff (`maxTotalSize`, `maxFileSize`, `countRoom`,
`countUser`, `countPaidUser`, `countRoomAdmin`, the feature flags) and `getPortalUsedSpace` the portal total
in gigabytes rounded to two decimals; `getPortalUsersCount` SHALL return the number of accounts in the
active state, every role counted, invited-but-pending and disabled accounts excluded; `getRightQuota` SHALL
pick the cheapest monthly quota whose user allowance exceeds that count and whose storage allowance exceeds
the used space, or answer 200 with an empty body when none exists (always empty on a host without a tariff
catalogue). All four SHALL require the portal-settings right (403 "Access denied" for a Room admin, a User
and a Guest, 401 for anonymous). `getPortalTariff` SHALL never refuse a signed-in caller: every member gets
`state`, a Room admin or DocSpace admin also `dueDate` and `delayDueDate`, and a caller with the
portal-settings right also `id`, `customerId`, `licenseDate`, the edition flags and `quotas`; a member without
the right gets `id` 0. `getQuotaPaymentInformation` SHALL answer every signed-in member but a Guest (403):
the features are graded by role — features marked for DocSpace admins are dropped for other roles, and the
`used` figures are withheld from a User except `total_size`.

#### Scenario: Used space and user count for the owner and a DocSpace admin

- **WHEN** the owner or a DocSpace admin calls `getPortalUsedSpace`, and `getPortalUsersCount` on a portal with the owner plus one DocSpace admin, Room admin, User and Guest
- **THEN** the used space is a number of 0 or more and the count is 5

#### Scenario: Statistics are refused to other roles

- **WHEN** a Room admin, a User or a Guest calls `getPortalQuota`, `getRightQuota`, `getPortalUsedSpace` or `getPortalUsersCount`, or an anonymous caller does
- **THEN** the system answers 403 with "Access denied" (401 for anonymous)

#### Scenario: Tariff is graded, not refused

- **WHEN** the owner or a DocSpace admin calls `getPortalTariff`
- **THEN** the system answers 200 with `state` Paid, `id` of 0 or more and a non-empty `quotas`
- **AND** a Room admin, a User or a Guest gets 200 with `id` 0 and `state` Paid, an anonymous caller 401

#### Scenario: Payment quota information by role

- **WHEN** a Room admin or a User calls `getQuotaPaymentInformation`
- **THEN** both get 200 with a non-empty feature list, and for the User the only feature carrying `used` is `total_size`
- **AND** a Guest gets 403 and an anonymous caller 401

### Requirement: Count limits of the tariff

The tariff SHALL limit the number of rooms (`room` / `countRoom`), of paid users (`manager` /
`countPaidUser`: DocSpace admins and Room admins), of free users (`users` / `countUser`) and of AI agents
(`aiagent`, v3.6); `-1` in a feature value means unlimited. The server SHALL check the limit before the
action that would exceed it, under a per-portal lock, and refuse with 402 (`TenantQuotaException`) and the
message "The number of rooms/admins/AI agents should not exceed <limit>": creating a room, a room from a
template or a third-party room, copying a room and restoring a room from the archive count against `room`;
creating an AI agent against `aiagent`; inviting or promoting an account to DocSpace admin or Room admin,
re-enabling such an account, SSO/LDAP provisioning of one and reading or creating the invitation link of a
paid role against `manager`; inviting a User or a Guest against `users`. A bulk invitation that would exceed
the paid limit is refused as a whole with "The number of admins exceeds the limit: <asked>/<free>." When
the tariff is in the grace or not-paid state the same checks answer 402 with "Your pricing plan does not
support this option" (`BillingNotFoundException`). A direct room invitation of a User or a Guest with a paid role is
refused regardless of the quota (403, the type rule in `room-role-permissions`); only when a User or a Guest
follows a room invitation link that grants a paid role is the role lowered to the highest free role of that
room type (see `invitations`).

#### Scenario: Invitation link of a paid role when the paid limit is reached

- **WHEN** the paid-user limit of the portal is reached and the owner reads the invitation link for DocSpace admin or Room admin
- **THEN** the system answers 402 instead of returning a link that would downgrade the invitee

#### Scenario: Unlimited plan has no quota-reached branch

- **WHEN** the portal's `manager` feature reports `-1`
- **THEN** the limit cannot be reached and the invitation link is returned

#### Scenario: Room limit on creation and on restore

- **WHEN** the number of rooms equals `countRoom` and a room is created, copied or restored from the archive
- **THEN** the operation is refused with 402 "The number of rooms should not exceed <limit>"

### Requirement: Quota recalculation

`GET /api/2.0/settings/recalculatequota` and `GET /api/2.0/people/recalculatequota` (hidden from the OpenAPI
document) SHALL start one background job per portal that recomputes the portal counter from the stored
files, the counter of every account, and the size of every folder, and stamps `lastRecalculateDate` on the
portal, user, room and AI agent quota settings; a job already running for the portal is reused, a finished
one is dropped and started again. `settings/checkrecalculatequota` SHALL answer `true` while the job runs,
`people/checkrecalculatequota` the progress (`isCompleted`, `progress`). All four SHALL require the
portal-settings right.

#### Scenario: Owner starts and polls the job

- **WHEN** the owner calls `settings/recalculatequota` and then `settings/checkrecalculatequota`
- **THEN** the job runs for the portal and the check reports `true` until it completes, then `false`

### Requirement: Quota changes are announced on the socket

Every change of a used-space counter SHALL send `change-quota-used-value` (`total_size`) to the portal's
quota room, and a change of an account's counter `change-user-quota-used-value` to that account; a room
count change sends `change-quota-used-value` (`room`). The soft overruns send `quota_exceeded` with the
scope `user`, `room` or `tenant`, and changing a default quota or the Standalone quota sends
`change-user-quota-used-value` to the DocSpace admins.

#### Scenario: Soft overrun notifies the scope

- **WHEN** a save overruns the user, room or portal quota within the allowed margin
- **THEN** `quota_exceeded` is sent with that scope and the id of the user, room or portal
