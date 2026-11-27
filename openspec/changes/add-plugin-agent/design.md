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
  `PUT`/`GET /sandboxes/{id}/files?path=`. Every route but `/healthz` wants a bearer token. The image, the
  sandbox size and the egress come from a profile of the gateway's profiles file; each profile has its own
  token, and a caller sees only the sandboxes of its token's profile. A sandbox has no egress unless its
  profile allows a target. The repository's `plugin` profile is an image with Node, npm, `git`, `jq` and
  `unzip` (no `zip`), the dependencies of a generated plugin project in npm's cache, and egress to
  `registry.npmjs.org` only.
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
async function bound to a timer and to the signal of the round's reply, which the controller now leaves in
the request context: a tool call is handed no signal of its own; a registry in memory holds the running
tasks, keyed by a task id, for the concurrency limit and for the registry tokens (decision 6). An entry
holds a random id and a random token and nothing of a portal or a user, and nothing in it is read across
tasks.

*Alternatives:* a child process or a worker thread per task isolates memory, but the work is network I/O and
the model profile, the request context and the progress callback would have to be passed across; a
detached task with a later notification needs a delivery channel to the chat that does not exist. Both were
set aside by the user's choice of "a task in the same process, the call waits".

*What is given up:* a restart ends the tasks that run, with no result; the chat reply they belong to ends
with the stream. See Risks.

### 2. A loop written for this task

A small loop over the `custom` action of `@onlyoffice/ai-chat/core`, modelled on `formAnalysisSubAgent.ts`
for the call shape and on the document agent's `task/loop.ts` for what a long task needs: a tool-call
budget that ends the task when it is spent, a nudge (twice at most) when the model asks a question nobody
can answer, and history compaction with the library's own `compactHistory`. It runs on the chat's own model
profile and bills to the chat's source, as `document_agent` does.

The model's tools:

| Tool | What it does |
|---|---|
| `bash` | one shell command in the workspace through `Exec`, with a timeout; output capped |
| `read_file`, `write_file` | a file of the sandbox by absolute path, through the file routes; size capped |
| `read_skill` | a file of the skill's `SKILL.md` and `references/`, read from the service's own disk |
| `deliver_plugin` | runs the gate of decision 5 and ends the task when it passes |

The sub-agent can ask nobody (the nudge above), so questions belong before the call. The description of
`plugin_agent` gives the chat's model three things: what a plugin can add to the portal (the entry points and
surfaces of the skill's scopes, in the product's words), what it cannot do (anything inside an open
document, work without a user's open tab, settings over 255 characters, a package over 5 MB), and what to
ask the user before the call - where the feature starts, what it shows or does and on what, who sees it,
the domains of an external service, what an admin configures - with the chat's own `ask_user_question`
tool when the client offers it. A request no plugin can serve is to be answered with the closest thing
that can be built, not with a task. This is guidance to a model, not a check of the service: a call with a
short `task` still starts a task. The text retells the skill and is not derived from it, so a move of the
submodule is the moment to read it again.

*Alternatives:* hand the chat's model the skill's own texts through a reading tool (an extra round and
the SDK's vocabulary for a handful of questions); build the list from the skill's "Pick scopes" table at
run time (a parser of somebody else's Markdown, and the limits are not in that table).

*Alternative:* copy the document agent's loop, client and tools. Rejected by the user in favour of code
written for this task; the copy would also bring the generated protobuf client and its two dependencies.

### 3. The skill as a submodule, split between the service and the sandbox

`ONLYOFFICE/agent-skills` becomes a submodule under `common/ASC.AI.Chat/skills/agent-skills`, pinned by its
commit; the service's Docker image copies it with the rest of the directory (`COPY . .`), so a build needs
the submodule checked out. A missing skill switches the feature off, said once in the log, and does not
stop the service; it is looked for when the tools are offered, not at start.

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
messages), files as HTTP bodies. Its requests go through `providerFetch`, the service's helper for an
address that is not one of the .NET services, so the ESLint rule that keeps `fetch` in the upstream clients
needs no exception; every call is bounded with `withTimeout`.

Sandbox lifetime: `Create` with a short `ttl` and a fresh session UUID per task, `Renew` on a timer while
the task runs, `Delete` in a `finally`. A `Delete` that fails is logged and left to the `ttl`.

*Alternative:* `@connectrpc/connect` with a copy of the generated `sandboxes_pb.ts`. Less protocol code of
our own, two more dependencies and a generated file to keep in step with another repository.

### 5. The delivery gate

`deliver_plugin` is the only way a task succeeds, and it is run by the harness, not reported by the model:

1. The project directory is a plain path under `/workspace` (names of letters, digits, `.`, `_`, `-`); the
   harness asks the sandbox whether it is a directory, so a wrong one is something the model reads.
2. `npm run build`, `npm run typecheck`, `node <skill>/scripts/validate-plugin.mjs . --invoke`, each through
   `Exec` in that directory; the first non-zero exit is returned to the model as the tool's result, and the
   loop goes on within its budget.
3. On success: read `name`, `version` and the direct dependencies from the project's `package.json` (each
   bounded and checked for its shape, since they go into the result the chat model reads); read
   `dist/plugin.zip`; archive the project without `node_modules`, `dist` and `.git` inside the sandbox and
   read that. Either file over 32 MiB fails the gate.
4. Save both files under the caller's session where `document_agent` would (among the artifacts of a
   conversation of an AI project, else the current folder when the caller may create there, else My
   documents). Each save answers for itself: a refusal says "not saved", an unknown answer "may have been
   saved", and the package alone is returned with a warning when only the sources fail.

What the gate is not: a defence against the model. The commands run in the sandbox, where the model can
write any file, the project's scripts and the copied validator included. The gate catches a plugin that does
not build or load; the admin-only offer and the approval of an installation are the security boundary.

A package over the portal's upload limit is still saved: the portal judges it at installation ("Uploading a
plugin package", 5 MB), and the result names its size in bytes.

### 6. The registry route

`/plugin-agent/npm/*`, registered outside the API prefix like the document agent's file routes, so the
portal's router does not forward to it and only the internal network sees it.

- **Off by default:** the route serves only an installation that names the address a sandbox reaches this
  service at. Without that address no registry is written into the sandbox's `.npmrc` and the route
  answers 401 to everybody: npm then works as the sandbox's owner set it up, from the cache of the image
  and through the egress of the profile. The route is for an installation whose sandboxes are given no way
  to a registry; with it the image's cache is not used, since npm keeps its cache by registry address.

- **Gate:** a random token per task, given to the sandbox in an `.npmrc` (`registry=` and the matching
  `_authToken`), compared in constant time against the tokens of the running tasks. The token dies with the
  task, and it is known to the instance that runs the task only.
- **What is served:** `GET`/`HEAD` of a package document and of an archive. The archive links of a document
  that lead to the registry are rewritten to this route; an archive request is mapped back to a path of the
  configured registry. No address is taken from a request or an answer: a redirect of the registry is not
  followed, it is a failed fetch.
- **Memory:** a document (up to 64 MiB) and an archive (up to 128 MiB) are held in memory while they are
  served, so at most four requests are served at a time in an instance and the rest wait: a request takes
  its turn before it fetches or reads from the cache and keeps it until its answer has left or its client is
  gone, which also ends its fetch. Streaming is left out (see Known limitations).
- **Cache:** archives are immutable and kept on disk under a configured directory (the system's temporary
  directory unless the installation names one) with a size cap; past it the ones read longest ago go, down
  to nine tenths of the cap. Only the cache's own files in that directory are counted or deleted, and a
  half-written one of the last ten minutes is left alone. The count is an estimate between listings, so
  requests in flight can pass the cap by what they write. Package documents are not kept.
- **Lock files:** the sandbox's npm is told to leave registry addresses out of `package-lock.json`, so the
  sources archive does not carry this service's internal address.
- **Upstream:** the public registry unless the installation names another (a mirror), reached through
  `providerFetch` with a timeout and a size limit on every body.

*Alternatives the user set aside:* a fixed list of allowed packages; pre-fetched archives only; a separate
Verdaccio.

### 7. Approval of the two tools

`plugin_agent` is declared with `requireApproval: true` under a server type listed in the engine's
`systemServerTypes`: unlike `document_agent` it asks under the default permission mode too, since a task
takes many minutes of a sandbox and of the model. The user may pre-approve it, and the mode that runs every
tool unasked runs it as well.

`install_plugin` has a server type of its own and must never run on a standing permission, because the
package then runs in every user's browser. The engine (`@onlyoffice/ai-chat`, `resolveAutoAllow`) pauses the
call under "ask" always and under "auto" unless the tool is on the user's always-allow list; under "allow"
it pauses nothing, and it has no way to pin one tool to "always ask". The adapter closes both gaps before it
reads the file:

- under "allow" it installs nothing and says that the mode has to change;
- under "auto" with the tool on the list it installs nothing, removes the entry (from the list read anew,
  not from the round's snapshot) and asks to be called again, so the next call pauses. The engine writes that entry before it runs the very call the user approved with
  "always allow", so that call lands here too: the user is asked once more instead of being sent to the
  settings, and the adapter never has to tell a fresh approval from an old one.

A per-tool "always ask" in the library would turn both refusals into a dialog (a follow-up for
`@onlyoffice/ai-chat`).

The upload itself is a multipart `POST api/2.0/settings/webplugins` with the caller's forwarded headers,
through the portal's People/settings client. The portal's answer is the truth: the tool checks nothing the
portal checks (role, switches, archive, name, limit). A 400 says that nothing was installed. A 403 does not:
the portal answers it before the installation (no right, uploads off) and after it (the plugin's domains do
not fit into the CSP header), so the answer relays the status and message and says that it is not known.
After a 5xx, a timeout or a 200 it cannot read, the answer says that the plugin may have been installed.

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
| address the sandbox reaches this service at | none (registry route off) | — |
| upstream registry of the route | `https://registry.npmjs.org` | — |
| cache directory, cache size cap in MiB | `asc-ai-chat/npm-cache` under the system's temporary directory, 2048 | 64 – 65536 |

Each has an environment override in the `AI__PLUGIN_AGENT__*` form. A value out of range falls back to the
next source (the appsettings for an environment value) or the default, with one warning.

### 9. What the sandbox is expected to give

The sandbox is somebody else's deliverable, so the design fixes only what the client side relies on. This
list is the request to its owner, and the place to change first when the answer differs:

- **API:** the gateway API as the document agent's repository documents it today — `Create`, `Renew`,
  `Delete`, `Exec`, `MakeDir`, and file bytes over `/sandboxes/{id}/files?path=`, with a bearer token.
  Nothing else of it is used: no MCP tunnel, no `ListDir`, no `Stat`, no `Remove`.
- **Image:** `sh`, `node`, `npm`, `git`, `unzip`, `mktemp`, `mkdir`, `rm` and `test` on the path, and a
  writable `/workspace`: the client works under that path and nowhere else. The sources archive is written
  by `git archive`, as the `plugin` image has no `zip`. The skill's scripts are copied in by ASC.AI.Chat,
  so the image does not have to carry them.
- **Network:** a way for npm in a sandbox to its packages. Either the profile's own (the image's cache and
  egress to a registry, as the `plugin` profile has), or, with the registry route on, egress to
  ASC.AI.Chat at the configured address. That address has to lead to the very instance that runs the task:
  the registry token is known to it alone.
- **Selection:** the token in `ai.pluginAgent` is expected to be the token of a profile that gives Node
  sandboxes (`plugin` in the repository's examples).
- **Lifetime:** a `ttl` of at least 60 seconds that `Renew` extends, and a refusal that can be told from
  "unreachable" when no sandbox is free (`RESOURCE_EXHAUSTED` today).

### Existing behaviour the design relies on

- `ai-agents` — "Tool calls and the approval pause" and "Decisions on a tool call": the pause, approval and
  denial of both tools are the engine's own. "Tool permission mode of a user": the three modes and the
  default one, which decide when the engine asks. "Pre-approving tools": the list `install_plugin` must not
  be served from, and the routes a user re-grants an entry through after the adapter removed it.
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
- **Install scripts of arbitrary packages run in the sandbox** → the sandbox is a VM whose egress is what its
  profile allows (a registry, or this service with the registry route on), holds no portal credential, and
  is removed when the task ends.
- **The registry route is an open proxy to the upstream for whoever holds a token** → tokens exist only while
  a task runs, are random and compared in constant time; only `GET`/`HEAD`; paths are mapped, not followed;
  the cache is capped.
- **A restart of the service loses running tasks and leaves their sandboxes** → short `ttl` with renewal, so
  an orphan expires within the `ttl`; the reply ends with the stream, and the user asks again.
- **A long loop in the request-serving process** → the concurrency cap, the tool-call budget, output and
  file size caps on every tool, compaction of the history.
- **The hand-written `Exec` stream reader may disagree with the gateway on a protocol detail** → unit tests
  against a fake built from the gateway's documented frames; task 3.4 runs the client against the provided
  sandbox, which had not been delivered when the loop was written, so that run is still to come.
- **The sandbox is delivered by someone else and may differ from what decision 9 assumes, or arrive late** →
  everything but tasks 3.4 and 7.1 is built and tested against a fake gateway; the gateway is called from one
  module only, so a different API changes that module and its tests.
- **The submodule must be present in the Docker build context** → the build fails the feature, not the
  service: a missing skill switches the tools off with a warning; the README of the service says so.
- **The skill's toolchain changes upstream** (script names, flags) → the submodule is pinned; a bump is a
  commit that re-runs the task tests.

## Known limitations

- **One quota for all portals.** The limit of concurrent tasks is per instance of the service, not per
  portal or user: two long tasks of one caller keep every other portal of that instance out until they end.
- **One instance, with the registry route on.** The registry token of a task is known to the instance that
  runs it, so with several instances behind one address the configured address has to lead to the instance
  itself.
- **The registry holds bodies in memory.** Four requests at a time, each an archive of up to 128 MiB (and
  the chunks it arrived in while it is read) or a document of up to 64 MiB with its parsed form. Streaming
  to disk and to the client would remove that cost.
- **A file route's 404 is read as "no such file".** The gateway answers 404 for an unknown sandbox too, so a
  sandbox that expired mid-task first shows as a missing file; the next command reports it as lost.
- **`install_plugin` installs any zip archive the admin can read**, by the file id the model names. The
  approval shows that id, not the content: a file replaced by another room member between the build and the
  approval, or an archive the model was talked into naming, is installed if approved.
- **The delivery gate does not hold against the model** (decision 5).
- **The upload and the registry are tested against stubs.** The request to `settings/webplugins` and what a
  real `npm install` asks of the route are first seen against the real services in task 7.1.

## Migration Plan

1. Ship the service with the block absent: nothing changes.
2. Per deployment, once the sandbox service is there: add `ai.pluginAgent` with the gateway's address and
   the token of its plugin profile. Only where sandboxes get no way to a registry: also name the address
   they reach ASC.AI.Chat at, open that address in the profile's egress and give ASC.AI.Chat outbound
   access to the registry.
3. Rollback: remove the block. Files that tasks saved and plugins that were installed stay, as ordinary
   files and plugins; the cache directory can be deleted.

## Open Questions

- None. The sandbox image warms an npm cache of its own; whether an installation uses it or the registry
  route is the configuration's choice (decision 6).
