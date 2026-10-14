# file-sharing Specification

## Purpose

Defines how a member shares files and folders from My documents with other accounts and groups without
creating a room, which access levels such a share carries, how the server picks one access level when a
person holds several (a personal share, a group share, a link), and the sections that result from sharing:
Shared with me, Recent and Favorites. It also covers the sharing-related settings (the per-account default
access rights and the portal-wide external sharing switches) and the removal of a share, both by the person
who granted it and by the person who received it.

Sources: the product release notes (v2.5 file sharing from My documents, v3.1 PDF forms shared "to fill
out", v3.5 folder sharing without rooms / Shared with me / Recent / Favorites, v3.6 access priority and the
Shared by column, v4.0 "Remove from list"), checked against the integration tests in
`products/ASC.Files/Tests` and the OpenAPI document `sdk/docspace-api-spec/docspace-backend.yaml`, with the
product code (`FileSecurity`, `FileSharing`, `EntryManager`, `FileMarker`, `TagDao`) consulted only where tests
and document leave a point open. Where the release notes and the server disagree, this specification follows
the server; the differences are listed in `verification.md` next to this file. External (shared) links — where
a link may be created, its settings and limits — are specified in `shared-links`; which user types may share
with whom at all is specified in `user-type-permissions`. Both are referenced here, not restated.

Operations referred to below: `setFileSecurityInfo` (`PUT /api/2.0/files/file/{id}/share`),
`setFolderSecurityInfo` (`PUT /api/2.0/files/folder/{id}/share`), `setSecurityInfo` (`PUT /api/2.0/files/share`),
`getFileSecurityInfo` (`GET /api/2.0/files/file/{id}/share`), `getFolderSecurityInfo`
(`GET /api/2.0/files/folder/{id}/share`), `getSecurityInfo` (`POST /api/2.0/files/share`),
`removeSecurityInfo` (`DELETE /api/2.0/files/share`), `getSharedUsers` (`GET /api/2.0/files/file/{fileId}/sharedusers`),
`getFolderByFolderId` (`GET /api/2.0/files/{folderId}`), `getRootFolders` (`GET /api/2.0/files/@root`),
`getRecentFolder` (`GET /api/2.0/files/recent`), `addFileToRecent` (`POST /api/2.0/files/file/{fileId}/recent`),
`deleteRecent` (`DELETE /api/2.0/files/recent`), `getFavoritesFolder` (`GET /api/2.0/files/@favorites`),
`addFavorites` / `deleteFavoritesFromBody` (`POST` / `DELETE /api/2.0/files/favorites`), `toggleFileFavorite`
(`GET /api/2.0/files/favorites/{fileId}`), `changeDefaultAccessRights` (`PUT /api/2.0/files/settings/dafaultaccessrights`),
`externalShare` (`PUT /api/2.0/files/settings/external`), `externalShareSocialMedia`
(`PUT /api/2.0/files/settings/externalsocialmedia`), `changeExternalSharingSettings`
(`PUT /api/2.0/files/settings/externalsharingsettings`), `getExternalShareData` (`GET /api/2.0/files/share/{key}`).

## Requirements

### Requirement: Sharing a file or folder from My documents with accounts and groups

The system SHALL let the creator of a file or folder in My documents grant access to it to individual portal
accounts and to groups through `setFileSecurityInfo`, `setFolderSecurityInfo` or the batch `setSecurityInfo`,
without creating a room. A share on a folder SHALL cover the folder and everything inside it. For an entry in
My documents the access levels accepted for an account or a group SHALL be Full access (`ReadWrite`),
Editing, Form filling, Review, Comment and Read; `None` withdraws the access. The levels reserved for room
membership (Room manager, Content creator) and the pseudo-levels `Varies` and `Restrict` SHALL NOT be
accepted on a share of a file or folder outside a room. The response SHALL list the resulting rights of the
subjects that were processed.

#### Scenario: File shared with two accounts at different levels

- **WHEN** the owner shares a file from My documents with one account at Read and another at Editing
- **THEN** `getFileSecurityInfo` lists both accounts with those levels (subject type `User`)
- **AND** the first account reads the file with access Read (`security.edit` false) and the second with access Editing (`security.edit` true)

#### Scenario: Folder shared with a group

- **WHEN** the owner shares a folder from My documents with a group at Editing
- **THEN** `getFolderSecurityInfo` lists the group (subject type `Group`, access Editing)
- **AND** every member of the group reads the folder and the files inside it with access Editing

#### Scenario: File shared with a group at Comment

- **WHEN** the owner shares a file with a group at Comment
- **THEN** every member of the group reads the file with access Comment: `security.comment` true, `security.edit` false

#### Scenario: Share withdrawn with None

- **WHEN** the owner sets the access of a shared account to `None`
- **THEN** the account disappears from the sharing rights of the entry
- **AND** the account can no longer read the entry (403)

#### Scenario: Empty share list

- **WHEN** a share request carries no subjects, or an element whose access is an empty string
- **THEN** the request succeeds (200) and answers with an empty list; nothing changes

#### Scenario: Sharing message too long

- **WHEN** a share request asks to notify the recipients and carries a sharing message longer than the field allows
- **THEN** the request is rejected (400)

### Requirement: Who may share and with whom

Only a caller who may change the access of an entry SHALL be able to share it: its creator, a portal
administrator, or — inside a room — a member with the right to manage it. A member who merely holds a share
on the entry, and a Guest in every case, SHALL be refused (403), even when the request would change nothing.
Which user types may share with which recipients is specified in `user-type-permissions`; the server applies
it by silently dropping a recipient the caller may not share with, so the answer SHALL be compared with the
request. A Room admin or a User MAY share a file with Guests they themselves invited, and NOT with Guests
that belong to another member.

#### Scenario: Shared account tries to re-share

- **WHEN** an account that holds a Read share on someone else's file calls `setFileSecurityInfo` or `setSecurityInfo` on it
- **THEN** the request is refused (403) and the existing rights are unchanged

#### Scenario: Guest tries to re-share

- **WHEN** a Guest with a share on a file calls `setFileSecurityInfo` or `setSecurityInfo` on it
- **THEN** the request is refused (403); the guest's own Read share stays and no other subject is added

#### Scenario: Guest of another member is dropped

- **WHEN** a Room admin or a User shares a file from their My documents with a Guest invited by somebody else
- **THEN** the request succeeds but the guest does not appear in the sharing rights of the file

#### Scenario: Own guest and ordinary members are accepted

- **WHEN** a Room admin shares a file from their My documents with a Guest they invited, or a User shares with a DocSpace admin, a Room admin or another User
- **THEN** the recipient appears in the sharing rights with the requested level

### Requirement: Sharing state of an entry

An entry SHALL report three flags: `sharedForUser` when it is shared with at least one account or group,
`shared` when it has at least one external link, and `sharedExternal` when at least one of those links is
public (not restricted to DocSpace users). A file inside a folder that has a link SHALL report `parentShared`.

#### Scenario: Entry shared with an account only

- **WHEN** a file or folder in My documents is shared with an account and has no link
- **THEN** `sharedForUser` is true, `shared` and `sharedExternal` are false

#### Scenario: Entry with an internal link only

- **WHEN** the only link of a file or folder is restricted to DocSpace users
- **THEN** `shared` is true and `sharedForUser` and `sharedExternal` are false

### Requirement: Reading the sharing rights of an entry

`getFileSecurityInfo`, `getFolderSecurityInfo` and the batch `getSecurityInfo` SHALL return one record per
subject that has access to the entry. The record of the entry's owner SHALL carry `isOwner` true,
`canEditAccess` false and `canRevoke` false; the record of an account the entry was shared with SHALL carry
`isOwner` false and `canRevoke` false. An unshared entry SHALL have exactly one record, the owner's. The
caller SHALL need read access to the entry; an unauthenticated caller SHALL get 401, a caller without access
403, and a file that does not exist or was permanently deleted 404. The batch operation SHALL skip
identifiers that match nothing and answer an empty list for an empty request.

#### Scenario: Owner record

- **WHEN** the sharing rights of a file nobody else has access to are read
- **THEN** exactly one record is returned, with `isOwner` true, `canEditAccess` false and subject type `User`

#### Scenario: Shared account reads the rights

- **WHEN** an account the file was shared with calls `getFileSecurityInfo` or `getSecurityInfo` for it
- **THEN** the records are returned (200)

#### Scenario: Guest shared directly on a personal file

- **WHEN** a Guest the file was shared with calls `getFileSecurityInfo`
- **THEN** the request is refused (403)
- **AND** the same Guest calling the batch `getSecurityInfo` for the same file gets the records (200)

#### Scenario: Guest reads folder sharing rights

- **WHEN** a Guest calls `getFolderSecurityInfo`, with or without access to the folder
- **THEN** the request is refused (403)

#### Scenario: Shared file moved to the trash

- **WHEN** the owner moves a shared file to the trash
- **THEN** the shared account's record is gone from the sharing rights and that account can no longer read the file (403)

#### Scenario: Unreadable entry in a batch

- **WHEN** `getSecurityInfo` lists a file the caller cannot read
- **THEN** the whole call is refused (403), for a User and for a Guest alike

### Requirement: Access priority when several rights apply

When an account holds more than one right on an entry, the system SHALL apply them in this order of
strength: a personal share first, then a share on a group the account belongs to (including Everyone and the
administrators' group), then the link the account is currently using, then links the account opened earlier.
A right granted on the entry itself SHALL beat a right inherited from a parent folder, and a right on a
closer parent SHALL beat one on a farther parent. A visitor without an account SHALL get exactly the rights
of the link they came in through. Inside rooms the same rule applies: a personal right in the room beats the
rights of a link.

#### Scenario: Personal Editing beats a Read link

- **WHEN** a file is shared with an account at Editing and the account opens it through a Read link
- **THEN** the file opens with access Editing (`security.edit` true)

#### Scenario: Personal Read beats a group Comment and an Editing link

- **WHEN** a file is shared with a group at Comment and the member of that group reads it
- **THEN** the member's access is Comment
- **WHEN** the same member is then shared the file personally at Read
- **THEN** the member's access is Read, also when they open the file through an Editing link

#### Scenario: Anonymous visitor gets the link's rights

- **WHEN** a visitor without an account opens the same file through the Editing link
- **THEN** the file opens with access Editing

#### Scenario: Everyone share does not reach an anonymous visitor

- **WHEN** a file is shared with Everyone at Full access and a visitor without an account opens it through a Read link
- **THEN** the file opens with access Read (`security.edit` false)

#### Scenario: Share on the file beats the share on its folder

- **WHEN** a folder is shared with an account at Full access and a file inside it is shared with the same account at Read
- **THEN** the account reads the file with access Read (`security.edit` false)

#### Scenario: Share on a sub-folder beats the share on its parent

- **WHEN** a folder is shared with an account at Read and a sub-folder inside it at Full access
- **THEN** the account reads the sub-folder and the files in it with access Full access (`security.edit` true)
- **AND** the sharing rights of a file in the sub-folder, read by that account, show the Full access record

#### Scenario: Folder share beats a link on a file inside

- **WHEN** a folder is shared with an account at Full access and the account opens a file inside it through a Read link on the file
- **THEN** the file opens with access Full access

#### Scenario: Share given on a file created inside a shared folder

- **WHEN** an account with Full access on a shared folder creates a file in it and the owner then shares that file with the account at Read
- **THEN** the sharing rights of the file list the account at Read

### Requirement: The last opened link is remembered for Shared with me

When a signed-in account opens an entry it does not own through a link, and the entry is recorded as
recent, the system SHALL remember that link for the account and SHALL use the link's rights for the entry in
Shared with me and when the entry is later read by its identifier without the link. Opening the same entry
through another link SHALL replace the remembered link, so only the last opened link counts. Deleting a link
the account never opened SHALL NOT remove the entry from the account's Shared with me.

#### Scenario: Opening a file through three links in turn

- **WHEN** an account opens a file from someone else's My documents through a Read link, then a Comment link, then an Editing link, recording it as recent each time
- **THEN** after each opening the file read by its identifier, without any link, reports the access of the link opened last

#### Scenario: Unused link deleted

- **WHEN** a file has two Read links, an account opened it through the first and the owner deletes the second
- **THEN** the file stays in the account's Shared with me with access Read

### Requirement: The Shared with me section

The system SHALL expose a Shared with me section to every signed-in account. It SHALL list the files and
folders other members shared with the account from their My documents, and the files and folders the
account reached through a link, each with the access the account holds. A folder reached through a link
SHALL appear in Shared with me for a signed-in account once the link has been resolved with
`getExternalShareData`. Each entry read through a share SHALL carry `sharedBy` (who gave the access) and
`ownedBy` (who owns the place it is shared from) next to its author, and the listing SHALL accept a `sharedBy`
filter that narrows it to what one member handed out. The section SHALL support the ordinary listing filters,
including the PDF and PDF form types. A file reached through a share whose real parent the caller cannot
open SHALL report the Shared with me section as its folder. A file newly shared with an account SHALL count as
new in that account's Shared with me.

#### Scenario: Shared file appears with its access

- **WHEN** a file from the owner's My documents is shared with an account at Read
- **THEN** the account's Shared with me lists the file with access Read

#### Scenario: Folder opened through a link

- **WHEN** a signed-in account resolves a Read link to a folder in someone else's My documents
- **THEN** the account's Shared with me lists the folder with access Read

#### Scenario: Sub-folder of another member's My documents

- **WHEN** a User reads a folder in the owner's My documents that was not shared with them
- **THEN** the request is refused (403)

#### Scenario: New-item count

- **WHEN** two files are shared with an account one after the other
- **THEN** the account's Shared with me reports 1 and then 2 new items

#### Scenario: PDF form filter

- **WHEN** a docx, a plain PDF and a PDF form are shared with an account and the account lists Shared with me with the PDF form filter
- **THEN** only the PDF form is listed; with the PDF filter only the plain PDF is listed

### Requirement: Removing a share and removing an entry from Shared with me

`removeSecurityInfo` SHALL revoke the access of every account and group on the listed files and folders when
the caller may change their access, keeping the owner's record and the external links untouched, and clear
the entries from the caller's own favorites, recent and new marks. An account the entry was shared with
directly, or that reached it through a link, SHALL be able to call the same operation to take the entry out
of its own Shared with me ("Remove from list"); this SHALL NOT change anybody else's rights. An account with
no access to the entry SHALL be refused (403), and an unauthenticated caller 401. The answer SHALL be `true`
in every accepted case, including identifiers that match nothing and an unshared entry.

#### Scenario: Owner revokes every share

- **WHEN** the owner calls `removeSecurityInfo` for a file, a folder, or several of them at once
- **THEN** the shared accounts disappear from the sharing rights and a formerly shared account can no longer read the entry (403)

#### Scenario: Administrators revoke shares

- **WHEN** a DocSpace admin calls `removeSecurityInfo` for the owner's file, or a Room admin for a file in their own room
- **THEN** the call succeeds (200, `true`)

#### Scenario: Recipient removes the entry from their own list

- **WHEN** a Guest, a User or a Room admin the file or folder was shared with at Read calls `removeSecurityInfo` for it
- **THEN** the call succeeds (200, `true`) and the entry is no longer listed in their Shared with me

#### Scenario: Unrelated account

- **WHEN** an account with no share on the owner's file or folder calls `removeSecurityInfo` for it
- **THEN** the request is refused (403) and the entry stays shared with the accounts it was shared with

#### Scenario: Folder reached by link removed and reopened

- **WHEN** an account removes from its list a folder it reached through a link, and later resolves the same link again
- **THEN** after the removal the folder is absent from Shared with me, and after the reopening it is listed again with the link's access

### Requirement: Guests cannot re-share

A Guest SHALL NOT be able to change the sharing of any entry, whatever access level the Guest holds on it
(Full access included), because a Guest has no access to the portal's contacts. The share-writing operations
SHALL refuse a Guest (403), and the users-to-mention list of a file SHALL show a Guest only the owner of the
file.

#### Scenario: Guest lists the users of a shared file

- **WHEN** a Guest with access to a file calls `getSharedUsers`
- **THEN** the list contains the owner of the file and none of the other accounts it is shared with

#### Scenario: Member lists the users of a shared file

- **WHEN** an account with access to a file calls `getSharedUsers`
- **THEN** the list contains the other accounts the file is shared with, without the caller and without accounts that have no access
- **AND** a caller without access is refused (403), an unauthenticated caller 401, and an unknown file is reported as missing (404)

### Requirement: Sharing a PDF form from My documents

A PDF form in My documents SHALL be shareable without a room. Its links SHALL offer Editing, Form filling,
Comment, Read and `None`, and a share with an account or a group SHALL accept Form filling in addition to
the levels available for any other file. A link created on a file in My documents without an explicit access
level SHALL be a Read link that does not expire.

#### Scenario: Link rights offered on a form in My documents

- **WHEN** the owner reads a PDF form in My documents after its primary link was created
- **THEN** `availableShareRights.externalLink` holds exactly Editing, FillForms, Comment, Read and None

#### Scenario: Link rights offered on a form in a Form filling room

- **WHEN** the owner reads a PDF form inside a Form filling room
- **THEN** `availableShareRights.externalLink` holds exactly FillForms and None

#### Scenario: Default link on a file in My documents

- **WHEN** the primary link of a file in My documents is requested without parameters
- **THEN** the link grants Read, has no expiration date, no password, allows downloading and is not restricted to DocSpace users

### Requirement: The Recent section

The system SHALL keep a personal Recent section per account: the files the account opened, newest first,
files only. Files from My documents, from rooms of every type and files reached through a link SHALL all be
recorded, and the listing SHALL accept a folder-type filter that narrows it to My documents, to all rooms or
to one room type. A file SHALL be recorded when it is opened through the file handler and MAY be recorded
explicitly with `addFileToRecent`, which SHALL require read access to the file (403 otherwise, 401 for an
unauthenticated caller, 404 for a file that does not exist) and SHALL be idempotent. `deleteRecent` SHALL
clear the listed files from the caller's own history without checking access, SHALL be idempotent and SHALL
answer no body. The server SHALL keep at most the 1000 most recent marks per account.

#### Scenario: File from My documents added to Recent

- **WHEN** an account adds a file from its My documents, or from a sub-folder of it, to Recent
- **THEN** the Recent listing contains the file exactly once

#### Scenario: File from a room added to Recent

- **WHEN** an account with at least Read access to a file in a room (the room owner, a member with any role, or a guest) adds it to Recent
- **THEN** the operation succeeds and the file is listed in Recent with its origin folder and room

#### Scenario: Another member's private file

- **WHEN** an account adds a file from another member's My documents that was not shared with it to Recent
- **THEN** the request is refused (403)

#### Scenario: Filtering by folder type

- **WHEN** the Recent listing is requested with the My documents folder type, the rooms folder types, one room type, or several types
- **THEN** only the files whose origin matches the requested types are listed

#### Scenario: Deleting from Recent

- **WHEN** an account deletes one or several files from its Recent, or a file that is not in it
- **THEN** the files are no longer listed and the call succeeds either way; an unauthenticated caller is refused (401)

### Requirement: The Favorites section

The system SHALL keep a personal Favorites section per account for files and folders. Marking an entry SHALL
require Read access to it and nothing more (a Guest with Read access to a room file may mark it); an
unauthenticated caller SHALL be refused (401). `addFavorites` and `deleteFavoritesFromBody` SHALL answer
`true`, SHALL be idempotent, and SHALL ignore identifiers that match nothing. Entries the account can no
longer read and entries in the trash SHALL drop out of the listing and out of its `total` although the mark
is not cleared; entries in archived rooms SHALL stay. The listing SHALL accept the item-kind filter
(documents, files only, folders only), the ancestor folder-type filter (My documents, all rooms, one room
type), paging, sorting and a search term; a page size of 0 SHALL be rejected (400).

#### Scenario: Entry marked and listed

- **WHEN** an account adds a file or a folder from My documents, from a sub-folder or from a room to Favorites
- **THEN** the Favorites listing contains it with `isFavorite` true; a file from a room also reports its origin room title

#### Scenario: Favorites are per account

- **WHEN** the owner marks a file and another member lists Favorites
- **THEN** the member's list does not contain the file, and the owner's list does not contain what the member marked

#### Scenario: Lost access

- **WHEN** an account marked a room file and is then removed from the room
- **THEN** the file is no longer listed and `total` equals the number of entries listed

#### Scenario: File moved to the trash

- **WHEN** a favorite file is moved to the trash, or a file already in the trash is marked
- **THEN** the file is not listed in Favorites

#### Scenario: Toggling one file

- **WHEN** `toggleFileFavorite` is called with `true` and then with `false` for a file
- **THEN** the answers are `true` and then `false`

#### Scenario: Removing a mark leaves the source untouched

- **WHEN** a file is removed from Favorites
- **THEN** it is still listed in its own folder

### Requirement: Default access rights offered in the sharing dialog

Each account SHALL have its own list of default access rights, set with `changeDefaultAccessRights` and
reported in the files settings as `defaultSharingAccessRights`. A new list SHALL replace the previous one, an
empty list SHALL reset it to the product default, duplicate values SHALL be accepted, and an unknown value or
a missing body SHALL be rejected (400). Every authenticated role MAY change its own list; an unauthenticated
or terminated caller SHALL be refused (401).

#### Scenario: List replaced

- **WHEN** an account sets Full access, Read and Review and then sets Comment
- **THEN** the setting holds Comment only

#### Scenario: Isolated per account

- **WHEN** the owner sets Read and another member sets Full access
- **THEN** the owner's files settings report Read and not Full access

### Requirement: Portal-wide external sharing switches

The external sharing switches SHALL be portal-wide and SHALL be changed only by the portal owner and
DocSpace admins (403 for a Room admin, a User and a Guest; 401 unauthenticated). Turning external sharing off
SHALL also turn off sharing on social media, and enabling social media sharing SHALL take effect only while
external sharing is on. `changeExternalSharingSettings` SHALL store whether new links are internal by default
and whether the restriction applies to documents, to rooms, and to links that already exist. While the
restriction applies to documents, a link created on a file in My documents SHALL be internal; while it applies
to rooms only, such a link SHALL be public. While existing links are blocked, an anonymous visitor resolving
an existing link SHALL get `ExternalAccessDenied`, and a signed-in account SHALL get `Ok`.

#### Scenario: Non-admin flips the switch

- **WHEN** a Room admin, a User or a Guest calls `externalShare` or `externalShareSocialMedia`
- **THEN** the request is refused (403, "You don't have enough permission to perform the operation")

#### Scenario: External sharing turned off

- **WHEN** an admin turns external sharing off
- **THEN** the files settings report both `externalShare` and `externalShareSocialMedia` as false

#### Scenario: Restriction applied to documents

- **WHEN** the restriction applies to documents and a link is created on a file in My documents
- **THEN** the link is internal (restricted to DocSpace users)

#### Scenario: Existing link blocked

- **WHEN** existing links are blocked and an anonymous visitor resolves a file link created before
- **THEN** the status is `ExternalAccessDenied`; the same link resolved by a signed-in account answers `Ok`
