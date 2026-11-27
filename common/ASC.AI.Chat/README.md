# ASC New AI Service

ASC New AI service — Express-based Node.js service for AI features integration.

### Installation and usage

Node.js 24.22.1 or newer and Yarn 4 (`engines` in `package.json`; the image is built from
`node:24.22.1-alpine`).

```bash
$ yarn install
$ yarn start
```

### Checks

No CI job runs them: a change is handed over with the local run green and with what
`yarn openapi` changes committed.

```bash
$ yarn check     # typecheck, lint, format:check and the tests with their coverage thresholds
$ yarn test      # the tests alone; `yarn vitest run <file>` for one file
$ yarn openapi   # rewrites app/generated and the API documentation's aichat_2.0.json
```

### Environment

Read in `config/index.ts` only. A value that is given and is not taken is reported once with
`console.warn` and the default is used; an optional value never stops the service.

| Variable                      | Accepted                               | Default          |
| ----------------------------- | -------------------------------------- | ---------------- |
| `AI_CHAT_UPSTREAM_TIMEOUT_MS` | a whole number from 1000 to 3600000    | `300000`         |
| `AI_CHAT_PERSIST_INTERVAL_MS` | a whole number from 1000 to 600000     | `10000`          |
| `AI_CHAT_CONTEXT`             | `off` (exactly) switches the read off  | on               |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | an `http:` or `https:` address         | - (no telemetry) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc`; anything else is http/protobuf | `http/protobuf`  |
| `OTEL_SERVICE_NAME`           | a non-empty text                       | `ASC.AI.Chat`    |
| `OTEL_METRIC_EXPORT_INTERVAL` | a whole number from 1000 to 3600000    | `15000`          |

`AI_CHAT_UPSTREAM_TIMEOUT_MS` bounds one call to the AI service, the Files API or the People
API; `AI_CHAT_PERSIST_INTERVAL_MS` is how often a streaming reply is saved while its round runs;
`AI_CHAT_CONTEXT=off` switches the chat-context read of a round off for the whole process.
Telemetry starts only when `openTelemetry:enable` of the shared `appsettings.json` is `true`
and the endpoint is given; `openTelemetry:ServiceName` wins over `OTEL_SERVICE_NAME`. The
`OTEL_*` values are read from the environment first and then from the settings files.

### Document agent

The chat can delegate the creation and editing of office documents to the external document
agent, an A2A server that works with ONLYOFFICE Document Builder in a sandbox of its own
(`app/tools/documentAgent.ts`). When an agent is configured the model gets one more tool,
`document_agent`, which needs the user's approval before it runs; without the configuration the
tool is not offered at all.

The agent is configured in the shared `appsettings.json` under `ai.documentAgent`, or from the
environment, which wins:

| `appsettings.json`                    | Environment                                | Default                   |
| ------------------------------------- | ------------------------------------------ | ------------------------- |
| `ai.documentAgent.url`                | `AI__DOCUMENT_AGENT__URL`                  | —                         |
| `ai.documentAgent.token`              | `AI__DOCUMENT_AGENT__TOKEN`                | —                         |
| `ai.documentAgent.taskTimeoutSeconds` | `AI__DOCUMENT_AGENT__TASK_TIMEOUT_SECONDS` | `1800`                    |
| `ai.documentAgent.maxToolCalls`       | `AI__DOCUMENT_AGENT__MAX_TOOL_CALLS`       | agent's                   |
| `ai.documentAgent.shareSystemServers` | `AI__DOCUMENT_AGENT__SHARE_SYSTEM_SERVERS` | `false`                   |
| `ai.documentAgent.portalUrl`          | `AI__DOCUMENT_AGENT__PORTAL_URL`           | the AI service's address  |
| `ai.documentAgent.filesUrl`           | `AI__DOCUMENT_AGENT__FILES_URL`            | `http://localhost:<port>` |

### Plugin agent

The chat can build a DocSpace plugin and install it (`app/pluginAgent/`). The model of the owner
and of a DocSpace admin gets two more tools: `plugin_agent` hands a description of a plugin to a
sub-agent that runs inside this service, builds the plugin in a sandbox by the `apps-plugin-sdk`
skill and saves two new files in the workspace, the package (`<name>.zip`) and its sources
(`<name>-src.zip`); `install_plugin` installs a package into the portal. Without the
configuration below, without the skill, or for any other member, neither tool is offered.

A client asks `GET api/2.0/ai/plugin-agent` whether the caller is offered them (`{ available }`)
before it offers to build a plugin.

The agent is configured in the shared `appsettings.json` under `ai.pluginAgent`, or from the
environment, which wins:

| `appsettings.json`                  | Environment                              | Default                                 | Bounds     |
| ----------------------------------- | ---------------------------------------- | --------------------------------------- | ---------- |
| `ai.pluginAgent.sandboxUrl`         | `AI__PLUGIN_AGENT__SANDBOX_URL`          | —                                       |            |
| `ai.pluginAgent.sandboxToken`       | `AI__PLUGIN_AGENT__SANDBOX_TOKEN`        | —                                       |            |
| `ai.pluginAgent.taskTimeoutSeconds` | `AI__PLUGIN_AGENT__TASK_TIMEOUT_SECONDS` | `1800`                                  | 60 – 7200  |
| `ai.pluginAgent.maxToolCalls`       | `AI__PLUGIN_AGENT__MAX_TOOL_CALLS`       | `150`                                   | 1 – 10000  |
| `ai.pluginAgent.maxConcurrentTasks` | `AI__PLUGIN_AGENT__MAX_CONCURRENT_TASKS` | `2`                                     | 1 – 16     |
| `ai.pluginAgent.sandboxTtlSeconds`  | `AI__PLUGIN_AGENT__SANDBOX_TTL_SECONDS`  | `600`                                   | 60 – 3600  |
| `ai.pluginAgent.serviceUrl`         | `AI__PLUGIN_AGENT__SERVICE_URL`          | — (the package registry is off)         |            |
| `ai.pluginAgent.registryUrl`        | `AI__PLUGIN_AGENT__REGISTRY_URL`         | `https://registry.npmjs.org`            |            |
| `ai.pluginAgent.cacheDir`           | `AI__PLUGIN_AGENT__CACHE_DIR`            | `asc-ai-chat/npm-cache` in the temp dir |            |
| `ai.pluginAgent.cacheMaxMb`         | `AI__PLUGIN_AGENT__CACHE_MAX_MB`         | `2048`                                  | 64 – 65536 |

The agent is on when the sandbox's URL and token are both set; for these two a variable that is
set wins even when it is empty. A number out of its bounds falls back to the next source or the
default, with one warning in the log.

**The sandbox is not part of this service.** It is the sandbox gateway of the document agent's
repository (`gateway.v1.SandboxService`), provided and deployed by its owner; this service only
calls it (`app/pluginAgent/sandboxClient.ts`). What it relies on:

- the gateway's `Create`, `Renew`, `Delete`, `Exec` and `MakeDir` methods and the file routes
  `/sandboxes/{id}/files?path=`, with the bearer token above;
- `sandboxToken` being the token of a gateway profile that gives Node sandboxes (`plugin` in the
  examples of that repository): the gateway picks the image, the size and the egress by the token;
- an image with `node`, `npm`, `git` and `unzip` on the path and a writable `/workspace` (the
  sources archive is written by `git archive`);
- a way for npm in a sandbox to its packages: the cache of the image and the egress of the
  profile, or the package registry below.

A sandbox is created per task with the lifetime `sandboxTtlSeconds`, renewed three times within
it while the task runs and deleted when the task ends. A task that dies with the process, or a
deletion the gateway does not take, leaves the sandbox to expire within that lifetime.

**The package registry.** Off unless `serviceUrl` is set, and meant for sandboxes that are given
no way to a registry. With `serviceUrl`, the address a sandbox reaches this service at, a sandbox
installs npm packages from this service:
`GET /plugin-agent/npm/...`, outside the API prefix, forwards to `registryUrl` and keeps the
archives it fetched in `cacheDir` (the one read longest ago is dropped past `cacheMaxMb`). The
route answers only a request that carries the token of a running task. This service then needs
outbound access to the registry, and the sandbox's profile egress to `serviceUrl`, which has to
lead to the very instance that runs the task (the token is known to it alone); any package may be
requested. The cache of the sandbox image is not used then: npm keeps it by registry address. A redirect of the registry is
not followed (a mirror has to serve its archives itself), at most four requests are served at a time, and
a document or an archive is held in memory while it is served (up to 64 and 128 MiB). The limit of
concurrent tasks is per instance of the service, for all its portals together.

**The skill** is the git submodule `skills/agent-skills` (`ONLYOFFICE/agent-skills`), pinned by
its commit: run `git submodule update --init` before `docker build`, as the image copies it. Its
`SKILL.md` and `references/` are read by the model from this service; its `scripts/` and
`templates/` are copied into the sandbox when a task starts.

**Installing.** `install_plugin` uploads the package with `POST api/2.0/settings/webplugins`
under the user's session, so the portal's own checks apply. The call needs the user's approval
each time: under the "Allow" permission mode it installs nothing and says what to change, and
with the tool on the always-allowed list it installs nothing and takes the tool off the list, so
that the next call asks. `plugin_agent` asks for approval too, but may be pre-approved.

### Sandbox of the chat

The chat's own model can run commands in a sandbox (`app/sandbox/`): `sandbox_exec`,
`sandbox_read_file`, `sandbox_write_file`, and `sandbox_save_file`, which saves a file of the
sandbox as a new file of the workspace under the user's session: among the artifacts of a
conversation of an AI project, else into the current folder when the user may create there, else
into My documents; an empty file is not saved. Nothing is delegated to a sub-agent. The tools
are offered to every caller of the chat when the sandbox is configured, in the shared
`appsettings.json` under `ai.sandbox` or from the environment:

| Setting                     | Environment                     | Default | Bounds    |
| --------------------------- | ------------------------------- | ------- | --------- |
| `ai.sandbox.url`            | `AI__SANDBOX__URL`              | —       |           |
| `ai.sandbox.token`          | `AI__SANDBOX__TOKEN`            | —       |           |
| `ai.sandbox.idleTtlSeconds` | `AI__SANDBOX__IDLE_TTL_SECONDS` | `900`   | 60 – 3000 |

It is the same gateway the plugin agent calls (`app/pluginAgent/sandboxClient.ts`), with the
token of the profile a chat sandbox is made from. What is said here of the gateway is read from
its source (the `document-agent` repository at commit f7f4509) and has not been run against a
gateway yet: its example profile `chat` (`GATEWAY_TOKEN_CHAT`, image `sandbox-chat`: Python 3.12
with libraries for data, charts and office files, no egress) is what the description of
`sandbox_exec` tells the model to expect.

**The profile has to be one without egress.** The tools ask for no approval of their own, so a
call pauses only under the permission mode that asks about every tool. The sandbox never holds a
credential of the portal, but the model can write into it what it read of the workspace: with
network access that content would have a way out nobody is asked about. This service does not
check the profile.

**One sandbox per user, one directory per conversation.** A tool call that is not refused before
the gateway asks it for the sandbox of the session that is the caller's user id
(`GET people/@self`): the gateway answers the one that runs, or starts one. A call is refused
before the gateway outside a conversation, for its arguments, when the portal does not say who
the caller is and, for a save, when there is no folder to save into or it cannot be read. All
conversations of a user share the sandbox, and each works in `/workspace/{threadId}`: a command
runs there, and a relative path of a file tool is read from there. Nothing about a sandbox is
stored by this service or by the AI service, so any instance serves any call.

**Lifetime.** Every call the gateway answers a sandbox for moves its expiry to the idle lifetime
plus 600 seconds from now, 600 being the longest timeout a command may ask for. The lifetime is
the same for every call, a file call too, so that no call moves the expiry back: not under a
command another conversation runs, and not into the idle time that follows a command. Nothing
renews the sandbox between calls. The gateway takes a lifetime of a minute to its maximum, an
hour by default (`GATEWAY_SANDBOX_MAX_TTL`), which is why the idle lifetime is 60 to 3000
seconds. This service never deletes a sandbox, not when a thread is deleted either: one nobody
calls expires at the gateway, and the next call starts an empty one. A call cannot tell a
sandbox it started from one that ran, so the model is not told; the description of
`sandbox_exec` says how long files stay at least.

### Automatic history compaction

The chat engine can fold a thread that nears its model's context window into a summary message
before the round, and send the summary in place of the older turns from then on. The summary is
stored as an ordinary message of the thread (the marker lives in its `contents`), so the storage
needs nothing new. On by default; switched off with `ai.compaction.enabled: false` in
`appsettings.json` or `AI__COMPACTION__ENABLED=false` (or `0`). The environment wins over the file:

| `appsettings.json`                   | Environment                              | Default  |
| ------------------------------------ | ---------------------------------------- | -------- |
| `ai.compaction.enabled`              | `AI__COMPACTION__ENABLED`                | `true`   |
| `ai.compaction.threshold`            | `AI__COMPACTION__THRESHOLD`              | `0.8`    |
| `ai.compaction.keepRecent`           | `AI__COMPACTION__KEEP_RECENT`            | `4`      |
| `ai.compaction.defaultContextWindow` | `AI__COMPACTION__DEFAULT_CONTEXT_WINDOW` | `128000` |

The window of a model comes from the library's id-based table, else the default above. The gateway
catalogue's `context_length` reaches the model list, but a saved profile does not keep it yet (the
C# side has no column for it).

In the Docker install the two required values come from `DOCUMENT_AGENT_URL` and
`DOCUMENT_AGENT_TOKEN` in `install/docker/.env`. For the URL and the token a variable that is set
wins even when it is empty, so leaving either empty there switches the agent off whatever
`appsettings.json` names.

A task runs on the chat's own model profile and sees the custom MCP servers of the current
scope. A profile of the portal's own provider is reached through `/api/2.0/ai/gateway` under
`portalUrl`, with the user's session and the round's source (the entry the chat bills to), so
the agent's model requests are billed like the chat's own; the default is the AI service's own
address, which an agent on the same network reaches, while an agent elsewhere needs the public
portal address there.
The portal's own `docspace` server of `ai.mcp` is always handed over too, with the user's
session: no file content is sent, the ids of the files attached to the message, and of the files
and folders the model names, are appended to the task's text, and the agent reads them itself
through that server. Of its tools the agent takes the reading ones only (those docspace-mcp
annotates read-only: `get_*`, `download_file_as_text`), as it does for every server it is not
told `readOnly: false` about: it runs its tools without asking anyone, so nothing of the user's
workspace may be changed from a task. Next to it goes a small MCP server of this service itself
(`app/tools/documentAgentFiles.ts`, at `/document-agent/files/mcp` under `filesUrl`) with one
tool, `workspace_get_file_link`: a workspace file id in, the Files API's pre-signed download
link out, re-hosted on `portalUrl` (else on the portal address this service itself uses, `API_HOST`); the agent fetches the link itself, so the file lands in its
sandbox as it is and Document Builder can open or edit it (`docspace_download_file_as_text`
gives only the text). The agent presents the document agent's token (`X-Docagent-Token`) and
the user's session, and a link is minted only for a file that session may read. With
`shareSystemServers` the other host-configured servers of `ai.mcp` go
along as well. Every server handed over must be reachable from the agent's host, as the agent
refuses a task whose server it cannot connect to: the portal server's `ai.mcp` endpoint and
`filesUrl` included, so an agent outside the deployment's network needs both named for it. The
originals are
never changed: the files the agent produces are saved as new files into the agent room's Result
Storage, else the current folder, else My documents. While an agent is configured the
`onlyoffice_generate_*` tools, which only create empty files, are not offered.

While the task runs the agent's steps (its one-line intent before every tool call) are shown live
under the tool card: each becomes an MCP-shaped progress notification of the tool call, which the
engine streams (`toolProgress`) until the result replaces it; nothing of it is persisted.

### AI projects

An AI project is a room of type 12 (`FolderType.AiProjectRoom`, 40) under the AI rooms root
(`FolderType.AiHub`) that links Instructions rooms (type 10) and Knowledge rooms (type 11) as its
sources and shares them with a team. The client creates, lists, updates and deletes projects
through the ordinary Files API (`files/rooms`, with `roomType=12` / `searchArea=AiHub&type=12`)
and manages their sources and skills through `files/rooms/{id}/sources` and
`files/rooms/{id}/skills` (`AiHubController` in ASC.Files).

The project's chat model is in the Files API as well: `GET/PUT files/rooms/{id}/model`
(`{ profileId }`, readable by every member, set by the owner and Project managers, never
cleared). This service has no project route of its own.

A chat round whose `entityId` is a project (`storage/aiProjectChatContext.ts`) runs on the
project, not on what the composer sent: members only (the room's `inRoom`), the project's model
(else the portal's Chat model), the linked Instructions rooms as context (skills) rooms and the
linked Knowledge rooms as knowledge bases. It reads the project from the Files API with the caller's credentials —
`GET files/rooms/{id}`, `GET files/rooms/{id}/sources`, `GET files/rooms/{id}/model` and
`GET files/rooms/{id}/skills`. The
skills of the Instructions rooms picked outside a project are read from
`GET files/rooms/skills?roomIds=…` (`storage/roomSkills.ts`).

### License

[GNU AGPL V3](LICENSE)
