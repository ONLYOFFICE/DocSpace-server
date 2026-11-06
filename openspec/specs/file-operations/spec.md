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

Sources: the integration tests of the Files product, the OpenAPI contract (operations are named by their
`operationId`), the product release notes (v2.5 to v4.0), which say what was shipped and when, and the product
code where a test leaves a point open; each is named per row in the verification record next to this
specification. Since v4.1 the integration host runs a document server that reaches the portals, so a queued
conversion, a copy converted to another format and a PDF saved from a document are covered by tests; the
content of a finished conversion and the generated thumbnails are not yet, and those points rest on the code
and the API document, as that record says. Where the release notes and the server disagree this specification
follows the server. HTTP status codes are stated where the tests assert them. Behaviour that looks like a
defect is not written here as a requirement; it is listed under "Suspected defects" in the verification
record, and a scenario that names a bug number describes what the server is meant to do while that bug is
open.

## Requirements

### Requirement: Creating a blank file by title

`createFile` (`POST api/2.0/files/{folderId}/file`) and `createFileInMyDocuments` (`POST
api/2.0/files/@my/file`) SHALL create a file from the portal's blank template, normalising the requested title
to a format the portal can create. A title longer than 170 characters SHALL be rejected (400); a section root
or a folder id the caller cannot resolve SHALL be refused (403). The `templateId` and `formId` sources are not
covered by tests and are not specified here.

#### Scenario: Title normalisation

- **WHEN** a file is created with the title `Autotest Document`, `Autotest Document.docx`, `Autotest Document.txt` or `Autotest Document.md`
- **THEN** the stored titles are `Autotest Document.docx`, `Autotest Document.docx`, `Autotest Document.docx` and `Autotest Document.md`, each with a positive id
- **AND** the rule is: a title without an extension gets `.docx`; `.docx`, `.xlsx`, `.pptx` and `.pdf` are kept; an extension the portal cannot create from a template (for example `.txt`) is replaced by `.docx`, except `.md` and `.markdown`, which are kept as given (bug 80324)

#### Scenario: Template chosen by culture

- **WHEN** a blank file is created
- **THEN** the template is taken from the template set of the caller's culture when one exists and from the default set otherwise
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

#### Scenario: Non-PDF file in a Form filling room

- **WHEN** a `.docx`, a text file or an HTML file is created directly in a Form filling room
- **THEN** the system answers 403 and stores nothing
- **AND** a Form filling room refuses the creation of anything but a PDF form

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

`createFolder` SHALL create a folder with the given title under the given parent and return it with `parentId`
equal to the parent and a positive id. A title of 166 characters SHALL be rejected (400); an empty title SHALL
be rejected (400, bug 81507). Folders SHALL NOT be created in a section root, in the Knowledge folder of an AI
room, nor in an Instructions, Knowledge or AI project room (403); a parent that does not exist is refused.

#### Scenario: Folder in a room and in My documents

- **WHEN** the Owner, a Room admin or a User creates `Test folder` in their My documents, or the Owner creates one in a Custom room
- **THEN** the folder is returned with that title and, in the room, `parentId` equal to the room id
- **AND** `Folder with spaces`, `Special_Chars-Folder` and `Nested.Folder` are stored unchanged, and a child created inside a folder reports that folder as `parentId`

#### Scenario: Duplicate title in one parent

- **WHEN** `Duplicate Folder` is created twice in My documents
- **THEN** both calls succeed and the second folder has the same title and parent
- **AND** titles need not be unique: two folders with the same title may sit in one parent

#### Scenario: Refused parents and title length

- **WHEN** a folder is created in a section root (Archive, Trash, Rooms, AI Agents, Forms), in the Knowledge folder of an AI room, in an Instructions room, or with a 166-character title
- **THEN** the system answers 403 for the roots, the Knowledge folder (which stays empty) and the Instructions room, and 400 for the title

### Requirement: Renaming a file

`updateFile` SHALL rename a file without changing its format: the original extension is always preserved and
appended to the requested title. An empty or whitespace-only title SHALL leave the name unchanged (200).
Characters a file name cannot hold are replaced with `_`. A title longer than 170 characters is rejected
(400); a file id that does not exist answers 404. A file locked by another account SHALL NOT be renamed.

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
- **AND** any other `lastVersion` restores that version, as described under the versions of a file

#### Scenario: Locked by another member

- **WHEN** a Content creator renames a file of the room that the Owner locked
- **THEN** the rename is refused, the title and the lock are unchanged, and the refusal is a client error (bug 84842: reported as 500 today)

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

`uploadFile` (`POST api/2.0/files/{folderId}/upload`), `uploadFileToMy`, `insertFile` (`POST
api/2.0/files/{folderId}/insert`) and `insertFileToMy` (`POST api/2.0/files/@my/insert`) SHALL store the
uploaded part as a file of the target folder and answer with the file (the upload calls as a list holding
exactly one file). On a name clash `createNewIfExist` `false` or omitted SHALL add a new version to the
existing file, `true` SHALL keep both under a numeric suffix.

#### Scenario: Upload into a room, a subfolder and My documents

- **WHEN** `autotest-upload.txt` is uploaded into a Custom room, into a subfolder of it and into My documents
- **THEN** the answer holds one file whose `folderId` is the target folder, whose `createdBy` is the caller and whose title is in the folder listing

#### Scenario: Formats and titles kept

- **WHEN** files named `.pdf`, `.xlsx`, `.pptx`, `.png`, `.jpg`, `.zip`, `.csv` or `.md`, or titles with spaces, parentheses and Cyrillic letters, are uploaded
- **THEN** `fileExst` follows the uploaded name, the titles are kept and `pureContentLength` equals the uploaded size
- **AND** `insertFileToMy` takes an explicit `title` that overrides the part's own name, and `storeOriginalFile` is accepted without changing the extension

#### Scenario: Insert into a folder with the contract's field names

- **WHEN** `insertFile` is sent to a folder of My documents through a generated client, whose multipart fields are named `insertFile.file` and `insertFile.title`
- **THEN** the file is stored under the given `title` with `fileExst` `.txt`, `folderId` the folder and `pureContentLength` the uploaded size — the flat names `file` and `title` are read as well

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

#### Scenario: Non-PDF file uploaded to a Form filling room

- **WHEN** a text file is sent to a Form filling room through `uploadFile` or `insertFile`
- **THEN** the system answers 403 and the room stays empty

### Requirement: Chunked upload session

`createUploadSessionInFolder` (`POST api/2.0/files/{folderId}/session`) and the older `createUploadSession`
(`POST .../upload/create_session`) SHALL reserve an upload for `fileName` and `fileSize`; size and target are
checked first. Nothing is stored until a part arrives. The parts go through the session routes and
`finalizeSession` (`PUT .../session/{sessionId}/finalize`) SHALL produce the file. What a private room demands of
a session is in `private-rooms`.

#### Scenario: Whole file in chunks

- **WHEN** a session is created for `new.docx` with its real length, the content is sent in chunks of at most `chunkUploadSize` of the files settings (several threads may upload them, v2.5) and the session is finalized
- **THEN** the answer reports `uploaded:true`, title `new.docx`, `file.folderId` equal to My documents and `pureContentLength` equal to the declared length, and the bytes read back through the file's `viewUrl` are identical to the upload

#### Scenario: Refused before the session exists

- **WHEN** the declared size is above the portal limit for chunked uploads, or — in a Knowledge folder or a Knowledge room — above the vectorisation limit or in a format that cannot be extracted
- **THEN** the session is refused (402 / 403) and nothing is reserved
- **AND** a room the caller only reads, a folder the caller cannot open and a section root answer 403 on both routes, an unknown folder 404, a negative `fileSize` 400, and no file appears

#### Scenario: PDF classified from the first chunk

- **WHEN** a `.pdf` is uploaded through a session
- **THEN** the file is classified as a PDF form or a plain PDF from its first chunk

#### Scenario: Name clash on a session

- **WHEN** a session is created for a name the folder already holds
- **THEN** `createNewIfExist` has the upload meaning: `true` stores the file beside the same-named one under a numeric suffix, `false` adds a new version to it holding the new bytes
- **AND** two sessions opened for one name with `createNewIfExist` true stay independent and produce two files with their own bytes

#### Scenario: Session reserved

- **WHEN** a session is opened on either route for a declared size
- **THEN** the answer carries a 32-character `id` that every later call quotes and a `bytesTotal` equal to the declared size, and no file is listed
- **AND** the older route answers inside a success envelope

#### Scenario: Session lapses

- **WHEN** twelve hours pass after the last part of a session
- **THEN** the session lapses

### Requirement: Sending the parts of a session

`uploadSession` (`POST .../session/{sessionId}`) SHALL append parts in arrival order, answering 200 with
`uploaded:false` while bytes are missing and 201 with the stored file on the part that completes the declared
size, after which the session is closed. `uploadAsyncSession` (`POST .../session/{sessionId}/upload`) SHALL
store a part under `chunkNumber` (from 1), in any order, answering the session; the file appears on
`finalizeSession` (201).

#### Scenario: Sequential parts

- **WHEN** a payload below `chunkUploadSize` is sent as one part, and a larger one as two parts in order
- **THEN** the small one answers 201 at once with `uploaded` true, `version` 1 and the title; the large one answers 200 with `uploaded` false after the first part and 201 with the file after the last, no file being listed in between
- **AND** afterwards finalize, an abort and another part answer 404, and the folder lists the file once with the exact bytes at version 1

#### Scenario: Numbered parts in any order

- **WHEN** three numbered parts are sent in order, out of order or in parallel, or a part is resent under its number with different bytes, and the session is finalized
- **THEN** nothing is listed before finalize, the answer to a part carries the session `id` and `bytesTotal`, and the assembled file is byte-for-byte the payload, the last copy of a resent part winning
- **AND** two large sessions whose parts interleave in parallel produce two files that never mix their bytes

#### Scenario: Finalized session is closed

- **WHEN** a session, large or small, is finalized and then finalized, sent a part or aborted again
- **THEN** every later call answers 404, the folder holds the file once at version 1 with the uploaded bytes

#### Scenario: Small payload sent as one numbered part

- **WHEN** a payload below `chunkUploadSize` is sent as part 1 through `uploadAsyncSession` and the session is finalized
- **THEN** the finalize answers 201 with `uploaded` true and `version` 1, and the folder holds the file once with the exact bytes

#### Scenario: Incomplete and malformed parts

- **WHEN** a session is finalized with a part missing, is sent a part above `chunkUploadSize`, an empty part, a request without a file part, or a part numbered 0 or -1
- **THEN** the incomplete finalize answers 400 and stores nothing; the oversize part is refused (402), the missing part 400 and the bad numbers 400, and the session still completes byte-for-byte once the proper parts are sent
- **AND** none of these refusals harms the session

#### Scenario: Unknown session

- **WHEN** a session id that never existed, or a value that is not even shaped like one, is finalized, sent a part on either route, or aborted
- **THEN** every call answers 404

#### Scenario: Non-PDF session in a Form filling room

- **WHEN** a session for a text file is opened in a Form filling room
- **THEN** a small payload is refused with 403 on its part, on either route, and a large one on `finalizeSession`, and the room stays empty

### Requirement: Isolation of upload sessions

Opening a session SHALL check the right to create in the folder; aborting one SHALL check the folder of the
route and that the session belongs to the caller. An anonymous caller SHALL get 401 on every session route.
Two accounts uploading at the same time SHALL each get their own file in their own folder, and a refused call
SHALL leave the other account's session untouched.

#### Scenario: Anonymous caller

- **WHEN** an anonymous caller opens a session, sends a part on either route, finalizes or aborts the Owner's session
- **THEN** every call answers 401, no file appears, and the Owner's session still completes byte-for-byte

#### Scenario: Caller without the create right

- **WHEN** a User with Read access to the room, or a Room admin without access to the Owner's My documents, opens a session there on either route, or aborts the Owner's session in that folder
- **THEN** the system answers 403 and creates nothing; the Owner's session still completes with the right bytes

#### Scenario: Two accounts side by side

- **WHEN** the Owner and a member each open a session in their own My documents and their parts are sent turn by turn, the member also trying to abort the Owner's session
- **THEN** the abort answers 403, and each account's folder holds exactly one file with that account's bytes

#### Scenario: Holder of the session id

- **WHEN** a caller holding the id of another account's session sends a part or finalizes it
- **THEN** the part and finalize routes find the session by its id alone, so who holds the id can write into it (suspected defect F24 of the verification record)

### Requirement: Aborting an upload session

`abortUploadSession` (`DELETE .../session/{sessionId}`) SHALL drop a session that was opened by the caller,
before any part or after some, without creating a file and without touching another open session; the
aborted session answers 404 on every later call. A session of another account is refused (403, bug 82276), a
session that no longer exists — unknown, aborted or finalized — answers 404 (bug 82278), as does one opened
against another folder, which stays open; an anonymous caller gets 401.

#### Scenario: Session aborted

- **WHEN** a session is aborted before any part, or after its first part while a neighbouring session holds a part too
- **THEN** the call succeeds, no file with that title appears, the aborted session answers 404 to a part, a finalize and a second abort, and the neighbouring session still completes byte-for-byte
- **AND** aborting a session opened in a Custom room succeeds as well

#### Scenario: Session of another folder

- **WHEN** a session is aborted through a folder other than the one it was opened against
- **THEN** the system answers 404 and the session stays open

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
background; the starting call returns the caller's operations of that kind, the new one last.
`getOperationStatuses` (`GET api/2.0/files/fileops`, optional `id`) and `getOperationStatusesByType` (`GET
api/2.0/files/fileops/{type}`) SHALL list only the caller's operations; a finished record is handed out once —
by any read or by the answer to the next start — and then dropped.

#### Scenario: Shape of an operation record

- **WHEN** an operation record is returned
- **THEN** it carries `id` (a GUID), `Operation` (one of Move, Copy, Delete, Download, MarkAsRead, Import, Convert, Duplicate), `status` (Created, Running, Completed or Canceled), `progress` 0–100, `finished`, `processed` (a decimal count in a string), `error` (empty on success — the only way to tell success from failure), `url` (bulk download only), `files` and `folders` (what the operation produced; empty for a deletion)

#### Scenario: Shape of a finished deletion

- **WHEN** a file in My documents is deleted with `immediately` true or false and the operation is polled until `finished`
- **THEN** the record has `Operation` Delete, `finished` true, `progress` 100, `processed` `"1"` and an empty `error`

#### Scenario: Life of a record

- **WHEN** a copy of one file is started and polled, and a copy of sixty files is read twice in a row while it runs
- **THEN** the start answer shows the record Created or Running at `progress` 0 and `processed` `"0"` with no error; it ends Completed at 100 with `processed` `"1"`, one file and no `url`
- **AND** while it runs the record is Running under the same id, `progress` and `processed` never decrease, two reads in a row return the same record, and the finished sample is the last one

#### Scenario: Handed out once

- **WHEN** a finished copy is read once, and another copy is started while an earlier one finished unread
- **THEN** the next reads, with and without `id`, are empty; the start answer carries the earlier record finished and the new one last, after which the earlier one is not reported again
- **AND** with `deleteAfter:true` the finished record is never reported at all, although the file arrives

#### Scenario: Nothing to report

- **WHEN** `getOperationStatuses` is called on a fresh portal, or with id `00000000-0000-0000-0000-000000000000`
- **THEN** the answer is an empty list — an empty list means nothing left to report, and the list returned by the starting call may already be empty for a fast operation
- **AND** `getOperationStatusesByType` with Delete, Copy, Duplicate, Download or MarkAsRead and no running operation, or Delete with id `9999999`, answers an empty list; a kind outside the set on the route (99, 9, -1, `abc`) answers 400

#### Scenario: Kinds listed together

- **WHEN** a long copy, a move, a deletion, a duplication and a download are alive at once and the list is read
- **THEN** all five are reported in the order duplications, moves and copies, deletions, downloads; the read with the copy's `id` returns that one alone
- **AND** `getOperationStatusesByType` returns only records of its kind, hands out only those, and leaves the other kinds in place; a kind or id that does not match answers an empty list and keeps the record; a running move is never listed under Copy

#### Scenario: Private to the account

- **WHEN** a DocSpace admin reads the list, the list by id, the list by kind, or terminates the Owner's running copy by id or with an empty id
- **THEN** every answer is empty, and the Owner's copy completes in full without error

#### Scenario: Single operation and record removal

- **WHEN** a move is started with `returnSingleOperation:true`
- **THEN** the answer holds exactly one record with `Operation` Move — the operation just started
- **AND** a move started with `deleteAfter:true` leaves `getOperationStatuses` empty once the file is at the destination, the record being removed as soon as the operation finishes
- **AND** so `returnSingleOperation` narrows the answer and `deleteAfter` prunes the record

### Requirement: Cancelling an operation

`terminateTasks` (`PUT api/2.0/files/fileops/terminate/{id}`, v4.0 "stop an operation") SHALL cancel the
caller's operation with that id, an empty id cancelling every operation the caller is running, and answer with
the caller's remaining operations (200). A cancelled job stops where it stands and is reported once
afterwards, finished with `status` Canceled and the cancellation in `error`. An id that is not among the
caller's operations cancels nothing and is not an error (200).

#### Scenario: Running operations of every kind

- **WHEN** a delete to Trash, a duplicate, a copy, a move, a mark-as-read or an empty-trash of fifty files is started and terminated by its id
- **THEN** the call answers 200 and the operation disappears from `getOperationStatuses`

#### Scenario: Copy, move and duplication stopped half way

- **WHEN** a copy, a move of a folder's content, or a duplication of sixty files is terminated at 15 % and the record is read
- **THEN** the record is finished at 100 with Canceled and a non-empty `error`; the copy left part of the files at the destination and the source whole; every moved file is in the source or the destination exactly once; the original of the duplication is whole and the partial copy stops growing; nothing keeps running
- **AND** a second terminate of the same id answers an empty list and changes nothing
- **AND** cancelling does not undo what was already done

#### Scenario: Download stopped

- **WHEN** a bulk download of sixty files is terminated at 10 %
- **THEN** the record goes away or ends finished, no sample ever carries a `url`, and nothing keeps running

#### Scenario: Operations too fast to cancel

- **WHEN** a delete of a hundred files or the archiving of a room is terminated right after the start
- **THEN** each file is in the Trash or still in place, never lost; the room is archived whole or not at all with its files intact
- **AND** terminating a copy that already finished undoes nothing: the copy stays at the destination

#### Scenario: Ids that cancel nothing

- **WHEN** the Owner terminates `00000000-0000-0000-0000-000000000000`, an already finished delete, a whitespace-only id, `abc`, `0`, `-1`, a 300-character id or a path-like id; a User terminates the Owner's operation; a Guest or an anonymous caller terminates an unknown id
- **THEN** every call answers 200 with an empty list and the Owner's running copy completes in full, Completed without error

#### Scenario: Only the named operation

- **WHEN** a copy and a duplication run side by side and the copy is terminated by its id, then both run and the empty id is sent
- **THEN** in the first case the copy is cancelled with an error while the duplication completes in full; in the second both are cancelled with an error and both sources are whole

### Requirement: Moving files and folders

`moveBatchItems` (`PUT api/2.0/files/fileops/move`) SHALL move the listed files and folders into
`destFolderId` as one operation: a moved entry keeps its id, versions and bytes, and the source no longer
lists it. With `content:true` only the folder's content moves. The caller needs create access at the
destination and the right to remove every entry from its source (`room-role-permissions`); a batch the caller
may not move as a whole SHALL be refused as a whole.

#### Scenario: File and folder moved

- **WHEN** a file is moved from My documents to a folder, and a folder holding a file is moved into a Custom room
- **THEN** the operation finishes without error, `getFileInfo` reports the new `folderId`, `getFolderInfo` reports the room as `parentId`, the inner file is still inside and the source no longer lists the folder

#### Scenario: Identity and bytes survive the move

- **WHEN** a file with two versions, and a folder tree holding a file, are moved into a room
- **THEN** the file keeps its id, both versions and the bytes of the last one and is listed in the room only; the folder, its sub-folder and its file keep their ids and the file its bytes

#### Scenario: Accepted routes

- **WHEN** entries are moved between My documents and its subfolders, from My documents into a Custom, Collaboration, Public or Virtual Data Room, between rooms, from a room back to My documents, or between subfolders of one room
- **THEN** every move is accepted, and a batch may mix files and folders

#### Scenario: Room types as destination

- **WHEN** a `.docx` is moved into a Collaboration, Public or Virtual Data Room, into a Form filling room, or into an archived room
- **THEN** the first three succeed, the Form filling room answers 403 (it takes no non-form file), the archived room answers 403
- **AND** a PDF carrying the ONLYOFFICE form signature is moved into the Form filling room and leaves the source

#### Scenario: Contents only, empty batch, same folder

- **WHEN** a folder is moved with `content:true`, an empty batch is sent, a file is moved onto its own parent, and a file id is listed twice
- **THEN** only the inner file appears at the destination without the folder, the other two calls answer 200, and the repeated file is moved once leaving nothing behind

#### Scenario: Refused destinations and sources

- **WHEN** a file or folder is moved into the Forms root or the AI Agents root, a User moves the Owner's file, a Content creator moves a file out of the room, or a User moves a room into their My documents
- **THEN** the system rejects each request (403; "Access denied" for the room, bug 65580)

#### Scenario: Folder into its own subtree

- **WHEN** a folder is moved into itself, its child or its grandchild
- **THEN** the system answers a client error, queues no operation and leaves every listing as it was

#### Scenario: Batch refused as a whole

- **WHEN** a Content creator moves their own file together with the Owner's private file into the room
- **THEN** the system answers 403 and neither file moves — the own file alone is accepted

#### Scenario: Unknown ids

- **WHEN** file id 999999999 or folder id 999999999 is moved
- **THEN** the system answers 404 and moves nothing (bug 82243 for the file; bug 84436 for the folder, which answered 500)

#### Scenario: Folder holding a file open in the editor

- **WHEN** a folder holding a file open in an editing session and an unrelated folder are moved together
- **THEN** the unrelated folder arrives at the destination whatever becomes of the other one (bug 84725: the whole selection stays put today)

### Requirement: Copying files and folders

`copyBatchItems` (`PUT api/2.0/files/fileops/copy`) SHALL copy the listed files and folders into
`destFolderId`, leaving the originals in place; `copyFileAs` (`POST api/2.0/files/file/{fileId}/copyas`) SHALL
copy one file under a new title into a folder. A copy carries the latest content only and lives on its own
afterwards. A destination the caller cannot write to, or a batch the caller cannot read in full, is refused as
a whole (403).

#### Scenario: Copy keeps the source

- **WHEN** a file is copied from My documents into a Custom, Public, Collaboration or Virtual Data Room, into a subfolder several levels deep, from a room into My documents, between rooms, and two files are copied at once
- **THEN** every copy is listed at the destination and every source is still listed where it was
- **AND** `deleteAfter:true` keeps the source as well: it only removes the operation record

#### Scenario: Latest content, then independent

- **WHEN** a file with two versions is copied into a room, the original then gets a third version and is deleted for good
- **THEN** the copy is a new id holding the bytes of the second version, the original keeps both versions, and the copy keeps its bytes after both changes
- **AND** a folder tree is copied byte for byte, and the copy survives the deletion of the original tree

#### Scenario: Copy as a new file

- **WHEN** `copyFileAs` copies a file under a new title into a folder
- **THEN** the answer is the copy with its `id`, `title` and `folderId`, listed at the destination
- **AND** with `toForm` a `.docx` is converted by the document server into a `.docxf` template and that into a `.pdf` that the portal recognises as a PDF form

#### Scenario: Folder copies

- **WHEN** a folder holding a file is copied into a room, with `content:true`, together with a file, as two folders at once, and as an empty folder
- **THEN** the folder appears with its inner file; with `content:true` only the inner file appears; mixed and multiple batches place everything; the empty folder arrives empty
- **AND** a level-3 subfolder copied into another room arrives with its inner file

#### Scenario: Refused copies

- **WHEN** a folder is copied into its own subfolder, two `.docx` files or a whole Form filling room are copied into a Form filling room, a file is copied into the Forms root or an archived room, a `.docx` into a folder of a Form filling room, or anything into a destination the caller cannot write to
- **THEN** the system rejects each request (403)
- **AND** a folder copied into itself, its child or its grandchild is refused with a client error, nothing is queued and nothing changes

#### Scenario: Locked, editing and unsupported

- **WHEN** a User copies a file the Owner locked, a file the Owner is editing, or the Owner copies a file whose extension is not vectorisable into the Knowledge folder of an AI room
- **THEN** the system rejects each request (403)

#### Scenario: PDF form into a Form filling room

- **WHEN** a PDF form is copied into a Form filling room, with or without `toFillOut`
- **THEN** the copy arrives in the room

#### Scenario: Ids and empty batch

- **WHEN** file id 999999999 is copied, folder id 999999999 is copied, a file is copied into folder 999999999, the same file id is listed twice, or an empty batch is sent
- **THEN** the first three answer 404 (bug 82204 for the file, bug 84435 for the folder), the duplicate id harms nothing and every copy matches the original, and the empty batch answers 200

#### Scenario: Batch refused as a whole

- **WHEN** a Content creator copies their own file together with the Owner's private file into the room
- **THEN** the system answers 403 and nothing is copied — the own file alone is accepted

### Requirement: Settling a name clash on move and copy

`conflictResolveType` (`Skip`, `Overwrite`, `Duplicate`) SHALL decide what a move or copy does with an item
whose title already exists at the destination. `Skip` SHALL leave the destination unchanged and, for a move,
leave the item at its source; `Overwrite` SHALL add the content as a new version of the existing entry (same
id, no second file); `Duplicate` SHALL store the incoming entry next to the existing one under the first free
numbered title, both keeping their bytes.

#### Scenario: Skip

- **WHEN** a same-titled file is copied or moved with `Skip` into a room (or a level-3 folder) that already holds one
- **THEN** the destination still holds one file with the original id and bytes, and a skipped move leaves the file in its source folder with its bytes

#### Scenario: Overwrite

- **WHEN** a same-titled file is copied or moved with `Overwrite`
- **THEN** the destination still holds one file with the original id, now holding the incoming bytes; a copied source is untouched, a moved source is gone
- **AND** the modes apply alike directly under a room and any level deep

#### Scenario: Duplicate

- **WHEN** a same-titled file is copied or moved with `Duplicate`, or a file is copied into its own room with `Duplicate`
- **THEN** the destination holds two files: the existing one unchanged and the incoming one as `same (1).bin` with its own bytes; a moved file keeps its id under the new name (bug 84437: it used to keep the clashing title)

### Requirement: Checking a move or copy for name clashes

`checkMoveOrCopyBatchItems` (`GET api/2.0/files/fileops/move`, flat query parameters `fileIds`, `folderIds`,
`destFolderId`, `conflictResolveType`, `deleteAfter`, `content`) SHALL answer with the listed items — each
once, with its id, title and, for a file, `folderId` — that have a same-named entry at the destination,
whatever `conflictResolveType` says, and an empty list when none clashes. Create access to the destination is
required, the destination MUST be named, and nothing is changed.

#### Scenario: Clash reported regardless of the resolve mode

- **WHEN** My documents and a room both hold `Autotest CheckMove ConflictFile.docx` and the check is called with `Skip`, `Overwrite` or `Duplicate`
- **THEN** the answer holds exactly that file, with its id, title and source `folderId`; with no same-named file the answer is empty, and no listing changes and no operation is queued
- **AND** a file checked against the folder it already lives in is reported as a clash

#### Scenario: What is reported

- **WHEN** a clashing folder, a mixed selection of two files and two folders of which one of each clashes, names differing only by case or using non-Latin letters, a file listed twice, or an unknown id next to a clashing file are checked
- **THEN** the folder is reported by id and title; only the clashing file and folder come back, each once; the case and Unicode variants clash; the repeated file is reported once; the unknown id is passed over and the real clash still reported
- **AND** a clash with a file in a room's sub-folder is reported against the sub-folder and not against the room root

#### Scenario: Content of the listed folders

- **WHEN** a folder whose two files both clash with the destination is checked with `content:true`
- **THEN** the answer names both files, as the copy of the folder's content would settle them (a copy with `Skip` leaves the destination exactly as it was)
- **AND** with `content` true the listed folders are replaced by their content

#### Scenario: Destinations and batch shapes

- **WHEN** a file is checked against a Collaboration, Form filling, Public or Virtual Data Room, a folder against a Custom room, two files, a file and a folder, a folder with `content:true`, or an empty batch
- **THEN** every answer is an empty list

#### Scenario: Errors

- **WHEN** the check names an archived room, folder 999999999, no `destFolderId`, file id 999999999 as the only source, or the Rooms, Forms or AI Agents root as destination
- **THEN** the system answers 403, 404 (bug 81881), 400 (bug 81882), 200 with an empty list, and 403 for the roots, which the copy itself refuses too

#### Scenario: Caller's rights

- **WHEN** a User with Read access to the room names it as destination, or a Content creator of the room names the Owner's private file as source
- **THEN** the reader gets 403; the creator gets no entry back and the answer never carries the private title, while the Owner gets the file reported

#### Scenario: Route shared with the Move status

- **WHEN** the generated clients call `GET fileops/move`
- **THEN** they reach this check, which also shadows the status route of the Move operation type

### Requirement: Checking whether a destination accepts the files

`checkMoveOrCopyDestFolder` (`GET api/2.0/files/fileops/checkdestfolder`) SHALL report whether the destination
accepts the listed files: `result` is `AllAllowed`, `PartAllowed` or `NoneAllowed` and `files` names the
accepted ones. Every folder and room accepts every file except a Form filling room, which accepts only PDF
forms. An unwritable or unknown destination, a section root and a missing `destFolderId` SHALL be refused.

#### Scenario: Accepted files are listed

- **WHEN** one or two files from My documents are checked against a Custom room
- **THEN** `result` is AllAllowed and `files` holds exactly those titles; a Collaboration, Public or Virtual Data Room and a folder as source give AllAllowed too
- **AND** a file listed twice gives AllAllowed naming it once, and a same-named file at the destination is not a reason to refuse
- **AND** name clashes are not checked

#### Scenario: Form filling room

- **WHEN** a `.docx`, then a PDF form, then both together are checked against a Form filling room
- **THEN** the results are NoneAllowed, AllAllowed and PartAllowed naming the form only; folder ids and `conflictResolveType` sent along change nothing, and an empty selection answers 200 naming no file
- **AND** `deleteAfter` does not change the result either

#### Scenario: Errors

- **WHEN** the check names an archived room, a room the User or Guest cannot write to, a room the User only reads, folder 999999999, no `destFolderId`, or the Rooms, Forms or AI Agents root
- **THEN** the system answers 403 (bug 82103), 403 (bug 82104), 403, 404 (bug 82158), 400 (bug 82159) and 403 for the roots
- **AND** a file the caller cannot read is neither named nor refused: the answer carries no entry and no title of it

#### Scenario: Check then act

- **WHEN** a clash is reported and the copy is then run with `Duplicate`; a destination is reported AllAllowed and the file is then moved there; a Form filling room is reported NoneAllowed for a document
- **THEN** the room holds the existing file and a copy; the file is in the room and its old folder reports no clash for it; the room is still empty and no operation exists

### Requirement: Duplicating files and folders

`duplicateBatchItems` (`PUT api/2.0/files/fileops/duplicate`, v2.6) SHALL copy each listed file and folder
next to itself in the folder it lives in and answer with the caller's duplicate operations. The copy carries
the latest content, keeps the original name with the first free index suffix so nothing is overwritten, and
lives on its own. A batch the caller may not duplicate in full is refused as a whole (403). Duplicating a
whole room is specified in `room-types`.

#### Scenario: Index suffix

- **WHEN** `source_document.docx` in My documents, or a folder `folder`, is duplicated with `returnSingleOperation:true`
- **THEN** the finished operation lists `source_document (1).docx` (or a folder `folder (1)`) and the parent now holds both
- **AND** the suffix grows with every repetition: ` (1)`, then ` (2)` and so on, every copy holding the original's bytes; a taken `taken (1).bin` is skipped and never overwritten
- **AND** the `files` and `folders` of the finished operation name the copies

#### Scenario: Content and places

- **WHEN** a folder holding a file is duplicated, a file and a folder are duplicated together, two files at once, a file in a Custom room, a file in a nested subfolder, or a file titled `Autotest Dup Üñó Résumé.docx`
- **THEN** each duplicate appears next to its source — in My documents, the room or the subfolder — the duplicated folder holds the inner file, and the diacritics are kept

#### Scenario: Latest content, then independent

- **WHEN** a file with two versions is duplicated, the original then gets a new version and is deleted for good; or a folder tree is duplicated and the original tree deleted
- **THEN** the copy holds the bytes of the last version and keeps them; the original keeps its versions until its deletion; the copied tree is byte for byte the original and survives it
- **AND** three files in one call each get a copy with their own bytes, and a file listed twice gets two copies

#### Scenario: Refused and unknown

- **WHEN** a file in an archived room, a file in Trash, file id 999999999 or folder id 999999999 is duplicated, or an empty batch is sent
- **THEN** the system answers 403, 403, 404 (bug 82210), 404 and 200

#### Scenario: Batch refused as a whole

- **WHEN** a Content creator duplicates a room file together with the Owner's private file, or a User duplicates the Owner's private file alone
- **THEN** the system answers 403 and copies nothing; the room file alone, or the User's own file, is duplicated into its own folder

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
operation with the `immediately` semantics of a single deletion. A batch naming an id that resolves to nothing
(404) or an entry the caller may not delete (403) SHALL be refused as a whole before anything is queued;
anonymous callers get 401.

#### Scenario: To Trash and for good

- **WHEN** a file is deleted with `immediately:false`, another with `immediately:true`, and a folder holding a file with `immediately:true`
- **THEN** the first is in the Trash and gone from the source, the second is in neither, and the folder is gone from its parent
- **AND** `false` moves entries to the Trash and removes them from the source, `true` removes them for good, a folder with its files

#### Scenario: Mixed batch and Trash item

- **WHEN** two files and a folder are deleted together, and a file already in the Trash is deleted with `immediately:true`
- **THEN** all three are gone from the source and the trashed file is gone from the Trash
- **AND** a batch may mix files and folders, and an empty selection answers 200

#### Scenario: Batch refused as a whole

- **WHEN** a real file is listed with id 99999999, a User lists their own file with the Owner's file, or a User deletes a file lying in the Owner's Trash
- **THEN** the system answers 404, 403 and a client error, queues nothing, and every file keeps its place and its bytes; the file in the Owner's Trash can still be restored by the Owner
- **AND** a file listed twice goes to the Trash once, and a file restored from the Trash is deleted again by the same request — the call is not idempotent

### Requirement: The Trash remembers where an entry came from

An entry in the Trash SHALL keep its origin: the Trash listing (`getFolderByFolderId` of the Trash,
`getTrashFolder`) SHALL return every trashed file and folder when no filter is given, SHALL narrow the listing
with `folderType` to entries that originally sat in a section or room of the listed types, and with `roomId`
to entries from that room. A trashed file SHALL be restored by `moveBatchItems` out of the Trash, with its id,
versions and bytes.

#### Scenario: Filter by origin

- **WHEN** files from My documents, a Custom, a Public and a Virtual Data Room are trashed and the Trash is read with no filter, with `USER`, with `VirtualRooms`, with `PublicRoom` and with `USER` plus `CustomRoom`
- **THEN** the unfiltered listing holds all four, `USER` (My documents) only the My documents file, `VirtualRooms` (every room) only the room files, `PublicRoom` only the Public room file, and the pair the My documents and Custom room files
- **AND** `CustomRoom`, `PublicRoom`, `EditingRoom` or `VirtualDataRoom` select one room type and several types may be listed at once; the same holds for trashed folders

#### Scenario: Filter by room

- **WHEN** files and folders of one room are trashed and the Trash is read with that `roomId`
- **THEN** every trashed entry of the room is listed, a file that was not deleted is not, and the file of a trashed subfolder is listed too
- **AND** files inside a trashed folder are listed as trashed files themselves

#### Scenario: Restored by a move out of the Trash

- **WHEN** a file with two versions is moved to the Trash and then moved back to its folder
- **THEN** in the Trash its content cannot be downloaded; after the move it is gone from the Trash, listed in the folder under the same id, with both versions and the bytes of the second
- **AND** a file dropped by emptying the Trash cannot be moved back: the move is refused with a client error and the folder stays empty

### Requirement: Emptying the Trash

`emptyTrash` (`PUT api/2.0/files/fileops/emptytrash`) SHALL queue an operation that removes the caller's own
Trash for good and SHALL answer 200 also when the Trash is already empty. With `folderType` the sweep is
narrowed to the entries whose origin was a section or room of the listed types, leaving the rest in place.
`single:true` returns only the operation just started. Every authenticated account empties its own Trash
only; anonymous gets 401.

#### Scenario: Whole Trash

- **WHEN** a file, a folder, and a file deleted from a room are in the Trash and `emptyTrash` runs
- **THEN** the Trash holds none of them afterwards; a call on an empty Trash answers 200

#### Scenario: Gone for good

- **WHEN** a file and a folder holding a file are in the Trash and it is emptied, then emptied again
- **THEN** the Trash lists nothing, `getFileInfo` and `getFolderInfo` of all three answer 404, the content cannot be downloaded, and the second emptying changes nothing
- **AND** the files outside the Trash — in My documents and in a room — keep their versions and bytes

#### Scenario: One section only

- **WHEN** files from My documents, a Custom room and a Form filling room are in the Trash and `emptyTrash` runs with `folderType=USER`
- **THEN** the My documents file is gone and the room and form-room files are still there (bug 82588)
- **AND** with `folderType=VirtualRooms` the room entries go and the My documents entries stay

#### Scenario: Per account

- **WHEN** the Owner has a trashed file and a Guest empties their own Trash, or a User, Room admin, DocSpace admin or Guest with a trashed file of their own empties theirs
- **THEN** the Owner's file stays (a Guest's call never touches the Owner's Trash), and each account's own file is gone; an anonymous caller gets 401
- **AND** the Owner's trashed file, with two versions, is still restorable with both versions and its bytes after a User emptied their Trash

### Requirement: File versions and their history

Every file SHALL start at `version` 1 in `versionGroup` 1 with the comment `Created` and `fileStatus` None.
`getFileVersionInfo` (`GET api/2.0/files/file/{fileId}/history`) SHALL list the versions, one record each;
`getEditHistory` (`GET .../edit/history`) the editing revisions. `updateFile` with `lastVersion` N SHALL store
that version's content again as a new version; `getFileInfo` with `version` SHALL return that version.

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
- **AND** reading the history and opening a past version require the right to read the history: in a room its managers and content creators

#### Scenario: Editing member reads the versions

- **WHEN** a member invited with Editing access reads `getFileVersionInfo` of a room file with two versions
- **THEN** the system answers 403 (bug 84774: the list is answered today)

### Requirement: Changing and restoring versions

`changeVersionHistory` (`PUT .../history`, `version`, `continueVersion`) SHALL split the given version into a
revision group of its own with `continueVersion:false` and fold it back with `true`. `restoreFileVersion`
(`POST .../restoreversion`, `version` or `url`) SHALL store the chosen version's content on top of the history
as a new version and answer the editing revisions, grown by one. A file locked or edited by somebody else
SHALL be refused.

#### Scenario: Split and merge

- **WHEN** a file with two versions has version 2 split with `continueVersion:false` and then merged with `true`
- **THEN** after the split versions 1 and 2 carry different `versionGroup` values and after the merge the number of groups is not larger than before
- **AND** the call on file 999999999 answers 404 and in an archived room 403

#### Scenario: Restore

- **WHEN** version 1 of a two-version file is restored
- **THEN** the answer lists the revisions, one more than before, each with id, key, version, group, user and `created`
- **AND** a restore without `version` and `url` answers 400, of file 999999999 answers 404, in an archived room 403, and a GET to the route is not 200 and restores nothing (bug 78027)
- **AND** the restore route accepts POST only

#### Scenario: File held by somebody else

- **WHEN** a Content creator changes the version history of a file the Owner locked, or of a file open in the Owner's editing session
- **THEN** the number of versions is unchanged in both cases, and the call is refused with a client error — not 500 for the locked file (bug 84771) and not 200 for the edited one (bug 84772)

### Requirement: Version comments

`updateFileComment` (`PUT api/2.0/files/file/{fileId}/comment`, `version`, `comment`) SHALL set the comment of
that one version, leaving the others alone, and answer the stored text; `getFileInfo` and `getFileVersionInfo`
report it. An empty or null comment clears it. A comment longer than 255 characters, `version` 0, a negative
version or a version the file does not have SHALL be rejected (400); an unknown file answers 404; a file in
the Trash is refused.

#### Scenario: Set, overwrite and clear

- **WHEN** the comment of version 1 is set to `Initial comment`, then to `Updated comment`, then to `""` and to null
- **THEN** the answers are `Initial comment`, `Updated comment`, `""` and `""`, and `getFileInfo` reports the current text

#### Scenario: Validation

- **WHEN** a 2000-character comment, version 999, version 0, version -1 or file id 999999999 is sent
- **THEN** the system answers 400, 400, 400, 400 and 404
- **AND** the bugs are 82266 for the length, where the stored text stays, 82271 for the versions and 82268 for the file

#### Scenario: One version at a time

- **WHEN** a file holds three versions and version 2 gets a note, the same note again, then a replacement; and version 1 is cleared with `""` and version 2 with null
- **THEN** the version list shows the note on version 2 only, a repeated text leaves it as it is, and each clearing empties that version alone
- **AND** `quotes " ' backslash \ percent %`, a text with a line break and a tab, `  padded  ` and a 255-character text come back and are stored exactly; 256 and 1000 characters are refused and the 255-character text stays

#### Scenario: Who may comment, and a trashed file

- **WHEN** a Content creator comments on their own and on the Owner's file of the room, a Room manager on the Owner's file, and the Owner comments on a file lying in the Trash
- **THEN** the first three are stored; the trashed file is refused with a client error and shows its earlier comment once restored
- **AND** five concurrent writes to one version all answer with their own text and the stored one is one of them; concurrent writes to three versions each land on their version

### Requirement: Deleting versions

`deleteFileVersions` (`PUT api/2.0/files/fileops/deleteversion`, `fileId`, `versions`, v3.1) SHALL remove the
listed versions for good as a background operation, answering a list of operations; the file stays in its
folder with the remaining versions and their own bytes. The current version SHALL be refused with a client
error before anything is queued. The call needs the rights that deleting the file needs.

#### Scenario: One and several versions

- **WHEN** version 1 of a two-version file, or versions 1 and 2 of a three-version file, are deleted
- **THEN** the version info no longer lists them and still lists the last one, and the file is still in My documents
- **AND** the same works for a file in a Custom room and with `returnSingleOperation:true`

#### Scenario: The others keep their bytes

- **WHEN** version 2, or versions 1 and 3, of a four-version file are deleted
- **THEN** the remaining versions still download their own bytes, the current download is the fourth version, and the file is still in its folder; a neighbouring file keeps its versions and bytes

#### Scenario: Current version and the whole file

- **WHEN** the current version alone, or the current version with an old one, or the only version of a file is deleted
- **THEN** the system answers a client error, queues nothing and leaves every version with its bytes
- **AND** an empty list, or `versions` null, deletes the whole file: `getFileInfo` answers 404, a bystander file keeps its bytes
- **AND** the call deleting the whole file answers 200

#### Scenario: Ignored and refused

- **WHEN** version 999 of a one-version file is deleted, a version already deleted is listed again, the file is in an archived room or in the Trash, or the file id is 999999999
- **THEN** the version info is unchanged for the first two, and the others answer 403, 403 and 404
- **AND** an anonymous caller gets 401

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

- **WHEN** another account tries to delete or copy a file that is locked, to save new content into it through the editor's save route, to claim it for editing, or to rename it
- **THEN** the deletion, the copy and the save are refused (403) and the file keeps its title, version, bytes and lock; the claim and the rename are refused too, with a client error (bugs 84768 and 84842: reported as 500 today)

#### Scenario: Editing session blocks others

- **WHEN** a Content creator saves new content into a file open in the Owner's editing session
- **THEN** the save is refused (403) and the file keeps its version, bytes and editing flag (bug 84769: the content is replaced today)

### Requirement: Downloading a file

`getPresignedUri` (`GET api/2.0/files/file/{fileId}/presigneduri`) SHALL answer an `http(s)` address of the
current version that carries a `stream_auth` token and `fileid=<id>`; `getPresignedFileUri`
(`GET .../presigned`) SHALL answer an object with `url` and the file's `filetype`. Both need read access to
the file, answer 404 for an unknown id and 401 for an anonymous caller. The bytes are fetched by following
the address; the file handler also serves one stored version on request.

#### Scenario: Address minted

- **WHEN** the Owner asks for the presigned URI and the presigned file URI of a room file
- **THEN** the first contains `stream_auth=` and `fileid=<id>` and the second has an `http(s)` `url` and `filetype` `.docx`
- **AND** the `viewUrl` of a file serves the same content, and the file handler with a `version` serves that version's bytes

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

`startFileConversion` (`PUT api/2.0/files/file/{fileId}/checkconversion`, `outputType`, `password`, `version`,
`createNewIfExist`, `sync`) SHALL queue the conversion of a file whose format must be converted and answer the
entries to poll; `checkConversionStatus` (`GET .../checkconversion`, `start`) reports them and starts the
conversion with `start` true. Starting needs the right to convert (a manager or content creator), polling the
right to read.

#### Scenario: Conversion queued

- **WHEN** an `.rtf` is sent with `startConvert:true`, with `startConvert:false`, with an empty body, or read with `start` true
- **THEN** each answer holds one entry with `Operation` Convert, `progress` 0, no `error` and a `source` naming the file id and version 1 — the flag is set by the operation itself; before the start the read answers an empty list

#### Scenario: Nothing to convert

- **WHEN** a `.docx`, `.xlsx` or `.pptx`, a `.txt`, `.csv` or `.md`, a `.docx` with `outputType` `pdf`, or a `version` the file does not have is sent with `startConvert:true`
- **THEN** the answer is an empty list, the status read is empty, no file and no version is added, and the source is byte for byte what was uploaded
- **AND** the answer to the start is 200

#### Scenario: Conversion that fails

- **WHEN** an `.xml` is queued and polled
- **THEN** the entry ends at `progress` 100 with a non-empty `error` and no `result`, the folder and the source are unchanged, the finished entry is handed out once and a new attempt queues again

#### Scenario: Target formats

- **WHEN** a target format is chosen
- **THEN** it comes from the document server's format list, `outputType` being accepted with or without the dot; a format outside the list answers 400 instead of a silent conversion to the internal format
- **AND** the release notes record XML to `docx`/`xlsx` (v3.0), presentations to `txt` (v3.6), `.pages`/`.numbers`/`.key`/`.hwp`/`.hwpx`/`.odg`/`.md` and diagram formats (v3.2.1), and that `docx` to `docxf` is no longer offered (v2.6)

#### Scenario: Unknown file and access

- **WHEN** file 999999999, 0 or -1 is converted or polled, `abc` is sent as id, a User outside the room converts or polls a room file, or an anonymous caller does
- **THEN** the unknown ids answer 200 with an empty list (not 404), the non-numeric id a client error, the User 403 on both calls with nothing made, and the anonymous caller 401
- **AND** the Owner, a DocSpace admin, a Room manager and a Content creator on their own file start a conversion; a Read member and an Editing member poll the status (bug 81825)

### Requirement: Document service conversion and builder calls

`convertFile` (`POST api/2.0/docs/converter`, `fileId`, `outputtype`) SHALL hand one file to the document
server for conversion, and `runBuilderScript` (`POST api/2.0/docs/builder`, `script`, `outputs`) SHALL run a
document builder script that opens portal files by their quoted id. Both SHALL refuse an anonymous caller
(401) and a caller without the needed access to the file (403) and check the request before the document
server is reached; one builder script runs at a time per caller.

#### Scenario: Converter refusals

- **WHEN** a User without access to the file, an anonymous caller, or the Owner with file 999999999, with an empty `outputtype` or with `notarealformat` calls `convertFile`
- **THEN** the answers are 403, 401, 404, 400 and 400

#### Scenario: Builder refusals

- **WHEN** a User without edit access to the opened file, an anonymous caller, the Owner with an unquoted numeric file id in the script, or with an empty script calls `runBuilderScript`
- **THEN** the answers are 403, 401, 400 and 400
- **AND** a second script sent while the first is queued or running answers 403

### Requirement: Thumbnails

`createThumbnails` (`POST api/2.0/files/thumbnails`) SHALL queue thumbnail generation for the listed file ids
and echo those ids back; `folderIds` are accepted and ignored, a body without `fileIds` answers an empty
list, and an id that names no file is echoed like any other. The thumbnails themselves are built in the
background in the configured sizes and are read through `thumbnailStatus` / `thumbnailUrl` of the file. Any
signed-in account with read access may queue them.

#### Scenario: Ids echoed

- **WHEN** one file, two files, or a file together with a folder are sent
- **THEN** the answer holds exactly the file ids, the folder id is not in it; id 999999999 is echoed as well
- **AND** a `{}` body or a body naming folders only answers 200 with an empty list

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
`createdBy` and `updated`; reading the folder clears the marks. `markAsRead` (`PUT
api/2.0/files/fileops/markasread`) SHALL clear the caller's own marks of the listed files and of the files
inside the listed folders as a MarkAsRead operation.

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
- **AND** marking the My documents root as read clears the news there and leaves the news of an unrelated room alone (bug 83509)

#### Scenario: Only the caller's marks

- **WHEN** two Read members see two new files and one of them marks the first as read, twice; or a member marks two files with one id repeated; or a readable room file together with the Owner's private file or with id 99999999
- **THEN** that member's news keeps the second file only while the other member still sees both; both files are cleared; the readable file is cleared and the private or unknown id passed over — the private file keeps its bytes and the marked file its version and folder
- **AND** `markAsRead` is idempotent and modifies nothing else

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

### Requirement: Concrete kind of an entry in a mixed answer

Where an answer declares an entry only as "a file or a folder" — the items of a folder listing, the two
pre-check answers and the copy made by `copyFileAs` — the entry SHALL carry `entryKind` (`file`, `folder`,
`thirdPartyFile` or `thirdPartyFolder`) so that a client can read it as the concrete kind, the one that
carries the `id`. An answer declared as the concrete kind needs no discriminator.

#### Scenario: Listing and copy read as their kind

- **WHEN** a generated client reads a folder listing, the clash pre-check, the destination pre-check, or the answer of `copyFileAs`
- **THEN** every entry is built as the file or folder it is, with its `id` and, for a file, its `folderId`; the copy's `id` is the one listed at the destination
