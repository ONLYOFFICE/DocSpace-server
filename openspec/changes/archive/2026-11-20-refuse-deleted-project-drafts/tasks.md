# Tasks

## 1. Test first

- [x] 1.1 Add `SaveFilesMany_DeletedDraftOfAProjectFileAttachedAgain_Answers404AndStoresNothing` to
  `products/ASC.AI/ASC.AI.Tests/Tests/UnsentAttachmentCleanupTests/UnsentAttachmentCleanupTests.cs` for the scenario
  "Deleted attachment attached again": a Content creator of a project uploads a file into the project, attaches it
  through `AiAttachmentsSaveFilesManyAsync`, deletes the draft through `AiAttachmentsDeleteAsync`, then attaches the
  project file together with a file of their My documents in one batch. Assert an `ApiException` with 404, no
  `ai_integration_attachments` row of the caller for the My documents file (read through `OpenDatabaseAsync`), and,
  through `WaitForRemovalAsync`, that the deleted draft reads as null, the project file answers 404 and the project's
  used space is back to what it was before the upload. Follow `.claude/rules/tests.md`.
- [x] 1.2 Prove the test red without the fix with `.claude/scripts/prove-red.sh` and keep its result line for the
  commit message. The pass runs every 5 seconds on the test stack; a pass that lands between the delete and the
  second attach also answers 404, so a green run without the fix is a timing hit - run it again before concluding.

## 2. Refuse a deleted draft

- [x] 2.1 Name the mark of a deleted draft once in `common/ASC.AI.Integration/Attachments/AttachmentsStorage.cs` and
  use it in `AgeUnboundAttachmentsAsync` (`Database/AttachmentQueries.cs`) and in a check of whether a draft carries
  it (creation time at or before the mark). Verify: the project builds with no new warnings
  (`dotnet build common/ASC.AI.Integration/ASC.AI.Integration.csproj --no-dependencies --no-incremental`).
- [x] 2.2 In `AttachmentsStorageService.CreateManyAsync` (`products/ASC.AI/Server/Service/`), right after the read of
  the rows to reuse and before any write, reuse a live row of a file when there is one and throw
  `ItemNotFoundException` when every row of a requested project file carries the mark. Verify: test 1.1 passes,
  and `ThreadAttachmentsTests.Attach_AnUploadedFile_ReturnsOneAttachmentWithItsText` and
  `Attach_AnUploadedPicture_ReturnsOneImageAttachment` still pass.
- [x] 2.3 Build `products/ASC.AI/Server` and `products/ASC.AI/Worker` with `--no-dependencies --no-incremental` and run
  `dotnet format style` on the edited files with `--verify-no-changes` (`.claude/rules/csharp-style.md`): no new
  warnings, no style changes.

## 3. Specs and verification

- [x] 3.1 In `openspec/specs/ai-agents/verification.md`, add the new test to the rows of "Attachment of a file stored
  in a project" (the existing citations `ThreadAttachmentsTests.cs:212,228` stay, the scenarios are unchanged), with
  the file and line of the test from 1.1. Verify: the cited lines are the test's `[Fact]` methods.
- [x] 3.2 Run `openspec validate --specs --no-interactive` and
  `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec ai-agents`: validation
  passes, and `aiAttachmentsSaveFilesMany` and `aiAttachmentsDelete` are called by a test (the change adds no
  operation). Run `python .claude/skills/review-branch/scan.py --worktree`: the modified requirement is not listed at
  500 characters or more.

## Workflow follow-up

- Archive the change after review with `openspec-archive-change`, which syncs the delta into
  `openspec/specs/ai-agents/spec.md`.
