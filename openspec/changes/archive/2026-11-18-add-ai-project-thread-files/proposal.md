# Proposal

## Why

A chat in an AI project produces files (documents and pictures the chat tools create) and takes files (what the
user sends with a message), and until now neither had a home in the project: a tool saved its result into the
folder the chat was opened from or into My documents, and an attachment stayed a reference to a file somewhere
else in the portal. A project needs both kept with the thread they belong to, private to the member who wrote
the thread, counted in the project's space and removed together with the thread.

## What Changes

- A thread of an AI project keeps two sets of files inside the project: **artifacts** (saved by the chat tools)
  and **attachments** (sent with its messages). Both are read through two new operations of the Files service,
  `getThreadArtifacts` and `getThreadAttachments` (`GET api/2.0/files/rooms/{id}/threads/{threadId}/artifacts`
  and `.../attachments`), by the creator of the thread only.
- A new storage route of the AI service, `POST internal/ai/threads/{id}/artifacts`, saves an artifact into the
  caller's project thread.
- A project member who may chat in the project uploads an attachment into the project through a chunked
  session (`createUploadSessionInFolder`) and copies a portal file into it with `copyBatchItems`. Before, both
  answered 403 for a project.
- Linking an attachment to a message of a project thread moves its file into the attachments of that thread.
- Everything inside a project is private to its creator: other members, the project's owner and DocSpace admins
  are refused, and the creator may only read, download and copy. The contents of a project are never listed.
- Deleting a project thread answers at once; its files are removed afterwards by a background pass.
- **BREAKING** Removing a member's own access to a project deletes the threads that member created there,
  with their files. In every other room a removed member still keeps the thread.
- A portal archive carries the links of a thread to its two file sets, and a restore points them at the new
  ids.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-agents`: found by the thread routes (`aiThreadsDelete`, `aiThreadsGetById`), the attachment routes
  (`aiAttachmentsSaveFilesMany`, `aiAttachmentsLinkToMessage`) and the AI rooms routes
  (`api/2.0/files/rooms/{id}/...`). The requirements "Thread privacy" and "One thread list per user" change
  for AI projects (`aiThreadsList`); new requirements
  cover the files of a project thread, the two list operations, the artifact route, attachments uploaded or
  copied into a project, the privacy and the listing of a project's contents, the recipients of an upload event,
  the deletion of a project thread and the loss of membership.
- `backup-and-restore`: found by the backup declaration of the AI tables. The requirement "Contents of a portal
  archive" gains the restore of a thread's links to its file sets.

## Breaks current behaviour

- **`ai-agents`, "Thread privacy"** — "A member removed from a room SHALL still manage the thread they wrote
  there but is refused a send into it (403)". The delta narrows it to rooms that are not AI projects and adds
  the scenario "Member removed from an AI project". For an AI project it no longer holds: when the member's access
  to the project is removed, the threads the member created in that project are deleted, so reading or renaming
  one answers 404 and its files are removed by the background pass. Rooms of every other type keep the old
  behaviour. Who is affected:
  - the web client (`../client`): a member removed from a project no longer finds the threads written there
    in the chat of that project;
  - the published SDKs and the OpenAPI document: no operation changes its shape; two operations are added;
  - webhooks: none added or changed. An upload or a copy into a project raises the same `FileUploaded` and
    `FileCopied` webhooks as anywhere else;
  - socket events: none added. The `create-file` event of an upload into a project reaches the uploader only;
  - backup and restore: the two new links of a thread and its deletion mark are declared and restored;
  - the portal migration and existing stored data: the schema gains three nullable columns on the thread table;
    threads stored before the change have no file sets and none is deleted by the migration; a thread gets a
    file set when the first file is saved into it.

- **`ai-agents`, "One thread list per user"** — "`aiThreadsList` SHALL show the caller's threads as one list
  whatever `entityId` it carries: a thread started in room A is listed for room B, a folder, a third-party
  room, a project, a string that is no id and no entity at all". A project is no longer part of that one list:
  with a project as `entityId` the list holds the threads started in that project only, and those threads
  are in no other list (new requirement "Thread list of an AI project"). Every other `entityId` behaves as
  before. Who is affected: the web client shows a chat history per project; the SDKs and the OpenAPI document
  keep the shape of `aiThreadsList`; webhooks, socket events, backup and the migration are not involved;
  a thread stored before the change belongs to no project and stays in the one list.

No row of `openspec/deviations.md` is touched.

## Impact

- **Files service**: the two list operations; the access rule for entries inside a project; the listing of a
  project; the upload session and the copy into a project; the recipients of socket events and of the
  shared-users list; the removal of a project member.
- **AI service**: the artifact storage route; attachments of files stored in a project; moving a file on
  linking; marking a project thread deleted.
- **AI worker**: a background pass that removes the files and the row of a deleted project thread.
- **Database**: three nullable columns on the AI thread table and an index, in the MySQL SaaS migrations (the
  only migration set that holds the AI integration tables).
- **AI chat service** (the public routes under `/api/2.0/ai/*`): it already uses the storage level described
  here. It creates a thread with the project as its entry when the chat is opened in an AI project, sends the
  results of the document agent and generated pictures to `POST internal/ai/threads/{id}/artifacts` (keeping a
  file where it kept it before when the thread takes none), and copies a portal file attached in a project chat
  into the project through a chunked upload before attaching the copy. This change describes the storage level
  and the Files service; of the public chat routes it changes two requirements, "Thread privacy" and "One
  thread list per user".
- **Web client**: an upload from the device goes into the project through the chunked session and needs no
  change. The two lists are there to be shown next to an open thread.
- **Not in this change**: no socket event tells the creator about a new artifact or attachment, so the two
  lists refresh only on a new request (a change of its own follows); the file of an attachment that was never
  sent stays in the project after its attachment is deleted.
