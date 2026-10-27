---
name: generate-webhook-sdk
description: "Generating the DocSpace webhook SDKs (webhooks-sdk/): checking the contract docspace-webhooks.yaml against the webhook payload DTOs and triggers and, after asking the user, regenerating it (common/Tools/ASC.Webhooks.Contract), running openapi-generator-cli per language from the skill's targets.conf with its post-steps, building each language and replaying the signed fixtures through its sample, and optionally checking that each package still builds and installs. USE FOR: generate webhook SDK, regenerate webhook models, update webhooks-sdk, a webhook payload DTO or trigger changed and the SDKs must follow, NOT VERIFIED languages, webhook SDK packaging check, how to regenerate the webhook SDKs by hand. DO NOT USE FOR: the REST API SDKs in sdk/ (generate-sdk), hand-editing anything under webhooks-sdk/<lang>/generated/, changing payload DTOs or WebhookTrigger itself."
---

# Generating the webhook SDKs

This is the inbound counterpart of `generate-sdk` and shares none of its tooling. The webhook SDKs
are built by calling `openapi-generator-cli` directly, one language at a time; the
`ASC.Api.Documentation` tool is deliberately not involved (its joiner rewrites the REST contract on
every run and its output folders point at the API SDK submodules — `webhooks-sdk/README.md`,
*Why not the generate-sdk tool*). Never route a webhook generation through `generate-sdk`, and never
add webhook commands to that tool as a side effect.

The skill does three things:

1. **Check the contract** against the code it describes — always — and **regenerate it** when the user says so.
2. **Generate and verify** the requested languages, step by step, with no shell script — stage A.
3. **Report** what changed and how each language verified; on request, **check the packages** —
   stage B.

## Ground rules

- **The contract is generated, never edited.** `common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml`
  is written by `common/Tools/ASC.Webhooks.Contract` from the payload DTOs, the envelope,
  `WebhookTrigger` and the hand-written `docspace-webhooks.header.yaml` next to it. Nothing
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
4. **Report**: per-language verify outcome, `NOT VERIFIED` languages, the `git status` of
   `webhooks-sdk/`, and anything the contract check found.
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
- **up to date** — recommend no: regenerating writes the same file. Still ask; the user may have
  edited `docspace-webhooks.header.yaml` or want the run as proof.

On yes:

```bash
dotnet run --project common/Tools/ASC.Webhooks.Contract
git diff -- common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml
```

On no, the SDKs are generated from the committed file as it is. Where each part of the contract
comes from — and therefore where a change to it belongs:

| Contract part | Source |
| --- | --- |
| `components.schemas.*Payload` (user, group, file, folder, room, form submit, summaries) | `common/ASC.Api.Core/Webhook/Payloads/*WebhookDto.cs` (user, group) and `products/ASC.Files/Core/ApiModels/WebhookDto/*.cs` (files, folders, rooms, forms) |
| the envelope (`WebhookEnvelope`, `WebhookEventInfo`, `WebhookConfigInfo`, `WebhookTargetInfo`) | `common/ASC.Webhooks.Core/WebhookPayload.cs` |
| a schema `description` | the type's `<summary>` and `<remarks>` |
| a property `description` | the property's `<summary>`; for an enum the generator appends its members |
| `x-docspace-trigger-payloads.map` | the trigger table in `WebhookContractGenerator` (by trigger family, with per-trigger exceptions), in `[Order]` order |
| `info`, the transport and serialization notes, `EntryId`, the map's description | `common/ASC.Webhooks.Core/Contract/docspace-webhooks.header.yaml` |

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

The signed deliveries in `webhooks-sdk/samples/` are captured from a running server by
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
git status --short webhooks-sdk/samples
```

The test needs the integration-test stack (Docker, the Aspire host); where tests run on another
machine, run it there and bring back `webhooks-sdk/samples/`. `unknown.trigger.json`,
`form.submit.json` and `samples/negative/` are written by hand and left alone; their `.sig` files
are re-signed only when their body changes.

## Stage A — generate and verify

**Working directory: `webhooks-sdk/`** for every command in this stage. Every path below is relative
to it. That is also what makes the generator version pinned in `webhooks-sdk/openapitools.json`
apply. Fixture files are the one exception: pass them as **absolute** paths, because `go -C` and
`dotnet run --project` resolve a relative path against a different directory. In a POSIX shell on
Windows (Git Bash, MSYS) write that path with `cygpath -m "$PWD/samples/<name>.json"`
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
`pip install pyyaml` — A1 would otherwise wipe `generated/` and leave it without its trigger tables.

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

Every language has one. The trigger -> payload model tables are derived from the contract by
`tools/gen-trigger-map.py`, never hand-maintained, and every sample picks its payload model through
its language's table. It is invoked as `python` here; on a box where only `python3` exists, use that.
A0 has already checked that it and PyYAML are there.

First, for **every** language: put back the generator's file manifest when only its line endings
changed. On Windows the generator writes `<lang>/generated/.openapi-generator/FILES` with CRLF; git
then warns `CRLF will be replaced by LF` on every diff, and depending on `core.autocrlf` lists the
nine manifests as modified although their content is the same -- rewriting the file with LF does
not help, git still sees a changed file it cannot tell apart. Restoring it from git does: it leaves
an LF file and a clean status. A manifest whose content really changed (a model added or removed)
fails the comparison and stays as generated.

```bash
# POSIX shell, from webhooks-sdk/
for f in */generated/.openapi-generator/FILES; do git diff --ignore-cr-at-eol --quiet -- "$f" && git checkout -- "$f"; done
```

```powershell
# PowerShell, from webhooks-sdk/
Get-ChildItem */generated/.openapi-generator/FILES | ForEach-Object { git diff --ignore-cr-at-eol --quiet -- $_.FullName; if ($LASTEXITCODE -eq 0) { git checkout -- $_.FullName } }
```

`git diff` and `git checkout` take paths relative to the current directory, so these run from
`webhooks-sdk/` like the rest of stage A.

Then, per language (`<spec>` is `../common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml`):

| Language | Post-step |
| --- | --- |
| `java` | copy the contents of `java/shim/` into `java/generated/src/main/java/` (merging directories); then `python tools/gen-trigger-map.py --lang java --spec <spec> --package com.onlyoffice.docspace.webhooks.sdk --model-package com.onlyoffice.docspace.webhooks.sdk.model --out java/generated/src/main/java/com/onlyoffice/docspace/webhooks/sdk/JSON.java`; then `python tools/gen-trigger-map.py --lang java-triggers --spec <spec> --package com.onlyoffice.docspace.webhooks.sdk --model-package com.onlyoffice.docspace.webhooks.sdk.model --out java/generated/src/main/java/com/onlyoffice/docspace/webhooks/sdk/WebhookTriggers.java` |
| `kotlin` | copy `kotlin/docs/*.md` into `kotlin/generated/docs/`; then `python tools/gen-trigger-map.py --lang kotlin --spec <spec> --package onlyoffice.docspace.webhooks.sdk --model-package onlyoffice.docspace.webhooks.sdk.models --out kotlin/generated/src/main/kotlin/onlyoffice/docspace/webhooks/sdk/WebhookTriggers.kt` |
| `go` | copy `go/shim/*.go` into `go/generated/`; then `python tools/gen-trigger-map.py --lang go --spec <spec> --package docspace_webhooks_sdk --out go/generated/triggers.go` |
| `csharp` | `python tools/gen-trigger-map.py --lang csharp --spec <spec> --package DocSpace.Webhooks.SDK --model-package DocSpace.Webhooks.SDK.Model --out csharp/generated/src/DocSpace.Webhooks.SDK/Triggers.cs`; then `python tools/gen-trigger-map.py --lang markdown --spec <spec> --doc-path docs/ --out csharp/generated/TRIGGERS.md` |
| `php` | `python tools/gen-trigger-map.py --lang php --spec <spec> --package "OnlyOffice\DocSpace\Webhooks\Sdk" --model-package "OnlyOffice\DocSpace\Webhooks\Sdk\Model" --out php/generated/lib/WebhookTriggers.php` |
| `python` | `python tools/gen-trigger-map.py --lang python --spec <spec> --package docspace_webhooks_sdk --out python/generated/docspace_webhooks_sdk/triggers.py` |
| `ruby` | `python tools/gen-trigger-map.py --lang ruby --spec <spec> --package DocspaceWebhooksSdk --out ruby/generated/lib/docspace-webhooks-sdk/webhook_triggers.rb` (the gem entry point from `templates/ruby/gem.mustache` requires it) |
| `swift` | `python tools/gen-trigger-map.py --lang swift --spec <spec> --out swift/generated/Sources/DocSpaceWebhooksSDK/WebhookTriggers.swift` |
| `typescript` | `python tools/gen-trigger-map.py --lang typescript --spec <spec> --out typescript/generated/triggers.ts` |

The PHP namespaces are quoted: both shells keep backslashes inside double quotes.

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
generated models, which is exactly what a receiver does. It must also pick the right model and refuse
what a receiver must refuse, so the run checks both: the model each sample reports, and its exit
code on the negative fixtures in `samples/negative/`.

Append the output of every command for a language to `<lang>/generated/.verify.log`
(`>> <lang>/generated/.verify.log 2>&1`, the same in both shells), starting from an empty file. A
run that generated the language has one already (A1 wiped the directory); a verify-only run empties
it first:

```bash
: > <lang>/generated/.verify.log            # POSIX shell
```

```powershell
Set-Content <lang>/generated/.verify.log '' # PowerShell
```

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
| `typescript` | `npm` | if `typescript/node_modules` is missing: `npm --prefix typescript ci --silent`; then `npm --prefix typescript run -s build` | `node typescript/dist/samples/log-webhook.js <body> <secret> <sig>` |

- The `-Dexec.args=…` argument is quoted as a whole on purpose: PowerShell splits an unquoted `-D`
  argument at the first dot.
- `typescript/package-lock.json` is committed, so `npm ci` installs exactly the locked versions. A
  failing `npm ci` is **BUILD FAILED**: never fall back to `npm install`, which resolves the ranges
  afresh and would verify the SDK against dependencies nobody locked.
- Fixtures: Glob `samples/*.json`. `<body>` is the fixture's **absolute** path (`cygpath -m` in a
  POSIX shell on Windows, see above). `<sig>` is the content of the `.sig` file with the same name
  (`sha256=…`). `<secret>` is the content of `samples/SECRET`. Read both files and pass the values
  trimmed. A fixture whose sample exits non-zero is **FAILED**.
- **The model check.** Every sample prints one line `model: <name>`: the payload model it parsed
  with, as the contract names the schema (`FilePayload`, `UserPayload`, …), `untyped` for the
  catch-all `*`, and `unknown` for a trigger the contract does not list. The expected name comes
  from the contract's `x-docspace-trigger-payloads` map, read with a one-liner (run from
  `webhooks-sdk/`, the same in both shells):

  ```
  python -c "import json,sys,yaml; m=yaml.safe_load(open(sys.argv[1],encoding='utf-8'))['x-docspace-trigger-payloads']['map']; t=json.load(open(sys.argv[2],encoding='utf-8'))['event'].get('trigger'); print((m[t] or 'untyped') if t in m else 'unknown')" ../common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml samples/<name>.json
  ```

  A run whose output has no line that, trimmed, equals `model: <expected>` is **FAILED (wrong
  model)**, even with exit code 0: the sample parsed the delivery with a model the contract does not
  give its trigger (a hand-written prefix rule that drifted, an unknown trigger read as a file).
- **The negative fixtures.** Glob `samples/negative/*.json` and run each sample over them exactly as
  over the positive ones (their `.sig` sits next to them; `samples/negative/README.md` says what is
  wrong with each). Every sample must exit **non-zero** on each of them; one that exits 0 is
  **FAILED (accepted a negative fixture)** — it would accept a forged or broken delivery, and the
  positive run cannot show that. The positive Glob `samples/*.json` does not descend into the
  subfolder, so the two sets never mix.
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
| `<fixture>: FAILED (wrong model)` | it parsed, but with another model than the contract gives its trigger |
| `negative/<fixture>: ok` / `FAILED (accepted a negative fixture)` | the sample refused (or accepted) a delivery it must refuse |
| `NOT VERIFIED (toolchain missing): …` | the language was generated but **not** checked |

The run has failed if any generator, build or fixture failed. A missing toolchain does **not** fail
it. So collect the `NOT VERIFIED` list every time and report it.

### A5. Docker fallback for PHP, Ruby and Swift

PHP, Ruby and Swift are the toolchains a machine most often lacks. When Docker is available, verify
them in a container instead of reporting them `NOT VERIFIED`: the same build and sample commands,
over the same fixtures, with `webhooks-sdk/` mounted at `/w`. Inside the container a fixture is
`/w/samples/<name>.json` (`/w/samples/negative/<name>.json`), not the host path; secret, signature
and the model check are as in A4. A language verified this way counts as verified; the report says
so, with the image and the Docker host it ran on.

```bash
# POSIX shell on Linux or macOS, from webhooks-sdk/; --user keeps the files the build writes owned by you
D="docker run --rm --user $(id -u):$(id -g) -e HOME=/tmp -v $PWD:/w -w /w"
$D php:8.3-cli sh -c 'find php/generated/lib -name "*.php" -print0 | xargs -0 -n1 php -l > /dev/null'
$D php:8.3-cli php php/samples/log_webhook.php /w/samples/<name>.json <secret> <sig>
$D ruby:3.4-slim ruby ruby/samples/log_webhook.rb /w/samples/<name>.json <secret> <sig>
# Swift: build once, then run the built sample per fixture in the same container -- a fresh container
# per fixture would resolve swift-crypto and compile from scratch each time
$D swift:6.1 bash -c 'swift build --package-path swift && for f in samples/*.json samples/negative/*.json; do echo "--- $f"; swift run --skip-build --package-path swift WebhookLogger "/w/$f" "<secret>" "$(cat "${f%.json}.sig")"; echo "exit $?"; done'
```

In Git Bash (MSYS) on Windows the lines above do not work as they are: MSYS rewrites every argument
that looks like a POSIX path for the native `docker.exe`, so `-v $PWD:/w`, `-w /w` and
`/w/samples/...` reach Docker as Windows paths. Switch the rewriting off for these commands and give
the mount a Windows source path (`cygpath -m`, `W:/Work/...`); Docker Desktop maps file ownership
itself, so drop `--user` too. This is for a local Docker Desktop only: with `DOCKER_HOST` set to a
remote daemon the Windows path means nothing to it (`invalid mode: /w`), see the note below.

```bash
# Git Bash on Windows with a local Docker Desktop, from webhooks-sdk/
D="docker run --rm -v $(cygpath -m "$PWD"):/w -w /w"
MSYS_NO_PATHCONV=1 $D php:8.3-cli php php/samples/log_webhook.php /w/samples/<name>.json <secret> <sig>
```

The same prefix goes before every `$D` line: `MSYS_NO_PATHCONV=1 $D ruby:3.4-slim ...`,
`MSYS_NO_PATHCONV=1 $D swift:6.1 bash -c '...'`.

```powershell
# PowerShell, from webhooks-sdk/ (Docker Desktop maps file ownership itself, so no --user)
docker run --rm -v "${PWD}:/w" -w /w php:8.3-cli sh -c 'find php/generated/lib -name "*.php" -print0 | xargs -0 -n1 php -l > /dev/null'
docker run --rm -v "${PWD}:/w" -w /w php:8.3-cli php php/samples/log_webhook.php /w/samples/<name>.json <secret> <sig>
docker run --rm -v "${PWD}:/w" -w /w ruby:3.4-slim ruby ruby/samples/log_webhook.rb /w/samples/<name>.json <secret> <sig>
docker run --rm -v "${PWD}:/w" -w /w swift:6.1 bash -c 'swift build --package-path swift && for f in samples/*.json samples/negative/*.json; do echo "--- $f"; swift run --skip-build --package-path swift WebhookLogger "/w/$f" "<secret>" "$(cat "${f%.json}.sig")"; echo "exit $?"; done'
```

The Swift command is one single-quoted argument in both shells, so `$f` and `$(cat ...)` are left to
`bash` inside the container.

- `-v $PWD:/w` mounts a path of the machine the Docker **daemon** runs on. With `DOCKER_HOST`
  pointing at a remote daemon, the mount works only when `webhooks-sdk/` exists at that same path on
  that host, so sync the tree there first and run the commands on that host (over `ssh`, with the
  POSIX form above); a mount from Windows fails there with `invalid mode: /w`.
- The images: `php:8.3-cli`, `ruby:3.4-slim`, `swift:6.1`. The Swift package needs network access
  for swift-crypto on its first build.

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
- Trigger tables (`Triggers.cs`, `triggers.ts`, `JSON.java`, `WebhookTriggers.java`,
  `WebhookTriggers.kt`, `triggers.go`, `triggers.py`, `WebhookTriggers.php`, `webhook_triggers.rb`,
  `WebhookTriggers.swift`, `TRIGGERS.md`) did not pick up a new trigger — the contract's
  `x-docspace-trigger-payloads` was not updated, or the post-step was skipped (no `python`). A
  skipped post-step also breaks the build: every sample imports its language's table.
- `FAILED (wrong model)` for one language only — its sample stopped going through the generated
  table. `unknown` where a model was expected for every language — the fixture's trigger is missing
  from the contract map.
- Every Java/Kotlin sample run fails with an unknown lifecycle phase `.args=…` — the `-Dexec.args`
  argument lost its quotes in PowerShell.
- A file from an older generator version survives in `generated/` — the wipe in A1 stopped at a
  locked file. Close whatever holds it and wipe again; never generate over leftovers.

Known-harmless noise:

- `warning: in the working copy of '…/.openapi-generator/FILES', CRLF will be replaced by LF`, or
  the nine manifests listed as modified with an empty diff — the generator's manifest written with
  CRLF on Windows, when the restore at the top of A3 was skipped. The repository's
  `* text=auto eol=lf` stores it as LF either way; run that restore.
- Generator warnings about unused models (`FileEntryPayload` is emitted but nothing points at it —
  README, *Known gaps*).
