# Design

## Context

See `proposal.md` - Why. The behaviour this design relies on:

- `history-and-audit`, "Who may read an entry history": the log endpoints decide by the Read right on the entry.
- `history-and-audit`, "Who may export a folder history": the report decides by the Read right minus guests.
- `history-and-audit`, "Actions that produce a history entry": an entry history is built from the audit events that
  reference the entry. An event references its entry and every non-root folder above it, so the history of a room
  or a folder holds the events of everything inside it.
- `ai-agents` and the AI rooms row of the section map: an entry inside a project is visible to its creator only, with
  Read, Download and Copy and nothing else, also against the project's owner and DocSpace admins.

Today an AI project is a room like any other for the history: its events and the events of the entries inside it
reference the project, the log endpoints answer by the Read right, and `update-history` is sent to the room of
every referenced entry.

## Goals / Non-Goals

**Goals:**

- No history references are written for an AI project or for anything inside one.
- Every history read and report of a project or an entry inside one answers 403, rows written before the change
  included.

**Non-Goals:**

- The portal audit trail. It keeps recording these actions with their titles, as it does for My documents; the
  user decided to keep it so (Known limitation in the PR).
- Removing history references already written for projects on development portals.
- The web client hiding its History tab for projects (frontend follow-up).

## Decisions

**Stop writing the references, and refuse the reads.** The references are what both the log and the
`update-history` event are built from, so not writing them closes both at once and keeps the table free of rows no
endpoint reads. Refusing only the reads was rejected: the event would still be sent for every action in a project.
Not writing only was rejected too: rows written before the change would stay readable, and the endpoints would
answer an empty 200, which the user did not want.

**Where the project is detected.** In the single-entry audit path the room of the entry is already found among its
parents (or is the entry itself); a room of type AI project means no references. In the move and copy path each side
is a chain of parents: a side whose chain or end folder is an AI project contributes no references, and the entry
itself is added only to a side that is not a project. The other side keeps its references, so copying a file out of
a project still shows in the history of the target folder.

**The reads are refused explicitly in the history helper, not through the `ReadHistory` right.** The log endpoints
decide by the Read right today; moving them to `ReadHistory` would change who may read every other history
(`ReadHistory` excludes Read members of rooms). The helper checks the entry instead: an AI project, or an entry for
which the existing "inside an AI project" test is true, is refused with 403 before anything is read.

**The report and the right flag go through `HistoryExport`.** The report endpoints already ask the `HistoryExport`
right, which the project's `security` object publishes. A rule next to the other AI project rules of the access
check returns false for `HistoryExport` on an AI project; entries inside a project already get false from the
creator-only rule, which allows Read, Download and Copy only.

## Risks / Trade-offs

- [The web client shows the History tab for every room of the AI rooms section and gets 403 for a project] → The
  proposal names the follow-up; the tab needs the project type or `security.HistoryExport`, both already sent.
- [An action whose audit path is not one of the two in the message service would still reference a project] → Every
  history reference is built in that service (the only writer of history references). The tests cover the
  single-entry path through a rename of the project and the move and copy path through copies into and out of it.
- [The portal audit trail still names the files of a project to the portal's admins] → Same as My documents, by the
  user's decision; recorded as a Known limitation.

## Migration Plan

No schema or data migration. References written for projects before the change stay; they are unreachable through
the refused endpoints and go away with the audit trail cleanup like any other row. Rollback restores the old reads
over the rows written before the change; rows are not written while the change is in place.
