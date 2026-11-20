# Design

## Context

See `proposal.md` - Why. The behaviour this design relies on:

- `ai-agents`, "Uploading an attachment into a project": the draft of a file stored in a project is written with the
  file, so every such file has one draft row of its uploader.
- `ai-agents`, "Attachment of a file stored in a project": `aiAttachmentsSaveFilesMany` reuses that row instead of
  writing a second one.
- `ai-agents`, "Reading and deleting attachments" and "Drafts that are not sent are removed": deleting a draft only
  marks it (its creation time is set to the start of the Unix epoch); the background pass removes a marked draft
  with its project file on its next run.
- `ai-agents`, "Creating chat attachments", scenario "Batch": a batch with a file that cannot be attached answers 404
  and stores nothing.

The reuse reads the caller's unlinked rows of the requested project files without looking at the mark, so a marked
row comes back as a live draft and the pass removes it afterwards.

## Goals / Non-Goals

**Goals:**

- A call that reaches a marked draft of a project file answers 404 before anything of the batch is written.

**Non-Goals:**

- Bringing a deleted draft back (the option rejected in the proposal discussion).
- Drafts of files outside a project: they are not reused, a new row is written per call, as today.
- The race of a link with the background pass described in the design of `clean-up-unsent-attachments`.

## Decisions

**Refuse in the service, not in the query.** The read of the rows to reuse keeps returning marked rows; the service
refuses a file whose rows are all marked. Filtering them out in the query was rejected: the file would then look
like one with no draft and get a new row, and the pass, removing the marked row, would remove the project file under
the new draft as well, because it finds the file by the row's file id and creator, not by the row.

**One live row wins.** If a file has a live row next to a marked one (none is written today, but the read does not
exclude it), the live row is reused and the call succeeds; only a file with marked rows only is refused.

**The check runs before every write.** It sits right after the read of the rows to reuse, before the text of a reused
row is filled in and before new rows are written, and throws `ItemNotFoundException`, as the service's own read of a
missing draft does, so the whole batch answers 404 and nothing is stored, as for a missing file (where the chat
service answers 404 itself)
(`.claude/rules/failure-paths.md` §1).

**The mark is named once.** The value the delete writes and the value the check compares with come from one member of
the attachments storage, so the two cannot drift apart. A row counts as marked when its creation time is at or
before that value. A separate `deleted_on` column was rejected for the same reason as in `clean-up-unsent-attachments`:
a schema change for what the existing mark already says.

## Existing data

Drafts deleted before the update already carry the mark, so they are refused the same way; no migration. Rows the
pass already removed leave no draft and no file, and the call answers 404 as for a missing file.

## Risks / Trade-offs

- [A delete that commits while an attach of the same file is in flight] → The attach may still return the draft
  with 200 and the pass then removes it; the same window exists today and needs the caller's own delete racing its
  own attach.
- [A caller that relied on getting the deleted draft back] → None found in the web client or the chat service
  (proposal, "Breaks current behaviour"); a direct API caller uploads the file again.

## Migration Plan

No schema change, no configuration. Rollback restores the reuse of marked rows and the defect with it.
