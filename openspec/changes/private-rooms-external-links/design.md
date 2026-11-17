# Design

## Context

See proposal.md for the motivation; the cryptography on the client side is in the client change
`private-rooms-external-links` (design.md, decisions 1–4 and 18).

What the server has today, read at `2029af1663`:

- `FileSharingAceHelper.SetAceObjectAsync` (`products/ASC.Files/Core/Utils/FileSharing.cs`, lines 107–113) refuses every
  link with an access on a private room or on an item located in one; a revocation (`Access == None`) passes. This is
  the code behind `private-rooms` "No links to a private room".
- An external link is a share record of a link subject with `FileShareOptions`: title, expiration, password, deny-download,
  internal flag, use limit. `ExternalShare` (`products/ASC.Files/Core/Core/Security/ExternalShare.cs`) validates the record
  and the password and keeps the visitor's session in a cookie (`CreatePasswordKeyAsync`); `applyExternalSharePassword`
  (`SecurityController.ApplyExternalSharePassword`) counts wrong attempts per link and address.
- `getExternalShareData` and `ExternalLinkHelper.ValidateAsync` answer the link status (Ok, Invalid, Expired,
  RequiredPassword, InvalidPassword, ExternalAccessDenied).
- Wrapped file keys are rows of user id, public key id, file id and wrap (`DbFileKeys`), written by `setEncryptionInfo`
  (`PUT files/{fileId}/access`), which skips recipients who cannot read the file (`private-rooms` "Issuing file keys").
  A link id is a Guid, as a user id is.
- The share records are backed up; the file-key store and the key pairs are not (`private-rooms` "Keys and file keys are
  not backed up", `BackupCoverageTests`).
- A portal-wide switch of external sharing already exists (`PUT files/settings/external`); it keeps applying to links of
  private items.

Existing behaviour the design relies on: `shared-links` "Entities that accept a shared link", "Link count limits", "Roles
that manage links", "A fixed 'who has access' value silently skips the update"; `private-rooms` "Room types that can be
private" (only Custom, Collaboration and Virtual Data rooms are private, so a link to the room itself exists only for a
Custom room), "Titles in the audit trail", "Titles in webhooks", "An archived private room keeps its protection", "The
section follows the app".

## Goals / Non-Goals

**Goals:**

- A visitor views an encrypted file through a link, and the server stores nothing that opens it.
- Ordinary links and the stored share records of ordinary items do not change.
- Every refusal of a private link happens before anything is stored.

**Non-Goals:**

- Comment, review and editing accesses through a link (a later change; the access check is the single place to widen).
- Server-side preview, thumbnails, conversion or full-text indexing of what a link serves.
- A recovery of a lost link secret by the server.

## Decisions

1. **Key material lives in its own store, not in `FileShareOptions`.** A new table keyed by tenant and link id holds
   `linkPublicKey`, `linkPrivateKeyEnc` and `linkProofHash`; a second one holds the wraps of a link's secret per manager
   (tenant, link id, user id, public key id, wrap). Both go on the list of tables a backup skips. `passwordRequired` is
   not key material and goes into `FileShareOptions`. Alternative: the three fields in the options of the share record.
   Rejected: the share records are backed up, so the sealed key would leave the portal in every backup, against "Keys
   and file keys are not backed up".
2. **A wrap for a link is a file-key row with the link id in place of the user id.** `setEncryptionInfo` resolves a
   recipient id first as a member, then as a working link that covers the file (the file, a folder above it, or the
   room); anything else goes to `skippedUserIds`, as today. Revocation deletes the rows by link id. Alternative: a
   separate table of link wraps. Rejected: `getEncryptionAccess`, `getRoomMissingFileKeys` and the deletion on revoke
   would each need a second query for the same shape.
3. **The proof rides on the password path.** `applyExternalSharePassword` takes `proof` instead of `password` for a link
   of a private item. The server compares SHA-256 of the decoded proof with `linkProofHash` in constant time, counts a
   miss in the same `BruteForceLoginManager` counter, and on a hit sets the same session cookie, keyed by the stored proof
   hash so that a new password or a revocation ends the old sessions. Every link of a private item answers
   `RequiredPassword` until the proof is sent, also one with `passwordRequired:false`, so that a known link id serves no
   ciphertext. `getExternalShareData` reports `passwordRequired` and that the link is private, so the visitor's page
   knows whether to ask a person for a password. Alternative: a new pair of anonymous operations. Rejected: they would
   duplicate the status, the counter and the cookie of the password path. The proof needs no salt: it carries 256 random
   bits.
4. **All the gates sit in link validation.** The portal setting, the `e2e-rooms` app, the archive and the room-wide
   download restriction of a private Virtual Data Room are checked in `ExternalShare.ValidateRecordAsync`, which answers
   Invalid, and in the link-setting path, which answers 403. Records are never deleted by a gate, so turning a gate off
   brings the links back as they were.
5. **Refusals come before the first write; the key material is written before the record.** `SetAceObjectAsync`
   validates the access, `denyDownload`, `password`, `passwordRequired`, the expiration, the managing role and the sizes
   of the key material for all link aces of the request before it stores any of them (`ValidatePrivateLinksAsync`). The
   share record and the key material live in two DAOs with contexts of their own, so they are not one transaction:
   the key material is written first, and a link resolves only through its share record, so a key row alone opens
   nothing. If the share record then fails, the key material is put back as it was right before the request wrote it,
   read again under a lock of the link (`SetPrivateLinkAsync`), so a concurrent change of the same link is not undone.
   A revocation deletes the key material, the wraps made for the link and the wraps of its secret in one transaction
   (`ShareLinkKeyDao.DeleteAsync`) first, then the share record; a failure in between leaves a link that opens nothing
   and that a repeated revocation removes. Key material is touched only for an id that a link record of the item proves
   to be its link, and a new link takes only an id that no user, no group and no other link holds: the store keeps the
   file-key wraps of users under their own ids, so a foreign id would delete or overwrite them.
   Alternative: one transaction over both DAOs. Rejected: `SecurityDao.SetShareAsync` opens its own context and
   execution strategy, and the order above leaves no state that opens anything.
6. **What a visitor gets is read-only by construction.** The link's access is fixed to `FileShare.Read`;
   `FileSecurity` grants a link session of a private item reading and downloading of the stored file and nothing else.
   The file handler serves the stored bytes with no conversion. `openEditFile` answers the view mode with the public
   document address (`private-rooms` "Encrypted files are offered on the public address"), and the save callback of such
   a session is refused as an unencrypted save is.
7. **The wraps of a link's secret follow the manager role.** `linkSecretWraps` is accepted on the link-setting operations
   for the owner and current room managers only. The place where a member's access is removed or lowered, which already
   revokes file keys (`FileSharingAceHelper.RevokeFileKeysAsync`), also deletes that user's secret wraps, in the same
   transaction as the access change. `getRoomLinks` returns, per link, the caller's own wrap and the ids of managers
   without one.
8. **A separate portal setting, next to the external sharing switch.** Stored with the files settings of the tenant,
   default on, written by a new admin-only operation beside `PUT files/settings/external` (proposed
   `PUT files/settings/external/private`) and reported by `getFilesSettings`. Alternative: reuse the external sharing
   switch. Rejected: an organisation may want ordinary links and no links out of encrypted rooms.
9. **Audit and events reuse what links have.** The existing link-created, link-updated and link-revoked actions are sent
   for private items, with the room named as "Titles in the audit trail" requires; one new action records an opening,
   once per link session. Socket events and webhooks of links are sent as for ordinary links and fall under "Titles in
   webhooks".
10. **The address is built by the server, the secret is appended by the browser.** `shareLink` and its short address
    come from the link data, as for every link today (`FileSharing.cs`, `FileStorageService.cs`, through
    `IUrlShortener`); no operation shortens an address the caller sends. The short-link store (`OnlyoShortener`) and
    Bitly therefore never see the part after `#`. The change keeps it so and pins it with a test. One gap is closed:
    for an unknown code `UrlShortRewriter` redirects to the portal root (`UrlShortRewriter.cs:63`), and the browser
    carries the secret there, onto a page under the full policy of decision 11. The redirect gets an empty fragment
    (`Location: https://portal/#`): a fragment in `Location`, even an empty one, replaces the one of the request.
    Alternative: answer 404. Rejected: it changes every short link of the portal, not only private ones. Alternative:
    let the client shorten the full address. Rejected: the store would hold the secret, and Bitly would receive it.
11. **The page of a link visitor gets its own Content-Security-Policy.** Today one policy per host is built by
    `CspSettingsHelper.CreateHeaderAsync`: the defaults, the Document Server and storage, plus the domains of the CSP
    settings, plugins (`WebPluginsController`), Docs Cloud, the support chat and the tag manager. A script from any of
    them could read `location.hash`. `CspSettingsHelper` also builds a restricted value (the defaults, the Document
    Server and storage only) and caches it beside the full one; the proxy sends the restricted value on the route of
    the visitor's page. Alternative: only a `<meta>` policy in the page, which the browser enforces together with the
    header. Kept as the client's second line, not instead: it covers only what the page loads after it, and the
    server's tests cannot see it.
    The proxy is `buildtools/config/nginx` with the CSP Lua of the docker entrypoint, outside this repository, and the
    route of the visitor's page is the client's; both are dependencies of this decision.
12. **An entry of a private room still reports no link settings.** `FileSecurity.GetLinksSettings` answers 0 for an item
    of a private room, and `FileEntryDto` shows no link counts there, so today's client offers no link it could not
    create. Only the link-setting path asks for the limit of a link with key material (`privateLink: true`), which is the
    limit of a room of the same type. The counts are reported once the client change lands, in that change or behind
    the portal setting of decision 8.

## Risks / Trade-offs

- [A proof or a sealed key is written to a log] → The request DTO fields are excluded from request logging and from
  exception messages; a test reads the log of a refused request.
- [A visitor's link session survives a revocation or a new password] → The session cookie is derived from the stored
  proof hash (decision 3), so changing or deleting the hash ends it; a test covers both.
- [An ordinary link changes behaviour] → The new validation runs only when the item is in a private room; the existing
  `shared-links` tests run unchanged.
- [A revoke fails halfway] → The key material goes first, so what stays is a share record that opens nothing, and
  repeating the revocation removes it (decision 5).
- [Key material of an item stays after the item is deleted] → `FileDao.DeleteFilesAsync` and
  `FolderDao.DeleteFolderAsync` delete the key material and the secret wraps of the links of the removed entries right
  before their share records, in the same transaction, since the share records are what names those links. Moving an
  item to Trash keeps both: the item and its links come back with a restore.
- [The proof is guessed] → 256 random bits plus the counter of wrong attempts; the hash comparison is constant-time.
- [A script on the visitor's page reads the secret from the address] → The restricted policy of decision 11; the
  client keeps third-party scripts off that page. The server delivers the page's own code, the same trust as for the
  page where a member types a passphrase.
- [A later change shortens an address that carries the secret] → The test of decision 10 fails if a stored short
  address of a private link holds a `#`.
- [Many links multiply the wraps of each upload] → Bounded by `shared-links` "Link count limits".

## Migration Plan

- One migration adds the two tables, and both are declared skipped in the backup. No existing data changes: no private
  item holds a link today. It is generated for MySQL SaaS only (`MigrationContext_Upgrade108`), as the other schema
  changes of the repository are: the PostgreSQL SaaS and the Standalone model snapshots have not followed the schema
  for months (they hold neither `files_file_keys` nor the `ai_` tables), and a migration on them would carry all of that.
- The portal setting defaults to on; a portal that never makes such a link sees no difference.
- Rollback: the release before this one refuses links of private items only when they are set, not when they are
  opened, and knows no proof, so it would serve a stored link without one. A rollback therefore first revokes the links
  of private items (a one-off query on the share records whose item is in a private room), then drops the two tables.

## Open Questions

- The route of the visitor's page (client) and who changes the proxy (`buildtools`) for decision 11.

- Final names of the request and response fields and of the setting's route, agreed with the client change before
  implementation starts; the specs name the proposals.
- Whether `private-rooms-release-gate` 1.13 (a portal setting for private rooms as a whole) ends up as the same switch.
  If it turns private rooms off, links stop by decision 4 either way.
