# room-types — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Status values: **TEST-CONFIRMED** — an integration test asserts it; **CODE-ONLY** — settled from the product
code or the OpenAPI contract (`sdk/docspace-api-spec/docspace-backend.yaml`, cited by `operationId`), no
test; **NOTES-ONLY** — taken from the release notes without server evidence.

Paths are relative to `products/ASC.Files/Tests/Tests/` unless they start with `products/`, `common/` or
`sdk/`. `[Bug N]` marks a test carrying `[Trait("Bug", "N")]`; it is a regression link, not an open bug.

Revision 2026-11-04 (release/v4.1.0 merged, `d58816bece..3533ad8a89`): the AI rooms section (Instructions,
Knowledge and AI project rooms, `FolderType.AiHub` 41, `SearchArea.AiHub`) is new, with the `03_Rooms/AiHub`
suite; the integration host now runs a document server, so the index export completes (its test is no longer
skipped); the listing tests read rooms as `FolderDto` through the `entryKind` discriminator instead of raw
JSON, which moved lines but changed no assertion; `updateRoomTag` validation got a test. The AI-chat side of
the new rooms (`getProjectSources`, `linkProjectSources`, `unlinkProjectSources`, `getRoomSkills`,
`getRoomsSkills`, `getProjectModel`, `setProjectModel`, `getRoomAiFolder` now serving an Instructions room)
is left to `ai-agents`, whose "`.ai` skills folder" requirement predates this change and is stale.

## Requirements and scenarios

| Requirement / scenario | Source | Status |
|---|---|---|
| **Room types offered by the API** — enum values 1, 2, 5, 6, 8, 9, 10, 11, 12; no Private type; `roomType` immutable | yaml `RoomType` schema (`x-enum-varnames`: 1 `FillingFormsRoom`, 2 `EditingRoom`, 5 `CustomRoom`, 6 `PublicRoom`, 8 `VirtualDataRoom`, 9 `AiRoom`, 10 `InstructionsRoom`, 11 `KnowledgeRoom`, 12 `AiProjectRoom`; `products/ASC.Files/Core/ApiModels/RequestDto/CreateRoomRequestDto.cs:43-67`), `CreateRoomRequestDto.roomType` ("cannot be changed once the room exists"), `UpdateRoomRequest` has no `roomType`; `private` is a boolean of `CreateRoomRequestDto` | CODE-ONLY |
| Every type is created with its own value | 03_Rooms/Create/RoomCreateTests.cs:44-66; 03_Rooms/Read/RoomInfoTests.cs:45-65 | TEST-CONFIRMED [run: PASS] |
| Unknown or missing room type is rejected (400) | 03_Rooms/Create/RoomCreateValidationTests.cs:100-130 | TEST-CONFIRMED [run: PASS] |
| AI room carries chat settings, other types refuse them (400) | 03_Rooms/Chat/RoomChatSettingsTests.cs:53-92 [Bug 82798]; yaml `CreateRoomRequestDto.chatSettings` | TEST-CONFIRMED [run: PASS] |
| Room info exposes only the flags that were set | 03_Rooms/Read/RoomInfoFieldsTests.cs:114-194 | TEST-CONFIRMED [run: PASS] |
| Section and primary link per type (Forms / Active / AiAgents / AiHub; primary link for Form filling and Public rooms) | 03_Rooms/FormFilling/FormFillingRoomSectionTests.cs:52-164; 03_Rooms/AiHub/AiHubRootTests.cs:47-81 and AiProjectRoomTests.cs:45-60 (AiHub root, not in Active); yaml `SearchArea` enum, `getRoomsFolder.searchArea`; roots `FolderType.Forms` (36), `FolderType.VirtualRooms` (14), `FolderType.AiHub` (41); primary link on creation: `shared-links` spec, 03_Rooms/Links/RoomLinkPrimaryTests.cs; no link for AiHub rooms: `products/ASC.Files/Core/Core/Security/FileSecurity.cs:237-263` (no link subject) | TEST-CONFIRMED [run: FAIL-BUG] (Forms/Active/AiHub, primary link), CODE-ONLY (AiAgents, no AiHub link) |
| **Who may create a room** | yaml `createRoom` 403 text | CODE-ONLY |
| Owner and DocSpace admin create a room | 03_Rooms/Permissions/RoomCreatePermissionsTests.cs:42-72; Room admin: 03_Rooms/RoomsApiTests.cs:245-246, 03_Rooms/Listing/RoomsFolderVisibilityTests.cs:45-58 | TEST-CONFIRMED [run: PASS] |
| User and Guest cannot create a room (403, message) | 03_Rooms/Permissions/RoomCreatePermissionsTests.cs:75-106, 198-235 | TEST-CONFIRMED [run: PASS] |
| Anonymous and terminated callers (401) | 03_Rooms/Permissions/RoomCreatePermissionsTests.cs:110-148 | TEST-CONFIRMED [run: PASS] |
| Share list on creation is rejected (400) | 03_Rooms/Create/RoomCreateOptionalFieldsTests.cs:213-232 [Bug 81582]; yaml `CreateRoomRequestDto.share` ("Not implemented on room creation") | TEST-CONFIRMED [run: PASS] |
| **Room title** — 170 max, sanitising, not unique | yaml `CreateRoomRequestDto.title` (maxLength 170), `UpdateRoomRequest.title` | CODE-ONLY |
| Blank or missing title on creation (400) | 03_Rooms/Create/RoomCreateValidationTests.cs:54-97 | TEST-CONFIRMED [run: PASS] |
| Over-long title (400), 170 accepted | 03_Rooms/Create/RoomCreateValidationTests.cs:133-146; 03_Rooms/Update/RoomUpdateTitleTests.cs:142-178 | TEST-CONFIRMED [run: PASS] |
| Forbidden characters are replaced | 03_Rooms/Create/RoomCreateTitleTests.cs:60-138; 03_Rooms/Update/RoomUpdateTitleTests.cs:99-121 | TEST-CONFIRMED [run: PASS] |
| Duplicate titles are allowed | 03_Rooms/Create/RoomCreateTests.cs:99-112 | TEST-CONFIRMED [run: PASS] |
| Blank title on update is a no-op | 03_Rooms/Update/RoomUpdateTests.cs:111-124; 03_Rooms/Update/RoomUpdateTitleTests.cs:76-92, 182-214; 03_Rooms/Read/RoomInfoTests.cs:182-194 | TEST-CONFIRMED [run: PASS] |
| **Room tags** — catalogue, replace semantics, 255 limit, verbatim chars | yaml `CreateRoomRequestDto.tags`, `UpdateRoomRequest.tags` ("replaces the whole tag set") | CODE-ONLY |
| Tags given on creation; dedup; 50 tags; null | 03_Rooms/Create/RoomCreateOptionalFieldsTests.cs:133-179; 03_Rooms/Create/RoomCreateEdgeCaseTests.cs:77-90; 03_Rooms/Create/RoomCreateValidationTests.cs:225-234 | TEST-CONFIRMED [run: PASS] |
| Update replaces the tag set | 03_Rooms/Update/RoomUpdateTagsColorCoverTests.cs:47-105 | TEST-CONFIRMED [run: PASS] |
| Tag name limits (over-long tag 400; forbidden chars stored verbatim) | 03_Rooms/Update/RoomUpdateTagsColorCoverTests.cs:47-105; yaml `addRoomTags`, `deleteRoomTags` | TEST-CONFIRMED [run: PASS] |
| Renaming a tag of the catalogue (`updateRoomTag`: catalogue and rooms follow; blank / missing / same / taken name 400, old tag kept; unknown 404) | 03_Rooms/Tags/RoomTagUpdateTests.cs:52-150 (rename, catalogue, rooms), 153-290 (404; same, taken, empty new, empty old, malformed, missing, null → 400); blank entries of a batch: 03_Rooms/Permissions/RoomCustomTagValidationPermissionsTests.cs:106-142 [Bug 81689] | TEST-CONFIRMED [run: PASS] |
| Tag filter must be JSON (400); any-match; `withoutTags` | 03_Rooms/Listing/RoomsFolderFilterTests.cs:220-327 [Bug 81808] | TEST-CONFIRMED [run: PASS] |
| Single-use tag disappears with its room; shared tag stays | 03_Rooms/Deletion/RoomDeleteTests.cs:160-200 | TEST-CONFIRMED [run: PASS] |
| **Room colour, cover and logo** | yaml `CreateRoomRequestDto.color` (pattern), `.cover` (maxLength 50), `.logo`; `createRoomLogo`, `getRoomCovers` | CODE-ONLY |
| Colour set on creation and update; "" resets; null no-op | 03_Rooms/Create/RoomCreateOptionalFieldsTests.cs:182-191; 03_Rooms/Update/RoomUpdateTagsColorCoverTests.cs:109-131; 03_Rooms/Covers/RoomCoverValidationTests.cs:128-149 | TEST-CONFIRMED [run: PASS] |
| Invalid colour (400) | 03_Rooms/Create/RoomCreateValidationTests.cs:188-198; 03_Rooms/Update/RoomUpdateTagsColorCoverTests.cs:135-175 [Bug 82364]; 03_Rooms/Covers/RoomCoverValidationTests.cs:46-87 [Bug 81558] | TEST-CONFIRMED [run: PASS] |
| Cover set, replaced and cleared; unknown cover 400 | 03_Rooms/Update/RoomUpdateTagsColorCoverTests.cs:179-219; 03_Rooms/Create/RoomCreateValidationTests.cs:201-211 | TEST-CONFIRMED [run: PASS] |
| Cover gallery (same for every role and language, contains `schedule`) | 03_Rooms/Covers/RoomCoverGalleryTests.cs:46-146 | TEST-CONFIRMED [run: PASS] |
| Logo applied from an uploaded image | 03_Rooms/Logos/RoomLogoCreateTests.cs:48-218 | TEST-CONFIRMED [run: PASS] |
| Logo refused on an archived (403), deleted (404) or foreign upload (403) | 03_Rooms/Logos/RoomLogoCreateTests.cs:129-218; 03_Rooms/Deletion/RoomDeleteAsyncTests.cs:200-219 | TEST-CONFIRMED [run: PASS] |
| **Data-protection settings** — accepted on every type; defaults | yaml `CreateRoomRequestDto.indexing/denyDownload/lifetime/watermark`; defaults: 03_Rooms/Create/RoomCreateTests.cs:69-81 | TEST-CONFIRMED [run: PASS] (defaults), CODE-ONLY (shape) |
| Shape of the four settings (lifetime `value` 1-999, `period`; watermark `text` ≤ 255, `additions`) | yaml `RoomDataLifetimePeriod` (0 Day, 1 Month, 2 Year), `WatermarkAdditions` bitmask (1 UserName, 2 UserEmail, 4 UserIpAdress, 8 CurrentDate, 16 RoomName), `RoomDataLifetime`, `WatermarkRequestDto` | CODE-ONLY |
| Settings given on creation of a VDR | 03_Rooms/Create/RoomCreateOptionalFieldsTests.cs:73-130 | TEST-CONFIRMED [run: PASS] |
| All settings updated at once and toggled off | 03_Rooms/Update/RoomUpdateTests.cs:68-108; 03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:47-168 | TEST-CONFIRMED [run: PASS] |
| Invalid lifetime (400) | 03_Rooms/Create/RoomCreateValidationTests.cs:172-185; 03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:161-167 | TEST-CONFIRMED [run: PASS] |
| Settings accepted on a Custom room | 03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:65-77; 03_Rooms/Update/RoomUpdateTests.cs:145-172 | TEST-CONFIRMED [run: PASS] |
| Wrongly typed field / unknown property (400) | 03_Rooms/Update/RoomUpdateTitleTests.cs:222-242; 03_Rooms/Update/RoomUpdateValidationTests.cs:128-139 [Bug 82365] | TEST-CONFIRMED [run: PASS] |
| **Manual order and reorder** — dense 1..N, one sequence, nested untouched, idempotent | yaml `reorderRoom`, `CreateRoomRequestDto.indexing` | CODE-ONLY |
| Gaps are compacted | 03_Rooms/Reorder/RoomReorderTests.cs:90-117, 240-286 | TEST-CONFIRMED [run: PASS] |
| Files and folders share one sequence; nested untouched; nothing lost | 03_Rooms/Reorder/RoomReorderTests.cs:143-237, 289-337 | TEST-CONFIRMED [run: PASS] |
| Reorder of an archived (403) or missing room (404); non-indexed room OK | 03_Rooms/Reorder/RoomReorderTests.cs:350-449 [Bug 81862, 81863] | TEST-CONFIRMED [run: PASS] |
| Reorder permissions | 03_Rooms/Permissions/RoomReorderPermissionsTests.cs:47-147 | TEST-CONFIRMED [run: PASS] |
| **Index export of a VDR** | yaml `startRoomIndexExport`, `getRoomIndexExport`, `terminateRoomIndexExport` | CODE-ONLY |
| Owner or DocSpace admin starts the export; re-export after unarchive | 03_Rooms/Permissions/RoomIndexExportStartPermissionsTests.cs:47-69; 03_Rooms/Archive/RoomUnarchiveTests.cs:364-382 | TEST-CONFIRMED [run: PASS] |
| Members without manager rights cannot export | 03_Rooms/Permissions/RoomIndexExportStartPermissionsTests.cs:75-128 | TEST-CONFIRMED [run: PASS] |
| Archived (403) or unknown room (404) | 03_Rooms/IndexExport/RoomIndexExportValidationTests.cs:50-87 [Bug 82368, 82369] | TEST-CONFIRMED [run: PASS] |
| Export completes with a result file (`isCompleted`, no error, `resultFileId`; indexing turned on after creation) | 03_Rooms/IndexExport/RoomIndexExportTests.cs:56-79 [Bug 81110] — no longer skipped: the integration host runs a document server since 4.1.0 (`FormsTestBase.cs:37-50`); release note v3.0 ("generation of a summary report") | TEST-CONFIRMED [run: PASS] |
| **Rooms of the AI rooms section** — types 10-12 under the AiHub root, own section, settings subset, never archived, no templates, deleted outright, 403 while AI is off | `products/ASC.Files/Core/Core/FileStorageService.cs:671-696` (settings refused with 400 for `IsAiHubRoom`; created under `GetFolderAiHubAsync`; the room count check runs for the Rooms root only), `:839` and `:1001` (no template from or of such a room), `:1190-1198` (nothing but such a room lands in the root); `products/ASC.Files/Server/Api/VirtualRoomsController.cs:658-661` (archive → 403); `products/ASC.Files/Core/Services/WCFService/FileOperations/CopyPermissionsCheck.cs:456-480` (never archived, never leaves its root); `FileSecurity.cs:1249-1252` (AI off → every access refused); `products/ASC.Files/Core/Helpers/DocSpaceHelper.cs:63-70` (`IsAiHubRoom`); yaml `createRoom`, `getRoomsFolder` (`SearchArea` `AiHub`) | CODE-ONLY |
| Created under the AI rooms root (rootFolderType 41, parentId = root, type / colour / tags echoed; listed in AiHub, filtered by `type=12`, not in Active; User 403; no title 400) | 03_Rooms/AiHub/AiHubRootTests.cs:47-81; 03_Rooms/AiHub/AiProjectRoomTests.cs:45-113 | TEST-CONFIRMED [run: PASS] |
| Settings that do not apply are refused (project: quota, denyDownload; Instructions: indexing, denyDownload → 400, nothing created; title and colour applied) | 03_Rooms/AiHub/AiHubRoomSettingsTests.cs:46-118; `FileStorageService.cs:671-676, 1513-1517` | TEST-CONFIRMED [run: PASS] |
| Never archived, no templates, no folder in the root (403) | 03_Rooms/AiHub/AiHubRootTests.cs:83-126; 03_Rooms/AiHub/AiProjectRoomTests.cs:195-222 | TEST-CONFIRMED [run: PASS] |
| Deleted outright (404 afterwards, gone from the section); non-member User 403 on read, update, delete | 03_Rooms/AiHub/AiHubRootTests.cs:128-145; 03_Rooms/AiHub/AiProjectRoomTests.cs:115-193 | TEST-CONFIRMED [run: PASS] |
| Refused while AI is off (create, read, update, delete, section listing, folder content, upload, file read and presigned URI → 403; back after AI is on) | 03_Rooms/AiHub/AiHubAiAccessTests.cs:44-191 (`setTenantAiAccessSettings`) | TEST-CONFIRMED [run: PASS] |
| **Form filling rooms live in the Forms section** | release note v4.0 ("Forms are a separate root section"); yaml `FolderType` 36 Forms / 14 VirtualRooms, `SearchArea` Forms/FormTemplates | — |
| Form filling room is listed only in Forms (even with type filter) | 03_Rooms/FormFilling/FormFillingRoomSectionTests.cs:52-181 | TEST-CONFIRMED [run: PASS] |
| Forms root folder (type 36, same id, lists form rooms only) | 03_Rooms/FormFilling/FormFillingRoomSectionTests.cs:185-267 | TEST-CONFIRMED [run: PASS] |
| Used space is accounted to Forms / Archive | 03_Rooms/FormFilling/FormFillingRoomUsedSpaceTests.cs:52-160 | TEST-CONFIRMED [run: PASS] |
| Form room templates have their own section; room from form template in Forms | 03_Rooms/FormFilling/FormFillingRoomTemplateSectionTests.cs:53-207 | TEST-CONFIRMED [run: PASS] |
| Form room opened through its link is listed in Forms | 03_Rooms/FormFilling/FormFillingSectionBugTests.cs:282-314 [Bug 83228] | TEST-CONFIRMED [run: PASS] |
| **Public room backed by third-party storage** — connected folder, every `RoomType` 1-8 | yaml `createRoomThirdParty`, `CreateThirdPartyRoomRequest`; release notes v2.5 ("only for Public rooms") | CODE-ONLY |
| New folder or the connected folder itself (`createAsNewFolder`, dropped `indexing`/`denyDownload`/`tags`/`logo`, string ids, `providerKey`/`providerId`) | yaml `CreateThirdPartyRoomRequest.createAsNewFolder`, `createRoomThirdParty` description; release note v2.5 ("Create a new folder in the storage"); 03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:50-111 (string ids, providerKey) | CODE-ONLY |
| Connected folder becomes the room; listed with providerKey | 03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:50-111, 154-170 (requires Nextcloud, `[Trait("Requires","Nextcloud")]`) | TEST-CONFIRMED [run: PASS] |
| Every room type is accepted | 03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:114-131 (requires Nextcloud) | TEST-CONFIRMED [run: PASS] |
| One connection, one room (403) | 03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:135-150 (requires Nextcloud) | TEST-CONFIRMED [run: PASS] |
| Foreign connection (403); non-connected id is 4xx | 03_Rooms/ThirdParty/ThirdPartyRoomCreationBugTests.cs:51-153 [Bug 83301, 83306] | TEST-CONFIRMED [run: PASS] |
| Deleting a third-party room disconnects the account | 03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:174-197 (requires Nextcloud) | TEST-CONFIRMED [run: PASS] |
| **A room reached through its link joins the caller's lists** | yaml `getExternalShareData` ("the first successful resolution puts the entry into the account's own lists"); release note v3.0 | CODE-ONLY |
| Member by link sees the room | Form filling room: 03_Rooms/FormFilling/FormFillingSectionBugTests.cs:282-314 [Bug 83228]; Public room: yaml text only (03_Rooms/Links/RoomLinkAnonymousAccessTests.cs:65-94 is skipped and covers the anonymous `shared` flag, not the listing) | TEST-CONFIRMED [run: PASS] (form room), CODE-ONLY (public room) |
| **Room templates** — async job, per-caller status, parameters, edit rights on source | yaml `createRoomTemplate`, `getRoomTemplateCreatingStatus`, `RoomTemplateDto`; release note v3.1 | CODE-ONLY |
| Template created from a room; distinct ids; duplicate titles; in Templates listing | 03_Rooms/Templates/RoomTemplateCreateTests.cs:49-138; 03_Rooms/Templates/RoomTemplateContentTests.cs:189-259, 333-407 | TEST-CONFIRMED [run: PASS] |
| Template options are stored (color, cover, tags, quota, public) | 03_Rooms/Templates/RoomTemplateContentTests.cs:51-145 | TEST-CONFIRMED [run: PASS] |
| Source room cannot be used (404 / 403 archived) | 03_Rooms/Templates/RoomTemplateCreateTests.cs:175-269 [Bug 81691]; 03_Rooms/Templates/RoomTemplateCreateValidationTests.cs:53-100 [Bug 81690] | TEST-CONFIRMED [run: PASS] |
| Callers without edit rights on the source (403); Room manager OK; 401 cases | 03_Rooms/Templates/RoomTemplateCreatePermissionsTests.cs:41-197 [Bug 81693] | TEST-CONFIRMED [run: PASS] |
| Status does not leak between users | 03_Rooms/Templates/RoomTemplateCreateTests.cs:147-166 [Bug 81692] | TEST-CONFIRMED [run: PASS] |
| **Template visibility and public setting** | yaml `getPublicSettings`, `setPublicSettings`, `RoomTemplateDto.public/share/groups` | CODE-ONLY |
| Non-public template is hidden from an unrelated user (403) | 03_Rooms/Templates/RoomTemplateContentTests.cs:262-280 | TEST-CONFIRMED [run: PASS] |
| Public template visible to admin roles only | 03_Rooms/Templates/RoomTemplateContentTests.cs:285-329 | TEST-CONFIRMED [run: PASS] |
| Shared template (share by email; groups accepted) | 03_Rooms/Templates/RoomTemplateContentTests.cs:148-185 | TEST-CONFIRMED [run: PASS] |
| Public flag toggled; isolation; survives source deletion/archiving | 03_Rooms/Templates/RoomTemplateShareTests.cs:52-371 [Bug 81726, 81939, 81949]; 03_Rooms/Templates/RoomTemplatePublicSettingsTests.cs:52-80 [Bug 81938] | TEST-CONFIRMED [run: PASS] |
| Public setting of an unusable id (404 deleted / non-template / unknown; 400 for 0, -1, non-integer) | 03_Rooms/Templates/RoomTemplateShareTests.cs:52-371 [Bug 81726, 81939, 81949] | TEST-CONFIRMED [run: PASS] |
| **Creating a room from a template** — inheritance rules, independence, permissions | yaml `createRoomFromTemplate`, `CreateRoomFromTemplateDto` | CODE-ONLY |
| Room created from a Custom room template; usable afterwards | 03_Rooms/Templates/RoomFromTemplateTests.cs:50-67, 288-338, 406-457 | TEST-CONFIRMED [run: PASS] |
| VDR settings partially inherited (type, indexing, denyDownload yes; lifetime, watermark no) | 03_Rooms/Templates/RoomFromTemplateTests.cs:71-236 | TEST-CONFIRMED [run: PASS] |
| Content is copied with new ids; independence; source deleted/unchanged | 03_Rooms/Templates/RoomFromTemplateContentTests.cs:52-186 [Bug 81666]; 03_Rooms/Templates/RoomFromTemplateTests.cs:240-403 | TEST-CONFIRMED [run: PASS] |
| Validation of the request (400 / 404) | 03_Rooms/Templates/RoomFromTemplateValidationTests.cs:47-247 [Bug 81667, 81669] | TEST-CONFIRMED [run: PASS] |
| Who may create a room from a template: admin+public OK; User/Guest 403; admin on foreign private 403; source access only 403; 401 | 03_Rooms/Templates/RoomFromTemplatePermissionsTests.cs:44-230 [Bug 81662, 81663, 81664] | TEST-CONFIRMED [run: PASS] |
| **Room groups** — name, icon, rooms, searchArea, per-account | yaml `addRoomGroup` (400/403/404 text), `RoomGroupRequestDto.searchArea`; release note v3.7 | CODE-ONLY |
| Group created over rooms of several types; private room grouped in the `PrivateRooms` area | 03_Rooms/Groups/RoomGroupCreateTests.cs:43-114, 262-291 (`Create_PrivateRoom_Succeeds` now passes `PrivateRooms`) | TEST-CONFIRMED [run: PASS] |
| Name and icon validation (400; non-BMP name refused with 500; trimmed) | 03_Rooms/Groups/RoomGroupCreateNameIconValidationTests.cs:53-166 [Bug 80921]; 03_Rooms/Groups/RoomGroupCreateTests.cs:176-258 [Bug 82573] | TEST-CONFIRMED [run: PASS] (the non-BMP refusal answers 500, not 400) |
| Rooms list validation (403 empty; 400 shapes; 404 none existing) | 03_Rooms/Groups/RoomGroupCreateRoomsValidationTests.cs:52-218 [Bug 82575, 82576, 82577, 82587] | TEST-CONFIRMED [run: PASS] |
| Section of a group (Forms vs Active; refused areas; numeric value) | 03_Rooms/Groups/RoomGroupSearchAreaTests.cs:49-199 | TEST-CONFIRMED [run: PASS] |
| Partially valid room list (403, group still created) | 03_Rooms/Groups/RoomGroupCreateRoomsValidationTests.cs:191-204; 03_Rooms/Groups/RoomGroupSearchAreaTests.cs:127-143 | TEST-CONFIRMED [run: PASS] |
| Rooms added and removed; searchArea immutable; 404/400 on update | 03_Rooms/Groups/RoomGroupUpdateRoomsTests.cs:45-336 [Bug 82591, 82592, 82594, 82595]; 03_Rooms/Groups/RoomGroupSearchAreaTests.cs:203-269 | TEST-CONFIRMED [run: PASS] |
| Group follows the rooms' lifecycle (archive, delete, rename); delete keeps rooms | 03_Rooms/Groups/RoomGroupLifecycleTests.cs:154-236 [Bug 82601]; 03_Rooms/Groups/RoomGroupDeleteTests.cs:74-85 | TEST-CONFIRMED [run: PASS] |
| Groups are per account (404 read/update/icon, 403 delete; own groups fully managed; gated create) | 03_Rooms/Groups/RoomGroupCrossUserIsolationTests.cs:43-135 [Bug 82597]; 03_Rooms/Groups/RoomGroupOwnCapabilityTests.cs:59-165 [Bug 82598] | TEST-CONFIRMED [run: PASS] |
| Listing rooms by group | 03_Rooms/Groups/RoomGroupSearchAreaTests.cs:273-285 | TEST-CONFIRMED [run: PASS] |
| Deleting a group (404 repeat/bad ids; includeMembers) | 03_Rooms/Groups/RoomGroupDeleteTests.cs:43-190 [Bug 82596]; 03_Rooms/Groups/RoomGroupReadTests.cs:45-270 | TEST-CONFIRMED [run: PASS] |
| Anonymous / invalid token (401) on every group call | 03_Rooms/Groups/RoomGroupAnonymousAccessTests.cs:45-214 | TEST-CONFIRMED [run: PASS] |
| **Duplicating rooms** — background copy next to the original, numeric suffix, rights of item creation | yaml `duplicateBatchItems`; release note v2.6 | CODE-ONLY |
| Room duplicated — settings, logo, tags, metadata copied; primary link recreated for non-Custom; members not copied; room limit | products/ASC.Files/Core/Core/Dao/TeamlabDao/FolderDao.cs:1395-1441 (`CopyFolderAsync`: title via `GetAvailableTitleAsync`, Settings* copied, custom tags and metadata copied, no shares); products/ASC.Files/Core/Services/WCFService/FileOperations/FileMoveCopyOperation.cs:294-346 (`isRoomCopying`: `countRoomChecker.CheckAppend`, `roomLogoManager.CopyAsync`, primary external link re-shared when `FolderType != CustomRoom`); products/ASC.Files/Core/Services/WCFService/FileOperations/FileDuplicateOperation.cs:89-99; release note v2.6 | CODE-ONLY |
| File duplicated inside a room (Owner, Room manager, Content creator) | 06_Operations/Duplicate/DuplicateBatchItemsTests.cs:100-113; 06_Operations/Duplicate/DuplicatePermissionsTests.cs:85-147 | TEST-CONFIRMED [run: PASS] |
| Rights below Content creator (403); anonymous 401 | 06_Operations/Duplicate/DuplicatePermissionsTests.cs:49-66, 107-124, 150-192 | TEST-CONFIRMED [run: PASS] |
| Archived room, Trash (403) and unknown ids (404) | 06_Operations/Duplicate/DuplicateBatchItemsTests.cs:146-220 [Bug 82210] | TEST-CONFIRMED [run: PASS] |
| Folder with content; file and folder together; suffix naming | 06_Operations/Duplicate/DuplicateBatchItemsTests.cs:48-130, 223-280; yaml `duplicateBatchItems` | TEST-CONFIRMED [run: PASS] |
| **Pinning rooms** — personal, limit 10, AI limit, ordering, archive | yaml `pinRoom` description; release note v2.6 ("limit of pinned rooms = 10") | CODE-ONLY |
| Pin limit of ten; unpin frees a slot; no reset of all pins | 03_Rooms/Pin/RoomPinLimitTests.cs:127-155; 03_Rooms/Pin/RoomUnpinLimitTests.cs:47-105 [Bug 80757] | TEST-CONFIRMED [run: PASS] |
| AI rooms have their own limit | 03_Rooms/Pin/RoomPinLimitTests.cs:157-204 [Bug 81852]; the third, separate count for the rooms of the AI rooms section: `FileStorageService.cs:5089-5096` (`aiHubTagsCount` against `MaxPinnedRooms`), no test | TEST-CONFIRMED [run: PASS] (agents) / CODE-ONLY (AI rooms section) |
| Pinned rooms stay on top (sort, filter, paging, stable order) | 03_Rooms/Pin/RoomPinOrderingTests.cs:47-222; 03_Rooms/RoomPinOrderTests.cs:47-75 | TEST-CONFIRMED [run: PASS] |
| Pin is personal; does not survive archiving; idempotent; concurrent safe | 03_Rooms/Pin/RoomPinTests.cs:50-211; 03_Rooms/Pin/RoomPinValidationTests.cs:126-142; 03_Rooms/Pin/RoomPinLimitTests.cs:47-123 | TEST-CONFIRMED [run: PASS] |
| Pin on archived (403), deleted (404), foreign room (403), unknown id (404, "The required folder was not found") | 03_Rooms/Pin/RoomPinValidationTests.cs:57-122 [Bug 81850]; 03_Rooms/Pin/RoomUnpinValidationTests.cs:52-111 [Bug 82366] | TEST-CONFIRMED [run: PASS] |
| Who may pin (uninvited DocSpace admin OK; non-member User/Guest 403; anonymous/terminated 401) | 03_Rooms/Permissions/RoomPinPermissionsTests.cs:51-149 | TEST-CONFIRMED [run: PASS] |
| Unpin never changes membership; unpin of never-pinned room 200 | 03_Rooms/Pin/RoomUnpinTests.cs:51-223 | TEST-CONFIRMED [run: PASS] |
| Every room type can be pinned / unpinned (form room within Forms) | 03_Rooms/Pin/RoomPinRoomTypesTests.cs:46-70; 03_Rooms/Pin/RoomUnpinRoomTypesTests.cs:44-66 | TEST-CONFIRMED [run: PASS] |
| **Archiving and restoring** | yaml `archiveRoom`, `unarchiveRoom`, `ArchiveRoomRequest.deleteAfter` | CODE-ONLY |
| Full cycle (sections, rootFolderType, content and metadata kept) | 03_Rooms/Archive/RoomArchiveTests.cs:48-171; 03_Rooms/Archive/RoomUnarchiveTests.cs:343-360; 03_Rooms/Read/RoomInfoTests.cs:105-138, 198-211 | TEST-CONFIRMED [run: PASS] |
| Archived room is read-only (folder, file, rename, tags, invite, logo, quota, duplicate, pin, reorder, export) | 03_Rooms/Archive/RoomArchiveTests.cs:100-146 [Bug 81551]; 03_Rooms/Logos/RoomLogoCreateTests.cs:129-142; 07_Settings/Quota/RoomQuotaChangeTests.cs:82-98; 07_Settings/Quota/RoomQuotaResetTests.cs:79-99; 06_Operations/Duplicate/DuplicateBatchItemsTests.cs:180-196; 03_Rooms/Pin/RoomPinValidationTests.cs:108-122; 03_Rooms/Reorder/RoomReorderTests.cs:392-406; 03_Rooms/IndexExport/RoomIndexExportValidationTests.cs:74-87 | TEST-CONFIRMED [run: PASS] |
| Who may archive and restore | 03_Rooms/Permissions/RoomArchivePermissionsTests.cs:44-118 [Bug 80938], 183-323 | TEST-CONFIRMED [run: PASS] |
| Idempotent and no-op cases | 03_Rooms/Permissions/RoomArchivePermissionsTests.cs:157-171; 03_Rooms/Archive/RoomUnarchiveTests.cs:131-194 | TEST-CONFIRMED [run: PASS] |
| Unknown room (404) and bad body (400) | 03_Rooms/Permissions/RoomArchivePermissionsTests.cs:122-153 [Bug 81550]; 03_Rooms/Archive/RoomArchiveTests.cs:175-201; 03_Rooms/Archive/RoomUnarchiveTests.cs:95-107, 202-269 | TEST-CONFIRMED [run: PASS] |
| Restored room can be renamed and shared again | 03_Rooms/Archive/RoomUnarchiveTests.cs:300-339 | TEST-CONFIRMED [run: PASS] |
| **Deleting a room** | yaml `deleteRoom`, `DeleteRoomRequest` | CODE-ONLY |
| Owner deletes a room of every type; operation finishes | 03_Rooms/Deletion/RoomDeleteTests.cs:50-111; 03_Rooms/Deletion/RoomDeleteAsyncTests.cs:72-160 [Bug 81698] | TEST-CONFIRMED [run: PASS] |
| Content and links go with the room; cover/logo/quota rooms deletable; archived deletable; member loses access | 03_Rooms/Deletion/RoomDeleteTests.cs:115-312 | TEST-CONFIRMED [run: PASS] |
| Who may delete | 03_Rooms/Permissions/RoomDeletePermissionsTests.cs:50-230 | TEST-CONFIRMED [run: PASS] |
| Invalid ids (404) and body (400) | 03_Rooms/Deletion/RoomDeleteValidationTests.cs:48-148 [Bug 81697] | TEST-CONFIRMED [run: PASS] |
| Deleted room cannot be revived (update 403, archive/delete/template 404) | 03_Rooms/Update/RoomUpdateValidationTests.cs:104-118; 03_Rooms/Permissions/RoomArchivePermissionsTests.cs:137-153; 03_Rooms/Deletion/RoomDeleteAsyncTests.cs:164-219; 03_Rooms/Templates/RoomTemplateCreateTests.cs:229-247 | TEST-CONFIRMED [run: PASS] |
| **Changing the room owner and leaving the room** | yaml `changeFileOwner` description; release note v2.0; `room-role-permissions` (leave, User cannot own) | CODE-ONLY |
| Previous owner stays a member (keeps the room as Room manager; item already owned by the target returned untouched; private room needs keys) | yaml `changeFileOwner` description | CODE-ONLY |
| New owner must be an active account (403, message) | 04_Security/Sharing/GroupAndOwnerSharingTests.cs:90-101 [Bug 66897] | TEST-CONFIRMED [run: PASS] |
| User cannot become the owner | `openspec/specs/room-role-permissions/spec.md` "User cannot own a room" | TEST-CONFIRMED [run: NO-LINK] (there) |
| **Per-room storage quota** | yaml `updateRoomsQuota`, `resetRoomQuota`, `CreateRoomRequestDto.quota`; release note v2.5 | CODE-ONLY |
| Custom quota set and reset; multiple rooms; zero accepted; isCustomQuota flags | 07_Settings/Quota/QuotaTests.cs:51-277; 07_Settings/Quota/RoomQuotaChangeTests.cs:49-141; 07_Settings/Quota/RoomQuotaResetTests.cs:48-146 | TEST-CONFIRMED [run: PASS] |
| Quota given on creation; negative not stored verbatim; info reflects quotaLimit/usedSpace | 03_Rooms/Create/RoomCreateOptionalFieldsTests.cs:48-70; 03_Rooms/Create/RoomCreateValidationTests.cs:149-169; 07_Settings/Quota/RoomQuotaInfoTests.cs:65-107; 03_Rooms/Read/RoomInfoTests.cs:69-79 | TEST-CONFIRMED [run: PASS] |
| Feature off (403), archived (403), wrong caller (403/401), empty list | 07_Settings/Quota/RoomQuotaChangeTests.cs:68-120 [Bug 82293]; 07_Settings/Quota/RoomQuotaResetTests.cs:65-121 [Bug 82293]; 07_Settings/Quota/RoomQuotaPermissionsTests.cs:54-112 | TEST-CONFIRMED [run: PASS] |
| **Behaviour when the room quota is exceeded** — soft overrun once, hard limit at 2x, 402 on upload | 07_Settings/Quota/QuotaTests.cs:281-577; release notes v2.6.1, v3.7, v4.0 ("allow a small overrun when saving from the editor") | TEST-CONFIRMED [run: PASS] |
| File within the quota | 07_Settings/Quota/QuotaTests.cs:281-339 | TEST-CONFIRMED [run: PASS] |
| Quota smaller than the file | 07_Settings/Quota/QuotaTests.cs:342-361; 07_Settings/Quota/RoomQuotaInfoTests.cs:48-62 | TEST-CONFIRMED [run: PASS] |
| Soft overrun allowed once; >2x refused; upload 402 "quota" | 07_Settings/Quota/QuotaTests.cs:364-577 | TEST-CONFIRMED [run: PASS] |
| **Private rooms** — flag, keys required, immutable | yaml `CreateRoomRequestDto.private`, `createRoom` 403 ("`private` is set while the caller has no encryption keys"), `getRoomsFolder` private filter. The v4.0 note on hiding them in the client left the spec: the section now exists (`c866c055e1`, `private-rooms`) | CODE-ONLY |
| Private room created by Owner / DocSpace admin / Room admin; logo applies; grouped | 03_Rooms/Permissions/RoomCreatePermissionsTests.cs:154-194 (via `CreatePrivateRoom`, Tests/BaseTest.cs:345-349, which sets keys first); 03_Rooms/Logos/RoomLogoCreateTests.cs:64-77; 03_Rooms/Groups/RoomGroupCreateTests.cs:103-113 | TEST-CONFIRMED [run: PASS] |
| User and Guest cannot create a private room (403); anonymous 401 | 03_Rooms/Permissions/RoomCreatePermissionsTests.cs:198-251 | TEST-CONFIRMED [run: PASS] |
| Default is not private | 03_Rooms/Create/RoomCreateTests.cs:69-81 | TEST-CONFIRMED [run: PASS] |
| Keys required (403 without keys) | yaml `createRoom` 403 text only — no test creates a private room without keys | CODE-ONLY |
| **Listing rooms and reading a room** | yaml `getRoomsFolder` parameters, `getRoomInfo` | CODE-ONLY |
| Supported filters and paging (`type`, `filterValue`, `tags`/`withoutTags`, `subjectId`, `subjectOwnerId`+`excludeSubject`, `groupId`, `count`/`startIndex`, sorting) | yaml `getRoomsFolder` parameters; 03_Rooms/Listing/RoomsFolderFilterTests.cs | CODE-ONLY (full set), TEST-CONFIRMED [run: PASS] (type/title/tags/subject/group) |
| Every listed room arrives as a folder with id, `roomType`, `pinned`, tags, logo (`entryKind` discriminator `folder`) | `products/ASC.Files/Core/ApiModels/ResponseDto/FileEntryDto.cs:64-73`; 03_Rooms/Listing/RoomsFolderTestBase.cs:103-137 (`ListRoomsAsync`); 03_Rooms/Pin/RoomPinTestsBase.cs:55-75; 03_Rooms/Tags/RoomTagDetachIntegrationTests.cs:256-276; 03_Rooms/Logos/RoomLogoDeleteTests.cs:303-314 | TEST-CONFIRMED [run: PASS] |
| Type filter | 03_Rooms/Listing/RoomsFolderFilterTests.cs:87-132; 03_Rooms/RoomsApiTests.cs:206-224 | TEST-CONFIRMED [run: PASS] |
| Title filter | 03_Rooms/Listing/RoomsFolderFilterTests.cs:136-209 | TEST-CONFIRMED [run: PASS] |
| Subject filters | 03_Rooms/Listing/RoomsFolderFilterTests.cs:331-390; 03_Rooms/RoomsApiTests.cs:229-275 | TEST-CONFIRMED [run: PASS] |
| Visibility by role; 401 cases | 03_Rooms/Listing/RoomsFolderVisibilityTests.cs:45-171 | TEST-CONFIRMED [run: PASS] |
| `getRoomInfo` 404 after deletion, idempotent reads, current metadata | 03_Rooms/Deletion/RoomDeleteTests.cs:67-69; 03_Rooms/Read/RoomInfoTests.cs:142-159; 03_Rooms/Listing/RoomsFolderFilterTests.cs:45-83 | TEST-CONFIRMED [run: PASS] |

## Revision 2026-11-06 (`1f1b052f7d..0ebdbb7c84`)

| Claim | Source | Status |
|---|---|---|
| Room group areas are Active, Forms and PrivateRooms; a private room in an Active group and an ordinary room in a PrivateRooms group answer 400 | 08_Private/PrivateRoomSectionTests.cs:217-238; `c866c055e1` | TEST-CONFIRMED [run: PASS] |
| New owner of a private room needs a key to its encrypted files (409) | `private-rooms`, "The new owner must hold the file keys"; `1259577f63` | CROSS-REFERENCE |
| Connected storage 400, editors that cannot encrypt 403, settings in the clear 400, no duplicate or template | `private-rooms` requirements of those names; `b562fc3fb1`, `bd89a24dcd` | CROSS-REFERENCE, not repeated here |

## Release notes versus server (specification follows the server)

| Release note | Server |
|---|---|
| v2.5: rooms on third-party storage "only for Public rooms"; the "Create a new folder in the storage" checkbox "only for Public room" | `createRoomThirdParty` accepts every `RoomType` 1-8 and `createAsNewFolder` for any of them (ThirdPartyRoomTests.cs:114-131, yaml). The restriction is a client choice. |
| v3.0: indexing, file lifetime, deny download and watermark are features of the Virtual Data Room | The API accepts `indexing` and `denyDownload` on a Custom room too (RoomUpdateVdrFieldsTests.cs:65-77, RoomUpdateTests.cs:145-172); the yaml puts no type restriction on any of the four. The client offers them in the VDR dialog only. |
| v4.0: "Private Room" presented as a room type | There is no `RoomType` value for it; `private` is a flag on a room of any type, and the v4.0 "Privacy rooms" note itself says the type is hidden in the client behind `localStorage.privateRooms`. |
| v2.6: duplicated rooms "copy the icon and tags; members are removed" | Confirmed in code, with more: colour, cover, indexing, lifetime, quota, watermark, deny-download settings and metadata are copied too, and a non-Custom copy gets a fresh primary external link (FolderDao.cs:1395-1441, FileMoveCopyOperation.cs:316-337). No test duplicates a room; the tests cover files and folders. |
| v2.6: pin limit of 10 | Confirmed, and AI rooms have a second, separate limit of 10 (RoomPinLimitTests.cs:157-204), which the notes do not mention. |
| v3.0: "public rooms shared by link are shown in the rooms list" | Confirmed by test for a Form filling room (bug 83228) and by the `getExternalShareData` contract for the general case; no .NET test exercises a Public room. |

## Deliberately left out

- **Room creation from files of My documents (v2.0)**, **room download (v2.0)**, **data reassignment on user
  deletion (v2.0)**: belong to file operations and account management, not to room types; no 03_Rooms test.
- **Form filling room specifics** (invitation to fill PDF forms, xlsx report, `sendFormToExternalDB`,
  `saveFormAsXLSX`, Connect Database, v2.6/v3.1/v3.7): the fields are accepted on create/update
  (RoomUpdateVdrFieldsTests.cs:171-199) but the workflow is specified in `form-filling-room-roles`.
- **VDR form filling by roles (v3.1)**: in `vdr-room-roles`.
- **Links** (primary links, Quick Sharing removal in v3.7.1, link roles in v3.0.1, embedding in v2.6):
  in `shared-links`.
- **Member roles, invitation, leaving, groups as members (v2.5), Room manager rename (v3.0)**: in
  `room-role-permissions`.
- **Room history / log report (v3.5 `folder/{id}/log/report`), AI chat inside rooms (v4.0), skills (v4.0,
  since 4.1.0 the Markdown files of an Instructions room: `getRoomAiFolder`, `getRoomSkills`,
  `getRoomsSkills`, 03_Rooms/AiHub/AiRoomSkillsTests.cs, RoomsSkillsTests.cs, RoomsSkillsLifecycleTests.cs,
  03_Rooms/Instructions/*), the sources and the model of an AI project (`getProjectSources`,
  `linkProjectSources`, `unlinkProjectSources`, `getProjectModel`, `setProjectModel`,
  03_Rooms/AiHub/AiProjectSources*Tests.cs, AiProjectModelTests.cs), the read access of a project member to
  the linked rooms (03_Rooms/AiHub/AiProjectMemberAccessTests.cs — stated in `room-role-permissions`)**:
  separate capabilities (`ai-agents`); the AI room and the AI rooms section appear here only as `RoomType`
  values with their settings, section and pin limits.
- **Third-party storage connection rules** (kDrive needs `url`, 03_Rooms/ThirdParty/ThirdPartyKDriveConnectBugTests.cs,
  bug 84672): the connection, not the room; `file-operations`.
- **"New" marks of a room** (03_Rooms/NewItems/*; bug 84797: listing a folder clears a file's mark, test red):
  `notifications` / `file-operations`.
- **`fields` projection of the member list** (03_Rooms/Sharing/RoomShareFieldsTests.cs, bug 84766): the API
  envelope, not a room-type rule.
- **Room tariff limit (402 on creation when the plan's room count is reached)**: stated by the yaml for
  `createRoom`/`createRoomThirdParty`, mentioned in v2.6.1 notes, not exercised by a test; omitted rather
  than written as NOTES-ONLY because the portal of the test fixture has no reachable limit.
- **Quota warning toasts and banners (v2.6.1, v3.7)**: client behaviour.
- **UI items** (room icons coloured in archive, tile design, tours, empty screens, "Share room" button):
  client behaviour with no server rule.
