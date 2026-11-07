# ai-agents Specification

## Purpose

Defines the AI product of a portal: the portal-wide AI switch and the readiness it reports, the model
profiles (provider + model + key/URL) and their catalogue, the portal-wide model assignments, the AI rooms
section with its Instructions rooms (skills), Knowledge rooms (vectorized knowledge) and AI projects (a fixed
chat model plus linked rooms), the chat threads and messages with streaming replies, regeneration and tool-call
approval, the per-user chat preferences, the MCP server registry and the tool switches, the web-search
configuration, the OpenAI-compatible passthrough, the editor tools, the chat attachments, the markdown export,
the metadata suggestions for a file, the vectorization of knowledge, the prompt library and the spending limits
on the AI services. The AI agent of 4.0 (a room of the AI room type with a Knowledge and a Chat outputs
folder) still exists as a room, but its own public routes were removed in 4.1; only its deltas against an
ordinary room are described here. Everything a room does (title, tags, logo, lifetime, watermark, archive,
pinning, quota mechanics, roles) is in `room-types` and `room-role-permissions`, the agent quota and the agent
count limit in `quotas`, the agent webhook events in `webhooks-and-api-keys`.

The public routes under `/api/2.0/ai/*` are served by the AI chat service, which keeps its data through the
internal storage routes of the DocSpace AI service; the settings, vectorization and export routes are the
DocSpace AI service itself; the AI rooms, projects, sources, model and skills routes are the Files service.
Sources: the .NET integration tests of the AI service (the public `/api/2.0/ai/*` surface and the storage
level), the Files, Web.Api and Core suites, the OpenAPI contract (operations named by `operationId`), the
product code where the tests stop, and — where no .NET test exists — the TypeScript API suite of the `ai`
area. Where a contract or test expectation and the server disagree this specification follows the server;
the evidence, the differences and the suspected defects are in `verification.md` next to this file.

## Requirements

### Requirement: Portal AI switch

The portal SHALL carry one AI switch, on by default, read by any signed-in member through
`getTenantAiAccessSettings` (anonymous 401) and changed through `setTenantAiAccessSettings` by the owner or
a DocSpace admin only (others 403, anonymous 401). While it is off every AI route this specification does
not name as ungated SHALL answer 403 before the target is looked up, and the AI rooms section with its rooms
and files SHALL answer 403 on every Files route.

#### Scenario: DocSpace admin turns AI off for everyone

- **WHEN** a DocSpace admin sets the switch off and a User then calls `aiProfilesList`
- **THEN** the User gets 403, and 200 again after the admin sets the switch on

#### Scenario: Room admin cannot flip the switch

- **WHEN** a Room admin, a User or a Guest calls `setTenantAiAccessSettings`
- **THEN** the system answers 403 and the switch is unchanged

#### Scenario: AI rooms are hidden while the switch is off

- **WHEN** AI is off and the owner reads an Instructions room, its contents or a skill file in it
- **THEN** every call answers 403, and all three answer 200 again once AI is switched on

#### Scenario: Switching back on restores everything

- **WHEN** a DocSpace admin switches AI on again after an off period
- **THEN** every stored setting and room is as it was, and the change is audited and pushed to clients

### Requirement: AI readiness reported to the portal

`aiSettingsGet` SHALL be readable by every signed-in member, Guests included, with AI on or off (anonymous
401), and report the same `aiReady`, `systemAiEnabled`, `vectorizationEnabled`, `vectorizationNeedReset`,
`embeddingModel`, `toolPermissionMode` and nullable `recommendedModelForForms` to every role. Hosted, the
first three SHALL be true only while the switch is on and the AI Tools service is paid for; self-hosted,
`aiReady` SHALL be true while the switch is on and a profile exists.

#### Scenario: Unpaid hosted portal

- **WHEN** the AI Tools wallet service is not enabled on a hosted portal and the owner calls `aiSettingsGet`
- **THEN** the answer is 200 with `aiReady` false, and true once the service is paid for

#### Scenario: Read while the switch is off

- **WHEN** AI is switched off and a member calls `aiSettingsGet`
- **THEN** the system answers 200 with `aiReady` false; switched on again, a portal without a profile still reads false

### Requirement: The AI Tools wallet service gates inference only

On a hosted portal the AI Tools wallet service SHALL gate the calls that run a model: `aiSendWithStream`,
`aiSendWithStreamOpenAI` and `aiRegenerateStream` SHALL answer 402 while it is off, storing nothing for the
refused turn; the gateway proxy paths SHALL answer 403. Assignments, tools, attachments, preferences,
vectorization requests and export SHALL stay available. A paid service with a zero or low AI credit balance
SHALL NOT stop inference.

#### Scenario: Send refused while unpaid

- **WHEN** the AI Tools service is off and the owner calls `aiSendWithStream` on a thread
- **THEN** the system answers 402 and `aiThreadsReadMessages` shows neither the question nor a failed answer

#### Scenario: Management surface stays open

- **WHEN** the AI Tools service is off and the owner registers a custom MCP server and reads the tools catalogue
- **THEN** every call answers 200

### Requirement: AI tools version of the editors

The build information of `GET api/2.0/settings/version/build` SHALL carry `aiTools`, the version of the AI
tools of the connected editors, next to the editors' own version; it SHALL be null when the editors have
their AI tools switched off or are too old to report it. Both values are read from the editors together and
cached together.

#### Scenario: Editors without AI tools

- **WHEN** the connected editors do not report an AI tools version and a member reads the build information
- **THEN** `aiTools` is null while the editors' version is still reported

### Requirement: Per-user AI chat settings

`aiSettingsGetUser` and `aiSettingsSetUser` SHALL read and store the caller's own
`chatRecommendedModelVisible` (true for a new user), usable by every member and while AI is off, never
touching another user's value or the portal settings. The body SHALL carry that exact name as a boolean; a
non-boolean, an empty object, a missing body or a non-object body SHALL be refused (400) and change nothing;
an unknown field next to a valid value is ignored; a 200 SHALL always mean the value was stored.

#### Scenario: One user's preference is private

- **WHEN** the owner stores false and a User reads their own settings
- **THEN** the User still reads true

#### Scenario: Non-boolean value

- **WHEN** a member sends `chatRecommendedModelVisible` as a string
- **THEN** the system answers 400 and the stored value is unchanged

### Requirement: Tool permission mode of a user

The caller's tool permission mode — `ask` (every host tool call waits), `auto` (the default: a pre-approved
tool or one that opts out of approval runs at once, one without the flag asks) or `allow` (nothing waits) —
SHALL be one value per user for every chat, written through `aiSettingsSetToolMode` or
`aiPreferencesSetToolPermissionMode` and read through `aiSettingsGetToolMode`,
`aiPreferencesGetToolPermissionMode` and `aiSettingsGet`; a new mode applies to the next tool call.

#### Scenario: Modes round-trip

- **WHEN** a User sets `ask`, then `allow`, then `auto`, and then saves another user preference
- **THEN** each value reads back through both read pairs and the other preference leaves the mode alone

#### Scenario: Mode steers the approval pause

- **WHEN** the mode is `allow` and the model calls a tool that requires approval
- **THEN** the tool runs without a pending tool call; in `ask` every host tool pauses for approval whatever its own flag says

#### Scenario: Modes are per user

- **WHEN** the owner sets `allow` and a User reads their own mode
- **THEN** the User still reads `auto`

#### Scenario: Value outside the three

- **WHEN** a member sets a mode that is not `ask`, `auto` or `allow`
- **THEN** the system answers 400 and the mode is unchanged

#### Scenario: Pre-approval in ask

- **WHEN** the mode is `ask` and the tool is on the caller's pre-approved list
- **THEN** the tool call still waits for approval

### Requirement: Gates on the tool permission mode routes

All four tool permission mode routes SHALL refuse a Guest (403) and answer 403 while the AI switch is off,
with one exception: validation comes first on the write, so an invalid value from a Guest or while AI is off is
a 400 (bugs 84303, 84302). The stored mode survives the off period.

#### Scenario: Guest on the two read routes

- **WHEN** a Guest calls `aiSettingsGetToolMode` and then `aiPreferencesGetToolPermissionMode`
- **THEN** both answer 403, while the Guest's own `aiSettingsGetUser` still reads the default `auto`

### Requirement: Profile catalogue

`aiProfilesList` and `aiProfilesGetById` SHALL describe every profile identically (id, name, provider type,
base URL, model id, capabilities, reasoning support) to DocSpace admins, Room admins and Users; Guests 403,
anonymous 401; malformed or empty id 400, unknown id 404. `aiProfilesGetById` SHALL carry neither the key
nor the headers. Hosted, the catalogue SHALL be the gateway's model list without embedding and restricted
models, served whole inside an AI project too.

#### Scenario: Guest cannot read the catalogue

- **WHEN** a Guest calls `aiProfilesList` or `aiProfilesGetById` with a real id
- **THEN** the system answers 403

#### Scenario: Restricted model leaves the catalogue

- **WHEN** a DocSpace admin restricts one model through `setRestrictedAiModels`
- **THEN** that profile disappears from `aiProfilesList` and reappears once the restriction is lifted

#### Scenario: Unknown id

- **WHEN** a member calls `aiProfilesGetById` with a well-formed id no profile carries
- **THEN** the system answers 404 with a message

#### Scenario: Key in the list for an administrator

- **WHEN** a DocSpace admin calls `aiProfilesList`
- **THEN** each profile carries its provider key, which the settings cards edit

### Requirement: Models offered by a stored profile's provider

`aiProfilesListModels` SHALL ask the provider of the stored profile named by `profileId` for the models it
currently offers, for a DocSpace admin, a Room admin or a User alike: a missing or malformed `profileId` 400,
an unknown one 404, a key the provider rejects 400, a provider that does not answer 502. A Guest, and any
caller while the AI switch is off, SHALL be refused (403) before the provider is dialled.

#### Scenario: Guest refused before the dial

- **WHEN** a Guest calls `aiProfilesListModels` with a real profile id
- **THEN** the system answers 403 and no request reaches the provider

### Requirement: Managing profiles

`aiProfilesCreate`, `aiProfilesUpdate` and `aiProfilesDelete` SHALL be allowed to DocSpace admins only
(403 for Room admins, Users and Guests, 401 anonymous, authentication before validation; AI off 403 before
the body is read). Hosted — the AI gateway configured — every write SHALL answer 403, the owner's included.
Self-hosted, the base URL is at most 2048 characters and the key at most 4096, an unusable provider type,
host or key is refused before any connection, and each write is audited.

#### Scenario: Owner on a hosted portal cannot add a profile

- **WHEN** the owner of a hosted portal calls `aiProfilesCreate` with a well-formed profile
- **THEN** the system answers 403 and the catalogue is unchanged

#### Scenario: Self-hosted admin round-trips a profile

- **WHEN** a DocSpace admin of a self-hosted portal creates a profile, reads it, updates it and deletes it
- **THEN** every field written is read back, the delete answers success and the read then answers 404

#### Scenario: Room admin cannot delete

- **WHEN** a Room admin calls `aiProfilesDelete`
- **THEN** the system answers 403 and the catalogue is untouched

#### Scenario: Unusable profile refused before any connection

- **WHEN** a DocSpace admin creates a profile with an unknown or `external` provider type, a loopback base URL or a key that cannot be an HTTP header
- **THEN** the system answers 400, nothing is stored and no socket is opened

### Requirement: Provider discovery

`aiProfilesListProviderModels` SHALL require `providerType` and `baseUrl` (400 naming exactly the missing
field), refuse an unknown provider type, a malformed base URL, a forbidden host and a key that cannot be an
HTTP header with 400 before any connection, report a key the provider rejects as 400 against the key, answer
502 for a provider that does not answer, and never echo the key or store a profile. Local provider types
need no key; Guests 403, AI off 403.

#### Scenario: Private address refused before dialling

- **WHEN** a member calls `aiProfilesListProviderModels` with a base URL on a loopback, link-local or metadata address, or with a non-http scheme
- **THEN** the system answers 400 and no socket is opened

#### Scenario: Wrong key

- **WHEN** a member sends a reachable provider URL with a key the provider refuses
- **THEN** the system answers 400 and the message names the key

#### Scenario: Local provider

- **WHEN** a member calls `aiProfilesListProviderModels` for a local provider type without a key
- **THEN** the key is never the reason for a refusal, and every model of any provider type carries a name

### Requirement: Connection check of a stored profile

`aiProfilesTestConnection` SHALL take a bare profile id or a `profileId` wrapper — missing or malformed
400, unknown 404 — probe the profile's provider and answer 200 with a flat `{field, message}` verdict that
leaks nothing; DocSpace admins, Room admins and Users SHALL get the same verdict, a Guest 403 (a malformed
id is refused before the role check).

#### Scenario: Verdict of a stored profile

- **WHEN** the owner calls `aiProfilesTestConnection` with a real profile id whose key the provider refuses
- **THEN** the system answers 200 with a verdict naming the key field and carrying no secret

### Requirement: Model restrictions and prices of a hosted portal

`getAiPrices` SHALL need a billing service and the AI gateway (403 otherwise) and a DocSpace admin.
`getRestrictedAiModels` SHALL answer an empty set while billing or the AI Tools service is off and the
barred models otherwise, to a DocSpace admin (402 when billing knows no customer). `setRestrictedAiModels`
SHALL replace the whole set: it needs the gateway, the portal-settings right, a billing customer (404),
AI Tools on (403) and a `models` list without null entries (400).

#### Scenario: Null entry

- **WHEN** the owner sends `models` holding a null entry, alone or next to a valid id
- **THEN** the system answers 400 before the action runs

#### Scenario: Restriction reaches an open conversation

- **WHEN** a project's model is restricted and a member sends into an existing thread of that project
- **THEN** the send is refused while a rename of the project still answers 200

### Requirement: Tasks and the scope of the assignments

Each task — Default, Chat, Code, Summarization, Translation, TextAnalyze, ImageGeneration, OCR, Vision,
FormAnalysis, GenerateMeta — SHALL hold at most one profile, portal-wide. An `entityId` sent to an
assignment route SHALL only be checked: one naming no folder 404, one the caller cannot open 403, any
accessible folder the portal map. An unknown task SHALL answer 400 on every route. The chat model of an AI
project is set through `setProjectModel`, not here.

#### Scenario: Unknown scope

- **WHEN** the owner reads or resolves assignments with an `entityId` that names no folder
- **THEN** the system answers 404 and does not fall back to the portal map

#### Scenario: Room is the portal map

- **WHEN** the owner reads all assignments with a Custom room as `entityId`, and assigns Chat with that id
- **THEN** the map equals the portal-wide one and the binding is read back without any `entityId`

### Requirement: Who may read and write assignments

`aiAssignmentsGetAllAssignments`, `aiAssignmentsGetAssignment`, `aiAssignmentsResolveForAction` and
`aiAssignmentsTryResolveForAction` SHALL be readable by DocSpace admins, Room admins and Users (Guest 403,
anonymous 401, AI off 403); `aiAssignmentsAssign`, `aiAssignmentsBulkAssign`, `aiAssignmentsUnassign` and
`aiAssignmentsCascadeProfileDelete` SHALL be DocSpace-admin only (403, a refused write writes nothing).

#### Scenario: Guest cannot read

- **WHEN** a Guest calls `aiAssignmentsGetAllAssignments`
- **THEN** the system answers 403

#### Scenario: Room admin cannot bind

- **WHEN** a Room admin calls `aiAssignmentsAssign`
- **THEN** the system answers 403

### Requirement: Resolving the model of a task

A task SHALL resolve to its own binding, else to Default, which the first profile of the portal fills; the
Chat binding never overrides FormAnalysis. `aiAssignmentsResolveForAction` SHALL answer 404, never 500,
when nothing is bound or the only candidate cannot serve the task; `aiAssignmentsTryResolveForAction`
answers 200 with a null body. `expand` `profile` on `aiAssignmentsGetAllAssignments` SHALL answer the
profiles with `profilesTotal` and `chatProfilesTotal`.

#### Scenario: Unassigned task falls back to Default

- **WHEN** Summarization has no binding and the owner calls `aiAssignmentsResolveForAction` for it
- **THEN** the Default profile is returned

#### Scenario: Default holds an image model

- **WHEN** Default is an image-only profile, Chat is unbound and the owner resolves Chat
- **THEN** the system answers 404 and `aiAssignmentsTryResolveForAction` answers 200 with null

#### Scenario: Hosted defaults

- **WHEN** a hosted portal is fresh
- **THEN** Default, ImageGeneration and FormAnalysis are bound to models of the gateway catalogue

#### Scenario: Chat and a profile that cannot chat

- **WHEN** the only profile Chat could resolve to cannot chat
- **THEN** Chat is not handed that profile

### Requirement: Writing assignments

`aiAssignmentsAssign` SHALL check the profile's capabilities against the task (Default exempt) and refuse,
leaving the binding, a profile the catalogue does not hold or one that cannot serve the task; a missing or
unknown field is 400. `aiAssignmentsBulkAssign` SHALL apply a flat task-to-profile map all or nothing.
`aiAssignmentsUnassign` removes one binding (repeat accepted); `aiAssignmentsCascadeProfileDelete` drops
every binding of one profile.

#### Scenario: Bulk assignment is all or nothing

- **WHEN** `aiAssignmentsBulkAssign` carries an unknown task key, a null value or a malformed profile id
- **THEN** the system answers 400 and no binding changes

#### Scenario: Unknown profile is refused

- **WHEN** the owner assigns a profile id the catalogue does not hold
- **THEN** the call is refused and the binding is unchanged

#### Scenario: Empty bulk map

- **WHEN** `aiAssignmentsBulkAssign` carries an empty map
- **THEN** the call is accepted and nothing changes

#### Scenario: Writes are audited

- **WHEN** a DocSpace admin assigns, bulk-assigns, unassigns or cascades a profile
- **THEN** the change is audited

### Requirement: The AI rooms section

Instructions rooms, Knowledge rooms and AI projects SHALL be rooms created through `createRoom` with their
`roomType`, by the owner, a DocSpace admin or a Room admin (User, Guest 403), landing in the AI rooms section
and never in Rooms; a Knowledge room needs vectorization enabled (403). Settings that do not apply to the
type SHALL be refused on create and update (400). They are never archived (403), make no template (403),
take no folder in the section root (403) and are deleted outright.

#### Scenario: Settings that do not apply

- **WHEN** the owner creates a project with `quota`, sets `denyDownload` on an Instructions room or `indexing`, `lifetime`, `watermark`, `chatSettings` or a form setting on any of the three
- **THEN** the system answers 400 and nothing is created or changed

#### Scenario: Section listing

- **WHEN** the owner lists the AI rooms section after creating a project and an Instructions room
- **THEN** both are listed there, neither in Rooms, and a filter by the project type lists the project only

#### Scenario: Instructions room cannot be archived

- **WHEN** the owner archives an Instructions room or makes a template from a project
- **THEN** the system answers 403

### Requirement: Members of the AI rooms

An Instructions room SHALL offer the Room manager, Content creator, Editor and Viewer roles, a Knowledge room
the same without Editor, an AI project only Project manager (Room manager) and Member (Read). A Guest SHALL
NOT hold any role in a project (403, nobody added) while a User joins through a link as Member. Every member
reads the project; only a manager edits it; a non-member of any role, a DocSpace admin included, SHALL NOT
read its model, sources or skills (403).

#### Scenario: Guest invited to a project

- **WHEN** the owner invites a Guest to a project as Member by id, by email or through an invitation link
- **THEN** the system answers 403 and the Guest is not a member

#### Scenario: Member reads, manager writes

- **WHEN** a Member reads the project model and then sets it
- **THEN** the read answers 200 and the write 403

### Requirement: The model of an AI project

`getProjectModel` SHALL answer the project's own chat model as a profile id, null while none is selected or
the profile is gone (the portal's Chat binding then answers); `setProjectModel` SHALL need a project
manager (403) and a `profileId` naming a catalogue profile that can chat (missing, unknown or image-only
400, the project keeping its model), replace the previous choice, be audited and pushed to clients. An
Instructions room or an unknown id answers 404 on either route.

#### Scenario: Image profile refused

- **WHEN** a manager sets an image-only profile on a project that runs on a chat profile
- **THEN** the system answers 400 and the project keeps its model

#### Scenario: Member reads the fixed model

- **WHEN** a Member reads the model of a project bound to profile A while the portal's Chat binding is B
- **THEN** A is returned

#### Scenario: Model moved

- **WHEN** a manager sets another chat profile on a project with an open thread
- **THEN** new rounds run on the new profile and the existing thread keeps its model

#### Scenario: Selection cannot be cleared

- **WHEN** a manager sends `setProjectModel` without a `profileId`
- **THEN** the system answers 400 and the selection stays: a selected model is replaced, never cleared

### Requirement: Sources of an AI project

`getProjectSources` SHALL list the Instructions and Knowledge rooms linked to a project, sorted by title;
`linkProjectSources` SHALL link active rooms of those two types that the caller manages and refuse the whole
call (400, linking nothing) for any other room id, an empty list, over 40 ids or more than 20 rooms of one
kind; `unlinkProjectSources` SHALL remove the listed links without checking rights on the rooms. Both writes
need a project manager (403) and are audited.

#### Scenario: Over the limit

- **WHEN** a manager links a 21st Knowledge room
- **THEN** the system answers 400 and the existing links stay

#### Scenario: Room not linkable

- **WHEN** a manager links an unknown room, a deleted one, a Custom room, another project or an Instructions room where they are only a Viewer
- **THEN** the system answers 400 and nothing is linked

#### Scenario: Deleted room

- **WHEN** a linked room is deleted
- **THEN** it leaves `getProjectSources`

#### Scenario: Duplicates and pushed changes

- **WHEN** a manager links a room twice, or one already linked
- **THEN** the room is linked once, and clients are told of the change

### Requirement: Access through a project

A member of a project SHALL read every room linked to it and the files inside, through a membership of the
project or of a group in it, without the room being listed in the project's member section; a non-member
SHALL NOT (403). Unlinking the room or removing the member from the project SHALL end that access, while a
member's own role in the room is never lowered by the project. `getRoomSkills` for a project SHALL answer
the skills of its linked Instructions rooms to any member.

#### Scenario: Access ends with the link

- **WHEN** a Member reads a file of a linked Knowledge room and the manager then unlinks the room
- **THEN** the read answers 200 before and 403 after

### Requirement: Skills of an Instructions room

A skill SHALL be a Markdown file in the root of an Instructions room whose YAML front matter carries `name`
(1–64 characters: lowercase letters, digits, single inner hyphens) and `description` (1–1024 characters, no
XML tag). A file without that front matter, with an invalid one or not Markdown SHALL NOT enter the room:
an upload, chunked upload or new version answers 400 keeping the previous version, a copy lands nowhere, a
move leaves the file where it was. The room takes no folder (403).

#### Scenario: Invalid name

- **WHEN** the owner uploads a skill whose `name` carries an upper-case letter or a double hyphen
- **THEN** the system answers 400 and the room stays empty

#### Scenario: Skill follows its file

- **WHEN** the owner uploads a new version of a skill, moves it to another Instructions room, then to the trash, and restores it
- **THEN** the catalogue shows the new version, then the other room, loses the skill in the trash and gets it back

### Requirement: Generating a skill for review

`aiSkillsGenerate` SHALL write a skill from a title and a description and return it for the author to review, saving
nothing in the Instructions room. Saving is an ordinary upload under "Skills of an Instructions room". The requests,
the result and the failures of the operation are in `portal-settings`.

#### Scenario: Nothing appears until the author saves

- **WHEN** a member generates a skill for an Instructions room
- **THEN** the answer holds the Markdown file and the room's skills are unchanged

### Requirement: Listing the skills

`getRoomSkills` SHALL answer the skills of an Instructions room (sorted by file title) or of a project's
linked Instructions rooms (rooms sorted by title) to a reader (403 others; unknown id or other room type
404). `getRoomsSkills` SHALL take 1–20 room ids (400 otherwise) and answer one entry per readable
Instructions room, leaving the rest out. `getRoomAiFolder` SHALL page the root of an Instructions room
(403 non-reader, 404 other type).

#### Scenario: Room without a skill

- **WHEN** the owner calls `getRoomAiFolder` on a fresh Instructions room and on a Custom room
- **THEN** the first answers 200 with no files and the second 404

#### Scenario: Unreadable room left out

- **WHEN** a User calls `getRoomsSkills` with an Instructions room they are not in, an unknown id and a Custom room next to a room they read
- **THEN** only the readable Instructions room is answered

#### Scenario: Instructions rooms in a folder listing

- **WHEN** the owner lists the AI rooms with `getRoomsFolder` and `withAiFolder`
- **THEN** the Instructions rooms are listed

### Requirement: Using the skills in a chat

The skills of the Instructions rooms connected to a round (`actionArgs.contextRooms`, or the linked rooms of
a project) SHALL be re-read from the room for every round and offered to the model as an index with a
`read_skill` tool that returns one skill's body by id; a body is never sent unless the model asks. The tool
SHALL serve a member of any role and refuse an outsider whatever catalogue the client spoofs, a Guest (403)
and a member whose invitation was revoked; anonymous 401.

#### Scenario: Model reads one skill

- **WHEN** the model calls `read_skill` with the id of one of several skills
- **THEN** only that skill's body reaches the model

#### Scenario: Outsider spoofing the catalogue

- **WHEN** a User who is not in the Instructions room sends the room's skill in `contextRooms`
- **THEN** the skill body never reaches the model

#### Scenario: Composer lists the rooms

- **WHEN** a member calls `aiContextGetContextFolders` and `aiContextGetRoomSkills` for a room they read
- **THEN** the readable Instructions rooms and that room's current skills are answered

### Requirement: An AI agent as a room: the deltas

An AI agent (`createRoom` with the AI room type, in the Rooms section) SHALL offer only the Room manager,
Content creator and Viewer roles; a Guest SHALL be accepted as Viewer only (403 above it). Its root SHALL
hold no files: an upload there answers 403, a Viewer's upload into Chat outputs 403, while Knowledge and
Chat outputs take files from a Content creator up. Knowledge SHALL accept only indexable formats (403) and
no subfolders.

#### Scenario: Guest as Content creator

- **WHEN** a Guest is invited to an agent as Content creator or Room manager
- **THEN** the system answers 403; the same Guest as Viewer answers 200

#### Scenario: Upload to the agent root

- **WHEN** the owner uploads a file to the agent itself and then to its Chat outputs folder
- **THEN** the first upload answers 403 and the second 200

#### Scenario: Agent imposes no model

- **WHEN** a thread is started in an agent on profile A while the portal's Chat binding is B
- **THEN** a send without `profileId` runs on A and the thread still reports A

#### Scenario: No template from an agent

- **WHEN** the owner makes a room template from an agent
- **THEN** the system refuses the call

#### Scenario: Stored prompt

- **WHEN** an agent carries a `chatSettings.prompt` and a member chats in it
- **THEN** the stored prompt reaches no model

### Requirement: Starting a chat thread

`aiThreadsCreate` SHALL start a thread for the caller — a DocSpace admin, a Room admin or a User; Guests
403, anonymous 401, AI off 403 — with a required non-blank `title` of at most 255 characters stored verbatim
(missing, null, blank, over-long or no body 400) and an optional `profileId` that must be well formed (400)
and name a catalogue profile (404). The thread belongs to its creator alone and is listed empty until a
message is appended.

#### Scenario: Blank title

- **WHEN** the owner sends an empty, whitespace or null title
- **THEN** the system answers 400 and nothing is listed

#### Scenario: Title at the limit

- **WHEN** the owner sends a 255-character title and then a 256-character one
- **THEN** the first is stored whole and the second answers 400

#### Scenario: Parallel creates

- **WHEN** the owner creates ten threads at once
- **THEN** all ten succeed with distinct ids

#### Scenario: No profile named

- **WHEN** the owner creates a thread without a `profileId`
- **THEN** the thread has no model until a send brings one

### Requirement: Where a thread may be started

`entityId` of `aiThreadsCreate` SHALL be checked, never used as a partition: omitted, null, empty or 0 is
the ordinary case; a value naming no folder, a malformed one included, 404; a folder the caller cannot open
403; any accessible folder is accepted, Read in a room sufficing. An AI project SHALL refuse a Room admin or
a User who is not its member (403).

#### Scenario: Non-member of a room

- **WHEN** a Room admin or a User who is not in an ordinary room, or a member removed from it, starts a thread with it as `entityId`
- **THEN** the system answers 403 and no thread is left behind

#### Scenario: Read-level member

- **WHEN** a member invited to a Custom room at Read starts a thread in it and sends a question
- **THEN** the thread is created and answered

#### Scenario: Outsider of a project

- **WHEN** a Room admin or a User who is not in a project starts a thread with it as `entityId`
- **THEN** the system answers 403

### Requirement: Thread privacy

A thread SHALL be visible to its creator only: `aiThreadsGetById`, `aiThreadsReadMessages`,
`aiThreadsRename`, `aiThreadsDelete`, `aiThreadsClearMessages`, `aiThreadsTouch`,
`aiThreadsOpenOrCreate`, every message route, a tool-call decision and a send or regeneration into it
SHALL answer 403 to any other member, a DocSpace admin or a member of the same room included. A member
removed from a room SHALL still manage the thread they wrote there but is refused a send into it (403).

#### Scenario: Two members of one room

- **WHEN** two members each start a thread in the same room
- **THEN** each lists only their own thread and reading the other's answers 403

#### Scenario: Removed member keeps their thread

- **WHEN** a member who wrote in a room is removed from it
- **THEN** creating for that room and sending into the thread answer 403, while the thread is still read and renamed by its creator

#### Scenario: Regeneration by a fellow member

- **WHEN** a member of the same room regenerates the owner's thread
- **THEN** the system answers 403, the reply is kept and the model is not reached

### Requirement: One thread list per user

`aiThreadsList` SHALL show the caller's threads as one list whatever `entityId` it carries: a thread started
in room A is listed for room B, a folder, a third-party room, a project, a string that is no id and no
entity at all; the id is not checked, so a room the caller cannot open lists
the same threads and never another user's. A thread SHALL survive the archiving or deletion of the room it
was started in and stays readable and answerable.

#### Scenario: Rooms share one list

- **WHEN** a member starts a thread in room A and lists threads for room B, for a folder and without an entity
- **THEN** the thread appears in all three lists

#### Scenario: Room of the thread deleted

- **WHEN** the room a thread was started in is archived, unarchived and then deleted
- **THEN** the thread stays listed, readable, answerable and deletable

### Requirement: Listing, paging and searching threads

`aiThreadsList` SHALL return the caller's threads most recently edited first with what a sidebar needs,
`count` pages of at least 1 (0 or less 400), a keyset `cursor` that visits every thread once, and `query` as
a case-insensitive substring of the title with `%` and `_` taken literally. A deleted thread SHALL leave the
list; a touch SHALL move the thread to the top, move only that thread and never move its date back.

#### Scenario: Paging by cursor

- **WHEN** the owner walks the list with `count` 1 and the returned cursor
- **THEN** every thread is visited exactly once and the last page carries no cursor

#### Scenario: Search

- **WHEN** the owner lists with `query` `REPORT` and a thread is titled `Monthly report`
- **THEN** only that thread is returned

### Requirement: Renaming, deleting and clearing a thread

`aiThreadsRename` SHALL store the new title verbatim (missing, null, blank, non-string or over 255
characters 400 keeping the name; unknown thread 404); `aiThreadsDelete` SHALL remove the thread with its
messages and attachments and no other thread (unknown or already deleted 404, missing id 400);
`aiThreadsClearMessages` SHALL empty the thread and keep its identity (empty thread or repeat 200, unknown
404). A malformed id SHALL answer 400 on every route.

#### Scenario: Cleared thread carries on

- **WHEN** the owner clears a thread with a conversation and sends a new question
- **THEN** the thread answers again and no longer knows the cleared history

#### Scenario: Rename mid-reply

- **WHEN** the owner renames a thread while its reply is still streaming
- **THEN** the rename survives the reply

#### Scenario: Deleted thread is gone everywhere

- **WHEN** the owner deletes a thread and then reads, renames, clears, touches or sends into it
- **THEN** every route answers 404

### Requirement: Touching and reopening a thread

`aiThreadsTouch` SHALL bump `lastEditDate` and change nothing else (bad or missing id 400);
`aiThreadsOpenOrCreate` with a `threadId` SHALL replay the thread's history whole and in order, writing
nothing, and leave its model alone whatever `profile` the request carries (no profile object 404); without
a `threadId` it SHALL create a thread titled from `firstMessage`, storing the message not being its job.

#### Scenario: Reopened with another profile

- **WHEN** the owner reopens a thread with another profile object
- **THEN** the history is replayed and the thread still reports its own model

#### Scenario: Opened by the first message

- **WHEN** the owner calls `aiThreadsOpenOrCreate` with a profile and a `firstMessage` but no `threadId`
- **THEN** a thread is created and listed, with nothing stored in it

### Requirement: Regenerating a thread title

`aiThreadsRegenerateTitle` SHALL take `threadId` and the resolved `profile` object (either missing, or a
`profileId` in place of the object, 400; a malformed or unknown thread refused without a change to the
title), ask the model for a title from the thread's conversation, store it on the thread, return it and
accept a repeat. It SHALL be refused for another user's thread (403) and while AI is off (403).

#### Scenario: Refused request leaves the title

- **WHEN** the owner sends a malformed thread id, or a real one without the profile
- **THEN** the system refuses the call and the thread keeps its title

#### Scenario: Title from a conversation

- **WHEN** the owner regenerates the title of a thread the model has answered
- **THEN** the system answers 200 with the generated title and `aiThreadsGetById` reports it

### Requirement: Messages of a thread

`aiThreadsAppendUserMessage` SHALL store a user message verbatim without drawing an answer and refuse an
empty text, a null message, one without content, a role other than `user` or an unusable thread id (400).
`aiThreadsUpdateMessage` SHALL rewrite the content in place keeping position, role and the standing answer
(empty text 400, unknown 404). Another user's message SHALL answer 403 on every route.

#### Scenario: Edited question stays in place

- **WHEN** the owner rewrites the first question of a two-turn thread
- **THEN** `aiThreadsReadMessages` shows the new text first and the assistant reply still after it

#### Scenario: Assistant turn appended by hand

- **WHEN** the owner appends a message with the role `assistant`
- **THEN** the system answers 400 and the thread is unchanged

#### Scenario: Parallel appends

- **WHEN** the owner appends several messages at once
- **THEN** all of them are stored, in order

#### Scenario: Too many attachments

- **WHEN** a message carries more than five file or more than five image attachments
- **THEN** `aiThreadsAppendUserMessage` and the send answer 200 and the message is stored with all of them: 11 attachments in the test (bug 82894)

#### Scenario: Reading one message

- **WHEN** the owner calls `aiThreadsGetMessageById` with a deleted, cleared or unknown id, and with a malformed or missing one
- **THEN** the first answers 404 and the second 400

#### Scenario: Deleting a message

- **WHEN** the owner calls `aiThreadsDeleteMessage` with an unknown id, and with a malformed one
- **THEN** the first answers 200 and the second 400

### Requirement: Reading the history

`aiThreadsReadMessages` SHALL page the thread oldest first with `count` of at least 1 (0 or less 400) and
a keyset cursor built from a message's timestamp and id that returns every message once and ends on an empty
page; `direction` `desc` SHALL page back from the newest; a bare id as cursor reads the first page. A
missing `threadId` 400, an unknown or deleted one 404. The assistant text SHALL be served as raw markdown,
identical to the streamed text, whose snapshots only ever grow.

#### Scenario: Paging back

- **WHEN** the owner reads a ten-message thread with `count` 3 and `direction` `desc`
- **THEN** the pages start at the newest message and together hold every message once

#### Scenario: Cursor from the last message

- **WHEN** the owner continues from a cursor built from the last message of a page
- **THEN** that message is not returned again and the walk ends on an empty page

### Requirement: Sending a message with a streamed reply

`aiSendWithStream` SHALL store the user message, run the thread's model and stream the reply as
newline-delimited chat events ending on the stored assistant text; without `threadId` it SHALL open a new
thread titled "New chat" and rename it from the first question after the stream. Only the thread's own
history reaches the model. A provider refusal SHALL fail the turn with the provider's words and keep the
question; a client hang-up SHALL stop the generation.

#### Scenario: First send opens the thread

- **WHEN** the owner calls `aiSendWithStream` with a prompt and no `threadId`
- **THEN** the system answers 200, a new thread is listed and the user message and the reply are stored

#### Scenario: Context stays inside the thread

- **WHEN** the owner teaches a fact in thread A and asks for it in thread B
- **THEN** thread B does not know it, while a second question in thread A does

#### Scenario: Hang-up cancels

- **WHEN** the client closes the connection mid-stream
- **THEN** the generation stops and the thread accepts the next message

#### Scenario: Provider refuses

- **WHEN** the model refuses the round and the owner sends again on another model in the same thread
- **THEN** the first turn is stored as failed with the question kept and the second is answered

### Requirement: Refusals of a send

`aiSendWithStream` SHALL refuse before the stream opens: an empty message 400, an unknown thread 404, a
malformed or unknown `profileId` 400 outside a project (the thread keeping its model), another user's thread
403, a Guest 403, a non-member of the room or project named as `entityId` 403, AI off 403, the AI Tools
service unpaid on a hosted portal 402, a body over the request size limit 413. A refused send SHALL store
neither the question nor an answer.

#### Scenario: Unknown profile

- **WHEN** the owner sends with a well-formed `profileId` no profile carries
- **THEN** the system answers 400 and the thread is unchanged

#### Scenario: Oversized body

- **WHEN** the owner sends a message larger than the request size limit
- **THEN** the system answers 413, no frame is streamed and the next ordinary message is answered

#### Scenario: Project without a usable model

- **WHEN** the owner or a manager sends into an AI project whose model is gone or cannot chat
- **THEN** the system answers 409 with `code` `projectModelUnavailable` before any stream

### Requirement: The model of a chat

A round that the owner or a manager of an AI project sends with the project as `entityId` SHALL run on the
project's model — its own, else the portal's Chat binding — whatever `profileId` it carries (blank or unknown
included), never moving the thread. Outside a project a send with no `profileId` runs on the thread's own
model, else on Chat, else on Default; a `profileId` sent becomes the thread's model; rebinding Chat moves no
thread.

#### Scenario: Project answers on its model

- **WHEN** a project manager sends into a project thread naming another profile
- **THEN** the reply runs on the project's model and the thread still reports that model
- **AND** when a read member sends the same, the request does not reach the project's model (bug 82914)

#### Scenario: Picker switches the thread's model

- **WHEN** the owner sends with profile A, then with profile B into the same room thread
- **THEN** the thread reports profile B afterwards and the second reply still knows the first exchange

#### Scenario: Thread created in a project on another profile

- **WHEN** a thread is created with a project as `entityId` naming another profile
- **THEN** that profile is stored on the thread, whether a manager or a member creates it (bug 82915)

#### Scenario: Image-only chat model

- **WHEN** the thread's model can only draw images
- **THEN** the reply fails and the question is kept

### Requirement: Compacting a long thread

A thread that nears the context window of its model SHALL be folded into a summary before the round, and later rounds
SHALL send the summary in place of the older turns. The summary is stored as an ordinary message of the thread. This
is on by default; an installation can turn it off, which takes effect from the next round without a restart.

#### Scenario: Long thread

- **WHEN** a member sends a message in a thread that nears the model's window
- **THEN** the round runs on the summary and the recent turns, and the summary is among the thread's messages

### Requirement: The window a thread is measured against

The window SHALL be `contextWindow` of the model: for the gateway's catalogue that is the gateway's `context_length`,
reported in the model list. Without it the window SHALL come from the known window of that model id, else from a
default. A saved profile does not keep the value.

#### Scenario: Model from the gateway

- **WHEN** the gateway reports a positive `context_length` for a model
- **THEN** the model list carries it as `contextWindow`

### Requirement: One-shot sends

`aiSend` and `aiSendCustom` SHALL run the model once for a DocSpace admin, a Room admin or a User (Guest
403) and persist nothing: no thread is created and two calls share no history. An empty message or a
malformed body SHALL be refused (400, never 500); a task type without its own binding resolves through
Default; a nonexistent `entityId` falls back to the portal, an inaccessible one answers 403. `aiSendCustom`
SHALL require `systemPrompt` (400) and a boolean `isStream` (a string 400).

#### Scenario: Two one-shot calls

- **WHEN** the owner calls `aiSend` twice
- **THEN** no thread is created and the second call does not see the first

### Requirement: OpenAI-shaped send

`aiSendWithStreamOpenAI` SHALL persist like `aiSendWithStream` — a new thread when `threadId` is missing,
the thread's model kept without `profileId`, an explicit profile moving it outside a project — and stream
OpenAI `chat.completion.chunk` events as server-sent events ended by `[DONE]`, surfacing a tool call as
`tool_calls` with the matching finish reason and a model failure inside the stream with 200. A hang-up
SHALL cancel the generation and leave the thread usable.

#### Scenario: OpenAI stream

- **WHEN** the owner calls `aiSendWithStreamOpenAI`
- **THEN** the answer is a server-sent-events stream of chunks terminated by `[DONE]` and both messages are stored in the thread

#### Scenario: Tool call in the OpenAI stream

- **WHEN** the model calls a tool during `aiSendWithStreamOpenAI`
- **THEN** the chunks carry `tool_calls` with the matching finish reason and no frame type of the chat service

### Requirement: Regenerating a reply

`aiRegenerateStream` SHALL delete the last assistant reply, keep the question and every earlier turn, and
stream a new reply on the thread's own model (the project's inside a project); a tool chain of the old reply
SHALL be replaced whole. A thread with nothing to regenerate SHALL answer 200 generating nothing, an unknown
thread 404 before any stream, another user's thread 403 keeping the reply, AI off 403.

#### Scenario: Multi-turn thread

- **WHEN** the owner regenerates a thread of three exchanges
- **THEN** only the last reply changes and the three questions stay

#### Scenario: Abandoned reply

- **WHEN** the client hangs up mid-reply and the owner then regenerates
- **THEN** the abandoned reply is replaced by a whole one

### Requirement: Tool calls and the approval pause

A tool the model wants to run SHALL pause the stream with a pending tool call that carries the model's
announcement; `aiApproveToolCall` (with a string or structured `result`, optionally `allowAlways`) and
`aiDenyToolCall` SHALL resume it, the refusal going to the model. A tool that opted out of approval or is
pre-approved runs unasked; a tool that answers "unavailable", was withdrawn or whose server left the registry
SHALL finish the reply.

#### Scenario: Two-hop chain

- **WHEN** the model calls tool A, the owner approves, the model calls tool B and the owner approves again
- **THEN** both calls, both results and the final answer are stored in order in one message

#### Scenario: Deny

- **WHEN** the owner denies a pending document generator call
- **THEN** no file is written and the reply finishes

#### Scenario: Client tool switched off

- **WHEN** the client sends a tool with `enabled` false
- **THEN** the tool is not offered to the model

#### Scenario: Announcement

- **WHEN** a pending tool call carries the model's announcement
- **THEN** it is not duplicated as chat text and is kept after the approval

### Requirement: Decisions on a tool call

`aiApproveToolCall` and `aiDenyToolCall` SHALL identify the call by `threadId`, `messageId` and `idx`; a
`message` snapshot in the body is accepted and ignored. No pending tool call, an unknown message id or an
index out of range SHALL answer 409 inventing nothing; a second decision on a decided call SHALL NOT
overwrite the first result, and a stale one never reopens a superseded message. Both routes answer 401
anonymous, 403 to a non-member of the thread and 403 while AI is off.

#### Scenario: Nothing to decide

- **WHEN** the owner approves a message that holds no tool call
- **THEN** the system answers 409 and the thread is unchanged

#### Scenario: Replayed approval

- **WHEN** the owner approves or denies a decided call again with a different result
- **THEN** the stored result is the first one and the follow-up is not regenerated

#### Scenario: Replayed denial

- **WHEN** the owner denies a call that was already denied
- **THEN** the follow-up text is not generated again

### Requirement: Deep mode and reasoning level

`aiPreferencesSetDeepMode`, `aiPreferencesGetDeepMode`, `aiPreferencesIsDeepModeSet`,
`aiPreferencesClearDeepMode`, `aiPreferencesSetReasoningLevel` and `aiPreferencesGetReasoningLevel` SHALL
keep one value per user — extended thinking on or off and its depth (`none`, `minimal`, `low`, `medium`,
`high`) — off and unset by default, invisible to other users. On after off, or without a chosen depth, lands
on `medium`; on keeps a stored depth; off replaces it; clearing returns the user to unset.

#### Scenario: One user's value

- **WHEN** the owner switches deep mode on and a User reads their own
- **THEN** the User still reads off and unset

#### Scenario: On after off

- **WHEN** a member chooses `high`, switches deep mode off and on again
- **THEN** the level reads `medium`

#### Scenario: Reasoning reaches the client

- **WHEN** deep mode is on and the model answers with reasoning
- **THEN** the reasoning reaches the client as a part of its own and the answer is healthy

### Requirement: Scope and refusals of the preferences

The preferences have no scope: an `entityId` on any route SHALL be a positive integer, empty or 0 (a malformed
value or a thread id 400, writing nowhere) and is otherwise not used — a room, a folder, an
AI room, an unknown id or a room the caller cannot open all read and write the caller's one value. A
non-boolean switch value, an empty body or a depth outside the five SHALL answer 400 and change nothing.
Guests 403, anonymous 401, every route 403 while AI is off.

#### Scenario: Room is no scope

- **WHEN** a member sets deep mode on with a Custom room as `entityId` and reads it without one
- **THEN** the value reads on

#### Scenario: Thread as scope

- **WHEN** a member sends a thread id as `entityId` to any route
- **THEN** the system answers 400 and nothing changes

### Requirement: Custom MCP servers

`aiToolsAddCustomServer`, `aiToolsUpdateCustomServer`, `aiToolsRemoveCustomServer` and
`aiToolsReplaceAllCustomServers` SHALL be allowed to DocSpace admins only (others 403; anonymous 401);
`aiToolsGetCustomServer` and `aiToolsListCustomServers` SHALL be readable by DocSpace admins, Room admins
and Users (Guest 403). Registration SHALL NOT dial the server; a config of any shape comes back verbatim and
is replaced whole on update; each change is audited.

#### Scenario: Round trip

- **WHEN** the owner adds a server with an HTTP config, reads it, updates it and removes it
- **THEN** the config comes back verbatim, the update replaces it whole and the remove answers 200

#### Scenario: Member cannot register

- **WHEN** a Room admin or a User registers or removes a server
- **THEN** the system answers 403 and the registry is unchanged

#### Scenario: A flag in the config switches nothing

- **WHEN** the owner registers a config carrying `disabled` true
- **THEN** it is stored verbatim and the server's tools are not switched off

#### Scenario: Unregistered name

- **WHEN** a member calls `aiToolsGetCustomServer` with a name nobody registered
- **THEN** the system answers 200 with null

#### Scenario: Off period loses nothing

- **WHEN** AI is off while servers are registered, and is switched on again
- **THEN** every server is still there

#### Scenario: AI off

- **WHEN** AI is off and a member calls any of these routes
- **THEN** the system answers 403 while the tools catalogue still answers

### Requirement: One registry per portal

The custom MCP server registry SHALL be one map per portal: an `entityId` on a write SHALL be checked — an
unknown or deleted room 404 writing nothing, a room or folder the caller cannot open 403 — and otherwise
ignored, so a server registered with a room, a folder or a third-party room is listed from every scope.
Adding a name without a config SHALL be refused as a duplicate for a registered name and answer 400 naming the
portal server otherwise.

#### Scenario: Registered from a room

- **WHEN** the owner registers a server with room A as `entityId`
- **THEN** the portal-wide list and a listing for room B both show it

#### Scenario: Unknown room on a write

- **WHEN** the owner adds, replaces or removes with an `entityId` naming no room
- **THEN** the system answers 404 and the registry is untouched

#### Scenario: Reads ignore the entity

- **WHEN** a member lists the servers with an `entityId` that names nothing or a room they cannot open
- **THEN** the portal registry is answered and nothing leaks about the room

### Requirement: Server names

A server name SHALL be a non-empty string of at most 128 characters (129 a hard 400), matched
case-insensitively while keeping its spelling, surrounding whitespace included. `.`, `..`, a path separator
or a control character SHALL be a hard refusal (400) on add, update and replace-all; an underscore SHALL be
refused on add and reported per entry on replace-all.

#### Scenario: Underscore in the name

- **WHEN** the owner adds `my_server`
- **THEN** the call is refused naming the `name` field and nothing is stored

#### Scenario: Name at the limit

- **WHEN** the owner adds a name of 128 characters and then one of 129
- **THEN** the first is stored and the second answers 400

#### Scenario: Unicode names

- **WHEN** the owner adds `café` spelled as one code point and as a letter plus a combining accent, and a name with an emoji
- **THEN** the first two are two servers and the third round-trips

#### Scenario: Soft refusals

- **WHEN** the owner adds a taken name in another casing, or updates an unknown name
- **THEN** the call answers 200 with success false and the original stays

#### Scenario: Removal

- **WHEN** the owner removes an unknown name, and a registered one in another casing
- **THEN** the first is a no-op and the second removes the server

### Requirement: Server configurations and replace-all

A config SHALL be stdio or HTTP, never both — both `url` and `command`, or either of the wrong type, SHALL
be refused storing nothing; a missing or null config is a hard 400, an empty object or a number counts as no
config. `aiToolsReplaceAllCustomServers` SHALL require a `map` object (missing, null, scalar or array 400),
leave the registry as exactly the map sent, apply nothing when one entry is invalid while reporting every
bad entry in `errors`, and clear the registry on an empty map.

#### Scenario: Both url and command

- **WHEN** the owner adds a server whose config carries both `url` and `command`
- **THEN** the call is refused and nothing is stored

#### Scenario: Replace-all with one bad entry

- **WHEN** the owner replaces the map with two valid servers and one invalid config
- **THEN** the registry keeps its previous servers and `errors` names the bad one

#### Scenario: Two names differing in case

- **WHEN** the owner replaces the map with `Server` and `server`
- **THEN** the call is refused like a duplicate on add and the registry survives

#### Scenario: Unusable map

- **WHEN** the owner calls `aiToolsReplaceAllCustomServers` with a missing, null, scalar or array `map`
- **THEN** the system answers 400 and the registry survives

### Requirement: Switching tools off

`aiToolsSetDisabled`, `aiToolsGetDisabled` and `aiToolsIsToolDisabled` SHALL keep, per user and
portal-wide, the tools switched off for a `serverType` that is a built-in group or a registered server (400
naming the valid values otherwise; missing, null or string `toolNames` 400), the list being a full
replacement — an empty list switches everything back on. One user's list SHALL be invisible to another; a
removed or replaced-away server takes its entries with it.

#### Scenario: Disable and re-enable

- **WHEN** the owner disables one tool of a registered server and then sends an empty list
- **THEN** the tool reads disabled and then enabled again

#### Scenario: Dropped server loses its entries

- **WHEN** a server with disabled tools is left out of a replace-all and later registered again
- **THEN** the new server starts with nothing disabled

#### Scenario: Refused list

- **WHEN** the owner sends missing, null or string `toolNames`
- **THEN** the system answers 400 and the stored list stays

#### Scenario: Guest naming a registered server

- **WHEN** a Guest, or any caller while AI is off, names a registered server in `aiToolsSetDisabled`
- **THEN** the system answers 403 before the server type is checked, not a 400 listing the valid values

### Requirement: Pre-approving tools

`aiToolsSetAllowAlways`, `aiToolsGetAllowAlways` and `aiToolsIsAllowAlways` SHALL keep, per user, the flat
`serverType_toolName` pre-approvals: setting is idempotent, revoking one entry touches no other, a falsy,
missing or string value grants nothing (400, never 500), and a grant for one server type never answers for
another. The list SHALL be independent of the disabled list; `allowAlways` on an approval lands in the same
list under the server-prefixed key, where the read finds it.

#### Scenario: One user's approval

- **WHEN** a member pre-approves a tool and the owner reads their own list
- **THEN** the owner's list does not carry it

#### Scenario: Approval with allowAlways

- **WHEN** the owner approves a pending call with `allowAlways` true and the model calls the same tool again
- **THEN** the second call runs without a pause and `aiToolsIsAllowAlways` answers true for it

#### Scenario: Same name on another server

- **WHEN** the owner pre-approves a host tool `delete_file`
- **THEN** the DocSpace `delete_file` still pauses for approval

### Requirement: Tools catalogue

`aiToolsListSystemTools` SHALL answer the portal's tools grouped by server type — a registered server's tools
under its name — the system group keys in `system` and in `errors` the reason a registered server delivered
none (its credentials never echoed), to every signed-in member, Guests included, with AI on or off, whatever
`entityId` it carries. The catalogue lists no tool twice, lists a registered server as no system group, and
a registered server's tools reach the model in every chat.

#### Scenario: Unreachable registered server

- **WHEN** a server that does not answer is registered and the owner reads the catalogue
- **THEN** the system answers 200 and names the server in `errors`

### Requirement: Web-search configuration on a self-hosted portal

`aiWebSearchConfigure` SHALL probe the provider and store the configuration (a wrong key or unreachable
provider 400 storing nothing; a malformed body or a config that fails a rule 400, never 200 with success
false); `aiWebSearchSetActiveConfig` SHALL store a well-formed configuration, the second replacing the first
(malformed 400; unresolvable `entityId` 404). Both are DocSpace-admin only (403 others), refuse a Guest and
an AI-off portal (403) before the body is validated, and are audited.

#### Scenario: Probe stores nothing

- **WHEN** the owner configures a provider whose key the provider refuses
- **THEN** the system answers 400 and `aiWebSearchIsConfigured` still answers false

#### Scenario: Guest with a malformed body

- **WHEN** a Guest calls `aiWebSearchSetActiveConfig` with a malformed body
- **THEN** the system answers 403

#### Scenario: Refused writes

- **WHEN** a Room admin or a User configures the provider, or the owner sends a malformed configuration to `aiWebSearchSetActiveConfig`
- **THEN** the first answers 403 and stores nothing, the second answers 400 naming the field

### Requirement: Clearing and the hosted add-on

`aiWebSearchClear` of a stored configuration SHALL be DocSpace-admin only (403 others, the configuration
surviving); clearing an unconfigured portal is accepted by every reading role. Hosted, the provider SHALL come
from the AI search add-on of the wallet (bought by the owner or a DocSpace admin, needing the AI Tools service
and the switch): enabling it configures every scope and gives the model a web-search tool with sources; while
it owns the provider the three write routes answer 403.

#### Scenario: Room admin cannot clear

- **WHEN** a Room admin calls `aiWebSearchClear` on a configured portal
- **THEN** the system answers 403 and the configuration survives

#### Scenario: Owner cannot clear the add-on config

- **WHEN** the owner calls `aiWebSearchClear` while the add-on is on
- **THEN** the system answers 403 and the configuration survives

#### Scenario: Clearing with an entity body

- **WHEN** a member clears an unconfigured portal sending an entity in the body
- **THEN** the call is accepted and changes nothing

#### Scenario: Clearing is audited

- **WHEN** a DocSpace admin clears a stored configuration
- **THEN** the configuration is gone and the change is audited

### Requirement: Reading and probing the web-search configuration

`aiWebSearchIsConfigured` and `aiWebSearchGetActiveConfig` SHALL be readable by DocSpace admins, Room admins
and Users (Guest 403, anonymous 401, AI off 403); an empty `entityId` is the portal, one that does not resolve
404, a room the caller cannot open 403. `aiWebSearchTestConnection` SHALL probe a configuration from the
request for every non-guest role (Guest or AI off 403 dialling nothing) and store nothing.

#### Scenario: Private base URL

- **WHEN** the owner probes or configures a provider whose base URL points at a loopback address
- **THEN** the system answers 400 and nothing is dialled or stored

#### Scenario: Guest cannot read

- **WHEN** a Guest calls `aiWebSearchIsConfigured`
- **THEN** the system answers 403 while a User reads that search is available

#### Scenario: Off period keeps the configuration

- **WHEN** AI is switched off and on again on a configured portal
- **THEN** the reads answer 403 while it is off and the configuration is intact afterwards

#### Scenario: Forbidden base URL everywhere

- **WHEN** a DocSpace admin stores or a member probes a configuration with a forbidden base URL
- **THEN** the system answers 400 before any connection

### Requirement: Web-search passthrough

`aiWebSearchPassthroughSearch` and `aiWebSearchPassthroughContents` SHALL relay the provider's answer to
DocSpace admins, Room admins and Users (Guest 403, anonymous 401, AI off 403), answer 404 while no provider
is configured, refuse a body of the wrong shape with 400 naming the field before anything is dialled, relay
a provider 4xx with its status and body, and answer a provider 5xx or an unreachable provider as a fixed 502
without the provider's body or URL.

#### Scenario: No provider

- **WHEN** the portal has no provider and the owner searches
- **THEN** the system answers 404; after a configuration is stored a search answers 200 with results and no secret

#### Scenario: Attributed to a document

- **WHEN** the owner searches naming a document they can open, and then one they cannot
- **THEN** the first search is attributed to the document and the second to nothing

#### Scenario: What reaches the provider

- **WHEN** a member searches on a configured portal
- **THEN** the query and the stored key go to the provider's own path

### Requirement: OpenAI-compatible passthrough

`aiOpenaiChatCompletions` and `aiOpenaiImagesGenerations` SHALL relay the request for the profile named in
the path to its provider and answer verbatim to DocSpace admins, Room admins and Users (Guest 403,
anonymous 401, AI off 403). A malformed or unknown `profileId` SHALL answer 400; `stream` true streams
chunks; an image generation on a text-only model is refused (400). The gateway proxy exposes only models,
chat completions, image generation, search and contents (others 404).

#### Scenario: Chat completion with every optional field

- **WHEN** the owner posts a completion request with temperature, max tokens and a Cyrillic prompt
- **THEN** the system answers 200 with a `chat.completion` whose text survives the round trip

#### Scenario: Guest refused

- **WHEN** a Guest posts a completion or an image generation
- **THEN** the system answers 403

### Requirement: Editor tools catalogue

`aiEditorToolsList` SHALL publish the tool catalogue — unique names, a description, an object input schema
whose required arguments are declared and never also defaulted, `requireApproval` off for read tools and on
for the ones that change something — identically to every non-guest member and on repeated reads, a Guest
getting it without the document generators. Anonymous 401; AI off 403.

#### Scenario: Guest catalogue

- **WHEN** a Guest and a User read the catalogue
- **THEN** the User's list holds the document generators and the Guest's does not

### Requirement: Calling an editor tool

`aiEditorToolsCall` SHALL run one tool by its trimmed exact name within the caller's own rights (a Guest may
call none, 403 before the name is read), report a failing tool in `result` with 200, answer 400 for an
unknown or empty name and a missing, null or mistyped required argument, scope a generator into the room
named by `entityId` only for a caller who may create files there (400 otherwise), and fall back to the
caller's My documents without `entityId`.

#### Scenario: Tool never exceeds the member

- **WHEN** a User calls the read, rename, delete or archive tool on the owner's data
- **THEN** every call reports failure inside `result` and nothing changes

#### Scenario: Generator in a room

- **WHEN** a Content creator of a room names it as `entityId` for a document generator
- **THEN** the document is created in that room

#### Scenario: Room the caller cannot write to

- **WHEN** a member who may not create files in a room names it for a generator
- **THEN** the system answers 400 and no document lands in the room or in any My documents

#### Scenario: Entity that names nothing

- **WHEN** the owner names a nonexistent or non-string `entityId` for a generator
- **THEN** the document lands in the owner's My documents

#### Scenario: AI off

- **WHEN** AI is off and a member calls a tool with a valid body, and with an invalid one
- **THEN** the first answers 403 and the second is validated first and answers 400

### Requirement: Creating chat attachments

`aiAttachmentsSaveFile` and `aiAttachmentsSaveFilesMany` SHALL turn a DocSpace file the caller can read
(`path` is the file id as a string; empty or numeric 400, no such file 404, unreadable 403, an archive, an
empty document or a file without text 400) into a draft the caller owns, titled after the file: a picture an
image draft with the file's bytes, anything else a text draft with the extracted text, whatever the request
body limit. Guests 403; anonymous 401; AI off 403.

#### Scenario: Attach by id

- **WHEN** the owner attaches a stored document by its id
- **THEN** the system answers 200 with a draft carrying the extracted text, and a large file attaches whole

#### Scenario: Guest with a shared file

- **WHEN** a Guest attaches a file shared with them
- **THEN** the system answers 403

#### Scenario: Batch with one bad element

- **WHEN** the owner sends a batch with one element without `content`, and a User a batch holding a file they cannot read
- **THEN** the first answers 400 naming the element and the second 403, neither storing a draft

#### Scenario: Fields stored as sent

- **WHEN** the owner attaches a document with a `type` of another kind, a blank `title` and an `entityId`
- **THEN** `type` is echoed as sent, the title comes from the file and the `entityId` is never echoed

#### Scenario: Archive by name or by bytes

- **WHEN** the owner attaches a file with an archive name, or with archive bytes under a text name
- **THEN** the system answers 400

#### Scenario: Batch

- **WHEN** a member calls `aiAttachmentsSaveFilesMany` with repeated paths and one file that does not exist
- **THEN** a clean batch collapses the repeats and keeps its order; the batch with the missing file answers 404 and stores nothing

### Requirement: Reading and deleting attachments

`aiAttachmentsGet`, `aiAttachmentsGetMany`, `aiAttachmentsDelete` and `aiAttachmentsDeleteMany` SHALL see
the caller's drafts only: another user's draft reads as null in place and is not deleted; an unknown id
reads as null and deletes with success without stopping a batch; a malformed id, an empty list or a missing
body 400. A draft is readable right after it is saved and gone right after it is deleted; deleting it SHALL
leave the DocSpace file it was made from.

#### Scenario: Another user's draft

- **WHEN** a User asks `aiAttachmentsGetMany` for the owner's draft id next to their own
- **THEN** the own draft is returned and the owner's slot is null

#### Scenario: Mixed delete

- **WHEN** a User deletes their own draft and the owner's in one `aiAttachmentsDeleteMany`
- **THEN** only the User's draft is gone

#### Scenario: Draft reaches the model

- **WHEN** the owner sends a message carrying a draft by id only, and then one carrying the whole record inline
- **THEN** the round ends healthy but no streamed request to the model carries the draft's text, either time (bug 82773)

#### Scenario: Unpaid portal

- **WHEN** the AI Tools service is off on a hosted portal
- **THEN** the whole draft lifecycle still works

### Requirement: Linking attachments to a message

`aiAttachmentsLinkToMessage` SHALL bind the caller's drafts to a message of the caller's thread so they are
removed with the message or the thread; it SHALL answer 403 for a thread the caller cannot read, 404 for an
unknown message or attachment, 400 for a message of another thread, an empty or non-array `ids`, a
malformed id or an empty body, and write nothing onto the message itself. Linking the same draft twice SHALL
be accepted; another user's draft cannot be linked.

#### Scenario: Linked draft dies with the message

- **WHEN** the owner links a draft to a message and deletes the message
- **THEN** the draft reads back as null while an unlinked draft stays

### Requirement: Suggested questions for a form attachment

`aiAttachmentsGetSuggestedQuestions` SHALL answer 200 with a `status` and a `questions` list for an
attachment id of the caller: `unavailable` and empty for a draft that is not an analysable started form,
for an unknown id and for another user's attachment (exactly as for an unknown one); a missing, empty, null
or malformed id 400; anonymous 401; AI off 403.

#### Scenario: Unknown id

- **WHEN** the owner asks for a well-formed id no attachment carries
- **THEN** the system answers 200 with `unavailable` and no questions

### Requirement: Exporting markdown into a document

`aiExportTextToDocx` SHALL queue the export of `content` under `title` into `folderId` as `format` `Docx`
(default), `Pdf` or `Md` and answer 202 with no file id; the file appears in the folder later with every
markdown construct rendered. It SHALL require `title`, non-blank `content` and `folderId` (400), refuse an
unknown or deleted folder, a file id or a numeric string (404), a body over 15 MiB (413), an unknown
`format` (400), and validate before the AI switch (400 before 403).

#### Scenario: Export into Chat outputs

- **WHEN** the owner exports a transcript with an agent's id as `folderId`
- **THEN** the document lands in the agent's Chat outputs folder, not in its root

#### Scenario: Unknown format

- **WHEN** the owner sends `format` `Rtf`
- **THEN** the system answers 400

#### Scenario: Whitespace content

- **WHEN** the owner sends a title with whitespace-only content
- **THEN** the system answers 400 and no document is built

#### Scenario: Size limit counts bytes

- **WHEN** the owner sends a body of exactly 15 MiB and one a byte over, in characters of several bytes
- **THEN** the first reaches validation and the second answers 413

#### Scenario: Unpaid portal

- **WHEN** the AI Tools service is off on a hosted portal and the owner exports
- **THEN** the export is accepted

### Requirement: Where an export may land

The caller SHALL be the author of the exported document and SHALL need the right to create in the target:
a Viewer, a non-member, a Guest (a Content creator Guest included), another user's My documents, an archived
room and Trash are refused (403), a Content creator exports into someone else's room. A repeated title SHALL
get `(1)`, `(2)`; an over-long title SHALL be truncated; a cleared thread exports nothing; simultaneous
exports with different titles each land in their folder.

#### Scenario: Member without the right

- **WHEN** a room member with the Viewer right exports into that room
- **THEN** the system answers 403

#### Scenario: Same title three times

- **WHEN** the owner exports the same title three times into one folder
- **THEN** three documents exist, the later two suffixed `(1)` and `(2)`

### Requirement: Vectorization of knowledge

`aiVectorizationStartTask` SHALL queue the indexing of `files` (a non-empty list of numeric ids, 400
otherwise) and answer 200 with no job handle: a missing file fails the whole request (404), a file outside an
agent's Knowledge folder or a Knowledge room is refused even for its owner (403), a Guest 403, anonymous 401,
AI off 403. Repeats and duplicates are accepted; an indexed file is skipped. `vectorizationStatus` and a
socket update report progress.

#### Scenario: File in My documents

- **WHEN** the owner submits a file that lies in My documents or in an ordinary room
- **THEN** the system answers 403

#### Scenario: Knowledge room without vectorization

- **WHEN** vectorization is not enabled and the owner creates a Knowledge room
- **THEN** the system answers 403

#### Scenario: Rights on a Knowledge room's files

- **WHEN** a member works with a file of a Knowledge room
- **THEN** only read, download, copy, delete and vectorization are offered

### Requirement: Searching knowledge in a chat

A round SHALL search the Knowledge rooms the composer connected (`actionArgs.knowledgeRooms`, at most 20)
or the linked Knowledge rooms of its project, limited to the rooms the caller can read, in one query across
them; inside an agent its Knowledge folder is searched when it holds files. The search tool is offered only
while vectorization is enabled and some room is searchable. `aiContextGetKnowledgeFolders` lists the
Knowledge rooms the caller can connect.

#### Scenario: Other room's knowledge

- **WHEN** a user with no access to Knowledge room A chats with room B connected
- **THEN** the content indexed for room A is not found

### Requirement: Vectorization settings

`aiSettingsGetVectorization` and `aiSettingsSetVectorization` SHALL be refused on a hosted portal (403 for
every role, AI on or off, paid or not — the gateway manages them) and allowed to DocSpace admins of a
self-hosted installation only (403 others whatever the body, 401 anonymous). Setting an embedding provider
SHALL verify the key against it (400 "invalid key"), a provider of none SHALL reset the settings, and each
change is audited. A body that is not an object answers 400.

#### Scenario: Hosted portal

- **WHEN** the owner of a hosted portal reads or sets the vectorization settings
- **THEN** the system answers 403 whatever the body

#### Scenario: Room admin on a self-hosted portal

- **WHEN** a Room admin reads the settings or sets them with a well-formed body
- **THEN** both calls answer 403

#### Scenario: Body that is not an object

- **WHEN** a DocSpace admin of a self-hosted portal sends a body that is not an object
- **THEN** the system answers 400 and nothing changes

### Requirement: Prompt library

Each member (DocSpace admin, Room admin, User; Guests 403, anonymous 401, AI off 403) SHALL own a prompt
library served by the `aiPrompts*` operations: create, update, move, delete, get-by-id and list for
prompts; create-folder, rename-folder, delete-folder, get-folder-by-id and list-folders; export and
import-bundle. Another user's prompts and folders SHALL stay invisible and unchanged; deleting a folder
deletes its prompts (unknown 404).

#### Scenario: Libraries are private

- **WHEN** a User lists prompts and exports their library after the owner created prompts
- **THEN** none of the owner's prompts appear, and two users may use the same names

#### Scenario: Folder read by id

- **WHEN** the owner calls `aiPromptsGetFolderById` with a malformed id, then with a well-formed unknown one
- **THEN** the first answers 400 and the second 200 with a null body

#### Scenario: Foreign folder

- **WHEN** a User renames or deletes the owner's folder, or moves a prompt into it
- **THEN** every call answers 404 and the owner's library is untouched

#### Scenario: Foreign prompt

- **WHEN** a User reads, edits or moves the owner's prompt
- **THEN** the read answers null, the writes are refused and the prompt is unchanged

### Requirement: Prompt names, texts and bundles

A prompt or folder name SHALL be a string of 1–255 characters (256 → 400, a rename never truncated), not
trimmed, unique case-insensitively within its folder on every write (a taken name 400 or 409); a prompt text
SHALL be a non-blank string (400) and unlimited. `aiPromptsImportBundle` with mode `merge` SHALL keep the
existing prompts byte-identical and refuse the bundle whole (400) on a collision, an invalid entry or a twin
inside it; `replace` drops the library and writes the bundle.

#### Scenario: Name limit

- **WHEN** a member creates a prompt or a folder with a 255-character name and then a 256-character one
- **THEN** the first answers 200 and is stored whole, the second 400

#### Scenario: Import modes

- **WHEN** a member imports an exported bundle with mode `merge` and then with mode `replace`
- **THEN** merge keeps the existing prompts byte-identical and refuses the bundle whole on a name collision; replace drops the library and writes the bundle

#### Scenario: Same name in another folder

- **WHEN** a member creates a prompt named like one in another folder, and like a folder
- **THEN** both are accepted

#### Scenario: Refused bundle writes nothing

- **WHEN** a bundle is refused in either mode
- **THEN** the library is exactly as it was

#### Scenario: Rename and move recheck the name

- **WHEN** a member renames or moves a prompt onto a taken name
- **THEN** the call answers 400 or 409, never 200 with success false

### Requirement: Metadata fields proposed for a file

`aiMetaGenerate` SHALL read one file as text under the caller's rights (`kind` must be `file`, `id` a
positive integer, `prompt` an optional hint; 400 otherwise) and answer, writing nothing, the custom fields
the model proposes — trimmed, deduplicated regardless of case, at most 50, each with the value the text
supports or null. A file the caller cannot read is skipped as forbidden. The route SHALL answer 403 while AI
is off.

#### Scenario: Folder is not accepted yet

- **WHEN** the owner calls `aiMetaGenerate` or `aiMetaFill` with `kind` `folder`
- **THEN** the system answers 400

### Requirement: Metadata values filled from a file

`aiMetaFill` SHALL take the same `kind`, `id` and `prompt`, propose values for the file's own fields — those
of its templates and its custom fields — and write them through the Files metadata API only with `dryRun`
false, filling empty fields unless `overwrite` is true; a value the portal refuses is reported per field and
the call fails only when nothing was written. It SHALL answer 403 while AI is off.

#### Scenario: Dry run writes nothing

- **WHEN** the owner calls `aiMetaFill` without `dryRun`
- **THEN** the proposed values are returned and the file's metadata is unchanged

### Requirement: The model and the failures of the metadata routes

The metadata routes SHALL run once, without tools, on the profile bound to GenerateMeta, else on Default
(503 when neither is bound). A provider failure SHALL be relayed with its classification in `code` and the
provider's own words in `providerMessage`: no credit 402, rate limiting 429, any other failure 502; a round
that does not finish in time 504; a text the model cannot read 4xx naming the skipped documents.

#### Scenario: Provider out of credit

- **WHEN** the model provider refuses the round for lack of credit
- **THEN** the system answers 402 with `code` and `providerMessage` instead of a bare failure

### Requirement: Billing of AI usage on a hosted portal

A chat reply SHALL be charged to the AI Tools wallet service and a web search to the AI search service; an
operation SHALL name as its source the folder the user works in — the connected folder context, else the
folder or room the chat is opened from, else the project the chat runs in — and none outside any entry.
Switching the AI Tools service off SHALL stop the spend (402 on the next send).

#### Scenario: Room named as source

- **WHEN** the owner chats from inside a room and reads the customer operations
- **THEN** the AI Tools operation names the room

### Requirement: Spending limits on the AI services

A DocSpace admin with the portal-settings right SHALL set money limits on the AI services (tools and search
together) through the `*ServiceLimit*` operations under `api/2.0/portal/payment/servicelimit`: one for the
portal (`Total`), one for every user (`Global`) and personal ones (`Custom`, per `userId`, bulk up to 1000);
a personal limit replaces the global one and the portal limit is checked on top. Missing `scope` 400, no
billing service 403, no customer 404, a second limit of one scope 409.

#### Scenario: Portal limit reached

- **WHEN** the spending of the portal in the current period reaches the `Total` limit
- **THEN** the AI services stop for every user until the period ends, even below their own limits

#### Scenario: Scope left out

- **WHEN** the owner calls `createServiceLimit` without `scope`
- **THEN** the system answers 400 and no limit is created

#### Scenario: Limit audited

- **WHEN** a DocSpace admin creates, changes or deletes a limit
- **THEN** the change is audited

### Requirement: Audit trail and real-time events of AI changes

The audit trail SHALL record: AI access enabled/disabled, profile created/updated/deleted, profile
assigned/unassigned, MCP server created/updated/deleted, vectorization and web-search settings set/reset, a
user's AI settings update, a project's model assigned, rooms linked or unlinked, and the custom quota per
agent changed/disabled/reset. Clients SHALL be told when the AI switch, a project's model or sources or a
file's vectorization status change, or a form's starter questions are ready.

#### Scenario: Switch audited

- **WHEN** the owner turns the AI switch off
- **THEN** the audit trail shows "AI access disabled" by the owner

#### Scenario: Reads are not audited

- **WHEN** a member reads any AI setting
- **THEN** the audit trail records nothing
