# Spec Delta: private-rooms

## RENAMED Requirements

- FROM: `### Requirement: Reassigning a deleted user's rooms ignores file keys`
- TO: `### Requirement: A deleted user's private room goes to someone holding a key`

The old name says the keys play no part; after this change they decide who gets a private room.

## MODIFIED Requirements

Current texts, quoted from `openspec/specs/private-rooms/spec.md` at `365d87fe60`:

> ### Requirement: Inviting requires a key
>
> `setRoomSecurity` on a private room MUST answer 403 "does not have an encryption key" for an invitee holding no key. The same call
> succeeds once they hold one. A guest, who cannot hold one, is always refused, though accepted in an ordinary room.
>
> #### Scenario: Keyless invitee
>
> - **WHEN** a user without a key is invited, and then invited again after storing one
> - **THEN** the first call is 403 and the second succeeds

> ### Requirement: Leaving a private room revokes file keys
>
> A user who is removed from a private room, or leaves it, SHALL lose their wrapped file keys for the whole room including
> subfolders, whether or not any client is online, and the revocation is recorded in the audit trail. The other members keep
> theirs, and changing a member's access or the room's owner does not count as leaving.
>
> #### Scenario: Member removed
>
> - **WHEN** a member of a room with encrypted files, some in subfolders, is removed
> - **THEN** `getRoomMissingFileKeys` names them for those files and not the member who stayed, and a revocation record exists
>
> #### Scenario: Access changed
>
> - **WHEN** a member's access level changes
> - **THEN** they miss no key

> ### Requirement: Reassigning a deleted user's rooms ignores file keys
>
> Reassigning the rooms of a user being deleted MUST NOT be stopped by a missing file key, since nobody is left to issue it.
>
> #### Scenario: Deleted owner
>
> - **WHEN** the rooms of a deleted owner go to somebody without a key for an encrypted file
> - **THEN** the operation finishes without an error and the room has the new owner

> ### Requirement: Room types that can be private
>
> `createRoom` with `private:true` SHALL work for a Custom, an Editing and a Virtual Data Room. A Public room and a Form filling
> room, which create an external link of their own, MUST be refused with 403. A key set that is empty counts as holding no key.
>
> #### Scenario: Type that creates a link
>
> - **WHEN** a caller with keys creates a Public or a Form filling room with `private:true`
> - **THEN** the answer is 403
>
> #### Scenario: Keys deleted before creating
>
> - **WHEN** a caller who deleted their only key creates a private room
> - **THEN** the answer is 403

> ### Requirement: Settings that need the content in the clear
>
> `createRoom` and `updateRoom` MUST answer 400 when a private room would get indexing, a lifetime or a watermark. Turning them off
> and renaming stay allowed. `createRoomFromTemplate` with such a template fails in its status and makes no room.
>
> #### Scenario: Enabling on update
>
> - **WHEN** `updateRoom` enables indexing, a lifetime or a watermark on a private room
> - **THEN** the answer is 400
>
> #### Scenario: Template with indexing
>
> - **WHEN** a private room is created from a template with indexing
> - **THEN** `getRoomCreatingStatus` reports an error and no room with that title exists

> ### Requirement: A private room cannot be duplicated or made a template
>
> `duplicateBatchItems` MUST make no copy of a private room, and `createRoomTemplate` from one MUST answer 403. The room's `security`
> reports `Duplicate`, `Copy` and `IndexExport` as false.
>
> #### Scenario: Duplicate
>
> - **WHEN** the owner duplicates a private room
> - **THEN** only the original exists, and nothing lands in Rooms
>
> #### Scenario: Template
>
> - **WHEN** the owner creates a template from a private room
> - **THEN** the answer is 403

> ### Requirement: Operations that need open content are refused
>
> `restoreFileVersion` by URL and `getReferenceData` on a file of a private room, and `startFillingFile` on an encrypted form, MUST answer
> 403. An encrypted form reports `security.StartFilling` false in Custom and Virtual Data rooms.
>
> #### Scenario: Each of them
>
> - **WHEN** a member calls one of them
> - **THEN** the answer is 403

> ### Requirement: The section follows the app
>
> While the end-to-end encrypted rooms app (`e2e-rooms`) is off, `getRootFolders` MUST NOT list the section and `getPrivateRoomsFolder` MUST answer
> 404.
>
> #### Scenario: App off
>
> - **WHEN** the app is turned off
> - **THEN** the root is absent and the endpoint answers 404

### Requirement: Inviting requires a key

`setRoomSecurity`, `setSecurityInfo` and `changeFileOwner` on a private room MUST answer 403 when they give a user without a key
access to the room for the first time; the same call succeeds once they hold one. A member who already has access is not
checked: they can leave, be removed or get another role, and a previous owner without a key does not stop `changeFileOwner`.
A guest, who cannot hold a key, is always refused, though accepted in an ordinary room.

#### Scenario: Keyless invitee

- **WHEN** a user without a key is invited, and then invited again after storing one
- **THEN** the first call is 403 and the second succeeds

#### Scenario: Keyless member

- **WHEN** a member deletes their only key, gets another role, and is then removed
- **THEN** both calls succeed

#### Scenario: Keyless previous owner

- **WHEN** an owner who deleted their only key hands the room to a member who holds every file key
- **THEN** `changeFileOwner` succeeds and the room has the new owner

### Requirement: Leaving a private room revokes file keys

A user who is removed from a private room, or leaves it, SHALL lose their wrapped file keys for the whole room including
subfolders, whether or not any client is online, and the revocation is recorded in the audit trail. The keys are revoked
before the access is removed, so a user who left holds no key. The other members keep theirs, and changing a member's access
or the room's owner does not count as leaving.

#### Scenario: Member removed

- **WHEN** a member of a room with encrypted files, some in subfolders, is removed
- **THEN** `getRoomMissingFileKeys` names them for those files and not the member who stayed, and a revocation record exists

#### Scenario: Access changed

- **WHEN** a member's access level changes
- **THEN** they miss no key

### Requirement: A deleted user's private room goes to someone holding a key

When the rooms of a user whose data is removed (`startRemove`, `deleteMember`) or transferred (`startReassign`) are
reassigned, a private room MUST go to a user who holds an encryption key: the user who started the removal or the
recipient of the transfer, else the portal owner. When neither holds one, the room stays with its owner and
`keptPrivateRoomIds` of `getRemoveProgress` or `getReassignProgress` names it. A missing file key does not stop it.

#### Scenario: Deleted owner

- **WHEN** the removal of a private room's owner is started by a user who holds a key but no key for an encrypted file
- **THEN** the operation finishes without an error and the room has that user as its owner

#### Scenario: Transfer to a key holder

- **WHEN** the data of a private room's owner is transferred to a user who holds a key
- **THEN** the operation finishes without an error, `keptPrivateRoomIds` is empty, and the room has that user as its owner

#### Scenario: Nobody holds a key

- **WHEN** neither the user who started the removal nor the portal owner holds a key
- **THEN** the operation finishes, the room keeps its owner, `keptPrivateRoomIds` of `getRemoveProgress` holds the room id, and its members still read its files

#### Scenario: Kept room handed over

- **WHEN** a DocSpace admin who is a member of the kept room and holds its file keys calls `changeFileOwner` for it afterwards
- **THEN** the room has the new owner

### Requirement: Room types that can be private

`createRoom` with `private:true` SHALL work for a Custom, an Editing and a Virtual Data Room. A Public room and a Form filling
room, which create an external link of their own, and an AI agent (`POST api/2.0/ai/agents`) with `private:true` MUST be
refused with 400 before anything is saved, audited or sent by webhook. A key set that is empty counts as holding no key.

#### Scenario: Type that creates a link

- **WHEN** a caller with keys creates a Public or a Form filling room with `private:true`
- **THEN** the answer is 400, no room with that title exists, and no audit record or `RoomCreated` webhook is left

#### Scenario: Private AI agent

- **WHEN** a caller with keys creates an AI agent with `private:true`
- **THEN** the answer is 400 and no agent with that title exists

#### Scenario: Keys deleted before creating

- **WHEN** a caller who deleted their only key creates a private room
- **THEN** the answer is 403

### Requirement: Settings that need the content in the clear

`createRoom` and `updateRoom` MUST answer 400 when a private room would get indexing, a lifetime or a watermark. Turning them off
and renaming stay allowed. A private room is never made from a template (see "A private room cannot be duplicated or made a
template").

#### Scenario: Enabling on update

- **WHEN** `updateRoom` enables indexing, a lifetime or a watermark on a private room
- **THEN** the answer is 400

#### Scenario: Template with indexing

- **WHEN** a private room is requested from a template with indexing
- **THEN** the answer is 400 and no room with that title exists

### Requirement: A private room cannot be duplicated or made a template

`duplicateBatchItems` MUST make no copy of a private room, and `createRoomTemplate` from one MUST answer 403.
`createRoomFromTemplate` with `private:true` MUST answer 400 before the operation starts, because a template's files are not
encrypted. The room's `security` reports `Duplicate`, `Copy` and `IndexExport` as false.

#### Scenario: Duplicate

- **WHEN** the owner duplicates a private room
- **THEN** only the original exists, and nothing lands in Rooms

#### Scenario: Template

- **WHEN** the owner creates a template from a private room
- **THEN** the answer is 403

#### Scenario: Private room from a template

- **WHEN** a caller with keys calls `createRoomFromTemplate` with `private:true`
- **THEN** the answer is 400 and no room with that title exists

### Requirement: Operations that need open content are refused

`restoreFileVersion` by URL and `getReferenceData` on a file of a private room, `startFillingFile` on an encrypted form, and
`updateFileComment` on an encrypted file MUST answer 403, also to the room's owner. An encrypted form reports
`security.StartFilling` false in Custom and Virtual Data rooms.

#### Scenario: Each of them

- **WHEN** a member calls one of them
- **THEN** the answer is 403

#### Scenario: Owner comments a version

- **WHEN** the owner of a private room calls `updateFileComment` on an encrypted file
- **THEN** the answer is 403 and the comment is unchanged

### Requirement: The section follows the app

While the end-to-end encrypted rooms app (`e2e-rooms`) is off, `getRootFolders` MUST NOT list the section,
`getPrivateRoomsFolder` MUST answer 404, no area or filter of `getRoomsFolder` lists a private room, and `createRoom` with
`private:true` MUST answer 403. Nothing is deleted: once the app is on, the rooms are listed as before.

#### Scenario: App off

- **WHEN** the app is turned off
- **THEN** the root is absent, the endpoint answers 404, and neither the PrivateRooms area nor Active with `privacyFilter=Private` lists the private room

#### Scenario: Creating while off

- **WHEN** a caller with keys creates a room with `private:true` while the app is off
- **THEN** the answer is 403

#### Scenario: App on again

- **WHEN** the app is turned on again
- **THEN** the PrivateRooms area lists the room again

## ADDED Requirements

### Requirement: A missing key has a refusal of its own

A 403 for a missing encryption key MUST carry an `error.type` that no refusal for rights carries, so that a client can tell
the two apart. The message stays "does not have an encryption key".

#### Scenario: Two refusals compared

- **WHEN** an owner invites a user without a key, and a Viewer of the room invites a user with a key
- **THEN** both answers are 403 and their `error.type` values differ

### Requirement: A refused key check stores nothing

Every key check of a `setRoomSecurity`, `setSecurityInfo` or `changeFileOwner` request on a private room MUST run before its
first write. When one named user is refused, no access of the request is stored, and no socket event, letter, webhook or
audit record is sent for it.

#### Scenario: Mixed list

- **WHEN** one request invites a user with a key and, after them, a user without one
- **THEN** the answer is 403 and neither of them is a member of the room

### Requirement: Groups are not invited to a private room

`setRoomSecurity` and `setSecurityInfo` MUST refuse a group on a private room with 403, whatever keys its people hold, because
file keys are issued per user. The refusal is not the one for a missing key.

#### Scenario: Group invited

- **WHEN** the owner invites a group whose every member holds a key
- **THEN** the answer is 403, its `error.type` is not that of a missing key, and the group is not a member

### Requirement: Who receives the keys of a file

`getEncryptionAccess` SHALL list the public keys of the users with access to the file, and the room's owner always, also
after an owner change. The file's creator is listed only while they are a member of the room. Groups are not listed.

#### Scenario: New owner

- **WHEN** the room is handed to a new owner and `getEncryptionAccess` is read for a file of it
- **THEN** the new owner's public key is in the answer

#### Scenario: Creator left

- **WHEN** the member who uploaded a file is removed from the room
- **THEN** their public key is not in the answer

### Requirement: Private entries answer as missing while the app is off

While the `e2e-rooms` app is off, a private room, its folders and its files MUST answer 404 to every caller, members included,
on `getRoomInfo`, `getFolderByFolderId`, `getFileInfo` and `openEditFile`. The caller's own key operations
(`privacyroom/keys`) stay open.

#### Scenario: Member reads while off

- **WHEN** a member reads the room, a folder and a file of it while the app is off
- **THEN** each answer is 404, and reading their own keys succeeds

### Requirement: Form sections hold no private room

`getRoomsFolder` with `searchArea=Forms` or `searchArea=FormTemplates` MUST NOT list a private room, also a Form filling room
saved as private before such rooms were refused.

#### Scenario: Private form room left from before

- **WHEN** a private Form filling room exists and the Forms area is listed
- **THEN** the room is not in the answer

### Requirement: Private rooms are not favourites

`addFavorites` MUST skip a private room and a folder inside one, and still answer `true`. `getFavoritesFolder` MUST NOT list
them, also when they were marked before this rule. Encrypted files are not added either.

#### Scenario: Marking a private room

- **WHEN** a member adds a private room and one of its folders to Favorites
- **THEN** the answer is `true` and `getFavoritesFolder` lists neither

### Requirement: An encrypted file offers no actions that need its content

The `security` of an encrypted file MUST report `Rename`, `ReadHistory`, `EditHistory`, `Lock`, `CopyLink`, `CustomFilter` and
`SubmitToFormGallery` as false for every caller, the room's owner included.

#### Scenario: Owner reads the file

- **WHEN** the owner of a private room calls `getFileInfo` on an encrypted file
- **THEN** each of those flags is false

### Requirement: Private rooms have no AI

The `security` of a private room MUST report `UseAi` as false, and the editor configuration of an encrypted file returned by
`openEditFile` MUST have `editorConfig.customization.ai.disabled` set to true, whatever the portal's AI setting.

#### Scenario: AI on for the portal

- **WHEN** AI is on for the portal and the owner reads the private room and opens an encrypted file of it
- **THEN** `security.UseAi` is false and `ai.disabled` is true

### Requirement: Disabling a user deletes their file keys

Disabling a user SHALL delete all their wrapped file keys and record the revocation in the audit trail, in the
background once the event of the disabling reaches the Files worker, whatever its way (an LDAP synchronization too). `updateMember`,
`updateUserStatus` and `deleteProfile` also try it before they answer. Their key pair and their memberships stay; once re-enabled, they get file keys only when a member issues them
again.

#### Scenario: Member disabled and re-enabled

- **WHEN** a member holding file keys is disabled and then re-enabled
- **THEN** `getRoomMissingFileKeys` names them for every encrypted file of the room, and a revocation record exists

#### Scenario: Disabled by the directory

- **WHEN** an LDAP synchronization disables a member holding file keys
- **THEN** their file keys are deleted and a revocation record exists, without a request to the API

### Requirement: The owner of a private room cannot be disabled

`updateMember`, `updateUserStatus` and `deleteProfile` MUST answer 403 for a user who owns a private room and change no
account of the request, until the room has another owner. An LDAP synchronization follows the directory and still
disables such a user; the room keeps them as its owner until a member DocSpace admin hands it over.

#### Scenario: Owner disabled

- **WHEN** the Owner disables the owner of a private room, and again after `changeFileOwner` gave the room to a member
- **THEN** the first call is 403 and the user stays active, and the second disables them

### Requirement: A member DocSpace admin may hand a private room over

`changeFileOwner` on a private room SHALL be allowed to its owner and to a DocSpace admin who is a member of the room, whatever
their role there. A DocSpace admin or the Owner who is not a member MUST get 403. The new owner must still hold every file key.

#### Scenario: Member administrator

- **WHEN** a DocSpace admin invited to a private room as a viewer gives it to a member who holds its file keys
- **THEN** the room has the new owner

#### Scenario: Administrator outside

- **WHEN** a DocSpace admin who is not a member calls `changeFileOwner` for a private room
- **THEN** the answer is 403 and the owner is unchanged
