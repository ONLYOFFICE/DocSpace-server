# Tasks

## 1. Refusing AI to a Guest

- [x] 1.1 In `FileSecurity.FilterEntryAsync`, refuse `FilesSecurityActions.CanUseAi` to a Guest (`isGuest`) before the role is looked at, for every folder type; no test or scenario of its own (design, "The security check refuses a Guest on any folder"); verified by `dotnet build products/ASC.Files/Core/ASC.Files.Core.csproj --no-dependencies --no-incremental` with no new warnings, `dotnet format style` on the changed file, and by `AiProjectGuestInviteTests` and `ChatPermissionGuestTests` still passing when the user runs the suite

## 2. Removing a converted Guest from the projects

- [x] 2.1 Add `RemoveFromAiProjectsAsync(userId)` to `ISecurityDao<T>` (implemented once in `SecurityBaseDao<T>`) and to its `FileSecurity` wrapper, declared on `IFileSecurity` for the job: in one transaction of `FilesDbContext` it deletes every share of the user on folders of type `AiProjectRoom` (a precompiled bulk delete `DeleteAiProjectSharesAsync` in `AiIntegrationQueries.cs`) and marks the user's live project threads with the query of `MarkAiProjectThreadsDeletedAsync`; `RemoveAiProjectMemberAsync` and `MarkAiProjectThreadsDeletedAsync` stay unchanged; verified by the build of `ASC.Files.Core` with no new warnings and `dotnet format style` on the changed files
- [x] 2.2 In `UpdateUserTypeProgressItem.DoJob`, for the type Guest: `UpdateShareByFolderTypesAsync` keeps only `FolderType.AiRoom`, and the method of 2.1 is called right after it, before `UpdateUserTypeAsync`; no catch around it; verified by `dotnet build common/ASC.Data.Reassigns/ASC.Data.Reassigns.csproj --no-dependencies --no-incremental` with no new warnings and `dotnet format style` on the changed file
- [x] 2.3 Write `GuestConversionTests` in `products/ASC.AI/ASC.AI.Tests/Tests/GuestConversionTests/` on `ThreadFilesTestBase`, starting the conversion with `startUserTypeUpdate` and polling `getUserTypeUpdateProgress` until `isCompleted` as `AiProjectRolesTests` does, one test per scenario of "Converting a member to Guest removes them from the projects": member converted (`getRoomSecurityInfo` no longer lists the user, artifact and sent attachment 404 after the pass, used space back); thread 404 to the user converted back to User right after the job (a Guest is refused with 403 by user type before the thread is looked at); the other member still reads their thread and lists its artifact; owner of a project converted with the project passed to another account (`createdBy` is that account, the converted user not listed, the artifact 404 after the pass); member of an agent converted (listed as Viewer); verified by the tests written and built, run on the user's command, and proven red with `.claude/scripts/prove-red.sh` (4 of 5 red without the change; the agent test guards the behaviour kept and stays green)

## 3. Specs

- [x] 3.1 Add the rows of the new requirement to `openspec/specs/ai-agents/verification.md` with the `File.cs:line` of the tests of 2.3; verified by every scenario of the delta having a row
- [x] 3.2 Re-check the touched spec with `openspec validate --specs --no-interactive` and run `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec ai-agents`. The change adds no operation; `GuestConversionTests` calls `startUserTypeUpdate`, `getUserTypeUpdateProgress`, `getRoomSecurityInfo`, `getRoomInfo`, `getFileInfo` and `getThreadArtifacts`, and reads the thread through the internal route of the AI service, as `ProjectMemberRemovalTests` does; verified by the validate output and the script naming no operation of the change as uncalled

## Workflow follow-up

- Commit the proposal (`docs(openspec): propose remove-guests-from-ai-projects`) on the user's command, then apply.
