# Design

## Context

See `proposal.md` for the motivation. The behaviour this design relies on:

- `ai-agents`, "Losing access to a project deletes the member's threads": the member's share on the project
  is removed and the member's threads there are marked deleted together.
- `ai-agents`, "Deleting a thread of a project": a marked thread is closed at once; the background pass of
  the AI worker removes its two folders with their files, returns their size to the project's space and
  deletes the row. Nothing in this change touches that pass.
- `ai-agents`, "Privacy of the contents of a project": a file or folder inside a project is readable by its
  creator only. The rule reads the creator of the entry, so whoever becomes its creator reads it.
- `people-and-groups`, "Starting a data removal": the rooms of the user pass to the caller.

How a profile is deleted today, read in the code:

- `startRemove` queues the removal job with "delete the profile" set; the job deletes the profile at its end.
- `deleteMember` and `removeUsers` delete the profile and all the user's shares in the request, then queue the
  same removal job with "delete the profile" off: the job only cleans up the data of a profile that is already
  gone. So the removal job always runs for a deleted profile.
- `startReassign` queues the transfer job; with `deleteProfile` true it deletes the personal data first and
  the profile at its end.

Both jobs delete the personal data first, which also removes every share of the user, then change the creator
of the user's entries: the removal job gives a file or folder inside a room to the room's owner, the transfer
job gives every file and folder of the user to the recipient. Neither looks at AI projects. A transfer without
deleting the profile skips the personal data step and keeps the user's shares and personal folders.

Ownership of a room is the creator of its folder, not a share: creating a room writes no share for its owner.
Changing the owner of a room from the client gives the former owner a Room manager share, but the transfer job
only rewrites the creator of the user's folders, the rooms included, and gives the former owner nothing. So
after a transfer the former owner has no access to a room they owned, and their files there belong to the
recipient.

## Goals / Non-Goals

**Goals:**

- Mark the threads of a deleted profile in every AI project, so the existing background pass removes them.
- Mark the threads of a user in the projects that user owned when a transfer hands those projects over.
- Keep the creator of every entry inside an AI project unchanged through both jobs, with and without the
  deletion of the profile.

**Non-Goals:**

- The conversion of a member to Guest.
- A file uploaded into a project and never sent. It keeps its creator, who is deleted or has no access to the
  project any more, so nobody reads it; deleting it with its attachment is a change of its own.
- Telling anyone about the deletion through a socket event, an audit record or a webhook. Removing a member
  from a project sends none for the threads either.

## Decisions

**When the threads are marked.** The removal job marks the user's threads in every AI project, the projects the
user owned included, right after the personal data is deleted and before any creator is changed. It does so
whatever "delete the profile" says, because `deleteMember` and `removeUsers` queue that job for a profile they
have already deleted. The transfer job does the same when it deletes the profile. Alternatives: marking in the
`deleteMember` and `removeUsers` requests too, a second place doing the same for a job that follows anyway; a
periodic pass that looks for threads whose creator is no longer a member, rejected for the feature earlier in
favour of acting where the membership ends.

**The threads are found by the type of their entry, not through the shares.** When the jobs mark them, the
personal-data step has already removed the user's shares, and the owner of a project never holds one, so the
shares no longer tell which projects the user was in. For the same reason no share is removed. Alternative: an
optional project on the removal of "Losing access to a project", with a share removal for every project;
rejected, since in both jobs that removal would find nothing to remove.

**A transfer without deleting the profile deletes the user's threads in the projects the user owned.** The
threads, their artifacts and their attachments are the user's personal data in the project. A transfer takes
the project away from the user and cannot pass these files to the recipient, so they are deleted, the same way
as when a member loses a project. The step runs before the creator of the user's folders is rewritten, because
after that nothing tells which projects the user owned. In a project where the user is a member, the share
stays, so the user keeps access and nothing is deleted there, as with a user who is only disabled.
Alternatives: leaving the threads, which would keep the project's space taken by files nobody can read until
the profile is deleted or the recipient invites the user back; giving the former owner a Content creator share
on the project, as changing the owner from the client does, which would make the user a member nobody invited,
while a transfer leaves the former owner nothing in an ordinary room.

**The contents of a project keep their creator; the project itself passes on.** Both jobs leave out what lies
inside AI projects from the steps that change the creator, through the exclusion lists the transfer already uses
for the personal folders. For folders the list holds the folders lying directly in a project, not the project:
an exclusion also matches the folder itself, so the project would no longer pass to its new owner. For files it
holds the projects, so a file lying directly in the project root is skipped too. Alternative: a condition "no
ancestor is an AI project" in each step, which MySQL refuses in an update of the folder table, since it reads
that table again.

**A failure fails the job.** The new steps are not caught: the job ends as failed, as for any of its other
steps. For `startRemove` and `startReassign` the profile is deleted only at the end of the job, so it is still
there and the job can be started again; marking again skips the threads already marked, so a repeat changes
nothing that the first run did. When a transfer fails after the marking and before the creator is rewritten,
the user still owns the projects, so the repeat finds them again.

## Risks / Trade-offs

- [A transfer is meant to keep a disabled user's data, and it now deletes the user's threads in the projects
  the user owned] → They are files nobody could read after the transfer, the user included; a project the user
  is a member of keeps them.

## Migration Plan

No schema or data change. The change ships with the code; rolling back restores the previous jobs, and
threads marked meanwhile are removed by the background pass either way.
