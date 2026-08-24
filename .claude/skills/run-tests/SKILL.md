---
name: run-tests
description: "Running DocSpace tests: ASC.Tests.slnx, MTP filters for xUnit v3, Aspire fixtures, reading failed-test logs, telling a regression from a flake. USE FOR: run tests, run a specific test, test filter, failing tests, figure out why a test fails. DO NOT USE FOR: writing new tests, running the application (aspire skill)."
---

# Running Tests

## Commands

- Everything: `dotnet test ASC.Tests.slnx`
- Single project: `dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj`
- Filters use **MTP** syntax (Microsoft.Testing.Platform, NOT VSTest) and go after `--`:

```bash
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj --no-build -- --filter-class "*Metadata*"
dotnet test products/ASC.Files/Tests/ASC.Files.Tests.csproj --no-build -- --filter-method "*TestName*"
```

## Infrastructure

- Integration tests boot the **real Aspire AppHost** (`integration-test` launch profile)
  via `Aspire.Hosting.Testing` — not Testcontainers. The first run is slow: MySQL/RabbitMQ/
  Redis/OpenSearch containers have to start.
- Tests are self-contained: do NOT run `aspire start` before running tests — the fixture
  boots its own AppHost instance, and a separately running app would compete for resources.
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
writes there, NOT to the regular `../Logs/`. Three files per service:

- `<service>.log` — main application log
- `<service>.asp.log` — `Microsoft.*` categories; host-level errors ("BackgroundService
  failed", FATAL on host stop) land ONLY here, not in the main log
- `<service>.sql.log` — EF Core / SQL

## Regression or flake

Compare against a baseline using `git stash push --include-untracked` in the same working
tree, then `git stash pop`. **`git worktree` does not work for this** — the Aspire fixture
fails to start there at all. If the list of failures matches the baseline name-by-name,
it is a flake, not a regression.
