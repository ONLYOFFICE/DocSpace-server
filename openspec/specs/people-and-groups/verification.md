# people-and-groups — verification

Paths are relative to the repo root. `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml` (working tree of the
submodule, HEAD `baf28cf`, 2026-11-06). Tests are under `products/ASC.People/Tests/Tests/`, by folder: `P/`
`01_Profiles`, `T/` `02_UserType`, `S/` `03_UserStatus`, `E/` `04_Email`, `F/` `06_Photos`, `Q/` `08_Search`, `H/`
`09_Theme`, `D/` `10_UserData`, `U/` `11_Guests`, `X/` `12_ThirdPartyAccounts`, `G/` `13_Groups`, `R/`
`14_GroupSearch`, `M/` `15_PeopleGroups`. Code: `User` `products/ASC.People/Server/Api/UserController.cs`, `Photo`
`.../PhotoController.cs`, `Third` `.../ThirdpartyController.cs`, `Reassign` `.../ReassignController.cs`, `Remove`
`.../RemoveUserDataController.cs`, `Groups` `.../GroupsController.cs`, `Accounts` `.../AccountsController.cs`,
`Queue` `common/ASC.Data.Reassigns/QueueWorker.cs`, `Portal` `web/ASC.Web.Api/Api/PortalController.cs`.

Status: **TEST-CONFIRMED** means the assertion at that line was read. **TEST-CONFIRMED (by name)** means the row
cites the generated test index only (method name, SDK call, asserted status) and the assertion body was not read.
**CODE-ONLY** means no test; read from the code or the yaml. No test was run for this document.
Line numbers of the code come from the tree as of 2026-11-06.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Reading another profile by ID** | | |
| Owner, DocSpace admin, Room admin read a Room admin; `id`, `email` returned | `P/GetProfileByUserIdTests.cs:41-52` | TEST-CONFIRMED |
| User 403 "Access denied" on the Owner | `P/GetProfileByUserIdTests.cs:58` | TEST-CONFIRMED |
| Guest 403; removed profile 404; anonymous 401 | `P/GetProfileByUserIdTests.cs:71,84,98` | TEST-CONFIRMED (by name) |
| **Reading a profile by email** | | |
| Own address, every type, same as self profile | `P/GetProfileByEmailTests.cs:49` | TEST-CONFIRMED |
| Owner→admin, admin→Room admin, Room admin→User succeed | `P/GetProfileByEmailTests.cs:78` | TEST-CONFIRMED |
| User/Guest on another 403; unknown 404; anonymous 401 | `P/GetProfileByEmailTests.cs:116,130,102,91` | TEST-CONFIRMED (by name) |
| `encemail` decrypted; no address 400 | `User:1211-1220` | CODE-ONLY |
| **Own profile and profile list** | | |
| Self profile; Guest has no personal folder | `P/GetSelfProfileTests.cs:45,62,87` | TEST-CONFIRMED (by name) |
| List with flags for Owner/admin; User, Guest 403; anonymous 401 | `P/GetAllProfilesTests.cs:46,78,91,104` | TEST-CONFIRMED (by name) |
| **Checking whether an email is taken** | | |
| Roles LinkInvite, GuestShareLink, Authenticated; `exists`, `status`; blank → 400 | `User:1169-1187`, yaml `checkUserExistsByEmail` | CODE-ONLY |
| **Token diagnostics** | | |
| `name`, `claims` as `type:value` | `User:121-131`, yaml `getClaims` | CODE-ONLY |
| **Editing a profile** | | |
| Own names persist; `displayName` built from them (letters only, see PG-1) | `P/UpdateMemberTests.cs:49-70` | TEST-CONFIRMED |
| `12345` → 400 "Incorrect firstname or lastname" | `P/UpdateMemberTests.cs:126` | TEST-CONFIRMED |
| Overlong names 400 (User, Guest); unknown 404; anonymous 401 | `P/UpdateMemberTests.cs:139,156,187,173` | TEST-CONFIRMED (by name) |
| **Nobody else edits the Owner** | | |
| 403 for the listed actors, bug 79724 | `P/UpdateMemberTests.cs:105-120`; `00_Regressions/UserTest.cs:67` | TEST-CONFIRMED |
| **Interface language of a profile** | | |
| Own `es` persists | `P/UpdateMemberCultureTests.cs:50-62` | TEST-CONFIRMED |
| Owner, admin, Room admin, User, Guest on others 403, bug 65478 | `P/UpdateMemberCultureTests.cs:66-85,105-127` | TEST-CONFIRMED |
| Overlong 400 (bug 79918); unknown 404; anonymous 401 | `P/UpdateMemberCultureTests.cs:180,165,151` | TEST-CONFIRMED (by name) |
| **Deleting one disabled profile** | | |
| Disabled target deleted, Guest included (bug 80753) | `P/DeleteMemberTests.cs:54-76` | TEST-CONFIRMED |
| Not disabled 403 (bug 79560); unknown 404 | `P/DeleteMemberTests.cs:82,96,126` | TEST-CONFIRMED (by name) |
| **Deleting several disabled profiles** | | |
| Both returned with status and flags | `P/RemoveUsersTests.cs:47-62` | TEST-CONFIRMED |
| Wrong field name 400, bug 80048 (body text "Value cannot be null. (Parameter 'inDto?.UserIds')") | `P/RemoveUsersTests.cs:140-157` | TEST-CONFIRMED |
| Not disabled 403 (79560, 79876); non-admin 403; anonymous 401 | `P/RemoveUsersTests.cs:71,87,105,124` | TEST-CONFIRMED (by name) |
| **Deleted profile leaves the contact list** | | |
| Plain and after reassignment, bug 83188 | `P/DeletedUserVisibilityTests.cs:46,61` | TEST-CONFIRMED (by name) |
| **Disabling and re-enabling accounts** | | |
| Four types, status and flag each | `S/UserStatusUpdateTests.cs:43-65` | TEST-CONFIRMED |
| Re-activation; denied pairs in `user-type-permissions` | `S/UserStatusUpdateTests.cs:76`; `S/UserStatusUpdatePermissionsTests.cs:100-131` | TEST-CONFIRMED (by name) |
| **Listing accounts by status** | | |
| Owner, admin, Room admin lists; Room admin Terminated empty 200; User, Guest 403 | `S/UserStatusGetTests.cs:43,106,174,199`; `S/UserStatusGetPermissionsTests.cs:44,62,86` | TEST-CONFIRMED (by name) |
| **Activation status through a confirmation link** | | |
| Roles Activation, EmailActivation; list must be the caller alone, else 400; welcome letter once | `User:2117-2159` | CODE-ONLY |
| **Who may start an email change** | | |
| Allowed pairs | `E/EmailChangeInstructionsTests.cs:43-55,100-110` | TEST-CONFIRMED |
| Denied pairs 403 "Access denied" | `E/EmailChangeInstructionsPermissionsTests.cs:42-73` | TEST-CONFIRMED |
| Anonymous 401 | `E/EmailChangeInstructionsPermissionsTests.cs:134` | TEST-CONFIRMED (by name) |
| **Immediate change versus instructions** | | |
| Owner on self: 200, sign-in with the new address | `E/EmailChangeInstructionsTests.cs:70-90` | TEST-CONFIRMED |
| Non-admin only gets a letter; admin path writes the address | `User:1924-1985` | CODE-ONLY |
| **Validation of an email change** | | |
| Malformed 400 naming the Email field; unknown 404 message | `E/EmailChangeInstructionsPermissionsTests.cs:86-115` | TEST-CONFIRMED |
| Longer than 255 → 400 | `E/EmailChangeInstructionsPermissionsTests.cs:117` | TEST-CONFIRMED (by name) |
| Disabled/pending 404, taken address 400, same address 400 | `User:1924-1985` | CODE-ONLY |
| **Applying an email change from a link** | | |
| Role EmailChange; 403 cases; lower-cased; Activated; empty 400 | `User:684-737` | CODE-ONLY |
| **Reading a user photo** | | |
| Own; Owner/admin on anyone; Room admin except Guest | `F/PhotoGetTests.cs:49-85` | TEST-CONFIRMED |
| Room admin→Guest 403; User/Guest→others 403 | `F/PhotoGetPermissionsTests.cs:48-100` | TEST-CONFIRMED |
| Anonymous 401; unknown 404 | `F/PhotoEdgeCaseTests.cs:48,63` | TEST-CONFIRMED (by name) |
| **Uploading a photo** | | |
| Own upload `success` true (bug 80569) | `F/PhotoUploadTests.cs:49-70` | TEST-CONFIRMED |
| `autosave` versus temporary file | `Photo:298-335` | CODE-ONLY |
| **A refused upload answers 200** | | |
| Other account: `success` false, "Security error." | `F/PhotoUploadPermissionsTests.cs:50-58` | TEST-CONFIRMED |
| 20 MB → message about 5 MB; SVG → `success` false; no part 400 | `F/PhotoEdgeCaseTests.cs:108-165` | TEST-CONFIRMED |
| Anonymous 401 | `F/PhotoEdgeCaseTests.cs:126` | TEST-CONFIRMED |
| **Setting a photo from a URL** | | |
| Own only, empty 400, same URL skips download | `Photo:224-251`, yaml `updateMemberPhoto` | CODE-ONLY |
| **Cropping a photo** | | |
| Own only; `tmpFile` flow; zero size = whole image | `Photo:84-126`, yaml `createMemberPhotoThumbnails` | CODE-ONLY |
| **Deleting a photo** | | |
| Own, every type; others 403 | `F/PhotoDeleteTests.cs:48-58`; `F/PhotoDeletePermissionsTests.cs:48-60` | TEST-CONFIRMED |
| Anonymous 401; unknown 404 | `F/PhotoEdgeCaseTests.cs:78,93` | TEST-CONFIRMED (by name) |
| **People search reads the stored names** | | |
| Plain query over stored columns, decoded name needed; never index lag | `common/ASC.Core.Common/Users/UserQueryHelper.cs:53`, `common/ASC.Core.Common/Data/DbUserService.cs:973`; comment and decode in `Q/SearchTestBase.cs:42-46` and its poll | CODE-ONLY (test helper comment agrees) |
| **Filtered people lists** | | |
| Every type found for Owner | `Q/FilterSearchTests.cs:43-52` | TEST-CONFIRMED |
| Room admin does not see Owner's guest | `Q/FilterSearchPermissionsTests.cs:44-60` | TEST-CONFIRMED |
| admin, Room admin finds; User/Guest 403; anonymous 401, both operations | `Q/FilterSearchTests.cs:59,75,105,122`; `Q/FilterSearchPermissionsTests.cs:64,77,90,100,118,131,144` | TEST-CONFIRMED (by name) |
| **Type flags in search results** | | |
| Owner flags, bug 80587 | `Q/FilterSearchTests.cs:140-150` | TEST-CONFIRMED |
| **Administrator-only searches** | | |
| Owner finds each role | `Q/GlobalSearchTests.cs:50-60` | TEST-CONFIRMED |
| admin finds; Room admin, User, Guest 403 on all three; anonymous 401 | `Q/GlobalSearchTests.cs:66,82,98`; `Q/GlobalSearchPermissionsTests.cs:48-213` | TEST-CONFIRMED (by name) |
| **Searching by status** | | |
| Active then Terminated | `Q/GlobalSearchTests.cs:114-122` | TEST-CONFIRMED |
| **People an entry can be shared with** | | |
| Invited User finds a fellow member | `Q/SharedSearchTests.cs:168-185,446-460` | TEST-CONFIRMED |
| User NOT invited 403 (the tests say "RoomMember" but never invite him) | `Q/SharedSearchPermissionsTests.cs:116-126,312-322` | TEST-CONFIRMED |
| Invited Guest 403, bug 79218 | `Q/SharedSearchPermissionsTests.cs:343-360` | TEST-CONFIRMED |
| Rule: can read and not a guest; unknown 404 | `User:3257-3265,3338` | CODE-ONLY |
| Folder variants; Owner, admin, Room admin finds; anonymous 401 | `Q/SharedSearchTests.cs:111-148,250-307,389-426`; `Q/SharedSearchPermissionsTests.cs:132,148,214-246,328,368` | TEST-CONFIRMED (by name) |
| **Accounts and groups an entry can be shared with** | | |
| Room manager finds; invited User with editing 403; outside admin 403 | `Q/SharedSearchTests.cs:89-104`; `Q/SharedSearchPermissionsTests.cs:47-82` | TEST-CONFIRMED |
| Rule: can edit access | `Accounts:175-180` | CODE-ONLY |
| Folder and room variants; Guest 403; anonymous 401 | `Q/SharedSearchTests.cs:190-228,329-367`; `Q/SharedSearchPermissionsTests.cs:83,101,163-199,261-297` | TEST-CONFIRMED (by name) |
| **Accounts search needs a text** | | |
| Empty text → count and total 0 | `Accounts:193-197` | CODE-ONLY |
| **Theme of an account** | | |
| Default System; cycles through three themes, every type | `H/ThemeTests.cs:43-85,88-95` | TEST-CONFIRMED |
| Stored per caller | `User:1810-1815` (loaded for the current user) | CODE-ONLY |
| Anonymous 401 | `H/ThemePermissionsTests.cs:42,53` | TEST-CONFIRMED (by name) |
| **Whether a reassignment is necessary** | | |
| Owner, admin true; non-admin 403. The tests send no `userId`, so they prove only "true when rooms exist", not "this user's rooms" | `D/NecessaryReassignTests.cs:43-78` | TEST-CONFIRMED |
| Peer admin 403; Guest conversion counts shared entries | `Reassign:199-217` | CODE-ONLY |
| **Starting a data reassignment** | | |
| Owner starts; active source 400; non-admin 403 | `D/ReassignStartTests.cs:43-55,77-108` | TEST-CONFIRMED |
| admin starts; anonymous 401 | `D/ReassignStartTests.cs:59,110` | TEST-CONFIRMED (by name) |
| Recipient rule, Owner and self as source | `Reassign:106-135` | CODE-ONLY |
| **Watching a data reassignment** | | |
| Completed, `error` empty; peer admin 403 (bug 78957) | `D/ReassignProgressTests.cs:43-58,105-125` | TEST-CONFIRMED |
| admin; non-admin 403; anonymous 401 | `D/ReassignProgressTests.cs:62,83,98` | TEST-CONFIRMED (by name) |
| **Cancelling a data reassignment** | | |
| Canceled, `isCompleted` true | `D/ReassignTerminateTests.cs:43-60` | TEST-CONFIRMED |
| admin; non-admin 403; peer admin 403 (bug 79320); anonymous 401 | `D/ReassignTerminateTests.cs:64,87,110,131` | TEST-CONFIRMED (by name) |
| **Starting a data removal** | | |
| Room passes to the Owner, bug 80714 | `D/RemoveStartTests.cs:44-74` | TEST-CONFIRMED |
| Owner, admin start; non-admin 403; anonymous 401 | `D/RemoveStartTests.cs:77,92,109,125` | TEST-CONFIRMED (by name) |
| 400 cases | `Remove:132-151` | CODE-ONLY |
| **Watching and cancelling a data removal** | | |
| Progress `error` empty; terminate succeeds (Owner) | `D/RemoveProgressTests.cs:43-52`; `D/RemoveTerminateTests.cs:44-52` | TEST-CONFIRMED |
| admin; non-admin 403; anonymous 401 | `D/RemoveProgressTests.cs:57,73,88`; `D/RemoveTerminateTests.cs:56,70,89` | TEST-CONFIRMED (by name) |
| **Asking to delete one's own profile** | | |
| Member gets own email in the answer; Owner 403 | `D/SendInstructionsToDeleteTests.cs:52-72` | TEST-CONFIRMED |
| LDAP 403 | `Remove:102-105` | CODE-ONLY |
| **Deleting a guest's personal folder** | | |
| All five types 403 (a fresh Guest has no folder) | `D/DeletePersonalFolderTests.cs:43-62`; `Remove:219-224`; `Queue:147-154` | TEST-CONFIRMED |
| A Guest with a folder gets the job | `Remove:224-226`; `common/ASC.Data.Reassigns/DeletePersonalFolderProgressItem.cs:59-117` | CODE-ONLY |
| **Watching the personal folder deletion** | | |
| Caller only, empty body when nothing queued | `Remove:243-250`, yaml `getDeletePersonalFolderProgress` | CODE-ONLY |
| **Watching a type conversion** | | |
| 100, `isCompleted`, `error` empty | `T/UserTypeChangeTests.cs:153-162` | TEST-CONFIRMED |
| Room admin, User, Guest 403 | `T/UserTypeProgressAndTerminatePermissionsTests.cs:44,63,82` | TEST-CONFIRMED (by name) |
| **Cancelling a type conversion** | | |
| Admin cancels the Owner's; Canceled; target unchanged | `T/UserTypeChangeTests.cs:188-208` | TEST-CONFIRMED |
| Room admin, User, Guest 403 | `T/UserTypeProgressAndTerminatePermissionsTests.cs:101-212` | TEST-CONFIRMED (by name) |
| **Listing third-party providers** | | |
| No linked entry, each names a provider, env empty list | `X/ThirdPartyAuthProvidersTests.cs:48-75` | TEST-CONFIRMED |
| Anonymous allowed, login URL | `Third:93-135` | CODE-ONLY |
| **Linking a third-party account** | | |
| 400, 403, cancellation (live OAuth not tested) | `Third:156-183`, yaml `linkThirdPartyAccount` | CODE-ONLY |
| **Unlinking a third-party account** | | |
| Link removed, 200 | `Third:381-388` | CODE-ONLY |
| **Joining through a third-party account** | | |
| Anonymous, invitation check, cancellation empty body | `Third:207-300`, yaml `signupThirdPartyAccount` | CODE-ONLY |
| **Who may manage groups** | | |
| User manager forbidden: `removeMembersFrom`, `setMembersTo` | `G/GroupPermissionRemoveMembersTests.cs:227-235`; `G/GroupPermissionSetMembersTests.cs:165` | TEST-CONFIRMED |
| DocSpace admin allowed, non-admin 403, anonymous 401, each operation | `G/GroupPermission*Tests.cs` (`_DocSpaceAdmin_`, `_NonAdminRoles_Forbidden`, `_Anonymous_Unauthorized`); `G/GroupPermissionDeleteGroupTests.cs:72,86` | TEST-CONFIRMED (by name) |
| **Who may read groups** | | |
| `getGroup`: admins and Room admin yes, User and Guest 403 | `G/GroupPermissionGetGroupTests.cs:83-110` | TEST-CONFIRMED |
| `getGroupByUserId` own 403; `getGroups` matrix | `G/GroupPermissionGetByUserIdTests.cs:77,90,102,116,128,142`; `G/GroupPermissionGetGroupsListTests.cs:46,61,77` | TEST-CONFIRMED (by name) |
| **Creating a group** | | |
| Missing, empty names and manager; unknown manager, member; dedupe; manager-only count 1 | `G/GroupPermissionCreateTests.cs:52-115,190-197`; `G/GroupCreateTests.cs:76-84` | TEST-CONFIRMED |
| Overlong (255 chars) 400 | `G/GroupPermissionCreateTests.cs:146-155` | TEST-CONFIRMED |
| Wrong types, blank name, special and unicode names | `G/GroupPermissionCreateTests.cs:119,128,137,159,170,180` | TEST-CONFIRMED (by name) |
| **Reading a group** | | |
| Malformed 404 | `G/GroupPermissionGetGroupTests.cs:55-59` | TEST-CONFIRMED |
| `includeMembers` default and false; unknown 404 | `G/GroupReadTests.cs:50,65,81`; `G/GroupPermissionGetGroupTests.cs:46` | TEST-CONFIRMED (by name) |
| **Groups of a user** | | |
| Malformed 404; unknown empty | `G/GroupPermissionGetByUserIdTests.cs:46-61` | TEST-CONFIRMED |
| Lists and follows changes | `G/GroupReadTests.cs:110-191` | TEST-CONFIRMED (by name) |
| **Listing and filtering groups** | | |
| Filters, paging, sort, bad parameters | `G/GroupsQueryTests.cs:54-217`; `G/GroupPermissionGetGroupsListTests.cs:94-119` | TEST-CONFIRMED (by name) |
| **Updating a group** | | |
| Remove beats add; empty body unchanged | `G/GroupPermissionUpdateGroupTests.cs:104-118`; `G/GroupUpdateTests.cs:147-158` | TEST-CONFIRMED |
| Each field; all at once; unknown 404 | `G/GroupUpdateTests.cs:42-117`; `G/GroupPermissionUpdateGroupTests.cs:46` | TEST-CONFIRMED (by name) |
| **Update ignores unknown users** | | |
| Unknown manager, unknown added, non-member removed | `G/GroupPermissionUpdateGroupTests.cs:58-102` | TEST-CONFIRMED |
| **Deleting a group** | | |
| Second delete 404 | `G/GroupPermissionDeleteGroupTests.cs:55-62` | TEST-CONFIRMED |
| Gone from get and list; unknown 404 | `G/GroupDeleteTests.cs:49,57,69`; `G/GroupPermissionDeleteGroupTests.cs:46` | TEST-CONFIRMED (by name) |
| **Moving members between groups** | | |
| One member; same group (bug 81710); target manager kept | `G/GroupMembersMoveTests.cs:49-60`; `G/GroupPermissionMoveMembersTests.cs:54-72,116-130`; `Groups:376-380` | TEST-CONFIRMED |
| Many, overlap, empty source, counts (81497), idempotent, 404s | `G/GroupMembersMoveTests.cs:65-192`; `G/GroupPermissionMoveMembersTests.cs:74,88,102` | TEST-CONFIRMED (by name) |
| **Replacing the members of a group** | | |
| Manager left out; manager kept once | `G/GroupMembersReplaceTests.cs:169-198` | TEST-CONFIRMED |
| Unknown ID 400; malformed 400; disabled only 400 (bug 79368) | `G/GroupPermissionSetMembersTests.cs:70-90`; `M/GroupMembersTests.cs:44-66` | TEST-CONFIRMED |
| Empty and null lists 400; unknown group 404 | `G/GroupPermissionSetMembersTests.cs:47,93,105,118,130` | TEST-CONFIRMED (by name) |
| Valid + unknown mix keeps the valid ones (any-valid check) | `Groups:427-455` | CODE-ONLY |
| **Adding members to a group** | | |
| All clauses, null list bug 81484 | `G/GroupMembersAddTests.cs:49-137`; `G/GroupPermissionAddMembersTests.cs:46,57,70,84` | TEST-CONFIRMED (by name) |
| **Removing members from a group** | | |
| null `members` unchanged (bug 81509); mixed malformed: status below 500, unchanged | `G/GroupPermissionRemoveMembersTests.cs:141-150,190-207` | TEST-CONFIRMED |
| Rest | `G/GroupMembersRemoveTests.cs:53-219`; `G/GroupPermissionRemoveMembersTests.cs:46,67,98,114,175` | TEST-CONFIRMED (by name) |
| **Setting the group manager** | | |
| New manager added, members stay; unknown user 404; empty `userId` 400 | `G/GroupManagerTests.cs:92-108`; `G/GroupPermissionSetManagerTests.cs:57-77` | TEST-CONFIRMED |
| Rest | `G/GroupManagerTests.cs:49-156`; `G/GroupPermissionSetManagerTests.cs:46,80,91` | TEST-CONFIRMED (by name) |
| **Groups an entry is shared with** | | |
| With and without access | `R/GroupFileSharedTests.cs:107-118` | TEST-CONFIRMED |
| Rest, all three operations | `R/GroupFileSharedTests.cs:48-227`; `R/GroupFolderSharedTests.cs:47-327`; `R/GroupRoomSharedTests.cs:49-135`; `R/GroupRoomSharedValidationTests.cs:81-120` | TEST-CONFIRMED (by name) |
| **Everyone in the group lists** | | |
| System and shared; fresh portal count 1; through parent | `R/GroupEveryoneSharedTests.cs:49-70,146-152` | TEST-CONFIRMED |
| Name search, exclude, paging, folder | `R/GroupEveryoneSharedTests.cs:73-128,156-180` | TEST-CONFIRMED (by name) |
| **Filtering the group lists** | | |
| All clauses | `R/GroupFileSharedTests.cs:159`; `R/GroupFolderSharedTests.cs:146-219`; `R/GroupRoomSharedTests.cs:162-272` | TEST-CONFIRMED (by name) |
| **Who may read the group lists** | | |
| File owner yes; admin on Owner's 403; invited Read 403; rule can edit access | `R/GroupSearchPermissionsTests.cs:43-72,195-225,260-275`; `Groups:828-830` | TEST-CONFIRMED |
| Others | `R/GroupSearchPermissionsTests.cs:75,91,107,119-183,228,244,278` | TEST-CONFIRMED (by name) |
| **Unknown entries in the group lists** | | |
| Non-numeric, empty, negative | `R/GroupFileSharedTests.cs:185-197`; `R/GroupRoomSharedValidationTests.cs:55-78` | TEST-CONFIRMED |
| Others, deleted folder | `R/GroupFileSharedTests.cs:174`; `R/GroupFolderSharedTests.cs:235-261`; `R/GroupRoomSharedValidationTests.cs:46` | TEST-CONFIRMED (by name) |
| **Guests of a Room admin's list** | | |
| All clauses, bug 80628 | `U/GuestsApiTests.cs:46,72`; `U/GuestsPermissionsTests.cs:44-156` | TEST-CONFIRMED (by name) |
| **Marking the gift message as read** | | |
| Per-account flag; error only logged | `Portal:838-851`, yaml `markGiftMessageAsRead` | CODE-ONLY |
| **Letters after portal registration** | | |
| One-hour key; letters; not idempotent; also switches on the configured second factor for the portal (see PG-4) | `Portal:1278-1307`, yaml `sendCongratulations` | CODE-ONLY |

## Differences between the yaml and the server

| Operation | yaml | Server |
|---|---|---|
| `getGroupsWithFilesShared`, `...Folders...`, `...Rooms...` | 400 "A parameter has the wrong type" | a non-numeric ID answers 404 (`R/GroupFileSharedTests.cs:185`, `R/GroupRoomSharedValidationTests.cs:65`) |
| `getGroupByUserId` | no 404 of its own | a malformed ID answers 404 from the route constraint (`G/GroupPermissionGetByUserIdTests.cs:46`) |
| `uploadMemberPhoto` | 200 carries a refusal in the body; no 403 or 404 listed | same, consistent, but the sibling photo operations answer 403 and 404 for the same conditions; an unknown ID ends up in the catch-all and is a 200 too (`Photo:345-365`) |

## Left out

| What | Reason |
|---|---|
| None of the operations of the owned tags | All 59 listed in `coverage-absent.txt` are in a requirement |
| `addMember`, `inviteUsers`, `resendUserInvites` (`P/AddMemberTests`, `P/InviteUsersTests`, `P/ResendActivationTests`) | `invitations` |
| `updateUserType`, `startUserTypeUpdate` (`T/UserTypeChangeTests`, `T/UserTypeUpdatePermissionsTests`) | `user-type-permissions` |
| People / Password, sign-in, two-factor, recovery | A separate capability, by instruction |
| The audit records and webhooks of these operations | `history-and-audit`, `webhooks-and-api-keys` |
| `RemoveMembersFrom_MissingMembersRequest_RejectedBySdk`, `SetGroupManager_NullRequestBody_RejectedBySdk` | The 400 is raised by the generated client, no request reaches the server |

## Suspected defects

| # | Where | What the server does | Evidence | Evidence kind |
|---|---|---|---|---|
| PG-1 | `displayName` of a profile | Comes back HTML-encoded (`O&#39;Hara`) while `firstName`, `lastName` and the stored columns are not, so feeding it back to a search finds nothing | `common/ASC.Core.Common/Users/DisplayUserSettings.cs:63,102` (encodes by default), `common/ASC.Api.Core/Model/EmployeeDto.cs:173`; `P/UpdateMemberTests.cs` comment at the name generation; `Q/SearchTestBase.cs:42-46` decodes | per test comment |
| PG-2 | `updateGroup` | Takes any non-null `groupName`, so an empty or blank name would be stored, while `addGroup` refuses a blank name with 400. `SaveGroupInfoAsync` itself was not read | `Groups:185` versus `Groups:262` (`?? group.Name`); yaml lists only the length as a 400 | red by code reading |
| PG-3 | `getRemoveProgress`, `terminateRemove` | Demand only the edit-user permission, so a DocSpace admin can read or cancel a removal of another DocSpace admin. The same gap was closed for reassignment by bugs 78957 and 79320. `startRemove` does refuse that pair | `Remove:64-73,185-191` versus `Reassign:66-76,157-175`; `Remove:155-160`; no test covers the pair | red by code reading |
| PG-4 | `sendCongratulations` | Accepts the key of any account, not only the portal Owner's, and on such a call switches the configured second factor on for the whole portal. Whether a non-owner can hold such a key was not checked | `Portal:1282-1302` | red by code reading |
| PG-5 | `signupThirdPartyAccount` | Downloads the provider's avatar URL with a plain client, without the URL validation `updateMemberPhoto` and `addMember` apply (bug 81491). The URL comes from a transported provider profile | `Third:458-470`; `Photo:242` | red by code reading |
