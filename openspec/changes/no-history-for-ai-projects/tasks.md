# Tasks

## 1. Tests first

- [ ] 1.1 Add `products/ASC.Files/Tests/Tests/03_Rooms/AiHub/AiProjectHistoryTests.cs` (derived from `AiHubTestBase`)
  for the requirement "An AI project has no history", following `.claude/rules/tests.md`:
  - "History of a project is refused": `getFolderHistory` of a project answers 403 for its owner, for a Content
    creator member and for a DocSpace admin who is not a member; the same caller first reads the history of an
    Instructions room with 200, so the 403 is not a broken setup.
  - "History of an own entry inside a project is refused": a member uploads a file into the project and gets 403
    from `getFileHistory` of it; a file of their My documents answers 200 in the same test.
  - "Report of a project is refused": `createReportFolderHistory`, `getReportFolderHistory` and
    `terminateReportFolderHistory` of the project answer 403 to its owner, while the report status of an
    Instructions room answers 200.
  - "Project reports no history export right": `getRoomInfo` of the project has `security.HistoryExport` false, and
    of an Instructions room true.
  - "The other side of a copy keeps its history": a member copies a file of a My documents folder into the project,
    and the copy back into that folder; the history of that folder contains `FileCopied` for both.
  No test is marked `Category=Bug`.
- [ ] 1.2 Prove the tests red without the fix with `.claude/scripts/prove-red.sh` and keep the result line for the
  commit message. "The other side of a copy keeps its history" passes before the fix as well; it guards the move and
  copy path of 2.1 and is named so in the commit message.

- [ ] 1.3 Add `products/ASC.Files/Tests/Tests/05_Features/Socket/SocketAiProjectHistoryEventsTests.cs`: the owner
  watches the project and an Instructions room, renames the project, copies a My documents file into it and renames
  the room; `s:update-history` arrives for the room and not for the project within the no-delivery window. Prove it
  red with `prove-red.sh`.

## 2. No history for a project

- [ ] 2.1 In `products/ASC.Files/Core/Helpers/FilesMessageService.cs`: in `GetAdditionalEntryDataAsync(FileEntry<int>…)`
  return no references when the room found for the entry is an AI project; in `SendMoveOrCopyMessageAsync` give no
  references to a side whose parent chain or end folder is an AI project, and add the entry itself only to a side
  that is not one. Verify: 1.1 copy scenario passes; `02_Folders/History/FolderHistoryFileActionsTests.cs` still
  passes (copy and move between rooms).
- [ ] 2.2 In `products/ASC.Files/Core/Core/Security/FileSecurity.cs` (`FilterEntryAsync`), next to the other AI project
  rules and before `HistoryExport` is mapped to `Read`, return false for `HistoryExport` on an AI project. Verify:
  1.1 report and right-flag scenarios pass; `FolderHistoryReportPermissionsTests` still passes.
- [ ] 2.3 In `HistoryApiHelper` (`products/ASC.Files/Server/ApiModels/ResponseDto/HistoryDto.cs`), after the Read
  check of `GetEntryEventsAsync`, throw `SecurityException` for an AI project and for an entry for which
  `DocSpaceHelper.InAiProjectAsync` is true. Verify: 1.1 log scenarios pass; `01_Files/Log/FileLogPermissionsTests`
  and `02_Folders/History/FolderHistoryPermissionsTests` still pass.
- [ ] 2.4 Update the `<remarks>` and the `[SwaggerResponse(403)]` texts of `GetFileHistory` (`FilesController`),
  `GetFolderHistory` and the three report actions (`FoldersController`) to name the AI project refusal
  (`.claude/rules/openapi-endpoint-docs.md`).
- [ ] 2.5 Build `products/ASC.Files/Core` and `products/ASC.Files/Server` with `--no-dependencies --no-incremental` and
  run `dotnet format style` on the edited files with `--verify-no-changes`: no new warnings, no style changes.

## 3. Rules, specs and verification

- [ ] 3.1 Add to the AI rooms row of `.claude/rules/files-sections.md` that a project and the entries inside it have
  no history: the log and report endpoints answer 403 and no `update-history` event is sent.
- [ ] 3.2 In `openspec/specs/history-and-audit/verification.md`, add rows for "An AI project has no history" citing the
  tests of 1.1 by file and line; the rows of "Who may read an entry history" and "Who may export a folder history"
  keep their citations (their scenarios are unchanged) and gain a row for the AI project exception pointing at the
  same tests.
- [ ] 3.3 Regenerate the Files OpenAPI document and the SDKs for the changed remarks (`generate-sdk` skill).
- [ ] 3.4 Run `openspec validate --specs --no-interactive`,
  `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec history-and-audit` (the
  change adds no operation; `getFileHistory`, `getFolderHistory`, `createReportFolderHistory`,
  `getReportFolderHistory` and `terminateReportFolderHistory` are each called by a test) and
  `python .claude/skills/review-branch/scan.py --worktree`: no requirement of the change is listed at 500 characters
  or more.

## Workflow follow-up

- Frontend: hide the History tab for an AI project and its entries (`client/packages/client/src/helpers/info-panel/tabs.ts`).
- PR "Known limitations": the portal audit trail keeps showing the titles of project entries to admins, as for My
  documents (user decision).
- Archive the change after review with `openspec-archive-change`.
