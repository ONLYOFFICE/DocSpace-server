---
name: generate-webhook-sdk
description: "Generating the DocSpace webhook SDKs (webhooks-sdk/): checking that the hand-written contract docspace-webhooks.yaml still matches the webhook payload DTOs and triggers, running webhooks-sdk/generate.sh for the chosen languages, reading its build-and-replay verification, and optionally checking that each package still builds and installs. USE FOR: generate webhook SDK, regenerate webhook models, update webhooks-sdk, a webhook payload DTO or trigger changed and the SDKs must follow, generate.sh failed, NOT VERIFIED languages, webhook SDK packaging check. DO NOT USE FOR: the REST API SDKs in sdk/ (generate-sdk), hand-editing anything under webhooks-sdk/<lang>/generated/, changing payload DTOs or WebhookTrigger itself."
---

# Generating the webhook SDKs

This is the inbound counterpart of `generate-sdk` and shares none of its tooling. The webhook SDKs
are built by **`webhooks-sdk/generate.sh`**, which calls `openapi-generator-cli` directly; the
`ASC.Api.Documentation` tool is deliberately not involved (its joiner rewrites the REST contract on
every run and its output folders point at the API SDK submodules — `webhooks-sdk/README.md`,
*Why not the generate-sdk tool*). Never route a webhook generation through `generate-sdk`, and never
add webhook commands to that tool as a side effect.

The skill does three things:

1. **Check the contract** against the code it describes — always.
2. **Run `generate.sh`** for the requested languages — stage A.
3. **Report** what changed and how each language verified; on request, **check the packages** —
   stage B.

## Ground rules

- **The contract is hand-written and nothing checks it.** `common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml`
  is not emitted by a build. `WebhookPayloadPairingTests` checks trigger ↔ payload kind ↔ DTO in C#
  only; it never reads the YAML. So a DTO change that was not mirrored in the contract regenerates
  nine SDKs that are confidently wrong. Step 1 below exists for that reason — do not skip it because
  the user only asked to "regenerate".
- **Never edit `<lang>/generated/`.** `generate.sh` wipes it on every run. A fix belongs in the
  contract, in `generate.sh` (generator flags), in `templates/<lang>/`, in a shim (`go/shim`,
  `java/shim`, `kotlin/docs`), or in the hand-written runtime `<lang>/src/`.
- **`generated/` is committed** (see `webhooks-sdk/.gitignore`): its diff is part of the change and
  goes into the same commit as the contract change that caused it. Only `.generate.log` and
  `.verify.log` are ignored.
- **No hardcoded language list.** The targets are the rows of the `targets=(…)` array in
  `generate.sh`; the first field of each row is the name `generate.sh` accepts. Read the array before
  offering choices. Names are lowercase directory names (`csharp`, `typescript`, …), unlike
  `generate-sdk`'s command names.
- **Keep the full log.** Tee the run to a file and read a trimmed copy; report the path. `generate.sh`
  also writes `<lang>/generated/.generate.log` (generator) and `.verify.log` (build + sample runs) per
  language — those are the evidence, not the console summary.
- **Run it in bash.** `generate.sh` is a bash script. On Windows use Git Bash; it converts paths for
  the native tools itself (`cygpath`). It calls `python` (not `python3`) for `tools/gen-trigger-map.py`
  — on a Linux box without a `python` alias that step fails, and the trigger tables go stale.

## Algorithm

1. **Contract check** (below). If the contract is behind the code, say exactly what differs and ask
   whether to update the contract first (`AskUserQuestion`). Updating it is part of the job when the
   user agrees — it is the only correct place for the change.
2. **Languages** — from the request; if none named, ask (`AskUserQuestion`, multiSelect) out of the
   targets read from `generate.sh`, plus "all". A contract change normally means **all**: every
   language carries the same models.
3. **Stage A**, in the background, with verification on (the default).
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

From the repo root, in bash:

```bash
webhooks-sdk/generate.sh                     # all targets
webhooks-sdk/generate.sh csharp typescript   # a subset
VERIFY=0 webhooks-sdk/generate.sh go         # generate only — say so in the report
```

Run it in the background and tee the output (`2>&1 | tee <scratchpad>/webhook-sdk.log`). The
README's own measurement is ~90 s for all nine; first runs are longer (the generator jar, Maven and
npm downloads).

What a run does per language: wipes `<lang>/generated/` (files first, then directories — Windows
can hold a handle on the directory), runs the generator with the row's flags and templates, applies
the language's post-step (shims, `gen-trigger-map.py`), then — unless `VERIFY=0` — builds the
language and replays **every** signed fixture in `webhooks-sdk/samples/` through its sample, which
verifies the signature and parses the payload with the generated models.

Outcomes:

| Console line | Meaning |
| --- | --- |
| `models: N files` | generation succeeded (not that it compiles) |
| `FAILED -- see <lang>/generated/.generate.log` | the generator failed; nothing for that language is verified |
| `BUILD FAILED -- see <lang>/generated/.verify.log` | generated, but the language does not build — the most common real failure |
| `<fixture>: ok` / `FAILED` | the sample parsed (or did not parse) that real delivery |
| `SKIPPED -- <tool> not found`, then `NOT VERIFIED (toolchain missing): …` at the end | the toolchain is missing; the language was generated but **not** checked |

The script exits non-zero on any generator, build or fixture failure. A missing toolchain does
**not** fail it — so read the `NOT VERIFIED` line every time and report it; never present an
unverified language as working. Ruby and Swift are the usual ones; the README records that they
were checked in the `ruby:3.4-slim` and `swift:6.1` images instead.

## Stage B — package check (on request)

`generate.sh` proves the models compile and parse. It does not prove the package a user installs
is right: its name, its dependencies, what it ships. Where each language's package metadata comes
from — and therefore where a fix belongs:

| Language | Package file | Owner |
| --- | --- | --- |
| C# | `csharp/src/DocSpace.Webhooks.SDK/DocSpace.Webhooks.SDK.csproj` | hand-written |
| Go | `go/generated/go.mod`, `go.sum` | generator (`supportingFiles` + `--git-user-id/--git-repo-id`); `generated/` is the module root |
| Java / Kotlin | `java/pom.xml`, `kotlin/pom.xml` | hand-written |
| PHP | `php/generated/composer.json` | `templates/php/composer.mustache` |
| Python | `python/generated/pyproject.toml`, package `__init__.py` | `templates/python/pyproject.mustache`, `__init__package.mustache` |
| Ruby | `ruby/generated/docspace-webhooks-sdk.gemspec` | `templates/ruby/gemspec.mustache` |
| Swift | `swift/Package.swift` | hand-written |
| TypeScript | `typescript/package.json` | hand-written |

Name, description, author, license and repository follow the matching API SDK in
`sdk/docspace-api-sdk-*` (`webhooks` in place of `api`); `README.md` → *Package naming* has the table.

Checks, each into a scratch directory outside the repo:

```bash
dotnet pack webhooks-sdk/csharp/src/DocSpace.Webhooks.SDK -o <scratch>            # nuspec id/version/license; only lib/ inside
(cd webhooks-sdk/java && mvn -q -DskipTests package)                              # jar name = README coordinates, no samples/ classes
(cd webhooks-sdk/kotlin && mvn -q -DskipTests package)
python -m pip wheel --no-deps -w <scratch> webhooks-sdk/python/generated          # then install into a fresh venv and import
(cd webhooks-sdk/typescript && npm pack --dry-run)                                # only dist/src and dist/generated
```

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

`bash`, a JDK (the generator is Java), `openapi-generator-cli` (`npm i -g @openapitools/openapi-generator-cli`),
and `python` for the trigger tables. Verification needs each language's toolchain: `dotnet`, `go`,
`mvn`, `php`, `python`, `ruby`, `swift`, `npm` — missing ones are skipped, not failed.

The generator version is pinned by `webhooks-sdk/openapitools.json`; `generate.sh` `cd`s there so
that pin applies. Do not run `openapi-generator-cli version` from the repo root to check the
install — it writes a stray `openapitools.json` into the current directory. Run it from
`webhooks-sdk/`.

## Failure modes

- `BUILD FAILED` in Go with `missing go.sum entry` or `updates to go.mod needed` — the sample module
  `go/samples` is out of step with `generated/go.mod`; run `go mod tidy` in `go/samples`, never in
  `generated/` (it is regenerated).
- A language builds but every fixture fails to parse — the contract declares something the wire does
  not carry (a `required` field that `WhenWritingDefault` omits, an enum that gained a value, a
  number format the language cannot map). Fix the contract, not the generated model. The README
  lists the past cases (`webhook.target.type` as a free string, `usedSpace` as `format: double`).
- Go does not compile because the field `HasAvatar` collides with the generated presence method
  `HasAvatar()` — the `--name-mappings hasAvatar=HasAvatarFlag` flag was lost from the Go row.
- Python, PHP or Kotlin silently lose the entry id (`EntryId` `oneOf`) — the `--schema-mappings`
  rows were changed; see README, *oneOf support varies sharply by generator*.
- Trigger tables (`Triggers.cs`, `triggers.ts`, `JSON.java`, `TRIGGERS.md`) did not pick up a new
  trigger — the contract's `x-docspace-trigger-payloads` was not updated, or `python` was not found.

Known-harmless noise:

- `warning: in the working copy of '…/.openapi-generator/FILES', CRLF will be replaced by LF` — the
  generator's file manifest on Windows; git normalizes it.
- Generator warnings about unused models (`FileEntryPayload` is emitted but nothing points at it —
  README, *Known gaps*).
