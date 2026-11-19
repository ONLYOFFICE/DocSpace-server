# Proposal

## Why

A file the caller uploads or copies into an AI project for a chat message stays in the project, hidden from
everyone but its creator, until the message is sent. A draft removed with `aiAttachmentsDelete` or
`aiAttachmentsDeleteMany`, or never sent at all, leaves that file behind: nothing removes it, and it keeps
the project's space taken for good. Draft rows are removed in two places today, the delete routes and a
background pass for drafts nobody sent, and neither looks at the file.

## What Changes

- **BREAKING** `aiAttachmentsDelete` and `aiAttachmentsDeleteMany` no longer remove a draft themselves: they
  hand the caller's drafts that are not linked to a message to the background pass, which removes them at its
  next run (every 15 minutes by default). Until then the draft is still read by `aiAttachmentsGet` and
  `aiAttachmentsGetMany`.
- **BREAKING** An attachment already linked to a message is left as it is by both delete routes, as an
  unknown id is; it goes with its message or its thread.
- **BREAKING** The background pass removes, together with a draft, the file it was made from when that file
  is stored in an AI project and has not been sent yet, and returns its size to the project's space. A file
  outside a project is still left alone.
- **BREAKING** A draft that is not linked to a message within 2 hours is removed by default (24 hours before).
- The background pass is the only place that removes drafts. Nothing is sent to a socket, the audit trail or
  a webhook when it does.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-agents`: found by `aiAttachmentsDelete` and `aiAttachmentsDeleteMany` (`DELETE api/2.0/ai/attachments/delete`,
  `DELETE api/2.0/ai/attachments/delete-many`) in the requirement "Reading and deleting attachments", and by
  `aiAttachmentsGet`, `aiAttachmentsGetMany` and `aiAttachmentsLinkToMessage`, which read and link drafts in
  the requirements "Reading and deleting attachments" and "Linking attachments to a message". The files in a
  project come from `createUploadSessionInFolder` and `copyBatchItems` ("Uploading an attachment into a
  project", "Copying a portal file into a project") and are listed by `getThreadAttachments` ("A sent
  attachment is kept with its thread"). "Reading and deleting attachments" is modified; one requirement is
  added: drafts nobody sends are removed with their file in a project.

## Breaks current behaviour

"Reading and deleting attachments" is modified:

- A deleted draft is gone after the next background pass, not right after the call; until then a read
  returns it and a link accepts it. Before, it was gone at once. The test of bug 82767, which reads the draft
  right after the delete, is changed to wait for the pass.
- Deleting an attachment linked to a message changes nothing. Before, it removed the attachment from its
  message.
- A draft whose file is stored in an AI project and was not sent takes that file with it. Before, every draft
  left its file.

A draft nobody linked is removed after 2 hours instead of 24. No requirement states the lifetime; a link of
a draft already removed answers 404, as "Linking attachments to a message" says for an unknown attachment.

Who is affected:

- the web client (`../client`): nothing to change. It removes a chip from the composer and never reads that
  draft again, and it never deletes an attachment after the message is sent. A composer left for more than
  2 hours before sending meets the 404 of the link sooner than before;
- the published SDKs and the OpenAPI document: `aiAttachmentsDelete` and `aiAttachmentsDeleteMany` are
  operations of the chat service in the published contract. Their descriptions say the deletion is permanent
  and immediate and covers attachments bound to a message, so the texts change; the shapes and status codes
  do not. The contract and the SDKs pick the texts up when they are next regenerated;
- webhooks: none sent before or after;
- socket events: none. The file was announced to its creator when it was stored in the project, but no list
  shows the files of a project's root (`getFolderByFolderId` of a project is empty), so no client view goes
  stale;
- backup and restore: nothing new is stored;
- the portal migration and existing stored data: no schema change. Drafts already older than 2 hours are
  removed by the first pass after the update, with their files in projects.

No row of `openspec/deviations.md` is touched.

## Impact

- **AI service** (`products/ASC.AI`): the two delete routes mark the caller's unlinked drafts for the pass
  instead of deleting them.
- **AI worker**: the pass for unsent drafts reads its settings with bounds, sets the portal of each group of
  drafts and removes the drafts with their files in projects through the Files service.
- **Files service**: a new method removes drafts and their unsent files in a project in one transaction and
  returns the size to the project.
- **Chat service** (`common/ASC.AI.Chat`): the descriptions of the two delete operations.
- **Test stack**: the pass runs every 5 seconds in the `integration-test` profile.
