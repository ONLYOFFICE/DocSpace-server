# Design

## Context

See `proposal.md` for the motivation. The constraints that shape the approach:

- An AI project is a room of the AI rooms section. Folders and files cannot be created in it through the
  public Files API, and its members are users only (`ai-agents`, "Members of the AI rooms": a group cannot be
  invited, a Guest holds no role). Membership is therefore an own share on the project or its ownership, and
  it can be lost only through the Files service.
- A thread is stored by the AI integration library, which the Files core references; the library cannot
  reference the Files core back. Code that touches both a thread and a folder lives in the Files core or in
  the AI service.
- A thread may be started in a project only by a caller who may chat there (`ai-agents`, "Where a thread may
  be started").
- The AI integration tables exist only in the MySQL SaaS migration set.

## Goals / Non-Goals

**Goals:**

- Keep the files a project thread produces and receives inside the project, per thread, counted in the
  project's and the portal's space and not in the personal quota of the member.
- Make them private to the creator of the thread, against the project's owner and DocSpace admins too.
- Remove them with the thread, and with the member's place in the project, without doing unbounded work in
  a request.

**Non-Goals:**

- Telling the creator about a new artifact or attachment through a socket event. The two lists are public
  and refresh only on a new request; the event is a change of its own.
- Removing a member's threads when the profile is deleted or the user becomes a Guest. Both paths end the
  membership outside the removal of a share and are separate changes.
- Deleting the file of an attachment that was never sent. Today deleting such an attachment, or its expiry,
  removes the attachment and leaves the file in the project.

## Decisions

**Two plain folders per thread, directly in the project, linked from the thread.** The thread row has two
nullable folder ids, filled when the first artifact is saved or the first attachment is sent. The thread
answer does not carry them; they stay visible as the `folderId` of each listed file, which is accepted.

**The folder and its link are written in one transaction.** A dedicated DAO method inserts the folder and
writes its id to the thread only if the column is still empty and the thread is not marked deleted; when the
write changes no row the insert is rolled back. A lock per thread serializes the callers, so three artifacts
saved at once end in one folder.

**One deny rule, keyed on the creator of the entry.** The entry filter refuses every entry inside a project
(not the project folder itself) unless the caller created it, and allows the creator Read, Download and Copy
only. The rule stands before the bypass for DocSpace admins and before the full-access check, which is how it
covers the project's owner and admins. Alternative rejected: keying the rule on the author of the thread,
which would need a thread lookup for every entry checked.

**Artifacts are written through the AI service, both sets are read through the Files service.** The write is
a storage route called by the chat service on behalf of the creator; the lists are public operations of the
Files service so that a client reads them without a proxy. Artifacts are saved through the DAO in the
creator's request, so their creation raises no socket event, "new" mark, audit record or webhook.

**An attachment from the device is an ordinary chunked upload into the project.** The session is allowed for
whoever may chat in the project, checked instead of the create right. The attachment record is created in the
transaction that stores the file, so a file never lies in the project without one; attaching the file later
fills that record instead of adding another. The upload keeps its ordinary side effects. Alternative
rejected: a dedicated "upload and attach" route.

**A portal file is copied, never referenced.** A copy into the project is allowed for whoever may chat there
and always produces a new file. The server copies nothing by itself. Alternatives rejected: a synchronous
copy inside the attach call; a background copy after the message is sent (the attachment list is read from
the folder, so the file would be missing right after the send).

**A file waits in the project and moves on send.** Until its attachment is linked to a message the file lies
directly in the project; linking moves it into the attachments of the thread. Deleting a message leaves the
file with the thread.

**Deleting a project thread is a mark, the work is done later.** The request sets a deletion time on the
thread and answers; every read filters marked threads out. A background service of the AI worker walks the
marked threads of all portals, deletes the files of both folders in one batch, subtracts their size from the
folder counters in one call for the files the batch really deleted, deletes the folders and then the row;
messages and attachment records go with the row. Only a project thread is deleted this way, a thread
elsewhere has nothing but rows and is deleted in the request as before. The delete checks authorship only: a
member may delete own data even without access to the project. Alternatives rejected: deleting the files in
the request (a thread may hold any number of files); a new table as a deletion queue; walking the folders of
project roots without a mark (a full scan, and it assumes every folder in a project is a thread folder).

**A write that races with the mark loses.** Saving a file into a thread folder, and moving one into it,
touches the live thread in the same transaction with a conditional update; when the thread is already marked
the transaction is rolled back and the caller gets 404.

**Losing the membership deletes the member's data in the project.** Removing the member's share on the
project and marking the member's threads of that project happen in one transaction of the Files context, so
there is no moment when the share is gone and the threads are alive. The owner of the project is never
removed this way. Nothing is deleted when access is lost without the membership: the AI features are switched
off, the project changes its owner and the former owner stays a member, the account is disabled.
Alternatives rejected: keeping the data of a member who left (the project's quota stays taken by files nobody
can open); moving it to the My documents of the project's owner (it breaks the privacy and lands in a
personal quota); a periodic check that the author is still a member; reacting to changes of group membership.

**Existing data.** A thread stored before the change has no folders: both lists answer an empty page and
nothing is created until the first file. The narrowed "Thread privacy" requirement changes nothing for rooms
of other types, and a removal from a project marks only the threads whose entry is that project.

## Risks / Trade-offs

- [A failure between deleting the files of a thread and subtracting their size] → the used space of the
  project stays too high. Accepted: the subtraction is a single call, and a repeat of the pass does not
  subtract twice, because it counts only the files that pass deleted.
- [The folder id of a file set is visible in `folderId`] → accepted: the folder answers 403 to everyone but
  its creator.
- [The races between the deletion mark and a write are not covered by an automatic test] → they are not
  deterministic; the conditional updates are the guard.
- [A participant can fill the project with files that are never sent] → they are private and take the
  project's space; removing them is the follow-up named under Non-Goals.

## Migration Plan

One migration adds the two folder columns, the deletion time and an index on it to the AI thread table (MySQL
SaaS). All three columns are nullable, and no existing row is changed. The backup declaration lists the two
folder links and the deletion time.

## Open Questions

- An edit right for the creator inside a project. "New" marks and the activity digest for the files of a
  thread are decided with it.
- Deleting a single artifact by its creator, which needs the delete right.
