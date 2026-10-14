# file-sharing — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); test paths omit the `products/ASC.Files/Tests/Tests/` prefix. `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml`. Status: **TEST-CONFIRMED** (an integration test asserts it),
**CODE-ONLY** (read from the product code or the OpenAPI document, no test), **NOTES-ONLY** (only the
release notes say it — nothing of that kind was written into the spec; such items are listed at the end as
left out).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Sharing a file or folder from My documents with accounts and groups** | | |
| Accepted levels for a user/group share on an entry in My documents: ReadWrite, Editing, FillForms, Review, Comment, Read, Restrict, None; room roles and Varies not accepted | `products/ASC.Files/Core/Core/Security/FileSecurity.cs:85-96` (`_availableRoomFileAccesses[FolderType.USER]`) | CODE-ONLY |
| File shared with two accounts at Read / Editing — listed, each reads with its own access | `04_Security/FileShareTests.cs:516-583` | TEST-CONFIRMED |
| Folder shared with a group at Editing — group record, members read folder and files with Editing | `04_Security/FolderShareTests.cs:446-579` | TEST-CONFIRMED |
| File shared with a group at Comment — members get comment, not edit | `04_Security/FileShareTests.cs:586-653` | TEST-CONFIRMED |
| Access None withdraws: record disappears, former member gets 403 | `04_Security/Sharing/FileSecurityInfoReadTests.cs:195-205`, `FolderShareTests.cs:665-727`, `FileSecurityInfoPermissionsTests.cs:222-234` | TEST-CONFIRMED |
| Empty share list / empty access string → 200, empty list | `04_Security/Sharing/SetSecurityInfoTests.cs:48-69`; yaml `setFileSecurityInfo`, `setSecurityInfo` descriptions | TEST-CONFIRMED |
| Sharing message too long → 400 | `04_Security/Sharing/SetSecurityInfoTests.cs:127-143` (bug 79156 trait); yaml `setFileSecurityInfo` | TEST-CONFIRMED |
| **Who may share and with whom** | | |
| Account with a Read share cannot re-share → 403 | `04_Security/Sharing/SetSecurityInfoTests.cs:73-87, 147-156` | TEST-CONFIRMED |
| Guest cannot re-share → 403, own share kept, nothing added | `04_Security/Sharing/SetSecurityInfoTests.cs:97-123, 165-186` (bug 83263); `products/ASC.Files/Core/Utils/FileSharing.cs:631-634` (`CanSetAccessAsync` returns false for a guest) | TEST-CONFIRMED |
| Guest of another member silently dropped (Room admin, User) | `04_Security/FileShareTests.cs:1227-1258, 1288-1309`; `SetSecurityInfoTests.cs:190-206`; yaml `setFileSecurityInfo` | TEST-CONFIRMED |
| Own guest / DocSpace admin / Room admin / User accepted as recipients of a User's or Room admin's file | `04_Security/FileShareTests.cs:1246-1258, 1261-1285` | TEST-CONFIRMED |
| **Sharing state of an entry** | | |
| `sharedForUser` / `shared` / `sharedExternal` for user share, public link, internal link | `04_Security/FileShareTests.cs:1312-1377`; `FolderShareTests.cs:799-864` | TEST-CONFIRMED |
| `parentShared` on a file inside a folder with a link | `04_Security/FolderShareTests.cs:79-113` | TEST-CONFIRMED |
| **Reading the sharing rights of an entry** | | |
| Owner record: isOwner true, canEditAccess false, canRevoke false, subject type User; single record when unshared | `04_Security/Sharing/FileSecurityInfoReadTests.cs:60-107`; `GetSecurityInfoTests.cs:130-137, 252-261` | TEST-CONFIRMED |
| Shared user record: isOwner false, canRevoke false (canEditAccess true in the batch answer) | `FileSecurityInfoReadTests.cs:129-140, 214-225`; `GetSecurityInfoTests.cs:236-248, 375-387` | TEST-CONFIRMED |
| 401 anonymous, 403 without access, 404 missing / id 0 / negative / permanently deleted | `FileSecurityInfoPermissionsTests.cs:49-57, 161-170`; `FileSecurityInfoLifecycleTests.cs:85-108, 126-135`; `GetSecurityInfoPermissionsTests.cs:47-55, 131-140` | TEST-CONFIRMED |
| Shared account reads rights (single and batch) | `FileSecurityInfoPermissionsTests.cs:147-158`; `GetSecurityInfoPermissionsTests.cs:110-122` | TEST-CONFIRMED |
| Guest shared directly: single-file GET 403, batch POST 200 | `FileSecurityInfoPermissionsTests.cs:188-205`; `GetSecurityInfoPermissionsTests.cs:144-157` | TEST-CONFIRMED |
| Guest on `getFolderSecurityInfo` → 403 with or without access | `04_Security/Sharing/GetFolderSecurityInfoPermissionsTests.cs:47-84` (bug 79219) | TEST-CONFIRMED |
| File moved to trash: record gone, shared account 403 | `FileSecurityInfoLifecycleTests.cs:112-151` | TEST-CONFIRMED |
| Batch skips unknown ids, empty request → empty list, unreadable entry → 403 for User and Guest | `GetSecurityInfoTests.cs:351-357, 391-407`; `GetSecurityInfoPermissionsTests.cs:125-173` (bug 82675); yaml `getSecurityInfo` | TEST-CONFIRMED |
| **Access priority when several rights apply** | | |
| Order user → group (incl. admin group, Everyone) → current link → remembered links; closer level first | `FileSecurity.cs:3705-3759` (`GetUserOrderedSubjectsAsync`, `SubjectOrderType`), `FileSecurity.cs:2636-2679` (`GetCurrentShareAsync`: subject order, then `Level`) | CODE-ONLY |
| Personal Editing beats a Read link | `04_Security/FileShareTests.cs:656-697` | TEST-CONFIRMED |
| Group Comment applies; personal Read then beats group Comment and an Editing link | `04_Security/FileShareTests.cs:700-786` | TEST-CONFIRMED |
| Anonymous visitor gets the link's rights | `04_Security/FileShareTests.cs:773-786`; `ShareInheritanceTests.cs:193-218` | TEST-CONFIRMED |
| Everyone share does not reach an anonymous visitor (Read link → Read) | `04_Security/FileShareTests.cs:1191-1224` (bug 78985); `FileSecurity.cs:3724-3727` (Everyone added only for a real account) | TEST-CONFIRMED |
| Share on the file beats the share on its folder | `04_Security/FileShareTests.cs:1118-1151` (bug 78782) | TEST-CONFIRMED |
| Sub-folder share beats parent share, for the folder and for files inside; rights read by the account show ReadWrite | `FileShareTests.cs:1154-1188` (bug 78968); `FolderShareTests.cs:764-796`; `ShareInheritanceTests.cs:885-909` (bug 78848) | TEST-CONFIRMED |
| Folder share beats a link on a file / folder inside | `FileShareTests.cs:1042-1077` (bug 78426), `1080-1115` (bug 78852) | TEST-CONFIRMED |
| Share given on a file created inside a shared folder is recorded at the file level | `FileShareTests.cs:1007-1039` (bug 78660) | TEST-CONFIRMED |
| In rooms a personal right beats link rights | same ordering code (`FileSecurity.cs:3705-3759`); release notes v3.6 | CODE-ONLY |
| **The last opened link is remembered** | | |
| Opening via link + recent records a `RecentByLink` tag; earlier link tags for the entry are removed (only the last link counts) | `products/ASC.Files/Core/Utils/EntryManager.cs:2117-2134` (`MarkAsRecent`), `products/ASC.Files/Core/Utils/FileMarker.cs:1295-1313` (`MarkAsRecentByLink`); `FileSecurity.cs:3735-3744` (remembered links join the subject list) | CODE-ONLY |
| Three links in turn → access by id equals the last link | `04_Security/FileShareTests.cs:894-948` | TEST-CONFIRMED |
| Unused link deleted → file stays in Shared with me | `04_Security/FileShareTests.cs:951-1003` (bug 77819) | TEST-CONFIRMED |
| **The Shared with me section** | | |
| Shared file / folder listed with its access | `FileShareTests.cs:566-582, 851-891`; `FolderShareTests.cs:428-442` | TEST-CONFIRMED |
| Folder reached through a link appears after `getExternalShareData` | `04_Security/FolderShareTests.cs:729-762` (bug 77613) | TEST-CONFIRMED |
| `sharedBy` and `ownedBy` on entries read through a share; `sharedBy` filter on the listing | yaml `getFolderByFolderId` parameter `sharedBy` (l. 15545-15553), `FileDto.sharedBy`/`ownedBy` (l. 76280-76292); `FileSecurity.cs:2724-2732` (`GetSharesForMeAsync(sharedBy)`) | CODE-ONLY |
| Sub-folder of another member's My documents → 403 | `02_Folders/Read/FolderReadTests.cs:44-55` (bug 78077) | TEST-CONFIRMED |
| New-item count 1 then 2 after sharing files; folders too | `FileShareTests.cs:789-848`; `FolderShareTests.cs:582-662` | TEST-CONFIRMED |
| PDF / PDF form filter in Shared with me | `01_Files/Forms/SharedWithMePdfFilterTests.cs:47-100` | TEST-CONFIRMED |
| File whose parent the caller cannot open reports Shared with me as its folder | yaml `getFileInfo` description (l. 10630-10635), `FileDto.folderId` (l. 75764-75768) | CODE-ONLY |
| **Removing a share / Remove from list** | | |
| Owner revokes shares on file, folder, several, mixed; former user 403 | `04_Security/Sharing/RemoveSecurityInfoTests.cs:53-201` (bug 83259) | TEST-CONFIRMED |
| DocSpace admin on owner's file, Room admin on file in own room → 200 true | `RemoveSecurityInfoPermissionsTests.cs:60-106` | TEST-CONFIRMED |
| Recipient (Guest/User/Room admin, Read) removes file or folder from own Shared with me → 200 | `RemoveSecurityInfoPermissionsTests.cs:136-180` (bug 83262) | TEST-CONFIRMED |
| Unrelated account → 403, entry stays shared; 401 anonymous | `RemoveSecurityInfoPermissionsTests.cs:48-56, 116-125, 185-245` | TEST-CONFIRMED |
| Unknown ids / unshared entry / empty body → 200 true | `RemoveSecurityInfoTests.cs:151-158, 205-225` | TEST-CONFIRMED |
| Clears caller's favorites, recent and new marks; owner record and links untouched | yaml `removeSecurityInfo` description (l. 27327-27353) | CODE-ONLY |
| Folder reached by link removed, then reopened → back in the list | `04_Security/FolderShareTests.cs:729-762` | TEST-CONFIRMED |
| **Guests cannot re-share** | | |
| `CanSetAccessAsync` false for every guest regardless of access | `FileSharing.cs:631-634` | CODE-ONLY (403 itself: TEST-CONFIRMED above) |
| Guest sees only the owner in `getSharedUsers` | `04_Security/Sharing/SharedUsersTests.cs:155-172` (bug 81109) | TEST-CONFIRMED |
| Member list: no caller, no accounts without access; 403 / 401 / 404 | `SharedUsersTests.cs:47-145, 176-238, 286-291` (bug 83105) | TEST-CONFIRMED |
| **Sharing a PDF form from My documents** | | |
| Link rights on a form in My documents: Editing, FillForms, Comment, Read, None | `04_Security/ShareInheritanceTests.cs:829-842` | TEST-CONFIRMED |
| Link rights on a form in a Form filling room: FillForms, None | `04_Security/ShareInheritanceTests.cs:812-826` | TEST-CONFIRMED |
| User/group share on a form in My documents accepts FillForms | `FileSecurity.cs:88-91` | CODE-ONLY |
| Default primary link on a file in My documents: Read, no expiration, no password, download allowed, not internal | `04_Security/FileShareTests.cs:73-92`; `FolderShareTests.cs:79-104` | TEST-CONFIRMED |
| **The Recent section** | | |
| Files only, newest first, personal | yaml `getRecentFolder` description (l. 13906-13923) | CODE-ONLY |
| My documents, sub-folder and room files recorded; folder-type filters (My documents, rooms, one type, several) | `01_Files/Recent/RecentTests.cs:43-217` | TEST-CONFIRMED |
| Room file added by owner / manager / editor / commenter / guest; origin folder and room reported | `01_Files/Recent/RecentRoomFileAccessTests.cs:49-171`; `AddFileToRecentTests.cs:90-134` | TEST-CONFIRMED |
| Another member's private file → 403; anonymous → 401; missing → 404; idempotent | `01_Files/Recent/RecentPermissionsTests.cs:81-111`; `AddFileToRecentTests.cs:60-72, 138-149` | TEST-CONFIRMED |
| Delete from Recent: single, several, not-in-recent; 401 anonymous; no access check, no body | `01_Files/Recent/DeleteRecentTests.cs:43-90`; `DeleteRecentPermissionsTests.cs:46-93`; yaml `deleteRecent` | TEST-CONFIRMED |
| Recorded when opened through the file handler | `products/ASC.Files/Core/HttpHandlers/FileHandler.ashx.cs:497` (`TryMarkAsRecent`) → `EntryManager.MarkAsRecent` | CODE-ONLY |
| At most 1000 recent marks per account | `products/ASC.Files/Core/Core/Dao/TeamlabDao/TagDao.cs:61, 342-355` (`MaxRecentTagsCount`) | CODE-ONLY |
| **The Favorites section** | | |
| Read access suffices (User, Guest on a room file); 401 anonymous | `06_Operations/Favorites/AddFavoritesPermissionsTests.cs:47-120`; `DeleteFavoritesPermissionsTests.cs:46-115` | TEST-CONFIRMED |
| Add/remove answer true, idempotent, unknown ids ignored, empty body ok | `AddFavoritesTests.cs:46-218`; `DeleteFavoritesTests.cs:46-210` | TEST-CONFIRMED |
| Per account; lost room access drops entry and total; trash hides; archived room stays | `02_Folders/Favorites/FavoritesAccessTests.cs:60-192`; `FavoritesListingTests.cs:134-146`; `AddFavoritesTests.cs:222-234, 273-286` | TEST-CONFIRMED |
| isFavorite true, origin room title | `FavoritesListingTests.cs:72-98, 150-174` | TEST-CONFIRMED |
| Item-kind filter, folder-type filter, paging, sort, search, count 0 → 400 | `FavoritesFilterTypeTests.cs:47-150`; `06_Operations/Favorites/FavoritesTests.cs:49-341`; `FavoritesQueryTests.cs:48-163` | TEST-CONFIRMED |
| Toggle true then false | `06_Operations/Favorites/ToggleFileFavoriteTests.cs:46-69` | TEST-CONFIRMED |
| Removing the mark leaves the source listed | `DeleteFavoritesTests.cs:144-157` | TEST-CONFIRMED |
| **Default access rights** | | |
| Replace, single, empty resets to default, duplicates ok, no body 400, invalid value 400 | `07_Settings/SharingDefaults/DefaultAccessRightsTests.cs:95-169` (invalid value: bug 82641 trait) | TEST-CONFIRMED |
| Reflected in files settings, per account, every role, 401 anonymous / terminated | `DefaultAccessRightsTests.cs:172-250` | TEST-CONFIRMED |
| **Portal-wide external sharing switches** | | |
| Owner and DocSpace admin may change; Room admin / User / Guest 403; 401 anonymous / terminated | `07_Settings/SharingDefaults/ExternalShareTests.cs:120-177`; `ExternalShareSocialMediaTests.cs:126-186`; `ExternalSharingSettingsTests.cs:97-112` | TEST-CONFIRMED |
| Turning external sharing off also turns social media off; social media ANDed with external share | `ExternalShareTests.cs:90-100`; `ExternalShareSocialMediaTests.cs:111-122`; `ExternalSharingSettingsTests.cs:116-130` | TEST-CONFIRMED |
| Settings persisted and read back (defaultShareLinkInternal, applyToDocuments/Rooms, blockExistingLinks) | `ExternalSharingSettingsTests.cs:59-93` | TEST-CONFIRMED |
| Restriction for documents → file link internal; rooms only → public; block existing links → ExternalAccessDenied for anonymous, Ok for signed-in | `04_Security/ExternalShareAccessTests.cs:60-211` | TEST-CONFIRMED |

Counts: 75 rows — TEST-CONFIRMED 63, CODE-ONLY 12 (one row, the guest `CanSetAccessAsync` rule, carries
both), NOTES-ONLY 0 (nothing was written into the spec on the strength of the release notes alone).
Spec: 14 requirements, 60 scenarios.

## Where the server differs from the release notes (spec follows the server)

| Release note | Server |
|---|---|
| v3.5: "Recent shows the last 100 files" | `TagDao.MaxRecentTagsCount` is 1000: the 1000 newest recent / recent-by-link marks per account are kept, older ones are deleted on each save. The OpenAPI document names no limit. |
| v3.1 item 13: a link on a PDF form in Documents "is initially created for editing" | A primary link requested without parameters on a file in My documents is a Read link (`FileShareTests.cs:73-92`); Editing is offered but has to be asked for. |
| v3.6: the Shared by filter "is not available to USER and GUEST, who have no access to contacts" | No server-side refusal exists: the `sharedBy` parameter of `getFolderByFolderId` is accepted for every caller (yaml l. 15545-15553; `FileSecurity.GetSharesForMeAsync`). The restriction is a client-side rule, so it is not in the spec. |
| v3.6: "Author, Shared by, Access level" in the Details tab | The server supplies `createdBy`, `sharedBy`, `ownedBy` and `access` on the entry (yaml `FileDto`); the tab itself is client behaviour and is not specified. |
| OpenAPI `removeSecurityInfo` description: "a directly invited member … is refused" | The tests for bug 83262 (`RemoveSecurityInfoPermissionsTests.cs:136-180`) assert and pass that a member the entry was shared with directly removes it from their own list with 200; the spec follows the tests, the document text is stale. |

## Left out of the spec, and why

- **30-day handling of shared documents when a member is demoted to Guest** (v3.1 Contacts): no
  integration test covers it (`products/ASC.People/Tests/Tests/10_UserData` covers reassign/remove of a
  deactivated member's data, not demotion), and the OpenAPI document describes `POST /api/2.0/people/type`
  as handing rooms and shared files to the chosen admin in the same job, with no 30-day delay or read-only
  period mentioned. Nothing verifiable to write; left out rather than copied from the notes.
- **Recent marks a file "opened for viewing, editing, form submit"** (v3.5): the server records a file in
  `EntryManager.MarkAsRecent` whenever it is opened through the file handler or added explicitly; the exact
  editor events are client-side. The spec says "opened" and names the handler.
- **Limited context menu in Recent; "Move to" / "Copy" in Recent and Favorites menus** (v3.5, v4.0): client
  menus, not server behaviour.
- **Location tooltip in Recent** (v3.5): client behaviour.
- **Link settings, link counts, password pages, folder links for anonymous visitors** (v3.5): specified in
  `shared-links`; this spec only uses links where they interact with user and group shares.
- **Delete-confirmation and tar.gz settings** (`07_Settings/SharingDefaults/ChangeDeleteConfirmTests.cs`,
  `DownloadTarGzTests.cs`) and the **default template** (`DefaultTemplateTests.cs`): they sit in the
  SharingDefaults test folder but are not sharing behaviour.
- **Editor mention notifications** (`04_Security/Sharing/SendEditorNotify*Tests.cs`): editor feature, not
  sharing; several of its cases are open bugs (83430).
- **`getGroupsMembersWithFileSecurity`** and **`changeFileOwner`** (`GroupAndOwnerSharingTests.cs`): both are
  covered only by bug-trait tests (81023, 66897) whose current state is unknown; not asserted here.
- **Link-resolution flags `isAuthenticated` / `isRoomMember`** (`ShareResolveLink*Tests.cs`): belong to links.
- **Deviations D2, D11, D12** in `openspec/deviations.md` concern links and are not restated.
