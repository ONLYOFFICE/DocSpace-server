# form-filling-workflow — verification

One row per requirement or scenario of `spec.md`. Status: **TEST-CONFIRMED** — an integration test in
`products/ASC.Files/Tests/Tests/` asserts it; **CODE-ONLY** — read from the product code and/or the published
OpenAPI document (`sdk/docspace-api-spec/docspace-backend.yaml`, cited by `operationId`), no test exercises it;
**NOTES-ONLY** — stated by the release notes and consistent with the code, but neither test nor a code path
pins the exact wording. Test references name the file and the test method; code references name the file and
the method (line of the method head as of 2026-10-14). Paths are relative to the repo root.

The integration host (`integration-test` launch profile) provisions no document server, so every behaviour
that starts in the editors — a submitted copy landing in "Complete", the results spreadsheet, the external
database export, a VDR step being submitted, the `form.submit`/`form.filled.out` webhooks, the anonymous
submit-only session — has no test and is CODE-ONLY here. The `FormsTestBase` remark records that
`SaveFileAsPdf` and `CopyFileAs` with `toForm` were ruled out for the same reason, and that a genuine form is
obtained by uploading the embedded `new.pdf` asset.

| Claim | Source | Status |
|---|---|---|
| **PDF form identification** — every `.pdf` is fillable (`isForm`), form vs document decided by content | `products/ASC.Files/Core/Core/Entries/File.cs` `IsPdf`, `IsPdfForm`, `FilterType` getter; `products/ASC.Files/Core/Utils/FileChecker.cs` `CheckExtendedPDFstream` (57), `IsFormPDFFile`; yaml `isFormPDF` | CODE-ONLY |
| Uploaded ONLYOFFICE form answers `true` | `01_Files/Forms/IsFormPdfTests.cs` `IsFormPDF_UploadedOnlyofficeForm_ReturnsTrue` | TEST-CONFIRMED |
| Blank PDF created on the portal is a form (`true`) | `01_Files/FormFilling/FormFilesTests.cs` `IsFormPDF_RegularPdf_ReturnsFalse` (asserts `true`); `FormFillingTestBase.CreateFormInRoom` remark | TEST-CONFIRMED |
| Ordinary PDF, DOCX, DOCXF answer `false` | `IsFormPdfTests.cs` `IsFormPDF_NonOnlyofficePdfFile_ReturnsFalse`, `IsFormPDF_RegularDocxFile_ReturnsFalse`, `IsFormPDF_DocxfFormTemplate_ReturnsFalse` | TEST-CONFIRMED |
| Access to the check: 403 non-member, 401 anonymous, 404 unknown, read-only member allowed, admin allowed | `01_Files/Forms/IsFormPdfPermissionsTests.cs` (all five tests); `IsFormPdfTests.cs` `IsFormPDF_NonExistentFile_Returns404` | TEST-CONFIRMED |
| `filterType=PdfForm` / `filterType=Pdf` split in Shared with me | `01_Files/Forms/SharedWithMePdfFilterTests.cs` `GetSharedWithMe_PdfFormFilter_ReturnsOnlyPdfForms`, `GetSharedWithMe_PdfFilter_ReturnsOnlyPlainPdfs` (bug 81919, fixed) | TEST-CONFIRMED |
| **Filling state** — `startFilling` on the properties and the file; `formFillingStatus` `None` outside a VDR | `products/ASC.Files/Core/Core/Entries/EntryProperties.cs` `FormFillingProperties`; yaml `FileDto.startFilling`, `FileDto.formFillingStatus` | CODE-ONLY |
| Not-started form hidden from form fillers, visible after Start | `products/ASC.Files/Core/Core/Security/FileSecurity.cs` `FilterEntryAsync` (from 1168), FFR `FillForms` share branch reading `FormFilling.StartFilling`; the listing/404 assertions are in `openspec/specs/form-filling-room-roles` ("Form Filler does not see a form that is not started") | CODE-ONLY (listing confirmed by the roles spec's live run) |
| State read from the file | yaml `FileDto.startFilling`; `FileDto.formFillingStatus` description ("worked out only inside a virtual data room") | CODE-ONLY |
| **Form filling actions** — endpoint, action values, 200 without body | `products/ASC.Files/Server/Api/FilesController.cs` `ManageFormFilling` (1176); `products/ASC.Files/Core/Core/FileStorageService.cs` `ManageFormFilling` (5719); yaml `manageFormFilling`, `FormFillingManageAction`; `01_Files/FormFilling/ManageFormFillingTests.cs` remark ("returns no body") | TEST-CONFIRMED (shape), CODE-ONLY (per-action effects) |
| Start: `startFilling` true, `startedByUserId`, history "FormStartedToFill", socket create for form fillers, draft links dropped when version changed | `FileStorageService.ManageFormFilling` case `Start` | CODE-ONLY |
| Stop: `startFilling` false, form fillers lose the form, history "FormStopped", `form.stopped` webhook | `FileStorageService.ManageFormFilling` case `Stop` | CODE-ONLY |
| Edit: same as Stop plus `OriginalFormVersion = form.Version`; webhook/history only when it was started | `FileStorageService.ManageFormFilling` case `Edit` | CODE-ONLY |
| Resume: clears `FillingStopedDate` and `FormFillingInterruption` | `FileStorageService.ManageFormFilling` case `Resume` | CODE-ONLY |
| Release notes v3.7 wording on Start filling / Edit | `nct2__Изменения к выпуску v3.7.0.md` lines 149–152 | NOTES-ONLY (behaviour itself CODE-ONLY above) |
| Start and stop by the owner answer 200 | `ManageFormFillingTests.cs` `ManageFormFilling_OwnerStartsAndStopsFormFilling`; `ManageFormFillingPermissionsTests.cs` (five tests, incl. bug 81470) | TEST-CONFIRMED |
| Not a PDF / outside a room / unknown file → 403 | `FileStorageService.ValidateChangeRolesPermission` (throws `InvalidOperationException`), `FilesController.ManageFormFilling` `[SwaggerResponse(403, …)]`; `FormFilesTests.cs` `ManageFormFilling_ValidAction_ExecutesSuccessfully` only tolerates 400/403/404/501 | CODE-ONLY |
| Resume of a never-started form → 500 | `FilesController.ManageFormFilling` `[SwaggerResponse(500, …)]`; `ManageFormFilling` dereferences `properties.FormFilling` | CODE-ONLY |
| `openedit?edit=true` in an FFR applies the Edit action | `products/ASC.Files/Server/Api/EditorController.cs` `OpenEditFile` (243), `if (inDto.Edit && rootFolder.FolderType == FolderType.FillingFormsRoom) ManageFormFilling(file.Id, Edit)` | CODE-ONLY |
| **Start filling endpoint** — writes properties for an FFR PDF, no-op otherwise, 403 for FillForms-only member and for non-members who cannot manage the room | `FileStorageService.StartFillingAsync` (2216); `EditorController.StartFillingFile` (185); yaml `startFillingFile` | CODE-ONLY |
| FFR PDF → 200 with the form | `01_Files/FormFilling/StartFillingFileTests.cs` `StartFillingFile_FormInFillingFormsRoom_ReturnsForm` | TEST-CONFIRMED |
| DOCX in My documents / PDF in My documents → 200 unchanged | `StartFillingFileTests.cs` `StartFillingFile_RegularDocxFile_ReturnsFile`, `StartFillingFile_FormOutsideFillingFormsRoom_ReturnsForm` | TEST-CONFIRMED |
| 404 unknown, 401 anonymous, 403 non-member User/Guest (bug 81400), admin 200 | `StartFillingFileTests.cs` `StartFillingFile_NonExistentFileId_ReturnsNotFound`; `StartFillingFilePermissionsTests.cs` (four tests) | TEST-CONFIRMED |
| **Move/copy** — PDF arriving in an FFR is started; leaving an FFR/VDR drops properties, draft links, roles; `toFillOut` sets `copyToFillOut` | `products/ASC.Files/Core/Services/WCFService/FileOperations/FileMoveCopyOperation.cs` `MoveOrCopyFilesAsync` (656): the `file.IsPdf` block with `toRoom`/`fromRoom`, the `_toFillOut` blocks | CODE-ONLY |
| Move from My documents into an FFR starts filling and shows it to form fillers | `openspec/specs/form-filling-room-roles` "Moving a PDF into the room starts collecting" (live run); `FileMoveCopyOperation` as above | CODE-ONLY (live-run confirmed in the roles spec) |
| Move out of the room clears properties and links | `FileMoveCopyOperation` `fromRoom?.FolderType is FillingFormsRoom … SaveProperties(file.Id, null)`, `DeleteAllLinkAsync` | CODE-ONLY |
| Release notes v3.1 "after copying a PDF form through Share into a VDR, a dialog offers to assign roles" | `nct2__Изменения к выпуску v3.1.0.md` line 105 | NOTES-ONLY (server side is the `copyToFillOut` flag) |
| **Opening in an FFR** — `StartFillingFormRoom` mode, `startFilling` flag, 403 for a not-started form unless the caller may start it, draft session id, read-only while the original is being edited, anonymous submit-only session, In process / Complete setups | `products/ASC.Files/Core/Services/DocumentService/DocumentServiceHelper.cs` `GetFormOpenSetupForFillingRoomAsync` (590), `GetFormOpenSetupForFolderInProgress`, `GetFormOpenSetupForFolderDone`; `EditorController.OpenEditFile` 336–358; yaml `StartFillingMode`, `ConfigurationDto.startFilling/startFillingMode/fillingSessionId` | CODE-ONLY |
| Not-started form, caller cannot start → 403 | `GetFormOpenSetupForFillingRoomAsync`: `if (!CanStartFillingAsync) throw SecurityException` | CODE-ONLY |
| Started form for a filler → draft, embedded, session id | `GetFormOpenSetupForFillingRoomAsync` tail (`FillingSessionId = $"{formDraft.Id}_{userId}"`) | CODE-ONLY |
| Anonymous visitor on a not-started form → 403 | `GetFormOpenSetupForFillingRoomAsync`: Guest branch `if (StartFilling != true) throw SecurityException` | CODE-ONLY |
| **Filling draft** — URL rules, draft title, folder, link, new mark removed, discard notice, link key accepted, 401 otherwise | `FileStorageService.CheckFillFormDraftAsync` (3488); `products/ASC.Files/Core/Utils/EntryManager.cs` `GetFillFormDraftAsync` (1304), `CheckFillFormDraftAsync` (1455), `LinkedForMeAsync`; resources `MessageFillFormDraftCreated`, `MessageFillFormDraftDiscard`; yaml `checkFillFormDraft` | CODE-ONLY |
| Owner gets `/doceditor` URL with `fileid`, with and without `requestView` | `01_Files/FormFilling/CheckFillFormDraftTests.cs` `CheckFillFormDraft_ValidDraft_ReturnsEditorUrl`, `CheckFillFormDraft_RequestView_ReturnsEditorUrl` | TEST-CONFIRMED |
| 401 anonymous (bug 81441), 403 non-member User/Guest, 404 unknown | `01_Files/FormFilling/CheckFillFormDraftPermissionsTests.cs` (three tests); `CheckFillFormDraftTests.cs` `CheckFillFormDraft_NonExistentFileId_ReturnsNotFound` | TEST-CONFIRMED |
| Obsolete draft refused on submission | `EntryManager.SubmitFillingRoomFormAsync` (2283): `ErrorMessage_FillFormDraftObsolete`; `EntryManager.CheckFillFormDraftAsync` version check | CODE-ONLY |
| **Result folders** — "In process" (26) / "Complete" (25), per-form 28 / 27 subfolders, ids on the form | `EntryManager.InitSystemFormFillingFolders`, `InitFormFillingFolders`, `InitFormFillingProperties` (2261), `CreateFormFillingFolder`; `products/ASC.Files/Core/Resources/FilesUCResource.resx` `InProcessFormFolder` = "In process", `ReadyFormFolder` = "Complete"; yaml `FolderType` enum | CODE-ONLY |
| Folder immutability rules, Guest cannot read, no copy link, file restrictions inside, draft fillable by creator only, completed copy neither filled nor edited | `FileSecurity.FilterEntryAsync`: folder block (`ReadyFormFolder`/`InProcessFormFolder`/`FormFillingFolderDone`/`FormFillingFolderInProgress`), `IsCompletedForm && Edit → false`, `FillForms` in `FormFillingFolderInProgress` (creator only) / `FormFillingFolderDone` → false, file-action block `Rename/Lock/Move/Duplicate/EditHistory/SubmitToFormGallery/Embed` in 27/28 → false | CODE-ONLY |
| Folders appear with the first draft, draft in the In process subfolder | `EntryManager.GetFillFormDraftAsync` (creates the system folders when both are missing, then the per-form folder) | CODE-ONLY |
| Submitted copy read-only (`security.Edit`/`FillForms` false, read-only editor) | `FileSecurity.FilterEntryAsync` as above; `DocumentServiceHelper.GetFormOpenSetupForFolderDone` (`CanEdit = false, CanFill = false`); `openspec/specs/vdr-room-roles` "Complete form is immutable" | CODE-ONLY |
| System folder cannot be renamed or deleted (403) | `FileSecurity.FilterEntryAsync` folder block (`Delete` on 25/26 → false; `Rename` on 25–28 → false) | CODE-ONLY |
| **Submission** — copy name `{N} - {title} (dd-MM-yyyy H-mm).pdf`, `ResultFormNumber` under a lock, `PdfForm` category, `IsCompletedForm`, properties on the copy, draft unlinked and removed from Recent, new mark for non-fillers, "form submitted" notification, anonymous path with links re-applied, Complete subfolder recreated | `EntryManager.SubmitFillingRoomFormAsync` (2283) through the anonymous `SetAceObjectAsync` block; `products/ASC.Files/Core/Services/DocumentService/DocumentServiceTracker.cs` 540–565 (user-submit forced save) | CODE-ONLY |
| `saveediting` with a binary replaces content, bumps the version, keeps `isForm` | `01_Files/FormFilling/SaveEditingFileFromFormTests.cs` `SaveEditingFileFromForm_SubmittedFormBinary_SavesNewVersion`, `SaveEditingFileFromForm_Forcesave_SavesImmediately` | TEST-CONFIRMED |
| `saveediting` 401 / 403 / 404 without exception leak (bug 81416) | `01_Files/FormFilling/SaveEditingFileFromFormPermissionsTests.cs` (four tests); `SaveEditingFileFromFormTests.cs` `SaveEditingFileFromForm_NonExistentFileId_ReturnsNotFoundWithoutLeakingException` | TEST-CONFIRMED |
| Submission numbering 1, 2, 3 | `EntryManager.SubmitFillingRoomFormAsync`: `ResultFormNumber++` then the title format | CODE-ONLY |
| **Results spreadsheet** — `saveFormAsXLSX`, `{title}.xlsx` in the Complete subfolder, created on first submission or first report, rewritten in place, CSV replaced | `EntryManager.SubmitFillingRoomFormAsync` (`room.SettingsSaveFormAsXLSX` branches), `CreateFillResultsFile`, `EnsureFormFillingOutputAsync`; `products/ASC.Files/Core/Helpers/FormFillingReportCreator.cs` `UpdateFormFillingReport` (54); `products/ASC.Files/Core/Utils/ExportToXLSX.cs` `UpdateXlsxReport`; yaml `saveFormAsXLSX` (room create/update DTOs) | CODE-ONLY |
| `POST file/{id}/xlsx` accepts form id or xlsx/csv result id; conditions; answer shape; `GET` reports the caller's own run or nothing; folder variant; `GET submissions` | `FileStorageService.GenerateXlsxAsync` (6085), `GenerateXlsxByFolderAsync` (6186), `GetSubmissionsByFormId`; `EditorController.GenerateXlsx` (531); `FoldersController.GenerateXlsxByFolder` (268); yaml `generateXlsx`, `generateXlsxByFolder`, `getFormSubmissions`, `XlsxReportResponseDto` | CODE-ONLY |
| Report for a not-started form / copy / non-FFR room → 403 | `GenerateXlsxAsync`: `StartFilling != true || OriginalFormId != form.Id → InvalidOperationException`; `room.FolderType != FillingFormsRoom → InvalidOperationException`; yaml `generateXlsx` 403 text | CODE-ONLY |
| Report addressed through the results file | `GenerateXlsxAsync` `.xlsx`/`.csv` branch (parent must be `FormFillingFolderDone`, `OriginalFormId` resolved) | CODE-ONLY |
| Status before any run → 200 empty | yaml `GET …/xlsx` description ("Nothing at all comes back when no run is on record") | CODE-ONLY |
| Release notes v3.7 "Update XLSX data" | `nct2__Изменения к выпуску v3.7.0.md` line 155 | NOTES-ONLY (name of the feature) |
| **External database export** — `sendFormToExternalDB`, per-submission push, table name stored, no-op without the setting or a configured client, sync endpoints, one job per room, per-form outcomes, 400 without a database, spreadsheet generation re-syncs | `FormFillingReportCreator.UpdateFormFillingReport`, `SyncExternalDbAsync` (69), `PublishExternalDbSubmissionAsync` (75), `ExportToExternalDbAsync` (87), `SetExternalDbTableNameAsync`; `FileStorageService.StartExternalDbSyncAsync` (6268), `GetExternalDbSyncTaskAsync`; `GenerateXlsxAsync` calls `SyncExternalDbAsync`; yaml `startExternalDbSync`, `getExternalDbSyncStatus`, `ExternalDbSyncTaskDto`, `ExternalDbSyncFormResultDto`, `sendFormToExternalDB` | CODE-ONLY |
| Sync for a Custom room → 403 | `StartExternalDbSyncAsync`: `room.FolderType != FillingFormsRoom → InvalidOperationException` | CODE-ONLY |
| Status without a job → 200 empty | yaml `getExternalDbSyncStatus` ("or an empty body when the room has no job") | CODE-ONLY |
| Release notes v3.7 "Connect Database" and the Integration setting | `nct2__Изменения к выпуску v3.7.0.md` lines 114, 154 | NOTES-ONLY (setting UI; server side CODE-ONLY above) |
| **Form fields filter** — reads the index, feeds `formsItemKey`/`formsItemType`, empty list otherwise, no authorization | yaml `getFolder` (`/files/{folderId}/formfilter`); `FormFillingReportCreator.GetFormsFields` (255) | CODE-ONLY |
| Empty folder, ordinary files, Recent, Favorites, deleted folder, unknown id → 200 empty array | `02_Folders/FormFilter/FormFilterTests.cs` (seven tests) | TEST-CONFIRMED |
| Read-only member and owner of another member's folder → 200 | `02_Folders/FormFilter/FormFilterPermissionsTests.cs` (two tests) | TEST-CONFIRMED |
| **Fill result** — anonymous, fields, cache mapping, `roomId` omitted for a file link | `FilesController.GetFillResult` (468, `[AllowAnonymous]`, `hybridCache.GetOrDefaultAsync`); `products/ASC.Files/Server/ApiModels/ResponseDto/FillingFormResultDto.cs` `FillingFormResultDtoHelper.GetAsync` (86); `EntryManager.SubmitFillingRoomFormAsync` `hybridCache.SetAsync(fillingSessionId, result.Id)`; yaml `getFillResult` | CODE-ONLY |
| Missing or empty session id → 400 | `01_Files/Forms/FillResultTests.cs` `GetFillResult_NoFillingSessionId_Returns400`, `GetFillResult_EmptyFillingSessionId_Returns400` | TEST-CONFIRMED |
| Random UUID, invalid format, 100 chars, anonymous unknown → 404 "The record could not be found" | `FillResultTests.cs` `GetFillResult_NonExistentFillingSessionId_Returns404`, `GetFillResult_InvalidUuidFormat_Returns404`, `GetFillResult_VeryLongFillingSessionId_Returns404`, `GetFillResult_Unauthenticated_Returns404` | TEST-CONFIRMED |
| **VDR role-based filling** — `formrolemapping` replaces the set, starts filling, notifies, reveals to form fillers, empty list resets | `FileStorageService.SaveFormRoleMapping` (5607); `FilesController.SaveFormRoleMapping` (1118); yaml `saveFormRoleMapping`, `FormRoleRequest`, `SaveFormRoleMappingDto` | CODE-ONLY |
| `formroles` fields and `roleStatus` mapping; empty list when never configured; PDF or completed copy only | `FileStorageService.GetAllFormRoles` (5673) and its `currentStep` switch; `products/ASC.Files/Core/Core/Entries/FormFillingStatus.cs`; yaml `getAllFormRoles`, `FormRoleDto`, `FormFillingStatus` | CODE-ONLY |
| Step submission: role submitted with time, last step completes the form (`isCompletedForm`, `form.filled.out`, history, notification), otherwise "your turn"; Stop records interruption and blocks submission until Resume | `EntryManager.SubmitVDRFormAsync` (≈2500–2570), `FormFilledOut` publish at 2561; `FileStorageService.ManageFormFilling` case `Stop` (VDR branch); `DocumentServiceHelper.GetFormOpenSetupForVirtualDataRoomAsync` (sets `OpenedAt`) | CODE-ONLY |
| Removing a role holder answers `FormRoleBlockingDeletion` unless `force` | `products/ASC.Files/Server/Api/VirtualRoomsController.cs` ≈769 (`ShouldPreventUserDeletion`); yaml `RoomSecurityError` | CODE-ONLY |
| Roles of an unconfigured form in a Custom room / My documents / archived room → 200 `[]` | `01_Files/Forms/FormRolesTests.cs` `GetAllFormRoles_OnlyofficeForm_ReturnsEmptyArray`, `GetAllFormRoles_OnlyofficeFormInMyDocuments_Returns200`, `GetAllFormRoles_OnlyofficeFormInArchivedRoom_Returns200` | TEST-CONFIRMED |
| Roles of a DOCX / DOCXF → 403 | `FormRolesTests.cs` `GetAllFormRoles_RegularDocxFile_Returns403`, `GetAllFormRoles_DocxfFormTemplate_Returns403` | TEST-CONFIRMED |
| Roles access: read-only / content creator / room manager / admin 200; non-member User 403, Guest 403 (bug 81348), anonymous 401, unknown or id 0 404 (bug 81346) | `01_Files/Forms/FormRolesPermissionsTests.cs` (eight tests); `FormRolesTests.cs` `GetAllFormRoles_FileIdZero_Returns404`, `GetAllFormRoles_NonExistentFile_Returns404` | TEST-CONFIRMED |
| Statuses along the queue (Complete / InProgress / Draft / YouTurn) | `FileStorageService.GetAllFormRoles` switch; yaml `FormRoleDto.roleStatus` | CODE-ONLY |
| Reset with an empty role list | `FileStorageService.SaveFormRoleMapping`: `roles?.Any() == false → SaveProperties(formId, null)` | CODE-ONLY |
| Release notes v3.1 "filling by roles in a VDR" | `nct2__Изменения к выпуску v3.1.0.md` lines 86–88 | NOTES-ONLY (feature name) |
| **Sharing to fill out outside a form room** — `ShareToFillOut` mode when the caller may copy and is signed in; `startFilling` true only for the creator in My documents | `EditorController.OpenEditFile` 351–356; `DocumentServiceHelper.GetFormOpenSetupForUserFolderAsync` (creator → `CanStartFilling = true`, others false), `GetFormOpenSetupForPublicRoomAsync` | CODE-ONLY |
| Quick share with the fill right only in My documents / FFR / VDR; refused in Public, Custom, Collaboration rooms (403 "role not available") | `openspec/specs/user-type-permissions` "Quick share of a form from My documents", "Form in a Collaboration room is not shared for filling" (live run); release notes v3.7.1 line 27 | CODE-ONLY (owned by the other spec; not re-verified here) |
| Own form in My documents → `ShareToFillOut`, `startFilling` true | `GetFormOpenSetupForUserFolderAsync` `file.CreateBy == current → CanStartFilling = true`; `OpenEditFile` 351–356 | CODE-ONLY |
| Form in a Custom room → `ShareToFillOut`, `startFilling` false | `OpenEditFile` default `FormOpenSetup` (`CanStartFilling` default false); 351–356 | CODE-ONLY |
| Release notes v3.1 items 13 and 17 (Share to fill out, link created with the Fill right) | `nct2__Изменения к выпуску v3.1.0.md` lines 89–92, 98–104 | NOTES-ONLY (client flow; the server offers the mode and the link right) |
| **Form room link** — one auto-created primary external link, no additional external links | `03_Rooms/Links/RoomLinkPrimaryTests.cs` `GetPrimaryLink_FillingFormsRoom_ReturnsAutoCreatedPrimaryLink`; `03_Rooms/Links/RoomLinkReadTests.cs` `GetRoomLinks_FillingFormsRoom_HasOneAutoCreatedExternalLinkByDefault`; live run 2026-10-04 (second external link → 403 "exceed 0"). Note: `SetRoomLink_FillingFormsRoom_RejectsInvitationLinkCreation` sends access Read, and its 403 is the role check — an invitation link with FillForms/ContentCreator access IS created (see `invitations`) | TEST-CONFIRMED |
| Anonymous visitor resolves the link: 200, `isRoom`, `entityId` | `03_Rooms/Links/RoomLinkAnonymousAccessTests.cs` `GetExternalShareData_AnonymousVisitor_PrimaryLinkOfRoom_ShouldReportShared` — the test is **skipped** (its `shared` assertion no longer describes the product; the `isRoom`/`entityId` half is stated in its remark as true regardless) | CODE-ONLY (test skipped; the remark documents the server shape) |
| Account visits the link: `external` true, room listed under `searchArea=Forms` (bug 83228, fixed) | `03_Rooms/FormFilling/FormFillingRoomSectionTests.cs` `FormRoomOpenedViaExternalLink_AppearsInFormsSection` | TEST-CONFIRMED |
| **Webhooks** — `form.submit`, `form.filled.out`, `form.stopped`; audiences; when each fires | `common/ASC.Webhooks.Core/WebhookTrigger.cs` 212–227; `products/ASC.Files/Core/Utils/WebhookManager.cs` `PublishAsync` (59, `FormSubmit` payload resolved to the original form, nothing published when there is none); `DocumentServiceTracker.cs` 561 (`isUserSubmit ? FormSubmit : FileUpdated`); `EntryManager.cs` 2561 (`FormFilledOut`); `FileStorageService.ManageFormFilling` `Stop` and `Edit` (`FormStopped`) | CODE-ONLY |
| Stop and Edit-of-started emit `form.stopped`; Edit of a not-started form emits nothing | `FileStorageService.ManageFormFilling`: `Stop` always, `Edit` only `if (wasFilling)` | CODE-ONLY |
| Release notes v3.7: the three events and their Developer Tools checkboxes | `nct2__Изменения к выпуску v3.7.0.md` lines 153, 181–187 | NOTES-ONLY (names; emission points CODE-ONLY above) |
| **Forms section** — `@forms`, Forms root (36), `searchArea=Forms`; excluded from Active and from the Rooms root even with `type=FillingFormsRoom` | `03_Rooms/FormFilling/FormFillingRoomSectionTests.cs` (eleven tests); `products/ASC.Files/Core/Core/VirtualRooms/SearchArea.cs` `MatchesRoomType`; yaml `getFormsFolder`, `SearchArea` | TEST-CONFIRMED |
| Forms root id consistent between `@root` and `@forms` | `FormFillingRoomSectionTests.cs` `RootFolders_ContainDedicatedFormsSection`, `FormsRootFolderId_IsConsistentAcrossEndpoints` | TEST-CONFIRMED |
| Form templates under `FormTemplates` only; regular templates under `Templates` only; templates root browse excludes form templates; room from a form template is an FFR in the Forms section | `03_Rooms/FormFilling/FormFillingRoomTemplateSectionTests.cs` (six tests) | TEST-CONFIRMED |
| Used space counted in `formsUsedSpace`, moves to Archive and back, released on delete; `roomsUsedSpace` untouched | `03_Rooms/FormFilling/FormFillingRoomUsedSpaceTests.cs` (four tests) | TEST-CONFIRMED |
| Release notes v4.0: `/forms` route, Forms block with its own Recent/Favorites/Trash, `searchArea=FormTemplates` | `nct2__Изменения к выпуску v4.0.0.md` lines 93–94 | NOTES-ONLY (the Recent/Favorites/Trash sub-sections of Forms are client views; see "left out" below) |

### Scenarios added in the 2026-10-25 clean-up, and the code names moved out of the spec text

The spec now has 21 requirements and 82 scenarios: "Results spreadsheet" was split into "Results spreadsheet"
and "Results report generation", and "Role-based filling in a Virtual Data Room" into that requirement, "Form
roles listing" and "Step submission and interruption in a Virtual Data Room"; the existing scenarios kept their
names and rows. The rows below cover the scenarios that are new; the claims themselves were already in the
rows above, which stay the primary evidence.

| Scenario (requirement) | Source | Status |
|---|---|---|
| Start action / Stop action / Edit action / Resume action (Form filling actions) — the `action` values are `FormFillingManageAction` `0` Stop, `1` Resume, `2` Start, `3` Edit; "the account that started the filling" is `startedByUserId`; the interruption record is `fillingStopedDate` + `FormFillingInterruption`; the remembered version is `OriginalFormVersion` | `FileStorageService.ManageFormFilling` (5719), cases `Start`/`Stop`/`Edit`/`Resume`; yaml `FormFillingManageAction` | CODE-ONLY |
| Form in a Form filling room (Start filling endpoint) — the stored properties are `roomId`, `title`, `startedByUserId`, `originalFormId`, `startFilling` | `FileStorageService.StartFillingAsync` (2216); `StartFillingFileTests.cs` `StartFillingFile_FormInFillingFormsRoom_ReturnsForm` | TEST-CONFIRMED (200 + form), CODE-ONLY (property set) |
| Copy to fill out (Filling state follows a move or copy) — the flag is `copyToFillOut` | `FileMoveCopyOperation.cs` `MoveOrCopyFilesAsync` `_toFillOut` blocks; `nct2__Изменения к выпуску v3.1.0.md` line 105 | CODE-ONLY / NOTES-ONLY (dialog) |
| Anonymous visitor on a started form (Opening in an FFR) | `DocumentServiceHelper.GetFormOpenSetupForFillingRoomAsync` (590), Guest branch | CODE-ONLY |
| Forms in the result folders (Opening in an FFR) | `DocumentServiceHelper.GetFormOpenSetupForFolderInProgress`, `GetFormOpenSetupForFolderDone` | CODE-ONLY |
| Draft created for a filler (Filling draft) | `EntryManager.GetFillFormDraftAsync` (1304); resource `MessageFillFormDraftCreated` | CODE-ONLY |
| Invalid draft (Filling draft) | `EntryManager.CheckFillFormDraftAsync` (1455), `LinkedForMeAsync`; resource `MessageFillFormDraftDiscard` | CODE-ONLY |
| System folders are immutable / Files inside the per-form folders (Result folders) — the folder types are `InProcessFormFolder` 26, `ReadyFormFolder` 25, `FormFillingFolderInProgress` 28, `FormFillingFolderDone` 27; the ids on the form are `toFolderId` and `resultsFolderId` | `FileSecurity.FilterEntryAsync` folder and file-action blocks; `EntryManager.InitFormFillingProperties` (2261); yaml `FolderType` | CODE-ONLY |
| What a submission records / Anonymous submission / Complete subfolder missing (Submission) — the counter is `resultFormNumber` | `EntryManager.SubmitFillingRoomFormAsync` (2283) through the anonymous `SetAceObjectAsync` block; `DocumentServiceTracker.cs` 540–565 | CODE-ONLY |
| Results file rewritten in place / Setting off (Results spreadsheet) — the results file id on the form is `resultsFileID` | `EntryManager.SubmitFillingRoomFormAsync` (`room.SettingsSaveFormAsXLSX` branches), `CreateFillResultsFile`; `FormFillingReportCreator.UpdateFormFillingReport` (54) | CODE-ONLY |
| Report by folder / Submissions listing (Results report generation) | `FileStorageService.GenerateXlsxByFolderAsync` (6186), `GetSubmissionsByFormId`; `FoldersController.GenerateXlsxByFolder` (268); yaml `generateXlsxByFolder`, `getFormSubmissions` | CODE-ONLY |
| Push per submission / Sync job per room / Status record fields (External database export) — the stored table name is `externalDbTableName` | `FormFillingReportCreator.PublishExternalDbSubmissionAsync` (75), `SetExternalDbTableNameAsync`, `SyncExternalDbAsync` (69); `FileStorageService.StartExternalDbSyncAsync` (6268), `GetExternalDbSyncTaskAsync`; yaml `ExternalDbSyncTaskDto`, `ExternalDbSyncFormResultDto` | CODE-ONLY |
| Room id for a file link (Fill result) — the mapping lives in the hybrid (distributed) cache | `FillingFormResultDtoHelper.GetAsync` (86); `FilesController.GetFillResult` (468) | CODE-ONLY |
| Roles assigned (Role-based filling in a VDR) | `FileStorageService.SaveFormRoleMapping` (5607) | CODE-ONLY |
| Statuses along the queue, rule part (Form roles listing) — `roleStatus` is `FormFillingStatus` `0` None, `1` Draft, `2` YouTurn, `3` InProgress, `4` Complete, `5` Stoped; `history` entries are `0` opened, `1` submitted, `2` stopped | `FileStorageService.GetAllFormRoles` (5673) and its `currentStep` switch; `products/ASC.Files/Core/Core/Entries/FormFillingStatus.cs`; yaml `FormRoleDto` | CODE-ONLY |
| Last step completes the form / Stop and resume / Member with an unfinished role removed (Step submission and interruption) | `EntryManager.SubmitVDRFormAsync` (≈2500–2570), `FormFilledOut` publish at 2561; `FileStorageService.ManageFormFilling` case `Stop` (VDR branch); `VirtualRoomsController.cs` ≈769 (`ShouldPreventUserDeletion`); yaml `RoomSecurityError` | CODE-ONLY |
| Fill link refused in other room types (Sharing a form to fill out) | `openspec/specs/user-type-permissions` (live run); release notes v3.7.1 line 27 | CODE-ONLY (owned by the other spec) |
| Submit payload (Webhook events) — the triggers are `WebhookTrigger` `FormSubmit`, `FormFilledOut`, `FormStopped` | `WebhookManager.PublishAsync` (59); `common/ASC.Webhooks.Core/WebhookTrigger.cs` 212–227 | CODE-ONLY |
| Names moved out of the spec text: the form signature is the `DSE3` header with the form flag or the configured `oform` signature (`FileChecker.CheckExtendedPDFstream`); `startFillingMode` values are `StartFillingMode` `ShareToFillOut` (1) and `StartFillingFormRoom` (3); the Forms root is `FolderType.Forms` (36); the "product code where the tests stop" of the Purpose is `FileStorageService`, `FileSecurity`, `EntryManager`, `DocumentServiceHelper`, `EditorController`, `FileMoveCopyOperation` and `FormFillingReportCreator` | `FileChecker.cs` (57); yaml `StartFillingMode`, `FolderType`; files cited in the rows above | CODE-ONLY |

## Left out of the spec, and why

- **Recent / Favorites inside the Forms section** (release notes v4.0 "own Recent/Favorites/Trash"):
  `03_Rooms/FormFilling/FormFillingSectionBugTests.cs` holds tests for bugs 82873 (`searchArea` ignored by
  `GET /files/recent`), 82874 / 82877 (`total` collapses to the page size), 82875 (`count` + `startIndex` on
  Favorites returns an empty page) and 82878 (tags of a form template dropped on room creation). The file
  states that every test asserts the intended behaviour and stays red while the bug is open; the memory notes
  record only the `folderType=15` case of 82873 as fixed. None of this is written as a requirement.
- **Client-side flows** of the release notes: the Form filling room tour (v3.1 item 10), the "Fill in as" /
  "Share to fill out" panels and the removed Share button in the editor (v3.1 item 17), the Edit-item warning
  dialog (v3.7), the "Sharing with DocSpace users" section of the share panel (v3.5), the quota block in the
  room header (v3.7). The server contributes only `startFillingMode`, `startFilling` and the link right, which
  the spec names.
- **Mobile push "form filled"** (v3.1 item 69) and the e-mail/notify letters (`FormStartedFillingNotifyAction`,
  `StoppedFormFillingNotifyAction`, `YourTurnFormFillingNotifyAction`, `FormWasCompletelyFilledNotifyAction`,
  `SendFormSubmittedAsync`): named in passing as "notified" in the VDR and submission requirements; their
  wording belongs to the notify-letters area, not to this capability.
- **AI form generation / forms as AI agent attachments** (v3.7, v4.0): a different capability.
- **Deviations D1, D5, D6, D13** (`openspec/deviations.md`): the unchecked `Resume`/`Edit` actions, the
  by-id read of a hidden form by a Form Filler, the copy-out of a hidden form, and the extension-only PDF check
  are defects, not requirements. The spec words the `Edit`/`Resume` effects without asserting a permission
  check, and the "Only PDFs enter the room" rule is referenced to `form-filling-room-roles`, which carries D13.
- **Role matrices** (who may start, stop, collect, configure, see what): `form-filling-room-roles` and
  `vdr-room-roles`, referenced rather than restated; the permission tests under `01_Files/FormFilling/*Permissions*`
  and `01_Files/Forms/*Permissions*` are cited here only for the status codes of the lifecycle endpoints.

## Release notes versus server

- v3.1 "PDF Form and PDF Document types": the server names them `FilterType.PdfForm` and `FilterType.Pdf`, and
  the form/document split is by content signature, while *fillability* (`isForm`, the filling operations) is
  by extension — every PDF, form or not, can be started (D13).
- v3.7 "after Start filling the form becomes available to all members": the server reveals it to the members
  holding only the form-filling right (the others already saw it) — same outcome, stated from the server side.
- v3.7 "Form Submitted / Form Filled Out / Form Stopped": the wire names are `form.submit`, `form.filled.out`,
  `form.stopped`; `form.submit` fires for any user-submit forced save whose file has an original form, which
  covers Form filling rooms and My-documents forms filled through a link alike; `form.filled.out` fires only for
  the VDR step flow.
- v3.7.1 "Quick Sharing and share-fill removed for forms in Public, Custom and Collaboration rooms": the server
  refuses the fill-right link there (403) — recorded in `user-type-permissions`, where the live run confirmed it.
- v4.0 "/forms route": the server has no `/forms` route of its own; the section is `GET /api/2.0/files/@forms`,
  the `Forms` root folder type (36) and `searchArea=Forms` / `FormTemplates` on `GET /api/2.0/files/rooms`.
