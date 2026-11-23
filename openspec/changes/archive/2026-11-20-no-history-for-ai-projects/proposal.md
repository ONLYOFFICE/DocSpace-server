# Proposal

## Why

An AI project is a set of private workspaces: what a member stores in it is visible to that member only, while the
project itself only carries its settings, members, linked rooms, skills and model. A shared activity history does not
fit that model, so an AI project and the entries inside it get no history at all.

## What Changes

- **BREAKING** `getFolderHistory` of an AI project and of a folder inside one, and `getFileHistory` of a file inside
  one, answer 403 for every caller, the project's owner and DocSpace admins included.
- **BREAKING** `createReportFolderHistory`, `getReportFolderHistory` and `terminateReportFolderHistory` answer 403 for
  an AI project and for a folder inside one.
- The `security` of an AI project reports no right to read or export its history.
- Actions on an AI project and on entries inside it no longer write an entry history and no longer send the
  `update-history` socket event. They are still recorded in the portal audit trail, unchanged.
- An action that copies or moves an entry between an AI project and another place keeps writing the history of the
  other place.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `history-and-audit`: requirements "Who may read an entry history" (`getFileHistory`, `getFolderHistory`) and "Who
  may export a folder history" (`createReportFolderHistory`, `getReportFolderHistory`,
  `terminateReportFolderHistory`) gain the AI project exception; a new requirement "An AI project has no history"
  states it. Found through the operationIds above and the routes `files/file/{fileId}/log`,
  `files/folder/{folderId}/log` and `files/folder/{folderId}/log/report`. "Actions that produce a history entry" and
  "What the audit trail records" are read and stay as they are: the audit event is still written, only its history
  references are not.
- `room-types` ("Rooms of the AI rooms section") is read and stays as it is; the AI rooms row of the section map in
  `.claude/rules/files-sections.md` gets the new rule.

## Breaks current behaviour

- "Who may read an entry history": today every caller who can read an AI project gets its history with 200, and the
  author of an entry inside a project gets that entry's history with 200. After the change both answer 403.
- "Who may export a folder history": today every non-guest reader of an AI project may start, read and stop a report of
  it. After the change these calls answer 403 for a project and for a folder inside one.
- Who is affected:
  - Web client (`../client`): `helpers/info-panel/tabs.ts` adds the History tab for every room of the AI rooms section
    and every entry, without looking at `security.ReadHistory`; for an AI project and its entries the tab would show
    a 403. The client has to hide the tab for them (follow-up for the frontend; the server change does not wait).
  - Published SDKs and the OpenAPI document: no schema change; the remarks of the five operations name the new 403.
    The OpenAPI documents and the C# SDK package are regenerated (4b35bd28d8); the SDK submodules are not changed.
  - Socket events: `update-history` is no longer sent to `{tenant}-DIR-{projectId}` nor to the rooms of entries inside
    a project. No other event changes.
  - Webhooks: none are raised by history.
  - Backup and restore, portal migration: no schema change. History references written for AI projects before the
    change stay in the table and are restored with it, but no endpoint reads them any more.
  - Existing stored data: the AI rooms section is not released yet; development portals keep their old references,
    unreachable through the refused endpoints.
  - Direct API callers (SDK users, agents) that read the history of an AI project get 403 instead of 200.

## Deviations

No row of `openspec/deviations.md` is touched.

## Impact

- `products/ASC.Files/Core/Helpers/FilesMessageService.cs`: no history references for the project and the entries
  inside it, in the single-entry path and on each side of a move or copy.
- `products/ASC.Files/Core/Core/Security/FileSecurity.cs`: no `HistoryExport` right on an AI project. `ReadHistory` and
  `EditHistory` are rights of a file and already false inside a project for everyone but the file's creator.
- `products/ASC.Files/Server/ApiModels/ResponseDto/HistoryDto.cs` (`HistoryApiHelper`): the history and report checks
  refuse an AI project and the entries inside it.
- Remarks and `[SwaggerResponse(403)]` texts of `getFileHistory`, `getFolderHistory` and the three report operations.
- Integration tests in `products/ASC.Files/Tests/Tests/03_Rooms/AiHub/` and `05_Features/Socket/`.
- `.claude/rules/files-sections.md`, AI rooms row.
