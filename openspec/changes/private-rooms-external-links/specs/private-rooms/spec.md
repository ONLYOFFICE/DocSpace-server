# Spec Delta: private-rooms

Field names (`linkPublicKey`, `linkPrivateKeyEnc`, `linkProofHash`, `passwordRequired`, `proof`, `isLink`,
`linkSecretWraps`) are proposals from the client's draft; they may be renamed before the change is applied, together
with the client change.

## MODIFIED Requirements

Current texts, quoted from `openspec/specs/private-rooms/spec.md` at `2029af1663`:

> ### Requirement: No links to a private room
>
> `setRoomLink` MUST answer 403 for an external and for an invitation link on a private room, because somebody entering by a link
> holds no key. Revoking an existing link stays possible.
>
> #### Scenario: Creating a link
>
> - **WHEN** the owner sets an external or an invitation link
> - **THEN** the answer is 403

> ### Requirement: Access keys of a room
>
> `getUserKeysForRoom` SHALL return one entry per key held by each member of an active or archived private room, to any
> member from read access upwards. Only the caller's own entries carry `privateKeyEnc`; another member's entry has the
> public half alone, and an entry without a public half is not reported.
>
> #### Scenario: Member reads the set
>
> - **WHEN** a Viewer, a Room manager or a DocSpace admin who is a member calls it
> - **THEN** the keys of the members come back, the caller's own with `privateKeyEnc` and the others' without it
>
> #### Scenario: Membership changes
>
> - **WHEN** a member is removed or rotates their key
> - **THEN** the removed member gets 403, and the set no longer holds their key or the rotated old value
>
> #### Scenario: Archived room
>
> - **WHEN** the owner calls it on an archived private room
> - **THEN** the keys are returned

> ### Requirement: Missing file keys of a room
>
> `getRoomMissingFileKeys` SHALL report, as identifiers only, which encrypted files anywhere in a private room each user lacks
> a key for, for the listed `userIds`, or for the room's members when none are listed. A user who misses nothing is left out;
> an unencrypted file and an id outside the portal are never reported.
>
> #### Scenario: Until the key is written
>
> - **WHEN** a member has no key for a file and it is then issued with `setEncryptionInfo`
> - **THEN** the first answer names the user and `fileIds`, and the second is empty
>
> #### Scenario: No users listed
>
> - **WHEN** it is called without `userIds`
> - **THEN** only members of the room are checked, and a non-member is not in the answer

> ### Requirement: Issuing file keys
>
> `setEncryptionInfo` SHALL store the key of each named recipient who can read the file and return the others in
> `skippedUserIds`, whatever their order in the request, instead of refusing the whole call. A write that stores something is
> recorded in the file's history; one that stores nothing records nothing.
>
> #### Scenario: Recipient who lost access
>
> - **WHEN** the request names a member and somebody who cannot read the file
> - **THEN** the member's key is stored and the other person is returned in `skippedUserIds`
>
> #### Scenario: Every recipient gone
>
> - **WHEN** nobody named can read the file
> - **THEN** nothing is stored and `getFileHistory` has no entry for it

> ### Requirement: Keys and file keys are not backed up
>
> A portal backup MUST NOT contain the personal key pairs or the wrapped file keys.
>
> #### Scenario: What a backup leaves out
>
> - **WHEN** the backed-up tables are compared with the schema
> - **THEN** both stores are on the list of intentionally skipped tables

### Requirement: No links to a private room

`setRoomLink`, `setFileExternalLink`, `setFolderPrimaryExternalLink`, `createFilePrimaryExternalLink` and
`createFolderPrimaryExternalLink` MUST answer 403 for an invitation link on a private item and for an external link without
key material, because whoever enters by it holds no key. An external link with key material follows "External link of a
private item". Revoking a link stays possible.

#### Scenario: Creating a link

- **WHEN** the owner sets an invitation link, or an external link without `linkPublicKey`, `linkPrivateKeyEnc` and `linkProofHash`
- **THEN** the answer is 403 and nothing is stored

#### Scenario: Creating a link with key material

- **WHEN** the owner or a room manager sets an external link with the read access and the three key fields
- **THEN** the link is stored and the answer carries it with `linkPublicKey` and `passwordRequired`

#### Scenario: Revoking

- **WHEN** a room manager revokes an existing link of a private item
- **THEN** the link is revoked

### Requirement: Access keys of a room

`getUserKeysForRoom` SHALL return one entry per key held by each member of an active or archived private room, and one entry
with `isLink:true` per working link of the room, to any member from read access upwards. Only the caller's own entries carry
`privateKeyEnc`; other entries have the public half alone, and an entry without a public half is not reported.

#### Scenario: Member reads the set

- **WHEN** a Viewer, a Room manager or a DocSpace admin who is a member calls it
- **THEN** the keys of the members come back, the caller's own with `privateKeyEnc` and the others' without it

#### Scenario: Room with a link

- **WHEN** a member calls it on a room that has a working external link
- **THEN** the answer also holds the link's id and `linkPublicKey` with `isLink:true` and no `privateKeyEnc`

#### Scenario: Membership changes

- **WHEN** a member is removed or rotates their key
- **THEN** the removed member gets 403, and the set no longer holds their key or the rotated old value

#### Scenario: Link revoked

- **WHEN** the link is revoked
- **THEN** the set no longer holds its entry

#### Scenario: Archived room

- **WHEN** the owner calls it on an archived private room
- **THEN** the keys are returned

### Requirement: Missing file keys of a room

`getRoomMissingFileKeys` SHALL report, as identifiers only, which encrypted files anywhere in a private room each user lacks
a key for, for the listed `userIds`, or for the room's members and working links when none are listed; a link is reported
for the files it covers with `isLink:true`. A user or link missing nothing is left out; an unencrypted file and an id
outside the portal are never reported.

#### Scenario: Until the key is written

- **WHEN** a member has no key for a file and it is then issued with `setEncryptionInfo`
- **THEN** the first answer names the user and `fileIds`, and the second is empty

#### Scenario: Link without a wrap

- **WHEN** a file is uploaded into a folder that has a working link and nobody has wrapped its key for the link
- **THEN** the answer called without `userIds` names the link with `isLink:true` and that file, and not files outside the folder

#### Scenario: No users listed

- **WHEN** it is called without `userIds`
- **THEN** only members and working links of the room are checked, and a non-member is not in the answer

### Requirement: Issuing file keys

`setEncryptionInfo` SHALL store the key of each named recipient who can read the file, a working link that covers the file
being one, and return the others in `skippedUserIds`, whatever their order in the request, instead of refusing the whole
call. A write that stores something is recorded in the file's history; one that stores nothing records nothing.

#### Scenario: Recipient who lost access

- **WHEN** the request names a member and somebody who cannot read the file
- **THEN** the member's key is stored and the other person is returned in `skippedUserIds`

#### Scenario: Wrap for a link

- **WHEN** a member issues a key for a working link of the file, of a folder above it or of the room
- **THEN** the wrap is stored

#### Scenario: Link that does not cover the file

- **WHEN** a member issues a key for a link of another item, or for a revoked link
- **THEN** nothing is stored for it and its id is returned in `skippedUserIds`

#### Scenario: Every recipient gone

- **WHEN** nobody named can read the file
- **THEN** nothing is stored and `getFileHistory` has no entry for it

### Requirement: Keys and file keys are not backed up

A portal backup MUST NOT contain the personal key pairs, the wrapped file keys, nor the key material of links: their sealed
keys, proof hashes and the wraps of their secrets.

#### Scenario: What a backup leaves out

- **WHEN** the backed-up tables are compared with the schema
- **THEN** every store of key material is on the list of intentionally skipped tables

#### Scenario: Restored link

- **WHEN** a portal with a link of a private item is restored from a backup
- **THEN** the link opens nothing: a visitor's proof is answered as for an invalid link

## ADDED Requirements

### Requirement: External link of a private item

An external link on a private room, or on a folder or file of one, SHALL be stored with `linkPublicKey`, `linkPrivateKeyEnc`
and `linkProofHash`, within the limits, the audience rules and the managing roles `shared-links` gives a room of the same
type. Its access MUST be read; any other access and `denyDownload:true` MUST be refused with 403, storing nothing.

#### Scenario: Limits of the room type

- **WHEN** a private Custom room already has its primary and five additional links and another is requested
- **THEN** the answer is 403, as for an ordinary Custom room

#### Scenario: Private Virtual Data Room

- **WHEN** a link is set on a file of a private Virtual Data Room with access for anyone with the link
- **THEN** the link is for DocSpace users only, as in an ordinary Virtual Data Room

#### Scenario: Another access

- **WHEN** the link is requested with the editing, review, comment or form filling access
- **THEN** the answer is 403 and nothing is stored

#### Scenario: Deny-download

- **WHEN** the link is requested with `denyDownload:true`
- **THEN** the answer is 403 and nothing is stored

#### Scenario: Content creator

- **WHEN** a Content creator of the private room sets a link on a file they created
- **THEN** the answer is 403

### Requirement: Key material of a link has fixed sizes

`linkPublicKey` and `linkProofHash` SHALL each decode from base64 to exactly 32 bytes; `linkPrivateKeyEnc` SHALL be a
non-empty string of at most 4096 characters. A missing, empty, malformed or oversized value MUST be refused with 400, storing
nothing.

#### Scenario: Public key of another length

- **WHEN** `linkPublicKey` decodes to 31 or to 33 bytes
- **THEN** the answer is 400

#### Scenario: Public key that is not base64

- **WHEN** `linkPublicKey` is not valid base64
- **THEN** the answer is 400

#### Scenario: Empty sealed key

- **WHEN** `linkPrivateKeyEnc` is empty
- **THEN** the answer is 400

#### Scenario: Sealed key at and over the limit

- **WHEN** `linkPrivateKeyEnc` holds 4096 characters, and then 4097
- **THEN** the first is stored and the second answers 400

#### Scenario: Proof hash of another length

- **WHEN** `linkProofHash` decodes to 31 or to 33 bytes, or is empty
- **THEN** the answer is 400

### Requirement: A link of a private item without a password is limited

A link of a private item SHALL carry `passwordRequired:true`, unless it is a link to one file with an expiration date. A
request that breaks the rule, at creation or at a change, MUST be refused with 403. The password MUST NOT reach the server: a
request carrying `password` for such a link MUST be refused with 400.

#### Scenario: File link with an expiration date

- **WHEN** a link to one file with an expiration date and `passwordRequired:false` is set
- **THEN** it is stored

#### Scenario: Outside the rule

- **WHEN** a link with `passwordRequired:false` is set on a folder or a room, or on a file without an expiration date
- **THEN** the answer is 403

#### Scenario: Expiration removed later

- **WHEN** the expiration date of a file link with `passwordRequired:false` is removed
- **THEN** the answer is 403 and the link keeps its date

#### Scenario: Password sent

- **WHEN** the request of a link of a private item carries `password`
- **THEN** the answer is 400 and nothing is stored

### Requirement: A link of a private item is served against its proof

`applyExternalSharePassword` called with `proof` SHALL open the link session and answer `linkPrivateKeyEnc` only when the
hash of `proof` equals `linkProofHash`. Without that session `getExternalShareData`, `getEncryptionAccess` and the file
content MUST answer as for a link that requires a password. Wrong proofs SHALL be counted and blocked as wrong passwords.

#### Scenario: Valid proof

- **WHEN** a visitor sends the proof of a link that is neither revoked nor expired
- **THEN** the answer carries `linkPrivateKeyEnc`, and the files the link covers are served as stored, without conversion

#### Scenario: Missing or wrong proof

- **WHEN** a request names the link without a proof, or with a wrong one
- **THEN** the answer carries no sealed key, no wrap and no content

#### Scenario: Too many wrong proofs

- **WHEN** wrong proofs for one link keep coming from one address
- **THEN** the answer is 403 until the block expires, as for wrong passwords

#### Scenario: Link for DocSpace users only

- **WHEN** a visitor who is not signed in sends a valid proof for a link of a private Virtual Data Room
- **THEN** authentication as a DocSpace user is required first

### Requirement: Wraps for a link are served to its visitor

In a link session, `getEncryptionAccess` SHALL answer the wrap made for the link for each file the link covers and nothing
for other files; a member's call keeps its answer. A file the link covers and has no wrap for SHALL be listed, so that the
visitor's page can say it is not ready yet.

#### Scenario: File with a wrap

- **WHEN** a visitor in a link session asks for the keys of a file the link covers
- **THEN** the answer carries the wrap made for the link and no member's private material

#### Scenario: File outside the link

- **WHEN** the visitor asks for a file of another folder of the room
- **THEN** the answer is 403

### Requirement: A visitor of a link reads only

A visitor of a link of a private item SHALL get the read right on the items the link covers and no other: no upload, delete,
move, copy into the room, rename, sharing or members. `openEditFile` SHALL answer the view mode, and a save callback for
such a session MUST be refused as an unencrypted save is.

#### Scenario: Opening a document

- **WHEN** a visitor of a link opens an encrypted document
- **THEN** the configuration is for viewing and keeps the public document address

#### Scenario: Upload through a link

- **WHEN** a visitor of a link starts an upload into the folder the link covers
- **THEN** the answer is 403

### Requirement: The secret of a link is wrapped for those who manage links

The link operations SHALL accept `linkSecretWraps` for the room owner and room managers only, store them, and answer a wrap
only to the user it was made for. The wraps of a user who stops managing links SHALL be deleted; the links stay.
`getRoomLinks` SHALL name the managers who have no wrap of a link's secret.

#### Scenario: Wrap read by its manager

- **WHEN** a room manager reads the links of the room
- **THEN** each link carries the wrap of its secret made for that manager and no other user's wrap

#### Scenario: Wrap for somebody who does not manage links

- **WHEN** `linkSecretWraps` names an Editor of the room or a non-member
- **THEN** the answer is 400 and nothing is stored

#### Scenario: Manager loses the role

- **WHEN** a room manager is removed from the room or given a lower role
- **THEN** the wraps made for that user are deleted and the links of the room keep working

#### Scenario: New manager

- **WHEN** a member becomes a room manager
- **THEN** `getRoomLinks` names that member among the managers without a wrap until another manager adds one

### Requirement: Replacing the password of a link of a private item

A user who manages links SHALL replace `linkPrivateKeyEnc` and `linkProofHash` of a link, with `passwordRequired`. The link
keeps its id, `linkPublicKey` and its wraps, and the earlier proof MUST be refused from then on. A request that changes
`linkPublicKey` MUST be refused with 400.

#### Scenario: Password replaced

- **WHEN** a manager sends a new sealed key and proof hash for a link
- **THEN** the earlier proof is answered as a wrong one and the new one opens the link session

#### Scenario: Public key in the same request

- **WHEN** the request carries another `linkPublicKey`
- **THEN** the answer is 400 and nothing changes

### Requirement: Revoking a link deletes its key material

Revoking a link of a private item SHALL delete its `linkPrivateKeyEnc`, `linkProofHash`, the wraps made for it and the
wraps of its secret, and end the sessions opened with it.

#### Scenario: Link revoked

- **WHEN** a room manager revokes a link and a visitor of it asks for a file key
- **THEN** the store holds no material of the link and the visitor is answered as for an invalid link

### Requirement: The portal switches external links of private rooms

A portal setting, on by default, SHALL allow external links of private items; only the portal owner and DocSpace admins
SHALL change it, and `getFilesSettings` SHALL report it. While it is off, setting such a link MUST answer 403 and an existing
one MUST be served as an invalid link; nothing is deleted.

#### Scenario: Setting off

- **WHEN** the setting is off and a room manager sets a link on a private item
- **THEN** the answer is 403

#### Scenario: Changed by somebody else

- **WHEN** a room manager who is not a DocSpace admin changes the setting
- **THEN** the answer is 403

#### Scenario: Setting on again

- **WHEN** the setting is turned on again
- **THEN** the links stored before are served as they were

### Requirement: Links of private items follow the app

While the end-to-end encrypted rooms app (`e2e-rooms`) is off, setting an external link on a private item MUST answer 403
and a link the item holds MUST be served as an invalid link. The links SHALL NOT be deleted and SHALL work again when the app
is turned on.

#### Scenario: App off and on

- **WHEN** the app is turned off and a visitor sends a valid proof, and then the app is turned on
- **THEN** the first answer is as for an invalid link and the link is served after

### Requirement: No link in a private Virtual Data Room that restricts downloading

While the room-wide restriction on downloading and copying is on in a private Virtual Data Room, setting an external link on
an item of the room MUST answer 403, and a link the room holds MUST be served as an invalid link. The links SHALL NOT be
deleted and SHALL work again when the restriction is turned off.

#### Scenario: Link requested with the restriction on

- **WHEN** a room manager sets a link on a file of such a room
- **THEN** the answer is 403

#### Scenario: Restriction turned on and off

- **WHEN** the restriction is turned on in a room that holds a link, and then off
- **THEN** the link is served as invalid while it is on and is served again after

### Requirement: Links of an archived private room are not served

Setting an external link on an archived private room, or on an item of one, MUST answer 403, and a link the room held MUST be
served as an invalid link while it is archived. The links SHALL NOT be deleted and SHALL work again when the room is
restored.

#### Scenario: Visitor of a link of an archived room

- **WHEN** a visitor sends a valid proof for a link of a room that has since been archived
- **THEN** the answer is as for an invalid link

#### Scenario: Room restored

- **WHEN** the room is restored
- **THEN** the link is served again

### Requirement: Links of private items are audited

Creating, changing, revoking and opening a link of a private item SHALL be recorded in the audit trail, the room named as
"Titles in the audit trail" states. An opening is recorded once per link session, not per file read.

#### Scenario: Created and opened

- **WHEN** a manager creates a link and a visitor opens it with a valid proof
- **THEN** both events are on record and neither names the room by its title
