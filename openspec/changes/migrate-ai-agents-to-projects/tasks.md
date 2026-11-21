# Tasks

Each group is one reviewable step: it builds with no new warnings, lands its own tests, and ends with a line saying
what can be seen after it. Nothing converts agents by itself before group 8; until then only the test route does.

## 1. Legacy providers migration reachable from the worker

- [x] 1.1 Move `LegacyAiProvidersMigrator`, `LegacyAiDbContext`, `LegacyAiQueries`, `LegacyProviderMapper` and `LegacyAiProvidersMigrationLogger` from `products/ASC.AI/Server/Migration` to `products/ASC.AI/Core/Migration`, keeping the namespace; the start-up task stays in the server. Put the two conditions of the start-up task (no AI gateway configured, MySQL) and the lock key into one method of the migrator's side that both callers use (design D8). Verified by `dotnet build` of `ASC.AI.Core`, `ASC.AI` and `ASC.AI.Worker` with `--no-dependencies --no-incremental` and no new warnings, `dotnet format style --verify-no-changes` on the moved files, and LSP `findReferences` on `LegacyAiProvidersMigrator` showing no reference left to the old location
- [ ] 1.2 Register `LegacyAiDbContext` and the migrator in `AiWorkerServiceExtensions`. Verified by the worker starting in a test run with no `WarmupBaseDbContextStartupTask` warning for the legacy queries in `../Logs/test/` and by the existing AI suite tests of the start-up path still passing when the user runs the suite

After this group: a refactoring only, no behaviour change.

## 2. Entry point and skeleton

- [x] 2.1 Add to `FilesDbContext` (file `Core/EF/Queries/AiAgentMigrationQueries.cs`) the compiled query `AiAgentRoomIdsAsync(tenantId, parentIds)`: ids of the direct children of the given parents with type `AiRoom`. Add `AiAgentMigrator` (`[Scope]`, `products/ASC.Files/Core/Core/AI/`) with `FindAgentsAsync()`, which reads the ids of the `aiagents`, `virtualrooms` and `aihub` roots without creating them and lists the agents, and `MigrateAgentsAsync(ids)`, which takes the lock `ai_agent_migration_{tenant}_{room}` per agent, calls an empty per-agent method in its own try/catch and returns the numbers converted and failed (design D3, D7 step 3). Logging through a `[LoggerMessage]` class. Verified by the build of `ASC.Files.Core` with no new warnings, `dotnet format style` on the new files and no warm-up warning for the query
- [x] 2.2 Add the action `POST internal/ai/agents/migration` in a new internal controller of `products/ASC.AI/Server/Api` without `[AiFeature]`: portal owner or DocSpace admin, otherwise 403; finds the agents, runs the legacy providers migration for the portal (1.1), then `MigrateAgentsAsync` (the sequence is `AiAgentsPortalMigration.RunAsync` in `ASC.AI.Core`, which the pass of group 8 runs per portal); answers a response DTO in `ApiModels` with `migrated` and `failed`, built in the controller (design D9). XML docs as `openapi-endpoint-docs.md` §0 asks for an internal action. Verified by the build of `ASC.AI` with no new warnings (API002–API006 included) and `dotnet format style`
- [ ] 2.3 Write `AgentMigrationTestBase` and `AgentMigrationRouteTests` in `products/ASC.AI/ASC.AI.Tests/Tests/AgentMigrationTests/`, calling the route by raw HTTP through `RawApiClient`, one test per scenario of "Starting the conversion by request" that needs no conversion yet: portal without agents answers 200 with zeros, repeated call answers zeros, Room admin and User get 403, no sign-in 401, AI switch off answers 200. Verified by the tests built, run on the user's command, and proven red with `.claude/scripts/prove-red.sh`

After this group: the route answers, finds the agents of a portal and changes nothing.

## 3. Chat outputs

- [ ] 3.1 Add the pure function for the titles of design D6 (agent title, system folder title, limit → title) in `ASC.Files.Core`, with the limit the room rename applies read from the code. Unit tests in `common/Tests/ASC.Core.Common.Tests`: short title, title exactly at the limit, one over, a suffix longer than the limit. Verified by the unit tests passing and proven red
- [x] 3.2 Add step 1 of design D3 to `AiAgentMigrator`: every child folder of the agent other than its Knowledge folder is retitled by the function of 3.1, set to `DEFAULT` and moved with `IFolderDao.MoveFolderAsync` into the My documents of the owner (the portal owner when the agent's owner is not found or is terminated), or deleted when it holds nothing. The agent itself is not converted yet and is not counted as converted. Verified by the build of `ASC.Files.Core` with no new warnings and by the tests of 3.3
- [ ] 3.3 Write `AgentMigrationOutputsTests`, one test per scenario of "Chat outputs of a converted agent move to My documents": outputs moved (folder title, same file id, another member gets 403), empty Chat outputs, owner over the quota (the folder is moved, used space above the quota), owner disabled (folder with the portal owner); and one test that a second call moves nothing again. Verified by the tests built, run on the user's command, proven red

After this group: the route moves the Chat outputs of every agent into My documents; the agents stay agents.

## 4. The project and the Knowledge room: the transaction

- [x] 4.1 Add `IFolderDao<T>.ConvertAiAgentAsync` (design D4): `FolderDao` implements steps 1, 2, 7 and 8 in one transaction (re-read of the type, the project row with colour and cover, the knowledge files moved into the room, the Knowledge folder removed, the type and title of the room, the link); third-party DAOs throw `NotSupportedException`. The `ExecuteUpdate`/`ExecuteDelete` statements go into `AiAgentMigrationQueries.cs` by `ef-compiled-queries.md`. Verified by the build of `ASC.Files.Core` with no new warnings, `dotnet format style`, no warm-up warning
- [x] 4.2 In `AiAgentMigrator`, after the outputs step: step 2 of design D3 (move under the AiHub root), then the call of 4.1; the agent is counted as converted when the transaction commits. Verified by the build and by the tests of 4.3
- [ ] 4.3 Write `AgentMigrationRoomTests`, one test per scenario of "Conversion of an AI agent into an AI project" (agent converted: project with the title and `createdBy`, no room of the AI room type in any section; run twice: one project) and "The room of a converted agent becomes a Knowledge room" (knowledge kept: `getRoomInfo`, `getProjectSources`, `getFileInfo`, 404 of the folder id; title at the limit; vectorization off). Add a test that no user's used space changes when an agent is moved under the AiHub root (design, first risk); if it fails, extend the Archive exclusion in `FolderDao.MoveFolderCoreAsync` to the AiHub root in this task. Add to `AgentMigrationRouteTests` the scenario "Owner starts the conversion" (two agents answer `migrated` 2, `failed` 0). Verified by the tests built, run on the user's command, proven red

After this group: an agent with knowledge files becomes a project plus a linked Knowledge room. Threads, members and marks are not carried yet; an agent without knowledge files leaves an empty room.

## 5. After the commit

- [x] 5.1 Add step 4 of design D3 to `AiAgentMigrator`: logo copy to the project, `FolderIndexIntegrationEvent` for the project and the room and the index events of the moved knowledge files, socket events through `SocketManager` (`create-folder`, `update-folder`, `delete-folder`), and `DeleteFolderAsync` of a room without knowledge files, followed by `RoomLogoManager.DeleteLogoFilesAsync` for an agent with a logo; each effect in its own try/catch with a warning log, after the commit (`failure-paths.md` §3). Verified by the build with no new warnings and by the tests of 5.2
- [ ] 5.2 Add to `AgentMigrationRoomTests`: "An agent without knowledge leaves no room" (empty `getProjectSources`, `getRoomInfo` 404); the project of an agent with a logo has a logo; a knowledge file is found by a title search in the Knowledge room after the conversion, polled on a deadline (`full-text-search.md`). Verified by the tests built, run on the user's command, proven red

After this group: the room side is complete, including the empty-knowledge case.

## 6. Threads and the model

- [ ] 6.1 Add a `DbSet<DbPreference>` to `FilesDbContext` (the table exists; no schema change) and step 3 of design D4 to `ConvertAiAgentAsync`: `entry_id` room → project in threads, assignments, tool preferences, MCP servers and preferences. Verified by the build of `ASC.Files.Core` with no new warnings, `dotnet format style`, no warm-up warning, and `ASC.Migration.Creator` reporting no model change
- [ ] 6.2 Write `AgentMigrationThreadsTests`: "Threads of an agent become threads of the project" (thread follows the agent with its messages; a thread started without an entity stays where it was) and "The model of an agent becomes the model of the project" (bound profile; no assignment answers null). Add a test for a thread of a Guest of the agent that records the status the Guest gets (design, Open Questions). Verified by the tests built, run on the user's command, proven red

- [x] 6.3 Add the index `(tenant_id, entry_id)` to `DbThread` (MySQL and PostgreSQL sections of the model) and generate `MigrationContext_Upgrade110` with `ASC.Migration.Creator`. Verified by the migration holding only that index, the runner applying it locally, and `EXPLAIN` of the move of the threads and of `DeleteThreadsByFolderIdsAsync` choosing `IX_tenant_id_entry_id`

After this group: the chat history and the model of an agent are in its project.

## 7. Members, access and marks

- [x] 7.1 Add steps 4, 5 and 6 of design D4 to `ConvertAiAgentAsync`: project members from the room's user shares and the members of its invited groups by the table of design D5, deletion of the room's Viewer and invitation-link shares and of the shares of the knowledge files, tag links re-pointed (Custom, Pin) or deleted (Recent, "new"). Verified by the build with no new warnings, `dotnet format style`, no warm-up warning
- [ ] 7.2 Write `AgentMigrationAccessTests`, one test per scenario of "Members of a converted agent" (roles carried over; group with a member who has a role of their own; joining the group later; Guest by own role and through a group) and "Access to the Knowledge room of a converted agent" (Content creator uploads; Viewer reads through the project and is not listed in the room; Guest gets 403; external link of a knowledge file gone), and the scenario "Pinned agent". Roles are checked with the lowest account that can hold them (`tests.md`). Verified by the tests built, run on the user's command, proven red

After this group: the conversion of one agent is complete; only the route starts it.

## 8. The background pass

- [ ] 8.1 Add to `TenantDbContext` (`common/ASC.Core.Common/EF/Queries`) two compiled queries: ids of active portals with `last_modified >= from`, and ids of active portals after an id, limited. Add the settings class of the pass state (`LastPassStart`, `Attempts`) and the pure functions of design D7: the window start from `LastPassStart` and the overlap, the candidate list from the changed portals and `Attempts`, and the next `Attempts` after a portal's result. Unit tests in `common/Tests/ASC.Core.Common.Tests`: no `LastPassStart`; overlap 0 and above; a portal at the attempt limit is left out; one below is kept; success after a failure removes the entry; a failure at limit − 1 reaches the limit; a portal skipped for the held lock of the legacy providers migration is a candidate of the next pass with its count unchanged. Verified by the unit tests passing and proven red; the build of `ASC.Core.Common` with no new warnings
- [x] 8.2 Add `AiAgentsMigrationService` to `products/ASC.AI/Worker/BackgroundServices` (design D7) with the keys of design D10 read once through `ConfigurationValueExtensions`, with their comments (unit, bounds, example), a single log line at start when the pass is off, and an error log when a portal reaches the attempt limit. The legacy lock not acquired skips the portal without counting and keeps it in `Attempts` for the next pass; a throw counts as a failed pass. Register it in `AiWorkerServiceExtensions`. Verified by the build of `ASC.AI.Worker` with no new warnings, `dotnet format style`, and a unit test that the services still register with a broken `ai:agentsMigration` section (`configuration.md` §8)
- [ ] 8.3 Set `ai:agentsMigration:enabled` to `false` for the AI worker in the `integration-test` profile (`common/ASC.AppHost/Configuration/ProjectConfigurator.cs`), next to the cleaner periods. Verified by the AppHost build and by the existing agent tests of the AI and Files suites still passing when the user runs them
- [x] 8.4 Document the four keys in the comment at the read and add them, with their defaults, to `../buildtools/config` only if the neighbouring `ai:*Cleaner` keys are there; otherwise the code defaults stand. Verified by a grep of `buildtools/config` for `deletedThreadsCleaner` deciding the case, noted in the commit message

After this group: agents are converted without anyone asking, on every stand except the test one.

## 9. Restore

- [ ] 9.1 Unit test in `common/Tests/ASC.Data.Backup.Core.Tests`: `TenantsModuleSpecifics` writes the current time into `last_modified` of a restored `tenants_tenants` row whatever the archive holds. Verified by the test passing and proven red
- [x] 9.2 No integration test restores an archive, by decision of 2026-11-21: the backup suite never starts a real restore (every restore request goes through `common/Tests/ASC.Data.Backup.Tests/Tests/Restore/RestoreGuard.cs`, and `RestoreGuardCoverageTests` fails on any other caller of the restore route), and the guard stays closed. The scenario "Restored archive" of "The background pass converts agents by itself" rests on the unit test of 9.1, on the `entry_id` relations of `AiModuleSpecifics` and on the conversion tests of the AI suite; its row in `verification.md` is CODE-ONLY and says so. The scenarios "Portal being restored", "Unchanged portal" and those of "A failed conversion is retried and then left" are covered by the unit tests of 8.1; the commit message says the periodic loop's wiring is not run by an integration suite

After this group: the last-modified time a restore writes is pinned by a unit test; no test restores an archive.

## 10. Specs and final checks

- [x] 10.1 Add the rows of the eleven new requirements to `openspec/specs/ai-agents/verification.md` with the `File.cs:line` of their tests; a scenario covered by a unit test or by code reading only says so in its row. Verified by every scenario of the delta having a row
- [ ] 10.2 Re-read the delta, `proposal.md` and `design.md` against the finished code (`claims.md` §3): every status code, title and count of a scenario is the one a test asserts, or the text moves. Verified by `python .claude/skills/review-branch/scan.py --worktree` listing no requirement of the change at 500 characters or more
- [x] 10.3 Re-check the touched spec with `openspec validate --specs --no-interactive` and run `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec ai-agents`. The change adds one operation, `POST internal/ai/agents/migration`, which is internal and absent from `docspace-backend.yaml`: it is called by raw HTTP in `AgentMigrationRouteTests` and by every other `AgentMigration*Tests` class. Verified by the validate output and by the script naming no operation of the change as uncalled

## Workflow follow-up

- Commit the proposal (`docs(openspec): propose migrate-ai-agents-to-projects`) on the user's command, then apply.
- One commit per group, so each step is reviewed on its own diff.
- Offer the hand-over check of `review-branch` when the branch is finished.
