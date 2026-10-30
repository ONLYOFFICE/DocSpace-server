# ASC New AI Service

ASC New AI service — Express-based Node.js service for AI features integration.

### Installation and usage

```bash
$ yarn install
$ yarn start
```

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

In the Docker install the two required values come from `DOCUMENT_AGENT_URL` and
`DOCUMENT_AGENT_TOKEN` in `install/docker/.env`.

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

`/api/2.0/ai/projects/*` (`app/controllers/aiProjectsController.ts`) serves the AI Projects page:
a project is a room of type 12 that binds a model, Instructions rooms (type 10) and Knowledge rooms
(type 11) and shares them with a team under three roles — project owner (`access` 1), project
manager (9) and member (11). As for agents, every route is delegated to the .NET AI service
(`internal/ai/projects…`, `AiProjectsController`) and answers its DocSpace envelope unchanged; the
only check made here is that a chosen model exists and can chat.

The .NET contract, method by method, is
`client/packages/client/src/pages/AiProjects/spec/AiProjects-API-for-CSharp.md`.

#### Mock

Until the .NET side lands, `AI_PROJECTS_MOCK=1` (or `"aiProjects": { "mock": true }` in the
config; never in production) answers every project route in process
(`app/controllers/aiProjectsMockController.ts`, `app/storage/aiProjectsMock/`). Each signed-in
user gets a world of their own, built from a scenario on the first call, with themselves as the
owner, a Project manager, a Member or an admin outside the project, as each project says. The
rights of the C# contract are enforced, and so are its 400s (an unknown body field, a blank or too
long title, more than 20 rooms of a kind, a room linked twice).

| Scenario | What it holds |
|---|---|
| `default` | ~36 projects that stress the UI: 170-character titles with and without spaces, emoji, right-to-left, CJK and markup-like titles; 20 + 20 linked rooms; rooms with 150 skills, 1000 files of 2400 (truncated) or none; gone and archived rooms; a deleted model; 260 members with groups, guests and pending invitations; 120 history entries with grouped events and project events the client does not draw yet; expired and used-up invitation links; 9 pinned projects (the 10th pin works, the 11th is refused); a muted project; membership through a group only; a grant warning; a project whose every read answers 500 and one whose sources take 4 s |
| `many` | `default` plus 300 plain projects, for paging and infinite scroll |
| `empty` | no projects at all |
| `one` | one ordinary project |

- `GET /api/2.0/ai/projects/mock` tells the scenario; `POST /api/2.0/ai/projects/mock`
  `{ "scenario": "many" }` drops every world and rebuilds from that scenario. The start scenario
  is `AI_PROJECTS_MOCK_SCENARIO` (default `default`).
- `AI_PROJECTS_MOCK_DELAY_MS` delays every answer, to see the loaders.
- Renaming a project to a title with `#400`, `#403` or `#500` makes its next save fail with that
  status.
- Real Instructions and Knowledge rooms picked in the dialog are linked as they are: their skills
  and files are read live. A chat round inside a mock project runs on its model (the portal default
  when none is set; a deleted one answers 409) with only those real rooms; the generated rooms have
  no files to read.
- Deleting and duplicating answer a file operation that is already finished.
- Nothing survives a restart.

### License

[GNU AGPL V3](LICENSE)
