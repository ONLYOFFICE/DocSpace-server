# vdr-room-roles — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); Files test paths omit the `products/ASC.Files/Tests/Tests/` prefix. `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml` (submodule `a0fc3a2`). Status: **TEST-CONFIRMED** (an
integration test asserts it on a Virtual Data Room), **CODE-ONLY** (read from the product code or the
OpenAPI document, no VDR test), **LIVE-CONFIRMED** (only the live run of 2026-10-14 recorded in
`openspec/deviations.md` asserts it), **MATRIX-ONLY** (stated by the product matrix, sheet "VDR"; no
test, code or live evidence found — flagged).

Nearly every permission suite of the Files tests creates a *Custom* room (`CreateCustomRoom`), so it is
evidence for `room-role-permissions`, not for this spec; such rows below are marked "(Custom-room analogue)"
and count as CODE-ONLY: the role check is the same `FileSecurity` path for every room type, the room type
only selects the role set (`products/ASC.Files/Core/Core/Security/FileSecurity.cs:219-227`, VDR entry of
`AvailableRoomAccesses`: Room manager, Content creator, Editor, Viewer, Form Filler). The theory data
`03_Rooms/RoomAccessData.cs:57-72, 150-152` (VDR access sets) exists but has no consumer. The message
"The role is not available for this user type" (`FilesCommonResource.resx:331`) is never asserted. Where
the Custom-room test is cited, its line range is given for the reader who wants the assertion.

Revision 2026-10-25: first verification; the spec was reshaped at the same time (the three matrices moved
into the scenarios "VDR room management matrix", "VDR file management matrix", "VDR form workflow matrix";
no requirement or scenario renamed).

Revision 2026-11-04 (release/v4.1.0, `d58816bece..3533ad8a89`): no VDR rule changed and no new VDR test
appeared; the range touches `FileSecurity.cs` only for the AI rooms section (the role tables moved to
`:159-263` / `:265-281`) and for the Form filling room PDF branch (`:1305-1318`, the owner exemption of bug
84849, which applies to Form filling rooms only). `manageFormFilling` (`FileStorageService.cs:5861-5960`) is
untouched, so D1 and D5 stand. Line references below were refreshed; the spec text is unchanged.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **VDR role set** | | |
| Reviewer and Commentator → 403, member absent; Viewer, Editor, Form Filler, Content creator accepted | `03_Rooms/Sharing/RoomShareAccessLevelTests.cs:73-80, 104-114` | TEST-CONFIRMED [run: PASS] |
| Room manager accepted in a VDR (for a Room admin) | `01_Files/Update/FileUpdateTests.cs:140-182` (Arrange) | TEST-CONFIRMED [run: PASS] |
| **VDR room management by role** | | |
| Edit the room: owner changes indexing, deny-download, watermark, lifetime | `03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:46-168` (owner only) | TEST-CONFIRMED [run: PASS] (owner) |
| Edit the room by role (Room manager yes, others 403) | `03_Rooms/Permissions/VdrRoomManagementRolesTests.cs:59-90` (rename: Room manager 200; Content creator, Editor, Form Filler, Viewer 403, title unchanged) | TEST-CONFIRMED [run: PASS] |
| Invite / assign / change roles / remove: Room manager yes, lower roles 403 | `03_Rooms/Permissions/VdrRoomManagementRolesTests.cs:94-141` (Room manager invites with a role, changes a role, removes; every lower role 403 for all three, members unchanged); `FileSharing.cs:87-104` | TEST-CONFIRMED [run: PASS] |
| View member list / history / info by every role | `03_Rooms/Permissions/VdrRoomReadArchiveRolesTests.cs:57-74` (room info, `rooms/{id}/share`, `folder/{id}/log` for all five roles) | TEST-CONFIRMED [run: PASS] |
| Archive / delete: owner only | `03_Rooms/Permissions/VdrRoomReadArchiveRolesTests.cs:78-121` (all five member roles incl. Room manager 403 for archive and delete, room stays active; the owner archives, then deletes) | TEST-CONFIRMED [run: PASS] |
| **VDR files and folders by role** | | |
| Form Filler sees folders and no file before a form role is assigned, the form after | `04_Security/RoomShareTests.cs:1039-1097` (bug 80322) | TEST-CONFIRMED [run: PASS] |
| Form Filler requesting a non-form file → 403 | `deviations.md` matrix row "A Form Filler views only forms they take part in / Non-form files are refused; see D5 for PDFs" | LIVE-CONFIRMED |
| Download by every role; deny-download restriction on a Viewer (download, view URL, copy to My documents → 403); Editor not restricted | `deviations.md` shared-links row "VDR: deny-download … the room setting still blocks the download"; the room flag itself `RoomUpdateVdrFieldsTests.cs:79-93`; `room-types` ("Room settings", `denyDownload`: members without editing rights cannot download or print) | LIVE-CONFIRMED |
| Comment: Viewer 403 | `01_Files/EditSession/VdrFileEditRolesTests.cs:66-82` (the file's Comment flag: false for a Viewer, true for Room manager, Content creator, Editor) | TEST-CONFIRMED [run: PASS] (right) / CODE-ONLY (the comment itself is written in a document server session the stand does not drive) |
| Review / Edit: Editor yes, Form Filler and Viewer no | `01_Files/EditSession/VdrFileEditRolesTests.cs:66-108` (Review / Edit flags; edit session: Room manager, Content creator, Editor 200, Form Filler and Viewer 403) | TEST-CONFIRMED [run: PASS] (rights, edit session) / CODE-ONLY (saving an edit through the document server) |
| View version history: Editor yes, Form Filler / Viewer no | `01_Files/History/VdrEditHistoryRolesTests.cs:55-86` (`GetEditHistory`: Room manager, Content creator, Editor read both versions; Form Filler, Viewer 403) | TEST-CONFIRMED [run: PASS] |
| Create / upload: Content creator yes; Editor, Form Filler, Viewer 403 | `01_Files/Create/VdrFileCreateRolesTests.cs:55-98` (document, text file, folder); `02_Folders/Upload/VdrUploadRolesTests.cs:55-87` (Room manager and Content creator 200; Editor, Form Filler, Viewer 403, room empty) | TEST-CONFIRMED [run: PASS] |
| Copy from My documents: Content creator yes; Viewer / Editor 403; owner moves a docx into a VDR, destination check "all allowed" | `06_Operations/Copy/VdrCopyRolesTests.cs:56-83` (Room manager, Content creator copied; Editor, Form Filler, Viewer 403, nothing copied); `06_Operations/Move/FileMoveRoomTypeTests.cs:46-83` (VDR row); `06_Operations/CheckDestFolder/CheckDestFolderTests.cs:77-94` | TEST-CONFIRMED [run: PASS] |
| Lock: Room manager / Content creator lock own file; Editor, Form Filler, Viewer 403 (VDR); Room manager unlocks another manager's lock; Content creator cannot unlock another Content creator's lock | `01_Files/Update/FileUpdateTests.cs:140-280` (runs on a VDR) | TEST-CONFIRMED [run: PASS] |
| Manage version history: Content creator and above | `01_Files/Versions/VdrChangeVersionHistoryRolesTests.cs:55-86` (Room manager, Content creator 200; Editor, Form Filler, Viewer 403) | TEST-CONFIRMED [run: PASS] |
| Delete / move / rename own vs others' (Content creator own only; Room manager any) | Custom-room analogues `01_Files/Deletion/FileDeletePermissionsTests.cs:111-189`; `06_Operations/Move/FileMovePermissionsTests.cs:221-315`; `01_Files/Update/FileUpdatePermissionsTests.cs:82-204`; `02_Folders/Crud/FolderRenamePermissionsTests.cs:46-53, 117-132`; `deviations.md` matrix rows "Rename …" and "Copy of other members' files …" (VDR listed) | LIVE-CONFIRMED |
| Copy out of the room: every role | `deviations.md` row above | LIVE-CONFIRMED |
| Copy within the room: Content creator | `06_Operations/Copy/VdrCopyRolesTests.cs:85-112` (owner's file into a folder of the room: Room manager, Content creator copied; Editor, Form Filler, Viewer 403) | TEST-CONFIRMED [run: PASS] |
| File and folder links: owner's folder / file primary link in a VDR is internal-only, access not editable; external room link → 403 | `04_Security/FolderShareTests.cs:304-321`; `04_Security/ShareInheritanceTests.cs:760-809`; `03_Rooms/Links/RoomLinkReadTests.cs:352-363`; `04_Security/RoomShareTests.cs:115-133`; role split `04_Security/VdrLinkRolesTests.cs:55-92` (creating the primary link of the owner's file and folder: Room manager 200, internal; Content creator, Editor, Form Filler, Viewer 403) | TEST-CONFIRMED [run: PASS] (shape, creation by role) / CODE-ONLY (editing and deleting a link by role: `shared-links`) |
| Third-party version history unavailable | `room-role-permissions` row | CODE-ONLY (cross-reference) |
| **PDF form filling in a VDR** | | |
| Create / upload PDF forms: owner (Arrange) | `RoomShareTests.cs:1049` | TEST-CONFIRMED [run: PASS] (owner) |
| Form Filler does not see a not-configured form | `RoomShareTests.cs:1058-1066` | TEST-CONFIRMED [run: PASS] |
| Viewer / Editor see not-configured forms; Editor edits them | `01_Files/FormFilling/VdrFormRolesTests.cs:54-68` (listing); `04_Security/RoomRoleFileSecurityFlagsTests.cs:86-100` (VDR PDF: Editor gets Edit, Viewer neither Edit nor FillForms) | TEST-CONFIRMED [run: PASS] (visibility, edit right) / CODE-ONLY (saving the edit through the document server) |
| Configure filling (assign participants): owner saves the role mapping | `RoomShareTests.cs:1068-1086` (`saveFormRoleMapping`, yaml 12847; `getAllFormRoles` 12930) | TEST-CONFIRMED [run: PASS] (owner) |
| Content creator configures the owner's form; Editor / Viewer / Form Filler cannot configure or stop | `01_Files/FormFilling/VdrFormRolesTests.cs:90-163` (`saveFormRoleMapping` by a Content creator on the owner's form saved; Editor, Viewer, Form Filler 403 for configure, form stays unconfigured, and 403 for `manageFormFilling` Stop) | TEST-CONFIRMED [run: PASS] |
| Stop filling of own forms: Content creator; cannot stop the owner's form even if they configured it | `deviations.md` matrix row "A Content creator stops filling of own forms and forms they organized / Only forms they created"; FFR analogue `01_Files/FormFilling/ManageFormFillingPermissionsTests.cs:88-145` | LIVE-CONFIRMED |
| Form Filler sees a configured form they take part in | `RoomShareTests.cs:1088-1096` | TEST-CONFIRMED [run: PASS] |
| Form Filler does not see a configured form they do not take part in; sees only own complete forms | `deviations.md` D5 evidence (`FileSecurity` PDF branch) and matrix row "A Form Filler sees a configured form only when their turn has come / Visible as soon as assigned" | LIVE-CONFIRMED |
| Form Filler before their turn: visible, filling not available | `deviations.md` matrix row above | LIVE-CONFIRMED |
| Viewer sees configured forms regardless of participation | `01_Files/FormFilling/VdrFormRolesTests.cs:70-88` | TEST-CONFIRMED [run: PASS] |
| Complete form is immutable (no role edits) | `form-filling-workflow` ("Completed forms") | CODE-ONLY (cross-reference) |
| Fill when the turn has come: ordered participants | `form-filling-workflow` ("Roles and the filling order") | CODE-ONLY (cross-reference) |

Counts: 35 rows — TEST-CONFIRMED 25 (3 of them owner-only; 4 with a part left CODE-ONLY: comment, edit, link
editing, form edit), CODE-ONLY 3 (all cross-references), LIVE-CONFIRMED 7, MATRIX-ONLY 0. Spec: 4 requirements,
21 scenarios (before the reshaping: 4 requirements, 18 scenarios).
Left CODE-ONLY, unreachable on the integration-test stand: third-party version history (needs a third-party
storage provider; `room-role-permissions`), complete-form immutability and the filling order (both need a
filling session in the document server; `form-filling-workflow`), and the parts that write through the document
server (a comment, a saved edit). No VDR test yet for the multi-step filling order.

Revision 2026-11-11: the CODE-ONLY role rows were covered by VDR role-matrix tests (one theory per action, a
per-test portal, Room manager = Room admin, other roles = Users): `03_Rooms/Permissions/VdrRoomManagementRolesTests.cs`,
`VdrRoomReadArchiveRolesTests.cs`, `01_Files/EditSession/VdrFileEditRolesTests.cs`,
`01_Files/History/VdrEditHistoryRolesTests.cs`, `01_Files/Versions/VdrChangeVersionHistoryRolesTests.cs`,
`01_Files/Create/VdrFileCreateRolesTests.cs`, `02_Folders/Upload/VdrUploadRolesTests.cs`,
`06_Operations/Copy/VdrCopyRolesTests.cs`, `04_Security/VdrLinkRolesTests.cs`,
`01_Files/FormFilling/VdrFormRolesTests.cs`; each class run alone, all passed. The server matched every row; the
spec text is unchanged.

## Where the server differs from the product matrix (spec follows the server)

| Matrix (sheet "VDR") | Server | Reference |
|---|---|---|
| A Form Filler sees a configured form only when their turn has come | Visible as soon as assigned; the turn governs filling | `deviations.md`, matrix table |
| A Content creator stops filling of own forms and forms they organized | Only forms they created | same |
| A Form Filler views only forms they take part in | Non-form files are refused; PDFs: see D5 | same |
| Rename: Owner and Room manager only | A Content creator renames own entries | same |
| Copy of other members' files: Owner and Room manager; copy out: Owner, Room manager, Content creator | Copy does not depend on authorship; every role copies out | same |
| Test titles in `01_Files/Editing` say "VDR Room" | The rooms are Custom rooms (`EditingTestBase.CreateRoomWithFile`); not VDR evidence | `01_Files/Editing/*` |

## Left out of the spec, and why

- **Link shape of a VDR** (no external links by default; folder / file primary links internal-only, access
  not editable; `FolderShareTests.cs:304-321`, `ShareInheritanceTests.cs:760-809`): `shared-links`.
- **Indexing / deny-download inherited by room templates, lifetime / watermark not**
  (`03_Rooms/Templates/RoomFromTemplateTests.cs:76-95`); **settings accepted and ignored on third-party
  rooms** (`03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:154-170`); **history entry on a deny-download
  change** (`02_Folders/History/FolderHistoryRoomSettingsTests.cs:314-332`): `room-types`.
- **`isformpdf` of an uploaded or document-server-converted form** (`01_Files/Forms/IsFormPdfTests.cs:112-168`;
  the conversion cases run since 4.1.0, the integration host has a document server): `form-filling-workflow`.
- **Start / stop / resume of filling as a lifecycle** (`manageFormFilling`): `form-filling-workflow`; this
  spec states only who may.

## Suspected defects

D-numbers are the ones of `openspec/deviations.md`; no new ones.

| # | Where | What the server does | Evidence | Status |
|---|-------|----------------------|----------|--------|
| D1 | PDF form filling (stop / resume) | `manageFormFilling` checks permissions only for Start and Stop; Resume on a stopped form and Edit on a started form answer 200 for a Viewer, a Form Filler without a role, and a non-member User or Guest. | `deviations.md` D1 (live run); `FileStorageService.cs:5861-5960` (Resume and Edit branches still without a permission demand) | STILL OPEN (2026-11-04: the method is untouched in the range) |
| D5 | Form Filler visibility | A Form Filler who holds no role on a form, or in a room where the form is not configured, cannot see it in the listing but reads it by id (`GET files/file/{id}` 200, presigned URI 200, open-edit 200). | `deviations.md` D5 (live run; `FileSecurity` PDF branch) | STILL OPEN (2026-11-04: the VDR PDF branch is untouched; the range changed only the Form filling room branch `FileSecurity.cs:1305-1318`) |
