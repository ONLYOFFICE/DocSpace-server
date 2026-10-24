# room-types — verification

Status values: **TEST-CONFIRMED** — an integration test asserts it; **CODE-ONLY** — settled from the product
code or the OpenAPI contract (`sdk/docspace-api-spec/docspace-backend.yaml`, cited by `operationId`), no
test; **NOTES-ONLY** — taken from the release notes without server evidence.

Paths are relative to `products/ASC.Files/Tests/Tests/` unless they start with `products/`, `common/` or
`sdk/`. `[Bug N]` marks a test carrying `[Trait("Bug", "N")]`; it is a regression link, not an open bug.

## Requirements and scenarios

| Requirement / scenario | Source | Status |
|---|---|---|
| **Room types offered by the API** — enum values 1, 2, 5, 6, 8, 9; no Private type; `roomType` immutable | yaml `RoomType` schema (`x-enum-varnames`: 1 `FillingFormsRoom`, 2 `EditingRoom`, 5 `CustomRoom`, 6 `PublicRoom`, 8 `VirtualDataRoom`, 9 `AiRoom`), `CreateRoomRequestDto.roomType` ("cannot be changed once the room exists"), `UpdateRoomRequest` has no `roomType`; `private` is a boolean of `CreateRoomRequestDto` | CODE-ONLY |
| Every type is created with its own value | 03_Rooms/Create/RoomCreateTests.cs:44-66; 03_Rooms/Read/RoomInfoTests.cs:45-65 | TEST-CONFIRMED |
| Unknown or missing room type is rejected (400) | 03_Rooms/Create/RoomCreateValidationTests.cs:100-130 | TEST-CONFIRMED |
| AI room carries chat settings, other types refuse them (400) | 03_Rooms/Chat/RoomChatSettingsTests.cs:53-92 [Bug 82798]; yaml `CreateRoomRequestDto.chatSettings` | TEST-CONFIRMED |
| Room info exposes only the flags that were set | 03_Rooms/Read/RoomInfoFieldsTests.cs:114-194 | TEST-CONFIRMED |
| Section and primary link per type (Forms / Active / AiAgents; primary link for Form filling and Public rooms) | 03_Rooms/FormFilling/FormFillingRoomSectionTests.cs:52-164; yaml `SearchArea` enum, `getRoomsFolder.searchArea`; roots `FolderType.Forms` (36) and `FolderType.VirtualRooms` (14); primary link on creation: `shared-links` spec, 03_Rooms/Links/RoomLinkPrimaryTests.cs | TEST-CONFIRMED (Forms/Active, primary link), CODE-ONLY (AiAgents) |
| **Who may create a room** | yaml `createRoom` 403 text | CODE-ONLY |
| Owner and DocSpace admin create a room | 03_Rooms/Permissions/RoomCreatePermissionsTests.cs:42-72; Room admin: 03_Rooms/RoomsApiTests.cs:245-246, 03_Rooms/Listing/RoomsFolderVisibilityTests.cs:45-58 | TEST-CONFIRMED |
| User and Guest cannot create a room (403, message) | 03_Rooms/Permissions/RoomCreatePermissionsTests.cs:75-106, 198-235 | TEST-CONFIRMED |
| Anonymous and terminated callers (401) | 03_Rooms/Permissions/RoomCreatePermissionsTests.cs:110-148 | TEST-CONFIRMED |
| Share list on creation is rejected (400) | 03_Rooms/Create/RoomCreateOptionalFieldsTests.cs:213-232 [Bug 81582]; yaml `CreateRoomRequestDto.share` ("Not implemented on room creation") | TEST-CONFIRMED |
| **Room title** — 170 max, sanitising, not unique | yaml `CreateRoomRequestDto.title` (maxLength 170), `UpdateRoomRequest.title` | CODE-ONLY |
| Blank or missing title on creation (400) | 03_Rooms/Create/RoomCreateValidationTests.cs:54-97 | TEST-CONFIRMED |
| Over-long title (400), 170 accepted | 03_Rooms/Create/RoomCreateValidationTests.cs:133-146; 03_Rooms/Update/RoomUpdateTitleTests.cs:142-178 | TEST-CONFIRMED |
| Forbidden characters are replaced | 03_Rooms/Create/RoomCreateTitleTests.cs:60-138; 03_Rooms/Update/RoomUpdateTitleTests.cs:99-121 | TEST-CONFIRMED |
| Duplicate titles are allowed | 03_Rooms/Create/RoomCreateTests.cs:99-112 | TEST-CONFIRMED |
| Blank title on update is a no-op | 03_Rooms/Update/RoomUpdateTests.cs:111-124; 03_Rooms/Update/RoomUpdateTitleTests.cs:76-92, 182-214; 03_Rooms/Read/RoomInfoTests.cs:182-194 | TEST-CONFIRMED |
| **Room tags** — catalogue, replace semantics, 255 limit, verbatim chars | yaml `CreateRoomRequestDto.tags`, `UpdateRoomRequest.tags` ("replaces the whole tag set") | CODE-ONLY |
| Tags given on creation; dedup; 50 tags; null | 03_Rooms/Create/RoomCreateOptionalFieldsTests.cs:133-179; 03_Rooms/Create/RoomCreateEdgeCaseTests.cs:77-90; 03_Rooms/Create/RoomCreateValidationTests.cs:225-234 | TEST-CONFIRMED |
| Update replaces the tag set | 03_Rooms/Update/RoomUpdateTagsColorCoverTests.cs:47-105 | TEST-CONFIRMED |
| Tag name limits (over-long tag 400; forbidden chars stored verbatim) | 03_Rooms/Update/RoomUpdateTagsColorCoverTests.cs:47-105; yaml `addRoomTags`, `deleteRoomTags` | TEST-CONFIRMED |
| Tag filter must be JSON (400); any-match; `withoutTags` | 03_Rooms/Listing/RoomsFolderFilterTests.cs:220-327 [Bug 81808] | TEST-CONFIRMED |
| Single-use tag disappears with its room; shared tag stays | 03_Rooms/Deletion/RoomDeleteTests.cs:160-200 | TEST-CONFIRMED |
| **Room colour, cover and logo** | yaml `CreateRoomRequestDto.color` (pattern), `.cover` (maxLength 50), `.logo`; `createRoomLogo`, `getRoomCovers` | CODE-ONLY |
| Colour set on creation and update; "" resets; null no-op | 03_Rooms/Create/RoomCreateOptionalFieldsTests.cs:182-191; 03_Rooms/Update/RoomUpdateTagsColorCoverTests.cs:109-131; 03_Rooms/Covers/RoomCoverValidationTests.cs:128-149 | TEST-CONFIRMED |
| Invalid colour (400) | 03_Rooms/Create/RoomCreateValidationTests.cs:188-198; 03_Rooms/Update/RoomUpdateTagsColorCoverTests.cs:135-175 [Bug 82364]; 03_Rooms/Covers/RoomCoverValidationTests.cs:46-87 [Bug 81558] | TEST-CONFIRMED |
| Cover set, replaced and cleared; unknown cover 400 | 03_Rooms/Update/RoomUpdateTagsColorCoverTests.cs:179-219; 03_Rooms/Create/RoomCreateValidationTests.cs:201-211 | TEST-CONFIRMED |
| Cover gallery (same for every role and language, contains `schedule`) | 03_Rooms/Covers/RoomCoverGalleryTests.cs:46-146 | TEST-CONFIRMED |
| Logo applied from an uploaded image | 03_Rooms/Logos/RoomLogoCreateTests.cs:48-218 | TEST-CONFIRMED |
| Logo refused on an archived (403), deleted (404) or foreign upload (403) | 03_Rooms/Logos/RoomLogoCreateTests.cs:129-218; 03_Rooms/Deletion/RoomDeleteAsyncTests.cs:200-219 | TEST-CONFIRMED |
| **Data-protection settings** — accepted on every type; defaults | yaml `CreateRoomRequestDto.indexing/denyDownload/lifetime/watermark`; defaults: 03_Rooms/Create/RoomCreateTests.cs:69-81 | TEST-CONFIRMED (defaults), CODE-ONLY (shape) |
| Shape of the four settings (lifetime `value` 1-999, `period`; watermark `text` ≤ 255, `additions`) | yaml `RoomDataLifetimePeriod` (0 Day, 1 Month, 2 Year), `WatermarkAdditions` bitmask (1 UserName, 2 UserEmail, 4 UserIpAdress, 8 CurrentDate, 16 RoomName), `RoomDataLifetime`, `WatermarkRequestDto` | CODE-ONLY |
| Settings given on creation of a VDR | 03_Rooms/Create/RoomCreateOptionalFieldsTests.cs:73-130 | TEST-CONFIRMED |
| All settings updated at once and toggled off | 03_Rooms/Update/RoomUpdateTests.cs:68-108; 03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:47-168 | TEST-CONFIRMED |
| Invalid lifetime (400) | 03_Rooms/Create/RoomCreateValidationTests.cs:172-185; 03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:161-167 | TEST-CONFIRMED |
| Settings accepted on a Custom room | 03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:65-77; 03_Rooms/Update/RoomUpdateTests.cs:145-172 | TEST-CONFIRMED |
| Wrongly typed field / unknown property (400) | 03_Rooms/Update/RoomUpdateTitleTests.cs:222-242; 03_Rooms/Update/RoomUpdateValidationTests.cs:128-139 [Bug 82365] | TEST-CONFIRMED |
| **Manual order and reorder** — dense 1..N, one sequence, nested untouched, idempotent | yaml `reorderRoom`, `CreateRoomRequestDto.indexing` | CODE-ONLY |
| Gaps are compacted | 03_Rooms/Reorder/RoomReorderTests.cs:90-117, 240-286 | TEST-CONFIRMED |
| Files and folders share one sequence; nested untouched; nothing lost | 03_Rooms/Reorder/RoomReorderTests.cs:143-237, 289-337 | TEST-CONFIRMED |
| Reorder of an archived (403) or missing room (404); non-indexed room OK | 03_Rooms/Reorder/RoomReorderTests.cs:350-449 [Bug 81862, 81863] | TEST-CONFIRMED |
| Reorder permissions | 03_Rooms/Permissions/RoomReorderPermissionsTests.cs:47-147 | TEST-CONFIRMED |
| **Index export of a VDR** | yaml `startRoomIndexExport`, `getRoomIndexExport`, `terminateRoomIndexExport` | CODE-ONLY |
| Owner or DocSpace admin starts the export; re-export after unarchive | 03_Rooms/Permissions/RoomIndexExportStartPermissionsTests.cs:47-69; 03_Rooms/Archive/RoomUnarchiveTests.cs:364-382 | TEST-CONFIRMED |
| Members without manager rights cannot export | 03_Rooms/Permissions/RoomIndexExportStartPermissionsTests.cs:75-128 | TEST-CONFIRMED |
| Archived (403) or unknown room (404) | 03_Rooms/IndexExport/RoomIndexExportValidationTests.cs:50-87 [Bug 82368, 82369] | TEST-CONFIRMED |
| Export completes with `resultFileId` | 03_Rooms/IndexExport/RoomIndexExportTests.cs:54-84 is `Skip`ped (document server unreachable in the integration-test profile); release note v3.0 ("generation of a summary report") | NOTES-ONLY |
| **Form filling rooms live in the Forms section** | release note v4.0 ("Forms are a separate root section"); yaml `FolderType` 36 Forms / 14 VirtualRooms, `SearchArea` Forms/FormTemplates | — |
| Form filling room is listed only in Forms (even with type filter) | 03_Rooms/FormFilling/FormFillingRoomSectionTests.cs:52-181 | TEST-CONFIRMED |
| Forms root folder (type 36, same id, lists form rooms only) | 03_Rooms/FormFilling/FormFillingRoomSectionTests.cs:185-267 | TEST-CONFIRMED |
| Used space is accounted to Forms / Archive | 03_Rooms/FormFilling/FormFillingRoomUsedSpaceTests.cs:52-160 | TEST-CONFIRMED |
| Form room templates have their own section; room from form template in Forms | 03_Rooms/FormFilling/FormFillingRoomTemplateSectionTests.cs:53-207 | TEST-CONFIRMED |
| Form room opened through its link is listed in Forms | 03_Rooms/FormFilling/FormFillingSectionBugTests.cs:282-314 [Bug 83228] | TEST-CONFIRMED |
| **Public room backed by third-party storage** — connected folder, every `RoomType` 1-8 | yaml `createRoomThirdParty`, `CreateThirdPartyRoomRequest`; release notes v2.5 ("only for Public rooms") | CODE-ONLY |
| New folder or the connected folder itself (`createAsNewFolder`, dropped `indexing`/`denyDownload`/`tags`/`logo`, string ids, `providerKey`/`providerId`) | yaml `CreateThirdPartyRoomRequest.createAsNewFolder`, `createRoomThirdParty` description; release note v2.5 ("Create a new folder in the storage"); 03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:50-111 (string ids, providerKey) | CODE-ONLY |
| Connected folder becomes the room; listed with providerKey | 03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:50-111, 154-170 (requires Nextcloud, `[Trait("Requires","Nextcloud")]`) | TEST-CONFIRMED |
| Every room type is accepted | 03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:114-131 (requires Nextcloud) | TEST-CONFIRMED |
| One connection, one room (403) | 03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:135-150 (requires Nextcloud) | TEST-CONFIRMED |
| Foreign connection (403); non-connected id is 4xx | 03_Rooms/ThirdParty/ThirdPartyRoomCreationBugTests.cs:51-153 [Bug 83301, 83306] | TEST-CONFIRMED |
| Deleting a third-party room disconnects the account | 03_Rooms/ThirdParty/ThirdPartyRoomTests.cs:174-197 (requires Nextcloud) | TEST-CONFIRMED |
| **A room reached through its link joins the caller's lists** | yaml `getExternalShareData` ("the first successful resolution puts the entry into the account's own lists"); release note v3.0 | CODE-ONLY |
| Member by link sees the room | Form filling room: 03_Rooms/FormFilling/FormFillingSectionBugTests.cs:282-314 [Bug 83228]; Public room: yaml text only (03_Rooms/Links/RoomLinkAnonymousAccessTests.cs:65-94 is skipped and covers the anonymous `shared` flag, not the listing) | TEST-CONFIRMED (form room), CODE-ONLY (public room) |
| **Room templates** — async job, per-caller status, parameters, edit rights on source | yaml `createRoomTemplate`, `getRoomTemplateCreatingStatus`, `RoomTemplateDto`; release note v3.1 | CODE-ONLY |
| Template created from a room; distinct ids; duplicate titles; in Templates listing | 03_Rooms/Templates/RoomTemplateCreateTests.cs:49-138; 03_Rooms/Templates/RoomTemplateContentTests.cs:189-259, 333-407 | TEST-CONFIRMED |
| Template options are stored (color, cover, tags, quota, public) | 03_Rooms/Templates/RoomTemplateContentTests.cs:51-145 | TEST-CONFIRMED |
| Source room cannot be used (404 / 403 archived) | 03_Rooms/Templates/RoomTemplateCreateTests.cs:175-269 [Bug 81691]; 03_Rooms/Templates/RoomTemplateCreateValidationTests.cs:53-100 [Bug 81690] | TEST-CONFIRMED |
| Callers without edit rights on the source (403); Room manager OK; 401 cases | 03_Rooms/Templates/RoomTemplateCreatePermissionsTests.cs:41-197 [Bug 81693] | TEST-CONFIRMED |
| Status does not leak between users | 03_Rooms/Templates/RoomTemplateCreateTests.cs:147-166 [Bug 81692] | TEST-CONFIRMED |
| **Template visibility and public setting** | yaml `getPublicSettings`, `setPublicSettings`, `RoomTemplateDto.public/share/groups` | CODE-ONLY |
| Non-public template is hidden from an unrelated user (403) | 03_Rooms/Templates/RoomTemplateContentTests.cs:262-280 | TEST-CONFIRMED |
| Public template visible to admin roles only | 03_Rooms/Templates/RoomTemplateContentTests.cs:285-329 | TEST-CONFIRMED |
| Shared template (share by email; groups accepted) | 03_Rooms/Templates/RoomTemplateContentTests.cs:148-185 | TEST-CONFIRMED |
| Public flag toggled; isolation; survives source deletion/archiving | 03_Rooms/Templates/RoomTemplateShareTests.cs:52-371 [Bug 81726, 81939, 81949]; 03_Rooms/Templates/RoomTemplatePublicSettingsTests.cs:52-80 [Bug 81938] | TEST-CONFIRMED |
| Public setting of an unusable id (404 deleted / non-template / unknown; 400 for 0, -1, non-integer) | 03_Rooms/Templates/RoomTemplateShareTests.cs:52-371 [Bug 81726, 81939, 81949] | TEST-CONFIRMED |
| **Creating a room from a template** — inheritance rules, independence, permissions | yaml `createRoomFromTemplate`, `CreateRoomFromTemplateDto` | CODE-ONLY |
| Room created from a Custom room template; usable afterwards | 03_Rooms/Templates/RoomFromTemplateTests.cs:50-67, 288-338, 406-457 | TEST-CONFIRMED |
| VDR settings partially inherited (type, indexing, denyDownload yes; lifetime, watermark no) | 03_Rooms/Templates/RoomFromTemplateTests.cs:71-236 | TEST-CONFIRMED |
| Content is copied with new ids; independence; source deleted/unchanged | 03_Rooms/Templates/RoomFromTemplateContentTests.cs:52-186 [Bug 81666]; 03_Rooms/Templates/RoomFromTemplateTests.cs:240-403 | TEST-CONFIRMED |
| Validation of the request (400 / 404) | 03_Rooms/Templates/RoomFromTemplateValidationTests.cs:47-247 [Bug 81667, 81669] | TEST-CONFIRMED |
| Who may create a room from a template: admin+public OK; User/Guest 403; admin on foreign private 403; source access only 403; 401 | 03_Rooms/Templates/RoomFromTemplatePermissionsTests.cs:44-230 [Bug 81662, 81663, 81664] | TEST-CONFIRMED |
| **Room groups** — name, icon, rooms, searchArea, per-account | yaml `addRoomGroup` (400/403/404 text), `RoomGroupRequestDto.searchArea`; release note v3.7 | CODE-ONLY |
| Group created over rooms of several types; private room grouped | 03_Rooms/Groups/RoomGroupCreateTests.cs:43-114, 262-291 | TEST-CONFIRMED |
| Name and icon validation (400; non-BMP name refused with 500; trimmed) | 03_Rooms/Groups/RoomGroupCreateNameIconValidationTests.cs:53-166 [Bug 80921]; 03_Rooms/Groups/RoomGroupCreateTests.cs:176-258 [Bug 82573] | TEST-CONFIRMED (the non-BMP refusal answers 500, not 400) |
| Rooms list validation (403 empty; 400 shapes; 404 none existing) | 03_Rooms/Groups/RoomGroupCreateRoomsValidationTests.cs:52-218 [Bug 82575, 82576, 82577, 82587] | TEST-CONFIRMED |
| Section of a group (Forms vs Active; refused areas; numeric value) | 03_Rooms/Groups/RoomGroupSearchAreaTests.cs:49-199 | TEST-CONFIRMED |
| Partially valid room list (403, group still created) | 03_Rooms/Groups/RoomGroupCreateRoomsValidationTests.cs:191-204; 03_Rooms/Groups/RoomGroupSearchAreaTests.cs:127-143 | TEST-CONFIRMED |
| Rooms added and removed; searchArea immutable; 404/400 on update | 03_Rooms/Groups/RoomGroupUpdateRoomsTests.cs:45-336 [Bug 82591, 82592, 82594, 82595]; 03_Rooms/Groups/RoomGroupSearchAreaTests.cs:203-269 | TEST-CONFIRMED |
| Group follows the rooms' lifecycle (archive, delete, rename); delete keeps rooms | 03_Rooms/Groups/RoomGroupLifecycleTests.cs:154-236 [Bug 82601]; 03_Rooms/Groups/RoomGroupDeleteTests.cs:74-85 | TEST-CONFIRMED |
| Groups are per account (404 read/update/icon, 403 delete; own groups fully managed; gated create) | 03_Rooms/Groups/RoomGroupCrossUserIsolationTests.cs:43-135 [Bug 82597]; 03_Rooms/Groups/RoomGroupOwnCapabilityTests.cs:59-165 [Bug 82598] | TEST-CONFIRMED |
| Listing rooms by group | 03_Rooms/Groups/RoomGroupSearchAreaTests.cs:273-285 | TEST-CONFIRMED |
| Deleting a group (404 repeat/bad ids; includeMembers) | 03_Rooms/Groups/RoomGroupDeleteTests.cs:43-190 [Bug 82596]; 03_Rooms/Groups/RoomGroupReadTests.cs:45-270 | TEST-CONFIRMED |
| Anonymous / invalid token (401) on every group call | 03_Rooms/Groups/RoomGroupAnonymousAccessTests.cs:45-214 | TEST-CONFIRMED |
| **Duplicating rooms** — background copy next to the original, numeric suffix, rights of item creation | yaml `duplicateBatchItems`; release note v2.6 | CODE-ONLY |
| Room duplicated — settings, logo, tags, metadata copied; primary link recreated for non-Custom; members not copied; room limit | products/ASC.Files/Core/Core/Dao/TeamlabDao/FolderDao.cs:1395-1441 (`CopyFolderAsync`: title via `GetAvailableTitleAsync`, Settings* copied, custom tags and metadata copied, no shares); products/ASC.Files/Core/Services/WCFService/FileOperations/FileMoveCopyOperation.cs:294-346 (`isRoomCopying`: `countRoomChecker.CheckAppend`, `roomLogoManager.CopyAsync`, primary external link re-shared when `FolderType != CustomRoom`); products/ASC.Files/Core/Services/WCFService/FileOperations/FileDuplicateOperation.cs:89-99; release note v2.6 | CODE-ONLY |
| File duplicated inside a room (Owner, Room manager, Content creator) | 06_Operations/Duplicate/DuplicateBatchItemsTests.cs:100-113; 06_Operations/Duplicate/DuplicatePermissionsTests.cs:85-147 | TEST-CONFIRMED |
| Rights below Content creator (403); anonymous 401 | 06_Operations/Duplicate/DuplicatePermissionsTests.cs:49-66, 107-124, 150-192 | TEST-CONFIRMED |
| Archived room, Trash (403) and unknown ids (404) | 06_Operations/Duplicate/DuplicateBatchItemsTests.cs:146-220 [Bug 82210] | TEST-CONFIRMED |
| Folder with content; file and folder together; suffix naming | 06_Operations/Duplicate/DuplicateBatchItemsTests.cs:48-130, 223-280; yaml `duplicateBatchItems` | TEST-CONFIRMED |
| **Pinning rooms** — personal, limit 10, AI limit, ordering, archive | yaml `pinRoom` description; release note v2.6 ("limit of pinned rooms = 10") | CODE-ONLY |
| Pin limit of ten; unpin frees a slot; no reset of all pins | 03_Rooms/Pin/RoomPinLimitTests.cs:127-155; 03_Rooms/Pin/RoomUnpinLimitTests.cs:47-105 [Bug 80757] | TEST-CONFIRMED |
| AI rooms have their own limit | 03_Rooms/Pin/RoomPinLimitTests.cs:157-204 [Bug 81852] | TEST-CONFIRMED |
| Pinned rooms stay on top (sort, filter, paging, stable order) | 03_Rooms/Pin/RoomPinOrderingTests.cs:47-222; 03_Rooms/RoomPinOrderTests.cs:47-75 | TEST-CONFIRMED |
| Pin is personal; does not survive archiving; idempotent; concurrent safe | 03_Rooms/Pin/RoomPinTests.cs:50-211; 03_Rooms/Pin/RoomPinValidationTests.cs:126-142; 03_Rooms/Pin/RoomPinLimitTests.cs:47-123 | TEST-CONFIRMED |
| Pin on archived (403), deleted (404), foreign room (403), unknown id (404, "The required folder was not found") | 03_Rooms/Pin/RoomPinValidationTests.cs:57-122 [Bug 81850]; 03_Rooms/Pin/RoomUnpinValidationTests.cs:52-111 [Bug 82366] | TEST-CONFIRMED |
| Who may pin (uninvited DocSpace admin OK; non-member User/Guest 403; anonymous/terminated 401) | 03_Rooms/Permissions/RoomPinPermissionsTests.cs:51-149 | TEST-CONFIRMED |
| Unpin never changes membership; unpin of never-pinned room 200 | 03_Rooms/Pin/RoomUnpinTests.cs:51-223 | TEST-CONFIRMED |
| Every room type can be pinned / unpinned (form room within Forms) | 03_Rooms/Pin/RoomPinRoomTypesTests.cs:46-70; 03_Rooms/Pin/RoomUnpinRoomTypesTests.cs:44-66 | TEST-CONFIRMED |
| **Archiving and restoring** | yaml `archiveRoom`, `unarchiveRoom`, `ArchiveRoomRequest.deleteAfter` | CODE-ONLY |
| Full cycle (sections, rootFolderType, content and metadata kept) | 03_Rooms/Archive/RoomArchiveTests.cs:48-171; 03_Rooms/Archive/RoomUnarchiveTests.cs:343-360; 03_Rooms/Read/RoomInfoTests.cs:105-138, 198-211 | TEST-CONFIRMED |
| Archived room is read-only (folder, file, rename, tags, invite, logo, quota, duplicate, pin, reorder, export) | 03_Rooms/Archive/RoomArchiveTests.cs:100-146 [Bug 81551]; 03_Rooms/Logos/RoomLogoCreateTests.cs:129-142; 07_Settings/Quota/RoomQuotaChangeTests.cs:82-98; 07_Settings/Quota/RoomQuotaResetTests.cs:79-99; 06_Operations/Duplicate/DuplicateBatchItemsTests.cs:180-196; 03_Rooms/Pin/RoomPinValidationTests.cs:108-122; 03_Rooms/Reorder/RoomReorderTests.cs:392-406; 03_Rooms/IndexExport/RoomIndexExportValidationTests.cs:74-87 | TEST-CONFIRMED |
| Who may archive and restore | 03_Rooms/Permissions/RoomArchivePermissionsTests.cs:44-118 [Bug 80938], 183-323 | TEST-CONFIRMED |
| Idempotent and no-op cases | 03_Rooms/Permissions/RoomArchivePermissionsTests.cs:157-171; 03_Rooms/Archive/RoomUnarchiveTests.cs:131-194 | TEST-CONFIRMED |
| Unknown room (404) and bad body (400) | 03_Rooms/Permissions/RoomArchivePermissionsTests.cs:122-153 [Bug 81550]; 03_Rooms/Archive/RoomArchiveTests.cs:175-201; 03_Rooms/Archive/RoomUnarchiveTests.cs:95-107, 202-269 | TEST-CONFIRMED |
| Restored room can be renamed and shared again | 03_Rooms/Archive/RoomUnarchiveTests.cs:300-339 | TEST-CONFIRMED |
| **Deleting a room** | yaml `deleteRoom`, `DeleteRoomRequest` | CODE-ONLY |
| Owner deletes a room of every type; operation finishes | 03_Rooms/Deletion/RoomDeleteTests.cs:50-111; 03_Rooms/Deletion/RoomDeleteAsyncTests.cs:72-160 [Bug 81698] | TEST-CONFIRMED |
| Content and links go with the room; cover/logo/quota rooms deletable; archived deletable; member loses access | 03_Rooms/Deletion/RoomDeleteTests.cs:115-312 | TEST-CONFIRMED |
| Who may delete | 03_Rooms/Permissions/RoomDeletePermissionsTests.cs:50-230 | TEST-CONFIRMED |
| Invalid ids (404) and body (400) | 03_Rooms/Deletion/RoomDeleteValidationTests.cs:48-148 [Bug 81697] | TEST-CONFIRMED |
| Deleted room cannot be revived (update 403, archive/delete/template 404) | 03_Rooms/Update/RoomUpdateValidationTests.cs:104-118; 03_Rooms/Permissions/RoomArchivePermissionsTests.cs:137-153; 03_Rooms/Deletion/RoomDeleteAsyncTests.cs:164-219; 03_Rooms/Templates/RoomTemplateCreateTests.cs:229-247 | TEST-CONFIRMED |
| **Changing the room owner and leaving the room** | yaml `changeFileOwner` description; release note v2.0; `room-role-permissions` (leave, User cannot own) | CODE-ONLY |
| Previous owner stays a member (keeps the room as Room manager; item already owned by the target returned untouched; private room needs keys) | yaml `changeFileOwner` description | CODE-ONLY |
| New owner must be an active account (403, message) | 04_Security/Sharing/GroupAndOwnerSharingTests.cs:90-101 [Bug 66897] | TEST-CONFIRMED |
| User cannot become the owner | `openspec/specs/room-role-permissions/spec.md` "User cannot own a room" | TEST-CONFIRMED (there) |
| **Per-room storage quota** | yaml `updateRoomsQuota`, `resetRoomQuota`, `CreateRoomRequestDto.quota`; release note v2.5 | CODE-ONLY |
| Custom quota set and reset; multiple rooms; zero accepted; isCustomQuota flags | 07_Settings/Quota/QuotaTests.cs:51-277; 07_Settings/Quota/RoomQuotaChangeTests.cs:49-141; 07_Settings/Quota/RoomQuotaResetTests.cs:48-146 | TEST-CONFIRMED |
| Quota given on creation; negative not stored verbatim; info reflects quotaLimit/usedSpace | 03_Rooms/Create/RoomCreateOptionalFieldsTests.cs:48-70; 03_Rooms/Create/RoomCreateValidationTests.cs:149-169; 07_Settings/Quota/RoomQuotaInfoTests.cs:65-107; 03_Rooms/Read/RoomInfoTests.cs:69-79 | TEST-CONFIRMED |
| Feature off (403), archived (403), wrong caller (403/401), empty list | 07_Settings/Quota/RoomQuotaChangeTests.cs:68-120 [Bug 82293]; 07_Settings/Quota/RoomQuotaResetTests.cs:65-121 [Bug 82293]; 07_Settings/Quota/RoomQuotaPermissionsTests.cs:54-112 | TEST-CONFIRMED |
| **Behaviour when the room quota is exceeded** — soft overrun once, hard limit at 2x, 402 on upload | 07_Settings/Quota/QuotaTests.cs:281-577; release notes v2.6.1, v3.7, v4.0 ("allow a small overrun when saving from the editor") | TEST-CONFIRMED |
| File within the quota | 07_Settings/Quota/QuotaTests.cs:281-339 | TEST-CONFIRMED |
| Quota smaller than the file | 07_Settings/Quota/QuotaTests.cs:342-361; 07_Settings/Quota/RoomQuotaInfoTests.cs:48-62 | TEST-CONFIRMED |
| Soft overrun allowed once; >2x refused; upload 402 "quota" | 07_Settings/Quota/QuotaTests.cs:364-577 | TEST-CONFIRMED |
| **Private rooms** — flag, keys required, immutable, hidden in client | yaml `CreateRoomRequestDto.private`, `createRoom` 403 ("`private` is set while the caller has no encryption keys"), `getRoomsFolder` private filter; release note v4.0 (Privacy rooms) | CODE-ONLY |
| Private room created by Owner / DocSpace admin / Room admin; logo applies; grouped | 03_Rooms/Permissions/RoomCreatePermissionsTests.cs:154-194 (via `CreatePrivateRoom`, Tests/BaseTest.cs:345-349, which sets keys first); 03_Rooms/Logos/RoomLogoCreateTests.cs:64-77; 03_Rooms/Groups/RoomGroupCreateTests.cs:103-113 | TEST-CONFIRMED |
| User and Guest cannot create a private room (403); anonymous 401 | 03_Rooms/Permissions/RoomCreatePermissionsTests.cs:198-251 | TEST-CONFIRMED |
| Default is not private | 03_Rooms/Create/RoomCreateTests.cs:69-81 | TEST-CONFIRMED |
| Keys required (403 without keys) | yaml `createRoom` 403 text only — no test creates a private room without keys | CODE-ONLY |
| **Listing rooms and reading a room** | yaml `getRoomsFolder` parameters, `getRoomInfo` | CODE-ONLY |
| Supported filters and paging (`type`, `filterValue`, `tags`/`withoutTags`, `subjectId`, `subjectOwnerId`+`excludeSubject`, `groupId`, `count`/`startIndex`, sorting) | yaml `getRoomsFolder` parameters; 03_Rooms/Listing/RoomsFolderFilterTests.cs | CODE-ONLY (full set), TEST-CONFIRMED (type/title/tags/subject/group) |
| Type filter | 03_Rooms/Listing/RoomsFolderFilterTests.cs:87-132; 03_Rooms/RoomsApiTests.cs:206-224 | TEST-CONFIRMED |
| Title filter | 03_Rooms/Listing/RoomsFolderFilterTests.cs:136-209 | TEST-CONFIRMED |
| Subject filters | 03_Rooms/Listing/RoomsFolderFilterTests.cs:331-390; 03_Rooms/RoomsApiTests.cs:229-275 | TEST-CONFIRMED |
| Visibility by role; 401 cases | 03_Rooms/Listing/RoomsFolderVisibilityTests.cs:45-171 | TEST-CONFIRMED |
| `getRoomInfo` 404 after deletion, idempotent reads, current metadata | 03_Rooms/Deletion/RoomDeleteTests.cs:67-69; 03_Rooms/Read/RoomInfoTests.cs:142-159; 03_Rooms/Listing/RoomsFolderFilterTests.cs:45-83 | TEST-CONFIRMED |

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
- **Room history / log report (v3.5 `folder/{id}/log/report`), AI chat inside rooms (v4.0), `.ai` folder
  and skills (v4.0)**: separate capabilities; the AI room appears here only as a `RoomType` value with its
  chat settings and its own pin limit.
- **Room tariff limit (402 on creation when the plan's room count is reached)**: stated by the yaml for
  `createRoom`/`createRoomThirdParty`, mentioned in v2.6.1 notes, not exercised by a test; omitted rather
  than written as NOTES-ONLY because the portal of the test fixture has no reachable limit.
- **Quota warning toasts and banners (v2.6.1, v3.7)**: client behaviour.
- **UI items** (room icons coloured in archive, tile design, tours, empty screens, "Share room" button):
  client behaviour with no server rule.
