# shared-links — verification

Every requirement and scenario of `spec.md`, with where it was verified. Paths are relative to the repo
root (`server/`); Files test paths omit the `products/ASC.Files/Tests/Tests/` prefix. `yaml` is
`sdk/docspace-api-spec/docspace-backend.yaml` (submodule `a0fc3a2`): room links `setRoomLink` (31069),
`getRoomLinks` (31172), `getRoomsPrimaryExternalLink` (31275); file links `createFilePrimaryExternalLink`
(12031), `getFilePrimaryExternalLink` (12138), `getFileLinks` (12269), `setFileExternalLink` (12402); folder
links `createFolderPrimaryExternalLink` (16995), `getFolderPrimaryExternalLink` (17102),
`setFolderPrimaryExternalLink` (17231 — the one PUT that creates, changes or revokes any folder link,
`primary` marking the primary; there is no separate "set folder external link" operation), `getFolderLinks`
(17336); visitors `getExternalShareData` (28356), `applyExternalSharePassword` (28467). Status:
**TEST-CONFIRMED** (an integration test asserts it), **CODE-ONLY** (read from the product code or the
OpenAPI document, no test), **LIVE-CONFIRMED** (only the live run of 2026-10-14 recorded in
`openspec/deviations.md` asserts it), **MATRIX-ONLY** (stated by the product matrix "Shared link rules"; no
test, code or live evidence found — flagged).

Code behind the rows: the link limits are two tables in
`products/ASC.Files/Core/Core/Security/FileSecurity.cs:105-156` (rooms: Custom and Public 1 primary + 5
additional, Form filling room 1 primary; entries inside: My documents, Custom, Collaboration, VDR and AI
rooms 6 additional, Public room 1 primary + 5 additional, Form filling room 1 primary), applied in
`products/ASC.Files/Core/Utils/FileSharing.cs:350-375` under a per-entry lock, with the message "The maximum
number of links may not exceed {0}" (`FilesCommonResource.resx:313`); the audience is forced in
`FileSharing.cs:223-239` (VDR → DocSpace users only, Public room → anyone); the primary link of a Public or
Form filling room drops a requested expiration (`FileSharing.cs:193-199`) and an expiration change on it is
skipped (`FileSharing.cs:207-221`); the role sets of links per room type are `FileSecurity.cs:158-235`
(`AvailableRoomAccesses`, the link subject rows) with the message "The role is not available for this user
type" (`FilesCommonResource.resx:331`, `FileSharing.cs:159-203`). Neither message is asserted by a test —
every "403 + message" row is confirmed for the status only. Shared theory data: `BaseTest.cs:87-110`
(valid file-link rights Editing / Review / Comment / Read; invalid ReadWrite / Varies / RoomManager /
ContentCreator; room types with links Custom, Public; without Collaboration, VDR). Trap in several
"limit" and "expiration" tests: the request omits `access`, which the server reads as a revocation, and a
revoked Public-room primary link is minted again at once (`03_Rooms/Links/RoomLinkPrimaryTests.cs:209-230`),
so an `expirationDate == null` there may be a fresh link rather than an ignored date; such rows say so.

Revision 2026-10-25: first verification; the spec was reshaped at the same time (two matrices moved into
the scenarios "Link availability matrix by location" and "Link settings matrix by location"; the fixed-value
bullets became the scenario "Fixed audience of Public room and VDR links" and were folded into the existing
Form filling room scenarios; the scenario "Member reads the room's primary link" was corrected to what the
test asserts — a Viewer is refused, Content creator and Room manager read it; no requirement or scenario
renamed).

## Requirement rows

| Claim | Source | Status |
|---|---|---|
| **Entities that accept a shared link** | | |
| My documents: file links (additional and primary) and folder primary link created | `01_Files/Links/FileLinkSetTests.cs:46-86`; `04_Security/FileShareTests.cs:139-184`; `02_Folders/Links/FolderLinkCreateTests.cs:204-217`; `04_Security/FolderShareTests.cs:44-59` | TEST-CONFIRMED |
| My documents: up to 6 additional | `FileSecurity.cs:141` (6); the most a test creates is 3 | CODE-ONLY |
| Public room: primary link auto-created, exactly one; 4 additional links listed with it; folder and file inside carry a primary link | `03_Rooms/Links/RoomLinkPrimaryTests.cs:47-60, 101-115`; `RoomLinkReadTests.cs:67-80, 289-314`; `RoomLinkCreateTests.cs:331-361`; `FolderShareTests.cs:324-340`; `04_Security/ShareInheritanceTests.cs:643-673` | TEST-CONFIRMED |
| Public room: up to 5 additional on the room and on entries | `FileSecurity.cs:122-129, 145-151` | CODE-ONLY |
| Form filling room: one auto-created primary link with the filling right; additional link → 403; file inside carries a primary link with filling only | `RoomLinkPrimaryTests.cs:63-77`; `RoomLinkReadTests.cs:367-380`; `04_Security/RoomShareTests.cs:75-98, 205-224`; `RoomLinkCreateTests.cs:364-383`; `ShareInheritanceTests.cs:812-826` | TEST-CONFIRMED |
| Form filling room: folder inside carries a primary link | `FolderShareTests.cs:363-374` asserts that `getFolderPrimaryExternalLink` on a folder in an FFR THROWS — contradicts the cell; `FileSecurity.cs:153` allows 1 primary for FFR entries; `deviations.md` D12 (live run: a primary link of a folder inside an FFR was created with an expiration) | LIVE-CONFIRMED (see R6) |
| Collaboration room and VDR: link to the room itself → 403; primary link GET 403; link list empty | `RoomLinkCreateTests.cs:364-383`; `RoomShareTests.cs:100-133`; `RoomLinkPrimaryTests.cs:172-185`; `RoomLinkReadTests.cs:338-364` | TEST-CONFIRMED |
| Collaboration room: folder inside gets a primary link; VDR: folder and file inside get primary links | `FolderShareTests.cs:342-361`; `ShareInheritanceTests.cs:760-809`; `FolderShareTests.cs:304-321` | TEST-CONFIRMED |
| Collaboration room: file inside; up to 6 additional in both room types | `FileSecurity.cs:142-144` | CODE-ONLY |
| Custom room: room link created; primary link minted on first GET (no auto-creation); primary + 5 additional, the 6th → 403; file / folder inside carry links | `RoomLinkCreateTests.cs:59-79, 331-361`; `RoomLinkPrimaryTests.cs:157-170`; `RoomLinkReadTests.cs:82-94, 246-258`; `RoomShareTests.cs:177-201`; `01_Files/Links/FileLinkPrimaryTests.cs:71-86`; `02_Folders/LinksPrimary/FolderLinkPrimaryGetTests.cs:70-86`; `02_Folders/Links/FolderLinkListTests.cs:244-260` | TEST-CONFIRMED |
| **Link count limits** | | |
| 7th additional link on a file in My documents → 403 "…6" | `FileSecurity.cs:141`; `FileSharing.cs:350-375` | CODE-ONLY |
| 6th additional link on a Custom room → 403 | `RoomShareTests.cs:177-201` | TEST-CONFIRMED |
| 6th additional link on a Public room → 403 "…5" | `FileSecurity.cs:122-129` (same table row as Custom) | CODE-ONLY |
| Second primary link in a Public room or on a file inside → 403 "…1" | `FileSecurity.cs:118, 148`; `deviations.md` D11 (live run: Public-room entries keep one primary link) | LIVE-CONFIRMED |
| Additional link in an FFR (room) → 403 | `RoomLinkCreateTests.cs:364-383`; `RoomShareTests.cs:205-224` | TEST-CONFIRMED |
| Additional link on a folder / file inside an FFR → 403 "…0" | `FileSecurity.cs:153` (no additional-link entry → limit 0) | CODE-ONLY |
| The refusal carries "The maximum number of links may not exceed {N}" | `FileSharing.cs:371-372` records the message as the result's warning | CODE-ONLY |
| **Room links and item links are independent** | | |
| A Custom room with its 6 room links still accepts a file link | separate tables for rooms and entries (`FileSecurity.cs:105-156`); the count is per entry (`FileSharing.cs:367-369`) | CODE-ONLY |
| File opened through a folder link opens with the folder link's right; through its own link with its own right (My documents and a Custom room; room / folder / file chain; short-URL redirect) | `ShareInheritanceTests.cs:123-189, 221-264, 471-547` | TEST-CONFIRMED |
| Parent folder and siblings not accessible with a file link (403) | `yaml` `getExternalShareData` (28356): the token names one entry; `ShareInheritanceTests.cs:365-415` opens only the linked entry | CODE-ONLY |
| Changing a folder link's right leaves file links unchanged | independent updates of two links on one entity `FileShareTests.cs:228-295`; `RoomShareTests.cs:227-295`; each link is its own share row | CODE-ONLY |
| **Link settings by room type** | | |
| My documents: access right (Editing / Review / Comment / Read accepted; manager rights 403), audience, password (asked from an anonymous visitor, wrong → invalid), deny download (visitor cannot download), lifetime (saved; expired refused) | `FileShareTests.cs:47-108, 186-225, 341-383`; `FileLinkSetTests.cs:116-147, 214-229`; `FolderShareTests.cs:133-203, 228-274, 844-864` | TEST-CONFIRMED |
| Public room: Read and Editing links accepted; deny download reflected | `RoomShareTests.cs:135-175`; `RoomLinkReadTests.cs:96-115` | TEST-CONFIRMED |
| Public room: audience fixed to anyone (file and folder inside: `internal:true` → stored false) | `ShareInheritanceTests.cs:701-725`; `FolderShareTests.cs:323-340` (both without `access` — see the trap; the code `FileSharing.cs:236` settles it) | TEST-CONFIRMED |
| Public room: password | `FileSecurity.cs:186-196` (same link options as Custom); no Public-room password test | CODE-ONLY |
| Public room: primary link ignores an expiration; additional links keep one; entries inside keep one | `RoomShareTests.cs:507-531` (no `access`, trap); `FileSharing.cs:193-199, 207-221` (the drop applies to the room's own primary link only) | CODE-ONLY |
| Form filling room: room link grants reading or filling; editing / review / comment → 403; a form inside offers filling only | `RoomShareTests.cs:75-98, 205-224`; `ShareInheritanceTests.cs:812-826`; `FileSecurity.cs:204-205`; `deviations.md` matrix row "Form filling room link: filling only / The room link also accepts the read right; entries accept filling only" | TEST-CONFIRMED |
| Form filling room: a link to a form / folder inside with the read right → 403 | `ShareInheritanceTests.cs:812-826` (offered rights are filling and none); `deviations.md` row above | TEST-CONFIRMED |
| Form filling room: audience, password, deny download offered; primary room link perpetual; entries keep an expiration | `FileSharing.cs:193-199` (room primary); `deviations.md` D12 and matrix row "Form filling room and Public room links are perpetual / Only the room's own primary link; entries and additional links keep an expiration date" | LIVE-CONFIRMED |
| Collaboration room: every setting on entry links | `FileSecurity.cs:142`; folder primary link `FolderShareTests.cs:342-361` (internal false, editable) | CODE-ONLY |
| VDR: audience fixed to DocSpace users (folder and file links internal by default; `internal:false` with access kept → still internal; audience not editable) | `ShareInheritanceTests.cs:760-809`; `FolderShareTests.cs:304-321` | TEST-CONFIRMED |
| VDR: access right, password, deny download, lifetime offered | `FileSecurity.cs:143`; `FileSharing.cs:223-239` forces only the audience | CODE-ONLY |
| Custom room: access right (every valid right echoed), audience toggles, password (visitor asked), deny download (visitor cannot download), lifetime (future saved, past ignored) | `RoomShareTests.cs:227-322, 344-367, 369-472`; `RoomLinkUpdateTests.cs:129-237`; `04_Security/Sharing/ShareResolveLinkTests.cs:126-146`; `FolderLinkCreateTests.cs:219-232`; `FolderLinkPrimarySetTests.cs:295-351`; `FolderLinkPrimaryGetTests.cs:283-303` | TEST-CONFIRMED |
| **A fixed "who has access" value silently skips the update** | | |
| Public room entry link with `internal:true` → 200, stays anyone | `ShareInheritanceTests.cs:701-725`; `FolderShareTests.cs:323-340` | TEST-CONFIRMED |
| VDR link with `internal:false` → 200, stays DocSpace-only | `ShareInheritanceTests.cs:777-809`; `FolderShareTests.cs:304-321` | TEST-CONFIRMED |
| The whole request is skipped (password, title, revocation not applied) | `FileSharing.cs:207-210` (`continue` when the audience differs and cannot be edited); `deviations.md` matrix row "'Rejects' an audience or lifetime value that is not offered / Answers 200 and silently normalises or skips" | LIVE-CONFIRMED |
| Anonymous visitor of a VDR link must sign in | `FileShareTests.cs:329-338` (My documents internal link, anonymous open → error); VDR links are internal (`FileSharing.cs:236`) | CODE-ONLY |
| **Deny-download on links combines with the VDR room setting** | | |
| Flag stored as requested in every room type | `RoomLinkUpdateTests.cs:193-214`; `RoomLinkReadTests.cs:96-115`; `FolderLinkPrimarySetTests.cs:295-312`; `FileLinkSetTests.cs:116-130` | TEST-CONFIRMED |
| Room setting on + link flag off → download refused; room setting off → the link flag decides | `deviations.md` matrix row "VDR: deny-download cannot be turned off on a link while the room setting is on / The flag is stored; the room setting still blocks the download"; room flag `03_Rooms/Update/RoomUpdateVdrFieldsTests.cs:79-93`; link flag effect on `download` `ShareInheritanceTests.cs:266-362` (Custom) | LIVE-CONFIRMED |
| Owner, Room managers, Content creators, Editors not restricted | owner `RoomShareTests.cs:324-342`, `FolderShareTests.cs:160-178`; `room-types` (`denyDownload`: members without editing rights) | TEST-CONFIRMED (owner) / CODE-ONLY (roles) |
| **Roles that manage links** | | |
| Room manager creates / edits / deletes a room link (Public) and a folder primary link (Custom); Owner and DocSpace admin in own room | `03_Rooms/Permissions/RoomLinkPermissionsTests.cs:227-248, 272-299`; `02_Folders/LinksPrimary/FolderLinkPrimaryPermissionsTests.cs:64-98`; `FolderLinkCreateTests.cs:293-331`; `FolderLinkListTests.cs:304-344` | TEST-CONFIRMED |
| Content creator cannot create / edit a room link or the room folder's link (403) | `RoomLinkPermissionsTests.cs:272-299`; `FolderLinkPrimaryPermissionsTests.cs:100-117` | TEST-CONFIRMED |
| Content creator cannot create a link on a file they created | `FileSharing.cs:101` (`CanSetAccessAsync`: manage right of the room); file-level test exists only for My documents (`FileLinkSetTests.cs:361-401`, another user's file 403) | CODE-ONLY |
| Viewer, Guest Viewer, non-members → 403; anonymous 401; terminated 401 | `RoomLinkPermissionsTests.cs:250-270, 301-359`; `FolderLinkPrimaryPermissionsTests.cs:119-159` | TEST-CONFIRMED |
| Editor, Form Filler, Reviewer, Commentator → 403 | same check as the Viewer (`FileSharing.cs:101`) | CODE-ONLY |
| Primary link of a Custom room: Room manager and Content creator read it; a Viewer (User with Read) → 403; the folder endpoint likewise | `RoomLinkPermissionsTests.cs:98-147`; `03_Rooms/RoomAccessData.cs:88-92`; `FolderLinkPrimaryPermissionsTests.cs:259-276`; `getRoomLinks` for a member without link access → 200 and empty `RoomLinkReadTests.cs:504-520` | TEST-CONFIRMED |
| Reading the primary link of a file requires the right to manage links | `01_Files/Links/FileLinkPrimaryTests.cs:189-210` (bug 81572: no-access caller gets 200 instead of 403, test red) | TEST-CONFIRMED (intent; see R7) |
| Non-member DocSpace admin reads the Public room's primary link (200) and cannot create a link there (403) | `RoomLinkPermissionsTests.cs:62-77, 250-270` | TEST-CONFIRMED |

Counts: 46 rows — TEST-CONFIRMED 26, CODE-ONLY 15, LIVE-CONFIRMED 5, MATRIX-ONLY 0. Spec: 7 requirements,
30 scenarios (before the reshaping: 7 requirements, 27 scenarios). Flag: no test asserts either message; no
test reaches the 6-link limit on an entry or the 5-link limit in a Public room; no test of a VDR link
against the room-wide deny-download setting; no test of the "whole request skipped" rule beyond the audience
field itself.

## Where the server differs from the product matrix (spec follows the server)

| Matrix ("Shared link rules") | Server | Reference |
|---|---|---|
| Maximum 6 links per file or folder | 6 additional plus primary links (D11: up to 6 primary links on entries outside Public rooms) | `deviations.md`, matrix table, D11 |
| Form filling room link: filling only | The room link also accepts the read right (`FileSecurity.cs:204-205`); entries accept filling only | same |
| Form filling room and Public room links are perpetual | Only the room's own primary link (`FileSharing.cs:193-199`); entries and additional links keep an expiration | same; D12 |
| "Rejects" an audience or lifetime value that is not offered | Answers 200 and silently normalises or skips | same; `FileSharing.cs:207-239` |
| VDR: deny-download cannot be turned off on a link while the room setting is on | The flag is stored; the room setting still blocks the download | same |
| Previous revision of this spec: "Any member MAY read the room's primary link" (scenario: a Viewer of a Custom room) | A User invited with Read gets 403 on `getRoomsPrimaryExternalLink` and on the folder endpoint; Room manager and Content creator get 200. The scenario now says so. | `RoomLinkPermissionsTests.cs:126-147`; `FolderLinkPrimaryPermissionsTests.cs:259-276` |
| yaml `setFolderPrimaryExternalLink` is named as if it set only the primary link | It is the one PUT for any folder or room link (create, change, revoke), `primary` marks the primary; the SDK method name follows the operationId | yaml 17231 |
| Tests `RoomShareTests.cs:507-531`, `ShareInheritanceTests.cs:675-699`, `FolderShareTests.cs:323-340`, `ShareInheritanceTests.cs:701-725` | Send no `access`, i.e. a revocation; a Public-room primary link is minted again at once, so the asserted `expirationDate == null` / `internal == false` may be a replacement link, not an ignored value. The spec relies on the code for those rows. | `RoomLinkPrimaryTests.cs:209-230` |

## Left out of the spec, and why

- **Revoked Public-room primary link is recreated at once; a revoked Custom-room primary stays revoked and
  GET answers 404** (`RoomLinkPrimaryTests.cs:209-230`; `RoomShareTests.cs:474-557, 643-673, 727-758`;
  `FolderLinkPrimarySetTests.cs:187-215`, bug 81807): link lifecycle, `room-types` ("Public room" primary
  link) and `file-sharing`.
- **Past or malformed expiration silently ignored** (`RoomLinkUpdateTests.cs:155-191`;
  `RoomLinkValidationTests.cs:295-319`; `FolderLinkPrimarySetTests.cs:333-351`); **unknown link id is an
  upsert**; **link type cannot be changed**; **missing type defaults to invitation**; **missing access creates
  nothing** (`RoomLinkCreateTests.cs:158-233`; `RoomLinkUpdateTests.cs:292-312`; `FileLinkSetTests.cs:236-252`):
  request semantics, `file-sharing`.
- **Folder primary link ignores the requested title** (bug 81573, `FolderLinkCreateTests.cs:102-120`): `file-sharing`.
- **Portal-wide external-sharing restriction** forces `internal` and blocks anonymous visitors
  (`04_Security/ExternalShareAccessTests.cs:59-237`, bugs 81840, 82051): portal settings (`file-sharing`).
- **Password of a room link does not propagate to entry links** (`ShareInheritanceTests.cs:365-468`); **a link
  never beats a member's personal rights; last visited link wins for non-members; expired room link falls back
  to the folder link** (`RoomShareTests.cs:605-834`; `FileShareTests.cs:700-786, 894-947`;
  `ShareInheritanceTests.cs:44-120, 550-640`): access resolution, `file-sharing`.
- **Invitation links** (singleton, use count, race; `RoomLinkCreateTests.cs:82-104`;
  `RoomLinkUpdateTests.cs:239-262, 414-443`; `RoomLinkValidationTests.cs:321-355`): `invitations`.
- **Archived room: PUT links 403, GET primary still works** (`RoomLinkValidationTests.cs:205-224`;
  `RoomLinkPrimaryTests.cs:187-202`): `room-types`.
- **Anonymous visitors see no author fields or creator e-mails; anonymous cookie confined to logos**
  (`RoomLinkFileAuthorTests.cs:59-105`; `FileLinkPrivacyTests.cs:57-106`, bug 80495;
  `RoomLinkStorageAccessTests.cs`, bug 84541): privacy, `file-sharing`.
- **`getExternalShareData` for an anonymous visitor returns `shared:false` by design** (skipped tests
  `RoomLinkAnonymousAccessTests.cs:65-95`, bugs 83319 / 83166).

## Suspected defects

D-numbers are the ones of `openspec/deviations.md`; R-numbers are new.

| # | Where | What the server does | Evidence | Status |
|---|-------|----------------------|----------|--------|
| D2 | Roles that manage links | In a Public room a non-member User or Guest gets 200 on the folder-links PUT with `primary:true` and the read right, and a primary read link is created on a folder they cannot read; the token is not returned to them. The same request on a Custom room is refused. | `deviations.md` D2 (live run); `FileSharing.cs:87-99` (the Public-room primary read link bypasses the manage check) | OPEN |
| D11 | Link count limits | Entries in My documents, Custom, Collaboration and VDR rooms accept up to 6 *primary* links in addition to 6 additional ones; rooms and Public-room entries keep one primary link. | `deviations.md` D11 (live run); `FileSharing.cs:353-358` (an entry without a primary-link limit inherits the additional-link limit) | OPEN |
| D12 | Link settings (FFR) | A primary link of a folder inside an FFR stores an expiration on create; later updates and the revocation are skipped unless the stored date is echoed. | `deviations.md` D12 (live run); `FileSharing.cs:207-221` | OPEN |
| R6 | Entities that accept a link (FFR folder) | `getFolderPrimaryExternalLink` on a folder inside a Form filling room throws (the test asserts the exception without a status) although the limits table allows one primary link on it and the live run created one through the PUT. GET and PUT disagree. | `FolderShareTests.cs:363-374`; `FileSecurity.cs:153`; D12 | OPEN |
| R7 | Roles that manage links | `getFilePrimaryExternalLink` answers 200 to a caller without access to the file instead of 403; `POST folder/{id}/link` answers 200 with an empty list to a Content creator, a Viewer, a non-member admin, a Guest, and on an archived room instead of 403. | `FileLinkPrimaryTests.cs:189-210` (bug 81572, red); `FolderLinkCreateTests.cs:271-421` (bug 81575, red) | OPEN |
| R8 | Link count limits | The link-limit refusal is produced as the operation's *warning* with the entry skipped, not as an exception; the observed 403 depends on the caller turning the warning into an error. Invalid link type or access answers 403 instead of 400. | `FileSharing.cs:371-373`; `RoomLinkValidationTests.cs:235, 258` (bugs 82370, 82371) | OPEN |
