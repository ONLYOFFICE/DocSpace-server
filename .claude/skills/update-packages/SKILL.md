---
name: update-packages
description: "Updating the server's NuGet packages in two phases. Phase 1 (read-only, the default): list what is outdated in Directory.Packages.props, collect release notes from nuget/GitHub, work out what breaks or changes behaviour in OUR code and in the runtime configs outside the repo, which new features we could apply and where, and whether the licenses still fit AGPL-3.0-only - then STOP with a report so the user decides what to update. Phase 2 (only on the user's go): bump the chosen packages, adapt code/configs, build, test, verify at runtime. USE FOR: which packages are outdated, what changed in package X, is the new major of X safe, what can we use from the new version, check licenses of packages, update packages X/Y/Z - in any language the user writes in. DO NOT USE FOR: npm/yarn packages of the client or of ASC.AI.Chat, Maven dependencies of ASC.Identity, regenerating the SDKs (generate-sdk), DB migrations after an EF Core bump (db-migrations)."
---

# Updating packages

Two phases, and **phase 1 never changes the repository**:

1. **Assessment (read-only).** What is outdated, what changed, what it means for our code and our runtime
   configs, what we could apply, licenses. Ends with a report and a question — the user decides which
   packages to take. A MAJOR bump is assessed in full before anyone touches a pin: that is where
   behaviour changes are likeliest.
2. **Update (only after the user chose).** Bump, adapt code and configs, build, test, verify at runtime,
   report. "Update X" still starts with phase 1 for X unless the user says to skip it, and a MAJOR found
   on the way is reported before it is applied.

Everything is verified against our code; a changelog line is a claim, not a finding.

**Stable package knowledge lives in `references/packages.md`** — per family: where the real release notes
are, package-specific verification, traps of our setup, and blockers an earlier assessment found. Read only
the sections of the packages at hand. It is not a run log: change it only when a run learned something new
and non-obvious (steps 1.7 / 2.7).

All pins live in `Directory.Packages.props`; the one exception is `Aspire.AppHost.Sdk`, pinned in the
`Sdk="..."` attribute of `common/ASC.AppHost/ASC.AppHost.csproj`.

## Ground rules

- **Never commit.** The user commits himself, often mid-session — check `git log`/`git status` before
  assuming your edits are still uncommitted, and before reverting anything.
- The working tree may already hold the user's own changes. Touch only what you were asked about; never
  "normalise" the rest of a file (it may lack a trailing newline on purpose). The same holds for
  `../buildtools`, which is a separate repository with its own uncommitted work.
- Write the report in the user's language. Files under `.claude/` stay English.
- Run the scripts with `python -I` from the repo root. `-I` ignores `PYTHON*` env vars, so the scripts
  set UTF-8 stdout themselves — without that, a release note with an emoji crashes on the cp1251
  console. Temporary files (plans, release notes, smoke apps, config copies) go to the scratchpad.
- `rm -f $VAR/*.json` is refused by the safety check (an unset var could become `/`); use a literal path.
- C# navigation follows `.claude/rules/csharp-lsp.md`: LSP / codegraph for symbols. Grep is fine for
  package ids in `.csproj`, `using` lines, config keys and string literals.

# Phase 1 — assessment (read-only)

### 1.1 List what is outdated

```bash
python -I .claude/skills/update-packages/outdated.py                          # everything
python -I .claude/skills/update-packages/outdated.py "Family*" Other --json <scratchpad>/plan.json
```

It prints current → target with the bump kind (MAJOR/minor/patch/revision). A stable pin targets the
newest stable; a prerelease pin targets the newest release of any kind, so it does not strand on an old
beta. `--json` writes the plan the next steps read. If the user did not name packages, show the table
and ask which to assess (AskUserQuestion, multiSelect, families as options). Read the XML comment above
a pin before planning it — some pins are deliberate. Assess a family together (a package and the
siblings released with it).

### 1.2 Dependencies, licenses, dependent pins — before reading any changelog

```bash
python -I .claude/skills/update-packages/licenses.py --plan <scratchpad>/plan.json
```

From the nuspecs alone, without touching a pin, it shows:
- **license** old vs new of every planned package, and of new / moved dependencies;
- **CHANGED TRANSITIVE** — dependencies we do not pin, at the version NuGet will resolve: the nuspec
  minimum, not the newest. A MAJOR move there is an update of its own — read its release notes too, and
  say in the report which version will resolve and whether pinning it higher is worth it;
- **OTHER PINS BUILT AGAINST AN OLDER MAJOR** — our pins that depend on a planned package through an
  open-ended range. NuGet accepts them; whether they still work is for 1.5 to prove. Check whether newer
  versions of them exist.

Judge licenses against **AGPL-3.0-only**:

| Verdict | Licenses |
|---|---|
| OK | MIT, Apache-2.0, BSD-2/3-Clause, ISC, Zlib, PostgreSQL, MPL-2.0, LGPL-2.1+/3.0, GPL-3.0(-or-later), AGPL-3.0 |
| Read before accepting | `type=file` or a bare `licenseUrl` (read the text), "custom"/EULA, MS-PL and EPL (not GPL-compatible), CC-BY-SA |
| Blocker | GPL-2.0-only, SSPL, BUSL/Business Source, Commons Clause, any non-commercial or proprietary license, a license that changed to one of these between versions |

Dependencies are collected over every target-framework group of a nuspec, so a .NET Framework-only group
shows up although net10 never resolves it — say so instead of flagging it. A package with native code
bundles its own third-party libraries; check its release notes for a newly bundled one. The .NET server
has no third-party notices file (only `common/ASC.Identity/3rd-Party.license`, Java).

### 1.3 Release notes

```bash
python -I .claude/skills/update-packages/release_notes.py PACKAGE FROM TO [--repo O/N] [--tag-prefix P] [--changelog PATH] --out <scratchpad>/rn/X.md
```

The repo is read from the nuspec. Monorepos tag per component (`--tag-prefix`) or keep one CHANGELOG per
package (`--changelog`); some projects publish the real notes elsewhere. `references/packages.md` records
the layouts that are not the nuspec repo's GitHub releases — check it first.

Unauthenticated GitHub API: 60 requests/hour, `gh` is not installed. For a MAJOR read everything from the
old version up, breaking sections first, plus the project's migration page if there is one. For minors
skim for "breaking", "behavior", "removed", "obsolete" and security advisories.

### 1.4 Check every claim against our code

Read the family's "Traps" and "Blockers" in `references/packages.md` first — they name what a search will
not find (configs outside the repo, deliberate pins). Then, for each breaking or behavioural item: do we use it? (LSP `workspaceSymbol`/`findReferences`, codegraph for
call paths, grep for `using X` and `PackageReference`.) Write "used at file:line" or "not used".

- **Enums reordered / members renamed** — harmless unless the numeric value is persisted (DB, cache, JSON).
- **Nullable annotations added** — new `CS86xx` warnings appear only in projects with
  `<Nullable>enable</Nullable>`, and the repo allows zero new warnings: name them as work.
- **Our workarounds** — an upstream fix can make one of our workaround classes dead code, and a changed
  output shape can break a class that keys on it. Search for code that compensates for the library.
- **A fix for "our database / our platform"** may still not reach us — confirm the code path is the one
  we use (a provider or platform check in the library can exclude us).
- **New features** — name the place in our code where each would apply, or why it does not. Read the
  library source on GitHub when the changelog is ambiguous.

### 1.5 Runtime configs outside the repo — smoke them, do not assume

The effective configs live in `../buildtools/config/` (`nlog.config`, `autofac*.json`, `appsettings*.json`,
`redis.json`, ...), not in this repo. Build and code search never see them, so a package that reads a
config can break there with a green build. For every planned package that parses one of them:

1. Copy the config into the scratchpad (never edit `../buildtools` in phase 1).
2. Write a file-based app next to it that loads it with the **new** versions the same way our startup
   does, with every extension package we reference at its pinned version, and that exercises what the
   config wires up (e.g. writes an event to each target the rules use):

   ```csharp
   #:package Some.Library@<new version>
   #:package Some.Library.Extension@<our pinned version>
   ```

   Run with `dotnet run smoke.cs -- <config>`. Use absolute paths for anything the config writes —
   relative ones resolve against the app's base directory, not the current one.
3. Run it against the config **as is** first — an exception there is the finding.
4. Patch the scratchpad copy minimally until it loads and works, and keep the patch as the proposal. Note
   its cost: a patch that needs a new package may stop the config loading on the old version, and
   `buildtools` is shared by every server branch — then the config change and the package update must
   ship together.
5. Note behaviour that only shows over time (file rolling, archive naming, compression) and how phase 2
   will check it.

The same smoke approach covers libraries without tests: run the exact code of our call site in a
file-based app with the new version.

### 1.6 Assessment report — then stop

Per package or family, most important first:
1. **Breaks** — in code (file:line) and in configs (the smoke result), with the proposed fix.
2. **Behaviour changes that reach us** — and, one line each, those that do not and why.
3. **Transitive and dependent packages** — which unpinned version will resolve; which of our pins are
   built against an older major and whether newer versions exist.
4. **Security fixes** — a reason to take the update.
5. **New features we can apply** — the concrete place.
6. **Licenses** — changed? compatible?
7. **Recommendation** — take now / take with the listed changes / wait, and the decisions the user has
   to make.

Then ask which packages to update and which decisions to take (AskUserQuestion). Do not bump anything
in this turn.

### 1.7 Record what was learned

Change `references/packages.md` only when the run found something new and non-obvious — the one file
phase 1 may change:
- a release-notes location that is not the nuspec repo's GitHub releases;
- a package-specific verification step or test mapping that was needed;
- a trap of our setup (config outside the repo, deliberate pin, a code path the library skips for us);
- a **blocker** for an update the user did not take: what must change first, undated, without test results.

Never add logs ("assessed X → Y", "taken, tests green", dates) or lists of classes using the package. A run
where everything was found the usual way leaves the file untouched.

# Phase 2 — update (after the user chose)

### 2.1 Bump

Edit the `Version` attributes of the chosen packages (whole families). A new package gets a
`PackageVersion` and a `PackageReference` next to its siblings. Pin an unpinned transitive dependency only
if the user chose to. Apply the agreed config patches to `../buildtools/config/` and say so — it is
another repository.

### 2.2 Build

```bash
dotnet build ASC.Web.slnx            # must stay at 0 warnings (.claude/rules/build-warnings.md)
dotnet build ASC.Tests.slnx          # test projects have pre-existing warnings: compare, do not chase
```

Fix new warnings in the code they point to, then
`dotnet format style <csproj> --include <files> --verify-no-changes`.

### 2.3 Package-specific verification

Run the "Verification" steps of the family in `references/packages.md` (generated-output diffs, strict
build flags, ...). Report which version each unpinned transitive dependency actually resolved to
(`obj/project.assets.json` → `libraries`).

### 2.4 Tests

Run the tests that cover the package — the family's "Verification" in `references/packages.md` names them
where it is not obvious (run-tests skill for syntax). Rebuild the services
first — a test-only build runs the Aspire services on stale product code. A package every service loads
at start-up (logging, DI, telemetry, caching, OpenAPI) is proven by any integration run plus the runtime
checks in 2.5. Several `--filter-namespace` options are OR-ed; `--filter-namespace` together with
`--filter-class` is AND-ed ("Zero tests ran") — use namespace filters only.

### 2.5 Runtime verification

- **Service logs** — the test AppHost writes to `../Logs/test/`: every service log should carry fresh
  timestamps; compare formats and file rolling with the previous run when the update touches logging.
- **Telemetry and cache packages**:

  ```bash
  OTEL_FILE_LOGGING=true dotnet test products/ASC.People/Tests/ASC.People.Tests.csproj --no-build -- --filter-namespace "*_06_Photos*"
  python -I .claude/skills/update-packages/otel_summary.py
  ```

  The otel-collector writes `../Logs/otel/traces.log` and `metrics.log`; the summary lists every service,
  `telemetry.sdk.version`, resource attributes (one row per service = no duplicate instances) and each
  instrumentation scope with its version — proof the new packages are the ones running. Delete the old
  logs before a comparison run.

### 2.6 Final report

The phase 1 report, updated with what was done and measured: bumped pins, code and config changes (the
other-repo ones called out), build warnings, test counts, runtime findings, and the follow-ups offered
but not applied.

### 2.7 Record what was learned

As in 1.7, plus: once an update is taken, delete the family's **Blockers** entry for it, and anything the
new version made obsolete.

### Rolling back

If the user drops the update: `git checkout --` the pins, csproj and code files you changed (only those),
`git -C ../buildtools checkout -- <config>` for the configs you patched, then **rebuild `ASC.Web.slnx` and
`ASC.Tests.slnx`** — otherwise `bin/` keeps the new packages, and a service started with `--no-build`
meets the old config. Confirm in `obj/project.assets.json` that the old versions resolve again.

## Traps

- `DocSpace.API.SDK` is our own SDK (built into `.nuget/packages` by the generate-sdk skill and also
  published on nuget.org). Never bump it here, even when `outdated.py` lists it.
- The dev stand and the integration tests both bind 443: stop the dev AppHost with
  `aspire stop --non-interactive`; never kill the `dotnet run` task — that orphans every session container
  (openresty keeps 443/8092) and the user has to `docker rm -f` them. Check `docker ps` before a test run.
- Generated files rewritten by a documentation or SDK build (`json/*.json`, `openapi-schemas.json`) must be
  restored unless the user asked to regenerate.
