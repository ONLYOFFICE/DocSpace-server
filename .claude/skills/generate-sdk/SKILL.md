---
name: generate-sdk
description: "Generating the DocSpace API SDKs: running the generator for the chosen languages, and on request re-emitting the OpenAPI documents first (rebuild of ASC.Api.Documentation.slnx). USE FOR: generate SDK, regenerate SDK, update SDK, regenerate openapi json, api-docs.json, openapi-generator, postman collection, markdown api reference. DO NOT USE FOR: hand-editing generated code in the generated SDK directories, changing controllers/DTOs to fix the spec, writing custom generator templates."
---

# Generating SDKs

The skill does exactly three things, and nothing else:

1. **Delete the `*.json` OpenAPI documents** in the resolved documents directory — only when asked.
2. **Re-emit those documents** by rebuilding `ASC.Api.Documentation.slnx` — only when asked, and
   always together with 1, in that order. Steps 1 + 2 are **stage A**. That rebuild also renders the
   Markdown API reference as a side effect (see stage A) — it is not a separate step you invoke.
3. **Run the generator tool** for the requested languages with the requested parameters — **stage B**.

Step 1 without step 2 leaves no documents to generate from, so the two are never split: the build of
the service projects is what writes the documents back. Stage B alone is fine when the API surface has
not changed. Measured on a warm machine (16 GB, .NET 10 / Maven 3.9 / generator 7.24): stage A
**~4 min plus a full stage-B-sized generator run** — the `GenerateMarkdownApiDocs` target re-enters
the tool, so a Maven build and one generator pass are inside stage A; budget ~5–6 min. Stage B is
**~35–48 s for one language, ~70–95 s for two, ~2 min 30 s for all eleven**. The
per-language cost collapses in a batch because the `mvn clean package` and the joiner run are paid
once per invocation, not once per language. Run both stages in the background rather than blocking
on them.

## Ground rules

- **Never state or assume where anything is written.** Both the OpenAPI documents directory and each
  language's SDK output directory are repo configuration. Resolve them at run time (see below), act
  on what you resolved, and report the resolved paths. If config moves the output, the skill keeps
  working with no edit — so do not hardcode a destination, and do not "fix" a path that disagrees
  with your expectation. The one exception is the package destinations table under
  *Reporting the result*: those are recorded because nothing in the config reveals them, and it says
  how to re-derive them from the command sources when they stop matching.
- **No IDE.** Everything runs through `dotnet`, `mvn` and `openapi-generator-cli`, which behave the
  same on Windows, Linux and macOS. Never fall back to "open the solution and rebuild it". The one
  spot with a known OS wrinkle is stage A's yarn step, flagged there.
- **Launch the tool as `dotnet <dll>`**, never the native apphost — its file name differs per OS.
- Prefer the harness Read/Glob/Grep tools over shell text utilities when inspecting config, so the
  steps do not depend on `grep`/`ls`/`cat` being present.
- **Keep the full log.** These runs are long and their output is the only evidence of what happened,
  so never pipe them through `tail`/`head`/`Select-Object -Last` alone — that discards the log for
  good and buffers the rest until the command exits, leaving the user staring at an empty file. Tee
  to a file and trim only the copy you read yourself, in the form that matches the shell you are in
  — `tee`/`tail` do not exist in PowerShell, and `Tee-Object` does not exist in a POSIX shell:

  ```bash
  # POSIX shell — Linux, macOS, Git Bash on Windows
  <command> 2>&1 | tee <scratchpad>/<stage>.log | tail -40
  ```

  ```powershell
  # PowerShell — any OS
  <command> 2>&1 | Tee-Object -FilePath <scratchpad>/<stage>.log | Select-Object -Last 40
  ```

  Report the log path so the user can follow along with `tail -f <path>` (POSIX) or
  `Get-Content <path> -Wait -Tail 50` (PowerShell).
- **Do not chain commands with `&&`.** It works in bash and PowerShell 7+, but not in Windows
  PowerShell 5.1. Where a command needs a different working directory, set the working directory
  instead of chaining a `cd` in front of it (the working directory persists between calls in both
  shell tools), or issue the two commands as separate calls and check the first one's exit code.
- The only fixed paths are where the tooling itself lives, relative to the repo root:
  `common/Tools/ASC.Api.Documentation/ASC.Api.Documentation.slnx` (solution),
  `.../ASC.Api.Documentation/ASC.Api.Documentation.csproj` (the generator tool),
  `.../ASC.Api.Documentation/SDK/` (Maven project + generator configs).

## Algorithm

1. **Languages** — take them from the user's request. If it does not name any, ask
   (`AskUserQuestion`, multiSelect) out of the commands registered in `Program.cs`: `CSharp`,
   `Python`, `PostmanCollection`, `TypeScript`, `Java`, `Kotlin`, `Php`, `Swift6`, `Go`, `Ruby`,
   `Markdown` — plus an "all" option. Names are case-sensitive. `Markdown` is not an SDK: it renders
   the API reference into `sdk/docspace-api-spec`, and stage A already runs it, so asking for it
   separately is only useful when the documents are current and the reference is not. Pass through
   any extra parameters the user gives.
2. **Always ask whether to regenerate the OpenAPI documents** (`AskUserQuestion`, yes/no) — never
   assume. Yes when controllers/DTOs/Swagger annotations changed since the last regeneration; no
   when only generator configs or templates changed.
3. If yes → stage A, and stop on failure. Then stage B.
4. Report the resolved paths, which documents changed, which languages ran, and the diffs — including
   the indexes outside the SDK submodules that the run dirtied (see *Reporting the result*).

## Resolving the paths (do this before touching anything)

| What | Configured in | How to read it |
| --- | --- | --- |
| OpenAPI documents directory | `OpenApiDocumentsDirectory` in each service csproj | `dotnet msbuild <service csproj> -getProperty:OpenApiDocumentsDirectory -p:GenerateApiDocs=true` |
| Document file name per service | `OpenApiGenerateDocumentsOptions` (`--file-name …`) in the same csproj | same command with `-getProperty:OpenApiGenerateDocumentsOptions`, or Grep the csprojs |
| Documents the joiner consumes, and the joined spec it writes | the tool's `appsettings.json` — `join` section and `pathToFile` | Read the file; entries are path segment arrays, resolved relative to the **current working directory** the tool was started in (`Path.GetFullPath` with no base) |
| The published contract the joiner also writes on **every** run | `publish` in the same `appsettings.json` | Read it. Same segment-array form. Today it lands inside the `sdk/docspace-api-spec` submodule, so every invocation of the tool dirties that submodule — see *Reporting the result* |
| Where the Markdown reference is rendered and bundled | `outputDir` in `SDK/tools/toolsMarkdown.json` (staging) and `markdown.bundle` in `appsettings.json` (the durable output) | Read both. `Markdown` is the exception to the row below: it has no `outputFolder`, so its `outputDir` *is* honoured — but only as staging, which `RemoveStaging` deletes once the bundle is written |
| The tool's build output directory (where stage B must run) | MSBuild, from the tool csproj | `dotnet msbuild <tool csproj> -getProperty:TargetDir` — never spell out `bin/Debug/net10.0`: the configuration varies and the TFM is set centrally in `Directory.Packages.props` |
| SDK output directory per language | `outputFolder` in `SDK/src/main/java/com/example/codegen/My<Lang>ClientCodegen.java`, relative to `SDK/` | Read/Grep that file. The file name is not always the command name — PHP is `MyPHPClientCodegen.java`, not `MyPhpClientCodegen.java`. List the `codegen/` directory instead of guessing |
| Package name/version per language | `SDK/tools/tools<Lang>.json` | Read it. Note `outputDir` there does **not** decide the location — the codegen's `outputFolder` overrides it. `Markdown` is the exception (no codegen `outputFolder`; see the row above) |

`dotnet msbuild -getProperty:` only evaluates, it builds nothing — safe to run first. The services all
point at one shared documents directory; read it from any of them rather than assuming.

## Stage A — re-emit the OpenAPI documents

1. Resolve the documents directory as above, and list it (Glob) to record which documents exist now.
2. Delete the `*.json` in **that resolved directory only**. Deleting first means a project that fails
   to emit shows up as a missing file instead of a silently stale one. Use whichever form matches the
   shell you are in, with the resolved path substituted:

```bash
# POSIX shell — Linux, macOS, Git Bash on Windows
rm -f <resolved documents dir>/*.json
```

```powershell
# PowerShell — any OS
Get-ChildItem <resolved documents dir> -Filter *.json | Remove-Item -Force
```

The PowerShell form is deliberately not `Remove-Item <dir>/*.json`: a wildcard that matches nothing
raises an error there, while `rm -f` stays silent, so an already-empty directory would look like a
failure on one OS and a success on the other.

Do not delete anything under `SDK/json/` — that directory holds a hand-maintained joiner input plus
the joined output, and `appsettings.json` is what says so.

3. Rebuild:

```bash
dotnet build common/Tools/ASC.Api.Documentation/ASC.Api.Documentation.slnx -t:Rebuild -p:GenerateApiDocs=true
```

- `-p:GenerateApiDocs=true` — pass it explicitly, always. It flips `OpenApiGenerateDocuments`
  (`Directory.Build.props`), which every service csproj defaults to `false`. The property group there
  also keys on `SolutionName`/`SolutionFileName`, so building *this* slnx may well set it anyway —
  but that is incidental, it does not hold for building a single service csproj, and relying on it
  (or on the `<Properties Name="MSBuild">` block inside the slnx) means a build that silently emits
  nothing the moment either condition changes.
- `-t:Rebuild` (not a plain build) — forces the document tool to re-run.
- One document is not .NET: the `EmitNewAiOpenApi` target in the tool's csproj runs
  `yarn install --immutable` + `yarn openapi` in the Node service under `common/ASC.NewAi`, writing
  into the same documents directory. That emitter reads the AI service's document from there, so
  `ASC.AI` must build first — the slnx `BuildDependency` guarantees the order. Yarn
  peer-dependency warnings are normal and not a failure.

  That yarn step also rewrites a **tracked** file of its own outside the documents directory —
  `common/ASC.NewAi/app/generated/openapi-schemas.json` — so stage A shows up in
  `git status` in two places, not one. Expected, not a stray edit.
- **The yarn step is the one part not proven OS-neutral.** The tool's csproj builds its working
  directory as `$(MSBuildProjectDirectory)\..\..\..\ASC.NewAi` — backslashes — and hands it to `Exec`.
  Stage A is only verified on Windows. If it fails on Linux/macOS with a path that looks like one
  mashed-together segment, that is the cause, and the fix belongs in the csproj, not here: do not
  work around it by running yarn by hand, because then `newai_2.0.json` is emitted outside the
  build and nothing guarantees `ASC.AI` ran first.
- **The build also renders the Markdown API reference.** The `GenerateMarkdownApiDocs` target in
  the tool's csproj (`AfterTargets="Build"`, gated on the same `OpenApiGenerateDocuments`) runs
  `dotnet <TargetPath> Markdown` from `TargetDir` — that is the tool re-entering itself, so the
  joiner, a full `mvn clean package` and one generator pass all happen inside stage A. Consequences:
  stage A needs the *whole* toolchain, not just Node (JDK, Maven and `openapi-generator-cli` too);
  it takes longer than the .NET build alone; and it writes into the `sdk/docspace-api-spec`
  submodule. The command stages `*.md` and `json/split/` inside the documents directory and deletes
  them again once the bundle is written, so an aborted run can leave those behind — they are staging,
  not output. Do not run `Markdown` again by hand after stage A; it has already run.

4. Verify the directory came back with the same set of documents you recorded in step 1. A missing one
   means that project's build (or yarn) failed: do not continue to stage B — build that project alone
   with the same `-p:GenerateApiDocs=true` and report the failure. Which documents actually matter to
   the SDK is the `join` set in `appsettings.json`; documents outside that set are not part of the
   spec.

## Stage B — generate the SDKs

Run the tool **from its build output directory** — it reads `appsettings.json` from the current
working directory, and every path inside that file is resolved against that same working directory.
Resolve the directory, do not type it out:

```bash
dotnet build common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/ASC.Api.Documentation.csproj
dotnet msbuild common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/ASC.Api.Documentation.csproj -getProperty:TargetDir
# then, with the working directory set to what TargetDir printed:
dotnet ASC.Api.Documentation.dll <Language> [parameters]
```

**Never `dotnet run --project`** for this tool, however much tidier it looks: `dotnet run` sets the
working directory to the project directory, not the output directory, so the tool finds the project's
copy of `appsettings.json` and then resolves every `..`-relative path in it one level too high — the
joiner fails on missing documents, or worse, writes somewhere unintended.

The tool has two modes.

**Interactive** — started with no arguments, it offers "Generate All SDK" or a multi-select. It needs
a real terminal: with stdin not a TTY it dies immediately (verified: exit 127,
`System.NotSupportedException: Cannot show selection prompt since the current terminal isn't
interactive`, no Maven build), so the skill cannot use it. Someone running the tool by hand can.

**By argument** — name one or more languages. The joiner runs once per process before arguments are
even parsed, and `RunCommands` in `Program.cs` pays for one `mvn clean package` per process no matter
how many languages follow, then loops the generator over them **sequentially, stopping at the first
non-zero exit code**. So batch them into one call instead of looping, and on a failure read the log to
see which languages were reached — a batch that dies halfway leaves the later ones untouched:

```bash
dotnet ASC.Api.Documentation.dll TypeScript Java Php
```

Argument validation, all three paths measured:

| Input | Result | Cost |
| --- | --- | --- |
| `Typescript Java` (typo in a batch of two or more) | exit 1, `Unknown SDK: … Available (case-sensitive): …` | instant, no Maven |
| `CSharp Java -c Release` (option with several languages) | exit 1, `Options cannot be combined with several SDKs …` | instant, no Maven |
| `Typescript` (typo, single language) | exit **127**, parser error with a `Did you mean 'TypeScript'?` hint | **the joiner and the whole `mvn clean package` run first** — ~13 s warm, longer cold |

The single-language path skips the pre-check (`requestedSdks.Length > 1` in `Program.cs` guards it),
so a one-language typo is the only spelling mistake that costs a build. Check the name against the
command list above before launching.

`CSharp` additionally accepts `-c|--configuration` (default `Debug`) — the one option any command
takes today. As the table shows, a language that carries an option needs its own invocation, and that
invocation pays its own Maven build. Pass through whatever extra parameters the user gave; do not
invent any.

**Never substitute an underlying tool for the tool itself.** Do not try to save the repeated
`mvn clean package` by calling `openapi-generator-cli` (or `mvn`, or `npm`) directly, however
tempting it looks with several languages queued up. Some commands do more than generate: `CSharp`
then runs `dotnet build` and copies a
`.nupkg`, `TypeScript` then runs `npm install` + `npm pack` and copies a `.tgz`. Which commands have
such steps, and where they write, is the tool's business and changes without notice — calling the
generator yourself silently skips them and produces a half-updated SDK with exit code 0. The
duplicated Maven build is the price of not encoding that knowledge here.

## Prerequisites

`dotnet`, a JDK, `mvn`, `openapi-generator-cli` (`npm i -g @openapitools/openapi-generator-cli`), and
Node + Yarn. **Stage A needs all of them, not just Node** — it shells out to yarn for the AI document
*and* re-enters the tool for the Markdown reference, which is a Maven + generator run. Individual
commands may need more — `TypeScript` also validates `npm` — and each one checks what it needs up front, failing with `Tool '<name>' was not found in PATH` before it
does any work.

Do not verify `openapi-generator-cli` from the repo root: `openapi-generator-cli version` **writes an
`openapitools.json` into the current directory** if there isn't one, leaving an untracked file behind.
Run that check from the `SDK/` directory (which owns a committed `openapitools.json`) or from a temp
directory, and delete the stray file if one appears.

`SDK/scripts/installTools.bat` covers Maven +
openapi-generator-cli on Windows; elsewhere install Maven through the system package manager
(`brew install maven`, `apt install maven`, …) — the npm and dotnet parts are the same everywhere.

## Reporting the result

For each language that ran, resolve its output directory from the codegen config and inspect it there.
**First check that the directory is its own repository root**, then ask for its status:

```bash
git -C <resolved sdk output dir> rev-parse --show-toplevel   # equal to the dir → it is a submodule
git -C <resolved sdk output dir> status --short
```

The check is not ceremony. `git -C` on a directory that is **not** a repo root walks up and answers
for the enclosing repository instead, so `status --short` returns this repo's changes with
`../..`-prefixed paths — a plausible-looking report about the wrong thing. Not every language has a
submodule: `Go` and `Ruby` are generated into `sdk/` directories that no submodule occupies, so
generating them (or `all`) leaves plain untracked directories in this repo, and only the
`rev-parse` check distinguishes that case from a submodule with no changes.

A directory that is a submodule has to be reviewed and committed inside it, after which the submodule
pointer is bumped here.

**The joiner writes two files on every invocation, whatever languages followed** — even a
single-language stage B, even a run that then failed on an unknown option:

| File | Configured as | Where it lands |
| --- | --- | --- |
| the joined spec | `pathToFile` | `SDK/json/api-docs.json`, inside this repo |
| the published contract (YAML, without the generator workarounds) | `publish` | the **`sdk/docspace-api-spec` submodule** |

Report both. The second one is easy to miss precisely because nothing asked for it: `git status` here
shows only a moved submodule pointer, and the actual change is one level down. Stage A adds to the
same submodule through the Markdown bundle (`markdown.bundle`).

**Post-generation steps write packages outside the SDK directory, into places `git status` in this
repo will not show you.** Verified destinations, both of which are *tracked* files that a run leaves
modified:

| Language | Package | Lands in | Whose index |
| --- | --- | --- | --- |
| `CSharp` | `DocSpace.API.SDK.<ver>.nupkg` | `.nuget/packages/` at this repo's root | **this** repo — a generation run dirties a tracked binary here |
| `TypeScript` | `onlyoffice-docspace-api-sdk-<ver>.tgz` | `client/libs/ui-kit/`, a sibling of `server/` | the **client** repo, inside its own `ui-kit` submodule |

So a `CSharp`/`TypeScript` run touches up to **four** indexes: the language's own SDK submodule,
`sdk/docspace-api-spec` (the joiner's published contract — unavoidable, every run), this repo (the
`.nupkg`, plus the submodule pointers), and a neighbouring repository (the `.tgz`). Say so when
reporting. These destinations are the tool's business and can change: re-read them out of the matching `Commands/Generate<Lang>SdkCommand.cs` (`CopyPackages`)
instead of trusting this table if anything looks off.

## Failure modes

- `Duplicate operationId '...'`, `Duplicate path and method`, `Component conflict in ...`,
  `Tag starts with lowercase` — joiner validation (`OpenApi/OpenapiJoiner.cs`). Fix the controller /
  DTO / Swagger attributes and redo stage A; never patch a document by hand.
- `Swagger file not found: ...json` — a document listed in `appsettings.json` → `join` is missing;
  stage A did not produce it.
- `'<opId>' and '<opId>' both publish as <file>.md` — the `Markdown` command (so: during stage A).
  Two operation ids differ only in case or punctuation and would overwrite each other as files. The
  joiner's duplicate check does not catch this; fix the operation ids on the controllers and redo
  stage A.
- Stage A "succeeded" but no document changed — `GenerateApiDocs=true` was missing, or the build was
  incremental instead of `-t:Rebuild`.
- The tool exits with a config or path error, or the joiner reports documents missing that stage A
  clearly produced — it was launched from the wrong working directory (most often via
  `dotnet run --project`); it must run from the `TargetDir` it was built into.
- Sources regenerated but the SDK's package was not refreshed, exit code still 0 — `openapi-generator-cli`
  was called directly instead of the tool, so that language's post-generation steps never ran. Rerun
  the language through the tool.

Known-harmless noise, do not chase it and do not report it as a failure:

- `WARN o.o.c.l.PostmanCollectionCodegen - Error formatting JSON` followed by a stack of
  `com.fasterxml.jackson.core.JsonParseException: Unrecognized token 'body_example'` — the
  `PostmanCollection` generator cannot pretty-print its own example placeholders. It emits ~15 of
  these per run, continues, and exits 0.
- `WARN o.o.codegen.DefaultCodegen - Could not compute datatypeWithEnum from file, null` and
  `No application/json content media type found in response` — generator chatter about form/file
  parameters.
- `Model <name>_request not generated since it's marked as unused ... skipFormModel` — expected: form
  request models are deliberately skipped.
- Grepping logs for `error`/`Exception` produces false positives from generated **file paths**
  (`Client/Exception`, `lib/ApiException.php`). Judge a run by the tool's exit code, not by a grep.
