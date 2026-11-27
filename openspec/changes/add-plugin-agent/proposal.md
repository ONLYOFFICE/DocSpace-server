# Proposal

## Why

A portal admin who wants a DocSpace plugin today has to set up the Plugins SDK toolchain, write the plugin by
hand and upload the package in the settings. ONLYOFFICE publishes an agent skill for exactly this work
(`apps-plugin-sdk` of `ONLYOFFICE/agent-skills`: references, a project generator and a validator), and the AI
chat already delegates long tasks to an agent (`document_agent`). Putting the two together lets an admin
describe a plugin in the chat and get it built, checked and installed without leaving the portal.

## What Changes

- The AI chat offers a new delegating tool, `plugin_agent`, to the owner and DocSpace admins of a portal
  whose installation configures a plugin agent. The tool hands a description of a plugin to a sub-agent that
  runs inside the ASC.AI.Chat process, as a background task of the chat round that called it; the call waits
  for the task and shows its steps, like `document_agent` does. Unlike it, the call asks for approval under
  the default permission mode too.
- The sub-agent works in a sandbox of its own, guided by the `apps-plugin-sdk` skill, which ships with
  ASC.AI.Chat as a git submodule. The sandbox service is not part of this change: it is built and deployed
  by its own owner, and ASC.AI.Chat only connects to it with the address and token of the installation's
  configuration and uses it through its existing API (design.md, "What the sandbox is expected to give"). Its texts are read by the model from the service; its scripts and
  templates are copied into the sandbox when a task starts.
- A package is taken from the sandbox only after the project built, passed its type check and passed the
  skill's validator. The task leaves two new files in the user's My documents, the plugin package and an archive
  of its sources, and installs nothing.
- A follow-up request ("add a button", "fix this") is a new task that starts from a sources archive of an
  earlier one, named by its file id.
- A second tool, `install_plugin`, installs a package file of the workspace through the existing
  `addWebPluginFromFile` operation under the caller's session. It never installs on a standing permission:
  the call is approved by the user each time, and where the user's settings would let it run unasked it
  installs nothing; a pre-approval of the tool is withdrawn when it is met, so that the next call asks.
- ASC.AI.Chat can serve the sandbox a read-only npm registry that forwards to the public registry and
  caches what it fetched, so that a sandbox needs network access to this service only; any package may be
  requested. The route is off unless the installation names the address a sandbox reaches the service at:
  without it npm in a sandbox uses what the sandbox gives it (the cache of its image, its own egress).
- A new read operation, `aiSettingsGetPluginAgent` (`GET api/2.0/ai/plugin-agent`), tells a client whether
  the caller is offered the plugin tools, so that it offers to build a plugin only where one can be built.
- No preview run and no screenshot in this change: the checks are the build, the type check and the
  validator.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ai-agents`: new requirements for the plugin agent — who is offered `plugin_agent` and `install_plugin`,
  what a task takes and returns, what a failed or interrupted task leaves, the approval of an installation,
  and the package registry a sandbox reads. Found by the tool and settings requirements that govern the
  sibling `document_agent` ("Tool calls and the approval pause", "Tool permission mode of a user",
  "Pre-approving tools", "Document agent settings") and the chat send operations they belong to (`aiApproveToolCall`, `aiDenyToolCall`). One existing
  requirement is modified, "Pre-approving tools" (`aiToolsSetAllowAlways`, `aiToolsIsAllowAlways`): see
  "Breaks current behaviour".

`plugins` is touched but not changed: `install_plugin` calls `addWebPluginFromFile`
(`POST api/2.0/settings/webplugins`) as any client would, and relies on "Installation-wide plugin switches",
"Uploading a plugin package", "Contents of a plugin package", "Plugin name and uniqueness", "Re-uploading a
plugin under the same name" and "Portal plugin count limit" exactly as they are.

## Breaks current behaviour

One requirement is modified, `ai-agents` "Pre-approving tools", in its scenarios and only for the new tool:

- **What a caller sees differently**: a pre-approval of `install_plugin` does not last. Today
  "`allowAlways` on an approval lands in the same list … where the read finds it", and the scenario "Approval
  with allowAlways" says the tool's next call runs without a pause. For `install_plugin` the call that meets
  the entry installs nothing and removes it: after an approval with `allowAlways`, or a grant through
  `aiToolsSetAllowAlways`, `aiToolsIsAllowAlways` answers false once the tool was called, and the next call
  pauses again. Only this tool's entry is taken out, and only from the caller's own list; the list is read
  anew and written back whole, as every write of it is, so a change the user makes to it in the same moment
  can be lost.
- **Who is affected**: only the owner and DocSpace admins of a portal with a plugin agent, since nobody else
  is offered the tool. In the web client the "always allow" choice on this tool's dialog has no lasting
  effect and that call installs nothing; the client shows the tool's answer, which says to ask again. The
  operations the tool is reached through are unchanged (no field or status changes); webhooks,
  socket events, backup and restore, the portal migration and stored data are not affected, as the list is
  the user's existing tool preferences and is only made shorter.

Two more requirements keep their letter and are named because one tool behaves unlike the others under
them: "Tool permission mode of a user" (under `allow` "every call runs without asking") and "Tool calls and
the approval pause" ("a tool that … is pre-approved runs unasked"). An `install_plugin` call does run
unasked there, as they say; what it does is answer that nothing was installed, so an admin in `allow` mode
cannot install a plugin from the chat until the mode is changed.

Everything else is added behaviour:

- **Web client (`../client`)**: the tools reach it through the existing tool-call and approval flow of the
  chat; a client that knows nothing of them shows them as any other tool call. A portal without a configured
  plugin agent, and every member who is not the owner or a DocSpace admin, sees the chat as before.
- **Published SDKs and the OpenAPI document**: one operation is added to the AI chat's document
  (`aichat_2.0.json`), `aiSettingsGetPluginAgent`; no operation is removed or changed, and
  `docspace-backend.yaml` is untouched. The registry route lives outside the API prefix the portal forwards to
  ASC.AI.Chat, like the document agent's file routes, and is not part of the published contract.
- **Webhooks, socket events, audit**: an installation is an ordinary `addWebPluginFromFile` call, so it is
  audited and announced to clients as an upload from the settings page is ("Audit trail of plugin changes",
  "Clients are told when a plugin changes").
- **Backup and restore, portal migration, stored data**: no table, column or stored setting is added. The
  files a task leaves are ordinary workspace files; an installed plugin is covered by "Plugins in backup and
  restore" as before.

No row of `openspec/deviations.md` is touched.

## Impact

- **`common/ASC.AI.Chat`**: a new tools adapter with the two tools, the sub-agent loop, a client of the
  sandbox gateway, the registry route with its disk cache, a new configuration block, the skill as a git
  submodule (and its place in the service's Docker image).
- **Dependencies**: none added to `package.json` by design (see design.md); the skill repository
  `ONLYOFFICE/agent-skills` (Apache-2.0) becomes a submodule.
- **External dependency, not built here**: a sandbox service for plugin work, provided by its owner. This
  change writes the client side only and states what it needs from the sandbox (design.md). With the
  registry route on, ASC.AI.Chat itself needs outbound access to the npm registry. Without the
  configuration block the feature is off and nothing else changes.
- **Security**: a built plugin runs in the browser of every user of the portal, and may bundle any npm
  package. The gates are the admin-only offer, the validator before delivery and the approval before an
  installation; see design.md, Risks.
