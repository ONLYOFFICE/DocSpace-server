# Design

## Context

See `proposal.md` for the motivation. The behaviour this design relies on:

- `ai-agents`, "Losing access to a project deletes the member's threads": the Files service removes the
  member's share on the project and marks the member's threads there deleted in one transaction of the Files
  database context. The method takes one project.
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

- The conversion of a member to Guest. It will use the same method in its "every project" form.
- A file uploaded into a project and never sent. It keeps its creator, who is deleted or has no access to the
  project any more, so nobody reads it; deleting it with its attachment is a change of its own.
- Telling anyone about the deletion through a socket event, an audit record or a webhook. Removing a member
  from a project sends none for the threads either.

## Decisions

**The removal job marks the threads in every case; the transfer job only when it deletes the profile.** The
step runs right after the personal data is deleted and before any creator is changed. In the removal job it
runs whatever "delete the profile" says, because `deleteMember` and `removeUsers` queue that job with the flag
off for a profile they have already deleted. Alternatives: calling the method in the `deleteMember` and
`removeUsers` requests too, which would add a second place doing the same for a job that follows anyway; a
periodic pass that looks for threads whose creator is no longer a member, rejected for the feature earlier
in favour of acting where the membership ends.

**One method, two forms.** The method of "Losing access to a project" takes the project as optional: with a
project it works as now; without one it removes the user's shares on every AI project of the portal and marks
every live thread the user created whose entry is an AI project, in one transaction. The threads are found by
the type of their entry, not through the shares: in both jobs the personal-data step has already removed the
user's shares, so they no longer tell which projects the user was in, and a project the user owned is found
the same way. The share removal of the "every project" form is for the Guest conversion, where the shares are
still there; here it removes nothing. The share removal is a compiled delete like its neighbours; the marking
is a bulk update written as not precompiled, with the comment the query rule asks for, as the one-project
marking is. Alternative: a separate method for the profile case, which would duplicate the transaction and
the marking.

**A transfer without deleting the profile marks the user's threads in the projects the user owns.** The
threads, their artifacts and their attachments are the user's personal data in the project. A transfer takes
the project away from the user and cannot pass these files to the recipient, so they are deleted, the same way
as when a member loses a project. The step runs before the job rewrites the creator of the user's folders,
because after that nothing tells which projects the user owned. It is one bulk update in the Files database
context: every live thread the user created whose entry is an AI project created by the user, with the same
"Not precompiled on purpose:" comment. No share is removed, since an owner holds none. The rule is the one of
the feature: a user's data in a project is deleted when the user loses access to the project. In a project
where the user is a member, a transfer without deleting the profile removes no share, so the user keeps access
and nothing is deleted there, as with a user who is only disabled. Alternatives: leaving the threads, which would keep the project's space taken by files
nobody can read until the profile is deleted or the recipient invites the user back; giving the former owner
a Content creator share on the project, as changing the owner from the client does, which would make the user
a member nobody invited, while a transfer leaves the former owner nothing in an ordinary room.

**The six creator-changing queries skip what lies inside an AI project.** The removal job's two queries for
folders and files inside rooms, and the transfer job's queries for all folders and all files with their two
"except personal folders" variants, get one more condition: the entry has no ancestor that is an AI project.
For a folder the ancestors exclude the folder itself, so the project folder still passes to its new owner as
other rooms do; for a file the ancestors start with its own folder, so a file lying directly in the project
root is skipped. The condition reads the folder tree by index, a few rows per entry, next to the walk over
the user's entries these queries already do. Alternative: pass the ids of all projects as excluded folders
to the existing "except" variants; their condition also matches the folder itself, so the project would no
longer pass to its new owner.

**A failure fails the job.** The new steps are not caught: the job ends as failed, as for any of its other
steps. For `startRemove` and `startReassign` the profile is deleted only at the end of the job, so it is still
there and the job can be started again; marking again skips the threads already marked and removing the
shares again finds none, so a repeat changes nothing that the first run did. When a transfer fails after the
marking and before the creator is rewritten, the user still owns the projects, so the repeat finds them again.

## Risks / Trade-offs

- [`deleteMember` and `removeUsers` delete the profile before the job runs, and the job sends no letter when it
  fails] → The threads then stay, readable by nobody, since their creator no longer exists and no step
  changed it. Accepted: every other step of that job fails the same way (the personal folders, the rooms and
  the files in rooms of the deleted profile stay as they were); the step runs early in the job, before the
  steps that touch rooms and files. Making that path repeatable is a matter for the whole job, not this change.
- [Between the request of `deleteMember` and the run of the job, the deleted user's threads are still live] →
  Only their creator could read them, and that profile is gone.
- [A transfer is meant to keep a disabled user's data, and it now deletes the user's threads in the projects
  the user owned] → They are files nobody could read after the transfer, the user included; a project the user
  is a member of keeps them.
- [The skip condition adds tree reads to queries that walk all of a user's files] → Bounded by the entries of
  one user, and those queries run in a background job.

## Migration Plan

No schema or data change. The change ships with the code; rolling back restores the previous jobs, and
threads marked meanwhile are removed by the background pass either way.
