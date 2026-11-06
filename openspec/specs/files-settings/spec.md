# files-settings Specification

## Purpose

Defines the settings of the Files module (`files/...` switches, trash auto-clearing, the third-party permission,
the Document Server address and the default document templates) and the file, folder, room and sharing operations
that no other capability names: editing sessions and their tracking, the bulk order, saving as PDF, creating HTML
in My documents, the primary and additional external link operations of files and folders, folder paths and
subfolders, the room cover, logo removal, the tag catalogue, group members with access, and unlocking a password
link. The meaning of links is in `shared-links` and `file-sharing`, rooms in `room-types`, general entry operations
in `file-operations`, and encryption in `private-rooms`; here only the contract of these operations is described.

Sources: the integration tests of `products/ASC.Files/Tests`, the contract
`sdk/docspace-api-spec/docspace-backend.yaml` and the product code where the tests stop. The evidence per scenario,
the differences and the suspected defects are in `verification.md` next to this file.

## Requirements

### Requirement: Personal file settings switches

`displayRecent`, `displayFileExtension`, `storeOriginal`, `changeDeleteConfirm`, `hideConfirmCancelOperation`,
`hideConfirmRoomLifetime`, `keepNewFileName` and `setOpenEditorInSameTab` SHALL store the request's `set` for the
calling account alone and answer with the resulting boolean. Every role down to a guest MAY change its own value;
an anonymous or terminated caller gets 401.

#### Scenario: Value set and read back

- **WHEN** a switch is called with `set` true and later false, or twice with the same value
- **THEN** each answer is the sent value, and `getFilesSettings` publishes the same value

#### Scenario: Empty body

- **WHEN** a switch is called with the JSON body `{}`
- **THEN** the answer is 200 with a boolean, and the switches stored and echoed this way treat `set` as false

#### Scenario: Other accounts unaffected

- **WHEN** one account changes a switch
- **THEN** another account's value in `getFilesSettings` stays as it was

#### Scenario: No account

- **WHEN** an anonymous caller, or one whose account was terminated, calls a switch
- **THEN** the answer is 401

### Requirement: Switches for quick actions and room grouping

`showQuickActions` and `setOrganizeRoomsGrouping` (`PUT files/showquickactions`, `PUT files/settings/organizegrouping`)
SHALL store `set` for the calling account and answer with a boolean, for every authenticated role. The portal only
records the preference: `getRoomsFolder` returns the same rooms whichever way the grouping is set.

#### Scenario: Grouping off

- **WHEN** an account calls `setOrganizeRoomsGrouping` with `set` false
- **THEN** the answer is false and the room groups and their rooms are unchanged

### Requirement: Conversion prompt can only be hidden

`hideConfirmConvert` SHALL mark the conversion prompt as hidden for the calling account and always answer true. The
`save` value picks which prompt: the one on saving or the one on opening. The two flags are independent, and this
operation cannot bring a prompt back.

#### Scenario: Both prompts hidden

- **WHEN** the operation is called with `save` true and then with `save` false
- **THEN** `getFilesSettings` reports both `hideConfirmConvertSave` and `hideConfirmConvertOpen` as true

#### Scenario: Without a body

- **WHEN** the operation is called with the JSON body `{}`
- **THEN** the answer is 200 with `response` true

### Requirement: Download archive format

`changeDownloadZip` SHALL store for the calling account whether multi-item downloads are packed as `.tar.gz`
(`set` true) or `.zip` (`set` false). The answer is an object that does not confirm the choice, so the value SHALL be
read from `downloadTarGz` of `getFilesSettings`.

#### Scenario: Format selected

- **WHEN** an account calls `changeDownloadZip` with `set` true
- **THEN** the answer is 200 and `getFilesSettings` reports `downloadTarGz` true for that account only

### Requirement: Compatibility stubs store nothing

`storeForcesave` SHALL always answer false and `updateFileIfExist` SHALL always answer false; neither stores anything
or changes any behaviour. `storeForcesave` takes no body and `updateFileIfExist` ignores its body.

#### Scenario: Forced saves are not kept separately

- **WHEN** `storeForcesave` is called
- **THEN** its answer equals `storeForcesave` in `getFilesSettings`, whatever was called before

#### Scenario: Upload does not update by name

- **WHEN** `updateFileIfExist` is called with `set` true
- **THEN** the answer is false

### Requirement: Trash auto-clearing

`getAutomaticallyCleanUp` and `changeAutomaticallyCleanUp` SHALL read and change the calling account's trash clearing
setting: `set` turns it on or off and `gap` selects the interval. Each field MAY be sent alone. Every role manages its
own setting; an anonymous or terminated caller gets 401.

#### Scenario: Enabled with an interval

- **WHEN** `changeAutomaticallyCleanUp` is called with `set` true and a `gap`
- **THEN** the answer and `getAutomaticallyCleanUp` report `isAutoCleanUp` true and that `gap`, and `getFilesSettings` agrees

#### Scenario: Partial request

- **WHEN** only `set` true, or only a `gap`, or the body `{}` is sent
- **THEN** the answer is 200; with `set` true `isAutoCleanUp` is true and with a `gap` that `gap` is reported

#### Scenario: Setting of another account

- **WHEN** one account enables the clearing
- **THEN** another account's `getAutomaticallyCleanUp` does not change

### Requirement: Permission to connect third-party storages

`changeAccessToThirdparty` SHALL set one portal-wide value, published as `enableThirdParty` by `getFilesSettings`, and
answer with the stored value. Only the owner and a DocSpace administrator MAY change it.

#### Scenario: Other role

- **WHEN** a room administrator, user or guest calls the operation
- **THEN** the answer is 403, and an anonymous, terminated or deleted caller gets 401

#### Scenario: Turned off

- **WHEN** the value is false and `saveThirdParty` is called
- **THEN** the answer is 403 before the credentials are used

### Requirement: Module description

`getFilesModule` SHALL answer every authenticated role with the Documents module's identifier, title and link. An
anonymous or terminated caller gets 401.

#### Scenario: Module read

- **WHEN** a user calls `getFilesModule`
- **THEN** `id`, `title` and `link` are non-empty

### Requirement: Document Server address is readable without signing in

`getDocServiceUrl` SHALL answer any caller, anonymous included, with the Document Server addresses and `isDefault`.
`version` true SHALL add the editor version; otherwise `version` is empty.

#### Scenario: Fresh portal

- **WHEN** `getDocServiceUrl` is called on a portal whose address was never changed
- **THEN** `isDefault` is true and the API address is non-empty

#### Scenario: Version requested

- **WHEN** the query carries `version` true, or false
- **THEN** `version` is a non-empty string, or empty

### Requirement: Document Server address is changed with verification

`checkDocServiceUrl` SHALL store the given addresses, signature settings and SSL verification flag and then verify
them against the Document Server, restoring every previous value if a step fails. It requires the portal settings
permission.

#### Scenario: Other role

- **WHEN** a caller without the permission calls the operation
- **THEN** the answer is 403 "Access denied", and an anonymous or terminated caller gets 401

#### Scenario: Invalid values

- **WHEN** an address cannot be parsed or has a query string, a secret is sent without its header, or an http address is given to a portal served over https
- **THEN** the answer is 400

#### Scenario: Verification fails

- **WHEN** the Document Server fails the verification
- **THEN** the previous settings are restored and the answer is 500

### Requirement: Default templates are read with the settings permission

`getDefaultTemplates` SHALL answer one entry for each extension in the built-in template set, with `selectedFile` null
where no custom template is set. A caller without the portal settings permission gets 403.

#### Scenario: No custom template

- **WHEN** the owner calls `getDefaultTemplates` on a fresh portal
- **THEN** every entry has `selectedFile` null

### Requirement: Default template from an existing file

`setDefaultTemplate` SHALL copy the file in `selectedFile` into the template storage for `fileExtension`, delete the
previous custom template and answer with all templates. It requires the portal settings permission and the right to
copy the file. An extension outside the built-in set SHALL change nothing and still answer 200.

#### Scenario: Refused requests

- **WHEN** `selectedFile` is missing, or the file's extension differs from `fileExtension`
- **THEN** the answer is 400

#### Scenario: File of another person

- **WHEN** a DocSpace administrator selects a file of the owner that they may not copy
- **THEN** the answer is 403

#### Scenario: Unknown file

- **WHEN** `selectedFile` names a file that does not exist
- **THEN** the answer is 404

### Requirement: Default template from an upload

`uploadDefaultTemplate` SHALL store the multipart file as the template of the `FileExtension` query value, delete the
previous custom template and answer with all templates. It requires the portal settings permission. The body is
limited to 100 MB. A PDF template MUST be a fillable form.

#### Scenario: Over the limit

- **WHEN** the file is larger than 100 MB
- **THEN** the answer is 400 with "The maximum file size is exceeded (100 MB)."

#### Scenario: Extension contradicts the declaration

- **WHEN** a file named `.pdf`, `.docx` or `.xlsx` is declared as another extension
- **THEN** the answer is 400

#### Scenario: PDF that is not a form

- **WHEN** a plain PDF is uploaded for the `.pdf` extension
- **THEN** the answer is 403

### Requirement: Default template reset

`resetDefaultTemplate` SHALL drop the custom template of the `fileExtension` in the body, delete its stored copy and
answer with all templates, the entry having `selectedFile` null. It requires the portal settings permission. A reset
with nothing to drop, or for an extension outside the built-in set, SHALL succeed and change nothing.

#### Scenario: Repeated reset

- **WHEN** `resetDefaultTemplate` is called twice for one extension
- **THEN** both answers are 200 with `selectedFile` null for that extension

### Requirement: Editing a file alone

`startEditFile` with `editingAlone` true SHALL claim the file for the caller and answer with the document key. It
requires edit access to the file.

#### Scenario: Claimed

- **WHEN** the owner, or a member invited as room manager, claims a file nobody edits
- **THEN** the answer is a non-empty document key

#### Scenario: Already being edited

- **WHEN** another member claims a file that is already claimed
- **THEN** the answer is 403

#### Scenario: Refused callers

- **WHEN** an anonymous caller claims the file, or a guest or user with read access starts editing, or the file does not exist
- **THEN** the answer is 401, 403 or 404 respectively

### Requirement: Starting a shared editing session

`startEditFile` with `editingAlone` false or absent SHALL build the editor configuration and ask the Document Server
to track the document, answering with the document key. A Document Server that does not accept the request SHALL
produce 403.

#### Scenario: Document Server does not start tracking

- **WHEN** the body is `{}` or `editingAlone` is false and the Document Server does not start tracking
- **THEN** the answer is 403 with "File editing start error"

### Requirement: Tracking an editing session

`trackEditFile` SHALL keep a session alive or, with `isFinish` true, end it for that tab, answering `key` true and an
empty `value`. It requires an editing right on the file and a `docKeyForTrack` of the current revision; the document is
never changed.

#### Scenario: Session refreshed

- **WHEN** the owner or a DocSpace administrator calls the operation with the document key, with or without `tabId` or `isFinish`
- **THEN** the answer is 200 with `key` true

#### Scenario: Refused callers

- **WHEN** an anonymous caller, a guest or user with read access, or any caller with an unknown file calls the operation
- **THEN** the answer is 401, 403 or 404 respectively

### Requirement: Users offered for document protection

`getProtectedFileUsers` SHALL list the people the file is shared with, room access included, groups expanded, without
the caller and deleted accounts, ordered by display name. A guest and an unknown file id get 403.

#### Scenario: Uninvited guest

- **WHEN** a guest without access calls the operation
- **THEN** the answer is 403 "Access denied"

### Requirement: Address of a version's changes

`getEditDiffUrl` SHALL answer with the key, address and file type of a version for a caller who may read the file's
history. `version` 0, the default, means the current version. `previous` is filled only when real editing history is
stored for that version.

#### Scenario: Version without editing history

- **WHEN** a version created by the API, or the first version, is requested
- **THEN** `key` and `url` are non-empty and `previous` is null

#### Scenario: Callers without history access

- **WHEN** a member with read or comment access, a guest, someone without room access, or an anonymous caller asks
- **THEN** the answer is 403, the anonymous caller included

#### Scenario: Unknown file

- **WHEN** the file does not exist
- **THEN** the answer is 404 with "The required file was not found"

### Requirement: Chunked session overwriting a file

`createEditSession` SHALL open a chunked upload session for an existing file for a caller allowed to edit it, answering
`success` true with the session `id`, `created`, `expired`, `location` and `bytesTotal`. `fileSize` MAY be omitted.

#### Scenario: Session created

- **WHEN** the owner sends a `fileSize`
- **THEN** `bytesTotal` equals it and `expired` is in the future

#### Scenario: Refused callers

- **WHEN** a member with read access, a guest, an anonymous caller, or any caller with an unknown file calls the operation
- **THEN** the answer is 403, 403, 401 or 404 respectively

### Requirement: Bulk order of entries

`setFilesOrder` SHALL set the `order` of each item (`entryId`, `entryType`) and answer with the entries, for a caller
who may administer each entry's room. `order` MUST be at least 1, and equal values are accepted. An empty `items` list
SHALL answer with an empty list.

#### Scenario: Files and folders ordered

- **WHEN** a room manager sends files and folders of an indexed room
- **THEN** the answer lists every entry with its `id`, `title` and `fileEntryType`, and the room's content follows the order

#### Scenario: Invalid order

- **WHEN** `order` is 0
- **THEN** the answer is 400

#### Scenario: Unknown entry

- **WHEN** an `entryId` does not exist or is sent with the wrong `entryType`
- **THEN** the answer is 404

#### Scenario: Refused callers

- **WHEN** a guest or user with read access, or an anonymous caller, calls the operation
- **THEN** the answer is 403 or 401

### Requirement: Saving a file as PDF

`saveFileAsPdf` SHALL convert the file and store a new PDF named by `title` in `folderId`, answering with that file.
The caller must be able to read the source and create in the destination. A refused request SHALL create nothing.

#### Scenario: Saved

- **WHEN** a room manager saves a `.docx` as PDF into the room
- **THEN** the answer's `title` ends with `.pdf`

#### Scenario: Missing source or destination

- **WHEN** the source file or the destination folder does not exist
- **THEN** the answer is 404

#### Scenario: Refused callers

- **WHEN** the caller may not create in the destination, cannot reach the source, the destination is an archived room, or the source is in a room of which the caller is no member
- **THEN** the answer is 403 and nothing is created, and an anonymous caller gets 401

### Requirement: HTML file in My documents

`createHtmlFileInMyDocuments` SHALL create an `.html` file from `title` and `content` in the caller's My documents.
`createNewIfExist` true SHALL use the file that already has the title; false or absent SHALL create another file.

#### Scenario: File created

- **WHEN** a non-guest role sends `title` and `content`
- **THEN** the answer has the title with `.html` and `fileExst` `.html`

#### Scenario: Existing title

- **WHEN** the same title is sent again with `createNewIfExist` true, or false or absent
- **THEN** the answer's `id` is the first file's, or a different one

#### Scenario: Refused requests

- **WHEN** `content` is missing, or the caller is a guest, or the caller is anonymous
- **THEN** the answer is 400, 404 or 401 respectively

### Requirement: Encryption data of a file

`getEncryptionInfo` SHALL answer with the calling account's key pairs in `userKeys` and the file keys issued to it for
this file in `fileKeys`. It requires read access to the file.

#### Scenario: Key issued

- **WHEN** a member with a key pair reads a file whose key was issued with `setEncryptionInfo`
- **THEN** `userKeys` holds their key pair and `fileKeys` names the file, the user and the public key

### Requirement: Primary external link of a file

`getFilePrimaryExternalLink` and `createFilePrimaryExternalLink` SHALL answer with the file's primary link, creating it
on the first call if the caller may share the file. Repeated calls SHALL return the same link.

#### Scenario: New file

- **WHEN** the owner calls `getFilePrimaryExternalLink` for a new file in My documents
- **THEN** `subjectType` is the primary external link, `access` Read, and `sharedTo` has `primary` true and a `shareLink`

#### Scenario: Repeated read

- **WHEN** the operation is called twice
- **THEN** both answers carry the same link id

#### Scenario: Refused callers

- **WHEN** an anonymous caller, or a user without access to the file, calls the operation
- **THEN** the answer is 401, or 403

### Requirement: Primary external link of a folder or room

`getFolderPrimaryExternalLink` and `createFolderPrimaryExternalLink` SHALL answer with the primary link, creating it on
the first call for a caller who may manage the links. The parameters apply only when the link is created. `count` 0
SHALL be rejected with 400, and `startIndex` has no effect.

#### Scenario: Called twice

- **WHEN** `createFolderPrimaryExternalLink` is called twice with `access` Read
- **THEN** both answers carry the same link id

#### Scenario: Access granting nothing

- **WHEN** `createFolderPrimaryExternalLink` is sent no `access` on a room without a link
- **THEN** the answer is 200 with no `response` and `count` 0, and no link exists

#### Scenario: Refused callers

- **WHEN** an anonymous caller, a member with read or content-creator access, someone without room access, or any caller on an archived room asks
- **THEN** the answer is 401 for the anonymous caller and 403 for the others

#### Scenario: Unknown folder

- **WHEN** the folder id is unknown, zero or negative
- **THEN** the answer is 404

### Requirement: Revoked primary link stays revoked on read

After a primary link of a folder or file was revoked, except in the rooms that replace it, `getFolderPrimaryExternalLink`
and `getFilePrimaryExternalLink` SHALL answer 404 instead of creating a new link. `setFileExternalLink` or
`setFolderPrimaryExternalLink` with `primary` true SHALL create one again and clear this state.

#### Scenario: Read after revocation

- **WHEN** a folder's primary link is revoked with `access` None and then read
- **THEN** the answer is 404

### Requirement: Setting an external link of a file

`setFileExternalLink` SHALL create the link when `linkId` is unused or empty, including with exactly the given id,
rewrite it when `linkId` exists, and revoke it when `access` is None. It requires the right to share the file.

#### Scenario: Created

- **WHEN** a file's owner of any non-guest role sends `access` Read with `title`, `denyDownload`, `internal`, a future `expirationDate` or a `password`
- **THEN** the answer reflects those values

#### Scenario: Access not available for links

- **WHEN** `access` is ReadWrite
- **THEN** the answer is 403

#### Scenario: Refused callers

- **WHEN** an anonymous caller, a user or DocSpace administrator without access to a private file, or any caller with an unknown file asks
- **THEN** the answer is 401, 403 or 404 respectively

### Requirement: Setting an external link of a folder or room

`setFolderPrimaryExternalLink` SHALL create, rewrite or revoke a link as `linkId` and `access` say, keeping the id when
it rewrites. An `expirationDate` in the past SHALL be ignored. Revoking SHALL answer 200 with no `response`.

#### Scenario: Fields updated

- **WHEN** a room manager updates `access`, `title`, `password`, `denyDownload`, `internal` or `expirationDate` of a link
- **THEN** the answer and later reads show the new values with the same link id

#### Scenario: Past expiration

- **WHEN** `expirationDate` lies in the past
- **THEN** the link has no expiration date and `isExpired` is false

#### Scenario: Refused requests

- **WHEN** `access` is ReadWrite, or the caller has content-creator or read access or is a guest or a room administrator outside the room
- **THEN** the answer is 403; an anonymous caller gets 401 and an unknown folder 404

### Requirement: Listing the links of a file

`getFileLinks` SHALL list the file's external links, paged by `startIndex` and `count`, with an empty list for a file
never shared by link. A caller without access gets 403 and an anonymous caller 401.

#### Scenario: New file

- **WHEN** the owner, or a DocSpace administrator, room administrator or user who created the file, lists its links
- **THEN** `response` is empty and `count` 0

#### Scenario: After a link is created

- **WHEN** a link was created with `setFileExternalLink`
- **THEN** the list contains it with its structure

### Requirement: Listing the links of a folder or room

`getFolderLinks` SHALL list the links for a caller who may manage them, and an empty list for a member who may only read
the folder. Someone without room access gets 403, an anonymous caller 401 and an unknown, zero or negative id 404.

#### Scenario: Read-only member

- **WHEN** a member with read access lists the links of a room that has a primary link
- **THEN** `response` is empty

#### Scenario: Revoked link

- **WHEN** a link is revoked with `setFolderPrimaryExternalLink`
- **THEN** it no longer appears in the list

### Requirement: Folder path

`getFolderPath` SHALL answer with the chain of folders from the section root down to the given folder, for a caller who
may read it, including virtual sections, rooms and folders in archived rooms.

#### Scenario: Nested folder

- **WHEN** the path of a child folder in a room is requested
- **THEN** the parent comes before the child in the chain

#### Scenario: Refused requests

- **WHEN** the caller is anonymous, may not read the folder, or the folder does not exist
- **THEN** the answer is 401, 403 with "You don't have enough permission to view the folder content", or 404

### Requirement: Subfolders

`getFolders` SHALL answer with all direct subfolders of a folder, without nested ones and without paging, and an empty
list when it has none. It applies to folders, rooms, archived rooms and virtual folders the caller may read.

#### Scenario: More than a page

- **WHEN** a room has 30 subfolders
- **THEN** the answer has 30 entries

#### Scenario: Refused requests

- **WHEN** the caller is anonymous, has no access (a non-member of a Public room included, or one whose access was revoked), or the folder does not exist or was deleted
- **THEN** the answer is 401, 403 or 404 respectively

### Requirement: Changing a room's cover

`changeRoomCover` SHALL set `color` and `cover` and answer with the whole room. Each MAY be sent alone, for any room
type and for a template. An empty `color` restores a default colour and an empty body changes nothing. A later request
overwrites an earlier one; the cover survives archiving and unarchiving.

#### Scenario: Cover and colour set

- **WHEN** a room manager sends a gallery `cover` and `color`
- **THEN** `logo.cover.id`, its `data` and `logo.color` are those values in the answer and in `getRoomInfo`

#### Scenario: Invalid values

- **WHEN** `color` is not six hexadecimal digits or `cover` is not in the gallery
- **THEN** the answer is 400

#### Scenario: Refused requests

- **WHEN** an invited user or a person outside the room, an anonymous caller, an archived room, or an unknown or deleted room is involved
- **THEN** the answer is 403, 401, 403 and 404 respectively, and an archived room keeps its cover

### Requirement: Removing a room's logo

`deleteRoomLogo` SHALL remove the uploaded logo and answer with the room, whose earlier cover is shown again. Membership
and other fields stay. A room with no logo, and a repeated call, SHALL answer 200. A template or room created afterwards
from the room has no logo.

#### Scenario: Logo removed

- **WHEN** a room manager removes a logo set over a cover
- **THEN** `logo.original` is empty and `getRoomInfo` reports that cover

#### Scenario: Refused requests

- **WHEN** an invited user or a room administrator with editor access or a person outside the room, an anonymous caller, an archived room, or an unknown or deleted room is involved
- **THEN** the answer is 403, 401, 403 and 404 respectively

### Requirement: Creating a tag in the catalogue

`createRoomTag` SHALL add `name` to the portal's tag catalogue and answer with the name. Only a room administrator, a
DocSpace administrator and the owner MAY call it. A name differing only in case from an existing tag SHALL NOT create a
second one; a deleted name can be created again.

#### Scenario: Existing name

- **WHEN** a name in the catalogue is sent again
- **THEN** the answer is the name and the catalogue holds it once

#### Scenario: Invalid names

- **WHEN** the name is empty, blank, null, missing, not a string, or over 255 characters
- **THEN** the answer is 400

#### Scenario: Refused callers

- **WHEN** a user or guest, or an anonymous or terminated caller, calls the operation
- **THEN** the answer is 403 or 401

### Requirement: Deleting tags from the catalogue

`deleteCustomTags` SHALL remove the tags in `names` from the catalogue and from every room, for a DocSpace administrator
or the owner. Names not in the catalogue are skipped, duplicates are removed once, and names with a similar prefix stay.

#### Scenario: Attached tag

- **WHEN** a tag attached to several rooms is deleted
- **THEN** `getRoomInfo` of those rooms no longer lists it and it can be created and attached again

#### Scenario: Invalid bodies

- **WHEN** `names` is absent, null, not an array, holds a non-string, a blank or null entry, or a name over 255 characters
- **THEN** the answer is 400

#### Scenario: Refused callers

- **WHEN** a room administrator, user or guest, or an anonymous or terminated caller, calls the operation
- **THEN** the answer is 403 "Access denied" or 401

### Requirement: Checking whether a tag is used

`hasTagLinks` SHALL answer whether any room carries the tag named by the `tagName` query parameter, for a DocSpace
administrator or the owner; the path segment does not decide. An unknown or blank name gets 404.

#### Scenario: Tag detached

- **WHEN** a tag is removed from its only room with `deleteRoomTags`
- **THEN** the answer turns false and the tag stays in `getRoomTagsInfo`

#### Scenario: Last room deleted

- **WHEN** the only room with the tag is deleted
- **THEN** the tag leaves the catalogue and the answer is 404

#### Scenario: Refused callers

- **WHEN** a room administrator, user or guest, or an anonymous caller, calls the operation
- **THEN** the answer is 403 or 401

### Requirement: Group members with their access

`getGroupsMembersWithFolderSecurity` and `getGroupsMembersWithFileSecurity` SHALL page through the members of a group
with `groupAccess`, `userAccess`, `overridden`, `owner` and `canEditAccess` on the entry. A group without rights on the
entry and an unreadable entry SHALL give an empty list. A guest gets 403.

#### Scenario: Guest with shared access

- **WHEN** a guest to whom the file, or the file and the group, was shared calls `getGroupsMembersWithFileSecurity`
- **THEN** the answer is 403

### Requirement: Unlocking a password-protected link

`applyExternalSharePassword` SHALL check `password` for the link named by its `requestToken` and answer with the link
data and a `status`. A wrong password SHALL be reported in `status`, not as an HTTP error. No authentication is needed.

#### Scenario: Wrong then right password

- **WHEN** a wrong password and then the right one are sent
- **THEN** `status` is `InvalidPassword` and then `Ok`

#### Scenario: Too many attempts

- **WHEN** the attempt limit for the link and calling address is reached
- **THEN** the answer is 403 until the block expires
