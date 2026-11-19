# Proposal

## Why

The AI rooms section has to show, for a Knowledge or an Instructions room, which AI projects use it: a number in
the listing ("Used by") and the projects themselves in the information panel. Today the link is readable from the
project side only (`getProjectSources`), so a client would have to read the sources of every project to answer it.

## What Changes

- A Knowledge or an Instructions room returned as a room object carries `linkedProjectsCount`: the number of AI
  projects the room is linked to. It counts every such project, whoever asks. Other room types do not carry the
  field.
- New operation `getRoomProjects` (`GET api/2.0/files/rooms/{id}/projects`): one page of the projects that use the
  room, each with `id`, `title` and `logo`, sorted by title. It lists only the projects the caller may read, so
  its total can be lower than `linkedProjectsCount`.
- Clients are told about a room whose number changes: when a project links it, unlinks it or is deleted.
- The requirement "Members of the AI rooms" is corrected: a DocSpace admin who is not a member of a project reads
  its model, sources and skills, as a DocSpace admin reads every room of the section. The server answers so
  today; the text and the test that expected 403 change, the product code of these reads does not.

Not in this change: sorting or filtering the section by the number, the webhook payload of a room, the chat
service (which refuses a round in a project to a DocSpace admin outside it).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-agents`: found by `getProjectSources`, `linkProjectSources`, `unlinkProjectSources`, `getProjectModel` and
  `getRoomSkills` (the link between a project and its rooms, and who reads it), by `getRoomsFolder` with
  `searchArea=AiHub` and `getRoomInfo` (the room objects that get the new field), and by `deleteRoom` on a
  project (the links go with it). "Members of the AI rooms" is modified; four requirements are added: the number
  on a room, the listing of the projects of a room, who is listed there, and the real-time event.

## Breaks current behaviour

- **"Members of the AI rooms"** (modified). The text said a DocSpace admin outside a project is refused its
  model, sources and skills with 403. The server already answers 200 to these three reads, so no caller sees a
  different status code, field or access after the change. Checked: the web client (no call changes its answer),
  the published SDKs and the OpenAPI document (the 403 of the three operations is still returned to a User or a
  Room admin outside the project, their texts stay true), webhooks (none involved), socket events (none
  involved), backup and restore, the portal migration and stored data (nothing stored changes).
- The rest is additive. A new field on the room object and a new operation reach the OpenAPI document and the
  SDKs when they are regenerated; a client that does not know the field ignores it. The `update-folder` event is
  an existing event, now also sent for the rooms a link change touches; a client that receives it re-reads the
  room as it does today. Backup and restore, the portal migration and stored data: no table or column is added,
  the links are read from the table they are stored in today.

No row of `openspec/deviations.md` is touched.

## Impact

- Files service: the room object (`linkedProjectsCount`), a new action in the AI rooms controller with its
  request and response models, the link service (events on link and unlink), the delete operation of a room
  (events when a project is deleted), new precompiled queries over the links.
- No database migration: the links are already indexed by room.
- Tests: `products/ASC.Files/Tests/Tests/03_Rooms/AiHub/`; the test of the three reads by a DocSpace admin loses
  its `Bug` mark and expects 200.
- OpenAPI documents and SDKs: regenerated after the change (new field, new operation), not part of it.
- Web client: reads the new field and calls the new operation when it builds the screens; nothing it does today
  changes.
