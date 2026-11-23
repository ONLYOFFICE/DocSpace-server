# Tasks

`T/` is `products/ASC.Files/Tests/Tests/08_Private/`. Tests follow `.claude/rules/tests.md`; until the SDKs carry the new
fields, they call the operations through `RawApiClient`.

## 0. Before starting

- [ ] 0.1 Agree the final field names and the setting's route with the client change `private-rooms-external-links`, and
  rename them in `specs/` and `design.md` if they change; verify with `openspec validate private-rooms-external-links --strict`.

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

- [ ] 3.1 Accept `proof` on `applyExternalSharePassword` for a link of a private item: constant-time comparison of
  SHA-256 with `linkProofHash`, the same wrong-attempt counter, the session cookie keyed by the stored hash, the sealed key
  in the answer (design decision 3); report `passwordRequired` and the private flag in `getExternalShareData`.
- [ ] 3.2 Answer every link of a private item as RequiredPassword without a session, `passwordRequired:false` included;
  keep `proof` and `linkPrivateKeyEnc` out of request logs and error messages.
  - 2026-11-20: the RequiredPassword answer is in (`ExternalShare.ValidateRecordAsync`, by the stored `PasswordRequired`
    that only such links carry), with `T/ExternalLinks/PrivateLinkServingTests.cs`. Until 3.1 brings the proof session, no
    link of a private item opens; the remarks of the link operations say so. The log part is open.
- [ ] 3.3 Tests for "A link of a private item is served against its proof": valid proof returns the sealed key and the
  ciphertext as stored; no proof and a wrong proof return no key, wrap or content; repeated wrong proofs 403 until the
  block expires; internal link of a private VDR asks an anonymous visitor to sign in; a refused request leaves no proof in
  `../Logs/files.log`.
- [ ] 3.4 Serve the wrap made for the link from `getEncryptionAccess` in a link session, and 403 for a file the link does
  not cover; tests for "Wraps for a link are served to its visitor".
- [ ] 3.5 Make a link session of a private item read-only in `FileSecurity`, view mode in `openEditFile`, and refuse its
  save callback; tests for "A visitor of a link reads only": view mode with the public document address, upload 403,
  delete, move, rename and sharing 403.
- [ ] 3.6 Test for "The address of a link of a private item carries no secret": `shareLink` of a link with key material
  from `setFileExternalLink` and `getFileLinks` has no `#`; the `short_links` row of it holds the same address; the short
  address answers a redirect whose `Location` has no fragment (design decision 10). Change the redirect of an unknown code
  in `UrlShortRewriter` to the portal root with an empty fragment, and test that `Location` of an unknown code ends in
  `/#`.
- [ ] 3.7 Build the restricted Content-Security-Policy value in `CspSettingsHelper` (the defaults, the Document Server and
  storage only), cache it beside the full one and refresh both wherever the full one is rebuilt; agree the route of the
  visitor's page with the client and the proxy change with `buildtools` (design decision 11). Tests for "The page of a
  link visitor admits no third-party script": with a domain set through `POST security/csp`, the restricted value lacks it and the
  full value keeps it; through the proxy of the test stand, the answer for the visitor's page carries the restricted
  value.

## 4. Links as recipients of file keys

- [ ] 4.1 Resolve a link id as a recipient in `setEncryptionInfo`, list working links in `getUserKeysForRoom`
  (`isLink:true`) and in `getRoomMissingFileKeys` (design decision 2).
- [ ] 4.2 Update the tests cited by "Access keys of a room" (`T/RoomAccessKeysPermissionsTests.cs:110-168`, `:193-221`;
  `T/RoomAccessKeysTests.cs:45-116`, `:169-180`; `T/PrivacyRoomIntegrationTests.cs:77-133`) so they still pass with
  link entries possible, and add: a room with a link lists it with `isLink:true` and no `privateKeyEnc`; a revoked link is
  gone.
- [ ] 4.3 Update `T/PrivateRoomFileKeysTests.cs:261-305` (cited by "Missing file keys of a room") and add: a file uploaded
  under a folder link without a wrap names the link, and no file outside the folder.
- [ ] 4.4 Update `T/PrivateRoomFileKeysTests.cs:381-422` (cited by "Issuing file keys") and add: a wrap for a link of
  the file, of a folder above and of the room is stored; a link of another item and a revoked link come back in
  `skippedUserIds`.

## 5. Managing the links of a private room

- [ ] 5.1 Store and return `linkSecretWraps` (own wrap only, managers without a wrap in `getRoomLinks`) and delete a
  user's wraps in the same transaction that removes or lowers their access (design decision 7); tests for "The secret of a
  link is wrapped for those who manage links": own wrap only; wrap for an Editor or a non-member 400; removed and demoted
  manager loses wraps while links work; a new manager is reported without a wrap.
- [ ] 5.2 Replace the password of a link (new `linkPrivateKeyEnc` and `linkProofHash`, same id and wraps); tests for
  "Replacing the password of a link of a private item": old proof refused and old session ended, new proof opens;
  another `linkPublicKey` 400 with nothing changed.
- [ ] 5.3 Delete the key material, the link wraps and the secret wraps on revocation; test for "Revoking a link deletes
  its key material": the store holds nothing of the link and a running session gets Invalid.
  - 2026-11-17: the deletion is in place (`ShareLinkKeyDao.DeleteAsync`, called by `SetAceObjectAsync` before the
    share record goes, and only for a link record of the item); its test is still to be written. A revocation with an id
    that is not a link of the item deletes nothing (`T/ExternalLinks/PrivateLinkIdTests.cs`).

- [x] 5.4 Delete the key material and the secret wraps of the links of a file, a folder (its whole subtree) and a
  private room when they are deleted for good; tests for "Deleting an item deletes the key material of its links".
  - 2026-11-17: `DeleteShareLinkKeysOfEntriesAsync` and `DeleteShareLinkSecretWrapsOfEntriesAsync`
    (`ShareLinkKeyQueries.cs`), called by `FileDao.DeleteFilesAsync` and `FolderDao.DeleteFolderAsync` before the share
    records go; `T/ExternalLinks/PrivateLinkCleanupTests.cs` passes and fails without the two calls (prove-red).

## 6. Gates

- [ ] 6.1 Add the portal setting (default on, admin-only write, reported by `getFilesSettings`) with its OpenAPI texts;
  tests for "The portal switches external links of private rooms": off → set 403 and existing link Invalid; a room manager
  who is not an admin 403; on again → served as before.
- [ ] 6.2 Check the setting, the `e2e-rooms` app, the archive and the VDR room-wide restriction in link validation and in
  the link-setting path (design decision 4); tests for "Links of private items follow the app", "No link in a private
  Virtual Data Room that restricts downloading" (with "Private VDR with the restriction on" of `shared-links`, moved
  here from 2.7) and "Links of an archived private room are not served", each with the
  state turned on and off again.

## 7. Audit, events, contract

- [ ] 7.1 Send the link audit actions for private items with the neutral room name and add the opening action, once per
  session; test for "Links of private items are audited" in `T/` and the title check of "Titles in the audit trail".
- [ ] 7.2 Check that the link socket events and webhooks of a private item go only to members (`.claude/rules/socket-events.md`,
  `[RaisesWebhook]` per `.claude/rules/openapi-endpoint-docs.md` §2.1); test that a non-member admin's webhook log stays empty.
- [ ] 7.3 Regenerate the OpenAPI documents and the SDKs (`generate-sdk` skill); verify the build has no new warnings
  (`.claude/rules/build-warnings.md`).

## 8. Integration check

- [ ] 8.1 Run `openspec validate --specs --no-interactive` and
  `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec private-rooms` and
  `--spec shared-links`; no operation of the change may be left uncalled by a test. Until the documents are regenerated,
  check against this list instead: `setRoomLink` (2.3, 2.4), `setFileExternalLink` (2.4–2.6), `setFolderPrimaryExternalLink`
  (2.4), `createFilePrimaryExternalLink` and `createFolderPrimaryExternalLink` (2.4), `applyExternalSharePassword` (3.3),
  `getExternalShareData` (3.3), `getEncryptionAccess` (3.4), `openEditFile` (3.5), `getUserKeysForRoom` (4.2),
  `getRoomMissingFileKeys` (4.3), `setEncryptionInfo` (4.4), `getRoomLinks` (5.1), `getFilesSettings` and the new setting
  operation (6.1).
- [ ] 8.2 Run `dotnet test ASC.Tests.slnx` through the `run-tests` skill; verify the `08_Private` and `04_Security` suites
  pass.

## Workflow follow-up

- After merge: archive the change (`openspec-archive-change`) and refresh `verification.md` of `private-rooms` and
  `shared-links` (`openspec-maintain`).
- The client change `private-rooms-external-links` mirrors the final field names in its mocks and contract tests.
