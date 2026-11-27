# Proposal

## Why

The chat's model can read and write the workspace, but it cannot compute: a question that needs a script
run, a data file transformed or an archive unpacked ends in a text answer. The installation may already run
a sandbox gateway for `document_agent` and `plugin_agent`, which use it behind a sub-agent and for one task
at a time. Giving the chat's own model a sandbox lets it run a command and hand the
result to the user, without a second model and without a task that takes minutes.

## What Changes

- The AI chat offers four new tools to every caller of the chat on an installation that configures a chat
  sandbox: `sandbox_exec` (one shell command), `sandbox_read_file`, `sandbox_write_file` (a text file of the
  sandbox) and `sandbox_save_file` (a file of the sandbox saved as a new file of the workspace). The chat's
  own model calls them; nothing is delegated to a sub-agent.
- A caller has one sandbox, shared by all their conversations: a call that is not refused before the
  gateway asks it for the sandbox of the session that is the caller's user id, and the gateway answers the
  one that runs or starts one. Each conversation works in a directory of its own, `/workspace/{threadId}`.
- The sandbox is kept by use alone: every call the gateway answers a sandbox for moves its expiry to the
  idle lifetime of the installation's configuration plus 600 seconds, the longest command timeout, from
  now, and nothing renews it between calls. ASC.AI.Chat never deletes it, not when a thread is deleted
  either: a sandbox nobody calls expires at the gateway, and the next call starts an empty one.
  This differs from `plugin_agent`, whose sandbox lives for exactly one
  task, renewed by a timer and deleted when the task ends.
- Nothing about a sandbox is stored, in ASC.AI.Chat or in the AI service, so any instance serves any call.
- The sandbox never holds a credential of the portal: a file reaches the workspace only through
  `sandbox_save_file`, which uploads it in ASC.AI.Chat under the caller's session, where `document_agent`
  saves its results.
- The sandbox service itself is not part of this change: it is the gateway the plugin agent already calls,
  reached with an address and a token of its own configuration block.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-agents`: new requirements for the chat sandbox — who is offered the tools, what each takes and
  answers, whose sandbox a call runs in and how long it lives. Found by the requirements that govern the
  sibling tools: "Tool calls and the approval pause" and "Tool permission mode of a user"
  (`aiApproveToolCall`, `aiDenyToolCall`), "Saving an artifact of a thread" (where a saved file of a project
  conversation goes). No existing requirement is modified.

## Breaks current behaviour

None. Every requirement of the change is added, and an installation without the configuration block behaves
as before: no tool is offered, no route is called.

- **`aiThreadsDelete`** is not touched: a deleted thread leaves its directory in the sandbox, which goes
  when the sandbox expires.
- **Web client (`../client`)**: the tools reach it through the existing tool-call and approval flow; a
  client that knows nothing of them shows them as any other tool call.
- **Published SDKs and the OpenAPI document**: no operation is added, removed or changed.
- **Webhooks, socket events, audit**: none. A file `sandbox_save_file` saves is an ordinary upload under the
  caller's session and is announced as one.
- **Backup and restore, portal migration, stored data**: no table, column, stored setting or Redis key
  is added.
- **Tool preferences**: the group of the new tools is not among the groups `aiToolsSetDisabled` accepts
  ("Switching tools off" is unchanged), so a user cannot switch them off; the installation's configuration
  decides.

No row of `openspec/deviations.md` is touched.

## Impact

- **`common/ASC.AI.Chat`**: a new tools adapter with the four tools, the acquisition of the caller's
  sandbox, a read of the caller's id from `GET people/@self`, a new configuration block; the sandbox gateway
  client of the plugin agent is reused as it is.
- **`products/ASC.AI`**: nothing.
- **Dependencies**: none added.
- **External dependency, not built here**: the sandbox gateway and a profile of it for chat sandboxes.
- **Security**: a command runs, unasked under the default permission mode, in a machine that holds no
  credential of the portal; what the model can do to the workspace through the sandbox is to save a new file
  under the caller's own rights. The gateway profile is required to have no egress, which the chat does not
  check. See design.md, Risks.
