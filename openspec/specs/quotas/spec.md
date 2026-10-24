# quotas Specification

## Purpose

Defines the storage and count limits of a portal and how the server accounts for them: the storage allowance
of the tariff (`maxTotalSize`) and what is charged against it, the portal-level quota settings (the default
per-user, per-room and per-AI-agent quota and their switches, the Standalone portal-wide quota), the custom
quota of an account, how a write is refused once a quota is used up and the soft overrun the server allows,
the used-space statistics of the sections and of the portal, what each user type is shown of the portal
quota, the count limits of the tariff (rooms, paid users, free users, AI agents), the quota recalculation
job and the socket events that announce quota changes. The per-room quota itself — setting, resetting and
the soft overrun of a room — is specified in `room-types` ("Per-room storage quota", "Behaviour when the
room quota is exceeded") and is only referred to here. Billing, wallet and payments are out of scope except
where a tariff limit is the direct cause of a refusal; a 402 can also come from a wallet spending limit on
the AI services, which is neither a tariff limit nor a count and is specified in `billing` ("Spending limits
on the AI services").

Sources: the integration tests of `products/ASC.Files/Tests/Tests/07_Settings/Quota`, `02_Folders/UsedSpace`,
`03_Rooms/FormFilling/FormFillingRoomUsedSpaceTests.cs`, `products/ASC.People/Tests/Tests/07_Quota`,
`web/ASC.Web.Api.Tests/Tests/02_Settings/Quota`, `04_Portal/Quota`, `04_Portal/Users` and the quota parts of
`04_Portal/Payments`; the OpenAPI contract `sdk/docspace-api-spec/docspace-backend.yaml` (operations are
named by their `operationId`); the product code where the tests stop (the quota-row controller of the
storage layer, the file DAO, the editor configuration builder, the quota DTO helper and the feature
checkers); and the product release notes (v1.1 to v4.0), which say what was shipped and when. Where the
release notes and the server disagree this specification follows the server; the differences and the
evidence for every scenario are listed in `verification.md` next to this file. HTTP status codes are stated
where the tests assert them.

## Requirements

### Requirement: Portal storage allowance and what is charged against it

The portal SHALL have one storage allowance, `maxTotalSize` of the tariff quota (`getPortalQuota`; feature
`total_size` of `getQuotaPaymentInformation`), in bytes. The server SHALL keep the used space as per-owner
counters updated by every write and delete of file content and SHALL report the portal total as the sum of
the tagged counters. The Trash keeps the space; only leaving the Trash releases it. Thumbnails SHALL NOT be
charged against the quota (v1.1).

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
- **THEN** the My documents counter is unchanged, because operations that do not change stored content move no counter

#### Scenario: Thumbnails and untagged counters are not counted

- **WHEN** the stored size of a file is summed, or the portal total is computed
- **THEN** the files whose name starts with `thumb` are excluded from the file's size, and a counter row without a product tag is left out of the portal total

### Requirement: Portal storage allowance enforced on uploads

A write of file content SHALL be refused when the file does not fit into the remaining allowance of the
portal (`maxTotalSize` minus the portal total) or exceeds the per-file limit of the tariff (`maxFileSize`).
The refusal SHALL answer 402 with "The maximum file size is exceeded (<remaining size>)". The check is
strict for uploads, chunked uploads, conversions, copies and every other write; only the editor save path is
allowed the overrun of the next requirement.

#### Scenario: File larger than the remaining allowance

- **WHEN** the remaining allowance of the portal is smaller than the file being uploaded
- **THEN** the system answers 402 with the "maximum file size is exceeded" message and the remaining allowance — not the file size — in brackets

#### Scenario: Refusal does not consume the allowance

- **WHEN** an upload is refused by the portal allowance
- **THEN** nothing is written and the counters are unchanged

#### Scenario: Chunked upload refused during the chunk upload

- **WHEN** a quota refuses the write while the chunks of a chunked upload are being received
- **THEN** the system answers 402 with a message naming the quota

### Requirement: Editor save may overrun the portal allowance once; the editor opens read-only when it is exhausted

A document saved from the editor SHALL be allowed to take the portal above `maxTotalSize` once, by at most a
configured grace size (100 MiB by default), when the portal is not yet above the limit and the file is within
`maxFileSize` (v3.7, v4.0); the grant is serialized per portal, audited and announced by `quota_exceeded`
(scope `tenant`). Once a quota is exhausted, opening a document SHALL set `quotaExceededScope` and open it
without the edit right.

#### Scenario: First save over the limit is accepted, the next is refused

- **WHEN** a document is saved from the editor and the portal is under `maxTotalSize` but the saved size takes it over by less than the grace size
- **THEN** the save is accepted, the audit trail records "saved but tenant quota exceeded", and the `tenant` quota-exceeded socket event is sent
- **AND** the grant is consumed once under concurrency, so a further save while the portal is above the limit is refused with 402

#### Scenario: Editor opens for viewing when a quota is exhausted

- **WHEN** a document is opened for editing while the portal total is above `maxTotalSize` (or above the Standalone portal-wide quota), the room is at its room quota or the caller is at their user quota
- **THEN** the editor configuration carries `quotaExceededScope` (`Tenant`, `Room` or `User`) and the document is opened without the edit right, so further saves are not attempted

### Requirement: Default user quota of the portal

The portal SHALL carry a per-user quota setting (`enableQuota`, `defaultQuota` in bytes,
`lastRecalculateDate`, `lastModified`), read with `getUserQuotaSettings` and written with
`POST /api/2.0/settings/userquotasettings`. Both SHALL require the portal-settings right (403 for a Room
admin, a User and a Guest, 401 for anonymous). The write SHALL validate `defaultQuota` against the portal's
storage and be audited; the read SHALL support `If-Modified-Since`.

#### Scenario: Owner and DocSpace admin read the settings

- **WHEN** the owner or a DocSpace admin calls `getUserQuotaSettings`
- **THEN** the system answers 200 with the settings and a non-default `lastModified`
- **AND** a repeated read with `If-Modified-Since` at that `lastModified` answers 304

#### Scenario: Other roles are refused

- **WHEN** a Room admin, a User or a Guest calls `getUserQuotaSettings`, or an anonymous caller does
- **THEN** the system answers 403 (401 for anonymous)

#### Scenario: Default quota is enabled over raw HTTP

- **WHEN** the owner posts `{"enableQuota":true,"defaultQuota":524288000}` to `settings/userquotasettings`, which is hidden from the OpenAPI document and the SDKs
- **THEN** the system answers 2xx and `resetUsersQuota` afterwards reports 500 MB as the limit of every reset account

#### Scenario: Validation of the default user quota

- **WHEN** `defaultQuota` is not a whole number, is larger than `maxTotalSize` of the tariff, or on a Standalone installation is larger than the portal-wide quota while that is on
- **THEN** the system answers 400 "Failed to set quota per user. The entered value is greater than the total storage."
- **AND** a hosted portal whose tariff lacks the `statistic` feature answers 402, and a value of 0 or less is stored as 0

### Requirement: Custom quota of an account

`updateUserQuota` SHALL give the listed accounts their own storage limit (`quota` in bytes; 0 or more is the
personal limit, a negative value removes it) and `resetUsersQuota` SHALL return them to the portal default.
Both SHALL require the portal-settings right (403 "Access denied" for a Room admin, a User and a Guest, even
on their own account; 401 for anonymous); the owner and a DocSpace admin may change any account, the owner's
included.

#### Scenario: Owner sets and resets a custom quota

- **WHEN** the owner sets 100 MB on their own account, then on a DocSpace admin, a Room admin and a User, and resets all of them with the 500 MB default enabled
- **THEN** every answer lists the accounts reached, each with `quotaLimit`, `usedSpace` and `isCustomQuota`, in the order given — `quotaLimit` 100 MB after the update and 500 MB after the reset

#### Scenario: DocSpace admin changes the owner's quota

- **WHEN** a DocSpace admin updates and resets the quota of the owner, a Room admin and a User
- **THEN** the system answers 200 with the three accounts

#### Scenario: Room admin, User, Guest and anonymous are refused

- **WHEN** a Room admin, a User or a Guest calls `updateUserQuota` or `resetUsersQuota` for themselves or for other accounts, or an anonymous caller does
- **THEN** the system answers 403 with "Access denied" (401 for anonymous)

#### Scenario: Quota above the total storage is refused

- **WHEN** the portal-wide quota is capped at 1 MB and the owner asks for 100 MB on an account, or the value is larger than `maxTotalSize` of the tariff
- **THEN** the system answers 400 with "Failed to set quota per user. The entered value is greater than the total storage."

#### Scenario: System accounts, the statistic feature and a limit below the usage

- **WHEN** the list of `updateUserQuota` or `resetUsersQuota` contains a system account, or `resetUsersQuota` is called on a hosted portal whose tariff lacks `statistic`, or an account is given a limit below what it already uses
- **THEN** the system account is dropped from the list silently, the reset without `statistic` answers 402, and the limit frees nothing — the account simply cannot add more

### Requirement: Behaviour when the user quota is exceeded

While the user quota is on, an account's limit SHALL be its custom quota or else the portal default (`-1`:
no limit) and its used space the sum of its own counters. A save into My documents that does not fit SHALL
be allowed once as a soft overrun — from under the limit to below twice it — announced on the socket and
audited; otherwise it SHALL be refused with 402 "User space quota exceeded (<limit>)". Moves, copies and
restores SHALL be checked against the destination.

#### Scenario: Soft overrun of the user quota

- **WHEN** the user quota is on and an account below its limit saves a file that takes it above the limit but below twice the limit
- **THEN** the file is saved, the `user` quota-exceeded socket event is sent and the audit trail records "saved but user quota exceeded"
- **AND** the next file is refused with 402 "User space quota exceeded"

#### Scenario: File more than twice the limit

- **WHEN** an account saves a file that would take it above twice its limit
- **THEN** the system refuses with 402 and records "not saved due to user quota"

#### Scenario: Move, copy and restore into a full destination

- **WHEN** a file that does not fit is moved or copied into My documents, or restored from the Trash into My documents
- **THEN** the file is refused outright — 402 from a move, and the advisory `checkdestfolder` reports the same "User space quota exceeded (<limit>)" message as an error
- **AND** a move or copy into a room is checked the same way against the room quota with "Room space quota exceeded (<limit>)"

#### Scenario: Move between owners raises the receiver's quota

- **WHEN** a file is moved from one account's My documents to another's and the receiver's remaining space is smaller than the file
- **THEN** the move is not refused: the receiver's custom quota becomes its limit plus the file size, the change is announced on the socket and the move succeeds

### Requirement: Default room quota and default AI agent quota of the portal

`saveRoomQuotaSettings` SHALL set the per-room default and `saveAiAgentQuotaSettings` the per-AI-agent
default (v3.6), each with `enableQuota` and `defaultQuota` in bytes. Both SHALL require the portal-settings
right (403 for a Room admin, a User and a Guest, 401 for anonymous) and on a hosted portal the `statistic`
feature (402), and SHALL answer the saved settings. While a feature is off, a custom quota on a room or agent
SHALL be refused (403, see `room-types`).

#### Scenario: Owner and DocSpace admin switch the room quota on and off

- **WHEN** the owner or a DocSpace admin calls `saveRoomQuotaSettings` with `enableQuota:true, defaultQuota:1073741824`, then with `enableQuota:false, defaultQuota:-1`
- **THEN** the first answer reports `enableQuota:true` and `defaultQuota` 1 GB, the second `enableQuota:false`; each answer carries `enableQuota`, `defaultQuota`, `lastRecalculateDate` and `lastModified`, and the default applies to rooms created afterwards

#### Scenario: AI agent quota behaves the same

- **WHEN** the owner or a DocSpace admin calls `saveAiAgentQuotaSettings` with the same two bodies
- **THEN** the answers mirror the room case
- **AND** `aiAgentsUpdateQuota` and `aiAgentsResetQuota` relay the room quota calls for AI agents through the AI service

#### Scenario: Other roles are refused

- **WHEN** a Room admin, a User or a Guest calls `saveRoomQuotaSettings` or `saveAiAgentQuotaSettings`, or an anonymous caller does
- **THEN** the system answers 403 (401 for anonymous)

#### Scenario: Validation of the default room or agent quota

- **WHEN** `defaultQuota` is larger than `maxTotalSize`, or on Standalone larger than the portal-wide quota while that is on
- **THEN** the system answers 500 (the refusal is a plain error, not a validation answer — see `verification.md`)
- **AND** a value of 0 or less is stored as 0, and `enableQuota:false` keeps the stored size but enforces nothing

### Requirement: Portal-wide quota of a Standalone installation

`setTenantQuotaSettings` SHALL set the storage quota of one tenant of a self-hosted installation
(`tenantId`, `quota` in bytes; negative removes the limit). It SHALL require the portal-settings right and
the `statistic` feature (402) and SHALL be refused on a hosted portal or for a caller below DocSpace admin
(415). The change is announced to the DocSpace admins and audited. While on, the quota SHALL cap every write
(soft overrun below twice) and every quota set afterwards.

#### Scenario: Owner caps the portal of a test host

- **WHEN** the owner calls `setTenantQuotaSettings` with the portal's own `tenantId` and 1 MB
- **THEN** the system answers 200, the DocSpace admins receive `change-user-quota-used-value`, and a per-user quota of 100 MB is refused afterwards with 400 — the portal quota caps every default and custom quota (user, room, AI agent) set after it

#### Scenario: Write above twice the portal quota

- **WHEN** the portal-wide quota is on and a write would take the portal above twice the quota, or the portal is already above it
- **THEN** the system answers 402 with "The used storage size should not exceed <quota>"
- **AND** a write that takes the portal above the quota but below twice is accepted and marked as exceeded (socket event `quota_exceeded`, scope `tenant`)

### Requirement: Used space by section

`getFilesUsedSpace` SHALL report, for the whole portal, the space in bytes taken by each root section with
its localized title: `myDocumentsUsedSpace`, `trashUsedSpace`, `roomsUsedSpace` and `archiveUsedSpace`
always and, when the root exists, `formsUsedSpace`, `aiAgentsUsedSpace`, the room templates and the default
templates. It SHALL require the portal-settings right (403; 401 for anonymous). The counters are incremental
and can lag the operation by a moment.

#### Scenario: Sections are present with titles and non-negative values

- **WHEN** the owner has a file in My documents and an archived room with a file, and calls `getFilesUsedSpace`
- **THEN** the four mandatory sections are present with the titles "Files", "Trash", "Rooms", "Archive" and whole-number values of 0 or more

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
- **THEN** the Forms counter grows by the upload, moves to Archive and back, and returns to its initial value on deletion, while the Rooms counter never changes although the room lives under the rooms tree
- **AND** an upload into a Custom room grows Rooms and leaves Forms unchanged

#### Scenario: Who may read the statistics

- **WHEN** a DocSpace admin, a Room admin, a User, a Guest or an anonymous caller calls `getFilesUsedSpace`
- **THEN** the DocSpace admin gets 200, the Room admin, User and Guest 403, the anonymous caller 401

### Requirement: Portal quota and usage statistics

`getPortalQuota` SHALL return the allowance of the tariff, `getPortalUsedSpace` the portal total in
gigabytes (two decimals), `getPortalUsersCount` the number of active accounts and `getRightQuota` the
cheapest monthly quota the portal still fits into (200 with an empty body when none). All four SHALL require
the portal-settings right (403 "Access denied"; 401 for anonymous). `getPortalTariff` SHALL never refuse a
signed-in caller but grade its answer by role.

#### Scenario: Used space and user count for the owner and a DocSpace admin

- **WHEN** the owner or a DocSpace admin calls `getPortalUsedSpace`, and `getPortalUsersCount` on a portal with the owner plus one DocSpace admin, Room admin, User and Guest
- **THEN** the used space is a number of 0 or more and the count is 5 — every role counted, invited-but-pending and disabled accounts excluded

#### Scenario: Shape of the allowance and of the recommendation

- **WHEN** the owner calls `getPortalQuota` and `getRightQuota`
- **THEN** the allowance carries `maxTotalSize`, `maxFileSize`, `countRoom`, `countUser`, `countPaidUser`, `countRoomAdmin` and the feature flags
- **AND** the recommendation is the cheapest monthly quota whose user allowance exceeds the active account count and whose storage allowance exceeds the used space — always empty on a host without a tariff catalogue

#### Scenario: Statistics are refused to other roles

- **WHEN** a Room admin, a User or a Guest calls `getPortalQuota`, `getRightQuota`, `getPortalUsedSpace` or `getPortalUsersCount`, or an anonymous caller does
- **THEN** the system answers 403 with "Access denied" (401 for anonymous)

#### Scenario: Tariff is graded, not refused

- **WHEN** the owner or a DocSpace admin calls `getPortalTariff`
- **THEN** the system answers 200 with `state` Paid, `id` of 0 or more and a non-empty `quotas` — a caller with the portal-settings right gets `id`, `customerId`, `licenseDate`, the edition flags and `quotas`
- **AND** every member gets `state`, a Room admin or DocSpace admin also `dueDate` and `delayDueDate`; a Room admin, a User or a Guest gets 200 with `id` 0 and `state` Paid, an anonymous caller 401

### Requirement: Payment quota information trimmed by user type

`getQuotaPaymentInformation` SHALL answer every signed-in member except a Guest (403; 401 for anonymous)
and SHALL trim the answer to the caller's user type (bug 81534): each feature carries the lowest user type
shown it, the caller sees the features open to their type with their `used` figures, and `price` and
`dueDate` go to a DocSpace admin only. The four portal quota settings are not billing data and SHALL be the
same for every caller.

#### Scenario: Room admin sees the limits without billing data

- **WHEN** a Room admin calls `getQuotaPaymentInformation`
- **THEN** the system answers 200 with exactly the features `manager`, `total_size`, `room` and `statistic`, an empty `price` (no value, no currency), no `dueDate`, and `usersQuota`, `roomsQuota`, `aiAgentsQuota` and `tenantCustomQuota` equal to what the owner is shown

#### Scenario: User sees the storage allowance only

- **WHEN** a User calls `getQuotaPaymentInformation`
- **THEN** the system answers 200 with a single feature, `total_size`, carrying `used`, an empty `price`, no `dueDate`, and the same four quota settings as the owner

#### Scenario: DocSpace admin sees the whole quota

- **WHEN** a DocSpace admin calls `getQuotaPaymentInformation`
- **THEN** the system answers 200 with the Room admin's four features and more, and a `price` whose value is always written (0 on a free plan, never left out)

#### Scenario: Guest and anonymous are refused

- **WHEN** a Guest or an anonymous caller calls `getQuotaPaymentInformation`
- **THEN** the system answers 403 for the Guest and 401 for the anonymous caller

#### Scenario: Quota settings are reported on every quota

- **WHEN** the three default quotas and the Standalone portal quota exist on a Standalone installation or on a hosted portal whose tariff has `statistic`
- **THEN** every quota of `getQuotaPaymentInformation` reports them as `usersQuota`, `roomsQuota`, `aiAgentsQuota` and `tenantCustomQuota`; without `statistic` on a hosted portal they are left out

### Requirement: Count limits of the tariff

The tariff SHALL limit the number of rooms (`room` / `countRoom`), paid users (`manager` / `countPaidUser`:
DocSpace and Room admins), free users (`users` / `countUser`) and AI agents (`aiagent`, v3.6); `-1` means
unlimited. Exceeding a limit SHALL be refused, under a per-portal lock, with 402 "The number of
rooms/admins/AI agents should not exceed <limit>". A 402 from an AI request may instead be a wallet
spending limit (`billing`, "Spending limits on the AI services").

#### Scenario: Which actions count against which limit

- **WHEN** a room is created, created from a template or as a third-party room, copied or restored from the archive; an AI agent is created; an account is invited or promoted to DocSpace admin or Room admin, re-enabled in such a role, provisioned by SSO/LDAP in one, or the invitation link of a paid role is read or created; a User or a Guest is invited
- **THEN** the room actions are checked against `room`, the agent against `aiagent`, the paid-role actions against `manager` and the User or Guest invitation against `users`

#### Scenario: Invitation link of a paid role when the paid limit is reached

- **WHEN** the paid-user limit of the portal is reached and the owner reads the invitation link for DocSpace admin or Room admin
- **THEN** the system answers 402 instead of returning a link that would downgrade the invitee

#### Scenario: Unlimited plan has no quota-reached branch

- **WHEN** the portal's `manager` feature reports `-1`
- **THEN** the limit cannot be reached and the invitation link is returned

#### Scenario: Room limit on creation and on restore

- **WHEN** the number of rooms equals `countRoom` and a room is created, copied or restored from the archive
- **THEN** the operation is refused with 402 "The number of rooms should not exceed <limit>"

#### Scenario: Bulk invitation and a lapsed tariff

- **WHEN** a bulk invitation would exceed the paid limit, or any counted action is attempted while the tariff is in the grace or not-paid state
- **THEN** the bulk invitation is refused as a whole with "The number of admins exceeds the limit: <asked>/<free>.", and the lapsed tariff answers 402 "Your pricing plan does not support this option"

#### Scenario: Paid role for a User or a Guest

- **WHEN** a User or a Guest is invited to a room directly with a paid role, or follows a room invitation link that grants one
- **THEN** the direct invitation is refused regardless of the quota (403, the type rule in `room-role-permissions`), and the link lowers the role to the highest free role of that room type (see `invitations`)

### Requirement: Quota recalculation

`GET /api/2.0/settings/recalculatequota` and `GET /api/2.0/people/recalculatequota` SHALL start one
background job per portal that recomputes the stored-space counters and stamps `lastRecalculateDate` on
every quota setting; a running job is reused, a finished one is started again.
`settings/checkrecalculatequota` SHALL answer `true` while the job runs, `people/checkrecalculatequota` the
progress (`isCompleted`, `progress`). All four SHALL require the portal-settings right.

#### Scenario: Owner starts and polls the job

- **WHEN** the owner calls `settings/recalculatequota` and then `settings/checkrecalculatequota` — both hidden from the OpenAPI document, like the two `people` calls
- **THEN** the job runs for the portal and the check reports `true` until it completes, then `false`

#### Scenario: What the job recomputes

- **WHEN** the job runs
- **THEN** it recomputes the portal counter from the stored files, the counter of every account and the size of every folder, and stamps `lastRecalculateDate` on the portal, user, room and AI agent quota settings

### Requirement: Quota changes are announced on the socket

Every change of a used-space counter SHALL send `change-quota-used-value` (`total_size`) to the portal's
quota room, and a change of an account's counter `change-user-quota-used-value` to that account; a room
count change sends `change-quota-used-value` (`room`). The soft overruns send `quota_exceeded` with the
scope `user`, `room` or `tenant`, and changing a default quota or the Standalone quota sends
`change-user-quota-used-value` to the DocSpace admins.

#### Scenario: Soft overrun notifies the scope

- **WHEN** a save overruns the user, room or portal quota within the allowed margin
- **THEN** `quota_exceeded` is sent with that scope and the id of the user, room or portal
