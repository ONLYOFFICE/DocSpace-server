# Design

## Context

See proposal.md for the motivation. What shapes the approach:

- **The gateway client exists.** `app/pluginAgent/sandboxClient.ts` is the one module that calls the
  sandbox gateway (`gateway.v1.SandboxService`): `Create` (idempotent by a session UUID, with a `ttl`),
  `Renew`, `Delete`, `Exec`, `MakeDir` and file bytes over HTTP. It takes the gateway's address and token as
  a two-field object, so another configuration block can use it unchanged.
- **A plugin task owns its sandbox.** `sandboxSession.ts` creates one per task, renews it on a timer at a
  third of its lifetime and deletes it in the task's `finally`. A conversation has no end to hang a
  `finally` on, and a timer per open conversation would keep sandboxes alive for threads nobody writes in.
- **The service keeps no state across requests** (`ts-ai-failure-paths.md` §8, `ts-ai-express.md` §5): a
  module-level map of thread to sandbox would be per instance, lost at a restart and shared by every portal
  of the process.
- **A tool call is handed the round's thread and signal** through the request context
  (`getRoundThreadId`, `getRoundSignal`), as the plugin agent reads them. The caller's user id is not in it:
  the service forwards the caller's credentials and learns who they are from `GET people/@self`.
- **Agreed with the owner of the gateway (2026-11-27):** the session of a chat sandbox is the user's id, the
  conversations of a user work in `/workspace/{threadId}` of one sandbox, the chat renews it on every tool
  call and never disposes of it.

## Goals / Non-Goals

**Goals:**

- No sandbox outlives its use by more than the idle lifetime and the timeout of its last command, with no
  timer and no background pass.
- No call of one conversation ends the sandbox under a command another conversation of the user runs, once
  that command started within a minute of its renewal.
- At most one chat sandbox per user, however many conversations they have.
- Any instance of ASC.AI.Chat serves any call, with nothing stored about a sandbox.
- The sandbox never holds a credential of the portal.
- An installation without the configuration block behaves exactly as before.

**Non-Goals:**

- Bringing a file of the workspace into the sandbox: the model writes its input with `sandbox_write_file`.
- A sub-agent, a task, a budget of tool calls: the chat's own round limits apply.
- Isolation between the conversations of one user: they are the same person's, and share a machine.
- Removing a deleted thread's directory: it goes with the sandbox.
- Building or changing the gateway, its image or its network policy.

## Decisions

### 1. Tools of the chat's own model

A `ToolsAdapter` with four tools under one group, composed in `aiController.ts` after the plugin agent's.
Each call is one command or one file; the result is text for the model. The arguments are checked before
anything is asked of the gateway or the AI service, so a wrong call costs nothing and does not renew a
sandbox. The group is listed in the engine's `systemServerTypes` and the tools ask for no approval of their
own: only the permission mode that asks about every tool pauses a call, as with `document_agent`.

*Alternative:* a delegating `sandbox_agent(task)` with a loop of its own, as the plugin agent has. Set aside
by the user: a second model's cost for work the chat's model does itself, and the sandbox's state would be
hidden from the conversation.

### 2. The user's sandbox, found by the user's id

Every call that is not refused first (for its arguments or, for a save, for want of a folder) acquires the
sandbox in `app/sandbox/userSandbox.ts`:

1. `GET people/@self` under the caller's session: the caller's id. The read ends with the round's signal.
2. `Create` with that id as the session: the gateway answers the sandbox that runs for it, or starts one.
3. `Renew` it: `Create` answers a running sandbox with the expiry it had, its `ttl` is for a new one only.
4. `MakeDir` of `/workspace/{threadId}`, an existing directory being a success.

Nothing is stored: the gateway's own session is the only record of which sandbox a user has, so two calls
that race end in the sandbox the gateway answers both. A thread id goes into a path and into `cwd`, so it is
taken only as a plain name (letters, digits, `-`, `_`, 100 characters at most; thread ids are UUIDs).

A call cannot tell a sandbox it started from one that ran, so its result does not say; the description of
`sandbox_exec` tells the model that files are gone after the idle time.

*Alternatives:* a sandbox per thread with a record of it in Redis behind three routes of the AI service
(written first, dropped: a sandbox per conversation instead of per user, and a second service to change); a
module-level map (per instance, see Context).

### 3. Lifetime by use, never deleted here

The lifetime asked for and renewed is the idle lifetime plus, for `sandbox_exec`, the command's timeout, so
the idle lifetime counts from where the command may end. A file call renews to the idle lifetime alone, so
the idle lifetime is at least the longest timeout and a minute, 660 seconds. The minute covers what lies
between a command's renewal and its start, the making of its directory (a call of 30 seconds at most): a
renewal by another call then does not land before the end of a command that started within it. Nothing runs between calls, and no code path of the
chat calls `Delete`: the gateway removes a sandbox whose lifetime ended. Deleting a thread is not hooked: the
sandbox belongs to the user and serves their other conversations.

### 4. Paths

A command runs in the thread's directory. A file tool takes an absolute path as it is and a relative one
under the thread's directory; a `..` part is refused in both. An absolute path may name another
conversation's directory: the machine is the one the gateway answers for the caller's user id, whose
conversations share it (Non-Goals). See Known limitations for two portals that hold the same user id.

### 5. Saving a file

`sandbox_save_file` reads the file from the sandbox (32 MiB at most) and saves it with `saveResult` of the
document agent: among the artifacts of a project conversation, else into the current folder when the caller
may create there, else into My documents. The folder is resolved before the sandbox is asked. The save
answers for itself (`ts-ai-failure-paths.md` §6): a 4xx says "not saved", anything else "may have been
saved". The result names where the file went, since the three places are not the model's to guess. An
empty file is refused: the artifacts route answers it 400, the status that also says "this conversation
takes none", and the file would land in a folder. A title takes no path separator, no control character
and no character that turns the direction of the text: the model picks the title, and with U+202E a file
`…fdp.html` is listed as a PDF.

### 6. Configuration

One block, `ai.sandbox`, read in `config/index.ts`; the feature is on when the gateway address and token are
both present.

| Value | Default | Bounds |
|---|---|---|
| gateway address, token | none (feature off) | — |
| idle lifetime, seconds | 900 | 660 – 3000 |

Each has an environment override in the `AI__SANDBOX__*` form. For the address and the token a set variable
wins even when empty, so an install can switch the sandbox off; an idle lifetime that is empty or out of its
bounds in the environment leaves the setting's value, and one out of bounds there leaves 900. A block, an
address or a token of the wrong type reads as absent and is reported once. A block of its own, not the plugin agent's: the token selects the
gateway profile, and a chat sandbox wants another image than the plugin build.

### 7. What the sandbox is expected to give

- **API:** `Create`, `Renew`, `Exec`, `MakeDir` and the file routes, as the plugin agent uses them.
Read from the gateway's source (`document-agent`, `gateway/internal/session/service.go`,
`api/proto/gateway/v1/sandboxes.proto`, commit f7f4509):

- **Session:** a UUID, which a user id is. `Create` looks for a pending or running sandbox of the session
  within the token's profile and answers it unchanged; without one it starts a new one. Concurrent `Create`
  calls of one session share one creation.
- **Profile:** a session is the token's profile's own, so a chat sandbox is never a plugin task's.
- **Lifetime:** a `ttl` of a minute to the gateway's maximum, an hour by default; outside it the call is
  refused. The idle lifetime ends at 3000 seconds so that it and a command's 600 stay within the hour, and
  starts at 660 (decision 3).
  `Renew` of a sandbox that does not run is refused as `FAILED_PRECONDITION`, read here as "gone".
- **Image:** a shell and a writable `/workspace` at least. The description of `sandbox_exec` tells the model
  to expect the gateway's example chat image (python3 with libraries for data and office files) and to
  check, since what is installed is the profile owner's choice.
- **Network:** none. A requirement of the deployment, which the chat does not check (Risks): the tools ask
  for no approval, and the description of `sandbox_exec` tells the model to expect no network.

### Existing behaviour the design relies on

- `ai-agents` — "Tool calls and the approval pause", "Decisions on a tool call", "Tool permission mode of a
  user": the pause under the asking mode is the engine's own. "Saving an artifact of a thread": where a
  saved file of a project conversation goes.

"Breaks current behaviour" lists nothing, so there is no existing data to keep consistent: the change stores
ordinary workspace files only.

## Risks / Trade-offs

- **The model runs arbitrary commands, unasked under the default permission mode** → in a machine that holds
  no credential of the portal and no file but what the model wrote into it. The profile is required to have
  no egress: with it closed, the only way out is a new file saved under the caller's own rights; with it
  open, a text planted in a document the chat reads could have the model send out what it wrote in. The
  chat does not check the profile, so the requirement is the deployment's to keep (README).
- **Every chat user can hold a sandbox** → one per user, gone after the idle lifetime and the timeout of its
  last command; the gateway's "no
  room" is relayed to the model. Nothing here limits the sandboxes of one portal, so the users of one
  portal can take the gateway's room from the others: that limit is the gateway's to set.
- **One more call per tool call** → `GET people/@self` each time; the caller's id is not kept between
  requests.
- **The conversations of a user share a machine** → a command of one can read, change or fill the disk of
  another's directory, and a process left running by one is the other's neighbour. Same person, same rights.

## Known limitations

- **A user id is the session on every portal that uses one gateway profile.** Two portals that hold a user
  of the same id (a copy of a portal) would share a sandbox under the same token.
- **A 404 to a read or a save is read as "no such file"**, also for a sandbox that expired between its
  renewal and the read; the next call starts a new one. A 404 to a write is answered as a sandbox that is
  gone.
- **A command that starts late can be cut.** One the gateway starts more than a minute after its renewal is
  not covered by the lower bound of the idle lifetime (decision 3): a file call of another conversation
  made in that time can move the expiry to before its end.
- **A stopped reply does not stop a save.** The folder lookup and the upload take no signal: a reply stopped
  after the file was read from the sandbox still saves it.
- **A project conversation still needs a folder.** The folder is resolved before the save, so a caller with
  no folder to create in and no My documents is refused although the artifacts would take the file.
- **No test drives a round**: that the default permission mode does not pause a call rests on
  `requireApproval: false` and the library's rule, and that deleting a thread deletes no sandbox on there
  being no call of `Delete` in the chat's code.
- **A deleted thread's directory stays** until the sandbox expires.
- **Not run against a real gateway**: tested against the fake gateway of the plugin agent's tests (task 4.1).

## Migration Plan

1. Ship ASC.AI.Chat with the block absent: nothing changes.
2. Per deployment: add `ai.sandbox` with the gateway's address and the token of its chat profile.
3. Rollback: remove the block. Saved files stay as ordinary files; sandboxes expire.

## Open Questions

- None from the gateway's source. A gateway whose owner sets a maximum lifetime under an hour refuses the
  calls whose lifetime is over it; the idle lifetime of the installation is then set under it by 600.
