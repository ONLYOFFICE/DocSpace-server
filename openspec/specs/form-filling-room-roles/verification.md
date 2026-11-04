# form-filling-room-roles — verification

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

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Form filling room role set** | | |
| Form Filler accepted (access echoed); Editor, Viewer → 403; Room manager for a User → 403 (user-type rule) | `03_Rooms/Sharing/RoomShareAccessLevelTests.cs:52-59, 104-114` | TEST-CONFIRMED |
| Reviewer, Commentator → 403 | `FileSecurity.cs:197-207` | CODE-ONLY |
| Content creator and Room manager (Room admin / DocSpace admin) land in the member list | `05_Features/ExternalDbSync/ExternalDbSyncTestBase.cs:61-68`; `ExternalDbSyncPermissionsTests.cs:85, 114`; `03_Rooms/Invitations/RoomInvitationResendTests.cs:243-254` | TEST-CONFIRMED |
| **Form filling room management by role** | | |
| Edit the room: owner switches spreadsheet collection and database sync on / off | `03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:170-200` | TEST-CONFIRMED (owner) |
| Edit / invite / change roles / remove by role | Custom-room analogues `03_Rooms/Permissions/RoomUpdatePermissionsTests.cs:166-199`, `RoomSharePermissionsTests.cs:104-242`; `FileSharing.cs:87-104` | CODE-ONLY |
| View member list / history / info by every role | Custom-room analogue `RoomReadPermissionsTests.cs:129-147`; a non-member who opened the primary link sees the room in the Forms section `03_Rooms/FormFilling/FormFillingRoomSectionTests.cs:281-315` (bug 83228) | CODE-ONLY |
| Archive / delete: owner | `03_Rooms/FormFilling/FormFillingRoomUsedSpaceTests.cs:95-161` (owner archives, restores, deletes) | TEST-CONFIRMED (owner) |
| Collect results into the spreadsheet: Room manager, Content creator (Room admin or User) on the owner's started form → task queued; Form Filler and non-members 403; anonymous 401 | `01_Files/FormFilling/FormXlsxPermissionsTests.cs:47-93, 116-144` (`generateXlsx` 9736, `getXlsx` 9832) | TEST-CONFIRMED |
| Spreadsheet / database switches: Room manager yes, Content creator 403 | `yaml` `updateRoom` (30402) demands the room-management right; the switches are room fields (`RoomUpdateVdrFieldsTests.cs:170-200`) | CODE-ONLY |
| Database sync endpoints: non-members (Room admin, User, Guest, DocSpace admin) and Form Filler members → 403; Room manager passes the gate; other room types → 403 | `05_Features/ExternalDbSync/ExternalDbSyncPermissionsTests.cs:45-127`; `ExternalDbSyncStartTests.cs:84-100`; `ExternalDbSyncStatusTests.cs:73-89` (`startExternalDbSync` 30095, `getExternalDbSyncStatus` 30187) | TEST-CONFIRMED |
| Form Filler / Content creator cannot invite | Custom-room analogue; `FileSharing.cs:101` | CODE-ONLY |
| **Only PDF files enter a Form filling room** | | |
| docx copied or moved into the room (root or subfolder) → 403; destination check "none allowed" | `06_Operations/Copy/FileCopyTests.cs:219-245, 334-362`; `06_Operations/Move/FileMoveRoomTypeTests.cs:113-136`; `06_Operations/CheckDestFolder/CheckDestFolderTests.cs:96-109` | TEST-CONFIRMED |
| docx uploaded or created in the room: refused with the message (500 on upload, see D8) | `deviations.md` D8 (live run) | LIVE-CONFIRMED |
| An ordinary PDF that is not a form is accepted; the destination check warns | `deviations.md` D13 and matrix row "Only ONLYOFFICE PDF forms enter the room / Any PDF (see D13)" | LIVE-CONFIRMED |
| ONLYOFFICE form copied from My documents (also with `toFillOut`) → in the room | `FileCopyTests.cs:531-561`; destination check "all allowed" `CheckDestFolderTests.cs:111-122` | TEST-CONFIRMED |
| Moving a form into the room: arrives, source emptied; collecting started and visible to Form Fillers | `FileMoveRoomTypeTests.cs:138-172` (arrival); started state: `deviations.md` D13 ("auto-started on move") | TEST-CONFIRMED (arrival) / LIVE-CONFIRMED (started) |
| **PDF forms and folders by role** | | |
| Form Filler, Content creator, Room manager read the submissions of a started form; non-members 403; anonymous 401 | `01_Files/FormFilling/FormSubmissionsPermissionsTests.cs:47-99` | TEST-CONFIRMED |
| Non-member DocSpace admin starts filling; non-member User / Guest 403 | `StartFillingFilePermissionsTests.cs:69-121` (bug 81400) | TEST-CONFIRMED |
| Content creator creates a form in the room | `ManageFormFillingPermissionsTests.cs:88-104` (Arrange) | TEST-CONFIRMED |
| Form Filler cannot create, upload, copy in, lock, manage versions, delete, move or rename | `FileSecurity`: the filling right carries no create / delete right (`IFileSecurity.cs:42-45`); Custom-room analogues for the Viewer | CODE-ONLY |
| Edit a form: Room manager (DocSpace admin) saves from the editor; non-member User / Guest 403; owner saves a new version; draft check | `SaveEditingFileFromFormPermissionsTests.cs:69-117`; `SaveEditingFileFromFormTests.cs:58-117`; `CheckFillFormDraftTests.cs:58-72`; `CheckFillFormDraftPermissionsTests.cs:61-107` (bug 81441) | TEST-CONFIRMED |
| Content creator edits a started form only if they created it or started its collection | `yaml` `manageFormFilling` (13046) / `startFillingFile` (9008) remarks; `deviations.md` matrix row "Results sync by a Content creator 'only own / organized' / Any Content creator, any form" settles the sync, not the edit | CODE-ONLY |
| View comments: Form Filler no | `form-filling-workflow` (the filling editor has no comment panel) | MATRIX-ONLY — not observable through the API |
| Delete / move / rename own vs others' (Content creator own only) | Custom-room analogues (`01_Files/Deletion/FileDeletePermissionsTests.cs`, `01_Files/Update/FileUpdatePermissionsTests.cs`); `deviations.md` matrix rows "Rename …", "Copy …" (FFR listed) | LIVE-CONFIRMED |
| Form Filler copies a started form out to My documents | `deviations.md` matrix row "A Form Filler does not copy or move forms … / Copies forms out" | LIVE-CONFIRMED |
| Form Filler opens another member's result → denied; sees only own results in the system folders | `deviations.md` matrix row above ("deletes own results in both folders (delete: code only)") and D6 evidence | LIVE-CONFIRMED |
| Room's primary link: one auto-created link with the filling right; a form inside offers filling only | `03_Rooms/Links/RoomLinkReadTests.cs:365-379`; `04_Security/RoomShareTests.cs:75-98`; `04_Security/ShareInheritanceTests.cs:811-826`; role split (Room manager yes, Content creator no): `shared-links` | TEST-CONFIRMED (shape) / CODE-ONLY (role split) |
| **Form collection lifecycle by role** | | |
| Owner, Room manager (DocSpace admin) start and stop; Content creator starts the owner's form | `ManageFormFillingTests.cs:46-57`; `ManageFormFillingPermissionsTests.cs:57-86` | TEST-CONFIRMED |
| Content creator cancels the owner's collection → 403 | `ManageFormFillingPermissionsTests.cs:128-145` | TEST-CONFIRMED |
| Content creator cancels own: a form they created and started; a form they started on the owner's upload (bug 81470) | `ManageFormFillingPermissionsTests.cs:88-126` | TEST-CONFIRMED |
| Room manager cancels the owner's collection | `ManageFormFillingPermissionsTests.cs:57-72` (the manager stops a form the manager started); the owner's form: `yaml` `manageFormFilling` (13046) | CODE-ONLY |
| Form Filler cannot start or cancel | `yaml` `manageFormFilling` (13046): Content creator and above | CODE-ONLY |
| Form Filler sees started forms, not not-started ones; `getFileInfo` of a not-started form → 403 for a Form Filler member | `03_Rooms/FormFilling/FormFillingRoomOwnerAccessTests.cs:120-137` (403 by id); submissions read `FormSubmissionsPermissionsTests.cs:47-65`; the VDR analogue `04_Security/RoomShareTests.cs:1058-1066`; `deviations.md` D6 (listing) | TEST-CONFIRMED (by id) / LIVE-CONFIRMED (listing) |
| Room owner keeps a not-started form whatever record decides their access (link token on the request; owner in a Form Filler group; a Room admin owner): `security.delete` true | `03_Rooms/FormFilling/FormFillingRoomOwnerAccessTests.cs:55-114` [Bug 84849]; `FileSecurity.cs:1305-1318` (`HasFullAccessAsync` exemption), `:3900-3925` | TEST-CONFIRMED |
| Results folders: every role sees them; Content creator and Form Filler see and delete only own results | `deviations.md` matrix row "A Form Filler does not … delete 'Complete' forms / deletes own results in both folders (delete: code only)" | LIVE-CONFIRMED |
| Results folder exists after the first spreadsheet run; the report completes and `resultFileId` resolves to the form | `FormXlsxTests.cs:52-114` (`resultsFolderId` > 0 at `:64-65`; no longer skipped since the host runs a document server) | TEST-CONFIRMED |

Counts: 36 rows — TEST-CONFIRMED 19 (2 owner-only), CODE-ONLY 9, LIVE-CONFIRMED 7, MATRIX-ONLY 1 (the
"Form Filler does not see comments" row — editor-side, not observable through the API). Spec: 5
requirements, 24 scenarios (before the reshaping: 5 requirements, 19 scenarios; 23 before revision
2026-11-04). Flag: no FFR test refuses a Form Filler a start / cancel, a create or a rename; no test produces
a result in "In process" / "Complete".

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
| D8 | Only PDF files enter | A docx uploaded or created in an FFR answers 500 with the "cannot be uploaded to this room" message; the copy of the same docx answers 403. | `deviations.md` D8 (live run); the 403 of copy / move `FileCopyTests.cs:219-245`, `FileMoveRoomTypeTests.cs:113-136`; `products/ASC.Files/Server/Api/UploadController.cs` still documents the 500 ("a file that is not a PDF is stored in a form-filling room"); `FileDao.cs:774-814` throws a plain exception | STILL OPEN (2026-11-04: the upload fix of the range covers a multipart request without the file part, not this) |
| D13 | Only PDF files enter | The rule is enforced by extension only, so an ordinary PDF enters the room and is auto-started on move; the form-signature check exists only in the advisory destination check. | `deviations.md` D13 (live run); `FileDao.cs:774-814` (`file.IsPdf`) | STILL OPEN (2026-11-04: untouched in the range) |
