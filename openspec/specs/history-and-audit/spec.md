# history-and-audit Specification

## Purpose

Defines the records a DocSpace portal keeps about what happened and who may read them: the activity
history of files, folders and rooms (the `log` endpoints and their export), the document version list
and the editing revisions of a file, the portal audit trail, the login history, the active connections
of an account and the ways of ending them, the forwarding of audit and login events to an external
syslog collector, and the suspicious-login check that runs after every sign-in.

Sources: the integration tests of `products/ASC.Files/Tests` (`01_Files/History`, `01_Files/Log`,
`02_Folders/History`) and `web/ASC.Web.Api.Tests` (`05_Security/AuditTrail`, `LoginHistory`,
`ActiveConnections`), the OpenAPI document `sdk/docspace-api-spec/docspace-backend.yaml`, the product
code (`HistoryService`, `HistoryApiHelper`, `AuditInterpreter`, `AuditEventsRepository`,
`LoginEventsRepository`, `EventTypeConverter`, `SecurityController`, `ConnectionsController`,
`AuditReportTask`, `AuditLogSender`, `SuspiciousLoginNotifier`) and the release notes v2.6 – v4.0
(October 2026). Where the notes and the server disagree, this specification follows the server; the
differences and the suspected defects are listed in `verification.md`. The "view history" right of a
user type or a room role is defined in `user-type-permissions` and `room-role-permissions`; form-filling
events are defined in `form-filling-workflow` and only cross-referenced here.

## Requirements

### Requirement: Entry history endpoints and the shape of an entry

The system SHALL expose the activity history of a file through `GET /api/2.0/files/file/{fileId}/log`
and of a folder or a room through `GET /api/2.0/files/folder/{folderId}/log`. Each entry SHALL carry
`id`, `action` (with the `MessageAction` in `action.id`), `initiator` (an account with at least
`displayName`), `date` and, where the action has details, `data`. Entries SHALL be returned newest
first, and the number of entries on the page SHALL be reported in the `count` field of the response
envelope. Only entries stored in the portal itself have a history; a file in a connected third-party
storage has none.

#### Scenario: History of a new room has the expected structure

- **WHEN** the owner reads the history of a room they have just created
- **THEN** the list is not empty, the first entry has a non-null `action.id`, an initiator with a display name and a date

#### Scenario: Unknown folder

- **WHEN** a caller reads the history of folder id 0 or of an id that does not exist
- **THEN** the system answers 404

#### Scenario: Count field matches the page

- **WHEN** the history of a room is read without paging parameters
- **THEN** the `count` of the envelope equals the number of entries in `response`

### Requirement: Entry history paging and date filter

The history endpoints SHALL accept `count` (1–100) and `startIndex` to page from the newest entry
backwards, and `fromDate` / `toDate`, read in the time zone of the portal, to narrow the period.

#### Scenario: Count limits the page

- **WHEN** a room with three entries is read with `count=1`
- **THEN** exactly one entry is returned

#### Scenario: Start index shifts the page

- **WHEN** a room with at least two entries is read with `startIndex=0,count=1` and then `startIndex=1,count=1`
- **THEN** the two pages hold different entries

#### Scenario: Start index beyond the total

- **WHEN** the history is read with `startIndex=99999`
- **THEN** the system answers 200 with an empty list

#### Scenario: Date range keeps the entries inside it

- **WHEN** a room created a moment ago is read with `fromDate` one minute before and `toDate` one minute after the creation
- **THEN** the list contains the `RoomCreated` entry

### Requirement: Who may read an entry history

The system SHALL return the history of an entry to every caller who can read the entry (the Read right
of `FileSecurity`), regardless of user type and room role, and SHALL refuse everyone else. The
initiator of an entry is rendered as an account DTO: a caller without access to Contacts (a User or a
Guest) SHALL receive it without a `profileUrl`.

#### Scenario: Owner, DocSpace admin, Room manager and a Read member read a file log

- **WHEN** the portal owner, a DocSpace admin who is not a member, a Room admin invited as Room manager or a User invited with Read reads `file/{id}/log` or `folder/{id}/log` of an entry in a Custom room
- **THEN** the system answers 200

#### Scenario: Non-member is refused

- **WHEN** a User or a Guest who is not a member of the room reads the history of the room or of a file in it
- **THEN** the system answers 403

#### Scenario: Anonymous caller is refused

- **WHEN** the history of a room or of a file is read without authentication
- **THEN** the system answers 401

#### Scenario: Initiator without profile link for User and Guest

- **WHEN** a User or a Guest invited to the room with Read reads the log of a file in it
- **THEN** every entry's `initiator.profileUrl` is empty

### Requirement: Actions that produce a history entry

The system SHALL write a history entry for the actions listed below and for no others
(`HistoryService.TrackedActions`); an audit event whose action is not tracked SHALL never appear in an
entry history even when it references the entry. The entry is written by the initiator of the action.

| Group | Actions |
|-------|---------|
| Files | FileCreated, FileUploaded, FileUploadedWithOverwriting, UserFileUpdated, FileRenamed, FileMoved, FileMovedWithOverwriting, FileMovedToTrash, FileCopied, FileCopiedWithOverwriting, FileVersionRemoved, FileDeleted, FileConverted, FileRestoreVersion, FileIndexChanged, FileLocked, FileUnlocked, FileCustomFilterEnabled, FileCustomFilterDisabled, FileSavedButUserQuotaExceeded, FileNotSavedDueToUserQuota, FileSavedButRoomQuotaExceeded, FileNotSavedDueToRoomQuota |
| Folders | FolderCreated, FolderRenamed, FolderMoved, FolderMovedWithOverwriting, FolderCopied, FolderCopiedWithOverwriting, FolderMovedToTrash, FolderDeleted, FolderIndexChanged, FolderIndexReordered |
| Room lifecycle and settings | RoomCreated, RoomCopied, RoomRenamed, RoomArchived, RoomUnarchived, AddedRoomTags, DeletedRoomTags, RoomLogoCreated, RoomLogoDeleted, RoomColorChanged, RoomCoverChanged, RoomIndexingEnabled, RoomIndexingDisabled, RoomLifeTimeSet, RoomLifeTimeDisabled, RoomDenyDownloadEnabled, RoomDenyDownloadDisabled, RoomWatermarkSet, RoomWatermarkDisabled, RoomIndexExportSaved |
| Room links | RoomExternalLinkCreated, RoomExternalLinkRenamed, RoomExternalLinkDeleted, RoomExternalLinkRevoked |
| Room members | RoomCreateUser, RoomUpdateAccessForUser, RoomRemoveUser, RoomChangeOwner, RoomGroupAdded, RoomUpdateAccessForGroup, RoomGroupRemove, RoomInviteResend |
| Forms (see `form-filling-workflow`) | FormSubmit, FormOpenedForFilling, FormStartedToFill, FormPartiallyFilled, FormCompletelyFilled, FormStopped |
| AI agents | AgentCreated, AgentRenamed |

#### Scenario: Room creation, rename, archive and restore

- **WHEN** a room is created, renamed through `PUT rooms/{id}`, archived and then restored from the archive
- **THEN** the room history contains `RoomCreated`, `RoomRenamed`, `RoomArchived` and `RoomUnarchived`, each with the owner as initiator

#### Scenario: Files inside a room

- **WHEN** a file is created, uploaded through `POST files/{folderId}/upload` or through a chunked upload session, renamed, moved or copied to another room, moved to trash, deleted permanently, locked and unlocked, or its index is changed in an indexed room
- **THEN** the room history contains `FileCreated`, `FileUploaded`, `FileRenamed`, `FileMoved` / `FileCopied` (in the source room), `FileMovedToTrash`, `FileDeleted`, `FileLocked`, `FileUnlocked` and `FileIndexChanged`

#### Scenario: Folders inside a room and in My documents

- **WHEN** a subfolder is created, renamed, moved or copied to another room, moved to trash or deleted permanently, or a folder is created inside My documents
- **THEN** the history of the room (or of the My documents subfolder) contains `FolderCreated`, `FolderRenamed`, `FolderMoved` / `FolderCopied`, `FolderMovedToTrash` and `FolderDeleted`
- **AND** `SetFolderOrder` on a subfolder of an indexed room writes `FolderIndexChanged` into the subfolder's own history

#### Scenario: Room settings

- **WHEN** tags are added and removed, a logo is set and deleted, the icon colour or the cover is changed, a lifetime is set and disabled, indexing is enabled and disabled, an indexed room is reordered, a watermark is set and disabled, download is restricted and allowed again
- **THEN** the room history contains `AddedRoomTags`, `DeletedRoomTags`, `RoomLogoCreated`, `RoomLogoDeleted`, `RoomColorChanged`, `RoomCoverChanged`, `RoomLifeTimeSet`, `RoomLifeTimeDisabled`, `RoomIndexingEnabled`, `RoomIndexingDisabled`, `FolderIndexReordered`, `RoomWatermarkSet`, `RoomWatermarkDisabled`, `RoomDenyDownloadEnabled`, `RoomDenyDownloadDisabled`

#### Scenario: Room links

- **WHEN** a Public room is created, an additional link is created and renamed, the primary link is revoked (`access: None`) and an additional link is deleted (`access: None`)
- **THEN** the room history contains `RoomExternalLinkCreated` (written at room creation for the primary link), `RoomExternalLinkRenamed`, `RoomExternalLinkRevoked` and `RoomExternalLinkDeleted`

#### Scenario: Members, groups, owner and invitations

- **WHEN** a user is invited with Read, changed to Content creator and removed (`access: None`); a group is invited, changed to Editing and removed; the room owner is changed through `changeFileOwner`; invitations are resent
- **THEN** the room history contains `RoomCreateUser`, `RoomUpdateAccessForUser`, `RoomRemoveUser`, `RoomGroupAdded`, `RoomUpdateAccessForGroup`, `RoomGroupRemove`, `RoomChangeOwner` and `RoomInviteResend`

#### Scenario: User type change is not room history

- **WHEN** a member of the room is promoted to DocSpace admin through `people/type`
- **THEN** the room history does not contain `UsersUpdatedType`

### Requirement: Entries of one action are grouped

The system SHALL fold the events that belong to one action into a single history entry: the newest
event of the group is the entry and the others are listed in its `related` field, and the resulting
list is ordered by date descending.

#### Scenario: Batch operation on several entries

- **WHEN** several files are moved in one operation and the history of the room is read
- **THEN** one entry carries the move and its `related` list holds the other files of the same operation

### Requirement: History of a form-filling folder for a Form filler

When the caller reached a form-filling folder with the `FillForms` right, the system SHALL narrow the
history to the folders and files inside it that the caller may see, and to the actions FolderCreated,
FolderMovedWithOverwriting, FolderMovedToTrash, FolderRenamed, FileCopied, FileUploaded, FileMoved,
FileRenamed and the form events.

#### Scenario: Form filler reads the history of a form-filling folder

- **WHEN** a member whose right on a form-filling folder is `FillForms` reads its history
- **THEN** only entries about entries visible to that member and about the listed actions are returned

### Requirement: Folder history report

The system SHALL export the history of a folder or a room through
`POST /api/2.0/files/folder/{folderId}/log/report`, which queues a background task and answers 200
with the task (`id`, `status` Created or Running, `isCompleted`, `percentage`, `error`,
`resultFileId`, `resultFileName`, `resultFileUrl`); `GET` on the same path reports the caller's own
task for that folder, `DELETE` asks to stop it. `format` is `Xlsx` (default) or `Csv`; `from` / `to`
limit the period and are read in the portal time zone; left out, the whole history is exported. The
file SHALL be saved in the caller's My documents. The CSV report holds a header row and one row per
event; the XLSX report keeps at most 200,000 events (`files:audit-report:xlsx-max-rows`) and says in its
header how many were left out. The folder report has no Country, City, Page, Action-text and Context
columns, and the IP, Browser and Platform columns SHALL be included only when the caller is a DocSpace
admin. A running report for the same folder and caller is joined instead of duplicated.

#### Scenario: Report of a room, an archived room and a subfolder

- **WHEN** the owner starts a report for a Custom room, for an archived room or for a subfolder of a room
- **THEN** the system answers 200 with a task whose `id` is set and whose status is Created or Running

#### Scenario: CSV report holds the events

- **WHEN** a CSV report is started for a room that contains a created file and polled until `isCompleted`
- **THEN** `error` is empty, `resultFileId` points at a file whose rows are unique, number more than one and mention the file name

#### Scenario: Unknown folder

- **WHEN** a report is started for folder id 0, -1 or an id that does not exist
- **THEN** the system answers 404

### Requirement: Who may export a folder history

The system SHALL allow the report to every caller who can read the folder except a Guest, and SHALL
refuse the report when the portal is a SaaS portal whose login history and audit trail section is
hidden (402).

#### Scenario: Readers of the room may export

- **WHEN** the owner, a non-member DocSpace admin, a Room admin invited as Room manager or with Editing, or a User invited with Read or Comment starts the report
- **THEN** the system answers 200 with a task

#### Scenario: Guest cannot export even with access

- **WHEN** a Guest invited to the room with Read, or a Guest without access, starts the report
- **THEN** the system answers 403

#### Scenario: Non-member and anonymous

- **WHEN** a User who is not a member starts the report
- **THEN** the system answers 403
- **AND** an unauthenticated caller is answered with 401

### Requirement: Version list and editing revisions of a file

The system SHALL expose the version list of a file through `GET /api/2.0/files/file/{fileId}/history`
and its editing revisions through `GET /api/2.0/files/file/{fileId}/edit/history`. Both require the
ReadHistory right of `FileSecurity`: editing access and above grant it, Read and Comment do not, a Guest
never has it, and a third-party file has no revisions. A new file has one version (`version` 1,
`versionGroup` 1, comment "Created", `fileStatus` None); a revision carries the file id, the document
key, the version, the version group, the author and the creation date.

#### Scenario: Version list and revisions of a new file

- **WHEN** the owner creates a file in My documents and reads its version list and its editing revisions
- **THEN** both answers hold exactly one item with version 1 and version group 1, and the envelope `count` is 1

#### Scenario: Specific version by number

- **WHEN** a file has two versions and `GET files/file/{id}?version=1` is called
- **THEN** the file with `version` 1 is returned

#### Scenario: Who may read revisions

- **WHEN** the owner of a file in My documents, a DocSpace admin or a Room admin in their own room, or a User invited to the room with Editing reads the editing revisions
- **THEN** the system answers 200
- **AND** a User invited with Read or Comment, a Guest invited with Read and a User who is not a member are answered with 403

#### Scenario: Who may read the version list

- **WHEN** a DocSpace admin, a Room admin or a User reads the version list of a file in their own My documents
- **THEN** the system answers 200
- **AND** a User reading another account's private file and a DocSpace admin who is not a member reading a file created by someone else in a room are answered with 403

#### Scenario: Anonymous and unknown file

- **WHEN** the version list or the revisions are read without authentication
- **THEN** the system answers 403
- **AND** the revisions of a file that does not exist are answered with 404 ("The required file was not found"), the version list of one with 403

#### Scenario: Archived room keeps revisions readable

- **WHEN** the owner reads the revisions of a file in a room they archived
- **THEN** the system answers 200

#### Scenario: Viewer cannot open an old version

- **WHEN** a User invited with Read opens the current version of a file in view mode and then the same file with `version=1`
- **THEN** the current version opens and the explicit version is refused with 403

### Requirement: What the audit trail records

The system SHALL record every `MessageAction` with a value of 2000 or more as an audit event and every
action below 2000 as a login event. An audit event SHALL carry the event id, the account (`user`,
`userId`), the readable action text and its `actionId`, the IP address with any port removed, the
country and city resolved from the IP (empty when unknown), browser, platform, page, the mapped
`actionType`, `product`, `location`, the `target` items, the `entries` types, the `context` and the
date in the portal time zone. An event performed by the shared guest account SHALL be named
"Anonymous", by the system account "System Account", and an account without a name "Unknown Account".

#### Scenario: Audit event shape

- **WHEN** the owner sends password-change instructions to a member and reads the audit events
- **THEN** an event appears with a non-zero id, a user id and name, an action text and `actionId`, a non-null `ip`, a date and a non-null `context`

### Requirement: Location (`context`) of an audit event

The system SHALL set the `context` of an audit event from the module of its action — Files, Folders,
Rooms, Settings, Contacts, AI agents — and, for Documents events that carry room information, SHALL
replace it with `Rooms: <room title>` (for `RoomRenamed`, the title before the rename), `AI agents:
<agent title>` for agent entries, and the root folder title (My documents or Trash) for entries outside
rooms. No product or "General" prefix is written.

#### Scenario: Action inside a room

- **WHEN** a file is created in a room and the audit trail is read
- **THEN** the event's `context` is `Rooms: <room title>`

#### Scenario: Action in a personal section

- **WHEN** a file is created in My documents
- **THEN** the event's `context` is the title of the My documents root

### Requirement: Reading the audit trail

The system SHALL expose the audit trail to callers with the portal-settings right (the DocSpace owner
and DocSpace admins) through `GET /api/2.0/security/audit/events/last` (the 20 most recent events
within the audit lifetime, action texts shortened to 50 characters), `GET …/events/filter` (filters
`userId`, `moduleType`, `actionType`, `action`, `entryType`, `target`, `from`, `to` as UTC instants;
`count` 1–100, default 100; `startIndex` applied after the filters), `GET …/types` (the names accepted
by the filters) and `GET …/mappers` (products → modules → actions, narrowed by `productType` and
`moduleType`). A non-default `action` matches that action only (with `target` exactly) and overrides
`moduleType` and `actionType`; `target` narrows only together with `action` or `entryType`. On a
portal without the audit option of its pricing plan the filter endpoint SHALL ignore its filters and
answer like `events/last`; on a SaaS portal whose login history section is hidden the event endpoints
SHALL answer 402.

#### Scenario: Owner and DocSpace admin read events

- **WHEN** the owner or a DocSpace admin reads `events/last` or `events/filter`
- **THEN** the system answers 200 with the events

#### Scenario: Other members are refused

- **WHEN** a Room admin, a User or a Guest reads `events/last`, `events/filter`, `types` or `mappers`
- **THEN** the system answers 403 with "Access denied"

#### Scenario: Anonymous caller

- **WHEN** any audit endpoint is called without authentication
- **THEN** the system answers 401

#### Scenario: Types and mappers catalogues

- **WHEN** the owner reads `audit/types` and `audit/mappers`
- **THEN** `actions` contains `FileCreated`, `productTypes` contains `Documents`, and the mappers list is not empty

### Requirement: Audit trail and login history reports

The system SHALL build an audit trail report through `POST /api/2.0/security/audit/events/report` and
a login history report through `POST /api/2.0/security/audit/login/report`, each answering 200 with
the queued task; `GET` on the same path returns the caller's own task of that kind, `DELETE` cancels
it. `format` is `Xlsx` (default) or `Csv`; `from` / `to` are UTC instants. The period SHALL be
resolved against the matching lifetime: a `from` older than the lifetime window is moved up to its
start, a `to` in the future is moved back to now, and a period that ends before it starts after that
resolution SHALL be refused with 400. The file SHALL be saved in the caller's My documents; the XLSX
report keeps at most 200,000 events, the CSV report every event. Finishing the report SHALL write an
`AuditTrailReportDownloaded` or `LoginHistoryReportDownloaded` audit event. Both reports require the
portal-settings right and the audit option of the pricing plan (402 otherwise on SaaS).

#### Scenario: Owner and DocSpace admin start a report

- **WHEN** the owner or a DocSpace admin starts the audit trail report or the login history report, with no period, with the last seven days, or with a `from` ten years ago
- **THEN** the system answers 200 with a task whose `id` is set

#### Scenario: Invalid period

- **WHEN** a report is started with `from` yesterday and `to` two days ago, or with `to` ten years ago
- **THEN** the system answers 400

#### Scenario: Other members and anonymous

- **WHEN** a Room admin, a User or a Guest starts either report
- **THEN** the system answers 403 with "Access denied"
- **AND** an unauthenticated caller is answered with 401

### Requirement: Retention settings of the two logs

The system SHALL keep the lifetime of the login history and of the audit trail in days, readable
through `GET /api/2.0/security/audit/settings/lifetime` (`loginHistoryLifeTime`, `auditTrailLifeTime`,
`lastModified`) and replaceable through `POST` on the same path with both values inside `settings`.
The default of each is 180 days and the accepted range is 1–180; a value outside it SHALL be refused
with 400 without saving either value. Saving SHALL write an `AuditSettingsUpdated` audit event. The
lifetimes bound `events/last` and the default period of the reports. The endpoints remain available
although the settings block was removed from the portal UI in v2.6.

#### Scenario: Read and update

- **WHEN** the owner or a DocSpace admin reads the settings and then posts `loginHistoryLifeTime: 180, auditTrailLifeTime: 180`
- **THEN** the read answers 200 with both values greater than 0 and a `lastModified` field, and the update answers 200 echoing 180 and 180

#### Scenario: Other members and anonymous

- **WHEN** a Room admin, a User or a Guest reads or updates the settings
- **THEN** the system answers 403 with "Access denied"
- **AND** an unauthenticated caller is answered with 401

### Requirement: Login actions that are recorded

The system SHALL record a login event for every sign-in attempt with the action that names the way in:
`LoginSuccessViaPassword` when the password hash was sent from the login page,
`LoginSuccessViaApi` when a plain password was sent, `LoginSuccessViaApiSocialAccount` with the
provider name in the description when a social account was used, `AuthLinkActivated` (initiator = the
e-mail, description = the key) when a confirmation link completed the sign-in, `LoginSuccessViaOAuth`
when the identity service signed the user in, and the matching `LoginFail*` action when the attempt
failed. A login event SHALL carry the account, the login, the action text, the IP with its port
removed, the country and city resolved from the IP (empty when unknown), browser, platform, page and
the date in the portal time zone. Logging out of a session records `Logout`; closing connections
records `UserLogoutActiveConnection`, `UserLogoutActiveConnections` or
`UserLogoutActiveConnectionsForUser` as an audit event.

#### Scenario: Successful password sign-in appears in the history

- **WHEN** the owner signs in and reads `audit/login/filter` with `action=LoginSuccessViaPassword`
- **THEN** the list is not empty

#### Scenario: Login event shape

- **WHEN** the owner reads `audit/login/last`
- **THEN** each event has a positive id, a user id and name, an action text and `actionId`, a non-null `ip` and a date

### Requirement: Reading the login history

The system SHALL expose the login history to callers with the portal-settings right through
`GET /api/2.0/security/audit/login/last` (the 20 most recent events, action text shortened) and
`GET …/login/filter` (`userId`, `action`, `from`, `to`, `count` 1–100 default 100, `startIndex`).
Events of every user type SHALL be visible. Without the audit option the filter endpoint answers like
`login/last`; on a SaaS portal whose section is hidden, 402.

#### Scenario: Owner and DocSpace admin see every member's sign-ins

- **WHEN** a DocSpace admin, a Room admin and a User sign in and the owner or a DocSpace admin reads `login/last`
- **THEN** the events of all three accounts are listed

#### Scenario: Filter by user and page size

- **WHEN** the owner reads `login/filter` with `userId` of a member, with `count=1`, or with both after another member signed in later
- **THEN** only that member's events come back, exactly one event comes back, and with both parameters the single event belongs to the named member

#### Scenario: Other members and anonymous

- **WHEN** a Room admin, a User or a Guest reads `login/last` or `login/filter`
- **THEN** the system answers 403 with "Access denied"
- **AND** an unauthenticated caller is answered with 401

### Requirement: Listing active connections

The system SHALL list the calling account's own active connections through
`GET /api/2.0/security/activeconnections`: one item per successful sign-in of the last year that is
still active, newest first, each with `id`, `tenantId`, `userId`, `browser`, `platform`, `page`,
`ip`, `country`, `city`, `mobile` and `date`. `loginEvent` names the connection the request was made
with (from the authentication cookie; 0 for a bearer-token request), and that connection is moved to
the top with its browser, platform, IP and location refreshed from the request. An account with no
stored connection and no cookie gets a single item describing the current request. Any signed-in user
of any type may call it; it never shows another account's connections.

#### Scenario: Every user type lists own connections

- **WHEN** the owner, a DocSpace admin, a Room admin, a User or a Guest calls the endpoint
- **THEN** the system answers 200 with a non-empty `items` list whose first item has an id, a positive `tenantId`, a non-default date and, when present, non-empty browser, platform and page

#### Scenario: DocSpace admin does not see the owner

- **WHEN** a DocSpace admin lists active connections
- **THEN** no item carries the owner's user id

#### Scenario: Anonymous caller

- **WHEN** the endpoint is called without authentication
- **THEN** the system answers 401

### Requirement: Ending one connection

`PUT /api/2.0/security/activeconnections/logout/{loginEventId}` SHALL close the connection behind the
login event: it is marked inactive, the token and cookie tied to it stop working and the session's
sockets are disconnected. A user may close their own connections; closing somebody else's requires the
DocSpace owner or a DocSpace admin. The answer is `true` when an active connection was closed and
`false` when no active connection with that id exists.

#### Scenario: Own connection

- **WHEN** a DocSpace admin, a Room admin, a Guest or a User closes the connection their own request was made with
- **THEN** the system answers 200 with `true`, and the User's next request with the same token is answered with 401

#### Scenario: Administrator closes another account's connection

- **WHEN** the owner closes a User's connection, or a DocSpace admin closes the owner's connection
- **THEN** the system answers 200 with `true`

#### Scenario: Member cannot close the owner's connection

- **WHEN** a Room admin, a User or a Guest closes the owner's connection
- **THEN** the system answers 403 with "Access denied"
- **AND** an unauthenticated caller is answered with 401

### Requirement: Ending every connection except the current one

`PUT /api/2.0/security/activeconnections/logoutallexceptthis` SHALL close every other active
connection of the calling account, answer 200 with the caller's display name and write a
`UserLogoutActiveConnections` audit event. The connection to keep is the one behind the
authentication cookie; a bearer-token request has none and then every connection is closed. Any
signed-in user may call it.

#### Scenario: Other session is invalidated

- **WHEN** a User holds two tokens and calls the endpoint with the second one
- **THEN** the system answers 200 with a non-empty name and `count` 1, and the first token is answered with 401 afterwards

#### Scenario: Every user type may call it

- **WHEN** the owner, a DocSpace admin, a Room admin, a User or a Guest calls the endpoint
- **THEN** the system answers 200 with a non-empty name
- **AND** an unauthenticated caller is answered with 401

### Requirement: Ending every connection of an account

`PUT /api/2.0/security/activeconnections/logoutall/{userId}` SHALL close every active connection of
the named account, reset its cookies and answer 200 without content. A user may name their own id;
naming another account requires the DocSpace owner or a DocSpace admin.

#### Scenario: Owner and DocSpace admin log a user out everywhere

- **WHEN** the owner or a DocSpace admin calls the endpoint with a User's id
- **THEN** the system answers 200, and the User's existing token is answered with 401 afterwards

#### Scenario: Member cannot log out the owner

- **WHEN** a Room admin, a User or a Guest calls the endpoint with the owner's id
- **THEN** the system answers 403 with "Access denied"
- **AND** an unauthenticated caller is answered with 401

### Requirement: Logging out everywhere with a password reset

`PUT /api/2.0/security/activeconnections/logoutallchangepassword` SHALL close every connection of the
calling account, record `UserSentPasswordChangeInstructions` and answer 200 with the confirmation URL
the caller has to open to set a new password (`/confirm/PasswordChange` with `type=PasswordChange`,
`key` and `uid`). The password itself is not changed. Any signed-in user may call it for their own
account.

#### Scenario: Owner logs out and gets the link

- **WHEN** the owner calls the endpoint
- **THEN** the system answers 200 with a URL containing `/confirm/PasswordChange`, `type=PasswordChange`, `key=` and `uid=`, and the owner's token is answered with 401 afterwards

#### Scenario: Every user type gets the link

- **WHEN** a DocSpace admin, a Room admin, a User or a Guest calls the endpoint
- **THEN** the system answers 200 with such a URL
- **AND** an unauthenticated caller is answered with 401

### Requirement: Forwarding audit and login events to syslog

Before an event is stored, the system SHALL hand every audit and login event to the `ASC.Audit`
logger, which the NLog configuration forwards to an external syslog collector as an RFC 5424 message
whose MSG part is a JSON object with `date`, `eventType` (`login` for actions below 2000, `audit`
otherwise), `action`, `actionId`, `tenantId`, `userId`, `initiator`, `ip`, `page`, `userAgent`,
`target`, `description` (the serialized description list), `instanceId`, `applicationContext` and
`traceId`. Values SHALL be truncated to 50 (ip), 200 (initiator), 256 (target), 512 (user agent, page)
and 2048 (description) characters with a `...` marker, and control characters and `|` replaced by a
space. Nothing is sent unless the `ASC_SYSLOG_SERVER` environment variable is set; the default
transport is UDP port 514, TCP with TLS being a configuration change in `nlog.config`.

#### Scenario: Collector configured

- **WHEN** `ASC_SYSLOG_SERVER` names a collector and a member signs in or renames a file
- **THEN** a syslog message with `eventType` `login` and one with `eventType` `audit` reach the collector with the fields above

#### Scenario: Collector not configured

- **WHEN** `ASC_SYSLOG_SERVER` is empty
- **THEN** no syslog message is sent and the events are stored as usual

### Requirement: Suspicious-login notification

After every successful sign-in that set the authentication cookie, the system SHALL compare the new
login event with the account's previous successful sign-ins (the last 30) and score three signs: a
new device (the browser + platform pair, version numbers removed, differs from every previous one),
a new country (the IP resolves to a country that none of the previous IPs resolved to, counted only
when at least one previous country resolved), and failed attempts (at least 3 failed sign-ins for the
account or its e-mail in the last 24 hours since the previous success). When at least two of the three
signs hold, the system SHALL write a `SuspiciousLoginDetected` login event (IP, browser, platform and
the reasons) and send the account the "Suspicious login" e-mail with the IP, device, location and
browser. The check is skipped when the account has no previous successful sign-in or the login event
is older than 5 minutes. Thresholds come from `web:suspiciousLogin:*` (`historyLimit`,
`failThreshold`, `suspiciousScore`, `failWindowHours`, `freshLoginWindowMinutes`).

#### Scenario: New device and new country

- **WHEN** a member who has signed in before signs in from a browser/OS pair and a country never seen in their history
- **THEN** a `SuspiciousLoginDetected` event is recorded and the member receives the e-mail

#### Scenario: Only one sign

- **WHEN** a member signs in from a new device but from a known country with no recent failures
- **THEN** nothing is recorded and no e-mail is sent

#### Scenario: First sign-in

- **WHEN** an account signs in for the first time
- **THEN** no check is made
