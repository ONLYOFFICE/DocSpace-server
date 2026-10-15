# file-operations Specification

## Purpose

Defines what the server does with files and folders regardless of who asks: creating files from the blank
templates, from text or HTML and by copying, creating folders, how titles are normalised and how a name clash
is settled, single-request and chunked uploads, the model every long-running file operation follows (the
`fileops` list, its statuses, cancellation), move and copy with their pre-checks and conflict modes,
duplication, deletion into and out of Trash, versions and their comments, locking, downloading, conversion,
thumbnails, the Custom Filter mode of a spreadsheet, the "new" marks of a folder and the reference data the
editors ask for. Who may do each of these inside a room is specified in `room-role-permissions`,
`vdr-room-roles` and `form-filling-room-roles`; what a room type adds (duplication of a whole room, archive,
quota) is in `room-types`; links are in `shared-links`; the lifecycle of a PDF form in `form-filling-workflow`.
This specification does not restate those rules and refers to them where an operation depends on them.

Sources: the integration tests of `products/ASC.Files/Tests/Tests/01_Files`, `02_Folders` and
`06_Operations`, the OpenAPI contract `sdk/docspace-api-spec/docspace-backend.yaml` (operations are named by
their `operationId`), the product release notes (v2.5 to v4.0), which say what was shipped and when, and the
product code (`FileUploader`, `FileStorageService`, `Global`, `ThumbnailSettings`) where a test leaves a point
open. The integration host runs without a document server, so nothing that needs a real conversion or editor
save is covered by tests; those points rest on the code and the API document, as recorded in
`verification.md` next to this file. Where the release notes and the server disagree this specification
follows the server. HTTP status codes are stated where the tests assert them. Behaviour that looks like a
defect is not written here as a requirement; it is listed under "Suspected defects" in `verification.md`.

## Requirements

### Requirement: Creating a blank file by title

`createFile` (`POST api/2.0/files/{folderId}/file`) and `createFileInMyDocuments` (`POST api/2.0/files/@my/file`)
SHALL create a file from the portal's blank template for the requested format. The stored title is derived
from the requested one: a title without an extension gets `.docx`; `.docx`, `.xlsx`, `.pptx` and `.pdf` are
kept; an extension the portal does not know how to create from a template (for example `.txt`) is replaced by
`.docx`, except `.md` and `.markdown`, which are kept as given (bug 80324). The template is taken from the
`new/<culture>/` folder of the template store when one exists for the caller's culture and from `new/default/`
otherwise (the release notes v3.0 describe the `en-US` templates as Letter and the default ones as A4). A title
longer than 170 characters SHALL be rejected (400). Creating a file directly in a section root (Rooms,
Archive, Trash, AI Agents, Forms) SHALL be refused (403), and so is a folder id the caller cannot resolve.
The `templateId` and `formId` sources of `createFile` are not covered by tests and are not specified here.

#### Scenario: Title normalisation

- **WHEN** a file is created with the title `Autotest Document`, `Autotest Document.docx`, `Autotest Document.txt` or `Autotest Document.md`
- **THEN** the stored titles are `Autotest Document.docx`, `Autotest Document.docx`, `Autotest Document.docx` and `Autotest Document.md`, each with a positive id

#### Scenario: Every blank format

- **WHEN** the Owner, a DocSpace admin, a Room admin or a User creates `test.docx`, `test.pptx`, `test.xlsx` or `test.pdf` in their own My documents
- **THEN** the file is created with that title and `getFileInfo` returns it with `version` 1 and the matching `fileExst`

#### Scenario: File in a room

- **WHEN** a file is created in a Custom room
- **THEN** its `folderId` is the room id

#### Scenario: Refused folders and over-long title

- **WHEN** a file is created in the Archive, Trash, Rooms, AI Agents or Forms root, or in a folder id that does not exist
- **THEN** the system rejects the request (403)
- **AND** a title of 166 characters plus `.docx` is rejected (400)

### Requirement: Creating a text or HTML file from content

`createTextFile`, `createHtmlFile` and their `@my` twins SHALL create a `.txt` or `.html` file whose body is the
`content` of the request; the extension is appended to the title. `content` MUST be present (400 otherwise).
An empty title (400) and a title of 300 characters (400) SHALL be rejected; letters of any script are kept.
The `createNewIfExist` flag of these calls is inverted with respect to the upload calls: `true` SHALL return
the file that already carries the title, `false` or omitted SHALL create another file with a numeric suffix.
A folder id that does not exist answers 404.

#### Scenario: Text file created

- **WHEN** `createTextFileInMyDocuments` is called with title `Autotest Text My Docs File` and content `Hello world`
- **THEN** the file `Autotest Text My Docs File.txt` is created in My documents with `fileExst` `.txt`; the same in a room yields `folderId` equal to the room id

#### Scenario: Missing content

- **WHEN** a text or HTML file is created without `content`, in My documents or in a room
- **THEN** the system rejects the request (400)

#### Scenario: Same title twice

- **WHEN** a text (or HTML) file is created twice with the same title, the second call with `createNewIfExist:true`
- **THEN** the second call returns the first file's id
- **AND** with `createNewIfExist:false` or omitted the second call returns a different id

#### Scenario: Title validation

- **WHEN** a text file is created in a room with an empty title, or with a 300-character title
- **THEN** the system rejects the request (400)
- **AND** the title `Ünïcödé Café résumé naïve` is stored as `Ünïcödé Café résumé naïve.txt`

### Requirement: Creating a folder

`createFolder` SHALL create a folder with the given title under the given parent and return it with
`parentId` equal to the parent and a positive id. Titles need not be unique: two folders with the same title
may sit in one parent. A title of 166 characters SHALL be rejected (400); an empty title SHALL be rejected
(400, bug 81507). Folders cannot be created in a section root (Archive, Trash, Rooms, AI Agents, Forms — 403)
nor in the Knowledge folder of an AI room (403); a parent that does not exist is refused.

#### Scenario: Folder in a room and in My documents

- **WHEN** the Owner, a Room admin or a User creates `Test folder` in their My documents, or the Owner creates one in a Custom room
- **THEN** the folder is returned with that title and, in the room, `parentId` equal to the room id
- **AND** `Folder with spaces`, `Special_Chars-Folder` and `Nested.Folder` are stored unchanged, and a child created inside a folder reports that folder as `parentId`

#### Scenario: Duplicate title in one parent

- **WHEN** `Duplicate Folder` is created twice in My documents
- **THEN** both calls succeed and the second folder has the same title and parent

#### Scenario: Refused parents and title length

- **WHEN** a folder is created in a section root, in the Knowledge folder of an AI room, or with a 166-character title
- **THEN** the system answers 403 for the roots and the Knowledge folder (which stays empty) and 400 for the title

### Requirement: Renaming a file

`updateFile` SHALL rename a file without changing its format: the original extension is always preserved
and appended to the requested title, so `Renamed.txt` on a `.docx` file becomes `Renamed.txt.docx` and a title
without extension gets the file's own extension. An empty or whitespace-only title SHALL leave the name
unchanged (200). Characters a file name cannot hold are replaced with `_` (`A/B` becomes `A_B`), while `&`
is kept. A title longer than 170 characters is rejected (400); a file id that does not exist answers 404
("The required file was not found", bug 80774). `lastVersion` restores that version as described under
versions; a `lastVersion` equal to the current version is rejected (400, "The new version cannot be the same
as the current one", bug 80773).

#### Scenario: Extension handling

- **WHEN** `Autotest No Ext Original.docx` is renamed to `Autotest No Ext Renamed`, to `Autotest With Ext Renamed.docx` and to `Autotest Change Ext Renamed.txt`
- **THEN** the titles become `Autotest No Ext Renamed.docx`, `Autotest With Ext Renamed.docx` and `Autotest Change Ext Renamed.txt.docx`, `fileExst` staying `.docx`

#### Scenario: Blank title and special characters

- **WHEN** a file is renamed to `""`, `"   "`, `Autotest A & B` or `Autotest A/B`
- **THEN** the first two leave the title unchanged (200) and the others yield `Autotest A & B.docx` and `Autotest A_B.docx`

#### Scenario: Invalid rename

- **WHEN** a file is renamed with a 166-character name plus `.docx`, or file id 999999999 is renamed
- **THEN** the system answers 400 and 404 respectively

### Requirement: Renaming a folder

`renameFolder` SHALL change only the title: id, `parentId`, counters, type and `rootFolderType` are
unchanged, the new title is what `getFolderInfo` and the parent listing return, and the old title is gone from
the listing. Renaming to the same title answers 200; Cyrillic letters, `&` and parentheses are kept. An empty
title is rejected (400, bug 81507); a 166-character title is rejected (400); folder id 0 or an unknown id
answers 404 (bug 81508).

#### Scenario: Rename keeps everything but the title

- **WHEN** a folder in My documents or in a room is renamed
- **THEN** the answer carries the same id and `parentId`, `filesCount`, `foldersCount`, `type` and `rootFolderType` as before, and the new title is read back by `getFolderInfo` and in the parent listing
- **AND** `Тест Папка & Folder (2024)` is stored unchanged and a rename to the current title answers 200

#### Scenario: Invalid rename

- **WHEN** a folder is renamed to `""`, to a 166-character title, or folder id 0 or 999999999 is renamed
- **THEN** the system answers 400 for the titles and 404 for the ids

### Requirement: Single-request upload

`uploadFile` (`POST api/2.0/files/{folderId}/upload`), `uploadFileToMy` and `insertFileToMy`
(`POST api/2.0/files/@my/insert`) SHALL store the uploaded part as a file of the target folder and answer
with the file (`uploadFile` and `uploadFileToMy` return a list holding exactly one file). The stored `fileExst`
follows the uploaded name (`.pdf`, `.xlsx`, `.pptx`, `.png`, `.jpg`, `.zip`, `.csv`, `.md`, ...), titles with
spaces, parentheses and Cyrillic letters are kept, `pureContentLength` equals the uploaded size and an empty
file (v3.6) is accepted and reported as `0 bytes`. `createNewIfExist` settles a name clash: `false` or omitted
SHALL add the content as a new version of the file that already carries the name (same id, higher
`version`), `true` SHALL keep both by storing the new one with a numeric suffix. `insertFileToMy` takes an
explicit `title` that overrides the part's own name. `storeOriginalFile` is accepted and does not change the
extension. A request without a file part SHALL be rejected (400, bugs 81547 and 81549); an archived room
answers 403; folder id 0 or an unknown id answers 404.

#### Scenario: Upload into a room, a subfolder and My documents

- **WHEN** `autotest-upload.txt` is uploaded into a Custom room, into a subfolder of it and into My documents
- **THEN** the answer holds one file whose `folderId` is the target folder, whose `createdBy` is the caller and whose title is in the folder listing

#### Scenario: Name clash on upload

- **WHEN** `autotest-overwrite.txt` is uploaded twice without `createNewIfExist`
- **THEN** the second answer has the first file's id and a greater `version`
- **AND** with `createNewIfExist:true` the second answer has a different id; `createNewIfExist:false` with no clash simply creates the file

#### Scenario: Empty file and missing file

- **WHEN** a zero-byte `autotest-empty.txt` is uploaded, and then a request with no file part is sent
- **THEN** the first answers 200 with one file (`contentLength` `0 bytes` on insert) and the second answers 400

#### Scenario: Bad destinations

- **WHEN** a file is uploaded into an archived room, into folder id 0 or into folder id 999999999
- **THEN** the system answers 403 for the archived room and 404 for the ids

### Requirement: Chunked upload session

`createUploadSession` (`POST api/2.0/files/{folderId}/upload/create_session`) SHALL reserve an upload for
`fileName` and `fileSize` and answer with a session whose non-empty `id` every later call quotes; the parts
are sent with `uploadAsyncSession` (`POST .../session/{sessionId}/upload`, numbered chunks of at most
`chunkUploadSize` from the files settings, which may be uploaded in several threads, v2.5) and the file is
produced by `finalizeSession` (`PUT .../session/{sessionId}/finalize`), which answers `uploaded:true`, the
title and the `file` with `folderId` equal to the target and `pureContentLength` equal to the declared size.
The stored bytes SHALL be the uploaded bytes. The session lapses twelve hours after the last accepted part. A
declared size above the portal limit for chunked uploads (and, in a Knowledge folder, above the vectorisation
limit or in a format that cannot be extracted) is refused before the session exists (402 / 403); the target
must accept new content (403 for a section root or a folder the caller cannot write to, 404 for an unknown
folder). A `.pdf` upload is classified as a PDF form or a plain PDF from its first chunk. `createNewIfExist`
has the upload meaning: `true` stores the file beside a same-named one under a numeric suffix, `false` adds a
new version to it. `abortUploadSession` (`DELETE .../session/{sessionId}`) SHALL drop a session that was
opened by the caller — before any part, or after it was finalized — without creating a file; a session of
another account is refused (403, bug 82276) and an anonymous caller gets 401.

#### Scenario: Whole file in chunks

- **WHEN** a session is created for `new.docx` with its real length, the content is sent in chunks of `chunkUploadSize` and the session is finalized
- **THEN** the answer reports `uploaded:true`, title `new.docx`, `file.folderId` equal to My documents and `pureContentLength` equal to the length, and the bytes read back through the file's `viewUrl` are identical to the upload

#### Scenario: Session aborted

- **WHEN** a session is created and aborted before any part is sent
- **THEN** the call succeeds and no file with that title appears in the folder
- **AND** aborting a session that was already finalized, or one opened in a Custom room, succeeds as well

#### Scenario: Who may abort

- **WHEN** a Content creator aborts the Owner's session in the room, a User with no access to the room aborts it, or an anonymous caller aborts any session
- **THEN** the system answers 403, 403 and 401; the caller's own session (Owner, or a User with Content creator rights) is aborted with 200

### Requirement: Upload pre-check for name clashes

`checkUpload` (`POST api/2.0/files/{folderId}/upload/check`) SHALL answer with the subset of `filesTitle` that
already names a file in the folder, compared case-insensitively, each title reported once, with `count`
equal to the number of clashes; an empty list answers an empty list. `filesTitle` MUST be present: `null`
(400) and a body without the field (400, bug 81331) are rejected. A folder that does not exist answers 404
(bug 81330).

#### Scenario: Clashing and new titles

- **WHEN** the folder holds `Autotest Conflicting File.docx` and the check is called with that title and `Brand New Non-Conflicting File.docx`
- **THEN** the answer is exactly the existing title with `count` 1; two existing titles give both; two brand-new titles give an empty list

#### Scenario: Case and duplicates

- **WHEN** the folder holds `Autotest Case File.docx` and the check is called with `autotest case file.docx`, or with the same existing title twice
- **THEN** the answer holds one title in both cases (bug 81365)

#### Scenario: Invalid request

- **WHEN** the check is called with `filesTitle` null, with `{}` as body, or against folder 999999999
- **THEN** the system answers 400, 400 and 404

### Requirement: Long-running operations

Deletion, move, copy, duplication, bulk download, mark-as-read and emptying the Trash SHALL run as background
operations. The starting call returns the caller's operations of that kind as a list of `FileOperationDto`:
`id`, `Operation` (`FileOperationType`: 0 Move, 1 Copy, 2 Delete, 3 Download, 4 MarkAsRead, 5 Import,
6 Convert, 7 Duplicate), `progress` 0–100, `finished`, `processed` (a decimal count in a string), `error`
(empty on success — the only way to tell success from failure), `url` (bulk download only), `files` and
`folders` (what the operation produced; empty for a deletion). `getOperationStatuses`
(`GET api/2.0/files/fileops`, optional `id`) and `getOperationStatusesByType` (`GET api/2.0/files/fileops/{type}`)
SHALL list only the caller's operations; a finished operation is reported once and then dropped, so an empty
list means nothing left to report, and the list returned by the starting call may already be empty for a
fast operation. `returnSingleOperation:true` SHALL narrow the answer to the operation just started;
`deleteAfter:true` SHALL remove the record as soon as the operation finishes. An unknown operation type on
the route is rejected (400).

#### Scenario: Shape of a finished deletion

- **WHEN** a file in My documents is deleted with `immediately` true or false and the operation is polled until `finished`
- **THEN** the record has `Operation` Delete, `finished` true, `progress` 100, `processed` `"1"` and an empty `error`

#### Scenario: Nothing to report

- **WHEN** `getOperationStatuses` is called on a fresh portal, or with id `00000000-0000-0000-0000-000000000000`
- **THEN** the answer is an empty list
- **AND** `getOperationStatusesByType` with Delete, Copy, Duplicate, Download or MarkAsRead and no running operation, or Delete with id `9999999`, answers an empty list; type 99 answers 400

#### Scenario: Single operation and record removal

- **WHEN** a move is started with `returnSingleOperation:true`
- **THEN** the answer holds exactly one record with `Operation` Move
- **AND** a move started with `deleteAfter:true` leaves `getOperationStatuses` empty once the file is at the destination

### Requirement: Cancelling an operation

`terminateTasks` (`PUT api/2.0/files/fileops/terminate/{id}`, v4.0 "stop an operation") SHALL cancel the
caller's operation with that id and answer with the caller's remaining operations (200); the cancelled record
is dropped from `getOperationStatuses` at once. Cancelling does not undo what was already done. An id that is
not among the caller's operations — unknown, already finished, or belonging to another account — cancels
nothing and is not an error (200, empty list); an anonymous caller gets the same empty answer.

#### Scenario: Running operations of every kind

- **WHEN** a delete to Trash, a duplicate, a copy, a move, a mark-as-read or an empty-trash of fifty files is started and terminated by its id
- **THEN** the call answers 200 and the operation disappears from `getOperationStatuses`

#### Scenario: Ids that cancel nothing

- **WHEN** the Owner terminates `00000000-0000-0000-0000-000000000000` or an already finished delete, a User terminates the Owner's operation, a Guest or an anonymous caller terminates an unknown id
- **THEN** every call answers 200 with an empty list

### Requirement: Moving files and folders

`moveBatchItems` (`PUT api/2.0/files/fileops/move`) SHALL move the listed files and folders into
`destFolderId` as one operation: a moved file reports the destination as `folderId`, a moved folder reports it
as `parentId` and keeps its content, and the source listing no longer holds the entry. With `content:true` the
listed folder's files and subfolders are moved and the folder itself is not recreated. Moves SHALL be
accepted between My documents and its subfolders, from My documents into a Custom, Collaboration, Public or
Virtual Data Room and between rooms, from a room back to My documents, and between subfolders of one room;
a batch may mix files and folders. The caller needs create access at the destination and the right to remove
the entry from its source (see `room-role-permissions`): a Content creator cannot move a file out of the room
(403) and a User cannot move a room (403, "Access denied", bug 65580). The Forms root, the AI Agents root,
an archived room (403) and a Form filling room offered a non-form file (403) SHALL be refused; an empty batch
and a move onto the folder the file already lives in answer 200. A file id that resolves to nothing SHALL
answer 404 (bug 82243).

#### Scenario: File and folder moved

- **WHEN** a file is moved from My documents to a folder, and a folder holding a file is moved into a Custom room
- **THEN** the operation finishes without error, `getFileInfo` reports the new `folderId`, `getFolderInfo` reports the room as `parentId`, the inner file is still inside and the source no longer lists the folder

#### Scenario: Room types as destination

- **WHEN** a `.docx` is moved into a Collaboration, Public or Virtual Data Room, into a Form filling room, or into an archived room
- **THEN** the first three succeed, the Form filling room answers 403, the archived room answers 403
- **AND** a PDF carrying the ONLYOFFICE form signature is moved into the Form filling room and leaves the source

#### Scenario: Contents only, empty batch, same folder

- **WHEN** a folder is moved with `content:true`, an empty batch is sent, and a file is moved onto its own parent
- **THEN** only the inner file appears at the destination without the folder, and the other two calls answer 200

#### Scenario: Refused destinations and sources

- **WHEN** a file or folder is moved into the Forms root or the AI Agents root, a User moves the Owner's file, or a User moves a room into their My documents
- **THEN** the system rejects each request (403)

### Requirement: Copying files and folders

`copyBatchItems` (`PUT api/2.0/files/fileops/copy`) SHALL copy the listed files and folders into
`destFolderId`, leaving the originals in place, and `copyFileAs` (`POST api/2.0/files/file/{fileId}/copyas`)
SHALL copy one file under a new title into a folder (only a destination of the same extension works without a
document server). A copied folder arrives with its content; `content:true` copies the content without the
folder itself; `deleteAfter:true` only removes the operation record, the source stays. The same file id listed
twice yields one copy. Copies are accepted from My documents into Custom, Public, Collaboration and Virtual
Data Rooms, into subfolders several levels deep, between rooms and from a room back to My documents. Refused
(403): a destination the caller cannot write to, an archived room, the Forms root, a Form filling room for a
non-form file or for a whole room, a folder into its own subfolder, a file that another account locked or is
editing (for a caller who is not that account), and a file of an unsupported format into the Knowledge folder
of an AI room. A file id that resolves to nothing answers 404 (bug 82204); a destination that does not exist
answers 404. A PDF form is copied into a Form filling room, with or without `toFillOut`.

#### Scenario: Copy keeps the source

- **WHEN** a file is copied from My documents into a Custom room, from a room into My documents, and two files are copied at once
- **THEN** every copy is listed at the destination and every source is still listed where it was

#### Scenario: Folder copies

- **WHEN** a folder holding a file is copied into a room, with `content:true`, together with a file, as two folders at once, and as an empty folder
- **THEN** the folder appears with its inner file; with `content:true` only the inner file appears; mixed and multiple batches place everything; the empty folder arrives empty
- **AND** a level-3 subfolder copied into another room arrives with its inner file

#### Scenario: Refused copies

- **WHEN** a folder is copied into its own subfolder, two `.docx` files or a Form filling room are copied into a Form filling room, a file is copied into the Forms root or an archived room, or a `.docx` into a folder of a Form filling room
- **THEN** the system rejects each request (403)

#### Scenario: Locked, editing and unsupported

- **WHEN** a User copies a file the Owner locked, a file the Owner is editing, or the Owner copies a file whose extension is not vectorisable into the Knowledge folder of an AI room
- **THEN** the system rejects each request (403)

#### Scenario: Ids and empty batch

- **WHEN** file id 999999999 is copied, a file is copied into folder 999999999, the same file id is listed twice, or an empty batch is sent
- **THEN** the first two answer 404, the duplicate id yields exactly one copy, and the empty batch answers 200

### Requirement: Settling a name clash on move and copy

`conflictResolveType` (`FileConflictResolveType`: `Skip`, `Overwrite`, `Duplicate`) SHALL decide what a move
or copy does with an item whose title already exists at the destination, directly under a room or any level
deep. `Skip` SHALL leave the destination unchanged and, for a move, leave the item at its source;
`Overwrite` SHALL add the content as a new version of the existing entry (same id, no second file);
`Duplicate` SHALL store a second entry next to the existing one.

#### Scenario: Skip

- **WHEN** a same-titled file is copied or moved with `Skip` into a room (or a level-3 folder) that already holds one
- **THEN** the destination still holds one file with the original id, and a skipped move leaves the file in its source folder

#### Scenario: Overwrite

- **WHEN** a same-titled file is copied or moved with `Overwrite`
- **THEN** the destination still holds one file with the original id

#### Scenario: Duplicate

- **WHEN** a same-titled file is copied or moved with `Duplicate`, or a file is copied into its own room with `Duplicate`
- **THEN** the destination holds two files

### Requirement: Checking a move or copy for name clashes

`checkMoveOrCopyBatchItems` (`GET api/2.0/files/fileops/move`, flat query parameters `fileIds`,
`folderIds`, `destFolderId`, `conflictResolveType`, `deleteAfter`, `content`) SHALL answer with the listed
items that already have a same-named entry at the destination, whatever `conflictResolveType` says, and an
empty list when none clashes; a file checked against the folder it already lives in is reported as a clash.
A source id that resolves to nothing is passed over. Create access to the destination is required: an archived
room answers 403, a destination that does not exist 404 (bug 81881), a request without `destFolderId` 400
(bug 81882). The route is reached by the generated clients under `fileops/move`, which also shadows the
status route of the Move operation type.

#### Scenario: Clash reported regardless of the resolve mode

- **WHEN** My documents and a room both hold `Autotest CheckMove ConflictFile.docx` and the check is called with `Skip` or `Overwrite`
- **THEN** the answer holds exactly that title; with no same-named file, or with `Duplicate` and no clash, the answer is empty
- **AND** a file checked against its own folder is reported

#### Scenario: Destinations and batch shapes

- **WHEN** a file is checked against a Collaboration, Form filling, Public or Virtual Data Room, a folder against a Custom room, two files, a file and a folder, a folder with `content:true`, or an empty batch
- **THEN** every answer is an empty list

#### Scenario: Errors

- **WHEN** the check names an archived room, folder 999999999, no `destFolderId`, or file id 999999999 as the only source
- **THEN** the system answers 403, 404, 400 and 200 with an empty list respectively

### Requirement: Checking whether a destination accepts the files

`checkMoveOrCopyDestFolder` (`GET api/2.0/files/fileops/checkdestfolder`) SHALL report whether the
destination accepts the listed files: `result` is `CheckDestFolderResult` 0 AllAllowed, 1 PartAllowed or
2 NoneAllowed and `files` names the accepted ones. Every folder and every room except a Form filling room
accepts every file; a Form filling room accepts only PDF forms, so a `.docx` yields NoneAllowed and a form
AllAllowed. Folder ids and `deleteAfter` do not change the result. An archived room (403, bug 82103), a
destination the caller cannot write to (403, bug 82104), a destination that does not exist (404, bug 82158)
and a request without `destFolderId` (400, bug 82159) SHALL be refused. The check does not look for name
clashes.

#### Scenario: Accepted files are listed

- **WHEN** one or two files from My documents are checked against a Custom room
- **THEN** `result` is AllAllowed and `files` holds exactly those titles; a Collaboration, Public or Virtual Data Room and a folder as source give AllAllowed too

#### Scenario: Form filling room

- **WHEN** a `.docx` and then a PDF form are checked against a Form filling room
- **THEN** the results are NoneAllowed and AllAllowed

#### Scenario: Errors

- **WHEN** the check names an archived room, a room the User or Guest cannot write to, folder 999999999, or no `destFolderId`
- **THEN** the system answers 403, 403, 404 and 400

### Requirement: Duplicating files and folders

`duplicateBatchItems` (`PUT api/2.0/files/fileops/duplicate`, v2.6) SHALL copy each listed file and folder
next to itself in the folder it lives in — My documents, a room or a nested subfolder — and answer with the
caller's duplicate operations. The copy keeps the original name with the index suffix ` (1)` (then ` (2)` and
so on) so nothing is overwritten; a duplicated folder carries its content; the `files` and `folders` of the
finished operation name the copies. An empty selection answers 200; a file or folder in an archived room and
a file in Trash answer 403; an id that does not exist answers 404 (bug 82210). Duplicating a whole room is
specified in `room-types`.

#### Scenario: Index suffix

- **WHEN** `source_document.docx` in My documents, or a folder `folder`, is duplicated with `returnSingleOperation:true`
- **THEN** the finished operation lists `source_document (1).docx` (or a folder `folder (1)`) and the parent now holds both

#### Scenario: Content and places

- **WHEN** a folder holding a file is duplicated, a file and a folder are duplicated together, two files at once, a file in a Custom room, a file in a subfolder, or a file titled `Autotest Dup Üñó Résumé.docx`
- **THEN** each duplicate appears next to its source, the duplicated folder holds the inner file, and the diacritics are kept

#### Scenario: Refused and unknown

- **WHEN** a file in an archived room, a file in Trash, file id 999999999 or folder id 999999999 is duplicated, or an empty batch is sent
- **THEN** the system answers 403, 403, 404, 404 and 200

### Requirement: Deleting a single file or folder

`deleteFile` (`DELETE api/2.0/files/file/{fileId}`) and `deleteFolder` (`DELETE api/2.0/files/folder/{folderId}`)
SHALL queue a delete operation whose body carries `deleteAfter` and `immediately`. With `immediately:false`
the entry (a folder together with everything inside it) SHALL be moved to the caller's Trash, where it
reports the Trash as `folderId` and keeps its original title; with `immediately:true` it SHALL be removed for
good, a folder with its nested folders and files. After the operation `getFileInfo` / `getFolderInfo` of a
permanently deleted entry fails and a second delete answers 404 (bug 79459); an id that does not exist
answers 404 ("The required file was not found"). The call SHALL be refused (403) for a file locked by
another account, a file being edited, a file shared read-write that its owner locked, and for callers without
delete rights (see `room-role-permissions`: Editing or Read in the room, a Guest, a User outside the room,
another User's My documents); anonymous gets 401.

#### Scenario: Trash and immediate deletion

- **WHEN** a file is deleted with `immediately:false` and another with `immediately:true`
- **THEN** the first is listed in the Trash with the Trash as its `folderId` and the second cannot be read any more

#### Scenario: Folder subtree

- **WHEN** a folder holding a child folder and a file is deleted with `immediately:true`
- **THEN** the folder, its child and the file are gone; with `immediately:false` the folder is listed in the Trash

#### Scenario: Locked or editing file

- **WHEN** a User deletes a file the Owner locked in a room or in My documents, a file the Owner is editing, or a shared file the Owner locked
- **THEN** the system rejects each request (403)

#### Scenario: Unknown and repeated ids

- **WHEN** file id 99999 or folder id 99999 is deleted, or a folder is deleted twice
- **THEN** every refused call answers 404

### Requirement: Deleting a batch

`deleteBatchItems` (`PUT api/2.0/files/fileops/delete`) SHALL delete the listed files and folders in one
operation with the same `immediately` semantics: `false` moves them to the Trash and removes them from the
source, `true` removes them for good (a folder with its files), and a file that is already in the Trash is
removed from it for good. A batch may mix files and folders; an empty selection answers 200; a file id that
resolves to nothing answers 404; anonymous callers get 401.

#### Scenario: To Trash and for good

- **WHEN** a file is deleted with `immediately:false`, another with `immediately:true`, and a folder holding a file with `immediately:true`
- **THEN** the first is in the Trash and gone from the source, the second is in neither, and the folder is gone from its parent

#### Scenario: Mixed batch and Trash item

- **WHEN** two files and a folder are deleted together, and a file already in the Trash is deleted with `immediately:true`
- **THEN** all three are gone from the source and the trashed file is gone from the Trash

### Requirement: The Trash remembers where an entry came from

An entry in the Trash SHALL keep its origin: the Trash listing (`getFolderByFolderId` of the Trash,
`getTrashFolder`) SHALL return every trashed file and folder when no filter is given, and SHALL narrow the
listing with `folderType` to entries that originally sat in a section or room of the listed types — `USER`
(5) for My documents, `VirtualRooms` (14) for every room, `CustomRoom`, `PublicRoom`, `EditingRoom` or
`VirtualDataRoom` for one type, several types at once — and with `roomId` to entries from that room. Files
inside a trashed folder are listed as trashed files themselves.

#### Scenario: Filter by origin

- **WHEN** files from My documents, a Custom, a Public and a Virtual Data Room are trashed and the Trash is read with no filter, with `USER`, with `VirtualRooms`, with `PublicRoom` and with `USER` plus `CustomRoom`
- **THEN** the unfiltered listing holds all four, `USER` only the My documents file, `VirtualRooms` only the room files, `PublicRoom` only the Public room file, and the pair the My documents and Custom room files
- **AND** the same holds for trashed folders

#### Scenario: Filter by room

- **WHEN** files and folders of one room are trashed and the Trash is read with that `roomId`
- **THEN** every trashed entry of the room is listed, a file that was not deleted is not, and the file of a trashed subfolder is listed too

### Requirement: Emptying the Trash

`emptyTrash` (`PUT api/2.0/files/fileops/emptytrash`) SHALL queue an operation that removes the caller's own
Trash for good — files, folders, and files that came from rooms — and SHALL answer 200 also when the Trash is
already empty. With `folderType` the sweep is narrowed to the entries whose origin was a section or room of
the listed types (`USER`, `VirtualRooms`, ...), leaving the rest, including entries from Form filling rooms,
in place (bug 82588). `single:true` returns only the operation just started. Every authenticated account
empties its own Trash only: a Guest's call never touches the Owner's Trash; anonymous gets 401.

#### Scenario: Whole Trash

- **WHEN** a file, a folder, and a file deleted from a room are in the Trash and `emptyTrash` runs
- **THEN** the Trash holds none of them afterwards; a call on an empty Trash answers 200

#### Scenario: One section only

- **WHEN** files from My documents, a Custom room and a Form filling room are in the Trash and `emptyTrash` runs with `folderType=USER`
- **THEN** the My documents file is gone and the room and form-room files are still there
- **AND** with `folderType=VirtualRooms` the room entries go and the My documents entries stay

#### Scenario: Per account

- **WHEN** the Owner has a trashed file and a Guest empties their own Trash, or a User, Room admin, DocSpace admin or Guest with a trashed file of their own empties theirs
- **THEN** the Owner's file stays, and each account's own file is gone; an anonymous caller gets 401

### Requirement: File versions and their history

Every file SHALL start at `version` 1 in `versionGroup` 1 with the comment `Created` and `fileStatus` None.
`getFileVersionInfo` (`GET api/2.0/files/file/{fileId}/history`) SHALL list the versions as file records
(`id`, `title`, `version`, `versionGroup`, `folderId`, `fileExst`, `webUrl`, `viewUrl`, `createdBy`,
`updatedBy`, `comment`), one per version; `getEditHistory` (`GET .../edit/history`) SHALL list the editing
revisions (`id`, `key`, `version`, `versionGroup`, `user`, `created`) and answers 404 for an unknown file
(bug 80962), while `getFileVersionInfo` answers 403 for the same id. `updateFile` with `lastVersion` N SHALL
store that version's content again as a new version, so the history grows; `getFileInfo` with `version`
SHALL return that specific version. `changeVersionHistory` (`PUT .../history`, `version`, `continueVersion`)
SHALL split the given version into a revision group of its own with `continueVersion:false` and fold it back
into the previous group with `true`; it answers 404 for an unknown file and 403 in an archived room.
`restoreFileVersion` (`POST .../restoreversion`, `version` or `url`) SHALL store the chosen version's content
on top of the history as a new version and answer the editing revisions, which have grown by one; without
`version` and `url` it answers 400, for an unknown file 404, in an archived room 403, and the route accepts
POST only (a GET is rejected and restores nothing, bug 78027). Reading the history works in an archived room.
Opening a specific past version through `openedit` SHALL require the right to read the history: a member
with Read access opens the current version but not `version=1` (403, bug 80683).

#### Scenario: New file

- **WHEN** a file is created in My documents or in a room and its version info is read
- **THEN** exactly one version is listed with `version` 1, `versionGroup` 1, the file's id and title, `comment` `Created` and `fileStatus` None, and `getEditHistory` lists one revision with a non-empty `key` and a user

#### Scenario: History grows

- **WHEN** `updateFile` is called with `lastVersion` 2 and then the edit history and `getFileInfo(version=1)` are read
- **THEN** the history holds more than one entry and the specific read reports `version` 1

#### Scenario: Split and merge

- **WHEN** a file with two versions has version 2 split with `continueVersion:false` and then merged with `true`
- **THEN** after the split versions 1 and 2 carry different `versionGroup` values and after the merge the number of groups is not larger than before

#### Scenario: Restore

- **WHEN** version 1 of a two-version file is restored
- **THEN** the answer lists the revisions, one more than before, each with id, key, version, group, user and `created`
- **AND** a restore without parameters answers 400, of file 999999999 answers 404, in an archived room 403, and a GET to the route is not 200 and leaves the history unchanged

### Requirement: Version comments

`updateFileComment` (`PUT api/2.0/files/file/{fileId}/comment`, `version`, `comment`) SHALL set the comment of
that version and answer the stored text; `getFileInfo` reports it as `comment`. An empty or null comment
clears it; Unicode and HTML-like characters are stored verbatim. A comment longer than 255 characters is
rejected (400, bug 82266); `version` 0, a negative version or a version the file does not have is rejected
(400, bug 82271); an unknown file answers 404 (bug 82268).

#### Scenario: Set, overwrite and clear

- **WHEN** the comment of version 1 is set to `Initial comment`, then to `Updated comment`, then to `""` and to null
- **THEN** the answers are `Initial comment`, `Updated comment`, `""` and `""`, and `getFileInfo` reports the current text

#### Scenario: Validation

- **WHEN** a 2000-character comment, version 999, version 0, version -1 or file id 999999999 is sent
- **THEN** the system answers 400, 400, 400, 400 and 404

### Requirement: Deleting versions

`deleteFileVersions` (`PUT api/2.0/files/fileops/deleteversion`, `fileId`, `versions`, v3.1) SHALL remove the
listed versions for good as a background operation, answering a list of operations; the file stays in its
folder with the remaining versions. A version number the file does not have is passed over; a null
`versions` list answers 200. The call needs the rights that deleting the file needs: a file in an archived
room and a file in the Trash answer 403, an unknown file 404, anonymous 401.

#### Scenario: One and several versions

- **WHEN** version 1 of a two-version file, or versions 1 and 2 of a three-version file, are deleted
- **THEN** the version info no longer lists them and still lists the last one, and the file is still in My documents
- **AND** the same works for a file in a Custom room and with `returnSingleOperation:true`

#### Scenario: Ignored and refused

- **WHEN** version 999 of a one-version file is deleted, `versions` is null, the file is in an archived room or in the Trash, or the file id is 999999999
- **THEN** the version info still holds version 1 only, the null list answers 200, and the others answer 403, 403 and 404

### Requirement: Locking and unlocking a file

`lockFile` (`PUT api/2.0/files/file/{fileId}/lock`, `lockFile` true/false) SHALL set or clear the lock and
answer the file with `locked` true, or without the flag when unlocked. Locking works in My documents (v3.7)
as well as in a room, is idempotent in both directions and affects only the named file. A lock set by another
account SHALL block that file's deletion and copying by others and can only be released by someone with
manager rights (a Room manager releases a Content creator's lock, a Content creator cannot release the
Owner's). An unknown file answers 404 (bug 80788); a caller without access answers 403, anonymous 401.
Which roles may lock is in `room-role-permissions`.

#### Scenario: Lock and unlock

- **WHEN** a file in My documents or in a room is locked, locked again, unlocked and unlocked again
- **THEN** the answers report `locked` true, true, not true and not true, and `getFileInfo` agrees

#### Scenario: Only the named file

- **WHEN** three files are locked and the second is unlocked
- **THEN** the first and third are still locked and the second is not (bug 82178)

#### Scenario: Someone else's lock

- **WHEN** a Room manager unlocks a file a Content creator locked, and a Content creator unlocks a file the Owner locked
- **THEN** the first succeeds and the second answers 403
- **AND** a lock request on file 999999999 answers 404

### Requirement: Downloading a file

`getPresignedUri` (`GET api/2.0/files/file/{fileId}/presigneduri`) SHALL answer an `http(s)` address of the
current version that carries a `stream_auth` token and `fileid=<id>`; `getPresignedFileUri`
(`GET .../presigned`) SHALL answer an object with `url` and the file's `filetype` (`.docx`). Both need read
access to the file (a Read member, a Room manager and a DocSpace admin get it; a User or Guest without access
to the room gets 403), answer 404 for an unknown id and 401 for an anonymous caller. The bytes are fetched by
following the address (the `viewUrl` of a file serves the same content).

#### Scenario: Address minted

- **WHEN** the Owner asks for the presigned URI and the presigned file URI of a room file
- **THEN** the first contains `stream_auth=` and `fileid=<id>` and the second has an `http(s)` `url` and `filetype` `.docx`

#### Scenario: Access

- **WHEN** a Read member, a DocSpace admin, a Room manager, a User without access, a Guest without access and an anonymous caller ask
- **THEN** the first three get an address and the others get 403, 403 and 401; id 999999999 answers 404

### Requirement: Bulk download as an archive

`bulkDownload` (`PUT api/2.0/files/fileops/bulkdownload`, `fileIds`, `folderIds`, `fileConvertIds`) SHALL
queue a Download operation that packs the listed files and folders into one archive and, once finished,
reports `url` containing `filehandler.ashx?action=bulk`, `processed` with the number of items and an empty
`error`. Items of `fileConvertIds` (`key` file id, `value` target extension, optional `password` for a
password-protected source, v3.1) are converted before packing. Read access to every item suffices (Room
manager, Editing, Read, a Guest with Read); a User with no access answers 403 (bug 81822), an anonymous
caller 401 (bug 81823), an id that resolves to nothing 404, and an empty selection 200.

#### Scenario: Files and folders packed

- **WHEN** one file, two files, one folder, two folders, a file and a folder, or a file of a Custom room are downloaded
- **THEN** the operation finishes with `Operation` Download, `finished` true, empty `error` and a `url` with `filehandler.ashx?action=bulk`; a single file reports `processed` `"1"`
- **AND** with `returnSingleOperation:true` the answer holds that one Download operation

#### Scenario: Download with conversion

- **WHEN** a `.docx` is listed in `fileConvertIds` with `pdf`
- **THEN** the operation finishes without error with `processed` `"1"` and a `url`

#### Scenario: Access and ids

- **WHEN** a User who is not a room member, or an anonymous caller, downloads a room file, file id 999999999 is listed, or the selection is empty
- **THEN** the system answers 403, 401, 404 and 200

### Requirement: Converting a file

`startFileConversion` (`PUT api/2.0/files/file/{fileId}/checkconversion`, `startConvert`, `outputType`,
`password`, `version`, `createNewIfExist`, `sync`) SHALL queue the conversion of a file into another format
and answer the conversion entries to poll together with a `checkconversion` link of action `PUT`;
`checkConversionStatus` (`GET .../checkconversion`, optional `start`) SHALL report the entries with a link of
action `GET`. Without a document server the queue stays empty, so both answer an empty list. A file id that
resolves to nothing answers an empty list too, not 404. The target formats come from the document server's
format list (release notes: XML to `docx`/`xlsx` v3.0, presentations to `txt` v3.6, `.pages`/`.numbers`/
`.key`/`.hwp`/`.hwpx`/`.odg`/`.md` and diagram formats v3.2.1; `docx` to `docxf` no longer offered v2.6). The
caller needs read access to the file: the Owner, a DocSpace admin, a Room manager and a Content creator on
their own file succeed; a User without access to the room gets 403 and anonymous 401.

#### Scenario: Conversion queued

- **WHEN** a `.docx`, `.xlsx` or `.pptx` is sent with `startConvert:true` and `outputType` `pdf`, or with `startConvert:false`
- **THEN** the answer is an empty list with one link to `checkconversion` (action `PUT`), and `checkConversionStatus` answers an empty list with a `GET` link

#### Scenario: Unknown file and access

- **WHEN** file 999999999 is converted, a User outside the room converts or polls a room file, or an anonymous caller does
- **THEN** the unknown file answers 200 with an empty list, the User 403 and the anonymous caller 401

### Requirement: Thumbnails

`createThumbnails` (`POST api/2.0/files/thumbnails`) SHALL queue thumbnail generation for the listed file ids
and echo those ids back; `folderIds` are accepted and ignored, and an id that names no file is echoed like
any other. The thumbnails themselves are built in the background in the sizes of the `thumbnail` settings
(1280×720 and 3840×2160 by default, v2.6) and are read through `thumbnailStatus` / `thumbnailUrl` of the
file; an image larger than 30 MB (default `MaxImageFileSize`) and a video larger than 1000 MB get none. Any
signed-in account with read access may queue them (a Guest or User with Read, a Room manager, a DocSpace
admin).

#### Scenario: Ids echoed

- **WHEN** one file, two files, or a file together with a folder are sent
- **THEN** the answer holds exactly the file ids, the folder id is not in it; id 999999999 is echoed as well

#### Scenario: Who may queue

- **WHEN** a Guest with Read, a User with Read, a Room manager or a DocSpace admin queues a room file
- **THEN** the file id is echoed

### Requirement: Custom Filter mode of a spreadsheet

`setCustomFilterTag` (`PUT api/2.0/files/file/{fileId}/customfilter`, v3.1) SHALL switch the Custom Filter
editing mode of a spreadsheet on or off and answer the file with `customFilterEnabled` (true when on, absent
when off) and `viewAccessibility.webCustomFilterEditing` true. An unknown file answers 404.

#### Scenario: Enable and disable

- **WHEN** the mode is enabled on an `.xlsx` in a room and then disabled
- **THEN** the first answer reports `customFilterEnabled` true and `webCustomFilterEditing` true, the second reports the flag not true

#### Scenario: Unknown file

- **WHEN** the mode is set on file 999999999
- **THEN** the system answers 404

### Requirement: New marks of a folder and marking as read

`getNewFolderItems` (`GET api/2.0/files/{folderId}/news`) SHALL list the files of a folder that are new for
the caller — created by another account since the caller's last visit — each with `title`, `fileEntryType`,
`createdBy` and `updated`; folders are never marked, the caller's own files are not marked for the caller, an
empty room gives an empty list, and reading the folder's content clears the marks (bug 81712). Folder id 0
or an unknown id answers 404 (bug 81519). `markAsRead` (`PUT api/2.0/files/fileops/markasread`) SHALL clear
the marks of the listed files and of the files inside the listed folders as a MarkAsRead operation; it is
idempotent, accepts an empty or absent body and an id that names nothing, checks no per-item access (a User
with no access to the room answers 200), and refuses only an anonymous caller (401).

#### Scenario: Files by other members are new

- **WHEN** two Content creators each create a file in the Owner's room after the Owner's visit, and another creates a subfolder
- **THEN** the Owner's news lists both files and not the subfolder; files the Owner created are not listed

#### Scenario: Visit clears

- **WHEN** a member creates a file before the Owner's first visit, or the Owner re-reads the room after the news appeared
- **THEN** the news does not list the file after the visit

#### Scenario: Mark as read

- **WHEN** a Read member marks one file, two files, or a subfolder holding a new file as read
- **THEN** the answer holds a MarkAsRead operation and the room news no longer lists those files; marking again answers 200

#### Scenario: Lenient input

- **WHEN** `markAsRead` is sent with `{}`, with empty ids, with file id 999999999, or by a User who is not a room member for a room file
- **THEN** every call answers 200; an anonymous caller gets 401

### Requirement: Reference data for the editors

`getReferenceData` (`POST api/2.0/files/file/referencedata`, `fileKey` and `instanceId` from the
`referenceData` of `openedit`, optional `sourceFileId`, `path`, `link`) SHALL answer 200 with a reference —
`url`, `key` and `referenceData` echoing `fileKey` and `instanceId`, plus `canEditRoom` (false for a Read
member) — and never throws for a pair it cannot resolve: the answer then carries `error` instead.

#### Scenario: Keys resolve

- **WHEN** the keys minted by `openedit` are sent, with or without `sourceFileId`, `path` and `link`
- **THEN** `url` and `key` are non-empty and `referenceData.fileKey` / `instanceId` equal the request; a Read member gets `canEditRoom` false

#### Scenario: Unknown keys

- **WHEN** `totally-fake-file-key-12345` / `fake-instance-id` are sent
- **THEN** the answer is 200 with a non-empty `error`

### Requirement: Personal file templates

`addTemplates` (`POST api/2.0/files/templates`) SHALL add the listed file ids to the caller's template list
and answer `true`; the same id twice, an empty list, a body of `{}` and an id that names no file all answer
`true`.

#### Scenario: Always true

- **WHEN** one file, two files, the same file twice, an empty list, `{}` or id 999999999 is sent
- **THEN** every answer is `true`
