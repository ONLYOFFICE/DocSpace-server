# Brief for writing a new OpenSpec capability (reused for every topic)

Write the OpenSpec capability spec named in your prompt for the ONLYOFFICE DocSpace server. It describes the CURRENT behaviour of the server as the integration tests and the code show it.

Files you may create or edit — ONLY openspec/specs/<NAME>/spec.md and openspec/specs/<NAME>/verification.md. Do not build, run tests or start services. Do not commit (the user commits, possibly while you work). Do not start sub-agents. Keep scratch files in a temporary folder outside the repository.

## Scope comes from data

The output of `.claude/skills/openspec-maintain/scripts/op_coverage.py` lists, per OpenAPI tag, the operations (operationId, method, route) that no spec names yet. Your prompt names the tags you own. Every operation of those tags must end up either in a requirement or in "Left out" with a reason. Report any you leave out.

## Token budget is tight

Every tool call re-sends your whole context, so keep the context small and the steps few.
- Format: read only the first 60 lines of openspec/specs/billing/spec.md and of openspec/specs/billing/verification.md. Do not read other specs whole; Grep them for a term when you need to avoid overlap.
- Tests first. Build yourself a one-file index with a small script (name, file:line, SDK call, expected status, Bug trait) over the test folders in your prompt, read that, then Read with offset/limit only around the assertions you cite. A test's name can contradict its assertion, so read the assertion before marking a row TEST-CONFIRMED. If you cite by index only, mark the row "by name" and say so in the header. Test conventions: .claude/rules/tests.md.
- Contract: Grep sdk/docspace-api-spec/docspace-backend.yaml for the operationId and read only that slice.
- Code last, only for open points: codegraph_explore first, or LSP (ToolSearch "select:LSP"); Read a C# file only for what they did not return. Never Grep for a C# identifier under products/, common/, web/.
- Write each file with one Write, or one script. No long series of Edit calls. Keep command output short. Do not re-read what you already have.

## Format

spec.md: `# <NAME> Specification`, `## Purpose`, `## Requirements`, `### Requirement: <name>` with SHALL/MUST text under 500 characters, each with `#### Scenario: <name>` of `- **WHEN** …` / `- **THEN** …` (optional `- **AND** …`). English only. One behaviour per requirement. HTTP statuses where tests assert them. Nothing "planned". Names API operations (operationId), routes, request fields and status codes; NO class names, file names, resource keys, enum numbers or configuration keys (those go to verification.md). Clear defects are not requirements.

verification.md: rows claim | source (test file:line / yaml operation / code file:line) | status (TEST-CONFIRMED, CODE-ONLY); then a table of differences between the yaml and the server, "Left out" with reasons, and "Suspected defects" with the prefix in your prompt: # | where | what the server does | evidence | evidence kind (red test by reading, red by code reading, per test comment). You run no tests, so never claim one passed or failed.

## Finish

`openspec validate <NAME> --type spec --no-interactive`: valid, zero warnings. Final message: requirement names, scenario count, status counts, operations left out and why, surprising facts, suspected defects, what you did not verify.
