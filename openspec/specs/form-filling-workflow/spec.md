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
v3.7.1, v4.0) and the product code where the tests stop (the file service, the file security check, the entry
manager, the editor configuration builder, the editor controller, the move/copy operation and the results
report builder; the files are named per row in `verification.md`). Where the release notes and the server
disagree, this specification follows the server; the behaviours listed as suspected defects D1, D5, D6 and D13
in `openspec/deviations.md` are deliberately not written here as requirements. The integration host runs
without a document server, so nothing that needs a real editor submission (a filled copy produced by the
editors, a results spreadsheet, a database export, a VDR step submission) is covered by tests; those
requirements rest on the code and the API document, as recorded in `verification.md`.

## Requirements

### Requirement: PDF form identification

The server SHALL treat every `.pdf` file as a fillable PDF (`isForm` true for a PDF, null otherwise), and
SHALL distinguish an ONLYOFFICE PDF form (`filterType` `PdfForm`) from an ordinary PDF document (`filterType`
`Pdf`) by content: whether the beginning of the file carries the editors' form signature.
`GET /api/2.0/files/file/{fileId}/isformpdf` (`isFormPDF`) SHALL answer that classification as a single
boolean, needs read access to the file, and says nothing about the filling state.

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

- **WHEN** a listing such as Shared with me is requested with `filterType=PdfForm` (the "PDF Form" type of release notes v3.1)
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

`PUT /api/2.0/files/file/{fileId}/manageformfilling` (`manageFormFilling`, body `formId` + `action` of Stop,
Resume, Start or Edit) SHALL drive the filling of a PDF form that lies in a room and SHALL answer 200 with no
body. Any other file (403) and an unknown action (403) SHALL be refused. In a Form filling room the actions
SHALL have the effects of the scenarios below; release notes v3.7 describe Start filling and Edit as the
client's way into and out of filling mode.

#### Scenario: Start action

- **WHEN** `Start` is applied to a form of a Form filling room
- **THEN** `startFilling` becomes true, the caller is recorded as the account that started the filling, the "form started to fill" history entry is written and the form is revealed to the members holding the form-filling right
- **AND** when the form's version differs from the version it was last started with, the links to the filling drafts of the previous round are dropped (so each member gets a fresh draft) and the version is marked as changed

#### Scenario: Stop action

- **WHEN** `Stop` is applied to a started form
- **THEN** `startFilling` becomes false, the form is hidden from the members holding the form-filling right, the "form stopped" history entry is written and the `form.stopped` webhook is emitted

#### Scenario: Edit action

- **WHEN** `Edit` is applied to a form
- **THEN** the form leaves filling mode the way Stop does (`startFilling` false, hidden from form fillers, history entry and `form.stopped` webhook when it was started)
- **AND** the server remembers the version the form is edited from, so that the next Start can detect a changed form

#### Scenario: Resume action

- **WHEN** `Resume` is applied to a form
- **THEN** the interruption record (the stop time and the interrupting role) is cleared; the action exists for the Virtual Data Room flow and changes nothing else in a Form filling room

#### Scenario: Start and stop

- **WHEN** the room owner starts filling of a form created in a Form filling room and then stops it
- **THEN** both requests answer 200 and the form returns to the not-started state

#### Scenario: Form outside a room or not a PDF

- **WHEN** `manageformfilling` is called for a `.docx` file, for a PDF in My documents, or for a file that does not exist
- **THEN** the request is refused (403)

#### Scenario: Resume of a form that was never started

- **WHEN** `Resume` is sent for a PDF in a room that has no filling properties yet
- **THEN** the server fails with 500, because there is no record to clear

#### Scenario: Edit through the editor

- **WHEN** a form of a Form filling room is opened with `GET /api/2.0/files/file/{fileId}/openedit?edit=true`
- **THEN** the server applies the `Edit` action before answering, so the form leaves filling mode

### Requirement: Start filling endpoint

`PUT /api/2.0/files/file/{fileId}/startfilling` (`startFillingFile`) SHALL mark a PDF lying directly in a Form
filling room as started and answer with the file. For any other file it SHALL change nothing and answer the
file unchanged (200). A caller with no access to the room SHALL be refused (403) unless they can manage the
room; a member holding only the form-filling right SHALL be refused (403).

#### Scenario: Form in a Form filling room

- **WHEN** the owner calls `startfilling` on a PDF in a Form filling room
- **THEN** the answer is 200 with the form (`id` unchanged) and the filling properties stored: the room id, the form title, the caller as the account that started the filling, the file's own id as the original form and `startFilling` true

#### Scenario: Not a form-room PDF

- **WHEN** `startfilling` is called on a `.docx` in My documents or on a PDF in My documents
- **THEN** the answer is 200 with the file unchanged and no filling properties are written

#### Scenario: Missing file and callers without access

- **WHEN** `startfilling` names a file that does not exist, or is called anonymously, or by a User or Guest who is not a member of the room
- **THEN** the answer is 404, 401 and 403 respectively
- **AND** a DocSpace administrator who is not a member is allowed (200)

### Requirement: Filling state follows a move or copy

A PDF moved or copied from outside into a Form filling room SHALL arrive with filling started. A PDF moved out
of a Form filling room or a Virtual Data Room to another room or section SHALL lose its filling properties. A
copy or move with `toFillOut` SHALL mark the new PDF as copied to fill out, the flag the client reads to offer
the role assignment. Only PDFs enter a Form filling room; the file-type rule and the deviation for ordinary
PDFs are in `form-filling-room-roles`.

#### Scenario: Form moved from My documents into a Form filling room

- **WHEN** a PDF is moved from My documents into a Form filling room through `fileops/move`
- **THEN** the file is started for filling on arrival (`startFilling` true, the new file being its own original form) and visible to the members with the form-filling right

#### Scenario: Form moved out of the room

- **WHEN** a started form is moved from a Form filling room to My documents or to another room
- **THEN** the moved file has no filling properties and is no longer linked to any draft
- **AND** a form moved out of a Virtual Data Room also loses its role assignment

#### Scenario: Copy to fill out

- **WHEN** a PDF is copied or moved with `toFillOut`
- **THEN** the new PDF carries the copied-to-fill-out flag (release notes v3.1: "after copying a PDF form through Share into a VDR, a dialog offers to assign roles")

### Requirement: Opening a form for filling in a Form filling room

When a PDF of a Form filling room is opened through `GET /api/2.0/files/file/{fileId}/openedit`, the server
SHALL decide the editor mode from the filling state and the caller's rights: the configuration carries
`startFillingMode` `StartFillingFormRoom` and `startFilling` telling whether this caller may start the filling. A not-started form opens for editing only for a caller who may start its filling (403 for
anyone else); a started form opens the caller's own draft in the embedded editor.

#### Scenario: Not-started form opened by a member who cannot start it

- **WHEN** a member who may not start the filling opens a form whose filling has not been started
- **THEN** the request is refused (403)

#### Scenario: Started form opened by a member who may fill it

- **WHEN** a member with the form-filling right opens a started form
- **THEN** the configuration is for the caller's draft (see the next requirement) in the embedded editor with `fillingSessionId` `{draftId}_{userId}`
- **AND** while the original is being edited and no draft exists yet, the form opens read-only

#### Scenario: Anonymous visitor on a not-started form

- **WHEN** a visitor through the room's external link opens a form whose filling has not been started
- **THEN** the request is refused (403)

#### Scenario: Anonymous visitor on a started form

- **WHEN** a visitor through the room's external link opens a started form
- **THEN** it opens as a submit-only embedded session whose `fillingSessionId` starts with the anonymous-session prefix

#### Scenario: Forms in the result folders

- **WHEN** a form in the per-form "In process" folder, or in the "Complete" folder, is opened
- **THEN** the "In process" form opens for filling with `fillingSessionId` `{fileId}_{userId}` and the "Complete" form opens read-only

### Requirement: Filling draft

`POST /api/2.0/files/masterform/{fileId}/checkfillformdraft` (`checkFillFormDraft`, body `version`,
`requestView`) SHALL answer the editor URL to open for a form in a Form filling room: the form itself for a
caller who may edit it, for a not-started form and for a view request; the caller's personal draft, created
or reused, for a caller who may fill but not edit a started form. An external link key is accepted instead of
a session; a caller with neither SHALL be refused (401).

#### Scenario: Draft created for a filler

- **WHEN** a member who may fill but not edit a started form asks for the draft URL and has no draft yet
- **THEN** a copy of the form titled `{First name} {Last name} - {form title}` is created in the per-form subfolder of the room's "In process" folder, linked to the original, with the new-item mark removed and the editing session of the original closed
- **AND** the URL points at the draft and, when the draft was just created, carries a `#message/...` fragment naming the folder

#### Scenario: Invalid draft

- **WHEN** a draft exists but is no longer valid — its source cannot be filled by the caller any more, or the original form's version changed since the draft was made
- **THEN** the URL points at the form with a `#message/...` discard notice

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
draft or submission: "In process" (`folderType` `InProcessFormFolder`) and "Complete" (`ReadyFormFolder`).
For each form it SHALL create a subfolder named after the form in each — `FormFillingFolderInProgress` for the
drafts, `FormFillingFolderDone` for the submitted copies — and record their ids on the form. These folders and
their files SHALL be protected as the scenarios state.

#### Scenario: System folders are immutable

- **WHEN** any member acts on one of the four result folder types
- **THEN** it cannot be renamed, moved, duplicated, given access or created in; the two room-level folders cannot be deleted; no link is copied for them; and the system account does not read them

#### Scenario: Files inside the per-form folders

- **WHEN** any member acts on a file inside a per-form subfolder
- **THEN** the file cannot be renamed, locked, moved, duplicated, have its history edited, be submitted to the form gallery or embedded
- **AND** a draft is fillable only by its creator, and a submitted copy is neither filled nor edited by anyone

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

When the editors submit a filled copy of a started form (a user-submit forced save, tracked by the document
service), the server SHALL store the copy in the form's "Complete" subfolder, numbered per form, classified as
a PDF form and marked completed, and SHALL announce the submission.
`PUT /api/2.0/files/file/{fileId}/saveediting` with a multipart body (`saveEditingFileFromForm`) instead
replaces the file's content with the posted binary as a new version — a save, not a submission.

#### Scenario: What a submission records

- **WHEN** a filled copy is stored
- **THEN** it carries filling properties pointing at the original form, the room and the result folder; the submitter's draft is unlinked and removed from the submitter's Recent; the copy is marked new for every member except those holding only the form-filling right; the "form submitted" notification is sent and the `form.submit` webhook emitted

#### Scenario: Anonymous submission

- **WHEN** a visitor through the room's link submits a filled copy
- **THEN** the copy is stored the same way under the original form's creator, with the room's links re-applied to it

#### Scenario: Complete subfolder missing

- **WHEN** the form's "Complete" subfolder no longer exists at submission time
- **THEN** it is recreated before the copy is stored

#### Scenario: Form binary saved through saveediting

- **WHEN** the owner posts a PDF binary of 512 bytes to `saveediting` of a form in a Form filling room, with or without `forcesave`
- **THEN** the answer is the same file at version 2, `pureContentLength` 512, comment "Edited", `isForm` still true — the save does not re-classify the file

#### Scenario: saveediting access

- **WHEN** `saveediting` with a form binary is called anonymously, by a User or Guest who is not a member of the room, or for a file that does not exist
- **THEN** the answer is 401, 403 and 404 respectively, and the 404 body does not leak an exception message

#### Scenario: Submission numbering

- **WHEN** three copies of the same form are submitted
- **THEN** they are stored as `1 - …`, `2 - …` and `3 - …` in the form's "Complete" subfolder
- **AND** the full name is `{N} - {form title} ({dd-MM-yyyy H-mm}).pdf`, where `N` is the submission number kept on the original form and incremented under a per-form lock

### Requirement: Results spreadsheet

A Form filling room SHALL carry the setting `saveFormAsXLSX` (release notes v3.7 "Update XLSX data"). With it on,
the server SHALL keep one results file `{form title}.xlsx` in the form's "Complete" subfolder, created on the
first submission or on the first report request and rewritten in place on every later submission, so that its
id and links stay; the server SHALL record the results file on the original form. With it off, submissions
SHALL be kept only as the filled copies.

#### Scenario: Results file rewritten in place

- **WHEN** a second submission arrives while `saveFormAsXLSX` is on
- **THEN** the existing `{form title}.xlsx` is rewritten and keeps its id and links

#### Scenario: Setting off

- **WHEN** `saveFormAsXLSX` is off and a copy is submitted
- **THEN** only the filled copy is stored and no results file is created

### Requirement: Results report generation

`POST /api/2.0/files/file/{fileId}/xlsx` (`generateXlsx`) SHALL queue a rebuild of a form's results file and
answer the queued task, the original form and `isNewFile`; it takes the id of the original form or of an XLSX
or CSV results file in its "Complete" subfolder. `GET …/file/{fileId}/xlsx` reports the caller's run, if any.
`POST /api/2.0/files/folder/{folderId}/xlsx` (`generateXlsxByFolder`) does the same by folder;
`GET …/file/{fileId}/submissions` (`getFormSubmissions`) lists them.

#### Scenario: Report for a form that is not started

- **WHEN** `POST …/xlsx` is called for a PDF whose filling has not been started, for a copy of a form, or for a form whose room is not a Form filling room
- **THEN** the request is refused (403) — the form must be started, be the original form (its own original) and still lie in the Form filling room that started it

#### Scenario: Report addressed through the results file

- **WHEN** `POST …/xlsx` is called with the id of the `.xlsx` results file in a "Complete" subfolder
- **THEN** the server resolves the original form behind it and queues the rebuild for that form
- **AND** a CSV results file is replaced by a new XLSX

#### Scenario: Status before any run

- **WHEN** `GET …/xlsx` is called for a form the caller never generated a report for
- **THEN** the answer is 200 with an empty body; a recorded run reports completion, progress, error and the produced file

#### Scenario: Report by folder

- **WHEN** `POST …/folder/{folderId}/xlsx` names a "Complete" subfolder, a folder that does not exist or holds nothing to report, or a folder of any other kind
- **THEN** the rebuild is queued for the subfolder's form, and the others answer 404 and 403 respectively

#### Scenario: Submissions listing

- **WHEN** `GET …/submissions` is called for a started original form by anyone who may read it
- **THEN** the answer carries the field metadata and one record per submitted copy of the version currently being filled

### Requirement: External database export

A Form filling room SHALL carry the setting `sendFormToExternalDB`. With it on and an external database
configured, every submission SHALL be pushed to the database; otherwise nothing is sent.
`POST /api/2.0/files/rooms/{id}/externaldbsync` (`startExternalDbSync`) SHALL queue a job that re-exports the
data of every original form of the room and answer the job record; `GET` on the same route
(`getExternalDbSyncStatus`) SHALL return that record, or an empty body when there is no job.

#### Scenario: Push per submission

- **WHEN** a copy is submitted while the setting is on and a database is configured (release notes v3.7 "Connect Database")
- **THEN** the submission goes to a table named after the original form and its version, keyed by the submission number, and the table name is recorded on the form
- **AND** generating the spreadsheet of a form also re-syncs that form to the database

#### Scenario: Sync job per room

- **WHEN** `POST …/externaldbsync` is called for a Form filling room the caller may edit
- **THEN** one job is kept per room: starting again while it runs returns the same record, a finished job is replaced
- **AND** a room the caller may not edit answers 403, an unknown room 404, and without a configured database the call fails with 400 and queues nothing

#### Scenario: Status record fields

- **WHEN** `GET …/externaldbsync` is called for a room with a job
- **THEN** the record carries `id`, `percentage`, `isCompleted`, `status`, `error` and, once finished, `forms` with one `id`/`title`/`success`/`error` entry per original form

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
value. For any other folder and while nothing has been submitted yet, the answer SHALL be an empty list, never
an error; the operation needs no authorization.

#### Scenario: Folder without submissions

- **WHEN** `formfilter` is requested for an empty folder, for a folder of ordinary documents, for Recent, for Favorites, for a deleted folder or for an id that does not exist
- **THEN** the answer is 200 with an empty array

#### Scenario: Readable folder of another member

- **WHEN** a member with read-only rights, or the room owner for a folder created by another member, requests `formfilter`
- **THEN** the answer is 200

### Requirement: Fill result of a finished session

`GET /api/2.0/files/file/fillresult?fillingSessionId=…` (`getFillResult`) SHALL answer, without sign-in, the
outcome of a completed filling session: the submitted copy (`completedForm`), the original form, the submission
number (`formNumber`), the manager (the form's creator), the room id and `isRoomMember`. The session
id is the one the editor reported at submission; its mapping to the copy is kept for a limited time only. A
missing parameter is refused (400); an unknown session answers 404.

#### Scenario: Session id missing or empty

- **WHEN** `fillresult` is called without `fillingSessionId` or with an empty value
- **THEN** the answer is 400

#### Scenario: Unknown session

- **WHEN** `fillresult` is called with a random UUID, with `invalid-uuid-format`, with a 100-character value, or anonymously with an unknown UUID
- **THEN** the answer is 404 with "The record could not be found" — a session that was never completed, one already forgotten and a value of the wrong shape are all answered this way

#### Scenario: Room id for a file link

- **WHEN** the caller came through a link to a file rather than to the room
- **THEN** `roomId` is omitted from the answer

### Requirement: Role-based filling in a Virtual Data Room

In a Virtual Data Room a PDF form SHALL be filled in steps by assigned participants (release notes v3.1).
`POST /api/2.0/files/file/{fileId}/formrolemapping` (`saveFormRoleMapping`, body `formId` + `roles` of
`roleName`, `roleColor`, `userId`, `sequence`, `roomId`) SHALL replace the form's role set and start its
filling; an empty role list resets it. Roles with the same `sequence` are filled in parallel; a
higher `sequence` waits for every lower one. The file SHALL be a PDF in a room (403).

#### Scenario: Roles assigned

- **WHEN** `formrolemapping` is sent with a non-empty role set
- **THEN** `startFilling` becomes true, the caller is recorded as the account that started the filling, the "form started to fill" history entry is written, every assigned account is notified and the form is revealed to the members holding only the form-filling right

#### Scenario: Reset with an empty role list

- **WHEN** `formrolemapping` is sent with an empty `roles` array for a configured form
- **THEN** the roles are removed, the filling properties are dropped and the form returns to the not-configured state

### Requirement: Form roles listing

`GET /api/2.0/files/file/{fileId}/formroles` (`getAllFormRoles`) SHALL return the roles ordered by sequence,
each with `roleName`, `roleColor`, `user`, `sequence`, `submitted`, `stopedBy`, `history` and `roleStatus`. A
form whose filling was never configured SHALL answer an empty list. The file SHALL be a PDF or the completed
copy of one (403 otherwise) and readable by the caller.

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
- **AND** the rule is: `roleStatus` is one of `None`, `Draft`, `YouTurn`, `InProgress`, `Complete`, `Stoped`; the current step is the lowest sequence that still has an unsubmitted role, roles of earlier steps are `Complete`, roles of later steps `Draft`, the role of the current step `YouTurn` until its holder opens the form and `InProgress` afterwards, every role is `Complete` when no step is left, and the role the filling was stopped at is `Stoped`; `history` lists the role's events as opened, submitted or stopped

### Requirement: Step submission and interruption in a Virtual Data Room

When a participant submits their step the server SHALL mark their role submitted with the time and SHALL
either notify the accounts of the next step ("your turn") or, when no step is left, mark the form completed.
`Stop` on a VDR form SHALL record the interruption and refuse further step submissions until `Resume` clears
the record. Removing a room member who still holds an unfinished role SHALL be refused unless the request is
forced.

#### Scenario: Last step completes the form

- **WHEN** the last outstanding step of a form is submitted
- **THEN** the form is marked completed (`isCompletedForm`, no longer editable by anyone), the editing session is closed, the `form.filled.out` webhook is emitted, the "form completely filled" history entry is written and every participant is notified

#### Scenario: Stop and resume

- **WHEN** `Stop` is applied to a VDR form
- **THEN** the stop time and the interrupting account and role are recorded, the other participants are notified and further step submissions are refused until `Resume` clears the record

#### Scenario: Member with an unfinished role removed

- **WHEN** a room member who still holds an unfinished role is removed through `PUT /api/2.0/files/rooms/{id}/share`
- **THEN** the membership change answers the `FormRoleBlockingDeletion` error instead of being applied, unless the request carries `force`

### Requirement: Sharing a form to fill out outside a form room

For a PDF opened from My documents, a Public room or any room other than a Form filling room or a Virtual Data
Room, the editor configuration SHALL offer `startFillingMode` `ShareToFillOut` when the caller may copy the
document and is signed in, and `startFilling` SHALL be true only for the form's own creator in My documents.
The quick-share link with the form-filling right exists in My documents, Form filling rooms and Virtual Data
Rooms only (`user-type-permissions`).

#### Scenario: Own form in My documents

- **WHEN** the creator opens their PDF form from My documents through `openedit`
- **THEN** the configuration carries `startFillingMode` `ShareToFillOut` and `startFilling` true (release notes v3.1: a PDF form is shared from Documents directly, the link is created with the fill right)

#### Scenario: Form in a Custom room

- **WHEN** a member opens a PDF form of a Custom room through `openedit`
- **THEN** the configuration carries `startFillingMode` `ShareToFillOut` and `startFilling` false

#### Scenario: Fill link refused in other room types

- **WHEN** a quick-share link with the form-filling right is requested for a form in a Public, Custom or Collaboration room
- **THEN** the link is refused (release notes v3.7.1); the scenarios are in `user-type-permissions`

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
admins and Users: `form.submit`, emitted when the editors deliver a user-submit forced save of a file that has
an original form behind it; `form.filled.out`, emitted when the last step of a Virtual Data Room form is
submitted and the form becomes complete; `form.stopped`, emitted on the `Stop` action and on the `Edit` action
of a started form in a Form filling room.

#### Scenario: Submit payload

- **WHEN** a `form.submit` webhook is published
- **THEN** its submitted-form payload is addressed by the original form

#### Scenario: Stop emits form.stopped

- **WHEN** filling of a started form is stopped, or a started form of a Form filling room is put back into editing
- **THEN** a `form.stopped` webhook is published for the form

#### Scenario: Edit of a not-started form emits nothing

- **WHEN** the `Edit` action is applied to a form whose filling is not started
- **THEN** no webhook is published

### Requirement: Forms section and form templates

Form filling rooms SHALL live under the Rooms tree but be listed only in the Forms section (release notes v4.0):
`GET /api/2.0/files/@forms` (`getFormsFolder`), the `Forms` root folder of `GET /api/2.0/files/@root`, and
`GET /api/2.0/files/rooms?searchArea=Forms`; `searchArea=Active` and the Rooms root SHALL leave them out. Room templates and used space SHALL be split the same way. The split is by
the room's folder type alone, so it covers rooms and templates created before the section existed.

#### Scenario: Form room in the Forms section only

- **WHEN** a Form filling room, a Custom room and a Public room exist
- **THEN** `@forms`, the Forms root folder and `rooms?searchArea=Forms` list the form room and not the others
- **AND** `rooms?searchArea=Active` and the Rooms root folder list the others and not the form room, even when `type=FillingFormsRoom` is requested

#### Scenario: Forms root is consistent

- **WHEN** the root folders are listed
- **THEN** one of them has `rootFolderType` `Forms`, and its id equals `current.id` of `@forms`

#### Scenario: Form template section

- **WHEN** a template is created from a Form filling room and another from a Custom room
- **THEN** `searchArea=FormTemplates` lists the first only and `searchArea=Templates` lists the second only, and browsing the templates root leaves the form template out
- **AND** a room created from the form template has `roomType` `FillingFormsRoom` and is listed under `searchArea=Forms`

#### Scenario: Used space

- **WHEN** a file is uploaded to a Form filling room, the room is archived, restored and deleted
- **THEN** `formsUsedSpace` grows by the file size, drops to the previous value while archived (the size moving to `archiveUsedSpace`), returns on restore and drops on delete, while `roomsUsedSpace` never changes
