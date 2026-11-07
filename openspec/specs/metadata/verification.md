# metadata — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo root
(`server/`); `yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`. Code: `Service` is
`products/ASC.Files/Core/Core/Metadata/MetadataService.cs`, `Controller` is
`products/ASC.Files/Server/Api/MetadataController.cs`, `Filter` is `products/ASC.Files/Core/Core/Metadata/MetadataFilter.cs`
(`MetadataFilterHelper`, `MetadataFilterConditionRequest`, `MetadataFilterOperators`), `Op` is
`products/ASC.Files/Core/Core/Metadata/Cascade/MetadataCascadeOperation.cs`, `Worker` is
`.../Cascade/MetadataCascadeWorker.cs`, `Resolver` is `.../Cascade/MetadataCascadeResolver.cs`, `Query` is
`products/ASC.Files/Core/Core/Search/MetadataSearchQuery.cs`, `Dto` is
`products/ASC.Files/Core/ApiModels/ResponseDto/MetadataDto.cs`, `EntryDto` is
`products/ASC.Files/Core/ApiModels/ResponseDto/FileEntryDto.cs`, `Request` is
`products/ASC.Files/Server/ApiModels/RequestDto/MetadataRequestDto.cs`, `Types` is
`products/ASC.Files/Core/Core/Metadata/MetadataFieldType.cs`, `Notifier` is
`products/ASC.Files/Core/Core/Metadata/MetadataEntryNotifier.cs`, `Security` is
`products/ASC.Files/Core/Core/Security/FileSecurity.cs`, `FoldersController` is
`products/ASC.Files/Server/Api/FoldersController.cs`, `RoomsController` is
`products/ASC.Files/Server/Api/VirtualRoomsController.cs`, `EntryManager` is `products/ASC.Files/Core/Utils/EntryManager.cs`,
`FolderDao` / `FileDao` / `MetadataDao` are `products/ASC.Files/Core/Core/Dao/TeamlabDao/FolderDao.cs` / `FileDao.cs` /
`MetadataDao.cs`, `Queries` is `products/ASC.Files/Core/Core/EF/Queries/MetadataQueries.cs`, `Socket` is
`products/ASC.Files/Core/Utils/SocketManager.cs`, `Backup` is
`common/ASC.Data.Backup.Core/Tasks/Modules/FilesModuleSpecifics.cs`, `Audit` is
`common/services/ASC.AuditTrail/AuditReportResource.resx`, `MessageAction` is
`common/ASC.Core.Common/Messaging/MessageAction.cs`, `Migration` is
`migrations/mysql/SaaS/MigrationContext/20260926090000_MigrationContext_Upgrade100.cs`. Tests: `T/` is
`products/ASC.Files/Tests/Tests/10_Metadata/`, `Listings` is `products/ASC.Files/Tests/ApiFactories/MetadataListings.cs`.
Status: **TEST-CONFIRMED** (a test asserts it), **CODE-ONLY** (read from the code or the OpenAPI document, no test),
**NOTES-ONLY** (only the release notes say it — none here: the notes up to v4.0 do not mention metadata and none exist
for v4.1.0). Exception → status: `SecurityException` 403, `ArgumentException` 400, `ItemNotFoundException` 404,
`AuthenticationException` 401 (`common/ASC.Api.Core/Middleware/ResponseWrapper.cs`).

Revised after the merge of `release/v4.1.0` (`3533ad8a89`, previous state `d58816bece`). The suite is 45 files: 40
integration classes (per-portal, through the generated SDK, raw HTTP where the SDK cannot send the request), 2 shared
bases (`MetadataApiTestBase`, `MetadataFieldTypeTestBase`) and 3 unit classes (`MetadataValidationTests`,
`MetadataSearchQueryTests`, `MetadataCascadeResolverTests`). The 4.1.0 range added 21 files: the template, field and
entry API suites (`MetadataTemplate*Tests`, `MetadataFieldManagementTests`, `MetadataFile*Tests`,
`MetadataFolder*Tests`, `MetadataEntryPermissionsTests`), the per-type value matrix run against a file and a folder
(`Metadata*ValueTests`, `MetadataValueBatchTests`), `MetadataHasFieldsFlagTests` and `MetadataUnicodeTests`.
`web/ASC.Web.Api.Tests` holds nothing on metadata.

What 4.1.0 changed in the product (commit `857b062058`): a template needs at least one field (creation without
fields 400, deletion of the last field 400), `hasMetadataFields` is answered on every listing entry, the info
endpoints and the entry metadata reads, and hiding or showing a template notifies the clients of the entries it is
assigned to. Commit `7d641bf729` added the `entryKind` discriminator to the listing entries, `67b8986472` the
`total` of every paginated wrapper, `09cfe2c5ed`/`32ad89fb81` changed the index refresh of the worker (no visible
rule changed: the tests poll).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Listing the metadata templates** | | |
| Any member, no right demanded; system template excluded; fields in display order | `Controller:57`; `Service:210-213` (`includeSystem: false`); yaml `getTemplates` (19613) | CODE-ONLY |
| `visible` filter narrows to the shown or the hidden templates; creation order | `T/MetadataTemplateReadUpdateDeleteTests.cs:43-67` | TEST-CONFIRMED [run: PASS] |
| Readable by a Room admin and a User (`getTemplates`, `getTemplate`) | `T/MetadataTemplatePermissionsTests.cs:121-132` | TEST-CONFIRMED [run: PASS] |
| `createBy`/`modifiedBy` are user objects with the owner's id and display name; creator is the last editor until a change; dates present; fields ascending by `order` with the template id | `T/MetadataTemplateManagementTests.cs:139-159`; `T/MetadataTemplateChangesTests.cs:43-58`; `Dto:392-404` | TEST-CONFIRMED [run: PASS] |
| The custom-field template and its fields are never listed | `T/MetadataCustomFieldsTests.cs:203-217` | TEST-CONFIRMED [run: PASS] |
| **Creating a metadata template** | | |
| DocSpace admin only, 403 for Room admin, User and Guest; the name is not listed afterwards | `T/MetadataTemplateManagementTests.cs:382-396`; `T/MetadataTemplatePermissionsTests.cs:43-71`; `Service:1055-1063` | TEST-CONFIRMED [run: PASS] |
| No fields (empty list or member left out) → 400, nothing created | `T/MetadataTemplateManagementTests.cs:44-60`; `Service:99, 1070-1076` | TEST-CONFIRMED [run: PASS] |
| Blank, whitespace or `null` name → 400, count unchanged (null: raw HTTP) | `T/MetadataTemplateCreateTests.cs:109-132`; `Service:1083-1094` | TEST-CONFIRMED [run: PASS] |
| Unicode and special characters in the name kept; 255 characters accepted | `T/MetadataTemplateCreateTests.cs:82-91, 134-142` | TEST-CONFIRMED [run: PASS] |
| `visible` false kept on insert and read back | `T/MetadataTemplateManagementTests.cs:203-215` | TEST-CONFIRMED [run: PASS] |
| `System` / `system` reserved (400) before and after the first custom field; custom fields still work | `T/MetadataCustomFieldsTests.cs:219-249`; `Service:1140-1158` | TEST-CONFIRMED [run: PASS] |
| Name 256 chars → 400; name taken in another case → 400 and the one template stays; two fields `Client`/`client` → 400 | `T/MetadataRequestLimitsTests.cs:163-197`; `T/MetadataTemplateCreateTests.cs:93-107`; `Service:1083-1111` | TEST-CONFIRMED [run: PASS] |
| Fields validated as a batch (an invalid choice field rejects the whole request), saved in one transaction with the template; every type with its options read back | `T/MetadataTemplateCreateTests.cs:43-69, 144-167`; `Service:101-117` | TEST-CONFIRMED [run: PASS] |
| Five concurrent creations: one 200, the rest 400, one template listed; a template is listed once | `T/MetadataTemplateManagementTests.cs:352-380`; `T/MetadataTemplateCreateTests.cs:71-80`; `Service:114-123` (duplicate key → `ArgumentException`) | TEST-CONFIRMED [run: PASS] |
| **Reading one metadata template** | | |
| 404 for an unknown identifier, 0, -1, a deleted or the system template | `T/MetadataTemplateReadUpdateDeleteTests.cs:69-79`; `T/MetadataRequestLimitsTests.cs:225-255` (deleted); `Service:889-899` | TEST-CONFIRMED [run: PASS] (unknown, deleted), CODE-ONLY (system) |
| **Template routes and anonymous callers** | | |
| The eight template and field routes answer 401 to an anonymous caller and change nothing | `T/MetadataTemplatePermissionsTests.cs:134-159` | TEST-CONFIRMED [run: PASS] |
| **Updating a metadata template** | | |
| DocSpace admin only; Guest refused; a refused update changes name, visibility and fields nothing | `T/MetadataRightsTests.cs:51-70`; `T/MetadataTemplatePermissionsTests.cs:73-118` | TEST-CONFIRMED [run: PASS] |
| New name and `visible` false persisted; name left out kept; `modifiedBy` the caller, `createBy` the creator | `T/MetadataTemplateManagementTests.cs:122-137, 139-159`; `T/MetadataTemplateReadUpdateDeleteTests.cs:81-92`; `T/MetadataTemplateChangesTests.cs:60-74`; `Service:130-181` | TEST-CONFIRMED [run: PASS] |
| Answer carries the fields | `T/MetadataTemplateManagementTests.cs:161-172`; `Service:136-166` | TEST-CONFIRMED [run: PASS] |
| Own name 200; another's name in another case 400, name kept; 256 characters 400, name kept; unknown 404 | `T/MetadataTemplateReadUpdateDeleteTests.cs:94-136`; `Service:140-164` | TEST-CONFIRMED [run: PASS] |
| Renaming keeps the assignments and values of a file and a folder | `T/MetadataTemplateChangesTests.cs:76-100` | TEST-CONFIRMED [run: PASS] |
| Hiding or showing notifies the clients of every entry the template is assigned to | `Service:150, 170-178`; `Notifier` | CODE-ONLY |
| **Deleting a metadata template** | | |
| DocSpace admin only, template kept on refusal (Room admin, User, Guest); admin deletion → 404 afterwards | `T/MetadataRightsTests.cs:72-91`; `T/MetadataTemplatePermissionsTests.cs:73-118` | TEST-CONFIRMED [run: PASS] |
| Gone from `getTemplates`, from the entries, `getTemplate` 404, `searchFolder` with the id 400 | `T/MetadataRequestLimitsTests.cs:225-255`; `T/MetadataTemplateReadUpdateDeleteTests.cs:138-150`; `Service:183-199`; `Filter:238-242` | TEST-CONFIRMED [run: PASS] |
| Other templates and their values untouched on a file and a folder | `T/MetadataFileAssignmentTests.cs:147-164`; `T/MetadataTemplateChangesTests.cs:102-127` | TEST-CONFIRMED [run: PASS] |
| Deleted twice or unknown → 404 | `T/MetadataTemplateReadUpdateDeleteTests.cs:152-172` | TEST-CONFIRMED [run: PASS] |
| **Field types and their options** | | |
| Five types; a number outside the set → 400 | `T/MetadataRequestLimitsTests.cs:151-161`; `Types:38-52`; `Service:1010-1049` | TEST-CONFIRMED [run: PASS] |
| Choice needs ≥1 option, non-blank, unique by lower case; options only on choice fields; empty, whitespace or over-long name; unknown type — each 400 on `createField`, nothing added; ids generated | `T/MetadataFieldManagementTests.cs:85-113`; `T/MetadataTemplateCreateTests.cs:160-167`; `T/RoomsMetadataSearchTestsBase.cs:62-66`; `Service:1010-1049` | TEST-CONFIRMED [run: PASS] |
| **Adding a field to a template** | | |
| Field added, template lists both; one of each type with `templateId` and `type`, listed after the seed field in request order | `T/MetadataRequestLimitsTests.cs:211-223`; `T/MetadataFieldManagementTests.cs:43-72` | TEST-CONFIRMED [run: PASS] |
| `CLIENT` next to `Client` → 400 | `T/MetadataRequestLimitsTests.cs:199-209`; `Service:1113-1119` | TEST-CONFIRMED [run: PASS] |
| Unknown template → 404 | `T/MetadataFieldManagementTests.cs:115-122` | TEST-CONFIRMED [run: PASS] |
| Admin only (403), Guest 403, anonymous 401 | `T/MetadataRightsTests.cs:93-123`; `T/MetadataTemplatePermissionsTests.cs:73-118, 134-159` | TEST-CONFIRMED [run: PASS] |
| **Display order of the fields** | | |
| Unpositioned fields follow the positioned ones in request order | `T/MetadataFieldOrderTests.cs:45-63`; `Service:1126-1138` | TEST-CONFIRMED [run: PASS] |
| New field without `order` gets highest + 1 (6 after 5 and 2); a negative `order` goes first | `T/MetadataFieldOrderTests.cs:65-78`; `T/MetadataFieldManagementTests.cs:74-83` | TEST-CONFIRMED [run: PASS] |
| Update with `order` moves (also to the front, options kept), without keeps | `T/MetadataFieldOrderTests.cs:80-101`; `T/MetadataFieldManagementTests.cs:186-202`; `Service:292` | TEST-CONFIRMED [run: PASS] |
| Entry metadata lists every field in template order, value or not | `T/MetadataFieldOrderTests.cs:103-124`; `Dto:420-434` | TEST-CONFIRMED [run: PASS] |
| **Updating a field** | | |
| Name-only update keeps id, type (number), order, and type+options (choice), persisted | `T/MetadataTemplateManagementTests.cs:85-120`; `T/MetadataFieldManagementTests.cs:124-140` | TEST-CONFIRMED [run: PASS] |
| Explicit type change on an empty field; refused with 400 once a file holds a value, type kept | `T/MetadataTemplateManagementTests.cs:174-185`; `T/MetadataFieldManagementTests.cs:204-219`; `Service:262-270` | TEST-CONFIRMED [run: PASS] |
| Options updated in place: known ids kept and renamed, a new option gets an id | `T/MetadataFieldManagementTests.cs:165-184`; `Service:272-290, 1005-1008` | TEST-CONFIRMED [run: PASS] |
| Unused option removable while another is selected; selected option → 400 | `T/MetadataTemplateManagementTests.cs:217-246`; `Service:277-290` | TEST-CONFIRMED [run: PASS] |
| Sibling name in another case → 400, both names kept | `T/MetadataFieldManagementTests.cs:142-152`; `Service:249-259` | TEST-CONFIRMED [run: PASS] |
| Field through another template's route → 404; unknown field → 404 | `T/MetadataTemplateManagementTests.cs:187-201`; `T/MetadataFieldManagementTests.cs:154-163`; `Service:986-996` | TEST-CONFIRMED [run: PASS] |
| Admin only (403), Guest 403, anonymous 401 | `T/MetadataRightsTests.cs:93-123`; `T/MetadataTemplatePermissionsTests.cs:73-118, 134-159` | TEST-CONFIRMED [run: PASS] |
| **Deleting a field** | | |
| Field gone from the template and the entry (room, folder, file), other value kept, condition on it → 400 | `T/MetadataLifecycleTests.cs:48-83`; `T/MetadataFileAssignmentTests.cs:166-183`; `T/MetadataTemplateChangesTests.cs:102-127`; `T/MetadataFieldManagementTests.cs:221-230`; `Service:303-318, 972-981` | TEST-CONFIRMED [run: PASS] |
| The last field → 400 and kept; the template goes as a whole | `T/MetadataTemplateManagementTests.cs:62-82`; `Service:315, 1070-1076` | TEST-CONFIRMED [run: PASS] |
| Through another template → 404 (both templates unchanged); deleted twice → 404 | `T/MetadataFieldManagementTests.cs:232-250`; `Service:311-312` | TEST-CONFIRMED [run: PASS] |
| Admin only (403), Guest 403, anonymous 401 | `T/MetadataRightsTests.cs:93-123`; `T/MetadataTemplatePermissionsTests.cs:73-118, 134-159` | TEST-CONFIRMED [run: PASS] |
| **Last-modified stamp of a template** | | |
| Moves on field add/rename/delete with `modifiedBy` the caller; not on a value write; one-second precision | `T/MetadataTemplateManagementTests.cs:248-289, 397-415` (`NextSecondAsync`) | TEST-CONFIRMED [run: PASS] |
| **Reading the metadata of an entry** | | |
| Read right; 403 for a non-member on the file, the folder and the progress; unknown folder 404; empty lists without metadata; fields with `value` null when empty | `T/MetadataEntryPermissionsTests.cs:108-130`; `T/MetadataFolderValuesTests.cs:144-158`; `T/MetadataFileAssignmentTests.cs:43-61`; `Service:449-454, 856-883`; `Dto:420-443` | TEST-CONFIRMED [run: PASS] |
| Viewer reads the file card with the value; a Read member reads file and folder | `T/MetadataRightsTests.cs:125-169`; `T/MetadataEntryPermissionsTests.cs:66-81` | TEST-CONFIRMED [run: PASS] |
| `cascade` and `conflictResolveType` on room / plain sub-folder / file | `T/MetadataCascadeAssignmentTests.cs:48-79`; `Dto:420-434` | TEST-CONFIRMED [run: PASS] |
| Custom field named like a template field kept apart; system template not in `templates` | `T/MetadataCustomFieldsTests.cs:182-201`; `Service:459-505` | TEST-CONFIRMED [run: PASS] |
| `hasMetadataFields` carried by the read (rule below) | `T/MetadataHasFieldsFlagTests.cs:218-221` (every scenario reads it); `Service:500-502`; `Dto:299-308` | TEST-CONFIRMED [run: PASS] |
| **The right to write the metadata of an entry** | | |
| File: edit right; folder (not room): create right; roots and knowledge folder refused; room: edit right | `Security:522-544`; `Service:856-883` | CODE-ONLY |
| Viewer: 403 on file values, room values, assign, unassign; nothing changed | `T/MetadataRightsTests.cs:125-169` | TEST-CONFIRMED [run: PASS] |
| Read member: every write on the file and the folder (assign, unassign, values, custom fields, cascade switch) 403, template kept, no value, no custom field | `T/MetadataEntryPermissionsTests.cs:66-106` | TEST-CONFIRMED [run: PASS] |
| Room manager writes values and custom fields on the file and the folder | `T/MetadataEntryPermissionsTests.cs:44-64` | TEST-CONFIRMED [run: PASS] |
| Editor: file assign+values 200; room values, folder values, folder custom fields, folder cascade switch 403 | `T/MetadataRightsTests.cs:171-214`; `T/MetadataCustomFieldsTests.cs:251-281` | TEST-CONFIRMED [run: PASS] |
| Content creator: folder assign, values, cascade on 200 (`id` returned); room values 403 | `T/MetadataRightsTests.cs:216-245` | TEST-CONFIRMED [run: PASS] |
| Owner on My documents root and Room admin on the rooms root: assign with cascade 403, custom fields 403 | `T/MetadataRightsTests.cs:247-298` | TEST-CONFIRMED [run: PASS] |
| Stranger: file values 403, nothing written | `T/MetadataEntryPermissionsTests.cs:125-129` | TEST-CONFIRMED [run: PASS] |
| **Assigning templates to a file** | | |
| Editor assigns, template listed with every field empty, `cascade` false | `T/MetadataRightsTests.cs:171-214`; `T/MetadataFileAssignmentTests.cs:43-61`; `T/MetadataCascadeTests.cs:143-185` | TEST-CONFIRMED [run: PASS] |
| Additive and idempotent; an empty list answers 200 and changes nothing | `T/MetadataFileAssignmentTests.cs:63-76`; `Service:507-535` (`Distinct`, upsert under a per-entry lock) | TEST-CONFIRMED [run: PASS] |
| Unknown template in the list → 404, nothing of the list linked; unknown file → 404; system template 404 | `T/MetadataFileAssignmentTests.cs:78-98`; `Service:515-525, 889-899` | TEST-CONFIRMED [run: PASS] (unknown), CODE-ONLY (system) |
| Re-assigned after an unassignment: field empty | `T/MetadataFileAssignmentTests.cs:123-126` | TEST-CONFIRMED [run: PASS] |
| Body without `templateIds` → 400 | `Controller:307`; yaml `assignFileTemplates` (20521) | CODE-ONLY |
| **Assigning templates to a folder or a room** | | |
| Without cascade: `{id:null, progress:100, isCompleted:true, error:null}`; progress endpoint reports the same; folder lists the template with `cascade` false and every field; file inside untouched | `T/MetadataCascadeTests.cs:440-453`; `T/MetadataFolderAssignmentTests.cs:43-60`; `Controller:338-344`; `Dto:445-461` | TEST-CONFIRMED [run: PASS] |
| Additive and idempotent; unknown template in the list → 404, nothing linked; unknown folder → 404 | `T/MetadataFolderAssignmentTests.cs:62-90`; `Service:543-555` | TEST-CONFIRMED [run: PASS] |
| Empty list with cascade: completed operation, nothing assigned | `T/MetadataTemplateManagementTests.cs:338-350`; `Service:545-550` | TEST-CONFIRMED [run: PASS] |
| Cascade queued: answer carries the template in `templateIds` and an `id` | `T/MetadataCascadeAssignmentTests.cs:81-104`; `T/MetadataFolderAssignmentTests.cs:112-118` | TEST-CONFIRMED [run: PASS] |
| **Unassigning a template from an entry** | | |
| Room unassigned: template gone from the room, file keeps template + inherited value; per-file unassign removes template with value | `T/MetadataCascadeTests.cs:265-300`; `T/MetadataFolderAssignmentTests.cs:255-272`; `Service:588-605, 687-713` | TEST-CONFIRMED [run: PASS] |
| Only this folder's inherited links converted: file under a nested cascading folder stays inherited, its copy carries nothing | `T/MetadataCascadeTests.cs:302-343` | TEST-CONFIRMED [run: PASS] |
| Template the file does not carry → 200 no-op; unknown template → 404; unknown folder or template on the folder route → 404 | `T/MetadataFileAssignmentTests.cs:129-145`; `T/MetadataFolderAssignmentTests.cs:274-285`; `Service:702, 706` | TEST-CONFIRMED [run: PASS] |
| The other template keeps its value | `T/MetadataFileAssignmentTests.cs:100-121` | TEST-CONFIRMED [run: PASS] |
| An inherited template unassigned from a file is assigned again by the next cascade | `T/MetadataTemplateChangesTests.cs:153-172`; `Controller:367-372` | TEST-CONFIRMED [run: PASS] |
| Link and values deleted in one transaction | `Service:704-706` (`DeleteLinkWithValuesAsync`) | CODE-ONLY |
| **Setting the values of template fields** | | |
| Answer is the whole entry metadata, earlier values and custom fields included | `T/MetadataTemplateManagementTests.cs:291-314`; `Service:769-771` | TEST-CONFIRMED [run: PASS] |
| Every type in one request on a file and a folder, answer and read agree, Cyrillic and CJK intact | `T/MetadataFileValuesTests.cs:43-79`; `T/MetadataFolderValuesTests.cs:42-77` | TEST-CONFIRMED [run: PASS] |
| Same field twice → 400 | `T/MetadataTemplateManagementTests.cs:316-336`; `T/MetadataFileValuesTests.cs:107-135`; `Service:723-728` | TEST-CONFIRMED [run: PASS] |
| `{"values": null}` → 400 (raw HTTP) | `T/MetadataRequestLimitsTests.cs:122-133`; `Controller:573` | TEST-CONFIRMED [run: PASS] |
| Refused batch writes none of its values: a string on a number field, a field of a template the entry does not carry, a number beyond the limit, a field twice (400), an unknown field (404) | `T/MetadataFileValuesTests.cs:107-135`; `T/MetadataFolderValuesTests.cs:79-98`; `T/MetadataValueBatchTests.cs:47-63`; `Service:738-761` | TEST-CONFIRMED [run: PASS] |
| Custom field through values → 400 | `Service:745-750`; yaml `setFileValues` (21116) | CODE-ONLY |
| Partial write keeps the other values; empty string clears; room no longer found by the cleared value | `T/MetadataFileValuesTests.cs:81-105`; `T/RoomsMetadataSearchTests.cs:188-203` | TEST-CONFIRMED [run: PASS] |
| Value member left out clears a field of every type, sibling kept (raw HTTP) | `T/MetadataValueBatchTests.cs:65-78`; `Service:793-796` (`IsEmpty`) | TEST-CONFIRMED [run: PASS] |
| **Validation of a value by its field type** | | |
| Exactly one member per type; single choice one distinct option; unknown option refused; empty value passes | `T/MetadataValidationTests.cs:42-245` (unit, incl. `IsEmpty`); `Service:791-854` | TEST-CONFIRMED [run: PASS] |
| 8001 chars → 400 (template field and custom field); 8000 stored and found by `eq`; `"   "` stored; empty and null clear | `T/MetadataRequestLimitsTests.cs:286-343`; `T/MetadataStringValueTests.cs:43-91`; `Service:69, 806-809` | TEST-CONFIRMED [run: PASS] |
| ±(2^53−1) accepted, 0 and -7 accepted, one beyond refused, value kept | `T/MetadataValidationTests.cs:124-148` (unit); `T/MetadataNumberValueTests.cs:43-87`; `Service:76, 825-828` | TEST-CONFIRMED [run: PASS] |
| `42.5` and `1e30` → 400, value kept (raw HTTP) | `T/MetadataValueBatchTests.cs:80-110` | TEST-CONFIRMED [run: PASS] |
| Member of another type → 400 on every type, value and sibling kept, file and folder alike | `T/MetadataStringValueTests.cs:73-91`; `T/MetadataNumberValueTests.cs:68-87`; `T/MetadataDateRefusedValueTests.cs:62-77`; `T/MetadataSingleChoiceValueTests.cs:76-95`; `T/MetadataMultiChoiceValueTests.cs:83-98` | TEST-CONFIRMED [run: PASS] |
| Single choice: other option replaces, two options 400, option of another field 400, malformed id 400 (raw), empty or null list clears | `T/MetadataSingleChoiceValueTests.cs:43-110` | TEST-CONFIRMED [run: PASS] |
| Multiple choice: all, one, empty or null clears; option of another field 400; malformed id 400 (raw) | `T/MetadataMultiChoiceValueTests.cs:43-113` | TEST-CONFIRMED [run: PASS] |
| **Date values and their time zone** | | |
| Offset-less `2026-01-15T00:00:00` stored as 00:00 UTC and found by the day filter; local → UTC | `T/MetadataDateValuesTests.cs:46-75`; `T/MetadataValidationTests.cs:64-98` (unit); `Service:774-789` | TEST-CONFIRMED [run: PASS] |
| Date only, time without offset and `+03:00` stored as the same UTC instant (raw HTTP) | `T/MetadataDateAcceptedValueTests.cs:75-87` | TEST-CONFIRMED [run: PASS] |
| Year 1, year 9999 and the epoch accepted; null clears | `T/MetadataDateAcceptedValueTests.cs:54-73` | TEST-CONFIRMED [run: PASS] |
| Year 0, year 10000, month 13, text → 400, value kept (raw HTTP) | `T/MetadataDateRefusedValueTests.cs:47-60` | TEST-CONFIRMED [run: PASS] |
| **Custom fields of an entry** | | |
| Several at once, partial update leaves others, read agrees | `T/MetadataCustomFieldsTests.cs:45-89`; `T/MetadataFileValuesTests.cs:137-154` | TEST-CONFIRMED [run: PASS] |
| Case-insensitive name reuses the field, the created spelling is shown | `T/MetadataCustomFieldsTests.cs:61-74`; `T/MetadataFileValuesTests.cs:146-149`; `Service:363-367` | TEST-CONFIRMED [run: PASS] |
| Null or empty value removes from this entry only; removing an unknown name is a no-op (200) | `T/MetadataCustomFieldsTests.cs:91-139`; `T/MetadataFolderValuesTests.cs:100-116` (`null` through the SDK) | TEST-CONFIRMED [run: PASS] |
| Rename on one entry = remove + add, the portal field keeps its name | `T/MetadataCustomFieldsTests.cs:109-127` | TEST-CONFIRMED [run: PASS] |
| Never cascaded to the content; template values untouched by a custom field write | `T/MetadataFolderValuesTests.cs:100-116`; `T/MetadataFileValuesTests.cs:156-172` | TEST-CONFIRMED [run: PASS] |
| Empty list 400; blank name 400; same name twice (any case) 400; name over 255 400; value 8001 400 — nothing changed on a file or a folder; null list 400 (raw) | `T/MetadataFileValuesTests.cs:174-200`; `T/MetadataFolderValuesTests.cs:118-142`; `T/MetadataCustomFieldsTests.cs:141-164`; `T/MetadataRequestLimitsTests.cs:135-149, 307-319`; `Service:915-954` | TEST-CONFIRMED [run: PASS] |
| Unknown file → 404 | `T/MetadataFileValuesTests.cs:218-225` | TEST-CONFIRMED [run: PASS] |
| **Limits and dictionary of the custom fields** | | |
| 51 fields → 400, nothing written; 50 accepted and a request pushing over refused | `T/MetadataCustomFieldsTests.cs:166-180`; `T/MetadataFileValuesTests.cs:202-216`; `Service:57, 395-398` | TEST-CONFIRMED [run: PASS] |
| Order across removals: `A,C,D` → `A,C,D,B` → `D,B,E` | `T/MetadataFieldOrderTests.cs:126-149`; `Service:419-421, 960-970` | TEST-CONFIRMED [run: PASS] |
| Dropped name: filter by it 400 after the last value is removed, 200 before | `T/MetadataCustomFieldsTests.cs:304-331`; `Service:901-913`; `Filter:292-321` | TEST-CONFIRMED [run: PASS] |
| Writes serialised under one lock per portal | `Service:339` | CODE-ONLY |
| **Cascade of a template over the content of a folder** | | |
| Every depth: sub-folder, folder two levels down and the files in both take the value; the folder reports the mode | `T/MetadataFolderAssignmentTests.cs:92-131` | TEST-CONFIRMED [run: PASS] |
| Skip fills the empty ones and keeps the held values; overwrite replaces all, on files and sub-folders | `T/MetadataFolderAssignmentTests.cs:133-176`; `Op:197-259` | TEST-CONFIRMED [run: PASS] |
| Nearest cascading folder wins, also in overwrite mode | `T/MetadataCascadeTests.cs:77-113`; `Op:197-259` | TEST-CONFIRMED [run: PASS] |
| Only filled fields travel; empty folder field never overwrites; empty template linked | `T/MetadataCascadeTests.cs:143-185`; `Op:197-215` (`!v.IsEmpty`) | TEST-CONFIRMED [run: PASS] |
| Snapshot: edit after the pass changes the folder only; new file inherits the edit; skip keeps, overwrite pushes | `T/MetadataCascadeTests.cs:187-230` | TEST-CONFIRMED [run: PASS] |
| Back-to-back {A} then {A,B}: both reach; re-request after an edit pushes the edit (not folded into a started pass) | `T/MetadataCascadeTests.cs:47-75, 232-263`; `Worker:43-80`; `Op:104-184` (per-tree run lock) | TEST-CONFIRMED [run: PASS] |
| New sub-folder in a cascading room found by the filter right away | `T/MetadataCascadeTests.cs:115-141`; `FolderDao:668-682` | TEST-CONFIRMED [run: PASS] |
| The room cascades like a folder, to its files | `T/MetadataTemplateChangesTests.cs:129-151` | TEST-CONFIRMED [run: PASS] |
| **Progress of a cascade** | | |
| Idle folder: completed, no `id`, empty `templateIds`; pass named by its templates | `T/MetadataCascadeAssignmentTests.cs:81-104`; `T/MetadataFolderAssignmentTests.cs:178-188`; `Worker:91-99`; `Dto:445-461` | TEST-CONFIRMED [run: PASS] |
| Finished pass stays reported with the same `id`, `templateIds`, progress 100, no error | `T/MetadataFolderAssignmentTests.cs:190-218` | TEST-CONFIRMED [run: PASS] |
| Read right: non-member 403; missing folder 404 | `T/MetadataEntryPermissionsTests.cs:116-123`; `T/MetadataFolderAssignmentTests.cs:220-227`; `Service:676-681` | TEST-CONFIRMED [run: PASS] |
| Failed pass completes with `error` | `Op:163-172`; `Dto:458` | CODE-ONLY |
| **Switching the cascade of an assigned template** | | |
| Off keeps template and values on the room, file keeps inherited as own, later file inherits nothing, off again is a no-op | `T/MetadataCascadeAssignmentTests.cs:106-146`; `Service:627-640` | TEST-CONFIRMED [run: PASS] |
| On queues a pass (`id`, `templateIds`), file receives the value, mode stored on the link | `T/MetadataCascadeAssignmentTests.cs:148-178`; `T/MetadataFolderAssignmentTests.cs:173-175`; `Service:642-665` | TEST-CONFIRMED [run: PASS] |
| Template not on the folder, or unknown → 404 | `T/MetadataCascadeAssignmentTests.cs:180-195`; `T/MetadataFolderAssignmentTests.cs:242-253`; `Service:621-625` | TEST-CONFIRMED [run: PASS] |
| Switched on from an inherited sub-folder: its link becomes cascading, the parent's stays | `T/MetadataTemplateChangesTests.cs:174-189`; `Service:644-655` | TEST-CONFIRMED [run: PASS] |
| **Inheritance when an entry is created or moved** | | |
| Created under a cascade: stamped inside the save transaction (file and folder) | `T/MetadataInheritanceOnMoveTests.cs:60-79`; `T/MetadataFolderAssignmentTests.cs:229-240`; `FileDao:738-742`; `FolderDao:837-841` | TEST-CONFIRMED [run: PASS] |
| Moved in (batch path inside a room and per-file path from My documents): skip fills empty, overwrite replaces | `T/MetadataInheritanceOnMoveTests.cs:81-113`; `FileDao:1593-1623, 1695-1715` | TEST-CONFIRMED [run: PASS] |
| Moved folder: itself stamped inline, content by a background stamp pass | `T/MetadataInheritanceOnMoveTests.cs:115-131`; `FolderDao:1305-1362`; `Op:261-333` | TEST-CONFIRMED [run: PASS] |
| Moved cascading folder keeps its own cascade for its subtree, `Department` stays empty | `T/MetadataInheritanceOnMoveTests.cs:133-161`; `Op:286-295` | TEST-CONFIRMED [run: PASS] |
| Moved out: inherited becomes own, a copy carries it (file and content of a moved folder) | `T/MetadataInheritanceOnMoveTests.cs:163-204`; `FolderDao:1317-1323` | TEST-CONFIRMED [run: PASS] |
| Move to Trash stamps nothing | `FileDao:1593, 1695`; `FolderDao:1301, 1317` (`toFolderId != trashId`) | CODE-ONLY |
| **Metadata of a copy** | | |
| Copy into a cascading room keeps copied and inherited values | `T/MetadataCascadeTests.cs:345-393`; `FileDao:1842-1845` | TEST-CONFIRMED [run: PASS] |
| Folder copy carries its own and its content's metadata; source untouched | `T/MetadataLifecycleTests.cs:183-225`; `FolderDao:1434-1437` | TEST-CONFIRMED [run: PASS] |
| Only direct assignments copied | `T/MetadataCascadeTests.cs:302-343`; `T/MetadataInheritanceOnMoveTests.cs:163-182` | TEST-CONFIRMED [run: PASS] |
| **Metadata through deletion, Trash and restore** | | |
| Deleted for good: 404 and not found | `T/MetadataLifecycleTests.cs:85-108`; `FileDao:1192-1193` | TEST-CONFIRMED [run: PASS] |
| Trash keeps, emptied Trash drops (404, not found in My documents) | `T/MetadataLifecycleTests.cs:110-147`; `FolderDao:1129-1130` | TEST-CONFIRMED [run: PASS] |
| Restored file keeps and is found; file in Trash found by the filter in Trash | `T/MetadataLifecycleTests.cs:149-181`; `T/MetadataMoveSearchTests.cs:67-93` | TEST-CONFIRMED [run: PASS] |
| Deleted room: room and file 404, room not in the rooms filter | `T/MetadataLifecycleTests.cs:227-262` | TEST-CONFIRMED [run: PASS] |
| **Whether an entry has fields to fill** | | |
| False, not absent, on a portal without templates: listing, info endpoints, metadata reads of a file and a folder (raw JSON: the SDK 4.0.0 model has no property for the flag) | `T/MetadataHasFieldsFlagTests.cs:45-61`; `EntryDto:447-466`; `Service:500-502` | TEST-CONFIRMED [run: PASS] |
| Visible template with a field → true on the assigned file and folder, false on the untouched one, everywhere | `T/MetadataHasFieldsFlagTests.cs:63-83`; `EntryDto:472-477`; `Queries:649-671` | TEST-CONFIRMED [run: PASS] |
| Hidden template only → false while `assignedMetadataTemplates` names it | `T/MetadataHasFieldsFlagTests.cs:85-103` | TEST-CONFIRMED [run: PASS] |
| Custom field only → true, no `assignedMetadataTemplates` on the file | `T/MetadataHasFieldsFlagTests.cs:105-127` | TEST-CONFIRMED [run: PASS] |
| Follows hiding and showing the template | `T/MetadataHasFieldsFlagTests.cs:129-149`; `Service:170-178` | TEST-CONFIRMED [run: PASS] |
| Stays true once filled; a Read member sees true next to `security.edit` false | `T/MetadataHasFieldsFlagTests.cs:151-193` | TEST-CONFIRMED [run: PASS] |
| Absent in the answers of a creation, an upload or a rename, and on third-party entries | `EntryDto:360-372` | CODE-ONLY |
| **Text of names, options and values** | | |
| BMP text (Cyrillic, CJK, symbols) survives in the template name, a field name, an option, a string value and a custom field | `T/MetadataUnicodeTests.cs:44-71`; `T/MetadataTemplateCreateTests.cs:82-91` | TEST-CONFIRMED [run: PASS] (`MetadataUnicodeTests` outside the BMP are MD10, next row) |
| Outside the BMP: the write fails with a 500 (scenario) | failure text of `CreateTemplate_WithACharacterOutsideTheBmpInTheName_StoresItIntact`, `CreateField_WithAnEmojiInTheFieldNameAndTheOptionValue_StoresThemIntact`, `SetFileValues_WithAnEmojiInAStringValueAndACustomField_StoresThemIntact` in run 365d87fe60 (MD10) | CODE-ONLY |
| Characters outside the BMP are stored as `?` — not written into the spec, see MD10 | `T/MetadataUnicodeTests.cs:73-120` (Category=Bug, red); `Migration:16-136` | — |
| **Assigned templates in the listings** | | |
| `getFileInfo`, `getFolderInfo` and the listing agree; empty before assignment | `T/MetadataRequestLimitsTests.cs:257-284`; `FoldersController:534-535`; yaml `assignedMetadataTemplates` (79738, 89147) | TEST-CONFIRMED [run: PASS] |
| First template of the portal drops the "no templates" memory | `T/FoldersMetadataSearchTests.cs:418-437` | TEST-CONFIRMED [run: PASS] |
| `entryKind` names the concrete kind of every listing entry (`file`, `folder`, `thirdPartyFile`, `thirdPartyFolder`); the SDK builds the concrete model from it | `EntryDto:49-59`; yaml `FileEntryBaseDto` (79393-79403); `Listings:66-70, 269-286` (the tests read `id`, `assignedMetadataTemplates`, `metadataMatches` through it) | TEST-CONFIRMED [run: NO-LINK] |
| **Filtering a listing by metadata** | | |
| Template alone keeps the carriers, `total` 6 | `T/FoldersMetadataSearchTests.cs:133-148`; `T/RoomsMetadataSearchTests.cs:283-292`; `Query:74-105, 307-317` | TEST-CONFIRMED [run: PASS] |
| Folder and file together, `total`/`count` 2; no match → empty and `total` 0 | `T/FoldersMetadataSearchTests.cs:87-131` | TEST-CONFIRMED [run: PASS] |
| Subtree by default, not with `withSubFolders` false | `T/FoldersMetadataSearchTests.cs:150-192` | TEST-CONFIRMED [run: PASS] |
| Invalid JSON, unknown field, field of another template, unknown template → 400 | `T/RoomsMetadataSearchTests.cs:205-306`; `T/MetadataTypedSearchTests.cs:74-85`; `Filter:233-290, 323-338` | TEST-CONFIRMED [run: PASS] |
| Combined with room `type`, `searchArea` (archive found, active not), `filterValue`; invited user sees only their rooms | `T/RoomsMetadataSearchTests.cs:46-67, 134-175, 309-330` | TEST-CONFIRMED [run: PASS] |
| Per-type conditions on rooms: string case-insensitive exact, number range inclusive/open, date range, choice OR, several conditions AND | `T/RoomsMetadataFieldFilterTests.cs:45-205`; `T/MetadataSearchQueryTests.cs:72-262` (unit predicate) | TEST-CONFIRMED [run: PASS] |
| Third-party entries left out; index candidates confirmed against the database | `Query:74-127`; yaml `getFolderByFolderId` (16321) | CODE-ONLY |
| **Access before the filter is validated** | | |
| Anonymous with unknown `metadataTemplateId` → 401 | `T/FoldersMetadataSearchTests.cs:399-416`; `FoldersController:456-465` | TEST-CONFIRMED [run: PASS] |
| **Conditions and operators of a filter** | | |
| `contains` substring case-insensitive, `eq` whole value; typed and listing agree | `T/MetadataFilterOperatorsTests.cs:50-82` | TEST-CONFIRMED [run: PASS] |
| `gt`/`gte`/`lt`/`lte` on numbers; typed agrees | `T/MetadataFilterOperatorsTests.cs:98-148` | TEST-CONFIRMED [run: PASS] |
| Choice `in` combines with OR, multi-choice room returned once | `T/RoomsMetadataFieldFilterTests.cs:145-177` | TEST-CONFIRMED [run: PASS] |
| Refused forms: `contains` on number, `gt` on string, `gt` with `from`, `gt` without value, `eq` with bound, `range` with value (number and date), bound 2^53, `yesterday`, `startswith`, `range` on string, >20 conditions | `T/MetadataFilterOperatorsTests.cs:83-96, 150-221, 276-299`; `T/RoomsMetadataSearchTests.cs:233-262`; `T/FoldersMetadataSearchTests.cs:194-209`; `T/MetadataRequestLimitsTests.cs:98-120`; `Filter:340-600` | TEST-CONFIRMED [run: PASS] |
| Custom field by `name` (listing and `searchRooms`); neither id nor name → 400 | `T/MetadataCustomFieldsTests.cs:283-302`; `T/MetadataTypedSearchTests.cs:169-193`; `Filter:292-321` | TEST-CONFIRMED [run: PASS] (name), CODE-ONLY (neither) |
| JSON numbers accepted in body and query | `T/MetadataTypedSearchTests.cs:87-124`; `Filter:157-180` | TEST-CONFIRMED [run: PASS] |
| Defaults of an omitted `op` | `Filter:113-121, 340-436`; yaml `MetadataFilterConditionRequest.op` (84868-84900) | CODE-ONLY |
| **Date bounds of a condition** | | |
| Day-only `to` covers the day; a `to` with time is an instant | `T/MetadataDateValuesTests.cs:77-103` | TEST-CONFIRMED [run: PASS] |
| `gt`/`gte`/`lt`/`lte` with day-only and timed bounds | `T/MetadataFilterOperatorsTests.cs:223-274`; `Filter:526-578` | TEST-CONFIRMED [run: PASS] |
| **Sections that take or refuse the metadata filter** | | |
| Recent, Favorites, Shared with me by id: template and condition, `total` | `T/MetadataSectionFiltersTests.cs:50-205`; `EntryManager:402-508` | TEST-CONFIRMED [run: PASS] |
| `@my`, `@trash`, `@share`, `@recent`, `@favorites`, `@forms`; unknown template 400 on alias | `T/MetadataSectionAliasFiltersTests.cs:48-186`; `T/MetadataSectionFiltersTests.cs:91-116`; `FoldersController:886-1117` | TEST-CONFIRMED [run: PASS] |
| Templates section 400 by id, `@templates` alias (raw) and `searchFolder` | `T/MetadataSectionFiltersTests.cs:207-221`; `T/MetadataSectionAliasFiltersTests.cs:188-200`; `T/MetadataTypedSearchTests.cs:126-136`; `EntryManager:402-409` | TEST-CONFIRMED [run: PASS] |
| Private room section refused too | `EntryManager:405-408` (`FolderType.Privacy`) | CODE-ONLY |
| **Typed metadata search** | | |
| Agrees with the listing (ids and `total`) | `T/MetadataTypedSearchTests.cs:46-72`; `T/MetadataFilterOperatorsTests.cs:70-82, 138-148` | TEST-CONFIRMED [run: PASS] |
| `count` 101/0/-1/absent, `startIndex` -1 → 400 on `searchFolder`; 101/-1 on `searchRooms` | `T/MetadataRequestLimitsTests.cs:50-96`; `Request` (`[Range(1, 100)]`, `[Range(0, int.MaxValue)]`) | TEST-CONFIRMED [run: PASS] |
| Recent through the body; Templates 400 | `T/MetadataTypedSearchTests.cs:126-167` | TEST-CONFIRMED [run: PASS] |
| Body fields and routes | `FoldersController:487-503`; `RoomsController:1394-1423`; yaml `searchFolder` (16771), `searchRooms` (33278) | CODE-ONLY |
| **String metadata values in the free text search** | | |
| Found by a custom field, match with null ids (sub-folder and room) | `T/FoldersMetadataSearchTests.cs:254-275`; `T/RoomsMetadataSearchTests.cs:69-89` | TEST-CONFIRMED [run: PASS] |
| Found by a template field, match with template/field ids and the whole value | `T/FoldersMetadataSearchTests.cs:277-297`; `T/RoomsMetadataSearchTests.cs:91-111` | TEST-CONFIRMED [run: PASS] |
| Two matches for two fields; no matches without text | `T/FoldersMetadataSearchTests.cs:299-317, 384-397` | TEST-CONFIRMED [run: PASS] |
| Extension filter still applies to metadata matches | `T/FoldersMetadataSearchTests.cs:233-252`; `FileDao:3124-3139` | TEST-CONFIRMED [run: PASS] |
| First custom field of the portal drops the "no templates" memory of the text search | `T/FoldersMetadataSearchTests.cs:439-461` | TEST-CONFIRMED [run: PASS] |
| **Hidden templates and the text search** | | |
| Hidden value found only with `includeHiddenMetadata` (listing, typed, rooms) | `T/FoldersMetadataSearchTests.cs:319-344`; `T/RoomsMetadataSearchTests.cs:113-132`; `Query:319-339` | TEST-CONFIRMED [run: PASS] |
| Found by title either way, hidden match reported only on request | `T/FoldersMetadataSearchTests.cs:346-365` | TEST-CONFIRMED [run: PASS] |
| Condition on a hidden field applies without the flag | `T/FoldersMetadataSearchTests.cs:367-382` | TEST-CONFIRMED [run: PASS] |
| **The stored values are the truth of a filter** | | |
| Unassigned folder absent on the very next single request, file with own value kept | `T/FoldersMetadataSearchTests.cs:211-231`; `Query:74-127` (candidates confirmed in SQL) | TEST-CONFIRMED [run: PASS] |
| Moved file found in its new folder only (index not scoped by the tree) | `T/MetadataMoveSearchTests.cs:47-65, 95-110`; `Query:44-46` | TEST-CONFIRMED [run: PASS] |
| **Metadata reached through an external link** | | |
| Read of file and folder through the room link | `T/MetadataExternalLinkTests.cs:48-62`; `Controller:253-254, 285-286` (`[AllowAnonymous]`) | TEST-CONFIRMED [run: PASS] |
| No session, no key → 401 on read and on file values | `T/MetadataExternalLinkTests.cs:64-78, 136-147`; `T/MetadataEntryPermissionsTests.cs:132-147`; `Service:861-864` | TEST-CONFIRMED [run: PASS] |
| Link of another room → 403 | `T/MetadataExternalLinkTests.cs:80-94` | TEST-CONFIRMED [run: PASS] |
| Editing link writes file values, read link 403 and nothing changed | `T/MetadataExternalLinkTests.cs:96-134` | TEST-CONFIRMED [run: PASS] |
| Folder values, unassign, custom fields → 401 for anonymous | `T/MetadataExternalLinkTests.cs:149-168`; `Controller` (no `[AllowAnonymous]` on 378-548) | TEST-CONFIRMED [run: PASS] |
| **Clients are told about metadata changes** | | |
| `update-file` / `update-folder` after every write; cascade, template and field deletions and a visibility change notify affected entries by parent folder recipients | `Service:170-178, 1167-1196`; `Notifier:35-103`; `Op:323, 348, 387`; `Socket:86-93` | CODE-ONLY |
| **Audit trail of the metadata actions** | | |
| Eight event texts with the object's name after one session | `T/MetadataAuditTests.cs:47-91`; `Audit:1291-1318`; `MessageAction:1158-1191` | TEST-CONFIRMED [run: PASS] |
| Custom field write audited as "Metadata values updated"; cascade start as "Metadata cascade assignment started" | `Service:440, 583, 662`; `Audit:1315-1318` | CODE-ONLY |
| **Metadata in backup and restore** | | |
| Four tables declared with user and date columns; template/field ids remapped, links and values follow template, field and entry ids, `source_folder_id` remapped | `Backup:80-101, 158-168` | CODE-ONLY |

Counts: 191 rows — TEST-CONFIRMED 173 (three of them carry a CODE-ONLY part for the system-template or the
"neither id nor name" case), CODE-ONLY 17, NOTES-ONLY 0, one row with no status (the non-BMP text, kept out of
the spec as MD10). Spec: 44 requirements, 163 scenarios. Before the 4.1.0 revision (`3533ad8a89`): 138 rows
(TEST-CONFIRMED 119, CODE-ONLY 19), 41 requirements, 109 scenarios; the rows upgraded from CODE-ONLY: the `visible` filter and creation
order of `getTemplates`, the 404 of an unknown template id, the name taken / too long 400 and 404 of
`updateTemplate`, the rules of the field options, the unknown template 404 of `createField`, the type change with
values, the empty-value clearing per type, the 404 of an unknown field / the 400 of a foreign template on the
values, the name over 255 of a custom field, the 403 and 404 of `getCascadeProgress`.

## Where the server differs from the OpenAPI document or the SDK (spec follows the server)

| Document / client | Server |
|---|---|
| yaml `getCascadeProgress` declares 200, 404 and the generic 401 only (20730-20774) | `Service:676-681` demands the read right on the folder, so a folder the caller cannot read answers 403 (`SecurityException`, `T/MetadataEntryPermissionsTests.cs:116-123`); the row is still missing after 4.1.0. |
| yaml routes `…/customfields` (lower case, 21288, 21384) | The controller publishes `customFields` (`Controller:521, 547`); routing is case-insensitive, both forms work. |
| yaml `assignFileTemplates` 400 "has no `templateIds`" (20521) | Only a body without the `templateIds` key (required) is refused; an empty list is accepted and answers 200 after writing nothing (`T/MetadataFileAssignmentTests.cs:63-76`; see MD4 for the audit it still writes). |
| SDK 4.0.0 (the one the tests are pinned to) predates `hasMetadataFields`: `FileDto`, `FolderDto` and `EntryMetadataDto` carry no property for it | The server answers the flag on the listing, the info and the metadata reads (`EntryDto:360-372`, `Dto:299-308`); the tests read it from the raw JSON (`T/MetadataHasFieldsFlagTests.cs:195-241`). |
| SDK `FolderMetadataSearch.Count` left out of the body when 0; `MetadataFilterConditionRequest.value/from/to` strings only; `SetMetadataValues`/`SetCustomFields` refuse a null list client-side; every value member is serialised, as null at the least; `numberValue` is a `long`, `dateValue` a `DateTime`, an option id a `Guid` | The server refuses a missing `count` with 400, accepts JSON numbers in the conditions, refuses a null list with 400, clears a field whose member is left out, refuses `42.5`, `1e30`, a date-only or offset-bearing string is read as UTC, a text that is no date or no identifier is refused — the tests use raw HTTP for these (`T/MetadataRequestLimitsTests.cs:50-96, 122-149`; `T/MetadataTypedSearchTests.cs:87-124`; `T/MetadataValueBatchTests.cs:65-110`; `T/MetadataDateAcceptedValueTests.cs:75-87`; `T/MetadataDateRefusedValueTests.cs:47-60`; `T/MetadataSingleChoiceValueTests.cs:97-110`). A null custom-field value goes through the SDK (`T/MetadataFolderValuesTests.cs:111`). |
| SDK has no method for `@share` and `@templates` | Both aliases exist on the server and take the filter / refuse it (`T/MetadataSectionAliasFiltersTests.cs:133-200`, raw HTTP). |
| Release notes v1.1 – v4.0 (Privacy rooms included); none for v4.1.0 | No mention of metadata, templates or custom fields: the feature is unreleased at the time of writing, so nothing is NOTES-ONLY. |
| Test comments "used to…" (default type String on update, position 0, filter dropped on aliases, worker folding requests, un-cascade 500, extension slip, stale documents, an empty template created and assigned) | All describe fixed behaviour; the tests assert the fixed state and the spec describes it. |

Rows closed by 4.1.0: the yaml text of `setFileValues` / `setFolderValues` 400 now carries "or exceeds the number
limit" (21150, 21249); `MetadataFilterConditionRequest.fieldId` now says a custom field is addressed by `name`
(84868-84876); `FolderContentDto.Folders`/`Files` are built as the concrete models through `entryKind`
(`Listings:66-70`), so the SDK no longer drops `id`, `assignedMetadataTemplates` and `metadataMatches`.

## Left out of the spec, and why

- **AI generation and filling of metadata** (`POST api/2.0/ai/meta/generate`, `/fill`): owned by `ai-agents`
  ("Metadata fields proposed for a file", "Metadata values filled from a file", "The model and the failures of the
  metadata routes"); cross-referenced in the Purpose only. The filling now relies on the `hasMetadataFields` rule
  of `Service:500-502` instead of a copy of its own.
- **Index mechanics**: the OpenSearch metadata documents, the `QueryLimit` overflow fallback, the per-entry
  re-indexing, the best-effort document writes (`Query:35-127`, `MetadataIndexHelper`, `MetadataSearchItem.cs`), and
  the worker's refresh policy changed in 4.1.0 (`09cfe2c5ed`, `32ad89fb81`); only their observable rule ("the stored
  values are the truth") is specified.
- **Cascade internals**: the per-tree run lock and its one-hour timeout, batch size 1000, the reconciliation of a
  pass that outlived a template/field change or an un-cascade (`Op:435-520`), the dedupe rule of the queue
  (`Worker:43-80`), the nearest-source resolver (`Resolver`, unit-tested in `T/MetadataCascadeResolverTests.cs`):
  implementation behind the "nearest cascading ancestor wins" requirement.
- **The portal-wide custom-field lock and the per-entry link lock** (`Service:78-85, 339`): concurrency plumbing.
- **The `MetadataTemplatesCache` "has templates" shortcut** (`FolderDao:300, 838`; `FileDao:739, 1593`): performance;
  the two tests on the first template / first custom field pin its invalidation and are listed under the listing and
  text-search requirements.
- **Third-party storage entries**: string ids never reach the metadata routes (`{fileId:int}`); the listings leave
  them out when a filter is set, and they carry no `hasMetadataFields`. Named in one requirement, not specified further.
- **`FormMetadata` of the OForm service and `Metadata.cs` of the encryption library**: unrelated namesakes.
- **Rate-limit headers, 429/5xx rows** of the yaml: generic policy.
- **Socket event payloads and the recipients rule of the notifier** beyond "the ordinary update event": client contract
  of `socket-events`.
- **Where `hasMetadataFields` is absent** (the answers of a creation, an upload, a rename; third-party entries): a
  statement of the DTO (`EntryDto:360-372`), no test reads those answers for it; mentioned in the requirement body only
  as "always present there" for the listed endpoints.

## Suspected defects

Re-checked against `3533ad8a89`. MD1–MD8 are STILL OPEN: none of the cited code paths changed in 4.1.0 beyond a
line shift. MD9 is narrowed: one of its three parts is fixed. MD10 is new.

| # | Where | What the server does | Evidence | 4.1.0 |
|---|-------|----------------------|----------|-------|
| MD1 | Updating a field | A type change between single and multiple choice is refused with 400 "values exist" like any other retype, although the two kinds share the option rows (the cascade reconciliation treats them as the same kind). A choice field retyped to another type must also be sent `options: []`, or the kept options fail with "Options are supported by choice fields only". | `Service:262-270, 1010-1049`; `Op:512-519` | STILL OPEN — the retype branch and `ValidateField` are unchanged |
| MD2 | Progress of a cascade | The stamp pass queued by a move is invisible: `getCascadeProgress` reports assign passes only, the stamp pass is queued with an empty template list and no audit event, so a client cannot tell that a moved folder's content is still being stamped. | `FolderDao:1354-1362`; `Worker:91-99`; `Op:261-333` | STILL OPEN — the stamp pass is still started with `[]` and no audit |
| MD3 | Progress of a cascade | The `error` of a failed pass is the raw exception message, returned to any member who may read the folder; a storage or SQL failure text reaches the client. | `Dto:458`; `Op:163-168` | STILL OPEN — `Exception?.Message` is still mapped as is |
| MD4 | Assigning templates to a file | An empty `templateIds` on a file is accepted: nothing is linked, yet the audit event "Metadata template assigned" and the socket update are sent; the folder variant returns early for the same input. The 4.1.0 test pins the 200 and the unchanged file, not the audit. | `Service:507-535` vs `545-550`; `T/MetadataFileAssignmentTests.cs:63-76` | STILL OPEN |
| MD5 | Switching the cascade | Turning a cascade off writes no audit event, while turning it on writes "Metadata cascade assignment started" and the unassignment writes "unassigned"; the only silent write of the API. | `Service:627-640` | STILL OPEN |
| MD6 | Custom fields | A request that only removes names the entry does not hold (after the system template exists) is a no-op that still writes "Metadata values updated", sends the socket event and re-indexes the entry. | `Service:402-410, 440-444` | STILL OPEN |
| MD7 | Listing filter validation | On the section listings and the rooms listing the filter is parsed before any access check (only the folder listing checks first); they are not anonymous, so a signed-in member of any kind can probe which template and field identifiers exist on the portal through the 400 texts. | `FoldersController:886-1117`; `RoomsController:1386, 1420` vs `FoldersController:456-465` | STILL OPEN — the parse sites moved by a few lines only |
| MD8 | Cascade and copy | The copy of a file pastes the source's direct metadata after the copy was created and stamped; the copy keeps both (tested), but the source's values are written without the destination's conflict mode, so an overwrite-cascading destination does not overwrite the copied values the way it overwrites a moved file's. | `FileDao:1842-1845`; `T/MetadataCascadeTests.cs:345-393` vs `T/MetadataInheritanceOnMoveTests.cs:99-113` | STILL OPEN |
| MD9 | Contract | The published document lacks the 403 of `getCascadeProgress` (see deviations); agents reading the yaml see an incomplete error set. The two other parts are fixed: the number-value limit is in the `setFileValues` / `setFolderValues` 400 text, and the SDK reads the listing entries as concrete models through `entryKind` (`7d641bf729`). | yaml 20730-20774, 21150, 21249 | PARTLY FIXED — the 403 row is still missing, so still open |
| MD10 | Text of names, options and values | A character outside the Basic Multilingual Plane in a template name, field name, option, string value or custom field fails on write with a 500 (`An error occurred while saving the entity changes`); the earlier "echoed intact, stored as `?`" was wrong on this run's MySQL stand. | `T/MetadataUnicodeTests.cs:73-120`; `Migration:16-136` (`name`, `options`, `value_string` columns) | OPEN — red test, run 365d87fe60 |
