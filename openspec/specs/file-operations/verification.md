# file-operations — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); test paths omit the `products/ASC.Files/Tests/Tests/` prefix. `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml` (line numbers of the submodule revision `5ea4dc6`). Status:
**TEST-CONFIRMED** (an integration test asserts it), **CODE-ONLY** (read from the product code or the OpenAPI
document, no test), **NOTES-ONLY** (only the release notes say it; such claims are marked as coming from the
notes inside the spec text, or left out). Tests carrying a `Bug` trait are named with their number; the trait
stays after a fix, so a numbered test is not necessarily red. Where a test is known to be red today (its
summary says so, or it carries `Category=Bug`) the row says "open bug". Exception → status:
`SecurityException`/`InvalidOperationException` 403, `ArgumentException` 400, `ItemNotFoundException`,
`FileNotFoundException` and `DirectoryNotFoundException` 404, `AuthenticationException` 401, a plain
`Exception` 500 (`common/ASC.Api.Core/Middleware/ResponseWrapper.cs:53-143`).

Revised after the merge of release/v4.1.0 (`3533ad8a89`, previous state `d58816bece`): the integration host
runs a document server since `bbba22fbf1`, the 06_Operations suite gained the UploadSession, Statuses,
BatchContent, CheckPreflight, CheckConversion, Comment, DeleteVersion, Duplicate and MarkAsRead content
suites (`6109dd948e`, `04d7b2fb32`), the pre-checks, uploads and conversion were tightened in `e9cfda8d43`,
and the listings publish `entryKind` (`7d641bf729`). The sources are the suites `01_Files`, `02_Folders` and
`06_Operations`, the yaml and the product code named per row. No test was run in this revision: "red" means the
test carries the mark or its summary says so, "red by code reading" means the path was read as well.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Creating a blank file by title** | | |
| No extension → `.docx`; `.docx` kept; `.txt` → `.docx`; `.md` kept (bug 80324) | `01_Files/Create/FileCreateVariantsTests.cs:47-99` | TEST-CONFIRMED [run: PASS] |
| `.docx/.pptx/.xlsx/.pdf` created in My documents by Owner, DocSpace admin, Room admin, User; `version` 1 | `01_Files/Create/FileCreateTests.cs:69-99`; `01_Files/Read/FileInfoTests.cs:42-54` | TEST-CONFIRMED [run: PASS] |
| File in a room reports the room as `folderId` | `FileCreateVariantsTests.cs:101-114`; `FileInfoTests.cs:56-68` | TEST-CONFIRMED [run: PASS] |
| Section roots (Archive, Trash, Rooms, AI Agents, Forms) and an unresolvable folder id → 403 | `FileCreateTests.cs:101-121` | TEST-CONFIRMED [run: PASS] |
| 166 chars + `.docx` → 400 (limit 170) | `FileCreateTests.cs:123-140` | TEST-CONFIRMED [run: PASS] |
| Template from `new/<culture>/` when present, else `new/default/` | `products/ASC.Files/Core/Helpers/Global.cs:331-343, 404-423` (`GetNewDocTemplatePath`, `GetPathDependingOnCulture`); `Helpers/Constants.cs:48-51` | CODE-ONLY |
| `en-US` template is Letter, default is A4 | release notes v3.0 ("При создании нового документа для языка en-US ... Letter ... A4") | NOTES-ONLY |
| Non-PDF file (docx, text, HTML) in a Form filling room refused with 403 | `01_Files/Create/FormsRoomNonPdfCreateTests.cs:63-95` (bug 84840; asserts 400-499, the 403 is `FileDao.cs:826-828` read with `common/ASC.Api.Core/Middleware/ResponseWrapper.cs:86-88` and the operation's `SwaggerResponse`) | TEST-CONFIRMED [run: PASS] (the range), CODE-ONLY (the exact 403) |
| **Creating a text or HTML file from content** | | |
| `.txt`/`.html` appended, `folderId`, `fileExst` | `01_Files/Create/TextFileCreateTests.cs:46-59, 120-135`; `HtmlFileCreateTests.cs:46-61, 84-97`; `FileCreateTests.cs:142-194` | TEST-CONFIRMED [run: PASS] |
| Missing `content` → 400 (My documents and room) | `TextFileCreateTests.cs:62-71, 178-189`; `HtmlFileCreateTests.cs:99-109` | TEST-CONFIRMED [run: PASS] |
| `createNewIfExist:true` returns the existing file; false/omitted creates another | `TextFileCreateTests.cs:73-118, 137-176, 202-216`; `HtmlFileCreateTests.cs:63-82, 111-156` | TEST-CONFIRMED [run: PASS] |
| Empty title 400 (bug 81440), 300-char title 400, diacritics kept, folder 999999999 → 404 | `TextFileCreateTests.cs:191-259` | TEST-CONFIRMED [run: PASS] |
| **Creating a folder** | | |
| Created in a room (`parentId` = room) and in My documents by Owner, Room admin, User; names with spaces/dots kept; nested child | `02_Folders/Crud/FolderCreateTests.cs:58-113, 199-219` | TEST-CONFIRMED [run: PASS] |
| Same title twice in one parent succeeds | `FolderCreateTests.cs:177-197` | TEST-CONFIRMED [run: PASS] |
| Section roots 403; AI room Knowledge 403 and stays empty; 166 chars 400; unknown parent refused | `FolderCreateTests.cs:115-175` | TEST-CONFIRMED [run: PASS] |
| Instructions room refuses a sub-folder (403); Knowledge and AI project rooms and the AI rooms root likewise | `03_Rooms/Instructions/InstructionsRoomFolderTests.cs:41-54`; `products/ASC.Files/Core/Core/FileStorageService.cs:1190-1193` | TEST-CONFIRMED [run: PASS] (Instructions) / CODE-ONLY (the others) |
| Empty title 400 (bug 81507, on rename; the same `CreateFolder` DTO validation applies to creation) | `02_Folders/Crud/FolderRenameTests.cs:176-193` | TEST-CONFIRMED [run: PASS] (rename) / CODE-ONLY (create) |
| **Renaming a file** | | |
| Extension preserved / appended (`Renamed.txt` → `Renamed.txt.docx`) | `01_Files/Update/FileRenameTests.cs:46-76, 98-116`; `FileUpdateTests.cs:54-72` | TEST-CONFIRMED [run: PASS] |
| Empty / whitespace title ignored (200); `&` kept; `/` → `_` | `FileRenameTests.cs:118-148, 173-205` | TEST-CONFIRMED [run: PASS] |
| 166 chars + `.docx` → 400; 999999999 → 404 "The required file was not found" (bug 80774) | `FileUpdateTests.cs:74-92`; `FileRenameTests.cs:78-96` | TEST-CONFIRMED [run: PASS] |
| `lastVersion` equal to current → 400 (bug 80773) | `FileRenameTests.cs:150-171` | TEST-CONFIRMED [run: PASS] |
| Locked by the Owner: a Content creator's rename changes nothing; refusal as a client error | `01_Files/Update/FileRenameLockedTests.cs:77-92` (unchanged), `94-111` (bug 84842, red: 500 today) | TEST-CONFIRMED [run: FAIL-BUG] (open bug for the code) |
| **Renaming a folder** | | |
| Only the title changes; id, parent, counters, type, root kept; persisted; old title gone from listing; same title 200; Cyrillic kept | `02_Folders/Crud/FolderRenameTests.cs:47-159`; `FolderUpdateTests.cs:42-60` | TEST-CONFIRMED [run: PASS] |
| Empty 400 (81507); 166 chars 400; id 0 and 999999999 → 404 (81508) | `FolderRenameTests.cs:161-209`; `FolderUpdateTests.cs:62-80` | TEST-CONFIRMED [run: PASS] |
| **Single-request upload** | | |
| One file returned, `folderId`, `createdBy`, listed; subfolder; My documents; `@my/upload`; `@my/insert` with `title` override | `02_Folders/Upload/UploadTests.cs:47-82, 156-176`; `UploadToMyTests.cs:47-79, 133-141`; `InsertToMyTests.cs:46-99` | TEST-CONFIRMED [run: PASS] |
| `insertFile` into a folder of My documents with the contract's `insertFile.*` field names; the flat names read first | `02_Folders/Upload/InsertToFolderTests.cs:46-74`; `products/ASC.Files/Core/ApiModels/Binders.cs:260-296` | TEST-CONFIRMED [run: PASS] |
| `fileExst` follows the name for pdf/xlsx/pptx/png/jpg/zip/csv/md; special characters kept; `pureContentLength` | `UploadFormatTests.cs:46-62`; `UploadToMyFormatTests.cs:46-60`; `UploadTests.cs:106-125, 177-186`; `InsertToMyTests.cs:141-151` | TEST-CONFIRMED [run: PASS] |
| Omitted/false `createNewIfExist` → new version, same id; true → new id; false with no clash → created | `UploadTests.cs:84-94, 127-138`; `UploadToMyTests.cs:81-102, 143-150`; `InsertToMyTests.cs:101-122`; yaml `uploadFile` (l. 19092) | TEST-CONFIRMED [run: PASS] |
| Empty file accepted (v3.6), `0 bytes` on insert | `UploadTests.cs:96-104`; `UploadToMyTests.cs:114-120`; `InsertToMyTests.cs:124-130` | TEST-CONFIRMED [run: PASS] |
| No file part → 400 (bugs 81547, 81549; insert) | `UploadTests.cs:211-226`; `UploadToMyTests.cs:122-131`; `InsertToMyTests.cs:132-139` | TEST-CONFIRMED [run: PASS] |
| `storeOriginalFile` accepted, extension kept | `UploadTests.cs:188-197`; `UploadToMyTests.cs:152-162` | TEST-CONFIRMED [run: PASS] |
| Archived room 403; folder 0 / 999999999 → 404 | `UploadTests.cs:199-244` | TEST-CONFIRMED [run: PASS] |
| **Chunked upload session** | | |
| Session id 32 hex chars, `bytesTotal` = declared size, nothing stored before a part; legacy route answers the success envelope with `id` and `bytes_total` | `06_Operations/UploadSession/UploadSessionTestBase.cs:79-110`; `UploadSessionLifecycleTests.cs:48-77` | TEST-CONFIRMED [run: PASS] |
| Chunks of `chunkUploadSize`, finalize → `uploaded`, title, `file.folderId`, `pureContentLength`, bytes identical | `02_Folders/Upload/ChunkedUploadTests.cs:44-95`; `UploadSessionChunkTests.cs:115-143` | TEST-CONFIRMED [run: PASS] |
| Twelve-hour sliding expiration | yaml `createUploadSessionInFolder` (l. 23714), `ChunkedUploadSessionDto` (l. 74277); `products/ASC.Files/Core/Utils/ChunkedUploadSessionHolder.cs:46`; `products/ASC.Files/Core/Utils/FileUploader.cs:300, 317` | CODE-ONLY |
| 402 above the chunked-upload limit; Knowledge folder / Knowledge room: unsupported format and size refused | `FileUploader.cs:205-221, 264-296`; yaml `createUploadSession` 402/403 (l. 23577) | CODE-ONLY |
| Reader of a room 403 on both routes; folder the caller cannot open 403; unknown folder 404 on both routes; nothing created | `UploadSessionIsolationTests.cs:87-113, 151-169`; `UploadSessionLifecycleTests.cs:79-101`; `FileUploader.cs:153-166` | TEST-CONFIRMED [run: PASS] |
| Negative `fileSize` → 400 when the session opens | `UploadSessionDefectTests.cs:247-266`; `FileUploader.cs:205-208, 264-269`; `products/ASC.Files/Server/Api/UploadController.cs:541-551` | TEST-CONFIRMED [run: FAIL-BUG] |
| PDF classified as form or plain PDF from the first chunk | `FileUploader.cs:331-388`; `06_Operations/CheckDestFolder/CheckDestFolderTestBase.cs:83-133` (uploads a real form this way and the server tags it) | TEST-CONFIRMED [run: NO-LINK] |
| `createNewIfExist` false on a session → the bytes become the next version of the same-named file; true → a second file | `UploadSessionTestBase.cs:149-165` (`PutFile`, used by `BatchCopyContentTests.cs:74-97` and `BatchMoveContentTests.cs:74-95` to build version 2); `UploadSessionLifecycleTests.cs:103-129` (two sessions, one name); yaml `SessionRequest.createNewIfExist` (l. 87450); `FileUploader.cs:88-125` | TEST-CONFIRMED [run: PASS] |
| **Sending the parts of a session** | | |
| Sequential route: small payload 201 at once with `uploaded`, `version` 1, title, bytes; large payload 200/`uploaded` false then 201; nothing listed in between; closed afterwards (404) | `UploadSessionChunkTests.cs:56-113` | TEST-CONFIRMED [run: PASS] |
| Numbered parts in order, out of order, in parallel; resend wins; answer carries session `id` and `bytesTotal`; finalize 201 | `UploadSessionChunkTests.cs:115-200` | TEST-CONFIRMED [run: PASS] |
| Two large sessions in parallel never mix bytes | `UploadSessionChunkTests.cs:202-229` | TEST-CONFIRMED [run: PASS] |
| Finalized session, large or small: finalize / part / abort 404, file once at version 1 | `UploadSessionChunkTests.cs:255-278` (large); `UploadSessionDefectTests.cs:164-226` (small) | TEST-CONFIRMED [run: PASS] |
| Incomplete finalize 400 and nothing stored; oversize part refused (402) without poisoning; empty part harmless | `UploadSessionChunkTests.cs:256-323`; `UploadController.cs:331-335`; `FileUploader.cs:319-322` | TEST-CONFIRMED [run: PASS] (incomplete, oversize ≥400, empty) / CODE-ONLY (the 402 itself) |
| No file part → 400 on the numbered route (was 500) | `UploadSessionDefectTests.cs:49-66`; `UploadController.cs:580-592, 611-627` | TEST-CONFIRMED [run: FAIL-BUG] |
| `chunkNumber` 0 / -1 → 4xx | `UploadSessionDefectTests.cs:142-162`; `FileUploader.cs:324-329` | TEST-CONFIRMED [run: FAIL-BUG] |
| Unknown or ill-shaped session id → 404 on finalize, both part routes and abort | `UploadSessionLifecycleTests.cs:183-199`; `ChunkedUploadSessionHolder.cs:70-85` | TEST-CONFIRMED [run: PASS] |
| **Isolation of upload sessions** | | |
| Anonymous: 401 on create, both part routes, finalize, abort; the Owner's session completes | `UploadSessionIsolationTests.cs:48-85` | TEST-CONFIRMED [run: PASS] |
| Abort checks the folder of the route (foreign My documents → 403) and the session owner; the upload completes | `UploadSessionIsolationTests.cs:115-149`; `FileUploader.cs:423-438` | TEST-CONFIRMED [run: PASS] |
| Two accounts interleaved: own file in own folder, abort of the other's session 403 | `UploadSessionIsolationTests.cs:171-218` | TEST-CONFIRMED [run: PASS] |
| Part and finalize find the session by id alone (no owner or folder check) | `UploadSessionIsolationTests.cs:220-263` (observation only); `FileUploader.cs:314-411`; `UploadController.cs:336-345` | CODE-ONLY (see F24) |
| **Aborting an upload session** | | |
| Abort before parts: no file; after one chunk: no file, session 404 on every method, neighbour completes; abort in room; own session by Owner / Content creator | `06_Operations/UploadSession/AbortUploadSessionTests.cs:49-84, 112-126`; `UploadSessionLifecycleTests.cs:131-181`; `AbortUploadSessionPermissionsTests.cs:67-101` | TEST-CONFIRMED [run: PASS] |
| Abort of another account's session → 403 (bug 82276); anonymous 401; unknown session → 404 (bug 82278) | `UploadSessionAbortBugTests.cs:47-126`; `UploadSessionLifecycleTests.cs:195`; `FileUploader.cs:423-438`; `ChunkedUploadSessionHolder.cs:70-85`; `AbortUploadSessionPermissionsTests.cs:47-65` | TEST-CONFIRMED [run: PASS] |
| Abort after finalize answers 404 for a large and for a small session; the file stays | `UploadSessionChunkTests.cs:272`; `AbortUploadSessionTests.cs:86-118` (now `AlreadyFinalized_ReturnsNotFound`, 404); `UploadSessionDefectTests.cs:206-226` (404, no longer `Category=Bug`) | TEST-CONFIRMED [run: PASS] |
| Multi-threaded chunk upload | release notes v2.5 ("чанки грузятся в несколько потоков"); the parallel case is now `UploadSessionChunkTests.cs:145-180` | TEST-CONFIRMED [run: PASS] (parallel parts) / NOTES-ONLY (as the reason for numbered chunks) |
| **Upload pre-check for name clashes** | | |
| Existing titles returned with `count`; new titles not; mixed; several; empty list | `02_Folders/UploadCheck/UploadCheckTests.cs:42-109, 192-204` | TEST-CONFIRMED [run: PASS] |
| Case-insensitive; duplicate request titles once (bug 81365); subfolder | `UploadCheckTests.cs:122-163` | TEST-CONFIRMED [run: PASS] |
| `filesTitle` null 400; `{}` 400 (81331); folder 999999999 → 404 (81330) | `UploadCheckTests.cs:111-120, 165-190` | TEST-CONFIRMED [run: PASS] |
| **Long-running operations** | | |
| `FileOperationDto` fields, type values, `status` values | yaml `FileOperationDto` (l. 80033), `FileOperationType` (l. 80162), `DistributedTaskStatus` (l. 72816), `getOperationStatuses` (l. 22418); `06_Operations/Statuses/OperationStatusLifecycleTests.cs:54-83` (id GUID, status, progress, processed, error, finished, files, url) | TEST-CONFIRMED [run: PASS] |
| Finished delete: Operation Delete, finished, progress 100, processed "1", empty error | `01_Files/Deletion/FileDeleteTests.cs:68-126` | TEST-CONFIRMED [run: PASS] |
| Start answer Created/Running at 0; ends Completed at 100; running record keeps id, progress and processed grow, two reads agree | `OperationStatusLifecycleTests.cs:54-151` | TEST-CONFIRMED [run: PASS] |
| Handed out once by any read; the next start answer carries the unread finished record and the new one last; `deleteAfter` never reports | `OperationStatusLifecycleTests.cs:153-215` | TEST-CONFIRMED [run: PASS] |
| Empty list on a fresh portal and for an unknown id | `06_Operations/Statuses/GetOperationStatusesTests.cs:48-66` | TEST-CONFIRMED [run: PASS] |
| By type: Delete/Copy/Duplicate/Download/MarkAsRead empty; Delete with id 9999999 empty; type 99 → 400; 9 / -1 / `abc` on the route → 400 | `GetOperationStatusesByTypeTests.cs:51-66, 98-117`; `OperationStatusListingTests.cs:275-290` | TEST-CONFIRMED [run: PASS] |
| Kinds listed together in the order duplications, moves/copies, deletions, downloads; filter by id; by-type hands out its own kind only and keeps the others; wrong kind/id keeps the record; a move never under Copy | `OperationStatusListingTests.cs:49-121, 158-273` | TEST-CONFIRMED [run: PASS] |
| Private to the account: another account's reads are empty and its terminate cancels nothing | `OperationStatusListingTests.cs:123-156` | TEST-CONFIRMED [run: PASS] |
| `fileops/move` status route shadowed by the check endpoint | `GetOperationStatusesByTypeTests.cs:44-50` (summary) | CODE-ONLY |
| `returnSingleOperation:true` → one Move record; `deleteAfter:true` → record gone | `06_Operations/Move/FileMoveOperationTests.cs:46-119` | TEST-CONFIRMED [run: PASS] |
| Starting call may return an already pruned (empty) list | `02_Folders/Deletion/FolderDeleteTests.cs:41-47`; `BatchDeleteTestBase.cs:44-62`; `BulkDownloadTestBase.cs:43-61` | TEST-CONFIRMED [run: PASS] |
| **Cancelling an operation** | | |
| Delete / duplicate / copy / move / markAsRead / emptyTrash terminated and gone | `06_Operations/Terminate/TerminateTasksTests.cs:60-190` | TEST-CONFIRMED [run: PASS] |
| Copy half way: record finished at 100, Canceled, non-empty error; part arrived, source whole, nothing running; second terminate empty | `06_Operations/Statuses/TerminateRunningOperationsTests.cs:49-98, 278-298`; `products/ASC.Files/Core/Services/WCFService/FileOperations/FileOperation.cs:373-378`; `products/ASC.Files/Server/Api/OperationController.cs:533-536` | TEST-CONFIRMED [run: PASS] |
| Move half way: every file in source or destination once; duplication half way: original whole, partial copy stops growing | `TerminateRunningOperationsTests.cs:100-145` | TEST-CONFIRMED [run: PASS] |
| Download half way: no `url` ever, record gone or finished, nothing running | `TerminateRunningOperationsTests.cs:147-188`; `FileOperations/FileDownloadOperation.cs:83-88` | TEST-CONFIRMED [run: FAIL-BUG] |
| Too fast to cancel (100-file delete, archiving): consistent either way; terminate of a finished copy undoes nothing | `TerminateRunningOperationsTests.cs:219-276` | TEST-CONFIRMED [run: FAIL-BUG] |
| Unknown id, finished id, whitespace, junk ids, another user's id, Guest, anonymous → 200 empty; the running copy completes | `TerminateTasksTests.cs:49-58, 96-114`; `TerminateTasksPermissionsTests.cs:48-127`; `TerminateRunningOperationsTests.cs:300-348` | TEST-CONFIRMED [run: PASS] |
| Only the named operation; empty id stops every running operation of the caller | `TerminateRunningOperationsTests.cs:350-406` | TEST-CONFIRMED [run: PASS] |
| Cancelling does not undo work | yaml `terminateTasks` (l. 23321); `TerminateRunningOperationsTests.cs:258-276` | TEST-CONFIRMED [run: PASS] |
| **Moving files and folders** | | |
| File to folder, several files to room, between rooms, room to My documents, between My documents subfolders | `06_Operations/Move/FileMoveTests.cs:42-79, 198-330` | TEST-CONFIRMED [run: PASS] |
| Moved file keeps id, versions and bytes, listed in the destination only; moved tree keeps every id and byte | `06_Operations/BatchContent/BatchMoveContentTests.cs:74-117` | TEST-CONFIRMED [run: PASS] |
| Folder with inner file into room; mixed batch; subfolder to subfolder; `content:true` | `FoldersMoveTests.cs:203-241, 274-427` | TEST-CONFIRMED [run: PASS] |
| Collaboration/Public/VDR accept; archived 403; FFR refuses `.docx`, accepts a form | `FileMoveRoomTypeTests.cs:46-170` | TEST-CONFIRMED [run: PASS] |
| Empty batch 200; move onto own parent 200; repeated id moves once | `FileMoveConflictTests.cs:171-208`; `BatchMoveContentTests.cs:166-183` | TEST-CONFIRMED [run: PASS] |
| Forms root 403; AI Agents root 403; another user's file 403; form-folder case 403; room into archive/My documents 403 | `FileMoveTests.cs:81-196`; `FoldersMoveTests.cs:42-153` | TEST-CONFIRMED [run: PASS] |
| Content creator cannot move out of the room; User cannot move a room (65580); Editing/Review 403; Guest 403; anonymous 401; Room manager, Content creator at destination, DocSpace admin succeed | `FileMovePermissionsTests.cs:46-422` | TEST-CONFIRMED [run: PASS] |
| Folder into itself / child / grandchild: client error, nothing queued, nothing changed | `BatchMoveContentTests.cs:47-72` | TEST-CONFIRMED [run: PASS] |
| Batch with an untouchable file refused as a whole (403), the own file alone accepted | `BatchMoveContentTests.cs:185-214` | TEST-CONFIRMED [run: PASS] |
| File id 999999999 → 404 (bug 82243); folder id 999999999 → 404 (bug 84436) | `FileMoveOperationTests.cs:121-144`; `06_Operations/Move/MoveBatchBugTests.cs:49-76`; `FileOperations/CopyPermissionsCheck.cs:224-237, 279-292` | TEST-CONFIRMED [run: PASS] |
| Folder holding a file open in the editor does not cancel the move of the rest of the selection | `MoveBatchBugTests.cs:125-163` (bug 84725, red) | TEST-CONFIRMED [run: FAIL-BUG] (open bug) |
| Move into a third-party room completes without error (bug 83271, Nextcloud-gated, self-skipping) | `ThirdPartyMoveBugTests.cs:59-95` | TEST-CONFIRMED [run: PASS] (skipped without Nextcloud) |
| **Copying files and folders** | | |
| Source preserved: My documents → room, room → My documents, two files, inter-room, subfolder source; Public/Collaboration/VDR destinations | `06_Operations/Copy/FileCopyBatchTests.cs:47-98, 138-170, 224-270` | TEST-CONFIRMED [run: PASS] |
| Copy carries the latest content only as a new id; copy independent of a new version and of the deletion of the original; tree byte for byte and surviving | `06_Operations/BatchContent/BatchCopyContentTests.cs:74-152` | TEST-CONFIRMED [run: PASS] |
| Folder with content, `content:true`, mixed, multiple, empty folder; level-2/3 destinations; level-3 source | `FolderCopyTests.cs:154-257`; `CopyNestedDestinationTests.cs:47-163` | TEST-CONFIRMED [run: PASS] |
| `copyFileAs` same extension into a room / with rename; the answer is the copy with `id` and `folderId` | `01_Files/CopySave/CopyFileAsTests.cs:56-78`; `FileCopyTests.cs:42-68, 101-124` | TEST-CONFIRMED [run: PASS] |
| `copyFileAs` with `toForm`: `.docx` → `.docxf` → `.pdf` form through the document server, recognised as a form | `01_Files/Forms/FormsTestBase.cs:73-93` (`ConvertToOoFormAsync`); `IsFormPdfTests.cs:124-145` | TEST-CONFIRMED [run: PASS] |
| `deleteAfter:true` keeps the source; duplicate id → every copy matches, original untouched; empty batch 200 | `FileCopyBatchTests.cs:172-222, 272-286`; `BatchCopyContentTests.cs:198-222` | TEST-CONFIRMED [run: PASS] |
| Folder into own subfolder 403; into itself/child/grandchild: client error, nothing queued; FFR refuses docx files and a room; Forms root 403; archived 403; form folder 403 | `FolderCopyTests.cs:42-125`; `BatchCopyContentTests.cs:47-72`; `FileCopyTests.cs:219-273, 333-362`; `FileCopyBatchTests.cs:99-118` | TEST-CONFIRMED [run: PASS] |
| Locked / editing file copied by another user 403; unsupported format into Knowledge 403 | `FileCopyTests.cs:364-505` | TEST-CONFIRMED [run: PASS] |
| PDF form copied into FFR, with and without `toFillOut` | `FileCopyTests.cs:507-539` | TEST-CONFIRMED [run: PASS] |
| File 999999999 → 404 (82204); folder 999999999 → 404 (84435); destination 999999999 → 404; no permission / foreign folder 403; anonymous 401 | `FileCopyTests.cs:126-331`; `FolderCopyTests.cs:127-152`; `FileCopyBatchTests.cs:120-136`; `CopyPermissionsTests.cs:71-90, 163-206, 254-300` | TEST-CONFIRMED [run: PASS] |
| Batch mixing an own file and an unreadable file refused as a whole (403) | `BatchCopyContentTests.cs:224-254` | TEST-CONFIRMED [run: PASS] |
| Who may copy (Owner, DocSpace admin, Room admin, Content creator incl. level 3, Guest with CC, folder from room by CC — bug 81906) | `CopyPermissionsTests.cs:48-69, 92-161, 208-252, 302-375` | TEST-CONFIRMED [run: PASS] |
| **Settling a name clash on move and copy** | | |
| Skip: one file, original id and bytes, skipped move stays in source (room and level 3) | `CopyConflictResolutionTests.cs:48-67, 126-145`; `FileMoveConflictTests.cs:47-91`; `BatchCopyContentTests.cs:154-196`; `BatchMoveContentTests.cs:119-164` | TEST-CONFIRMED [run: PASS] |
| Overwrite: one file, original id, now the incoming bytes (room and level 3) | `CopyConflictResolutionTests.cs:69-91, 147-169`; `FileMoveConflictTests.cs:93-132`; `BatchCopyContentTests.cs:184-187`; `BatchMoveContentTests.cs:149-153` | TEST-CONFIRMED [run: PASS] |
| Duplicate: two files, the incoming one as `same (1).bin` with its bytes; a moved file keeps its id under the new name (bug 84437); same room with Duplicate | `CopyConflictResolutionTests.cs:93-124, 171-187`; `FileMoveConflictTests.cs:134-169`; `BatchCopyContentTests.cs:188-194`; `BatchMoveContentTests.cs:154-162`; `MoveBatchBugTests.cs:84-123`; `FileOperations/FileMoveCopyOperation.cs:805-812, 1059-1068` | TEST-CONFIRMED [run: FAIL-BUG] |
| **Checking a move or copy for name clashes** | | |
| Clash reported with Skip, Overwrite and Duplicate; none → empty; own folder reported; nothing changed, no operation queued | `06_Operations/CheckMoveCopy/CheckMoveOrCopyConflictTests.cs:46-158`; `06_Operations/CheckPreflight/CheckMoveOrCopyReportTests.cs:50-72, 200-220` | TEST-CONFIRMED [run: PASS] |
| Reported with id, title and source `folderId`; folder by id and title; mixed selection each once; case / Unicode clash; sub-folder vs room root; repeated id once; unknown id passed over next to a real clash | `CheckMoveOrCopyReportTests.cs:50-198` | TEST-CONFIRMED [run: PASS] |
| `content:true` expands the listed folders to their content (clashes among the files) | `CheckPreflightBugTests.cs:92-142`; `FileStorageService.cs:3296-3337`; `Binders.cs:239-243` | TEST-CONFIRMED [run: PASS] |
| Room types, several files, `deleteAfter:false`, mixed, `content:true`, empty → empty | `CheckMoveOrCopyDestinationTests.cs:47-202` | TEST-CONFIRMED [run: PASS] |
| Archived 403; source 999999999 skipped; dest 999999999 → 404 (81881); no dest → 400 (81882); Rooms / Forms / AI Agents root → 403 as the copy refuses it | `CheckMoveOrCopyErrorTests.cs:47-136`; `CheckPreflightBugTests.cs:144-180`; `FileStorageService.cs:3428-3447` | TEST-CONFIRMED [run: PASS] |
| Reader of the room 403; a file the caller cannot read is neither returned nor named in the body | `CheckPreflightPermissionsTests.cs:47-94` | TEST-CONFIRMED [run: PASS] |
| Typed clients send the ids and the server reads a numeric text as a portal id (bug 84727); encrypted files left out of the report | `CheckSdkSerializationBugTests.cs:51-83`; `CheckMoveCopyTestBase.cs:36-79`; yaml `checkMoveOrCopyBatchItems` (l. 22787) | TEST-CONFIRMED [run: PASS] (ids) / CODE-ONLY (encrypted) |
| **Checking whether a destination accepts the files** | | |
| AllAllowed with `files` listing the sources; two files; Collaboration/Public/VDR; folder source; `deleteAfter:false` | `06_Operations/CheckDestFolder/CheckDestFolderTests.cs:42-161` | TEST-CONFIRMED [run: PASS] |
| FFR: docx NoneAllowed, form AllAllowed, both together PartAllowed naming the form | `CheckDestFolderTests.cs:96-122`; `CheckPreflight/CheckDestFolderVerdictTests.cs:49-65` | TEST-CONFIRMED [run: PASS] |
| Repeated id counts once (AllAllowed); folder ids and `conflictResolveType` ignored; same-named file no refusal; empty selection 200 | `CheckPreflightBugTests.cs:66-90`; `CheckDestFolderVerdictTests.cs:67-126`; `OperationController.cs:454-460` | TEST-CONFIRMED [run: PASS] |
| Archived 403 (82103); User/Guest without access 403 (82104); reader 403; 999999999 → 404 (82158); no dest 400 (82159); section roots 403; unreadable file not revealed | `CheckDestFolderBugTests.cs:47-143`; `CheckPreflightPermissionsTests.cs:47-94`; `CheckPreflightBugTests.cs:144-180`; `FileStorageService.cs:3373-3378` | TEST-CONFIRMED [run: PASS] |
| Check then act: reported clash + Duplicate copy; AllAllowed + move; NoneAllowed leaves the room empty and no operation | `CheckPreflight/CheckThenActTests.cs:46-118` | TEST-CONFIRMED [run: PASS] |
| **Duplicating files and folders** | | |
| ` (1)` suffix on file and folder, operation lists the copy | `06_Operations/Copy/FileCopyTests.cs:70-99`; `Move/FoldersMoveTests.cs:243-272` | TEST-CONFIRMED [run: PASS] |
| Suffix grows with every repetition, each copy the original's bytes; taken `taken (1).bin` skipped, nothing overwritten | `06_Operations/Duplicate/DuplicateContentTests.cs:60-87, 228-249` | TEST-CONFIRMED [run: PASS] |
| File/folder in My documents, several files, in a room, in a subfolder, diacritics, nested content, mixed | `06_Operations/Duplicate/DuplicateBatchItemsTests.cs:47-130, 222-280` | TEST-CONFIRMED [run: PASS] |
| Latest content; copy independent of a new version and of the deletion; tree byte for byte; three files; repeated id → two copies | `DuplicateContentTests.cs:89-226` | TEST-CONFIRMED [run: PASS] |
| Empty batch 200; 999999999 file/folder → 404 (82210); archived 403; Trash 403 | `DuplicateBatchItemsTests.cs:132-220` | TEST-CONFIRMED [run: PASS] |
| Batch with the Owner's private file refused as a whole (403); a User cannot duplicate the Owner's file, duplicates their own into their own folder | `DuplicateContentTests.cs:251-309` | TEST-CONFIRMED [run: PASS] |
| **Deleting a single file or folder** | | |
| File to Trash (Trash as `folderId`, title kept) and immediately; room file immediately; folder immediately / to Trash; subtree; folder with files; created by another member | `01_Files/Deletion/FileDeleteTests.cs:47-126, 265-278, 602-638`; `02_Folders/Deletion/FolderDeleteTests.cs:54-204`; `Crud/FolderUpdateTests.cs:82-107` | TEST-CONFIRMED [run: PASS] |
| 99999 → 404 "The required file was not found"; folder 99999 → 404 and double delete → 404 (79459) | `FileDeleteTests.cs:128-145`; `FolderDeleteTests.cs:206-251` | TEST-CONFIRMED [run: PASS] |
| Locked (room, My documents, shared) and editing file → 403 for another user | `FileDeleteTests.cs:169-263` | TEST-CONFIRMED [run: PASS] |
| Roles: DocSpace admin / Room manager / own file as CC succeed; Editing, Read, no access, Guest 403 "Access denied"; anonymous 401; another user's folder 403 | `01_Files/Deletion/FileDeletePermissionsTests.cs:49-247`; `FolderDeleteTests.cs:253-272`; `FolderDeletePermissionsTests.cs` | TEST-CONFIRMED [run: PASS] |
| **Deleting a batch** | | |
| To Trash and gone from source; permanent; folder to Trash; folder with files permanent; mixed; empty; 999999999 → 404; file already in Trash removed for good | `06_Operations/BatchDelete/BatchDeleteTests.cs:48-216` | TEST-CONFIRMED [run: PASS] |
| Set with an unknown id → 404 and nothing deleted, nothing queued; repeated id → once; mixed with a foreign file → 403, nothing deleted; another user on the Owner's Trash → client error, still restorable; not idempotent after a restore | `06_Operations/BatchContent/BatchTrashContentTests.cs:47-86, 117-190` | TEST-CONFIRMED [run: PASS] |
| Anonymous 401 | `BatchDeletePermissionsTests.cs:49-63` | TEST-CONFIRMED [run: PASS] |
| **The Trash remembers where an entry came from** | | |
| Files: no filter / USER / VirtualRooms / one room type / two types | `FileDeleteTests.cs:280-408, 496-510` | TEST-CONFIRMED [run: PASS] |
| Folders: the same filters | `FolderDeleteTests.cs:274-411` | TEST-CONFIRMED [run: PASS] |
| `roomId` filter; file inside a trashed folder listed | `FileDeleteTests.cs:602-638` | TEST-CONFIRMED [run: PASS] |
| Restore = `moveBatchItems` out of the Trash: id, both versions and bytes back; content not downloadable in the Trash; a purged file cannot be moved back | `BatchTrashContentTests.cs:88-115, 192-226` | TEST-CONFIRMED [run: PASS] |
| **Emptying the Trash** | | |
| File, folder, mixed, already empty, room file; `single:true` | `06_Operations/EmptyTrash/EmptyTrashTests.cs:47-157` | TEST-CONFIRMED [run: PASS] |
| Gone for good: lookups 404, no download, restore refused; files outside untouched with their versions; emptying an empty Trash changes nothing | `BatchTrashContentTests.cs:192-277` | TEST-CONFIRMED [run: PASS] |
| `folderType=USER` leaves room and form-room entries (82588); `VirtualRooms` leaves My documents entries; no filter clears all | `EmptyTrashTests.cs:159-201`; `FileDeleteTests.cs:410-494, 537-553` | TEST-CONFIRMED [run: PASS] |
| Per account: Owner, User, DocSpace admin, Room admin, Guest; Guest does not touch the Owner's; anonymous 401; the Owner's file stays restorable with its versions | `EmptyTrashPermissionsTests.cs:49-184`; `BatchTrashContentTests.cs:279-306` | TEST-CONFIRMED [run: PASS] |
| Whole flow copy → move → duplicate → delete → empty: bytes intact, survivors untouched | `BatchTrashContentTests.cs:308-358` | TEST-CONFIRMED [run: PASS] |
| **File versions and their history** | | |
| New file: one version, `version` 1, `versionGroup` 1, fields, `Created`, `fileStatus` None; room file | `01_Files/History/FileVersionInfoTests.cs:43-128` | TEST-CONFIRMED [run: PASS] |
| `getFileVersionInfo` 999999999 → 403; `getEditHistory` → 404 (80962) | `FileVersionInfoTests.cs:102-113`; `EditHistoryTests.cs:96-113` | TEST-CONFIRMED [run: PASS] |
| Edit history: one revision with key/user/created; grows after `lastVersion` 2; archived room readable | `EditHistoryTests.cs:43-94, 115-144` | TEST-CONFIRMED [run: PASS] |
| `getFileInfo(version)` returns that version | `FileVersionInfoTests.cs:130-147` | TEST-CONFIRMED [run: PASS] |
| Read member cannot open `version=1` through `openedit` (80683) | `01_Files/History/OpenFileVersionAccessTests.cs:48-75` | TEST-CONFIRMED [run: PASS] |
| Editing member refused the version list (403) | `01_Files/History/FileVersionInfoEditingAccessTests.cs:47-66` (bug 84774, red) | TEST-CONFIRMED [run: FAIL-BUG] (open bug) |
| **Changing and restoring versions** | | |
| Split / merge groups; structure; room file; 999999999 → 404; archived 403 | `01_Files/Versions/ChangeVersionHistoryTests.cs:43-206` | TEST-CONFIRMED [run: PASS] |
| Restore: revisions returned, history grows, structure, room; no params 400; 999999999 → 404; archived 403; GET rejected (78027) | `RestoreVersionTests.cs:43-204` | TEST-CONFIRMED [run: PASS] |
| Held file: version count unchanged (locked and edited); locked → client error; edited → not 200 | `01_Files/States/ChangeVersionHistoryHeldFileTests.cs:75-88` (unchanged), `90-106` (bug 84771, red), `108-125` (bug 84772, red) | TEST-CONFIRMED [run: FAIL-BUG] (open bugs for the codes) |
| Restore stores the content as a new version with a comment; `url` source | yaml `restoreFileVersion` (l. 12140), `updateFile` (l. 11460) | CODE-ONLY |
| **Version comments** | | |
| Set, empty, null, overwrite, Unicode, HTML-like | `06_Operations/Comment/CommentTests.cs:47-140`; `01_Files/Update/FileUpdateTests.cs:282-302` (read back through `getFileInfo`) | TEST-CONFIRMED [run: PASS] |
| 2000 chars 400 (82266); 999999999 → 404 (82268); version 999 / 0 / -1 → 400 (82271) | `CommentTests.cs:142-241` | TEST-CONFIRMED [run: PASS] |
| One version at a time, repeated text, clearing one version; quotes / line breaks / padding verbatim; 255 stored; 256 and 1000 refused and the stored text stays | `06_Operations/Comment/CommentVersionTests.cs:69-126, 155-188` | TEST-CONFIRMED [run: FAIL-BUG] |
| Content creator and Room manager comment on others' files; file in Trash refused, comment survives the restore; concurrent writes | `CommentVersionTests.cs:190-275` | TEST-CONFIRMED [run: PASS] |
| **Deleting versions** | | |
| One version, two versions, file stays listed, non-existent version ignored, room file, `returnSingleOperation` | `06_Operations/DeleteVersion/DeleteVersionTests.cs:43-113, 130-148, 205-221` | TEST-CONFIRMED [run: PASS] |
| Remaining versions keep their own bytes, current download is the last; neighbour file untouched; an already deleted version passed over | `06_Operations/DeleteVersion/DeleteVersionContentTests.cs:70-94, 138-158, 195-212` | TEST-CONFIRMED [run: PASS] |
| Current version (alone, with an old one, or the only one) → client error, nothing queued, history untouched | `DeleteVersionContentTests.cs:96-136` | TEST-CONFIRMED [run: PASS] |
| Empty or null `versions` → 200 and the whole file is deleted, a bystander kept | `DeleteVersionContentTests.cs:160-193`; `DeleteVersionTests.cs:191-203`; yaml `deleteFileVersions` (l. 22147) | TEST-CONFIRMED [run: PASS] |
| 999999999 → 404; archived 403; Trash 403 | `DeleteVersionTests.cs:115-128, 150-189` | TEST-CONFIRMED [run: PASS] |
| Anonymous 401; own file by Owner / User; other user's file 403; Room manager in own room; DocSpace admin on another user's My documents 403; Guest Read 403 | `DeleteVersionPermissionsTests.cs:43-178` | TEST-CONFIRMED [run: PASS] |
| **Locking and unlocking a file** | | |
| My documents (v3.7) and room lock; idempotent lock and unlock; `getFileInfo` agrees | `01_Files/Lock/FileLockTests.cs:46-123`; `Update/FileUpdateTests.cs:94-138` | TEST-CONFIRMED [run: PASS] |
| Only the named file unlocked (82178); 999999999 → 404 (80788) | `FileLockTests.cs:125-168` | TEST-CONFIRMED [run: PASS] |
| Room manager unlocks a CC's lock; CC cannot unlock the Owner's; CC cannot unlock another CC's; Room manager unlocks another manager's | `FileLockPermissionsTests.cs:175-231`; `FileUpdateTests.cs:211-280` | TEST-CONFIRMED [run: PASS] |
| No access 403, Guest 403, anonymous 401; DocSpace admin as manager, Room manager on the Owner's file, CC on own file succeed; Editing/FillForms/Read 403 | `FileLockPermissionsTests.cs:49-173`; `FileUpdateTests.cs:140-209` | TEST-CONFIRMED [run: PASS] |
| Locked file blocks deletion and copy by others; the editor's save route answers 403 and nothing changes; a claim for editing changes nothing | `FileDeleteTests.cs:169-243`; `FileCopyTests.cs:364-394`; `01_Files/States/LockedFileStateTests.cs:57-92` | TEST-CONFIRMED [run: FAIL-BUG] |
| Claim of a locked file (`startedit`) and rename of a locked file refused as a client error | `LockedFileStateTests.cs:94-113` (bug 84768, red); `FileRenameLockedTests.cs:94-111` (bug 84842, red) | TEST-CONFIRMED [run: FAIL-BUG] (open bugs) |
| Save into a file open in somebody else's editing session → 403, version/bytes/flag kept | `01_Files/States/EditedFileStateTests.cs:56-83` (bug 84769, red: 200 and the content replaced) | TEST-CONFIRMED [run: FAIL-BUG] (open bug) |
| **Downloading a file** | | |
| `presigneduri`: `stream_auth=`, `fileid=`, starts with http; `presigned`: `url` and `filetype` `.docx` | `01_Files/Presigned/PresignedUriTests.cs:45-59`; `PresignedFileUriTests.cs:45-58`; `06_Operations/Statuses/FileOperationsTests.cs:41-56` | TEST-CONFIRMED [run: PASS] |
| Read member, DocSpace admin, Room manager succeed; User/Guest without access 403; anonymous 401; 999999999 → 404 | `PresignedUriTests.cs:61-125`; `PresignedFileUriTests.cs:60-177` | TEST-CONFIRMED [run: PASS] |
| `viewUrl` serves the content; the file handler serves one version with `version=` | `ChunkedUploadTests.cs:84-93`; `06_Operations/OperationFileBytes.cs:110-119` (used by `DeleteVersionContentTests.cs:70-94`) | TEST-CONFIRMED [run: PASS] |
| **Bulk download as an archive** | | |
| One/two files, one/two folders, mixed, room file: Download, finished, empty error, `url` with `filehandler.ashx?action=bulk`, `processed` "1"; `returnSingleOperation` | `06_Operations/BulkDownload/BulkDownloadTests.cs:48-199` | TEST-CONFIRMED [run: PASS] |
| `fileConvertIds` with `pdf` finishes | `BulkDownloadTests.cs:227-246` | TEST-CONFIRMED [run: PASS] |
| `password` for a protected source (v3.1) | yaml `DownloadRequestItemDto.password` (l. 76815) | CODE-ONLY |
| Empty selection 200; 999999999 → 404 | `BulkDownloadTests.cs:201-225` | TEST-CONFIRMED [run: PASS] |
| Room manager, Editing, Read, Guest Read succeed; non-member 403 (81822); anonymous 401 (81823) | `BulkDownloadPermissionsTests.cs:49-233` | TEST-CONFIRMED [run: PASS] |
| One download at a time per caller | yaml `bulkDownload` (l. 21856) | CODE-ONLY |
| **Converting a file** | | |
| `.rtf` queued: one entry, Convert, progress 0, no error, `source` id/version; `startConvert:false`, empty body and `GET` with `start` true all start; empty before the start | `06_Operations/CheckConversion/FileConversionStartTests.cs:48-100` | TEST-CONFIRMED [run: PASS] |
| docx/xlsx/pptx, txt/csv/md, docx → pdf, missing version: empty list, no job, no file, no version, source intact | `06_Operations/CheckConversion/FileConversionNoWorkTests.cs:47-89`; `FileConversionStartTests.cs:102-119`; `CheckConversionStatusTests.cs:51-140` (docx/xlsx/pptx with `outputType` pdf: empty list with the `checkconversion` links) | TEST-CONFIRMED [run: PASS] |
| `.xml` queued, ends at 100 with error and no result, source intact, handed out once, retry queues again | `FileConversionStartTests.cs:121-149`; `FileConversionTestBase.cs:98-105` | TEST-CONFIRMED [run: PASS] |
| 999999999 / 0 / -1 → 200 empty on both methods; `abc` → 4xx | `FileConversionNoWorkTests.cs:91-123`; `CheckConversionStatusTests.cs:142-153`; yaml `checkConversionStatus` (l. 21505) | TEST-CONFIRMED [run: PASS] |
| Owner, DocSpace admin, Room manager, CC on own file start; no access 403 on both methods; anonymous 401 (start and status) | `StartFileConversionPermissionsTests.cs:51-133`; `CheckConversionStatusPermissionsTests.cs:47-102`; `FileConversionNoWorkTests.cs:125-149` | TEST-CONFIRMED [run: PASS] |
| Start needs the convert right, polling the read right: Read member and Editing member poll (bug 81825) | `FileConversionNoWorkTests.cs:151-166`; `CheckConversionStatusBugTests.cs:51-71`; `FileStorageService.cs:3555-3565` | TEST-CONFIRMED [run: PASS] |
| `outputType` outside the list → 400, no silent conversion; dot-less value matched | `FileConversionOutputTypeBugTests.cs:48-77` (accepts 400 or an empty answer); `FileStorageService.cs:3567-3578`; `products/ASC.Files/Core/Utils/FileConverter.cs:311-313`; `products/ASC.Files/Server/Api/FilesController.cs:805` | TEST-CONFIRMED [run: PASS] (refusal) / CODE-ONLY (the 400 itself) |
| `fileops/convert` and `fileops/import` → 400 (bug 82225, open) | `GetOperationStatusesByTypeTests.cs:68-96` | TEST-CONFIRMED [run: PASS] (open bug) |
| Target formats from the `FileFormats` configuration (document server list); request fields | `products/ASC.Files/Core/Helpers/FileUtility.cs:58-114`; yaml `CheckConversionRequestDto` (l. 74004) | CODE-ONLY |
| XML → docx/xlsx (v3.0), presentation → txt (v3.6), new formats (v3.2.1), docx → docxf removed (v2.6) | release notes only | NOTES-ONLY |
| **Document service conversion and builder calls** | | |
| `convertFile`: no access 403, anonymous 401, 999999999 → 404, empty / unknown `outputtype` 400 | `01_Files/Docs/DocsConverterTests.cs:46-104`; yaml `convertFile` (l. 8686) | TEST-CONFIRMED [run: PASS] |
| `runBuilderScript`: no edit access 403, anonymous 401, unquoted numeric id 400, empty script 400, second script while one runs 403 | `01_Files/Docs/DocsBuilderTests.cs:47-125`; yaml `runBuilderScript` (l. 8800) | TEST-CONFIRMED [run: PASS] |
| **Thumbnails** | | |
| One/two files echoed; folder id dropped; 999999999 echoed; `{}` body and folders only → 200 empty list | `01_Files/Thumbnails/ThumbnailsCreateTests.cs:49-86, 88-110, 117-176` (the "unfiled-thumbnails-null-fileids" trait stays after the fix); `FilesController.cs:1453-1463` | TEST-CONFIRMED [run: PASS] |
| Guest Read, User Read, Room manager, DocSpace admin may queue | `ThumbnailsPermissionsTests.cs:69-167` | TEST-CONFIRMED [run: PASS] |
| Sizes 1280×720 and 3840×2160 (v2.6); 30 MB image / 1000 MB video limits | `buildtools/config/appsettings.json:318-324`; `products/ASC.Files/Core/Core/ThumbnailSettings.cs:101-111` | CODE-ONLY |
| **Custom Filter mode of a spreadsheet** | | |
| Enable → `customFilterEnabled` true and `webCustomFilterEditing` true; disable clears; 999999999 → 404 | `01_Files/CustomFilter/CustomFilterTests.cs:46-92` | TEST-CONFIRMED [run: PASS] |
| Only spreadsheet formats; who may switch it off | yaml `setCustomFilterTag` (l. 12015) | CODE-ONLY |
| **New marks of a folder and marking as read** | | |
| Array for room / My documents; empty room; fields; file by another member appears; subfolder does not; several; two users; own files not | `02_Folders/News/FolderNewItemsTests.cs:49-193, 252-266` | TEST-CONFIRMED [run: PASS] |
| Created before the visit not listed; revisit clears (81712) | `FolderNewItemsTests.cs:195-250` | TEST-CONFIRMED [run: PASS] |
| Folder 999999999 / 0 → 404 (81519) | `FolderNewItemsTests.cs:272-302` | TEST-CONFIRMED [run: PASS] |
| Mark one file, two files, folder (clears its files), idempotent; folder/mixed answer a MarkAsRead operation | `06_Operations/MarkAsRead/MarkAsReadTests.cs:47-215` | TEST-CONFIRMED [run: PASS] |
| My documents root marked as read leaves an unrelated room's news alone (bug 83509) | `MarkAsReadTests.cs:217-271`; `products/ASC.Files/Core/Utils/FileMarker.cs` (`RemoveTagLinksAsync`, commit `73aa4e8ab8`) | TEST-CONFIRMED [run: PASS] |
| Only the caller's marks; the other member keeps theirs; repeated call harmless; repeated id; unreadable and unknown ids passed over; nothing modified | `06_Operations/MarkAsRead/MarkAsReadScopeTests.cs:54-170` | TEST-CONFIRMED [run: PASS] |
| `{}` 200; empty ids 200; 999999999 200; non-member 200; every role 200; anonymous 401 | `MarkAsReadTests.cs:138-175`; `MarkAsReadPermissionsTests.cs:49-102` | TEST-CONFIRMED [run: PASS] |
| **Reference data for the editors** | | |
| Keys from `openedit` resolve; roundtrip; `sourceFileId`; all optional fields; Read member `canEditRoom` false | `01_Files/ReferenceData/ReferenceDataTests.cs:49-152` | TEST-CONFIRMED [run: PASS] |
| Unknown keys → 200 with `error` | `ReferenceDataTests.cs:154-175` | TEST-CONFIRMED [run: PASS] |
| **Personal file templates** | | |
| One, two, twice, empty, `{}`, 999999999 → true | `01_Files/Templates/AddTemplatesTests.cs:47-134` | TEST-CONFIRMED [run: PASS] |
| **Concrete kind of an entry in a mixed answer** | | |
| `entryKind` on the base entry (`file`, `folder`, `thirdPartyFile`, `thirdPartyFolder`); listings, pre-checks and `copyFileAs` read as the concrete kind with `id` | `products/ASC.Files/Core/ApiModels/ResponseDto/FileEntryDto.cs:49-58`; yaml `FileEntryBaseDto.entryKind` (l. 79393); `06_Operations/FolderListing.cs:42-63`; `CheckPreflightTestBase.cs:136-144`; `CopyFileAsTests.cs:67-78` | TEST-CONFIRMED [run: PASS] |

### Scenarios added in the 2026-10-24 clean-up, and the code names moved out of the spec text

| Scenario (requirement) | Source | Status |
|---|---|---|
| Template chosen by culture (Creating a blank file by title) | `products/ASC.Files/Core/Helpers/Global.cs:331-343, 404-423`; release notes v3.0 | CODE-ONLY / NOTES-ONLY (paper size) |
| Unknown folder (Creating a text or HTML file from content) | `TextFileCreateTests.cs:191-200` | TEST-CONFIRMED [run: PASS] |
| Same version as lastVersion (Renaming a file) | `FileRenameTests.cs:150-171` (bug 80773) | TEST-CONFIRMED [run: PASS] |
| Formats and titles kept (Single-request upload) | `UploadFormatTests.cs:46-62`; `UploadToMyFormatTests.cs:46-60`; `UploadTests.cs:106-125, 177-197`; `InsertToMyTests.cs:46-99, 141-151`; `UploadToMyTests.cs:152-162` | TEST-CONFIRMED [run: PASS] |
| Refused before the session exists (Chunked upload session) | yaml `createUploadSessionInFolder` (l. 23714); `FileUploader.cs:153-166, 205-221, 264-296`; `UploadSessionIsolationTests.cs:87-113, 151-169`; `UploadSessionLifecycleTests.cs:79-101`; `UploadSessionDefectTests.cs:247-266` | TEST-CONFIRMED [run: FAIL-BUG] (rights, unknown folder, negative size) / CODE-ONLY (402, Knowledge rules) |
| PDF classified from the first chunk (Chunked upload session) | `FileUploader.cs:331-388`; `CheckDestFolderTestBase.cs:83-133` | TEST-CONFIRMED [run: NO-LINK] |
| Name clash on a session (Chunked upload session) | `UploadSessionTestBase.cs:149-165`; `UploadSessionLifecycleTests.cs:103-129`; yaml `SessionRequest.createNewIfExist` (l. 87450); `FileUploader.cs:88-125` | TEST-CONFIRMED [run: PASS] |
| Shape of an operation record (Long-running operations) — the record is `FileOperationDto`, `Operation` is `FileOperationType` with 0 Move, 1 Copy, 2 Delete, 3 Download, 4 MarkAsRead, 5 Import, 6 Convert, 7 Duplicate; `status` is `DistributedTaskStatus` (0 Created, 1 Running, 2 Completed, 3 Canceled, 4 Failted) | yaml `FileOperationDto` (l. 80033), `FileOperationType` (l. 80162), `DistributedTaskStatus` (l. 72816); `OperationStatusLifecycleTests.cs:54-83` | TEST-CONFIRMED [run: PASS] |
| Accepted routes (Moving files and folders) | `FileMoveTests.cs:42-79, 198-330`; `FoldersMoveTests.cs:203-241, 274-427` | TEST-CONFIRMED [run: PASS] |
| Copy as a new file (Copying files and folders) | `01_Files/CopySave/CopyFileAsTests.cs:56-78`; `FileCopyTests.cs:42-68, 101-124`; `FormsTestBase.cs:73-93`; `IsFormPdfTests.cs:124-145` | TEST-CONFIRMED [run: PASS] |
| PDF form into a Form filling room (Copying files and folders) | `FileCopyTests.cs:507-539` | TEST-CONFIRMED [run: PASS] |
| Route shared with the Move status (Checking a move or copy for name clashes) | `GetOperationStatusesByTypeTests.cs:44-50`; `OperationStatusListingTests.cs:36-41` | CODE-ONLY |
| Callers without delete rights (Deleting a single file or folder) | `FileDeletePermissionsTests.cs:49-247`; `FolderDeleteTests.cs:253-272`; `FolderDeletePermissionsTests.cs` | TEST-CONFIRMED [run: PASS] |
| Unknown file (File versions and their history) | `FileVersionInfoTests.cs:102-113`; `EditHistoryTests.cs:96-113` (bug 80962) | TEST-CONFIRMED [run: PASS] |
| Past version needs history access (File versions and their history) | `OpenFileVersionAccessTests.cs:48-75` (bug 80683) | TEST-CONFIRMED [run: PASS] |
| Lock blocks others (Locking and unlocking a file) | `FileDeleteTests.cs:169-243`; `FileCopyTests.cs:364-394`; `LockedFileStateTests.cs:57-113`; `FileRenameLockedTests.cs:77-111` | TEST-CONFIRMED [run: FAIL-BUG] (two codes are open bugs) |
| Target formats (Converting a file) — the list is the `FileFormats` configuration; the 400 is `FilesCommonResource.ErrorMessage_NotSupportedFormat` | `products/ASC.Files/Core/Helpers/FileUtility.cs:58-114`; `FileStorageService.cs:3567-3578`; release notes v2.6, v3.0, v3.2.1, v3.6 | CODE-ONLY / NOTES-ONLY (format pairs) |
| Sizes and media limits (Thumbnails) — the "image limit" is `MaxImageFileSize`, the "video limit" `MaxVideoFileSize` of `ThumbnailSettings` | `buildtools/config/appsettings.json:318-324`; `products/ASC.Files/Core/Core/ThumbnailSettings.cs:101-111` | CODE-ONLY |
| Unknown folder (New marks of a folder and marking as read) | `FolderNewItemsTests.cs:272-302` (bug 81519) | TEST-CONFIRMED [run: PASS] |
| "Aborting an upload session" and "Changing and restoring versions" were split out of "Chunked upload session" and "File versions and their history"; their scenarios kept their names and rows | — | — |
| Names moved out of the spec text: `conflictResolveType` is `FileConflictResolveType`; `result` of `checkdestfolder` is `CheckDestFolderResult` (0 AllAllowed, 1 PartAllowed, 2 NoneAllowed); the Trash `folderType` values are `FolderType` `USER` (5) and `VirtualRooms` (14); the "product code where a test leaves a point open" of the Purpose is `FileUploader`, `FileStorageService`, `Global` and `ThumbnailSettings`; "the editor's save route" is `saveEditingFileFromForm` (`PUT api/2.0/files/file/{fileId}/saveediting`), "claim for editing" is `startEditFile` (`POST .../startedit`); the AI rooms refused as folder parents are `FolderType.InstructionsRoom`, `KnowledgeRoom`, `AiProjectRoom` and the `AiHub` root | yaml `FileConflictResolveType`, `CheckDestFolderResult` (l. 74088), `FolderType`; files cited in the rows above | CODE-ONLY |

### Scenarios added in the 2026-11-04 revision (release/v4.1.0)

| Scenario (requirement) | Source | Status |
|---|---|---|
| Non-PDF file in a Form filling room (Creating a blank file by title) | `FormsRoomNonPdfCreateTests.cs:63-95` (bug 84840); `FileDao.cs:826-828` | TEST-CONFIRMED [run: PASS] |
| Locked by another member (Renaming a file) | `FileRenameLockedTests.cs:77-111` (bug 84842) | TEST-CONFIRMED [run: FAIL-BUG] (open bug for the code) |
| Insert into a folder with the contract's field names (Single-request upload) | `InsertToFolderTests.cs:46-74`; `Binders.cs:260-296` | TEST-CONFIRMED [run: PASS] |
| Sequential parts, Numbered parts in any order, Finalized session is closed, Incomplete and malformed parts, Unknown session (Sending the parts of a session — new requirement) | `UploadSessionChunkTests.cs:56-323`; `UploadSessionDefectTests.cs:49-66, 142-162`; `UploadSessionLifecycleTests.cs:183-199` | TEST-CONFIRMED [run: FAIL-BUG] |
| Anonymous caller, Caller without the create right, Two accounts side by side (Isolation of upload sessions — new requirement) | `UploadSessionIsolationTests.cs:48-218` | TEST-CONFIRMED [run: PASS] |
| Session aborted (Aborting an upload session) — the one-chunk and neighbour cases | `UploadSessionLifecycleTests.cs:131-181` | TEST-CONFIRMED [run: PASS] |
| Life of a record, Handed out once, Kinds listed together, Private to the account (Long-running operations) | `OperationStatusLifecycleTests.cs:49-215`; `OperationStatusListingTests.cs:49-290` | TEST-CONFIRMED [run: PASS] |
| Copy, move and duplication stopped half way; Download stopped; Operations too fast to cancel; Only the named operation (Cancelling an operation) | `TerminateRunningOperationsTests.cs:49-188, 219-406` | TEST-CONFIRMED [run: FAIL-BUG] |
| Identity and bytes survive the move; Folder into its own subtree; Batch refused as a whole; Unknown ids; Folder holding a file open in the editor (Moving files and folders) | `BatchMoveContentTests.cs:47-117, 185-214`; `MoveBatchBugTests.cs:49-76, 125-163` (bug 84725 open) | TEST-CONFIRMED [run: FAIL-BUG] |
| Latest content, then independent; Batch refused as a whole (Copying files and folders) | `BatchCopyContentTests.cs:74-152, 224-254` | TEST-CONFIRMED [run: PASS] |
| What is reported; Content of the listed folders; Caller's rights (Checking a move or copy for name clashes) | `CheckMoveOrCopyReportTests.cs:50-198`; `CheckPreflightBugTests.cs:92-142`; `CheckPreflightPermissionsTests.cs:47-94` | TEST-CONFIRMED [run: PASS] |
| Check then act (Checking whether a destination accepts the files) | `CheckThenActTests.cs:46-118` | TEST-CONFIRMED [run: PASS] |
| Latest content, then independent; Batch refused as a whole (Duplicating files and folders) | `DuplicateContentTests.cs:89-226, 251-309` | TEST-CONFIRMED [run: PASS] |
| Batch refused as a whole (Deleting a batch) | `BatchTrashContentTests.cs:47-86, 117-190` | TEST-CONFIRMED [run: PASS] |
| Restored by a move out of the Trash (The Trash remembers where an entry came from) | `BatchTrashContentTests.cs:88-115, 192-226` | TEST-CONFIRMED [run: PASS] |
| Gone for good (Emptying the Trash) | `BatchTrashContentTests.cs:192-277` | TEST-CONFIRMED [run: PASS] |
| Editing member reads the versions (File versions and their history) | `FileVersionInfoEditingAccessTests.cs:47-66` (bug 84774) | TEST-CONFIRMED [run: FAIL-BUG] (open bug) |
| File held by somebody else (Changing and restoring versions) | `ChangeVersionHistoryHeldFileTests.cs:75-125` (bugs 84771, 84772) | TEST-CONFIRMED [run: FAIL-BUG] (open bugs for the codes) |
| One version at a time; Who may comment, and a trashed file (Version comments) | `CommentVersionTests.cs:69-126, 155-275` | TEST-CONFIRMED [run: FAIL-BUG] |
| The others keep their bytes; Current version and the whole file (Deleting versions) | `DeleteVersionContentTests.cs:70-212` | TEST-CONFIRMED [run: PASS] |
| Editing session blocks others (Locking and unlocking a file) | `EditedFileStateTests.cs:56-83` (bug 84769) | TEST-CONFIRMED [run: FAIL-BUG] (open bug) |
| Conversion queued, Nothing to convert, Conversion that fails (Converting a file) — rewritten for the document server | `FileConversionStartTests.cs:48-149`; `FileConversionNoWorkTests.cs:47-123` | TEST-CONFIRMED [run: PASS] |
| Converter refusals, Builder refusals (Document service conversion and builder calls — new requirement) | `DocsConverterTests.cs:46-104`; `DocsBuilderTests.cs:47-125` | TEST-CONFIRMED [run: PASS] |
| Only the caller's marks (New marks of a folder and marking as read) | `MarkAsReadScopeTests.cs:54-170` | TEST-CONFIRMED [run: PASS] |
| Listing and copy read as their kind (Concrete kind of an entry in a mixed answer — new requirement) | `FileEntryDto.cs:49-58`; `FolderListing.cs:42-63`; `CheckPreflightTestBase.cs:136-144`; `CopyFileAsTests.cs:67-78` | TEST-CONFIRMED [run: PASS] |

### Scenarios added in the 2026-11-06 length pass

Requirement text was brought under 500 characters; what left it went into existing scenarios (covered by the
rows above) or into these new ones. Requirement names are unchanged.

| Scenario (requirement) | Source | Status |
|---|---|---|
| Session reserved (Chunked upload session) — the older route's envelope carries `id` and `bytes_total` | `UploadSessionTestBase.cs:79-110`; `UploadSessionLifecycleTests.cs:48-77` | TEST-CONFIRMED [run: PASS] |
| Session lapses (Chunked upload session) | yaml `createUploadSessionInFolder` (l. 23714); `ChunkedUploadSessionHolder.cs:46`; `FileUploader.cs:317` | CODE-ONLY |
| Holder of the session id (Isolation of upload sessions) | `UploadSessionIsolationTests.cs:220-263` (observation only); `FileUploader.cs:314-411`; `UploadController.cs:336-345` (F24) | CODE-ONLY |
| Template sets of the scenario "Template chosen by culture" are the `new/<culture>/` and `new/default/` folders of the template store | `products/ASC.Files/Core/Helpers/Global.cs:404-423` | CODE-ONLY |

Counts: 251 rows, each under its strongest status (the one note row marked — is not counted): TEST-CONFIRMED
230 (14 of them open bugs or Nextcloud-gated, marked in the row), CODE-ONLY 19, NOTES-ONLY 2; 9 rows carry a
weaker part next to the stronger one. Spec: 38 requirements, 162 scenarios.

## Revision 2026-11-06 (`1f1b052f7d..0ebdbb7c84`)

| Claim | Source | Status |
|---|---|---|
| Small session closes once the file is stored, on finalize and on the sequential call (Sequential parts; Finalized session is closed) | `UploadSessionDefectTests.cs:164-247`; `ae457bdff6` | TEST-CONFIRMED [run: FAIL-BUG] |
| Small payload as one numbered part, then finalize: 201, version 1, one file | `UploadSessionChunkTests.cs:80-103` | TEST-CONFIRMED [run: PASS] |
| Non-PDF through `uploadFile`, `insertFile`: 403, room empty | `02_Folders/Upload/FormsRoomNonPdfUploadTests.cs:57-86` (bug 84840) | TEST-CONFIRMED [run: PASS] |
| Non-PDF session in a Form filling room: 403 on the part of a small one, on finalize of a large one; room empty | `UploadSessionFormsRoomTests.cs:56-99` (bug 84840) | TEST-CONFIRMED [run: PASS] |
| Abort through another folder: 404, session stays | `08_Private/PrivateRoomContentRulesTests.cs:143-157` (in the private-room suite); `cc60512acd`, route code is shared by all folders | TEST-CONFIRMED [run: PASS] (the 404), per commit message (the session stays, ordinary folders) |
| Sessions, copies and duplicates in a private room stay in `private-rooms`; this spec only points there | `private-rooms` spec | CROSS-REFERENCE |

## Where the server differs from the release notes or the API document (spec follows the server)

| Source | Server |
|---|---|
| OpenAPI `createThumbnails`: "an anonymous caller without such a link is refused" (401) | The action carries `[AllowAnonymous]` (`FilesController.cs:1451`); `ThumbnailsPermissionsTests.cs:44-67` (bug 81268) asserts 401 and is red. Not written as a requirement; listed under defects (F1). |
| OpenAPI `uploadSession` / `finalizeSession` declare 200 for the stored file | The server answers 201 on the part that completes the upload and on finalize (`UploadSessionChunkTests.cs:70, 102`; `UploadSessionTestBase.cs:144, 160`); the spec says 201. |
| OpenAPI `abortUploadSession` / `finalizeSession` remarks: an unknown, closed or expired session "fails instead of answering as missing" | Since `73aa4e8ab8` the session lookup answers 404 (`ChunkedUploadSessionHolder.cs:70-85`; `UploadSessionLifecycleTests.cs:183-199`); the 404 rows of the same operations are right, the remarks are stale. |
| OpenAPI `uploadAsyncSession`: "this call never completes the upload" | A session whose payload is below `chunkUploadSize` stores the file on the first part (`UploadController.cs:194-195` documents it for the sequential route only); the spec describes the large-session behaviour and lists the small-session effects under F14. |
| OpenAPI `terminateTasks` (before `e9cfda8d43`): "the cancelled record is dropped at once" | The record comes back once, finished, Canceled, with the cancellation in `error` (`TerminateRunningOperationsTests.cs:76-98`); the remarks were rewritten in the same commit and the spec follows them. |
| OpenAPI `restoreFileVersion`: `version` "already the current one" is refused with 400 | Confirmed only for the `lastVersion` path of `updateFile` (`FileRenameTests.cs:150-171`); the restore route itself is tested with a non-current version only. |
| OpenAPI `checkConversionStatus`: "the same empty list is the answer for an identifier no file matches" vs its own 404 entry | `FileConversionNoWorkTests.cs:91-109` asserts 200 with an empty list for 999999999, 0 and -1 on both methods; the spec says so. |
| OpenAPI `startFileConversion`: `startConvert` "is set by the operation itself" | Confirmed: `startConvert:false` and an empty body queue the conversion all the same (`FileConversionStartTests.cs:86-100`). |
| OpenAPI `lockFile`: "a lock set by somebody else can only be released by a room manager" | Tests agree (`FileLockPermissionsTests.cs:175-231`) and add that a second Room manager also releases the lock of a Room manager (`FileUpdateTests.cs:243-280`). |
| OpenAPI `getFileVersionInfo`: "in a room only its managers and content creators may read the history" | An Editing member gets the list (`FileVersionInfoEditingAccessTests.cs:47-66`, bug 84774, red); the spec follows the document and the row is an open bug (F23). |
| OpenAPI `saveEditingFileFromForm` / `changeVersionHistory`: a file open in another account's editing session is refused | The save answers 200 and replaces the content, the history change answers 200 and does nothing (bugs 84769, 84772); the spec follows the document (F20, F21). |
| SDK `DeleteVersionBatchRequestDto` comment in `DeleteVersionTestBase.cs:84-86` says the model lacks `returnSingleOperation` | `DeleteVersionTests.cs:164, 185` set it through the typed model; the base's remark is stale, the raw helper there is kept for the null-list case only. |
| Release notes v2.5: "the overwrite-confirmation dialog is always shown on upload" | Client behaviour. The server default of the upload calls (`createNewIfExist` omitted) is to add a version to the existing file (`UploadTests.cs:127-138`); the spec states the server default. |
| Release notes v2.6: "thumbnails are not created when the file is larger than 30 MB" | The limit is per media kind: `MaxImageFileSize` 30 MB, `MaxVideoFileSize` 1000 MB (`ThumbnailSettings.cs:101-111`), both configurable. |
| Release notes v3.0: en-US template is Letter, others A4 | The server selects `new/<culture>/` by culture prefix and falls back to `new/default/` (`Global.cs:404-423`); the paper size of the template files is not visible in the code and is attributed to the notes in the spec. |

## Left out of the spec, and why

- **Editing sessions, `openedit`, `startedit`, `trackedit`, editor notifications, `getEditDiff`**
  (`01_Files/Editing`, `EditSession`, `EditDiff`): editor-internal mechanics; only the version-access case
  of `openedit` (bug 80683) and the held-file refusals of `saveediting` / `startedit` (`01_Files/States`) are
  used, because they govern version reading and the lock. The co-authoring refusal "File editing start error"
  (`EditingStartCoAuthoringTests.cs`, bug 81267) is an editor contract with the document server, not a file
  operation.
- **Folder history reports** (`02_Folders/History/FolderHistoryReport*Tests.cs`,
  `getReportFolderHistory` / `terminateReportFolderHistory`): an audit report built by the document builder,
  owned by `history-and-audit`; it is not a `fileops` operation. The missing log entry of an HTML file
  (`01_Files/Log/FileLogHtmlCreationTests.cs`, bug 84773) belongs there too.
- **File and folder order inside an indexed room** (`01_Files/Order`, `02_Folders/Crud/FolderOrderTests.cs`):
  specified in `room-types` (manual order and reorder).
- **Favorites, Recent, links, sharing, security info** (`01_Files/Links`, `Recent`, `02_Folders/Favorites`,
  `06_Operations/Favorites`, `Links*`): `file-sharing` and `shared-links`.
- **Form filling, PDF form checks, form filters, fill results** (`01_Files/FormFilling`, `Forms`,
  `02_Folders/FormFilter`): `form-filling-workflow`; only the two conversions that make a form or a plain PDF
  are cited here, as the evidence that `copyFileAs` with `toForm` and `saveFileAsPdf` convert.
- **Folder history / activity log, file log** (`02_Folders/History`, `01_Files/Log`): audit, not a file
  operation.
- **Used space and quota accounting** (`02_Folders/UsedSpace`): `room-types` (quota) and portal settings.
- **Root folders, folder info, folder path, subfolder listing** (`02_Folders/Subfolders`, `Read`): listing and
  navigation rather than operations; the Trash listing is kept only as far as it shows origin tracking.
- **Document text extraction strategies** (`09_Text`, `MarkdownDocumentProcessingStrategyTests` etc.): unit
  tests of the AI chunking strategies, not file operations.
- **Deleting personal templates** (`01_Files/Templates/DeleteTemplatesTests.cs`,
  `TemplatesPermissionsTests.cs`): not read; only the add call is specified.
- **`deleteAfter` on `deleteFile`** ("postpones the deletion until the editing session has ended",
  OpenAPI): no test exercises an editing session with a delete, so only the field's presence is mentioned.
- **`templateId` / `formId` sources of `createFile`, synchronous conversion (`sync:true`), the content of a
  finished conversion, `saveFileAsPdf` positives, the generated thumbnails**: the document server now runs in
  the integration host, but those cases are not ported yet (`CopyFileAsTests.cs:41-50`,
  `SaveFileAsPdfTests.cs:40-46`, `ThumbnailsCreateTests.cs:37-42`).
- **Conversion format pairs** (XML → docx/xlsx, pptx → txt, docx → docxf forbidden): the server reads the
  list from the `FileFormats` configuration supplied by the document server; nothing in the repo tests or
  pins the pairs, so they are attributed to the notes and not stated as rules.
- **Third-party storage variants** (string ids, `x-thirdparty-variant` operations, the kDrive connection of
  bug 84672): only the Nextcloud-gated move test exists for operations; the rest is untested here.
- **Webhooks raised by the operations** (`[RaisesWebhook]` added in `c6d6bd5cd4`): `webhooks-and-api-keys`.
- **Instructions and Knowledge room rules for uploads and moves** (skill frontmatter, vectorisation, the
  skills limit; `03_Rooms/Instructions`): `ai-agents`; only the refusal of a sub-folder is cited here.

## Suspected defects (not written into the spec)

Re-checked against `3533ad8a89`, by reading: no test was run. FIXED rows keep their number so that `openspec/deviations.md` still resolves.

| # | Where | What the server does | Evidence | Verdict |
|---|---|---|---|---|
| F1 | thumbnails | `POST api/2.0/files/thumbnails` is `[AllowAnonymous]`; an anonymous caller without a link is accepted although the API document says 401. | `FilesController.cs:1450-1452`; `01_Files/Thumbnails/ThumbnailsPermissionsTests.cs:44-67` (bug 81268, red) | STILL OPEN — the attribute is still there |
| F2 | thumbnails | A request without `fileIds` answered 400 with a stack trace instead of 200 with an empty list. | `FilesController.cs:1455-1460`; `ThumbnailsCreateTests.cs:88-110, 137-157` | FIXED (`a79b825223`, before the base; the earlier row was stale) |
| F3 | copy | `copyBatchItems` with a folder id that does not exist answered 500. | `FolderCopyTests.cs:127-152` asserts 404; `CopyPermissionsCheck.cs:224-237` | FIXED (`184d90789e`, bug 84435) |
| F4 | move | `moveBatchItems` with a file id that does not exist answered 403 instead of 404. | `CopyPermissionsCheck.cs:279-292` (`FileNotFoundException` → 404); `FileMoveOperationTests.cs:121-144`; `MoveBatchBugTests.cs:58-62` (control) | FIXED (`01d0b7713d`, bug 82204, shared check; the test's "403 today" comment is stale) |
| F5 | upload session | Aborting a session id that never existed answered 500 instead of 404. | `ChunkedUploadSessionHolder.cs:70-85`; `UploadSessionLifecycleTests.cs:183-199` | FIXED (`73aa4e8ab8`, bug 82278) |
| F6 | conversion | `GET fileops/convert` and `GET fileops/import` answer 400 instead of an empty list; `GET fileops/move` is shadowed by the check endpoint and dies on the missing `destFolderId`. | `GetOperationStatusesByTypeTests.cs:44-96` (bug 82225); `OperationStatusListingTests.cs:36-41` | STILL OPEN — route unchanged, tests still carry the bug |
| F7 | conversion | A room member with Editing rights got 403 from `checkConversionStatus`. | `FileStorageService.cs:3555-3565` (poll needs read only); `CheckConversionStatusBugTests.cs:51-71`; `FileConversionNoWorkTests.cs:151-166` | FIXED (`73aa4e8ab8`, bug 81825) |
| F8 | mark as read | `markAsRead` on the My documents root cleared the "new" marks of an unrelated room too. | `FileMarker.cs` (`RemoveTagLinksAsync` detaches links instead of deleting the shared tag row); `MarkAsReadTests.cs:217-271` | FIXED (`73aa4e8ab8`, bug 83509) |
| F9 | mark as read | No per-item access check: a User who is not a member of the room marks its file as read with 200; a file the caller cannot read is passed over in silence. | `MarkAsReadPermissionsTests.cs:89-102`; `MarkAsReadScopeTests.cs:124-153` (both assert it as current behaviour) | STILL OPEN as a design question — now pinned by two tests, the spec states it |
| F10 | version info | `getFileVersionInfo` answers 403 for a file id that does not exist while `getEditHistory` answers 404 for the same id. | `FileVersionInfoTests.cs:102-113` vs `EditHistoryTests.cs:100-113` | STILL OPEN |
| F11 | text/HTML creation | `createNewIfExist` is inverted with respect to every upload call: `true` returns the existing file, `false` creates a new one. | `TextFileCreateTests.cs:73-118`; `HtmlFileCreateTests.cs:63-82` | STILL OPEN (written into the spec as the server does it) |
| F12 | create file | `createFile` with a folder id that does not exist answers 403 instead of 404 (the text/HTML twin answers 404). | `FileCreateTests.cs:101-112` vs `TextFileCreateTests.cs:191-200` | STILL OPEN |
| F13 | SDK contract | `checkMoveOrCopyBatchItems`, `checkMoveOrCopyDestFolder` and `deleteFileVersions` could not be called through the generated clients. | `CheckSdkSerializationBugTests.cs:51-83`; `CheckMoveCopyTestBase.cs:36-79`; `CheckDestFolderTestBase.cs:54-72`; `DeleteVersionTestBase.cs:62-81` (all typed now) | FIXED (`06eecdbd77`, bug 84727, for the pre-checks; `632c2fc262` for `deleteversion`) |
| F14 | upload session | A session whose payload is below `chunkUploadSize` stores the file on the first `uploadAsyncSession` part, although the contract says that call never completes the upload: a 400-byte part of a declared 1000 bytes is listed as a truncated file, and every repeated part adds a version to a brand-new file. The other half of the old finding is gone: a second finalize, a part after finalize and an abort after finalize now answer 404, and a small session completed by `uploadSession` closes too. | Open: `UploadSessionDefectTests.cs:86-121, 249-270` (`Category=Bug`). Closed: `UploadSessionDefectTests.cs:164-247`, `AbortUploadSessionTests.cs:86-118`, `UploadController.cs:223-230, 345-352` | STILL OPEN, narrowed: the storing on the first part and the extra versions. FIXED (`ae457bdff6`): repeat finalize, part after finalize, abort after finalize |
| F15 | upload session | Finalize is not honest about a missing file: a session finalized without any part, or a 100-byte session that received 300 bytes, answers 201 with `uploaded` true and file id 0 and stores nothing; a short intermediate part makes finalize answer 500 ("smaller than the minimum allowed size") instead of a client error. | `UploadSessionDefectTests.cs:68-84, 123-140, 268-291` (`Category=Bug`) | STILL OPEN (new) — no part and over-declared size: red by code reading (nothing saved, finalize skipped, file id 0: `FileDao.cs:2147`, `UploadController.cs:336-345`); the short-chunk 500 rests on the test's summary, the storage refusal was not traced |
| F16 | cancel | A terminate that arrives while the copy still waits for a free worker thread is dropped: the queue cancels only a task one of its threads has taken, so the copy runs to its end when the queue is busy. The sibling case (cancel before the first entry is processed ending with an empty error) is fixed in `e9cfda8d43`. | `TerminateRunningOperationsTests.cs:190-217` (`Category=Bug`); `common/ASC.Common/Threading/DistributedTaskQueue.cs` | STILL OPEN (new, flaky by load) — red by the test's remarks; the queue was not re-read |
| F17 | version comments | A comment holding a character outside the Basic Multilingual Plane (an emoji) is echoed as sent but stored as `?`: the history column cannot hold it. | `CommentVersionTests.cs:128-153` (`Category=Bug`) | STILL OPEN (new) — red by the test's mark; the column was not re-read |
| F18 | move | A batch move of several folders is refused or fails as a whole when one of them holds a file open in an editing session; the folders that have nothing to do with the open file stay put. | `MoveBatchBugTests.cs:125-163` (bug 84725, red) | STILL OPEN (new) — red by the test's mark; the operation was not traced |
| F19 | create file | Creating a `.docx`, a text file or an HTML file directly in a Form filling room was refused through a plain `Exception` → 500. It now answers 403 and stores nothing, on the upload and session routes as well. | `FileDao.cs:826-828`; `ResponseWrapper.cs:86-88`; `FormsRoomNonPdfCreateTests.cs:63-95`, `02_Folders/Upload/FormsRoomNonPdfUploadTests.cs:57-86`, `06_Operations/UploadSession/UploadSessionFormsRoomTests.cs:56-99` | FIXED (`1a1d539775`) |
| F20 | versions | `changeVersionHistory` on a file locked by somebody else answers 500 (plain exception "The file is currently locked by another user"); on a file open in somebody else's editing session it answers 200 and changes nothing. | `ChangeVersionHistoryHeldFileTests.cs:90-125` (bugs 84771, 84772, red) | STILL OPEN (new) — red by the tests' marks |
| F21 | editing session | `startedit` on a file locked by somebody else is refused through a 500; `saveediting` into a file open in somebody else's editing session answers 200, replaces the content, grows the version and clears the editing flag. | `LockedFileStateTests.cs:94-113` (bug 84768); `EditedFileStateTests.cs:56-83` (bug 84769); both red | STILL OPEN (new) — red by the tests' marks |
| F22 | rename | `updateFile` on a file locked by somebody else is refused through a 500 instead of a client error; the title stays. | `FileRenameLockedTests.cs:94-111` (bug 84842, red) | STILL OPEN (new) — red by the test's mark |
| F23 | version info | `getFileVersionInfo` answers the version list to a member invited with Editing access, although the document says only managers and content creators read the history. | `FileVersionInfoEditingAccessTests.cs:47-66` (bug 84774, red) | STILL OPEN (new) — red by the test's mark |
| F24 | upload session | The part and finalize routes find the session by its id alone: a caller without access to the target folder who holds the id can write parts into the Owner's session and finalize it into the Owner's folder; only abort checks the owner and, since `cc60512acd`, the folder. The contract documents it; the test records the observation and pins nothing. | `UploadSessionIsolationTests.cs:220-263`; `FileUploader.cs:314-411` (no owner check) vs `423-438` | STILL OPEN (new, security smell, design decision pending) — by code reading: no owner check on the part and finalize paths |
| F25 | pre-checks | `checkMoveOrCopyDestFolder` counted a repeated id as refused (PartAllowed); `checkMoveOrCopyBatchItems` ignored `content`; both accepted the Rooms, Forms and AI Agents roots the copy refuses. | `CheckPreflightBugTests.cs:70-180`; `OperationController.cs:454-460`; `FileStorageService.cs:3304-3309, 3375-3378, 3432-3436` | FIXED (`e9cfda8d43`) — guards read in the code; the test should be green, not run |
| F26 | conversion | An `outputType` the portal cannot produce was queued and silently converted to the internal format; a dot-less value (`pdf`) never matched. | `FileConversionOutputTypeBugTests.cs:48-77` (accepts 400 or an empty answer); `FileStorageService.cs:3567-3578`; `FileConverter.cs:311-313` | FIXED (`e9cfda8d43`) — guards read in the code; not run |
| F27 | move | `moveBatchItems` with a folder id that does not exist answered 500. | `MoveBatchBugTests.cs:49-76` (bug 84436); `CopyPermissionsCheck.cs:224-237` | FIXED (`184d90789e`, the shared check of F3; no commit names 84436) |
| F28 | move | A file moved with `Duplicate` onto a same-named file kept the clashing title. | `MoveBatchBugTests.cs:84-123` (bug 84437); `FileMoveCopyOperation.cs:805-812, 1059-1068` | FIXED (`77dd46d96a`) |
| F29 | upload session | A numbered part without a file part answered 500; part 0 or -1 was stored under a name the assembly never reads; `fileSize` -1 opened a session that could never finish. | `UploadSessionDefectTests.cs:49-66, 142-162, 247-266`; `FileUploader.cs:207-208, 324-329`; `UploadController.cs:580-592, 611-627` | FIXED (`e9cfda8d43`) — the size and number guards read in the code; not run |

Bug tests named in this revision and their rows: `UploadSessionDefectTests` F14, F15, F29; `CheckPreflightBugTests`
F25; `CheckSdkSerializationBugTests` F13 (bug 84727, no new defect); `FileConversionOutputTypeBugTests` F26;
`MoveBatchBugTests` F18, F27, F28.
