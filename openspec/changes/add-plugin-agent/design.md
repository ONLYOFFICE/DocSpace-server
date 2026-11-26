# Design

## Context

See proposal.md for the motivation. What shapes the approach:

- **Two delegating tools already exist in ASC.AI.Chat.** `analyze_form` runs a hand-driven loop over the bare
  `custom` action inside the service (`app/tools/formAnalysisSubAgent.ts`, 20 rounds at most);
  `document_agent` hands the task to another process over A2A (`app/tools/documentAgent.ts`) and waits for
  it, forwarding its steps. Both are a `ToolsAdapter` with one tool, composed in `aiController.ts`.
- **The sandbox is the gateway of the document agent's repository** (`gateway.v1.SandboxService`): `Create`
  (idempotent by a session UUID, with a `ttl`), `Get`, `Renew`, `Delete`, `Exec` (one shell string, streamed
  stdout/stderr and an exit event), `ListDir`, `Stat`, `MakeDir`, `Remove`, and file bytes over
  `PUT`/`GET /sandboxes/{id}/files?path=`. Every route but `/healthz` wants a bearer token. The image and the
  sandbox size come from the gateway's configuration, one image per gateway, and a sandbox has no egress
  unless the gateway's policy file allows a target.
- **The skill is more than text.** `apps-plugin-sdk` holds `SKILL.md` and `references/` for the model, and
  `scripts/generate-plugin.mjs`, `scripts/validate-plugin.mjs` with `templates/` that have to run next to the
  project. A generated project builds with `npm install && npm run build && npm run typecheck` and leaves
  `dist/plugin.zip`; the skill's own text says not to run `npx create-docspace-plugin`.
- **Rules of the service** (`.claude/rules/ts-ai-style.md` and its siblings): a bare `fetch` lives in the
  upstream clients only, module-level state is shared by every portal the process serves, configuration is
  read in `config/index.ts` through bounded readers, every upstream call is bounded by a timeout.
- **The service has been a thin HTTP layer so far.** A loop that runs for many minutes inside it is new
  here; `analyze_form` is the only precedent and it is short.

## Goals / Non-Goals

**Goals:**

- One process: no new service to deploy for the agent itself. The sandbox gateway is the only new piece of a
  deployment.
- The sandbox never holds a credential of the portal: no session, no API key. Everything that needs the
  caller's rights (reading the sources archive, saving the results, installing) happens in ASC.AI.Chat.
- A package leaves the sandbox only through a deterministic gate that the model cannot skip.
- A deployment without the configuration block behaves exactly as before.

**Non-Goals:**

- A general "skill runner": the loop and its tools are written for this one skill.
- Tasks that survive a restart of the service, or that keep running after the reply is stopped.
- A preview of the plugin (the skill's `preview-plugin.mjs`, Chromium) and screenshots.
- Per-portal settings of the agent (a switch, a model profile, limits). The installation's configuration and
  the caller's role decide; a settings object like the document agent's can follow in its own change.
- Building, changing or deploying the sandbox: its gateway, its image and its network policy belong to
  their owner. This change only connects to a sandbox service and uses it; the same goes for the document
  agent.

## Decisions

### 1. An in-process task, awaited by the tool call

`plugin_agent` is a `ToolsAdapter` like the two existing ones. Its `callTool` creates the task, awaits it
and reports steps through the engine's `ProgressCallback`, as `document_agent` does. The task is a plain
async function bound to the round's `AbortSignal` and to a timer; a registry in memory holds the running
tasks, keyed by a task id, for the concurrency limit and for the registry tokens (decision 6). Each entry
carries its portal and user, and nothing in it is read across tasks.

*Alternatives:* a child process or a worker thread per task isolates memory, but the work is network I/O and
the model profile, the request context and the progress callback would have to be passed across; a
detached task with a later notification needs a delivery channel to the chat that does not exist. Both were
set aside by the user's choice of "a task in the same process, the call waits".

*What is given up:* a restart ends the tasks that run, with no result; the chat reply they belong to ends
with the stream. See Risks.

### 2. A loop written for this task

A small loop over the `custom` action of `@onlyoffice/ai-chat/core`, modelled on `formAnalysisSubAgent.ts`
for the call shape and on the document agent's `task/loop.ts` for what a long task needs: a tool-call
budget, a nudge when the model asks a question nobody can answer, a "finish now" message when the budget is
spent, and history compaction with the library's own `compactHistory`. It runs on the chat's own model
profile and bills to the chat's source, as `document_agent` does.

The model's tools:

| Tool | What it does |
|---|---|
| `bash` | one shell command in the project directory through `Exec`, with a timeout; output capped |
| `read_file`, `write_file` | a file of the sandbox by absolute path, through the file routes; size capped |
| `read_skill` | a file of the skill's `SKILL.md` and `references/`, read from the service's own disk |
| `deliver_plugin` | runs the gate of decision 5 and ends the task when it passes |

*Alternative:* copy the document agent's loop, client and tools. Rejected by the user in favour of code
written for this task; the copy would also bring the generated protobuf client and its two dependencies.

### 3. The skill as a submodule, split between the service and the sandbox

`ONLYOFFICE/agent-skills` becomes a submodule under `common/ASC.AI.Chat/skills/agent-skills`, pinned by its
commit; the service's Docker image copies it with the rest of the directory (`COPY . .`), so a build needs
the submodule checked out. A missing or empty skill directory at start switches the feature off with one
warning and does not stop the service.

- `SKILL.md` goes into the system prompt of the task; `references/` is served by `read_skill`, which refuses
  any path that leaves the skill directory.
- `scripts/` and `templates/` are copied into the sandbox at task start (`MakeDir` plus one `PUT` per file,
  through `pooled`), under a fixed directory the system prompt names.

*Alternatives:* bake the skill into the sandbox image (the version then lives apart from the service and
changes with an image rebuild); publish it as an npm package served by the registry of decision 6 (needs
packaging the skill).

### 4. A hand-written gateway client

One new upstream client module, the only place that calls the gateway: unary methods as the plain JSON
`POST` the gateway documents, `Exec` as a Connect server stream (`application/connect+json`, enveloped
messages), files as HTTP bodies. It gets its place in the ESLint rule that keeps `fetch` in the upstream
clients, and every call is bounded with `withTimeout`.

Sandbox lifetime: `Create` with a short `ttl` and a fresh session UUID per task, `Renew` on a timer while
the task runs, `Delete` in a `finally`. A `Delete` that fails is logged and left to the `ttl`.

*Alternative:* `@connectrpc/connect` with a copy of the generated `sandboxes_pb.ts`. Less protocol code of
our own, two more dependencies and a generated file to keep in step with another repository.

### 5. The delivery gate

`deliver_plugin` is the only way a task succeeds, and it runs in the harness, not in the model:

1. `npm run build`, `npm run typecheck`, `node <skill>/scripts/validate-plugin.mjs <project> --invoke`, each
   through `Exec`; the first non-zero exit is returned to the model as the tool's result, and the loop goes
   on within its budget.
2. On success: read `dist/plugin.zip`; archive the project without `node_modules` and `dist` inside the
   sandbox and read that; read `name`, `version` and the direct dependencies from the project's manifest and
   `package.json`.
3. Save both files under the caller's session in the folder `document_agent` would choose (the current
   folder when the caller may create there, else My documents), with the thread kept as their origin when
   there is one.

A package over the portal's upload limit is still saved: the portal judges it at installation ("Uploading a
plugin package", 5 MB), and the result text says the size.

### 6. The registry route

`/plugin-agent/npm/*`, registered outside the API prefix like the document agent's file routes, so the
portal's router does not forward to it and only the internal network sees it.

- **Gate:** a random token per task, given to the sandbox in an `.npmrc` (`registry=` and the matching
  `_authToken`), compared in constant time against the tokens of the running tasks. The token dies with the
  task.
- **What is served:** `GET`/`HEAD` of a package document and of an archive. The archive links of a document
  are rewritten to this route; an archive request is mapped back to a path of the configured upstream, so
  the route never follows a URL taken from a response or a request.
- **Cache:** archives are immutable and kept on disk under a configured directory with a size cap (oldest
  first out); package documents are not kept. This is a file cache of a Node service, outside the
  FusionCache rule, which governs the C# services.
- **Upstream:** the public registry unless the installation names another (a mirror), reached through a
  second small upstream client with a timeout.

*Alternatives the user set aside:* a fixed list of allowed packages; pre-fetched archives only; a separate
Verdaccio.

### 7. Installation as a second tool with a hard approval

`install_plugin` is declared with `requireApproval: true` under a server type of its own that is listed in
the engine's `systemServerTypes`. The spec asks for more than `document_agent` has: no pre-approval and no
permission mode may skip the pause, because the package then runs in every user's browser. Whether the
engine can hold that for one tool is not known from this repository; task 6.2 proves it with a test, and if
the engine cannot, the adapter refuses to run without a fresh approval marker for the call and the gap goes
to the library as a follow-up.

The upload itself is a multipart `POST api/2.0/settings/webplugins` with the caller's forwarded headers,
through the portal API client. The portal's answer is the truth: the tool checks nothing the portal checks
(role, switches, archive, name, limit) and relays the status and message.

### 8. Configuration

One block, `ai.pluginAgent`, read in `config/index.ts`; the feature is on when the gateway address and token
are both present, as with `ai.documentAgent`.

| Value | Default | Bounds |
|---|---|---|
| sandbox gateway address, token | none (feature off) | — |
| task time limit, seconds | 1800 | 60 – 7200 |
| tool-call budget | 150 | 1 – 10000 |
| concurrent tasks | 2 | 1 – 16 |
| sandbox `ttl`, seconds | 600 | 60 – 3600 |
| address the sandbox reaches this service at | `http://localhost:<port>` | — |
| upstream registry | `https://registry.npmjs.org` | — |
| cache directory, cache size cap in MiB | under the service's data path, 2048 | 64 – 65536 |

Each has an environment override in the `AI__PLUGIN_AGENT__*` form. A value out of range falls back to the
default with one warning.

### 9. What the sandbox is expected to give

The sandbox is somebody else's deliverable, so the design fixes only what the client side relies on. This
list is the request to its owner, and the place to change first when the answer differs:

- **API:** the gateway API as the document agent's repository documents it today — `Create`, `Renew`,
  `Delete`, `Exec`, `MakeDir`, `Stat`, and file bytes over `/sandboxes/{id}/files?path=`, with a bearer
  token. Nothing else of it is used: no MCP tunnel, no `ListDir`, no `Remove`.
- **Image:** `node`, `npm`, `zip` and `unzip` on the path, and a writable working directory. The skill's
  scripts are copied in by ASC.AI.Chat, so the image does not have to carry them.
- **Network:** a sandbox reaches ASC.AI.Chat at the configured address and nothing else is needed.
- **Selection:** a gateway serves one image, so the address in `ai.pluginAgent` is expected to be an
  endpoint that gives Node sandboxes; how that endpoint is provided is the owner's choice.
- **Lifetime:** a `ttl` of at least 60 seconds that `Renew` extends, and a refusal that can be told from
  "unreachable" when no sandbox is free (`RESOURCE_EXHAUSTED` today).

### Existing behaviour the design relies on

- `ai-agents` — "Tool calls and the approval pause" and "Decisions on a tool call": the pause, approval and
  denial of both tools are the engine's own. "Pre-approving tools": the list `install_plugin` must not be
  served from.
- `plugins` — "Installation-wide plugin switches", "Uploading a plugin package", "Contents of a plugin
  package", "Plugin name and uniqueness", "Re-uploading a plugin under the same name", "Portal plugin count
  limit": every check of an installation, and the replacement of an installed plugin that makes a follow-up
  task installable. "Audit trail of plugin changes", "Clients are told when a plugin changes", "Plugins in
  backup and restore": what follows an installation.

"Breaks current behaviour" lists nothing, so there is no existing data to keep consistent: the change stores
only ordinary workspace files and the files of the cache directory.

## Risks / Trade-offs

- **A generated plugin runs in every user's browser and may bundle any npm package** → only the owner and
  DocSpace admins are offered the tools; the validator runs before delivery; an installation always asks,
  and the task's result names the direct dependencies the admin is about to trust. The portal's own archive
  checks and CSP handling apply on upload.
- **Install scripts of arbitrary packages run in the sandbox** → the sandbox is a VM without egress except to
  this service, holds no portal credential, and is removed when the task ends.
- **The registry route is an open proxy to the upstream for whoever holds a token** → tokens exist only while
  a task runs, are random and compared in constant time; only `GET`/`HEAD`; paths are mapped, not followed;
  the cache is capped.
- **A restart of the service loses running tasks and leaves their sandboxes** → short `ttl` with renewal, so
  an orphan expires within the `ttl`; the reply ends with the stream, and the user asks again.
- **A long loop in the request-serving process** → the concurrency cap, the tool-call budget, output and
  file size caps on every tool, compaction of the history.
- **The hand-written `Exec` stream reader may disagree with the gateway on a protocol detail** → unit tests
  against a fake built from the gateway's documented frames, and task 3.4 runs the client against the
  provided sandbox before the loop is built on it.
- **The sandbox is delivered by someone else and may differ from what decision 9 assumes, or arrive late** →
  everything but tasks 3.4 and 7.1 is built and tested against a fake gateway; the gateway is called from one
  module only, so a different API changes that module and its tests.
- **The submodule must be present in the Docker build context** → the build fails the feature, not the
  service: a missing skill switches the tools off with a warning; the deployment note of task 1.2 says so.
- **The skill's toolchain changes upstream** (script names, flags) → the submodule is pinned; a bump is a
  commit that re-runs the task tests.

## Migration Plan

1. Ship the service with the block absent: nothing changes.
2. Per deployment, once the sandbox service is there: give ASC.AI.Chat outbound access to the registry and
   add `ai.pluginAgent` with the sandbox's address and token.
3. Rollback: remove the block. Files that tasks saved and plugins that were installed stay, as ordinary
   files and plugins; the cache directory can be deleted.

## Open Questions

- Whether the sandbox image warms an npm cache of its own is its owner's call; the registry route works
  either way.
