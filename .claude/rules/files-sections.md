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
| AI Agents | agents, inside them Knowledge / ChatOutputs | `AiAgents` (34) / `SearchArea.AiAgents`, rooms of type `AiRoom` (31) |

The Rooms/Forms split is one function, `SearchArea.MatchesRoomType`: a `FillingFormsRoom` belongs to
Forms and nowhere else; every other room type belongs to Rooms. Use it — do not re-derive the split.

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
