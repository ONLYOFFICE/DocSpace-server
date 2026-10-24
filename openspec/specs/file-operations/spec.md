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
product code where a test leaves a point open (the upload helper, the file service, the template lookup and
the thumbnail settings; the files are named per row in `verification.md`). The integration host runs without
a document server, so nothing that needs a real conversion or editor save is covered by tests; those points
rest on the code and the API document, as recorded in `verification.md` next to this file. Where the release
notes and the server disagree this specification follows the server. HTTP status codes are stated where the
tests assert them. Behaviour that looks like a defect is not written here as a requirement; it is listed under
"Suspected defects" in `verification.md`.

## Requirements

### Requirement: Creating a blank file by title

`createFile` (`POST api/2.0/files/{folderId}/file`) and `createFileInMyDocuments` (`POST api/2.0/files/@my/file`)
SHALL create a file from the portal's blank template, normalising the requested title to a format the portal
can create. A title longer than 170 characters SHALL be rejected (400); a section root or a folder id the
caller cannot resolve SHALL be refused (403). The `templateId` and `formId` sources of `createFile` are not
covered by tests and are not specified here.

#### Scenario: Title normalisation

- **WHEN** a file is created with the title `Autotest Document`, `Autotest Document.docx`, `Autotest Document.txt` or `Autotest Document.md`
- **THEN** the stored titles are `Autotest Document.docx`, `Autotest Document.docx`, `Autotest Document.docx` and `Autotest Document.md`, each with a positive id
- **AND** the rule is: a title without an extension gets `.docx`; `.docx`, `.xlsx`, `.pptx` and `.pdf` are kept; an extension the portal cannot create from a template (for example `.txt`) is replaced by `.docx`, except `.md` and `.markdown`, which are kept as given (bug 80324)

#### Scenario: Template chosen by culture

- **WHEN** a blank file is created
- **THEN** the template is taken from the `new/<culture>/` folder of the template store when one exists for the caller's culture and from `new/default/` otherwise
- **AND** the release notes v3.0 describe the `en-US` templates as Letter and the default ones as A4

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
`content` of the request; the extension is appended to the title. `content` MUST be present (400 otherwise);
an empty or over-long title SHALL be rejected (400). The `createNewIfExist` flag of these calls is inverted
with respect to the upload calls: `true` SHALL return the file that already carries the title, `false` or
omitted SHALL create another file with a numeric suffix.

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
- **AND** letters of any script are kept: the title `Ünïcödé Café résumé naïve` is stored as `Ünïcödé Café résumé naïve.txt`

#### Scenario: Unknown folder

- **WHEN** a text file is created in folder id 999999999
- **THEN** the system answers 404

### Requirement: Creating a folder

`createFolder` SHALL create a folder with the given title under the given parent and return it with
`parentId` equal to the parent and a positive id. Titles need not be unique: two folders with the same title
may sit in one parent. A title of 166 characters SHALL be rejected (400); an empty title SHALL be rejected
(400, bug 81507). Folders cannot be created in a section root nor in the Knowledge folder of an AI room
(403); a parent that does not exist is refused.

#### Scenario: Folder in a room and in My documents

- **WHEN** the Owner, a Room admin or a User creates `Test folder` in their My documents, or the Owner creates one in a Custom room
- **THEN** the folder is returned with that title and, in the room, `parentId` equal to the room id
- **AND** `Folder with spaces`, `Special_Chars-Folder` and `Nested.Folder` are stored unchanged, and a child created inside a folder reports that folder as `parentId`

#### Scenario: Duplicate title in one parent

- **WHEN** `Duplicate Folder` is created twice in My documents
- **THEN** both calls succeed and the second folder has the same title and parent

#### Scenario: Refused parents and title length

- **WHEN** a folder is created in a section root (Archive, Trash, Rooms, AI Agents, Forms), in the Knowledge folder of an AI room, or with a 166-character title
- **THEN** the system answers 403 for the roots and the Knowledge folder (which stays empty) and 400 for the title

### Requirement: Renaming a file

`updateFile` SHALL rename a file without changing its format: the original extension is always preserved and
appended to the requested title. An empty or whitespace-only title SHALL leave the name unchanged (200).
Characters a file name cannot hold are replaced with `_`. A title longer than 170 characters is rejected
(400); a file id that does not exist answers 404. `lastVersion` restores that version as described under
versions; a `lastVersion` equal to the current version is rejected (400).

#### Scenario: Extension handling

- **WHEN** `Autotest No Ext Original.docx` is renamed to `Autotest No Ext Renamed`, to `Autotest With Ext Renamed.docx` and to `Autotest Change Ext Renamed.txt`
- **THEN** the titles become `Autotest No Ext Renamed.docx`, `Autotest With Ext Renamed.docx` and `Autotest Change Ext Renamed.txt.docx`, `fileExst` staying `.docx`
- **AND** so a title without extension gets the file's own extension and `Renamed.txt` on a `.docx` file becomes `Renamed.txt.docx`

#### Scenario: Blank title and special characters

- **WHEN** a file is renamed to `""`, `"   "`, `Autotest A & B` or `Autotest A/B`
- **THEN** the first two leave the title unchanged (200) and the others yield `Autotest A & B.docx` and `Autotest A_B.docx` — `&` is kept, `/` becomes `_`

#### Scenario: Invalid rename

- **WHEN** a file is renamed with a 166-character name plus `.docx`, or file id 999999999 is renamed
- **THEN** the system answers 400 and 404 ("The required file was not found", bug 80774) respectively

#### Scenario: Same version as lastVersion

- **WHEN** `updateFile` is sent with a `lastVersion` equal to the file's current version
- **THEN** the system answers 400 "The new version cannot be the same as the current one" (bug 80773)

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
with the file (the first two as a list holding exactly one file). `fileExst` follows the uploaded name and
`pureContentLength` the uploaded size. On a name clash `createNewIfExist` `false` or omitted SHALL add a new
version to the existing file, `true` SHALL keep both under a numeric suffix.

#### Scenario: Upload into a room, a subfolder and My documents

- **WHEN** `autotest-upload.txt` is uploaded into a Custom room, into a subfolder of it and into My documents
- **THEN** the answer holds one file whose `folderId` is the target folder, whose `createdBy` is the caller and whose title is in the folder listing

#### Scenario: Formats and titles kept

- **WHEN** files named `.pdf`, `.xlsx`, `.pptx`, `.png`, `.jpg`, `.zip`, `.csv` or `.md`, or titles with spaces, parentheses and Cyrillic letters, are uploaded
- **THEN** `fileExst` follows the uploaded name, the titles are kept and `pureContentLength` equals the uploaded size
- **AND** `insertFileToMy` takes an explicit `title` that overrides the part's own name, and `storeOriginalFile` is accepted without changing the extension

#### Scenario: Name clash on upload

- **WHEN** `autotest-overwrite.txt` is uploaded twice without `createNewIfExist`
- **THEN** the second answer has the first file's id and a greater `version`
- **AND** with `createNewIfExist:true` the second answer has a different id; `createNewIfExist:false` with no clash simply creates the file

#### Scenario: Empty file and missing file

- **WHEN** a zero-byte `autotest-empty.txt` is uploaded, and then a request with no file part is sent
- **THEN** the first answers 200 with one file (an empty file is accepted since v3.6 and reported as `contentLength` `0 bytes` on insert) and the second answers 400 (bugs 81547 and 81549)

#### Scenario: Bad destinations

- **WHEN** a file is uploaded into an archived room, into folder id 0 or into folder id 999999999
- **THEN** the system answers 403 for the archived room and 404 for the ids

### Requirement: Chunked upload session

`createUploadSession` (`POST api/2.0/files/{folderId}/upload/create_session`) SHALL reserve an upload for
`fileName` and `fileSize` and answer a session whose non-empty `id` every later call quotes; the parts go
through `uploadAsyncSession` (`POST .../session/{sessionId}/upload`) as numbered chunks and `finalizeSession`
(`PUT .../session/{sessionId}/finalize`) produces the file from the uploaded bytes. The session lapses twelve
hours after the last part. Size and target are checked first.

#### Scenario: Whole file in chunks

- **WHEN** a session is created for `new.docx` with its real length, the content is sent in chunks of at most `chunkUploadSize` of the files settings (several threads may upload them, v2.5) and the session is finalized
- **THEN** the answer reports `uploaded:true`, title `new.docx`, `file.folderId` equal to My documents and `pureContentLength` equal to the declared length, and the bytes read back through the file's `viewUrl` are identical to the upload

#### Scenario: Refused before the session exists

- **WHEN** the declared size is above the portal limit for chunked uploads, or — in a Knowledge folder — above the vectorisation limit or in a format that cannot be extracted
- **THEN** the session is refused (402 / 403) and nothing is reserved
- **AND** a section root or a folder the caller cannot write to answers 403 and an unknown folder 404

#### Scenario: PDF classified from the first chunk

- **WHEN** a `.pdf` is uploaded through a session
- **THEN** the file is classified as a PDF form or a plain PDF from its first chunk

#### Scenario: Name clash on a session

- **WHEN** a session is created for a name the folder already holds
- **THEN** `createNewIfExist` has the upload meaning: `true` stores the file beside the same-named one under a numeric suffix, `false` adds a new version to it

### Requirement: Aborting an upload session

`abortUploadSession` (`DELETE .../session/{sessionId}`) SHALL drop a session that was opened by the caller —
before any part, or after it was finalized — without creating a file; a session of another account is
refused (403, bug 82276) and an anonymous caller gets 401.

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

Deletion, move, copy, duplication, bulk download, mark-as-read and emptying the Trash SHALL run in the
background; the starting call returns the caller's operations of that kind. `getOperationStatuses`
(`GET api/2.0/files/fileops`, optional `id`) and `getOperationStatusesByType`
(`GET api/2.0/files/fileops/{type}`) SHALL list only the caller's operations; a finished one is reported once
and then dropped. `returnSingleOperation` and `deleteAfter` SHALL narrow the answer and prune the record.

#### Scenario: Shape of an operation record

- **WHEN** an operation record is returned
- **THEN** it carries `id`, `Operation` (one of Move, Copy, Delete, Download, MarkAsRead, Import, Convert, Duplicate), `progress` 0–100, `finished`, `processed` (a decimal count in a string), `error` (empty on success — the only way to tell success from failure), `url` (bulk download only), `files` and `folders` (what the operation produced; empty for a deletion)

#### Scenario: Shape of a finished deletion

- **WHEN** a file in My documents is deleted with `immediately` true or false and the operation is polled until `finished`
- **THEN** the record has `Operation` Delete, `finished` true, `progress` 100, `processed` `"1"` and an empty `error`

#### Scenario: Nothing to report

- **WHEN** `getOperationStatuses` is called on a fresh portal, or with id `00000000-0000-0000-0000-000000000000`
- **THEN** the answer is an empty list — an empty list means nothing left to report, and the list returned by the starting call may already be empty for a fast operation
- **AND** `getOperationStatusesByType` with Delete, Copy, Duplicate, Download or MarkAsRead and no running operation, or Delete with id `9999999`, answers an empty list; an unknown operation type on the route (99) answers 400

#### Scenario: Single operation and record removal

- **WHEN** a move is started with `returnSingleOperation:true`
- **THEN** the answer holds exactly one record with `Operation` Move — the operation just started
- **AND** a move started with `deleteAfter:true` leaves `getOperationStatuses` empty once the file is at the destination, the record being removed as soon as the operation finishes

### Requirement: Cancelling an operation

`terminateTasks` (`PUT api/2.0/files/fileops/terminate/{id}`, v4.0 "stop an operation") SHALL cancel the
caller's operation with that id and answer with the caller's remaining operations (200); the cancelled record
is dropped from `getOperationStatuses` at once. Cancelling does not undo what was already done. An id that is
not among the caller's operations cancels nothing and is not an error (200, empty list).

#### Scenario: Running operations of every kind

- **WHEN** a delete to Trash, a duplicate, a copy, a move, a mark-as-read or an empty-trash of fifty files is started and terminated by its id
- **THEN** the call answers 200 and the operation disappears from `getOperationStatuses`

#### Scenario: Ids that cancel nothing

- **WHEN** the Owner terminates `00000000-0000-0000-0000-000000000000` or an already finished delete, a User terminates the Owner's operation, a Guest or an anonymous caller terminates an unknown id
- **THEN** every call answers 200 with an empty list — unknown, finished and foreign ids and anonymous callers all get the same empty answer

### Requirement: Moving files and folders

`moveBatchItems` (`PUT api/2.0/files/fileops/move`) SHALL move the listed files and folders into
`destFolderId` as one operation: a moved file reports the destination as `folderId`, a moved folder as
`parentId` and keeps its content, and the source no longer lists the entry. With `content:true` only the
folder's content moves. The caller needs create access at the destination and the right to remove the entry
from its source (`room-role-permissions`). An unknown file id answers 404 (bug 82243).

#### Scenario: File and folder moved

- **WHEN** a file is moved from My documents to a folder, and a folder holding a file is moved into a Custom room
- **THEN** the operation finishes without error, `getFileInfo` reports the new `folderId`, `getFolderInfo` reports the room as `parentId`, the inner file is still inside and the source no longer lists the folder

#### Scenario: Accepted routes

- **WHEN** entries are moved between My documents and its subfolders, from My documents into a Custom, Collaboration, Public or Virtual Data Room, between rooms, from a room back to My documents, or between subfolders of one room
- **THEN** every move is accepted, and a batch may mix files and folders

#### Scenario: Room types as destination

- **WHEN** a `.docx` is moved into a Collaboration, Public or Virtual Data Room, into a Form filling room, or into an archived room
- **THEN** the first three succeed, the Form filling room answers 403 (it takes no non-form file), the archived room answers 403
- **AND** a PDF carrying the ONLYOFFICE form signature is moved into the Form filling room and leaves the source

#### Scenario: Contents only, empty batch, same folder

- **WHEN** a folder is moved with `content:true`, an empty batch is sent, and a file is moved onto its own parent
- **THEN** only the inner file appears at the destination without the folder, and the other two calls answer 200

#### Scenario: Refused destinations and sources

- **WHEN** a file or folder is moved into the Forms root or the AI Agents root, a User moves the Owner's file, a Content creator moves a file out of the room, or a User moves a room into their My documents
- **THEN** the system rejects each request (403; "Access denied" for the room, bug 65580)

### Requirement: Copying files and folders

`copyBatchItems` (`PUT api/2.0/files/fileops/copy`) SHALL copy the listed files and folders into
`destFolderId`, leaving the originals in place; `copyFileAs` (`POST api/2.0/files/file/{fileId}/copyas`)
SHALL copy one file under a new title into a folder. A copied folder arrives with its content; `content:true`
copies the content alone; `deleteAfter:true` only removes the operation record. A destination the caller
cannot write to is refused (403); an unknown file id or destination answers 404.

#### Scenario: Copy keeps the source

- **WHEN** a file is copied from My documents into a Custom, Public, Collaboration or Virtual Data Room, into a subfolder several levels deep, from a room into My documents, between rooms, and two files are copied at once
- **THEN** every copy is listed at the destination and every source is still listed where it was

#### Scenario: Copy as a new file

- **WHEN** `copyFileAs` copies a file under a new title into a folder
- **THEN** the copy appears with that title; without a document server only a destination of the same extension works

#### Scenario: Folder copies

- **WHEN** a folder holding a file is copied into a room, with `content:true`, together with a file, as two folders at once, and as an empty folder
- **THEN** the folder appears with its inner file; with `content:true` only the inner file appears; mixed and multiple batches place everything; the empty folder arrives empty
- **AND** a level-3 subfolder copied into another room arrives with its inner file

#### Scenario: Refused copies

- **WHEN** a folder is copied into its own subfolder, two `.docx` files or a whole Form filling room are copied into a Form filling room, a file is copied into the Forms root or an archived room, a `.docx` into a folder of a Form filling room, or anything into a destination the caller cannot write to
- **THEN** the system rejects each request (403)

#### Scenario: Locked, editing and unsupported

- **WHEN** a User copies a file the Owner locked, a file the Owner is editing, or the Owner copies a file whose extension is not vectorisable into the Knowledge folder of an AI room
- **THEN** the system rejects each request (403)

#### Scenario: PDF form into a Form filling room

- **WHEN** a PDF form is copied into a Form filling room, with or without `toFillOut`
- **THEN** the copy arrives in the room

#### Scenario: Ids and empty batch

- **WHEN** file id 999999999 is copied, a file is copied into folder 999999999, the same file id is listed twice, or an empty batch is sent
- **THEN** the first two answer 404 (bug 82204 for the file), the duplicate id yields exactly one copy, and the empty batch answers 200

### Requirement: Settling a name clash on move and copy

`conflictResolveType` (`Skip`, `Overwrite`, `Duplicate`) SHALL decide what a move or copy does with an item
whose title already exists at the destination, directly under a room or any level deep. `Skip` SHALL leave
the destination unchanged and, for a move, leave the item at its source; `Overwrite` SHALL add the content
as a new version of the existing entry (same id, no second file); `Duplicate` SHALL store a second entry
next to the existing one.

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
empty list when none clashes. A source id that resolves to nothing is passed over. Create access to the
destination is required, and the destination MUST be named.

#### Scenario: Clash reported regardless of the resolve mode

- **WHEN** My documents and a room both hold `Autotest CheckMove ConflictFile.docx` and the check is called with `Skip` or `Overwrite`
- **THEN** the answer holds exactly that title; with no same-named file, or with `Duplicate` and no clash, the answer is empty
- **AND** a file checked against the folder it already lives in is reported as a clash

#### Scenario: Destinations and batch shapes

- **WHEN** a file is checked against a Collaboration, Form filling, Public or Virtual Data Room, a folder against a Custom room, two files, a file and a folder, a folder with `content:true`, or an empty batch
- **THEN** every answer is an empty list

#### Scenario: Errors

- **WHEN** the check names an archived room, folder 999999999, no `destFolderId`, or file id 999999999 as the only source
- **THEN** the system answers 403, 404 (bug 81881), 400 (bug 81882) and 200 with an empty list respectively

#### Scenario: Route shared with the Move status

- **WHEN** the generated clients call `GET fileops/move`
- **THEN** they reach this check, which also shadows the status route of the Move operation type

### Requirement: Checking whether a destination accepts the files

`checkMoveOrCopyDestFolder` (`GET api/2.0/files/fileops/checkdestfolder`) SHALL report whether the
destination accepts the listed files: `result` is `AllAllowed`, `PartAllowed` or `NoneAllowed` and `files`
names the accepted ones. Every folder and room accepts every file except a Form filling room, which accepts
only PDF forms. Folder ids and `deleteAfter` do not change the result; name clashes are not checked. An
unwritable or unknown destination and a missing `destFolderId` SHALL be refused.

#### Scenario: Accepted files are listed

- **WHEN** one or two files from My documents are checked against a Custom room
- **THEN** `result` is AllAllowed and `files` holds exactly those titles; a Collaboration, Public or Virtual Data Room and a folder as source give AllAllowed too

#### Scenario: Form filling room

- **WHEN** a `.docx` and then a PDF form are checked against a Form filling room
- **THEN** the results are NoneAllowed and AllAllowed

#### Scenario: Errors

- **WHEN** the check names an archived room, a room the User or Guest cannot write to, folder 999999999, or no `destFolderId`
- **THEN** the system answers 403 (bug 82103), 403 (bug 82104), 404 (bug 82158) and 400 (bug 82159)

### Requirement: Duplicating files and folders

`duplicateBatchItems` (`PUT api/2.0/files/fileops/duplicate`, v2.6) SHALL copy each listed file and folder
next to itself in the folder it lives in and answer with the caller's duplicate operations. The copy keeps
the original name with an index suffix so nothing is overwritten; a duplicated folder carries its content;
the `files` and `folders` of the finished operation name the copies. Duplicating a whole room is specified in
`room-types`.

#### Scenario: Index suffix

- **WHEN** `source_document.docx` in My documents, or a folder `folder`, is duplicated with `returnSingleOperation:true`
- **THEN** the finished operation lists `source_document (1).docx` (or a folder `folder (1)`) and the parent now holds both
- **AND** the suffix grows with every repetition: ` (1)`, then ` (2)` and so on

#### Scenario: Content and places

- **WHEN** a folder holding a file is duplicated, a file and a folder are duplicated together, two files at once, a file in a Custom room, a file in a nested subfolder, or a file titled `Autotest Dup Üñó Résumé.docx`
- **THEN** each duplicate appears next to its source — in My documents, the room or the subfolder — the duplicated folder holds the inner file, and the diacritics are kept

#### Scenario: Refused and unknown

- **WHEN** a file in an archived room, a file in Trash, file id 999999999 or folder id 999999999 is duplicated, or an empty batch is sent
- **THEN** the system answers 403, 403, 404 (bug 82210), 404 and 200

### Requirement: Deleting a single file or folder

`deleteFile` (`DELETE api/2.0/files/file/{fileId}`) and `deleteFolder` (`DELETE api/2.0/files/folder/{folderId}`)
SHALL queue a delete operation whose body carries `deleteAfter` and `immediately`: `false` moves the entry
to the caller's Trash, `true` removes it for good. An id that does not exist answers 404. The call SHALL be
refused (403) for a locked or edited file and for callers without delete rights (`room-role-permissions`);
anonymous gets 401.

#### Scenario: Trash and immediate deletion

- **WHEN** a file is deleted with `immediately:false` and another with `immediately:true`
- **THEN** the first is listed in the Trash with the Trash as its `folderId` and its original title, and the second cannot be read any more (`getFileInfo` fails)

#### Scenario: Folder subtree

- **WHEN** a folder holding a child folder and a file is deleted with `immediately:true`
- **THEN** the folder, its child and the file are gone (`getFolderInfo` fails); with `immediately:false` the folder is listed in the Trash together with everything inside it

#### Scenario: Locked or editing file

- **WHEN** a User deletes a file the Owner locked in a room or in My documents, a file the Owner is editing, or a file shared read-write that the Owner locked
- **THEN** the system rejects each request (403)

#### Scenario: Callers without delete rights

- **WHEN** a member with Editing or Read rights in the room, a Guest, a User outside the room, or a User in another User's My documents deletes a file or folder, or an anonymous caller does
- **THEN** the system answers 403 for the signed-in callers and 401 for the anonymous one

#### Scenario: Unknown and repeated ids

- **WHEN** file id 99999 or folder id 99999 is deleted, or a folder is deleted twice
- **THEN** every refused call answers 404 ("The required file was not found" for the file; the second delete is bug 79459)

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
`getTrashFolder`) SHALL return every trashed file and folder when no filter is given, SHALL narrow the
listing with `folderType` to entries that originally sat in a section or room of the listed types, and with
`roomId` to entries from that room. Files inside a trashed folder are listed as trashed files themselves.

#### Scenario: Filter by origin

- **WHEN** files from My documents, a Custom, a Public and a Virtual Data Room are trashed and the Trash is read with no filter, with `USER`, with `VirtualRooms`, with `PublicRoom` and with `USER` plus `CustomRoom`
- **THEN** the unfiltered listing holds all four, `USER` (My documents) only the My documents file, `VirtualRooms` (every room) only the room files, `PublicRoom` only the Public room file, and the pair the My documents and Custom room files
- **AND** `CustomRoom`, `PublicRoom`, `EditingRoom` or `VirtualDataRoom` select one room type and several types may be listed at once; the same holds for trashed folders

#### Scenario: Filter by room

- **WHEN** files and folders of one room are trashed and the Trash is read with that `roomId`
- **THEN** every trashed entry of the room is listed, a file that was not deleted is not, and the file of a trashed subfolder is listed too

### Requirement: Emptying the Trash

`emptyTrash` (`PUT api/2.0/files/fileops/emptytrash`) SHALL queue an operation that removes the caller's own
Trash for good and SHALL answer 200 also when the Trash is already empty. With `folderType` the sweep is
narrowed to the entries whose origin was a section or room of the listed types, leaving the rest in place.
`single:true` returns only the operation just started. Every authenticated account empties its own Trash
only; anonymous gets 401.

#### Scenario: Whole Trash

- **WHEN** a file, a folder, and a file deleted from a room are in the Trash and `emptyTrash` runs
- **THEN** the Trash holds none of them afterwards; a call on an empty Trash answers 200

#### Scenario: One section only

- **WHEN** files from My documents, a Custom room and a Form filling room are in the Trash and `emptyTrash` runs with `folderType=USER`
- **THEN** the My documents file is gone and the room and form-room files are still there (bug 82588)
- **AND** with `folderType=VirtualRooms` the room entries go and the My documents entries stay

#### Scenario: Per account

- **WHEN** the Owner has a trashed file and a Guest empties their own Trash, or a User, Room admin, DocSpace admin or Guest with a trashed file of their own empties theirs
- **THEN** the Owner's file stays (a Guest's call never touches the Owner's Trash), and each account's own file is gone; an anonymous caller gets 401

### Requirement: File versions and their history

Every file SHALL start at `version` 1 in `versionGroup` 1 with the comment `Created` and `fileStatus` None.
`getFileVersionInfo` (`GET api/2.0/files/file/{fileId}/history`) SHALL list the versions, one record each;
`getEditHistory` (`GET .../edit/history`) the editing revisions. `updateFile` with `lastVersion` N SHALL
store that version's content again as a new version; `getFileInfo` with `version` SHALL return that version.
Opening a past version SHALL require the right to read the history.

#### Scenario: New file

- **WHEN** a file is created in My documents or in a room and its version info is read
- **THEN** exactly one version is listed with `version` 1, `versionGroup` 1, the file's id and title, `comment` `Created` and `fileStatus` None, and `getEditHistory` lists one revision with a non-empty `key` and a user
- **AND** a version record carries `id`, `title`, `version`, `versionGroup`, `folderId`, `fileExst`, `webUrl`, `viewUrl`, `createdBy`, `updatedBy` and `comment`; a revision carries `id`, `key`, `version`, `versionGroup`, `user` and `created`

#### Scenario: History grows

- **WHEN** `updateFile` is called with `lastVersion` 2 and then the edit history and `getFileInfo(version=1)` are read
- **THEN** the history holds more than one entry and the specific read reports `version` 1
- **AND** reading the history works in an archived room

#### Scenario: Unknown file

- **WHEN** the edit history and the version info of file 999999999 are read
- **THEN** `getEditHistory` answers 404 (bug 80962) while `getFileVersionInfo` answers 403 for the same id

#### Scenario: Past version needs history access

- **WHEN** a member with Read access opens the current version and then `version=1` through `openedit`
- **THEN** the current version opens and `version=1` is refused (403, bug 80683)

### Requirement: Changing and restoring versions

`changeVersionHistory` (`PUT .../history`, `version`, `continueVersion`) SHALL split the given version into
a revision group of its own with `continueVersion:false` and fold it back with `true`. `restoreFileVersion`
(`POST .../restoreversion`, `version` or `url`) SHALL store the chosen version's content on top of the
history as a new version and answer the editing revisions, grown by one. Both answer 404 for an unknown file
and 403 in an archived room; the restore route accepts POST only.

#### Scenario: Split and merge

- **WHEN** a file with two versions has version 2 split with `continueVersion:false` and then merged with `true`
- **THEN** after the split versions 1 and 2 carry different `versionGroup` values and after the merge the number of groups is not larger than before
- **AND** the call on file 999999999 answers 404 and in an archived room 403

#### Scenario: Restore

- **WHEN** version 1 of a two-version file is restored
- **THEN** the answer lists the revisions, one more than before, each with id, key, version, group, user and `created`
- **AND** a restore without `version` and `url` answers 400, of file 999999999 answers 404, in an archived room 403, and a GET to the route is not 200 and restores nothing (bug 78027)

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
and in a room, is idempotent and affects only the named file. Another account's lock can only be released
by someone with manager rights. Unknown file 404, no access 403, anonymous 401. Which roles may lock is in
`room-role-permissions`.

#### Scenario: Lock and unlock

- **WHEN** a file in My documents or in a room is locked, locked again, unlocked and unlocked again
- **THEN** the answers report `locked` true, true, not true and not true, and `getFileInfo` agrees

#### Scenario: Only the named file

- **WHEN** three files are locked and the second is unlocked
- **THEN** the first and third are still locked and the second is not (bug 82178)

#### Scenario: Someone else's lock

- **WHEN** a Room manager unlocks a file a Content creator locked, and a Content creator unlocks a file the Owner locked
- **THEN** the first succeeds and the second answers 403
- **AND** a lock request on file 999999999 answers 404 (bug 80788)

#### Scenario: Lock blocks others

- **WHEN** another account tries to delete or copy a file that is locked
- **THEN** the deletion and the copy are refused (403) while the lock stands

### Requirement: Downloading a file

`getPresignedUri` (`GET api/2.0/files/file/{fileId}/presigneduri`) SHALL answer an `http(s)` address of the
current version that carries a `stream_auth` token and `fileid=<id>`; `getPresignedFileUri`
(`GET .../presigned`) SHALL answer an object with `url` and the file's `filetype`. Both need read access to
the file, answer 404 for an unknown id and 401 for an anonymous caller. The bytes are fetched by following
the address.

#### Scenario: Address minted

- **WHEN** the Owner asks for the presigned URI and the presigned file URI of a room file
- **THEN** the first contains `stream_auth=` and `fileid=<id>` and the second has an `http(s)` `url` and `filetype` `.docx`
- **AND** the `viewUrl` of a file serves the same content

#### Scenario: Access

- **WHEN** a Read member, a DocSpace admin, a Room manager, a User without access, a Guest without access and an anonymous caller ask
- **THEN** the first three get an address and the others get 403, 403 and 401; id 999999999 answers 404

### Requirement: Bulk download as an archive

`bulkDownload` (`PUT api/2.0/files/fileops/bulkdownload`, `fileIds`, `folderIds`, `fileConvertIds`) SHALL
queue a Download operation that packs the listed files and folders into one archive and, once finished,
reports `url`, `processed` with the number of items and an empty `error`. Items of `fileConvertIds` are
converted before packing. Read access to every item suffices; no access 403, anonymous 401, an id that
resolves to nothing 404, an empty selection 200.

#### Scenario: Files and folders packed

- **WHEN** one file, two files, one folder, two folders, a file and a folder, or a file of a Custom room are downloaded
- **THEN** the operation finishes with `Operation` Download, `finished` true, empty `error` and a `url` with `filehandler.ashx?action=bulk`; a single file reports `processed` `"1"`
- **AND** with `returnSingleOperation:true` the answer holds that one Download operation

#### Scenario: Download with conversion

- **WHEN** a `.docx` is listed in `fileConvertIds` with `pdf` (an item is `key` file id, `value` target extension, optional `password` for a password-protected source, v3.1)
- **THEN** the operation finishes without error with `processed` `"1"` and a `url`

#### Scenario: Access and ids

- **WHEN** a User who is not a room member, or an anonymous caller, downloads a room file, file id 999999999 is listed, or the selection is empty
- **THEN** the system answers 403 (bug 81822), 401 (bug 81823), 404 and 200
- **AND** a Room manager, an Editing member, a Read member and a Guest with Read download the file

### Requirement: Converting a file

`startFileConversion` (`PUT api/2.0/files/file/{fileId}/checkconversion`, `startConvert`, `outputType`,
`password`, `version`, `createNewIfExist`, `sync`) SHALL queue the conversion of a file into another format
and answer the entries to poll with a `checkconversion` link of action `PUT`; `checkConversionStatus`
(`GET .../checkconversion`, optional `start`) reports them with a `GET` link. Without a document server the
queue stays empty, so both answer an empty list. The caller needs read access.

#### Scenario: Conversion queued

- **WHEN** a `.docx`, `.xlsx` or `.pptx` is sent with `startConvert:true` and `outputType` `pdf`, or with `startConvert:false`
- **THEN** the answer is an empty list with one link to `checkconversion` (action `PUT`), and `checkConversionStatus` answers an empty list with a `GET` link

#### Scenario: Target formats

- **WHEN** a target format is chosen
- **THEN** it comes from the document server's format list; the release notes record XML to `docx`/`xlsx` (v3.0), presentations to `txt` (v3.6), `.pages`/`.numbers`/`.key`/`.hwp`/`.hwpx`/`.odg`/`.md` and diagram formats (v3.2.1), and that `docx` to `docxf` is no longer offered (v2.6)

#### Scenario: Unknown file and access

- **WHEN** file 999999999 is converted, a User outside the room converts or polls a room file, or an anonymous caller does
- **THEN** the unknown file answers 200 with an empty list (not 404), the User 403 and the anonymous caller 401
- **AND** the Owner, a DocSpace admin, a Room manager and a Content creator on their own file succeed

### Requirement: Thumbnails

`createThumbnails` (`POST api/2.0/files/thumbnails`) SHALL queue thumbnail generation for the listed file ids
and echo those ids back; `folderIds` are accepted and ignored, and an id that names no file is echoed like
any other. The thumbnails themselves are built in the background in the configured sizes and are read
through `thumbnailStatus` / `thumbnailUrl` of the file. Any signed-in account with read access may queue
them.

#### Scenario: Ids echoed

- **WHEN** one file, two files, or a file together with a folder are sent
- **THEN** the answer holds exactly the file ids, the folder id is not in it; id 999999999 is echoed as well

#### Scenario: Sizes and media limits

- **WHEN** thumbnails are built
- **THEN** they come in the sizes of the installation's thumbnail settings (1280×720 and 3840×2160 by default, v2.6)
- **AND** an image larger than the installation's image limit (30 MB by default) and a video larger than its video limit (1000 MB by default) get none

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
the caller — created by another account since the caller's last visit — with `title`, `fileEntryType`,
`createdBy` and `updated`; reading the folder clears the marks. `markAsRead`
(`PUT api/2.0/files/fileops/markasread`) SHALL clear the marks of the listed files and of the files inside
the listed folders as a MarkAsRead operation; it is idempotent and checks no per-item access.

#### Scenario: Files by other members are new

- **WHEN** two Content creators each create a file in the Owner's room after the Owner's visit, and another creates a subfolder
- **THEN** the Owner's news lists both files and not the subfolder (folders are never marked); files the Owner created are not listed, and an empty room gives an empty list

#### Scenario: Visit clears

- **WHEN** a member creates a file before the Owner's first visit, or the Owner re-reads the room after the news appeared
- **THEN** the news does not list the file after the visit (bug 81712)

#### Scenario: Unknown folder

- **WHEN** the news of folder id 0 or of an unknown id are read
- **THEN** the system answers 404 (bug 81519)

#### Scenario: Mark as read

- **WHEN** a Read member marks one file, two files, or a subfolder holding a new file as read
- **THEN** the answer holds a MarkAsRead operation and the room news no longer lists those files; marking again answers 200

#### Scenario: Lenient input

- **WHEN** `markAsRead` is sent with `{}`, with empty ids, with file id 999999999, or by a User who is not a room member for a room file
- **THEN** every call answers 200 — an empty or absent body and an id that names nothing are accepted; only an anonymous caller is refused (401)

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
