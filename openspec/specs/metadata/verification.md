# metadata — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo root
(`server/`); `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`. Code: `Service` is
`products/ASC.Files/Core/Core/Metadata/MetadataService.cs`, `Controller` is
`products/ASC.Files/Server/Api/MetadataController.cs`, `Filter` is `products/ASC.Files/Core/Core/Metadata/MetadataFilter.cs`
(`MetadataFilterHelper`, `MetadataFilterConditionRequest`, `MetadataFilterOperators`), `Op` is
`products/ASC.Files/Core/Core/Metadata/Cascade/MetadataCascadeOperation.cs`, `Worker` is
`.../Cascade/MetadataCascadeWorker.cs`, `Resolver` is `.../Cascade/MetadataCascadeResolver.cs`, `Query` is
`products/ASC.Files/Core/Core/Search/MetadataSearchQuery.cs`, `Dto` is
`products/ASC.Files/Core/ApiModels/ResponseDto/MetadataDto.cs`, `Request` is
`products/ASC.Files/Server/ApiModels/RequestDto/MetadataRequestDto.cs`, `Types` is
`products/ASC.Files/Core/Core/Metadata/MetadataFieldType.cs`, `Notifier` is
`products/ASC.Files/Core/Core/Metadata/MetadataEntryNotifier.cs`, `Security` is
`products/ASC.Files/Core/Core/Security/FileSecurity.cs`, `FoldersController` is
`products/ASC.Files/Server/Api/FoldersController.cs`, `RoomsController` is
`products/ASC.Files/Server/Api/VirtualRoomsController.cs`, `EntryManager` is `products/ASC.Files/Core/Utils/EntryManager.cs`,
`FolderDao` / `FileDao` are `products/ASC.Files/Core/Core/Dao/TeamlabDao/FolderDao.cs` / `FileDao.cs`, `Socket` is
`products/ASC.Files/Core/Utils/SocketManager.cs`, `Backup` is
`common/ASC.Data.Backup.Core/Tasks/Modules/FilesModuleSpecifics.cs`, `Audit` is
`common/services/ASC.AuditTrail/AuditReportResource.resx`, `MessageAction` is
`common/ASC.Core.Common/Messaging/MessageAction.cs`. Tests: `T/` is `products/ASC.Files/Tests/Tests/10_Metadata/`,
`Listings` is `products/ASC.Files/Tests/ApiFactories/MetadataListings.cs`. Status: **TEST-CONFIRMED** (a test asserts
it), **CODE-ONLY** (read from the code or the OpenAPI document, no test), **NOTES-ONLY** (only the release notes say
it — none here: the notes up to v4.0 do not mention metadata). Exception → status: `SecurityException` 403,
`ArgumentException` 400, `ItemNotFoundException` 404, `AuthenticationException` 401
(`common/ASC.Api.Core/Middleware/ResponseWrapper.cs`).

The suite is 24 files: 21 integration classes (per-portal, through the generated SDK, raw HTTP where the SDK cannot
send the request) and 3 unit classes (`MetadataValidationTests`, `MetadataSearchQueryTests`,
`MetadataCascadeResolverTests`). `web/ASC.Web.Api.Tests` holds nothing on metadata.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Listing the metadata templates** | | |
| Any member, no right demanded; `visible` filter; system template excluded; creation order, fields in display order | `Controller:43-50`; `Service:191-194` (`includeSystem: false`); yaml `getTemplates` (19252-19336) | CODE-ONLY |
| `createBy`/`modifiedBy` are user objects with the owner's id and display name; creator is the last editor until a change | `T/MetadataTemplateManagementTests.cs:99-119`; `Dto:376-392` | TEST-CONFIRMED |
| The custom-field template and its fields are never listed | `T/MetadataCustomFieldsTests.cs:203-217` | TEST-CONFIRMED |
| **Creating a metadata template** | | |
| DocSpace admin only, 403 for Room admin and User | `T/MetadataTemplateManagementTests.cs:342-356`; `Service:1027-1034` | TEST-CONFIRMED |
| `visible` false kept on insert and read back | `T/MetadataTemplateManagementTests.cs:163-175` | TEST-CONFIRMED |
| `System` / `system` reserved (400) before and after the first custom field; custom fields still work | `T/MetadataCustomFieldsTests.cs:219-249`; `Service:1103-1121` | TEST-CONFIRMED |
| Name 256 chars → 400; field name 256 → 400; two fields `Client`/`client` → 400 | `T/MetadataRequestLimitsTests.cs:163-197`; `Service:1041-1073` | TEST-CONFIRMED |
| Fields validated as a batch, saved in one transaction with the template | `Service:86-110` | CODE-ONLY |
| Five concurrent creations: one 200, the rest 400, one template listed | `T/MetadataTemplateManagementTests.cs:312-340`; `Service:97-106` (duplicate key → `ArgumentException`) | TEST-CONFIRMED |
| **Reading one metadata template** | | |
| 404 for unknown, deleted or system template | `T/MetadataRequestLimitsTests.cs:225-255` (deleted → 404); `Service:861-870` | TEST-CONFIRMED (deleted), CODE-ONLY (system) |
| **Updating a metadata template** | | |
| DocSpace admin only, refused update changes nothing | `T/MetadataRightsTests.cs:51-70` | TEST-CONFIRMED |
| New name and `visible` false persisted; `modifiedBy` the caller | `T/MetadataTemplateManagementTests.cs:82-97, 99-119`; `Service:127-156` | TEST-CONFIRMED |
| Answer carries the fields | `T/MetadataTemplateManagementTests.cs:121-132`; `Service:131-153` | TEST-CONFIRMED |
| Name taken / too long 400, unknown 404 | `Service:133-150`; yaml `updateTemplate` (19496-19585) | CODE-ONLY |
| **Deleting a metadata template** | | |
| DocSpace admin only, template kept on refusal | `T/MetadataRightsTests.cs:72-91` | TEST-CONFIRMED |
| Gone from `getTemplates`, from the entries, `getTemplate` 404, `searchFolder` with the id 400 | `T/MetadataRequestLimitsTests.cs:225-255`; `Service:168-185`; `Filter:238-242` | TEST-CONFIRMED |
| **Field types and their options** | | |
| Five types; a number outside the set → 400 | `T/MetadataRequestLimitsTests.cs:151-161`; `Types:38-52`; `Service:986-1025` | TEST-CONFIRMED |
| Choice needs ≥1 option, non-blank, unique by lower case; options only on choice fields; ids generated | `Service:974-1025`; options returned with ids: `T/RoomsMetadataSearchTestsBase.cs:62-66` (used by every `data.OptionId`) | TEST-CONFIRMED (ids), CODE-ONLY (rules) |
| **Adding a field to a template** | | |
| Field added, template lists both | `T/MetadataRequestLimitsTests.cs:211-223` | TEST-CONFIRMED |
| `CLIENT` next to `Client` → 400 | `T/MetadataRequestLimitsTests.cs:199-209`; `Service:1076-1082` | TEST-CONFIRMED |
| Admin only (403) | `T/MetadataRightsTests.cs:93-123` | TEST-CONFIRMED |
| **Display order of the fields** | | |
| Unpositioned fields follow the positioned ones in request order | `T/MetadataFieldOrderTests.cs:45-63`; `Service:1084-1101` | TEST-CONFIRMED |
| New field without `order` gets highest + 1 (6 after 5 and 2) | `T/MetadataFieldOrderTests.cs:65-78` | TEST-CONFIRMED |
| Update with `order` moves, without keeps | `T/MetadataFieldOrderTests.cs:80-101`; `Service:272` | TEST-CONFIRMED |
| Entry metadata lists every field in template order, value or not | `T/MetadataFieldOrderTests.cs:103-124`; `Dto:405-422` | TEST-CONFIRMED |
| **Updating a field** | | |
| Name-only update keeps type (number) and type+options (choice), persisted | `T/MetadataTemplateManagementTests.cs:45-80` | TEST-CONFIRMED |
| Explicit type change on an empty field | `T/MetadataTemplateManagementTests.cs:134-145`; with values → 400: `Service:244-252` | TEST-CONFIRMED / CODE-ONLY |
| Unused option removable while another is selected; selected option → 400 | `T/MetadataTemplateManagementTests.cs:177-206`; `Service:253-268` | TEST-CONFIRMED |
| Field through another template's route → 404 | `T/MetadataTemplateManagementTests.cs:147-161`; `Service:959-968` | TEST-CONFIRMED |
| Admin only (403) | `T/MetadataRightsTests.cs:93-123` | TEST-CONFIRMED |
| **Deleting a field** | | |
| Field gone from the template and the entry, other value kept, condition on it → 400 | `T/MetadataLifecycleTests.cs:48-83`; `Service:288-298, 948-957` | TEST-CONFIRMED |
| Admin only (403) | `T/MetadataRightsTests.cs:93-123` | TEST-CONFIRMED |
| **Last-modified stamp of a template** | | |
| Moves on field add/rename/delete with `modifiedBy` the caller; not on a value write; one-second precision | `T/MetadataTemplateManagementTests.cs:208-249, 357-375` (`NextSecondAsync`) | TEST-CONFIRMED |
| **Reading the metadata of an entry** | | |
| Read right; 403 / 404; fields with `value` null when empty | `Service:429-434, 832-850`; `Dto:405-431` | CODE-ONLY |
| Viewer reads the file card with the value | `T/MetadataRightsTests.cs:125-169` | TEST-CONFIRMED |
| `cascade` and `conflictResolveType` on room / plain sub-folder / file | `T/MetadataCascadeAssignmentTests.cs:48-79`; `Dto:405-422` | TEST-CONFIRMED |
| Custom field named like a template field kept apart; system template not in `templates` | `T/MetadataCustomFieldsTests.cs:182-201`; `Service:436-482` | TEST-CONFIRMED |
| **The right to write the metadata of an entry** | | |
| File: edit right; folder (not room): create right; roots and knowledge folder refused; room: edit right | `Security:503-515`; `Service:832-850` | CODE-ONLY |
| Viewer: 403 on file values, room values, assign, unassign; nothing changed | `T/MetadataRightsTests.cs:125-169` | TEST-CONFIRMED |
| Editor: file assign+values 200; room values, folder values, folder custom fields, folder cascade switch 403 | `T/MetadataRightsTests.cs:171-214`; `T/MetadataCustomFieldsTests.cs:251-281` | TEST-CONFIRMED |
| Content creator: folder assign, values, cascade on 200 (`id` returned); room values 403 | `T/MetadataRightsTests.cs:216-245` | TEST-CONFIRMED |
| Owner on My documents root and Room admin on the rooms root: assign with cascade 403, custom fields 403 | `T/MetadataRightsTests.cs:247-298` | TEST-CONFIRMED |
| **Assigning templates to a file** | | |
| Editor assigns, template listed with empty fields | `T/MetadataRightsTests.cs:171-214`; `T/MetadataCascadeTests.cs:143-185` (empty instance linked) | TEST-CONFIRMED |
| Duplicates skipped (`Distinct`, upsert under a per-entry lock); unknown or system template 404 | `Service:483-512`; yaml `assignFileTemplates` (20142-20229) | CODE-ONLY |
| **Assigning templates to a folder or a room** | | |
| Without cascade: `{id:null, progress:100, isCompleted:true, error:null}`; progress endpoint reports the same | `T/MetadataCascadeTests.cs:440-453`; `Controller:302-331`; `Dto:433-449` | TEST-CONFIRMED |
| Empty list with cascade: completed operation, nothing assigned | `T/MetadataTemplateManagementTests.cs:298-310`; `Service:522-527` | TEST-CONFIRMED |
| Cascade queued: answer carries the template in `templateIds` | `T/MetadataCascadeAssignmentTests.cs:81-104` | TEST-CONFIRMED |
| **Unassigning a template from an entry** | | |
| Room unassigned: template gone from the room, file keeps template + inherited value; per-file unassign removes template with value | `T/MetadataCascadeTests.cs:265-300`; `Service:564-581, 659-690` | TEST-CONFIRMED |
| Only this folder's inherited links converted: file under a nested cascading folder stays inherited, its copy carries nothing | `T/MetadataCascadeTests.cs:302-343` | TEST-CONFIRMED |
| Link and values deleted in one transaction | `Service:670-690` (`DeleteLinkWithValuesAsync`) | CODE-ONLY |
| **Setting the values of template fields** | | |
| Answer is the whole entry metadata, earlier values and custom fields included | `T/MetadataTemplateManagementTests.cs:251-274`; `Service:740-748` | TEST-CONFIRMED |
| Same field twice → 400 | `T/MetadataTemplateManagementTests.cs:276-296`; `Service:697-701` | TEST-CONFIRMED |
| `{"values": null}` → 400 (raw HTTP) | `T/MetadataRequestLimitsTests.cs:122-133`; `Controller:555-563` | TEST-CONFIRMED |
| Empty value clears; room no longer found by it, other values still match | `T/RoomsMetadataSearchTests.cs:188-203` | TEST-CONFIRMED |
| Field of a template not assigned → 400; custom field through values → 400; unknown field → 404 | `Service:707-725`; yaml `setFileValues` (20721-20823) | CODE-ONLY |
| **Validation of a value by its field type** | | |
| Exactly one member per type; single choice one distinct option; unknown option refused; empty value passes | `T/MetadataValidationTests.cs:42-245` (unit, incl. `IsEmpty`); `Service:767-830` | TEST-CONFIRMED |
| 8001 chars → 400 (template field and custom field); 8000 stored and found by `eq` | `T/MetadataRequestLimitsTests.cs:286-343`; `Service:69, 795-800` | TEST-CONFIRMED |
| ±(2^53−1) accepted, one beyond refused | `T/MetadataValidationTests.cs:124-148` (unit); `Service:76, 810-813` | TEST-CONFIRMED |
| **Date values and their time zone** | | |
| Offset-less `2026-01-15T00:00:00` stored as 00:00 UTC and found by the day filter; local → UTC | `T/MetadataDateValuesTests.cs:46-75`; `T/MetadataValidationTests.cs:64-98` (unit); `Service:750-766` | TEST-CONFIRMED |
| **Custom fields of an entry** | | |
| Several at once, partial update leaves others, read agrees | `T/MetadataCustomFieldsTests.cs:45-89` | TEST-CONFIRMED |
| Case-insensitive name reuses the field, the created spelling is shown | `T/MetadataCustomFieldsTests.cs:61-74`; `Service:331-337` | TEST-CONFIRMED |
| Null value removes from this entry only; removing an unknown name is a no-op (200) | `T/MetadataCustomFieldsTests.cs:91-139` | TEST-CONFIRMED |
| Rename on one entry = remove + add, the portal field keeps its name | `T/MetadataCustomFieldsTests.cs:109-127` | TEST-CONFIRMED |
| Blank name 400; same name twice (any case) 400; null list 400 (raw); value 8001 → 400 | `T/MetadataCustomFieldsTests.cs:141-164`; `T/MetadataRequestLimitsTests.cs:135-149, 307-319`; `Service:891-930` | TEST-CONFIRMED |
| Name over 255 → 400 | `Service:911-914` | CODE-ONLY |
| **Limits and dictionary of the custom fields** | | |
| 51 fields → 400, nothing written | `T/MetadataCustomFieldsTests.cs:166-180`; `Service:57, 357-359` | TEST-CONFIRMED |
| Order across removals: `A,C,D` → `A,C,D,B` → `D,B,E` | `T/MetadataFieldOrderTests.cs:126-149`; `Service:377-383, 932-946` | TEST-CONFIRMED |
| Dropped name: filter by it 400 after the last value is removed, 200 before | `T/MetadataCustomFieldsTests.cs:304-331`; `Service:877-889`; `Filter:292-321` | TEST-CONFIRMED |
| Writes serialised under one lock per portal | `Service:308-310` | CODE-ONLY |
| **Cascade of a template over the content of a folder** | | |
| Nearest cascading folder wins, also in overwrite mode | `T/MetadataCascadeTests.cs:77-113`; `Op:197-259` | TEST-CONFIRMED |
| Only filled fields travel; empty folder field never overwrites; empty template linked | `T/MetadataCascadeTests.cs:143-185`; `Op:197-215` (`!v.IsEmpty`) | TEST-CONFIRMED |
| Snapshot: edit after the pass changes the folder only; new file inherits the edit; skip keeps, overwrite pushes | `T/MetadataCascadeTests.cs:187-230` | TEST-CONFIRMED |
| Back-to-back {A} then {A,B}: both reach; re-request after an edit pushes the edit (not folded into a started pass) | `T/MetadataCascadeTests.cs:47-75, 232-263`; `Worker:43-80`; `Op:104-184` (per-tree run lock) | TEST-CONFIRMED |
| New sub-folder in a cascading room found by the filter right away | `T/MetadataCascadeTests.cs:115-141`; `FolderDao:669-682` | TEST-CONFIRMED |
| **Progress of a cascade** | | |
| Idle folder: completed, empty `templateIds`; pass named by its templates | `T/MetadataCascadeAssignmentTests.cs:81-104`; `Worker:91-99`; `Dto:433-449` | TEST-CONFIRMED |
| Read right, 404 missing folder; failed pass completes with `error` | `Service:652-657`; `Op:163-172` | CODE-ONLY |
| **Switching the cascade of an assigned template** | | |
| Off keeps template and values on the room, file keeps inherited as own, later file inherits nothing, off again is a no-op | `T/MetadataCascadeAssignmentTests.cs:106-146`; `Service:583-601` | TEST-CONFIRMED |
| On queues a pass (`id`, `templateIds`), file receives the value, mode stored on the link | `T/MetadataCascadeAssignmentTests.cs:148-178`; `Service:602-641` | TEST-CONFIRMED |
| Template not on the folder, or unknown → 404 | `T/MetadataCascadeAssignmentTests.cs:180-195`; `Service:588-592` | TEST-CONFIRMED |
| **Inheritance when an entry is created or moved** | | |
| Created under a cascade: stamped inside the save transaction | `T/MetadataInheritanceOnMoveTests.cs:60-79`; `FileDao:715-727`; `FolderDao:863-876` | TEST-CONFIRMED |
| Moved in (batch path inside a room and per-file path from My documents): skip fills empty, overwrite replaces | `T/MetadataInheritanceOnMoveTests.cs:81-113`; `FileDao:1505-1534, 1607-1626` | TEST-CONFIRMED |
| Moved folder: itself stamped inline, content by a background stamp pass | `T/MetadataInheritanceOnMoveTests.cs:115-131`; `FolderDao:1316-1372`; `Op:261-333` | TEST-CONFIRMED |
| Moved cascading folder keeps its own cascade for its subtree, `Department` stays empty | `T/MetadataInheritanceOnMoveTests.cs:133-161`; `Op:286-295` | TEST-CONFIRMED |
| Moved out: inherited becomes own, a copy carries it (file and content of a moved folder) | `T/MetadataInheritanceOnMoveTests.cs:163-204`; `FolderDao:1328-1333` | TEST-CONFIRMED |
| Move to Trash stamps nothing | `FileDao:1505, 1607`; `FolderDao:1328` (`toFolderId != trashId`) | CODE-ONLY |
| **Metadata of a copy** | | |
| Copy into a cascading room keeps copied and inherited values | `T/MetadataCascadeTests.cs:345-393`; `FileDao:1754-1757` | TEST-CONFIRMED |
| Folder copy carries its own and its content's metadata; source untouched | `T/MetadataLifecycleTests.cs:183-225`; `FolderDao:1445-1448` | TEST-CONFIRMED |
| Only direct assignments copied | `T/MetadataCascadeTests.cs:302-343`; `T/MetadataInheritanceOnMoveTests.cs:163-182` | TEST-CONFIRMED |
| **Metadata through deletion, Trash and restore** | | |
| Deleted for good: 404 and not found | `T/MetadataLifecycleTests.cs:85-108`; `FileDao:1136-1137` | TEST-CONFIRMED |
| Trash keeps, emptied Trash drops (404, not found in My documents) | `T/MetadataLifecycleTests.cs:110-147`; `FolderDao:1155-1156` | TEST-CONFIRMED |
| Restored file keeps and is found; file in Trash found by the filter in Trash | `T/MetadataLifecycleTests.cs:149-181`; `T/MetadataMoveSearchTests.cs:67-93` | TEST-CONFIRMED |
| Deleted room: room and file 404, room not in the rooms filter | `T/MetadataLifecycleTests.cs:227-262` | TEST-CONFIRMED |
| **Assigned templates in the listings** | | |
| `getFileInfo`, `getFolderInfo` and the listing agree; empty before assignment | `T/MetadataRequestLimitsTests.cs:257-284`; `FoldersController:528-529`; yaml `assignedMetadataTemplates` (69713, 78252, 87345) | TEST-CONFIRMED |
| First template of the portal drops the "no templates" memory | `T/FoldersMetadataSearchTests.cs:417-436` | TEST-CONFIRMED |
| **Filtering a listing by metadata** | | |
| Template alone keeps the carriers, `total` 6 | `T/FoldersMetadataSearchTests.cs:132-147`; `T/RoomsMetadataSearchTests.cs:283-292`; `Query:74-105, 307-317` | TEST-CONFIRMED |
| Folder and file together, `total`/`count` 2; no match → empty and `total` 0 | `T/FoldersMetadataSearchTests.cs:86-130` | TEST-CONFIRMED |
| Subtree by default, not with `withSubFolders` false | `T/FoldersMetadataSearchTests.cs:149-191` | TEST-CONFIRMED |
| Invalid JSON, unknown field, field of another template, unknown template → 400 | `T/RoomsMetadataSearchTests.cs:205-306`; `T/MetadataTypedSearchTests.cs:74-85`; `Filter:233-290, 323-338` | TEST-CONFIRMED |
| Combined with room `type`, `searchArea` (archive found, active not), `filterValue`; invited user sees only their rooms | `T/RoomsMetadataSearchTests.cs:46-67, 134-175, 309-330` | TEST-CONFIRMED |
| Per-type conditions on rooms: string case-insensitive exact, number range inclusive/open, date range, choice OR, several conditions AND | `T/RoomsMetadataFieldFilterTests.cs:45-205`; `T/MetadataSearchQueryTests.cs:72-262` (unit predicate) | TEST-CONFIRMED |
| Third-party entries left out; index candidates confirmed against the database | `Query:74-127`; yaml `getFolderByFolderId` (16299-16343) | CODE-ONLY |
| **Access before the filter is validated** | | |
| Anonymous with unknown `metadataTemplateId` → 401 | `T/FoldersMetadataSearchTests.cs:398-415`; `FoldersController:450-459` | TEST-CONFIRMED |
| **Conditions and operators of a filter** | | |
| `contains` substring case-insensitive, `eq` whole value; typed and listing agree | `T/MetadataFilterOperatorsTests.cs:50-82` | TEST-CONFIRMED |
| `gt`/`gte`/`lt`/`lte` on numbers; typed agrees | `T/MetadataFilterOperatorsTests.cs:98-148` | TEST-CONFIRMED |
| Choice `in` combines with OR, multi-choice room returned once | `T/RoomsMetadataFieldFilterTests.cs:145-177` | TEST-CONFIRMED |
| Refused forms: `contains` on number, `gt` on string, `gt` with `from`, `gt` without value, `eq` with bound, `range` with value (number and date), bound 2^53, `yesterday`, `startswith`, `range` on string, >20 conditions | `T/MetadataFilterOperatorsTests.cs:83-96, 150-221, 276-299`; `T/RoomsMetadataSearchTests.cs:233-262`; `T/FoldersMetadataSearchTests.cs:193-208`; `T/MetadataRequestLimitsTests.cs:98-120`; `Filter:340-600` | TEST-CONFIRMED |
| Custom field by `name` (listing and `searchRooms`); neither id nor name → 400 | `T/MetadataCustomFieldsTests.cs:283-302`; `T/MetadataTypedSearchTests.cs:169-193`; `Filter:292-321` | TEST-CONFIRMED (name), CODE-ONLY (neither) |
| JSON numbers accepted in body and query | `T/MetadataTypedSearchTests.cs:87-124`; `Filter:157-180` | TEST-CONFIRMED |
| Defaults of an omitted `op` | `Filter:113-121, 340-436` | CODE-ONLY |
| **Date bounds of a condition** | | |
| Day-only `to` covers the day; a `to` with time is an instant | `T/MetadataDateValuesTests.cs:77-103` | TEST-CONFIRMED |
| `gt`/`gte`/`lt`/`lte` with day-only and timed bounds | `T/MetadataFilterOperatorsTests.cs:223-274`; `Filter:526-578` | TEST-CONFIRMED |
| **Sections that take or refuse the metadata filter** | | |
| Recent, Favorites, Shared with me by id: template and condition, `total` | `T/MetadataSectionFiltersTests.cs:50-205`; `EntryManager:402-508` | TEST-CONFIRMED |
| `@my`, `@trash`, `@share`, `@recent`, `@favorites`, `@forms`; unknown template 400 on alias | `T/MetadataSectionAliasFiltersTests.cs:48-186`; `T/MetadataSectionFiltersTests.cs:91-116`; `FoldersController:877-1033` | TEST-CONFIRMED |
| Templates section 400 by id, `@templates` alias (raw) and `searchFolder` | `T/MetadataSectionFiltersTests.cs:207-221`; `T/MetadataSectionAliasFiltersTests.cs:188-200`; `T/MetadataTypedSearchTests.cs:126-136`; `EntryManager:402-409` | TEST-CONFIRMED |
| Private room section refused too | `EntryManager:405-408` (`FolderType.Privacy`) | CODE-ONLY |
| **Typed metadata search** | | |
| Agrees with the listing (ids and `total`) | `T/MetadataTypedSearchTests.cs:46-72`; `T/MetadataFilterOperatorsTests.cs:70-82, 138-148` | TEST-CONFIRMED |
| `count` 101/0/-1/absent, `startIndex` -1 → 400 on `searchFolder`; 101/-1 on `searchRooms` | `T/MetadataRequestLimitsTests.cs:50-96`; `Request` (`[Range(1, 100)]`, `[Range(0, int.MaxValue)]`) | TEST-CONFIRMED |
| Recent through the body; Templates 400 | `T/MetadataTypedSearchTests.cs:126-167` | TEST-CONFIRMED |
| Body fields and routes | `Controller`-side `FoldersController:481-497`, `RoomsController:1375-1401`; yaml `searchFolder` (16432-16500), `searchRooms` (32701-32760) | CODE-ONLY |
| **String metadata values in the free text search** | | |
| Found by a custom field, match with null ids (sub-folder and room) | `T/FoldersMetadataSearchTests.cs:253-274`; `T/RoomsMetadataSearchTests.cs:69-89` | TEST-CONFIRMED |
| Found by a template field, match with template/field ids and the whole value | `T/FoldersMetadataSearchTests.cs:276-296`; `T/RoomsMetadataSearchTests.cs:91-111` | TEST-CONFIRMED |
| Two matches for two fields; no matches without text | `T/FoldersMetadataSearchTests.cs:298-316, 383-396` | TEST-CONFIRMED |
| Extension filter still applies to metadata matches | `T/FoldersMetadataSearchTests.cs:232-251`; `FileDao:3036-3060` | TEST-CONFIRMED |
| First custom field of the portal drops the "no templates" memory of the text search | `T/FoldersMetadataSearchTests.cs:438-460` | TEST-CONFIRMED |
| **Hidden templates and the text search** | | |
| Hidden value found only with `includeHiddenMetadata` (listing, typed, rooms) | `T/FoldersMetadataSearchTests.cs:318-343`; `T/RoomsMetadataSearchTests.cs:113-132`; `Query:319-339` | TEST-CONFIRMED |
| Found by title either way, hidden match reported only on request | `T/FoldersMetadataSearchTests.cs:345-364` | TEST-CONFIRMED |
| Condition on a hidden field applies without the flag | `T/FoldersMetadataSearchTests.cs:366-381` | TEST-CONFIRMED |
| **The stored values are the truth of a filter** | | |
| Unassigned folder absent on the very next single request, file with own value kept | `T/FoldersMetadataSearchTests.cs:210-230`; `Query:74-127` (candidates confirmed in SQL) | TEST-CONFIRMED |
| Moved file found in its new folder only (index not scoped by the tree) | `T/MetadataMoveSearchTests.cs:47-65, 95-110`; `Query:44-46` | TEST-CONFIRMED |
| **Metadata reached through an external link** | | |
| Read of file and folder through the room link | `T/MetadataExternalLinkTests.cs:48-62`; `Controller:220-259` (`[AllowAnonymous]`) | TEST-CONFIRMED |
| No session, no key → 401 on read and on file values | `T/MetadataExternalLinkTests.cs:64-78, 136-147`; `Service:832-838` | TEST-CONFIRMED |
| Link of another room → 403 | `T/MetadataExternalLinkTests.cs:80-94` | TEST-CONFIRMED |
| Editing link writes file values, read link 403 and nothing changed | `T/MetadataExternalLinkTests.cs:96-134` | TEST-CONFIRMED |
| Folder values, unassign, custom fields → 401 for anonymous | `T/MetadataExternalLinkTests.cs:149-168`; `Controller` (no `[AllowAnonymous]` on 353-540) | TEST-CONFIRMED |
| **Clients are told about metadata changes** | | |
| `update-file` / `update-folder` after every write; cascade, template and field deletions notify affected entries by parent folder recipients | `Service:1149-1160, 1130-1148`; `Notifier:35-103`; `Op:323, 348, 387`; `Socket:86-93` | CODE-ONLY |
| **Audit trail of the metadata actions** | | |
| Eight event texts with the object's name after one session | `T/MetadataAuditTests.cs:47-91`; `Audit:1285-1312`; `MessageAction:1137-1170` | TEST-CONFIRMED |
| Custom field write audited as "Metadata values updated"; cascade start as "Metadata cascade assignment started" | `Service:295, 399, 461`; `Audit:1309-1312` | CODE-ONLY |
| **Metadata in backup and restore** | | |
| Four tables declared with user and date columns; template/field ids remapped, links and values follow template, field and entry ids, `source_folder_id` remapped | `Backup:78-100, 150-162` | CODE-ONLY |

## Where the server differs from the OpenAPI document or the SDK (spec follows the server)

| Document / client | Server |
|---|---|
| yaml `setFileValues` / `setFolderValues` 400 text: "A value does not match the field type, a field is listed twice or is a custom field, or …" | `Controller:426, 461` adds "or exceeds the number limit" — the number bound (commit 5ec6f92594) landed after the contract was generated; the published document is one revision behind. |
| yaml `getCascadeProgress` declares 200, 404 and the generic 401 only | `Service:652-657` demands the read right on the folder, so a folder the caller cannot read answers 403 (`SecurityException`); the row is missing. |
| yaml routes `…/customfields` (lower case) | The controller publishes `customFields` (`Controller:490, 516`); routing is case-insensitive, both forms work. |
| yaml `assignFileTemplates` 400 "has no `templateIds`" | Only a body without the `templateIds` key (required) is refused; an empty list is accepted and answers 200 after writing nothing (see MD4). |
| SDK `FolderContentDto.Folders`/`Files` typed as the base entry without `id`, `assignedMetadataTemplates`, `metadataMatches` | The server returns them (`FileEntryDto:352, 360`); the tests re-read the raw JSON (`Listings:40-47`). |
| SDK `CustomFieldRequest.value` non-nullable; `FolderMetadataSearch.Count` left out of the body when 0; `MetadataFilterConditionRequest.value/from/to` strings only; `SetMetadataValues`/`SetCustomFields` refuse a null list client-side | The server accepts `"value": null` (removal), refuses a missing `count` with 400, accepts JSON numbers in the conditions and refuses a null list with 400 — the tests use raw HTTP for these (`T/MetadataRequestLimitsTests.cs:50-96, 122-149`; `T/MetadataTypedSearchTests.cs:87-124`). |
| SDK has no method for `@share` and `@templates` | Both aliases exist on the server and take the filter / refuse it (`T/MetadataSectionAliasFiltersTests.cs:133-200`, raw HTTP). |
| yaml `MetadataFilterConditionRequest.fieldId` is a plain integer with no "or `name`" note in its own description | The helper treats `fieldId` 0 with a `name` as a custom field condition (`Filter:292-321`); the operation descriptions say so, the schema property does not. |
| Release notes v1.1 – v4.0 (Privacy rooms included) | No mention of metadata, templates or custom fields: the feature is unreleased at the time of writing, so nothing is NOTES-ONLY. |
| Test comments "used to…" (default type String on update, position 0, filter dropped on aliases, worker folding requests, un-cascade 500, extension slip, stale documents) | All describe fixed behaviour; the tests assert the fixed state and the spec describes it. |

## Left out of the spec, and why

- **AI generation and filling of metadata** (`POST api/2.0/ai/meta/generate`, `/fill`): owned by `ai-agents`
  ("Metadata fields proposed for a file", "Metadata values filled from a file", "The model and the failures of the
  metadata routes"); cross-referenced in the Purpose only.
- **Index mechanics**: the OpenSearch metadata documents, the `QueryLimit` overflow fallback, the per-entry
  re-indexing, the best-effort document writes (`Query:35-127`, `MetadataIndexHelper`, `MetadataSearchItem.cs`); only
  their observable rule ("the stored values are the truth") is specified.
- **Cascade internals**: the per-tree run lock and its one-hour timeout, batch size 1000, the reconciliation of a
  pass that outlived a template/field change or an un-cascade (`Op:435-520`), the dedupe rule of the queue
  (`Worker:43-80`), the nearest-source resolver (`Resolver`, unit-tested in `T/MetadataCascadeResolverTests.cs`):
  implementation behind the "nearest cascading ancestor wins" requirement.
- **The portal-wide custom-field lock and the per-entry link lock** (`Service:77-84, 308`): concurrency plumbing.
- **The `MetadataTemplatesCache` "has templates" shortcut** (`FolderDao:299, 864`; `FileDao:716, 1505`): performance;
  the two tests on the first template / first custom field pin its invalidation and are listed under the listing and
  text-search requirements.
- **Third-party storage entries**: string ids never reach the metadata routes (`{fileId:int}`); the listings leave
  them out when a filter is set. Named in one requirement, not specified further.
- **`FormMetadata` of the OForm service and `Metadata.cs` of the encryption library**: unrelated namesakes.
- **Rate-limit headers, 429/5xx rows** of the yaml: generic policy.
- **Socket event payloads and the recipients rule of the notifier** beyond "the ordinary update event": client contract
  of `socket-events`.

## Suspected defects

| # | Where | What the server does | Evidence |
|---|-------|----------------------|----------|
| MD1 | Updating a field | A type change between single and multiple choice is refused with 400 "values exist" like any other retype, although the two kinds share the option rows (the cascade reconciliation treats them as the same kind). A choice field retyped to another type must also be sent `options: []`, or the kept options fail with "Options are supported by choice fields only". | `Service:244-271, 986-1025`; `Op:512-519` |
| MD2 | Progress of a cascade | The stamp pass queued by a move is invisible: `getCascadeProgress` reports assign passes only, the stamp pass is queued with an empty template list and no audit event, so a client cannot tell that a moved folder's content is still being stamped. | `FolderDao:1365-1372`; `Worker:91-99`; `Op:261-333` |
| MD3 | Progress of a cascade | The `error` of a failed pass is the raw exception message, returned to any member who may read the folder; a storage or SQL failure text reaches the client. | `Dto:433-449`; `Op:163-168` |
| MD4 | Assigning templates to a file | An empty `templateIds` on a file is accepted: nothing is linked, yet the audit event "Metadata template assigned" and the socket update are sent; the folder variant returns early for the same input. | `Service:483-512` vs `522-527` |
| MD5 | Switching the cascade | Turning a cascade off writes no audit event, while turning it on writes "Metadata cascade assignment started" and the unassignment writes "unassigned"; the only silent write of the API. | `Service:596-601` |
| MD6 | Custom fields | A request that only removes names the entry does not hold (after the system template exists) is a no-op that still writes "Metadata values updated", sends the socket event and re-indexes the entry. | `Service:361-381, 388-390` |
| MD7 | Listing filter validation | On the section listings and the rooms listing the filter is parsed before any access check (only the folder listing checks first); they are not anonymous, so a signed-in member of any kind can probe which template and field identifiers exist on the portal through the 400 texts. | `FoldersController:877-1033`; `RoomsController:1366, 1400` vs `450-459` |
| MD8 | Cascade and copy | The copy of a file pastes the source's direct metadata after the copy was created and stamped; the copy keeps both (tested), but the source's values are written without the destination's conflict mode, so an overwrite-cascading destination does not overwrite the copied values the way it overwrites a moved file's. | `FileDao:1754-1757`; `T/MetadataCascadeTests.cs:345-393` vs `T/MetadataInheritanceOnMoveTests.cs:99-113` |
| MD9 | Contract | The published document predates the number-value limit and lacks the 403 of `getCascadeProgress` (see deviations); agents reading the yaml see an incomplete error set. | yaml 20332-20424, 20721-20823 |
