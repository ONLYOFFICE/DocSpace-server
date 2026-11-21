# Proposal

## Why

The AI agent of 4.0 (a room of the AI room type with a Knowledge and a Chat outputs folder) is replaced by the AI
project with linked Knowledge and Instructions rooms. Portals still hold agents with knowledge files, chat outputs,
members and chat threads, and once the agents are hidden from users (a separate change) that data is out of reach
unless it is carried into the new model. A backup archive made before the conversion brings agents back on restore,
so the conversion cannot be a one-time step of the release.

## What Changes

- A background pass converts every AI agent of an active portal, with no request from a user:
  - a new AI project takes the agent's title, owner, colour, cover, logo, custom tags and the pins of its users;
  - the agent's room keeps its id and becomes a Knowledge room titled `<agent title> (<title of its Knowledge
    folder>)`, linked to the project, with the knowledge files moved into the room itself; an agent whose Knowledge
    folder holds no file is deleted instead;
  - the threads started in the agent, its Chat assignment and the tool preferences and MCP server entries scoped to
    it move to the project;
  - the agent's Chat outputs folder moves to the My documents of the agent's owner as `<agent title> (<title of the
    Chat outputs folder>)`; an empty one is deleted.
- Access is carried over by fixed rules: Room manager and Content creator keep their role in the project, a Viewer
  becomes a Content creator, the members of an invited group become Content creators one by one, Guests and
  invitation links are left out. In the Knowledge room Room managers and Content creators (users and groups) keep
  their role; Viewers and invitation links are removed and read the room through the project.
- The pass finds its work from the data: it checks the portals changed since its previous pass (and every portal on
  its first pass) for rooms of the AI room type, so a portal restored or transferred from an archive with agents is
  converted by the next pass without a step in the backup service.
- An agent that fails stays an agent and is retried on the next passes; after a configured number of failed passes
  the portal is left alone until its entry is removed from the state of the pass by hand.
- A new internal route `POST internal/ai/agents/migration` (portal owner or DocSpace admin) runs the conversion for
  the caller's portal at once. It exists as the entry point the integration tests drive the conversion through; it
  is not a product feature and no user interface calls it.
- The stored prompt of an agent (`chatSettings.prompt`) is not carried over: it already reaches no model.
- No letter, no new audit action. Open clients get the usual folder events for the created project, the changed
  room and the moved folder.
- Out of scope: hiding the agents section and closing `createRoom` for the AI room type (a separate change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-agents`: new requirements for the conversion of an AI agent into an AI project (what becomes of the room, the
  knowledge, the threads, the model, the members, the Chat outputs), for the background pass that finds and retries
  it, and for the route that starts it. Found through `createRoom` with the AI room type (requirement "An AI agent
  as a room: the deltas"), `getProjectSources` and `linkProjectSources` ("Sources of an AI project"),
  `getProjectModel` ("The model of an AI project"), `getRoomSecurityInfo` and `setRoomSecurity` ("Members of the AI
  rooms", "Users only in an AI project", "Access through a project"), `aiThreadsList` ("One thread list per user",
  "Thread list of an AI project"). All of them are read and stay as they are.

## Breaks current behaviour

No requirement is modified or removed: every route answers as the current specs say for the room type it is called
on. What changes is the stored data, so a caller meets other objects under the ids it knew:

- The id of an agent answers as a Knowledge room (`getRoomInfo`: the Knowledge room type, the new title) or, for an
  agent without knowledge files, 404. The ids of the agent's Knowledge and Chat outputs folders answer 404; the
  knowledge files keep their ids and report the room as their folder; the Chat outputs content keeps its ids and
  reports a folder in My documents.
- A thread started in an agent was listed by `aiThreadsList` for any `entityId` ("One thread list per user"). After
  the conversion it is a thread of the project: listed only with the project id as `entityId` ("Thread list of an AI
  project"), and it follows the rules of project threads from then on ("Losing access to a project deletes the
  member's threads", "Deleting a profile deletes its project threads").
- A Viewer of an agent may now upload into the project and start threads there as a Content creator, and loses the
  role in the room that holds the knowledge. A Guest and a group lose the agent; the members of the group keep access
  as project members, fixed at the moment of the conversion.
- The files of Chat outputs are charged to the personal quota of the agent's owner, with no check of the limit, and
  the other members of the agent no longer reach them.
- Web client (`../client`): no route or field changes. The agents section turns empty for a converted portal; the
  client change that hides it belongs to the separate change.
- Published SDKs and the OpenAPI document: unchanged. The new route is internal and not published.
- Webhooks: none are raised; the conversion does not go through the API actions that raise them.
- Socket events: existing events only (`create-folder` for the project and the moved folder, `update-folder` for the
  room, `delete-folder` for a deleted agent).
- Backup and restore: no new table or column; the one schema change is an index on `ai_integration_threads`. An archive made before the conversion restores agents; the
  background pass converts them, so between the end of the restore and that pass the portal still holds agents.
  The pass depends on the restore and the transfer setting the portal's last-modified time, which both do today.
- Portal migration (data import): not affected, it creates no agents.
- Existing stored data: converted as described; the legacy settings row of the agent (provider, prompt) stays with
  the room and is read by nothing.

## Deviations

No row of `openspec/deviations.md` is touched. S11 (the agent quota endpoints accept other rooms) and Q1 (the default
agent quota) describe routes this change does not call; a Knowledge room made from an agent keeps the quota value the
agent had.

## Impact

- `products/ASC.AI/Worker`: a new background service next to `DeletedThreadsCleanerService`; configuration keys for
  its switch, period, window overlap and attempt limit, with the test profile in `common/ASC.AppHost/Configuration`.
- `products/ASC.AI/Server`: the internal route; `Migration/LegacyAiProviders*` moves to `products/ASC.AI/Core` so
  the worker can run it for a portal before converting its agents.
- `products/ASC.Files/Core`: the conversion service, one new `IFolderDao` operation that does the conversion in one
  transaction, new compiled queries in `Core/EF/Queries`, `DbPreference` added to `FilesDbContext`.
- `common/ASC.Core.Common`: a compiled query for the active portals changed since a moment and for paging over all
  active portals; a settings class for the state of the pass.
- One schema migration, `MigrationContext_Upgrade110` (MySQL SaaS): the index `(tenant_id, entry_id)` on
  `ai_integration_threads`. No change in `common/ASC.Data.Backup.Core`.
- OpenSearch: the vectors of the knowledge files in `files_embeddings` are not touched. The full-text documents of the
  project, the room and the knowledge files are updated through the usual index events, which Files.Worker applies.
- Tests: `products/ASC.AI/ASC.AI.Tests` (conversion scenarios through the internal route), unit tests of the
  candidate window and the attempt limit, a unit test in `common/Tests/ASC.Data.Backup.Core.Tests` pinning the
  last-modified time a restore writes.
