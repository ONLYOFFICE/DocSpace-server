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

### AI projects (mock)

`/api/2.0/ai/projects/*` (`app/controllers/projectsController.ts`) serves the AI Projects page: a
project is a room of type 12 that binds a model, Instructions rooms (type 10) and Knowledge rooms
(type 11) and shares them with a team under three roles — owner (`access` 1), project manager (9)
and member (11). The routes answer in the Files API's shapes (the DocSpace envelope, `FolderDto`
with an `aiProject` block, `FileShareDto` members) so the client can later be pointed at
`files/rooms` without changes.

For now the data is an in-memory mock (`app/storage/aiProjectsMockStorage.ts`): it is seeded on
start, lost on restart, and the caller is always the seeded portal administrator, who holds each
role once (owner of "Marketing copy", manager of "Legal review", member of "Support answers").
The mock enforces the access rules of the spec, including the one the real service will
implement in `files_security`: every member added to a project, and every member of a project a
room is added to, gets Viewer on that room unless they already have a share on it. Nothing is
revoked on removal.

### License

[GNU AGPL V3](LICENSE)
