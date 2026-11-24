# Tasks

`T/` is `products/ASC.Files/Tests/Tests/08_Private/`. Tests follow `.claude/rules/tests.md`. Line citations are those of
`openspec/specs/private-rooms/verification.md` at `365d87fe60`.

## 0. Before starting

- [x] 0.1 Agree the status code for disabling a private room's owner and the name of the `getRemoveProgress` field.
  - 2026-11-20: 403 (design decision 13) and `keptPrivateRoomIds` (decision 7), written into `specs/` and `design.md`;
    `openspec validate private-rooms-server-followups --strict --no-interactive` passes.

## 1. Inviting, refusals and order of writes

- [x] 1.1 Add `EncryptionKeyNotSetException : InvalidOperationException` and throw it from every key check of
  `EncryptionLoginProvider` (design decision 2); verify `error.type` in a 403 body.
  - Run 2026-11-20: `PrivateRoomMembershipKeyTests.KeylessInvitee_IsRefusedWithATypeOfItsOwn` reads `error.type` of both 403 bodies; passes.
- [x] 1.2 Split `SetAceObjectAsync` into a validation pass and the write loop (decision 3): the group rule (decision 4)
  and the first-access key check (decision 1) for every ace before the first write; skip the previous owner's ace of
  `ChangeOwnerAsync`.
- [x] 1.3 Revoke the leavers' file keys before the first ace is written, also in `removeSecurityInfo` and `RemoveAceAsync`
  (decision 5).
- [x] 1.4 Update the tests cited by "Inviting requires a key" (`T/PrivacyRoomIntegrationTests.cs:137-165`,
  `T/RoomAccessKeysPermissionsTests.cs:172-186`, `T/PrivacyRoomTest.cs:179-221`) and add the scenarios "Keyless member"
  and "Keyless previous owner"; verify they pass.
  - Run 2026-11-20: the cited tests pass unchanged in the `_08_Privacy` run; "Keyless member" and "Keyless previous owner" are `T/PrivateRoomMembershipKeyTests.cs:86`, `:102`; pass.
- [x] 1.5 Tests for "A missing key has a refusal of its own", "A refused key check stores nothing" (the keyless user
  first and last in the list, and neither member nor socket event, letter, webhook or audit record left) and "Groups are
  not invited to a private room" (invite refused; revoking a group ace still passes).
  - Run 2026-11-20: `T/PrivateRoomMembershipKeyTests.cs:119-175` pass: the type, the mixed list both ways (no member stored) and the group refusal.
  - Run 2026-11-20: `T/PrivateRoomRefusalEffectsTests.cs:102` passes: after a refused list, a granted invitation of another account shows its room on that account's socket, sends its letter and writes one `RoomCreateUser` record; the refused account gets no socket event and no letter, and no second record appears. `setRoomSecurity` raises no webhook (no `[RaisesWebhook]`), so there is none to assert. `:142` passes: a group record planted in `files_security` is removed by `setRoomSecurity` with `None`.
- [x] 1.6 Update the test cited by "Leaving a private room revokes file keys" (`T/PrivateRoomFileKeysTests.cs:172-216`)
  and verify it passes with the new order.
  - Run 2026-11-20: the cited test (now `T/PrivateRoomFileKeysTests.cs:303-350`) passes unchanged.

## 2. Keys of a file and reassignment

- [x] 2.1 List the room owner and only a member creator in `getEncryptionAccess` (decision 6); tests for "Who receives the
  keys of a file" (new owner, creator removed, groups absent), next to `EncryptionInfoAccessTests.cs:44,59,77`.
  - Run 2026-11-20: `T/PrivateRoomFileKeysTests.cs:252`, `:267` (new owner listed, creator who left not listed, owner listed) pass. Groups are dropped as before; no test of its own.
- [x] 2.2 Split private rooms off in `ReassignRoomsAsync` and report the kept ones in `keptPrivateRoomIds` of the removal progress, with its
  OpenAPI property text (decision 7, `.claude/rules/openapi-dto-docs.md`);
  check whether `ReassignProgressItem` moves rooms too and apply the same rule if it does.
  - 2026-11-20: `ReassignProgressItem` moves every folder of its source, rooms included; it now applies the same rule with the recipient, keeps the kept rooms out of its folder and file reassignment and reports them in `getReassignProgress`. The requirement and design decision 7 say so.
- [x] 2.3 Update the test cited by the renamed "A deleted user's private room goes to someone holding a key"
  (`T/PrivateRoomFileKeysTests.cs:127-153`) and add "Nobody holds a key", through `deleteMember` as well as `startRemove`:
  the room keeps the removed account as owner, a member still reads its files, and a DocSpace admin who is a member can
  hand it over with `changeFileOwner` afterwards (if not, record it in design.md, Risks); verify they pass.
  - Run 2026-11-20: `T/PrivateRoomFileKeysTests.cs:153-249` pass: transfer to a key holder, removal by a key holder, and "Nobody holds a key" through `startRemove` and `deleteMember` (owner kept, `keptPrivateRoomIds` names the room, a member reads its files). The owner is read from the database, because the setup writes it there behind the cached room of the Files service.
  - Run 2026-11-20: the first version of the handover test found that a kept room could not be handed over: a DocSpace admin who manages the room and holds the file's key got 403 from `changeFileOwner`, because in a private room only the owner changed the owner. Fixed by tasks 8.3-8.4 (design decision 16); the test `KeptPrivateRoom_IsHandedOverByAMemberAdministrator` now expects the handover.

## 3. Room creation

- [x] 3.1 Move the refusals of a private Public or Form filling room, of `createRoomFromTemplate` with `private:true` and of
  a private AI agent before the save, as 400 (decision 12); OpenAPI responses of `createRoom`, `createRoomFromTemplate` and
  `POST api/2.0/ai/agents` (`.claude/rules/openapi-endpoint-docs.md`).
- [x] 3.2 Update the tests cited by "Room types that can be private" (`T/PrivateRoomCreateTests.cs:47-60`, `:67-102`): 400
  instead of 403, and no room, audit record or `RoomCreated` webhook left; add "Private AI agent" (raw HTTP, the route is not
  in `docspace-backend.yaml`).
  - Run 2026-11-20: `T/PrivateRoomCreateTests.cs:90-118` pass (400, no room, no audit row naming the title); the private agent is `products/ASC.AI/ASC.AI.Tests/Tests/AgentsTests/PrivateAgentTests.cs` (through the internal agents route the AI suite uses), passes.
  - Run 2026-11-20: `T/PrivateRoomCreateTests.cs:146` passes: with a `RoomCreated` subscription (a WireMock target of the Files suite on `127.0.0.2:18091`), a refused private Public room adds no row to `webhooks_logs`, and the granted private room after it adds one.
- [x] 3.3 Update the tests cited by "A private room cannot be duplicated or made a template"
  (`T/PrivateRoomContentBoundaryTests.cs:177-212`) and "Settings that need the content in the clear"
  (`T/PrivateRoomSettingsTests.cs:51-143`, its template scenario now 400); add "Private room from a template".
  - Run 2026-11-20: the cited boundary tests pass unchanged; the template scenario is `T/PrivateRoomSettingsTests.cs:111` (with and without indexing, 400, no room), passes.

## 4. The app gate

- [x] 4.1 Add the gate service and use it in the read filter, `createRoom` and the agent route; replace the checks in
  `FoldersController` (decision 10).
- [x] 4.2 Update the test cited by "The section follows the app" (`T/PrivateRoomSectionTests.cs:124-137`) with "Creating
  while off" and "App on again", and add tests for "Private entries answer as missing while the app is off" (room, folder,
  file, `openEditFile` 404; own keys readable).
  - Run 2026-11-20: the cited test passes unchanged; "Creating while off", "App on again" and the 404s with own keys readable are `T/PrivateRoomContentGatesTests.cs:94-146`, pass. The first run found the room lists and the folder content still served; both are gated now.

## 5. Encrypted content, AI and favourites

- [x] 5.1 Check whether `ReadHistory` gates `getFileHistory`; if it does, leave ReadHistory out of the encrypted-file list
  and correct the requirement "An encrypted file offers no actions that need its content".
  - 2026-11-20: it does not. `GET api/2.0/files/file/{fileId}/log` checks read access only (`HistoryDto.cs:189`);
    ReadHistory gates the version history and the edit diff. ReadHistory stays in the list.
- [x] 5.2 Deny the encrypted-file actions before full access, `UseAi` for private rooms, and refuse `updateFileComment` on
  an encrypted file (decision 9); update the test cited by "Operations that need open content are refused"
  (`T/PrivateRoomEditorTests.cs:191-237`) with "Owner comments a version", and add tests for "An encrypted file offers no
  actions that need its content".
  - Run 2026-11-20: the cited editor tests pass unchanged; "Owner comments a version" and the encrypted-file flags are `T/PrivateRoomContentGatesTests.cs:191-217`, pass.
- [x] 5.3 Pass the file to `GetAIConfigAsync` (decision 14); tests for "Private rooms have no AI" in
  `T/PrivateRoomEditorTests.cs`.
  - Run 2026-11-20: in `T/PrivateRoomContentGatesTests.cs:219-243` instead of the editor tests (room `CanUseAi` false, editor `ai.disabled` true, and an ordinary room as the counterpart); pass.
- [x] 5.4 Exclude private rooms from the Forms areas (decision 8); test for "Form sections hold no private room" (the room
  can no longer be created through the API: seed it, or record the row as CODE-ONLY).
  - Run 2026-11-20: `T/PrivateRoomContentGatesTests.cs:148` seeds the private flag of a Form filling room in the database; passes.
- [x] 5.5 Skip private rooms and their folders in `addFavorites` and in the Favorites listing (decision 11); tests for
  "Private rooms are not favourites", including a mark stored before the rule if the test can seed one.
  - Run 2026-11-20: `T/PrivateRoomContentGatesTests.cs:161-189` pass. The first run showed the folder query by tag did not load room settings, so a marked private room was listed; it loads them now (design decision 11).

## 6. Disabling a user

- [x] 6.1 Add the Files method of decision 13 and call it from `UpdateMember`, `UpdateUserStatus` and `DeleteProfile`;
  the 403 texts of the three actions in their OpenAPI responses.
- [x] 6.2 Tests for "Disabling a user deletes their file keys" (each of the three operations; re-enabled user misses every
  key; revocation record) and "The owner of a private room cannot be disabled" (403, nothing saved in a batch; succeeds
  after `changeFileOwner`).
  - Run 2026-11-20: `T/PrivateRoomDisableTests.cs` passes for `updateUserStatus` and `updateMember` (keys deleted, missing after re-enabling, revocation record; owner refused with 403 until `changeFileOwner`; a batch disables nobody).
  - Run 2026-11-20: `deleteProfile` added to both theories (`T/PrivateRoomDisableTests.cs:152`, `:173`): the removal letter is read from the event bus, its link signs the call; keys deleted, missing after re-enabling, revocation record; the owner of a private room gets 403 until `changeFileOwner`. Pass.

## 8. Disabling outside the API and handing a room over (added 2026-11-20)

- [x] 8.1 Publish `UserDisabledIntegrationEvent` from `UserManager.UpdateUserInfoAsync` when an account becomes disabled,
  handle it in the Files worker with the revocation of task 6.1, and write the revocation record without a request
  (design decision 15).
  - 2026-11-20: the first run showed the worker's revocation record dropped ("Empty Http Request" in `files.worker.log`), and, when the worker got there first, the People action's record lost too; `RevokeFileKeysAsync` now writes it through the overload that needs no request.
- [x] 8.2 Tests for "Disabled by the directory": the announcement of a disabling, and the worker deleting the keys of the
  announced account with a revocation record.
  - Run 2026-11-20: `T/PrivateRoomDisableTests.cs:197` (an `updateUserStatus` disabling is announced with the account id) and `:211` (an event published on the bus by the test makes the worker delete the keys and record it) pass. The LDAP code itself is not run: the stack has no directory.
- [x] 8.3 Allow `ChangeOwner` on a private room to a DocSpace admin who is a member (design decision 16); remarks of
  `changeFileOwner` and the text of `keptPrivateRoomIds`.
- [x] 8.4 Tests for "A member DocSpace admin may hand a private room over" (member viewer hands over, new owner without a
  key 409, administrator and portal owner outside 403) and "Kept room handed over".
  - Run 2026-11-20: `T/PrivateRoomHandOverTests.cs` (4 cases) and `T/PrivateRoomFileKeysTests.cs:258` pass; the whole `_08_Privacy` namespace passes, 357 of 357.

## 7. Specs and close-out

- [x] 7.1 Mark deviation PR2 fixed in `openspec/deviations.md` and update the rows of `verification.md` the tasks above
  touch.
  - 2026-11-20: PR2 moved to "Closed by v4.1.0" as FIXED `a77956ac1c`, with the two Favorites tests of
    `T/PrivateRoomContentGatesTests.cs` (run 2026-11-20, green).
  - 2026-11-20: the moved citations of `openspec/specs/private-rooms/verification.md` are updated, and the rows whose cited tests now assert the new behaviour say so. PR2 is not marked fixed yet: the register records a fix with its commit.
- [x] 7.2 Re-check the touched specs with `openspec validate --specs --no-interactive` and run
  `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec private-rooms`; no operation of
  the change may be left uncalled. The change adds no operationId; `POST api/2.0/ai/agents` is not in the published
  `docspace-backend.yaml` and is called by raw HTTP in the 3.2 test.
  - 2026-11-20: `openspec validate --specs` passes (30 of 30); `api_test_coverage.py --spec private-rooms` reports 40 operations, all called through the SDK, none uncalled.

## 9. Hand-over check fixes (2026-11-20)

- [x] 9.1 Revoke the file keys of a disabled user last in `updateMember`, `updateUserStatus` and `deleteProfile`, in a catch
  of its own (`PrivateRoomAccountHelper.TryRevokeFileKeysOfDisabledAsync`), after the sessions, sockets, audit and webhook;
  a failure is logged and left to the Files worker (design decision 15).
- [x] 9.2 Run the role checks of the write loop (`AvailableRoomAccesses`, `AvailableUserAccesses`) in
  `ValidatePrivateRoomMembersAsync`, in the loop's order, before the leavers' keys are revoked.
  - Run 2026-11-20: `T/PrivateRoomRefusalEffectsTests.cs` (`RefusedRole_RevokesNobodysKeys`: removing one member and giving
    another a role their user type cannot have is 403, the leaver keeps their file keys and their membership) passes.
- [x] 9.3 Bring the texts of the operations the change reaches in line with the code: `createRoom`, `createRoomFromTemplate`,
  `setRoomSecurity`, `setFolderSecurityInfo`, `setSecurityInfo`, `changeFileOwner`, `getRoomsFolder`, `addFavorites`,
  `updateFileComment` and the version, history, lock and filter operations on an encrypted file, the link operations,
  `updateMember`, `deleteMember`, `deleteUsers`, the start and progress of remove and reassign; the DTO texts of the link
  key fields and `keptPrivateRoomIds`; `TaskProgressResponseDto` through a Mapperly mapper; the resource texts.
- [x] 9.4 Tests: the class split into `T/PrivateRoomReassignTests.cs`, presence before absence in the webhook, audit and
  agent checks, filtered by the portal.

## 10. Items of the client document left out (added 2026-11-20)

- [x] 10.1 Client item 1.6: a user made a guest hands their private rooms to the recipient, leaves every private room,
  and loses their file keys and key pair; the type change is refused with 403 before any change when the recipient cannot
  take a room (design decision 17).
  - Run 2026-11-20: `T/PrivateRoomGuestDemotionTests.cs` passes (a member leaves and gets nothing back after being made a
    user again; an owner's room goes to the recipient; a recipient without the file key is refused with 403 and nothing
    changes); prove-red.sh --patch: 3 of 3 failed without the change. People `_02_UserType` 25 of 25.
- [x] 10.2 Client item 8.1: the refusal to disable the owner of a private room carries `error.type`
  `ASC.Files.Core.Helpers.PrivateRoomOwnershipException` (design decision 18).
  - Run 2026-11-20: `T/PrivateRoomDisableTests.cs` reads the type in both owner refusals; prove-red.sh --patch: 3 of 3
    failed with the old exception type.
- [x] 10.3 Client item 2.5: tests that members, access changes, room keys, file keys and the download are refused while
  the app is off.
  - Run 2026-11-20: `T/PrivateRoomContentGatesTests.cs` (`AppOff_MembersKeysAndDownloadAreRefusedToAMember`) passes; each
    call answers 200 before the switch. No product change, so nothing to prove red.
- Client item 1.8 (a failed revocation leaves the member in the room with their keys): the order is in the code
  (decision 5, revocation before the access record), but no test makes the revocation fail. The stand cannot fail one DAO
  call, and there is no decision to move into a unit test: it is the order of two writes. Not proven. The client accepted
  the code reading on 2026-11-23 and asked to record the case left over (task 11.3).

## 11. Client answers of 2026-11-23

- [x] 11.1 Data migration (design decision 19): for every account in the guests group, delete its access records on
  private rooms and on entries inside them, its rows in `files_file_keys` and its `e2e` row in `account_links`; no audit
  record. Check which migration projects it goes into (MySQL SaaS, and whether `migrations/mysql/Standalone` needs it too).
  Test for "Guest made before this requirement": a guest given access to a private room and keys directly in the
  database is out of the room with no keys after the migration runs, and a member who is not a guest keeps both.
  - 2026-11-24: `MigrationContext_Upgrade113` (MySQL SaaS, SQL only, the designer a copy of Upgrade112 since the model
    does not change): the access records of guests on private rooms and on folders and files in them, their
    `files_file_keys` rows and their `e2e` rows of `account_links`; no record of any kind. Not in
    `migrations/mysql/Standalone`: its snapshot does not hold the private room tables (design of private-rooms-external-
    links, Migration Plan). `T/PrivateRoomGuestCleanupMigrationTests.cs` plants a guest's records and runs the migration's
    statements (the Files suite references the migrations project); prove-red.sh --patch: RED as expected, 1 of 1.
- [x] 11.2 Write `PrivacyRoomKeyDeleted` once per deleted key when `LeavePrivateRoomsAsync` deletes the key pair of a new
  guest, after the deletion and in a catch of its own (design decision 20); extend
  `T/PrivateRoomGuestDemotionTests.cs` to read the records, and add a test that `POST`, `PUT` and `DELETE`
  `privacyroom/keys` write `PrivacyRoomKeyCreated`, `PrivacyRoomKeyUpdated` and `PrivacyRoomKeyDeleted` (none has a test
  today).
  - 2026-11-24: `PrivateRoomAccountHelper.LeavePrivateRoomsAsync` reads the key ids before the deletion and writes one
    record per key after it, in a catch of its own, through `SendHeadersMessage` (the type change is a background job, and
    the request overloads record nothing outside a request). `T/PrivateRoomGuestDemotionTests.cs` reads the record;
    `T/PrivacyRoomKeyAuditTests.cs` covers the three key routes. prove-red.sh --patch: RED as expected, 1 of 3 failed
    without the change (the other two are the ownership cases of the class).
- [x] 11.3 Record the case left by client item 1.8 (the access record fails after the file keys were revoked, so the
  member stays in the room without keys) as deviation PR7 in `openspec/deviations.md`.
  - 2026-11-23: row PR7 under "Access and integrity".
