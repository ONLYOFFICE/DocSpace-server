---
name: generate-webhook-sdk
description: "Generating the DocSpace webhook SDKs (webhooks-sdk/): checking that the hand-written contract docspace-webhooks.yaml still matches the webhook payload DTOs and triggers, running openapi-generator-cli per language from the skill's targets.conf with its post-steps, building each language and replaying the signed fixtures through its sample, and optionally checking that each package still builds and installs. USE FOR: generate webhook SDK, regenerate webhook models, update webhooks-sdk, a webhook payload DTO or trigger changed and the SDKs must follow, NOT VERIFIED languages, webhook SDK packaging check, how to regenerate the webhook SDKs by hand. DO NOT USE FOR: the REST API SDKs in sdk/ (generate-sdk), hand-editing anything under webhooks-sdk/<lang>/generated/, changing payload DTOs or WebhookTrigger itself."
---

# Generating the webhook SDKs

This is the inbound counterpart of `generate-sdk` and shares none of its tooling. The webhook SDKs
are built by calling `openapi-generator-cli` directly, one language at a time; the
`ASC.Api.Documentation` tool is deliberately not involved (its joiner rewrites the REST contract on
every run and its output folders point at the API SDK submodules — `webhooks-sdk/README.md`,
*Why not the generate-sdk tool*). Never route a webhook generation through `generate-sdk`, and never
add webhook commands to that tool as a side effect.

The skill does three things:

1. **Check the contract** against the code it describes — always.
2. **Generate and verify** the requested languages, step by step, with no shell script — stage A.
3. **Report** what changed and how each language verified; on request, **check the packages** —
   stage B.

## Ground rules

- **The contract is hand-written and nothing checks it.** `common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml`
  is not emitted by a build. `WebhookPayloadPairingTests` checks trigger ↔ payload kind ↔ DTO in C#
  only; it never reads the YAML. So a DTO change that was not mirrored in the contract regenerates
  nine SDKs that are confidently wrong. Step 1 below exists for that reason — do not skip it because
  the user only asked to "regenerate".
- **No script, no particular shell.** There is no generation script: this skill *is* the procedure,
  and its steps go through `openapi-generator-cli`, `python` and each language's own toolchain, which
  behave the same in bash and PowerShell on every OS. Where a step is a file operation that differs
  between shells, both forms are given; use the one for the shell you are in. Do not write a script
  to wrap the steps — a person regenerating by hand follows this file the same way.
- **Do not chain commands with `&&` or prefix them with `cd`.** It fails in Windows PowerShell 5.1.
  Set the working directory once (it persists between calls in both shell tools) and use each
  tool's own directory flag (`go -C`, `mvn -f`, `npm --prefix`, `swift --package-path`,
  `dotnet --project`) for the commands that need another one.
- **Never edit `<lang>/generated/`.** It is wiped on every run. A fix belongs in the contract, in
  `targets.conf` (generator flags), in `templates/<lang>/`, in a shim (`go/shim`, `java/shim`,
  `kotlin/docs`), or in the hand-written runtime `<lang>/src/`.
- **`generated/` is committed** (see `webhooks-sdk/.gitignore`): its diff is part of the change and
  goes into the same commit as the contract change that caused it. Only `.generate.log` and
  `.verify.log` are ignored.
- **No hardcoded language list or flags.** The targets are the lines of
  [`targets.conf`](targets.conf), next to this file; read it before offering choices or running
  anything. The first
  field of a line is the language name (lowercase directory names — `csharp`, `typescript`, … —
  unlike `generate-sdk`'s command names).
- **Keep the evidence.** Each language's generator output goes to `<lang>/generated/.generate.log`
  and its build and sample output to `<lang>/generated/.verify.log` (both gitignored); those files
  are the proof, not your summary. Report their paths.
- **A change to the procedure is a change to this skill.** A new generator flag goes into
  `targets.conf`; a new post-step, build or sample command goes into the stage A tables. The
  comments in `webhooks-sdk/` (samples, shims, templates, `README.md`) point here instead of
  repeating the steps.

## Algorithm

1. **Contract check** (below). If the contract is behind the code, say exactly what differs and ask
   whether to update the contract first (`AskUserQuestion`). Updating it is part of the job when the
   user agrees — it is the only correct place for the change.
2. **Languages** — from the request; if none named, ask (`AskUserQuestion`, multiSelect) out of the
   targets read from `targets.conf`, plus "all". A contract change normally means **all**: every
   language carries the same models.
3. **Stage A** for each chosen language, with verification on unless the user asked for generation
   only.
4. **Report**: per-language verify outcome, `NOT VERIFIED` languages, the `git status` of
   `webhooks-sdk/`, and anything the contract check found.
5. **Stage B** only when asked (publishing, packaging changes, "does it still install").

## 1. Contract check

What the contract mirrors, and where it lives:

| Contract part | Source in code |
| --- | --- |
| `components.schemas.*Payload` (user, group, file, folder, room, form submit, summaries) | `common/ASC.Api.Core/Webhook/Payloads/*WebhookDto.cs` (user, group) and `products/ASC.Files/Core/ApiModels/WebhookDto/*.cs` (files, folders, rooms, forms) |
| the envelope (`WebhookEnvelope`, `WebhookEventInfo`, `WebhookConfigInfo`, `WebhookTargetInfo`) | `common/ASC.Webhooks.Core/WebhookPayload.cs` |
| `x-docspace-trigger-payloads` (trigger → payload) | `[WebhookPayload(...)]` on `common/ASC.Webhooks.Core/WebhookTrigger.cs` fields |
| serialization rules in `info.description` | `WebhookPublisher.cs` (`_serializerOptions`) |

Do it from the diff, not from memory:

```bash
git diff <base>... -- common/ASC.Api.Core/Webhook/Payloads products/ASC.Files/Core/ApiModels/WebhookDto common/ASC.Webhooks.Core/WebhookTrigger.cs common/ASC.Webhooks.Core/WebhookPayload.cs common/ASC.Webhooks.Core/WebhookPublisher.cs
git diff <base>... -- common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml
```

For every DTO property added, removed or renamed, and for every changed meaning (null vs default,
encoding, absolute vs relative URL), the YAML needs the matching change — including the property
`description`, which is what lands in every language's `docs/<Model>.md`. Serialized names are
camelCase (`PropertyNamingPolicy = CamelCase`); a property that is `[JsonIgnore]` or never set is
not on the wire. A trigger added to `WebhookTrigger.cs` needs its row in
`x-docspace-trigger-payloads` — that block is what `tools/gen-trigger-map.py` turns into each
language's dispatch table.

When the trigger/DTO pairing changed, also run the pairing tests (fast, no Aspire):

```bash
dotnet test common/Tests/ASC.Core.Common.Tests/ASC.Core.Common.Tests.csproj -- --filter-class "*WebhookPayloadPairing*"
```

## Stage A — generate and verify

**Working directory: `webhooks-sdk/`** for every command in this stage. Every path below is relative
to it. That is also what makes the generator version pinned in `webhooks-sdk/openapitools.json`
apply. Fixture files are the one exception: pass them as **absolute** paths, because `go -C` and
`dotnet run --project` resolve a relative path against a different directory.

Languages are independent; do them one after another and run the slow steps (the generator, Maven,
Swift, the first `npm ci`) in the background. All nine with verification take ~90 s on a warm
machine; first runs are longer (the generator jar, Maven and npm downloads).

### A1. Wipe `<lang>/generated/`

Create the directory if it is missing, then delete its **contents**, files first and directories
after, never the directory itself: on Windows an editor or the previous generator run can still hold
a handle on it. A leftover empty directory after this step is harmless; a leftover file is not.

```bash
# POSIX shell — Linux, macOS, Git Bash on Windows
mkdir -p <lang>/generated
find <lang>/generated -mindepth 1 -type f -delete
find <lang>/generated -mindepth 1 -depth -type d -exec rmdir {} +
```

```powershell
# PowerShell — any OS
New-Item -ItemType Directory -Force <lang>/generated | Out-Null
Get-ChildItem <lang>/generated -Recurse -File -Force | Remove-Item -Force
Get-ChildItem <lang>/generated -Directory -Force | Remove-Item -Recurse -Force
```

### A2. Run the generator

From the language's line in [`targets.conf`](targets.conf)
(`dirname|generator|global-properties|additional-properties|template-dir|extra-cli-args`):

```
openapi-generator-cli generate -i ../common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml -g "<generator>" -o "<dirname>/generated" --global-property "<global-properties>" --additional-properties="<additional-properties>" -t "<template-dir>" -c "<template-dir>/files.yaml" <extra-cli-args> > <dirname>/generated/.generate.log 2>&1
```

- Leave out `--additional-properties` when that field is empty, and `-t` when the template dir is
  empty.
- Add `-c <template-dir>/files.yaml` **only when that file exists** (Glob). A custom output file can
  only be declared in a config file; anything it names must also be in the `supportingFiles` list
  of the global properties, or the generator silently skips it.
- `<extra-cli-args>` are separate words (`--name-mappings hasAvatar=HasAvatarFlag --git-user-id …`)
  — pass them unquoted, as they are.
- Quote the other values exactly as shown. They contain commas, and an unquoted `a,b` is an array in
  PowerShell. PHP's `invokerPackage` contains backslashes, which both shells keep inside double quotes.
- In PowerShell, if `openapi-generator-cli` (or later `npm`) is refused by the execution policy,
  you got npm's `.ps1` shim; call `openapi-generator-cli.cmd` / `npm.cmd` instead.

Judge it by the exit code. On a failure the language is **FAILED**: report
`<lang>/generated/.generate.log` and skip its post-step and verification. Otherwise count the
generated files (Glob `<lang>/generated/**/*`, minus `.generate.log`) for the report.

### A3. Post-step

Only these languages have one. The trigger tables are derived from the contract by
`tools/gen-trigger-map.py`, never hand-maintained. It is invoked as `python` here; on a box where only
`python3` exists, use that. If neither is there, stop: without this step the trigger tables go stale.

| Language | Post-step |
| --- | --- |
| `java` | copy the contents of `java/shim/` into `java/generated/src/main/java/` (merging directories); then `python tools/gen-trigger-map.py --lang java --spec ../common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml --package com.onlyoffice.docspace.webhooks.sdk --model-package com.onlyoffice.docspace.webhooks.sdk.model --out java/generated/src/main/java/com/onlyoffice/docspace/webhooks/sdk/JSON.java` |
| `kotlin` | copy `kotlin/docs/*.md` into `kotlin/generated/docs/` |
| `go` | copy `go/shim/*.go` into `go/generated/` |
| `csharp` | `python tools/gen-trigger-map.py --lang csharp --spec ../common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml --package DocSpace.Webhooks.SDK --model-package DocSpace.Webhooks.SDK.Model --out csharp/generated/src/DocSpace.Webhooks.SDK/Triggers.cs`; then `python tools/gen-trigger-map.py --lang markdown --spec ../common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml --doc-path docs/ --out csharp/generated/TRIGGERS.md` |
| `typescript` | `python tools/gen-trigger-map.py --lang typescript --spec ../common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml --out typescript/generated/triggers.ts` |

The copies:

```bash
# POSIX shell
cp -r java/shim/. java/generated/src/main/java/
cp kotlin/docs/*.md kotlin/generated/docs/
cp go/shim/*.go go/generated/
```

```powershell
# PowerShell — Copy-Item merges into the existing package directories when the target is their parent
Copy-Item java/shim/* java/generated/src/main/java/ -Recurse -Force
Copy-Item kotlin/docs/*.md kotlin/generated/docs/ -Force
Copy-Item go/shim/*.go go/generated/ -Force
```

### A4. Verify

Skip only when the user asked for generation alone, and then say so in the report. Generating is not
the same as working: every SDK here once generated cleanly and still failed to compile or to parse
a real delivery. So each generated language is built, and then its sample is run over **every**
signed fixture in `samples/`. The sample verifies the signature and parses the payload with the
generated models, which is exactly what a receiver does.

Append the output of every command for a language to `<lang>/generated/.verify.log`
(`>> <lang>/generated/.verify.log 2>&1`, the same in both shells), starting from an empty file.

**Toolchain first.** If the tool in the *Needs* column is not on PATH (`command -v <tool>` /
`Get-Command <tool>`), the language is **NOT VERIFIED**. Record it and move on. That is not a failure,
and it must never be reported as a pass.

| Language | Needs | Build (any non-zero exit = BUILD FAILED) | Sample, per fixture |
| --- | --- | --- | --- |
| `csharp` | `dotnet` | `dotnet build csharp/samples/WebhookLogger -nologo -v q` | `dotnet run --project csharp/samples/WebhookLogger --no-build -- --file <body> --secret <secret> --signature <sig>` |
| `go` | `go` | `go -C go/generated vet ./...`; then `go -C go/samples vet ./...` | `go -C go/samples run . <body> <secret> <sig>` |
| `java` | `mvn` | `mvn -f java/pom.xml -q compile` | `mvn -f java/pom.xml -q exec:java "-Dexec.args=<body> <secret> <sig>"` |
| `kotlin` | `mvn` | `mvn -f kotlin/pom.xml -q compile` | `mvn -f kotlin/pom.xml -q exec:java "-Dexec.args=<body> <secret> <sig>"` |
| `php` | `php` | `php -l` on every `php/generated/lib/**/*.php` (below) | `php php/samples/log_webhook.php <body> <secret> <sig>` |
| `python` | `python` | `python -m compileall -q python/generated` | `python python/samples/log_webhook.py <body> <secret> <sig>` |
| `ruby` | `ruby` | — | `ruby ruby/samples/log_webhook.rb <body> <secret> <sig>` |
| `swift` | `swift` | `swift build --package-path swift` | `swift run --package-path swift WebhookLogger <body> <secret> <sig>` |
| `typescript` | `npm` | if `typescript/node_modules` is missing: `npm --prefix typescript ci --silent` (if that fails: `npm --prefix typescript install --silent`); then `npm --prefix typescript run -s build` | `node typescript/dist/samples/log-webhook.js <body> <secret> <sig>` |

- The `-Dexec.args=…` argument is quoted as a whole on purpose: PowerShell splits an unquoted `-D`
  argument at the first dot.
- Fixtures: Glob `samples/*.json`. `<body>` is the fixture's **absolute** path. `<sig>` is the
  content of the `.sig` file with the same name (`sha256=…`). `<secret>` is the content of
  `samples/SECRET`. Read both files and pass the values trimmed. A fixture whose sample exits
  non-zero is **FAILED**.
- Skip the samples of a language whose build failed.

PHP lint, which has to be a loop because `php -l` takes one file:

```bash
# POSIX shell
find php/generated/lib -name '*.php' -print0 | xargs -0 -n1 php -l > /dev/null
```

```powershell
# PowerShell
Get-ChildItem php/generated/lib -Recurse -Filter *.php | ForEach-Object { php -l $_.FullName > $null; if ($LASTEXITCODE) { "FAILED: $($_.FullName)" } }
```

The bash form fails through `xargs`' exit code. The PowerShell loop does not set one, so the
build counts as failed when it prints any `FAILED:` line.

Outcomes, per language — use these words in the report:

| Outcome | Meaning |
| --- | --- |
| `models: N files` | generation succeeded (not that it compiles) |
| `FAILED — see <lang>/generated/.generate.log` | the generator failed; nothing for that language is verified |
| `BUILD FAILED — see <lang>/generated/.verify.log` | generated, but the language does not build — the most common real failure |
| `<fixture>: ok` / `FAILED` | the sample parsed (or did not parse) that real delivery |
| `NOT VERIFIED (toolchain missing): …` | the language was generated but **not** checked |

The run has failed if any generator, build or fixture failed. A missing toolchain does **not** fail
it. So collect the `NOT VERIFIED` list every time and report it. Ruby and Swift are the usual ones;
the README records that they were checked in the `ruby:3.4-slim` and `swift:6.1` images instead.

## Stage B — package check (on request)

Stage A proves the models compile and parse. It does not prove the package a user installs
is right: its name, its dependencies, what it ships. Where each language's package metadata comes
from — and therefore where a fix belongs:

| Language | Package file | Owner |
| --- | --- | --- |
| C# | `csharp/src/DocSpace.Webhooks.SDK/DocSpace.Webhooks.SDK.csproj` | hand-written |
| Go | `go/generated/go.mod`, `go.sum` | generator (`supportingFiles` + `--git-user-id/--git-repo-id` in `targets.conf`); `generated/` is the module root |
| Java / Kotlin | `java/pom.xml`, `kotlin/pom.xml` | hand-written |
| PHP | `php/generated/composer.json` | `templates/php/composer.mustache` |
| Python | `python/generated/pyproject.toml`, package `__init__.py` | `templates/python/pyproject.mustache`, `__init__package.mustache` |
| Ruby | `ruby/generated/docspace-webhooks-sdk.gemspec` | `templates/ruby/gemspec.mustache` |
| Swift | `swift/Package.swift` | hand-written |
| TypeScript | `typescript/package.json` | hand-written |

Name, description, author, license and repository follow the matching API SDK in
`sdk/docspace-api-sdk-*` (`webhooks` in place of `api`); `README.md` → *Package naming* has the table.

Checks, from the repo root, each into a scratch directory outside the repo:

```bash
dotnet pack webhooks-sdk/csharp/src/DocSpace.Webhooks.SDK -o <scratch>     # nuspec id/version/license; only lib/ inside
mvn -f webhooks-sdk/java/pom.xml -q -DskipTests package                    # jar name = README coordinates, no samples/ classes
mvn -f webhooks-sdk/kotlin/pom.xml -q -DskipTests package
python -m pip wheel --no-deps -w <scratch> webhooks-sdk/python/generated   # then install into a fresh venv and import
npm pack --dry-run ./webhooks-sdk/typescript                               # only dist/src and dist/generated
```

Keep the `./` in the `npm pack` argument: without it npm reads `webhooks-sdk/typescript` as a
GitHub `user/repo` and packs something else.

For Go, the sample module (`go/samples`, with a `replace` onto `../generated`) is the consumer test,
and stage A's verify already runs it. PHP has no `composer` check unless Composer is installed —
then `composer validate webhooks-sdk/php/generated/composer.json`.

## Reporting the result

- Per language: generated / built / each fixture ok — or exactly which step failed and the log path.
- The `NOT VERIFIED` list, verbatim.
- `git status --short webhooks-sdk/` — the generated diff, which belongs in the commit. These are
  plain directories in **this** repo, not submodules; there is no pointer to bump.
- Whether the contract was changed, and if it was behind the code but the user chose not to update
  it, say plainly that the generated SDKs describe the old payload.

## Prerequisites

A JDK (the generator is Java), `openapi-generator-cli` (`npm i -g @openapitools/openapi-generator-cli`),
and `python` (or `python3`) for the trigger tables. No bash. Verification needs each language's
toolchain: `dotnet`, `go`, `mvn`, `php`, `python`, `ruby`, `swift`, `npm`. A missing one is
reported as not verified, not as a failure.

The generator version is pinned by `webhooks-sdk/openapitools.json` and applies because stage A runs
from `webhooks-sdk/`. Do not run `openapi-generator-cli version` from the repo root to check the
install — it writes a stray `openapitools.json` into the current directory. Run it from
`webhooks-sdk/`.

## Failure modes

- `BUILD FAILED` in Go with `missing go.sum entry` or `updates to go.mod needed` — the sample module
  `go/samples` is out of step with `generated/go.mod`; run `go -C go/samples mod tidy`, never in
  `generated/` (it is regenerated).
- A language builds but every fixture fails to parse — the contract declares something the wire does
  not carry (a `required` field that `WhenWritingDefault` omits, an enum that gained a value, a
  number format the language cannot map). Fix the contract, not the generated model. The README
  lists the past cases (`webhook.target.type` as a free string, `usedSpace` as `format: double`).
- Go does not compile because the field `HasAvatar` collides with the generated presence method
  `HasAvatar()` — the `--name-mappings hasAvatar=HasAvatarFlag` flag was lost from the `go` line of
  `targets.conf`.
- Python, PHP or Kotlin silently lose the entry id (`EntryId` `oneOf`) — the `--schema-mappings`
  in `targets.conf` were changed; see README, *oneOf support varies sharply by generator*.
- Trigger tables (`Triggers.cs`, `triggers.ts`, `JSON.java`, `TRIGGERS.md`) did not pick up a new
  trigger — the contract's `x-docspace-trigger-payloads` was not updated, or the post-step was
  skipped (no `python`).
- Every Java/Kotlin sample run fails with an unknown lifecycle phase `.args=…` — the `-Dexec.args`
  argument lost its quotes in PowerShell.
- A file from an older generator version survives in `generated/` — the wipe in A1 stopped at a
  locked file. Close whatever holds it and wipe again; never generate over leftovers.

Known-harmless noise:

- `warning: in the working copy of '…/.openapi-generator/FILES', CRLF will be replaced by LF` — the
  generator's file manifest on Windows; git normalizes it.
- Generator warnings about unused models (`FileEntryPayload` is emitted but nothing points at it —
  README, *Known gaps*).
