# Tasks

## 1. Refusing AI to a Guest

- [ ] 1.1 In `FileSecurity.FilterEntryAsync`, refuse `FilesSecurityActions.CanUseAi` to a Guest (`isGuest`) before the role is looked at, for every folder type; no test or scenario of its own (design, "The security check refuses a Guest on any folder"); verified by `dotnet build products/ASC.Files/Core/ASC.Files.Core.csproj --no-dependencies --no-incremental` with no new warnings, `dotnet format style` on the changed file, and by `AiProjectGuestInviteTests` and `ChatPermissionGuestTests` still passing when the user runs the suite

## 2. Removing a converted Guest from the projects

- [ ] 2.1 Add `RemoveFromAiProjectsAsync(userId)` to `ISecurityDao<T>` (implemented once in `SecurityBaseDao<T>`) and to its `FileSecurity` wrapper: in one transaction of `FilesDbContext` it deletes every share of the user on folders of type `AiProjectRoom` (a bulk delete in `AiIntegrationQueries.cs`, not precompiled) and marks the user's live project threads with the query of `MarkAiProjectThreadsDeletedAsync`; `RemoveAiProjectMemberAsync` and `MarkAiProjectThreadsDeletedAsync` stay unchanged; verified by the build of `ASC.Files.Core` with no new warnings and `dotnet format style` on the changed files
- [ ] 2.2 In `UpdateUserTypeProgressItem.DoJob`, for the type Guest: `UpdateShareByFolderTypesAsync` keeps only `FolderType.AiRoom`, and the method of 2.1 is called right after it, before `UpdateUserTypeAsync`; no catch around it; verified by `dotnet build common/ASC.Data.Reassigns/ASC.Data.Reassigns.csproj --no-dependencies --no-incremental` with no new warnings and `dotnet format style` on the changed file
- [ ] 2.3 Write `GuestConversionTests` in `products/ASC.AI/ASC.AI.Tests/Tests/GuestConversionTests/` on `ThreadFilesTestBase`, starting the conversion with `startUserTypeUpdate` and polling `getUserTypeUpdateProgress` until `isCompleted` as `AiProjectRolesTests` does, one test per scenario of "Converting a member to Guest removes them from the projects": member converted (`getRoomSecurityInfo` no longer lists the user, artifact and sent attachment 404 after the pass, used space back); thread 404 right after the job, before the pass; the other member still reads their thread and lists its artifact; owner of a project converted with the project passed to another account (`createdBy` is that account, the converted user not listed, the artifact 404 after the pass); member of an agent converted (listed as Viewer); verified by the tests written and built, run on the user's command, each proven red with `.claude/scripts/prove-red.sh`

## 3. Specs and plan

- [ ] 3.1 Add the rows of the new requirement to `openspec/specs/ai-agents/verification.md` with the `File.cs:line` of the tests of 2.3; verified by every scenario of the delta having a row
- [ ] 3.2 Re-check the touched spec with `openspec validate --specs --no-interactive` and run `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec ai-agents`. The change adds no operation; the operations it reaches (`startUserTypeUpdate`, `getUserTypeUpdateProgress`, `getRoomSecurityInfo`, `aiThreadsGetById`, `getThreadArtifacts`, `getFileInfo`, `getFolderInfo`) are called by `GuestConversionTests`; verified by the validate output and the script naming no operation of the change as uncalled
- [ ] 3.3 Tick stage 13 in `AI-THREAD-ARTIFACTS-PLAN.md` and add its line to the plan's log; verified by the plan's progress list

## Workflow follow-up

- Commit the proposal (`docs(openspec): propose remove-guests-from-ai-projects`) on the user's command, then apply.
