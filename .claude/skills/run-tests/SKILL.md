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
  8092. Each port it picks is reserved for the run (a lock file under the temp folder,
  `docspace-test-ports/`), so other test runs skip it. So runs from two checkouts on different
  branches, or a run next to a development stack, can go at the same time. The test log shows
  the ports picked when the stack is built ("Host ports of this run (fixed -> used)"); the
  webhook receiver picks its port later, on first use, and is not in that line.
- Storage is per checkout and suite: `../Data/test/_main/<test assembly>/` for the main
  checkout, `../Data/test/<worktree>/<test assembly>/` for a git worktree (the assembly name in
  lower case), deleted at the end of its run. Logs are per checkout: `../Logs/test/` for the
  main checkout, `../Logs/test/<worktree>/` for a worktree (a worktree name with characters
  other than `[a-z0-9_.-]` gets a hash suffix). The editors keep no named volume in a test run.
- Within one checkout, run suites one after another: two runs of the *same* suite share its
  storage, and the first to finish deletes it; two different suites share the log files, so
  `SocketContractTests` of one also counts the socket refusals of the other.
- This holds for the default, non-docker runs (`Docker=false` in the test profiles). With
  `Docker=true` the services and the socket service publish their fixed ports.
- Every test registers its own portal (via the `Origin` header); the DB is NOT cleaned between
  tests — test classes run in parallel and must not depend on each other's data.

## Logs

**Test runner log** (assertion failures, test output):

- File: `products/ASC.Files/Tests/bin/Debug/net10.0/TestResults/*.log`
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
tree, then `git stash pop`. **`git worktree` does not work for this** — the Aspire fixture
fails to start there at all. If the list of failures matches the baseline name-by-name,
it is a flake, not a regression.
