# form-filling-workflow Specification

## Purpose

Defines the lifecycle of a PDF form on the server, independent of who may do what: what the server treats as a
form, the filling states a form passes through in a Form filling room (FFR) and the actions that move it between
them, the result folders and files a filling produces, the results spreadsheet and the external database export,
the role-based filling of a Virtual Data Room (VDR), the fill-result lookup of an anonymous visitor, the webhook
events of the workflow and the Forms section that lists form rooms and form templates. Role permissions are NOT
restated here: who may start, stop, fill, collect or configure is in `form-filling-room-roles` and
`vdr-room-roles`; link rules are in `shared-links`; quick-share of a form by user type is in
`user-type-permissions`.

Sources: the integration tests under `products/ASC.Files/Tests/Tests/01_Files/FormFilling`, `01_Files/Forms`,
`02_Folders/FormFilter`, `03_Rooms/FormFilling`, `03_Rooms/Links` and `03_Rooms/Templates`, the published
OpenAPI document (`sdk/docspace-api-spec/docspace-backend.yaml`), the product release notes (v3.1, v3.5, v3.7,
v3.7.1, v4.0) and the product code (`FileStorageService`, `FileSecurity`, `EntryManager`,
`DocumentServiceHelper`, `EditorController`, `FileMoveCopyOperation`, `FormFillingReportCreator`). Where the
release notes and the server disagree, this specification follows the server; the behaviours listed as
suspected defects D1, D5, D6 and D13 in `openspec/deviations.md` are deliberately not written here as
requirements. The integration host runs without a document server, so nothing that needs a real editor
submission (a filled copy produced by the editors, a results spreadsheet, a database export, a VDR step
submission) is covered by tests; those requirements rest on the code and the API document, as recorded in
`verification.md`.

## Requirements

### Requirement: PDF form identification

The server SHALL treat every file whose name ends in `.pdf` as a fillable PDF (`isForm` is true for a PDF and
null for any other file), and SHALL distinguish an ONLYOFFICE PDF form from an ordinary PDF document by content:
a PDF is classified as a form (`filterType` `PdfForm`, release notes v3.1 "PDF Form") when the beginning of the
file carries the editors' form signature (the `DSE3` header with the form flag, or the configured `oform`
signature), otherwise as a PDF document (`filterType` `Pdf`). `GET /api/2.0/files/file/{fileId}/isformpdf`
(`isFormPDF`) SHALL answer that classification as a single boolean, SHALL require read access to the file, and
SHALL say nothing about the filling state.

#### Scenario: Uploaded ONLYOFFICE form

- **WHEN** a PDF carrying the ONLYOFFICE form signature is uploaded and `isformpdf` is requested for it
- **THEN** the answer is `true`

#### Scenario: Blank PDF created on the portal is a form

- **WHEN** a PDF is created through `POST /api/2.0/files/{folderId}/file` from the built-in blank template and `isformpdf` is requested
- **THEN** the answer is `true`, because the built-in blank PDF template is itself an ONLYOFFICE form

#### Scenario: Ordinary PDF, DOCX and DOCXF are not forms

- **WHEN** `isformpdf` is requested for a PDF without the form signature, for a `.docx` file or for a `.docxf` template
- **THEN** the answer is `false`

#### Scenario: Access to the check

- **WHEN** `isformpdf` is requested by a caller without read access to the file, by an anonymous caller, or for a file that does not exist
- **THEN** the answer is 403, 401 and 404 respectively
- **AND** a room member with read-only rights receives the answer

#### Scenario: PDF form and PDF document filters

- **WHEN** a listing such as Shared with me is requested with `filterType=PdfForm`
- **THEN** it contains the ONLYOFFICE forms and neither the ordinary PDFs nor the other documents
- **AND** with `filterType=Pdf` it contains the ordinary PDFs and neither the forms nor the other documents

### Requirement: Filling state of a form in a Form filling room

A PDF form in a Form filling room SHALL be in one of two states, stored in the form's filling properties as
`startFilling` and reported on the file as `startFilling`: not started (editable, hidden from members holding
only the form-filling right) or started (open for filling, visible to every member). The state SHALL be changed
only through the actions below; `formFillingStatus` on the file SHALL stay `None` outside a Virtual Data Room.

#### Scenario: Not-started form is hidden from form fillers

- **WHEN** a member whose room right is form filling lists a Form filling room holding a form whose filling has not been started
- **THEN** that form is absent from the listing and answered 404 by id
- **AND** it appears once filling has been started

#### Scenario: State is read from the file

- **WHEN** a form in a Form filling room is read through `GET /api/2.0/files/file/{fileId}`
- **THEN** `startFilling` is `true` once filling was started and `false` or null before
- **AND** `formFillingStatus` is `None`

### Requirement: Form filling actions

`PUT /api/2.0/files/file/{fileId}/manageformfilling` (`manageFormFilling`, body `formId` + `action` of
`FormFillingManageAction`: `0` Stop, `1` Resume, `2` Start, `3` Edit) SHALL drive the filling of a PDF form that
lies in a room. The endpoint SHALL answer 200 with no body. The file SHALL be a PDF lying in a room, otherwise the
request is refused (403); an unknown action SHALL be refused (403). In a Form filling room:

- `Start` SHALL set `startFilling` true, record the caller as the account that started the filling
  (`startedByUserId`), write the "form started to fill" history entry and reveal the form to the members
  holding the form-filling right. When the form's version differs from the version it was last started with,
  Start SHALL drop the links to the filling drafts of the previous round (so each member gets a fresh draft)
  and mark the version as changed.
- `Stop` SHALL set `startFilling` false, hide the form from the members holding the form-filling right, write
  the "form stopped" history entry and emit the `form.stopped` webhook.
- `Edit` SHALL leave filling mode the same way Stop does (`startFilling` false, the form hidden from form
  fillers, history entry and `form.stopped` webhook when it was started) and SHALL remember the version the
  form is edited from, so that the next Start can detect a changed form.
- `Resume` SHALL clear the interruption record (`fillingStopedDate`, the interrupting role); it exists for the
  Virtual Data Room flow and changes nothing else in a Form filling room.

Release notes v3.7: "to start work, the form must be put into filling mode (Start filling); in filling mode the
Edit item remains, and choosing it takes the form out of filling mode and hides it from Form fillers" — the
server implements both through this endpoint.

#### Scenario: Start and stop

- **WHEN** the room owner starts filling of a form created in a Form filling room and then stops it
- **THEN** both requests answer 200 and the form returns to the not-started state

#### Scenario: Form outside a room or not a PDF

- **WHEN** `manageformfilling` is called for a `.docx` file, for a PDF in My documents, or for a file that does not exist
- **THEN** the request is refused (403)

#### Scenario: Resume of a form that was never started

- **WHEN** `Resume` (1) is sent for a PDF in a room that has no filling properties yet
- **THEN** the server fails with 500, because there is no record to clear

#### Scenario: Edit through the editor

- **WHEN** a form of a Form filling room is opened with `GET /api/2.0/files/file/{fileId}/openedit?edit=true`
- **THEN** the server applies the `Edit` action before answering, so the form leaves filling mode

### Requirement: Start filling endpoint

`PUT /api/2.0/files/file/{fileId}/startfilling` (`startFillingFile`) SHALL mark a PDF lying directly in a Form
filling room as started: it stores the room id, the form title, the caller as `startedByUserId`, the file's own
id as `originalFormId` and `startFilling` true, and answers with the file. For any other file it SHALL change
nothing and answer the file unchanged (200). A caller with no access to the room SHALL be refused (403) unless
they can manage the room; a member holding only the form-filling right SHALL be refused (403).

#### Scenario: Form in a Form filling room

- **WHEN** the owner calls `startfilling` on a PDF in a Form filling room
- **THEN** the answer is 200 with the form (`id` unchanged) and the filling properties stored

#### Scenario: Not a form-room PDF

- **WHEN** `startfilling` is called on a `.docx` in My documents or on a PDF in My documents
- **THEN** the answer is 200 with the file unchanged and no filling properties are written

#### Scenario: Missing file and callers without access

- **WHEN** `startfilling` names a file that does not exist, or is called anonymously, or by a User or Guest who is not a member of the room
- **THEN** the answer is 404, 401 and 403 respectively
- **AND** a DocSpace administrator who is not a member is allowed (200)

### Requirement: Filling state follows a move or copy

A PDF moved or copied from outside into a Form filling room SHALL arrive with filling started (`startFilling`
true, `originalFormId` the new file). A PDF moved out of a Form filling room or a Virtual Data Room to another
room or section SHALL lose its filling properties; leaving a Form filling room also drops the links to its
filling drafts, leaving a Virtual Data Room also deletes its role assignment. A copy or move with `toFillOut` SHALL mark the
new PDF as copied to fill out (`copyToFillOut`), the flag the client reads to offer the role assignment
(release notes v3.1: "after copying a PDF form through Share into a VDR, a dialog offers to assign roles").
Only PDFs enter a Form filling room; the file-type rule and the deviation for ordinary PDFs are in
`form-filling-room-roles`.

#### Scenario: Form moved from My documents into a Form filling room

- **WHEN** a PDF is moved from My documents into a Form filling room through `fileops/move`
- **THEN** the file is started for filling on arrival and visible to the members with the form-filling right

#### Scenario: Form moved out of the room

- **WHEN** a started form is moved from a Form filling room to My documents or to another room
- **THEN** the moved file has no filling properties and is no longer linked to any draft

### Requirement: Opening a form for filling in a Form filling room

When a PDF of a Form filling room is opened through `GET /api/2.0/files/file/{fileId}/openedit`, the server
SHALL decide the editor mode from the filling state and the caller's rights: the configuration carries
`startFillingMode` `StartFillingFormRoom` (3) and `startFilling` telling whether this caller may start the
filling from the editor. A not-started form SHALL open for editing only for a caller who may start its filling
and SHALL be refused (403) for anyone else. A started form SHALL open the caller's own draft (see the next
requirement) in the embedded editor with a `fillingSessionId` of `{draftId}_{userId}`; while the original is
being edited and no draft exists yet, the form opens read-only. An anonymous visitor SHALL open only a started
form, as a submit-only embedded session whose `fillingSessionId` starts with the anonymous-session prefix, and
SHALL be refused (403) for a not-started form. A form in the per-form "In process" folder opens for filling
with `fillingSessionId` `{fileId}_{userId}`; a form in the "Complete" folder opens read-only.

#### Scenario: Not-started form opened by a member who cannot start it

- **WHEN** a member who may not start the filling opens a form whose filling has not been started
- **THEN** the request is refused (403)

#### Scenario: Started form opened by a member who may fill it

- **WHEN** a member with the form-filling right opens a started form
- **THEN** the configuration is for the caller's draft in the embedded editor with `fillingSessionId` set

#### Scenario: Anonymous visitor on a not-started form

- **WHEN** a visitor through the room's external link opens a form whose filling has not been started
- **THEN** the request is refused (403)

### Requirement: Filling draft

`POST /api/2.0/files/masterform/{fileId}/checkfillformdraft` (`checkFillFormDraft`, body `version`,
`requestView`) SHALL answer the editor URL the caller has to open for a form in a Form filling room. For a
caller who may edit the form, for a form whose filling has not been started and for a view request, the URL
SHALL point at the form itself. For a caller who may fill but not edit a started form, the server SHALL create
or reuse the caller's personal draft: a copy of the form titled `{First name} {Last name} - {form title}` in the
per-form subfolder of the room's "In process" folder, linked to the original, with the new-item mark removed
and the editing session of the original closed; the URL points at the draft and, when the draft was just
created, carries a `#message/...` fragment naming the folder. When a draft exists but is no longer valid (its
source cannot be filled by the caller any more, or the original form's version changed since the draft was
made), the URL SHALL point at the form with a `#message/...` discard notice. The endpoint accepts an external
link key instead of a session; a caller with neither SHALL be refused (401).

#### Scenario: Owner asks for the draft URL

- **WHEN** the owner of a Form filling room calls `checkfillformdraft` for a started form, with or without `requestView: true`
- **THEN** the answer is 200 with a `/doceditor` URL carrying `fileid={formId}`

#### Scenario: Callers without access

- **WHEN** `checkfillformdraft` is called anonymously, by a User or Guest who is not a member of the room, or for a file that does not exist
- **THEN** the answer is 401, 403 and 404 respectively

#### Scenario: Obsolete draft after the form changed

- **WHEN** a filling draft was made from version N of a form, the form was edited and restarted, and the draft owner submits it
- **THEN** the submission is refused with the "draft obsolete" error

### Requirement: Result folders of a Form filling room

The server SHALL keep the results of a Form filling room in two system folders created in the room on the first
draft or submission: "In process" (`folderType` `InProcessFormFolder`, 26) and "Complete"
(`folderType` `ReadyFormFolder`, 25). For each form the server SHALL create a subfolder named after the form in
each of them: a `FormFillingFolderInProgress` (28) folder under "In process" holding the drafts, and a
`FormFillingFolderDone` (27) folder under "Complete" holding the submitted copies; their ids are stored on the
form as `toFolderId` and `resultsFolderId`. These four folder types SHALL NOT be renamed, moved, duplicated,
given access or created in, the two room-level folders SHALL NOT be deleted, no link SHALL be copied for them,
and the system account SHALL NOT read them. Files inside the per-form folders SHALL NOT be renamed, locked,
moved, duplicated, have their history edited, be submitted to the form gallery or embedded; a draft SHALL be
fillable only by its creator, and a submitted copy SHALL NOT be filled or edited at all.

#### Scenario: Folders appear with the first draft

- **WHEN** the first member opens a started form of a room for filling
- **THEN** the room gains "In process" and "Complete", each with a subfolder named after the form
- **AND** the member's draft lies in the "In process" subfolder

#### Scenario: Submitted copy is read-only

- **WHEN** any member, including the room owner, attempts to edit or fill a file in a "Complete" subfolder
- **THEN** the action is not offered (`security.Edit` and `security.FillForms` false) and the editor opens it read-only

#### Scenario: System folder cannot be renamed or deleted

- **WHEN** a member attempts to rename the "In process" folder or to delete it
- **THEN** the request is refused (403)

### Requirement: Submission of a filled form

When the editors submit a filled copy of a started form (a forced save with the user-submit initiator, tracked
by the document service), the server SHALL store the copy in the form's "Complete" subfolder as
`{N} - {form title} ({dd-MM-yyyy H-mm}).pdf`, where `N` is the submission number kept on the original form
(`resultFormNumber`, incremented under a per-form lock), classified as a PDF form and marked as a completed
form. The copy SHALL carry filling properties pointing at the original form, the room and the result folder;
the submitter's draft SHALL be unlinked and removed from the submitter's Recent; the submission SHALL be marked
new for every member except those holding only the form-filling right; the "form submitted" notification SHALL
be sent and the `form.submit` webhook emitted. An anonymous submission SHALL be stored the same way under the
original form's creator, with the room's links re-applied to the copy. When the form's "Complete" subfolder no
longer exists it SHALL be recreated before the copy is stored.

`PUT /api/2.0/files/file/{fileId}/saveediting` with a multipart file body (`saveEditingFileFromForm`) SHALL
replace the content of the addressed file with the posted binary as a new version — it is the save of the file
itself, not a submission, and SHALL NOT re-classify the file.

#### Scenario: Form binary saved through saveediting

- **WHEN** the owner posts a PDF binary of 512 bytes to `saveediting` of a form in a Form filling room, with or without `forcesave`
- **THEN** the answer is the same file at version 2, `pureContentLength` 512, comment "Edited", `isForm` still true

#### Scenario: saveediting access

- **WHEN** `saveediting` with a form binary is called anonymously, by a User or Guest who is not a member of the room, or for a file that does not exist
- **THEN** the answer is 401, 403 and 404 respectively, and the 404 body does not leak an exception message

#### Scenario: Submission numbering

- **WHEN** three copies of the same form are submitted
- **THEN** they are stored as `1 - …`, `2 - …` and `3 - …` in the form's "Complete" subfolder

### Requirement: Results spreadsheet

A Form filling room SHALL carry the setting `saveFormAsXLSX` (release notes v3.7 "Update XLSX data"). With it on,
the server SHALL keep one results file `{form title}.xlsx` in the form's "Complete" subfolder (`resultsFileID`
on the original form), created on the first submission or on the first report request and rewritten in place on
every later submission, so that its id and links stay. With it off, submissions SHALL be kept only as the filled
copies. `POST /api/2.0/files/file/{fileId}/xlsx` (`generateXlsx`) SHALL queue a rebuild of that file: it accepts
the id of the original form or the id of an XLSX or CSV results file in a "Complete" subfolder, requires that
the form is started, is the original form (`originalFormId` equals its own id) and still lies in the Form
filling room that started it, and answers the queued task, the original form and `isNewFile`. A CSV results
file SHALL be replaced by a new XLSX. `GET /api/2.0/files/file/{fileId}/xlsx` SHALL report the caller's own
run for that form (completion, progress, error, produced file) and nothing when there is none.
`POST /api/2.0/files/folder/{folderId}/xlsx` (`generateXlsxByFolder`) SHALL do the same starting from a
"Complete" subfolder (404 when the folder does not exist or holds nothing to report, 403 for any other folder
kind). `GET /api/2.0/files/file/{fileId}/submissions` (`getFormSubmissions`) SHALL return the field metadata and
one record per submitted copy of the version currently being filled, for a started original form, to anyone who
may read it.

#### Scenario: Report for a form that is not started

- **WHEN** `POST …/xlsx` is called for a PDF whose filling has not been started, for a copy of a form, or for a form whose room is not a Form filling room
- **THEN** the request is refused (403)

#### Scenario: Report addressed through the results file

- **WHEN** `POST …/xlsx` is called with the id of the `.xlsx` results file in a "Complete" subfolder
- **THEN** the server resolves the original form behind it and queues the rebuild for that form

#### Scenario: Status before any run

- **WHEN** `GET …/xlsx` is called for a form the caller never generated a report for
- **THEN** the answer is 200 with an empty body

### Requirement: External database export

A Form filling room SHALL carry the setting `sendFormToExternalDB` (release notes v3.7 "Connect Database"). With
it on and an external database configured for the portal, every submission SHALL be pushed to a table named
after the original form and its version, keyed by the submission number, and the table name SHALL be stored on
the form (`externalDbTableName`); with the setting off or no database configured, nothing SHALL be sent.
`POST /api/2.0/files/rooms/{id}/externaldbsync` (`startExternalDbSync`) SHALL queue a job that re-exports the
collected data of every original form of the room and answer the job record; the room SHALL be a Form filling
room the caller may edit (403 otherwise, 404 for an unknown room); when no external database is configured the
call SHALL fail with 400 and queue nothing. One job is kept per room: starting again while it runs SHALL return
the same record, a finished job SHALL be replaced. `GET /api/2.0/files/rooms/{id}/externaldbsync`
(`getExternalDbSyncStatus`) SHALL return that record — `id`, `percentage`, `isCompleted`, `status`, `error` and,
once finished, `forms` with one `id`/`title`/`success`/`error` entry per original form — or an empty body when
the room has no job. Generating the spreadsheet of a form SHALL also re-sync that form to the database when the
setting is on.

#### Scenario: Sync requested for a room that is not a Form filling room

- **WHEN** `POST …/externaldbsync` is called for a Custom room
- **THEN** the request is refused (403)

#### Scenario: Status without a job

- **WHEN** `GET …/externaldbsync` is called for a Form filling room no export was ever started for
- **THEN** the answer is 200 with an empty body

### Requirement: Form fields filter of a results folder

`GET /api/2.0/files/{folderId}/formfilter` (`getFolder`) SHALL list the fields (`key`, `type`) carried by the
submitted copies in a "Complete" subfolder, read from the search index, so that a client can pass one back as
`formsItemKey`/`formsItemType` of `GET /api/2.0/files/{folderId}` to keep only the copies whose field holds a
value. For any other folder, for a folder that does not exist, for a deleted folder and while nothing has been
submitted yet, the answer SHALL be an empty list, never an error; the operation needs no authorization.

#### Scenario: Folder without submissions

- **WHEN** `formfilter` is requested for an empty folder, for a folder of ordinary documents, for Recent, for Favorites, for a deleted folder or for an id that does not exist
- **THEN** the answer is 200 with an empty array

#### Scenario: Readable folder of another member

- **WHEN** a member with read-only rights, or the room owner for a folder created by another member, requests `formfilter`
- **THEN** the answer is 200

### Requirement: Fill result of a finished session

`GET /api/2.0/files/file/fillresult?fillingSessionId=…` (`getFillResult`) SHALL answer, without sign-in, the
outcome of a completed filling session: the submitted copy (`completedForm`), the original form, the submission
number (`formNumber`), the manager (the original form's creator), the room id and `isRoomMember`. The session
id is the one the editor reported at submission; the server SHALL remember the mapping from it to the copy only
for a limited time in the distributed cache. A missing parameter SHALL be refused (400); a session that was
never completed, one already forgotten and a value of the wrong shape SHALL all be answered 404 "The record
could not be found". `roomId` SHALL be omitted when the caller came through a link to a file rather than to the
room.

#### Scenario: Session id missing or empty

- **WHEN** `fillresult` is called without `fillingSessionId` or with an empty value
- **THEN** the answer is 400

#### Scenario: Unknown session

- **WHEN** `fillresult` is called with a random UUID, with `invalid-uuid-format`, with a 100-character value, or anonymously with an unknown UUID
- **THEN** the answer is 404 with "The record could not be found"

### Requirement: Role-based filling in a Virtual Data Room

In a Virtual Data Room a PDF form SHALL be filled in steps by assigned participants (release notes v3.1).
`POST /api/2.0/files/file/{fileId}/formrolemapping` (`saveFormRoleMapping`, body `formId` + `roles` of
`roleName`, `roleColor`, `userId`, `sequence`, `roomId`) SHALL replace the whole role set of the form, set
`startFilling` true, record the caller as `startedByUserId`, write the "form started to fill" history entry,
notify every assigned account and reveal the form to the members holding only the form-filling right; an empty
role list SHALL reset the filling and drop the filling properties. Roles with the same `sequence` are filled in
parallel; a higher `sequence` waits for every lower one. The file SHALL be a PDF in a room (403 otherwise).

`GET /api/2.0/files/file/{fileId}/formroles` (`getAllFormRoles`) SHALL return the roles ordered by sequence,
each with `roleName`, `roleColor`, `user`, `sequence`, `submitted`, `stopedBy`, `history` (0 opened, 1
submitted, 2 stopped) and `roleStatus` of `FormFillingStatus` (`0` None, `1` Draft, `2` YouTurn, `3`
InProgress, `4` Complete, `5` Stoped): with the current step being the lowest sequence that still has an
unsubmitted role, roles of earlier steps are `Complete`, roles of later steps are `Draft`, the role of the
current step is `YouTurn` until its holder opens the form and `InProgress` afterwards, every role is `Complete`
when no step is left, and the role the filling was stopped at is `Stoped`. A form whose filling was never
configured SHALL answer an empty list. The file SHALL be a PDF or the completed copy of one (403 otherwise),
readable by the caller (403 for a non-member and for a guest outside the room, 401 anonymously, 404 for an
unknown file or id 0).

When a participant submits their step the server SHALL mark their role submitted with the time, and when no
step is left SHALL mark the form completed (`isCompletedForm`, no longer editable by anyone), close the editing
session, emit the `form.filled.out` webhook, write the "form completely filled" history entry and notify every
participant; otherwise it SHALL notify the accounts of the next step ("your turn"). `Stop` on a VDR form SHALL
record the stop time and the interrupting account and role, notify the other participants, and refuse further
step submissions until `Resume` clears the record. Removing a room member who still holds an unfinished role
through `PUT /api/2.0/files/rooms/{id}/share` SHALL be answered with the `FormRoleBlockingDeletion` error of
the membership change instead of being applied, unless the request carries `force`.

#### Scenario: Roles of an unconfigured form

- **WHEN** `formroles` is requested for an ONLYOFFICE form uploaded to a Custom room, to My documents, or to a room that was archived afterwards
- **THEN** the answer is 200 with an empty array

#### Scenario: Roles of a non-PDF

- **WHEN** `formroles` is requested for a `.docx` file or a `.docxf` template
- **THEN** the request is refused (403)

#### Scenario: Roles access

- **WHEN** `formroles` is requested by a room member with read-only, content-creator or room-manager rights, or by a DocSpace administrator
- **THEN** the answer is 200
- **AND** a User or Guest who is not a member gets 403, an anonymous caller 401, and an unknown file or id 0 gets 404

#### Scenario: Statuses along the queue

- **WHEN** a form has roles A (sequence 1, submitted), B (sequence 2, opened, not submitted) and C (sequence 3)
- **THEN** `formroles` reports A `Complete`, B `InProgress` and C `Draft`
- **AND** B is `YouTurn` instead while its holder has not opened the form

#### Scenario: Reset with an empty role list

- **WHEN** `formrolemapping` is sent with an empty `roles` array for a configured form
- **THEN** the roles are removed and the form returns to the not-configured state

### Requirement: Sharing a form to fill out outside a form room

For a PDF opened from My documents, a Public room or any room other than a Form filling room or a Virtual Data
Room, the editor configuration SHALL offer `startFillingMode` `ShareToFillOut` (1) when the caller may copy the
document and is signed in, and `startFilling` SHALL be true only for the form's own creator in My documents
(release notes v3.1: a PDF form is shared from Documents directly, the link is created with the fill right). The
quick-share link with the form-filling right is available in My documents, Form filling rooms and Virtual Data
Rooms only; in Public, Custom and Collaboration rooms such a link SHALL be refused (release notes v3.7.1) — the
rule and its scenarios are in `user-type-permissions`.

#### Scenario: Own form in My documents

- **WHEN** the creator opens their PDF form from My documents through `openedit`
- **THEN** the configuration carries `startFillingMode` `ShareToFillOut` and `startFilling` true

#### Scenario: Form in a Custom room

- **WHEN** a member opens a PDF form of a Custom room through `openedit`
- **THEN** the configuration carries `startFillingMode` `ShareToFillOut` and `startFilling` false

### Requirement: Form filling room link and anonymous filling

A Form filling room SHALL be created with one primary external link and SHALL accept no additional external
link; the primary link is what an anonymous visitor follows to fill a form, and visiting it with an account
grants that account access to the room (`external` true on the room) and lists the room in the visitor's
Forms section. The access rights the link may carry are in `shared-links`; invitation links (one per room,
Form Filler or Content creator access) are in `invitations`.

#### Scenario: Auto-created primary link

- **WHEN** a Form filling room is created and its links are read
- **THEN** exactly one external link exists, marked primary

#### Scenario: Additional external link refused

- **WHEN** a second external link is requested for a Form filling room
- **THEN** the request is refused (403, "The maximum number of links may not exceed 0") and only the primary link exists

#### Scenario: Anonymous visitor resolves the link

- **WHEN** a visitor with no account resolves the room's primary link through `GET /api/2.0/files/share/{token}?folderId={roomId}`
- **THEN** the answer is 200 with `isRoom` true and `entityId` the room id

#### Scenario: Account visits the link

- **WHEN** a portal member who is not a room member resolves the link and then reads the room
- **THEN** `GET /api/2.0/files/rooms/{id}` answers 200 with `external` true and the room is listed under `searchArea=Forms`

### Requirement: Webhook events of the workflow

The server SHALL offer three webhook triggers for forms (release notes v3.7), available to DocSpace admins, Room
admins and Users: `form.submit` (`FormSubmit`), emitted when the editors deliver a user-submit forced save of a
file that has an original form behind it, with the submitted-form payload addressed by the original form;
`form.filled.out` (`FormFilledOut`), emitted when the last step of a Virtual Data Room form is submitted and the
form becomes complete; `form.stopped` (`FormStopped`), emitted on the `Stop` action and on the `Edit` action of
a started form in a Form filling room.

#### Scenario: Stop emits form.stopped

- **WHEN** filling of a started form is stopped, or a started form of a Form filling room is put back into editing
- **THEN** a `form.stopped` webhook is published for the form

#### Scenario: Edit of a not-started form emits nothing

- **WHEN** the `Edit` action is applied to a form whose filling is not started
- **THEN** no webhook is published

### Requirement: Forms section and form templates

Form filling rooms SHALL live under the Rooms tree but be listed only in the Forms section (release notes v4.0):
`GET /api/2.0/files/@forms` (`getFormsFolder`), the root folder of `rootFolderType` `Forms` (36) in
`GET /api/2.0/files/@root`, and `GET /api/2.0/files/rooms?searchArea=Forms`; `searchArea=Active` and browsing
the Rooms root SHALL leave them out, even when `type=FillingFormsRoom` is requested. Room templates SHALL be
split the same way: a template made from a Form filling room appears under `searchArea=FormTemplates` and not
under `searchArea=Templates` nor when browsing the templates root, and a room created from it is a Form filling
room listed in the Forms section. The used space of a Form filling room SHALL be counted in the Forms section's
figure (`formsUsedSpace`), not in the Rooms figure, moving to the Archive figure while the room is archived and
released when the room is deleted. The split is by the room's folder type alone, so it applies to rooms and
templates created before the section existed.

#### Scenario: Form room in the Forms section only

- **WHEN** a Form filling room, a Custom room and a Public room exist
- **THEN** `@forms`, the Forms root folder and `rooms?searchArea=Forms` list the form room and not the others
- **AND** `rooms?searchArea=Active` and the Rooms root folder list the others and not the form room

#### Scenario: Forms root is consistent

- **WHEN** the root folders are listed
- **THEN** one of them has `rootFolderType` `Forms`, and its id equals `current.id` of `@forms`

#### Scenario: Form template section

- **WHEN** a template is created from a Form filling room and another from a Custom room
- **THEN** `searchArea=FormTemplates` lists the first only and `searchArea=Templates` lists the second only
- **AND** a room created from the form template has `roomType` `FillingFormsRoom` and is listed under `searchArea=Forms`

#### Scenario: Used space

- **WHEN** a file is uploaded to a Form filling room, the room is archived, restored and deleted
- **THEN** `formsUsedSpace` grows by the file size, drops to the previous value while archived (the size moving to `archiveUsedSpace`), returns on restore and drops on delete, while `roomsUsedSpace` never changes
