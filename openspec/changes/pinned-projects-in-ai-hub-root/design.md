# Design

## Context

`getRootFolders` (`FoldersController.GetRootFolders`) builds each section with
`FolderContentDtoHelper.GetAsync` → `FileStorageService.GetFolderItemsAsync` → `EntryManager.GetEntriesAsync`, the
same path that lists any folder. For the AI rooms root that path takes the rooms branch
(`EntryManager.GetEntriesAsync`, the `VirtualRooms or … AiHub …` branch), and `FileSecurity.GetVirtualRoomsAsync`:

- reads every folder share record of the caller portal-wide (`securityDao.GetSharesAsync(subjects)`);
- reads every room of the section with no paging in SQL (`FolderDao.GetRoomsAsync`: the rooms whose id is among
  those records, or that the caller created under the root);
- sets custom tags and the `Pinned` flag on all of them (`SetTagsAsync`, `SetPinAsync`).

`EntryManager` then sorts everything in memory, pinned first, and takes `count`. A DocSpace admin goes through
`GetAllVirtualRoomsAsync` instead, which lists every room of the section without looking at shares.

The section listing does not check rights per room: visibility is the join with the caller's share records, and
the listing's filter sets the found record on the room (`entry.ShareRecord`, `entry.Access`) for the DTO.

Pins are `files_tag` rows of type `Pin` owned by the account, linked through `files_tag_link`, whose `entry_id` is
a string. The existing tag query by owner (`TagDao.GetTagsAsync(owner, default, TagType.Pin)`, compiled as
`TagsByOwnerAsync`) returns them newest link first; `pinRoom` uses it to count the pin limit. A project is
`FolderType.AiProjectRoom` directly under the AI rooms root; its members are users only and its creator holds no
share record on it.

## Goals / Non-Goals

**Goals:**

- The AI rooms section of `getRootFolders` reads the caller's pinned projects in two queries by keys.
- The projects carry the same `access`, `pinned`, `tags`, `new` and favourite values as in the listing of the section.

**Non-Goals:**

- The per-entry share preload of `FileSecurity.SetSecurity` (`8d38bf57ee`): each listed project still costs one
  share query when its DTO is built, as every room of any listing does.
- The other sections of `getRootFolders`.
- Removing pins of projects the caller can no longer see, or counting them out of the pin limit.

## Decisions

**A mode of the listing, not a second endpoint or a post-filter.** `GetRootFolders` asks for the AI rooms section
with a flag (`pinnedAiProjects`) that `FolderContentDtoHelper.GetAsync` passes through
`FileStorageService.GetFolderItemsAsync` to `EntryManager.GetEntriesAsync`. `current`, `pathParts`, `new` of the
section and the rights on the root keep coming from the code that computes them today. Alternative: build a
`DataWrapper` by hand in the controller — rejected, it would copy the breadcrumbs, the root's rights and the new
counter. Alternative: filter the full list in the controller — rejected, it keeps the full read the change removes.

**Where the flag acts.** In the rooms branch of `EntryManager.GetEntriesAsync`, when the flag is set and the parent
is the AI rooms root, the entries come from a new `FileSecurity.GetPinnedAiProjectsAsync()` instead of
`GetVirtualRoomsAsync`; `total` is their number; the in-memory sort and `Skip`/`Take` (both the ordinary path and
the `SortedBy.New` path) are skipped, so the pin order stands; `SetTagsNewAsync` and `SetIsFavoriteFoldersAsync` run
on these entries as on any page. The request's filters, search text and paging are not passed for this section;
`FolderContentDtoHelper` reports `startIndex` 0. The flag is ignored for any other parent, so no other caller of
these methods changes.

**Two queries, both by keys.**

1. The caller's pins: the existing `TagDao.GetTagsAsync(userId, default, TagType.Pin)` (`TagsByOwnerAsync`). It
   returns every pin of the account (rooms, AI rooms and the rooms of the section, at most 10 of each kind under the
   pin limits), newest first. The internal ids are parsed from `entry_id`; the order is kept.
2. The projects: a new precompiled query (`[PreCompileQuery]` in `FolderQueries.cs`, called from `FolderDao`) over
   `files_folder` by primary key (`id in ids`), with `folder_type` = AI project, `parent_id` = the AI rooms root,
   and a left join to the caller's own `files_security` record on it (`internal_entry_id`, `entry_type` folder,
   `subject` = the caller). It keeps a row when `isAdmin`, or `create_by` = the caller, or the record exists, and
   returns the folder in the shape the folder mapper reads together with that record.

Alternative: one query joining pins to folders — rejected: `files_tag_link.entry_id` is a string, so the join is
`CAST(f.id AS CHAR) = l.entry_id` and cannot use the primary key of `files_folder`; the plan would scan the projects
of the portal (from the folders) or the children of the root (from the pins), and its cost would follow the size of
the portal on MySQL and PostgreSQL alike. Alternative: read the folders, then check `CanRead` per project —
rejected: one query per project, which the join replaces.

**Visibility equals the listing of the section, without groups.** The condition mirrors what the listing shows:
a DocSpace admin sees every project (as `GetAllVirtualRoomsAsync`, which applies no privacy filter to the AI rooms
area), anyone else the
projects they created or hold a record on. Only the caller's own record counts (`subject` = caller): a project
cannot take a group since `d2e3fe02c9`, so a group record exists only on projects shared before that, and those
are not listed. This keeps the open defect of "Members of the AI rooms" (an admin outside a project reads it) as
the listing has it; fixing it is a change of its own and would change both places.

**What is set on each project.** `ShareRecord` and `Access` from the joined record, as the listing's filter does,
so `access` and `inRoom` of the DTO are what `getRoomsFolder` reports; `Pinned` = true; custom `Tags` in one batch
(`tagDao.GetTagsAsync([TagType.Custom], projects)`, as `SetTagsAsync` does), so the DTO does not read them per
room. The creator has no record, so `ShareRecord` stays null and the rights of their project are computed through
the usual path in `FilterEntryAsync`, as in the listing.

**Methods on the DAO interface.** The query method goes on `IFolderDao<T>` next to `GetRoomsAsync`; the
third-party folder DAOs return nothing, since a project is never a provider room. `GetPinnedAiProjectsAsync` reads
the AI rooms root id from `GlobalFolder` (cached) and works on `int` ids only.

**No new socket event** (decided with the user). Pin and unpin already send `update-folder` for the room.

## Risks / Trade-offs

- [The client relied on the first `count` rooms of the section in `@root`] → it does not: `getFoldersTree` reads
  `current`, `new` and `pathParts` only.
- [A pin of a project the caller lost stays and still counts against the pin limit of the section] → kept as
  today; the project reappears in the list when access returns (spec scenario "Member loses access and regains it").
- [Each listed project costs one share query in its DTO (the preload)] → bounded by the pin limit (10 projects).
- [A group record on a project from before `d2e3fe02c9` is no longer enough to see it in this list] → such records
  cannot be created any more; the listing of the section still shows those projects. No automatic test: the API
  cannot create the record.
- [`sortBy` of the request is still saved as the account's default order when the AI rooms section is built] →
  unchanged behaviour of `getRootFolders` for every section; the section itself ignores it.

## Migration Plan

No schema change and no data migration. Rollback is a code revert; pins are not touched.
