# Tasks

## 1. Marking the threads of every project

- [x] 1.1 Add `MarkAiProjectThreadsDeletedAsync(userId)` and `MarkOwnedAiProjectThreadsDeletedAsync(userId)` to `ISecurityDao<T>` (implemented once in `SecurityBaseDao<T>`) and to its `FileSecurity` wrapper: the first marks every live thread the user created whose entry is an `AiProjectRoom`, the second only those whose project the user created (two bulk updates in `AiIntegrationQueries.cs`, not precompiled); neither removes a share; `RemoveAiProjectMemberAsync` stays unchanged; verified by `dotnet build products/ASC.Files/Core/ASC.Files.Core.csproj --no-dependencies --no-incremental` with no new warnings and `dotnet format style` on the changed files
- [x] 1.2 Add `FileStorageService.DeleteAiProjectThreadsAsync(userId)` calling the first method of 1.1, for the two jobs; verified by the same build of `ASC.Files.Core` and by `ProjectMemberRemovalTests` (the unchanged one-project removal) still passing when the user runs the suite

## 2. Deleting a profile

- [x] 2.1 Call the method of 1.2 in `RemoveProgressItem.DoJob` right after `DeletePersonalDataAsync`, whatever `_deleteProfile` says, and in `ReassignProgressItem.DoJob` right after `DeletePersonalDataAsync` when `_deleteProfile` is true; no catch around it; verified by `dotnet build common/ASC.Data.Reassigns/ASC.Data.Reassigns.csproj --no-dependencies --no-incremental` with no new warnings
- [x] 2.2 Write `ProfileRemovalTests` in `products/ASC.AI/ASC.AI.Tests/Tests/ProfileRemovalTests/` on `ThreadFilesTestBase`, calling the data jobs through the People SDK clients as `RemoveStartTests` and `ReassignStartTests` do, one test per scenario of "Deleting a profile deletes its project threads": data of a member removed (both files 404, used space back); the project's owner gets 403 or 404 on the member's artifact right after the job completes, never 200; member deleted with `deleteMember` (artifact 404, used space back); owner of the project deleted (the project's `createdBy` is the caller, the former owner's files 404, the member still reads their thread and lists its artifact); data transferred with `deleteProfile` true (the recipient, the project's owner, gets 403 or 404, never 200, then 404 after the pass); verified by the tests written and built, run on the user's command, each proven red with `.claude/scripts/prove-red.sh`

## 3. Keeping the creator inside a project

- [x] 3.1 Keep the contents of AI projects with their creator through the exclusion lists: `FolderDao.ReassignFoldersAsync` and `ReassignRoomFoldersAsync` add the folders lying directly in a project, `FileDao.ReassignFilesAsync` and `ReassignRoomsFilesAsync` add the projects; `GetRoomsFoldersReassignInfoAsync` and `GetRoomsFilesReassignInfoAsync` (`FolderQueries.cs`, `FileQueries.cs`) take the list as a new parameter; verified by the build of `ASC.Files.Core` with no new warnings and by the test of 2.2 where the project of a deleted owner still passes to the caller
- [x] 3.2 Add to `ProfileRemovalTests` the scenario of "Transferring data keeps the contents of a project with their creator": a disabled member's data transferred with `deleteProfile` false to the project's owner; after the member is enabled again, the member reads the thread and the artifact and the project's owner gets 403 on the artifact; verified by the test written and built, run on the user's command, proven red with `.claude/scripts/prove-red.sh`

## 4. Transferring a project the user owns

- [x] 4.1 Add `FileStorageService.DeleteOwnedAiProjectThreadsAsync(userId)` calling the second method of 1.1, for a transfer that keeps the profile; verified by the build of `ASC.Files.Core` with no new warnings and `dotnet format style` on the changed files
- [x] 4.2 Call it in `ReassignProgressItem.DoJob` when `_deleteProfile` is false, before `ReassignFoldersAsync`; no catch around it; verified by the build of `ASC.Data.Reassigns` with no new warnings
- [x] 4.3 Add to `ProfileRemovalTests` the three scenarios of "Transferring a project deletes its former owner's threads": the owner's data transferred (the project's `createdBy` is the recipient, both files of the former owner's thread 404 after the pass, used space back); a member's thread in the transferred project still read and its artifact listed; the former owner's thread in a project owned by someone else, where the former owner is a member, still read with its artifact after the former owner is enabled again; verified by the tests written and built, run on the user's command, each proven red with `.claude/scripts/prove-red.sh`

## 5. Specs

- [x] 5.1 Add the rows of the three new requirements to `openspec/specs/ai-agents/verification.md` with the `File.cs:line` of the tests of 2.2, 3.2 and 4.3; verified by every scenario of the delta having a row
- [x] 5.2 Re-check the touched spec with `openspec validate --specs --no-interactive` and run `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec ai-agents`. The change adds no operation; the operations it reaches (`startRemove`, `deleteMember`, `startReassign`, `getThreadArtifacts`, `getFileInfo`) are called by `ProfileRemovalTests`, and `removeUsers` shares the job of `deleteMember` and is called by the People suite; verified by the validate output and the script naming no operation of the change as uncalled

## Workflow follow-up

- Commit the proposal (`docs(openspec): propose delete-project-threads-on-profile-removal`) on the user's command, then apply.
- Archive the change after its review, in a commit of its own.
- Mark stage 12 done in `AI-THREAD-ARTIFACTS-PLAN.md` and write its journal line.
