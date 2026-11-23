# Proposal

## Why

A Guest cannot be invited to an AI project (`ai-agents`, "Members of the AI rooms"), yet a member converted to
Guest stays in every project as a Viewer, with the threads and files the member created there. Those files
are meant for their creator alone (`ai-agents`, "Privacy of the contents of a project"). A Guest cannot chat,
so they keep the project's space taken for nothing. Losing one's place in a project already deletes these
threads in the other cases ("Losing access to a project deletes the member's threads", "Deleting a profile
deletes its project threads"); the conversion to Guest is the last case that keeps them.

## What Changes

- `startUserTypeUpdate` with the type Guest removes the user from every AI project of the portal and deletes
  the threads the user created in AI projects, as `aiThreadsDelete` does: the thread is closed at once and its
  files are removed by the background pass, which returns their size to the project's space. A project the
  user owned still passes to the account named in the request first; the role the user is left with there is
  removed too.
- In AI agents (the AI room type of the Rooms section) the converted user still becomes a Viewer, as today.
- When the job fails at the new step, it fails as a whole and the type stays as it was. Starting it again
  marks only the threads still live, so the repeat does no harm. A cancel after the step leaves the user
  with the old type and without the projects, as it leaves the rooms already passed to the other account.
- A Guest is never allowed to use AI in a folder, whatever share the Guest holds. This only guards the
  project routes of Files; every AI service route already refuses a Guest by the user type.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-agents`: found by `startUserTypeUpdate` (`POST api/2.0/people/type`) together with the requirements
  "Members of the AI rooms", "Losing access to a project deletes the member's threads", "Deleting a thread of
  a project" and "An AI agent as a room: the deltas", and by the thread and file routes the scenarios read
  (`aiThreadsGetById`, `getThreadArtifacts`, `getFileInfo`, `getRoomSecurityInfo`, `getFolderInfo`). One
  requirement is added: converting a member to Guest deletes the member's place and threads in the projects.
  The requirements of `user-type-permissions` and `people-and-groups` about who may convert whom and how the
  job is watched or cancelled stay as they are.

## Breaks current behaviour

None. No requirement is modified or removed:

- No requirement says what a converted Guest keeps in an AI project. "Members of the AI rooms" says a Guest
  holds no role in a project, which the change makes true after a conversion as well.
- "An AI agent as a room: the deltas" accepts a Guest as a Viewer of an agent; the conversion still leaves
  the user a Viewer there.
- "Losing access to a project deletes the member's threads" and "Deleting a profile deletes its project
  threads" list other ways a member leaves a project; the new requirement adds one next to them.

Who is affected:

- the web client (`../client`): nothing to change; the deleted threads were shown only to their creator, who
  as a Guest could not open a chat anyway, and the project no longer lists the user among its members;
- the published SDKs and the OpenAPI document: no operation changes its shape or its status codes;
- webhooks: `user.updated` is raised as before; nothing is added;
- socket events: none added; the step that set the projects to Viewer sent none either;
- backup and restore: nothing new is stored; the deletion mark of a thread is already declared;
- the portal migration and existing stored data: no schema change. Guests converted before the change keep
  their Viewer share in the projects and their threads.

No row of `openspec/deviations.md` is touched.

## Impact

- **Type conversion job** (`ASC.Data.Reassigns`): the step that sets the AI agents and the AI projects of a
  new Guest to Viewer now sets only the agents, and a new step removes the user from the projects.
- **Files service**: a new method removes every share of a user on AI projects and marks the user's project
  threads deleted in one transaction; the security check of using AI refuses a Guest.
- **AI worker**: no change; the background pass of `aiThreadsDelete` removes the files.
