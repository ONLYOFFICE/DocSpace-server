# Brief: integration tests for CODE-ONLY rows (reused per spec)

**FIRST RULE: never run python with `-` (stdin) or with a heredoc. Create the .py file with the Write tool, then `python -I -X utf8 <path>`. On Windows with Git Bash it hangs a REPL.**

Write integration tests for the rows of openspec/specs/<NAME>/verification.md whose status is CODE-ONLY, in the ONLYOFFICE DocSpace server repository. The aim is to replace "the code says so" with a test that ran. Close EVERY row that the integration-test stand can reach; do not stop at a round number.

## Token discipline (the user asked for it)

Every tool call re-sends your whole context, so keep it small and the steps few.
- Read .claude/rules/tests.md once.
- C#: codegraph_explore first (name the controller actions and services), then LSP (ToolSearch "select:LSP") for signatures and references. Read a C# file only with offset/limit. Never Grep for a C# identifier under products/, common/, web/; Grep is fine for route literals, status codes and string literals.
- SDK: read the generated method signature under sdk/docspace-api-sdk-csharp with Grep on the operationId and Read offset/limit. Do not guess signatures.
- Existing tests: Grep for the route or SDK operation, Read only the base class and one sibling test as a pattern.
- git: narrow commands only (`git log --format='%h %s' -- <path>`, `git diff -U3 -- <one file>`).
- Write each new test file in one Write; fix with targeted Edits. Keep command output short: grep the run log for `^failed `, `total:`, `failed:`, `succeeded:` only.
- No sub-agents.
- Python: write scripts to a file in your scratch folder and run `python -I -X utf8 <file>`. Never pipe a heredoc into python or use `python -` with `< /dev/null`: on this machine that starts an endless REPL and writes gigabytes.

## Steps

1. `grep -n CODE-ONLY openspec/specs/<NAME>/verification.md`. Sort rows into reachable and unreachable. Unreachable means one of: needs a letter body the suite cannot see, a tariff other than the stand's standalone one (the billing suite web/ASC.Web.Api.Billing.Tests on profile integration-test-billing covers SaaS payment rows: use it when a row needs it), the document server editing session, a browser-set cookie, a configuration value the stand cannot change, or a service outside the stack. List unreachable rows with the reason.
2. Write tests next to the existing tests of that area, following .claude/rules/tests.md: per-test portal, role via Authenticate, ApiException with the status code, raw HTTP only for an endpoint the SDK lacks or cannot express. A matrix test over roles may close several rows. The test name says what the assertion checks.
3. Build only the test project: `dotnet build <test csproj> -c Debug -v q -nologo -p:BuildProjectReferences=false` (the service binaries are current; if a result looks stale, rebuild the service project once). Run: `dotnet test --project <csproj> --no-build --filter-class "*<Class>*"`. Do not start Aspire yourself, do not touch Docker. Run one test class at a time, in the FOREGROUND (timeout up to 10 minutes per call): no background runs, no Monitor, no until-loops or tail -f, since they survive a stop and keep an Aspire stack busy for the next agent.
4. On a failure, first decide whether the test is wrong and fix it. If the server does not do what the row claims: do NOT change product code. If it is a defect, keep the test asserting the correct behaviour and mark it `[Trait("Category", "Bug")]`; if the spec misdescribed behaviour that looks intended, correct the spec row and scenario. Report which and why.
5. Update the rows: `TEST-CONFIRMED [run: PASS]` (or `[run: FAIL-BUG]`) with file:line of the new test. For a row only partly covered, say which part stays CODE-ONLY. Add new defects to the spec's "Suspected defects" with the next free number.

Shared harness (common/Tests/ASC.Tests.Common, each suite's BaseTest, ApiFactories, GlobalUsings) is used by every suite: change it only when a row cannot be reached otherwise, keep the change additive, then build the whole test solution once with `dotnet build ASC.Tests.slnx -c Debug -v q -nologo -p:BuildProjectReferences=false` and name the change in your final message.

You may edit only test files of that area and openspec/specs/<NAME>/spec.md and verification.md. Never edit product code, the TS suite ../tests/api-tests, other specs or openspec/deviations.md. Do not commit. Zero new build warnings (.claude/rules/build-warnings.md); never suppress a warning, use a non-obsolete API instead.

## Finish

`openspec validate <NAME> --type spec --no-interactive`: valid, no warnings. Final message, at most 25 lines: test files (absolute paths) and counts, the run summary lines, rows closed fully / partly / unreachable (with reasons in groups) / reachable but not done (why), red tests and your verdict, new defects with one-line text, what you did not verify.
