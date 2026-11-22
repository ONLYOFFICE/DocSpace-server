# Design

## Context

See `proposal.md` for the motivation. The facts of the code that shape the approach:

- **An agent** is a folder of type `AiRoom`, a direct child of the `aiagents` root (4.0) or of the `virtualrooms`
  root (4.1, `FileStorageService.CreateRoomAsync`). It has exactly two system subfolders, `Knowledge` and
  `ChatOutputs`, titled from `FilesCommonResource` in the creator's language at creation
  (`FileStorageService.InternalCreateFolderAsync`). Agents are never archived.
- **A project** is a folder of type `AiProjectRoom` under the `aihub` root with no subfolders of its own. Its model
  is the `Chat` row of `ai_integration_assignments` with `entry_id` = project id; its threads are the rows of
  `ai_integration_threads` with that `entry_id`; its knowledge is the Knowledge rooms linked through
  `files_ai_project_room`. AiHub rooms are outside the room count limit.
- **Knowledge vectors** live in the OpenSearch index `files_embeddings`; a chunk carries `RoomId`, the id of the
  nearest ancestor of type `AiRoom` or `KnowledgeRoom` (`VectorizationTask`), and the search filters by it
  (`KnowledgeSearchEngine`). The store has no update-by-query, and a comment in `VectorStoreCollection` records
  that rebuilding a vector from its derived source fails on OpenSearch 3.5.
- **Threads of an agent** already use `ai_integration_threads` with `entry_id` = agent id; the old `ai_chats`
  tables are schema only. `LegacyAiProvidersMigrator` (a start-up task of the AI server) writes the agent's model
  as a `Chat` assignment with `entry_id` = agent id and finds agents by `FolderType.AiRoom`.
- **A restore** inserts the portal as a new row with new ids, sets `tenants_tenants.last_modified` to now
  (`TenantsModuleSpecifics.TryPrepareValue`, then `DbTenantService` on activation), and runs no EF migration. A
  transfer between regions sets it too (`TransferPortalTask.SaveTenant`). The column is indexed and
  `DbTenantService.GetTenantsAsync(from)` already reads by it. `files_folder` has no index on the folder type.
- `FilesDbContext` already maps `Threads`, `Assignments`, `McpServers`, `ToolPrefs` and `Attachments`, and
  `FolderDao.SaveThreadFolderAsync` is the precedent for saving a folder and a thread row in one transaction.

Specs relied on, unchanged: `ai-agents` "The AI rooms section", "Members of the AI rooms", "Users only in an AI
project", "Sources of an AI project", "Access through a project", "The model of an AI project", "Thread list of an
AI project", "Files of a project thread", "Privacy of the contents of a project".

## Goals / Non-Goals

**Goals:**

- No write to the vector index `files_embeddings` and no re-embedding; the full-text index gets only the usual index
  events of the project, the room and the moved knowledge files (D3).
- One short database transaction decides whether an agent is converted; every other step can be repeated.
- No periodic scan of `files_folder` across portals, no new table, no change in the backup service.
- A restored, transferred or dump-restored portal is converted without anyone marking it.

**Non-Goals:**

- Hiding the agents section, closing `createRoom` for the AI room type, removing the agent code.
- Carrying the stored prompt, the legacy MCP and per-user chat settings tables, Recent and "new" marks.
- Splitting Chat outputs by author.
- A progress view for administrators.

## Decisions

### D1. The agent's room becomes the Knowledge room; the project is a new room

The room keeps its id, so every chunk in `files_embeddings` (`RoomId` = that id) and every row of
`files_file_vectorization` (keyed by file id) stays valid untouched. The project gets a new id, which costs five
`UPDATE … SET entry_id` statements and a copy of the members.

Alternative: keep the id for the project and re-key the chunks. Rejected: there is no update-by-query in the store,
writing one would rebuild every document from its source on an engine where that is known to fail, and the fallback
is re-embedding every knowledge file (time and money).

Alternative: two paths, in-place project when there is no knowledge. Rejected: one path with a different last step
(change the type, or delete the room) is less code to keep right.

### D2. The folder type is the mark of "converted"

An agent is pending while its type is `AiRoom`. The type changes inside the transaction of D4, together with the
creation of the project. There is no per-agent table and no "completed" flag per portal: an agent brought back by a
restore is pending again by its type alone.

### D3. Steps for one agent

Run under the system account with the portal set, holding a distributed lock on the agent
(`ai_agent_migration_{tenant}_{room}`), in this order:

1. **Chat outputs out.** Every child folder of the agent other than its Knowledge folder is an outputs folder (the
   agent's root takes no folders or files from users). With content: set the type to `DEFAULT` (a folder of a
   system type cannot be moved), then `IFolderDao.MoveFolderAsync` with the new title `<agent title> (<its title>)`
   into the My documents of the owner (`GetFolderIDUserAsync(createIfNotExists: true, owner)`; the portal owner
   when the agent's owner is not found or is terminated). The move stores the title in its own update, so a run
   that stops between the two statements leaves the title as it was and the next run composes the same one.
   The update that gives the folder its new parent, the new tree rows of its subtree and two more updates, which the
   migrator passes to the move as a delegate run right before its commit, are committed in one transaction: one
   sets `modified_on` of the current and the first version of every file of the moved subtree to now (the periodic
   pass of the index finds a file by its first version and reads its current one; the versions between keep their
   dates), the other sets `modified_on` of every subfolder of that subtree. The move itself sends an index event for
   the moved folder only, and Files.Worker re-indexes only the folder an event names, so after the commit the
   migrator publishes an `UpdateFolders` event for every file and every subfolder of the subtree, each in its own
   try/catch with a warning log. A lost event leaves the entry to the periodic pass of the full-text index, which
   reads files and folders by `modified_on` and stores the new chain on its next run (every minute by default,
   `elastic:Period`); a run that stops after the commit has already moved those dates, so no later run has to
   find the folder again.
   Without content: `DeleteFolderAsync`. Matching "every other child" instead of "the folder of type
   `ChatOutputs`" makes a rerun pick up a folder whose type was already changed.
   **Knowledge made flat.** A Knowledge folder is flat: the API refuses to create a folder in it, its folders could
   not be used in the UI and their files were never vectorized, so a folder there holds nothing of value and got
   there only around those checks. The step is a safeguard that keeps such a folder from failing the conversion:
   the files of every folder inside the Knowledge folder are deleted by `IFileDao.DeleteFilesAsync` (rows, storage,
   vectors, index), then each of its folders by `DeleteFolderAsync`, with an information line in the log. The files
   directly in the Knowledge folder stay and go to the room in step 3.
2. **Under the AiHub root.** If the agent's parent is not the `aihub` root, `MoveFolderAsync` there.
3. **Conversion transaction** (D4).
4. **After the commit**, each in its own try/catch with a warning log: copy the logo to the project and set its
   flag; `FolderIndexIntegrationEvent` for the project and the room, a `Delete` index event for the removed Knowledge
   folder, and an `UpdateFolders` index event for each moved knowledge file, each event in its own try/catch so that
   one failed publish does not skip the rest; socket events (`create-folder` for the project and for the Knowledge room); for an agent without
   knowledge files `DeleteFolderAsync` of the room with no socket event (`create-folder` for the room when the deletion
   fails), and once the room is deleted the logo
   files of an agent with a logo (`RoomLogoManager.DeleteLogoFilesAsync`), after the copy to the project.

`MoveFolderAsync` and `DeleteFolderAsync` are reused as they are: the folder tree, the counters and the used space
are the logic not worth a second copy.

### D4. The conversion transaction

A new `IFolderDao<int>` operation (third-party implementations throw `NotSupportedException`, as
`SaveThreadFolderAsync` does). In one transaction of `FilesDbContext`:

1. Change the room's type to `KnowledgeRoom`, and its title (D6) when its Knowledge folder holds files, with one
   `UPDATE … WHERE folder_type = AiRoom`. No row changed: roll back and report "already converted". The Knowledge
   folder is flat by then (step 1 of D3), so only its own files are moved. Then clear the room settings a Knowledge
   room refuses (lifetime, watermark, download restriction, indexing, privacy, the two form settings) with one
   update of its `files_room_settings` row.
2. Insert the project through `InternalSaveFolderToDbAsync` (title, owner, creation date of the agent) and its
   `files_room_settings` row with the colour and the cover.
3. Re-key `entry_id` room → project in `ai_integration_threads`, `_assignments`, `_tool_preferences`,
   `_mcp_servers` and `_preferences` (`DbPreference` is added to `FilesDbContext`).
4. Members of the project: read the room's user shares and the members of its invited groups, leave out Guests
   and the owner, map the roles (D5) and add the rows with one `SaveChanges`.
5. Shares of the room: one `ExecuteDelete` of every row that is not a user or a group with Room manager or
   Content creator (an invitation link goes whatever role it invites to), and one of the shares of the
   knowledge files.
6. Tag links of the room: re-point Custom and Pin links to the project, delete Recent and "new" links. A
   Favorite link is left on the room.
7. With knowledge files: move them to the room with `MoveAiAgentKnowledgeFilesAsync`, one update that moves every
   version row and sets `modified_on` of the current and the first one to now (the versions between keep their dates), so the periodic pass of the full-text index, which reads files by
   `modified_on`, picks them up when their index event is lost; delete the Knowledge folder row with its tree rows and the rows
   `DeleteFolderAsync` removes for a folder, lower the folder count of the room and its ancestors by one, insert
   the `files_ai_project_room` link. Without them: nothing more, the room is no longer pending since step 1;
   step 4 of D3 deletes it.
8. Commit.

Every update is an `ExecuteUpdate` / `ExecuteDelete` placed in `Core/EF/Queries` by the rules of
`ef-compiled-queries.md` (compiled where it compiles, "Not precompiled on purpose" for `ExecuteUpdate`).

What is batch and what is DAO code:

| Work | How |
|---|---|
| threads, model, preferences, MCP entries | 5 `ExecuteUpdate` |
| members of the project | 2 reads, 1 `SaveChanges` |
| shares of the room and of its files | 2 `ExecuteDelete` |
| tags, pins | 1 `ExecuteUpdate`, 1 `ExecuteDelete` |
| knowledge files into the room | 1 `ExecuteUpdate` |
| project row, tree, root counters | `InternalSaveFolderToDbAsync` |
| Chat outputs, the move under the AiHub root, deletion of an empty room | `MoveFolderAsync`, `DeleteFolderAsync` |

### D5. Access rules

| On the agent | In the project | In the Knowledge room |
|---|---|---|
| owner (`CreateBy`) | owner | owner |
| user, Room manager | Room manager | Room manager |
| user, Content creator | Content creator | Content creator |
| user, Viewer | Content creator | removed |
| group, Content creator | each member: Content creator | kept |
| group, Viewer | each member: Content creator | removed |
| Guest, Content creator | not carried | Content creator |
| Guest, Viewer | not carried | removed |
| invitation link | not carried | removed |
| external link of a knowledge file | — | removed |

A user's own role wins over the role that comes through a group. A group member who is a Guest or the owner gets no
row. Group membership is read once, inside the transaction. The rows written are ordinary `files_security` rows, so
"Access through a project" gives every project member Read on the linked room with no further work.

### D6. Titles

`<agent title> (<title of the system folder>)`, with the stored title of that agent's own Knowledge or Chat outputs
folder: no new resource and no translation. When the result is longer than the limit the room rename applies, the
agent title is cut and the suffix kept. The function is pure and unit tested with its boundaries.

### D7. The pass: a worker with a window over `last_modified`

`AiAgentsMigrationService`, an `ActivePassiveBackgroundService` in `products/ASC.AI/Worker`. State: one settings
record of the installation (`SettingsManager.LoadForDefaultTenantAsync` / `SaveForDefaultTenantAsync`):
`LastPassStart`, `Attempts` (portal id → number of passes that ended with a failed agent), and the progress of a
pass that has not completed: `PendingPassStart` (the start of its first part) and `Cursor` (the last portal it
checked).

One pass:

1. `start = PendingPassStart ?? UtcNow`.
2. Candidates: the active portals with `last_modified >= LastPassStart - overlap` (a new compiled query in
   `TenantDbContext`, ids only), plus the portals in `Attempts` below the limit. With no `LastPassStart`: every
   active portal, paged by id. With a `PendingPassStart`, only the candidates after `Cursor`.
3. For each candidate: scope, set the portal, authenticate the system account; read the ids of its `aiagents`,
   `virtualrooms` and `aihub` roots without creating them and the ids of their direct children of type `AiRoom`
   (index `(tenant_id, parent_id)`). None, or the portal is no longer active: drop the portal from `Attempts`,
   next.
4. Run the legacy providers migration for the portal (D8), then D3 for each agent, each in its own try/catch.
5. Any agent failed: `Attempts[portal] += 1`; at the limit, an error log naming the portal, and the portal is
   skipped from then on. All converted: drop it from `Attempts`. The lock of D8 not acquired: keep the portal in
   `Attempts` with its count unchanged (0 when absent).
6. Every 20 portals, and once more when the pass ends by an exception or a cancellation, save `PendingPassStart =
   start`, `Cursor` = the last portal checked and `Attempts`, keeping `LastPassStart`. The next pass goes on after
   `Cursor` in the same window; a process killed without a cancellation checks again at most the 19 portals after
   the last save.
7. Save `LastPassStart = start` and `Attempts`, with no `PendingPassStart` and `Cursor`. A pass resumed after a
   stop thus opens the next window at the start of its first part, so a portal before `Cursor` changed while the pass
   was stopped is in that window.

`overlap` covers clock differences between the hosts that write `last_modified`, the time between a stamp and its
commit, and the activation stamp of a restore cut to whole seconds, so it is at least a minute; a portal without agents checked
twice costs its four short reads again (see Risks). A portal in `Restoring` is not active and is not a candidate;
its activation writes `last_modified` again, which puts it into the window of the next pass.

Alternatives rejected: a query for portals that hold a room of type `AiRoom` on every pass (a scan of
`files_folder`, which has no index on the type); a per-portal marker row written by a data migration and by the
restore (every path that brings old data in — restore, dump restore, transfer — has to remember to write it); a
start-up task as `LegacyAiProvidersMigrationStartupTask` (runs only when the service starts).

A dump restore replaces the settings record together with the data, so `LastPassStart` goes back to the moment of
the dump or disappears, and the pass covers every portal changed since, or all of them.

### D8. The legacy providers migration runs first

`LegacyAiProvidersMigrator` binds a model to an agent by `FolderType.AiRoom`; run after the conversion it finds
nothing and the project is left without the agent's model. It matters on a self-hosted installation only (the
migrator does nothing where the AI gateway is configured), in two cases: an upgrade straight from 4.0, which
brings both migrations at once, and a restore of an archive made before the profiles. The migrator is not run a
second time: `MigrateAsync` works on one portal and returns at once when its completion flag is set, so the worker
is a second caller that makes sure it has run. The migrator, its context, queries, mapper and logger
move from `products/ASC.AI/Server/Migration` to `products/ASC.AI/Core` (the worker references Core only); the
start-up task stays in the server. The worker calls it for a portal under the start-up task's lock
(`ai_legacy_providers_migration`) and under the same conditions (no AI gateway configured, MySQL): both callers
take them from one method, `LegacyAiProvidersMigrationGate.TryEnterAsync`. Lock not acquired: the portal is
skipped for this pass and kept in `Attempts` with its count unchanged (0 when it had none), so the next pass tries
it again whether or not it changed. The migrator throws: a failed pass.

### D9. The route

`POST internal/ai/agents/migration` in a controller of the AI server without `[AiFeature]`, restricted to the
portal owner and DocSpace admins. It is the entry point of the integration tests (`tests.md`: "restrict the pass to
that portal through a test entry point"), not a product feature: no user interface calls it and it does not read
or change the state of the pass. It runs steps 3 and 4 of D7 for the caller's portal in the request
(`AiAgentsPortalMigration.RunAsync` in `ASC.AI.Core`, the code the pass runs per portal) and answers
`{ migrated, failed }` built in the controller (`ApiModels`, a response DTO; no request body). The per-agent lock
makes a call that overlaps a pass safe: the second one finds the room already converted. Where the pass skips a
portal because the lock of the legacy providers migration is held elsewhere, the route answers 409 and converts
nothing: zeros would read as "no agents". No spec scenario states it: a test cannot hold that lock on the stand.

### D10. Configuration

Read once in the service constructor through `ConfigurationValueExtensions`:

| Key | Default | Bounds | Meaning |
|---|---|---|---|
| `ai:agentsMigration:enabled` | `true` | — | `false`: the pass does nothing, logged once at start; the route still works |
| `ai:agentsMigration:period` | `0.00:05:00` | 5 s – 1 day | time between passes |
| `ai:agentsMigration:overlap` | `0.00:05:00` | 1 minute – 1 hour | how far before `LastPassStart` the window opens; at least a minute to cover the clock difference of the hosts, the time between the stamp and the commit, and the restore stamp cut to whole seconds |
| `ai:agentsMigration:maxAttempts` | `10` | 1 – 1000 | failed passes before a portal is left alone |

The `integration-test` profile sets `enabled` to `false`: a test builds an agent in several calls and a pass in the
middle would convert a half-built one. The tests start the conversion through the route.

### D11. What a step leaves behind

| Step | It throws | The process stops right after it | Another instance takes the agent |
|---|---|---|---|
| 1 outputs type changed | agent pending; next run moves the folder as "other child" under the same title | same | waits for the lock, then finds the folder as "other child" |
| 1 outputs moved or deleted | agent pending, without outputs; next run skips step 1 | same | same |
| 1 files inside Knowledge folders deleted | agent pending; next run finds the folders without files and deletes them | same | waits for the lock, then the same |
| 1 Knowledge folders deleted | agent pending with a flat Knowledge folder; next run skips this part of step 1 | same | same |
| 2 moved under AiHub | agent pending under the AiHub root; next run skips step 2 | same | same |
| 3 transaction | rolled back, agent pending and whole | commit seen or not: either pending or converted, nothing between | re-reads the type in the transaction, rolls back if converted |
| 4 logo, events | warning; the project has no logo, the index catches up on its periodic pass | same | not repeated: the room is no longer pending |
| 4 delete of an empty room | warning; an empty Knowledge room linked to nothing stays, visible to its owner | same | not repeated |
| 4 logo files of the deleted room | warning; the logo files stay in the storage, referenced by no folder | same | not repeated |

The lock is released on every exit (`await using`). Nothing is marked before the transaction, so there is no mark to
release.

## Risks / Trade-offs

- [`MoveFolderAsync` of a room from the `aiagents` or `virtualrooms` root to the `aihub` root adds the room's
  size to a user's used space, as it does for a folder leaving a room: by the code the moved room counts as
  "inside a room" and the root it goes to does not] → the existing Archive exclusion in
  `FolderDao.MoveFolderCoreAsync` is extended to the AiHub root; a test asserts that no user's used space changes
  in step 2.
- [The pass depends on restore and transfer writing `last_modified`] → a unit test in
  `ASC.Data.Backup.Core.Tests` pins `TenantsModuleSpecifics`. No integration test restores an archive with an
  agent: the backup suite never starts a real restore (`RestoreGuard`), and that guard stays closed. The restore
  scenario of the spec rests on that unit test, on the `entry_id` relations of `AiModuleSpecifics` and on the
  conversion tests of the AI suite.
- [The periodic loop itself is not run by an integration suite, the pass being off in the test profile] → the
  window, the candidate list and the attempt count are pure functions with unit tests; the per-portal code is the
  same one the route runs. The wiring between them is read in review.
- [A crash between the commit and the deletion of an empty room leaves an empty, unlinked Knowledge room] →
  accepted; it holds no data and its owner can delete it.
- [The owner's personal quota can be exceeded by the outputs folder] → accepted by the product decision; the
  alternative is a conversion that fails on a full quota.
- [The first pass after the release visits every active portal] → per portal without agents, its
  record and four short indexed reads: the ids of the `aiagents`, `virtualrooms` and `aihub` roots in
  `files_bunch_objects` and the agents under them in `files_folder` (`AiAgentMigrator.FindAgentsAsync`); the portals
  are paged, on one instance; no scan of `files_folder`.
- [A portal left alone after the attempt limit hides its agents from users] → the error log names the portal; no later
  pass resumes it, and the internal route of D9 still converts its agents.
- [Threads of a Guest move to the project, where the Guest is no member] → they stay stored and unreachable, as
  decided; no deletion of user data in the conversion.
- [Moving the legacy migrator between projects] → no behaviour change intended; the existing start-up path is
  re-run by the AI suite.

## Migration Plan

1. Deploy. Two schema migrations (MySQL SaaS), each adding only indexes `(tenant_id, entry_id)`:
   `MigrationContext_Upgrade110` on `ai_integration_threads`, `MigrationContext_Upgrade111` on
   `ai_integration_assignments`, `_tool_preferences`, `_mcp_servers` and `_preferences`. The move of `entry_id` in D4
   filters all five tables by these columns, and `FolderDao.DeleteFolderAsync` deletes the rows of a deleted folder from
   all of them but `_preferences` the same way; without the index each statement read the whole portal's range of the
   table, inside the transaction. The start-up task of the AI server and the worker may start in any order: the
   worker runs the legacy providers migration for a portal itself before converting it (D8).
2. The first pass walks all active portals; later passes only the changed ones.
3. Rollback: set `ai:agentsMigration:enabled` to `false`. Converted agents are not converted back; a portal
   restored from an archive made before the conversion holds agents again and waits for the pass.

## Open Questions

None. Both questions raised while designing are closed by the code:

- The length limit of a room title is `Global.MaxTitle`, 170 characters, the default of
  `AiAgentMigrationTitle.Compose`, pinned by `AiAgentMigrationTitleTests.Compose_DefaultLimit_IsTheLimitOfAFolderTitle`.
- A Guest reading their own thread after it moved to the project gets 403: the thread routes refuse a Guest by the
  account type, and the thread stays stored in the project
  (`AgentMigrationThreadsTests.Convert_ThreadOfAGuestOfTheAgent_StaysStoredAndAnswers403ToTheGuest`).
