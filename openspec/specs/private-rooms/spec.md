# private-rooms Specification

## Purpose

Defines end-to-end encrypted private rooms as the server's API sees them: the personal key pairs of an account
(`privacyroom/keys`), the keys that give access to a room or are needed before inviting (`privacyroom/{roomId}/access`,
`candidates`, `missing`), the wrapped file keys and who may hold them, what the server refuses so that plaintext and
titles stay out, the protection of an archived private room, and the Private rooms section (`files/@private`).

The server never decrypts anything: encryption happens in the client, and these rules are what the server enforces
around it. A private room is an ordinary room with the `private` flag, so this specification states only the
differences. What each role may do in a room is in `room-role-permissions`; room types, the `private` flag and who may
create rooms are in `room-types`; the audit trail and the entry history are in `history-and-audit`; letters are in
`notifications`; webhook delivery is in `webhooks-and-api-keys`; backups are in `backup-and-restore`.

Sources: the integration tests of `products/ASC.Files/Tests/Tests/08_Private`, the private-room tests of
`web/ASC.Web.Api.Tests` and `common/Tests/ASC.Notify.Tests`, the OpenAPI contract, and the product code where the tests
stop. Evidence per requirement, the differences and the suspected defects are in `verification.md` next to this file.

## Requirements

### Requirement: Creating an encryption key

`setKeys` SHALL add a key pair to the calling account's own set and answer 201 with every key the caller now holds. A
pair is named by `id` and made of `publicKey` and `privateKeyEnc`, which the server keeps as opaque text. An account may
hold several pairs.

#### Scenario: Second pair with a new id

- **WHEN** an account holding one pair calls `setKeys` with a new `id`
- **THEN** the answer is 201 and `getUserKeys` lists both pairs, each with its own `publicKey`

### Requirement: Refused key material stores nothing

`setKeys` and `replaceKey` MUST answer 400 and change nothing when a half is missing or blank, the body is absent, `id`
is malformed, or the set would be too large to store. The stored pairs, including those the request did not name, stay
as they were.

#### Scenario: Blank or missing half on create

- **WHEN** `setKeys` is called with a blank `publicKey`, without a half, or with no body
- **THEN** the answer is 400 and the caller's key set stays empty

#### Scenario: Partial body on rotation

- **WHEN** `replaceKey` is called with only `publicKey`, with a blanked half, or with no body
- **THEN** the answer is 400 and the stored pair keeps both of its halves

#### Scenario: Oversized public key

- **WHEN** `setKeys` or `replaceKey` carries a `publicKey` too large to store
- **THEN** the answer is 400 and every pair the caller already held survives, while an 8192-character key is accepted and stored intact

#### Scenario: Malformed id

- **WHEN** `setKeys` or `replaceKey` carries an `id` that is not a GUID
- **THEN** the answer is 400

### Requirement: A key id cannot be created twice

`setKeys` MUST answer 409 when the `id` is already in the caller's set and leave the stored pair unchanged.

#### Scenario: Repeated id

- **WHEN** `setKeys` is repeated with an `id` already stored and other material
- **THEN** the answer is 409, the set holds one entry for that `id`, and its `publicKey` is the original one

### Requirement: Rotating an encryption key

`replaceKey` SHALL overwrite both halves of the pair named by `id`, leave the caller's other pairs untouched and answer
200 with the whole set. It MUST answer 404 when the `id` is not in the caller's set. Omitting `id` targets the all-zero
pair, so repeated calls update it in place.

#### Scenario: Rotation of one pair

- **WHEN** an account with two pairs rotates one of them
- **THEN** that pair holds the new material, the old value is gone, and the other pair is unchanged

#### Scenario: Nothing to rotate

- **WHEN** `replaceKey` names an `id` the caller does not hold
- **THEN** the answer is 404 and nothing is stored

#### Scenario: No id

- **WHEN** `replaceKey` is called three times without `id`
- **THEN** the set holds one pair with the all-zero id and the last material

### Requirement: Reading own keys

`getUserKeys` SHALL return exactly the caller's own pairs, each with `id`, `userId`, `date`, `publicKey`, `privateKeyEnc`
and the same `cryptoEngineId`. An account with no key gets an empty answer. There is no parameter for another account.

#### Scenario: Every field

- **WHEN** an account with two pairs calls `getUserKeys`
- **THEN** both come back with `userId` the caller, a recent `date` and one braced GUID as `cryptoEngineId`

#### Scenario: No key yet

- **WHEN** an account that never stored a key calls `getUserKeys`
- **THEN** the answer is 200 with an empty set

### Requirement: Deleting an encryption key

`deleteKeys` SHALL remove the named pair of the caller, leave the other pairs and answer 204. It MUST answer 404 for an
`id` not held, including a second delete of the same `id`, and 400 for a malformed `id`. The collection route has no
DELETE, so a call without an id gets 405.

#### Scenario: One of three pairs

- **WHEN** an account with three pairs deletes one
- **THEN** the answer is 204 and the other two remain

#### Scenario: Unknown or repeated id

- **WHEN** `deleteKeys` names a valid GUID that is not held, or repeats a delete
- **THEN** the answer is 404

### Requirement: Keys are personal

Keys SHALL be managed only by their owner. No portal role, DocSpace admin included, MUST be able to read, rotate or
delete another account's pair through these operations; the same `id` in another account's set is a different pair.

#### Scenario: Same all-zero id in two accounts

- **WHEN** a User or a DocSpace admin rotates or deletes the all-zero pair
- **THEN** only their own pair changes and the Owner's key set is unchanged

### Requirement: Guests cannot own keys

A guest MUST be refused with 403 on `setKeys`, `replaceKey` and `deleteKeys`, and nothing is stored; `getUserKeys` answers
a guest 200 with an empty set. An anonymous caller gets 401 on all four. Owner, DocSpace admin, Room admin and User manage
their own keys.

#### Scenario: Guest stores a key

- **WHEN** a guest calls `setKeys`
- **THEN** the answer is 403 "Access denied" and the guest's key set stays empty

#### Scenario: Anonymous caller

- **WHEN** an unauthenticated caller uses any of the four key operations
- **THEN** the answer is 401

### Requirement: Keys do not touch sign-in links

Storing, rotating or deleting keys MUST NOT remove a sign-in link the account has with an external provider.

#### Scenario: Account with a Google link

- **WHEN** an account linked to Google calls `setKeys`, `replaceKey` or `deleteKeys`
- **THEN** the link is still there afterwards

### Requirement: A key set stored in the old format stays usable

A key stored before key sets existed, as one pair without id or date, SHALL be read as one pair with the all-zero `id` and
the minimal `date`, the same on every read. It MUST survive adding a new pair and SHALL be removable by `deleteKeys` with
the all-zero `id`.

#### Scenario: Reading and extending an old key

- **WHEN** an account with an old-format key calls `getUserKeys` and then `setKeys` with a new `id`
- **THEN** the first answer has the old key under the all-zero `id`, and the second holds both pairs

### Requirement: Access keys of a room

`getUserKeysForRoom` SHALL return one entry per key held by each member of an active or archived private room, to any
member from read access upwards. Only the caller's own entries carry `privateKeyEnc`; another member's entry has the
public half alone, and an entry without a public half is not reported.

#### Scenario: Member reads the set

- **WHEN** a Viewer, a Room manager or a DocSpace admin who is a member calls it
- **THEN** the keys of the members come back, the caller's own with `privateKeyEnc` and the others' without it

#### Scenario: Membership changes

- **WHEN** a member is removed or rotates their key
- **THEN** the removed member gets 403, and the set no longer holds their key or the rotated old value

#### Scenario: Archived room

- **WHEN** the owner calls it on an archived private room
- **THEN** the keys are returned

### Requirement: Who is refused the access keys of a room

`getUserKeysForRoom` MUST answer 403 to a non-member, a DocSpace admin who is not a member, and a member holding no key, the
room creator included; 400 for a room that is not private; 404 for an unknown room or one in Trash; 401 to anonymous.

#### Scenario: Creator deleted their last key

- **WHEN** the creator deletes their last key and calls it
- **THEN** the answer is 403 while the room survives and still reports `private`

#### Scenario: Room that does not qualify

- **WHEN** it is called on an ordinary room, a room in Trash, or an unknown or out-of-range id
- **THEN** the answer is 400 for the ordinary room and 404 for the others

### Requirement: Public keys of people to invite

`getRoomCandidateKeys` SHALL return the public keys of the listed `userIds`, never `privateKeyEnc`, not even the caller's.
A user without a key, a guest, a terminated user and an id outside the portal are left out, a duplicate counts once, and
no ids answer an empty list. It grants nothing.

#### Scenario: Non-member candidate

- **WHEN** the owner lists a non-member who holds a key
- **THEN** one entry comes back with their `publicKey`, a null `privateKeyEnc`, and the person can then be invited

#### Scenario: Someone who cannot be a member

- **WHEN** the list names a user without keys, a guest, a terminated user or an account of another portal
- **THEN** that person is absent from the answer

### Requirement: Who may ask for invitee keys

`getRoomCandidateKeys` MUST require the right to change the room's access: the owner or a room manager. Other members,
non-members and a non-member DocSpace admin get 403, as does anyone on an archived room; a room that is not private, a
subfolder, or a malformed id gets 400; an unknown room 404; anonymous 401.

#### Scenario: Member without the right

- **WHEN** a member who cannot manage access calls it
- **THEN** the answer is 403, while a Room manager gets the keys

#### Scenario: Not a private room itself

- **WHEN** it is called with an ordinary room or a subfolder of a private room
- **THEN** the answer is 400

### Requirement: Missing file keys of a room

`getRoomMissingFileKeys` SHALL report, as identifiers only, which encrypted files anywhere in a private room each user lacks
a key for, for the listed `userIds`, or for the room's members when none are listed. A user who misses nothing is left out;
an unencrypted file and an id outside the portal are never reported.

#### Scenario: Until the key is written

- **WHEN** a member has no key for a file and it is then issued with `setEncryptionInfo`
- **THEN** the first answer names the user and `fileIds`, and the second is empty

#### Scenario: No users listed

- **WHEN** it is called without `userIds`
- **THEN** only members of the room are checked, and a non-member is not in the answer

### Requirement: Who may ask for missing file keys

`getRoomMissingFileKeys` MUST require the right to change the room's access. A member without it, a non-member DocSpace admin
and any caller on an archived room get 403; a room that is not private or a malformed id gets 400; an unknown room 404;
anonymous 401.

#### Scenario: Archived room

- **WHEN** the owner calls it on an archived private room
- **THEN** the answer is 403

### Requirement: Issuing file keys

`setEncryptionInfo` SHALL store the key of each named recipient who can read the file and return the others in
`skippedUserIds`, whatever their order in the request, instead of refusing the whole call. A write that stores something is
recorded in the file's history; one that stores nothing records nothing.

#### Scenario: Recipient who lost access

- **WHEN** the request names a member and somebody who cannot read the file
- **THEN** the member's key is stored and the other person is returned in `skippedUserIds`

#### Scenario: Every recipient gone

- **WHEN** nobody named can read the file
- **THEN** nothing is stored and `getFileHistory` has no entry for it

### Requirement: File keys of an archived room stay readable

`getEncryptionAccess` SHALL still answer for a file of an archived private room, so that its readers can open the content.

#### Scenario: Archived file

- **WHEN** the owner asks for the keys of a file in an archived private room
- **THEN** the owner's public key is in the answer

### Requirement: The new owner must hold the file keys

`changeFileOwner` MUST answer 409 and keep the owner when the new owner of a private room lacks a key for an encrypted file
anywhere in it. It succeeds once the keys are written, or when the room holds no encrypted file, and the new owner keeps their
keys.

#### Scenario: Missing key, also in a subfolder

- **WHEN** the new owner lacks a key for an encrypted file of the room or of a subfolder
- **THEN** the answer is 409 and the room's owner is unchanged

#### Scenario: Key written first

- **WHEN** the missing key is issued and the call is repeated
- **THEN** the owner changes and `getRoomMissingFileKeys` reports nothing for the new owner

### Requirement: Reassigning a deleted user's rooms ignores file keys

Reassigning the rooms of a user being deleted MUST NOT be stopped by a missing file key, since nobody is left to issue it.

#### Scenario: Deleted owner

- **WHEN** the rooms of a deleted owner go to somebody without a key for an encrypted file
- **THEN** the operation finishes without an error and the room has the new owner

### Requirement: Leaving a private room revokes file keys

A user who is removed from a private room, or leaves it, SHALL lose their wrapped file keys for the whole room including
subfolders, whether or not any client is online, and the revocation is recorded in the audit trail. The other members keep
theirs, and changing a member's access or the room's owner does not count as leaving.

#### Scenario: Member removed

- **WHEN** a member of a room with encrypted files, some in subfolders, is removed
- **THEN** `getRoomMissingFileKeys` names them for those files and not the member who stayed, and a revocation record exists

#### Scenario: Access changed

- **WHEN** a member's access level changes
- **THEN** they miss no key

### Requirement: Clearing access on a private room

`removeSecurityInfo` on a private room SHALL remove whoever loses access together with their file keys. The caller stops being
able to read the room (403 on `getRoomInfo`) when they left as a member.

#### Scenario: Member leaves

- **WHEN** a member calls `removeSecurityInfo` on the room
- **THEN** they lose the room and their keys, and the others keep theirs

#### Scenario: Owner clears the access

- **WHEN** the owner calls it
- **THEN** the members lose their keys

### Requirement: The owner always keeps the room and its keys

The owner of a private room MUST stay its owner with their file keys through `removeSecurityInfo`, through their access being
set to none by them or a room manager, and through removal of a share of their own.

#### Scenario: Owner's access set to none

- **WHEN** the owner or a room manager sets the owner's access to none
- **THEN** the owner is still listed as owner and misses no key

### Requirement: An administrator outside cannot empty a private room

`removeSecurityInfo` MUST answer 403 to a DocSpace admin who is not a member of the private room, and the members keep their
access and keys. On an ordinary room the same caller still clears the access.

#### Scenario: Non-member admin

- **WHEN** a DocSpace admin outside the private room calls it
- **THEN** the answer is 403 and the member misses no key

### Requirement: Deleting a user deletes their file keys

Deleting a user's personal data SHALL delete all their wrapped file keys.

#### Scenario: User deleted

- **WHEN** a user holding a file key is deleted
- **THEN** none of their file keys remain

### Requirement: Keys and file keys are not backed up

A portal backup MUST NOT contain the personal key pairs or the wrapped file keys.

#### Scenario: What a backup leaves out

- **WHEN** the backed-up tables are compared with the schema
- **THEN** both stores are on the list of intentionally skipped tables

### Requirement: Room types that can be private

`createRoom` with `private:true` SHALL work for a Custom, an Editing and a Virtual Data Room. A Public room and a Form filling
room, which create an external link of their own, MUST be refused with 403. A key set that is empty counts as holding no key.

#### Scenario: Type that creates a link

- **WHEN** a caller with keys creates a Public or a Form filling room with `private:true`
- **THEN** the answer is 403

#### Scenario: Keys deleted before creating

- **WHEN** a caller who deleted their only key creates a private room
- **THEN** the answer is 403

### Requirement: Private rooms need editors that can encrypt

`createRoom` with `private:true` MUST answer 403 and `openEditFile` on an encrypted file of a private room 415 while the document
server does not serve its encryption library; `getFilesSettings` reports this in `docServiceEncryptionSupported`.

#### Scenario: Support reported

- **WHEN** `getFilesSettings` is read on a stack that has the support
- **THEN** `docServiceEncryptionSupported` is true

#### Scenario: No support

- **WHEN** the document server does not serve the library
- **THEN** creating a private room answers 403 and opening its encrypted file answers 415

### Requirement: No private room on a connected storage

`createRoomThirdParty` with `private:true` MUST answer 400 with or without keys, because keys of third-party files cannot be
stored. The connection stays free for an ordinary room.

#### Scenario: Refused, connection unused

- **WHEN** a private room is requested on a connected storage
- **THEN** the answer is 400 naming third-party storage, and an ordinary room can then use the same connection

### Requirement: Settings that need the content in the clear

`createRoom` and `updateRoom` MUST answer 400 when a private room would get indexing, a lifetime or a watermark. Turning them off
and renaming stay allowed. `createRoomFromTemplate` with such a template fails in its status and makes no room.

#### Scenario: Enabling on update

- **WHEN** `updateRoom` enables indexing, a lifetime or a watermark on a private room
- **THEN** the answer is 400

#### Scenario: Template with indexing

- **WHEN** a private room is created from a template with indexing
- **THEN** `getRoomCreatingStatus` reports an error and no room with that title exists

### Requirement: Inviting requires a key

`setRoomSecurity` on a private room MUST answer 403 "does not have an encryption key" for an invitee holding no key. The same call
succeeds once they hold one. A guest, who cannot hold one, is always refused, though accepted in an ordinary room.

#### Scenario: Keyless invitee

- **WHEN** a user without a key is invited, and then invited again after storing one
- **THEN** the first call is 403 and the second succeeds

### Requirement: No links to a private room

`setRoomLink` MUST answer 403 for an external and for an invitation link on a private room, because somebody entering by a link
holds no key. Revoking an existing link stays possible.

#### Scenario: Creating a link

- **WHEN** the owner sets an external or an invitation link
- **THEN** the answer is 403

### Requirement: A private room cannot be duplicated or made a template

`duplicateBatchItems` MUST make no copy of a private room, and `createRoomTemplate` from one MUST answer 403. The room's `security`
reports `Duplicate`, `Copy` and `IndexExport` as false.

#### Scenario: Duplicate

- **WHEN** the owner duplicates a private room
- **THEN** only the original exists, and nothing lands in Rooms

#### Scenario: Template

- **WHEN** the owner creates a template from a private room
- **THEN** the answer is 403

### Requirement: Content does not cross the border of a private room

Folders and files MUST NOT be moved or copied into or out of a private room. `copyBatchItems` directly answers 403 when it can tell;
otherwise the queued operation leaves things where they were. Moving within the room stays allowed.

#### Scenario: Copy across the border

- **WHEN** a member copies a file or folder of their documents into the room, or a room folder out of it
- **THEN** the answer is 403

#### Scenario: Move across the border

- **WHEN** a subfolder is moved out, or an open folder is moved in
- **THEN** each folder stays where it was

#### Scenario: Move inside

- **WHEN** a subfolder is moved into another folder of the same room
- **THEN** it arrives

### Requirement: Copy-as across the border is refused

`copyFileAs` MUST answer 403 when the source is in a private room, or the destination is one or a subfolder of one.

#### Scenario: Out and in

- **WHEN** `copyFileAs` copies out of the room, into the room or into its subfolder
- **THEN** the answer is 403

### Requirement: A private room takes encrypted files only

`createFile` in a private room or its subfolder MUST answer 403, and `createUploadSessionInFolder` without `encrypted:true` MUST answer
403 at session creation. An ordinary room keeps accepting open files.

#### Scenario: Open file

- **WHEN** a member creates a file, or opens a session without the flag
- **THEN** the answer is 403

### Requirement: File names in a private room are generated

`createUploadSessionInFolder` in a private room MUST answer 400 unless the `fileName` is a lowercase UUID with an optional lowercase
extension, since the server sees it in the clear. An ordinary room accepts any name.

#### Scenario: Readable or wrongly cased name

- **WHEN** the name is readable, an uppercase UUID, has an uppercase extension, or has more after the UUID
- **THEN** the answer is 400

#### Scenario: Generated name

- **WHEN** the name is a lowercase UUID with or without an extension
- **THEN** a session is opened

### Requirement: Aborting a session is bound to its folder

`abortUploadSession` MUST answer 404 and keep the session when the session was opened against another folder.

#### Scenario: Another folder

- **WHEN** the session is aborted through a folder it does not belong to
- **THEN** the answer is 404

### Requirement: Unencrypted saves are refused

In a private room the document server's save callback MUST be refused, writing no version, when it lacks the encrypted flag, is a form
submit, or comes from somebody who has no access or lost it. `saveEditingFileFromForm` without the flag MUST answer 403.

#### Scenario: Refused callback

- **WHEN** a save callback lacks the flag, submits a form, or names a non-member or a removed member
- **THEN** the answer carries the error and the refusal, and the file's version is unchanged

#### Scenario: Valid callback

- **WHEN** a member's callback carries the flag
- **THEN** the server goes on to download the file

#### Scenario: Save through the API

- **WHEN** `saveEditingFileFromForm` is called without the flag
- **THEN** the answer is 403 and the version is unchanged

### Requirement: Operations that need open content are refused

`restoreFileVersion` by URL and `getReferenceData` on a file of a private room, and `startFillingFile` on an encrypted form, MUST answer
403. An encrypted form reports `security.StartFilling` false in Custom and Virtual Data rooms.

#### Scenario: Each of them

- **WHEN** a member calls one of them
- **THEN** the answer is 403

### Requirement: Encrypted files are offered on the public address

The document address of an encrypted file MUST keep the portal's public host instead of being rewritten for the document server, because
the browser downloads it.

#### Scenario: Compared with an open file

- **WHEN** the editor configuration of an encrypted and of an open file are read
- **THEN** their document addresses have different hosts

### Requirement: An archived private room keeps its protection

Archiving MUST NOT widen access. A DocSpace admin or the Owner who is not a member MUST get 403 on the content, on deleting and on
unarchiving the room; an archived ordinary room keeps the administrator's rights.

#### Scenario: Non-member administrator

- **WHEN** a non-member DocSpace admin reads the room's file or folder, deletes the room or unarchives it
- **THEN** the answer is 403 and the room still exists

### Requirement: Nothing is copied out of an archived private room

`Copy` MUST be false for everyone in an archived private room. The creator keeps `Read` and `Delete` on the room, and an invited administrator
keeps `Read`.

#### Scenario: Creator and member

- **WHEN** the creator reads the `security` of the room, or a member copies a file or subfolder out
- **THEN** `Copy` is false, and nothing arrives at the destination

### Requirement: The Private rooms section

`getRoomsFolder` SHALL list private rooms only with `searchArea=PrivateRooms`, or `privacyFilter=Private` on the Active area; Active without
that filter leaves them out, and `privacyFilter=NotPrivate` lists the others. The split follows the room's flag, not its type.

#### Scenario: Each listing

- **WHEN** an ordinary and a private room exist
- **THEN** the PrivateRooms area and Active with `Private` hold the private room only, and Active and Active with `NotPrivate` the ordinary one only

### Requirement: The section's own endpoint

`getPrivateRoomsFolder` SHALL return the private rooms the caller belongs to as `folders`, with empty `files` and `current.rootFolderType`
PrivateRooms. `getRootFolders` exposes the section, and `getFolderByFolderId` with its root lists the same rooms.

#### Scenario: Reading the section

- **WHEN** the owner of an ordinary and of a private room calls `getPrivateRoomsFolder`
- **THEN** only the private room is listed and `files` is empty

### Requirement: The section follows the app

While the end-to-end encrypted rooms app (`e2e-rooms`) is off, `getRootFolders` MUST NOT list the section and `getPrivateRoomsFolder` MUST answer
404.

#### Scenario: App off

- **WHEN** the app is turned off
- **THEN** the root is absent and the endpoint answers 404

### Requirement: Membership decides who sees a private room

A private room MUST NOT be listed to an Owner or DocSpace admin who is not a member, unlike an ordinary room. Once invited, they see it in the
section and not in Rooms.

#### Scenario: Administrator outside, then invited

- **WHEN** a non-member DocSpace admin lists the PrivateRooms area, is invited, and lists it again
- **THEN** the room is absent the first time and present the second

### Requirement: Archived private rooms stay in the shared archive

An archived private room SHALL leave the section and be listed in the Archive area.

#### Scenario: Archiving

- **WHEN** a private room is archived
- **THEN** the Archive area lists it and the PrivateRooms area does not

### Requirement: New marks count in the section

New items of a private room SHALL count in the section's `current.new` and not in the Rooms section's.

#### Scenario: New file

- **WHEN** a member gets a new file in a private room
- **THEN** the section's `new` is above zero and that of Rooms is zero

### Requirement: Recent has no private rooms area

`getRecentFolder` with `searchArea=PrivateRooms` MUST answer 400.

#### Scenario: Recent

- **WHEN** it is called with that area
- **THEN** the answer is 400

### Requirement: Room groups of the section

`addRoomGroup` with `searchArea=PrivateRooms` SHALL take private rooms only, and MUST answer 400 for an ordinary room there and for a private room
in a group of the Active area.

#### Scenario: Matching and mismatching rooms

- **WHEN** groups are added with a private room in PrivateRooms, an ordinary room in PrivateRooms, and a private room in Active
- **THEN** the first has one room and the others answer 400

### Requirement: Titles in the audit trail

The audit trail MUST keep the record of a private room's events while naming the room "Private room" and hiding folder titles, for an administrator
outside it as well. Names of users stay. Ordinary rooms keep their titles. Everything else about the audit trail is in `history-and-audit`.

#### Scenario: Created, renamed and moved

- **WHEN** a private room is created and renamed, and a folder in it is created and moved
- **THEN** the events are on record and neither `action` nor `context` holds the old or new title

#### Scenario: Member invited

- **WHEN** a member is invited
- **THEN** the event names the member and not the room

#### Scenario: Quota changed

- **WHEN** a room quota changes for a private room
- **THEN** the event lists "Private room"

### Requirement: Members still read the titles in the history

The history of a private room SHALL keep showing its members the real titles.

#### Scenario: Member reads the history

- **WHEN** a member reads the history of a room with a folder
- **THEN** the folder's title appears

### Requirement: Titles in letters

Invitations and the room letters and pushes of Files MUST name a private room "Private room" and hide its folder titles. An ordinary room keeps its
title. The letters themselves are in `notifications`.

#### Scenario: Invitation and new role

- **WHEN** an invitation or a new-role letter is sent for a private room
- **THEN** the room is named "Private room"

#### Scenario: Room activity

- **WHEN** a room letter of Files is prepared for a private room
- **THEN** neither the room's nor the folder's title is among its tags

### Requirement: Titles in webhooks

A webhook MUST NOT deliver a private room's or its folder's event to the subscription of somebody outside the room, a DocSpace admin included, also when
a member's subscription is examined first.

#### Scenario: Administrator's subscription

- **WHEN** a private room or a folder in it is created, with subscriptions of a member and of a non-member admin
- **THEN** the member's receives the payload and the admin's log stays empty
