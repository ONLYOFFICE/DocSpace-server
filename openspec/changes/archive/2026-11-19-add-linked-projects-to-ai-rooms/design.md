# Design

## Context

See proposal.md for the motivation. What shapes the approach:

- A link is a row of `files_ai_project_room` (tenant, project, room), with the primary key in that order and a
  second index on `(tenant_id, room_id)`. A project holds at most 20 rooms of each kind; the number of projects
  per room has no limit.
- The rows of a deleted project or room are removed in the transaction that deletes the folder
  (`FolderDao.DeleteFolderAsync`), so a row always names a live project and a live room.
- Which projects a caller may see is already decided in SQL for the pinned projects
  (`AiProjectsWithOwnRecordAsync`): a DocSpace admin, the creator, or a share record of the caller's own that is
  not Restrict. A group record never counts: a group cannot be invited to a project.
- The listing of a section builds its room objects in `FolderContentDtoHelper.GetAsync`, in parallel, through
  `FolderDtoHelper.GetAsync`. What a page needs once is loaded before and handed in (`currentUserRecords`); the
  helper loads it itself when a single room is built.
- A socket event of a folder carries its id, parent, title and room type only; the client re-reads the entry.

Existing behaviour the design relies on, capability `ai-agents`:

- "Sources of an AI project": the links, their limits, and that a deleted room leaves the sources.
- "Access through a project": a member of a project reads its linked rooms, so such a member is a reader of the
  room and may call the new operation.
- "Who finds a pinned project in the root folders": the rule of who sees a project, reused as it is.
- "The AI rooms section": the rooms of the section are deleted outright, so no link outlives its ends in an
  archive or a trash.
- "Listing the files of a thread": the bounds of `count` and `startIndex`, reused for the new listing.

"Members of the AI rooms", the one requirement under "Breaks current behaviour", changes in its text only. The
three reads it names keep their code and their answers, for stored projects as for new ones; nothing stored is
read differently.

## Goals / Non-Goals

**Goals:**

- The number costs the listing of the section one indexed query per page, and nothing for a page without
  Knowledge or Instructions rooms.
- The list of projects never costs more than the projects of one room, and its rights filter runs in the query.
- A room object without the number always means "not a Knowledge or Instructions room", on every route that
  answers the full room object. The short room object of the new-items listing (`FolderDtoHelper.GetShortAsync`,
  which also leaves out the tags and the quota) does not carry the number.

**Non-Goals:**

- A list of projects inside the room object.
- Sorting or filtering the section by the number.
- An event for a renamed project or a changed project logo: the number does not change, and the project's own
  event is sent as today.

## Decisions

### 1. The number on the room, the list in its own operation

The room object carries a number; the projects come from `getRoomProjects`, paged.

Alternative: a list in the room object, cut to N projects. It needs the first N projects of every room of the
page. Cut in memory, a DocSpace admin's request reads every link of the page to keep a few. Cut in SQL, it is a
window function over a join, which no query of the repository uses and which would have to translate as a
precompiled query on both database providers. The listing shows a number only, so the cost would be paid on
every page for data the page does not show.

### 2. The number counts link rows, without a join

One precompiled query groups the rows of `files_ai_project_room` by room for the room ids of the page and
answers `(room, count)`; it is served by the index on `(tenant_id, room_id)`. A room without a row gets 0.

Alternative: join the projects and count the live ones. The delete path leaves no row behind (Context), so the
join would buy nothing on the hottest read path.

The number is not filtered by the caller's rights: it tells how many projects depend on the room, which is what
the reader of the room needs before changing or deleting it, and it names no project.

### 3. The number is filled where the room object is built

`FolderDtoHelper.GetAsync` fills `LinkedProjectsCount` for an internal room of type Knowledge or Instructions
under the AI rooms root. It takes one more optional argument, the numbers of the page; when it is absent the
helper reads the number of its one room. `FolderContentDtoHelper.GetAsync` loads the numbers once before it
builds the room objects, when the listed folder is the AI rooms root and its page holds such a room, and hands
them in, as it does with `currentUserRecords`. The dictionary is only read after that, so the parallel build is
safe. Every other listing pays one comparison. A listing inside such a room builds the room as its current
folder, and the helper reads that one number itself. Two listings can answer such a room among their entries
without the AI rooms root as the listed folder: the Rooms root asked with `searchArea=AiHub`
(`GET api/2.0/files/{id}` keeps the caller's area for that root), and Favorites, after the room was marked with
`POST api/2.0/files/favorites`, which takes any folder the caller reads. There the helper also reads the number
of each such room itself: the number is right, at one read per room instead of one per page.

Alternatives:

- A join in the query of the rooms: the query is already the heaviest of the section and would grow for every
  room type.
- A property on `Folder`, filled next to the tags: the number is response data and no code but the response
  reads it.
- A pass over the finished objects, as `SetMetadataAsync`: every action that answers one room (`getRoomInfo`,
  `createRoom`, `updateRoom` and the other room actions) would need its own call, and a missed call answers a
  room without the field, which the contract reads as "not a Knowledge or Instructions room".

### 4. The field

`int? LinkedProjectsCount` on `FolderDto<T>`, written only when it has a value, like the other optional room
fields. Its text follows `.claude/rules/openapi-dto-docs.md` §3.4: in which rooms it is present, what 0 means,
and that it counts projects the caller may not read.

### 5. The operation

`GET api/2.0/files/rooms/{id}/projects` in the AI rooms controller, under its `[AiFeature]` gate and its tag.

- Request model `GetAiRoomProjectsRequestDto`: `id` from the route, `count` with `[Range(1, ApiContext.MaxCount)]`
  and `ApiContext.DefaultCount` as its default, `startIndex` with `[Range(0, int.MaxValue)]`, the shape of
  `GetAiThreadFilesRequestDto` without sorting and text.
- Response `IEnumerable<AiRoomProjectDto>` (`Id`, `Title`, `Logo`) with the count and the total set on
  `ApiContext`, a Mapperly mapper next to the DTO that takes the logo from `RoomLogoManager`, as
  `AiProjectSourcesDtoMapper` does. The room of a source and the project of a room are different entities, so
  `AiProjectSourceDto` is not reused.
- `AiHubService` checks in the order of its `GetSkillsAsync(int roomId)`: the folder is a Knowledge or
  Instructions room under the AI rooms root, else 404; the caller may read it, else 403.

### 6. Two precompiled queries, the rights inside

One query answers the page, one the total; both start from the links of the room and join the project folder
(type AI project, parent the AI rooms root) with the predicate of `AiProjectsWithOwnRecordAsync`:
`isAdmin || CreateBy == userId || own record that is not Restrict`. The page is ordered by title and id in the
database, then `Skip`/`Take`; it selects what `RoomLogoManager.GetLogoAsync` needs (the folder with its settings).

No `FileSecurity` call per project: the predicate is the one the pinned projects use and is pinned by its own
tests; here it gets a test per caller (below). The sort handles the projects of one room only, and the total
has to visit the same rows anyway.

Alternative: read the ids of the room's projects, pass them to `GetAiProjectsVisibleToAsync`, sort and page in
memory. Every page would load every project of the room.

Settled: a room of the AI rooms section cannot be end-to-end encrypted, so the predicate needs no case for a
private project. `createRoom` for an AI project with `private: true` answers 400 on a stand where the
`e2e-rooms` app is on and the caller holds encryption keys: the same test first creates a private Custom room
(`AiHubRoomSettingsTests.CreateProject_Private_Returns400`). The refusal is the first check of
`FileStorageService.CreateRoomAsync` for every type of the section, and the flag is written at creation only;
`updateRoom` does not carry it. Had a project been private, `FileSecurity.FilterEntryAsync` would have kept it
from a DocSpace admin outside it: in the branch of the AI rooms root the admin bypass is skipped for a room
with the flag, and such an admin falls through to the share records.

### 7. Events

- **Link**: `LinkSourcesAsync` already holds the newly linked rooms; after the audit record and the project's
  event it sends `UpdateFolderAsync` for each of them.
- **Unlink**: `UnlinkSourcesAsync` already reads the folders of the unlinked rooms for the audit text; it keeps
  them and sends the event for each.
- **Project deleted**: the delete operation of a room reads the ids of the project's rooms
  (`AiProjectRoomDao.GetRoomIdsAsync`, at most 40 rows by the primary key) before the folder is deleted, and
  sends the event for those rooms after it. The other callers of `FolderDao.DeleteFolderAsync` are checked for
  one that can delete a project.

The sends come last, in their own `try/catch` with a `[LoggerMessage]` warning: a failed send fails neither the
call nor the sends after it (`failure-paths.md` §3). `SocketManager` resolves the recipients from the room
after the change, so a member who read the room only through the unlinked or deleted project is not told about
a room they no longer read. The event exists on both halves of the socket service; nothing is added there.

### 8. The requirement about a DocSpace admin

The delta rewrites the last sentence of "Members of the AI rooms" and adds a scenario. The rule that a project
takes users only (no group, no Guest) moves from it into "Users only in an AI project", with its two scenarios,
so that neither requirement reaches 500 characters.
`AiRoomOutsiderAccessTests.ProjectReads_DocSpaceAdminOutsideTheProject_Return403` becomes a test of 200 without
the `Bug` category; the citations of that file in `verification.md` follow its lines.

### 9. Tests before the SDK knows the change

The tests were written before the SDK knew the change: they called `getRoomProjects` over raw HTTP and read
`linkedProjectsCount` from the raw JSON of the room. Once the OpenAPI documents and the C# SDK package were
regenerated on the branch, they moved to the SDK, with the rooms created and linked through it as well. One case
stays over raw HTTP: an empty `count`, which the typed client cannot send.

## Who learns what

The rule: whoever may read a Knowledge or Instructions room learns how many projects use it; which projects
they are, only for the projects the caller may read.

Sources of the result:

| Source | Restrict is no membership | A group record is no membership |
|---|---|---|
| The number on the room | n/a: counts rows, names nothing | n/a |
| The page query | the joined record excludes Restrict | the record is matched by the caller's own id |
| The total query | same predicate as the page | same predicate as the page |

Output channels by caller:

| Channel | DocSpace admin | Has a role in the room, in no project | Member of one project (with or without a role in the room) | Cannot read the room |
|---|---|---|---|---|
| `linkedProjectsCount` | all projects | all projects | all projects | room not returned |
| Page of `getRoomProjects` | all projects | empty | own projects | 403 |
| Total of `getRoomProjects` | all projects | 0 | own projects | 403 |
| Order, paging | over the listed projects only | over nothing | over the listed projects only | 403 |
| 404 against 403 | 404 for an id that is no such room | same | same | 403 for such a room, 404 otherwise, as `getRoomSkills` answers today |
| Event | told; payload names the room only | told | told while the room is still read | not told |

A Guest never holds a role in a project ("Members of the AI rooms"), so a Guest who reads such a room is the
second column. A role held in the room through a group is a role in the room and changes no column: only a
record of the caller's own on a project lists that project. Each cell of the page and the total gets a test
with the weakest caller, a User.

## Risks / Trade-offs

- [The rule of who reads a project lives in SQL here and in `FileSecurity` for the reads of a project] → the
  predicate is copied from the pinned projects, and the tests of "Who is listed among the projects of a room"
  cover a DocSpace admin, the creator, a member, and a reader outside the projects.
- [The number and the total can differ for one caller] → intended and stated in the texts of the field and of
  the operation; the difference is the number of projects the caller may not read.
- [The order is the database's order of titles, not the culture-aware order `getProjectSources` uses] → the
  requirement promises "by title" with titles that sort the same in both; the remarks of the operation do not
  promise a culture.
- [A project deleted on a path other than the delete operation would leave the rooms' numbers stale on open
  clients] → the callers of `FolderDao.DeleteFolderAsync` are checked in the task that adds the event.
- [The scenario of an empty `count` is written before its test] → if the server answers otherwise, the scenario
  or the binding is changed before the branch is handed over (`claims.md` §2).
- [Up to 40 events for one link call] → each is a small message; the recipients are resolved per room, on a
  write that a manager makes by hand.

## Migration Plan

No migration and no stored data to convert. After the merge the OpenAPI documents and the SDKs are regenerated
(new field, new operation). The frontend subscribes to nothing new: the event is the existing `update-folder`
in the room of the AI rooms section.
