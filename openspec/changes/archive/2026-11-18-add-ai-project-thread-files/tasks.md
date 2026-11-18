# Tasks

A ticked task names the test class that covers it. Runs of 2026-11-18: the thread classes of `ASC.AI.Tests`
named in groups 2–6 passed 90 of 90 and `AiIntegrationModuleSpecificsTests` 3 of 3 before tasks 4.4, 4.5, 5.3
and 7.1; after them `ChatProjectThreadListTests`, `ChatEntityContextTests`, `ThreadFilesRecipientsTests`,
`ThreadDeletionTests`, `ProjectMemberRemovalTests` and `ChatPermissionRevokedTests` passed 29 of 29; after
tasks 2.4 and 3.5 `ThreadArtifactsTests` and `ThreadFilesListBoundsTests` passed 24 of 24.

## 1. Schema and backup

- [x] 1.1 Add the two folder links, the deletion time and its index to the AI thread table (MySQL SaaS migration); verified by the classes of groups 2–6 passing against a schema that has them
- [x] 1.2 Declare the folder links and the deletion time in the backup module of the AI integration tables; verified by `AiIntegrationModuleSpecificsTests`
- [x] 1.3 Add the row for the scenario "A project thread follows its project and its file sets" to `openspec/specs/backup-and-restore/verification.md`, citing `AiIntegrationModuleSpecificsTests.cs:53,73,91`

## 2. Artifacts

- [x] 2.1 Create the artifacts folder of a thread with its link in one transaction, one folder for parallel callers; verified by `ThreadArtifactsTests` (`Upload_InParallelToANewThread_CreatesOneFolder`, `Upload_ToThreadsOfTheSameProject_UsesAFolderPerThread`)
- [x] 2.2 Add the storage route `POST internal/ai/threads/{id}/artifacts` with its refusals (400, 404, 403) and the new title on a clash; verified by `ThreadArtifactsTests`
- [x] 2.3 Add `getThreadArtifacts` to the Files service; verified by `ThreadArtifactsTests` (`List_*`)
- [x] 2.4 Add tests for the size limits of the artifact route (an empty file, a file of 100 MB and one byte) and state the limits in the requirement "Saving an artifact of a thread"; verified by `ThreadArtifactsTests` (`Upload_AnEmptyFile_Returns400AndAddsNothing`, `Upload_AFileOverTheLimit_Returns400AndAddsNothing`)

## 3. Attachments

- [x] 3.1 Allow a chunked upload session in a project for whoever may chat there, refuse a relative path and an unsupported format, and create the attachment record with the file; verified by `ThreadAttachmentsTests` (`Upload_*`, `Attach_*`)
- [x] 3.2 Allow `copyBatchItems` into a project as a new file, refuse folders, unsupported formats and a move; verified by `ThreadAttachmentsTests` (`Copy_*`, `Move_*`)
- [x] 3.3 Move a linked file into the attachments of its thread; verified by `ThreadAttachmentsTests` (`Bind_*`)
- [x] 3.4 Add `getThreadAttachments` to the Files service; verified by `ThreadAttachmentsTests` (`List_*`)
- [x] 3.5 Add tests for the boundaries of both lists (`count` 0, `count` 101, `startIndex` -1, each answering 400, and `count` 100 answering 200); verified by `ThreadFilesListBoundsTests` (8 cases)

## 4. Privacy

- [x] 4.1 Refuse every entry inside a project to everyone but its creator, and leave the creator read, download and copy; verified by `ThreadFilesStrangerAccessTests` and `ThreadFilesAuthorAccessTests`
- [x] 4.2 Answer an empty listing and zero counters for a project; verified by `ThreadFilesProjectListingTests`
- [x] 4.3 Send the socket event of an upload into a project to the uploader only and keep admins out of the shared-users list; verified by `ThreadFilesRecipientsTests`
- [x] 4.4 Do not mark a file uploaded into a project as new for the project's owner; verified by `ThreadFilesRecipientsTests.UploadToProject_IsNotMarkedNewForTheProjectOwner` (prove-red: "RED as expected: 1 of 1 failed without the uncommitted product changes (6 file(s)); green with it: 1 of 1 passed.")
- [x] 4.5 Do not publish the room's upload notification event for a project, in each of the five places that publish it, and drop such an event in the worker's handler as a second guard. The decision is a function of the room type, verified by the unit test `DocSpaceHelperAnnouncementTests` (prove-red: "RED as expected: 1 of 9 failed without the change in announce.patch (1 file(s)); green with it: 9 of 9 passed."). Not proven: the calls of that function at the publishing places and in the handler, because the test stack has no channel that shows this notification; `ThreadAttachmentsTests` and `ThreadFilesRecipientsTests` passed 28 of 28 with them in place

## 5. Deleting a project thread

- [x] 5.1 Mark a project thread deleted in the request and filter marked threads out of every read; verified by `ThreadDeletionTests` (`Delete_ProjectThread_LeavesTheListAndTheSearchAtOnce`, `Delete_ProjectThread_ClosesItsMessagesAndFilesAtOnce`)
- [x] 5.2 Add the background service that removes the files, the folders and the row of a marked thread and returns the space; verified by `ThreadDeletionTests` (`Delete_ProjectThread_CleanerDeletesItsFilesAndFreesTheProjectSpace`, `Delete_ProjectThread_KeepsTheOtherThreadsOfTheProject`, `DeleteProject_WithADeletedThread_Finishes`)
- [x] 5.3 Read the period of that service through the configuration helper, between 5 seconds and 1 day, 5 minutes by default; verified by `ThreadDeletionTests` passing with the 5-second period of the test stack

## 6. Losing access to a project

- [x] 6.1 Remove a member's share on a project and mark the member's threads there in one transaction, on removal, on leaving and on "remove from the list" by the owner; verified by `ProjectMemberRemovalTests`
- [x] 6.2 Check that the tests cited by the "Thread privacy" rows of `openspec/specs/ai-agents/verification.md` (`ChatPermissionRevokedTests.cs:52,67,90`) still hold for the narrowed requirement, and add the row for the scenario "Member removed from an AI project" citing `ProjectMemberRemovalTests.cs:43`; verified by running `ChatPermissionRevokedTests`

## 7. Thread list of a project

- [x] 7.1 Cover the thread list of a project on the public chat routes: a thread started in a project is listed for that project only, and a thread started in a room is not listed for a project; verified by `ChatProjectThreadListTests` (2 of 2) next to `ChatEntityContextTests`, which keeps the one list of every other entry
- [x] 7.2 Check the row "Rooms share one list" of `openspec/specs/ai-agents/verification.md` against the narrowed requirement "One thread list per user" (it names rooms, a folder and a third-party room only, so it stays as it is) and add the row of "Thread list of an AI project" citing `ChatProjectThreadListTests.cs:41,62`; verified by the row naming those tests

## 8. Specs and rules

- [x] 8.1 Add the rows of the other twelve new requirements to `openspec/specs/ai-agents/verification.md` with the `File.cs:line` of the tests named in groups 2–6; verified by every scenario of the delta having a row
- [x] 8.2 Update the "AI rooms" row of `.claude/rules/files-sections.md`: it says that projects hold no files; verified by the row describing the thread files and the access rule
- [x] 8.3 Re-check the touched specs with `openspec validate --specs --no-interactive` and run `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec ai-agents` and `--spec backup-and-restore`. The published contract does not hold the new operations until the documents and SDKs are regenerated, so they are checked against this list: `getThreadArtifacts` is called by `ThreadArtifactsTests` and `getThreadAttachments` by `ThreadAttachmentsTests`, both through raw HTTP

## Workflow follow-up

- Archive the change after its review.
- Follow-up changes, each proposed when its work starts: removing a member's threads when the profile is deleted; removing a Guest from projects; deleting the file of an attachment that was never sent; a socket event for the creator about a new artifact or attachment.
