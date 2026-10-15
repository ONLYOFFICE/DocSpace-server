# file-operations — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); test paths omit the `products/ASC.Files/Tests/Tests/` prefix. `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml`. Status: **TEST-CONFIRMED** (an integration test asserts it),
**CODE-ONLY** (read from the product code or the OpenAPI document, no test), **NOTES-ONLY** (only the
release notes say it; such claims are marked as coming from the notes inside the spec text, or left out).
Tests carrying a `Bug` trait are named with their number; the trait stays after a fix, so a numbered test is
not necessarily red. Where a test is known to be red today (its summary says so) the row says "open bug".

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Creating a blank file by title** | | |
| No extension → `.docx`; `.docx` kept; `.txt` → `.docx`; `.md` kept (bug 80324) | `01_Files/Create/FileCreateVariantsTests.cs:47-99` | TEST-CONFIRMED |
| `.docx/.pptx/.xlsx/.pdf` created in My documents by Owner, DocSpace admin, Room admin, User; `version` 1 | `01_Files/Create/FileCreateTests.cs:69-99`; `01_Files/Read/FileInfoTests.cs:42-54` | TEST-CONFIRMED |
| File in a room reports the room as `folderId` | `FileCreateVariantsTests.cs:101-114`; `FileInfoTests.cs:56-68` | TEST-CONFIRMED |
| Section roots (Archive, Trash, Rooms, AI Agents, Forms) and an unresolvable folder id → 403 | `FileCreateTests.cs:101-121` | TEST-CONFIRMED |
| 166 chars + `.docx` → 400 (limit 170) | `FileCreateTests.cs:123-140` | TEST-CONFIRMED |
| Template from `new/<culture>/` when present, else `new/default/` | `products/ASC.Files/Core/Helpers/Global.cs:331-343, 404-423` (`GetNewDocTemplatePath`, `GetPathDependingOnCulture`); `Helpers/Constants.cs:48-51` | CODE-ONLY |
| `en-US` template is Letter, default is A4 | release notes v3.0 ("При создании нового документа для языка en-US ... Letter ... A4") | NOTES-ONLY |
| **Creating a text or HTML file from content** | | |
| `.txt`/`.html` appended, `folderId`, `fileExst` | `01_Files/Create/TextFileCreateTests.cs:46-59, 120-135`; `HtmlFileCreateTests.cs:46-61, 84-97`; `FileCreateTests.cs:142-194` | TEST-CONFIRMED |
| Missing `content` → 400 (My documents and room) | `TextFileCreateTests.cs:62-71, 178-189`; `HtmlFileCreateTests.cs:99-109` | TEST-CONFIRMED |
| `createNewIfExist:true` returns the existing file; false/omitted creates another | `TextFileCreateTests.cs:73-118, 137-176, 202-216`; `HtmlFileCreateTests.cs:63-82, 111-156` | TEST-CONFIRMED |
| Empty title 400 (bug 81440), 300-char title 400, diacritics kept, folder 999999999 → 404 | `TextFileCreateTests.cs:191-259` | TEST-CONFIRMED |
| **Creating a folder** | | |
| Created in a room (`parentId` = room) and in My documents by Owner, Room admin, User; names with spaces/dots kept; nested child | `02_Folders/Crud/FolderCreateTests.cs:58-113, 199-219` | TEST-CONFIRMED |
| Same title twice in one parent succeeds | `FolderCreateTests.cs:177-197` | TEST-CONFIRMED |
| Section roots 403; AI room Knowledge 403 and stays empty; 166 chars 400; unknown parent refused | `FolderCreateTests.cs:115-175` | TEST-CONFIRMED |
| Empty title 400 (bug 81507, on rename; the same `CreateFolder` DTO validation applies to creation) | `02_Folders/Crud/FolderRenameTests.cs:176-193` | TEST-CONFIRMED (rename) / CODE-ONLY (create) |
| **Renaming a file** | | |
| Extension preserved / appended (`Renamed.txt` → `Renamed.txt.docx`) | `01_Files/Update/FileRenameTests.cs:46-76, 98-116`; `FileUpdateTests.cs:54-72` | TEST-CONFIRMED |
| Empty / whitespace title ignored (200); `&` kept; `/` → `_` | `FileRenameTests.cs:118-148, 173-205` | TEST-CONFIRMED |
| 166 chars + `.docx` → 400; 999999999 → 404 "The required file was not found" (bug 80774) | `FileUpdateTests.cs:74-92`; `FileRenameTests.cs:78-96` | TEST-CONFIRMED |
| `lastVersion` equal to current → 400 (bug 80773) | `FileRenameTests.cs:150-171` | TEST-CONFIRMED |
| **Renaming a folder** | | |
| Only the title changes; id, parent, counters, type, root kept; persisted; old title gone from listing; same title 200; Cyrillic kept | `02_Folders/Crud/FolderRenameTests.cs:47-159`; `FolderUpdateTests.cs:42-60` | TEST-CONFIRMED |
| Empty 400 (81507); 166 chars 400; id 0 and 999999999 → 404 (81508) | `FolderRenameTests.cs:161-209`; `FolderUpdateTests.cs:62-80` | TEST-CONFIRMED |
| **Single-request upload** | | |
| One file returned, `folderId`, `createdBy`, listed; subfolder; My documents; `@my/upload`; `@my/insert` with `title` override | `02_Folders/Upload/UploadTests.cs:46-81, 155-174`; `UploadToMyTests.cs:46-78, 132-140`; `InsertToMyTests.cs:46-99` | TEST-CONFIRMED |
| `fileExst` follows the name for pdf/xlsx/pptx/png/jpg/zip/csv/md; special characters kept; `pureContentLength` | `UploadFormatTests.cs:46-62`; `UploadToMyFormatTests.cs:46-60`; `UploadTests.cs:105-124, 176-185`; `InsertToMyTests.cs:141-151` | TEST-CONFIRMED |
| Omitted/false `createNewIfExist` → new version, same id; true → new id; false with no clash → created | `UploadTests.cs:83-93, 126-137`; `UploadToMyTests.cs:80-101, 142-149`; `InsertToMyTests.cs:101-122`; yaml `uploadFile` (l. 18050-18066) | TEST-CONFIRMED |
| Empty file accepted (v3.6), `0 bytes` on insert | `UploadTests.cs:95-103`; `UploadToMyTests.cs:113-119`; `InsertToMyTests.cs:124-130` | TEST-CONFIRMED |
| No file part → 400 (bugs 81547, 81549; insert) | `UploadTests.cs:210-225`; `UploadToMyTests.cs:121-130`; `InsertToMyTests.cs:132-139` | TEST-CONFIRMED |
| `storeOriginalFile` accepted, extension kept | `UploadTests.cs:187-196`; `UploadToMyTests.cs:151-161` | TEST-CONFIRMED |
| Archived room 403; folder 0 / 999999999 → 404 | `UploadTests.cs:198-243` | TEST-CONFIRMED |
| **Chunked upload session** | | |
| Session id, chunks of `chunkUploadSize`, finalize → `uploaded`, title, `file.folderId`, `pureContentLength`, bytes identical | `02_Folders/Upload/ChunkedUploadTests.cs:44-95` | TEST-CONFIRMED |
| Twelve-hour sliding expiration; 402 above the chunked-upload limit; 403 section root / no create right; 404 unknown folder | yaml `createUploadSession` (l. 22300-22359), `ChunkedUploadSessionDto.expired` (l. 71792-71802); `products/ASC.Files/Core/Utils/FileUploader.cs:153-166, 205-218, 296-304` | CODE-ONLY |
| Knowledge folder: unsupported format / size refused | `FileUploader.cs:265-278`; yaml `createUploadSession` 402/403 | CODE-ONLY |
| PDF classified as form or plain PDF from the first chunk | `FileUploader.cs:306-362`; `06_Operations/CheckDestFolder/CheckDestFolderTestBase.cs:117-159` (uploads a real form this way and the server tags it) | TEST-CONFIRMED |
| `createNewIfExist` upload semantics on a session | yaml `SessionRequest.createNewIfExist` (l. 84046-84053); `FileUploader.cs:88-125` | CODE-ONLY |
| Abort before parts: no file; abort after finalize; abort in room; own session by Owner / Content creator | `06_Operations/UploadSession/AbortUploadSessionTests.cs:49-126`; `AbortUploadSessionPermissionsTests.cs:67-101` | TEST-CONFIRMED |
| Abort of another account's session → 403 (bug 82276); anonymous 401 | `UploadSessionAbortBugTests.cs:68-126`; `FileUploader.cs:398-413` (owner check now in place); `AbortUploadSessionPermissionsTests.cs:47-65` | TEST-CONFIRMED |
| Multi-threaded chunk upload | release notes v2.5 ("чанки грузятся в несколько потоков"); the API accepts numbered chunks (`uploadAsyncSession`) | NOTES-ONLY (mentioned as the reason for numbered chunks) |
| **Upload pre-check for name clashes** | | |
| Existing titles returned with `count`; new titles not; mixed; several; empty list | `02_Folders/UploadCheck/UploadCheckTests.cs:42-109, 192-204` | TEST-CONFIRMED |
| Case-insensitive; duplicate request titles once (bug 81365); subfolder | `UploadCheckTests.cs:122-163` | TEST-CONFIRMED |
| `filesTitle` null 400; `{}` 400 (81331); folder 999999999 → 404 (81330) | `UploadCheckTests.cs:111-120, 165-190` | TEST-CONFIRMED |
| **Long-running operations** | | |
| `FileOperationDto` fields, type values, dropped once finished, per caller | yaml `FileOperationDto` (l. 77019-77133), `FileOperationType` (l. 77148-77170), `getOperationStatuses` (l. 21150-21188) | CODE-ONLY |
| Finished delete: Operation Delete, finished, progress 100, processed "1", empty error | `01_Files/Deletion/FileDeleteTests.cs:68-126` | TEST-CONFIRMED |
| Empty list on a fresh portal and for an unknown id | `06_Operations/Statuses/GetOperationStatusesTests.cs:48-66` | TEST-CONFIRMED |
| By type: Delete/Copy/Duplicate/Download/MarkAsRead empty; Delete with id 9999999 empty; type 99 → 400 | `GetOperationStatusesByTypeTests.cs:51-66, 98-117` | TEST-CONFIRMED |
| `fileops/move` status route shadowed by the check endpoint | `GetOperationStatusesByTypeTests.cs:44-50` (summary) | CODE-ONLY |
| `returnSingleOperation:true` → one Move record; `deleteAfter:true` → record gone | `06_Operations/Move/FileMoveOperationTests.cs:46-119` | TEST-CONFIRMED |
| Starting call may return an already pruned (empty) list | `02_Folders/Deletion/FolderDeleteTests.cs:41-47`; `BatchDeleteTestBase.cs:44-62`; `BulkDownloadTestBase.cs:43-61` | TEST-CONFIRMED |
| **Cancelling an operation** | | |
| Delete / duplicate / copy / move / markAsRead / emptyTrash terminated and gone | `06_Operations/Terminate/TerminateTasksTests.cs:60-190` | TEST-CONFIRMED |
| Unknown id, finished id, another user's id, Guest, anonymous → 200 empty | `TerminateTasksTests.cs:49-58, 96-114`; `TerminateTasksPermissionsTests.cs:48-127` | TEST-CONFIRMED |
| Cancelling does not undo work | yaml `terminateTasks` (l. 22050-22061) | CODE-ONLY |
| **Moving files and folders** | | |
| File to folder, several files to room, between rooms, room to My documents, between My documents subfolders | `06_Operations/Move/FileMoveTests.cs:42-79, 198-330` | TEST-CONFIRMED |
| Folder with inner file into room; mixed batch; subfolder to subfolder; `content:true` | `FoldersMoveTests.cs:203-241, 274-427` | TEST-CONFIRMED |
| Collaboration/Public/VDR accept; archived 403; FFR refuses `.docx`, accepts a form | `FileMoveRoomTypeTests.cs:46-170` | TEST-CONFIRMED |
| Empty batch 200; move onto own parent 200 | `FileMoveConflictTests.cs:171-208` | TEST-CONFIRMED |
| Forms root 403; AI Agents root 403; another user's file 403; form-folder case 403; room into archive/My documents 403 | `FileMoveTests.cs:81-196`; `FoldersMoveTests.cs:42-153` | TEST-CONFIRMED |
| Content creator cannot move out of the room; User cannot move a room (65580); Editing/Review 403; Guest 403; anonymous 401; Room manager, Content creator at destination, DocSpace admin succeed | `FileMovePermissionsTests.cs:46-422` | TEST-CONFIRMED |
| File id 999999999 → 404 (bug 82243; summary says the product returned 403) | `FileMoveOperationTests.cs:121-142` | TEST-CONFIRMED (open bug, see defects) |
| Move into a third-party room completes without error (bug 83271, Nextcloud-gated, self-skipping) | `ThirdPartyMoveBugTests.cs:59-95` | TEST-CONFIRMED (skipped without Nextcloud) |
| **Copying files and folders** | | |
| Source preserved: My documents → room, room → My documents, two files, inter-room, subfolder source; Public/Collaboration/VDR destinations | `06_Operations/Copy/FileCopyBatchTests.cs:47-98, 138-170, 224-270` | TEST-CONFIRMED |
| Folder with content, `content:true`, mixed, multiple, empty folder; level-2/3 destinations; level-3 source | `FolderCopyTests.cs:154-257`; `CopyNestedDestinationTests.cs:47-163` | TEST-CONFIRMED |
| `copyFileAs` same extension into a room / with rename | `01_Files/CopySave/CopyFileAsTests.cs:60-80`; `FileCopyTests.cs:42-68, 101-124` | TEST-CONFIRMED |
| `deleteAfter:true` keeps the source; duplicate id → one copy; empty batch 200 | `FileCopyBatchTests.cs:172-222, 272-286` | TEST-CONFIRMED |
| Folder into own subfolder 403; FFR refuses docx files and a room; Forms root 403; archived 403; form folder 403 | `FolderCopyTests.cs:42-125`; `FileCopyTests.cs:219-273, 333-362`; `FileCopyBatchTests.cs:99-118` | TEST-CONFIRMED |
| Locked / editing file copied by another user 403; unsupported format into Knowledge 403 | `FileCopyTests.cs:364-529` | TEST-CONFIRMED |
| PDF form copied into FFR, with and without `toFillOut` | `FileCopyTests.cs:531-561` | TEST-CONFIRMED |
| File 999999999 → 404 (82204); destination 999999999 → 404; no permission / foreign folder 403; anonymous 401 | `FileCopyTests.cs:126-331`; `FileCopyBatchTests.cs:120-136`; `CopyPermissionsTests.cs:71-90, 163-206, 254-300` | TEST-CONFIRMED |
| Who may copy (Owner, DocSpace admin, Room admin, Content creator incl. level 3, Guest with CC, folder from room by CC — bug 81906) | `CopyPermissionsTests.cs:48-69, 92-161, 208-252, 302-375` | TEST-CONFIRMED |
| **Settling a name clash on move and copy** | | |
| Skip: one file, original id, skipped move stays in source (room and level 3) | `CopyConflictResolutionTests.cs:48-67, 126-145`; `FileMoveConflictTests.cs:47-91` | TEST-CONFIRMED |
| Overwrite: one file, original id (room and level 3) | `CopyConflictResolutionTests.cs:69-91, 147-169`; `FileMoveConflictTests.cs:93-132` | TEST-CONFIRMED |
| Duplicate: two files; same room with Duplicate | `CopyConflictResolutionTests.cs:93-124, 171-187`; `FileMoveConflictTests.cs:134-169` | TEST-CONFIRMED |
| **Checking a move or copy for name clashes** | | |
| Clash reported with Skip and Overwrite; none → empty; Duplicate no clash → empty; own folder reported | `06_Operations/CheckMoveCopy/CheckMoveOrCopyConflictTests.cs:46-158` | TEST-CONFIRMED |
| Room types, several files, `deleteAfter:false`, mixed, `content:true`, empty → empty | `CheckMoveOrCopyDestinationTests.cs:47-202` | TEST-CONFIRMED |
| Archived 403; source 999999999 skipped; dest 999999999 → 404 (81881); no dest → 400 (81882) | `CheckMoveOrCopyErrorTests.cs:47-136` | TEST-CONFIRMED |
| Flat query binding; encrypted files left out of the report | `CheckMoveCopyTestBase.cs:36-50`; yaml `checkMoveOrCopyBatchItems` (l. 21495-21507) | CODE-ONLY |
| **Checking whether a destination accepts the files** | | |
| AllAllowed with `files` listing the sources; two files; Collaboration/Public/VDR; folder source; `deleteAfter:false` | `06_Operations/CheckDestFolder/CheckDestFolderTests.cs:42-161` | TEST-CONFIRMED |
| FFR: docx NoneAllowed, form AllAllowed | `CheckDestFolderTests.cs:96-122` | TEST-CONFIRMED |
| Archived 403 (82103); User/Guest without access 403 (82104); 999999999 → 404 (82158); no dest 400 (82159) | `CheckDestFolderBugTests.cs:47-143` | TEST-CONFIRMED |
| `PartAllowed` value; folder ids ignored | yaml `CheckDestFolderResult` (l. 71569-71581), `checkMoveOrCopyDestFolder` (l. 21810-21830) | CODE-ONLY |
| **Duplicating files and folders** | | |
| ` (1)` suffix on file and folder, operation lists the copy | `06_Operations/Copy/FileCopyTests.cs:70-99`; `Move/FoldersMoveTests.cs:243-272` | TEST-CONFIRMED |
| File/folder in My documents, several files, in a room, in a subfolder, diacritics, nested content, mixed | `06_Operations/Duplicate/DuplicateBatchItemsTests.cs:47-130, 222-280` | TEST-CONFIRMED |
| Empty batch 200; 999999999 file/folder → 404 (82210); archived 403; Trash 403 | `DuplicateBatchItemsTests.cs:132-220` | TEST-CONFIRMED |
| Numeric suffix grows with every repetition | yaml `duplicateBatchItems` (l. 21725-21742) | CODE-ONLY |
| **Deleting a single file or folder** | | |
| File to Trash (Trash as `folderId`, title kept) and immediately; room file immediately; folder immediately / to Trash; subtree; folder with files; created by another member | `01_Files/Deletion/FileDeleteTests.cs:47-126, 265-278, 613-649`; `02_Folders/Deletion/FolderDeleteTests.cs:54-204`; `Crud/FolderUpdateTests.cs:82-107` | TEST-CONFIRMED |
| 99999 → 404 "The required file was not found"; folder 99999 → 404 and double delete → 404 (79459) | `FileDeleteTests.cs:128-145`; `FolderDeleteTests.cs:206-251` | TEST-CONFIRMED |
| Locked (room, My documents, shared) and editing file → 403 for another user | `FileDeleteTests.cs:169-263` | TEST-CONFIRMED |
| Roles: DocSpace admin / Room manager / own file as CC succeed; Editing, Read, no access, Guest 403 "Access denied"; anonymous 401; another user's folder 403 | `01_Files/Deletion/FileDeletePermissionsTests.cs:49-247`; `FolderDeleteTests.cs:253-272`; `FolderDeletePermissionsTests.cs` | TEST-CONFIRMED |
| **Deleting a batch** | | |
| To Trash and gone from source; permanent; folder to Trash; folder with files permanent; mixed; empty; 999999999 → 404; file already in Trash removed for good | `06_Operations/BatchDelete/BatchDeleteTests.cs:48-216` | TEST-CONFIRMED |
| Anonymous 401 | `BatchDeletePermissionsTests.cs:49-63` | TEST-CONFIRMED |
| **The Trash remembers where an entry came from** | | |
| Files: no filter / USER / VirtualRooms / one room type / two types | `FileDeleteTests.cs:280-408, 496-510` | TEST-CONFIRMED |
| Folders: the same filters | `FolderDeleteTests.cs:274-411` | TEST-CONFIRMED |
| `roomId` filter; file inside a trashed folder listed | `FileDeleteTests.cs:613-649` | TEST-CONFIRMED |
| **Emptying the Trash** | | |
| File, folder, mixed, already empty, room file; `single:true` | `06_Operations/EmptyTrash/EmptyTrashTests.cs:47-157` | TEST-CONFIRMED |
| `folderType=USER` leaves room and form-room entries (82588); `VirtualRooms` leaves My documents entries; no filter clears all | `EmptyTrashTests.cs:159-201`; `FileDeleteTests.cs:410-494, 537-564` | TEST-CONFIRMED |
| Per account: Owner, User, DocSpace admin, Room admin, Guest; Guest does not touch the Owner's; anonymous 401 | `EmptyTrashPermissionsTests.cs:49-184` | TEST-CONFIRMED |
| **File versions and their history** | | |
| New file: one version, `version` 1, `versionGroup` 1, fields, `Created`, `fileStatus` None; room file | `01_Files/History/FileVersionInfoTests.cs:43-128` | TEST-CONFIRMED |
| `getFileVersionInfo` 999999999 → 403; `getEditHistory` → 404 (80962) | `FileVersionInfoTests.cs:102-113`; `EditHistoryTests.cs:96-113` | TEST-CONFIRMED |
| Edit history: one revision with key/user/created; grows after `lastVersion` 2; archived room readable | `EditHistoryTests.cs:43-94, 115-144` | TEST-CONFIRMED |
| `getFileInfo(version)` returns that version | `FileVersionInfoTests.cs:130-147` | TEST-CONFIRMED |
| Split / merge groups; structure; room file; 999999999 → 404; archived 403 | `01_Files/Versions/ChangeVersionHistoryTests.cs:43-206` | TEST-CONFIRMED |
| Restore: revisions returned, history grows, structure, room; no params 400; 999999999 → 404; archived 403; GET rejected (78027) | `RestoreVersionTests.cs:43-204` | TEST-CONFIRMED |
| Read member cannot open `version=1` through `openedit` (80683) | `01_Files/History/OpenFileVersionAccessTests.cs:48-75` | TEST-CONFIRMED |
| Restore stores the content as a new version with a comment; `url` source | yaml `restoreFileVersion` (l. 11415-11433), `updateFile` (l. 10751-10756) | CODE-ONLY |
| **Version comments** | | |
| Set, empty, null, overwrite, Unicode, HTML-like | `06_Operations/Comment/CommentTests.cs:47-140`; `01_Files/Update/FileUpdateTests.cs:282-302` (read back through `getFileInfo`) | TEST-CONFIRMED |
| 2000 chars 400 (82266); 999999999 → 404 (82268); version 999 / 0 / -1 → 400 (82271) | `CommentTests.cs:142-241` | TEST-CONFIRMED |
| **Deleting versions** | | |
| One version, two versions, file stays listed, non-existent version ignored, room file, `returnSingleOperation` | `06_Operations/DeleteVersion/DeleteVersionTests.cs:43-113, 130-148, 205-221` | TEST-CONFIRMED |
| 999999999 → 404; archived 403; Trash 403; null `versions` 200 | `DeleteVersionTests.cs:115-128, 150-203` | TEST-CONFIRMED |
| Anonymous 401; own file by Owner / User; other user's file 403; Room manager in own room; DocSpace admin on another user's My documents 403; Guest Read 403 | `DeleteVersionPermissionsTests.cs:43-178` | TEST-CONFIRMED |
| Current version number refused; empty list deletes the whole file | yaml `deleteFileVersions` (l. 20895-20910) | CODE-ONLY |
| **Locking and unlocking a file** | | |
| My documents (v3.7) and room lock; idempotent lock and unlock; `getFileInfo` agrees | `01_Files/Lock/FileLockTests.cs:46-123`; `Update/FileUpdateTests.cs:94-138` | TEST-CONFIRMED |
| Only the named file unlocked (82178); 999999999 → 404 (80788) | `FileLockTests.cs:125-168` | TEST-CONFIRMED |
| Room manager unlocks a CC's lock; CC cannot unlock the Owner's; CC cannot unlock another CC's; Room manager unlocks another manager's | `FileLockPermissionsTests.cs:175-231`; `FileUpdateTests.cs:211-280` | TEST-CONFIRMED |
| No access 403, Guest 403, anonymous 401; DocSpace admin as manager, Room manager on the Owner's file, CC on own file succeed; Editing/FillForms/Read 403 | `FileLockPermissionsTests.cs:49-173`; `FileUpdateTests.cs:140-209` | TEST-CONFIRMED |
| Locked file blocks deletion and copy by others | `FileDeleteTests.cs:169-243`; `FileCopyTests.cs:364-394` | TEST-CONFIRMED |
| **Downloading a file** | | |
| `presigneduri`: `stream_auth=`, `fileid=`, starts with http; `presigned`: `url` and `filetype` `.docx` | `01_Files/Presigned/PresignedUriTests.cs:45-59`; `PresignedFileUriTests.cs:45-58`; `06_Operations/Statuses/FileOperationsTests.cs:41-56` | TEST-CONFIRMED |
| Read member, DocSpace admin, Room manager succeed; User/Guest without access 403; anonymous 401; 999999999 → 404 | `PresignedUriTests.cs:61-125`; `PresignedFileUriTests.cs:60-177` | TEST-CONFIRMED |
| `viewUrl` serves the content | `ChunkedUploadTests.cs:84-93` | TEST-CONFIRMED |
| **Bulk download as an archive** | | |
| One/two files, one/two folders, mixed, room file: Download, finished, empty error, `url` with `filehandler.ashx?action=bulk`, `processed` "1"; `returnSingleOperation` | `06_Operations/BulkDownload/BulkDownloadTests.cs:48-199` | TEST-CONFIRMED |
| `fileConvertIds` with `pdf` finishes | `BulkDownloadTests.cs:227-246` | TEST-CONFIRMED |
| `password` for a protected source (v3.1) | yaml `DownloadRequestItemDto.password` (l. 73985-73994) | CODE-ONLY |
| Empty selection 200; 999999999 → 404 | `BulkDownloadTests.cs:201-225` | TEST-CONFIRMED |
| Room manager, Editing, Read, Guest Read succeed; non-member 403 (81822); anonymous 401 (81823) | `BulkDownloadPermissionsTests.cs:49-233` | TEST-CONFIRMED |
| One download at a time per caller | yaml `bulkDownload` (l. 20647-20650) | CODE-ONLY |
| **Converting a file** | | |
| docx/xlsx/pptx → pdf: empty list with `PUT checkconversion` link; `startConvert:false` link; status: empty list with `GET` link | `06_Operations/CheckConversion/CheckConversionStatusTests.cs:51-140` | TEST-CONFIRMED |
| 999999999 → 200 empty list | `CheckConversionStatusTests.cs:142-153`; yaml `checkConversionStatus` (l. 20300-20301) | TEST-CONFIRMED |
| Owner, DocSpace admin, Room manager, CC on own file succeed; no access 403; anonymous 401 (start and status) | `StartFileConversionPermissionsTests.cs:51-133`; `CheckConversionStatusPermissionsTests.cs:47-102` | TEST-CONFIRMED |
| Editing member can poll the status (bug 81825, open) | `CheckConversionStatusBugTests.cs:51-71` | TEST-CONFIRMED (open bug) |
| `fileops/convert` and `fileops/import` → 400 (bug 82225, open) | `GetOperationStatusesByTypeTests.cs:68-96` | TEST-CONFIRMED (open bug) |
| Target formats from the `FileFormats` configuration (document server list); request fields | `products/ASC.Files/Core/Helpers/FileUtility.cs:58-114`; yaml `CheckConversionRequestDto` (l. 71485-71544) | CODE-ONLY |
| XML → docx/xlsx (v3.0), presentation → txt (v3.6), new formats (v3.2.1), docx → docxf removed (v2.6) | release notes only | NOTES-ONLY |
| **Thumbnails** | | |
| One/two files echoed; folder id dropped; 999999999 echoed | `01_Files/Thumbnails/ThumbnailsCreateTests.cs:48-85, 116-175` | TEST-CONFIRMED |
| Guest Read, User Read, Room manager, DocSpace admin may queue | `ThumbnailsPermissionsTests.cs:69-167` | TEST-CONFIRMED |
| Sizes 1280×720 and 3840×2160 (v2.6); 30 MB image / 1000 MB video limits | `buildtools/config/appsettings.json:318-324`; `products/ASC.Files/Core/Core/ThumbnailSettings.cs:101-111` | CODE-ONLY |
| **Custom Filter mode of a spreadsheet** | | |
| Enable → `customFilterEnabled` true and `webCustomFilterEditing` true; disable clears; 999999999 → 404 | `01_Files/CustomFilter/CustomFilterTests.cs:46-92` | TEST-CONFIRMED |
| Only spreadsheet formats; who may switch it off | yaml `setCustomFilterTag` (l. 11302-11311) | CODE-ONLY |
| **New marks of a folder and marking as read** | | |
| Array for room / My documents; empty room; fields; file by another member appears; subfolder does not; several; two users; own files not | `02_Folders/News/FolderNewItemsTests.cs:49-193, 252-266` | TEST-CONFIRMED |
| Created before the visit not listed; revisit clears (81712) | `FolderNewItemsTests.cs:195-250` | TEST-CONFIRMED |
| Folder 999999999 / 0 → 404 (81519) | `FolderNewItemsTests.cs:272-302` | TEST-CONFIRMED |
| Mark one file, two files, folder (clears its files), idempotent; folder/mixed answer a MarkAsRead operation | `06_Operations/MarkAsRead/MarkAsReadTests.cs:47-215` | TEST-CONFIRMED |
| `{}` 200; empty ids 200; 999999999 200; non-member 200; every role 200; anonymous 401 | `MarkAsReadTests.cs:138-175`; `MarkAsReadPermissionsTests.cs:49-102` | TEST-CONFIRMED |
| **Reference data for the editors** | | |
| Keys from `openedit` resolve; roundtrip; `sourceFileId`; all optional fields; Read member `canEditRoom` false | `01_Files/ReferenceData/ReferenceDataTests.cs:49-152` | TEST-CONFIRMED |
| Unknown keys → 200 with `error` | `ReferenceDataTests.cs:154-175` | TEST-CONFIRMED |
| **Personal file templates** | | |
| One, two, twice, empty, `{}`, 999999999 → true | `01_Files/Templates/AddTemplatesTests.cs:47-134` | TEST-CONFIRMED |

Counts: 141 rows — TEST-CONFIRMED 121 (four of them open bugs or Nextcloud-gated, marked in the row; one
row, the blank folder title on creation, carries TEST-CONFIRMED and CODE-ONLY), CODE-ONLY 18, NOTES-ONLY 3
(the Letter/A4 paper size, the multi-threaded chunk upload and the format pairs of conversion; each is
attributed to the release notes inside the spec text). Spec: 32 requirements, 92 scenarios.

## Where the server differs from the release notes or the API document (spec follows the server)

| Source | Server |
|---|---|
| OpenAPI `createThumbnails`: "an anonymous caller without such a link is refused" (401) | The action carries `[AllowAnonymous]`; `ThumbnailsPermissionsTests.cs:44-67` (bug 81268) asserts 401 and is red. Not written as a requirement; listed under defects. |
| OpenAPI `restoreFileVersion`: `version` "already the current one" is refused with 400 | Confirmed only for the `lastVersion` path of `updateFile` (`FileRenameTests.cs:150-171`); the restore route itself is tested with a non-current version only. |
| OpenAPI `checkConversionStatus`: "the same empty list is the answer for an identifier no file matches" vs its own 404 entry "The file id is neither a number nor the id of a file in a known third-party storage" | `CheckConversionStatusTests.cs:142-153` asserts 200 with an empty list for id 999999999 on `startFileConversion`; the spec says so. |
| OpenAPI `lockFile`: "a lock set by somebody else can only be released by a room manager" | Tests agree (`FileLockPermissionsTests.cs:175-231`) and add that a second Room manager also releases the lock of a Room manager (`FileUpdateTests.cs:243-280`). |
| Release notes v2.5: "the overwrite-confirmation dialog is always shown on upload" | Client behaviour. The server default of the upload calls (`createNewIfExist` omitted) is to add a version to the existing file (`UploadTests.cs:126-137`); the spec states the server default. |
| Release notes v2.6: "thumbnails are not created when the file is larger than 30 MB" | The limit is per media kind: `MaxImageFileSize` 30 MB, `MaxVideoFileSize` 1000 MB (`ThumbnailSettings.cs:101-111`), both configurable. |
| Release notes v3.0: en-US template is Letter, others A4 | The server selects `new/<culture>/` by culture prefix and falls back to `new/default/` (`Global.cs:404-423`); the paper size of the template files is not visible in the code and is attributed to the notes in the spec. |

## Left out of the spec, and why

- **Editing sessions, `openedit`, `startedit`, `trackedit`, editor notifications, `getEditDiff`**
  (`01_Files/Editing`, `EditSession`, `EditDiff`): editor-internal mechanics; only the version-access case
  of `openedit` (bug 80683) is used because it governs version reading.
- **File and folder order inside an indexed room** (`01_Files/Order`, `02_Folders/Crud/FolderOrderTests.cs`):
  specified in `room-types` (manual order and reorder).
- **Favorites, Recent, links, sharing, security info** (`01_Files/Links`, `Recent`, `02_Folders/Favorites`,
  `Links*`): `file-sharing` and `shared-links`.
- **Form filling, PDF form checks, form filters, fill results** (`01_Files/FormFilling`, `Forms`,
  `02_Folders/FormFilter`): `form-filling-workflow`.
- **Folder history / activity log, file log** (`02_Folders/History`, `01_Files/Log`): audit, not a file
  operation.
- **Used space and quota accounting** (`02_Folders/UsedSpace`): `room-types` (quota) and portal settings.
- **Root folders, folder info, folder path, subfolder listing** (`02_Folders/Subfolders`, `Read`): listing and
  navigation rather than operations; the Trash listing is kept only as far as it shows origin tracking.
- **Document text extraction strategies** (`09_Text`, `MarkdownDocumentProcessingStrategyTests` etc.): unit
  tests of the AI chunking strategies, not file operations.
- **Deleting personal templates** (`01_Files/Templates/DeleteTemplatesTests.cs`,
  `TemplatesPermissionsTests.cs`): not read; only the add call is specified.
- **Restoring from Trash**: no test moves an entry back out of the Trash; the server treats it as a move
  (`moveBatchItems`) whose source is the Trash, and the Origin tags that drive it are described in
  `.claude/rules/files-sections.md`, not asserted anywhere. Not specified.
- **`deleteAfter` on `deleteFile`** ("postpones the deletion until the editing session has ended",
  OpenAPI): no test exercises an editing session, so only the field's presence is mentioned.
- **`templateId` / `formId` sources of `createFile`, `copyFileAs` with a different extension or `toForm`,
  synchronous conversion (`sync:true`)**: they need a document server, which the integration host does not run.
- **Conversion format pairs** (XML → docx/xlsx, pptx → txt, docx → docxf forbidden): the server reads the
  list from the `FileFormats` configuration supplied by the document server; nothing in the repo tests or
  pins the pairs, so they are attributed to the notes and not stated as rules.
- **Third-party storage variants** (string ids, `x-thirdparty-variant` operations): only the Nextcloud-gated
  move test exists; the rest is untested here.

## Suspected defects (not written into the spec)

| # | Where | What the server does | Evidence |
|---|---|---|---|
| F1 | thumbnails | `POST api/2.0/files/thumbnails` is `[AllowAnonymous]`; an anonymous caller without a link is accepted although the API document says 401. | `01_Files/Thumbnails/ThumbnailsPermissionsTests.cs:44-67` (bug 81268, red) |
| F2 | thumbnails | A request without `fileIds` (`{}` body or only `folderIds`) answers 400 with a stack trace (`ArgumentNullException` on `inDto.FileIds.ToList()`) instead of 200 with an empty list. | `ThumbnailsCreateTests.cs:87-109, 136-156` (trait `unfiled-thumbnails-null-fileids`, red) |
| F3 | copy | `copyBatchItems` with a folder id that does not exist answers 500; a file id that does not exist answers 404. | `06_Operations/Copy/FolderCopyTests.cs:127-152` asserts the 500 |
| F4 | move | `moveBatchItems` with a file id that does not exist answers 403 instead of 404. | `06_Operations/Move/FileMoveOperationTests.cs:121-142` (bug 82243, summary says 403 today) |
| F5 | upload session | Aborting a session id that never existed answers 500 instead of 404. | `06_Operations/UploadSession/UploadSessionAbortBugTests.cs:47-66` (bug 82278) |
| F6 | conversion | `GET fileops/convert` and `GET fileops/import` answer 400 instead of an empty list; `GET fileops/move` is shadowed by the check endpoint and dies on the missing `destFolderId`. | `GetOperationStatusesByTypeTests.cs:44-96` (bug 82225) |
| F7 | conversion | A room member with Editing rights gets 403 from `checkConversionStatus` although Editing lets them open the file. | `CheckConversionStatusBugTests.cs:46-71` (bug 81825) |
| F8 | mark as read | `markAsRead` on the My documents root clears the "new" marks of an unrelated room too (the mark is one shared tag per user, same root cause as 82588). | `06_Operations/MarkAsRead/MarkAsReadTests.cs:217-271` (bug 83509) |
| F9 | mark as read | No per-item access check: a User who is not a member of the room marks its file as read with 200. | `MarkAsReadPermissionsTests.cs:89-102` (asserted as current behaviour) |
| F10 | version info | `getFileVersionInfo` answers 403 for a file id that does not exist while `getEditHistory` answers 404 for the same id. | `FileVersionInfoTests.cs:102-113` vs `EditHistoryTests.cs:100-113` |
| F11 | text/HTML creation | `createNewIfExist` is inverted with respect to every upload call: `true` returns the existing file, `false` creates a new one. | `TextFileCreateTests.cs:73-118`; `HtmlFileCreateTests.cs:63-82` (asserted as current behaviour; written into the spec as the server does it) |
| F12 | create file | `createFile` with a folder id that does not exist answers 403 instead of 404 (the text/HTML twin answers 404). | `FileCreateTests.cs:101-112` vs `TextFileCreateTests.cs:191-200` |
| F13 | SDK contract | `checkMoveOrCopyBatchItems`, `checkMoveOrCopyDestFolder` and `deleteFileVersions` cannot be called through the generated clients (query binding / single-vs-array response); the tests use raw HTTP. | `CheckMoveCopyTestBase.cs:36-50`; `CheckDestFolderTestBase.cs:51-59`; `DeleteVersionTestBase.cs:70-75` |
