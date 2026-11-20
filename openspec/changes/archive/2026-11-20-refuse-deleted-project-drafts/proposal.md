# Proposal

## Why

A draft of a file stored in an AI project that its creator deleted can be attached again: `aiAttachmentsSaveFilesMany`
returns the deleted draft as if it were live. The next background pass still removes that draft together with the
file, so a message sent with it after the pass carries an attachment whose file is gone and whose record no longer
binds.

## What Changes

- **BREAKING** `aiAttachmentsSaveFilesMany` (and `aiAttachmentsSaveFile`, which goes through the same backend call)
  for a file the caller stored in an AI project answers 404 when the caller deleted the draft of that file, from the
  delete until the background pass; after the pass it answers 404 already, because the file is gone. Nothing is
  stored for the batch.
- A repeated call for a file whose draft was not deleted keeps returning the same draft, as today.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-agents`: requirement "Attachment of a file stored in a project" (`aiAttachmentsSaveFilesMany`, Node route
  `POST /api/2.0/ai/attachments/save-files-many`, backend `POST ai/attachments`) gains the case of a deleted draft.
  Found through the operationIds `aiAttachmentsSaveFilesMany`, `aiAttachmentsDelete` and `aiAttachmentsDeleteMany`;
  the requirements "Reading and deleting attachments" and "Drafts that are not sent are removed" are read but stay as
  they are.

## Breaks current behaviour

- "Attachment of a file stored in a project": today a repeated call for the same file returns the same attachment in
  every case; after this change a repeated call after the caller deleted that draft answers 404 instead of 200.
  - Web client (`../client`): not affected. A device upload goes into the project under a new file id each time
    (`ai-agent/providers/files/upload-files.ts`); a file of the portal attached in a chat of a project is copied
    into the project under a new id by the chat service each time (`ASC.AI.Chat/app/storage/projectAttachments.ts`,
    `copyIntoProject`); the root of a project is listed nowhere, so no picker offers its files; the editor
    (`doceditor/src/utils/aiAttachFile.ts`) creates a draft and deletes it without attaching it again; "Analyze
    responses" (`use-attach-to-chat.ts`) clears the draft and attaches the same portal form again, which is copied
    again under a new id. No path sends the id of a project file after deleting its draft.
  - Chat service (`ASC.AI.Chat`): no code change; it forwards the status of a backend error to its caller
    (`controllers/_helpers.ts`, `clientError`), so the backend's 404 reaches the caller as 404. Its OpenAPI texts of
    the save operations do not promise that a repeated call returns the same draft, so they stay.
  - Published SDKs and the OpenAPI document of the backend: the backend route is internal and hidden from the
    document; nothing to regenerate.
  - Webhooks, socket events: none are raised by saving or deleting a draft.
  - Backup and restore, portal migration, stored data: no schema change. Drafts deleted before the update are
    refused the same way, since the delete already wrote the same mark.
  - Direct API callers (SDK users, agents using the published tool definitions) that delete a draft and attach the
    same project file again get 404 where they got 200; before this change that 200 led to a message without its
    file after the next pass.

## Deviations

No row of `openspec/deviations.md` is touched.

## Impact

- `products/ASC.AI/Server/Service/AttachmentsStorageService.cs` (`CreateManyAsync`): the reuse of a draft of a project
  file refuses a deleted one before anything is written.
- `common/ASC.AI.Integration/Attachments/AttachmentsStorage.cs` and `Database/AttachmentQueries.cs`: the mark of a
  deleted draft is named once and used by the delete and the check.
- Integration tests in `products/ASC.AI/ASC.AI.Tests/Tests/ThreadAttachmentsTests/`.
