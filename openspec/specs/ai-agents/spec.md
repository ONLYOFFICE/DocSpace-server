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

### Requirement: BYOK keys of a hosted portal

`aiByokProviders` SHALL list, to a DocSpace admin, the providers through which a chat or image model of the
gateway catalogue can be called with the portal's own key, each with its models and a logo, leaving out
models that already have a key. `aiByokKeysList` SHALL list the stored keys by model revision id with the
model name while the catalogue still has it, the provider and a logo, and never the key itself. Other roles
403; no gateway 404.

#### Scenario: Stored key never read back

- **WHEN** a DocSpace admin sets a key through `aiByokKeysSet` and then calls `aiByokKeysList`
- **THEN** both answers carry the model revision id and the provider, and neither carries the key

### Requirement: Setting and deleting a BYOK key

`aiByokKeysSet` on a model revision id SHALL take `provider` and `key`: 404 for a model the catalogue lacks
or that is no chat or image model, 400 when that provider does not serve the model or rejects the key; a
later call replaces the earlier key. `aiByokKeysDelete` SHALL remove the key, 404 when there is none, and
SHALL succeed while the gateway catalogue is unreachable. Both are DocSpace admin only and audited.

#### Scenario: Key the provider rejects

- **WHEN** a DocSpace admin calls `aiByokKeysSet` with a key the provider's verification refuses
- **THEN** the system answers 400 with a message and stores nothing

#### Scenario: Delete without the catalogue

- **WHEN** the gateway catalogue does not answer and a DocSpace admin calls `aiByokKeysDelete` for a stored key
- **THEN** the key is removed and the audit entry names the model by its revision id

### Requirement: Models running on a BYOK key

A model with a BYOK key SHALL be called, by the chat and by the document agent alike, through the BYOK
route of its profile, `/api/2.0/ai/gateway/byok/{profileId}/...`, with a gateway token bound to that model
revision; a model the portal's offer lacks joins the catalogue through its key. A key whose provider no
longer serves the model SHALL be ignored; a key that cannot be decrypted makes the route answer 404.
Wallet charges: the billing spec.

#### Scenario: Provider dropped the model

- **WHEN** the gateway catalogue no longer lists the key's provider among the model's providers
- **THEN** the model is served on the built-in key, or leaves the catalogue if the portal's offer lacks it

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
the same without Editor, an AI project only Room manager and Content creator. Every member reads the project;
only a Room manager edits it; a non-member other than a DocSpace admin SHALL NOT read its model, sources or
skills (403).

#### Scenario: Content creator reads, Room manager writes

- **WHEN** a Content creator of the project reads the project model and then sets it
- **THEN** the read answers 200 and the write 403

#### Scenario: DocSpace admin outside the project

- **WHEN** a DocSpace admin who is not a member of a project calls `getProjectModel`, `getProjectSources` and `getRoomSkills` for it
- **THEN** each call answers 200

### Requirement: Users only in an AI project

The members of an AI project SHALL be users only: a group SHALL NOT be invited to a project with any role
(403, "The role is not available for this user type"). A Guest SHALL NOT hold any role in a project (403,
nobody added), while a User joins through an invitation link as Content creator.

#### Scenario: Guest invited to a project

- **WHEN** the owner invites a Guest to a project as Content creator by id, by email or through an invitation link
- **THEN** the system answers 403 and the Guest is not a member

#### Scenario: Group invited to a project

- **WHEN** the owner invites a group to a project with any role
- **THEN** the system answers 403 and the group is not a member

### Requirement: The model of an AI project

`getProjectModel` SHALL answer the project's own chat model as a profile id, null while none is selected or
the profile is gone (the portal's Chat binding then answers); `setProjectModel` SHALL need a project
manager (403) and a `profileId` naming a catalogue profile that can chat (missing, unknown or image-only
400, the project keeping its model), replace the previous choice, be audited and pushed to clients. An
Instructions room or an unknown id answers 404 on either route.

#### Scenario: Image profile refused

- **WHEN** a Room manager of the project sets an image-only profile on a project that runs on a chat profile
- **THEN** the system answers 400 and the project keeps its model

#### Scenario: Content creator reads the fixed model

- **WHEN** a Content creator of the project reads the model of a project bound to profile A while the portal's Chat binding is B
- **THEN** A is returned

#### Scenario: Model moved

- **WHEN** a Room manager of the project sets another chat profile on a project with an open thread
- **THEN** new rounds run on the new profile and the existing thread keeps its model

#### Scenario: Selection cannot be cleared

- **WHEN** a manager sends `setProjectModel` without a `profileId`
- **THEN** the system answers 400 and the selection stays: a selected model is replaced, never cleared

### Requirement: Sources of an AI project

`getProjectSources` SHALL list the Instructions and Knowledge rooms linked to a project, sorted by title;
`linkProjectSources` SHALL link active rooms of those two types that the caller manages and refuse the whole
call (400, linking nothing) for any other room id, an empty list, over 40 ids or more than 20 rooms of one
kind; `unlinkProjectSources` SHALL remove the listed links without checking rights on the rooms. Both writes
need a Room manager of the project (403) and are audited.

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
project, without the room being listed in the project's member section; a non-member SHALL NOT (403).
Unlinking the room or removing the member from the project SHALL end that access, while a
member's own role in the room is never lowered by the project. `getRoomSkills` for a project SHALL answer
the skills of its linked Instructions rooms to any member.

#### Scenario: Access ends with the link

- **WHEN** a Content creator of the project reads a file of a linked Knowledge room and a Room manager of the project then unlinks the room
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

### Requirement: Creating a skill from the template

`aiSkillsCreate` SHALL save a new skill file `<title>.md` in the Instructions room `destId` from a fixed
template, with no model: `name` derived from `title` as for a generated skill, `description` the title, a body
of sections to fill in. `title` SHALL be 1–165 characters after a trailing `.md` is dropped and hold a letter
or digit (400). Another kind of folder SHALL answer 400, an unknown one 404, a caller who cannot create files
there 403, all before anything is written.

#### Scenario: Title already taken

- **WHEN** a member creates a skill whose title a file of the room already has
- **THEN** the file is saved under a numbered title and the answer carries the id and the title of the new file

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

`entityId` of `aiThreadsCreate` SHALL be checked and, an AI project excepted, never used as a partition: omitted, null, empty or 0 is
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
removed from a non-project room SHALL still manage the thread they wrote there but is refused a send into it
(403).

#### Scenario: Two members of one room

- **WHEN** two members each start a thread in the same room
- **THEN** each lists only their own thread and reading the other's answers 403

#### Scenario: Removed member keeps their thread

- **WHEN** a member who wrote in a room that is not an AI project is removed from it
- **THEN** creating for that room and sending into the thread answer 403, while the thread is still read and renamed by its creator

#### Scenario: Member removed from an AI project

- **WHEN** a member who wrote a thread in an AI project is removed from the project
- **THEN** reading that thread answers 404

#### Scenario: Regeneration by a fellow member

- **WHEN** a member of the same room regenerates the owner's thread
- **THEN** the system answers 403, the reply is kept and the model is not reached

### Requirement: One thread list per user

`aiThreadsList` SHALL show the caller's threads as one list whatever `entityId` it carries, an AI project
excepted: a thread started in room A is listed for room B, a folder, a third-party room, a string that is no
id and no entity at all; the id is not checked, so a room the caller cannot open lists
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

- **WHEN** a Room manager of the project sends into a project thread naming another profile
- **THEN** the reply runs on the project's model and the thread still reports that model
- **AND** when a read member sends the same, the request does not reach the project's model (bug 82914)

#### Scenario: Picker switches the thread's model

- **WHEN** the owner sends with profile A, then with profile B into the same room thread
- **THEN** the thread reports profile B afterwards and the second reply still knows the first exchange

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
body 400. A draft is readable right after it is saved. A deleted draft SHALL stay readable until the next
background pass and be gone after it; a linked attachment SHALL stay.

#### Scenario: Another user's draft

- **WHEN** a User asks `aiAttachmentsGetMany` for the owner's draft id next to their own
- **THEN** the own draft is returned and the owner's slot is null

#### Scenario: Mixed delete

- **WHEN** a User deletes their own draft and the owner's in one `aiAttachmentsDeleteMany`, and the background pass has run
- **THEN** only the User's draft is gone

#### Scenario: Deleted draft

- **WHEN** the owner deletes a draft made from a file of their My documents, and the background pass has run
- **THEN** the draft reads as null and `getFileInfo` still answers for the file

#### Scenario: Linked attachment

- **WHEN** the owner links a draft to a message, deletes it, and the background pass has run
- **THEN** the attachment still reads back

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

### Requirement: Which drafts a link binds

`aiAttachmentsLinkToMessage` SHALL leave unlinked a draft deleted before the call, which the background pass
then removes, and SHALL leave a draft linked to a message of another thread with that message; linking a draft
to another message of the same thread SHALL move it there.

#### Scenario: Deleted draft is not linked

- **WHEN** the owner deletes a draft and then links it to a message
- **THEN** the link is accepted and the draft is still removed by the background pass

#### Scenario: Draft of another thread

- **WHEN** the owner links a draft to a message of one thread, links it to a message of a second thread and deletes that second message
- **THEN** the draft still reads back, while a draft linked only to the second message is gone

#### Scenario: Another message of the same thread

- **WHEN** the owner links a draft to one message of a thread, then to a second message of it, and deletes the second message
- **THEN** the draft reads back as not found

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

#### Scenario: Operations of the library

- **WHEN** a member uses `aiPromptsCreate`, `aiPromptsUpdate`, `aiPromptsMove`, `aiPromptsDelete`, `aiPromptsGetById`, `aiPromptsList`, `aiPromptsCreateFolder`, `aiPromptsRenameFolder`, `aiPromptsDeleteFolder`, `aiPromptsListFolders` and `aiPromptsExport`
- **THEN** `aiPromptsList` answers the prompts of one folder, the root when `folderId` is left out or empty, newest first; `aiPromptsListFolders` answers every folder in one flat list, newest first; only `aiPromptsExport` answers the whole library at once
- **AND** `aiPromptsUpdate` writes only the fields sent, `aiPromptsMove` changes only the folder, `aiPromptsRenameFolder` keeps the folder's prompts
- **AND** `aiPromptsDelete` answers success for an unknown id and on a repeat, `aiPromptsDeleteFolder` answers 404 on a repeat, and `aiPromptsGetById` answers 200 with an empty body for an unknown id

### Requirement: Document agent settings

`aiSettingsGetDocumentAgent` SHALL answer any member but a guest (403; AI off 403) with `enabled`, `profileId`,
`reasoning`, `maxToolCalls`, `maxFileBytes`, `taskLog`, `taskLogFolderId`: the defaults (on, no profile,
`default`, 150, 32 MiB, no log) until saved, a deleted profile or a log folder deleted or in the trash as null.
`aiSettingsSetDocumentAgent` SHALL let a DocSpace admin (403 otherwise) replace the whole object and answer it.

#### Scenario: Refused settings

- **WHEN** a DocSpace admin sends a missing field, an unknown `reasoning`, `maxToolCalls` or `maxFileBytes` below 1 or above 150 and 32 MiB, an unknown `profileId` or a third-party folder id
- **THEN** the system answers 400 and the stored settings stay as they were
- **AND** a log folder that does not exist or is in the trash answers 404, and one the admin cannot create files in 403

#### Scenario: Agent switched off

- **WHEN** a DocSpace admin stores `enabled` false
- **THEN** the chat of every member no longer offers the `document_agent` tool, until the settings are stored with `enabled` true

#### Scenario: Settings cannot be read

- **WHEN** a chat round cannot read the portal's document agent settings
- **THEN** the agent is off for that round instead of running on the defaults

#### Scenario: Task log

- **WHEN** `taskLog` is true and a task of the agent ends
- **THEN** the task leaves its full log as a file in `taskLogFolderId`, or next to its results when that is null, and nothing of the log is returned to the chat model

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
(503 when neither is bound). A provider failure SHALL be relayed with its classification in `code`: no
credit 402, rate limiting 429, any other failure 502; a round
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

### Requirement: Files of a project thread

A thread whose entry is an AI project SHALL keep two sets of files inside that project: its artifacts, saved
by the chat tools, and its attachments, the files sent with its messages. Each thread SHALL have sets of its
own, never shared with another thread of the project, and the thread itself SHALL NOT report where they are
stored. A thread of any other entry has neither set.

#### Scenario: Two threads of one project

- **WHEN** the creator saves an artifact into each of two threads of one project
- **THEN** `getThreadArtifacts` of each thread lists its own file only, and the two files report different `folderId` values

#### Scenario: Artifacts saved at the same moment

- **WHEN** three artifacts are saved at once into a thread that has none yet
- **THEN** the three files report one `folderId` and the thread lists three artifacts

#### Scenario: The thread does not name its folders

- **WHEN** the creator reads a thread that has an artifact
- **THEN** the answer carries no `folderId`

### Requirement: Saving an artifact of a thread

The storage route `POST internal/ai/threads/{id}/artifacts` SHALL save the sent file among the artifacts of
the caller's project thread and answer with the stored file. It SHALL answer 400 for a thread that is not in
an AI project, for an empty file and for a file over 100 MB, 404 for an unknown thread and 403 for a thread
of another user, storing nothing. A title that an artifact of the thread already has SHALL NOT replace that
artifact: the new file is stored under another title.

#### Scenario: Saved artifact is listed

- **WHEN** the creator saves `report.txt` into a thread of a project
- **THEN** the answer is the file titled `report.txt`, and `getThreadArtifacts` lists it with total 1

#### Scenario: Same title twice

- **WHEN** the creator saves `report.txt` into one thread twice
- **THEN** the thread lists two artifacts with different ids and different titles

#### Scenario: Thread outside a project

- **WHEN** the creator saves a file into a thread that has no entry
- **THEN** the system answers 400

#### Scenario: Empty file

- **WHEN** the creator saves a file of 0 bytes into a thread of a project
- **THEN** the system answers 400 and the thread lists no artifacts

#### Scenario: File over the limit

- **WHEN** the creator saves a file of 100 MB and one byte into a thread of a project
- **THEN** the system answers 400 and the thread lists no artifacts

#### Scenario: Unknown thread

- **WHEN** a file is saved into a thread id that does not exist
- **THEN** the system answers 404

#### Scenario: Thread of another member

- **WHEN** a member of the project saves a file into a thread the project's owner created
- **THEN** the system answers 403 and the thread still lists no artifacts

### Requirement: Listing the files of a thread

`getThreadArtifacts` and `getThreadAttachments` SHALL answer to the creator of a thread of the given AI project
one page of its files with the total, an empty page when it has none. They SHALL answer 404 for a thread of
another user, the project's owner included, for a thread of another project and for a deleted thread, and
403 to a caller outside the project. `count` SHALL be 1 to 100, 100 when omitted, and `startIndex` 0 or more;
any other value answers 400.

#### Scenario: Thread without files

- **WHEN** the creator lists the artifacts of a thread nothing was saved into
- **THEN** the page is empty and the total is 0

#### Scenario: One page

- **WHEN** the creator lists two artifacts with `count` 1 and `startIndex` 1
- **THEN** the page holds one file and the total is 2

#### Scenario: Project owner asks for a member's thread

- **WHEN** a member has an artifact and a sent attachment in a thread, and the project's owner calls `getThreadArtifacts` and `getThreadAttachments` for it
- **THEN** the member's own calls list the files and both calls of the owner answer 404

#### Scenario: Thread of another project

- **WHEN** the creator names the thread together with the id of another project they own
- **THEN** the system answers 404

#### Scenario: Caller outside the project

- **WHEN** a User who is not a member of the project calls `getThreadArtifacts`
- **THEN** the system answers 403

#### Scenario: Attachment that was not sent

- **WHEN** the creator uploads a file into the project and attaches it without sending a message
- **THEN** `getThreadAttachments` answers an empty page with total 0

#### Scenario: Count of zero

- **WHEN** the creator calls either list with `count` 0
- **THEN** the system answers 400

#### Scenario: Count above the maximum

- **WHEN** the creator calls either list with `count` 101
- **THEN** the system answers 400

#### Scenario: Negative start index

- **WHEN** the creator calls either list with `startIndex` -1
- **THEN** the system answers 400

#### Scenario: Largest count

- **WHEN** the creator calls either list with `count` 100 and `startIndex` 0
- **THEN** the system answers 200

### Requirement: Uploading an attachment into a project

A caller who may chat in an AI project SHALL upload a file into the project itself through a chunked session
opened by `createUploadSessionInFolder` with the project's id. The session SHALL be refused with 403 for a
caller outside the project and for a `relativePath`, and with 400 for a file that is neither a picture nor a
document text can be read from. `uploadFile` into a project SHALL answer 403.

#### Scenario: Member uploads

- **WHEN** a Content creator of the project uploads `notes.txt` through a chunked session opened in the project
- **THEN** the stored file reports the project's id as `folderId`

#### Scenario: Caller outside the project

- **WHEN** a User who is not a member opens a session in the project
- **THEN** the system answers 403

#### Scenario: Relative path

- **WHEN** the owner opens a session in the project with `relativePath` `nested`
- **THEN** the system answers 403

#### Scenario: Unsupported format

- **WHEN** the owner opens a session in the project for `data.bin`
- **THEN** the system answers 400

#### Scenario: Upload in one request

- **WHEN** the owner sends `notes.txt` to the project with `uploadFile`
- **THEN** the system answers 403

### Requirement: Copying a portal file into a project

`copyBatchItems` with an AI project as the destination SHALL copy files for a caller who may chat in the
project, storing each copy in the project as a new file whatever `conflictResolveType` is sent. It SHALL answer
403 to a caller outside the project and when the items include a folder, and 415 for a file that is neither a
picture nor a document text can be read from. `moveBatchItems` into a project SHALL answer 403.

#### Scenario: Member copies

- **WHEN** a Content creator of the project copies a file of their My documents into the project
- **THEN** the copy reports the project's id as `folderId` and an id of its own

#### Scenario: Same file twice

- **WHEN** the owner copies one file into the project twice, both times with the overwrite conflict type
- **THEN** the project holds two copies with different ids and different titles

#### Scenario: Caller outside the project

- **WHEN** a User who is not a member copies a file of their My documents into the project
- **THEN** the system answers 403

#### Scenario: Folder among the items

- **WHEN** the owner copies a folder of their My documents into the project
- **THEN** the system answers 403

#### Scenario: Unsupported format

- **WHEN** the owner copies `data.bin` into the project
- **THEN** the system answers 415

#### Scenario: Move

- **WHEN** the owner moves a file of their My documents into the project
- **THEN** the system answers 403

### Requirement: Attachment of a file stored in a project

`aiAttachmentsSaveFilesMany` for a file the caller stored in an AI project SHALL answer one attachment per
file: a repeated call for the same file SHALL return the same attachment, with the extracted text for a
document and as an image attachment for a picture. Once the caller deleted that attachment, a call for the
file SHALL answer 404 and store nothing of the batch, before the background pass and after it.

#### Scenario: Document attached twice

- **WHEN** the owner uploads `notes.txt` into the project and attaches it twice
- **THEN** both answers carry one attachment id, the kind `file`, the file's id as `entryId` and the text of the file

#### Scenario: Picture attached twice

- **WHEN** the owner uploads a PNG picture into the project and attaches it twice
- **THEN** both answers carry one attachment id and the kind `image`

#### Scenario: Deleted attachment attached again

- **WHEN** a Content creator of a project uploads a file into the project, attaches it, deletes the attachment and attaches the file again in a batch with a file of their My documents
- **THEN** the system answers 404, no attachment of the My documents file is stored, and after the next background pass the project file answers 404 and the deleted attachment reads as null

### Requirement: A sent attachment is kept with its thread

`aiAttachmentsLinkToMessage` for a message of a project thread SHALL move each linked file that the caller
stored in the project into the attachments of that thread, where `getThreadAttachments` lists it. A copy
linked this way SHALL stay after the file it was copied from is deleted. Linking in a thread outside a
project SHALL leave the file where it is, and so SHALL linking a draft deleted before the call or a draft
that a message of another thread already holds.

#### Scenario: Linked file is listed

- **WHEN** the creator uploads a file into the project, attaches it and links the attachment to a message of a project thread
- **THEN** the link answers 204, `getThreadAttachments` lists that file with total 1, and its `folderId` is no longer the project's id

#### Scenario: Copy outlives the original

- **WHEN** the creator copies a file of their My documents into the project, links the copy to a message and deletes the original
- **THEN** `getThreadAttachments` still lists the copy

#### Scenario: Two messages of one thread

- **WHEN** the creator sends one file with each of two messages of a thread
- **THEN** the thread lists both attachments and they report one `folderId`

#### Scenario: Threads of one project

- **WHEN** the creator sends a file in each of two threads of one project
- **THEN** each thread lists its own attachment only, under different `folderId` values

#### Scenario: Thread outside a project

- **WHEN** the owner links an attachment of a file in their My documents to a message of a thread that has no entry
- **THEN** the link answers 204 and the file still reports My documents as `folderId`

#### Scenario: Deleted draft in a project

- **WHEN** the creator uploads a file into the project, attaches it, deletes the attachment and links it to a message of a project thread
- **THEN** the link answers 204 and `getThreadAttachments` lists nothing with total 0

### Requirement: Privacy of the contents of a project

A file or folder inside an AI project (an artifact, an attachment, a file not sent yet) SHALL be readable by
its creator only: `getFileInfo`, `getPresignedFileUri`, `copyBatchItems`, `bulkDownload` and `getFolderInfo`
SHALL answer 403 to another member, to the project's owner and to a DocSpace admin. The creator SHALL read,
download and copy it and nothing else: `updateFile`, `deleteFile`, `moveBatchItems`, an external link and
`insertFile` SHALL answer 403.

#### Scenario: Artifact of another member

- **WHEN** another member of the project, the project's owner and a DocSpace admin each read a member's artifact, ask for its download link, copy it into their My documents and request it as an archive
- **THEN** every call answers 403

#### Scenario: Folder of another member's thread

- **WHEN** another member, the project's owner and a DocSpace admin each call `getFolderInfo` with the `folderId` of a member's artifact
- **THEN** the system answers 403

#### Scenario: File that was not sent

- **WHEN** a member uploads a file into the project, and another member, the project's owner and a DocSpace admin each read it
- **THEN** the system answers 403

#### Scenario: Creator reads, downloads and copies

- **WHEN** the creator reads an own artifact, asks for its download link and copies it into their My documents
- **THEN** every call succeeds and the copy reports My documents as `folderId`

#### Scenario: Creator cannot change the file

- **WHEN** the creator renames an own artifact, deletes it, moves it into their My documents, creates an external link to it or inserts a file into its folder
- **THEN** every call answers 403

#### Scenario: Creator opens the editor

- **WHEN** the creator calls `openEditFile` for an own artifact
- **THEN** the document opens with the edit permission false

### Requirement: Used space of a project

The project's `usedSpace` SHALL be null for a caller who may not edit the project, a DocSpace admin outside it
included, and sorting the AI rooms root by `UsedSpace` SHALL order projects by title.

#### Scenario: Used space of a project

- **WHEN** a member uploads a file into a project, and another member, the project's owner and a DocSpace admin each call `getRoomInfo`
- **THEN** `usedSpace` is positive for the owner and null for the member and the admin

#### Scenario: Used space in a room webhook

- **WHEN** the owner and a Content creator of a project each subscribe to `room.updated`, and the owner renames the project
- **THEN** the owner's payload carries `usedSpace` and the Content creator's does not

#### Scenario: Sorting by used space

- **WHEN** a member of two projects lists the AI rooms root with `sortBy` `UsedSpace` descending, and only the later title holds a file
- **THEN** the project with the earlier title comes first

### Requirement: The contents of a project are not listed

`getFolderByFolderId` of an AI project SHALL answer no files, no folders and total 0, `getFolders` an empty
list, and a search inside the project nothing, to the creator of the files, the project's owner and a DocSpace
admin alike. `getRoomInfo` of a project SHALL report `filesCount` and `foldersCount` 0. `getProjectSources`
and `getRoomSkills` of the project SHALL answer as they do for a project without files.

#### Scenario: Listing a project that holds thread files

- **WHEN** the creator of the files, the project's owner and a DocSpace admin each call `getFolderByFolderId` and `getFolders` for a project with an artifact and a file not sent yet
- **THEN** every answer holds no files and no folders, with total 0

#### Scenario: Search inside the project

- **WHEN** the creator of an artifact `report.txt` calls `getFolderByFolderId` of the project with `filterValue` `report` and `withSubFolders` true
- **THEN** the answer holds no files and total 0

#### Scenario: Counters of the project

- **WHEN** the owner reads `getRoomInfo` of a project that holds thread files
- **THEN** `filesCount` and `foldersCount` are 0

#### Scenario: Sources and skills

- **WHEN** the owner links an Instructions room to a project that holds thread files
- **THEN** `getProjectSources` lists that room and `getRoomSkills` of the project answers

### Requirement: The rooms roots list nothing inside a project

`getFolderByFolderId` and `searchFolder` of the AI rooms root, and `getFolderByFolderId` of the rooms root with
`searchArea` AiHub, SHALL list rooms only, whatever `withSubFolders` says. `getFolderByFolderId` of the rooms
root with `searchArea` Any SHALL list no entry inside a project, with `withSubFolders` true as well. No entry
inside a project SHALL count in `total` of these listings.

#### Scenario: Listing the AI rooms root

- **WHEN** another member of the project, the project's owner and a DocSpace admin each call `getFolderByFolderId` and `searchFolder` of the AI rooms root, and `getFolderByFolderId` of the rooms root with `searchArea` AiHub, for a project with an artifact and a file not sent yet
- **THEN** every answer lists the project and other rooms only, no file and no folder of a thread, with a total equal to the number of rooms listed

#### Scenario: Filtering the AI rooms root by the title of a thread folder

- **WHEN** the same callers call `getFolderByFolderId` of the AI rooms root with `filterValue` `artifacts`
- **THEN** the answer holds no folder of a thread and no file

#### Scenario: Listing the rooms root across all sections

- **WHEN** another member of the project, the project's owner and a DocSpace admin each call `getFolderByFolderId` of the rooms root with `searchArea` Any, without a filter and with `filterValue` `artifacts`, for a project with an artifact and a file not sent yet
- **THEN** no answer holds the folder of the thread, the artifact or the file not sent yet, and each total equals the number of entries listed; the unfiltered answer to the member lists the project

### Requirement: Recipients of an upload into a project

The socket event that announces a file uploaded into an AI project SHALL reach the uploader only: the
project's owner, a DocSpace admin and another member subscribed to the project SHALL NOT receive it. The
upload SHALL NOT mark the file as new for the project's owner, and the members of the project SHALL NOT be
sent the notification about a file uploaded to a room. `getSharedUsers` of a file inside a project SHALL
answer an empty list.

#### Scenario: Uploader is told

- **WHEN** a member subscribed to the project uploads a file into it
- **THEN** the member receives the event that the file was created

#### Scenario: Others are not told

- **WHEN** a member uploads a file into the project while the project's owner, a DocSpace admin or another member is subscribed to it
- **THEN** the uploader receives the event and the other subscriber receives none within 2 seconds

#### Scenario: Owner's new items

- **WHEN** a member uploads one file into a project and one into an ordinary room of the same owner
- **THEN** `getNewRoomItems` of the room lists the room's file for the owner, and `getNewRoomItems` of the project lists nothing

#### Scenario: Upload notification

- **WHEN** a member uploads a file into a project
- **THEN** no notification about a file uploaded to the room is prepared for the other members and the owner

#### Scenario: Shared users of a file

- **WHEN** a member uploads a file into a project on a portal that has a DocSpace admin, and calls `getSharedUsers` for it
- **THEN** the list is empty

### Requirement: Activity digests leave out AI projects

The hourly room activity digest and the daily feed digest SHALL leave out every event of an AI project and of
the entries inside it, for every recipient, the project's owner and DocSpace admins included.

#### Scenario: Activity digests

- **WHEN** a member uploads or creates a file in a project, or the project is created, renamed or gets a new member
- **THEN** neither the hourly room activity digest nor the daily feed digest of any recipient lists the event

### Requirement: Deleting a thread of a project

`aiThreadsDelete` of a thread in an AI project SHALL answer at once and close the thread everywhere: it
leaves the list and the search, and reading or renaming it, reading or adding a message, saving an artifact
and both file lists answer 404. Its files SHALL be removed afterwards by a background pass, every 5 minutes
by default, which then returns their size to the project's used space unless that step fails (A37). Other
threads of the project SHALL keep their files.

#### Scenario: Gone at once

- **WHEN** the creator deletes a project thread that has a message and an artifact
- **THEN** the delete answers 204, the thread is absent from the list of the project and from a search by its title, and reading it, renaming it, reading its message, adding a message, saving an artifact, `getThreadArtifacts` and `getThreadAttachments` answer 404

#### Scenario: Files removed by the background pass

- **WHEN** the creator deletes a project thread with an artifact and a sent attachment, and the background pass has run
- **THEN** reading either file answers 404 and the project's used space equals what it was before the thread

#### Scenario: Other thread of the project

- **WHEN** the creator deletes one of two threads of a project, each with an artifact, and the background pass has run
- **THEN** the other thread is still read and still lists its artifact, and the used space equals what it was with that thread alone

#### Scenario: Project deleted afterwards

- **WHEN** the owner deletes a project one of whose threads was deleted before
- **THEN** the deletion of the project finishes without an error

### Requirement: Losing access to a project deletes the member's threads

When a member's own access to an AI project is removed (by `setRoomSecurity` with no access, sent by a manager
or by the member, or by `removeSecurityInfo` sent by the project's owner), the threads that member created in
the project SHALL be deleted as by `aiThreadsDelete`, with their files. Threads of other members SHALL stay, a
later invitation SHALL NOT bring the threads back, and a change of the project's owner SHALL delete nothing.

#### Scenario: Member removed by the owner

- **WHEN** the owner removes a member who has a thread with an artifact and a sent attachment, and the background pass has run
- **THEN** the member's read of the thread, 200 before, answers 404, both files answer 404 and the project's used space equals what it was before the member's thread

#### Scenario: Member leaves

- **WHEN** a member removes their own access to the project
- **THEN** the thread answers 404 and its files are removed as above

#### Scenario: Deactivated member removed

- **WHEN** the owner removes a member whose account was disabled
- **THEN** the files of the member's thread are removed and the used space returns

#### Scenario: Other members

- **WHEN** one of two members with a thread each is removed
- **THEN** the other member still reads their thread and lists its artifact

#### Scenario: Invited again

- **WHEN** a removed member is invited to the project again as Content creator
- **THEN** the old thread and its artifact list answer 404

#### Scenario: Owner removes the project from the list

- **WHEN** the project's owner calls `removeSecurityInfo` for a project where two members have a thread each
- **THEN** both threads answer 404 and their files are removed

#### Scenario: Member removes the project from the list

- **WHEN** a member calls `removeSecurityInfo` for the project
- **THEN** the member is still in the project, still reads the thread and still lists its artifact

#### Scenario: Owner changed

- **WHEN** the project passes to a new owner who had a thread in it, the former owner having one too
- **THEN** both still read their threads and the new owner still lists their artifact

### Requirement: Converting a member to Guest removes them from the projects

When `startUserTypeUpdate` converts a user to Guest, the job SHALL remove the user's role in every AI project,
one the user owned and passed on included, and SHALL delete the threads the user created in AI projects as by
`aiThreadsDelete`, with their files. Threads of other members SHALL stay. In an AI agent the user SHALL stay a
Viewer.

#### Scenario: Member converted

- **WHEN** a User who is a Content creator of a project and has a thread there with an artifact and a sent attachment is converted to Guest, the job completes and the background pass has run
- **THEN** `getRoomSecurityInfo` of the project no longer lists the user, both files answer 404 and the project's used space equals what it was before the user's thread

#### Scenario: Thread closed when the job completes

- **WHEN** a User who reads their thread in a project is converted to Guest, the job completes and the user is converted back to User
- **THEN** the thread answers 404 to the user

#### Scenario: Other members

- **WHEN** one of two members with a thread each is converted to Guest
- **THEN** the other member still reads their thread and lists its artifact

#### Scenario: Owner of a project converted

- **WHEN** a Room admin who owns a project and has a thread there with an artifact is converted to Guest with the project passed to another account, and the background pass has run
- **THEN** the other account owns the project (its `createdBy`), `getRoomSecurityInfo` does not list the converted user, and the artifact answers 404

#### Scenario: Member of an agent converted

- **WHEN** a User who is a Content creator of an AI agent is converted to Guest
- **THEN** `getRoomSecurityInfo` of the agent lists the user as Viewer

### Requirement: Thread list of an AI project

`aiThreadsList` with the id of an AI project as `entityId` SHALL list the threads the caller started in that
project and no other thread. A thread started in a project SHALL NOT be listed for another project, for any
other `entityId` or without one.

#### Scenario: Thread started in a project

- **WHEN** the owner of two projects starts a thread in one of them and lists threads for that project, for the other project, for a room and without an entity
- **THEN** the list of that project holds exactly this thread and the other three lists do not hold it

#### Scenario: Thread started in a room

- **WHEN** the owner starts one thread in a room and one in a project, and lists threads for the project and without an entity
- **THEN** the list of the project holds the project's thread only, and the list without an entity holds the room's thread

### Requirement: Deleting a profile deletes its project threads

When a profile is deleted (`startRemove`, `deleteMember`, `removeUsers`, or `startReassign` with
`deleteProfile` true), the threads the user created in any AI project, one the user owned included, SHALL be
deleted as by `aiThreadsDelete` by the data job, with their files. Threads of other members SHALL stay. No
file or folder inside a project SHALL pass to another creator, so the new owner of the project or the
recipient of the data SHALL NOT read them.

#### Scenario: Data of a member removed

- **WHEN** the data of a disabled member who has a thread with an artifact and a sent attachment in a project is removed with `startRemove`, the job completes and the background pass has run
- **THEN** both files answer 404 and the project's used space equals what it was before the member's thread

#### Scenario: Owner of the project before the background pass

- **WHEN** the data of a disabled member with a thread in a project is removed with `startRemove` and the job completes
- **THEN** the project's owner reading the member's artifact gets 403 or 404, never 200, and 404 after the background pass

#### Scenario: Member deleted at once

- **WHEN** a disabled member who has a thread with an artifact in a project is deleted with `deleteMember`, and the background pass has run
- **THEN** the artifact answers 404 and the project's used space equals what it was before the member's thread

#### Scenario: Owner of the project deleted

- **WHEN** the data of a disabled room admin who owns a project and has a thread in it is removed with `startRemove`, while a member of the project has a thread too, and the background pass has run
- **THEN** the project passes to the caller, the files of the former owner's thread answer 404, and the member still reads their thread and lists its artifact

#### Scenario: Data transferred with the profile deleted

- **WHEN** a disabled member's data is transferred with `startReassign` and `deleteProfile` true to the project's owner, who then reads the member's artifact
- **THEN** the read answers 403 or 404, never 200, and 404 after the background pass

### Requirement: Transferring data keeps the contents of a project with their creator

`startReassign` with `deleteProfile` false SHALL NOT change the creator of a file or folder inside an AI
project, and SHALL NOT delete the threads the user created in a project the user does not own. Once enabled
again, such a member SHALL still read their thread and its artifact, and the recipient SHALL be refused the
artifact with 403, the project's owner included.

#### Scenario: Member's data transferred to the project's owner

- **WHEN** a disabled member who has a thread with an artifact in a project has their data transferred with `startReassign` and `deleteProfile` false to the project's owner, and the member is enabled again
- **THEN** the member reads the thread and the artifact, and the project's owner reading the artifact gets 403

### Requirement: Transferring a project deletes its former owner's threads

When `startReassign` with `deleteProfile` false hands a project the user owns to the recipient, the threads the
user created in that project SHALL be deleted as by `aiThreadsDelete` by the data job, with their files. The
user gets no access to the project. Threads of other members of the project SHALL stay.

#### Scenario: Owner's data transferred

- **WHEN** the data of a disabled room admin who owns a project and has a thread there with an artifact and a sent attachment is transferred with `startReassign` and `deleteProfile` false, and the background pass has run
- **THEN** the recipient owns the project (its `createdBy`), both files answer 404 and the project's used space equals what it was before the former owner's thread

#### Scenario: Members of the transferred project

- **WHEN** a project whose member has a thread with an artifact is handed over this way
- **THEN** the member still reads their thread and lists its artifact

#### Scenario: Former owner's thread in another project

- **WHEN** the former owner also has a thread with an artifact in a project owned by someone else, where the former owner is a member, and is enabled again after the transfer
- **THEN** the former owner still reads that thread and its artifact

### Requirement: Pinned projects in the root folders

`getRootFolders` SHALL return in `folders` of the AI rooms section only the AI projects the caller pinned and may
see, sorted as the listing of the section sorts pinned rooms. `total` and `count` of that section SHALL equal their
number and `startIndex` SHALL be 0, whatever paging and filters the request carries. The other sections SHALL keep
their behaviour.

#### Scenario: Only pinned projects are listed

- **WHEN** the owner creates three AI projects and an Instructions room, pins two of the projects and then the Instructions room, and calls `getRootFolders` with `filterType=2` and `count=1`
- **THEN** `folders` of the AI rooms section holds the two pinned projects with `total` and `count` 2 and `startIndex` 0
- **AND** neither the Instructions room nor the unpinned project is in it

#### Scenario: Nothing pinned

- **WHEN** the caller has pinned no AI project, or only an Instructions room
- **THEN** `folders` of the AI rooms section is empty and its `total` is 0, and the section itself is still listed

#### Scenario: Paging and filters do not cut the list

- **WHEN** ten AI projects are pinned, the most the pin limit of the AI rooms section allows, and `getRootFolders` is called with `count=1`, `startIndex=5`, a `filterValue` no project title contains and `sortBy=AZ`
- **THEN** `folders` of the AI rooms section holds all ten projects in the order of their titles, with `total` and `count` 10 and `startIndex` 0

#### Scenario: Unpinned project leaves the list

- **WHEN** a pinned project is unpinned with `unpinRoom` and `getRootFolders` is called again
- **THEN** the project is no longer in `folders` of the AI rooms section and `getRoomsFolder` with `searchArea=AiHub` still lists it

#### Scenario: Other sections are not changed

- **WHEN** the owner has two Custom rooms and a pinned AI project and calls `getRootFolders` with `count=1`
- **THEN** the Rooms section holds one room with `total` 2, as before the change

### Requirement: Who finds a pinned project in the root folders

A pinned AI project SHALL be listed in the root folders when the caller is a DocSpace admin, created the project
or holds a share of their own on it; a share held through a group SHALL NOT count. A pin SHALL be personal: the
pins of other accounts SHALL NOT be listed. A pin on a project the caller may no longer see SHALL be skipped and
kept, so the project is listed again once the caller regains access.

#### Scenario: Member pins a project

- **WHEN** the owner invites a User to a project as Content creator and the User pins it
- **THEN** the project is in `folders` of the AI rooms section of the User's `getRootFolders`
- **AND** it is not in the owner's list, since the owner did not pin it

#### Scenario: Member loses access and regains it

- **WHEN** a member who pinned a project is removed from it and calls `getRootFolders`, and is then invited again
- **THEN** the project is not listed while the member is out of it, and is listed again after the new invitation without a new pin

#### Scenario: DocSpace admin outside the project

- **WHEN** a DocSpace admin who was never invited to a project pins it
- **THEN** the project is in `folders` of the AI rooms section of the admin's `getRootFolders`, as it is in the admin's listing of the section

#### Scenario: Creator of the project

- **WHEN** a Room admin creates a project, pins it and calls `getRootFolders`
- **THEN** the project is listed, although the creator holds no share record on it

### Requirement: Drafts that are not sent are removed

A background pass, every 15 minutes by default, SHALL remove each draft its creator deleted and each draft
not linked to a message 2 hours, by default, after it was created, in a project from the upload or copy.
Any linked attachment SHALL stay. A draft the pass has taken SHALL be left unlinked by a link that comes after.

#### Scenario: Sent attachment older than the lifetime

- **WHEN** a Content creator uploads a file into a project, attaches it and links it to a message of a project thread, and 2 hours pass after the upload
- **THEN** after the next background pass `getThreadAttachments` still lists the file

### Requirement: The file of a removed draft

With a draft it removes, the background pass SHALL remove the file the draft was made from when that file still
lies in an AI project unsent, then return its size to the project's space unless that step fails (A37). Any
other file SHALL stay.

#### Scenario: Deleted draft of a file in a project

- **WHEN** a Content creator of a project uploads a file into the project, attaches it, deletes the draft, and the background pass has run
- **THEN** the draft reads as null, the file answers 404 and the project's used space equals what it was before the upload

#### Scenario: Abandoned draft of a file in a project

- **WHEN** a Content creator uploads a file into a project and attaches it, and the draft is not linked for 2 hours
- **THEN** after the next background pass the draft reads as null, the file answers 404 and the project's used space equals what it was before the upload

#### Scenario: Abandoned draft of a file outside a project

- **WHEN** the owner attaches a file of their My documents and the draft is not linked for 2 hours
- **THEN** after the next background pass the draft reads as null and `getFileInfo` still answers for the file

### Requirement: Number of projects that use a room

A Knowledge or an Instructions room SHALL carry `linkedProjectsCount` in the answers of `getRoomsFolder`,
`getRoomInfo`, `createRoom` and `updateRoom`: the number of AI projects the room is linked to, 0 when there is
none. The number SHALL count every such project, whether the caller may read it or not. A room of another type,
an AI project included, SHALL NOT carry the field.

#### Scenario: Room linked to two projects

- **WHEN** the owner links a Knowledge room to two projects, lists the section with `getRoomsFolder` and `searchArea=AiHub` and reads the room with `getRoomInfo`
- **THEN** the room carries `linkedProjectsCount` 2 in both answers

#### Scenario: Room linked to no project

- **WHEN** the owner creates an Instructions room and reads it with `getRoomInfo`
- **THEN** the answer of `createRoom` and the answer of `getRoomInfo` carry `linkedProjectsCount` 0

#### Scenario: Room changed

- **WHEN** the owner renames a Knowledge room linked to one project with `updateRoom`
- **THEN** the answer carries `linkedProjectsCount` 1

#### Scenario: Rooms of other types

- **WHEN** the owner reads an AI project and a Custom room with `getRoomInfo` and lists the section with `getRoomsFolder` and `searchArea=AiHub`
- **THEN** neither the project nor the Custom room carries `linkedProjectsCount`

#### Scenario: Reader of the room outside the projects

- **WHEN** a User who is a Viewer of a Knowledge room linked to two projects, and a member of neither, reads the room with `getRoomInfo`
- **THEN** the room carries `linkedProjectsCount` 2

#### Scenario: Number follows the links

- **WHEN** a room is linked to two projects, a Room manager of the first unlinks it with `unlinkProjectSources`, and the second project is then deleted with `deleteRoom`
- **THEN** `getRoomInfo` of the room carries `linkedProjectsCount` 1 after the unlink and 0 after the deletion has completed

### Requirement: Listing the projects that use a room

`getRoomProjects` SHALL answer one page of the AI projects a Knowledge or an Instructions room is linked to, each
with `id`, `title` and `logo`, sorted by title and then by id, with the number of listed projects as the total;
an empty page when it lists none. It SHALL need read access to the room (403) and answer 404 for any other id:
an unknown one, an AI project, a room of another type. `count` SHALL be 1 to 100, 100 when omitted, and
`startIndex` 0 or more; any other value answers 400.

#### Scenario: Projects sorted by title

- **WHEN** the owner links a Knowledge room to the projects "Gamma", "Alpha" and "Beta", in that order, and calls `getRoomProjects` for the room
- **THEN** the page holds "Alpha", "Beta" and "Gamma" in that order, each with its `id`, `title` and `logo`, and the total is 3

#### Scenario: Projects with one title

- **WHEN** the owner links an Instructions room to two projects that both have the title "Twin"
- **THEN** the page lists the project with the lower id first

#### Scenario: Room linked to no project

- **WHEN** the owner calls `getRoomProjects` for a Knowledge room no project links
- **THEN** the page is empty and the total is 0

#### Scenario: Id that is no Knowledge or Instructions room

- **WHEN** the owner calls `getRoomProjects` with the id of an AI project, of a Custom room, and with an id no room has
- **THEN** each call answers 404

#### Scenario: Caller who cannot read the room

- **WHEN** a User who has no role in an Instructions room and is a member of no project that links it calls `getRoomProjects` for it
- **THEN** the system answers 403

#### Scenario: One page

- **WHEN** the owner lists the three projects of a room with `count` 1 and `startIndex` 1
- **THEN** the page holds the second project by title and the total is 3

#### Scenario: Start index past the end

- **WHEN** the owner lists the three projects of a room with `startIndex` 3
- **THEN** the page is empty and the total is 3

#### Scenario: Count of zero

- **WHEN** the owner calls `getRoomProjects` with `count` 0
- **THEN** the system answers 400

#### Scenario: Negative count

- **WHEN** the owner calls `getRoomProjects` with `count` -1
- **THEN** the system answers 400

#### Scenario: Count above the maximum

- **WHEN** the owner calls `getRoomProjects` with `count` 101
- **THEN** the system answers 400

#### Scenario: Empty count

- **WHEN** the owner calls `getRoomProjects` with `count` sent without a value
- **THEN** the system answers 400

#### Scenario: Negative start index

- **WHEN** the owner calls `getRoomProjects` with `startIndex` -1
- **THEN** the system answers 400

#### Scenario: Largest count

- **WHEN** the owner calls `getRoomProjects` with `count` 100 and `startIndex` 0 for a room linked to three projects
- **THEN** the system answers 200 with the three projects

### Requirement: Who is listed among the projects of a room

`getRoomProjects` SHALL list a project only to a caller who may read it: a DocSpace admin, the creator of the
project, or a member who holds a role of their own in it. A project the caller may not read SHALL be left out
of the page and of the total, while `linkedProjectsCount` of the room still counts it.

#### Scenario: Member of one of two projects

- **WHEN** a User who is a Viewer of a Knowledge room and a Content creator of one of the two projects that link it calls `getRoomProjects` for the room
- **THEN** the page holds that project only and the total is 1, while `getRoomInfo` of the room carries `linkedProjectsCount` 2

#### Scenario: Reader of the room outside the projects

- **WHEN** a User who is a Viewer of a Knowledge room linked to two projects, and a member of neither, calls `getRoomProjects` for the room
- **THEN** the page is empty and the total is 0

#### Scenario: Member who reads the room through a project

- **WHEN** a User who is a Content creator of one of the two projects that link an Instructions room, and holds no role in the room, calls `getRoomProjects` for the room
- **THEN** the system answers 200 and the page holds that project only

#### Scenario: DocSpace admin outside the projects

- **WHEN** a DocSpace admin who is a member of neither of the two projects that link a room calls `getRoomProjects` for the room
- **THEN** the page holds both projects and the total is 2

#### Scenario: Creator of a project

- **WHEN** a Room admin creates a project and a Knowledge room, links the room to the project and calls `getRoomProjects` for the room
- **THEN** the page holds the project, although its creator holds no share record on it

### Requirement: Clients are told when the number of projects of a room changes

When `linkProjectSources` links a room, `unlinkProjectSources` unlinks one or `deleteRoom` deletes a project, the
clients of the accounts that may read each room whose `linkedProjectsCount` changed SHALL receive the event that
reports an updated folder for that room, after the change is stored. A call that changes the number of no room
SHALL send no such event for a room.

#### Scenario: Room linked

- **WHEN** a Room manager of a project links a Knowledge room while the owner of the portal listens to the AI rooms section
- **THEN** the owner's client receives the updated-folder event for the room

#### Scenario: Room unlinked

- **WHEN** a Room manager of a project unlinks a linked Instructions room while the owner of the portal listens to the AI rooms section
- **THEN** the owner's client receives the updated-folder event for the room

#### Scenario: Project deleted

- **WHEN** a project that links two rooms is deleted with `deleteRoom` while the owner of the portal listens to the AI rooms section, and the deletion has completed
- **THEN** the owner's client receives the updated-folder event for each of the two rooms

#### Scenario: Nothing linked anew

- **WHEN** a Room manager links a room the project already links, after a first link whose event the owner's client received
- **THEN** the owner's client receives no second updated-folder event for the room

### Requirement: Conversion of an AI agent into an AI project

An AI agent of an active portal SHALL be converted with no request from a user: an AI project is created with
the agent's title, owner, colour, cover, logo and custom tags, and the agent stops being a room of the AI room
type. An agent SHALL be converted once, and an agent the conversion has not reached yet SHALL stay unchanged.

#### Scenario: Agent converted

- **WHEN** the conversion has run for a portal that holds one agent
- **THEN** `getRoomsFolder` of the AI rooms section lists a project with the agent's title whose `createdBy` is the agent's owner, and no room of the AI room type is listed in any section

#### Scenario: Conversion run twice

- **WHEN** the conversion runs a second time for the same portal
- **THEN** the AI rooms section still lists one project for the agent

#### Scenario: Pinned agent

- **WHEN** a member had pinned the agent and the conversion has run
- **THEN** the project is pinned for that member

### Requirement: The room of a converted agent becomes a Knowledge room

An agent whose Knowledge folder holds at least one file SHALL keep its id as a Knowledge room linked to the
project and titled `<agent title> (<title of its Knowledge folder>)`; a title over the length limit of a room
title SHALL be cut inside the agent title. The knowledge files SHALL keep their ids and lie directly in the
room, and the Knowledge folder SHALL be gone; folders inside it SHALL be deleted with their files. The room SHALL be
made also while vectorization is off.

#### Scenario: Knowledge kept

- **WHEN** the conversion has run for an agent "Sales bot" with one knowledge file
- **THEN** `getRoomInfo` with the agent's id answers a Knowledge room titled "Sales bot (Knowledge)", `getProjectSources` of the project lists that room, `getFileInfo` of the file answers the same file id with the room as its `folderId`, and the id of the former Knowledge folder answers 404

#### Scenario: Title at the limit

- **WHEN** the agent's title has the greatest length a room title may have
- **THEN** the title of the Knowledge room has that same length and ends with ` (Knowledge)`

#### Scenario: Vectorization off

- **WHEN** the conversion runs for an agent with a knowledge file on a portal where vectorization is turned off
- **THEN** the Knowledge room is made and linked all the same

### Requirement: The Knowledge room of a converted agent drops the settings it refuses

The Knowledge room made from an agent SHALL keep none of the agent's lifetime, watermark, download restriction,
indexing, privacy and form settings: `updateRoom` refuses each of them for a Knowledge room and `createRoom` refuses a
private one, so one carried over could not be turned off.

#### Scenario: Restricted agent converted

- **WHEN** the conversion has run for a private agent with a knowledge file, a lifetime, a watermark, downloads denied, indexing on and both form settings on
- **THEN** `getRoomInfo` with the agent's id answers a Knowledge room with no `lifetime` and no `watermark`, and `denyDownload`, `indexing` and `private` false, and both form settings of the room are off

### Requirement: An agent without knowledge leaves no room

An agent whose Knowledge folder holds no file SHALL be deleted once its project exists: the project has no linked
room and the agent's id answers 404. When that deletion fails, a Knowledge room with the agent's id and no file stays.

#### Scenario: Empty Knowledge folder

- **WHEN** the conversion has run for an agent whose Knowledge folder is empty
- **THEN** `getProjectSources` of the project answers an empty list and `getRoomInfo` with the agent's id answers 404

### Requirement: Threads of an agent become threads of the project

Every thread started with the agent as `entityId` SHALL become a thread of the project with its messages,
whoever created it. `aiThreadsList` SHALL list it for the project's id, and no longer for another `entityId` or
without one, to its creator while the creator may use AI in the project. A Guest is refused on every thread route,
so the thread of a creator who is a Guest at the conversion stays stored in the project and answers 403 to them.

#### Scenario: Thread follows the agent

- **WHEN** a member has a thread with messages in an agent and the conversion has run
- **THEN** `aiThreadsList` with the project's id lists the thread to that member, `aiThreadsList` without an entity does not, and `aiThreadsReadMessages` of the thread answers the same messages

#### Scenario: Thread started elsewhere

- **WHEN** the same member has a thread started without an entity
- **THEN** that thread is still listed without an entity and is not listed for the project

#### Scenario: Thread of a Guest

- **WHEN** a member who started a thread in the agent is a Guest when the conversion runs
- **THEN** the thread is stored as a thread of the project, the project does not list the Guest among its members, and the Guest reading the thread gets 403

### Requirement: The model of an agent becomes the model of the project

The Chat assignment scoped to the agent SHALL become the model of the project: `getProjectModel` answers the
profile the agent was bound to, and null when the agent had no assignment of its own.

#### Scenario: Agent bound to a profile

- **WHEN** the agent carried a Chat assignment to profile A while the portal's Chat binding is B, and the conversion has run
- **THEN** `getProjectModel` of the project answers A

#### Scenario: Agent without an assignment

- **WHEN** the agent carried no Chat assignment of its own
- **THEN** `getProjectModel` of the project answers null

### Requirement: Members of a converted agent

The project SHALL take the agent's members as users only: a Room manager stays Room manager, a Content creator
stays Content creator, a Viewer becomes Content creator. Each member of a group invited to the agent, as the
group is at the conversion, SHALL become a Content creator unless the user holds a role of their own in the
agent. A Guest and an invitation link SHALL NOT be carried over.

#### Scenario: Roles carried over

- **WHEN** an agent with a Room manager, a Content creator and a Viewer is converted
- **THEN** `getRoomSecurityInfo` of the project lists the first as Room manager and the other two as Content creator

#### Scenario: Group invited to the agent

- **WHEN** a group of two Users was invited to the agent as Viewer, one of the two being also a Room manager of the agent by their own role
- **THEN** `getRoomSecurityInfo` of the project lists no group, the first User as Content creator and the second as Room manager

#### Scenario: Joining the group later

- **WHEN** a User joins that group after the conversion
- **THEN** the User is not a member of the project

#### Scenario: Guest of the agent

- **WHEN** a Guest was a Viewer of the agent
- **THEN** `getRoomSecurityInfo` of the project does not list the Guest

### Requirement: Access to the Knowledge room of a converted agent

In the Knowledge room made from an agent, the users and groups that were Room manager or Content creator of the
agent SHALL keep that role, a Guest included. Viewers and invitation links SHALL be removed from the room: a Viewer
reads it through the project only. An external link of a knowledge file SHALL be removed.

#### Scenario: Content creator keeps the room

- **WHEN** an agent with a Room manager and a Content creator is converted
- **THEN** `getRoomSecurityInfo` of the Knowledge room lists both with the role they had in the agent

#### Scenario: Viewer reads through the project

- **WHEN** a Viewer of the agent reads a knowledge file after the conversion
- **THEN** the read answers 200 and `getRoomSecurityInfo` of the Knowledge room does not list that user

#### Scenario: Guest of the agent

- **WHEN** a Guest who was a Viewer of the agent reads a knowledge file after the conversion
- **THEN** the read answers 403

#### Scenario: External link of a knowledge file

- **WHEN** a knowledge file had an external link and the conversion has run
- **THEN** the link is no longer listed among the links of the file

### Requirement: Chat outputs of a converted agent move to My documents

The Chat outputs folder of an agent SHALL move, with its content and the ids of its files, into the My documents
of the agent's owner as an ordinary folder titled `<agent title> (<title of the Chat outputs folder>)`; an empty
one SHALL be deleted. Its size SHALL be added to that user's used space with no check of the quota. If the
owner's account is deleted or disabled, the portal owner SHALL take the folder.

#### Scenario: Outputs moved

- **WHEN** the conversion has run for an agent "Sales bot" whose Chat outputs folder holds a file
- **THEN** `getMyFolder` of the agent's owner lists a folder "Sales bot (Chat outputs)", `getFileInfo` of the file answers the same file id inside that folder, and another member of the agent reading the file gets 403

#### Scenario: Empty Chat outputs

- **WHEN** the Chat outputs folder of the agent holds nothing
- **THEN** `getMyFolder` of the agent's owner lists no folder for the agent

#### Scenario: Owner over the quota

- **WHEN** the files of Chat outputs are larger than the free space left in the personal quota of the agent's owner
- **THEN** the conversion completes, the folder is in the owner's My documents and the owner's used space is above the quota

#### Scenario: Owner disabled

- **WHEN** the account of the agent's owner is disabled
- **THEN** the folder is listed by `getMyFolder` of the portal owner

### Requirement: The background pass converts agents by itself

A background pass SHALL convert the agents of every active portal changed since its previous pass, and of every
active portal on its first pass. A portal restored or transferred from an archive that holds agents SHALL be
converted by the first pass that starts after the portal became active, or by a later one when the lock of the legacy
AI providers migration is held or an agent fails; until then it holds agents. A portal that is not active SHALL be
skipped.

#### Scenario: Restored archive

- **WHEN** an archive made while the portal held an agent is restored and a pass has started after the restore completed
- **THEN** the restored portal holds a project for the agent and no room of the AI room type

#### Scenario: Portal being restored

- **WHEN** a pass runs while a portal is still being restored
- **THEN** the pass changes nothing in that portal and a later pass converts it

#### Scenario: Unchanged portal

- **WHEN** a pass runs and a portal without agents was not changed since the previous pass
- **THEN** the pass reads nothing of that portal

### Requirement: A failed conversion is retried and then left

An agent whose conversion fails SHALL stay an agent and SHALL NOT stop the other agents of the portal. The pass
SHALL try the portal again on its next passes; after the configured number of passes that ended with a failed
agent it SHALL leave the portal alone; no later pass resumes it, while the internal route still converts its agents.

#### Scenario: One agent of two fails

- **WHEN** the conversion of one of two agents of a portal fails
- **THEN** the other agent is converted and the failed one is still a room of the AI room type

#### Scenario: Failure repeated

- **WHEN** the same agent fails on every pass up to the configured number
- **THEN** the following passes do not read the portal

#### Scenario: Success after a failure

- **WHEN** an agent that failed on one pass is converted on the next
- **THEN** the portal is no longer retried and its count of failed passes starts from zero

### Requirement: Starting the conversion by request

`POST internal/ai/agents/migration`, an internal route kept for the integration tests, SHALL convert the agents
of the caller's portal before it answers: 200 with `migrated` and `failed` (this call's counts), or 409 with nothing
converted while the legacy AI providers migration holds its lock. It SHALL need the portal owner or a DocSpace admin
(others 403, no sign-in 401) and work while the AI switch is off. A repeated call converts nothing again.

#### Scenario: Owner starts the conversion

- **WHEN** the portal owner calls the route for a portal with two agents
- **THEN** the call answers 200 with `migrated` 2 and `failed` 0

#### Scenario: Portal without agents

- **WHEN** the route is called a second time, or for a portal that never held an agent
- **THEN** the call answers 200 with `migrated` 0 and `failed` 0

#### Scenario: Room admin refused

- **WHEN** a Room admin or a User calls the route
- **THEN** the system answers 403 and no agent is converted

#### Scenario: AI switched off

- **WHEN** the portal owner calls the route while the AI switch of the portal is off
- **THEN** the call answers 200 and the agents are converted
