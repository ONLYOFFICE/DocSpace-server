# ai-agents Specification

## Purpose

Defines the AI product of a portal: the portal-wide AI switch and the readiness it reports, the model
profiles (provider + model + key/URL) and their catalogue, the per-task model assignments, the AI agents
(rooms of the AI room type that live in the AI Agents section, with a fixed model and AI instructions, a
Knowledge folder and a Chat outputs folder), the chat threads and messages with streaming replies,
regeneration and tool-call approval, the per-user chat preferences, the MCP server registry and the tool
switches, the web-search configuration, the OpenAI-compatible passthrough, the editor tools, the chat
attachments, the markdown export, the vectorization of agent knowledge, the room `.ai` skills folder and
the prompt library. Only the deltas of an AI agent against an ordinary room are described here; everything
a room does (title, tags, logo, lifetime, watermark, archive, pinning, quota mechanics, roles) is in
`room-types` and `room-role-permissions`, the default agent quota and the agent count limit in `quotas`,
the agent webhook events in `webhooks-and-api-keys`.

The public routes under `/api/2.0/ai/*` are served by the AI chat service, which keeps its data through the
internal storage routes of the DocSpace AI service; the agent, settings, vectorization and export routes are
the DocSpace AI service itself. Sources: the storage-level integration tests of `products/ASC.AI/ASC.AI.Tests`
and the Web.Api and Files suites, the TypeScript API suite of the `ai` area, the OpenAPI contract (operations
named by `operationId`), the product code where the tests stop, and the release notes (v3.5–v4.0). Where
notes, contract or TypeScript expectations and the server disagree this specification follows the server;
the evidence, the differences and the suspected defects are in `verification.md` next to this file.

## Requirements

### Requirement: Portal AI switch

The portal SHALL carry one AI switch, on by default, read by any signed-in member through
`getTenantAiAccessSettings` (200, anonymous 401) and changed through `setTenantAiAccessSettings` by the
owner or a DocSpace admin only (others 403, anonymous 401). While it is off every AI route except the
portal-wide reads named below SHALL answer 403 for every member, before the target of the call is looked up;
switching it back on SHALL restore every stored AI setting unchanged. The change is audited and pushed to
connected clients.

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
`recommendedModelForForms` and `toolPermissionMode`. On a hosted portal `aiReady` and `systemAiEnabled`
SHALL be true only while the AI switch is on and the AI Tools wallet service is paid for; on a self-hosted
installation `aiReady` SHALL be true while the switch is on and at least one profile exists.

#### Scenario: Unpaid hosted portal

- **WHEN** the AI Tools wallet service is not enabled on a hosted portal and the owner calls `aiSettingsGet`
- **THEN** the answer is 200 with `aiReady` false, and true once the service is paid for

#### Scenario: Read while the switch is off

- **WHEN** AI is switched off and a member calls `aiSettingsGet`
- **THEN** the system answers 200 with `aiReady` false

### Requirement: The AI Tools wallet service gates inference only

On a hosted portal the AI Tools wallet service SHALL gate the calls that run a model: a send, a
regeneration, the OpenAI passthrough and the gateway models list answer 402 while it is off and nothing
is stored for the refused turn. Agents, assignments, tools, attachments, preferences, vectorization and
export SHALL stay available without it. A paid service with a zero AI credit balance or a balance under the
low-balance threshold SHALL NOT stop inference.

#### Scenario: Send refused while unpaid

- **WHEN** the AI Tools service is off and the owner calls `aiSendWithStream` on a thread
- **THEN** the system answers 402 and `aiThreadsReadMessages` shows neither the question nor a failed answer

#### Scenario: Management surface stays open

- **WHEN** the AI Tools service is off and the owner creates an agent, registers a custom MCP server and reads the tools catalogue
- **THEN** every call answers 200

### Requirement: Per-user AI chat settings

`aiSettingsGetUser` and `aiSettingsSetUser` SHALL read and store the caller's own
`chatRecommendedModelVisible` (true for a new user), usable by every member and while AI is off, never
touching another user's value or the portal settings. A non-boolean value SHALL be refused (400) and
change nothing; an empty object SHALL change nothing; an unknown field SHALL be ignored; the field name
binds case-insensitively.

#### Scenario: One user's preference is private

- **WHEN** the owner stores false and a User reads their own settings
- **THEN** the User still reads true

#### Scenario: Non-boolean value

- **WHEN** a member sends `chatRecommendedModelVisible` as a string
- **THEN** the system answers 400 and the stored value is unchanged

### Requirement: Tool permission mode of a user

The caller's tool permission mode — `ask` (every tool call waits for approval unless pinned), `auto`
(tools that opt out of approval run at once, the default) or `allow` (nothing waits) — SHALL be stored per
user for every chat of the portal through `aiSettingsSetToolMode` / `aiPreferencesSetToolPermissionMode`
and read back through `aiSettingsGetToolMode` / `aiPreferencesGetToolPermissionMode`. Guests SHALL be
refused (403); a value outside the three modes SHALL be refused (400) and leave the mode alone; saving
another user preference SHALL leave the mode alone.

#### Scenario: Modes round-trip

- **WHEN** a User sets `ask`, then `allow`, then `auto`
- **THEN** each value reads back, and `aiSettingsGet` reports the same `toolPermissionMode`

#### Scenario: Mode steers the approval pause

- **WHEN** the mode is `allow` and the model calls a tool that requires approval
- **THEN** the tool runs without a pending tool call; in `ask` the same call pauses for approval

### Requirement: Profile catalogue

`aiProfilesList` and `aiProfilesGetById` SHALL return the profiles a chat can run on — id, name,
provider type, base URL, model id, capabilities (chat, vision, image, tools), reasoning support — with the
key and headers stripped, to DocSpace admins, Room admins and Users; Guests 403, anonymous 401. A malformed
or empty id SHALL answer 400, an unknown one 404. On a hosted portal the catalogue SHALL be the gateway's
model list without embedding models and without the models restricted for the portal, served whole inside an
agent too.

#### Scenario: Guest cannot read the catalogue

- **WHEN** a Guest calls `aiProfilesList` or `aiProfilesGetById` with a real id
- **THEN** the system answers 403

#### Scenario: Restricted model leaves the catalogue

- **WHEN** a DocSpace admin restricts one model through `setRestrictedAiModels`
- **THEN** that profile disappears from `aiProfilesList` and reappears once the restriction is lifted

#### Scenario: Unknown id

- **WHEN** a member calls `aiProfilesGetById` with a well-formed id no profile carries
- **THEN** the system answers 404

### Requirement: Managing profiles

`aiProfilesCreate`, `aiProfilesUpdate` and `aiProfilesDelete` SHALL be allowed to DocSpace admins only
(403 for Room admins, Users and Guests, 401 anonymous, authentication before validation). On a hosted
portal — the AI gateway configured — the catalogue is read-only and every write SHALL answer 403, the
owner's included, before the body is looked at. On a self-hosted installation a profile SHALL carry a name
of at most 255 characters, a base URL of at most 2048 and a key of at most 4096; each write SHALL be
audited.

#### Scenario: Owner on a hosted portal cannot add a profile

- **WHEN** the owner of a hosted portal calls `aiProfilesCreate` with a well-formed profile
- **THEN** the system answers 403 and the catalogue is unchanged

#### Scenario: Self-hosted admin round-trips a profile

- **WHEN** a DocSpace admin of a self-hosted portal creates a profile, reads it, updates it and deletes it
- **THEN** every field written is read back, the delete answers success and the read then answers 404

#### Scenario: Room admin cannot delete

- **WHEN** a Room admin calls `aiProfilesDelete`
- **THEN** the system answers 403

### Requirement: Provider discovery and connection checks

`aiProfilesListProviderModels` SHALL require `providerType` and `baseUrl` (400 naming the missing field),
refuse an unknown provider type, a malformed base URL and a loopback, private or unresolvable host with 400
before any connection is opened, report a key the provider rejects as 400 against the key, answer 502 when a
reachable host does not answer as a provider, and never echo the key or store a profile. Local provider
types SHALL need no key. `aiProfilesTestConnection` SHALL take a bare profile id (400 without one) and
answer 200 with a flat verdict.

#### Scenario: Private address refused before dialling

- **WHEN** a member calls `aiProfilesListProviderModels` with a base URL on a loopback or link-local address
- **THEN** the system answers 400 and no socket is opened

#### Scenario: Wrong key

- **WHEN** a member sends a reachable provider URL with a key the provider refuses
- **THEN** the system answers 400 and the message names the key

#### Scenario: Verdict of a stored profile

- **WHEN** the owner calls `aiProfilesTestConnection` with a real profile id
- **THEN** the system answers 200 with a `{field, message}` verdict that leaks nothing

### Requirement: Model restrictions and prices of a hosted portal

`getAiPrices` SHALL need a billing service and the AI gateway (403 otherwise) and a DocSpace admin.
`getRestrictedAiModels` SHALL answer an empty set while billing or the AI Tools service is off and the
barred models otherwise, to a DocSpace admin. `setRestrictedAiModels` SHALL replace the whole set: it needs
the gateway, the portal-settings right, a billing customer (404), AI Tools on (403) and a `models` list
without null entries (400). The set SHALL take effect immediately on the catalogue and on the next model
call.

#### Scenario: Null entry

- **WHEN** the owner sends `models` holding a null entry
- **THEN** the system answers 400

#### Scenario: Restriction reaches an open conversation

- **WHEN** an agent's model is restricted and a member sends into an existing thread of that agent
- **THEN** the send answers 400 while a rename of the agent without `profileId` still answers 200

### Requirement: Model assignments per task

Each task — Default, Chat, Code, Summarization, Translation, TextAnalyze, ImageGeneration, OCR, Vision,
FormAnalysis — SHALL hold at most one profile per scope. `aiAssignmentsGetAllAssignments`,
`aiAssignmentsGetAssignment`, `aiAssignmentsResolveForAction` and `aiAssignmentsTryResolveForAction` SHALL
be readable by DocSpace admins, Room admins and Users (Guest 403); `aiAssignmentsAssign`,
`aiAssignmentsBulkAssign`, `aiAssignmentsUnassign` and `aiAssignmentsCascadeProfileDelete` SHALL be
DocSpace-admin only (403). An unknown task name SHALL answer 400; a scope the caller cannot open 403; an
unknown scope 404.

#### Scenario: Fresh hosted portal

- **WHEN** the owner calls `aiAssignmentsGetAllAssignments` on a new hosted portal
- **THEN** the map carries a Default and an ImageGeneration binding chosen from the gateway catalogue and nothing else

#### Scenario: Unassigned task falls back to Default

- **WHEN** Summarization has no binding and the owner calls `aiAssignmentsResolveForAction` for it
- **THEN** the Default profile is returned, and `aiAssignmentsTryResolveForAction` with nothing to resolve answers 200 with a null body

#### Scenario: Agent's model wins for Chat

- **WHEN** Chat is resolved with an agent's id as `entityId`
- **THEN** the agent's own profile is returned, while FormAnalysis in the same scope still resolves through the portal

#### Scenario: Bulk assignment is all or nothing

- **WHEN** `aiAssignmentsBulkAssign` carries an unknown task key, a null value or a malformed profile id
- **THEN** the system answers 400 and no binding changes

#### Scenario: Unknown profile is a soft refusal

- **WHEN** the owner assigns a profile id the catalogue does not hold, or one that is restricted
- **THEN** the system answers 200 with `success` false and the binding is unchanged

### Requirement: Creating an agent

`aiAgentsCreate` SHALL be allowed to the owner, DocSpace admins and Room admins (Users and Guests 403,
anonymous 401) and require `profileId` naming a chat-capable catalogue profile (400 when missing, malformed,
unknown, restricted or image-only) and `prompt` (400 when missing), with the room fields of `room-types`
(title at most 170 characters). It SHALL create an AI room in the AI Agents section with a Knowledge and a
Chat outputs folder, bind the profile as the agent's Chat assignment, count against the agent limit (402),
ignore `mcpServers` and `attachDefaultTools`, and audit and publish `agent.created`.

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

`aiAgentsList` SHALL return the AI Agents section as a folder listing: DocSpace admins see every agent, other
members only the agents they were invited to, a Guest invited as Viewer the same agent a Viewer sees, and an
agent the caller is not in SHALL be absent and uncounted (`total` 0, `current.foldersCount` 0). Ordinary
rooms SHALL never be listed. The listing SHALL accept `subjectId`, `subjectOwnerId`, `excludeSubject`,
`tags`, `withoutTags`, `quotaFilter`, `text`, `sortBy`, `sortOrder`, `startIndex` and `count`. Anonymous 401.

#### Scenario: Hidden agents are not counted

- **WHEN** the owner creates an agent and a User who was not invited calls `aiAgentsList`
- **THEN** the system answers 200 with no folders, `total` 0 and `current.foldersCount` 0

#### Scenario: Invited member sees the agent

- **WHEN** a User or a Guest is added to the agent as Viewer and lists agents
- **THEN** the agent is in the list

### Requirement: Reading an agent

`aiAgentsGet` SHALL answer the agent as a room with `chatSettings` (the AI instructions) and, when a model is
bound, its `profileId`, to every member of the agent and to DocSpace admins; a member not in the agent 403;
a non-positive or non-numeric id 400; an id that names no agent — an ordinary room included — 404;
anonymous 401. A plain member SHALL read the AI instructions; HTML stored in them SHALL reach another
member neutralised.

#### Scenario: Non-member refused

- **WHEN** a Room admin, a User or a Guest who was not invited reads the agent
- **THEN** the system answers 403; a DocSpace admin reads it with 200

#### Scenario: Ordinary room by id

- **WHEN** the owner calls `aiAgentsGet` with the id of a Custom room
- **THEN** the system answers 404

### Requirement: Updating an agent

`aiAgentsUpdate` SHALL be allowed to the agent's Room managers only — its creator included; the portal
owner or a DocSpace admin who is not a member, a Content creator and a Viewer get 403, anonymous 401. It
SHALL accept the room fields of `room-types` plus `chatSettings.prompt` and `profileId`; a flat `prompt`
SHALL be refused (400). Leaving `profileId` out SHALL keep the model; a malformed `profileId` SHALL be
refused (400) atomically; a valid new one SHALL replace the Chat assignment and leave title, tags, prompt and
existing threads alone. Audited; `agent.updated`.

#### Scenario: Content creator cannot update

- **WHEN** a member invited as Content creator renames the agent
- **THEN** the system answers 403

#### Scenario: Portal owner outside the agent

- **WHEN** the owner updates an agent created by a DocSpace admin who did not invite them
- **THEN** the system answers 403

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
Viewer only (Room manager or Content creator 403). Its root SHALL hold no files: an upload to the root
answers 403, while Knowledge and Chat outputs take files from a Content creator or Room manager. Knowledge
SHALL accept only indexable formats (403 otherwise) and no subfolders; Chat outputs SHALL keep its files out
of the agent's index. An agent SHALL NOT become a room template. Everything else is as in `room-types`.

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

`aiThreadsCreate` SHALL start a thread for the caller — a DocSpace admin, a Room admin or a User; Guests 403,
anonymous 401 — with a required non-blank `title` of at most 255 characters (400), an optional `profileId`
that must exist (404) and an optional `entityId`: omitted or null is the portal-wide scope, a non-numeric
one 400, one naming no folder 404, one the caller may not use for AI 403. In an agent the caller SHALL be a
member with more than the Viewer right; in an ordinary room a Read member may start a thread. The thread
belongs to its creator alone.

#### Scenario: Viewer of an agent

- **WHEN** a member invited to an agent as Viewer starts a thread in it
- **THEN** the system answers 403

#### Scenario: Non-member of an agent

- **WHEN** a DocSpace admin, a Room admin or a User who is not in the agent starts a thread in it
- **THEN** the system answers 403

#### Scenario: Blank title

- **WHEN** the owner sends an empty or whitespace title
- **THEN** the system answers 400 and nothing is listed

### Requirement: Thread privacy and scope

A thread SHALL be visible to its creator only: `aiThreadsGetById`, `aiThreadsReadMessages`,
`aiThreadsRename`, `aiThreadsDelete`, `aiThreadsClearMessages`, `aiThreadsTouch`, every message route and a
send into it SHALL answer 403 to any other member, a DocSpace admin or a member of the same agent included.
`aiThreadsList` SHALL show, per caller, the threads of one agent when its id is the `entityId`, and one
shared list for the portal-wide scope and every room and folder (BUG 82855 closed by design). Listing for a
room the caller cannot open SHALL answer 403.

#### Scenario: Two members of one agent

- **WHEN** two members each start a thread in the same agent
- **THEN** each lists only their own thread and reading the other's answers 403

#### Scenario: Rooms share one list

- **WHEN** a member starts a thread in room A and lists threads for room B or for a folder
- **THEN** the thread appears in both lists; a thread of an agent appears in that agent's list only

#### Scenario: Removed member keeps their thread

- **WHEN** a member who wrote in a room is removed from it
- **THEN** listing or creating for that room answers 403, while the thread itself is still read and renamed by its creator

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

### Requirement: Thread management calls

`aiThreadsRename` SHALL store the new title (blank or over 255 characters 400; unknown thread 404),
`aiThreadsDelete` SHALL remove the thread with its messages and attachments (unknown 404, a second delete
404), `aiThreadsClearMessages` SHALL empty the thread and keep its identity (repeat 200, unknown 404),
`aiThreadsTouch` SHALL bump `lastEditDate` and may set or clear the thread's profile, and
`aiThreadsOpenOrCreate` with a `threadId` SHALL replay the thread's history read-only. A malformed id SHALL
answer 400 on every route; a deleted thread SHALL be unreachable on every route (404).

#### Scenario: Cleared thread carries on

- **WHEN** the owner clears a thread with a conversation and sends a new question
- **THEN** the thread answers again and no longer knows the cleared history

#### Scenario: Rename mid-reply

- **WHEN** the owner renames a thread while its reply is still streaming
- **THEN** the rename survives the reply

### Requirement: Messages of a thread

`aiThreadsAppendUserMessage` SHALL store a user message verbatim (markdown, links, Unicode, inline
attachment records) without drawing an answer; `aiThreadsGetMessageById` SHALL answer the message, 200 with
a null body for an unknown id and 400 for a malformed one; `aiThreadsUpdateMessage` SHALL rewrite the
content in place keeping the message's position and the standing answer; `aiThreadsDeleteMessage` SHALL be
idempotent (200 for an unknown id, 400 malformed). `aiThreadsReadMessages` SHALL page oldest first with
`count` of at least 1 and `direction` `desc` paging back from the newest.

#### Scenario: Edited question stays in place

- **WHEN** the owner rewrites the first question of a two-turn thread
- **THEN** `aiThreadsReadMessages` shows the new text first and the assistant reply still after it

#### Scenario: Another member's message

- **WHEN** a User reads, rewrites or deletes a message of the owner's thread
- **THEN** the system answers 403

### Requirement: Sending a message with a streamed reply

`aiSendWithStream` SHALL store the user message, run the thread's model and stream the reply as
newline-delimited chat events that end on the stored assistant text; without `threadId` it SHALL open a new
thread titled "New chat" and rename it from the first question afterwards. An empty message SHALL answer
400, an unknown thread 404, a body over 100 KB 413, a `profileId` naming no model 400, another user's
thread 403, a Guest 403. A profile sent with the message SHALL become the thread's model; a provider refusal
SHALL fail the turn and leave the thread usable.

#### Scenario: First send opens the thread

- **WHEN** the owner calls `aiSendWithStream` with a prompt and no `threadId`
- **THEN** the system answers 200, a new thread is listed and the user message and the reply are stored

#### Scenario: Context stays inside the thread

- **WHEN** the owner teaches a fact in thread A and asks for it in thread B of the same agent
- **THEN** thread B does not know it, while a second question in thread A does

#### Scenario: Hang-up cancels

- **WHEN** the client closes the connection mid-stream
- **THEN** the generation stops and the thread accepts the next message

### Requirement: The model of a chat

Inside an agent a send with no `profileId` SHALL run on the agent's profile, and a regeneration SHALL run on
it whatever `profileId` it carries. Outside an agent a send with no `profileId` SHALL run on the thread's
own model when it has one, else on the Chat binding of the scope, else on Default; an agent picked in the
composer SHALL answer on its own model and come back when the thread is reopened. An image-only profile
SHALL NOT be used as a chat model (400).

#### Scenario: Agent answers on its model

- **WHEN** a member sends into an agent thread without `profileId`
- **THEN** the reply runs on the profile bound to the agent

#### Scenario: Picker switches the thread's model

- **WHEN** the owner sends with profile A, then with profile B into the same room thread
- **THEN** the thread reports profile B afterwards and the second reply still knows the first exchange

### Requirement: One-shot and OpenAI-shaped sends

`aiSend` and `aiSendCustom` SHALL run the model once and persist nothing: an empty message is accepted, a
task type without its own binding resolves through Default, a nonexistent `entityId` falls back to the
portal binding and an inaccessible one answers 403; `aiSendCustom` takes `systemPrompt` and `isStream`.
`aiSendWithStreamOpenAI` SHALL persist like `aiSendWithStream` and stream OpenAI `chat.completion.chunk`
events ended by `[DONE]`, surfacing tool calls as `tool_calls` and a model failure inside the stream with 200.

#### Scenario: Two one-shot calls

- **WHEN** the owner calls `aiSend` twice
- **THEN** no thread is created and the second call does not see the first

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

### Requirement: Tool calls, approval and the always-allow list

A tool the model wants to run SHALL pause the stream with a pending tool call that carries the model's
announcement; `aiApproveToolCall` (with a string or structured result, optionally `allowAlways`) and
`aiDenyToolCall` SHALL resume it, the refusal going to the model. Both SHALL answer 401 anonymous, 403 to a
non-member of the thread and 403 while AI is off. A tool that opted out of approval SHALL run unasked; a
listed always-allow tool SHALL run unasked; a tool that answers "unavailable" or was withdrawn SHALL
finish the reply instead of stalling.

#### Scenario: Two-hop chain

- **WHEN** the model calls tool A, the owner approves, the model calls tool B and the owner approves again
- **THEN** both calls, both results and the final answer are stored in order

#### Scenario: Deny

- **WHEN** the owner denies a pending document generator call
- **THEN** no file is written and the reply finishes

### Requirement: Deep mode and reasoning level

`aiPreferencesSetDeepMode`, `aiPreferencesGetDeepMode`, `aiPreferencesIsDeepModeSet`,
`aiPreferencesClearDeepMode`, `aiPreferencesSetReasoningLevel` and `aiPreferencesGetReasoningLevel` SHALL
keep, per user and per scope (portal-wide or one agent), whether extended thinking is on and its depth
(`none`, `minimal`, `low`, `medium`, `high`); off and unset by default. A non-boolean value, a depth
outside the five, a malformed or thread `entityId` SHALL answer 400 and write nowhere; a scope the caller
cannot open 403; Guests 403; anonymous 401. Switching on after off SHALL land on `medium`.

#### Scenario: Agent scope is its own

- **WHEN** a member sets deep mode on portal-wide and reads it for an agent with no value of its own
- **THEN** the agent reads unset

#### Scenario: Clearing one agent

- **WHEN** a member clears the deep mode of one agent
- **THEN** that agent reads unset and the portal-wide and other agents' values stay

### Requirement: Custom MCP servers

`aiToolsAddCustomServer`, `aiToolsUpdateCustomServer`, `aiToolsRemoveCustomServer` and
`aiToolsReplaceAllCustomServers` SHALL be allowed to DocSpace admins only (403 for Room admins, Users,
Guests; 401 anonymous); `aiToolsGetCustomServer` and `aiToolsListCustomServers` SHALL be readable by
DocSpace admins, Room admins and Users of the scope (Guest 403, outsider of an agent 403). The portal and
every agent SHALL be separate scopes; a write against an unknown or deleted agent SHALL answer 404 while a
read falls back to the portal. Registration SHALL NOT dial the server; each change is audited.

#### Scenario: Round trip

- **WHEN** the owner adds a server with an HTTP config, reads it, updates it and removes it
- **THEN** the config comes back verbatim, the update replaces it whole and the remove answers 200

#### Scenario: Scoped to an agent

- **WHEN** the owner registers a server for agent A
- **THEN** agent B and the portal-wide list do not show it, and a portal-level server is not listed for an agent

#### Scenario: Member cannot register

- **WHEN** a Room admin or a User in the agent registers or removes a server
- **THEN** the system answers 403 and the scope is unchanged

### Requirement: Server names and configurations

A server name SHALL be at most 128 characters, non-blank, without whitespace-only or control characters,
matched case-insensitively while keeping its spelling; a duplicate name, an unknown name on update or a
missing portal server to copy SHALL be soft refusals (200 with `success` false or 400), and removing an
unknown name a no-op. A config SHALL be stdio or HTTP, never both; `aiToolsReplaceAllCustomServers` SHALL
require a `map` object (missing, null, string, number or array 400) and apply nothing when one entry is
invalid, reporting every bad entry in `errors`.

#### Scenario: Both url and command

- **WHEN** the owner adds a server whose config carries both `url` and `command`
- **THEN** the system answers 200 with an error and nothing is stored

#### Scenario: Replace-all with one bad entry

- **WHEN** the owner replaces the portal map with two valid servers and one invalid config
- **THEN** the scope keeps its previous servers and `errors` names the bad one

### Requirement: Switching tools off and pre-approving them

`aiToolsSetDisabled`, `aiToolsGetDisabled` and `aiToolsIsToolDisabled` SHALL keep, per user and per scope,
the tools switched off for a `serverType` that is a built-in group or a registered server (400 otherwise),
the list being a full replacement; `aiToolsSetAllowAlways`, `aiToolsGetAllowAlways` and
`aiToolsIsAllowAlways` SHALL keep the flat `serverType_toolName` pre-approvals. The two lists SHALL be
independent; one user's lists SHALL be invisible to another; a removed server SHALL take its entries with
it; a portal-wide and an agent list SHALL be separate. Guests 403.

#### Scenario: Disable and re-enable

- **WHEN** the owner disables one tool of a registered server and then sends an empty list
- **THEN** the tool reads disabled and then enabled again

#### Scenario: One user's approval

- **WHEN** a member pre-approves a tool and the owner reads their own list
- **THEN** the owner's list does not carry it

### Requirement: Tools catalogue

`aiToolsListSystemTools` SHALL answer the scope's tools grouped by server type, the system group keys in
`system` and in `errors` the reason a registered server delivered none, to every signed-in member, Guests
included, with AI on or off. The catalogue lists no tool twice and is not the toolset the model receives:
the built-in DocSpace tools stay offered to the model whatever is written to the disabled list.

#### Scenario: Unreachable registered server

- **WHEN** a server that does not answer is registered and the owner reads the catalogue
- **THEN** the system answers 200 and names the server in `errors`

### Requirement: Web-search configuration

On a hosted portal the web-search provider SHALL come from the AI search add-on of the wallet: enabling it
configures the provider for every scope, `aiWebSearchConfigure`, `aiWebSearchSetActiveConfig` and
`aiWebSearchClear` SHALL answer 403 to everyone and nothing is stored; the add-on SHALL require AI Tools and
the AI switch (403 otherwise) and only the owner or a DocSpace admin may buy it. `aiWebSearchIsConfigured`
and `aiWebSearchGetActiveConfig` SHALL be readable by DocSpace admins, Room admins and Users (Guest 403),
never return the key, and answer 404 for an unknown `entityId`. A private, unresolvable or malformed base
URL SHALL be refused (400) before any connection.

#### Scenario: Add-on configures search

- **WHEN** the owner enables the AI search add-on
- **THEN** `aiWebSearchIsConfigured` answers true for the portal, an agent, a room and a folder, and the model's reply to a search question carries sources

#### Scenario: Owner cannot clear the add-on config

- **WHEN** the owner or a DocSpace admin calls `aiWebSearchClear` while the add-on is on
- **THEN** the system answers 403 and the configuration survives

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
the path to its provider and return the provider's answer verbatim, for DocSpace admins, Room admins and
Users (Guest 403, anonymous 401, AI off 403). A malformed `profileId` SHALL answer 400, an unknown one 400;
`stream` true SHALL stream chunks; an image generation on a text-only model SHALL be refused by the gateway
(400). The gateway proxy SHALL expose only the models, chat completions, image generation, search and
contents paths (others 404).

#### Scenario: Chat completion with every optional field

- **WHEN** the owner posts a completion request with temperature, max tokens and a Cyrillic prompt
- **THEN** the system answers 200 with a `chat.completion` whose text survives the round trip

#### Scenario: Guest refused

- **WHEN** a Guest posts a completion or an image generation
- **THEN** the system answers 403

### Requirement: Editor tools

`aiEditorToolsList` SHALL publish the same tool catalogue (name, description, input schema,
`requireApproval` on for tools that change something) to every member, a Guest getting it without the
document generators; `aiEditorToolsCall` SHALL run one tool by name within the caller's own rights, report a
failing tool inside `result` with 200, answer 400 for a name no tool carries, scope a generator into the
room named by `entityId` only when the caller may write there (nothing lands otherwise), and fall back to
My documents without `entityId`. Anonymous 401; AI off 403 after body validation (400 first).

#### Scenario: Tool never exceeds the member

- **WHEN** a User calls the read, rename, delete or archive tool on the owner's data
- **THEN** every call reports failure inside `result` and nothing changes

#### Scenario: Generator in a room

- **WHEN** a Content creator of a room names it as `entityId` for a document generator
- **THEN** the document is created in that room

### Requirement: Chat attachments

`aiAttachmentsSaveFile` and `aiAttachmentsSaveFilesMany` SHALL turn a DocSpace file the caller can read
(`path` is the file id; 404 when it names no file, 403 when unreadable, 400 for an archive or a file no
text can be extracted from) into a draft the caller owns: a picture becomes an image draft with a download
link, any other file a text draft, regardless of the request body limit. `aiAttachmentsGet`,
`aiAttachmentsGetMany`, `aiAttachmentsDelete` and `aiAttachmentsDeleteMany` SHALL see the caller's drafts
only (another user's reads as null, deletes nothing). Guests 403; anonymous 401.

#### Scenario: Attach by id

- **WHEN** the owner attaches a stored document by its id
- **THEN** the system answers 200 with a draft carrying the extracted text, and a large file attaches whole

#### Scenario: Another user's draft

- **WHEN** a User asks `aiAttachmentsGetMany` for the owner's draft id next to their own
- **THEN** the own draft is returned and the owner's slot is null

#### Scenario: Guest with a shared file

- **WHEN** a Guest attaches a file shared with them
- **THEN** the system answers 403

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

`aiExportTextToDocx` SHALL queue the export of `content` under `title` into `folderId` as `format`
`Docx` (default), `Pdf` or `Md` and answer 202 with no file id; the file appears in the folder later. It
SHALL require `title`, `content` and a numeric `folderId` (400), refuse an unknown or deleted folder or a
file id (404), an archived room, Trash, a folder the caller may not create in and a Guest (403), a body
over 15 MiB (413), and run validation before the AI switch (400 before 403). The caller SHALL be the
author; a repeated title SHALL get `(1)`, `(2)`; forbidden characters SHALL be replaced as the Files API does.

#### Scenario: Export into Chat outputs

- **WHEN** the owner exports a transcript with an agent's id as `folderId`
- **THEN** the document lands in the agent's Chat outputs folder, not in its root

#### Scenario: Member without the right

- **WHEN** a room member with the Viewer right exports into that room
- **THEN** the system answers 403

#### Scenario: Unknown format

- **WHEN** the owner sends `format` `Rtf`
- **THEN** the system answers 400

### Requirement: Vectorization of agent knowledge

`aiVectorizationStartTask` SHALL queue the indexing of the listed files (`files`, a non-empty list of ids,
400 otherwise) and answer 200 with no job handle: a file that does not exist fails the whole request (404),
a file outside an agent's Knowledge folder is refused even for its owner (403), a file the caller may not
vectorize 403, a Guest 403, anonymous 401, AI off 403. Repeats and duplicates SHALL be accepted; a file
already indexed SHALL be skipped. The file's `vectorizationStatus` and a socket update SHALL report progress;
the indexed content SHALL be searchable by that agent's chats only.

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

A folder titled `.ai` (trimmed, case-sensitive) in the root of a room SHALL be the room's skills folder:
a second one SHALL be refused on create, rename and move (403); moving, copying or renaming it out of the
root SHALL make it a regular folder, and into a root a skills folder again; a duplicate of the room or a
room made from its template SHALL carry one. `getRoomAiFolder` SHALL list it for any member who reads the
room (403 others) and answer 404 while the room holds none. A skill file's name and description reach the
model's prompt for every chat in the room; outsiders and Guests cannot use it.

#### Scenario: Second skills folder

- **WHEN** a room already holds `.ai` and the owner creates or renames another folder to `.ai` in its root
- **THEN** the system answers 403

#### Scenario: Skill used by an invited User

- **WHEN** a User invited to the room asks a matching question in the room's chat
- **THEN** the model reads the skill; after the invitation is revoked the same question answers 403

### Requirement: Prompt library

Each member — DocSpace admin, Room admin or User; Guests 403, anonymous 401 — SHALL own a prompt library:
`aiPromptsCreate`, `aiPromptsUpdate`, `aiPromptsMove`, `aiPromptsDelete`, `aiPromptsGetById`,
`aiPromptsList`, `aiPromptsCreateFolder`, `aiPromptsRenameFolder`, `aiPromptsDeleteFolder`,
`aiPromptsListFolders`, `aiPromptsExport` and `aiPromptsImportBundle`. A name SHALL be 1–255 characters
(256 → 400) and unique case-insensitively within its folder; a text SHALL be non-blank (400) and unlimited.
Another user's prompts SHALL be invisible (list, read, export) and untouchable; deleting a folder deletes its
prompts (unknown folder 404).

#### Scenario: Name limit

- **WHEN** a member creates a prompt or a folder with a 255-character name and then a 256-character one
- **THEN** the first answers 200 and is stored whole, the second 400

#### Scenario: Libraries are private

- **WHEN** a User lists prompts and exports their library after the owner created prompts
- **THEN** none of the owner's prompts appear, and two users may use the same names

#### Scenario: Import modes

- **WHEN** a member imports an exported bundle with mode `merge` and then with mode `replace`
- **THEN** merge keeps the existing prompts byte-identical and refuses the bundle whole on a name collision; replace drops the library and writes the bundle

### Requirement: Billing of AI usage on a hosted portal

A chat reply SHALL be charged to the AI Tools wallet service and a web search to the AI search service; an
operation inside an agent SHALL name the agent as its source, one outside any agent none. Switching the AI
Tools service off SHALL stop the spend (402 on the next send).

#### Scenario: Agent named as source

- **WHEN** the owner chats inside an agent and reads the customer operations
- **THEN** the AI Tools operation names the agent

### Requirement: Audit trail and real-time events of AI changes

The audit trail SHALL record: AI access enabled/disabled, profile created/updated/deleted, profile
assigned/unassigned (per agent when scoped), MCP server created/updated/deleted (per agent when scoped),
vectorization settings set/reset, web-search settings set/reset, a user's AI settings update and the custom
quota per agent changed/disabled/reset; reads record nothing. Connected clients SHALL be told when the AI
switch changes, when a file's vectorization status changes and when a form's starter questions are ready.

#### Scenario: Switch audited

- **WHEN** the owner turns the AI switch off
- **THEN** the audit trail shows "AI access disabled" by the owner
