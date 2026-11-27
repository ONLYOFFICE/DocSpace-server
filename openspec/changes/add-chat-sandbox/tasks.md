# Tasks

Tests of groups 1–3 are vitest tests of the service (`common/ASC.AI.Chat/app/**/__tests__`,
`.claude/rules/ts-ai-tests.md`), with the fake sandbox gateway the plugin agent's tests share
(`app/__tests__/helpers/sandboxGateway.ts`) and a stubbed `GET people/@self`: the tools live in ASC.AI.Chat
only. The rule of `openspec/config.yaml` that asks for an integration-test task for every new requirement
is answered by group 6, which is not done: the suite of the AI service can script a model's tool call
(`StubModelProvider.ToolCall`), but a round that reaches a configured sandbox needs a stub of the gateway
in its AppHost, which does not exist. What only the real gateway shows is in group 4, not done either.

## 1. Configuration

- [x] 1.1 Read `ai.sandbox` in `config/index.ts` (gateway address, token, idle lifetime 60–3000 seconds, 900 by default) with the `AI__SANDBOX__*` overrides, returning undefined when the address or the token is missing; verify with `app/__tests__/chatSandboxConfig.test.ts`: absent, blank, non-numeric, zero, negative, below the minimum, above the maximum, the environment over the settings and an empty or refused variable leaving them, one warning per refused value, a block that is not an object and an address or a token that is not a text reported (the token not shown, nothing said of an address or a token the environment overrides, said again for a value set right and then wrong; each test reads a fresh module)
- [x] 1.2 Document the block, whose sandbox a call runs in and its lifetime in `common/ASC.AI.Chat/README.md`; verify every default and bound in the text against the constants of 1.1

## 2. The sandbox of a caller

- [x] 2.1 Read the caller's id from `GET people/@self` (`getSelfId` in `app/storage/docspacePeopleApi.ts`), an answer without an id refused as a wrong shape; verify with the `getSelfId` tests of `storage/__tests__/docspacePeopleApi.upstream.test.ts`, its row in `storage/__tests__/refusedBody.test.ts` and its `CONTRACTS` row in `app/__tests__/contract.dotnet.test.ts`
- [x] 2.2 Write the acquisition (`app/sandbox/userSandbox.ts`) per design.md decisions 2 and 3; verify with the tests of `app/sandbox/__tests__/sandboxTools.test.ts` under "the sandbox of a caller" (the four requests of a call and their bodies, a second thread of the caller with the same session and its own directory, no `Delete` after a failed command, no sandbox free, a failed creation, renewal and directory, a sandbox gone at its renewal and under a command, a portal that fails or sends no id, a reply stopped while the portal is asked) and the rows of thread ids that are not directory names, an empty one and one of a single character; one lifetime for a command and for a file call that follows it, a portal that does not answer or answers no JSON

## 3. The tools

- [x] 3.1 Write the adapter (`app/sandbox/sandboxTools.ts`) with `sandbox_exec`, `sandbox_read_file`, `sandbox_write_file`, `sandbox_save_file` and their limits, every argument checked before a sandbox is asked for; verify with the tables of bad arguments per tool, each asserting that no request was made, and a relative path taken under the thread's directory, and the boundary rows (timeout 0 and 601, content one character over, a file one byte over each read limit, a title of 166 characters, and each of those limits itself; a file of one byte; every character Unicode lists as not displayed refused in a title but the two joiners)
- [x] 3.2 Compose the adapter in `aiController.ts` and list its group in `systemServerTypes`; verify with the tests of `aiController.engine.test.ts`, "composes the chat's sandbox tools into the adapter, after the plugin agent's" and "lists the system, custom, integration, document agent, plugin agent and sandbox groups as the ones the asking mode pauses"
- [x] 3.3 Answer every failure as a text for the model: no sandbox free, a sandbox gone under a command, an unreachable gateway, a portal that fails with its status, a caller without an id, a reply that was stopped, and for a save a refusal apart from an unknown answer; verify with the tests of those names in `sandboxTools.test.ts`, and where a saved file goes (the artifacts of a project conversation, the current folder, My documents) with `sandboxTools.upstream.test.ts`, which runs the save over the network stub
- [x] 3.4 Run `yarn check` in `common/ASC.AI.Chat` (typecheck, lint, format, the tests with their coverage thresholds); verified green with 7756 tests

## 4. Against the real gateway

- [ ] 4.1 Run the tools against the gateway its owner provides, with the token of a chat profile: a first call, a second one that renews, a call from a second conversation, a call after the lifetime ended, a `ttl` of 3600 seconds, `MakeDir` of an existing directory, the shell a command is run by, a write into a sandbox that expired, what `MakeDir` answers for a path whose parent is a file. Not done: no gateway was available when groups 1–3 were written; decision 7 of design.md is read from the gateway's source instead. If the running gateway differs from it, `userSandbox.ts` and its tests change first

## 5. Specs

- [x] 5.1 Re-check the touched specs with `openspec validate --specs --no-interactive` (30 passed, 0 failed) and run `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec ai-agents` (163 operations, 16 not called by a test). The change adds no operationId and no route, so none of the 16 is the change's

## 6. Integration tests (`products/ASC.AI/ASC.AI.Tests`)

- [ ] 6.1 "Who is offered the chat sandbox": a round on the suite's installation, which configures no sandbox, is offered none of the four tools. Not done: needs no gateway, only the test
- [ ] 6.2 A stub of the sandbox gateway in the suite's AppHost (`Create`, `Renew`, `MakeDir`, `Exec`, the file routes) and `ai.sandbox` pointed at it. Not done: 6.3 waits for it
- [ ] 6.3 With 6.2, one test per requirement and its boundary scenarios: the tools offered and a call under the default permission mode run without a pending tool call; one session for two threads of a user; the lifetime sent; a failed call answered as a text; a command; a file written and read; a file saved into the folder, among the artifacts of a project thread and into My documents; a deleted thread asking the gateway for no `Delete`. Not done
