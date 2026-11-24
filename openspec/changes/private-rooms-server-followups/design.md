# Design

## Context

See proposal.md for the motivation. Item numbers in brackets follow the client's document "Server tasks of private rooms"
(2026-11-16). What the server has today, read at `52c8407261`:

- Key check [1.1]: `FileSharingAceHelper.SetAceObjectAsync` (`products/ASC.Files/Core/Utils/FileSharing.cs:292-298`) calls
  `EncryptionLoginProvider.ThrowIfKeysAreNotSetAsync` (`common/ASC.FederatedLogin/LoginProviders/EncryptionLoginProvider.cs:97-105`)
  for every user ace on a private room, `None` included; `selfLeave` skips only the rights guard.
  `FileStorageService.ChangeOwnerAsync` (`:5553-5578`, `beforeOwnerChange: true`) sends the previous owner's ace through
  the same check. A new group ace reaches the check with `Constants.LostUser` and fails as "no encryption key", a side
  effect rather than a rule.
- Refusal [1.2]: the key check throws `InvalidOperationException("The user does not have an encryption key")`, which
  `CustomExceptionHandler` (`common/ASC.Api.Core/Middleware/ResponseWrapper.cs:86-88`) turns into 403 with its message; a
  rights refusal is a `SecurityException`, 403 "Access denied" (`:71-76`). Many other Files refusals are also
  `InvalidOperationException`. The body's `error.type` is the CLR type name; `CommonApiError.FromException` fills it while
  `withStackTrace` stays true, which it does for `InvalidOperationException`.
- Order of writes [1.7]: the ace loop (`FileSharing.cs:150-596`) writes each ace at `:447`, sends sockets at `:458-472` and
  letters at `:484-516`; the audit is written by the wrapper (`FileStorageService.cs:4570-4704`) after the helper returns.
  Only `ValidatePrivateLinksAsync` (`:141-144`) runs before the first write.
- Revocation [1.8]: leavers are collected at `:479-482` and `RevokeFileKeysAsync` (`:705-720`, audit
  `RoomFileKeysRevoked`) runs after the loop (`:602-606`); also from `removeSecurityInfo` (`FileStorageService.cs:4747-4756`)
  and `RemoveAceAsync` (`FileSharing.cs:679-688`).
- Keys of a file [1.3]: `EncryptionKeyPairDto.cs:194-243` through `FileSharing.GetSharedInfoAsync` (`:1225-1290`) lists
  records of the file and its ancestors, drops groups (`:222`), and adds a synthetic ace for `file.CreateBy`
  (`:1276-1290`) without a membership check. The room owner is missing when they hold no record of their own, which an
  owner change removes (`FileSharing.cs:477-478`).
- Reassign [1.4]: `FileStorageService.ReassignRoomsAsync` (`:4149-4162`) -> `FolderDao` (`:1818-1823`) ->
  `FolderQueries.cs:326-330`, an `ExecuteUpdate` of `create_by` with no checks and no audit. The caller is
  `common/ASC.Data.Reassigns/RemoveProgressItem.cs:146`; the new owner is the user who started the job.
- Forms areas [2.1]: `SearchAreaExtensions.MatchesRoom` (`products/ASC.Files/Core/Core/VirtualRooms/SearchArea.cs:90-99`)
  takes any Form filling room for `Forms` and `FormTemplates`.
- Encrypted-file rights [2.2]: the private block of `FileSecurity` (`:1708-1721`) denies Copy, Duplicate, IndexExport,
  UseChat, Vectorization and StartFilling, but `HasFullAccessAsync` (`:2063`, `:3940-3968`) answers true for the room's
  creator first, so the Encrypted check of EditHistory (`:2497-2501`) is never reached for them. `CanUseAi` (`:1364-1369`)
  passes for a private room. `UpdateCommentAsync` (`FileStorageService.cs:2522-2561`) is gated by `CanEditHistoryAsync`.
- App gate [2.5]: only `FoldersController` (`:875`, `:1140-1155`, `:1189-1192`) asks whether `e2e-rooms` is on.
- Favorites [2.6]: `AddToFavoritesAsync` (`FileStorageService.cs:4258-4285`) drops encrypted files (`:4264`) and filters
  folders by read access only (`:4265`); the listing is `EntryManager.cs:488-526`.
- Editor AI [16.1]: `Configuration.GetAIConfigAsync()` (`products/ASC.Files/Core/Services/DocumentService/Configuration.cs:934-949`)
  takes no file; `GetMentionShare` and `GetSubmitForm` (`:908-932`) already check `!file.Encrypted`.
- Room creation [12.8, 12.3]: `CreateRoomAsync` (`FileStorageService.cs:667-701`) and its wrapper (`:1083-1155`) write the
  audit (`:1093-1096`), the webhook (`:1098-1102`) and the primary link (`:1112-1118`) after the save (`:1309`); the
  Public/Form filling refusal comes from the link, after the room exists. `createRoomFromTemplate` with `private:true`
  (`:1004-1081`, `CreateRoomFromTemplateOperation.cs:142-239`) copies the template's unencrypted files into the room; a
  private AI agent (`:703-720`, from `AgentsController.cs:125-145`) is refused by nothing.
- Disabling [8.1]: `UserController` sets the status in `UpdateMember` (`:2254-2260`, save `:2296`), `UpdateUserStatus`
  (`:2344-2435`) and `DeleteProfile` (`:834-869`); `IFileDao.DeleteFileKeysAsync(userId)` is called only from
  `FileStorageService.DeletePersonalDataAsync` (`:3977-3995`).

Existing behaviour the design relies on: `private-rooms` "The new owner must hold the file keys", "The owner always keeps
the room and its keys", "Clearing access on a private room", "Deleting a user deletes their file keys", "Private rooms need
editors that can encrypt" (its 403 for `createRoom`), "Membership decides who sees a private room", "Issuing file keys";
`people-and-groups` "Starting a data removal" (only a disabled user's data is removed) and "Watching and cancelling a data
removal"; `file-sharing` "The Favorites section" (`addFavorites` answers `true` and skips what it cannot take);
`form-filling-workflow` "Forms section and form templates".

## Goals / Non-Goals

**Goals:**

- A member without keys is never stuck: leaving, removal, a role change and an owner change work without a key check.
- A client can tell a key refusal from a rights refusal, and a refused request stores nothing.
- Nobody outside a private room is offered or keeps its file keys.
- Every refusal of a private room, agent or template copy comes before anything is saved, audited or sent.
- With the `e2e-rooms` app off, the server serves no private entry, and turning it on restores everything.

**Non-Goals:**

- Translations of the private rooms root and of letter titles (deviation PR6).
- A portal setting for private rooms as a whole; cleanup of rooms and favourites saved by the old behaviour.
- Making every other `RoleNotAvailable` refusal of the ace loop (`FileSharing.cs:199`, `:210`, `:218`, `:304`, `:312`,
  `:320`) store nothing; only key checks move before the first write.

## Decisions

1. **The key check runs for a first access only.** In the validation pass (decision 3), a user ace on a private room is
   checked when the user holds no access record on the room yet and the new access is not `None`. The previous owner's ace
   of `ChangeOwnerAsync` is not checked. The new owner keeps the existing file-key check ("The new owner must hold the file
   keys"). Alternative: check every access that is not `None`. Rejected: a role change of a keyless member would still be
   refused, and that member could only be removed.
2. **The key refusal is a type of its own.** A new `EncryptionKeyNotSetException : InvalidOperationException` keeps the 403
   mapping and the message, and its full name becomes `error.type`. Every throw of `ThrowIfKeysAreNotSetAsync` uses it
   (room creation, inviting, candidates), so the client matches one value. Alternative: a new field in the error body.
   Rejected: the body is shared by every service; a type is already there.
3. **One validation pass before the first write.** `SetAceObjectAsync` first resolves every ace of the request: the group
   rule (decision 4) and the key check (decision 1) for all of them, then the existing loop writes. Sockets, letters and the
   wrapper's audit run only after a successful pass, so a refused request sends none.
4. **A group is refused by a rule of its own.** On a private room a group ace answers 403 with a message of its own,
   thrown as an `InvalidOperationException` that is not the key type, before the key check. Revoking an existing group ace
   (`None`) still passes, so a group added before the rule can be removed.
5. **Revoke, then remove.** The leavers of a request are known after the validation pass; `RevokeFileKeysAsync` runs for
   them before the first ace is written. A failure throws before any access changes, so nobody leaves while keeping keys.
   The same order applies in `removeSecurityInfo` and `RemoveAceAsync`. The revocation of a member whose removal then
   fails is accepted: they stay a member and can be issued their keys again.
6. **Keys of a file list the room owner and only a member creator.** `GetSharedInfoAsync`, when asked for encryption,
   adds the owner of the file's room when they hold no record, and adds the file's creator only when they hold access to
   the room. Groups stay dropped.
7. **A private room goes to a key holder.** `ReassignRoomsAsync` splits off private rooms: the target is the user who
   started the removal if they hold a key, else the portal owner if they hold one, else the room stays. The kept room ids
   go back to `RemoveProgressItem` and into the progress DTO as `keptPrivateRoomIds`, empty when every room moved. The other rooms move as today.
   A data transfer (`ReassignProgressItem`) moves every folder of its source, rooms included, so it applies the same
   rule with the recipient in place of the user who started the job, and leaves the kept rooms out of its folder and file
   reassignment. Since disabling an owner of a private room is refused (decision 13) and only a disabled user's data is
   removed or transferred (`startRemove`, `startReassign` answer 400 for an active source), this path is reached only
   for owners disabled before the change or by a directory synchronization (decision 15); the client noted on
   2026-11-23 that the list is almost always empty, which is intended. A kept room stays usable: `deleteMember` deletes the
   profile softly (`UserManager.DeleteUserAsync` -> `DbUserService.RemoveUserAsync` without `immediate` marks the account
   removed and keeps its row), so the room keeps that account as its owner, and the members keep their access and keys.
   Alternative: refuse `startRemove` and `deleteMember` while nobody with a key can take the room. Rejected (2026-11-20):
   the user could then be deleted only after someone with a key was made the room's owner, that is, added to the room.
8. **The Forms areas exclude private rooms.** `MatchesRoom` adds `!room.SettingsPrivate` for `Forms` and `FormTemplates`.
9. **Encrypted files are checked before full access.** In `FileSecurity`, an encrypted file answers false for Rename,
   ReadHistory, EditHistory, Lock, CopyLink, CustomFilter and SubmitToFormGallery before `HasFullAccessAsync`, and
   `CanUseAi` answers false for a private room and its content. `UpdateCommentAsync` refuses an encrypted file itself
   (403), so it does not depend on the flag alone.
10. **One gate for the app.** A `PrivateRoomsGate` service (Files core) answers whether `e2e-rooms` is on, cached for the
    request. `FileSecurity`'s read filter drops private entries while it is off, so listings leave them out and direct
    reads answer 404 through the existing not-found paths; `createRoom` and the agent route answer 403, the same code as
    "Private rooms need editors that can encrypt". The `privacyroom/keys` routes do not ask the gate. Records are never
    touched, so turning the app on restores everything.
11. **Favorites skip private rooms.** The folder query by tag (Favorites, Recent) did not load room settings, so a room
    listed there reported no private flag; it loads them now. `AddToFavoritesAsync` drops a private room and any folder whose room is private;
    the listing in `EntryManager` filters the same, so marks made earlier stay stored but are not shown.
12. **Refusals of creation move before the save.** The type check of a private Public or Form filling room, the private
    template copy and the private agent are `ArgumentException`s (400) in the validation block of room creation
    (`FileStorageService.cs:1225-1235`), before the save, the audit and the webhook.
13. **Disabling a user goes through Files.** A Files service method, called by `UpdateMember`, `UpdateUserStatus` and
    `DeleteProfile` before they save the status, answers 403 when any listed user owns a private room (nothing is saved for
    the request), and otherwise deletes the users' file keys with a `RoomFileKeysRevoked` record per room after the status
    is saved. The code is 403, as for the other state refusals of these actions (an account that is not disabled, the
    portal owner); the message names the owner and the number of their private rooms (not the rooms, which the caller
    may not see), agreed 2026-11-20. These three actions are not the
    only way to disable an account: an LDAP synchronization (`LdapOperationJob`, `LdapUserManager`) does it in Web.Api,
    which hosts no Files code, and decision 15 reaches it. The 403 does not apply there: a synchronization follows the
    directory, so the owner of a private room can still end up disabled, and decision 16 lets the room get a new owner.
14. **Editor AI follows the file.** `GetAIConfigAsync(file)` sets `Disabled` when the file is encrypted.
15. **Every disabling announces itself.** `UserManager.UpdateUserInfoAsync`, which every path saves an account through,
    publishes `UserDisabledIntegrationEvent` when the status goes to `Terminated`, after the save and in a catch of its
    own. The Files worker handles it with the same revocation as decision 13 (`UserDisabledIntegrationEventHandler`).
    The three People actions also revoke, as their last step and in a catch of their own, so the keys are usually
    gone when they answer, and the worker then finds nothing left; a failure there is logged and left to the worker,
    so the disabling keeps its answer, sessions reset, audit and webhook. The revocation record is written through the overload of `FilesMessageService` that does not need a request: the worker has none.
    Alternative: call Files from the LDAP code. Rejected: Web.Api references no Files project, and the next path that
    disables an account would be missed again.
16. **A member DocSpace admin hands a private room over.** In a private room the administrator bypass of `FileSecurity`
    is off, and the share-based checks give `ChangeOwner` to nobody, so only the owner could hand the room over, and a
    room whose owner was disabled or removed could not get a new one. `ChangeOwner` is now also true for a DocSpace
    admin who holds a user record of their own in the room, any role. It reveals no content: `changeFileOwner` still
    refuses a new owner without every file key (409). An administrator who is not a member stays refused.

17. **A user made a guest leaves the private rooms (client item 1.6).** `ChangeRoomsOwnerAsync` lists the rooms of the
    Rooms section, which holds no private room, so a guest kept their private rooms, memberships and keys. The type change
    job now checks first that the recipient can take each private room the user owns (a key, and the key of every
    encrypted file; `PrivateRoomAccountHelper.DemandPrivateRoomsCanPassToAsync`, also called by `startUserTypeUpdate` so
    the refusal is a 403 of the call), then hands those rooms over as their owner with the file keys demanded, then takes
    the user out of every private room after revoking their file keys, and deletes their key pair. It runs before the
    room manager roles are lowered. The access records go straight through `FileSecurity.ShareAsync`: the job acts for
    an administrator who need not be a member, and the type change is that administrator's decision.
18. **The ownership refusal has a type of its own.** Disabling the owner of a private room and a type change whose
    recipient cannot take the rooms throw `PrivateRoomOwnershipException` (an `InvalidOperationException`, so 403), and
    `error.type` tells a client the reason, as decision 2 does for a missing key.
19. **Guests made before decision 17 are cleaned up once, by SQL (client answer of 2026-11-23).** A data migration of
    MySQL SaaS deletes, for every account in the guests group, its access records on private rooms and on entries inside
    them, its wrapped file keys and its key pair (the `e2e` row of `account_links`). It writes no audit record, no
    revocation record and no socket event. Alternative: a one-off background job through `LeavePrivateRoomsAsync`, with
    the records of decision 17. Rejected by the product owner: private rooms have not been released, so the data is
    small and a record of each exit is not needed. A guest owns no room, so no room changes owner.
20. **Deleting the key pair of a new guest is audited.** `LeavePrivateRoomsAsync` deletes the pair through
    `EncryptionLoginProvider.DeleteAllKeysAsync`, which writes nothing, while `DELETE privacyroom/keys/{id}` writes
    `PrivacyRoomKeyDeleted`. The job writes the same action once per deleted key, after the deletion, in a catch of its
    own (`failure-paths.md` §3). The cleanup of decision 19 does not, as decided there.

## Risks / Trade-offs

- [Decision 5 revokes keys of a removal that later fails] -> the member stays and lacks keys until a member issues them
  again; `getRoomMissingFileKeys` reports it. Recorded as deviation PR7, as the client asked (2026-11-23).
- [Decision 10 hides private entries in every read while the app is off] -> a member who has an editor open loses it at
  the next request; this is what the app switch is for.
- [Decision 9 turns ReadHistory off for encrypted files] -> the version history of an encrypted file is closed to its
  owner too. `getFileHistory` (the activity log, read by the "Issuing file keys" scenario) checks read access only, so it
  stays open (task 5.1).
- [Decision 7 leaves a private room owned by a removed account] -> it has no working owner until a DocSpace admin who is
  a member hands it over (decision 16); `keptPrivateRoomIds` of `getRemoveProgress` names the room so the administrator
  knows. A room with no member DocSpace admin needs one invited first, by a member who may manage its access.
- [Decision 15 is best effort] -> a lost event leaves the file keys of an account disabled by a directory
  synchronization in place; the three People actions do not depend on it. When the worker pass overlaps the People
  action's own, a room can get two revocation records.
- [Decision 13 refuses a whole batch of `updateUserStatus` for one owner] -> an administrator disabling many accounts gets
  403 for all; the message names the first owner it meets.

## Migration Plan

No schema change. One data migration, decision 19: guests made before decision 17 leave the private rooms and lose
their file keys and key pair. Other existing data under the "Breaks current behaviour" items, kept as the client agreed
on 2026-11-23:

- Private Public or Form filling rooms saved by the old refusal stay; decision 8 keeps them out of the Forms areas, and the
  owner may delete them.
- Favourite marks of private rooms stay stored and are hidden by decision 11.
- Rooms already reassigned to a user without keys stay with that user.
- Disabled users who still hold file keys keep them until they are deleted or re-enabled and removed; the change does not
  backfill.

Rollback is a plain revert: nothing is stored that the old code cannot read. The cleanup of decision 19 is not undone by
it: the guests stay out of the rooms, and their keys are gone.

## Open Questions

None left: the status code, the field name and the two checks of the code are settled in tasks 0.1, 2.2 and 5.1;
decisions 15 and 16 were agreed on 2026-11-20 after task 2.3 found that a kept room could not be handed over; decisions
19 and 20 follow the client's answers of 2026-11-23. Which migration projects the cleanup of decision 19 goes into (a
standalone installation reads `migrations/mysql/Standalone`) is checked when task 11.1 is done.
