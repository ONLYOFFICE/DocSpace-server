---
paths:
  - "products/ASC.Files/Core/**/*.cs"
  - "products/ASC.Files/Server/**/*.cs"
---

# Sections: a feature must work everywhere an entry is shown

The same file, folder or room is listed through several sections, and each section has **its own
code path**: a branch in `EntryManager.GetEntriesAsync` (`Utils/EntryManager.cs`) for the listing
and a branch in `FileSecurity.FilterEntryAsync` (`Core/Security/FileSecurity.cs`) for what may be
done with it there. A change that works in "My documents" can be invisible, unfiltered or wrongly
permitted in Recent, Favorites, Shared with me, Trash or Forms. Most section bugs in this repo were
exactly that.

## Section map

`FolderType` (`Core/Entries/Folder.cs`) selects the root; `SearchArea`
(`Core/VirtualRooms/SearchArea.cs`) selects the room section; `DocSpaceHelper` holds the room-type
helpers.

| Section | Subsection | Selected by |
|---|---|---|
| Files | My documents | `USER` (5) |
| | Shared with me | `SHARE` (6) — `fileSecurity.GetSharesForMeAsync` |
| | Recent | `Recent` (11) — a `TagType.Recent` tag, not a folder |
| | Favorites | `Favorites` (10) — a `TagType.Favorite` tag, not a folder |
| | Trash | `TRASH` (3) |
| | Templates | `Templates` (12), `DefaultTemplates` (35) |
| Rooms | active / archive / templates | `VirtualRooms` (14) + `SearchArea.Active`; `Archive` (20) / `SearchArea.Archive`; `RoomTemplates` (30) / `SearchArea.Templates` |
| Forms | form rooms / form templates | `Forms` (36) / `SearchArea.Forms`, `SearchArea.FormTemplates` — `FillingFormsRoom` (15) rooms that live under the `VirtualRooms` root |
| Private rooms | private rooms | `PrivateRooms` (42) / `SearchArea.PrivateRooms` — rooms with `SettingsPrivate` that live under the `VirtualRooms` root, whatever their type; the root is shown only while the `e2e-rooms` app is on; archived ones stay in `SearchArea.Archive` |
| AI Agents | agents, inside them Knowledge / ChatOutputs | `AiAgents` (34) / `SearchArea.AiAgents`, rooms of type `AiRoom` (31) |
| AI rooms | Instructions / Knowledge rooms, AI projects | `AiHub` (41) / `SearchArea.AiHub`, rooms of type `InstructionsRoom` (38), `KnowledgeRoom` (39) and `AiProjectRoom` (40) — `DocSpaceHelper.IsAiHubRoom`; created and changed through the shared `files/rooms` routes (settings that do not apply to the type are refused with 400), never archived, no templates, deleted outright, outside the room count limit; a project's members are users only, invited as Content creator, the only role it offers; a group cannot be invited to it and it has no invitation link (`AvailableRoomAccesses` has only a `User` entry for `AiProjectRoom`); the listing of a project is empty and its file and folder counters are zero (`EntryManager.GetEntriesAsync` returns nothing for a project parent), yet a project holds the files of its chat threads: per thread an artifacts folder and an attachments folder directly in the project, and in the project itself the files uploaded or copied there and not sent yet (`files/rooms/{id}/threads/{threadId}/artifacts` and `/attachments` list them for the creator of the thread); the section root is always listed flat, rooms only, whatever `withSubFolders` says (`FileSecurity.GetVirtualRoomsAsync`), so no listing or search of it reaches inside a project, and a listing of the rooms root with `searchArea` Any leaves out every entry inside a project (`excludeAiProjectContent` of `FolderDao.GetRoomsAsync`, set by `GetVirtualRoomsForMeAsync`; the admin path does not read the AI rooms root for Any); an entry inside a project is visible to its `CreateBy` only, with Read, Download and Copy (and ReadHistory for a file) and nothing else, also against the project's owner and DocSpace admins (the rule in `FileSecurity.FilterEntryAsync` stands before the admin bypass), and no socket event, room upload notification or "new" mark about it goes to anyone else (`DocSpaceHelper.InAiProjectAsync`, `DocSpaceHelper.AnnouncesEntriesToMembers`); the hourly and daily activity digests list no event of a project or of an entry inside it, for anyone (`DocSpaceHelper.ReportsActivityInDigest`); a project and the entries inside it have no history: `FilesMessageService` writes no history references for them (so no `update-history` event), and the `log` and `log/report` endpoints answer 403 to everyone (`HistoryApiHelper`, `HistoryExport` in `FileSecurity.FilterEntryAsync`), while the portal audit trail still records the actions; their linked rooms are `files/rooms/{id}/sources`, skills `files/rooms/{id}/skills`, the chat model `files/rooms/{id}/model` (the project-scoped `Chat` AI assignment, readable by any member, never set by a member, never cleared); from the room side, `files/rooms/{id}/projects` lists the projects that use a Knowledge or an Instructions room (only the ones the caller may read) and the room object carries their number in `linkedProjectsCount` (every project, readable or not; the field is absent on other room types), with `update-folder` sent for the room when a link, an unlink or the deletion of a project changes it; a project member reads the linked rooms through a virtual `SubjectType.AiProject` record that `SecurityDao.GetSharesAsync` builds for AiHub entries only (never stored), so those rooms are not listed in the section |

The split of Rooms, Forms and Private rooms is one function, `SearchArea.MatchesRoom`: a `FillingFormsRoom`
belongs to Forms and a private room to Private rooms and nowhere else; every other room belongs to Rooms. Use it —
do not re-derive the split. `privacyFilter=Private` on the Rooms area means the Private rooms section.

## Checklist for any change that touches entries

1. **Listing and filters.** A new field, flag or filter must reach every branch of
   `GetEntriesAsync` it applies to — Shared with me, Recent, Favorites, rooms, trash — not only the
   regular folder query. If a section cannot support it, refuse it explicitly there (as the metadata
   filter is refused for Templates and Privacy); never return an unfiltered list with 200.
   An explicit `folderType` wins over a `searchArea` default; on `GET /files/{id}` the
   `searchArea` is not nullable, so "not sent" arrives as `Active` — test the request shape the
   client really sends, not only the one in the bug report.
2. **Virtual sections.** Recent, Favorites and Shared with me are views: an entry's `ParentId` there
   is its real parent, and the section root itself allows Read only. Never derive an entry's or a
   group's section from its members — store it (room groups keep it in `files_group.folder_type`).
3. **Trash and Archive.** In trash only Read, Delete and Move are allowed, and only in the caller's
   own trash; restore relies on the Origin tags (`OriginId`, `OriginRoomId`, `OriginTitle`), which
   exist only on trashed entries. Archive is read-mostly. A special folder type (e.g. the `.ai`
   folder) must not keep its behaviour after being moved to trash.
4. **Rooms vs Forms.** Anything per room — counters, used space, templates, groups, "new" marks,
   external-link landing — must show a form room in Forms and not in Rooms, and the other way
   round.
5. **Third-party storages.** Provider rooms and provider entries in Recent/Favorites go through
   separate DAOs (`GetProviderBasedRoomsAsync`, `GetThirdParty*ByTagAsync`, the thirdparty
   `IFileDao`/`IFolderDao` implementations). Apply the new filter or behaviour there too, or refuse
   it explicitly — a thirdparty DAO that accepts a filter and ignores it is a bug.
6. **Tags are shared rows.** Recent, Favorites and "new" marks are `files_tag` rows per user linked
   through `files_tag_link`. To clear a mark on one entry, remove the link; deleting the tag row
   clears that user's marks everywhere.

## Tests

Besides the home section, add a case in each section the change reaches
(`products/ASC.Files/Tests/Tests/`): `01_Files/Recent`, `02_Folders/Favorites`, `02_Folders/News` /
`03_Rooms/NewItems`, `04_Security` (Shared with me), `06_Operations/EmptyTrash` and the `Deletion`
folders, `03_Rooms/Archive`, `03_Rooms/Templates`, `03_Rooms/FormFilling` (the Forms split),
`03_Rooms/Groups`, `03_Rooms/Apps` (AI agents), `03_Rooms/ThirdParty`.
