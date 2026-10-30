---
name: generate-webhook-sdk
description: "Generating the DocSpace webhook SDKs (the webhooks/ folder of each API SDK submodule in sdk/): checking the contract docspace-webhooks.yaml against the webhook payload DTOs and triggers and, after asking the user, regenerating it (common/Tools/ASC.Webhooks.Contract), running openapi-generator-cli per language from the skill's targets.conf with its post-steps, building each language and replaying the signed fixtures through its sample, and optionally checking that each package still builds and installs. USE FOR: generate webhook SDK, regenerate webhook models, update the webhook SDKs, a webhook payload DTO or trigger changed and the SDKs must follow, NOT VERIFIED languages, webhook SDK packaging check, how to regenerate the webhook SDKs by hand. DO NOT USE FOR: the REST API SDKs in sdk/ (generate-sdk), hand-editing what the generator writes into a webhook SDK, changing payload DTOs or WebhookTrigger itself."
---

# Generating the webhook SDKs

This is the inbound counterpart of `generate-sdk` and shares none of its tooling. The webhook SDKs
are built by calling `openapi-generator-cli` directly, one language at a time; the
`ASC.Api.Documentation` tool is deliberately not involved (its joiner rewrites the REST contract on
every run and its output folders point at the API SDK submodules — `common/Tools/ASC.Webhooks.Contract/Sdk/README.md`,
*Why not the generate-sdk tool*). Never route a webhook generation through `generate-sdk`, and never
add webhook commands to that tool as a side effect.

The skill does three things:

1. **Check the contract** against the code it describes — always — and **regenerate it** when the user says so.
2. **Generate and verify** the requested languages, step by step, with no shell script, and pack
   the TypeScript package — stage A.
3. **Report** what changed and how each language verified; on request, **check the packages** —
   stage B.

## Ground rules

- **The contract is generated, never edited.** `common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml`
  is written by `common/Tools/ASC.Webhooks.Contract` from the payload DTOs, the envelope,
  `WebhookTrigger`, `WebhookDelivery` and the hand-written `ContractHeader` in the tool. Nothing
  regenerates it on its own (`WebhookContractTests` only fails when it is behind), so a DTO change
  that was not followed by a run of the tool leaves the SDKs describing the old payload. Step 1 below
  exists for that reason: always check the contract and always ask whether to regenerate it — never
  skip the question because the user only asked to "regenerate the SDKs", and never regenerate
  without the answer.
- **No script, no particular shell.** There is no generation script: this skill *is* the procedure,
  and its steps go through `openapi-generator-cli`, `python` and each language's own toolchain, which
  behave the same in bash and PowerShell on every OS. Where a step is a file operation that differs
  between shells, both forms are given; use the one for the shell you are in. Do not write a script
  to wrap the steps — a person regenerating by hand follows this file the same way.
- **Do not chain commands with `&&` or prefix them with `cd`.** It fails in Windows PowerShell 5.1.
  Set the working directory once (it persists between calls in both shell tools) and use each
  tool's own directory flag (`go -C`, `mvn -f`, `npm --prefix`, `swift --package-path`,
  `dotnet --project`) for the commands that need another one.
- **Never edit `<out>/`.** It is wiped on every run. A fix belongs in the contract, in
  `targets.conf` (generator flags), in `<tools>/templates/<lang>/`, in a shim (`<root>/shim` of java, python, php, ruby and swift,
  `<root>/_shim` of go, `<root>/docs` of kotlin), or in the hand-written runtime `<root>/src/`.
- **The generated files are committed** (see the `.gitignore` in each `<root>`): its diff is part of the change and
  goes into the same commit as the contract change that caused it. Only `.generate.log` and
  `.verify.log` are ignored.
- **Where things are.** Every command runs from the **repository root**. Four names stand for paths:
  `<root>` is the directory of one language's webhook SDK, the second field of its `targets.conf`
  line (`sdk/docspace-api-sdk-csharp/webhooks`, …); `<out>` is where the generator writes,
  `<root>` joined with the third field -- `<root>/generated` for every language but go, `<root>` itself
  for go, whose module sits at the root of `webhooks/`; `<spec>` is the published contract
  `sdk/docspace-api-spec/webhooks/docspace-webhooks.yaml`; `<fixtures>` is `web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks/Fixtures`, the source of the signed
  deliveries, kept by the suite that captures them, and `<root>/fixtures` is each SDK's committed copy of it;
  `<tools>` is `common/Tools/ASC.Webhooks.Contract/Sdk` (the pinned `openapitools.json`, the
  templates and `gen-trigger-map.py`). A `<root>` inside a submodule is committed there, not here.
- **No hardcoded language list or flags.** The targets are the lines of
  [`targets.conf`](targets.conf), next to this file; read it before offering choices or running
  anything. The first
  field of a line is the language name (lowercase directory names — `csharp`, `typescript`, … —
  unlike `generate-sdk`'s command names).
- **Keep the evidence.** Each language's generator output goes to `<out>/.generate.log`
  and its build and sample output to `<out>/.verify.log` (both gitignored); those files
  are the proof, not your summary. Report their paths.
- **A change to the procedure is a change to this skill.** A new generator flag goes into
  `targets.conf`; a new post-step, build or sample command goes into the stage A tables. The
  comments in the SDK folders and the tooling (samples, shims, templates, `README.md`) point here instead of
  repeating the steps.

## Algorithm

1. **Contract** (below): check whether the committed contract matches the code, then **always ask
   whether to regenerate it** (`AskUserQuestion`, yes/no) — the counterpart of `generate-sdk` asking
   about the OpenAPI documents. If yes, regenerate it and review its diff: a change in it that the
   code change does not explain is a finding — report it before generating the SDKs. If no while
   the check said "out of date", go on with the committed contract and say in the report that the
   SDKs describe the old payload.
   **Fixtures** (below) next, only when the regenerated contract changed more than descriptions: the
   verification in A4 replays them, and a capture from stale binaries replays the old wire format.
2. **Languages** — from the request; if none named, ask (`AskUserQuestion`, multiSelect) out of the
   targets read from `targets.conf`, plus "all". A contract change normally means **all**: every
   language carries the same models.
3. **Stage A** for each chosen language, with verification on unless the user asked for generation
   only.
4. **Report**: per-language verify outcome, `NOT VERIFIED` languages, the `git status` of every
   `<root>` and of `sdk/docspace-api-spec`, and anything the contract check found.
5. **Stage B** only when asked (publishing, packaging changes, "does it still install").

## 1. Contract

From the repository root, first the check, which writes nothing and exits 1 when the committed file
is out of date:

```bash
dotnet run --project common/Tools/ASC.Webhooks.Contract -- --check
```

Then ask (`AskUserQuestion`, yes/no) whether to regenerate the contract before the SDKs, and put the
check's result into the question so the user decides on facts:

- **out of date** — recommend yes: the code changed since the contract was last written, and SDKs
  generated from the committed file would describe the old payload;
- **up to date** — recommend no: regenerating writes the same file. Still ask; the user may want the
  run as proof.

On yes:

```bash
dotnet run --project common/Tools/ASC.Webhooks.Contract
git diff -- common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml
git -C sdk/docspace-api-spec diff -- webhooks/docspace-webhooks.yaml
```

The tool writes the same file twice: the server's copy, which `ASC.Webhooks.Core` embeds and
Web.Studio serves, and the published `<spec>` in the `sdk/docspace-api-spec` submodule, which the
SDKs are generated from. The check compares both; with the submodule not checked out it says so and
skips the published copy -- then initialise it (`git submodule update --init sdk/docspace-api-spec`)
before generating.

The contract reads the API documents as they are on disk. When a `[RaisesWebhook]` attribute changed, re-emit
them first (stage A of the `generate-sdk` skill), or the contract lists the operations as they were; the check
compares against the same files, so it does not notice either.

On no, the SDKs are generated from the committed file as it is. Where each part of the contract
comes from — and therefore where a change to it belongs:

| Contract part | Source |
| --- | --- |
| `components.schemas.*Payload` (user, group, file, folder, room, form submit, summaries) | `common/ASC.Api.Core/Webhook/Payloads/*WebhookDto.cs` (user, group) and `products/ASC.Files/Core/ApiModels/WebhookDto/*.cs` (files, folders, rooms, forms) |
| the envelope (`WebhookEnvelope`, `WebhookEventInfo`, `WebhookConfigInfo`, `WebhookTargetInfo`) | `common/ASC.Webhooks.Core/WebhookPayload.cs` |
| a schema `description` | the type's `<summary>` and `<remarks>` |
| a property `description` | the property's `<summary>`; for an enum the generator appends its members |
| `x-docspace-trigger-payloads.map` | the trigger table in `WebhookContractGenerator` (by trigger family, with per-trigger exceptions), in `[Order]` order |
| `webhooks` (one POST per trigger: headers, body, answers), `tags` (one per trigger family) and the `*Delivery` schemas (`WebhookEnvelope` with `payload` typed, one per payload schema) | the same trigger table, with the texts in `ContractHeader` (`GetWebhook`, `GetTag`, `GetDeliverySchema`); a family without a tag description stops the generator |
| the list of REST API operations that raise each trigger, in its `webhooks` description | `[RaisesWebhook]` on the API actions, read from `x-docspace-webhooks` in the generated API documents (`common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/json/*.json`, see `RaisingOperations`); what raises a trigger outside the API is `_otherSources` in `ContractHeader` |
| `info`, the transport and serialization notes, `EntryId`, the map's description | `common/Tools/ASC.Webhooks.Contract/ContractHeader.cs` (prose, `OpenApiInfo`, the `EntryId` `OpenApiSchema`) |
| the numbers and names in the transport notes: request timeout, retry intervals and jitter, header names, config keys | `common/ASC.Webhooks.Core/WebhookDelivery.cs`, which the sender reads as well |

How the generator reads the code, so that its output can be predicted:

- Property names and the set of properties come from `WebhookPublisher.CreateSerializerOptions`:
  camelCase, `[JsonIgnore]` honoured, read-only getters included.
- A property typed by the entry ID parameter `T` becomes `EntryId`; a base class becomes `allOf`;
  `int` is `int32`, `long` `int64`; `[EmailAddress]` adds `format: email`. An enum is `type: integer`,
  or `type: string` when the payload serializer writes it by name (`SchemaBuilder.IsWrittenAsString`
  serializes the enum's default value and checks for a quote, so a `JsonStringEnumConverter` on the
  enum type is what flips it); either way the property description lists the members with their
  numeric values.
- A schema name is the DTO name with `WebhookDto` replaced by `Payload` (the envelope types have
  fixed names). A payload type that does not end in `WebhookDto` stops the generator.
- A `$ref` property carries no description: several generators turn a `$ref` with siblings into a
  new inline model.
- A member without a `<summary>` stops the generator with the list of them.

A trigger whose family the table does not know stops the generator: add the family, or an
exception for the trigger, to the table.

### Fixtures

The signed deliveries in `<fixtures>` are captured from a running server by
`WebhookFixtureCaptureTests`, an explicit integration test that writes the bodies and signature
headers byte for byte as the receiver got them. Re-capture them whenever a payload DTO or the
envelope changed, before A4, or A4 checks the SDKs against the old wire format. Skip this when the
contract was not regenerated, or its diff is empty or touches nothing but descriptions: a capture
rewrites every captured fixture with fresh names, ids, timestamps and signatures even when the wire
format did not change, which only adds noise to the commit.

Build the whole solution first. `dotnet test` on `ASC.Web.Api.Tests` builds only that project and
its references, which do not include the Files service or Web.Studio, while the AppHost starts them
from their last build output: a payload DTO of files or rooms changed since then would be captured
from the old binaries. From the repository root:

```bash
dotnet build ASC.Web.slnx
dotnet test web/ASC.Web.Api.Tests/ASC.Web.Api.Tests.csproj -- --filter-class "*WebhookFixtureCaptureTests" --explicit only
git status --short web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks/Fixtures
```

The test needs the integration-test stack (Docker, the Aspire host); where tests run on another
machine, run it there and bring back `<fixtures>`. `unknown.trigger.json`,
`form.submit.json` and `negative/` are written by hand and left alone; their `.sig` files
are re-signed only when their body changes. A capture, or a change to a hand-written fixture, reaches
the SDKs only through the copy step in A3, so run it for **every** language afterwards, even when
the contract and the models did not change.

## Stage A — generate and verify

**Working directory: the repository root** for every command in this stage; every path below is
relative to it, with `<root>`, `<spec>`, `<fixtures>` and `<tools>` as in the ground rules. The
generator version is pinned by `--openapitools <tools>/openapitools.json` on every generator call.
Fixture files are passed as **absolute** paths, because `go -C` and
`dotnet run --project` resolve a relative path against a different directory. In a POSIX shell on
Windows (Git Bash, MSYS) write that path with `cygpath -m "$PWD/<root>/fixtures/<name>.json"`
(`W:/Work/...`): MSYS converts a `/w/...` argument for a native program only when the argument
*starts* with the path, so `"-Dexec.args=/w/... <secret> <sig>"` reaches Maven unconverted and
Java and Kotlin open a file that does not exist. PowerShell's `(Resolve-Path ...).Path` is already
native.

Languages are independent; do them one after another and run the slow steps (the generator, Maven,
Swift, the first `npm ci`) in the background. Measured on a warm Windows machine: the generator
takes ~25 s for all nine, the post-steps a few seconds, and verification several minutes, almost all
of it Maven (one JVM start per Java and Kotlin fixture) and `dotnet` -- 225 s for seven languages
here, plus ~25 s for PHP, Ruby and Swift in Docker with a warm Swift build. Expect four to five
minutes for the whole run with verification; first runs are longer (the generator jar, Maven and npm downloads), and
Swift in Docker adds a few minutes for its first package build.

### A0. Check the post-step toolchain

Before anything is wiped: every language's post-step (A3) writes its trigger table with Python and
PyYAML. Check it once, up front:

```bash
python -c "import yaml"     # or python3 on a box without a `python`
```

If neither `python` nor `python3` is there, or the import fails, stop the run here and ask for
`pip install pyyaml` — A1 would otherwise wipe `<out>` and leave it without its trigger tables.

### A1. Wipe `<out>`

Create the directory if it is missing, then delete its **contents**, files first and directories
after, never the directory itself: on Windows an editor or the previous generator run can still hold
a handle on it. A leftover empty directory after this step is harmless; a leftover file is not.

```bash
# POSIX shell — Linux, macOS, Git Bash on Windows
mkdir -p <out>
find <out> -mindepth 1 -type f -delete
find <out> -mindepth 1 -depth -type d -exec rmdir {} +
```

```powershell
# PowerShell — any OS
New-Item -ItemType Directory -Force <out> | Out-Null
Get-ChildItem <out> -Recurse -File -Force | Remove-Item -Force
Get-ChildItem <out> -Directory -Force | Remove-Item -Recurse -Force
```

**When `<out>` is `<root>` itself (go)**, the directory also holds the hand-written `_shim/`,
`samples/` and `.gitignore`, so it is never wiped wholesale. Delete instead what the previous run
wrote: every path listed in `<out>/.openapi-generator/FILES`, the `.openapi-generator/` directory,
the post-step outputs (`triggers.go` and a copy of every file in `_shim/`) and the two logs; then
remove the directories left empty (`docs/`, `api/`). A model the contract dropped is in the old
manifest, so it goes too.

```bash
# POSIX shell
while IFS= read -r f; do rm -f "<out>/${f:?}"; done < <out>/.openapi-generator/FILES
rm -rf <out>/.openapi-generator <out>/triggers.go <out>/.generate.log <out>/.verify.log
for f in <root>/_shim/*; do b=$(basename "$f"); rm -f "<out>/${b:?}"; done
find <out> -mindepth 1 -depth -type d -empty -delete
```

```powershell
# PowerShell
Get-Content <out>/.openapi-generator/FILES | ForEach-Object { Remove-Item "<out>/$_" -Force -ErrorAction SilentlyContinue }
Remove-Item <out>/.openapi-generator, <out>/triggers.go, <out>/.generate.log, <out>/.verify.log -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem <root>/_shim -File | ForEach-Object { Remove-Item "<out>/$($_.Name)" -Force -ErrorAction SilentlyContinue }
Get-ChildItem <out> -Directory -Recurse | Sort-Object { $_.FullName.Length } -Descending | Where-Object { -not (Get-ChildItem $_.FullName -Force) } | Remove-Item
```

With no manifest yet (a first run), there is nothing of a previous run to delete.

### A2. Run the generator

From the language's line in [`targets.conf`](targets.conf)
(`name|root|generator|global-properties|additional-properties|template-dir|extra-cli-args`):

```
openapi-generator-cli --openapitools <tools>/openapitools.json generate -i <spec> -g "<generator>" -o "<out>" --global-property "<global-properties>" --additional-properties="<additional-properties>" -t "<template-dir>" -c "<template-dir>/files.yaml" <extra-cli-args> > <out>/.generate.log 2>&1
```

- Never leave out `--openapitools`: without it the CLI uses whatever version it finds and writes a
  stray `openapitools.json` into the repository root.
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
`<out>/.generate.log` and skip its post-step and verification. Otherwise count the
generated files (Glob `<out>/**/*`, minus `.generate.log`) for the report.

### A3. Post-step

Every language has one. The trigger -> payload model tables are derived from the contract by
`<tools>/gen-trigger-map.py`, never hand-maintained, and every sample picks its payload model through
its language's table. It is invoked as `python` here; on a box where only `python3` exists, use that.
A0 has already checked that it and PyYAML are there.

First, for **every** language: put back the generator's file manifest when only its line endings
changed. On Windows the generator writes `<out>/.openapi-generator/FILES` with CRLF; git
then warns `CRLF will be replaced by LF` on every diff, and depending on `core.autocrlf` lists the
nine manifests as modified although their content is the same -- rewriting the file with LF does
not help, git still sees a changed file it cannot tell apart. Restoring it from git does: it leaves
an LF file and a clean status. A manifest whose content really changed (a model added or removed)
fails the comparison and stays as generated.

```bash
# POSIX shell, per <root>
git -C <out> diff --ignore-cr-at-eol --quiet -- .openapi-generator/FILES && git -C <out> checkout -- .openapi-generator/FILES
```

```powershell
# PowerShell, per <root>
git -C <out> diff --ignore-cr-at-eol --quiet -- .openapi-generator/FILES; if ($LASTEXITCODE -eq 0) { git -C <out> checkout -- .openapi-generator/FILES }
```

`git -C <out>` runs git in the repository that owns `<out>` -- the API SDK submodule or this one --
so the same line works for both; from the server root a plain `git checkout` refuses a path inside a
submodule.

Then, per language:

| Language | Post-step |
| --- | --- |
| `java` | copy the contents of `<root>/shim/` into `<out>/src/main/java/` (merging directories); then `python <tools>/gen-trigger-map.py --lang java --spec <spec> --package com.onlyoffice.docspace.webhooks.sdk --model-package com.onlyoffice.docspace.webhooks.sdk.model --out <out>/src/main/java/com/onlyoffice/docspace/webhooks/sdk/JSON.java`; then `python <tools>/gen-trigger-map.py --lang java-triggers --spec <spec> --package com.onlyoffice.docspace.webhooks.sdk --model-package com.onlyoffice.docspace.webhooks.sdk.model --out <out>/src/main/java/com/onlyoffice/docspace/webhooks/sdk/WebhookTriggers.java`; then `python <tools>/gen-trigger-map.py --lang java-router --spec <spec> --package com.onlyoffice.docspace.webhooks.sdk --model-package com.onlyoffice.docspace.webhooks.sdk.model --out <out>/src/main/java/com/onlyoffice/docspace/webhooks/sdk/WebhookRouter.java` (the router's typed `on<Trigger>` methods) |
| `kotlin` | copy `<root>/docs/*.md` into `<out>/docs/`; then `python <tools>/gen-trigger-map.py --lang kotlin --spec <spec> --package onlyoffice.docspace.webhooks.sdk --model-package onlyoffice.docspace.webhooks.sdk.models --out <out>/src/main/kotlin/onlyoffice/docspace/webhooks/sdk/WebhookTriggers.kt` |
| `go` | copy `<root>/_shim/*.go` into `<out>/`; then `python <tools>/gen-trigger-map.py --lang go --spec <spec> --package docspace_webhooks_sdk --out <out>/triggers.go` |
| `csharp` | `python <tools>/gen-trigger-map.py --lang csharp --spec <spec> --package DocSpace.Webhooks.SDK --model-package DocSpace.Webhooks.SDK.Model --out <out>/src/DocSpace.Webhooks.SDK/Triggers.cs`; then `python <tools>/gen-trigger-map.py --lang markdown --spec <spec> --doc-path docs/ --out <out>/TRIGGERS.md`; then `python <tools>/gen-trigger-map.py --lang csharp-router --spec <spec> --package DocSpace.Webhooks.SDK --model-package DocSpace.Webhooks.SDK.Model --out <out>/src/DocSpace.Webhooks.SDK/WebhookRouter.Triggers.cs` (the `On<Trigger>` methods of `WebhookRouter`) |
| `php` | copy `<root>/shim/*.php` into `<out>/lib/` (the router); then `python <tools>/gen-trigger-map.py --lang php --spec <spec> --package "OnlyOffice\DocSpace\Webhooks\Sdk" --model-package "OnlyOffice\DocSpace\Webhooks\Sdk\Model" --out <out>/lib/WebhookTriggers.php` |
| `python` | copy `<root>/shim/*.py` into `<out>/docspace_webhooks_sdk/` (the router); then `python <tools>/gen-trigger-map.py --lang python --spec <spec> --package docspace_webhooks_sdk --out <out>/docspace_webhooks_sdk/triggers.py` |
| `ruby` | copy `<root>/shim/*.rb` into `<out>/lib/docspace-webhooks-sdk/` (the router, which the gem entry requires); then `python <tools>/gen-trigger-map.py --lang ruby --spec <spec> --package DocspaceWebhooksSdk --out <out>/lib/docspace-webhooks-sdk/webhook_triggers.rb` (the gem entry point from `<tools>/templates/ruby/gem.mustache` requires it) |
| `swift` | copy `<root>/shim/*.swift` into `<out>/Sources/DocSpaceWebhooksSDK/` (the router); then `python <tools>/gen-trigger-map.py --lang swift --spec <spec> --out <out>/Sources/DocSpaceWebhooksSDK/WebhookTriggers.swift` |
| `typescript` | `python <tools>/gen-trigger-map.py --lang typescript --spec <spec> --out <out>/triggers.ts` |

The PHP namespaces are quoted: both shells keep backslashes inside double quotes.

Then, for **every** language, replace its copy of the fixtures with the source and put the copy's
README next to it. Each SDK repository carries its own copy, committed like `<out>`, so that its
sample and tests run from a clone of that repository alone; nothing in an SDK reads `<fixtures>`
itself. The copy is never edited by hand: a fixture is changed in `<fixtures>` and copied again.

```bash
# POSIX shell
rm -rf "<root>/fixtures"; mkdir -p <root>/fixtures
cp -r <fixtures>/. <root>/fixtures/
cp <tools>/fixtures-README.md <root>/fixtures/README.md
```

```powershell
# PowerShell
Remove-Item <root>/fixtures -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item <fixtures> <root>/fixtures -Recurse
Copy-Item <tools>/fixtures-README.md <root>/fixtures/README.md -Force
```

The shim copies, each with the `<root>` of its own language (C# and TypeScript have none):

```bash
# POSIX shell
cp -r <root>/shim/. <out>/src/main/java/                       # java
cp <root>/docs/*.md <out>/docs/                                # kotlin
cp <root>/_shim/*.go <out>/                                    # go
cp <root>/shim/*.py <out>/docspace_webhooks_sdk/               # python
cp <root>/shim/*.php <out>/lib/                                # php
cp <root>/shim/*.rb <out>/lib/docspace-webhooks-sdk/           # ruby
cp <root>/shim/*.swift <out>/Sources/DocSpaceWebhooksSDK/      # swift
```

```powershell
# PowerShell — Copy-Item merges into the existing package directories when the target is their parent
Copy-Item <root>/shim/* <out>/src/main/java/ -Recurse -Force                  # java
Copy-Item <root>/docs/*.md <out>/docs/ -Force                                 # kotlin
Copy-Item <root>/_shim/*.go <out>/ -Force                                     # go
Copy-Item <root>/shim/*.py <out>/docspace_webhooks_sdk/ -Force                # python
Copy-Item <root>/shim/*.php <out>/lib/ -Force                                 # php
Copy-Item <root>/shim/*.rb <out>/lib/docspace-webhooks-sdk/ -Force            # ruby
Copy-Item <root>/shim/*.swift <out>/Sources/DocSpaceWebhooksSDK/ -Force       # swift
```

### A4. Verify

Skip only when the user asked for generation alone, and then say so in the report. Generating is not
the same as working: every SDK here once generated cleanly and still failed to compile or to parse
a real delivery. So each generated language is built, and then its sample is run over **every**
signed fixture in `<root>/fixtures`. The sample verifies the signature and parses the payload with the
generated models, which is exactly what a receiver does. It must also pick the right model and refuse
what a receiver must refuse, so the run checks both: the model each sample reports, and its exit
code on the negative fixtures in `<root>/fixtures/negative/`.

Append the output of every command for a language to `<out>/.verify.log`
(`>> <out>/.verify.log 2>&1`, the same in both shells), starting from an empty file. A
run that generated the language has one already (A1 wiped the directory); a verify-only run empties
it first:

```bash
: > <out>/.verify.log            # POSIX shell
```

```powershell
Set-Content <out>/.verify.log '' # PowerShell
```

**Toolchain first.** If the tool in the *Needs* column is not on PATH (`command -v <tool>` /
`Get-Command <tool>`), the language is **NOT VERIFIED**. Record it and move on. That is not a failure,
and it must never be reported as a pass.

| Language | Needs | Build (any non-zero exit = BUILD FAILED) | Sample, per fixture |
| --- | --- | --- | --- |
| `csharp` | `dotnet` | `dotnet build <root>/samples/WebhookLogger -nologo -v q` | `dotnet run --project <root>/samples/WebhookLogger --no-build -- --file <body> --secret <secret> --signature <sig>` |
| `go` | `go` | `go -C <out> vet ./...`; then `go -C <root>/samples vet ./...` | `go -C <root>/samples run . <body> <secret> <sig>` |
| `java` | `mvn` | `mvn -f <root>/pom.xml -q compile` | `mvn -f <root>/pom.xml -q exec:java "-Dexec.args=<body> <secret> <sig>"` |
| `kotlin` | `mvn` | `mvn -f <root>/pom.xml -q compile` | `mvn -f <root>/pom.xml -q exec:java "-Dexec.args=<body> <secret> <sig>"` |
| `php` | `php` | `php -l` on every `<out>/lib/**/*.php` (below) | `php <root>/samples/log_webhook.php <body> <secret> <sig>` |
| `python` | `python` | `python -m compileall -q <out>` | `python <root>/samples/log_webhook.py <body> <secret> <sig>` |
| `ruby` | `ruby` | — | `ruby <root>/samples/log_webhook.rb <body> <secret> <sig>` |
| `swift` | `swift` | `swift build --package-path <root>` | `swift run --package-path <root> WebhookLogger <body> <secret> <sig>` |
| `typescript` | `npm` | if `<root>/node_modules` is missing: `npm --prefix <root> ci --silent`; then `npm --prefix <root> run -s build` | `node <root>/dist/samples/log-webhook.js <body> <secret> <sig>` |

- The `-Dexec.args=…` argument is quoted as a whole on purpose: PowerShell splits an unquoted `-D`
  argument at the first dot.
- `<root>/package-lock.json` is committed, so `npm ci` installs exactly the locked versions. A
  failing `npm ci` is **BUILD FAILED**: never fall back to `npm install`, which resolves the ranges
  afresh and would verify the SDK against dependencies nobody locked.
- Fixtures: Glob `<root>/fixtures/*.json`. `<body>` is the fixture's **absolute** path (`cygpath -m` in a
  POSIX shell on Windows, see above). `<sig>` is the content of the `.sig` file with the same name
  (`sha256=…`). `<secret>` is the content of `<root>/fixtures/SECRET`. Read both files and pass the values
  trimmed. A fixture whose sample exits non-zero is **FAILED**.
- **The model check.** Every sample prints one line `model: <name>`: the payload model it parsed
  with, as the contract names the schema (`FilePayload`, `UserPayload`, …), `untyped` for the
  catch-all `*`, and `unknown` for a trigger the contract does not list. The expected name comes
  from the contract's `x-docspace-trigger-payloads` map, read with a one-liner (from the
  repository root, the same in both shells):

  ```
  python -c "import json,sys,yaml; m=yaml.safe_load(open(sys.argv[1],encoding='utf-8'))['x-docspace-trigger-payloads']['map']; t=json.load(open(sys.argv[2],encoding='utf-8'))['event'].get('trigger'); print((m[t] or 'untyped') if t in m else 'unknown')" <spec> <root>/fixtures/<name>.json
  ```

  A run whose output has no line that, trimmed, equals `model: <expected>` is **FAILED (wrong
  model)**, even with exit code 0: the sample parsed the delivery with a model the contract does not
  give its trigger (a hand-written prefix rule that drifted, an unknown trigger read as a file).
- **The delivery check.** Every sample also reads the whole body into the delivery model of its
  trigger, through the generated trigger table, and prints one line `delivery: <name>`: the
  `*Delivery` schema (`FileDelivery`, `UserDelivery`, …), or `none` for the catch-all `*` and for a
  trigger the contract does not list. The expected name is the body of the trigger's entry in the
  contract's `webhooks` section, read with a one-liner (from the repository root, the same in both
  shells; `chr(36)` spells the `$` of `$ref`, which both shells would expand inside the quotes):

  ```
  python -c "import json,sys,yaml; w=yaml.safe_load(open(sys.argv[1],encoding='utf-8')).get('webhooks') or {}; t=json.load(open(sys.argv[2],encoding='utf-8'))['event'].get('trigger'); print(w[t]['post']['requestBody']['content']['application/json']['schema'][chr(36)+'ref'].rsplit('/',1)[1] if t in w else 'none')" <spec> <root>/fixtures/<name>.json
  ```

  A run whose output has no line that, trimmed, equals `delivery: <expected>` is **FAILED (wrong
  delivery)**, even with exit code 0: the delivery table of that language no longer follows the
  contract's `webhooks` section, or the sample stopped going through it.
- **The negative fixtures.** Glob `<root>/fixtures/negative/*.json` and run each sample over them exactly as
  over the positive ones (their `.sig` sits next to them; `<root>/fixtures/negative/README.md` says what is
  wrong with each). Every sample must exit **non-zero** on each of them; one that exits 0 is
  **FAILED (accepted a negative fixture)** — it would accept a forged or broken delivery, and the
  positive run cannot show that. The positive Glob `<root>/fixtures/*.json` does not descend into the
  subfolder, so the two sets never mix.
- Skip the samples of a language whose build failed.

PHP lint, which has to be a loop because `php -l` takes one file:

```bash
# POSIX shell
find <out>/lib -name '*.php' -print0 | xargs -0 -n1 php -l > /dev/null
```

```powershell
# PowerShell
Get-ChildItem <out>/lib -Recurse -Filter *.php | ForEach-Object { php -l $_.FullName > $null; if ($LASTEXITCODE) { "FAILED: $($_.FullName)" } }
```

The bash form fails through `xargs`' exit code. The PowerShell loop does not set one, so the
build counts as failed when it prints any `FAILED:` line.

Outcomes, per language — use these words in the report:

| Outcome | Meaning |
| --- | --- |
| `models: N files` | generation succeeded (not that it compiles) |
| `FAILED — see <out>/.generate.log` | the generator failed; nothing for that language is verified |
| `BUILD FAILED — see <out>/.verify.log` | generated, but the language does not build — the most common real failure |
| `<fixture>: ok` / `FAILED` | the sample parsed (or did not parse) that real delivery |
| `<fixture>: FAILED (wrong model)` | it parsed, but with another model than the contract gives its trigger |
| `<fixture>: FAILED (wrong delivery)` | the whole body went into another delivery model than the trigger's `webhooks` entry names |
| `negative/<fixture>: ok` / `FAILED (accepted a negative fixture)` | the sample refused (or accepted) a delivery it must refuse |
| `NOT VERIFIED (toolchain missing): …` | the language was generated but **not** checked |

The run has failed if any generator, build or fixture failed. A missing toolchain does **not** fail
it. So collect the `NOT VERIFIED` list every time and report it.

### A5. Docker fallback for PHP, Ruby and Swift

PHP, Ruby and Swift are the toolchains a machine most often lacks. When Docker is available, verify
them in a container instead of reporting them `NOT VERIFIED`: the same build and sample commands,
over the same fixtures, with the repository root mounted at `/w`. Inside the container a fixture is
`/w/<root>/fixtures/<name>.json` (`/w/<root>/fixtures/negative/<name>.json`), not the host path; secret, signature
and the model and delivery checks are as in A4. A language verified this way counts as verified; the report says
so, with the image and the Docker host it ran on.

```bash
# POSIX shell on Linux or macOS, from the repository root; --user keeps the files the build writes owned by you
D="docker run --rm --user $(id -u):$(id -g) -e HOME=/tmp -v $PWD:/w -w /w"
$D php:8.3-cli sh -c 'find <out>/lib -name "*.php" -print0 | xargs -0 -n1 php -l > /dev/null'
$D php:8.3-cli php <root>/samples/log_webhook.php /w/<root>/fixtures/<name>.json <secret> <sig>
$D ruby:3.4-slim ruby <root>/samples/log_webhook.rb /w/<root>/fixtures/<name>.json <secret> <sig>
# Swift: build once, then run the built sample per fixture in the same container -- a fresh container
# per fixture would resolve swift-crypto and compile from scratch each time
$D swift:6.1 bash -c 'swift build --package-path <root> && for f in <root>/fixtures/*.json <root>/fixtures/negative/*.json; do echo "--- $f"; swift run --skip-build --package-path <root> WebhookLogger "/w/$f" "$(tr -d "\r\n" < <root>/fixtures/SECRET)" "$(tr -d "\r\n" < "${f%.json}.sig")"; echo "exit $?"; done'
```

In Git Bash (MSYS) on Windows the lines above do not work as they are: MSYS rewrites every argument
that looks like a POSIX path for the native `docker.exe`, so `-v $PWD:/w`, `-w /w` and
`/w/<root>/fixtures/...` reach Docker as Windows paths. Switch the rewriting off for these commands and give
the mount a Windows source path (`cygpath -m`, `W:/Work/...`); Docker Desktop maps file ownership
itself, so drop `--user` too. This is for a local Docker Desktop only: with `DOCKER_HOST` set to a
remote daemon the Windows path means nothing to it (`invalid mode: /w`), see the note below.

```bash
# Git Bash on Windows with a local Docker Desktop, from the repository root
D="docker run --rm -v $(cygpath -m "$PWD"):/w -w /w"
MSYS_NO_PATHCONV=1 $D php:8.3-cli php <root>/samples/log_webhook.php /w/<root>/fixtures/<name>.json <secret> <sig>
```

The same prefix goes before every `$D` line: `MSYS_NO_PATHCONV=1 $D ruby:3.4-slim ...`,
`MSYS_NO_PATHCONV=1 $D swift:6.1 bash -c '...'`.
Set it on each Docker command only and never `export` it: with it in the environment the
Git Bash `mvn` launcher fails to start (`Could not find or load main class
...plexus.classworlds.launcher.Launcher`), so a scripted run of A4 and A5 together breaks Java and
Kotlin.

```powershell
# PowerShell, from the repository root (Docker Desktop maps file ownership itself, so no --user)
docker run --rm -v "${PWD}:/w" -w /w php:8.3-cli sh -c 'find <out>/lib -name "*.php" -print0 | xargs -0 -n1 php -l > /dev/null'
docker run --rm -v "${PWD}:/w" -w /w php:8.3-cli php <root>/samples/log_webhook.php /w/<root>/fixtures/<name>.json <secret> <sig>
docker run --rm -v "${PWD}:/w" -w /w ruby:3.4-slim ruby <root>/samples/log_webhook.rb /w/<root>/fixtures/<name>.json <secret> <sig>
docker run --rm -v "${PWD}:/w" -w /w swift:6.1 bash -c 'swift build --package-path <root> && for f in <root>/fixtures/*.json <root>/fixtures/negative/*.json; do echo "--- $f"; swift run --skip-build --package-path <root> WebhookLogger "/w/$f" "$(tr -d "\r\n" < <root>/fixtures/SECRET)" "$(tr -d "\r\n" < "${f%.json}.sig")"; echo "exit $?"; done'
```

The Swift command reads the secret and each signature inside the container, so it runs as it
stands. It is one single-quoted argument in both shells, so `$f` and `$(tr ...)` are left to
`bash` inside the container.

- `-v $PWD:/w` mounts a path of the machine the Docker **daemon** runs on. With `DOCKER_HOST`
  pointing at a remote daemon, the mount works only when the repository exists at that same path on
  that host, so sync the tree there first and run the commands on that host (over `ssh`, with the
  POSIX form above); a mount from Windows fails there with `invalid mode: /w`.
- The images: `php:8.3-cli`, `ruby:3.4-slim`, `swift:6.1`. The Swift package needs network access
  for swift-crypto on its first build.

### A6. Pack the TypeScript package

The counterpart of the `npm pack` that the `TypeScript` command of `ASC.Api.Documentation` runs for
the API SDK (`GenerateTypeScriptSdkCommand`), which leaves `onlyoffice-docspace-api-sdk-<ver>.tgz`
committed in the root of `sdk/docspace-api-sdk-typescript`. The webhook package goes next to it.
Run this step whenever `typescript` was generated and A4 built it; skip it when the build failed or
verification was skipped, and say so in the report.

Remove the previous archive first, so that a version bump does not leave the old one behind, then
pack into the repository root. `npm pack` runs the package's `prepare` (`tsc`) over the
`node_modules` that A4 installed with `npm ci`.

```bash
# POSIX shell
rm -f sdk/docspace-api-sdk-typescript/onlyoffice-docspace-webhooks-sdk-*.tgz
npm pack ./sdk/docspace-api-sdk-typescript/webhooks --pack-destination sdk/docspace-api-sdk-typescript >> <out>/.verify.log 2>&1
```

```powershell
# PowerShell
Remove-Item sdk/docspace-api-sdk-typescript/onlyoffice-docspace-webhooks-sdk-*.tgz -Force -ErrorAction SilentlyContinue
npm pack ./sdk/docspace-api-sdk-typescript/webhooks --pack-destination sdk/docspace-api-sdk-typescript >> <out>/.verify.log 2>&1
```

Keep the `./` in the first argument (see Stage B). A non-zero exit is **PACK FAILED — see
`<out>/.verify.log`**. The archive is `onlyoffice-docspace-webhooks-sdk-<version>.tgz`, the version
from `<root>/package.json`; it is committed in the TypeScript SDK repository together with the
regenerated `<root>`, like the API SDK's archive next to it.

## Stage B — package check (on request)

Stage A proves the models compile and parse. It does not prove the package a user installs
is right: its name, its dependencies, what it ships. Where each language's package metadata comes
from — and therefore where a fix belongs:

| Language | Package file | Owner |
| --- | --- | --- |
| C# | `<root>/src/DocSpace.Webhooks.SDK/DocSpace.Webhooks.SDK.csproj` | hand-written |
| Go | `<out>/go.mod`, `go.sum` | `<tools>/templates/go/go.mod.mustache` (the generator's own template with the `go` directive raised; re-copy it from the jar when the pinned generator version changes) + `supportingFiles` and `--git-user-id/--git-repo-id` in `targets.conf`; `<out>` is `<root>`, the module root, so the import path is `github.com/ONLYOFFICE/docspace-api-sdk-go/webhooks` and a release is a tag `webhooks/v<version>` |
| Java / Kotlin | `<root>/pom.xml`, `<root>/pom.xml` | hand-written |
| PHP | `<out>/composer.json` | `<tools>/templates/php/composer.mustache` |
| Python | `<out>/pyproject.toml`, package `__init__.py` | `<tools>/templates/python/pyproject.mustache`, `__init__package.mustache` |
| Ruby | `<out>/docspace-webhooks-sdk.gemspec` | `<tools>/templates/ruby/gemspec.mustache` |
| Swift | `<root>/Package.swift` | hand-written |
| TypeScript | `<root>/package.json` | hand-written |

Name, description, author, license and repository follow the matching API SDK in
`sdk/docspace-api-sdk-*` (`webhooks` in place of `api`); `<tools>/README.md` → *Package naming* has the table.

Checks, from the repo root, each into a scratch directory outside the repo:

```bash
dotnet pack sdk/docspace-api-sdk-csharp/webhooks/src/DocSpace.Webhooks.SDK -o <scratch>  # nuspec id/version/license; only lib/ inside
mvn -f sdk/docspace-api-sdk-java/webhooks/pom.xml -q -DskipTests package                   # jar name = README coordinates, no samples/ classes
mvn -f sdk/docspace-api-sdk-kotlin/webhooks/pom.xml -q -DskipTests package
python -m pip wheel --no-deps -w <scratch> sdk/docspace-api-sdk-python/webhooks/generated  # then install into a fresh venv and import
npm pack --dry-run ./sdk/docspace-api-sdk-typescript/webhooks                             # only dist/src and dist/generated
```

Keep the `./` in the `npm pack` argument: without it npm reads the path as a GitHub `user/repo` and
packs something else.

For Go, the sample module (`<root>/samples`, with a `replace` onto `../generated`) is the consumer test,
and stage A's verify already runs it. PHP has no `composer` check unless Composer is installed —
then `composer validate <out>/composer.json`.

Every moved package lives in its API SDK's repository but is a package of its own, with its own
manifest in `<root>`: Maven's `pom.xml`, Python's `<out>/pyproject.toml`, Ruby's gemspec. The C# one is not in `DocSpace.API.SDK.sln`,
so the repository's build workflow does not build it. The TypeScript one is kept
out of the API SDK's own build by `"webhooks"` in the `exclude` of its `tsconfig.json`, which comes
from the `tsconfig.mustache` template of `ASC.Api.Documentation`; without it `npm pack` of the API
SDK would compile `webhooks/` as well.

## Reporting the result

- Per language: generated / built / each fixture ok — or exactly which step failed and the log path.
- The `NOT VERIFIED` list, verbatim.
- For TypeScript, the path of the packed `onlyoffice-docspace-webhooks-sdk-<version>.tgz` (A6), or
  why it was not packed; `git -C sdk/docspace-api-sdk-typescript status --short` shows it.
- `git -C <root> status --short .` for every generated language, and
  `git -C sdk/docspace-api-spec status --short webhooks` -- the generated diff, which belongs in the
  commit. Every `<root>` and the published contract are committed in **their** repositories (the API
  SDK submodules and `sdk/docspace-api-spec`), and the server then bumps the submodule pointers; the
  fixtures are files of this repository.
- Whether the contract was changed, and if it was behind the code but the user chose not to update
  it, say plainly that the generated SDKs describe the old payload.

## Prerequisites

A JDK (the generator is Java), `openapi-generator-cli` (`npm i -g @openapitools/openapi-generator-cli`),
and `python` (or `python3`) for the trigger tables. No bash. Verification needs each language's
toolchain: `dotnet`, `go`, `mvn`, `php`, `python`, `ruby`, `swift`, `npm`. A missing one is
reported as not verified, not as a failure.

The generator version is pinned by `<tools>/openapitools.json`, passed with `--openapitools` on every
call. Check the install the same way: `openapi-generator-cli --openapitools <tools>/openapitools.json
version`. Without the flag the CLI writes a stray `openapitools.json` into the current directory.

## Failure modes

- `BUILD FAILED` in Go with `missing go.sum entry` or `updates to go.mod needed` — the sample module
  `<root>/samples` is out of step with `<out>/go.mod`; run `go -C <root>/samples mod tidy`, never in
  the module root (its `go.mod` is regenerated).
- A language builds but every fixture fails to parse — the contract declares something the wire does
  not carry (a `required` field that `WhenWritingDefault` omits, an enum that gained a value, a
  number format the language cannot map). Fix the contract, not the generated model. The README
  lists the past cases (`webhook.target.type` as a free string, `usedSpace` as `format: double`).
- Go does not compile because the field `HasAvatar` collides with the generated presence method
  `HasAvatar()` — the `--name-mappings hasAvatar=HasAvatarFlag` flag was lost from the `go` line of
  `targets.conf`.
- Python, PHP or Kotlin silently lose the entry id (`EntryId` `oneOf`) — the `--schema-mappings`
  in `targets.conf` were changed; see README, *oneOf support varies sharply by generator*.
- Trigger tables (`Triggers.cs`, `triggers.ts`, `JSON.java`, `WebhookTriggers.java`,
  `WebhookTriggers.kt`, `triggers.go`, `triggers.py`, `WebhookTriggers.php`, `webhook_triggers.rb`,
  `WebhookTriggers.swift`, `TRIGGERS.md`, `WebhookRouter.Triggers.cs`) did not pick up a new trigger — the contract's
  `x-docspace-trigger-payloads` was not updated, or the post-step was skipped (no `python`). A
  skipped post-step also breaks the build: every sample imports its language's table.
- `FAILED (wrong model)` for one language only — its sample stopped going through the generated
  table. `unknown` where a model was expected for every language — the fixture's trigger is missing
  from the contract map.
- Every Java/Kotlin sample run fails with an unknown lifecycle phase `.args=…` — the `-Dexec.args`
  argument lost its quotes in PowerShell.
- A file from an older generator version survives in `<out>` — the wipe in A1 stopped at a
  locked file. Close whatever holds it and wipe again; never generate over leftovers.

Known-harmless noise:

- `warning: in the working copy of '…/.openapi-generator/FILES', CRLF will be replaced by LF`, or
  the nine manifests listed as modified with an empty diff — the generator's manifest written with
  CRLF on Windows, when the restore at the top of A3 was skipped. The repository's
  `* text=auto eol=lf` stores it as LF either way; run that restore.
- Generator warnings about unused models (`FileEntryPayload` is emitted but nothing points at it —
  README, *Known gaps*).
