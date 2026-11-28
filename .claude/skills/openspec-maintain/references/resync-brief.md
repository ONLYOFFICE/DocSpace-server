# Brief: update existing specs for a merge (reused by several agents)

Update existing OpenSpec specs of the ONLYOFFICE DocSpace server for the merge range <A>..<B> (the release side of the merge; HEAD of this branch may also hold test commits that are not part of the delta). Specs describe the CURRENT server behaviour. Your prompt names the specs you own and the commits that matter; other agents own other specs at the same time, so edit ONLY openspec/specs/<your specs>/spec.md and verification.md. Do not edit openspec/deviations.md (the coordinator merges defects), tests, product code, or translated copies of the specs. Do not build, do not run tests, do not commit, no sub-agents.

## Token discipline (required, the user asked for it)

Every tool call re-sends your whole context, so keep it small and the steps few.
- git: `git log --no-merges --format='%h %s' <A>..<B> -- <paths>`, `git show --stat <hash>`, and `git diff -U3 <A>..<B> -- <one path>` for one file at a time. Never print a whole diff of a folder.
- C#: codegraph_explore first (name the symbols you need), then LSP (ToolSearch "select:LSP") for exact references. Read a C# file only with offset/limit for what they did not return. Never Grep for a C# identifier under products/, common/, web/; Grep is fine for string literals, routes and config keys.
- Specs: do not read them whole. Grep for the requirement or the operation you change, then Read with offset/limit around it.
- Tests: Grep the test folders for route literals, SDK operation names and status codes; Read only around the assertion you cite. A test's name can contradict its assertion.
- Edits: one script or a few Edits per file, never a long series of Edit calls. Keep command output short.

## What to change

- Behaviour that changed in the range: fix the requirement and scenario, add a verification row (claim | source | status). No test was run in this revision: cite a test as TEST-CONFIRMED only when its assertion says it, and put no `[run: …]` mark on rows you add or change.
- New tests in the range that cover an existing CODE-ONLY row: upgrade the row to TEST-CONFIRMED, citing the test by name, `File.cs::Method` (no run mark).
- Defects your specs list: set FIXED (commit) / STILL OPEN / REFUTED where the range touches them, with a one-line reason. New defects get the spec's prefix and the next free number. Exception: a security defect (a caller reads, changes or deletes what their rights do not allow, a secret, key, token or personal data leaks, a check runs for the wrong account or portal, a request reaches an internal address, a payment, licence or second-factor gate is bypassed, an audit event names the wrong actor) is never written into spec.md or verification.md; describe it only in your final message, marked SECURITY.
- Add one revision line under the header of each verification.md you change: "Revision <date> (<A>..<B>)".

Content rule for spec.md: API operations (operationId), routes, request fields and status codes; no class names, file names, enum numbers or configuration keys. Requirement bodies under 500 characters. English only. Keep requirement names stable.

## Finish

`openspec validate <name> --type spec --no-interactive` for each spec you changed: valid, no warnings. Final message, at most 25 lines: per spec what changed, rows upgraded, defect verdicts changed, new defects with one-line text (so the coordinator can add them to deviations.md), what you did not verify.

## Extra for this merge

- Many commits in the range only rewrite endpoint descriptions and the documented status codes (docs(...): "remove documented 500", "correct status codes", "correct repeat statements"). Where a spec or verification row cites a status code or a yaml text that these commits changed, re-check the claim against the code and fix the row; a removed documented 500 may close a defect your spec lists.
- Tests added in the range count as evidence: cite them without a run mark.
- Python: write scripts to a file with the Write tool and run `python -I -X utf8 <path>`; never `python -` or a heredoc into python.
