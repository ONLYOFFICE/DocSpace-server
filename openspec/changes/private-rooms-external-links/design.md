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
5. **Refusals come before the first write.** `SetAceObjectAsync` validates the access, `denyDownload`, `password`,
   `passwordRequired`, the expiration and the sizes of the key material for all link aces of the request before it
   stores any of them, and writes the share record and the key material in one transaction.
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

## Risks / Trade-offs

- [A proof or a sealed key is written to a log] → The request DTO fields are excluded from request logging and from
  exception messages; a test reads the log of a refused request.
- [A visitor's link session survives a revocation or a new password] → The session cookie is derived from the stored
  proof hash (decision 3), so changing or deleting the hash ends it; a test covers both.
- [An ordinary link changes behaviour] → The new validation runs only when the item is in a private room; the existing
  `shared-links` tests run unchanged.
- [A wrap for a link stays after a revoke if the delete fails] → Revocation deletes the share record and the key material
  in one transaction (decision 5); a failure leaves the link working, never half revoked.
- [The proof is guessed] → 256 random bits plus the counter of wrong attempts; the hash comparison is constant-time.
- [Many links multiply the wraps of each upload] → Bounded by `shared-links` "Link count limits".

## Migration Plan

- One migration adds the two tables for MySQL and PostgreSQL, SaaS and Standalone, and both are declared skipped in the
  backup. No existing data changes: no private item holds a link today.
- The portal setting defaults to on; a portal that never makes such a link sees no difference.
- Rollback: the release before this one refuses links of private items only when they are set, not when they are
  opened, and knows no proof, so it would serve a stored link without one. A rollback therefore first revokes the links
  of private items (a one-off query on the share records whose item is in a private room), then drops the two tables.

## Open Questions

- Final names of the request and response fields and of the setting's route, agreed with the client change before
  implementation starts; the specs name the proposals.
- Whether `private-rooms-release-gate` 1.13 (a portal setting for private rooms as a whole) ends up as the same switch.
  If it turns private rooms off, links stop by decision 4 either way.
