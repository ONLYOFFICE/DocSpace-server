# Design

## Context

See `proposal.md` for the motivation. The behaviour this design relies on:

- `ai-agents`, "Losing access to a project deletes the member's threads": a share removal on a project and
  the marking of the member's threads there are done together, in one transaction.
- `ai-agents`, "Deleting a thread of a project": a marked thread is closed at once; the background pass of
  the AI worker removes its two folders with their files, returns their size to the project's space and
  deletes the row. Nothing in this change touches that pass.
- `ai-agents`, "Deleting a profile deletes its project threads": the data jobs mark the user's threads in
  every AI project of the portal, found by the type of the thread's entry, not through the shares.
- `ai-agents`, "Members of the AI rooms": a Guest holds no role in a project; the invitation of a Guest is
  refused.
- `people-and-groups`, "Cancelling a type conversion": the job can be cancelled between its steps.

How a conversion to Guest runs today, read in the code. `startUserTypeUpdate` queues the conversion job. For a
user who is not a Guest yet, the job first passes every room the user owns, AI agents and AI projects
included, to the account named in the request; the former owner is left a Content creator share in each. For
the type Guest it then sets every share of the user on AI agents and AI projects to Viewer in one update, and
changes the type last. Nothing looks at the threads.

The security check of using AI on a folder lets a Room manager or a Content creator through and does not look
at the user type. The AI service routes refuse a Guest by the user type before they reach it; the routes of
Files that reach it for a project (an upload or a copy into the project, the file lists of a thread) do not.

## Goals / Non-Goals

**Goals:**

- Take a user converted to Guest out of every AI project and mark the user's project threads, so the existing
  background pass removes them.
- Keep the agents as they are: the converted user stays a Viewer there.
- Refuse AI to a Guest in the security check itself.

**Non-Goals:**

- Guests converted before this change. They keep their Viewer share and their threads.
- A file uploaded into a project and never sent. It is removed with its attachment by a change of its own, as
  for the other ways of leaving a project.
- Telling anyone through a socket event, an audit record or a webhook. The step it replaces sent none, and
  the other ways of leaving a project send none for the threads.

## Decisions

**One new step removes the shares and marks the threads in one transaction.** The step that set the agents
and the projects to Viewer now sets only the agents. A new data access method deletes every share of the user
on folders of the AI project type and marks every live thread the user created whose entry is an AI project,
in one transaction of the Files database context, as the removal from one project does. Alternatives: setting
the project shares to none and calling the marking of the profile deletion separately, which leaves a window
where the user is out of a project and the threads are still live, or the other way round; a call of the
one-project removal for each project, which needs the list of projects read first and gives nothing more.

**All the user's project threads are marked, not only those of the projects with a share.** After the step
the user holds no role in any project, and the projects the user owned have already passed to another
account. So every project thread of the user belongs to a project the user is no longer in. This is the same
query the profile deletion uses, and it also finds a thread in a project whose share was removed earlier
without its threads.

**The step runs after the rooms are passed on.** Passing a project to the other account leaves the former
owner a Content creator share there; running after it, the step removes that share too. It runs before the
type is changed.

**A failure fails the job.** The step is not caught: the job ends as failed, as for its other steps, and the
type stays as it was. A repeat removes nothing more and marks only the threads still live. A cancel after the
step leaves the user with the old type and without the projects; the rooms passed on before stay passed on
too, so the job keeps one rule for all its steps.

**The security check refuses a Guest on any folder.** It is checked before the role, for every folder type.
Every AI service route already refuses a Guest by the user type, and a Guest can hold only a Viewer role in an
agent, which the check refuses anyway; so the refusal changes behaviour only on the project routes of Files.
It is a second guard behind the refused invitation and the new step, kept without a test or a scenario of its
own: the test of the refused invitation is the one that keeps a Guest out of a project.

## Risks / Trade-offs

- [A cancel after the new step deletes the threads of a user who keeps the old type] → The threads are the
  user's own; the job behaves the same for the rooms it has already passed on.

## Migration Plan

No schema or data change. The change ships with the code; rolling back restores the Viewer step, and threads
marked meanwhile are removed by the background pass either way.
