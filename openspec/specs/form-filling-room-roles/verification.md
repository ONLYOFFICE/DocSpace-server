# form-filling-room-roles — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); Files test paths omit the `products/ASC.Files/Tests/Tests/` prefix. `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml` (submodule `a0fc3a2`). Status: **TEST-CONFIRMED** (an
integration test asserts it on a Form filling room), **CODE-ONLY** (read from the product code or the
OpenAPI document, no FFR test), **LIVE-CONFIRMED** (only the live run of 2026-10-14 recorded in
`openspec/deviations.md` asserts it), **MATRIX-ONLY** (stated by the product matrix, sheet "FFR"; no
test, code or live evidence found — flagged).

The FFR role set is the Form filling room entry of `AvailableRoomAccesses`
(`products/ASC.Files/Core/Core/Security/FileSecurity.cs:199-209`: Room manager, Content creator, Form
Filler for users; filling and read for links). The "only PDF" rule and its message ("The file cannot be
uploaded to this room. Please try to upload the ONLYOFFICE PDF form.", `FilesCommonResource.resx:433`) are
in the file DAO and the copy permission check (`deviations.md` D13 evidence); no test asserts the text.
Tests that produce *results* ("In process" / "Complete") need a filling session through the editor, which
no integration test drives, so every result-folder row is CODE-ONLY or LIVE-CONFIRMED (the integration host
has a document server since 4.1.0 — the spreadsheet report and `isformpdf` conversions run — but no test
fills a form). Custom-room analogues of a role rule are marked as such and count as CODE-ONLY.

Revision 2026-10-25: first verification; the spec was reshaped at the same time (the three matrices moved
into the scenarios "Form filling room management matrix", "Form filling room file matrix", "Form lifecycle
matrix"; the prose about editing a started form became the scenario "Content creator edits a started
form"; no requirement or scenario renamed).

Revision 2026-11-04 (release/v4.1.0, `d58816bece..3533ad8a89`): bug 84849 (commit `0fa0ac3f4c`) changed
the Form filling room PDF branch of `FileSecurity.cs:1305-1318` — the check that hides a not-started form
from a Form Filler now skips the room's owner, whose access record can be a link's or a group's — and
brought `03_Rooms/FormFilling/FormFillingRoomOwnerAccessTests.cs`; the scenario "Room owner keeps a
not-started form whatever record decides their access" was added and "Form Filler does not see a form that
is not started" got the 403-by-id bullet. The spreadsheet report tests are no longer skipped. The role set
moved from `FileSecurity.cs:197-207` to `:199-209`. D1, D6, D8 and D13 are unchanged.

Revision 2026-11-11: the ten CODE-ONLY rows got Form filling room tests and ran green (the role matrices `03_Rooms/Permissions/FormFillingRoomManagementRolesTests.cs`, `01_Files/FormFilling/FormFillingRoomFileRolesTests.cs`, two cases in `ManageFormFillingPermissionsTests.cs`, two rows in `RoomShareAccessLevelTests.cs`). A Form Filler's 403 on `getFileInfo` of a not-started form is the intended rule of "Form Filler does not see a form that is not started", already asserted by `FormFillingRoomOwnerAccessTests.cs:120-137`; no spec change, no new defect.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Form filling room role set** | | |
| Form Filler accepted (access echoed); Editor, Viewer → 403; Room manager for a User → 403 (user-type rule) | `03_Rooms/Sharing/RoomShareAccessLevelTests.cs:52-59, 104-114` | TEST-CONFIRMED [run: PASS] |
| Reviewer, Commentator → 403 | `03_Rooms/Sharing/RoomShareAccessLevelTests.cs:60-61` (rows of the theory at `:87-115`); `FileSecurity.cs:199-209` | TEST-CONFIRMED [run: PASS] |
| Content creator and Room manager (Room admin / DocSpace admin) land in the member list | `05_Features/ExternalDbSync/ExternalDbSyncTestBase.cs:61-68`; `ExternalDbSyncPermissionsTests.cs:85, 114`; `03_Rooms/Invitations/RoomInvitationResendTests.cs:243-254` | TEST-CONFIRMED [run: PASS] |
| **Form filling room management by role** | | |
| Edit the room: owner switches spreadsheet collection and database sync on / off | `03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:170-200` | TEST-CONFIRMED [run: PASS] (owner) |
| Edit / invite / change roles / remove by role | `03_Rooms/Permissions/FormFillingRoomManagementRolesTests.cs:56-93` (rename: Room manager yes, Content creator / Form Filler 403, room unchanged), `:95-145` (invite, re-role, remove: Room manager yes, the others 403 each, members unchanged) | TEST-CONFIRMED [run: PASS] |
| View member list / history / info by every role | `03_Rooms/Permissions/FormFillingRoomManagementRolesTests.cs:147-175` (Room manager, Content creator, Form Filler read `getRoomInfo`, `getRoomSecurityInfo`, `getFolderHistory`); a non-member who opened the primary link sees the room in the Forms section `03_Rooms/FormFilling/FormFillingRoomSectionTests.cs:281-315` (bug 83228) | TEST-CONFIRMED [run: PASS] |
| Archive / delete: owner | `03_Rooms/FormFilling/FormFillingRoomUsedSpaceTests.cs:95-161` (owner archives, restores, deletes) | TEST-CONFIRMED [run: PASS] (owner) |
| Collect results into the spreadsheet: Room manager, Content creator (Room admin or User) on the owner's started form → task queued; Form Filler and non-members 403; anonymous 401 | `01_Files/FormFilling/FormXlsxPermissionsTests.cs:47-93, 116-144` (`generateXlsx` 9736, `getXlsx` 9832) | TEST-CONFIRMED [run: PASS] |
| Spreadsheet / database switches: Room manager yes, Content creator 403 | `03_Rooms/Permissions/FormFillingRoomManagementRolesTests.cs:56-93` (`saveFormAsXLSX` and `sendFormToExternalDB` flipped by a Room manager; Content creator and Form Filler 403, both switches unchanged) | TEST-CONFIRMED [run: PASS] |
| Database sync endpoints: non-members (Room admin, User, Guest, DocSpace admin) and Form Filler members → 403; Room manager passes the gate; other room types → 403 | `05_Features/ExternalDbSync/ExternalDbSyncPermissionsTests.cs:45-127`; `ExternalDbSyncStartTests.cs:84-100`; `ExternalDbSyncStatusTests.cs:73-89` (`startExternalDbSync` 30095, `getExternalDbSyncStatus` 30187) | TEST-CONFIRMED [run: PASS] |
| Form Filler / Content creator cannot invite | `03_Rooms/Permissions/FormFillingRoomManagementRolesTests.cs:95-145` | TEST-CONFIRMED [run: PASS] |
| **Only PDF files enter a Form filling room** | | |
| docx copied or moved into the room (root or subfolder) → 403; destination check "none allowed" | `06_Operations/Copy/FileCopyTests.cs:219-245, 334-362`; `06_Operations/Move/FileMoveRoomTypeTests.cs:113-136`; `06_Operations/CheckDestFolder/CheckDestFolderTests.cs:96-109` | TEST-CONFIRMED [run: PASS] |
| docx uploaded or created in the room: refused with 403 and nothing stored; no test asserts the message | `02_Folders/Upload/FormsRoomNonPdfUploadTests.cs:57-86`, `01_Files/Create/FormsRoomNonPdfCreateTests.cs:63-95` (bug 84840) | TEST-CONFIRMED [run: PASS] |
| An ordinary PDF that is not a form is accepted; the destination check warns | `deviations.md` D13 and matrix row "Only ONLYOFFICE PDF forms enter the room / Any PDF (see D13)" | LIVE-CONFIRMED |
| ONLYOFFICE form copied from My documents (also with `toFillOut`) → in the room | `FileCopyTests.cs:531-561`; destination check "all allowed" `CheckDestFolderTests.cs:111-122` | TEST-CONFIRMED [run: PASS] |
| Moving a form into the room: arrives, source emptied; collecting started and visible to Form Fillers | `FileMoveRoomTypeTests.cs:138-172` (arrival); started state: `deviations.md` D13 ("auto-started on move") | TEST-CONFIRMED [run: PASS] (arrival) / LIVE-CONFIRMED (started) |
| **PDF forms and folders by role** | | |
| Form Filler, Content creator, Room manager read the submissions of a started form; non-members 403; anonymous 401 | `01_Files/FormFilling/FormSubmissionsPermissionsTests.cs:47-99` | TEST-CONFIRMED [run: PASS] |
| Non-member DocSpace admin starts filling; non-member User / Guest 403 | `StartFillingFilePermissionsTests.cs:69-121` (bug 81400) | TEST-CONFIRMED [run: PASS] |
| Content creator creates a form in the room | `ManageFormFillingPermissionsTests.cs:88-104` (Arrange) | TEST-CONFIRMED [run: PASS] |
| Form Filler cannot create, upload, copy in, lock, manage versions, delete, move or rename | `01_Files/FormFilling/FormFillingRoomFileRolesTests.cs:47-74` (create file / folder, upload session), `:76-100` (rename, lock, `changeVersionHistory`), `:102-130` (delete, move out to My documents), `:132-157` (copy own form in); 403 each, room content and form unchanged; all on a started form | TEST-CONFIRMED [run: PASS] |
| Edit a form: Room manager (DocSpace admin) saves from the editor; non-member User / Guest 403; owner saves a new version; draft check | `SaveEditingFileFromFormPermissionsTests.cs:69-117`; `SaveEditingFileFromFormTests.cs:58-117`; `CheckFillFormDraftTests.cs:58-72`; `CheckFillFormDraftPermissionsTests.cs:61-107` (bug 81441) | TEST-CONFIRMED [run: PASS] |
| Content creator edits a started form only if they created it or started its collection | `01_Files/FormFilling/FormFillingRoomFileRolesTests.cs:159-192` (`security.edit` false on the owner's form the owner started, true on own created-and-started and on the owner's form the Content creator started) | TEST-CONFIRMED [run: PASS] (the right) / CODE-ONLY (a save through the editor) |
| View comments: Form Filler no | `form-filling-workflow` (the filling editor has no comment panel) | MATRIX-ONLY — not observable through the API |
| Delete / move / rename own vs others' (Content creator own only) | Custom-room analogues (`01_Files/Deletion/FileDeletePermissionsTests.cs`, `01_Files/Update/FileUpdatePermissionsTests.cs`); `deviations.md` matrix rows "Rename …", "Copy …" (FFR listed) | LIVE-CONFIRMED |
| Form Filler copies a started form out to My documents | `deviations.md` matrix row "A Form Filler does not copy or move forms … / Copies forms out" | LIVE-CONFIRMED |
| Form Filler opens another member's result → denied; sees only own results in the system folders | `deviations.md` matrix row above ("deletes own results in both folders (delete: code only)") and D6 evidence | LIVE-CONFIRMED |
| Room's primary link: one auto-created link with the filling right; a form inside offers filling only | `03_Rooms/Links/RoomLinkReadTests.cs:365-379`; `04_Security/RoomShareTests.cs:75-98`; `04_Security/ShareInheritanceTests.cs:811-826`; role split on the one primary link (an FFR takes no second external link, `RoomLinkCreateTests.cs:326-330`): `03_Rooms/Permissions/FormFillingRoomManagementRolesTests.cs:177-219` (Room manager renames it, Content creator / Form Filler 403, title unchanged) | TEST-CONFIRMED [run: PASS] |
| **Form collection lifecycle by role** | | |
| Owner, Room manager (DocSpace admin) start and stop; Content creator starts the owner's form | `ManageFormFillingTests.cs:46-57`; `ManageFormFillingPermissionsTests.cs:57-86` | TEST-CONFIRMED [run: PASS] |
| Content creator cancels the owner's collection → 403 | `ManageFormFillingPermissionsTests.cs:128-145` | TEST-CONFIRMED [run: PASS] |
| Content creator cancels own: a form they created and started; a form they started on the owner's upload (bug 81470) | `ManageFormFillingPermissionsTests.cs:88-126` | TEST-CONFIRMED [run: PASS] |
| Room manager cancels the owner's collection | `01_Files/FormFilling/ManageFormFillingPermissionsTests.cs:147-167` (Room admin as Room manager stops the owner's started form) | TEST-CONFIRMED [run: PASS] |
| Form Filler cannot start or cancel | `01_Files/FormFilling/ManageFormFillingPermissionsTests.cs:169-196` (Start of a not-started form and Stop of a started one: 403 each, the form stays started; Resume / Edit not checked, D1) | TEST-CONFIRMED [run: PASS] |
| Form Filler sees started forms, not not-started ones; `getFileInfo` of a not-started form → 403 for a Form Filler member | `03_Rooms/FormFilling/FormFillingRoomOwnerAccessTests.cs:120-137` (403 by id); submissions read `FormSubmissionsPermissionsTests.cs:47-65`; the VDR analogue `04_Security/RoomShareTests.cs:1058-1066`; `deviations.md` D6 (listing) | TEST-CONFIRMED [run: PASS] (by id) / LIVE-CONFIRMED (listing) |
| Room owner keeps a not-started form whatever record decides their access (link token on the request; owner in a Form Filler group; a Room admin owner): `security.delete` true | `03_Rooms/FormFilling/FormFillingRoomOwnerAccessTests.cs:55-114` [Bug 84849]; `FileSecurity.cs:1305-1318` (`HasFullAccessAsync` exemption), `:3900-3925` | TEST-CONFIRMED [run: PASS] |
| Results folders: every role sees them; Content creator and Form Filler see and delete only own results | `deviations.md` matrix row "A Form Filler does not … delete 'Complete' forms / deletes own results in both folders (delete: code only)" | LIVE-CONFIRMED |
| Results folder exists after the first spreadsheet run; the report completes and `resultFileId` resolves to the form | `FormXlsxTests.cs:52-114` (`resultsFolderId` > 0 at `:64-65`; no longer skipped since the host runs a document server) | TEST-CONFIRMED [run: PASS] |

Counts: 36 rows — TEST-CONFIRMED 29 (2 owner-only, 1 with the editor save still CODE-ONLY), CODE-ONLY 0, LIVE-CONFIRMED 7, MATRIX-ONLY 1 (the
"Form Filler does not see comments" row — editor-side, not observable through the API). Spec: 5
requirements, 24 scenarios (before the reshaping: 5 requirements, 19 scenarios; 23 before revision
2026-11-04). Flag: no test produces a result in "In process" / "Complete".

## Revision 2026-11-06 (`1f1b052f7d..0ebdbb7c84`)

| Claim | Source | Status |
|---|---|---|
| Upload, create and session routes refuse a non-PDF with 403 (Uploading a non-PDF file) | same tests as D8 | TEST-CONFIRMED [run: NO-LINK] |
| D13 (PDF by extension only) | `1a1d539775` changed the exception type only | STILL OPEN |

## Where the server differs from the product matrix (spec follows the server)

| Matrix (sheet "FFR") | Server | Reference |
|---|---|---|
| Only ONLYOFFICE PDF forms enter the room | Any PDF (D13) | `deviations.md`, matrix table |
| A Form Filler does not copy or move forms; does not delete "Complete" forms | Copies forms out; deletes own results in both folders (delete: code only) | same |
| Results sync by a Content creator "only own / organized" (resolved comment) | Any Content creator, any form | same; `FormXlsxPermissionsTests.cs:47-64` |
| Rename: Owner and Room manager only | A Content creator renames own entries | same |
| Copy of other members' files: Owner and Room manager | Copy does not depend on authorship | same |
| Test `FileCopyTests.cs:334-362` (`CopyFile_FormToFillingFormsRoom_ReturnsError`) sets `IsForm = true` on the local DTO | No server effect; the case is a docx copied into an FFR subfolder | `FileCopyTests.cs:334-362` |
| `01_Files/FormFilling/FormFilesTests.cs:42-139` | Legacy smoke file: asserts `true` for a regular PDF and swallows 400 / 403 / 404 / 501; no evidence for any row | — |

## Left out of the spec, and why

- **Start filling outside an FFR is a no-op (200)**, unknown id 404 (`StartFillingFileTests.cs:65-106`);
  **spreadsheet / submissions on a not-started form or a docx → 403** (`FormXlsxTests.cs:120-149`,
  `FormSubmissionsTests.cs:90-104`); **second spreadsheet run reuses the report, parallel runs share one
  task, another user sees null** (`FormXlsxTests.cs:86-101`, `FormXlsxConcurrencyTests.cs:46-62`,
  `FormXlsxPermissionsTests.cs:95-114`): `form-filling-workflow`.
- **Database sync on other room types → 403, unknown room 404, no external DB → 400** (bug 84473;
  `05_Features/ExternalDbSync/*`): `form-filling-workflow`.
- **Forms section listing, templates and used space of an FFR** (`03_Rooms/FormFilling/*`,
  `FileCopyTests.cs:247-273`): `room-types`, `quotas`.
- **Primary link shape of an FFR** (`RoomLinkReadTests.cs:365-379`, `ShareInheritanceTests.cs:811-826`):
  `shared-links`.

## Suspected defects

D-numbers are the ones of `openspec/deviations.md`; no new ones.

| # | Where | What the server does | Evidence | Status |
|---|-------|----------------------|----------|--------|
| D1 | Form collection lifecycle | `manageFormFilling` checks permissions only for Start and Stop; Resume and Edit answer 200 for a Viewer, a Form Filler without a role, and a non-member User or Guest. | `deviations.md` D1 (live run); `FileStorageService.cs:5861-5960` | STILL OPEN (2026-11-04: the method is untouched in the range) |
| D6 | PDF forms by role | A Form Filler copies a not-started form, hidden from them in the listing and 403/404 by id, to My documents through the copy operation (200, copied). | `deviations.md` D6 (live run); the by-id refusal is now asserted as 403 (`FormFillingRoomOwnerAccessTests.cs:120-137`) | STILL OPEN (2026-11-04: the copy path `CopyPermissionsCheck.cs` changed only for the AI rooms section) |
| D8 | Only PDF files enter | A docx uploaded or created in an FFR used to answer 500; it now answers 403, the same as the copy of that docx. | `file-operations/verification.md` F19; `FileDao.cs:826-828`; `FormsRoomNonPdfUploadTests.cs:57-86`; `UploadSessionFormsRoomTests.cs:56-99`; the `SwaggerResponse` of the upload routes moved from 500 to 403 | FIXED (`1a1d539775`); `openspec/deviations.md` D8 still has the old text |
| D13 | Only PDF files enter | The rule is enforced by extension only, so an ordinary PDF enters the room and is auto-started on move; the form-signature check exists only in the advisory destination check. | `deviations.md` D13 (live run); `FileDao.cs:774-814` (`file.IsPdf`) | STILL OPEN (2026-11-04: untouched in the range) |
