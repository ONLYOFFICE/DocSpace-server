# vdr-room-roles — verification

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
only selects the role set (`products/ASC.Files/Core/Core/Security/FileSecurity.cs:217-225`, VDR entry of
`AvailableRoomAccesses`: Room manager, Content creator, Editor, Viewer, Form Filler). The theory data
`03_Rooms/RoomAccessData.cs:57-72, 150-152` (VDR access sets) exists but has no consumer. The message
"The role is not available for this user type" (`FilesCommonResource.resx:331`) is never asserted. Where
the Custom-room test is cited, its line range is given for the reader who wants the assertion.

Revision 2026-10-25: first verification; the spec was reshaped at the same time (the three matrices moved
into the scenarios "VDR room management matrix", "VDR file management matrix", "VDR form workflow matrix";
no requirement or scenario renamed).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **VDR role set** | | |
| Reviewer and Commentator → 403, member absent; Viewer, Editor, Form Filler, Content creator accepted | `03_Rooms/Sharing/RoomShareAccessLevelTests.cs:73-80, 104-114` | TEST-CONFIRMED |
| Room manager accepted in a VDR (for a Room admin) | `01_Files/Update/FileUpdateTests.cs:140-182` (Arrange) | TEST-CONFIRMED |
| **VDR room management by role** | | |
| Edit the room: owner changes indexing, deny-download, watermark, lifetime | `03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:46-168` (owner only) | TEST-CONFIRMED (owner) |
| Edit the room by role (Room manager yes, others 403) | Custom-room analogue `03_Rooms/Permissions/RoomUpdatePermissionsTests.cs:166-199` | CODE-ONLY |
| Invite / assign / change roles / remove: Room manager yes, lower roles 403 | Custom-room analogue `RoomSharePermissionsTests.cs:104-145, 196-242`; `FileSharing.cs:87-104` | CODE-ONLY |
| View member list / history / info by every role | Custom-room analogue `RoomReadPermissionsTests.cs:130-135`; `RoomShareReadPermissionsTests.cs:84-170` | CODE-ONLY |
| Archive / delete: owner only | Custom-room analogue `RoomArchivePermissionsTests.cs:87-101, 241-280`; `RoomDeletePermissionsTests.cs:187-230` | CODE-ONLY |
| **VDR files and folders by role** | | |
| Form Filler sees folders and no file before a form role is assigned, the form after | `04_Security/RoomShareTests.cs:1039-1097` (bug 80322) | TEST-CONFIRMED |
| Form Filler requesting a non-form file → 403 | `deviations.md` matrix row "A Form Filler views only forms they take part in / Non-form files are refused; see D5 for PDFs" | LIVE-CONFIRMED |
| Download by every role; deny-download restriction on a Viewer (download, view URL, copy to My documents → 403); Editor not restricted | `deviations.md` shared-links row "VDR: deny-download … the room setting still blocks the download"; the room flag itself `RoomUpdateVdrFieldsTests.cs:79-93`; `room-types` ("Room settings", `denyDownload`: members without editing rights cannot download or print) | LIVE-CONFIRMED |
| Comment: Viewer 403 | Custom-room analogue `04_Security/RoomShareTests.cs:605-672` (Viewer: comment flag false) | CODE-ONLY |
| Review / Edit: Editor yes, Form Filler and Viewer no | Custom-room analogue `01_Files/EditSession/EditSessionPermissionsTests.cs:64-125` | CODE-ONLY |
| View version history: Editor yes, Form Filler / Viewer no | Custom-room analogue `01_Files/History/EditHistoryPermissionsTests.cs:57-196` | CODE-ONLY |
| Create / upload: Content creator yes; Editor, Form Filler, Viewer 403 | Custom-room analogue `01_Files/Create/TextFileCreatePermissionsTests.cs:144-205`; `02_Folders/Upload/UploadPermissionsTests.cs:86-151` | CODE-ONLY |
| Copy from My documents: Content creator yes; Viewer / Editor 403; owner moves a docx into a VDR, destination check "all allowed" | `06_Operations/Move/FileMoveRoomTypeTests.cs:46-83` (VDR row); `06_Operations/CheckDestFolder/CheckDestFolderTests.cs:77-94`; Custom-room analogue `06_Operations/Copy/CopyPermissionsTests.cs:143-161, 255-300` | TEST-CONFIRMED (owner) / CODE-ONLY (roles) |
| Lock: Room manager / Content creator lock own file; Editor, Form Filler, Viewer 403 (VDR); Room manager unlocks another manager's lock; Content creator cannot unlock another Content creator's lock | `01_Files/Update/FileUpdateTests.cs:140-280` (runs on a VDR) | TEST-CONFIRMED |
| Manage version history: Content creator and above | Custom-room analogue `01_Files/Versions/ChangeVersionHistoryPermissionsTests.cs:98-216` | CODE-ONLY |
| Delete / move / rename own vs others' (Content creator own only; Room manager any) | Custom-room analogues `01_Files/Deletion/FileDeletePermissionsTests.cs:111-189`; `06_Operations/Move/FileMovePermissionsTests.cs:221-315`; `01_Files/Update/FileUpdatePermissionsTests.cs:82-204`; `02_Folders/Crud/FolderRenamePermissionsTests.cs:46-53, 117-132`; `deviations.md` matrix rows "Rename …" and "Copy of other members' files …" (VDR listed) | LIVE-CONFIRMED |
| Copy out of the room: every role | `deviations.md` row above | LIVE-CONFIRMED |
| Copy within the room: Content creator | Custom-room analogue `CopyPermissionsTests.cs:327-347` | CODE-ONLY |
| File and folder links: owner's folder / file primary link in a VDR is internal-only, access not editable; external room link → 403 | `04_Security/FolderShareTests.cs:304-321`; `04_Security/ShareInheritanceTests.cs:760-809`; `03_Rooms/Links/RoomLinkReadTests.cs:352-363`; `04_Security/RoomShareTests.cs:115-133`; role split: `shared-links` | TEST-CONFIRMED (shape) / CODE-ONLY (role split) |
| Third-party version history unavailable | `room-role-permissions` row | CODE-ONLY (cross-reference) |
| **PDF form filling in a VDR** | | |
| Create / upload PDF forms: owner (Arrange) | `RoomShareTests.cs:1049` | TEST-CONFIRMED (owner) |
| Form Filler does not see a not-configured form | `RoomShareTests.cs:1058-1066` | TEST-CONFIRMED |
| Viewer / Editor see not-configured forms; Editor edits them | read / edit rights of the role; `FileSecurity` PDF branch (`deviations.md` D5 evidence) | CODE-ONLY |
| Configure filling (assign participants): owner saves the role mapping | `RoomShareTests.cs:1068-1086` (`saveFormRoleMapping`, yaml 12847; `getAllFormRoles` 12930) | TEST-CONFIRMED (owner) |
| Content creator configures the owner's form; Editor / Viewer / Form Filler cannot configure or stop | `yaml` `saveFormRoleMapping` (12847) remarks; `manageFormFilling` (13046) | CODE-ONLY |
| Stop filling of own forms: Content creator; cannot stop the owner's form even if they configured it | `deviations.md` matrix row "A Content creator stops filling of own forms and forms they organized / Only forms they created"; FFR analogue `01_Files/FormFilling/ManageFormFillingPermissionsTests.cs:88-145` | LIVE-CONFIRMED |
| Form Filler sees a configured form they take part in | `RoomShareTests.cs:1088-1096` | TEST-CONFIRMED |
| Form Filler does not see a configured form they do not take part in; sees only own complete forms | `deviations.md` D5 evidence (`FileSecurity` PDF branch) and matrix row "A Form Filler sees a configured form only when their turn has come / Visible as soon as assigned" | LIVE-CONFIRMED |
| Form Filler before their turn: visible, filling not available | `deviations.md` matrix row above | LIVE-CONFIRMED |
| Viewer sees configured forms regardless of participation | read right of the Viewer; `FileSecurity` PDF branch | CODE-ONLY |
| Complete form is immutable (no role edits) | `form-filling-workflow` ("Completed forms") | CODE-ONLY (cross-reference) |
| Fill when the turn has come: ordered participants | `form-filling-workflow` ("Roles and the filling order") | CODE-ONLY (cross-reference) |

Counts: 35 rows — TEST-CONFIRMED 11 (3 of them owner-only), CODE-ONLY 17 (2 cross-references), LIVE-CONFIRMED 7,
MATRIX-ONLY 0. Spec: 4 requirements, 21 scenarios (before the reshaping: 4 requirements, 18 scenarios).
Flag: no VDR-specific test exists for any lower-role refusal of a room-level action, for the deny-download
restriction against a Viewer, or for the multi-step filling order; all of those rest on the Custom-room
analogue, the live run or the code.

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
- **`isformpdf` of an uploaded form** (`01_Files/Forms/IsFormPdfTests.cs:115-128`): `form-filling-workflow`.
- **Start / stop / resume of filling as a lifecycle** (`manageFormFilling`): `form-filling-workflow`; this
  spec states only who may.

## Suspected defects

D-numbers are the ones of `openspec/deviations.md`; no new ones.

| # | Where | What the server does | Evidence | Status |
|---|-------|----------------------|----------|--------|
| D1 | PDF form filling (stop / resume) | `manageFormFilling` checks permissions only for Start and Stop; Resume on a stopped form and Edit on a started form answer 200 for a Viewer, a Form Filler without a role, and a non-member User or Guest. | `deviations.md` D1 (live run) | OPEN |
| D5 | Form Filler visibility | A Form Filler who holds no role on a form, or in a room where the form is not configured, cannot see it in the listing but reads it by id (`GET files/file/{id}` 200, presigned URI 200, open-edit 200). | `deviations.md` D5 (live run; `FileSecurity` PDF branch) | OPEN |
