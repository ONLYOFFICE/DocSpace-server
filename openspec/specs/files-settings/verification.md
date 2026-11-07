# files-settings — verification

> **Test run 365d87fe60** (`dotnet test ASC.Tests.slnx --max-parallel-test-modules 1`, 2026-11-07), written by script. After TEST-CONFIRMED: `[run: PASS]` the cited tests passed; `[run: PASS-ON-RERUN]` failed in the full run and passed alone; `[run: FAIL-BUG]` a cited test failed and carries a bug mark, so the row may describe a defect; `[run: FAIL]` failed without a bug mark, repeated alone; `[run: SKIP]` only skipped; `[run: NOT-IN-RUN]` cited test not in the results; `[run: NO-LINK]` no test citation the script could resolve. PASS means the cited test passed, not that it proves the claim. Line ranges map to test methods approximately.

Every requirement of `spec.md` with where it was verified. No test was run for this document: TEST-CONFIRMED means a
test of this repository asserts the claim (read at the cited lines, or "by name" where only its name was read), not that
the test is green. Many cited tests carry a numbered `Bug` trait; it may mark an open bug or a regression guard, and the
two are told apart below only where stated. CODE-ONLY means read in the product code or the contract, no test.

Paths: `T/` is `products/ASC.Files/Tests/Tests/`, `S` is `products/ASC.Files/Server/Api/SettingsController.cs`, `E` is
`products/ASC.Files/Server/Api/EditorController.cs`, `Fi` is `products/ASC.Files/Server/Api/FilesController.cs`, `Fo` is
`products/ASC.Files/Server/Api/FoldersController.cs`, `Sec` is `products/ASC.Files/Server/Api/SecurityController.cs`,
`FSS` is `products/ASC.Files/Core/Core/FileStorageService.cs`, `DT` is
`products/ASC.Files/Core/Configuration/DefaultTemplateSettings.cs`, `Tags` is
`products/ASC.Files/Core/Core/VirtualRooms/CustomTagsService.cs`, `Trk` is `products/ASC.Files/Core/Utils/FileTracker.cs`,
`yaml` is `sdk/docspace-api-spec/docspace-backend.yaml`.

Operations that fit an existing capability and are described here because that capability does not name them:
`file-operations`: `saveFileAsPdf`, `createHtmlFileInMyDocuments`, `createEditSession`, `getFolderPath`, `getFolders`,
`setFilesOrder`, `startEditFile`, `trackEditFile`, `getEditDiffUrl`, `getProtectedFileUsers`. `room-types`:
`changeRoomCover`, `deleteRoomLogo`, `createRoomTag`, `deleteCustomTags`, `hasTagLinks` (its tag and cover requirements
cover the room fields, not these operations). `file-sharing` / `shared-links`: the link operations of files and
folders, `getGroupsMembersWith*Security`, `applyExternalSharePassword`. `private-rooms`: `getEncryptionInfo`.
None of those specs was edited.

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Personal file settings switches** | | |
| Hide-cancel, room-lifetime, keep-name, same-tab: returns value, both ways, idempotent, `{}` = false, in settings, per user, every role, 401 anonymous/terminated | `T/07_Settings/Editor/SettingsToggleTestBase.cs:70-181`, derived by `HideConfirmCancelOperationTests`, `HideConfirmRoomLifetimeTests`, `KeepNewFileNameTests`, `SetOpenEditorInSameTabTests` | TEST-CONFIRMED [run: NOT-IN-RUN] |
| `displayRecent`: same behaviours; `{}` answers a boolean | `T/07_Settings/Storage/DisplayRecentTests.cs:49-115` (85-96 read, others by name), `DisplayRecentPermissionsTests.cs:46-88` by name | TEST-CONFIRMED [run: PASS] |
| `displayFileExtension` | `T/07_Settings/Storage/DisplayFileExtensionTests.cs:49-115`, `DisplayFileExtensionPermissionsTests.cs:46-88` by name | TEST-CONFIRMED [run: PASS] |
| `storeOriginal` | `T/07_Settings/Storage/StoreOriginalTests.cs:48-114` (84-95 read), `StoreOriginalPermissionsTests.cs:46-88` by name | TEST-CONFIRMED [run: PASS] |
| `changeDeleteConfirm` | `T/07_Settings/SharingDefaults/ChangeDeleteConfirmTests.cs:56-149` (84-94 read, others by name) | TEST-CONFIRMED [run: PASS] |
| **Switches for quick actions and room grouping** | | |
| Stored per account, boolean answer | `S:495-499`, `S:739-744`, their remarks | CODE-ONLY |
| `getRoomsFolder` not affected by the grouping | `S` remark at 722-734 (not traced further) | CODE-ONLY |
| **Conversion prompt can only be hidden** | | |
| Always true, two independent flags, `{}` true | `T/07_Settings/Editor/HideConfirmConvertTests.cs:67-116` | TEST-CONFIRMED [run: PASS] |
| Isolated per user, every role, 401 | same file 120-168 by name | TEST-CONFIRMED [run: NO-LINK] |
| **Download archive format** | | |
| Value readable through `downloadTarGz`, per user | `T/07_Settings/SharingDefaults/DownloadTarGzTests.cs:58-68`, 97 by name | TEST-CONFIRMED [run: PASS] |
| Answer object does not confirm the choice | `S` remark 100-114 | CODE-ONLY |
| **Compatibility stubs store nothing** | | |
| `storeForcesave` equals the settings value | `T/07_Settings/Storage/ForceSaveTests.cs:77-84` | TEST-CONFIRMED [run: PASS] |
| `storeForcesave` returns the constant false | `S:409-414` | CODE-ONLY |
| `updateFileIfExist` returns the constant false | `S:517-521` | CODE-ONLY |
| **Trash auto-clearing** | | |
| Enable with gap, disable, without gap, gap alone, `{}` | `T/07_Settings/Storage/AutoCleanupTests.cs:55-161` | TEST-CONFIRMED [run: PASS] |
| Reflected in read and in settings, per user | same file 165-251 by name | TEST-CONFIRMED [run: NO-LINK] |
| Roles and 401 | `T/07_Settings/Storage/AutoCleanupPermissionsTests.cs:49-146` by name | TEST-CONFIRMED [run: PASS] |
| **Permission to connect third-party storages** | | |
| Answer and settings value | `T/07_Settings/Storage/ThirdPartyAccessTests.cs:50-103` by name | TEST-CONFIRMED [run: PASS] |
| Non-admin 403; deleted/terminated/anonymous 401 | `T/07_Settings/Storage/ThirdPartyAccessPermissionsTests.cs:46,79-91,102-118,122` | TEST-CONFIRMED [run: PASS] |
| `saveThirdParty` refused when off | `T/07_Settings/Storage/ThirdPartyAccessTests.cs:116-136` | TEST-CONFIRMED [run: PASS] |
| **Module description** | `T/07_Settings/Editor/GetFilesModuleTests.cs:52-61`; 65-97 by name | TEST-CONFIRMED [run: PASS] |
| **Document Server address is readable without signing in** | | |
| `isDefault`, version true/false, anonymous read | `T/07_Settings/Editor/GetDocServiceUrlTests.cs:56-62,77-93,97-107` | TEST-CONFIRMED [run: PASS] |
| **Document Server address is changed with verification** | | |
| 403 "Access denied", 401 anonymous/terminated | `T/07_Settings/Editor/CheckDocServiceUrlTests.cs:61-112` | TEST-CONFIRMED [run: PASS] |
| Validation, mixed content, rollback, 500 on failed verification | `E:609-700`, yaml `checkDocServiceUrl` | CODE-ONLY |
| **Default templates are read with the settings permission** | `S:628-634`, `DT:67-85` | CODE-ONLY |
| **Default template from an existing file** | | |
| Missing `selectedFile` 400 | `T/07_Settings/SharingDefaults/DefaultTemplateTests.cs:45-55` | TEST-CONFIRMED [run: PASS] |
| Administrator on owner's file 403 | same file 65-80 | TEST-CONFIRMED [run: NO-LINK] |
| Extension mismatch 400 | same file 98-109; `DT:218-221` (throws the 400 exception; the test's "still red" comment is stale by that code) | TEST-CONFIRMED [run: NO-LINK] |
| Unknown file 404 | same file 119-128; `DT:210-211` | TEST-CONFIRMED [run: NO-LINK] |
| Copy, previous file deleted, unknown extension is a silent 200 | `DT:87-123` | CODE-ONLY |
| **Default template from an upload** | | |
| Over 100 MB 400 with the message | `T/07_Settings/SharingDefaults/DefaultTemplateTests.cs:140-151`; `S:710-712` | TEST-CONFIRMED [run: PASS] |
| Declared extension contradicted 400 | same file 165-213; `DT:129-134` (the comments saying 403 are stale by that code) | TEST-CONFIRMED [run: NO-LINK] |
| PDF not a form 403; storing and previous file deleted | `DT:143-168`; `InvalidOperationException` maps to 403 per `common/ASC.Api.Core/Middleware/ResponseWrapper.cs` | CODE-ONLY |
| **Default template reset** | `S:681-687`, `DT:87-123` | CODE-ONLY |
| **Editing a file alone** | | |
| Owner and invited manager get a key | `T/01_Files/Editing/EditingStartTests.cs:52-62`, 73 by name | TEST-CONFIRMED [run: PASS] |
| Second claim 403 | same file 116-136 | TEST-CONFIRMED [run: NO-LINK] |
| 401 anonymous, 403 guest and read-only, 404 unknown | `T/01_Files/Editing/EditingStartPermissionsTests.cs:46-59,63-80`, 84 by name; `EditingStartTests.cs:92` by name | TEST-CONFIRMED [run: PASS] |
| **Starting a shared editing session** | | |
| `{}` or false: 403 "File editing start error" | `T/01_Files/Editing/EditingStartCoAuthoringTests.cs:47-59,65-80` (integration host has no Document Server tracking) | TEST-CONFIRMED [run: PASS] |
| Key on success | `FSS:2365-2381` | CODE-ONLY |
| **Tracking an editing session** | | |
| `key` true, empty `value` | `T/01_Files/Editing/EditingTrackTests.cs:126-140`; 51,81 | TEST-CONFIRMED [run: PASS] |
| 401, 403 guest/read, 404, administrator allowed | `T/01_Files/Editing/EditingTrackPermissionsTests.cs:46-117`, 85 by name; `EditingTrackTests.cs:112-122` | TEST-CONFIRMED [run: PASS] |
| Stale `docKeyForTrack` 403; `isFinish` drops the tab | yaml `trackEditFile` | CODE-ONLY |
| **Users offered for document protection** | | |
| Guest 403 "Access denied" | `T/01_Files/Editing/ProtectUsersPermissionsTests.cs:47-67` | TEST-CONFIRMED [run: PASS] |
| List content, unknown id 403 | yaml `getProtectedFileUsers` | CODE-ONLY |
| **Address of a version's changes** | | |
| Latest is version 0, API-bumped and first version have no `previous` | `T/01_Files/EditDiff/GetEditDiffTests.cs:44-57,77-92,96-106` | TEST-CONFIRMED [run: PASS] |
| 404 message | same file 115-122 | TEST-CONFIRMED [run: NO-LINK] |
| Comment, no access, anonymous 403; read and guest | `T/01_Files/EditDiff/GetEditDiffPermissionsTests.cs:103-120,145-177`; 124,181 by name | TEST-CONFIRMED [run: PASS] |
| **Chunked session overwriting a file** | | |
| Fields, without size, 404 | `T/01_Files/EditSession/EditSessionTests.cs:54-96` | TEST-CONFIRMED [run: PASS] |
| Read access and guest 403, anonymous 401 | `T/01_Files/EditSession/EditSessionPermissionsTests.cs:88-125`, 129 by name | TEST-CONFIRMED [run: PASS] |
| **Bulk order of entries** | | |
| Entry fields, count | `T/01_Files/Order/FilesBulkOrderTests.cs:49-65,298-315` | TEST-CONFIRMED [run: PASS] |
| Reflected in content, equal values | same file 92,122,243 by name | TEST-CONFIRMED [run: NO-LINK] |
| Empty list, 0 is 400, maximum accepted, 404 unknown and wrong type | same file 172-181,192-205,226-239,264-294 | TEST-CONFIRMED [run: NO-LINK] |
| Guest and read 403, anonymous 401, room manager allowed | `T/01_Files/Order/FilesBulkOrderPermissionsTests.cs:67-84,117-134`; 47,97 by name; `FSS:2832` | TEST-CONFIRMED [run: PASS] |
| **Saving a file as PDF** | | |
| Title ends with `.pdf` | `T/01_Files/Forms/IsFormPdfTests.cs:159-168` | TEST-CONFIRMED [run: PASS] |
| 404, archived 403, nothing created | `T/01_Files/CopySave/SaveFileAsPdfTests.cs:53-107` | TEST-CONFIRMED [run: PASS] |
| No create access, other room 403; no access, anonymous | `T/01_Files/CopySave/SaveFileAsPdfPermissionsTests.cs:52-75,131-156`; 81,107 by name | TEST-CONFIRMED [run: PASS] |
| **HTML file in My documents** | | |
| Created, 400, true same id, omitted new id | `T/01_Files/Create/HtmlFileCreateTests.cs:85-96,101-108,112-124,145-155`; 128 by name | TEST-CONFIRMED [run: PASS] |
| Roles, guest 404, anonymous 401 | `T/01_Files/Create/HtmlFileCreatePermissionsTests.cs:102-111`; 47-88,115 by name | TEST-CONFIRMED [run: PASS] |
| `true` updates the content with a version | yaml `createHtmlFileInMyDocuments` (not in the spec text beyond "use the file") | CODE-ONLY |
| **Encryption data of a file** | | |
| Keys returned | `T/08_Private/PrivacyRoomTest.cs:149-159` | TEST-CONFIRMED [run: PASS] |
| Read access required | yaml `getEncryptionInfo` | CODE-ONLY |
| **Primary external link of a file** | | |
| Shape, lazily created | `T/01_Files/Links/FileLinkPrimaryTests.cs:47-67`; `FSS:4491-4542` | TEST-CONFIRMED [run: PASS] |
| Same id, roles | same file 89,104-151 by name | TEST-CONFIRMED [run: NO-LINK] |
| Anonymous 401, no access 403 | same file 174-186,195-209 (the "currently 200" comment at 190 is stale by `FSS:4523`) | TEST-CONFIRMED [run: NO-LINK] |
| **Primary external link of a folder or room** | | |
| Shape, `count` 0, `startIndex` | `T/02_Folders/LinksPrimary/FolderLinkPrimaryGetTests.cs:46-66,146-172` | TEST-CONFIRMED [run: PASS] |
| Same link | `T/02_Folders/Links/FolderLinkCreateTests.cs:255-268` | TEST-CONFIRMED [run: PASS] |
| No `access`: 200, count 0 | same file 158-170 | TEST-CONFIRMED [run: NO-LINK] |
| Refusals, 404s | same file 189-200,277-290,339-355,382-398; 361,404,423 by name; `T/02_Folders/LinksPrimary/FolderLinkPrimaryPermissionsTests.cs:260-297`; `FolderLinkPrimaryGetTests.cs:104-114`, 118,132 by name | TEST-CONFIRMED [run: PASS] |
| **Revoked primary link stays revoked on read** | | |
| Folder: 404 after revoke | `T/02_Folders/LinksPrimary/FolderLinkPrimarySetTests.cs:195-214` | TEST-CONFIRMED [run: PASS] |
| File: same path; `primary` true clears the state; replaced in Public and Form filling rooms | `FSS:4491-4503,6622-6660` | CODE-ONLY |
| **Setting an external link of a file** | | |
| Structure and fields | `T/01_Files/Links/FileLinkSetTests.cs:46-197` by name | TEST-CONFIRMED [run: PASS] |
| ReadWrite 403, unknown id creates, 404, 401, 403 | same file 214-228,237-251,255-268,343-379; 383 by name | TEST-CONFIRMED [run: NO-LINK] |
| Roles | same file 272-325 by name | TEST-CONFIRMED [run: NO-LINK] |
| **Setting an external link of a folder or room** | | |
| Revoke answer, past expiration, ReadWrite 403, unknown id | `T/02_Folders/LinksPrimary/FolderLinkPrimarySetTests.cs:170-184,263-274,334-350,354-366` | TEST-CONFIRMED [run: PASS] |
| Fields, same id | same file 46-150,278,370,387 by name | TEST-CONFIRMED [run: NO-LINK] |
| Roles, 401, 404 | `T/02_Folders/LinksPrimary/FolderLinkPrimaryPermissionsTests.cs:50-178` by name; `FolderLinkPrimarySetTests.cs:218-248` by name | TEST-CONFIRMED [run: PASS] |
| **Listing the links of a file** | | |
| Empty, 401, 403 | `T/01_Files/Links/FileLinkListTests.cs:45-56,206-243` | TEST-CONFIRMED [run: PASS] |
| Paging, created link, roles | same file 76-190 by name | TEST-CONFIRMED [run: NO-LINK] |
| **Listing the links of a folder or room** | | |
| Empty, revoked, 404 zero id, read-only empty | `T/02_Folders/Links/FolderLinkListTests.cs:66-76,221-240,277-287,347-364` | TEST-CONFIRMED [run: PASS] |
| 403, 401, other 404s | same file 263,291,374,396,417 by name | TEST-CONFIRMED [run: NO-LINK] |
| **Folder path** | | |
| Order, trash, 404, 403 message | `T/02_Folders/Read/FolderPathTests.cs:58-72,132-139,166-171`; `FolderPathPermissionsTests.cs:67-78` | TEST-CONFIRMED [run: PASS] |
| Room, section, archived, favourites, recent, 401 | `FolderPathTests.cs:90-154`, `FolderPathPermissionsTests.cs:54` by name | TEST-CONFIRMED [run: PASS] |
| **Subfolders** | | |
| Direct only, empty, count, virtual, 404, deleted | `T/02_Folders/Subfolders/SubfoldersTests.cs:89-146,151-177,228-238` | TEST-CONFIRMED [run: PASS] |
| Public room 403; others | `T/02_Folders/Subfolders/SubfoldersPermissionsTests.cs:130-141`; 43,56,95,227,265 by name; `SubfoldersTests.cs:182,214` by name | TEST-CONFIRMED [run: PASS] |
| **Changing a room's cover** | | |
| Answer, twice, idempotent, template, private, 404, archived | `T/03_Rooms/Covers/RoomCoverChangeTests.cs:66-85,162-209,234-255,285-319,344-371` | TEST-CONFIRMED [run: PASS] |
| Colour alone, cover alone, every type, every cover, survive archive, deleted | same file 89,106,213,263,323,375 by name | TEST-CONFIRMED [run: NO-LINK] |
| Too short colour 400, empty colour, empty body | `T/03_Rooms/Covers/RoomCoverValidationTests.cs:73-87,128-174`; 49,91 by name | TEST-CONFIRMED [run: PASS] |
| Foreign, invited user, anonymous | `T/03_Rooms/Covers/RoomCoverPermissionsTests.cs:180-198,234-254,285-301` | TEST-CONFIRMED [run: PASS] |
| **Removing a room's logo** | | |
| Removed, no logo, 404, archived, repeated, deleted, restores cover, template and derived room | `T/03_Rooms/Logos/RoomLogoDeleteTests.cs:46-57,76-115,153-164,238-252,273-293,316-356` | TEST-CONFIRMED [run: PASS] |
| Sharing preserved (assertion is on the room id) | same file 360-375 | TEST-CONFIRMED [run: NO-LINK] |
| Foreign, anonymous, invited user, editor | `T/03_Rooms/Logos/RoomLogoDeletePermissionsTests.cs:65-96,148-163,190-205` | TEST-CONFIRMED [run: PASS] |
| **Creating a tag in the catalogue** | | |
| Empty, blank, long 400; repeated once; case; recreated; missing/null/non-string | `T/03_Rooms/Tags/RoomTagCreateTests.cs` assertions at 123,142,206,216-223,237-240,264-275; 151-183 per index (BadRequest) | TEST-CONFIRMED [run: PASS] |
| Roles | `T/03_Rooms/Permissions/RoomTagCreatePermissionsTests.cs:45,69,87,103` by name; `Tags:47-53` | TEST-CONFIRMED [run: PASS] |
| **Deleting tags from the catalogue** | | |
| Attached, duplicates, unknown, long 400, similar names, reuse | `T/03_Rooms/Tags/RoomCustomTagDeleteTests.cs:70-87,120-142,171-183,268-288`; 308 by name | TEST-CONFIRMED [run: PASS] |
| Validation of the body | `T/03_Rooms/Permissions/RoomCustomTagValidationPermissionsTests.cs:47-60,91-100,112-122,131-140` | TEST-CONFIRMED [run: PASS] |
| Roles | `T/03_Rooms/Permissions/RoomTagDeletePermissionsTests.cs:45-103` | TEST-CONFIRMED [run: PASS] |
| **Checking whether a tag is used** | | |
| Detached, deleted room, unknown/blank, query decides | `T/03_Rooms/Tags/RoomTagHasLinksTests.cs:67-89,114-156,194-209` | TEST-CONFIRMED [run: PASS] |
| Roles | `T/03_Rooms/Permissions/RoomTagDeletePermissionsTests.cs:111-178` | TEST-CONFIRMED [run: PASS] |
| **Group members with their access** | | |
| Guest 403 on the file operation | `T/04_Security/Sharing/GroupAndOwnerSharingTests.cs:49-85` | TEST-CONFIRMED [run: PASS] |
| Fields, paging, folder operation, empty list | `Sec:320-345`, yaml `getGroupsMembersWithFolderSecurity` | CODE-ONLY |
| **Unlocking a password-protected link** | | |
| `InvalidPassword`, `Ok` | `T/04_Security/RoomShareTests.cs:434-439` | TEST-CONFIRMED [run: PASS] |
| Attempt limit 403 | yaml `applyExternalSharePassword` | CODE-ONLY |

## Differences between the yaml and the server

| Operation | yaml | Server |
|---|---|---|
| `getFolderLinks` | lists 200 and 404 only | Tests assert 403 for a person without room access (`T/02_Folders/Links/FolderLinkListTests.cs:374,396`, by name) |
| `uploadDefaultTemplate` | description: a name with another extension answers 403; the 400 response in the same operation says 400 | 400 (`DT:129-134`) |
| `deleteCustomTags`, `hasTagLinks` | "matched exactly" | Case-insensitive in the tests (`RoomCustomTagDeleteTests.cs:203-215`, `RoomTagHasLinksTests.cs:160-172`), on the MySQL stand; PostgreSQL not checked |
| `deleteCustomTags` | empty list is a no-op | Code agrees (`Tags`, `names.Count == 0`), the test accepts 200 or 400 (`RoomCustomTagValidationPermissionsTests.cs:64-83`) |
| `createRoomTag` | no 500 listed | Name outside the BMP answers 500 (`RoomTagCreateTests.cs:95-109`) |
| `getEncryptionInfo` | text: a missing file is 403; responses also list 404 for it | Not checked in code |
| `createFilePrimaryExternalLink` | documents 404 after revocation | Code agrees; the test for bug 84872 expects recreation (FX3) |

## Left out

None of the owned operations. All 51 operations of `Files / Settings`, `Files / Files`, `Files / Folders`, `Rooms` and
`Files / Sharing` listed in `coverage-absent.txt` are in a requirement. `Files / Third-party integration` was not
owned by this capability. `setDefaultTemplate` on third-party string ids was not described in detail.

## Suspected defects

| # | Where | What the server does | Evidence | Evidence kind |
|---|---|---|---|---|
| FX1 | `startEditFile` | Any refused call drops the file's whole tracker: the catch calls the tracker removal with no tab or user, which removes the entry. A second person's refused claim, or a reader's refused start, therefore ends the first person's editing mark, and the next claim succeeds | `FSS:2383-2388`, `Trk:86-115` | red by code reading (no test covers the state after the refusal) |
| FX2 | `startEditFile` | A second person refused a claim is told "This document is being edited by you in another tab" | `T/01_Files/Editing/EditingStartTests.cs:116-136` asserts that text for another account | pinned by a test; wording judged by reading |
| FX3 | `createFilePrimaryExternalLink` (and the folder one, same path) | After a revocation the POST answers 404, because create shares the read path; the UI button cannot share the file again. `setFileExternalLink` with `primary` true still works | `T/01_Files/Links/FileLinkPrimaryTests.cs:212-239` (Bug 84872), `FSS:4496-4503` | red test by reading |
| FX4 | `get*PrimaryExternalLink`, `create*PrimaryExternalLink` | The revocation check runs before the sharing right check, so a caller with no access to a revoked entry gets 404 rather than 403, while an unrevoked entry gives 403 | `FSS:4496-4503` before `FSS:4523` | red by code reading |
| FX5 | `getDocServiceUrl` | The anonymous answer includes the internal Document Server address, the portal address as the service sees it, the signature header name and the SSL flag | `E:721-746`; `T/07_Settings/Editor/GetDocServiceUrlTests.cs:97-107` pins anonymous reading only | red by code reading |
| FX6 | `setFilesOrder` | Items are applied one at a time with no transaction, so a later item's 403 or 404 leaves earlier items reordered while the caller sees an error | `FSS:2824-2870` | red by code reading |
| FX7 | `createRoomTag` | A name with a character outside the BMP is refused by the database write with 500, not validated as 400; the test comment says bug 81682 was closed as by-design | `T/03_Rooms/Tags/RoomTagCreateTests.cs:85-109` | per test comment |
| FX8 | `setDefaultTemplate` | A numeric `selectedFile` that is not a 32-bit integer answers 500; the yaml even declares it | `DT:103`, yaml `setDefaultTemplate` | red by code reading |
| FX9 | `getGroupsMembersWithFolderSecurity`, `getGroupsMembersWithFileSecurity` | `filterValue` with an uppercase letter matches nothing, per the yaml text | yaml `getGroupsMembersWithFolderSecurity`; not read in code | yaml text only |

Stale test comments seen (assertions cited above are unaffected): `DefaultTemplateTests.cs:90-95,156-162,178-181,197-200`
say the mismatches answer 403, `DT:129-134,218-221` raise the 400 exception; `FileLinkPrimaryTests.cs:189-192` and
`FolderLinkCreateTests.cs:271-274` say "currently answers 200", while `FSS:4523` refuses and commits `021004d5bd` and
`cdddfed875` are titled as fixing bug 81575. Whether those tests are green now was not established.
