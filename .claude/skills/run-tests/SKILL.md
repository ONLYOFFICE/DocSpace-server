---
name: run-tests
description: "Running DocSpace tests: ASC.Tests.slnx, MTP filters for xUnit v3, Aspire fixtures, reading failed-test logs, telling a regression from a flake. USE FOR: run tests, run a specific test, test filter, failing tests, figure out why a test fails. DO NOT USE FOR: writing new tests (tests rule), running the application locally (`dotnet run --project common/ASC.AppHost`)."
---

# Running Tests

## Commands

- Everything: `dotnet test ASC.Tests.slnx`
- Single project: `dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj`
- Filters use **MTP** syntax (Microsoft.Testing.Platform, NOT VSTest) and go after `--`:

```bash
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj --no-build -- --filter-class "*Metadata*"
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj --no-build -- --filter-method "*TestName*"
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj --no-build -- --filter-namespace "*_03_Rooms.Covers*"
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj --no-build -- --filter-trait "Bug=82519"
```

## Infrastructure

- Integration tests boot the **real Aspire AppHost** (`integration-test` launch profile)
  via `Aspire.Hosting.Testing` — not Testcontainers. The first run is slow: MySQL/RabbitMQ/
  Redis/OpenSearch containers have to start.
- Tests are self-contained: do NOT run `aspire start` before running tests — the fixture
  boots its own AppHost instance, and a separately running app would compete for resources.
- A test run binds free ports (`HostPorts` in `common/ASC.AppHost/Configuration/`), not the
  fixed ones of a development stack: the services are not on 50xx and the router is not on
  8092. Each port it picks is reserved for the run (a lock file `<port>.lock` in
  `docspace-test-ports/` under the temp folder on Windows, under `~/.local/share` on Unix, or the
  shared temp folder as `docspace-test-ports-<user>/` for a user without a home folder), so other
  test runs of the same user skip it. So runs from two checkouts on different
  branches, or a run next to a development stack, can go at the same time. The test log shows
  the ports picked when the stack is built ("Host ports of this run (fixed -> used)"); the
  webhook receiver picks its port later, on first use, and is not in that line.
- Storage and logs below are under the base folder: the nearest folder up from the AppHost that holds
  `buildtools/` (`AppPaths.GetBasePath`) — the parent of `server/` for the main checkout, and for a
  worktree under `.claude/worktrees` the same one, unless a `buildtools` link in `.claude/worktrees`
  makes that folder the base.
- Storage is per checkout and suite: `../Data/test/_main/<test assembly>/` for the main
  checkout, `../Data/test/<worktree>/<test assembly>/` for a git worktree (the assembly name in
  lower case), deleted at the end of its run. Logs are per checkout: `../Logs/test/` for the
  main checkout, `../Logs/test/<worktree>/` for a worktree (a worktree name with characters
  other than `[a-z0-9_.-]` gets a hash suffix). The editors keep no named volume in a test run.
- Within one checkout, run suites one after another: two runs of the *same* suite share its
  storage, and the first to finish deletes it; two different suites share the log files, so
  `SocketContractTests` of one also counts the socket refusals of the other.
- This holds for the default, non-docker runs. The docker mode of a test run is switched on by the
  environment variable `Docker=true` of the test process, or, when that is not set, by
  `"Docker": "true"` in the suite's profile in `common/ASC.AppHost/Properties/launchSettings.json`
  (`AspireHostFixture.IsDocker`); the fixture hands the value it decided by to the AppHost. A `Docker`
  variable set on the machine for another tool switches it on too.
  Such a run takes no free ports: the socket, the router, identity and the other unproxied listeners
  keep their constants; the proxied endpoints - MySQL, the forms PostgreSQL and the service
  containers of the docker mode - get ports from the testing builder. It cannot run next to a
  development stack or another test run.
- Every test registers its own portal (via the `Origin` header); the DB is NOT cleaned between
  tests — test classes run in parallel and must not depend on each other's data.

## Logs

**Test runner log** (assertion failures, test output):

- File: `products/ASC.Files/Tests/bin/Debug/net11.0/TestResults/*.log`
- It is **UTF-16LE** — re-encode before grepping: `iconv -f UTF-16LE -t UTF-8 <file>`,
  otherwise grep sees text "with spaces between letters".
- The first ~2600 lines are stack-startup noise ("Couldn't connect", "failed to start
  containers"); look for real failures with `^failed `.

**Service logs** (what the services did during the run): `../Logs/test/` — the test AppHost
writes there, NOT to the regular `../Logs/`; a run from a git worktree writes to
`../Logs/test/<worktree name>/`. Three files per service:

- `<service>.log` — main application log
- `<service>.asp.log` — `Microsoft.*` categories; host-level errors ("BackgroundService
  failed", FATAL on host stop) land ONLY here, not in the main log
- `<service>.sql.log` — EF Core / SQL

## Regression or flake

Compare against a baseline using `git stash push --include-untracked` in the same working
tree, then `git stash pop`. A git worktree at the base commit works too, when it has what a
worktree run needs: `buildtools` found from it, the `common/resources/DocStore` submodule
initialised, and the Files templates built from it (`products/ASC.Files/Server/DocStore`) — without
them file creation fails and gives false reds. Run it at the same time as another run only when its
commit already has the per-run ports, storage and logs (`HostPorts`, `AppPaths.GetTestStorageRoot`
per checkout). An older commit binds the fixed ports, logs to the shared `Logs/test`, and deletes the
whole `Data/test` of its base when it ends, the storage of a run next to it included: run such a
baseline after the other run, not alongside it. If the list of failures matches the baseline
name-by-name, it is a flake, not a regression.

## The AI chat service (vitest)

The Node service `common/ASC.AI.Chat` has unit tests of its own. They are not part of
`dotnet test ASC.Tests.slnx` and need no AppHost, only `yarn install --immutable` once:

```bash
cd common/ASC.AI.Chat
yarn test                                                    # all of them
yarn vitest run app/storage/__tests__/roomSkills.test.ts     # one file
yarn vitest run app/controllers -t "without their headers"   # by test name
yarn check                                                   # typecheck, lint, format:check, tests
```

A test of this service that needs the real .NET services is not a vitest test: it lives in
`products/ASC.AI/ASC.AI.Tests`, whose AppHost starts the service. Which suite proves what:
`.claude/rules/ts-ai-tests.md` §1.
