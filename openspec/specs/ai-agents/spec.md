# ai-agents Specification

## Purpose

Defines the AI product of a portal: the portal-wide AI switch and the readiness it reports, the model
profiles (provider + model + key/URL) and their catalogue, the per-task model assignments, the AI agents
(rooms of the AI room type that live in the AI Agents section, with a fixed model and AI instructions, a
Knowledge folder and a Chat outputs folder), the chat threads and messages with streaming replies,
regeneration and tool-call approval, the per-user chat preferences, the MCP server registry and the tool
switches, the web-search configuration, the OpenAI-compatible passthrough, the editor tools, the chat
attachments, the markdown export, the metadata suggestions for a file, the vectorization of agent knowledge,
the room `.ai` skills folder, the prompt library and the spending limits on the AI services. Only the deltas
of an AI agent against an ordinary room are described here; everything a room does (title, tags, logo,
lifetime, watermark, archive, pinning, quota mechanics, roles) is in `room-types` and
`room-role-permissions`, the default agent quota and the agent count limit in `quotas`, the agent webhook
events in `webhooks-and-api-keys`.

The public routes under `/api/2.0/ai/*` are served by the AI chat service, which keeps its data through the
internal storage routes of the DocSpace AI service; the agent, settings, vectorization and export routes are
the DocSpace AI service itself. Sources: the storage-level integration tests of the AI service and the
Web.Api and Files suites, the TypeScript API suite of the `ai` area, the OpenAPI contract (operations named
by `operationId`), the product code where the tests stop, and the release notes (v3.5–v4.0). Where notes,
contract or TypeScript expectations and the server disagree this specification follows the server; the
evidence, the differences and the suspected defects are in `verification.md` next to this file.

## Requirements

### Requirement: Portal AI switch

The portal SHALL carry one AI switch, on by default, read by any signed-in member through
`getTenantAiAccessSettings` (anonymous 401) and changed through `setTenantAiAccessSettings` by the owner or
a DocSpace admin only (others 403). While it is off every AI route this specification does not name as
ungated SHALL answer 403 before the target of the call is looked up; switching it back on SHALL restore
every stored setting. The change is audited and pushed to clients.

#### Scenario: DocSpace admin turns AI off for everyone

- **WHEN** a DocSpace admin sets the switch off and a User then calls `aiProfilesList`
- **THEN** the User gets 403, and 200 again after the admin sets the switch on

#### Scenario: Room admin cannot flip the switch

- **WHEN** a Room admin, a User or a Guest calls `setTenantAiAccessSettings`
- **THEN** the system answers 403 and the switch is unchanged

#### Scenario: The switch is checked before the agent

- **WHEN** AI is off and the owner reads, updates or deletes an agent id that does not exist
- **THEN** the system answers 403; with AI on the same calls answer 404

### Requirement: AI readiness reported to the portal

`aiSettingsGet` SHALL be readable by every signed-in member, Guests included, with AI on or off, and report
`aiReady`, `systemAiEnabled`, `vectorizationEnabled`, `vectorizationNeedReset`, `embeddingModel`,
`recommendedModelForForms` and `toolPermissionMode`. On a hosted portal the first three SHALL be true only
while the switch is on and the AI Tools wallet service is paid for; on a self-hosted installation `aiReady`
SHALL be true while the switch is on and a profile exists.

#### Scenario: Unpaid hosted portal

- **WHEN** the AI Tools wallet service is not enabled on a hosted portal and the owner calls `aiSettingsGet`
- **THEN** the answer is 200 with `aiReady` false, and true once the service is paid for

#### Scenario: Read while the switch is off

- **WHEN** AI is switched off and a member calls `aiSettingsGet`
- **THEN** the system answers 200 with `aiReady` false

### Requirement: The AI Tools wallet service gates inference only

On a hosted portal the AI Tools wallet service SHALL gate the calls that run a model: `aiSendWithStream`,
`aiSendWithStreamOpenAI` and `aiRegenerateStream` SHALL answer 402 while it is off, storing nothing for the
refused turn; the gateway proxy paths SHALL answer 403. Agents, assignments, tools, attachments,
preferences, vectorization requests and export SHALL stay available. A paid service with a zero or low AI
credit balance SHALL NOT stop inference.

#### Scenario: Send refused while unpaid

- **WHEN** the AI Tools service is off and the owner calls `aiSendWithStream` on a thread
- **THEN** the system answers 402 and `aiThreadsReadMessages` shows neither the question nor a failed answer

#### Scenario: Management surface stays open

- **WHEN** the AI Tools service is off and the owner creates an agent, registers a custom MCP server and reads the tools catalogue
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
touching another user's value or the portal settings. The body SHALL carry `chatRecommendedModelVisible`
under exactly that name as a boolean; a non-boolean, an empty object or a differently cased name SHALL be
refused (400) and change nothing, while an unknown field next to a valid value is ignored.

#### Scenario: One user's preference is private

- **WHEN** the owner stores false and a User reads their own settings
- **THEN** the User still reads true

#### Scenario: Non-boolean value

- **WHEN** a member sends `chatRecommendedModelVisible` as a string
- **THEN** the system answers 400 and the stored value is unchanged

### Requirement: Tool permission mode of a user

The caller's tool permission mode — `ask` (every tool call waits for approval unless pre-approved), `auto`
(the default: a tool that opts out of approval runs at once) or `allow` (nothing waits) — SHALL be one value
per user for every chat of the portal, written through `aiSettingsSetToolMode` or
`aiPreferencesSetToolPermissionMode` and read through `aiSettingsGetToolMode` or
`aiPreferencesGetToolPermissionMode`. A value outside the three SHALL be refused (400) and leave the mode
alone.

#### Scenario: Modes round-trip

- **WHEN** a User sets `ask`, then `allow`, then `auto`, and then saves another user preference
- **THEN** each value reads back, `aiSettingsGet` reports the same `toolPermissionMode`, and the other preference leaves the mode alone

#### Scenario: Mode steers the approval pause

- **WHEN** the mode is `allow` and the model calls a tool that requires approval
- **THEN** the tool runs without a pending tool call; in `ask` the same call pauses for approval

### Requirement: Gates on the tool permission mode routes

`aiSettingsGetToolMode` and `aiSettingsSetToolMode` SHALL refuse a Guest (403) and answer 403 while the AI
switch is off, authorization before validation. `aiPreferencesGetToolPermissionMode` SHALL serve the mode
out of the portal AI settings without either check: a Guest reads it, and it stays readable while AI is off.

#### Scenario: Guest on the two read routes

- **WHEN** a Guest calls `aiSettingsGetToolMode` and then `aiPreferencesGetToolPermissionMode`
- **THEN** the first answers 403 and the second 200 with the Guest's mode

### Requirement: Profile catalogue

`aiProfilesList` and `aiProfilesGetById` SHALL return the profiles a chat can run on (id, name, provider
type, base URL, model id, capabilities, reasoning support) with the key stripped, to DocSpace admins, Room admins and Users; Guests 403, anonymous 401; malformed or empty id 400,
unknown 404. On a hosted portal the catalogue SHALL be the gateway's model list without embedding models and
without the models restricted for the portal, served whole inside an agent too.

#### Scenario: Guest cannot read the catalogue

- **WHEN** a Guest calls `aiProfilesList` or `aiProfilesGetById` with a real id
- **THEN** the system answers 403

#### Scenario: Restricted model leaves the catalogue

- **WHEN** a DocSpace admin restricts one model through `setRestrictedAiModels`
- **THEN** that profile disappears from `aiProfilesList` and reappears once the restriction is lifted

#### Scenario: Unknown id

- **WHEN** a member calls `aiProfilesGetById` with a well-formed id no profile carries
- **THEN** the system answers 404

### Requirement: Models offered by a stored profile's provider

`aiProfilesListModels` SHALL ask the provider of the stored profile named by `profileId` for the models it
currently offers: a missing or malformed `profileId` 400, an unknown one 404, a key the provider rejects
400, a provider that does not answer 502. A Guest, and any caller while the AI switch is off, SHALL be
refused (403) before the provider is dialled.

#### Scenario: Guest refused before the dial

- **WHEN** a Guest calls `aiProfilesListModels` with a real profile id
- **THEN** the system answers 403 and no request reaches the provider

### Requirement: Managing profiles

`aiProfilesCreate`, `aiProfilesUpdate` and `aiProfilesDelete` SHALL be allowed to DocSpace admins only
(403 for Room admins, Users and Guests, 401 anonymous, authentication before validation). On a hosted
portal — the AI gateway configured — the catalogue is read-only and every write SHALL answer 403, the
owner's included, before the body is looked at. Self-hosted: base URL at most 2048 characters, key at most
4096; each write is audited.

#### Scenario: Owner on a hosted portal cannot add a profile

- **WHEN** the owner of a hosted portal calls `aiProfilesCreate` with a well-formed profile
- **THEN** the system answers 403 and the catalogue is unchanged

#### Scenario: Self-hosted admin round-trips a profile

- **WHEN** a DocSpace admin of a self-hosted portal creates a profile, reads it, updates it and deletes it
- **THEN** every field written is read back, the delete answers success and the read then answers 404

#### Scenario: Room admin cannot delete

- **WHEN** a Room admin calls `aiProfilesDelete`
- **THEN** the system answers 403

### Requirement: Provider discovery

`aiProfilesListProviderModels` SHALL require `providerType` and `baseUrl` (400 naming the missing field),
refuse an unknown provider type, a malformed base URL and a loopback, private or unresolvable host with 400
before any connection, report a key the provider rejects as 400 against the key, never return a model list
from a host that is not the provider (400 or 502), and never echo the key or store a profile. Local provider
types need no key; Guests 403.

#### Scenario: Private address refused before dialling

- **WHEN** a member calls `aiProfilesListProviderModels` with a base URL on a loopback or link-local address
- **THEN** the system answers 400 and no socket is opened

#### Scenario: Wrong key

- **WHEN** a member sends a reachable provider URL with a key the provider refuses
- **THEN** the system answers 400 and the message names the key

### Requirement: Connection check of a stored profile

`aiProfilesTestConnection` SHALL take a bare profile id — missing or malformed 400, unknown 404 — probe the
profile's provider and answer 200 with a flat `{field, message}` verdict that leaks nothing; DocSpace admins,
Room admins and Users SHALL get the same verdict.

#### Scenario: Verdict of a stored profile

- **WHEN** the owner calls `aiProfilesTestConnection` with a real profile id
- **THEN** the system answers 200 with a `{field, message}` verdict that leaks nothing

### Requirement: Model restrictions and prices of a hosted portal

`getAiPrices` SHALL need a billing service and the AI gateway (403 otherwise) and a DocSpace admin.
`getRestrictedAiModels` SHALL answer an empty set while billing or the AI Tools service is off and the
barred models otherwise, to a DocSpace admin (402 when billing knows no customer). `setRestrictedAiModels`
SHALL replace the whole set: it needs the gateway, the portal-settings right, a billing customer (404),
AI Tools on (403) and a `models` list without null entries (400).

#### Scenario: Null entry

- **WHEN** the owner sends `models` holding a null entry
- **THEN** the system answers 400

#### Scenario: Restriction reaches an open conversation

- **WHEN** an agent's model is restricted and a member sends into an existing thread of that agent
- **THEN** the send answers 400 while a rename of the agent without `profileId` still answers 200

### Requirement: Tasks and scopes of the assignments

Each task — Default, Chat, Code, Summarization, Translation, TextAnalyze, ImageGeneration, OCR, Vision,
FormAnalysis, GenerateMeta — SHALL hold at most one profile per scope, the portal or one agent, named by
`entityId`. An unknown task SHALL answer 400; a scope the caller cannot open 403; an unknown scope 404
instead of a fall-back to the portal map.

#### Scenario: Unknown scope

- **WHEN** the owner reads or resolves assignments with an `entityId` that names no folder
- **THEN** the system answers 404 and does not fall back to the portal map

#### Scenario: Unknown task

- **WHEN** the owner reads or assigns a task name the list does not hold
- **THEN** the system answers 400

### Requirement: Who may read and write assignments

`aiAssignmentsGetAllAssignments`, `aiAssignmentsGetAssignment`, `aiAssignmentsResolveForAction` and
`aiAssignmentsTryResolveForAction` SHALL be readable by DocSpace admins, Room admins and Users (Guest 403,
anonymous 401); `aiAssignmentsAssign`, `aiAssignmentsBulkAssign`, `aiAssignmentsUnassign` and
`aiAssignmentsCascadeProfileDelete` SHALL be DocSpace-admin only (403).

#### Scenario: Guest cannot read

- **WHEN** a Guest calls `aiAssignmentsGetAllAssignments`
- **THEN** the system answers 403

#### Scenario: Room admin cannot bind

- **WHEN** a Room admin calls `aiAssignmentsAssign` portal-wide
- **THEN** the system answers 403

### Requirement: Resolving the model of a task

A task SHALL resolve to the binding of its scope, else to the portal binding, else to Default; Chat inside
an agent SHALL resolve to the agent's own profile while every other task resolves through the portal. A
fresh hosted portal SHALL carry Default, ImageGeneration and FormAnalysis bindings chosen from the gateway
catalogue and drop a binding to a model the gateway no longer lists;
`aiAssignmentsTryResolveForAction` with nothing to resolve answers 200 with a null body.

#### Scenario: Unassigned task falls back to Default

- **WHEN** Summarization has no binding and the owner calls `aiAssignmentsResolveForAction` for it
- **THEN** the Default profile is returned

#### Scenario: Agent's model wins for Chat

- **WHEN** Chat is resolved with an agent's id as `entityId`
- **THEN** the agent's own profile is returned, while FormAnalysis in the same scope still resolves through the portal

### Requirement: Writing assignments

`aiAssignmentsAssign` SHALL check the profile's capabilities against the task (Default exempt) and answer
200 with `success` false, leaving the binding, for a profile the catalogue does not hold or a restricted one.
`aiAssignmentsBulkAssign` SHALL apply a flat task-to-profile map all or nothing (bad key, null or malformed
id 400). `aiAssignmentsUnassign` removes one binding (repeat accepted);
`aiAssignmentsCascadeProfileDelete` drops every binding of one profile. Writes are audited.

#### Scenario: Bulk assignment is all or nothing

- **WHEN** `aiAssignmentsBulkAssign` carries an unknown task key, a null value or a malformed profile id
- **THEN** the system answers 400 and no binding changes

#### Scenario: Unknown profile is a soft refusal

- **WHEN** the owner assigns a profile id the catalogue does not hold, or one that is restricted
- **THEN** the system answers 200 with `success` false and the binding is unchanged

### Requirement: Creating an agent

`aiAgentsCreate` SHALL be allowed to the owner, DocSpace admins and Room admins (Users, Guests 403;
anonymous 401) and require `profileId` naming a chat-capable catalogue profile (missing, malformed,
unknown, restricted or image-only 400) and `prompt` (missing 400), plus the `room-types` fields. It SHALL
create an AI room in the AI Agents section with Knowledge and Chat outputs folders, bind the profile as the
agent's Chat assignment, count against the agent limit (402); `agent.created` is published.

#### Scenario: Tool fields of the body are ignored

- **WHEN** the owner posts an agent with `mcpServers` and `attachDefaultTools`
- **THEN** the agent is created and its tool scope holds no server

#### Scenario: Room admin creates an agent

- **WHEN** a Room admin posts a title, a prompt and a chat-capable `profileId`
- **THEN** the system answers 200 with the agent, `aiAgentsGet` reports the prompt as `chatSettings.prompt` and `aiAssignmentsGetAssignment` for Chat in the agent's scope returns that profile

#### Scenario: Missing prompt or profile

- **WHEN** the owner posts without `prompt`, or without `profileId`, or with `profileId` `not-a-uuid`
- **THEN** the system answers 400 and no agent is created

#### Scenario: User cannot create

- **WHEN** a User or a Guest calls `aiAgentsCreate`
- **THEN** the system answers 403

### Requirement: Listing agents

`aiAgentsList` SHALL return the AI Agents section as a folder listing: DocSpace admins see every agent,
other members only the agents they were invited to, a Guest invited as Viewer the same agent a Viewer sees;
an agent the caller is not in SHALL be absent and uncounted. Ordinary rooms SHALL never be listed. It SHALL
accept `subjectId`, `subjectOwnerId`, `excludeSubject`, `tags`, `withoutTags`, `quotaFilter`, `text`,
`sortBy`, `sortOrder`, `startIndex` and `count`. Anonymous 401.

#### Scenario: Hidden agents are not counted

- **WHEN** the owner creates an agent and a User who was not invited calls `aiAgentsList`
- **THEN** the system answers 200 with no folders, `total` 0 and `current.foldersCount` 0

#### Scenario: Invited member sees the agent

- **WHEN** a User or a Guest is added to the agent as Viewer and lists agents
- **THEN** the agent is in the list

### Requirement: Reading an agent

`aiAgentsGet` SHALL answer the agent as a room with `chatSettings` (the AI instructions) and, when a model
is bound, its `profileId`, to every member of the agent and to DocSpace admins; a member not in the agent
403; a non-positive or non-numeric id 400; an id that names no agent — an ordinary room included — 404;
anonymous 401. A plain member SHALL read the AI instructions; HTML stored in them SHALL reach another member
neutralised.

#### Scenario: Non-member refused

- **WHEN** a Room admin, a User or a Guest who was not invited reads the agent
- **THEN** the system answers 403; a DocSpace admin reads it with 200

#### Scenario: Ordinary room by id

- **WHEN** the owner calls `aiAgentsGet` with the id of a Custom room
- **THEN** the system answers 404

### Requirement: Who may update an agent

`aiAgentsUpdate` SHALL be allowed to the agent's Room managers only — its creator included; the portal
owner or a DocSpace admin who is not a member, a Content creator and a Viewer get 403, anonymous 401, an
unknown agent 404. The change is audited and `agent.updated` is published.

#### Scenario: Content creator cannot update

- **WHEN** a member invited as Content creator renames the agent
- **THEN** the system answers 403

#### Scenario: Portal owner outside the agent

- **WHEN** the owner updates an agent created by a DocSpace admin who did not invite them
- **THEN** the system answers 403

### Requirement: What an update changes

`aiAgentsUpdate` SHALL accept the room fields of `room-types` plus `chatSettings.prompt` and `profileId`;
a flat `prompt` SHALL be refused (400). Leaving `profileId` out SHALL keep the model; a malformed, unknown,
restricted or image-only `profileId` SHALL be refused atomically; a valid new one SHALL replace the Chat
assignment and leave title, tags, prompt and existing threads alone.

#### Scenario: Flat prompt

- **WHEN** a Room manager sends `prompt` at the top level of the body
- **THEN** the system answers 400 and the AI instructions are unchanged

#### Scenario: Model moved

- **WHEN** a Room manager sends only `profileId` of another chat-capable profile
- **THEN** the system answers 200, Chat in the agent's scope resolves to the new profile and title, tags and prompt are untouched

### Requirement: Deleting an agent

`aiAgentsDelete` SHALL be allowed to a caller who may delete the room — its creator, a Room manager, a
DocSpace admin — and refused to a User, a Guest or a non-member (403, anonymous 401, unknown id 404). It
SHALL queue a file operation (200 with the operation) and, once done, remove the agent for every member
together with its threads, its Chat assignment, its MCP server scope and its tool switches; `agent.deleted`
is published.

#### Scenario: Owner deletes

- **WHEN** the owner deletes an agent and waits for the operation
- **THEN** `aiAgentsGet` answers 404 for the owner and for every member, and `aiAssignmentsGetAllAssignments` in the agent's scope falls back to the portal

#### Scenario: Unknown agent

- **WHEN** the owner deletes an id that names no agent
- **THEN** the system answers 404

### Requirement: Per-agent storage quota

`aiAgentsUpdateQuota` SHALL set, and `aiAgentsResetQuota` reset to the portal default, the storage quota of
the listed agents for a caller who manages them — the owner, a DocSpace admin, a Room admin on their own
agent (Users, Guests and non-managers 403, anonymous 401). A quota above the total portal storage, or any
custom quota while the AI agent quota feature is off, SHALL be refused (403). Each call is audited.

#### Scenario: Several agents at once

- **WHEN** the owner sends two agent ids and a quota
- **THEN** both agents come back with the new quota

#### Scenario: Larger than the portal

- **WHEN** the owner sets a quota larger than the portal's total storage
- **THEN** the system answers 403

### Requirement: Agent news

`aiAgentsNews` SHALL return the unread items of the caller's agents grouped by date, empty when nothing is
new, to any member (Guest included) and 401 to anonymous.

#### Scenario: Nothing new

- **WHEN** a member with an agent and no new items calls `aiAgentsNews`
- **THEN** the system answers 200 with an empty list

### Requirement: An agent as a room: the deltas

An agent SHALL offer only the Room manager, Content creator and Viewer roles; a Guest SHALL be accepted as
Viewer only (403 otherwise). Its root SHALL hold no files: an upload to the root answers 403, while
Knowledge and Chat outputs take files from a Content creator or Room manager. Knowledge SHALL accept only
indexable formats (403 otherwise) and no subfolders; Chat outputs stays out of the agent's index. An agent
SHALL NOT become a room template.

#### Scenario: Guest as Content creator

- **WHEN** a Guest is invited to an agent as Content creator or Room manager
- **THEN** the system answers 403; the same Guest as Viewer answers 200

#### Scenario: Upload to the agent root

- **WHEN** the owner uploads a file to the agent itself and then to its Chat outputs folder
- **THEN** the first upload answers 403 and the second 200

#### Scenario: Knowledge takes only indexable files

- **WHEN** a member uploads an archive into Knowledge
- **THEN** the system answers 403, and a text document is accepted and queued for vectorization

### Requirement: Starting a chat thread

`aiThreadsCreate` SHALL start a thread for the caller — a DocSpace admin, a Room admin or a User; Guests
403, anonymous 401 — with a required non-blank `title` of at most 255 characters (400) and an optional
`profileId` that must name a catalogue profile (404). The thread SHALL belong to its creator alone and SHALL
be listed empty until a message is appended; parallel creates SHALL all succeed with distinct ids.

#### Scenario: Blank title

- **WHEN** the owner sends an empty or whitespace title
- **THEN** the system answers 400 and nothing is listed

#### Scenario: Title at the limit

- **WHEN** the owner sends a 255-character title and then a 256-character one
- **THEN** the first is stored whole and the second answers 400

### Requirement: Where a thread may be started

`entityId` of `aiThreadsCreate` SHALL name the scope: omitted or null is the portal-wide scope, a
non-numeric value 400, one naming no folder 404, one the caller may not use for AI 403. In an agent the
caller SHALL be a member with more than the Viewer right (a Viewer, a non-member of any role and a Guest
403); in an ordinary room or folder a member with the Read right may start a thread, a non-member 403.

#### Scenario: Viewer of an agent

- **WHEN** a member invited to an agent as Viewer starts a thread in it
- **THEN** the system answers 403

#### Scenario: Non-member of an agent

- **WHEN** a DocSpace admin, a Room admin or a User who is not in the agent starts a thread in it
- **THEN** the system answers 403 and no thread is left behind

### Requirement: Thread privacy

A thread SHALL be visible to its creator only: `aiThreadsGetById`, `aiThreadsReadMessages`,
`aiThreadsRename`, `aiThreadsDelete`, `aiThreadsClearMessages`, `aiThreadsTouch`,
`aiThreadsOpenOrCreate`, every message route and a send into it SHALL answer 403 to any other member, a
DocSpace admin or a member of the same agent included. A member removed from a room SHALL still read and
rename the thread they wrote there, but SHALL be refused a send into it (403, the model is not reached).

#### Scenario: Two members of one agent

- **WHEN** two members each start a thread in the same agent
- **THEN** each lists only their own thread and reading the other's answers 403

#### Scenario: Removed member keeps their thread

- **WHEN** a member who wrote in a room is removed from it
- **THEN** listing or creating for that room answers 403, while the thread itself is still read and renamed by its creator

### Requirement: Thread lists per scope

`aiThreadsList` SHALL show, per caller, the threads of one agent when its id is the `entityId`, and one
shared list for the portal-wide scope and every room and folder (BUG 82855, closed by design: isolation
exists inside agents only). Listing for a room the caller cannot open SHALL answer 403; an `entityId` that
names nothing SHALL be answered as the shared list. A thread SHALL survive the archiving or deletion of the
room it was started in.

#### Scenario: Rooms share one list

- **WHEN** a member starts a thread in room A and lists threads for room B or for a folder
- **THEN** the thread appears in both lists; a thread of an agent appears in that agent's list only

#### Scenario: Room the caller cannot open

- **WHEN** a User who is not in a Custom room lists threads with that room as `entityId`
- **THEN** the system answers 403

### Requirement: Listing, paging and searching threads

`aiThreadsList` SHALL return the caller's threads most recently edited first, `count` pages of at least 1
(0 or less 400), a keyset `cursor` that visits every thread once, and `query` as a case-insensitive
substring of the title with `%` and `_` taken literally. A deleted thread SHALL leave the list; a touch
SHALL move the thread to the top and never move its date back.

#### Scenario: Paging by cursor

- **WHEN** the owner walks the list with `count` 1 and the returned cursor
- **THEN** every thread is visited exactly once and the last page carries no cursor

#### Scenario: Search

- **WHEN** the owner lists with `query` `REPORT` and a thread is titled `Monthly report`
- **THEN** only that thread is returned

### Requirement: Renaming, deleting and clearing a thread

`aiThreadsRename` SHALL store the new title (missing, blank or over 255 characters 400; unknown thread
404) and ignore a `profileId` sent with it; `aiThreadsDelete` SHALL remove the thread with its messages and
attachments (unknown or already deleted 404); `aiThreadsClearMessages` SHALL empty the thread and keep its
identity (repeat 200, unknown 404). A malformed id SHALL answer 400 on every route.

#### Scenario: Cleared thread carries on

- **WHEN** the owner clears a thread with a conversation and sends a new question
- **THEN** the thread answers again and no longer knows the cleared history

#### Scenario: Rename mid-reply

- **WHEN** the owner renames a thread while its reply is still streaming
- **THEN** the rename survives the reply

### Requirement: Touching and reopening a thread

`aiThreadsTouch` SHALL bump `lastEditDate` and change nothing else; `aiThreadsOpenOrCreate` with a
`threadId` SHALL replay the thread's history read-only, attachments included, and leave its model alone
whatever profile the request carries; a deleted thread SHALL be unreachable on every route (404).

#### Scenario: Reopened with another profile

- **WHEN** the owner reopens a thread with another profile object
- **THEN** the history is replayed and the thread still reports its own model

### Requirement: Regenerating a thread title

`aiThreadsRegenerateTitle` SHALL take `threadId` and the resolved `profile` object (either missing 400, a
malformed, unknown or deleted thread 404 or 400 without a change to the title), ask the model for a title
from the thread's first user message, store it and return it; a thread with no user message has nothing to
title. It SHALL be refused for another user's thread (403) and while AI is off (403).

#### Scenario: Refused request leaves the title

- **WHEN** the owner sends a malformed thread id, or a real one without the profile
- **THEN** the system refuses the call and the thread keeps its title

### Requirement: Messages of a thread

`aiThreadsAppendUserMessage` SHALL store a user message verbatim (markdown, links, Unicode, inline
attachment records) without drawing an answer; `aiThreadsGetMessageById` SHALL answer the message (unknown
404, malformed 400); `aiThreadsUpdateMessage` SHALL rewrite the content in place, keeping the position and
the standing answer (unknown 404); `aiThreadsDeleteMessage` SHALL be idempotent (200 unknown, 400
malformed). Another user's message SHALL answer 403 on every route.

#### Scenario: Edited question stays in place

- **WHEN** the owner rewrites the first question of a two-turn thread
- **THEN** `aiThreadsReadMessages` shows the new text first and the assistant reply still after it

#### Scenario: Another member's message

- **WHEN** a User reads, rewrites or deletes a message of the owner's thread
- **THEN** the system answers 403

### Requirement: Reading the history

`aiThreadsReadMessages` SHALL page the thread oldest first with `count` of at least 1 (0 or less 400) and
a keyset cursor that returns every message once and ends on an empty page; `direction` `desc` SHALL page
back from the newest. An unknown, deleted or malformed `threadId` SHALL be refused (404 / 400). The
assistant text SHALL be served as raw markdown, identical to the streamed text.

#### Scenario: Paging back

- **WHEN** the owner reads a ten-message thread with `count` 3 and `direction` `desc`
- **THEN** the pages start at the newest message and together hold every message once

### Requirement: Sending a message with a streamed reply

`aiSendWithStream` SHALL store the user message, run the thread's model and stream the reply as
newline-delimited chat events ending on the stored assistant text; without
`threadId` it SHALL open a new thread titled "New chat" and rename it from the first question afterwards. A
profile sent with the message outside an agent SHALL become the thread's model; a provider refusal SHALL
fail the turn and leave the thread usable; a client hang-up SHALL stop the generation.

#### Scenario: First send opens the thread

- **WHEN** the owner calls `aiSendWithStream` with a prompt and no `threadId`
- **THEN** the system answers 200, a new thread is listed and the user message and the reply are stored

#### Scenario: Context stays inside the thread

- **WHEN** the owner teaches a fact in thread A and asks for it in thread B of the same agent
- **THEN** thread B does not know it, while a second question in thread A does

#### Scenario: Hang-up cancels

- **WHEN** the client closes the connection mid-stream
- **THEN** the generation stops and the thread accepts the next message

### Requirement: Refusals of a send

`aiSendWithStream` SHALL refuse before the stream opens: an empty message 400, an unknown thread 404, a
`profileId` naming no model 400 (in an agent too), another user's thread 403, a Guest 403, AI off 403, the
AI Tools service unpaid on a hosted portal 402, a body over the request size limit 413 without touching the
thread. A refused send SHALL store neither the question nor an answer.

#### Scenario: Unknown profile

- **WHEN** the owner sends with a well-formed `profileId` no profile carries
- **THEN** the system answers 400 and the thread is unchanged

#### Scenario: Oversized body

- **WHEN** the owner sends a message larger than the request size limit
- **THEN** the system answers 413, no frame is streamed and the next ordinary message is answered

### Requirement: The model of a chat

Inside an agent every send and regeneration SHALL run on the agent's profile whatever `profileId` it
carries, and SHALL NOT move the thread onto another model. Outside an agent a send with no `profileId` SHALL
run on the thread's own model when it has one, else on the Chat binding of the scope, else on Default; an
agent picked in the composer SHALL answer on its own model and come back when the thread is reopened. An
image-only profile SHALL NOT be used as a chat model (400).

#### Scenario: Agent answers on its model

- **WHEN** a member sends into an agent thread naming another profile
- **THEN** the reply runs on the profile bound to the agent and the thread still reports that profile

#### Scenario: Picker switches the thread's model

- **WHEN** the owner sends with profile A, then with profile B into the same room thread
- **THEN** the thread reports profile B afterwards and the second reply still knows the first exchange

### Requirement: One-shot sends

`aiSend` and `aiSendCustom` SHALL run the model once for a DocSpace admin, a Room admin or a User (Guest
403) and persist nothing: no thread is created and two calls share no history. An empty message SHALL be
refused (400); a task type without its own binding resolves through Default; a nonexistent `entityId`
falls back to the portal binding and an inaccessible one answers 403. `aiSendCustom` SHALL take
`systemPrompt` and `isStream`.

#### Scenario: Two one-shot calls

- **WHEN** the owner calls `aiSend` twice
- **THEN** no thread is created and the second call does not see the first

### Requirement: OpenAI-shaped send

`aiSendWithStreamOpenAI` SHALL persist like `aiSendWithStream` — a new thread when `threadId` is missing,
the thread's model kept without `profileId`, an explicit profile moving it outside an agent — and stream
OpenAI `chat.completion.chunk` events as server-sent events ended by `[DONE]`, surfacing a tool call as
`tool_calls` with the matching finish reason and a model failure inside the stream with 200. A hang-up SHALL
cancel the generation and leave the thread usable.

#### Scenario: OpenAI stream

- **WHEN** the owner calls `aiSendWithStreamOpenAI`
- **THEN** the answer is a server-sent-events stream of chunks terminated by `[DONE]` and both messages are stored in the thread

### Requirement: Regenerating a reply

`aiRegenerateStream` SHALL delete the last assistant reply of the thread, keep the question and every
earlier turn, and stream a new reply on the thread's model; a tool chain of the old reply SHALL be replaced
whole. A thread with nothing to regenerate SHALL answer 200, an unknown thread 404, another user's thread
403 (a member of the same agent included), AI off 403.

#### Scenario: Multi-turn thread

- **WHEN** the owner regenerates a thread of three exchanges
- **THEN** only the last reply changes and the three questions stay

### Requirement: Tool calls and the approval pause

A tool the model wants to run SHALL pause the stream with a pending tool call that carries the model's
announcement; `aiApproveToolCall` (with a string or structured `result`, optionally `allowAlways`) and
`aiDenyToolCall` SHALL resume it, the refusal going to the model. A tool that opted out of approval SHALL
run unasked; a pre-approved tool SHALL run unasked; a tool that answers "unavailable" or was withdrawn SHALL
finish the reply instead of stalling.

#### Scenario: Two-hop chain

- **WHEN** the model calls tool A, the owner approves, the model calls tool B and the owner approves again
- **THEN** both calls, both results and the final answer are stored in order

#### Scenario: Deny

- **WHEN** the owner denies a pending document generator call
- **THEN** no file is written and the reply finishes

### Requirement: Decisions on a tool call

`aiApproveToolCall` and `aiDenyToolCall` SHALL identify the call by `threadId`, `messageId` and `idx`; a
`message` snapshot in the body SHALL be accepted and ignored. A message without a pending tool call, an
unknown message id or an index out of range SHALL answer 409. Both routes SHALL answer 401 anonymous, 403
to a non-member of the thread and 403 while AI is off.

#### Scenario: Nothing to decide

- **WHEN** the owner approves a message that holds no tool call
- **THEN** the system answers 409 and the thread is unchanged

### Requirement: Deep mode and reasoning level

`aiPreferencesSetDeepMode`, `aiPreferencesGetDeepMode`, `aiPreferencesIsDeepModeSet`,
`aiPreferencesClearDeepMode`, `aiPreferencesSetReasoningLevel` and `aiPreferencesGetReasoningLevel` SHALL
keep, per user and per scope, whether extended thinking is on and its depth (`none`, `minimal`, `low`,
`medium`, `high`), off and unset by default. Switching on after off, or without a chosen depth, lands on
`medium`; switching off replaces a stored depth; clearing returns the scope to unset.

#### Scenario: One user's value

- **WHEN** the owner switches deep mode on and a User reads their own
- **THEN** the User still reads off and unset

#### Scenario: Agent scope is its own

- **WHEN** a member sets deep mode on portal-wide and reads it for an agent with no value of its own
- **THEN** the agent reads unset

#### Scenario: Clearing one agent

- **WHEN** a member clears the deep mode of one agent
- **THEN** that agent reads unset and the portal-wide and other agents' values stay

### Requirement: Scopes and refusals of the preferences

The scopes of the preferences SHALL be the portal and each agent: a room, a folder or an unknown `entityId`
SHALL read and write the portal-wide value, a malformed or thread `entityId` SHALL answer 400 and write
nowhere, an agent the caller cannot open 403. A non-boolean switch value, an empty body or a depth outside
the five SHALL answer 400 and change nothing. Guests SHALL be refused (403), anonymous 401, and every route
SHALL answer 403 while AI is off.

#### Scenario: Room is the portal scope

- **WHEN** a member sets deep mode on with a Custom room as `entityId` and reads it portal-wide
- **THEN** the portal-wide value reads on

#### Scenario: Thread as scope

- **WHEN** a member sends a thread id as `entityId` to any write route
- **THEN** the system answers 400 and no scope changes

### Requirement: Custom MCP servers

`aiToolsAddCustomServer`, `aiToolsUpdateCustomServer`, `aiToolsRemoveCustomServer` and
`aiToolsReplaceAllCustomServers` SHALL be allowed to DocSpace admins only (others 403; anonymous 401);
`aiToolsGetCustomServer` and `aiToolsListCustomServers` SHALL be readable by
DocSpace admins, Room admins and Users of the scope (Guest 403, outsider of an agent 403). Registration SHALL
NOT dial the server; a config comes back verbatim and is replaced whole; each change is audited.

#### Scenario: Round trip

- **WHEN** the owner adds a server with an HTTP config, reads it, updates it and removes it
- **THEN** the config comes back verbatim, the update replaces it whole and the remove answers 200

#### Scenario: Member cannot register

- **WHEN** a Room admin or a User in the agent registers or removes a server
- **THEN** the system answers 403 and the scope is unchanged

### Requirement: Scopes of the MCP registry

The portal and every agent SHALL be separate server scopes; a server registered for one agent SHALL be
invisible to another agent and to the portal list, and a portal server SHALL not be listed for an agent. A
write against an unknown or deleted agent SHALL answer 404 while a read with such an `entityId` falls back
to the portal scope. Adding a name without a config in an agent SHALL copy the portal server of that name
(none 400); copy and original are then independent.

#### Scenario: Scoped to an agent

- **WHEN** the owner registers a server for agent A
- **THEN** agent B and the portal-wide list do not show it, and a portal-level server is not listed for an agent

#### Scenario: Copy of a portal server

- **WHEN** the owner adds a name with no config in an agent while the portal holds a server of that name
- **THEN** the agent gets a copy whose later update leaves the portal original alone

### Requirement: Server names

A server name SHALL be a non-empty string of at most 128 characters, matched case-insensitively while
keeping its spelling, surrounding whitespace included. `.`, `..`, a path separator or a control character
SHALL be a hard refusal (400) on add, update and replace-all; an underscore SHALL be a soft refusal (200
with `success` false — the separator of the tool token is reserved); a duplicate name or an unknown name on
update SHALL be soft refusals; removing an unknown name is a no-op.

#### Scenario: Underscore in the name

- **WHEN** the owner adds `my_server`
- **THEN** the system answers 200 with `success` false naming the `name` field and nothing is stored

#### Scenario: Name at the limit

- **WHEN** the owner adds a name of 128 characters and then one of 129
- **THEN** the first is stored and the second refused

### Requirement: Server configurations and replace-all

A config SHALL be stdio or HTTP, never both — a config carrying both `url` and `command`, or a `url` or
`command` of the wrong type, SHALL be refused and store nothing, and a missing config is a hard 400.
`aiToolsReplaceAllCustomServers` SHALL require a `map` object (missing, null, string, number or array 400),
leave the scope as exactly the map sent, and apply nothing when one entry is invalid, reporting every bad
entry in `errors`; an empty map SHALL clear the scope.

#### Scenario: Both url and command

- **WHEN** the owner adds a server whose config carries both `url` and `command`
- **THEN** the system answers 200 with an error and nothing is stored

#### Scenario: Replace-all with one bad entry

- **WHEN** the owner replaces the portal map with two valid servers and one invalid config
- **THEN** the scope keeps its previous servers and `errors` names the bad one

### Requirement: Switching tools off

`aiToolsSetDisabled`, `aiToolsGetDisabled` and `aiToolsIsToolDisabled` SHALL keep, per user and per scope
(portal or one agent), the tools switched off for a `serverType` that is a built-in group or a registered
server (400 otherwise; missing or non-array `toolNames` 400), the list being a full replacement — an empty
list switches everything back on. One user's list SHALL be invisible to another; a server removed or
dropped by replace-all takes its entries with it. Guests 403.

#### Scenario: Disable and re-enable

- **WHEN** the owner disables one tool of a registered server and then sends an empty list
- **THEN** the tool reads disabled and then enabled again

#### Scenario: Dropped server loses its entries

- **WHEN** a server with disabled tools is left out of a replace-all and later registered again
- **THEN** the new server starts with nothing disabled

### Requirement: Pre-approving tools

`aiToolsSetAllowAlways`, `aiToolsGetAllowAlways` and `aiToolsIsAllowAlways` SHALL keep, per user and per
scope, the flat `serverType_toolName` pre-approvals: setting is idempotent, revoking one entry touches no
other, a falsy or missing value grants nothing (400), and the portal and an agent hold separate lists. The
list SHALL be independent of the disabled list: approving a tool does not enable it and disabling one does
not revoke it; `allowAlways` on an approval lands in the same list.

#### Scenario: One user's approval

- **WHEN** a member pre-approves a tool and the owner reads their own list
- **THEN** the owner's list does not carry it

#### Scenario: Approval with allowAlways

- **WHEN** the owner approves a pending call with `allowAlways` true and the model calls the same tool again
- **THEN** the second call runs without a pause and `aiToolsIsAllowAlways` answers true for it

### Requirement: Tools catalogue

`aiToolsListSystemTools` SHALL answer the scope's tools grouped by server type, the system group keys in
`system` and in `errors` the reason a registered server delivered none, to every signed-in member, Guests
included, with AI on or off. The catalogue lists no tool twice and is not the toolset the model receives:
the built-in DocSpace tools stay offered to the model whatever is written to the disabled list.

#### Scenario: Unreachable registered server

- **WHEN** a server that does not answer is registered and the owner reads the catalogue
- **THEN** the system answers 200 and names the server in `errors`

### Requirement: Web search through the AI search add-on

On a hosted portal the web-search provider SHALL come from the AI search add-on of the wallet: enabling it
configures the provider for every scope (portal, agent, room, folder) and gives the model a web-search tool
whose replies carry sources; switching it off takes the tool away. Each search is charged to the AI search
service.

#### Scenario: Add-on configures search

- **WHEN** the owner enables the AI search add-on
- **THEN** `aiWebSearchIsConfigured` answers true for the portal, an agent, a room and a folder, and the model's reply to a search question carries sources

### Requirement: Buying the add-on and the manual configuration routes

The AI search add-on SHALL require the AI Tools service and the AI switch (403 otherwise) and be bought by
the owner or a DocSpace admin only. While the add-on owns the provider `aiWebSearchConfigure`,
`aiWebSearchSetActiveConfig` and `aiWebSearchClear` SHALL answer 403 to everyone and store nothing;
clearing an unconfigured scope is accepted.

#### Scenario: Room admin cannot buy

- **WHEN** a Room admin switches the AI search add-on on
- **THEN** the system answers 403 and the provider stays unconfigured

#### Scenario: Owner cannot clear the add-on config

- **WHEN** the owner or a DocSpace admin calls `aiWebSearchClear` while the add-on is on
- **THEN** the system answers 403 and the configuration survives

### Requirement: Reading and probing the web-search configuration

`aiWebSearchIsConfigured` and `aiWebSearchGetActiveConfig` SHALL be readable by DocSpace admins, Room
admins and Users (Guest 403, anonymous 401, AI off 403), never return the key; unknown `entityId` 404, an
empty one is the portal. `aiWebSearchTestConnection` SHALL probe a configuration
taken from the request and store nothing. A private, loopback, unresolvable or malformed base URL, a
non-http scheme or credentials in the URL SHALL be refused (400) before any connection.

#### Scenario: Private base URL

- **WHEN** the owner probes or configures a provider whose base URL points at a loopback address
- **THEN** the system answers 400 and nothing is dialled or stored

#### Scenario: Guest cannot read

- **WHEN** a Guest calls `aiWebSearchIsConfigured`
- **THEN** the system answers 403 while a User reads that search is available

### Requirement: Web-search passthrough

`aiWebSearchPassthroughSearch` and `aiWebSearchPassthroughContents` SHALL relay the provider's response to
DocSpace admins, Room admins and Users (Guest 403, anonymous 401, AI off 403), answer 404 while no provider
is configured, refuse a body the gateway rejects with 400 before any search is billed, and charge each search
to the AI search service, attributed to a document the caller can open when one is named.

#### Scenario: No provider

- **WHEN** the add-on is off and the owner searches
- **THEN** the system answers 404; after enabling the add-on a search answers 200 with results and no secret

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
whose required arguments are declared, `requireApproval` off for read tools and on for the ones that change
something — identically to every member and on repeated reads, a Guest getting it without the document
generators. Anonymous 401; AI off 403.

#### Scenario: Guest catalogue

- **WHEN** a Guest and a User read the catalogue
- **THEN** the User's list holds the document generators and the Guest's does not

### Requirement: Calling an editor tool

`aiEditorToolsCall` SHALL run one tool by name within the caller's own rights (a Guest may call none),
report a failing tool in `result` with 200, answer 400 for an unknown name and for a missing or mistyped
required argument, scope a generator into the room named by `entityId` only when the caller may write there
(nothing lands otherwise), and fall back to My documents without `entityId`. Body validation runs
before the AI switch (400 before 403); while off no tool changes anything.

#### Scenario: Tool never exceeds the member

- **WHEN** a User calls the read, rename, delete or archive tool on the owner's data
- **THEN** every call reports failure inside `result` and nothing changes

#### Scenario: Generator in a room

- **WHEN** a Content creator of a room names it as `entityId` for a document generator
- **THEN** the document is created in that room

### Requirement: Creating chat attachments

`aiAttachmentsSaveFile` and `aiAttachmentsSaveFilesMany` SHALL turn a DocSpace file the caller can read
(`path` is the file id; empty 400, no such file 404, unreadable 403, an archive or a file without text 400)
into a draft the caller owns: a picture an image draft with the file's bytes, anything else a text draft
with the extracted text, whatever the request body limit. A batch SHALL be all or nothing, collapse
repeated paths and keep its order. Guests 403; anonymous 401; AI off 403.

#### Scenario: Attach by id

- **WHEN** the owner attaches a stored document by its id
- **THEN** the system answers 200 with a draft carrying the extracted text, and a large file attaches whole

#### Scenario: Guest with a shared file

- **WHEN** a Guest attaches a file shared with them
- **THEN** the system answers 403

### Requirement: Reading and deleting attachments

`aiAttachmentsGet`, `aiAttachmentsGetMany`, `aiAttachmentsDelete` and `aiAttachmentsDeleteMany` SHALL see
the caller's drafts only: another user's draft reads as null in place and is not deleted; an unknown id
reads as null and deletes with success; a malformed id, an empty list or a missing body 400. Deleting a
draft SHALL leave the DocSpace file it was made from. The attachment lifecycle SHALL work without a paid AI
Tools service.

#### Scenario: Another user's draft

- **WHEN** a User asks `aiAttachmentsGetMany` for the owner's draft id next to their own
- **THEN** the own draft is returned and the owner's slot is null

#### Scenario: Mixed delete

- **WHEN** a User deletes their own draft and the owner's in one `aiAttachmentsDeleteMany`
- **THEN** only the User's draft is gone

### Requirement: Linking attachments to a message

`aiAttachmentsLinkToMessage` SHALL bind the caller's drafts to a message of the caller's thread so they are
removed with the message or the thread; it SHALL answer 403 for a thread the caller cannot read, 404 for an
unknown message or attachment, 400 for a message of another thread, an empty `ids` list or a non-array
value, and write nothing onto the message itself. Linking the same draft twice SHALL be accepted.

#### Scenario: Linked draft dies with the message

- **WHEN** the owner links a draft to a message and deletes the message
- **THEN** the draft reads back as null while an unlinked draft stays

### Requirement: Suggested questions for a form attachment

`aiAttachmentsGetSuggestedQuestions` SHALL answer 200 with a `status` and a `questions` list for an
attachment id of the caller: `unavailable` and empty for a draft that is not an analysable started form,
for an unknown id and for another user's attachment; a missing, empty, null or malformed id 400; anonymous
401; AI off 403.

#### Scenario: Unknown id

- **WHEN** the owner asks for a well-formed id no attachment carries
- **THEN** the system answers 200 with `unavailable` and no questions

### Requirement: Exporting markdown into a document

`aiExportTextToDocx` SHALL queue the export of `content` under `title` into `folderId` as `format` `Docx`
(default), `Pdf` or `Md` and answer 202 with no file id; the file appears in the folder later. It SHALL
require `title`, non-blank `content` and a numeric `folderId` (400, unknown `format` too), refuse an
unknown or deleted folder or a file id (404), a body over 15 MiB (413), and run validation before the AI
switch (400 before 403). It works without a paid AI Tools service.

#### Scenario: Export into Chat outputs

- **WHEN** the owner exports a transcript with an agent's id as `folderId`
- **THEN** the document lands in the agent's Chat outputs folder, not in its root

#### Scenario: Unknown format

- **WHEN** the owner sends `format` `Rtf`
- **THEN** the system answers 400

### Requirement: Where an export may land

The caller SHALL be the author of the exported document and SHALL need the right to create in the target:
a Viewer, a non-member, a Guest, another user's My documents, an archived room and Trash are refused (403),
a Content creator exports into someone else's room. A repeated title SHALL get `(1)`, `(2)`; an over-long
title SHALL be truncated and forbidden characters replaced as the Files API does; simultaneous exports each
land in their folder.

#### Scenario: Member without the right

- **WHEN** a room member with the Viewer right exports into that room
- **THEN** the system answers 403

#### Scenario: Same title three times

- **WHEN** the owner exports the same title three times into one folder
- **THEN** three documents exist, the later two suffixed `(1)` and `(2)`

### Requirement: Vectorization of agent knowledge

`aiVectorizationStartTask` SHALL queue the indexing of `files` (a non-empty list of numeric ids, 400
otherwise) and answer 200 with no job handle: a missing file fails the whole request (404), a file outside
an agent's Knowledge folder is refused even for its owner (403), a Guest 403, anonymous 401, AI off 403.
Repeats and duplicates are accepted; an indexed file is skipped. `vectorizationStatus` and a socket update
report progress; the index serves that agent's chats only.

#### Scenario: File in My documents

- **WHEN** the owner submits a file that lies in My documents or in an ordinary room
- **THEN** the system answers 403

#### Scenario: Other agent's knowledge

- **WHEN** a user with no access to agent A chats in agent B
- **THEN** the content indexed for agent A is not found

### Requirement: Vectorization settings

`aiSettingsGetVectorization` and `aiSettingsSetVectorization` SHALL be refused on a hosted portal (403 for
every role, AI on or off, paid or not — the gateway manages them) and allowed to DocSpace admins of a
self-hosted installation only (403 others, 401 anonymous). Setting an embedding provider SHALL verify the key
against it (400 "invalid key"), a provider of none SHALL reset the settings, and each change is audited. An
unparseable body SHALL answer 400 without side effect.

#### Scenario: Hosted portal

- **WHEN** the owner of a hosted portal reads or sets the vectorization settings
- **THEN** the system answers 403 whatever the body

### Requirement: The `.ai` skills folder of a room

A folder titled `.ai` (case-sensitive) in the root of a Custom, Public, Virtual data or Collaboration room
SHALL be the room's skills folder: a second one in the same root SHALL be refused on create, rename and move
(403); out of the root it is a regular folder, into a root a skills folder again, keeping its title; a
duplicate of the room or a room made from its template SHALL carry one.
`getRoomsFolder` with `withAiFolder` SHALL list the rooms that hold one.

#### Scenario: Second skills folder

- **WHEN** a room already holds `.ai` and the owner creates or renames another folder to `.ai` in its root
- **THEN** the system answers 403

### Requirement: Listing and using the skills of a room

`getRoomAiFolder` SHALL list the skills folder of a room for any member who reads the room (403 others,
404 no such room) and answer 200 with an empty page (no entries, `total` 0, no current folder) while the
room holds none. A skill's name and description SHALL reach the model's prompt for every
chat in the room, the model reading the file through a tool when the question matches; members of any role
use it, outsiders and Guests cannot, and a revoked invitation ends the access.

#### Scenario: Room without a skills folder

- **WHEN** the owner calls `getRoomAiFolder` on a fresh Custom room
- **THEN** the system answers 200 with no files and no folders

#### Scenario: Skill used by an invited User

- **WHEN** a User invited to the room asks a matching question in the room's chat
- **THEN** the model reads the skill; after the invitation is revoked the same question answers 403

### Requirement: Prompt library

Each member (DocSpace admin, Room admin, User; Guests 403, anonymous 401, AI off 403) SHALL own a prompt
library served by the `aiPrompts*` operations: create, update, move, delete, get-by-id and list for
prompts; create-folder, rename-folder, delete-folder, get-folder-by-id and list-folders; export and
import-bundle. Another user's prompts and folders SHALL be invisible and untouchable (a foreign folder reads
as null); deleting a folder deletes its prompts (unknown 404).

#### Scenario: Libraries are private

- **WHEN** a User lists prompts and exports their library after the owner created prompts
- **THEN** none of the owner's prompts appear, and two users may use the same names

#### Scenario: Folder read by id

- **WHEN** the owner calls `aiPromptsGetFolderById` with a malformed id, then with a well-formed unknown one
- **THEN** the first answers 400 and the second 200 with a null body

### Requirement: Prompt names, texts and bundles

A prompt or folder name SHALL be 1–255 characters (256 → 400), not trimmed, unique case-insensitively
within its folder; a prompt text SHALL be non-blank (400) and unlimited; a name used in another folder is
allowed. `aiPromptsImportBundle` with mode `merge` SHALL keep
the existing prompts byte-identical and refuse the bundle whole on a name collision, with `replace` drop the
library and write the bundle; either mode writes nothing when one entry is invalid.

#### Scenario: Name limit

- **WHEN** a member creates a prompt or a folder with a 255-character name and then a 256-character one
- **THEN** the first answers 200 and is stored whole, the second 400

#### Scenario: Import modes

- **WHEN** a member imports an exported bundle with mode `merge` and then with mode `replace`
- **THEN** merge keeps the existing prompts byte-identical and refuses the bundle whole on a name collision; replace drops the library and writes the bundle

### Requirement: Metadata fields proposed for a file

`POST api/2.0/ai/meta/generate` SHALL read one file as text under the caller's rights (`kind` must be
`file`, `id` a positive integer, `prompt` an optional hint; 400 otherwise) and answer, writing nothing, the
custom fields the model proposes — trimmed, deduplicated regardless of case, at most 50, each with the
value the text supports or null. A file the caller cannot read is skipped as forbidden. The route SHALL
answer 403 while AI is off.

#### Scenario: Folder is not accepted yet

- **WHEN** the owner calls `meta/generate` or `meta/fill` with `kind` `folder`
- **THEN** the system answers 400

### Requirement: Metadata values filled from a file

`POST api/2.0/ai/meta/fill` SHALL take the same `kind`, `id` and `prompt`, propose values for the file's
own fields — those of its templates and its custom fields — and write them through the Files metadata API
only with `dryRun` false, filling empty fields unless `overwrite` is true; a value the portal refuses is
reported per field and the call fails only when nothing was written. It SHALL answer 403 while AI is off.

#### Scenario: Dry run writes nothing

- **WHEN** the owner calls `meta/fill` without `dryRun`
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
operation inside an agent SHALL name the agent as its source, one outside any agent none. Switching the AI
Tools service off SHALL stop the spend (402 on the next send).

#### Scenario: Agent named as source

- **WHEN** the owner chats inside an agent and reads the customer operations
- **THEN** the AI Tools operation names the agent

### Requirement: Spending limits on the AI services

A DocSpace admin with the portal-settings right SHALL set money limits on the AI services (tools and search
together) under `api/2.0/portal/payment/servicelimit`: one for the portal (`Total`), one for every user
(`Global`) and personal ones (`Custom`, per `userId`, bulk up to 1000 users); a personal
limit replaces the global one for its user and the portal limit is checked on top. No billing service 403,
no billing customer 404, a second limit of one scope 409. Each write is audited.

#### Scenario: Portal limit reached

- **WHEN** the spending of the portal in the current period reaches the `Total` limit
- **THEN** the AI services stop for every user until the period ends, even below their own limits

#### Scenario: Room admin cannot set a limit

- **WHEN** a Room admin creates or reads a service limit
- **THEN** the system answers 403

### Requirement: Audit trail and real-time events of AI changes

The audit trail SHALL record: AI access enabled/disabled, profile created/updated/deleted, profile
assigned/unassigned and MCP server created/updated/deleted (per agent when scoped), vectorization settings
set/reset, web-search settings set/reset, a user's AI settings update and the custom
quota per agent changed/disabled/reset; reads record nothing. Clients SHALL be told when the AI switch
changes, a file's vectorization status changes or a form's starter questions are ready.

#### Scenario: Switch audited

- **WHEN** the owner turns the AI switch off
- **THEN** the audit trail shows "AI access disabled" by the owner
