# Design

## Context

See `proposal.md` for the motivation. The behaviour this design relies on:

- `ai-agents`, "Uploading an attachment into a project" and "Copying a portal file into a project": a file
  for a chat message is stored in the project itself, and its draft row is written in the same transaction
  as the file. A file in the root of a project therefore always has a draft row; the row is the only way to
  find the file.
- `ai-agents`, "Attachment of a file stored in a project": `aiAttachmentsSaveFilesMany` reuses that row and
  fills in its text.
- `ai-agents`, "A sent attachment is kept with its thread": linking moves the file from the project's root
  into the attachments folder of the thread, before the row is bound to the message.
- `ai-agents`, "The contents of a project are not listed": the root of a project lists nothing, so no client
  shows its files.
- `ai-agents`, "Deleting a thread of a project": a background pass of the AI worker removes the folders of a
  deleted thread and returns their size to the project; it is the model for this change.

How drafts are removed today, read in the code. The two delete routes of the AI service delete the caller's
rows by id, linked or not, and leave every file. A background pass of the AI worker (`OrphanAttachmentsCleanerService`)
reads unlinked rows older than a lifetime (24 hours, every hour) across portals, oldest first, 100 at a time,
and deletes them by id; it reads its two settings with `TimeSpan.Parse`. `FileDao.DeleteFilesAsync` removes
files with their storage, quota and index, but not their draft rows and not the size of the folder tree.

## Goals / Non-Goals

**Goals:**

- One place removes drafts: the background pass. It removes a draft together with its file when that file
  still lies in the root of a project.
- The delete routes hand the caller's unlinked drafts to that pass.
- The pass returns the size of the files it removes to the project.

**Non-Goals:**

- A file in the attachments folder of a thread: it goes with the thread.
- Deleting an attachment linked to a message.
- Socket events, audit records or webhooks for the removal.
- Rows of drafts that point at a file outside a project: their file is the user's own and stays, as today.

## Decisions

**The delete routes age the draft instead of deleting it.** `aiAttachmentsDelete` and
`aiAttachmentsDeleteMany` run one update that sets `created_at` of the caller's own rows with no message to
`DateTime.UnixEpoch`. The pass reads rows older than its lifetime oldest first, so an aged row is taken at
its next run whatever the lifetime is configured to. The update is an `ExecuteUpdate`, like the neighbouring
writes of `AttachmentQueries.cs`; the two delete queries of the caller's rows lose their last callers and
are removed. Alternatives, rejected by the user: deleting the row and the file in the request (two places
that delete), a `deleted_on` column (a schema change for what the lifetime already gives), doing nothing in
the routes (the file holds the project's space for the whole lifetime).

**A row linked to a message is never touched.** The aging update, the read of the pass and the deletion of
rows all carry `message_id is null`. Deleting a linked attachment through the routes changes nothing.

**Rows and files go in one transaction of `FilesDbContext`.** `FilesDbContext` maps the attachments table.
A new `IFileDao<int>` method takes the ids of a batch for the current portal and, in one transaction, deletes
the rows that are still unlinked, then reads the files they pointed at that lie in a folder of type
`AiProjectRoom` and were created by the row's creator, and deletes those files with the steps of
`DeleteFilesAsync`. It is a thin entry into the existing deletion: the body of `DeleteFilesAsync` takes an
optional step that runs first inside its transaction, as `SaveThreadFileAsync` passes its thread to the body
of `SaveFileAsync`. The method returns the files it deleted. The third-party DAOs throw `NotSupportedException`.
A failure rolls both back, so a file in the root of a project never loses its row.

**Racing a link.** Linking moves the file out of the root first and binds the row after, in two
transactions. If the move commits first, the pass deletes the unlinked row, finds the file outside the root
and leaves it in the thread's folder, where the thread's deletion removes it; the link then binds nothing.
If the pass commits first, the move finds no file and the link fails; a repeat answers 404 for the missing
attachment. No lock is added: a link needs a draft the client kept for the whole lifetime or deleted itself.

**`AiProjectThreadsService` calls it and returns the size.** A new method of `AiProjectThreadsService`
(Files.Core) calls the DAO method and then `ChangeTreeFolderSizeAsync` once per project with minus the size
of the files that were deleted, as `DeleteFolderAsync` does for a thread. A failure between the two leaves
the project's size too high, as for a thread.

**The pass walks a cursor, a portal at a time.** The read takes unlinked rows older than the cutoff after
the cursor (`created_at`, `id`), ordered by both, 100 at a time, as `DeletedThreadsCleanerService` does; a
batch that fails therefore does not keep the pass on the same rows. Each portal of a batch gets its own
scope with the portal set and the system account authenticated, and the method above. A portal that no
longer exists gets its rows deleted through `AiIntegrationContext`, without files. A failed portal is logged
and the pass goes on; its rows are taken again by the next pass. The count of removed rows and files is
logged in `finally`.

**Settings with bounds.** `ai:orphanAttachmentsCleaner:period` and `:lifetime` are read once with
`ReadInterval`: the period defaults to 15 minutes within 5 seconds and 1 day, the lifetime to 2 hours within
1 minute and 7 days. Neither key is set in `buildtools/config` or in the AppHost today. The
`integration-test` branch of `ProjectConfigurator` sets the period to `0.00:00:05`, as for the deleted
threads; the lifetime keeps its default there.

**Tests age a draft with SQL.** A draft older than the lifetime is a state the API cannot produce without
waiting 2 hours, so the test of an abandoned draft sets the `created_at` of its own row back by a day through
`Fixture.OpenDatabaseAsync` and then polls on a deadline. The code that writes `created_at` is covered by the
tests of saving a draft. A short lifetime in the test stack was rejected: the pass covers every portal, and
the drafts of other tests would vanish under them.

**The texts of the chat service follow.** The descriptions of `aiAttachmentsDelete` and
`aiAttachmentsDeleteMany` in `common/ASC.AI.Chat/app/openapi.ts` say the deletion is permanent, immediate and
covers linked attachments; they are rewritten to the new behaviour. Nothing else in the chat service changes.

## Risks / Trade-offs

- [The size of a project stays too high if the process stops between the deletion and the size update] →
  The same window exists for a deleted thread; the next upload or deletion does not correct it.
- [A link that races the pass] → Covered above; the outcome is a file in the thread's folder or a 404,
  never a lost file of a sent message.

## Migration Plan

No schema change. Drafts already older than 2 hours are removed by the first pass after the update, with
their files in projects. Rolling back restores immediate deletion; aged rows are then deleted by the old pass
as well, since their `created_at` is older than any lifetime.
