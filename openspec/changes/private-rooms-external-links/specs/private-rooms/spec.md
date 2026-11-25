# Spec Delta: private-rooms

Field names (`linkPublicKey`, `linkPrivateKeyEnc`, `linkProofHash`, `passwordRequired`, `proof`, `isLink`,
`linkSecretWraps`) and the setting's route (`PUT files/settings/external/private`) were agreed with the client change on
2026-11-23.

## RENAMED Requirements

- FROM: `### Requirement: No links to a private room`
- TO: `### Requirement: Links of a private item need key material`

The old name says no link exists; after this change a link with key material does.

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

### Requirement: Links of a private item need key material

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

- **WHEN** a private Custom room already has five additional links and another is requested
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

### Requirement: Links of an item of a private room belong to those who manage the room's links

The links of a folder or a file of a private room SHALL be set, revoked and listed by whoever manages the links of the
room (its owner and room managers), since nobody manages the access of such an item itself. Sharing the item with a user
or a group MUST stay refused with 403, also when the request carries a link as well.

#### Scenario: Sharing an item of a private room with a member

- **WHEN** the room owner shares a file or a folder of a private room with a user
- **THEN** the answer is 403 and nothing is stored

#### Scenario: Revoking a link of an item

- **WHEN** the room owner revokes a link of a file or a folder of a private room, and a Content creator revokes another
- **THEN** the owner's revocation removes the link, and the Content creator's is refused with 403, leaving the link

#### Scenario: Links of an item listed to those who manage the room's links

- **WHEN** the room owner, a reader and a Content creator of a private room list the links of a file or a folder that
  has a link
- **THEN** the owner gets the link, and the reader and the Content creator get an empty list

### Requirement: A link of a private item takes an id of its own

A new link of a private item SHALL take a `linkId` that no user, no group and no other link holds; any other id MUST be
refused with 400, storing nothing and changing nothing of its holder. A revocation SHALL touch the key material of an id
only when that id is a link of the item it names.

#### Scenario: New link with the id of a user

- **WHEN** the room owner sets a new link on a file of a private room with `linkId` set to a member's id
- **THEN** the answer is 400, no link is stored and the member keeps every key of the room's files

#### Scenario: New link with the id of another item's link

- **WHEN** the room owner sets a new link on a file with `linkId` set to the id of a link of another file
- **THEN** the answer is 400 and the other link keeps its key material

#### Scenario: Revoking an id that is not a link of the item

- **WHEN** the room owner revokes, on a folder or a file of a private room, the id of a member or of another item's link
- **THEN** the member keeps every key of the room's files and the other link keeps its key material

### Requirement: Deleting an item deletes the key material of its links

Deleting a file, a folder or a private room for good SHALL delete the key material and the wraps of the secrets of the
links of every file and folder it removes, the room itself included, together with their share records.

#### Scenario: File deleted

- **WHEN** a file of a private room that has a link is deleted for good
- **THEN** the store holds no material of that link, and its id is free for a new link

#### Scenario: Folder deleted

- **WHEN** a folder of a private room that has a link, and holds a file that has another, is deleted for good
- **THEN** the store holds no material of either link

#### Scenario: Room deleted

- **WHEN** a private room that has a link, and holds a file that has another, is deleted
- **THEN** the store holds no material of either link

### Requirement: Key material of a link has fixed sizes

`linkPublicKey` and `linkProofHash` SHALL each be canonical base64 of exactly 32 bytes; `linkPrivateKeyEnc` SHALL be a
non-empty string of at most 4096 characters. An empty, malformed or oversized value, or one left out while another
of the three is sent, MUST be refused with 400, storing nothing. A new link with none of them gets 403.

#### Scenario: Public key of another length

- **WHEN** `linkPublicKey` decodes to 31 or to 33 bytes
- **THEN** the answer is 400

#### Scenario: Public key that is not base64

- **WHEN** `linkPublicKey` is not valid base64
- **THEN** the answer is 400

#### Scenario: Value padded with white space

- **WHEN** `linkPublicKey` or `linkProofHash` is base64 of 32 bytes with spaces before or after it
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

`applyExternalSharePassword` called with `proof`, the base64 of 32 bytes, SHALL open the link session and answer
`linkPrivateKeyEnc` only when SHA-256 of those 32 bytes equals `linkProofHash`. Without that session
`getExternalShareData`, `getEncryptionAccess` and the file content MUST answer as for a link that requires a password,
and `getExternalShareData` SHALL mark the link as private. Wrong proofs SHALL be counted and blocked as wrong passwords.

#### Scenario: Valid proof

- **WHEN** a visitor sends the proof of a link that is neither revoked nor expired
- **THEN** the answer carries `linkPrivateKeyEnc`, and the files the link covers are served as stored, without conversion

#### Scenario: Missing or wrong proof

- **WHEN** a request names the link without a proof, or with a wrong one
- **THEN** the answer carries no sealed key, no wrap and no content

#### Scenario: Visitor's page reads the link before the proof

- **WHEN** `getExternalShareData` is called for a link of a private item without a link session
- **THEN** the status is RequiredPassword, and the answer marks the link as private and carries `passwordRequired`

#### Scenario: Proof sent as the base64 text

- **WHEN** a visitor sends a proof whose base64 text, not the 32 bytes it encodes, hashes to `linkProofHash`
- **THEN** the proof is wrong and no session opens

#### Scenario: Too many wrong proofs

- **WHEN** wrong proofs for one link keep coming from one address
- **THEN** the answer is 403 until the block expires, as for wrong passwords

#### Scenario: Link for DocSpace users only

- **WHEN** a visitor who is not signed in sends a valid proof for a link of a private Virtual Data Room
- **THEN** authentication as a DocSpace user is required first

### Requirement: Wraps for a link are served to its visitor

In a link session of a visitor who is not a member, signed in or not, `getEncryptionAccess` SHALL answer, for a file the
link covers, the wrap made for the link and the public keys of the room's members, among them the member who made the
wrap, and 403 for other files; a member's call keeps its answer. A covered file without a wrap SHALL get the link's entry with no wrap, so that the visitor's page can
say it is not ready yet.

#### Scenario: File with a wrap

- **WHEN** a visitor in a link session asks for the keys of a file the link covers
- **THEN** the answer carries the wrap made for the link and the public keys of the members, the author of the wrap among
  them, and no member's private material

#### Scenario: File without a wrap

- **WHEN** a visitor asks for a covered file before any member wrapped its key for the link
- **THEN** the answer holds the link's entry with no wrap

#### Scenario: File outside the link

- **WHEN** the visitor asks for a file of another folder of the room
- **THEN** the answer is 403

### Requirement: A visitor of a link reads only

A visitor of a link of a private item SHALL get the read right on the items the link covers and no other: no upload, delete,
move, copy into the room, rename, sharing or members. `openEditFile` SHALL answer the view mode, and a save callback for
such a session MUST be refused as an unencrypted save is.

#### Scenario: Opening a document

- **WHEN** a visitor of a link opens an encrypted document, also asking for the edit mode
- **THEN** the configuration is for viewing and keeps the public document address

#### Scenario: Upload through a link

- **WHEN** a visitor of a link starts an upload into the folder the link covers
- **THEN** the answer is 403 for a signed-in visitor who is not a member, and 401 for a visitor who has not signed in,
  as for every operation that changes data and is not open without signing in

#### Scenario: Signed-in visitor without the session

- **WHEN** a signed-in visitor who is not a member opens a covered file without the cookie of the link session
- **THEN** the answer is 403

### Requirement: The address of a link of a private item carries no secret

The server SHALL build `shareLink` of a link of a private item, and its short address, without a fragment. It SHALL NOT
shorten, store or pass to an external shortener an address a caller sends. The short address MUST redirect to the stored
address with no fragment, so that the browser keeps the secret the creator's page appended after `#`. An unknown short
address MUST redirect to the portal root with an empty fragment, so that the browser drops it.

#### Scenario: Link with key material created

- **WHEN** a manager sets a link with key material on a private file
- **THEN** `shareLink` in the answer and in `getFileLinks` has no `#`, and the stored short address holds that same address

#### Scenario: Short address opened

- **WHEN** a visitor requests the short address of such a link
- **THEN** the answer redirects to the stored address of the visitor's page, and its `Location` has no fragment

#### Scenario: Unknown short address

- **WHEN** a request names a short address that `short_links` does not hold, such as a mistyped or cut one
- **THEN** the answer redirects to the portal root with `Location` ending in `/#`, and the secret appended by the sender
  does not reach the portal's page

### Requirement: A link of a private item opens the visitor's page

`shareLink` of a link of a private item SHALL be the address of the visitor's page, `share/private?key=<key>` for a room
or a file and `share/private?key=<key>&folder=<id>` for a folder, whatever the file type; the address of an ordinary
link, its download address and the editor page MUST NOT be used for it. Ordinary links keep their addresses.

#### Scenario: Link of a document

- **WHEN** a manager sets a link with key material on an encrypted docx
- **THEN** `shareLink` is `share/private?key=<key>`, and the short address redirects there, not to the editor

#### Scenario: Link of a folder

- **WHEN** a manager sets a link with key material on a folder of a private room
- **THEN** `shareLink` is `share/private?key=<key>&folder=<id>` with the folder's id

#### Scenario: Ordinary link

- **WHEN** a link is set on an item outside private rooms
- **THEN** its `shareLink` is the address it had before this change

### Requirement: Managers see how many links a private item has

An item of a private room SHALL report the number of its working external links (`externalLinksCount`) to the room owner
and the room managers, and none to other members; a revoked or expired link is not counted. The link settings of such an item keep reporting no
limit outside the link-setting path.

#### Scenario: Item with two links

- **WHEN** a room manager reads a file of a private room that has two working links and one revoked
- **THEN** the file reports two links

#### Scenario: Reader of the room

- **WHEN** a reader of the room reads the same file
- **THEN** the file reports no link count

### Requirement: The page of a link visitor admits no third-party script

The page that opens a link of a private item SHALL be served with a Content-Security-Policy whose script and connect sources
are only the portal, its storage and its Document Server. The domains of the CSP settings, plugins, support chat and tag
manager MUST NOT be in it. Every other page SHALL keep the portal's policy. `GET security/csp`
SHALL return that value as `privateLinkHeader` beside `header`. Until the proxy serves the page with it, the page gets the
portal's policy.

#### Scenario: Portal with extra domains

- **WHEN** the portal's CSP settings list a domain and a visitor loads the page of a link of a private item
- **THEN** the Content-Security-Policy of that answer does not list the domain

#### Scenario: Other pages

- **WHEN** the same portal serves any other page
- **THEN** its Content-Security-Policy lists the domain as before

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
SHALL change it (`PUT files/settings/external/private`), and `getFilesSettings` SHALL report it as `privateRoomLinks`. While it is off, setting such a link MUST answer 403 and an existing
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
"Titles in the audit trail" states. An opening (`PrivateLinkOpened`) is recorded once per link session, not per file read.

#### Scenario: Created and opened

- **WHEN** a manager creates a link and a visitor opens it with a valid proof
- **THEN** both events are on record and neither names the room by its title
