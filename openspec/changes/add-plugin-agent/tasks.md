# Tasks

Tests of this change are vitest tests of the service (`common/ASC.AI.Chat/app/**/__tests__`,
`.claude/rules/ts-ai-tests.md`), with a fake sandbox gateway and a fake upstream registry: the tools and the
registry route live in ASC.AI.Chat only, and the C# integration suites cannot drive a model round. Every
scenario of the delta has a test named in the task that implements it. No requirement of an existing spec is
modified, so no `verification.md` citation needs updating.

## 1. Skill submodule

- [ ] 1.1 Add `ONLYOFFICE/agent-skills` as a git submodule at `common/ASC.AI.Chat/skills/agent-skills`, pinned to a commit that holds `skills/apps-plugin-sdk/SKILL.md`, `scripts/generate-plugin.mjs` and `scripts/validate-plugin.mjs`; verify with `git submodule status` and by listing the three files
- [ ] 1.2 Make the service's Docker image carry the skill (check `.dockerignore`, `COPY . .`) and keep the skill out of `eslint`, `prettier`, `tsc` and the coverage of `vitest`; verify with `yarn check` green and `docker build` of `common/ASC.AI.Chat` showing the three files in the image
- [ ] 1.3 Correct the submodule count and list in `CLAUDE.md` ("Git Workflow") from what `.gitmodules` holds after 1.1; verify the number in the text equals `git submodule status | wc -l`

## 2. Configuration

- [ ] 2.1 Read `ai.pluginAgent` in `config/index.ts` with the values, defaults and bounds of design.md decision 8 and the `AI__PLUGIN_AGENT__*` overrides, returning undefined when the gateway address or token is missing; verify with unit tests for each value: absent, in range, below the minimum, above the maximum, malformed
- [ ] 2.2 Switch the feature off with one warning when the skill directory is missing or has no `SKILL.md`, without stopping the service; verify with a unit test of the start-up check
- [ ] 2.3 Document the block in `common/ASC.AI.Chat/README.md` (values, defaults, what the service expects of the sandbox per design.md decision 9, outbound access to the registry, the submodule in the build context); verify every default in the text against the constants of 2.1

## 3. Sandbox gateway client

- [ ] 3.1 Write the gateway client module: `Create`, `Renew`, `Delete`, `MakeDir`, `Stat` as JSON `POST`, file read and write over `/sandboxes/{id}/files?path=`, bearer token on every call, `withTimeout` on every call, size cap on a read; verify with unit tests against a fake gateway, including 404, 503, a timeout and a body over the cap
- [ ] 3.2 Add `Exec` as a Connect server stream (`application/connect+json`): ordered stdout and stderr chunks, the exit code, an end-of-stream error, cancellation by `AbortSignal`, an output cap; verify with unit tests fed the frames the gateway documents, including a stream cut before the exit event
- [ ] 3.3 Give the module its place in the ESLint rule that keeps `fetch` in the upstream clients, without a new entry in `eslint-suppressions.json`; verify with `yarn lint`
- [ ] 3.4 Run the client against the sandbox its owner provides (address and token from them; this change does not build or start one): create, write a file, exec `node --version`, read the file back, delete; record the commands and their output in the PR description. Needs the provided sandbox: until it exists the task stays unticked and groups 4–6 go on against the fake gateway. If the sandbox differs from design.md decision 9, or the hand-written `Exec` reader disagrees with it, stop and report before group 5 is finished

## 4. Package registry route

- [ ] 4.1 Add the in-memory registry of running tasks (task id, portal, user, registry token, abort handle) with the concurrency limit; verify with unit tests: a place taken and freed, the limit of 2 by default, a token unknown after its task ended
- [ ] 4.2 Add the upstream registry client and the route `/plugin-agent/npm/*` outside the API prefix: constant-time token check (401), `GET`/`HEAD` only (405), a package document with archive links rewritten to this route, an archive mapped back to an upstream path, 404 and 502 relayed as the delta says; verify with tests for the scenarios "Install inside a task", "No token or a wrong one", "Token of an ended task", "Publishing refused", "Unknown package", "Registry does not answer"
- [ ] 4.3 Keep fetched archives on disk under the configured directory with the size cap, oldest out first, written through a temporary file and a rename; verify with tests for "Kept archive", for the eviction at the cap, and that a scoped package name and a name with `..` cannot leave the directory
- [ ] 4.4 Register the route in the Express application following `.claude/rules/ts-ai-express.md` (order, error handler) and confirm `yarn openapi` leaves `app/generated` and `aichat_2.0.json` unchanged, since the route is not part of the published catalogue; verify with `git status` after the command

## 5. Plugin task

- [ ] 5.1 Load the skill: `SKILL.md` for the system prompt, `read_skill` restricted to the skill directory, the list of `scripts/` and `templates/` files to copy; verify with unit tests, including a `read_skill` path that climbs out of the directory
- [ ] 5.2 Implement the sandbox tools `bash`, `read_file`, `write_file` over the client of group 3 with their timeouts and caps, and the task start: sandbox with a fresh session id, the skill files copied through `pooled`, the `.npmrc` with the route address and the task's token, the `Renew` timer, `Delete` in a `finally`; verify with unit tests against the fake gateway: files copied, timer stopped and sandbox deleted on success, on failure and on abort, a failed `Delete` logged and not thrown
- [ ] 5.3 Implement the loop over the `custom` action: tool-call budget, the "nobody can answer" nudge, the finish message when the budget is spent, history compaction, one progress line per tool call without file content or command output; verify with unit tests driven by a scripted model, including the scenarios "Checks never pass", "Time limit", "Reply stopped" and "Steps shown"
- [ ] 5.4 Implement `deliver_plugin` (design.md decision 5): the three checks in order with the first failure returned to the model, the package and the sources archive read from the sandbox, name, version and direct dependencies read, both files saved under the caller's session in the folder `document_agent` would choose; verify with tests for "Plugin built", "Folder the caller cannot write to" and "Sources archive without build output", and that a failed save leaves no half result reported as success
- [ ] 5.5 Implement the `plugin_agent` adapter: offered only with the configuration and a caller who is the owner or a DocSpace admin, off when the role cannot be read; argument checks; the sources archive read under the caller's rights with the 32 MiB cap and the zip check; the busy and unavailable answers; verify with tests for every scenario of "Who is offered the plugin agent", "Arguments of a plugin task", "Continuing from a sources archive", "Concurrent plugin tasks" and "Sandbox cannot be created", each boundary (20000 and 20001 characters, 32 MiB and one byte more, `sourcesFileId` 0, negative, fraction, string) in a test of its own
- [ ] 5.6 Compose the adapter in `aiController.ts` and list its server type among the approval-required ones; verify with a controller test that the tool reaches the model for an admin and pauses for approval, and with "Call denied"

## 6. Installation

- [ ] 6.1 Implement `install_plugin`: argument check before any pause, the file read under the caller's rights, a multipart upload to `api/2.0/settings/webplugins` through the portal API client with the caller's forwarded headers, the answer relayed (name, version, `enabled` on 200; status and message otherwise); verify with tests for "Approved installation", "New version of an installed plugin", "Portal refuses the package", "File id that is not a positive integer" and "File the caller cannot read"
- [ ] 6.2 Prove with a test that neither an `allowAlways` entry for the tool nor a tool permission mode that runs tools unasked skips the pause ("Pre-approval does not apply", "Denied installation"). If the engine cannot hold this for one tool, apply the fallback of design.md decision 7, record the gap for `@onlyoffice/ai-chat` under "Known limitations" of the PR, and re-read the requirement "Installing a built plugin" against what the code then does

## 7. Integration checks

- [ ] 7.1 Run one task end to end on the provided sandbox with a real model (needs the same sandbox as 3.4): describe a plugin, let it build, open the two saved files, approve `install_plugin`, see the plugin in `getWebPlugins`, then a follow-up task from the sources archive and a second installation that replaces the first; record the steps and what was seen in the PR description
- [ ] 7.2 Run `yarn check` in `common/ASC.AI.Chat` and `dotnet build ASC.Web.slnx`; verify both end without errors and without new warnings
- [ ] 7.3 Re-read the texts this change wrote (proposal.md, design.md, the delta, the README section, comments) against the code as it ended up, per `.claude/rules/claims.md`: every number, default and status code has its constant or its test
- [ ] 7.4 Run `openspec validate --specs --no-interactive`, `openspec validate add-plugin-agent --strict`, `python .claude/skills/review-branch/scan.py --worktree` (no requirement of the delta at 500 characters or more) and `python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py --spec ai-agents`. New operationIds of this change: none — the tools are not operations and the registry route is outside the published contract; verify that list is still empty against the routes the change registered

## Workflow follow-up

- Offer the `review-branch` hand-over check when the branch is finished.
- When the change is archived, add the rows of the new requirements to `openspec/specs/ai-agents/verification.md` with the test citations of groups 4–6 (`openspec-maintain`).
- Hand design.md decision 9 ("What the sandbox is expected to give") to the owner of the sandbox and bring their answer back into the design before group 3 is finished.
