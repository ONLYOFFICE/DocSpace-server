# Proposal

## Why

A private room is reached through membership only: `setRoomLink` and the link operations of its files and folders refuse
every link, because somebody entering by a link holds no key. The product owner decided on 2026-11-16 to allow external
links to encrypted items, with the link kinds and settings the portal already has, while the server still holds no key
that opens the content.

A link becomes one more recipient of file keys. The creator's browser makes a key pair for the link, wraps the file keys
for its public half, and stores the private half sealed under a secret that stays after `#` in the address. The server
never receives that secret, nor the password of such a link.

This change is the first iteration: links that open encrypted items **for viewing only**. Commenting, reviewing and
editing through a link need an encrypted editing session of a visitor in the Document Server and come in a later change.

Source: the client's draft `../client/openspec/changes/private-rooms-external-links/server-change/` (2026-11-16), read
against this repository at `2029af1663`. The client change `private-rooms-external-links` holds the reasons behind each
product decision.

## What Changes

- **BREAKING** `setRoomLink`, `setFileExternalLink`, `setFolderPrimaryExternalLink`, `createFilePrimaryExternalLink` and
  `createFolderPrimaryExternalLink` accept an external link on a private room, folder or file when the request carries
  the link's key material (`linkPublicKey`, `linkPrivateKeyEnc`, `linkProofHash`). A request without it, and every
  invitation link, is still refused with 403.
- A link of a private item carries the read access only in this change; any other access, the form filling access and
  `denyDownload` are refused.
- A link of a private item has a password unless it is a link to one file with an expiration date. The password does not
  reach the server: the server stores `passwordRequired` and the hash of a proof value; a request that sends `password`
  for such a link is refused.
- A visitor receives the sealed key of the link only from `applyExternalSharePassword` called with the right proof, and
  then reads the wraps made for the link and the stored ciphertext in that link session. Wrong proofs count towards the
  same block as wrong link passwords.
- **BREAKING** `getUserKeysForRoom`, `getEncryptionAccess` and `getRoomMissingFileKeys` also cover the working links of
  the item, each entry marked as a link; `setEncryptionInfo` stores a wrap for a link that covers the file.
- The secret of a link is stored wrapped for the room owner and each room manager; `getRoomLinks` and the link reads of a
  file or folder answer the caller's own wrap only. The wraps of a user who stops managing links are deleted; the links
  stay.
- A new password of a link replaces `linkPrivateKeyEnc` and `linkProofHash`; the address, `linkPublicKey` and the wraps
  stay.
- Revoking a link deletes its sealed key, its proof hash, the wraps made for it and the wraps of its secret.
- Links of private items are not served, and cannot be set, while the portal setting for them is off, while the
  end-to-end encrypted rooms app is off, while the room is archived, and in a Virtual Data Room while its room-wide
  restriction on downloading is on. Nothing is deleted in those states.
- A portal setting, on by default, switches external links of private rooms; only the portal owner and DocSpace admins
  change it.
- **BREAKING** The key material of links is left out of backups like the other key material.
- The server builds the address of such a link and its short address without the secret, and shortens no address a
  caller sends, so the short-link store and Bitly never hold the part after `#`. An unknown short address, which
  redirects to the portal root as today, gets an empty fragment, so the browser does not carry a secret there.
- **BREAKING** The address of a link of a private item is the visitor's page `share/private?key=<key>` (with
  `&folder=<id>` for a folder), whatever the file type, instead of the address of an ordinary link.
- The visitor's page learns from `getExternalShareData` that a link is private, and gets with each wrap the public key
  of the member who made it.
- An item of a private room reports the number of its working links to the room owner and room managers.
- The page that opens such a link gets a Content-Security-Policy without the portal's extra domains, plugins, support
  chat and tag manager, so no third-party script can read the secret from the address.
- Creating, changing, revoking and opening such a link are recorded in the audit trail, with the room named as other
  private-room events name it.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `private-rooms`: found by `setRoomLink` ("No links to a private room"), `getUserKeysForRoom` ("Access keys of a room"),
  `getRoomMissingFileKeys` ("Missing file keys of a room"), `setEncryptionInfo` ("Issuing file keys") and the backup
  requirement "Keys and file keys are not backed up". New requirements for the link operations above,
  `applyExternalSharePassword`, `getExternalShareData`, `getRoomLinks`, `getFileLinks`, `getFolderLinks`,
  `getEncryptionAccess`, `openEditFile` and the new portal setting.
- `shared-links`: found by `setRoomLink`, `setFileExternalLink`, `setFolderPrimaryExternalLink`, `getRoomLinks` and the
  deny-download flag ("Link settings by room type", "Deny-download on links combines with the VDR room setting"). The
  matrices of where a link is allowed and how many apply to a private room of the same type unchanged.
- `files-settings`: found by `setFileExternalLink`, `setFolderPrimaryExternalLink`, `createFilePrimaryExternalLink`,
  `createFolderPrimaryExternalLink` and the primary-link reads. Who sets the links of a private item and which `linkId` it
  takes are stated in the `private-rooms` requirements of this change (see "Breaks current behaviour").

## Breaks current behaviour

- `private-rooms`, "No links to a private room", renamed to "Links of a private item need key material". Today every external link on a private item answers 403. After the
  change an external link with key material is stored; a request without it, and every invitation link, still answers
  403.
  - Web client (`../client`): offers no link for private items today, so no existing call changes; the client change
    `private-rooms-external-links` shows the controls only after this lands.
  - SDKs and the OpenAPI document: three optional request fields and `passwordRequired` on the link operations; the link
    fields on the link responses; a `proof` field on `applyExternalSharePassword`; the new setting.
  - `shareLink` of a link of a private item: the visitor's page instead of the download, editor or `rooms/share`
    address. No such link exists before this change, so no stored address changes.
  - Webhooks and socket events: the link events now also occur for private items, under the delivery rule of "Titles in
    webhooks" (no delivery to subscribers outside the room).
  - Backup and restore: see the backup item below.
  - Portal migration and stored data: none, since no private item holds a link today.
- `files-settings`, "Setting an external link of a file", "Setting an external link of a folder or room", "Primary
  external link of a file" and "Primary external link of a folder or room". On an item of a private room, the room owner
  and room managers set and revoke its links, although nobody may share the item itself; a new `linkId` held by a user,
  a group or another link answers 400; the primary-link reads never create a link there. Ordinary items are unchanged.
  The requirements of `private-rooms` added by this change state the private case; `files-settings` is updated when the
  change is archived.
- `private-rooms`, "Access keys of a room", "Missing file keys of a room" and "Issuing file keys". The answers gain
  entries for links, each marked as a link and named by the link id. A client that counts entries as members must skip
  them. Only rooms with a link are affected, and none exists today.
- `private-rooms`, "Keys and file keys are not backed up". The sealed keys, proof hashes and wraps of links join the
  skipped stores. A restored portal keeps the links of private items as records that open nothing.
- `shared-links`, "Link settings by room type". For a private item the deny-download flag is refused instead of stored,
  the password is replaced by `passwordRequired`, and only the read access is offered. Ordinary items are not affected.
- `shared-links`, "Deny-download on links combines with the VDR room setting". In a private Virtual Data Room with the
  room-wide restriction on, a link is refused at creation and an existing one is served as invalid, where an ordinary
  room stores the link and refuses the download. A visitor must receive the ciphertext to see anything, so the
  restriction could not be kept otherwise (product owner, 2026-11-16).

## Deviations

None of the rows of `openspec/deviations.md` is touched.

## Out of scope

- Commenting, reviewing and editing through a link of a private item (a later change, after the Document Server gives a
  visitor an encrypted editing session).
- A visitor who uploads through a link (decided against).
- Links for members, invitation links and embedding on private items.
- Groups and guests as members of a private room.
- The web client and the SDK code; they follow in `../client`.

## Impact

- `products/ASC.Files/Core/Utils/FileSharing.cs` (the refusal of links in a private room, the link settings),
  `products/ASC.Files/Core/Core/Security/ExternalShare.cs` and `products/ASC.Files/Core/Helpers/ExternalLinkHelper.cs`
  (proof instead of password, serving), `products/ASC.Files/Server/Api/SecurityController.cs`, the link and key DTOs,
  `products/ASC.Files/Core/Core/Entries/EncryptionKeyPairDto.cs`, the file-key store and its DAO, the files settings.
- A database migration for the link key material (MySQL SaaS, where the repository keeps its schema changes) and its
  backup declaration.
- `web/ASC.Web.Core/UrlShortRewriter.cs` (the redirect of an unknown short address).
- `products/ASC.Files/Core/Helpers/CspSettingsHelper.cs` (a restricted policy for the visitor's page); outside this
  repository, the `location` of `share/private` in the proxy of `buildtools` and that page itself, both made by the
  client change.
- Field names, the setting's route, the proof format and the visitor's page were agreed with the client on 2026-11-23.
- The OpenAPI documents and the SDKs; the web client after this lands.
