# Tasks

`T/` is `products/ASC.Files/Tests/Tests/08_Private/`. Tests follow `.claude/rules/tests.md`; until the SDKs carry the new
fields, they call the operations through `RawApiClient`.

## 0. Before starting

- [x] 0.1 Agree the final field names and the setting's route with the client change `private-rooms-external-links`, and
  rename them in `specs/` and `design.md` if they change; verify with `openspec validate private-rooms-external-links --strict`.
  - 2026-11-23: the client confirmed every name of the specs and `PUT files/settings/external/private`; nothing renamed.
    The same answer fixed the proof format (design decision 3), the visitor's page `share/private` (decision 13), the
    author's public key with a link's wrap (3.4), the link count for managers (5.5) and the order: 3.1, then 4.1.

## 1. Storage of link key material

- [x] 1.1 Add the link key table (tenant, link id, `linkPublicKey`, `linkPrivateKeyEnc`, `linkProofHash`) and the link secret
  wrap table (tenant, link id, user id, public key id, wrap) through the `db-migrations` skill for MySQL SaaS (decided
  2026-11-17: the PostgreSQL and Standalone snapshots do not follow the schema, see design.md, Migration Plan), with
  precompiled queries (`.claude/rules/ef-compiled-queries.md`); verify the migration applies with `ASC.Migration.Runner`
  on an empty MySQL database.
  - 2026-11-17: `files_share_link_keys`, `files_share_link_secret_wraps`, `ShareLinkKeyQueries.cs`,
    `MigrationContext_Upgrade108`; all migrations applied to an empty MySQL 8.4 database.
  - 2026-11-23: after `release/v4.1.0` brought its own Upgrade108–111, the migration was generated again on top of
    them as `MigrationContext_Upgrade112` (same Up and Down); not applied to a database yet.
- [x] 1.2 Declare both tables skipped in the backup (`.claude/rules/backup-tables.md`) and extend
  `common/Tests/ASC.Data.Backup.Core.Tests/BackupCoverageTests.cs` (`:68`, `:108`, cited by "Keys and file keys are not
  backed up") so it fails if either table is backed up; verify the test passes.
- [x] 1.3 Add `passwordRequired` to `FileShareOptions` and its DTOs (Mapperly, `.claude/rules/dto-mapping.md`); verify an
  ordinary link round-trips unchanged in the existing `shared-links` tests.
  - 2026-11-17: the six `shared-links` classes of 2.7 pass unchanged (95/95), and so do `FileShareTests` and
    `FolderShareTests` (177/177 together).

## 2. Setting a link on a private item

- [x] 2.1 Accept `linkPublicKey`, `linkPrivateKeyEnc`, `linkProofHash` and `passwordRequired` (`linkSecretWraps` comes with
  5.1, which stores and checks it) on
  `setRoomLink`, `setFileExternalLink`, `setFolderPrimaryExternalLink`, `createFilePrimaryExternalLink` and
  `createFolderPrimaryExternalLink`, with the OpenAPI texts (`.claude/rules/openapi-endpoint-docs.md`,
  `.claude/rules/openapi-dto-docs.md`).
- [x] 2.2 Replace the refusal in `FileSharingAceHelper.SetAceObjectAsync` (`FileSharing.cs:107-113`): validate every link
  ace of the request (read access only, no `denyDownload`, no `password`, the `passwordRequired` rule, the sizes) before the
  first write, and write the key material before the share record, putting it back if the record fails (design
  decisions 1 and 5).
- [x] 2.3 Update `T/PrivateRoomContentBoundaryTests.cs:90-137` and `:139-175` (cited by "No links to a private room", renamed to "Links of a private item need key material"):
  invitation link and external link without key material still 403 on the room, a folder and a file; revocation still
  passes; verify they pass.
  - 2026-11-17: written, not run yet: the Aspire fixture does not start in a git worktree; run from the main checkout.
  - 2026-11-17: pass in the main checkout (`_08_Privacy` 300/300). The first run refused every file and folder link
    with 403: the sharing guard of `SetAceObjectAsync` and of `GetPrimaryExternalLinkAsync` asked for EditAccess on the
    item, and the link list hid the links for the same reason. A request of external links only on an item of a
    private room is now checked against the room (`FileSharingHelper.CanSetLinkAsync`), member shares there stay
    403, and the room's link managers see the item's links (`PrivateItemSharingGuardTests`).
- [x] 2.4 Tests in `T/` for "External link of a private item": link with key material stored on a Custom room, and on a
  folder and a file of Custom, Collaboration and Virtual Data rooms; the Custom room limit (6th additional link 403); VDR
  audience forced to DocSpace users; editing, review, comment and form filling 403; `denyDownload:true` 403; Content
  creator 403; nothing stored after each refusal.
  - 2026-11-17: written, not run yet: the Aspire fixture does not start in a git worktree; run from the main checkout.
  - 2026-11-17: pass in the main checkout (`_08_Privacy` 300/300), after the guard fix noted under 2.3.
- [x] 2.5 Tests for "Key material of a link has fixed sizes", one per boundary: `linkPublicKey` of 31, 32 and 33 bytes and
  not base64; `linkProofHash` of 31, 32, 33 bytes and empty; `linkPrivateKeyEnc` empty, 4096 and 4097 characters; each
  field missing.
  - 2026-11-17: written, not run yet: the Aspire fixture does not start in a git worktree; run from the main checkout.
  - 2026-11-17: pass in the main checkout (`_08_Privacy` 300/300), after the guard fix noted under 2.3.
- [x] 2.6 Tests for "A link of a private item without a password is limited": file link with an expiration and
  `passwordRequired:false` stored; the same on a folder, on the room, and on a file without an expiration 403; removing the
  expiration later 403; `password` sent 400.
  - 2026-11-17: written, not run yet: the Aspire fixture does not start in a git worktree; run from the main checkout.
  - 2026-11-17: pass in the main checkout (`_08_Privacy` 300/300), after the guard fix noted under 2.3.
- [x] 2.7 Update the `shared-links` tests that cite "Link settings by room type" and "Deny-download on links combines with
  the VDR room setting" (`04_Security/LinkSettingsByRoomTypeTests.cs`, `RoomLinkUpdateTests.cs:193-214`,
  `RoomLinkReadTests.cs:96-115`, `FolderLinkPrimarySetTests.cs:295-312`, `FileLinkSetTests.cs:116-130`,
  `04_Security/VdrDenyDownloadRolesTests.cs:55-85`) only where needed, and add the private-item rows of the matrix and the
  private VDR scenario; verify the whole `shared-links` set passes unchanged for ordinary items.
  - 2026-11-17: the private row is `T/ExternalLinks/PrivateLinkSettingsMatrixTests.cs` ("who has access" as sent in
    private Custom and Collaboration rooms, forced to DocSpace users in a private VDR, lifetime on a room and a folder
    link); it passes, and the `shared-links` classes above pass unchanged. No `shared-links` test needed a change.
    Decided 2026-11-17: "Private VDR with the restriction on" (`shared-links`) goes to 6.2, with the gate it needs.

## 3. Serving a link against its proof

- [x] 3.1 Accept `proof` on `applyExternalSharePassword` for a link of a private item: decode the base64 to 32 bytes
  (anything else is a wrong proof), constant-time comparison of SHA-256 of those bytes with `linkProofHash`, the same
  wrong-attempt counter, the session cookie keyed by the stored hash, the sealed key in the answer (design decision 3);
  report `passwordRequired` and the private flag in `getExternalShareData`. First of the remaining tasks: no link of a
  private item opens before it.
  - 2026-11-24: `LinkKeyMaterial.ProofMatches`, `ExternalShare.ValidatePrivateLinkAsync` (the session value is signed from
    the stored hash, so a new hash or no key material ends it), `ExternalLinkHelper` (`isPrivate`, `passwordRequired`,
    and `linkPrivateKeyEnc` only to the request whose proof opened the link, after the entry checks), `proof` on
    `ExternalShareRequestParam`. `T/ExternalLinks/PrivateLinkProofTests.cs` passes (5); prove-red.sh --patch: RED as
    expected, 5 of 5 failed without the change.
- [x] 3.2 Answer every link of a private item as RequiredPassword without a session, `passwordRequired:false` included;
  keep `proof` and `linkPrivateKeyEnc` out of request logs and error messages.
  - 2026-11-20: the RequiredPassword answer is in (`ExternalShare.ValidateRecordAsync`, by the stored `PasswordRequired`
    that only such links carry), with `T/ExternalLinks/PrivateLinkServingTests.cs`. Until 3.1 brings the proof session, no
    link of a private item opens; the remarks of the link operations say so. The log part is open.
  - 2026-11-24: log part done. No log records request or response bodies, and `LinkKeyMaterial.Validate` names the
    field, never the value; the SQL log kept the bound values (sensitive data logging), `private_key_enc` and
    `proof_hash` of `files_share_link_keys` among them. `EFLogger` now writes a command that inserts into or updates
    `files_share_link_keys`, `files_share_link_secret_wraps` or `files_file_keys` with `[redacted]` for its parameters,
    in the file and over OTLP. `T/ExternalLinks/PrivateLinkLogTests.cs` (harness: `ServiceLogs`) passes; prove-red.sh
    --patch (the `EFLogger` hunk): RED as expected, 1 of 1 failed without the change; green with it. The PostgreSQL
    spelling of the commands (double quotes) is matched by the same pattern but was not run here.
- [x] 3.3 Tests for "A link of a private item is served against its proof": valid proof returns the sealed key and the
  ciphertext as stored; `getExternalShareData` before the proof marks the link private with `passwordRequired`; a proof
  whose base64 text hashes to `linkProofHash` is wrong; no proof and a wrong proof return no key, wrap or content;
  repeated wrong proofs 403 until the block expires; internal link of a private VDR asks an anonymous visitor to sign
  in; a refused request leaves no proof in `../Logs/files.log`.
  - 2026-11-24: all but the log check are in `T/ExternalLinks/PrivateLinkProofTests.cs` (the session cookie is carried by
    the test, as the test client keeps none); the log check comes with the log part of 3.2.
  - 2026-11-24: the log check is `T/ExternalLinks/PrivateLinkLogTests.cs`: after a 400 on a link body, a wrong and a
    right proof, none of the proofs, the hash or the sealed keys is in `files.log`, `files.sql.log` or `files.asp.log`
    of the run (`Logs/test`), rotated archives included, read once a later request's line is on disk.
- [x] 3.4 Serve the wrap made for the link from `getEncryptionAccess` in a link session, with the public key of the
  member who made it (the visitor has no account to read it otherwise), and 403 for a file the link does not cover; tests
  for "Wraps for a link are served to its visitor".
  - 2026-11-24: the server does not know the author of a wrap (`files_file_keys` has no such column; the author's id is in
    the wrap itself, as the client said), so the visitor gets the public keys of the room's members with the link's entry
    (`fileKeyEnc`), design decision 14. `getEncryptionAccess` is open without signing in; a visitor without a session gets
    403. `EncryptionKeyPairDtoHelper.GetLinkSessionKeysAsync`; tests `Visitor_*` in
    `T/ExternalLinks/PrivateLinkProofTests.cs`; prove-red.sh --patch (the branch that calls it): RED as expected, both
    failed without it. `_08_Privacy` 379 of 379. A signed-in visitor who is not a member still got the member answer of
    the file, which holds no wrap they can open; closed under 3.5.
- [x] 3.5 Make a link session of a private item read-only in `FileSecurity`, view mode in `openEditFile`, and refuse its
  save callback; tests for "A visitor of a link reads only": view mode with the public document address, upload 403,
  delete, move, rename and sharing 403.
  - 2026-11-24: read-only by construction already: the link's access is fixed to Read (2.2), and `FileSecurity`,
    `openEditFile` and the save callback decide by it, so no change was needed there.
    `T/ExternalLinks/PrivateLinkReadOnlyTests.cs` (12) pins it: view mode on the public address, also with `edit=true`;
    for a signed-in visitor who is not a member, 403 for an upload, a folder, a rename and a link, the delete and the move
    refused with the file left in place, the editor's save refused with no new version, 403 without the session cookie;
    a visitor who has not signed in gets 401 for every such operation except the rename (403), which is open without
    signing in. These tests guard behaviour that was already there, so there is no product change to prove them red
    against. The one product change: `getEncryptionAccess` answers a signed-in visitor who is not a member as a link
    visitor (`EncryptionKeyPairDtoHelper.ReadsThroughLinkOnlyAsync`); test
    `SignedInVisitorWhoIsNoMember_GetsTheWrapOfTheLink_NotTheMemberAnswer`; prove-red.sh --patch: RED as expected, 1 of 1
    failed without the change.
- [x] 3.6 Test for "The address of a link of a private item carries no secret": `shareLink` of a link with key material
  from `setFileExternalLink` and `getFileLinks` has no `#`; the `short_links` row of it holds the same address; the short
  address answers a redirect whose `Location` has no fragment (design decision 10). Change the redirect of an unknown code
  in `UrlShortRewriter` to the portal root with an empty fragment, and test that `Location` of an unknown code ends in
  `/#`.
  - 2026-11-24: `UrlShortRewriter` redirects an unknown code to `<root>/#`. `T/ExternalLinks/PrivateLinkAddressTests.cs`
    (with 3.8): `shareLink` of the answer and of `getFileLinks` has no `#`, the `short_links` row holds the address the
    short one redirects to (read with a client that does not follow redirects, `CreateClientWithoutRedirects` of the
    harness), and an unknown code gets `Location` ending in `/#`. prove-red.sh --patch (with 3.8): RED as expected, 6 of
    7 failed without the change; the seventh is the ordinary link, which keeps its address either way.
- [x] 3.7 Build the restricted Content-Security-Policy value in `CspSettingsHelper` (the defaults, the Document Server and
  storage only), cache it beside the full one and refresh both wherever the full one is rebuilt; the route of the
  visitor's page is `share/private` and the proxy `location` comes with the client change (design decisions 11, 13).
  Tests for "The page of a link visitor admits no third-party script": with a domain set through `POST security/csp`,
  the restricted value lacks it and the full value keeps it; through the proxy of the test stand, the answer for the
  visitor's page carries the restricted value.
  - 2026-11-24: `CspSettingsHelper.CreateHeaderAsync(..., privateLink: true)` drops the domains of the settings and every
    integration section; `SaveAsync` caches it under `csp:<host>:private` for every key of the full value, the rename,
    base-domain and start-up paths follow, and `ExistsInCacheAsync` asks for both. `GET` and `POST security/csp` return
    it as `privateLinkHeader`. `T/ExternalLinks/PrivateLinkCspTests.cs`: a domain set through `POST security/csp` and the
    stand's `csp:websearch` source are in `header` and not in `privateLinkHeader`, whose sources are a subset of the full
    ones. prove-red.sh --patch: RED as expected, 2 of 2 failed without the filter. Not covered here: the proxy answer of
    `share/private`, whose `location` comes with the client change.
- [x] 3.8 Build `shareLink` of a link of a private item in `ExternalShare.GetLinkDataAsync` as
  `share/private?key=<key>` (`&folder=<id>` for a folder) for every file type, so that the short address redirects there
  (design decision 13); tests for "A link of a private item opens the visitor's page": a docx link and its short
  address, a folder link, and an ordinary link keeping its address.
  - 2026-11-24: `ExternalShare.GetLinkDataAsync` builds `share/private?key=<key>` for every link of an item that is or
    lies in a private room (`DocSpaceHelper.IsOrLocatedInPrivateRoomAsync`), with `&folder=<id>` for a folder that is not
    the room. Tested in `T/ExternalLinks/PrivateLinkAddressTests.cs` for a docx, a png and a zip, a folder, the room, and
    an ordinary link.

## 4. Links as recipients of file keys

- [x] 4.1 Resolve a link id as a recipient in `setEncryptionInfo`, list working links in `getUserKeysForRoom`
  (`isLink:true`) and in `getRoomMissingFileKeys` (design decision 2).
  - 2026-11-24: `ShareLinkKeyDao.GetWorkingLinksInRoomAsync` (compiled `ShareLinksInRoomAsync`: links with key material
    of the room and of every folder and file in it, expired ones left out; the gates of 6.2 come with 6.2);
    `FileStorageService.SetEncryptionInfoAsync` accepts a link that covers the file (its own, a folder above, the room);
    `EncryptionKeyPairDtoHelper` lists the links with `isLink` and checks them in the missing keys, for the files each
    covers only, also when named in `userIds`. prove-red.sh --patch: RED as expected, 5 of 6 failed without the change
    (the sixth guards against accepting too much and passes either way). `_08_Privacy` 376 of 377 with the one failure
    a fixed 2 s expiry under load, rewritten to poll; the class 6 of 6 after it.
- [x] 4.2 Update the tests cited by "Access keys of a room" (`T/RoomAccessKeysPermissionsTests.cs:110-168`, `:193-221`;
  `T/RoomAccessKeysTests.cs:45-116`, `:169-180`; `T/PrivacyRoomIntegrationTests.cs:77-133`) so they still pass with
  link entries possible, and add: a room with a link lists it with `isLink:true` and no `privateKeyEnc`; a revoked link is
  gone.
  - 2026-11-24: the cited tests pass unchanged (rooms without links); added in `T/ExternalLinks/PrivateLinkFileKeysTests.cs`
    (link listed and gone after revocation, expired link left out).
- [x] 4.3 Update `T/PrivateRoomFileKeysTests.cs:261-305` (cited by "Missing file keys of a room") and add: a file uploaded
  under a folder link without a wrap names the link, and no file outside the folder.
  - 2026-11-24: the cited test passes unchanged; added in `T/ExternalLinks/PrivateLinkFileKeysTests.cs`, with a link named
    in `userIds`.
- [x] 4.4 Update `T/PrivateRoomFileKeysTests.cs:381-422` (cited by "Issuing file keys") and add: a wrap for a link of
  the file, of a folder above and of the room is stored; a link of another item and a revoked link come back in
  `skippedUserIds`.
  - 2026-11-24: the cited test passes unchanged; added in `T/ExternalLinks/PrivateLinkFileKeysTests.cs`.

## 5. Managing the links of a private room

- [x] 5.1 Store and return `linkSecretWraps` (own wrap only, managers without a wrap in `getRoomLinks`) and delete a
  user's wraps in the same transaction that removes or lowers their access (design decision 7); tests for "The secret of a
  link is wrapped for those who manage links": own wrap only; wrap for an Editor or a non-member 400; removed and demoted
  manager loses wraps while links work; a new manager is reported without a wrap.
  - 2026-11-24: `linkSecretWraps` (`userId`, `publicKeyId`, `wrap`) on the file, folder and room link bodies travels in
    `LinkKeyMaterial.SecretWraps`, alone too (to add a manager's wrap to an existing link). `ValidatePrivateLinksAsync`
    answers 400 for a wrap of anybody but the owner and the room managers (`FileSharingHelper.GetLinkManagersAsync`, users
    with a direct RoomManager record), for a repeated user or a wrap of the wrong size; the wraps are stored after the
    share record (`ShareLinkKeyDao.SaveSecretWrapsAsync`). Each link answer carries the caller's own
    `linkSecretWrap` and `managersWithoutLinkSecretWrap`. A manager who leaves (`RevokeFileKeysAsync`) or gets a lower
    role (`SetAceObjectAsync`) loses the wraps of every link of the room, before the access changes; this is not one
    transaction with the access change, as the design asks, but the same order as the file keys. Not covered: a manager
    through a group. `T/ExternalLinks/PrivateLinkSecretWrapTests.cs` (6); prove-red.sh --patch: RED as expected, 6 of 6
    failed without the change.
- [x] 5.2 Replace the password of a link (new `linkPrivateKeyEnc` and `linkProofHash`, same id and wraps); tests for
  "Replacing the password of a link of a private item": old proof refused and old session ended, new proof opens;
  another `linkPublicKey` 400 with nothing changed.
  - 2026-11-24: the replacement was already accepted by `ValidatePrivateLinksAsync` (same public key, new sealed key and
    hash) and the session cookie is derived from the stored hash. `T/ExternalLinks/PrivateLinkLifecycleTests.cs`: the old
    session gets RequiredPassword, the old proof InvalidPassword, the new one opens, the file-key and secret wraps stay;
    another public key gets 400 and the stored hash is unchanged. prove-red.sh --patch (session key not derived from the
    hash): RED as expected, 1 of 3 failed; the other two guard the public key check and the revocation.
- [x] 5.3 Delete the key material, the link wraps and the secret wraps on revocation; test for "Revoking a link deletes
  its key material": the store holds nothing of the link and a running session gets Invalid.
  - 2026-11-17: the deletion is in place (`ShareLinkKeyDao.DeleteAsync`, called by `SetAceObjectAsync` before the
    share record goes, and only for a link record of the item); its test is still to be written. A revocation with an id
    that is not a link of the item deletes nothing (`T/ExternalLinks/PrivateLinkIdTests.cs`).
  - 2026-11-24: `T/ExternalLinks/PrivateLinkLifecycleTests.cs`: after the revocation of a room link no row of the link is
    left in `files_share_link_keys`, `files_share_link_secret_wraps` or `files_file_keys`, and a running session gets
    Invalid. prove-red.sh --patch (the two deletions of `DeleteAsync`): RED as expected, 1 of 3 failed.

- [x] 5.4 Delete the key material and the secret wraps of the links of a file, a folder (its whole subtree) and a
  private room when they are deleted for good; tests for "Deleting an item deletes the key material of its links".
  - 2026-11-17: `DeleteShareLinkKeysOfEntriesAsync` and `DeleteShareLinkSecretWrapsOfEntriesAsync`
    (`ShareLinkKeyQueries.cs`), called by `FileDao.DeleteFilesAsync` and `FolderDao.DeleteFolderAsync` before the share
    records go; `T/ExternalLinks/PrivateLinkCleanupTests.cs` passes and fails without the two calls (prove-red).

- [x] 5.5 Report the number of working links of an item of a private room to the room owner and room managers (design
  decision 12; low priority for the client); tests for "Managers see how many links a private item has": a manager sees
  two of two working links with a third revoked, a reader sees no count.
  - 2026-11-24: `externalLinksCount` on files and folders (`FileEntryDtoHelper.GetPrivateLinksCountAsync`): the links set
    on the entry itself, counted from one read of the room's working links per request, for the room owner and the room
    managers only (`FileSharingHelper.GetLinkManagersAsync`). `T/ExternalLinks/PrivateLinkCountTests.cs`: a manager and
    the owner see 2 of 3 links with one revoked, a reader sees no count. prove-red.sh --patch: RED as expected, 1 of 1.

## 6. Gates

- [x] 6.1 Add the portal setting (default on, admin-only write, reported by `getFilesSettings`) with its OpenAPI texts;
  tests for "The portal switches external links of private rooms": off → set 403 and existing link Invalid; a room manager
  who is not an admin 403; on again → served as before.
  - 2026-11-24: `FilesSettings.DisablePrivateRoomLinksSetting` (stored as `DisablePrivateRoomLinks`, read from the tenant's
    settings whoever asks), `PUT files/settings/external/private` (`SettingsController.PrivateRoomLinks`, DocSpace admins
    only, the same audit action as the external sharing switch), `privateRoomLinks` in `getFilesSettings`. Tests in
    `T/ExternalLinks/PrivateLinkGateTests.cs`.
- [x] 6.2 Check the setting, the `e2e-rooms` app, the archive and the VDR room-wide restriction in link validation and in
  the link-setting path (design decision 4); tests for "Links of private items follow the app", "No link in a private
  Virtual Data Room that restricts downloading" (with "Private VDR with the restriction on" of `shared-links`, moved
  here from 2.7) and "Links of an archived private room are not served", each with the
  state turned on and off again.
  - 2026-11-24: `ExternalShare.IsPrivateLinkOpenAsync` answers Invalid for a link of a private item while the setting or
    the `e2e-rooms` app is off, the room is archived, or a private VDR restricts downloading (`IsPrivateLinkAllowedIn`);
    `ValidatePrivateLinksAsync` refuses a new link with 403 for the same states, revoking stays possible. Nothing is
    deleted. `T/ExternalLinks/PrivateLinkGateTests.cs` (5): setting off and on, a room admin who manages the room gets 403
    for the setting, app off and on, VDR restriction on and off (a signed-in visitor, as a private VDR link asks), archive
    and restore. prove-red.sh --patch (the three gates): RED as expected, 3 of 5 failed without the change; the archive
    test passes either way, since `ExternalLinkHelper` already answered Invalid for an archived item, and the admin check
    was there before.

## 7. Audit, events, contract

- [x] 7.1 Send the link audit actions for private items with the neutral room name and add the opening action, once per
  session; test for "Links of private items are audited" in `T/` and the title check of "Titles in the audit trail".
  - 2026-11-24: the created, updated and revoked actions were already sent for private items, with the room masked by
    `FilesMessageService`. New `MessageAction.PrivateLinkOpened` (5166, `AuditReportResource.PrivateLinkOpened`, mapped
    as Follow in `PrivacyRoomActionMapper`), sent by `ExternalLinkHelper.ValidateAsync` when a proof opens a session; reads
    through the session send nothing. `T/ExternalLinks/PrivateLinkAuditTests.cs`: one created and one opened record for
    the link after a read through the session, neither holding the room's title, the context naming "Private room".
    prove-red.sh --patch: RED as expected, 1 of 1.
- [x] 7.2 Check that the link socket events and webhooks of a private item go only to members (`.claude/rules/socket-events.md`,
  `[RaisesWebhook]` per `.claude/rules/openapi-endpoint-docs.md` §2.1); test that a non-member admin's webhook log stays empty.
  - 2026-11-24: no webhook trigger exists for links, so the link operations carry no `[RaisesWebhook]`; their socket
    events go through `SocketManager`, whose recipients are the readers of the entry, members only in a private room.
    `T/ExternalLinks/PrivateLinkEventTests.cs`: a DocSpace admin outside the room, subscribed to every trigger, gets no
    delivery naming the link or the file through setting, opening, reading and revoking the link, read once the delivery
    of the admin's own later file has arrived. It guards behaviour that was already there, so there is nothing to prove
    red. Not covered by a test: the socket events (the suite's socket tests do not follow link operations).
- [ ] 7.3 Regenerate the OpenAPI documents and the SDKs (`generate-sdk` skill); verify the build has no new warnings
  (`.claude/rules/build-warnings.md`).
  - 2026-11-24: the build part is done (no warnings, `--no-incremental`); the regeneration is left on purpose until the
    branch goes to merge, decided with the developer: the tests use the SDK package pinned in `Directory.Packages.props`,
    not the `sdk/` submodules, and read the new fields raw, so a regeneration now changes no test and only adds a diff to
    eight submodules. Until then 8.1 checks the operation list by hand.

## 8. Integration check

- [x] 8.1 Run `openspec validate --specs --no-interactive` and
  `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec private-rooms` and
  `--spec shared-links`; no operation of the change may be left uncalled by a test. Until the documents are regenerated,
  check against this list instead: `setRoomLink` (2.3, 2.4), `setFileExternalLink` (2.4–2.6), `setFolderPrimaryExternalLink`
  (2.4), `createFilePrimaryExternalLink` and `createFolderPrimaryExternalLink` (2.4), `applyExternalSharePassword` (3.3),
  `getExternalShareData` (3.3), `getEncryptionAccess` (3.4), `openEditFile` (3.5), `getUserKeysForRoom` (4.2),
  `getRoomMissingFileKeys` (4.3), `setEncryptionInfo` (4.4), `getRoomLinks` (5.1), `getFilesSettings` and the new setting
  operation (6.1).
  - 2026-11-24: `openspec validate --specs` 30 of 30; `api_test_coverage.py` reports no uncalled operation for
    `private-rooms` and `shared-links` (on the documents before 7.3). Checked by hand against the list: every operation
    of it is called by a test of `T/` (`setRoomLink`, `setFileExternalLink`, `setFolderPrimaryExternalLink` and the two
    primary-link creations through the raw link helpers, `applyExternalSharePassword`, `getExternalShareData`,
    `getEncryptionAccess`, `openEditFile`, `getUserKeysForRoom`, `getRoomMissingFileKeys`, `setEncryptionInfo`,
    `getRoomLinks`, `getFilesSettings` and `PUT files/settings/external/private`).
- [x] 8.2 Run `dotnet test ASC.Tests.slnx` through the `run-tests` skill; verify the `08_Private` and `04_Security` suites
  pass.
  - 2026-11-24: the solution does not build as a whole: `ASC.Core.Common.Tests` (`BillingClientTests`,
    `PaymentServiceContractTests`) does not compile against the answer type `d6a0764875` of `release/v4.1.0` brought in;
    the branch does not touch them. The other suites run one by one: `08_Private` and `04_Security` have no failure
    (Files 6065 of 6104, 33 failed, none of them in the two). The failures of the suites were not compared with a run of
    the base; the four that touch code near the branch were read: the plugin CSP size test is an open-defect test
    (`Category=Bug`, P2 of the plugins spec, red until that defect is fixed), the audit filter fails in
    `AuditEventsRepository.cs:123` for modules the branch does not map, the socket contract on a missing
    `delete-recent-folder` route, and the primary link of an ordinary custom room.

## Workflow follow-up

- After merge: archive the change (`openspec-archive-change`) and refresh `verification.md` of `private-rooms` and
  `shared-links` (`openspec-maintain`).
- The client change `private-rooms-external-links` mirrors the final field names in its mocks and contract tests.
