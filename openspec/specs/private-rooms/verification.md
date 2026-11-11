# private-rooms — verification

Revision 2026-11-09 (bb406b409b..6fb013e745)

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement of `spec.md`, with where it was verified. Paths are relative to the repo root (`server/`). `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml` (submodule `baf28cf`, 2026-11-06). Code read at `0ebdbb7c84`. `T/` is
`products/ASC.Files/Tests/Tests/08_Private/`. `Helper` is `products/ASC.Files/Core/Core/Entries/EncryptionKeyPairDto.cs`,
`Controller` is `products/ASC.Files/Server/Api/PrivacyRoomController.cs`, `Login` is
`common/ASC.FederatedLogin/LoginProviders/EncryptionLoginProvider.cs`, `Sharing` is `products/ASC.Files/Core/Utils/FileSharing.cs`,
`Storage` is `products/ASC.Files/Core/Core/FileStorageService.cs`.

Status: **TEST-CONFIRMED** means a test asserts it and its assertion was read (via a digest of the `Should()` lines, with the bodies
read where the name was unclear). **CODE-ONLY** means it was read from the code, the commit or the yaml with no test. No test was run for
this document, so nothing here says that a test passed. Several `[Trait("Bug", ...)]` tests in `T/` still carry comments saying the API
answers 200 (bugs 82544, 82545, 82546, 82551, 82552, 82553, 82554, 82800, 82802, 82803); the assertions and the code (`Controller:82`, `:267`;
`Helper:84-87`, `:103-106`, `:135-151`; `Login:64-67`) say the fixed behaviour, which is what is cited. Those comments are stale.

| Claim | Source | Status |
|---|---|---|
| Creating a key: 201, new id adds a pair | T/SetKeysTests.cs:44-57, :213-229 | TEST-CONFIRMED [run: PASS] |
| Refused material: blank, missing, no body on create | T/SetKeysTests.cs:88-149 | TEST-CONFIRMED [run: PASS] |
| Refused material: partial, blank, no body on rotation | T/ReplaceKeyTests.cs:148-215 | TEST-CONFIRMED [run: PASS] |
| Oversized key 400, other pairs survive, 8192 stored | T/SetKeysTests.cs:153-189; T/ReplaceKeyTests.cs:220-240 | TEST-CONFIRMED [run: PASS] |
| Malformed id 400 | T/SetKeysTests.cs:193-208; T/ReplaceKeyTests.cs:102-112 | TEST-CONFIRMED [run: PASS] |
| Repeated id 409, stored pair unchanged | T/SetKeysTests.cs:62-81 | TEST-CONFIRMED [run: PASS] |
| Rotation: one pair, 404, all-zero id | T/ReplaceKeyTests.cs:44-98, :117-131 | TEST-CONFIRMED [run: PASS] |
| Reading own keys: fields, empty set | T/KeyFieldsAndIsolationTests.cs:46-96 | TEST-CONFIRMED [run: PASS] |
| Delete: 204, others remain, 404, repeat, 400, 405 | T/DeleteKeysTests.cs:45-140 | TEST-CONFIRMED [run: PASS] |
| Keys are personal | T/CrossUserIsolationTests.cs:46-144; T/KeyFieldsAndIsolationTests.cs:100-117 | TEST-CONFIRMED [run: PASS] |
| Guests refused, read empty; anonymous 401; other roles manage own | T/KeyAccessPermissionsTests.cs:54-309 | TEST-CONFIRMED [run: PASS] |
| Sign-in links kept | T/KeysAndLoginLinksTests.cs:51-85 | TEST-CONFIRMED [run: PASS] |
| Old-format key | T/LegacyKeysFormatTests.cs:49-105 | TEST-CONFIRMED [run: PASS] |
| Room access keys: set per member, own private half only | T/RoomAccessKeysPermissionsTests.cs:110-168, :193-221; T/RoomAccessKeysTests.cs:45-116 | TEST-CONFIRMED [run: PASS] |
| Membership change and rotation reach the set | T/PrivacyRoomIntegrationTests.cs:77-133 | TEST-CONFIRMED [run: PASS] |
| Archived private room still answers | T/RoomAccessKeysTests.cs:169-180 | TEST-CONFIRMED [run: PASS] |
| Refusals: non-member, admin 403; no key 403 | T/RoomAccessKeysPermissionsTests.cs:55-102; T/RoomAccessKeysTests.cs:120-142 | TEST-CONFIRMED [run: PASS] |
| Refusals: ordinary 400, Trash and unknown 404 | T/RoomAccessKeysTests.cs:184-231 | TEST-CONFIRMED [run: PASS] |
| Candidates: public half only, left-outs, duplicates | T/RoomCandidateKeysTests.cs:74-156 | TEST-CONFIRMED [run: PASS] |
| Candidates: who may ask, refusals | T/RoomCandidateKeysTests.cs:160-250 | TEST-CONFIRMED [run: PASS] |
| Missing keys: answer shape | T/PrivateRoomFileKeysTests.cs:261-305 | TEST-CONFIRMED [run: PASS] |
| Missing keys: refusals | T/PrivateRoomFileKeysTests.cs:309-375 | TEST-CONFIRMED [run: PASS] |
| Issuing file keys: skipped, history | T/PrivateRoomFileKeysTests.cs:381-422 | TEST-CONFIRMED [run: PASS] |
| File keys of an archived room | T/PrivateRoomContentBoundaryTests.cs:279-291 | TEST-CONFIRMED [run: PASS] |
| New owner must hold keys | T/PrivateRoomFileKeysTests.cs:74-123, :157-168; Storage:5529-5537 | TEST-CONFIRMED [run: PASS] |
| Reassign of a deleted user | T/PrivateRoomFileKeysTests.cs:127-153 | TEST-CONFIRMED [run: PASS] |
| Leaving revokes file keys; audit record; access change keeps | T/PrivateRoomFileKeysTests.cs:172-216 | TEST-CONFIRMED [run: PASS] |
| Revocation does not depend on a client being online: the removal is a server-side API call with no client connected, and the next read already reports the key missing, also in a subfolder (run 2026-11-11) | T/PrivateRoomFileKeysTests.cs:171-204; Sharing:RevokeFileKeysAsync; commit cc60512acd | TEST-CONFIRMED [run: PASS] |
| Clearing access: member and owner | T/PrivateRoomLeaveTests.cs:58-98 | TEST-CONFIRMED [run: PASS] |
| Owner keeps the room and keys | T/PrivateRoomLeaveTests.cs:102-160 | TEST-CONFIRMED [run: PASS] |
| Non-member admin cannot clear; ordinary room can | T/PrivateRoomLeaveTests.cs:176-194 | TEST-CONFIRMED [run: PASS] |
| Deleting a user deletes file keys | T/PrivateRoomFileKeysTests.cs:220-234 | TEST-CONFIRMED [run: PASS] |
| Keys and file keys not backed up | common/Tests/ASC.Data.Backup.Core.Tests/BackupCoverageTests.cs:68, :101 | TEST-CONFIRMED [run: NOT-IN-RUN] |
| Room types that can be private; 403 otherwise | T/PrivateRoomCreateTests.cs:47-60, :67-102 | TEST-CONFIRMED [run: PASS] |
| Empty key set counts as no key | T/PrivateRoomContentRulesTests.cs:129-139; Login:97-105 | TEST-CONFIRMED [run: PASS] |
| Editors without the library: 403 create, 415 open | Storage:DemandPrivateRoomCanBeCreatedAsync; DocumentServiceHelper hunk of b562fc3fb1; yaml openEditFile 415 | CODE-ONLY (unreachable: the test stack's document server serves the library, `docServiceEncryptionSupported` is true, and the stack cannot switch it off) |
| `docServiceEncryptionSupported` true on the test stack | T/PrivateRoomEditorTests.cs:255-266 | TEST-CONFIRMED [run: PASS] |
| Connected storage refused 400 (test skipped unless Nextcloud is configured) | products/ASC.Files/Tests/Tests/03_Rooms/ThirdParty/ThirdPartyPrivateRoomTests.cs:49 | TEST-CONFIRMED [run: PASS] |
| Settings refused, off and rename allowed, template fails | T/PrivateRoomSettingsTests.cs:51-143 | TEST-CONFIRMED [run: PASS] |
| Inviting needs a key; guest always refused | T/PrivacyRoomIntegrationTests.cs:137-165; T/RoomAccessKeysPermissionsTests.cs:172-186; T/PrivacyRoomTest.cs:179-221 | TEST-CONFIRMED [run: PASS] |
| No links | T/PrivateRoomContentBoundaryTests.cs:93-102 | TEST-CONFIRMED [run: PASS] |
| Revoking an existing link stays possible: a revocation (`access: None`) passes the private-room check, invitation and external (run 2026-11-11) | T/PrivateRoomContentBoundaryTests.cs:105-142; Sharing hunk of bd89a24dcd (`Access != None`) | TEST-CONFIRMED [run: PASS]; revoking a link that really exists stays CODE-ONLY: the API cannot put a link on a private room, so the tests name an unknown id (an external one then meets the link limit of 0) |
| No duplicate, no template, `security` flags | T/PrivateRoomContentBoundaryTests.cs:144-179 | TEST-CONFIRMED [run: PASS] |
| Copy across the border 403 | T/PrivacyRoomTest.cs:225-277 | TEST-CONFIRMED [run: PASS] |
| Move across the border and inside | T/PrivateRoomContentBoundaryTests.cs:182-215 | TEST-CONFIRMED [run: PASS] |
| Copy-as refused | T/PrivateRoomContentBoundaryTests.cs:232-277 | TEST-CONFIRMED [run: PASS] |
| Plaintext: createFile and session 403; ordinary room accepts | T/PrivateRoomContentRulesTests.cs:62-91, :119-125 | TEST-CONFIRMED [run: PASS] |
| Generated names | T/PrivateRoomContentRulesTests.cs:99-115; products/ASC.Files/Core/Utils/FileUploader.cs regex of cc60512acd | TEST-CONFIRMED [run: PASS] |
| Abort bound to folder | T/PrivateRoomContentRulesTests.cs:143-157 | TEST-CONFIRMED [run: PASS] |
| Save callback refused and allowed; saveediting | T/PrivateRoomEditorTests.cs:105-187 | TEST-CONFIRMED [run: PASS] |
| Restore, reference data, start filling, form flag | T/PrivateRoomEditorTests.cs:191-237 | TEST-CONFIRMED [run: PASS] |
| Public document address | T/PrivateRoomEditorTests.cs:241-251 | TEST-CONFIRMED [run: PASS] |
| Archived: non-member refused; ordinary unchanged | T/ArchivedPrivateRoomSecurityTests.cs:66-113, :177-193 | TEST-CONFIRMED [run: PASS] |
| Archived: no copy; creator and member rights | T/ArchivedPrivateRoomSecurityTests.cs:117-173; T/PrivateRoomContentBoundaryTests.cs:218-228 | TEST-CONFIRMED [run: PASS] |
| Section split by area and filter | T/PrivateRoomSectionTests.cs:64-85 | TEST-CONFIRMED [run: PASS] |
| Section endpoint, root, browse by id | T/PrivateRoomSectionTests.cs:89-120 | TEST-CONFIRMED [run: PASS] |
| App off | T/PrivateRoomSectionTests.cs:124-137 | TEST-CONFIRMED [run: PASS] |
| Non-member not listed; invited listed (in the section only the DocSpace admin is asserted; the Owner only on the default Active area) | T/PrivateRoomSectionTests.cs:141-165; T/PrivateRoomVisibilityTests.cs:55-91 | TEST-CONFIRMED [run: PASS] |
| Archived in the Archive area | T/PrivateRoomSectionTests.cs:169-178 | TEST-CONFIRMED [run: PASS] |
| New marks | T/PrivateRoomSectionTests.cs:182-201 | TEST-CONFIRMED [run: PASS] |
| Recent 400 | T/PrivateRoomSectionTests.cs:205-213 | TEST-CONFIRMED [run: PASS] |
| Groups of the section | T/PrivateRoomSectionTests.cs:217-238 | TEST-CONFIRMED [run: PASS] |
| Audit trail hides titles, keeps record and user names | web/ASC.Web.Api.Tests/Tests/05_Security/AuditTrail/AuditPrivateRoomTitleTests.cs:85-199 | TEST-CONFIRMED [run: PASS] |
| History keeps titles for members | same file:203-222 | TEST-CONFIRMED [run: NO-LINK] |
| Letters use the neutral title (tags of the action, not rendered pushes) | common/Tests/ASC.Notify.Tests/Invites/PrivateRoomTitleInLettersTests.cs:115-177 | TEST-CONFIRMED [run: PASS] |
| Webhooks: outsider gets nothing | web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks/WebhookPrivateRoomAccessTests.cs:71-109 | TEST-CONFIRMED [run: PASS] |
| Webhook contract: the room-type values of the payloads include the Private rooms section type (d8b46d2b9a); documentation only, no spec change | `sdk/docspace-api-spec/webhooks/docspace-webhooks.yaml` `components.schemas` room-type descriptions | CODE-ONLY (documentation, not stack behaviour; that the committed yaml matches what the generator emits is guarded by common/Tests/ASC.Core.Common.Tests/Webhooks/WebhookContractTests.cs, not run here) |

## Differences between the yaml and the server

| Operation | yaml | Server |
|---|---|---|
| deleteKeys | No 405 | A call without an id reaches the collection route, which has no DELETE: 405 (T/DeleteKeysTests.cs:90-98). Framework behaviour, not a contract promise |
| getRoomCandidateKeys, getRoomMissingFileKeys | "owner or a room manager" | Decided by the right to change access, which also applies the archived refusal; matches, stated more narrowly in the yaml |
| getPrivateRoomsFolder | Tagged Files / Folders, not Rooms / Privacy room | Named here because this spec describes the section; another spec may own the tag |

No other difference was found in the seven operations of the tag. Their status codes in the yaml match the `[SwaggerResponse]`s and the tests.

## Left out

All seven operations of `Rooms / Privacy room` are in requirements. These facts are left out of `spec.md`:

| What | Why |
|---|---|
| The section as default start page; used space staying in the Rooms counter; private room's group link kept but not shown in a Rooms group | From commit c866c055e1 only, not read in code or tests |
| The 30-second memory of an unresponsive document server; the development setting that assumes editor support | Configuration detail, not API behaviour |
| Dropping an editor session when a member is removed; socket recipients; the hidden submit button of a form | The commits say the test stack cannot observe them; not checked in code |
| Audit report and history report outputs | Commit b562fc3fb1 says no test of their own; the API events are covered |
| How wrapped keys are bound to ids | Client behaviour (see PR4) |

## Suspected defects

| # | Where | What the server does | Evidence | Evidence kind |
|---|---|---|---|---|
| PR1 | Deleting a subfolder of a private room | It still goes to Trash | Commit c866c055e1 "Not addressed here" | per commit message |
| PR2 | Favorites | A private room or its subfolder can be added to Favorites | Commit c866c055e1 "Not addressed here" | per commit message |
| PR3 | Audit trail | Events written before b562fc3fb1 keep showing a private room's titles, because the list of titles to hide is stored with new events only | Commit b562fc3fb1; `common/services/ASC.AuditTrail/Models/Mappings/PrivateTitleMask.cs` | per commit message, code reading of the mask |
| PR4 | Backup and restore | Key pairs and file keys are skipped, and a module restore assigns new user and file ids, so a restored private room's content cannot be opened. Nothing in what was read warns about this | BackupCoverageTests.cs:68, :101; notes of 2026-11-05 from code reading, no live restore | red by code reading |
| PR5 | Per-user security check | A check on one entry instance reuses the share record of the previous user. Callers in the private-room paths reset it, and the core is unchanged, so another caller checking several users on one instance answers for the wrong one | Commits 1259577f63, 20262fdcb5 | per commit message |
| PR6 | Letters | The neutral title exists in the default culture only, so a letter in another language names the room in that default language | Commit 1259577f63 | per commit message |

## Not verified

- Whether a group can be invited to a private room. `Sharing:270-276` checks keys through a user lookup of the subject id, which suggests a refusal, and `Helper:269` and the revocation skip group subjects. If a group can be a member, its people would get 403 on `getUserKeysForRoom` and keep their file keys on removal. No test covers it.
- `searchArea=Any` for a non-member administrator: the split accepts any room for that area (`products/ASC.Files/Core/Core/VirtualRooms/SearchArea.cs:90-98`), and no test asserts the listing. The same goes for the Archive area listing to a non-member administrator.
- `getUserKeysForRoom` with a subfolder id: the code has no room check there, unlike `candidates` and `missing` (`Helper:260` against `Helper:381`). No test.
- `setEncryptionInfo` refusals (403 for missing rights and for a file outside a private room): read in the yaml only.

## Points for other specs

- `room-types` says room groups take `Active` or `Forms` and that a private room can be grouped: the area `PrivateRooms` now exists, and a private room in an `Active` group answers 400 (T/PrivateRoomSectionTests.cs:217-238).
- `openspec/deviations.md` R1 (bug 82956, private room listed to a non-member) is marked OPEN. T/PrivateRoomVisibilityTests.cs now reads as satisfied by the section split, but that test calls the default Active area, which leaves every private room out for anyone.
