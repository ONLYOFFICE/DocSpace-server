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

| `appsettings.json`                    | Environment                                | Default                  |
| ------------------------------------- | ------------------------------------------ | ------------------------ |
| `ai.documentAgent.url`                | `AI__DOCUMENT_AGENT__URL`                  | —                        |
| `ai.documentAgent.token`              | `AI__DOCUMENT_AGENT__TOKEN`                | —                        |
| `ai.documentAgent.taskTimeoutSeconds` | `AI__DOCUMENT_AGENT__TASK_TIMEOUT_SECONDS` | `1800`                   |
| `ai.documentAgent.maxToolCalls`       | `AI__DOCUMENT_AGENT__MAX_TOOL_CALLS`       | agent's                  |
| `ai.documentAgent.shareSystemServers` | `AI__DOCUMENT_AGENT__SHARE_SYSTEM_SERVERS` | `false`                  |
| `ai.documentAgent.portalUrl`          | `AI__DOCUMENT_AGENT__PORTAL_URL`           | the AI service's address |

In the Docker install the two required values come from `DOCUMENT_AGENT_URL` and
`DOCUMENT_AGENT_TOKEN` in `install/docker/.env`.

A task runs on the chat's own model profile and sees the custom MCP servers of the current
scope. A profile of the portal's own provider is reached through `/api/2.0/ai/gateway` under
`portalUrl`, with the user's session and the round's source (the entry the chat bills to), so
the agent's model requests are billed like the chat's own; the default is the AI service's own
address, which an agent on the same network reaches, while an agent elsewhere needs the public
portal address there.
With `shareSystemServers` the host-configured servers of `ai.mcp` (the portal's own
`docspace` server among them) are handed over too, with the user's session, so the agent can
read and write the workspace itself; their endpoints must then be reachable from the agent's
host, as the agent refuses a task whose server it cannot connect to. The files attached to the
message, and the workspace files the model names, are read under the user's credentials and sent
to the agent as bytes; the files the agent produces are saved into the agent room's Result
Storage, else the current folder, else My documents. While an agent is configured the
`onlyoffice_generate_*` tools, which only create empty files, are not offered.

While the task runs the agent's steps (its one-line intent before every tool call) are shown live
under the tool card, as text of the assistant message (`app/tools/toolProgress.ts`); the engine's
own events replace it with the result when the task ends, and nothing of it is persisted.

### License

[GNU AGPL V3](LICENSE)
