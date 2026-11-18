# Proposal

## Why

Losing one's place in an AI project deletes the threads one wrote there, with their files (`ai-agents`, "Losing
access to a project deletes the member's threads"), but only when a share on the project is removed through the
Files service. Deleting a profile ends the membership by another path, so the threads and files of a deleted
member stay in the project and keep its space taken. Deleting a profile and transferring a user's data also
hand the created files over to someone else, and for the contents of a project that new creator would be the
project's owner or the recipient, while those files are meant for their creator alone (`ai-agents`, "Privacy
of the contents of a project"). A transfer also hands a project the user owned to the recipient without leaving
the user any access to it, so the user's threads there would stay, readable by nobody.

## What Changes

- Deleting a profile deletes the threads the user created in every AI project of the portal, the projects the
  user owned included, as `aiThreadsDelete` does: the thread is closed at once and its files are removed by
  the background pass, which returns their size to the project's space. This holds for each way a profile is
  deleted: `startRemove`, `deleteMember`, `removeUsers` and `startReassign` with `deleteProfile` true.
- Neither deleting a profile nor transferring data changes the creator of a file or folder inside an AI
  project. The project itself still passes to the new owner as before.
- Transferring data without deleting the profile (`startReassign` with `deleteProfile` false) deletes the
  threads the user created in the projects the user owned, with their files, since those projects pass to the
  recipient and the user loses access to them, as the former owner of any room does on a transfer. In a project
  where the user is a member, the user keeps the share, the threads and their files, and the recipient cannot
  read them.
- When a job fails at the new step, it fails as a whole. A repeated `startRemove` or `startReassign` leaves the
  threads marked before as they are and marks only those still live, so the repeat does no harm.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-agents`: found by the thread routes (`aiThreadsGetById`, `aiThreadsDelete`), the file routes of a
  project thread (`getThreadArtifacts`, `getFileInfo` on an artifact) and the requirements "Losing access to a
  project deletes the member's threads", "Deleting a thread of a project" and "Privacy of the contents of a
  project". Three requirements are added: the deletion of a profile deletes its project threads, a transfer
  of data leaves the contents of a project with their creator, and a transfer deletes the former owner's
  threads in the projects it hands over. The operations that start them are those of
  `people-and-groups` (`startRemove`, `deleteMember`, `removeUsers`, `startReassign`); their requirements in
  that capability ("Starting a data removal", "Starting a data reassignment", "Deleting one disabled
  profile", "Deleting several disabled profiles") name rooms and accounts only and stay as they are.

## Breaks current behaviour

None. No requirement is modified or removed:

- "Starting a data removal" says the rooms of the user pass to the Owner; an AI project still does. "Starting a
  data reassignment" says the user's data is transferred and does not name the contents of a project. The
  contents of a project are files that the existing "Privacy of the contents of a project" keeps readable by
  their creator only, so leaving their creator unchanged is what that requirement already asks.
- "Losing access to a project deletes the member's threads" lists the share removals that delete threads; the
  new requirements add the deletion of a profile and the transfer of a project the user owned next to them
  without changing those cases.
- `startReassign` with `deleteProfile` false now deletes something: the user's threads in the projects the
  user owned. No requirement says a transfer keeps them; "Starting a data reassignment" speaks of the
  transfer, and these threads cannot be transferred without breaking "Privacy of the contents of a project".

Who is affected:

- the web client (`../client`): nothing to change; the deleted threads were shown only to their creator, whose
  profile is deleted or who no longer has access to the project;
- the published SDKs and the OpenAPI document: no operation changes its shape or its status codes;
- webhooks: `UserDeleted` is raised as before; nothing is added;
- socket events: none added; marking a thread deleted sends none, as for a member removed from a project;
- backup and restore: nothing new is stored; the deletion mark of a thread is already declared;
- the portal migration and existing stored data: no schema change. Threads of profiles deleted before the
  change are not touched.

No row of `openspec/deviations.md` is touched.

## Impact

- **Data removal and reassignment jobs** (`ASC.Data.Reassigns`): one more step after the personal data is
  deleted; in a transfer without deleting the profile, one more step before the creator of the user's folders
  is changed.
- **Files service**: two new methods mark the user's threads, one in every AI project of the portal, the other
  only in the projects the user owns; the method that removes a member from one project is unchanged. The steps that
  change the creator of files and folders during a removal or a transfer skip the contents of AI projects
  through their exclusion lists.
- **AI worker**: no change; the background pass of `aiThreadsDelete` removes the files.
- **Not in this change**: a file uploaded into a project and never sent stays there after its creator's
  profile is deleted or the project the creator owned is transferred, readable by nobody, as it stays after the
  member is removed from the project; deleting
  the file of an attachment that was never sent is a change of its own. The conversion of a member to Guest
  is a change of its own too.
