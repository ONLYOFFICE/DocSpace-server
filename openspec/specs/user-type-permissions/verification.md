# user-type-permissions — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); Files test paths omit the `products/ASC.Files/Tests/Tests/` prefix, People test paths the
`products/ASC.People/Tests/Tests/` prefix and Web.Api test paths the `web/ASC.Web.Api.Tests/Tests/` prefix.
`yaml` is `sdk/docspace-api-spec/docspace-backend.yaml` (submodule `a0fc3a2`), operations are named by
their `operationId`. Status: **TEST-CONFIRMED** (an integration test asserts it), **CODE-ONLY** (read from
the product code or the OpenAPI document, no test), **LIVE-CONFIRMED** (only the live run of 2026-10-14
recorded in `openspec/deviations.md` asserts it; the logs are gone, the row of `deviations.md` is cited),
**MATRIX-ONLY** (stated by the product matrix "Types and Roles. Access rights", sheet "Types"; no test, code
or live evidence was found — flagged).

Conventions that apply to every row: a refusal is asserted as an `ApiException` with the status code, the
message only where the row says so; "Guest" and "User" are the user types (`EmployeeType` Guest / User),
"Room admin" is `EmployeeType.RoomAdmin`. The message "The role is not available for this user type" is the
resource `FilesCommonResource.resx:331` and is thrown by `products/ASC.Files/Core/Utils/FileSharing.cs:170,
181-189, 206, 266-289`; the AI-project guest tests assert the text (`03_Rooms/AiHub/AiProjectGuestInviteTests.cs:57, 78`),
every other row only the 403. The user-type → room-role table is
`products/ASC.Files/Core/Core/Security/FileSecurity.cs:265-281` (`AvailableUserAccesses`: User and Guest
without Room manager).

Revision 2026-10-25: first verification; the spec was reshaped at the same time (matrices moved into a
lead scenario per requirement, prose split into scenarios; no requirement or scenario renamed, the
scenarios "Inviting needs the Room manager role", "Section matrix by user type", "Room visibility matrix",
"Room action matrix by user type", "Foreign room action matrix", "Archive action matrix by user type",
"Invitation matrix by inviter type", "Type change matrix by caller type", "Disable and delete matrix by
target type", "Accounts matrix by user type", "Sharing matrix by user type" are new containers for text
that already existed).

Revision 2026-11-04 (release/v4.1.0, `d58816bece..3533ad8a89`): no type right changed. The range adds the AI
rooms section, whose rooms a User cannot create either (`03_Rooms/AiHub/AiProjectRoomTests.cs:62-74`) and
whose AI projects refuse Guests (`03_Rooms/AiHub/AiProjectGuestInviteTests.cs`, stated in
`room-role-permissions`); the People and Settings controllers changed only in their webhook declarations.
Line references refreshed: `FileSecurity.cs` role tables `:159-263` / `:265-281`, the `FileSharing.cs`
throws after line 272 moved by +8 (`:283-289` for the user-type check). D3, D4, D7, D9, D10, R1, R2 and R3
are unchanged (verdicts below).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Section availability by user type** | | |
| Root sections: Owner / User / DocSpace admin see "Files", "Rooms", "Trash"; Guest sees "Rooms", "Trash" and not "Files" | `02_Folders/Subfolders/RootFoldersPermissionsTests.cs:53-99` | TEST-CONFIRMED [run: PASS] |
| Guest creating a text / HTML file, uploading or inserting into My documents → 404; Owner / DocSpace admin / Room admin / User succeed | `01_Files/Create/TextFileCreatePermissionsTests.cs:47-113`; `HtmlFileCreatePermissionsTests.cs:46-112`; `02_Folders/Upload/UploadToMyPermissionsTests.cs:46-78`; `InsertToMyPermissionsTests.cs:46-81` | TEST-CONFIRMED [run: PASS] |
| Guest requesting the My documents root itself → 404 | spec scenario; only the root listing (above) is asserted; `yaml` `createFileInMyDocuments` (13451), `createTextFileInMyDocuments` (13625) document 404 for a guest | CODE-ONLY |
| Rooms section open to every type | `03_Rooms/Listing/RoomsFolderVisibilityTests.cs:44-137` (DocSpace admin, Room admin, User, Guest get 200 with content) | TEST-CONFIRMED [run: PASS] |
| Archive section open to every type | only the Owner lists the Archive (`03_Rooms/Archive/RoomArchiveTests.cs:62-64`); the listing is the same `getRoomsFolder` (29480) with another search area, no type check of its own | CODE-ONLY |
| Accounts closed to User and Guest: people list, filters, group list → 403 "Access denied"; open to Room admin | `01_Profiles/GetAllProfilesTests.cs:44-101`; `08_Search/FilterSearchPermissionsTests.cs:63-87, 117-141`; `13_Groups/GroupPermissionGetGroupsListTests.cs:58-87`; `01_Profiles/GetProfileByUserIdTests.cs:57-81`; `GetProfileByEmailTests.cs:115-141` | TEST-CONFIRMED [run: PASS] |
| Portal settings closed to Room admin / User / Guest → 403 "Access denied" | `02_Settings/Greeting/GreetingSavePermissionsTests.cs:61-79`; `02_Settings/IpRestrictions/IpRestrictionsSavePermissionsTests.cs:61-78, 98-116`; `02_Settings/Security/WebItemSecurityWriteTests.cs:45-117`; `ProductAdministratorTests.cs:82-99` | TEST-CONFIRMED [run: PASS] |
| **My documents operations** | | |
| Create, upload, insert by User (and the other three types) | tests of the previous requirement | TEST-CONFIRMED [run: NO-LINK] |
| Move, copy, rename, download, delete by a User in own My documents | the functional suites run as the Owner (`06_Operations/*`, `01_Files/Update/FileRenameTests.cs`); duplicate by the Owner and DocSpace admin on own files `06_Operations/Duplicate/DuplicatePermissionsTests.cs:68-82, 195-210`; the My documents root belongs to its user and no type check exists on these operations | CODE-ONLY |
| **Room visibility by user type** | | |
| Owner lists a room they neither own nor joined | `03_Rooms/RoomsApiTests.cs:1346-1394`; `03_Rooms/Deletion/RoomDeleteFileOpsTests.cs:67-93` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin sees a foreign room (info, pin) | `03_Rooms/Permissions/RoomReadPermissionsTests.cs:43-63`; `RoomPinPermissionsTests.cs:73-88` (the listing itself is asserted only for own rooms, `RoomsFolderVisibilityTests.cs:44-59`) | TEST-CONFIRMED [run: PASS] |
| Room admin sees own rooms and the rooms they joined, nothing else | `RoomsApiTests.cs:1363-1393`; `RoomsFolderVisibilityTests.cs:61-85` | TEST-CONFIRMED [run: PASS] |
| User / Guest see only rooms they joined | `RoomsFolderVisibilityTests.cs:87-137`; `02_Folders/Subfolders/RootFoldersPermissionsTests.cs:171-188` | TEST-CONFIRMED [run: PASS] |
| Same rules in the Archive section | no per-type archive listing test; same listing operation | CODE-ONLY |
| Private room hidden from a non-member Owner / DocSpace admin | `08_Private/PrivateRoomSectionTests.cs:141-153` (the Owner in the `PrivateRooms` area, then the creator); `products/ASC.Files/Core/Core/Security/FileSecurity.cs:3069-3073` (the administrator's view, Owner and DocSpace admin alike). `PrivateRoomVisibilityTests.cs:53-92` no longer proves it: it calls the default Active area, which leaves private rooms out for everyone | TEST-CONFIRMED [run: PASS] (Owner), CODE-ONLY (DocSpace admin; Archive and `Any` areas) |
| **Room creation and management in the Rooms section** | | |
| Create rooms: Owner, DocSpace admin, Room admin succeed; User, Guest → 403 ("You don't have enough permission to create"); a User → 403 for an AI project too | `03_Rooms/Permissions/RoomCreatePermissionsTests.cs:41-107, 182-236`; `03_Rooms/AiHub/AiProjectRoomTests.cs:62-74` | TEST-CONFIRMED [run: PASS] |
| Pin: every invited member of every type pins; non-member User / Guest → 403 | `RoomPinPermissionsTests.cs:50-71, 90-109`; `RoomPinInvitedMemberPermissionsTests.cs:41-59` | TEST-CONFIRMED [run: PASS] |
| View member list: Owner, invited Room admin, invited User; non-members 403 | `RoomShareReadPermissionsTests.cs:46-152` (bugs 81787, 81788); invited Guest / DocSpace admin not asserted separately | TEST-CONFIRMED [run: PASS] |
| View room history: Owner, DocSpace admin, Room admin (Room manager), User (Read); non-members 403 | `02_Folders/History/FolderHistoryPermissionsTests.cs:48-144` | TEST-CONFIRMED [run: PASS] |
| View room info: every invited type and role | `RoomReadPermissionsTests.cs:43-83, 129-147` | TEST-CONFIRMED [run: PASS] |
| Edit rooms: Owner / DocSpace admin on own room, Room admin as Room manager; non-member User / Guest 403, title unchanged | `RoomUpdatePermissionsTests.cs:43-76, 99-140, 166-224` | TEST-CONFIRMED [run: PASS] |
| Invite internal users: Owner / DocSpace admin on own room, Room admin as Room manager; non-member User / Guest 403 | `RoomSharePermissionsTests.cs:43-80, 104-145, 196-217` | TEST-CONFIRMED [run: PASS] |
| Invite external users (by e-mail) by type | no test sends an e-mail invitation into a room; the same `setRoomSecurity` (30843) carries `email` and runs the same Room manager check (`FileSharing.cs:87-104, 273-300`) | CODE-ONLY |
| Assign roles when inviting: Owner per room type | `03_Rooms/Sharing/RoomShareAccessLevelTests.cs:51-116` | TEST-CONFIRMED [run: PASS] |
| Change roles / remove members: Owner; Room admin owner removes a guest | `03_Rooms/Sharing/RoomShareSetTests.cs:68-85, 108-144`; `04_Security/RoomShareTests.cs:1099-1149` (bug 80124) | TEST-CONFIRMED [run: PASS] |
| Archive own room: Owner, DocSpace admin | `RoomArchivePermissionsTests.cs:66-81`; `03_Rooms/Archive/RoomArchiveTests.cs:47-77`; Room admin own room not asserted (`archiveRoom` 30627 demands the room's owner or a portal admin) | TEST-CONFIRMED [run: PASS] |
| Copy own room: Owner | `RoomDeleteFileOpsTests.cs:46-65`; `yaml` `duplicateBatchItems` (22577) | TEST-CONFIRMED [run: PASS] |
| Inviting / role changes / removal need the Room manager role, which a User or Guest cannot hold | `RoomSharePermissionsTests.cs:248-270`; `FileSecurity.cs:265-281`; `deviations.md` matrix row "Users and Guests may invite to a room … 403" | TEST-CONFIRMED [run: PASS] |
| User / Guest who are members cannot invite, member list unchanged | the 403 is asserted for non-members only (`RoomSharePermissionsTests.cs:104-145`); the check is role-based (`FileSharing.cs:101`), a member with any non-manager role fails it; live run `deviations.md` row above | LIVE-CONFIRMED |
| Room admin makes another Room admin a Room manager, who then invites | `04_Security/RoomShareTests.cs:1099-1126` | TEST-CONFIRMED [run: PASS] |
| Room manager role refused for a User / Guest, by the Owner too → 403, member absent | `RoomSharePermissionsTests.cs:248-270`; `RoomShareAccessLevelTests.cs:56-59`; message `FileSharing.cs:283-289` | TEST-CONFIRMED [run: PASS] |
| Room admin hands out any role but Room manager to a User / Guest; a Guest gets no role in an AI project (403, message) | `FileSecurity.cs:265-281` (the check is on the target's type only); no test has a Room admin as the granting actor; AI project: `03_Rooms/AiHub/AiProjectGuestInviteTests.cs:45-79`, `FileSharing.cs:275-281` | CODE-ONLY (Room admin as actor) / TEST-CONFIRMED [run: PASS] (AI project guest) |
| **Actions on rooms owned by someone else** | | |
| Non-member DocSpace admin: view member list, history, info | `RoomShareReadTests.cs:208-221`; `FolderHistoryPermissionsTests.cs:62-76`; `RoomReadPermissionsTests.cs:43-63` | TEST-CONFIRMED [run: PASS] |
| Non-member DocSpace admin pins a foreign room | `RoomPinPermissionsTests.cs:73-88` | TEST-CONFIRMED [run: PASS] |
| Non-member Owner downloads content of a foreign room | `RoomDeleteFileOpsTests.cs:102-134` (bug 83820: duplication copies the file, gated on download) | TEST-CONFIRMED [run: PASS] |
| Non-member DocSpace admin reads the primary link of a Public room; Room admin / User / Guest 403 | `RoomLinkPermissionsTests.cs:62-95, 149-167`; `deviations.md` matrix row "Nobody sees the links of someone else's public room / A non-member DocSpace admin reads the primary link" | TEST-CONFIRMED [run: PASS] |
| Owner copies a foreign room | `RoomDeleteFileOpsTests.cs:67-93` (bug 81232) | TEST-CONFIRMED [run: PASS] |
| Owner / DocSpace admin change the owner of a foreign room; previous owner stays as Room manager; new owner active Room admin or higher; User / Guest → 403 | `room-types` spec, requirement "Room ownership transfer" and its `verification.md`; `02_Folders/History/FolderHistoryMembershipTests.cs:156-173` (transfer to a Room admin, history event); disabled target 403 "You cannot select this user" asserted only for the file-owner endpoint `04_Security/Sharing/GroupAndOwnerSharingTests.cs:88-102` (bug 66897) | CODE-ONLY (cross-reference) |
| Owner archives a foreign room; Room admin / User / Guest → 403 | `RoomArchivePermissionsTests.cs:43-64 (bug 80938), 83-102` | TEST-CONFIRMED [run: PASS] |
| Non-member DocSpace admin cannot invite, change roles, edit, create links → 403 | `RoomSharePermissionsTests.cs:82-102`; `RoomUpdatePermissionsTests.cs:78-97`; `RoomLinkPermissionsTests.cs:250-270`; resend `RoomResendPermissionsTests.cs:80-102` | TEST-CONFIRMED [run: PASS] |
| Room admin cannot copy a foreign room | `yaml` `duplicateBatchItems` (22577) demands read access to the source; a non-member Room admin has none | CODE-ONLY |
| **Archive section actions** | | |
| Archived room: create folder / file, update, tags, invite → 403 (Owner) | `RoomArchiveTests.cs:99-147` (bug 81551); `03_Rooms/Sharing/RoomShareValidationTests.cs:176-193`; `03_Rooms/Links/RoomLinkValidationTests.cs:206`; `03_Rooms/Invitations/RoomInvitationTests.cs:206-223`; `RoomUnarchiveTests.cs:299-319` | TEST-CONFIRMED [run: PASS] |
| Pinning an archived room refused | `yaml` `pinRoom` (32037): 403 for an archived room; no test | CODE-ONLY |
| View member list / history / info of an archived room by type | Owner only (`03_Rooms/Read/RoomInfoTests.cs:105-120`; `02_Folders/History/FolderHistoryQueryTests.cs:212`; `01_Files/Forms/FormRolesTests.cs:93`); read rights are not changed by archiving (`room-types`, "Archiving a room") | CODE-ONLY |
| Copy own / foreign archived room | `room-types`, "Archiving a room" (duplication of an archived room is refused — 403 for everyone, `RoomDeleteFileOpsTests.cs` sibling in `room-types/verification.md`) | MATRIX-ONLY — contradicted by `room-types` (see deviations table) |
| Restore own room: Owner, DocSpace admin | `RoomArchivePermissionsTests.cs:182-197, 219-235` | TEST-CONFIRMED [run: PASS] |
| Restore any room: DocSpace admin; Room admin / User / Guest foreign 403, invited User 403, removed member 403 | `RoomArchivePermissionsTests.cs:199-217, 237-306` | TEST-CONFIRMED [run: PASS] |
| Delete own room: Owner (archived), Owner / DocSpace admin (active) | `03_Rooms/Deletion/RoomDeleteTests.cs:114-133`; `RoomDeletePermissionsTests.cs:49-80` | TEST-CONFIRMED [run: PASS] |
| Delete any room: Room admin foreign and invited at every level 403; User / Guest 403 "Access denied" | `RoomDeletePermissionsTests.cs:82-138, 186-231`; DocSpace admin deleting a foreign room: `yaml` `deleteRoom` (30525) | TEST-CONFIRMED [run: PASS] |
| Room admin restores / deletes own archived room | `yaml` `unarchiveRoom` (30734), `deleteRoom`: the room's owner | CODE-ONLY |
| DocSpace admin restores, archives again and deletes a foreign archived room | restore `RoomArchivePermissionsTests.cs:199-217`; archive `43-64` (Owner); delete: `yaml` `deleteRoom` | TEST-CONFIRMED [run: PASS] (restore) / CODE-ONLY (the chain) |
| **Inviting users by type** | | |
| The six allowed pairs create a pending account (`inviteUsers`, `addMember`) and an invitation link | `01_Profiles/InviteUsersTests.cs:43-72`; `AddMemberTests.cs:43-85`; `00_Regressions/UserTest.cs:38-62` (bug 79334); `04_Portal/Users/InvitationLinkCreateTests.cs:45-67` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin → DocSpace admin 403 "Access denied"; Room admin → Room admin / DocSpace admin 403 | `InviteUsersTests.cs:100-124` (bug 79500); `AddMemberTests.cs:152-194`; `InvitationLinkCreateTests.cs:137-159` | TEST-CONFIRMED [run: PASS] |
| Guest type refused for every inviter: people endpoints 403 "Access denied", invitation link 400 | `InviteUsersTests.cs:74-98`; `AddMemberTests.cs:289-308`; `InvitationLinkCreateTests.cs:116-135` | TEST-CONFIRMED [run: PASS] |
| **Changing a user's type** | | |
| Owner promotes Guest → User → Room admin → DocSpace admin; DocSpace admin / Room admin cannot promote to DocSpace admin (403) | `02_UserType/UserTypeChangeTests.cs:43-69`; `UserTest.cs:128-147` (bug 80474); `UserTypeUpdatePermissionsTests.cs:44-67` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin promotes to Room admin; Room admin cannot | `UserTypeChangeTests.cs:71-92`; `UserTypeUpdatePermissionsTests.cs:44-58` | TEST-CONFIRMED [run: PASS] |
| Room admin promotes own Guest (queued job, type becomes User); cannot promote a foreign Guest (403); a User cannot either | `UserTypeUpdatePermissionsTests.cs:162-195` (bug 80669), `94-110`; `UserTest.cs:149-170` (bug 80478) | TEST-CONFIRMED [run: PASS] |
| Owner demotes DocSpace admin → Room admin / User; DocSpace admin demotes Room admin → User and User → Guest | `UserTypeChangeTests.cs:94-123, 125-185` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin cannot demote a DocSpace admin; type unchanged | `UserTest.cs:64-126` (bug 79724, profile update path); `UserTypeChangeTests.cs:187-212` (type still admin after the job is terminated) | TEST-CONFIRMED [run: PASS] |
| Room admin demoting a User → job completes with "You don't have enough permission to perform the operation" | `UserTypeUpdatePermissionsTests.cs:140-160` (type unchanged not asserted) | TEST-CONFIRMED [run: PASS] |
| Room admin cannot demote a Room admin | `yaml` `startUserTypeUpdate` (44928) | CODE-ONLY |
| **Disabling, deleting and reassigning users** | | |
| Owner disables every type; DocSpace admin disables Room admin / User / Guest | `03_UserStatus/UserStatusUpdateTests.cs:42-73, 110-138` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin cannot disable a DocSpace admin or the Owner (403) | `UserStatusUpdatePermissionsTests.cs:59-98` (bug 79900 — the tests assert the spec, their comment says the product still allows it; expected red) | TEST-CONFIRMED [run: PASS] (see R2) |
| Room admin / User / Guest cannot disable anyone (15 actor/target pairs → 403) | `UserStatusUpdatePermissionsTests.cs:100-164` | TEST-CONFIRMED [run: PASS] |
| Owner deletes User / DocSpace admin / Room admin / Guest; DocSpace admin deletes Room admin / User; DocSpace admin → DocSpace admin 403 | `01_Profiles/DeleteMemberTests.cs:43-79 (bug 80753), 109-123`; `RemoveUsersTests.cs:45-66, 101-121` | TEST-CONFIRMED [run: PASS] |
| Active account cannot be deleted → 403 "not suspended" | `DeleteMemberTests.cs:81-107` (bug 79560); `RemoveUsersTests.cs:68-99` | TEST-CONFIRMED [run: PASS] |
| Reassign: Owner / DocSpace admin start; Room admin / User / Guest 403; active source 400 | `10_UserData/ReassignStartTests.cs:42-107`; `RemoveStartTests.cs:46-122` (bug 80714); `NecessaryReassignTests.cs:65-77` | TEST-CONFIRMED [run: PASS] |
| Target must be an active Room admin or DocSpace admin, else 400 ("Can not reassign data to user …") | `products/ASC.People/Server/Api/ReassignController.cs:105-119`; `yaml` `startReassign` (43675) | CODE-ONLY |
| DocSpace admin cannot reassign a DocSpace admin's data | `ReassignController.cs:102` (400 "is a DocSpace or People administrator while the caller is not the portal owner") | CODE-ONLY |
| **Groups and guest lists in Accounts** | | |
| Create / edit / change membership: DocSpace admin succeeds, Room admin / User / Guest 403 | `13_Groups/GroupPermissionCreateTests.cs:204-243`; `GroupPermissionUpdateGroupTests.cs:123-174`; `GroupPermissionSetManagerTests.cs:115-159`; `GroupPermissionAddMembersTests.cs:99-143`; `GroupPermissionSetMembersTests.cs:148-210`; `GroupPermissionRemoveMembersTests.cs:212-271`; `GroupPermissionMoveMembersTests.cs:134-182` | TEST-CONFIRMED [run: PASS] |
| Room admin reads groups and members | `GroupPermissionGetGroupsListTests.cs:74-87`; `GroupPermissionGetGroupTests.cs:80-93`; `GroupPermissionGetByUserIdTests.cs:74-87` | TEST-CONFIRMED [run: PASS] |
| Room admin sees only own Guests (filter, sharing link, photo); Owner / DocSpace admin see every Guest | `08_Search/FilterSearchPermissionsTests.cs:43-61, 99-115`; `04_Portal/Guests/GuestSharingLinkTests.cs:59-121`; `GuestSharingLinkPermissionsTests.cs:45-63`; `04_Security/RoomShareTests.cs:923-944` (bug 79366); `11_Guests/GuestsApiTests.cs:45-81` | TEST-CONFIRMED [run: PASS] |
| Guest skipped when added to a group; group with a guest member or manager → 400 | `products/ASC.People/Server/Api/GroupsController.cs:177` (400 "one of the listed members or the manager is a guest"), `232-235` (members that cannot be group members are skipped); `yaml` `addGroup` (36536), `addMembersTo` (37216) | CODE-ONLY |
| Demoted group member leaves the group | `yaml` `updateUserType` (44821) / `startUserTypeUpdate` (44928) remarks | CODE-ONLY |
| **Portal settings** | | |
| Delete / suspend instructions by DocSpace admin, Room admin, User, Guest → 403 "Access denied" | `04_Portal/Settings/SendDeleteInstructionsPermissionsTests.cs:61-95` (bug 81375); `SendSuspendInstructionsPermissionsTests.cs:61-95` (bug 81374) | TEST-CONFIRMED [run: PASS] |
| DocSpace admin cannot grant or revoke the DocSpace admin type | `02_Settings/Security/ProductAdministratorTests.cs:172-211` (bug 80586); `UserTest.cs:128-147` | TEST-CONFIRMED [run: PASS] |
| DocSpace admin changes an ordinary setting | `WebItemSecurityWriteTests.cs:64-79`; `ProductAdministratorTests.cs:64-80` (the greeting save cases of `GreetingSaveTests.cs:45-90` are commented out: 500 on the `localhost` alias) | TEST-CONFIRMED [run: PASS] |
| **Sharing files from My documents** | | |
| User shares own file with DocSpace admin / Room admin / User | `04_Security/FileShareTests.cs:1260-1285` | TEST-CONFIRMED [run: PASS] |
| Guest cannot share (403); User cannot share the owner's file | `04_Security/Sharing/SetSecurityInfoTests.cs:72-124, 146-187` (bug 83263) | TEST-CONFIRMED [run: PASS] |
| Room admin shares with own guest | `FileShareTests.cs:1225-1258` (bug 79420) | TEST-CONFIRMED [run: PASS] |
| Room admin sharing with a foreign guest, even a member of the Room admin's room: 200, guest absent | `FileShareTests.cs:1228-1244`; `SetSecurityInfoTests.cs:189-207`; `deviations.md` matrix row "A Room admin shares files with own guests and guests of own rooms / Only guests the Room admin invited" | TEST-CONFIRMED [run: PASS] |
| User sharing with a guest: 200, guest absent | `FileShareTests.cs:1287-1309` | TEST-CONFIRMED [run: PASS] |
| Skipped guest cannot open the file | the share row is absent, so no right exists; not asserted directly | CODE-ONLY |
| Quick share for forms (filling link) in My documents by a User | `FileSecurity.cs:96-100` (My documents links take the default file access set, filling included); no test creates a filling link on a file in My documents | CODE-ONLY |
| Filling link refused on a form in a Custom / Public / Collaboration room → 403 "The role is not available for this user type" | `FileSharing.cs:159-170`; `deviations.md` matrix row "Forms in rooms other than FFR/VDR are shared 'for editing only' / Editing, comment and read links are offered; filling is refused" | LIVE-CONFIRMED |

Counts: 78 rows — TEST-CONFIRMED 56, CODE-ONLY 17, LIVE-CONFIRMED 3, MATRIX-ONLY 1 (unchanged by revision
2026-11-04, which widened two rows instead of adding any; the copy of an archived
room, contradicted by `room-types` — kept in the table as the matrix states it, flagged below), plus 1
cross-reference. Spec: 12 requirements, 57 scenarios (before the reshaping: 12 requirements, 46 scenarios;
the 11 new scenarios are the matrix containers and the split prose, no fact added or dropped).

## Revision 2026-11-06 (`1f1b052f7d..0ebdbb7c84`)

| Claim | Source | Status |
|---|---|---|
| R1 re-judged on the section split and the listing code | `c866c055e1`; `73aa4e8ab8`; `private-rooms/verification.md` "Points for other specs" | CODE-ONLY plus one Owner test |
| Provider-based (third-party) rooms are listed outside the private filter of the administrator's view; a new private room on a connected storage is refused since `b562fc3fb1`, existing ones stay | `FileSecurity.cs` the `thirdPartyRoomsEntries` query after `:3073` | NOT VERIFIED: whether such a room can reach a non-member through Active, Archive or `Any` |
| Non-member administrator refused on an archived private room and on clearing its access | `private-rooms`, "An archived private room keeps its protection", "An administrator outside cannot empty a private room" | CROSS-REFERENCE |

## Where the server differs from the product matrix (spec follows the server)

| Matrix (sheet "Types") | Server | Reference |
|---|---|---|
| Users and Guests may invite to a room ("ships in the next version") | Inviting needs the Room manager role, which they cannot hold; 403 | `deviations.md`, matrix table rows 1-7 of user-type-permissions |
| Nobody sees the links of someone else's public room | A non-member DocSpace admin reads the primary link | same |
| Owner and DocSpace admin see all rooms | Not private (end-to-end encrypted) rooms, unless they are members | same; R1 |
| A Room admin requests promotion of a User to Room admin, confirmed in Accounts | No such operation in the API | same |
| A Room admin shares files with own guests and guests of own rooms | Only guests the Room admin invited | same |
| Forms in rooms other than FFR / VDR are shared "for editing only" | Editing, comment and read links are offered; filling is refused | same |
| DocSpace admin manages every setting except portal deletion | Suspension and granting the DocSpace admin type are owner-only too | same |
| Copy of an archived room (own: Room admin and above; foreign: Owner / DocSpace admin) | `room-types` ("Archiving a room"): duplication of an archived room is refused for everyone; the room has to be restored first | `room-types/spec.md`, `room-types/verification.md` |
| Guest invited through the invitation link endpoint | 400 with "EmployeeType" in the message, where the people endpoints answer 403 | `InvitationLinkCreateTests.cs:116-135` |

## Left out of the spec, and why

- **Removing a guest from one's guest list** (`deleteGuests`, 38329): only the Room admin who invited the
  guest may do it; the Owner and DocSpace admins get 403 (`11_Guests/GuestsPermissionsTests.cs:43-186`,
  bug 80628). Guest-list maintenance, not an access right of the matrix; belongs with `invitations`.
- **Type-change job progress and termination** (`getUserTypeUpdateProgress`, `terminateUserTypeUpdate`):
  Owner / DocSpace admin only, a Room admin cannot read the job it started
  (`02_UserType/UserTypeProgressAndTerminatePermissionsTests.cs:43-230`). Job plumbing.
- **A DocSpace admin cannot deactivate or edit the Owner, nor demote themselves** (`UserTest.cs:64-117`,
  `UserStatusUpdatePermissionsTests.cs:80-98`): follows from the owner-only rules; not a matrix row.
- **Group manager who is a plain User** cannot replace or remove members
  (`GroupPermissionSetMembersTests.cs:164-178`, `GroupPermissionRemoveMembersTests.cs:226-239`): the matrix
  has no group-manager row.
- **Guest reading security info of a file shared with them** (403 single, 200 batch;
  `04_Security/Sharing/FileSecurityInfoPermissionsTests.cs:187-206`, `GetSecurityInfoPermissionsTests.cs:143-156`):
  specified in `file-sharing`.
- **Resend of room invitations** is Room manager only (`RoomResendPermissionsTests.cs:47-185`): `invitations`.
- **Deleting a room by an invited Room admin at any level, Room manager included → 403**
  (`RoomDeletePermissionsTests.cs:208-231`): stated in `room-role-permissions` (Room manager cannot delete).
- **Terminated accounts get 401 everywhere** (`RoomCreatePermissionsTests.cs:125-149`): authentication,
  not a type right.
- **Invitation-link deletion rights by type** (`InvitationLinkDeleteTests.cs:69-135`): `invitations`.

## Suspected defects

D-numbers are the ones of `openspec/deviations.md` ("Suspected defects"); R-numbers are new.

| # | Where | What the server does | Evidence | Status |
|---|-------|----------------------|----------|--------|
| D3 | Changing a user's type | A Room admin promotes a foreign guest to User through the queued job endpoint (200, job completes, type changes), while the synchronous endpoint refuses the same guest (403). | `deviations.md` D3 (live run); `products/ASC.People/Server/Api/UserController.cs:2662-2700` (`startUserTypeUpdate`) | STILL OPEN (2026-11-04: the type-change actions changed only in their webhook declarations) |
| D4 | Room membership | A Room admin cannot add a foreign guest to their room by id (200, nobody added) but can by e-mail: the guest joins the room and the Room admin's guest list. | `deviations.md` D4 (live run); `FileSharing.cs:291-318` (the e-mail branch adds a user relation before any guest-list check) | STILL OPEN (2026-11-04: the e-mail branch is untouched; the range only inserted the AI-project guest check before it) |
| D7 | My documents | `POST files/@my/file` as a Guest answers 500 (null reference); `@my/text` and `@my/html` answer 404. | `deviations.md` D7 (live run); the 404 of text / html: `TextFileCreatePermissionsTests.cs:102-113`, `HtmlFileCreatePermissionsTests.cs`; `products/ASC.Files/Server/Api/FilesController.cs:1325-1340` | STILL OPEN (2026-11-04: the action changed only in its webhook declaration) |
| D9 | Inviting users by type | `GET portal/users/invite/{type}` never refuses: a disallowed pair answers 200 with an empty string, and for the Guest type it returns a real link to the Owner, a DocSpace admin and a Room admin. | `deviations.md` D9 (live run); `04_Portal/Users/InvitationLinkGetDeprecatedTests.cs`; `web/ASC.Web.Api/Api/PortalController.cs:196-215` | STILL OPEN (2026-11-04: `PortalController.cs` is outside the range) |
| D10 | Disabling and deleting | `PUT people/delete` by a DocSpace admin on a disabled DocSpace admin answers 200 with the account echoed and deletes nothing; `DELETE people/{id}` answers 403. | `deviations.md` D10 (live run); `UserController.cs:1563-1630` (`removeUsers`: the skipped account is still returned) | STILL OPEN (2026-11-04: the action was rewrapped for the `user.deleted` webhook, the skip is as before) |
| R1 | Room visibility | A private room was listed to a non-member Owner and DocSpace admin (bug 82956). The administrator's listing has dropped such a room since `73aa4e8ab8`; the earlier STILL OPEN came from the stale "expected to fail" remark of `PrivateRoomVisibilityTests.cs:44-92`, which still stands although those tests now pass trivially after `c866c055e1`, for members as well. | `FileSecurity.cs:3069-3073`; `git log -S` on that comment; `PrivateRoomSectionTests.cs:141-153` is the assertion that can fail | FIXED (`73aa4e8ab8`). Third-party rooms do not pass through that filter, see the revision rows. `openspec/deviations.md` R1 still says OPEN |
| R2 | Disabling users | A DocSpace admin can deactivate another DocSpace admin and the Owner; the spec, the matrix and the tests say 403. | `03_UserStatus/UserStatusUpdatePermissionsTests.cs:59-98` (bug 79900, comment says the product allows it, tests red) | STILL OPEN (2026-11-04: `updateUserStatus` changed only in its webhook declaration; the commit "fix Bug 79900" of 2026-02 predates the previous revision and the test still asserts against the product) |
| R3 | Inviting users by type | The invitation-link endpoint refuses the Guest type with 400 ("EmployeeType"), the people endpoints with 403 "Access denied": the same rule, two status codes. | `InvitationLinkCreateTests.cs:116-135`; `InviteUsersTests.cs:74-98`; `PortalController.cs:253-256` | STILL OPEN (2026-11-04: `PortalController.cs` is outside the range) |
