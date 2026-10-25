# room-role-permissions — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); Files test paths omit the `products/ASC.Files/Tests/Tests/` prefix. `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml` (submodule `a0fc3a2`). Status: **TEST-CONFIRMED** (an
integration test asserts it), **CODE-ONLY** (read from the product code or the OpenAPI document, no test),
**LIVE-CONFIRMED** (only the live run of 2026-10-14 recorded in `openspec/deviations.md` asserts it),
**MATRIX-ONLY** (stated by the product matrix, sheet "Roles"; no test, code or live evidence found —
flagged).

Role → right mapping used by the tests and the code: the roles are the `FileShare` values Room manager
(`RoomManager`), Content creator (`ContentCreator`), Editor (`Editing`), Form Filler (`FillForms`),
Reviewer (`Review`), Commentator (`Comment`), Viewer (`Read`); the numeric values are not used anywhere in
the spec. The per-room-type role table is `products/ASC.Files/Core/Core/Security/FileSecurity.cs:158-235`
(`AvailableRoomAccesses`), the per-user-type table `FileSecurity.cs:237-252` (`AvailableUserAccesses`);
both are enforced in `products/ASC.Files/Core/Utils/FileSharing.cs:180-188` and `:265-271` with the message
"The role is not available for this user type" (`products/ASC.Files/Core/Resources/FilesCommonResource.resx:331`),
which no test asserts (only the 403). Shared theory data of the suite: `03_Rooms/RoomAccessData.cs:43-66`
(all accesses; the User / Guest set without Room manager; update / link access expectations) and
`.claude/rules/tests.md:170-187`. Unless a row says otherwise the test runs on a Custom room and the member
is a User-type account invited with the stated role.

Revision 2026-10-25: first verification; the spec was reshaped at the same time (matrices moved into a
lead scenario per requirement; the prose about demotion, self-role, foreign guests, copying and locking
split into the scenarios "Room manager invites a foreign guest", "Copying does not depend on authorship",
"Non-member administrators restore an archived room"; no requirement or scenario renamed).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Roles available in each room type** | | |
| Collaboration room: Viewer, Editor, Content creator accepted; Form Filler, Reviewer, Commentator → 403 and member absent | `03_Rooms/Sharing/RoomShareAccessLevelTests.cs:62-67, 104-114` | TEST-CONFIRMED |
| VDR: Viewer, Editor, Form Filler, Content creator accepted; Reviewer, Commentator → 403 | `RoomShareAccessLevelTests.cs:75-80` | TEST-CONFIRMED |
| Public room: Content creator accepted; Viewer, Editor → 403; Room manager accepted | `RoomShareAccessLevelTests.cs:70-72`; `03_Rooms/Permissions/RoomLinkPermissionsTests.cs:98-124, 273-299` (Room manager invited in Arrange) | TEST-CONFIRMED |
| Public room: Reviewer, Commentator, Form Filler → 403 | `FileSecurity.cs:186-196` | CODE-ONLY |
| Form filling room: Form Filler accepted; Editor, Viewer → 403; Content creator accepted | `RoomShareAccessLevelTests.cs:56-59`; `03_Rooms/Invitations/RoomInvitationResendTests.cs:243-254` | TEST-CONFIRMED |
| Form filling room: Reviewer, Commentator → 403 | `FileSecurity.cs:197-207` | CODE-ONLY |
| Custom room: every role but Form Filler accepted | every Permissions suite invites with the full access set (`RoomReadPermissionsTests.cs:129-147`; `RoomDeletePermissionsTests.cs:186-231`); `RoomShareSetTests.cs:87-105`; `RoomShareReadTests.cs:79-90, 127-142` | TEST-CONFIRMED |
| Custom room: Form Filler → 403 | `FileSecurity.cs:162-185` (no `FillForms` in the Custom set) | CODE-ONLY |
| Room Owner is the creator, not assignable | `FileShare` has no owner value; ownership is transferred through `setRoomSecurity`'s owner-change path (`room-types`, "Room ownership transfer") | CODE-ONLY |
| **Roles available to each user type** | | |
| Room manager refused for a User / Guest by the Owner → 403, member absent | `RoomSharePermissionsTests.cs:248-270`; `RoomShareAccessLevelTests.cs:59` | TEST-CONFIRMED |
| Guest as Content creator (invitation accepted, used for copying) | `06_Operations/Copy/CopyPermissionsTests.cs:302-324`; `RoomReadPermissionsTests.cs:129-147` (Guest × Content creator) | TEST-CONFIRMED |
| User / Guest as Content creator, Editor, Reviewer, Commentator, Viewer; User as Form Filler | `RoomDeletePermissionsTests.cs:186-206` (Arrange); `RoomShareAccessLevelTests.cs:56, 77` | TEST-CONFIRMED |
| Owner, DocSpace admin, Room admin hold every role | `RoomReadPermissionsTests.cs:129-147`; `02_Folders/Upload/UploadPermissionsTests.cs:127-138`; `01_Files/EditSession/EditSessionPermissionsTests.cs:65`; `02_Folders/Crud/FolderRenamePermissionsTests.cs:51`; `CopyPermissionsTests.cs:382-420` | TEST-CONFIRMED |
| Full per-type table | `FileSecurity.cs:237-252` | CODE-ONLY |
| User / Guest cannot own a room → 403 | `room-types` spec, scenario "User cannot become the owner" and its `verification.md` | CODE-ONLY (cross-reference) |
| **Room management by role** | | |
| Edit the room: Room manager 200, Content creator / Editor / Viewer 403; Owner 200 | `RoomUpdatePermissionsTests.cs:43-58, 166-199` | TEST-CONFIRMED |
| Invite / assign roles: Room manager adds a member with a role; revoked manager 403; Owner | `RoomSharePermissionsTests.cs:43-59, 196-242` | TEST-CONFIRMED |
| Change roles / remove: Owner (Read → Editing; access None removes) | `03_Rooms/Sharing/RoomShareReadTests.cs:144-206`; `RoomShareSetTests.cs:67-85, 107-144`; `02_Folders/History/FolderHistoryMembershipTests.cs:82-98` | TEST-CONFIRMED |
| Content creator … Viewer cannot invite, change roles, remove (403, list unchanged) | `FileSharing.cs:87-104` (`CanSetAccessAsync` demands the manage right); `deviations.md` matrix row "Every role invites users … / Only Owner and Room manager invite; no moderation exists" | LIVE-CONFIRMED |
| Room links: Room manager 200, Content creator 403 (Public); Viewer 403 (Custom); Owner 200 | `RoomLinkPermissionsTests.cs:227-248, 273-322`; `02_Folders/LinksPrimary/FolderLinkPrimaryPermissionsTests.cs:49-136` | TEST-CONFIRMED |
| Leave the room: member removes only themselves | `FileSharing.cs:85` (`selfLeave` bypasses the manage check); `yaml` `setRoomSecurity` (30843) | CODE-ONLY |
| Owner cannot leave: 200, owner still in the room | `RoomShareSetTests.cs:171-190` | TEST-CONFIRMED |
| View member list: Owner, Room manager, Viewer; non-members 403 | `RoomShareReadPermissionsTests.cs:46-152` (bugs 81787, 81788) | TEST-CONFIRMED |
| View room history: Owner, Room manager, Viewer; non-members 403 | `02_Folders/History/FolderHistoryPermissionsTests.cs:48-144` | TEST-CONFIRMED |
| View room info: every role of every type | `RoomReadPermissionsTests.cs:129-147` | TEST-CONFIRMED |
| Archive: Owner; Room manager 403 | `03_Rooms/Archive/RoomArchiveTests.cs:47-77`; `yaml` `archiveRoom` (30627) — the Room manager case has no test (`RoomArchivePermissionsTests.cs:83-102` uses non-members) | TEST-CONFIRMED (Owner) / CODE-ONLY (Room manager) |
| Delete: Owner; every invited role including Room manager 403 | `RoomDeletePermissionsTests.cs:49-63, 186-231` | TEST-CONFIRMED |
| Room manager demotes another Room manager; lowers own role; cannot restore it; no other member changes own role | `deviations.md` matrix row "Nobody changes own role / A Room manager can lower own role; any member can leave" | LIVE-CONFIRMED |
| Invitations take effect immediately | `RoomShareSetTests.cs:46-65` | TEST-CONFIRMED |
| Room manager inviting a foreign guest → empty result | `RoomSharePermissionsTests.cs:174-194` (bug 79361; the inviter is the Room admin who owns the room, not an invited manager); `04_Security/RoomShareTests.cs:888-921` | TEST-CONFIRMED |
| **Working with file content by role** | | |
| View content: Viewer, Commentator, Reviewer, Editor, Content creator, Room manager, Guest Viewer, Form Filler (FFR) | `02_Folders/Subfolders/SubfoldersPermissionsTests.cs:75-92, 144-160, 181-216` (bug 81463); `01_Files/Read/FileInfoPermissionsTests.cs:59-100`; `01_Files/History/OpenFileVersionAccessTests.cs:48-75` | TEST-CONFIRMED |
| Download: Owner, Room manager, Editor, Viewer, Guest Viewer | `06_Operations/BulkDownload/BulkDownloadPermissionsTests.cs:49-186`; `01_Files/Presigned/PresignedFileUriTests.cs:108-140` | TEST-CONFIRMED |
| Comment: Viewer no, Commentator yes, Editor yes (security flags of the file) | `04_Security/RoomShareTests.cs:605-672` (rights acquired through room links of a Custom room) | TEST-CONFIRMED |
| Edit: Room manager, Editor yes; Viewer, Commentator, Guest no (403 "You do not have enough permissions to edit the file") | `01_Files/EditSession/EditSessionPermissionsTests.cs:64-125`; `01_Files/Editing/EditingStartPermissionsTests.cs:62-102`; `EditingTrackPermissionsTests.cs:62-101` (bugs 81224, 81225) | TEST-CONFIRMED |
| Review: Reviewer and above yes; Form Filler, Commentator, Viewer no | `yaml` `openEdit` / `getFileSecurityInfo` (26974) rights; `FileSecurity.CanReviewAsync` (`products/ASC.Files/Core/Core/Security/IFileSecurity.cs:40`) | CODE-ONLY |
| Fill in form fields: Form Filler and above except Reviewer / Commentator / Viewer | `IFileSecurity.cs:42`; the role-specific cases in `vdr-room-roles` and `form-filling-room-roles` | CODE-ONLY |
| View comments, copy to clipboard, print for every role | editor permissions derived from the read right; not observable through the API | MATRIX-ONLY |
| **Managing files and folders by role** | | |
| Create files / folders: Content creator yes; Viewer 403 | `01_Files/Create/TextFileCreatePermissionsTests.cs:142-189`; `02_Folders/Deletion/FolderDeletePermissionsTests.cs:138-164` (Arrange) | TEST-CONFIRMED |
| Upload: Room manager, Content creator yes; Editor, Viewer, Guest Viewer 403 | `02_Folders/Upload/UploadPermissionsTests.cs:85-151`; `02_Folders/UploadCheck/UploadCheckPermissionsTests.cs:72-171`; `06_Operations/UploadSession/AbortUploadSessionPermissionsTests.cs:82-100` | TEST-CONFIRMED |
| Copy from My documents into the room: Content creator yes (room root and level-3 subfolder), Viewer / Editor 403; Owner as Room manager of a foreign room of every type | `CopyPermissionsTests.cs:142-161, 231-300, 382-420, 486-505`; `06_Operations/Move/FileMovePermissionsTests.cs:220-282, 368-402` (Reviewer 403) | TEST-CONFIRMED |
| Lock: Room manager locks the owner's file; Content creator locks own file; Editor, Form Filler, Viewer 403 (VDR) | `01_Files/Lock/FileLockPermissionsTests.cs:72-117, 137-157`; `01_Files/Update/FileUpdateTests.cs:140-209` | TEST-CONFIRMED |
| Content creator locks a file created by the owner and unlocks it again | `deviations.md` matrix row "A Content creator locks only own files / Locks any unlocked file; cannot unlock someone else's lock" | LIVE-CONFIRMED |
| Unlock a foreign lock: Room manager yes; Content creator 403 (even on own file) | `FileLockPermissionsTests.cs:175-231`; `FileUpdateTests.cs:211-280` | TEST-CONFIRMED |
| View version history: Room manager, Editor yes; Commentator, Viewer, Guest 403 | `01_Files/History/EditHistoryPermissionsTests.cs:57-196`; `01_Files/EditDiff/GetEditDiffPermissionsTests.cs:81-141` | TEST-CONFIRMED |
| Manage version history: Room manager yes; Editor, Viewer, Guest 403 | `01_Files/Versions/ChangeVersionHistoryPermissionsTests.cs:98-216`; `RestoreVersionPermissionsTests.cs:98-171`; `06_Operations/DeleteVersion/DeleteVersionPermissionsTests.cs:114-177` | TEST-CONFIRMED |
| Save a docxf as oform: Content creator and above | `yaml` `saveAsPdf` / conversion operations demand the create right; `01_Files/CopySave/SaveFileAsPdfPermissionsTests.cs:48-75` (Editor, Viewer, Guest 403; the positive case needs the document server) | CODE-ONLY |
| Delete own: Content creator own file, own folder; Room manager own folder | `01_Files/Deletion/FileDeletePermissionsTests.cs:110-139`; `FolderDeletePermissionsTests.cs:138-193` | TEST-CONFIRMED |
| Rename own: Content creator own file | `01_Files/Update/FileUpdatePermissionsTests.cs:181-204`; `deviations.md` matrix row "Rename: Owner and Room manager only / A Content creator renames own entries" | TEST-CONFIRMED |
| Move own: Content creator | `FileMovePermissionsTests.cs:368-402` (from My documents into the room); within the room: `FileSecurity.CanDeleteAsync` on the source (`IFileSecurity.cs:45`) | CODE-ONLY |
| Delete others': Room manager yes; Content creator 403 (folder, bug 79459); Editor, Viewer, Guest 403 | `FileDeletePermissionsTests.cs:79-108, 145-231`; `FolderDeletePermissionsTests.cs:111-136, 195-270`; `06_Operations/BatchDelete/BatchDeletePermissionsTests.cs:86-170` | TEST-CONFIRMED |
| Move others': Content creator cannot move the owner's file out of the room (403) | `FileMovePermissionsTests.cs:284-315` | TEST-CONFIRMED |
| Rename others': Room manager yes; Editor, Viewer, Guest 403 "Access denied"; Content creator 403 on the owner's folder | `FileUpdatePermissionsTests.cs:49-159, 206-229`; `02_Folders/Crud/FolderRenamePermissionsTests.cs:46-53, 115-132` | TEST-CONFIRMED |
| Copy out of the room: Content creator folder → My documents (bug 81906), Content creator → another room; Guest Viewer → a room where they are Content creator | `CopyPermissionsTests.cs:302-375`; `deviations.md` matrix row "Copy of other members' files … / Copy does not depend on authorship; every role copies out" | TEST-CONFIRMED |
| Viewer, Reviewer, Commentator, Editor copy out to My documents | `deviations.md` row above (live run) | LIVE-CONFIRMED |
| Viewer loses copy-out when the room denies downloading | `vdr-room-roles`, scenario "Viewer in a room with the download restriction"; `04_Security/ShareInheritanceTests.cs:266-295` (download flag false for a deny-download link) | CODE-ONLY |
| Copy within the room: Content creator and above; Viewer / Editor into the room root 403 | `CopyPermissionsTests.cs:254-300`; the destination needs the create right | TEST-CONFIRMED |
| File and folder links: Room manager yes; Content creator, Viewer 403 (room folder) | `FolderLinkPrimaryPermissionsTests.cs:49-136`; `02_Folders/Links/FolderLinkCreateTests.cs:338-398` (bug 81575, red: 200 with an empty list instead of 403) | TEST-CONFIRMED |
| Guest has no My documents to copy from | `TextFileCreatePermissionsTests.cs:102-113` | TEST-CONFIRMED |
| **Version history of third-party provider files** | | |
| No version history for provider files | `yaml` `getFileVersionInfo` (10180) remarks; `03_Rooms/ThirdParty/ThirdPartyFileTests.cs` (Nextcloud-gated) calls no history endpoint | CODE-ONLY |
| **Archived room is read-only** | | |
| Owner: create folder / file, update room, tags, invite, resend, rename room, upload, copy / move in, version management, duplicate, save as PDF → 403 | `RoomArchiveTests.cs:99-147` (bug 81551); `RoomUnarchiveTests.cs:300-319`; `RoomShareValidationTests.cs:176-193`; `RoomInvitationTests.cs:206-223`; `02_Folders/Upload/UploadTests.cs:199-208`; `UploadCheckPermissionsTests.cs:173-185`; `06_Operations/Copy/FileCopyBatchTests.cs:100-118`; `06_Operations/Move/FileMoveRoomTypeTests.cs:85-111`; `FoldersMoveTests.cs:43-94`; `ChangeVersionHistoryTests.cs:190`; `RestoreVersionTests.cs:154`; `DeleteVersionTests.cs:151`; `DuplicateBatchItemsTests.cs:180`; `SaveFileAsPdfTests.cs:90` | TEST-CONFIRMED |
| Editor cannot edit, comment, rename, delete or lock in an archived room | the archived check precedes every role check (`room-types`, "Archiving a room"); Owner cases above | CODE-ONLY |
| View content in an archived room: Viewer 200; Owner | `SubfoldersPermissionsTests.cs:264-283`; `RoomArchiveTests.cs:79-97` | TEST-CONFIRMED |
| View member list / history / info in an archived room | Owner: `03_Rooms/Read/RoomInfoTests.cs:105-120`; `02_Folders/History/FolderHistoryQueryTests.cs:212`; other roles: read rights unchanged by archiving | TEST-CONFIRMED (Owner) / CODE-ONLY (roles) |
| View file version history in an archived room: Owner yes; Reviewer / Commentator / Viewer 403 | `01_Files/History/EditHistoryTests.cs:131-143`; the role rule is the active-room rule (`EditHistoryPermissionsTests.cs:99-196`) | TEST-CONFIRMED (Owner) / CODE-ONLY (roles) |
| Copy out of an archived room to My documents or own rooms, every role | `deviations.md` matrix row "Copy out of an archived room: Owner, Room manager, Content creator / Every role" | LIVE-CONFIRMED |
| Restore: Owner yes; invited Viewer / Editor 403; non-members 403; removed member 403; non-member DocSpace admin yes | `RoomArchivePermissionsTests.cs:182-306` | TEST-CONFIRMED |
| Room manager cannot restore or delete an archived room | `yaml` `unarchiveRoom` (30734), `deleteRoom` (30525): the room's owner or a portal admin; active-room delete by a Room manager 403 (`RoomDeletePermissionsTests.cs:212-231`) | CODE-ONLY |
| Delete an archived room: Owner | `03_Rooms/Deletion/RoomDeleteTests.cs:115` | TEST-CONFIRMED |
| Download, print, clipboard, comments in an archived room | derived from the read right | MATRIX-ONLY |

Counts: 61 rows — TEST-CONFIRMED 40, CODE-ONLY 14, LIVE-CONFIRMED 6, MATRIX-ONLY 2 (the editor-side
"view comments / copy to clipboard / print" rows, active and archived — not observable through the API),
plus 1 cross-reference. Spec: 7 requirements, 40 scenarios (before the reshaping: 7 requirements, 31
scenarios).

## Where the server differs from the product matrix (spec follows the server)

| Matrix (sheet "Roles") | Server | Reference |
|---|---|---|
| Every role invites users and assigns roles; Owner and Room manager "moderate" invitations | Only Owner and Room manager invite; no moderation exists | `deviations.md`, matrix table |
| Nobody changes own role | A Room manager can lower own role; any member can leave | same |
| A Content creator locks only own files | Locks any unlocked file; cannot unlock someone else's lock | same |
| Rename: Owner and Room manager only | A Content creator renames own entries | same; `FileUpdatePermissionsTests.cs:181-204` |
| Copy of other members' files: Owner and Room manager; copy out: Owner, Room manager, Content creator | Copy does not depend on authorship; every role copies out (a Viewer unless download is denied) | same; `CopyPermissionsTests.cs:302-375` |
| Copy out of an archived room: Owner, Room manager, Content creator | Every role | same |
| Form Filler is a role of a general room | Only VDR and Form filling rooms accept it; a Public room accepts only Content creator and Room manager | same; `FileSecurity.cs:158-235` |
| Test name `SetRoomSecurity_RoomAdminInvitesForeignGuest_SilentlySkipped` | The inviter is the Room admin who owns the room; the spec's "Room manager invites a foreign guest" is the same code path (`FileSharing.cs:273-300`) but not asserted for an invited manager | `RoomSharePermissionsTests.cs:174-194` |
| Test file `06_Operations/Comment/CommentPermissionsTests.cs:90-148` (Editor, Viewer, Guest → 403) | This is the *version comment* (`updateFileComment`, 21380), not document commenting; it does not contradict the Comment row of the content matrix | — |

## Left out of the spec, and why

- **Non-member DocSpace admin rights on a room** (info, member list, history and primary link 200; update,
  invite, link, rename, version info 403; archive / unarchive 200): specified in `user-type-permissions`
  ("Actions on rooms owned by someone else").
- **Content creator reads the room's primary link** (`RoomLinkPermissionsTests.cs:98-124`): `shared-links`
  ("Roles that manage links", scenario "Member reads the room's primary link").
- **Resend of invitations is Room manager only** (`RoomResendPermissionsTests.cs:152-185`): `invitations`.
- **Aborting another member's upload session** (bug 82276, open; `UploadSessionAbortBugTests.cs:75-124`):
  `file-operations`.
- **Form roles endpoint readable by Viewer / Content creator / Room manager** (`FormRolesPermissionsTests.cs:79-171`):
  `form-filling-workflow`.
- **Viewer opens the current version but not an explicit past version** (bug 80683,
  `OpenFileVersionAccessTests.cs:48-75`): `file-operations` (versions).
- **Anonymous answers 403 instead of 401** on restore version, edit history, version info, edit diff
  ("ported as observed", bugs 80752, 81168, 81231, 81571): `file-operations`, defect F10 / H2 family.
- **Share list of a non-member User / Guest → 403** (bugs 81787, 81788): `user-type-permissions`.

## Suspected defects

D-numbers are the ones of `openspec/deviations.md`; R-numbers are new.

| # | Where | What the server does | Evidence | Status |
|---|-------|----------------------|----------|--------|
| R4 | File and folder links by role | `POST folder/{id}/link` (primary link create) by a Content creator, a Viewer, a non-member DocSpace admin, a Guest, or on an archived room answers 200 with an empty list instead of 403. | `02_Folders/Links/FolderLinkCreateTests.cs:271-421` (bug 81575, tests red) | OPEN |
| R5 | Content matrix | `updateFileComment` (the version comment, `PUT file/{id}/comment`) is refused for an Editor (403) although an Editor may edit the file; only the create right passes. | `06_Operations/Comment/CommentPermissionsTests.cs:90-148` | OPEN (design question; the spec treats it as outside the Comment row) |
