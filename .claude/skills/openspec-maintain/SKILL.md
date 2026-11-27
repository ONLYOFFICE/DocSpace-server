---
name: openspec-maintain
description: "Keeping the OpenSpec capability specs (openspec/specs/*/spec.md + verification.md, openspec/deviations.md) true to the current server: re-sync them after a merge brings product changes, cover CODE-ONLY verification rows with integration tests, run the whole test solution and write the run marks onto the rows, find API operations no spec names or no test calls, and keep the rules of openspec/config.yaml that guard new features against the current specs. USE FOR: re-sync the specs after a merge, update the specs for the release, which spec rows are only confirmed by code, add tests for code-only rows, apply the test run to the specs, refresh the run marks, which operations have no spec, which API operations have no test, API test coverage, register a defect found by a spec test. DO NOT USE FOR: OpenSpec change proposals and their workflow (openspec-propose, openspec-apply-change, openspec-archive-change), fixing the defects the specs list (fix-bug), running a suite without touching the specs (run-tests)."
---

# Maintaining the OpenSpec specs

## What lives where

- `openspec/specs/<capability>/spec.md`: requirements (`### Requirement:` with SHALL/MUST, body under 500
  characters) and scenarios (`#### Scenario:` with `- **WHEN**` / `- **THEN**` / `- **AND**`). It names API
  operations (operationId), routes, request fields and status codes only: no class names, file names, enum
  numbers, resource keys or configuration keys. Those belong in verification.md.
- `openspec/specs/<capability>/verification.md`: one row per claim, `claim | source | status`.
- `openspec/deviations.md`: the defect register, coordinator-owned. Every suspected defect from any spec, except a
  security defect (next section), is listed there with its spec prefix number, evidence and status (OPEN, OPEN (product decision), FIXED <commit>,
  REFUTED). Rows that leave OPEN move to the "Closed" table.

**Specs describe the current server.** When the code and an older document (role matrix, yaml text, release
notes, a TS test) disagree, the spec follows the server. Behaviour that looks intended goes into the spec;
behaviour that is clearly wrong stays out of the requirements and goes to "Suspected defects" plus deviations.md.
A test that asserts the correct behaviour of an open defect carries `[Trait("Category", "Bug")]` and stays red.

### Security defects

Security defects stay out of the repository until a released version fixes them. The repository is public
(GitHub mirror), so a security defect (a caller reads, changes or deletes what their rights do not allow, a secret, key, token or personal data leaks, a check runs for the wrong account or portal, a request reaches an internal address, a payment, licence or second-factor gate is bypassed, an audit event names the wrong actor) is described in none of deviations.md, spec.md or
verification.md. The briefs make agents report such a defect only in their final message, marked SECURITY;
the coordinator records it in the private register outside the repository. Its Bug-marked test depends on
whether the hole is public already: a test that is already in the repository (its history is public) stays in
the test project with its `Category=Bug` trait, because it is the check that the fix works and nothing is gained
by hiding it; a test for a hole nobody has published yet stays uncommitted, kept with the register entry, until
the release that ships the fix. After that release the defect may enter the repository like any fixed defect,
with the advisory or changelog entry the user names. The same goes for a security fix that is
merged but not released: the Closed table does not list it until the release.
The register's path is the environment variable `OPENSPEC_SECURITY_REGISTER`, set per developer in the ignored
`.claude/settings.local.json` (`"env": {"OPENSPEC_SECURITY_REGISTER": "<path>"}`). When it is unset, ask the
user for the path and never fall back to a file inside the repository. At the end of every mode, collect the
SECURITY items of the agents' reports, check each in the code, append it to the register (id, class, route,
evidence, test code) and name the new ids to the user, so that the user files them in the bug tracker.

### verification.md statuses

| Status | Meaning |
|---|---|
| TEST-CONFIRMED | an integration test asserts the claim, cited by name as `File.cs::Method` (several: `File.cs::A, B`). Older rows cite `File.cs:line`; a row you touch is rewritten to the name form, since a line citation moves with every edit above it (one review found 121 of 378 stale) |
| CODE-ONLY | read from code, configuration or the OpenAPI document; no test. Say why when it is unreachable |
| LIVE-CONFIRMED | checked by hand against a running stand |
| MATRIX-ONLY / NOTES-ONLY / TS-ONLY | only the role matrix, the release notes or a TS test says it |

After TEST-CONFIRMED a script writes `[run: PASS|PASS-ON-RERUN|FAIL-BUG|FAIL|SKIP|NOT-IN-RUN|NO-LINK]` from the
last full run (mode C). Rows added or changed by hand carry no run mark until the next run.

`openspec validate --specs --no-interactive` must report every spec valid with no warnings after any edit.

## Mode A: re-sync after a merge

1. Find the product delta. The branch is merged with the release, so take the release side of the two merges:
   `A=$(git rev-parse <previous merge>^2)`, `B=$(git rev-parse <new merge>^2)`, then
   `git log --no-merges --format='%s' $A..$B | sort | uniq -c | sort -rn`. Commits that only rewrite endpoint
   descriptions still matter when they change a documented status code or a yaml text a row cites.
2. Group the affected specs by area (2 to 7 specs per agent) and name the commits that matter for each group.
3. Fill `references/resync-brief.md` (range, date) and start one general-purpose agent per group, at most two at a
   time: these agents run no tests, but parallel batches still drain the 5-hour usage window fast.
4. When they report: check every claimed new defect in the code yourself (codegraph/LSP), then add it to
   deviations.md; apply FIXED/REFUTED verdicts there too. Agents never edit deviations.md. A defect marked
   SECURITY goes to the private register instead (see "Security defects" above), never to deviations.md.
5. `python -I -X utf8 .claude/skills/openspec-maintain/scripts/op_coverage.py` lists operations of the published
   yaml that no spec.md names. Each one goes into an existing spec (an agent with the resync brief) or a new
   capability (`references/new-spec-brief.md`).
6. `openspec validate --specs --no-interactive`.

## Mode B: cover CODE-ONLY rows with tests

1. Count: `grep -c CODE-ONLY openspec/specs/*/verification.md`.
2. One general-purpose agent per spec with `references/tests-brief.md`, strictly ONE AT A TIME: each test run
   boots an Aspire stack, and two stacks compete for ports, Docker and memory.
3. Read each report critically: a red test marked Bug is a defect claim. Confirm it in the code before it
   goes to deviations.md. A spec "correction" must describe behaviour that looks intended, not a defect.
4. After the last agent, build the whole test solution once to check the shared harness:
   `dotnet build ASC.Tests.slnx -c Debug -v q -nologo -p:BuildProjectReferences=false`; new warnings in touched
   files are not allowed (`.claude/rules/build-warnings.md`).

Unreachable on the `integration-test` stand (state the reason in the row, do not fake it): letter bodies, the
document server's editing/saving/conversion, SMS, LDAP, CAPTCHA, provider sign-in, a tariff other than standalone
(the billing suite on `integration-test-billing` covers SaaS payment rows), an Enterprise licence, https. A
Nextcloud container IS on the stand (`Requires=Nextcloud` tests, see `03_Rooms/ThirdParty/ThirdPartyTestBase`).

## Mode C: full run and run marks

1. Build the services, not only the tests: a test-only build leaves the Aspire services on old product code.
2. `bash .claude/skills/openspec-maintain/scripts/run_full.sh <out-dir>` runs `ASC.Tests.slnx` one module at a
   time with TRX reports into `<out-dir>/results`. It takes hours; run it in the background and wait for it.
3. Re-run every failed test class alone (`dotnet test --project <csproj> --no-build --filter-class "*<Class>*"
   --report-xunit-trx --results-directory <out-dir>/rerun/<Class>`): a pass alone becomes PASS-ON-RERUN.
4. `python -I -X utf8 .claude/skills/openspec-maintain/scripts/apply_run_marks.py <out-dir> [--ru <dir>]`
   rewrites the marks and the run legend of every verification.md (and of translated copies with the same row
   count, if `--ru` names their root). It is idempotent.
5. Judge every FAIL row: a test fault, a flake, or a defect. A defect gets a Bug-marked test and a deviations.md row;
   a security defect gets no row: it goes to the private register, and its test, already in the repository,
   stays where it is (see "Security defects" above).

## Mode D: which operations the tests call

`python -I -X utf8 .claude/skills/openspec-maintain/scripts/api_test_coverage.py [--out <file.md>] [--spec <name>]`
maps every operation of the published yaml to the test projects of `ASC.Tests.slnx`: `sdk` (a test calls
`.<OperationId>Async(` or `.<OperationId>WithHttpInfoAsync(`), `raw` (only a route literal matches; the HTTP
method is not checked, so read the test), `none`. It prints the counts per spec and, with `--out`, lists every
`raw` and `none` operation per spec with the files that hit it. It is a text scan: a same-named helper called
with a receiver counts as a call, and an operation not yet in the published yaml (one a change adds before the
documents are regenerated) is invisible to it. Use the `none` list to queue mode B work (an operation no test calls has no
TEST-CONFIRMED row behind it either) and run it after every batch. This is the "every operation is called"
level; the "every row is TEST-CONFIRMED" level is the CODE-ONLY count of mode B.

## New features against the specs

The OpenSpec change workflow (`openspec-propose`, `openspec-apply-change`, `openspec-verify-change`,
`openspec-archive-change`) reads `openspec/config.yaml`. Its `context` tells the agent how to find the governing
requirements by operationId or route, and its `rules` make every proposal list the touched capabilities and a
"Breaks current behaviour" section, make every delta quote the requirements it modifies or removes, state
boundaries for new numeric, length and list inputs, and make the tasks update the tests that cite a modified
requirement and end with mode D for the touched specs. Keep those rules in step with this skill when either
changes.

## Traps

- Never run `python -` or pipe a heredoc into python: on Windows with Git Bash it starts an endless REPL. Write
  the script to a file and run `python -I -X utf8 <file>`. Every agent brief repeats this as its first line.
- TaskStop on an agent does not stop the Git Bash scripts it started; check for leftover `dotnet test` and
  `bash.exe` processes afterwards.
- Document-server tests no longer need port 8092 free: a test run publishes its router on a free port (`HostPorts`; not with `Docker=true`, which keeps the fixed ports), next to an installed DocSpace or a dev stand.
- Licence tests must not run on a stand whose configuration sets a licence file path.
- A ported TS expectation is a claim, not the spec: check it against resources and enums before calling a defect.
