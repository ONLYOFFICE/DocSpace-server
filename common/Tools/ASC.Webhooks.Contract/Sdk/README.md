# DocSpace Webhooks SDK

Client-side libraries for **receiving** DocSpace webhooks: payload models for every
trigger, plus (in progress) a runtime that verifies the signature and narrows the
payload to the right type.

## Layout

Each language's webhook SDK lives in the repository of that language's API SDK, as a
`webhooks/` folder next to the API SDK, and is published as a package of its own. This folder holds
what turns the contract into those SDKs.

```
sdk/docspace-api-spec/webhooks/
└── docspace-webhooks.yaml   the published contract the SDKs are generated from
web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks/Fixtures/
├── *.json, *.sig, SECRET    source of the fixtures: deliveries, their signatures, the key
└── negative/                deliveries every sample must refuse
sdk/docspace-api-sdk-<language>/webhooks/   the webhook SDK of every language
common/Tools/ASC.Webhooks.Contract/Sdk/
├── openapitools.json        pins the generator (7.25.0), passed with --openapitools
├── fixtures-README.md       the README of every SDK's copy of the fixtures
├── templates/<language>/    generator template overrides
└── gen-trigger-map.py       emits the trigger→payload table for every language
```

Inside every language's folder:

```
├── generated/       openapi-generator output — DO NOT EDIT, wiped on regen
│   └── docs/        per-model Markdown reference, same as the API SDKs
├── src/             hand-written runtime — signature verification, parsing
├── fixtures/        copy of the shared fixtures — DO NOT EDIT, rewritten on regen
└── samples/         runnable examples (C#: samples/WebhookLogger)
```

Go is the exception: its module sits at the root of `webhooks/`, so that the import path is
`github.com/ONLYOFFICE/docspace-api-sdk-go/webhooks` and a release is a tag `webhooks/v<version>`.
The generator writes there directly, the hand-written router lives in `_shim/` (Go ignores a
directory starting with `_`), and the sample is a module of its own in `samples/`.

### Publishing

Go, Java, Kotlin, Python, Ruby, C# and TypeScript publish from the subfolder: each package manager
takes a manifest that is not at the root of the repository (a nested Go module, `pom.xml`,
`pyproject.toml`, a gemspec, a `.csproj`, `package.json`). Packagist and the Swift Package Manager
do not: both read `composer.json` / `Package.swift` from the root of a repository only. PHP and
Swift therefore publish through read-only mirrors, `docspace-webhooks-sdk-php` and
`docspace-webhooks-sdk-swift`, filled by `.github/workflows/webhooks-mirror.yml` in their API SDK
repositories on a tag `webhooks/v<version>` (a draft until the mirrors and their token exist).
A second product in the API SDK's own `Package.swift` was the alternative for Swift; it would make
every API SDK user resolve swift-crypto and hold the webhook SDK to the API SDK's platform floor.

In every API SDK repository the tag workflow (`create-tag.yml`) ignores `webhooks/**`, so a change
to the webhook SDK alone does not try to tag a new API SDK release.

The fixtures have one source, `web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks/Fixtures`, where the server suite that captures
them writes them and where the hand-written ones are changed. Every SDK carries a committed copy in
`webhooks/fixtures/`, written by the skill on regeneration, so that its sample and tests run from a
clone of that repository alone. The copy is never edited; its README says where the source is.

### Markdown reference

C# additionally gets `generated/README.md` — the index the model docs link back
to. Without it every `[[Back to Model list]]` footer is a dead link, which is
what models-only output leaves you with by default. It comes from
`templates/csharp/README.mustache` rather than the stock template, because the
generated one advertises an HTTP client this package does not contain: it tells
the reader to `Install-Package JsonSubTypes`, documents an empty endpoints
table, and shows `ApiClient` usage. Only that one file is overridden; the
generator falls back to its built-ins for the model docs.

Emitting it costs nothing extra: `supportingFiles=README.md` names the single
supporting file to produce, so none of the client machinery comes with it.
The other eight languages still have no index — same one-line fix when wanted.

Each language gets `generated/docs/<Model>.md` — a title, the schema
description and a properties table — exactly like the `docs/` folders in the
`sdk/docspace-api-sdk-*` submodules. Nothing in that pipeline switches those on:
they are openapi-generator's default, and models-only output keeps them so long
as `modelDocs` is not turned off. Descriptions written in the contract are what
lands in the tables, so the contract is the only place to edit them.

### Package READMEs

Every language emits `generated/README.md` — the package landing page, in the
same shape as the `sdk/docspace-api-sdk-*` ones: title, intro, Installation,
Usage, Getting Started, then `Documentation for …` sections. Two of those are
renamed for an inbound library: *Authorization* becomes **Verification**, and
*API Endpoints* becomes **Events**, since this SDK receives rather than calls.

Each comes from `templates/<lang>/README.mustache`. Seven languages simply
override the generator's own README; TypeScript has none to override, so it is
declared through `templates/typescript/files.yaml` instead.

Every Getting Started sample was extracted from the generated README and
checked: C#, Go, Java and Kotlin compile, Python and PHP pass a syntax check,
and the C# one was additionally run against the shared fixture end to end. The
Ruby and Swift snippets have not been extracted and checked this way; the full
samples in `ruby/samples` and `swift/` have (see *Samples*).

A caveat found while writing them: `packageName` is not the variable that names
the package everywhere. Java uses `artifactId`, Ruby `gemName`, Swift
`projectName`, Python `projectName` for the pip name. Guessing wrong renders an
empty heading, so probe before adding a language.

### Custom Markdown pages

Beyond the per-model docs, a language can ship hand-written pages. C# has two,
and is the only one that needs them — its pages are the generic webhook
documentation; the other eight get a single self-contained README:

- `RECEIVING.md` — verification, the unsigned pre-filter headers, parsing, the
  retry budget and deduplication. Written as `templates/csharp/RECEIVING.mustache`,
  so `{{packageName}}` and the model list are interpolated.
- `TRIGGERS.md` — every event you can subscribe to and the payload it
  delivers, grouped by subject. Written for the public documentation site, so
  it carries no generator or server-internal detail.

The two use different mechanisms, for a reason:

**`RECEIVING.md` is native.** openapi-generator takes extra output files only
from a config file — there is no command-line equivalent — so regeneration
passes `-c templates/<lang>/files.yaml` whenever that file exists. Two traps,
both hit while wiring this up:

1. A file declared in `files:` is *still* filtered by the `supportingFiles`
   allowlist. Not named there, and it is silently skipped.
2. That allowlist separates entries with a **colon**, because commas already
   separate the top-level global properties:
   `--global-property "models,supportingFiles=README.md:RECEIVING.md"`.

**`TRIGGERS.md` cannot be.** It is built from `x-docspace-trigger-payloads`, a
root-level vendor extension, and mustache templates only ever see the model and
operation trees. So `tools/gen-trigger-map.py --lang markdown` emits it as a
post-step, the same way it emits `Triggers.cs` and Java's `JSON.java`.

To give another language its own pages: add `templates/<lang>/files.yaml` plus
the `.mustache`, set a `template-dir` on its target row, and name the output in
that target's `supportingFiles` list.

DocSpace also has a second, unrelated Markdown mechanism —
`GenerateMarkdownDocsCommand` in `ASC.Api.Documentation`, which splits the
joined spec per service, renders it through a custom `my-markdown` codegen,
slices it into one file per operation and bundles the result for the API
reference site. That one is endpoint-shaped. This contract has `paths: {}`, so
it has nothing to render and is not used here.

`generated/triggers.ts` is derived from the contract's
`x-docspace-trigger-payloads` block, never hand-written: 37 triggers × 9
languages is exactly the list that rots silently. Add a trigger to
`WebhookTrigger.cs`, regenerate the contract and the SDKs, and it propagates.

Nine targets: `csharp go java kotlin php python ruby swift typescript`.

### Package naming

Each target follows the same convention as the matching API SDK in `sdk/`,
substituting `webhooks` for `api`:

| Language | API SDK | This SDK | Property that controls it |
|---|---|---|---|
| C# | `DocSpace.API.SDK` | `DocSpace.Webhooks.SDK` | `packageName` |
| Go | `docspace_api_sdk` | `docspace_webhooks_sdk` | `packageName` |
| Java | `org.openapitools.client` | `com.onlyoffice.docspace.webhooks.sdk` | `modelPackage` + `invokerPackage` |
| Kotlin | `onlyoffice.docspace.api.sdk` | `onlyoffice.docspace.webhooks.sdk` | `packageName` |
| PHP | `OpenAPI\Client` | `OnlyOffice\DocSpace\Webhooks\Sdk` | `invokerPackage` |
| Python | `docspace_api_sdk` | `docspace_webhooks_sdk` | `packageName` |
| Ruby | — | `DocspaceWebhooksSdk` | `gemName` + `moduleName` |
| Swift | `OpenAPIClient` | `DocSpaceWebhooksSDK` | `projectName` |
| TypeScript | `@onlyoffice/docspace-api-sdk` | `@onlyoffice/docspace-webhooks-sdk` | `package.json` (see below) |

**Three of the API SDKs are not actually branded.** Java ships as
`org.openapitools.client`, PHP as `OpenAPI\Client` and Swift as `OpenAPIClient`,
even though `tools*.json` sets a `packageName` for each. That property does not
control the namespace for those three generators — Java needs `modelPackage`,
PHP `invokerPackage`, Swift `projectName` — so the branding silently had no
effect. Worth fixing there too; the properties above are the ones that work.

TypeScript is the exception to the table: its `npmName` property relocates the
whole output under `src/` and emits a second `package.json` we do not use, so
the package name lives in the hand-written `webhooks/package.json` instead.

The contract lives with the server code that defines it, not here:
[`common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml`](../../../ASC.Webhooks.Core/Contract/docspace-webhooks.yaml),
published to `sdk/docspace-api-spec/webhooks/` by the same run.
It is generated from the code by `common/Tools/ASC.Webhooks.Contract` (the
payload DTOs, the envelope, `WebhookTrigger` and their XML docs); what the code
cannot say is written in `ContractHeader` in that tool, with the transport
numbers and header names taken from `WebhookDelivery` in ASC.Webhooks.Core.
Never edit the YAML itself: change the code and
run `dotnet run --project common/Tools/ASC.Webhooks.Contract` (`-- --check`
only reports whether the committed file is out of date).

## Regenerating

Regeneration is driven by the `generate-webhook-sdk` Claude Code skill
([`.claude/skills/generate-webhook-sdk/`](../../../../.claude/skills/generate-webhook-sdk/SKILL.md)),
not by a script: ask it to regenerate all languages or a subset, with or without
the verification below. It runs the same steps in bash or PowerShell on any OS.
Its `SKILL.md` lists every step as a plain command, so a regeneration can also be
done by hand from there. The generator flags of each language, and why each one is
there, are in [`targets.conf`](../../../../.claude/skills/generate-webhook-sdk/targets.conf)
next to it.

Needs `openapi-generator-cli` (`npm i -g @openapitools/openapi-generator-cli`), a
JDK, and `python` with PyYAML (`pip install pyyaml`) for the trigger tables; the
skill checks for both before it wipes anything. Generating all nine takes ~25 s;
with verification the run takes four to five minutes, most of it Maven. `generated/` is
wiped and rewritten; `src/` is never touched.

After generating, each language is built and its sample is run over every
fixture in `samples/` (see *Samples*); the outcome is in
`<lang>/generated/.verify.log`, and any failure fails the run.
A clean generation proves nothing on its own: every SDK here once generated
without a warning and still failed to compile or to parse a real delivery.

## Why not the generate-sdk tool

`common/Tools/ASC.Api.Documentation` is the documented pipeline for the REST API
SDKs, and it deliberately is **not** used here. Three reasons, all structural:

1. Its joiner runs once per process *before arguments are parsed*, so every
   invocation rewrites `SDK/json/api-docs.json` and the published contract inside
   the `sdk/docspace-api-spec` submodule — whatever you asked it to generate.
2. Each language's destination is a hardcoded `outputFolder` in
   `My<Lang>ClientCodegen.java` pointing at the API SDK submodules. Generating
   webhooks through those commands would overwrite the API SDKs.
3. It takes its input from the `join` set in `appsettings.json`. It has no concept
   of a second spec.

The tool's own rule against calling `openapi-generator-cli` directly exists because
its API commands do post-generation work (build a `.nupkg`, run `npm pack`). A
models-only webhook generation has none of that, and is not a command the tool
offers. If webhook SDKs ever need packaging and publishing, the right move is to add
first-class commands to that tool — not to wire packaging into the webhook regeneration.

## The payloads are webhook-owned DTOs

Each trigger carries a DTO that belongs to the webhook contract:

| Trigger family | Payload | Built from |
|---|---|---|
| `user.*` | `UserPayload` | `UserWebhookDto` |
| `group.*` | `GroupPayload` | `GroupWebhookDto` |
| `file.*`, `form.filled.out`, `form.stopped` | `FilePayload` | `FileWebhookDto<T>` |
| `folder.*` | `FolderPayload` | `FolderWebhookDto<T>` |
| `room.*`, `agent.*` | `RoomPayload` | `RoomWebhookDto<T>` |
| `form.submit` | `FormSubmitPayload` | `FormSubmitWebhookDto<T>` |

The pairing above is declared once, in the trigger table of
`common/Tools/ASC.Webhooks.Contract` (`WebhookContractGenerator`), which writes it into the
`x-docspace-trigger-payloads` block of the contract. A trigger whose family the table does
not know stops the generator.

Every SDK carries that map as code, written by `tools/gen-trigger-map.py` after each
generation: `Triggers.cs` (C#), `triggers.ts` (TypeScript), `WebhookTriggers.java`,
`WebhookTriggers.kt`, `triggers.go`, `triggers.py`, `WebhookTriggers.php`,
`webhook_triggers.rb` and `WebhookTriggers.swift`. Each names the payload model of every
trigger with a typed body, marks the untyped catch-all `*`, and tells whether a trigger is
in the contract at all. The samples choose their model only through it, so a trigger
added on the server later is acknowledged with its raw payload, never parsed with some
other model.

The contract also describes every trigger as an entry of the OpenAPI 3.1 `webhooks`
section, so that Scalar, Redoc and other viewers list the deliveries with their headers,
body and the meaning of the receiver's answer. The body of each entry is a `*Delivery`
schema: `WebhookEnvelope` with `payload` typed (`FileDelivery`, `FolderDelivery`,
`RoomDelivery`, `UserDelivery`, `GroupDelivery`, `FormSubmitDelivery`). Every SDK has
these as models too, and its trigger table pairs each trigger with its delivery model
as well as with its payload model, so the whole body can be read into the right type
by the trigger alone:

```csharp
// C#: a switch on the type, or one expected kind
switch (WebhookParser.ParseDelivery(body))
{
    case FileDelivery file: Handle(file.Payload.Title); break;
    case null: break; // '*' or a trigger newer than this SDK
}
var room = WebhookParser.ParseDelivery<RoomDelivery>(body); // throws for any other kind
```

```ts
// TypeScript: narrowing on the trigger narrows the delivery
const d = parseDelivery(body);
if (d?.trigger === 'file.created') {
    console.log(d.delivery.payload?.title); // d.delivery is a FileDelivery
}
```

The other languages have the table (`DeliveryTypes`, `deliveryType(trigger)`,
`NewDelivery`, `decodeDelivery` and the like, named as the payload lookups beside them),
and every language has the router below, which uses it.

### Handlers per event

Instead of parsing and switching, a receiver can register handlers with a router and
hand it each request. The router checks the signature, drops an event it has processed
before, reads the body into the delivery model of its trigger, awaits every handler of
that trigger and returns the status to answer DocSpace with: a 2xx only after all of
them completed, a 500 when one threw, so that DocSpace retries. Handlers therefore have
to be idempotent. Any number of handlers can be registered for one trigger, and each
registration can be removed again.

```csharp
var router = new WebhookRouter(secret, store);              // store: IWebhookEventStore, for duplicates

using var onUpload = router.OnFileCreated(async (FileDelivery d, CancellationToken ct) =>
    await search.IndexAsync(d.Payload, ct));                // generated: one On<Trigger> per trigger
router.OnFileCreated(async (d, ct) => await audit.LogAsync(d.Event, ct));   // a second handler
router.On<RoomDelivery>(async (d, ct) => await sync.RoomAsync(d, ct));      // every room trigger
router.OnOther(async (hook, ct) => await raw.KeepAsync(hook, ct));          // '*' and newer triggers

// in the HTTP endpoint, e.g. minimal API
var result = await router.HandleAsync(body, request.Headers["x-docspace-signature-256"],
    request.Headers["x-docspace-event-id"], ct);
return Results.StatusCode(result.StatusCode);
```

Disposing a registration removes that handler, as `-=` does for an event. A C# `event`
is not used on purpose: its handlers return `void`, so an `async` one could not be
awaited, its failure would never reach the router, and DocSpace would get its 200 before
the work was done.

```ts
const router = new WebhookRouter({ secret, store });       // store: WebhookEventStore

const off = router.on('file.created', async d => {         // d: FileDelivery, typed by the name
    await search.index(d.payload);
});
router.on(['room.created', 'room.archived'], d => sync(d)); // d: RoomDelivery
router.onOther(hook => keepRaw(hook));                       // '*' and newer triggers
off();                                                       // or router.off('file.created', handler)

const result = await router.handle(rawBody, req.headers['x-docspace-signature-256'],
    req.headers['x-docspace-event-id']);
res.status(result.statusCode).end();
```

The router works with any HTTP framework: it takes the raw body and the two headers,
and leaves the request and response to the framework. Keep the handlers quick: DocSpace
waits 30 seconds for the answer by default, so slow work belongs in a queue of your own,
and a handler that only enqueues it makes the 2xx mean "accepted".

Every SDK has the same router, in its own idiom. The typed languages get one generated
method per trigger; the dynamic ones register by name:

| Language | Create | One trigger | Remove | Anything else | Handle |
|---|---|---|---|---|---|
| C# | `new WebhookRouter(secret, store)` | `OnFileCreated(h)`, `On<FileDelivery>(h)` for a whole kind | `Dispose()` the result | `OnOther(h)` | `await HandleAsync(body, sig, id)` |
| TypeScript | `new WebhookRouter({ secret, store })` | `on('file.created', h)`, or a list of triggers | the returned function, `off()` | `onOther(h)` | `await handle(body, sig, id)` |
| Go | `NewWebhookRouter(secret, store)` | `r.OnFileCreated(h)`, `On[FileDelivery](r, trigger, h)` | the returned func | `OnOther(h)` | `Handle(ctx, body, sig, id)` |
| Java | `new WebhookRouter(secret, store)` | `onFileCreated(h)`, `on(trigger, FileDelivery.class, h)` | `close()` the result, `off()` | `onOther(h)` | `handle(body, sig, id)` |
| Kotlin | `WebhookRouter(secret, store)` | `onFileCreated { }`, `on(trigger, FileDelivery::class.java, h)` | `close()` the result, `off()` | `onOther { }` | `handle(body, sig, id)` |
| Swift | `try WebhookRouter(secret:store:)` | `onFileCreated { }`, `on(trigger, FileDelivery.self, h)` | `cancel()` the result | `onOther { }` | `await handle(body:signature:eventIdHeader:)` |
| Python | `WebhookRouter(secret, store)` | `on("file.created", h)` or `@router.on("file.created")` | the returned callable, `off()` | `on_other(h)` | `handle(body, sig, id)` |
| PHP | `new WebhookRouter($secret, $store)` | `on('file.created', $h)` | the returned closure, `off()` | `onOther($h)` | `handle($body, $sig, $id)` |
| Ruby | `WebhookRouter.new(secret, store:)` | `on('file.created') { }` | the returned lambda, `off()` | `on_other { }` | `handle(body, sig, id)` |

All of them return the outcome (`handled`, `duplicate`, `unhandled`, `rejected`,
`invalid`, `failed`) with the status to answer: 200 for the first three, 401, 400 and
500 for the others. Each has an in-memory event store for samples and tests, and an
explicitly named unsigned-replay router (`ForUnsignedReplay`, `{ unsignedReplay: true }`,
`NewUnsignedReplayRouter`, `forUnsignedReplay`, `for_unsigned_replay`) that skips the
signature check for replaying saved bodies during development; never put such a router
behind a reachable endpoint. Every sample in this directory runs on its language's
router.

## Receiving: the parts a runtime has to get right

- **Signature.** `x-docspace-signature-256: sha256=<hex>`, HMAC-SHA256 over the
  **raw body bytes**. The hex is **uppercase**, unlike GitHub's — compare
  case-insensitively and in constant time. Verify before parsing; never re-serialize
  first. This is the only authenticated header.
- **Cheap pre-filtering.** `x-docspace-event-id` and `x-docspace-event-timestamp`
  duplicate `event.id` and `event.createOn`, so a delivery can be dropped without
  parsing the body at all. They are **not** covered by the signature — reject on
  them freely, but never accept on them.
- **Replay.** The signature covers the body only; it does not prove when the
  request was sent. Deduplicate on `event.id`. Do **not** reject a delivery for
  being old: `event.createOn` is when the event happened, and it stays the same on
  every automatic retry (up to about 18 hours later on the default schedule) and
  on a manual retry, which can come at any time. A freshness check on it would
  throw away legitimate retries, and one on the unsigned timestamp header proves
  nothing.
- **Idempotency.** Dedupe on `event.id` **from the body**, and keep the ids you
  have processed for at least the retry window — longer if a portal administrator
  may replay old deliveries. The id is stable across the server's automatic retries
  and a *manual* retry from the delivery log alike: a manual retry is a new
  delivery record of its own (in the delivery log it has its own `id`, the
  `eventId` of the first record and the member who asked for it in `retriedBy`),
  but it still sends the `event.id` of the first one, so the same logical event
  always arrives with the same key, possibly days later.
- **Answer fast.** The server waits 30 s for a response by default
  (`webhooks:requestTimeout` in the server config); a timeout, network error
  or non-2xx status is a failed attempt. A failed delivery is retried after
  ~1 min, 5 min, 30 min, 2 h, 5 h and 10 h (±10% jitter) — 7 attempts over
  roughly 18 hours by default (`webhooks:retryIntervals` in the server config) —
  after which it is abandoned; a portal administrator or the subscription's
  creator can still replay it from the delivery log. Acknowledge immediately and
  process out of band.
- **Sustained failure disables the subscription.** Three days without a single
  successful delivery and it is switched off until someone re-enables it; the
  clock runs from the last success, or from creation if it has never succeeded.
  Answering **410 Gone** deletes the subscription outright on the first
  occurrence, with no grace period.

## Samples

Every sample hands the delivery to its language's router: one handler for every
trigger with a delivery model, a second one for `file.created` (through the typed
`OnFileCreated` where the language has it), and an "other" handler for `*` and
unknown triggers. Each prints a `model: <name>` line — the schema name, `untyped` for
`*`, `unknown` for a trigger the contract does not list — and a `delivery: <name>`
line (`FileDelivery`, …, `none` for `*` and unknown triggers), and exits non-zero
when the router rejects the signature, cannot parse the body or a handler fails. Run
without arguments, each replays `file.created.json` from its own `fixtures/`.

The router's event store is the duplicate check every receiver needs: DocSpace
delivers at least once, a retry carries the same `event.id`, and the signature does
not prove when a request was sent. The samples use the in-memory store; a real
receiver persists the ids for as long as an event can still arrive (about 18 hours of
automatic retries on the default schedule, and a manual retry at any time later). The
unsigned `x-docspace-event-id` header, passed to the router as well, is a cheap
pre-check that may drop a delivery but never accept one.

| Language | Run from | Command | Status |
|---|---|---|---|
| C# | `sdk/docspace-api-sdk-csharp/webhooks/samples/WebhookLogger` | `dotnet run -- --file ../../fixtures/file.created.json` | tested |
| TypeScript | `sdk/docspace-api-sdk-typescript/webhooks` | `npm run sample` | tested |
| Python | `sdk/docspace-api-sdk-python/webhooks/samples` | `python log_webhook.py` | tested |
| Go | `sdk/docspace-api-sdk-go/webhooks/samples` | `go run .` | tested |
| PHP | `sdk/docspace-api-sdk-php/webhooks/samples` | `php log_webhook.php` | tested |
| Java | `sdk/docspace-api-sdk-java/webhooks` | `mvn -q compile exec:java` | tested |
| Kotlin | `sdk/docspace-api-sdk-kotlin/webhooks` | `mvn -q compile exec:java` | tested |
| Ruby | `sdk/docspace-api-sdk-ruby/webhooks/samples` | `ruby log_webhook.rb` | tested |
| Swift | `sdk/docspace-api-sdk-swift/webhooks` | `swift run WebhookLogger` | tested |

Kotlin needs no local `kotlinc`: the compiler comes down as a Maven artifact.
The pinned Kotlin (2.4.21) builds on JDK 17 through 25; 2.0.x fails on JDK 25
while parsing the runtime version string.

Regeneration runs every sample after generating, over **every** signed fixture
in `web/ASC.Web.Api.Tests/Tests/02_Settings/Webhooks/Fixtures/` (each SDK replays its copy):

| Fixture | What it exercises |
|---|---|
| `user.created`, `user.invited`, `user.updated`, `user.deleted` | `UserPayload` in its states: a new member, a pending invitation, an update, a removal; `user.updated` comes from a subscription scoped to that member, so `webhook.target` carries its id and type |
| `group.created`, `group.updated`, `group.deleted` | `GroupPayload`, with the manager as a user summary |
| `room.created`, `agent.created` | `RoomPayload` of an ordinary room and of an AI agent |
| `folder.created` | `FolderPayload` inside a room |
| `file.created` | `FilePayload`: integer ids, the file fields beyond the shared entry part, user summaries with absolute avatar URLs |
| `file.created.retry` | an automatic retry: `webhook` carries `retryCount`, `retryOn`, `lastFailureOn`, `lastFailureContent` and `lastSuccessOn`. Every timestamp is whole-second UTC with its `Z`: the strict `date-time` parsers (Java, Kotlin, Go) reject the whole envelope without the `Z`, and Swift's decoder a fraction |
| `unknown.trigger` | written by hand: a trigger family and target type this contract does not know, as after a server upgrade; every sample must report `model: unknown` and not parse it as a file |
| `form.submit` | written by hand, since the integration-test profile runs no Document Server to submit a form: the one wrapper payload, `originalForm` plus `submittedForm` |

Every fixture except the two hand-written ones is captured from a running server by an on-demand
integration test, which writes the bodies and signature headers byte for byte as the receiver got
them. Re-capture after a payload change, then re-run the skill's verification. Build the whole
solution first: the test project does not reference the Files service or Web.Studio, which the test
host starts from their last build, so a capture without the build can record the old payloads:

```
dotnet build ASC.Web.slnx
dotnet test web/ASC.Web.Api.Tests/ASC.Web.Api.Tests.csproj -- --filter-class "*WebhookFixtureCaptureTests" --explicit only
```

The captured bodies carry the test portal's address and the fake names and emails the suite generates.

The run also checks the model each sample reports against the contract's map, and
replays `samples/negative/` (see its README): a wrong signature, a signed
envelope with no trigger, and a typed trigger with no payload. A sample that exits 0
on any of them fails the run.

A language whose toolchain is missing is reported as `NOT VERIFIED` at the end
rather than passed; a generate-only run skips the check. The skill verifies PHP,
Ruby and Swift in the `php:8.3-cli`, `ruby:3.4-slim` and `swift:6.1` images when
the machine has Docker but not the toolchain (stage A5). Kotlin was also checked on
`maven:3-eclipse-temurin-25`.

A fixture is the raw body plus `<name>.sig`, the signature over those exact
bytes keyed with `samples/SECRET`; regenerate the `.sig` whenever the body
changes.

### `oneOf` support varies sharply by generator

`EntryId` (`integer | string`) and `WebhookTargetId` (the same two types: the
envelope's `target.id`, a GUID string for `user.*`/`group.*`) are the `oneOf`s in
the contract, and they are where the generators differ most. Each mapping below
applies to both. This is worth knowing before trusting an id:

| Quality | Languages | Behaviour |
|---|---|---|
| Correct | Swift, Go, Ruby, TypeScript, C#, Java, Python | Value preserved — Swift's enum with associated values is the cleanest |
| Mapped | PHP, Kotlin | The generator renders an empty class that drops the value, so the generator is told to map the schema away from it |

PHP maps `EntryId` to `mixed`, so `getId()` returns the `int` or `string` as it
arrived. Kotlin maps it to the hand-written `onlyoffice.docspace.webhooks.sdk.EntryId`
in `kotlin/src/`, whose `value` is a `Long` or a `String`; register its
`EntryIdAdapter()` with Moshi before `KotlinJsonAdapterFactory`.

The C# sample doubles as a listener: `dotnet run -- --port 5555 --secret '<key>'`.

`samples/SECRET` holds the throwaway key the fixture is signed with, so the
signature check is exercised for real rather than skipped.

### Per-language shims

Models-only output does not compile anywhere without help. What each target
needed, all of it applied automatically on regeneration:

| Language | Needed |
|---|---|
| C# | `library=httpclient` (the default wraps every property in `Option<T>`), plus `FileParameter`, `OpenAPIDateConverter`, `AbstractOpenAPISchema` in `src/` |
| TypeScript | `supportingFiles` for `runtime.ts` |
| Go | `MappedNullable`, `newStrictDecoder`, `IsNil` copied into `generated/` (Go packages are one directory); `--name-mappings hasAvatar=HasAvatarFlag`, since the field `HasAvatar` collides with the generated presence method `HasAvatar()` |
| PHP | `supportingFiles` for `ObjectSerializer`; too large to stand in for. `EntryId` mapped to `mixed` |
| Java | `AbstractOpenApiSchema` shim, a generated `JSON` class registering all nine type-adapter factories, and three deps: gson, jsr305, `jackson-databind-nullable`; `disallowAdditionalPropertiesIfNotPresent=false`, without which any field the contract does not list fails the whole payload |
| Kotlin | moshi + moshi-kotlin; `EntryId` mapped to the hand-written class in `kotlin/src/`, plus `OffsetDateTime`/`UUID` adapters |
| Ruby | `supportingFiles` for `api_model_base.rb` (every model inherits it), `version.rb`, the gem entry point and the gemspec; the last two are overridden in `templates/ruby` so the gem loads without an HTTP client |
| Swift | `Package.swift` and swift-crypto — Foundation has no HMAC; `supportingFiles` for `JSONValue.swift`, the type of the untyped envelope `payload` |
| Python | nothing — generates cleanly as-is |

Only Python needed no help at all. Every other target needed a flag, a shim or
both.

The contract itself carries two choices made for the generators.
`webhook.target.type` is a plain string, not an enum: its value is the prefix of
the trigger, so every new trigger family adds one, and a closed enum made C#,
Python, Java, Kotlin, PHP, Ruby and Swift reject the whole delivery on a value
they did not know. And `usedSpace` declares `format: double` — a bare `number`
becomes `BigDecimal` in Kotlin, which Moshi has no adapter for, so every
`user.*` delivery failed to parse.

DocSpace validates a subscription's URL before storing it and rejects anything
resolving into a loopback or private range, so `http://localhost:5555` cannot be
registered directly — put a tunnel in front of it, or replay a saved body with
`--file`.

### Behind nginx

`HttpListener` routes on the **Host header**, not just the port. Bound to
`localhost` it answers `400 Bad Request - Invalid Hostname` to anything else —
and http.sys does that before the process sees the request, so nothing is
logged. A reverse proxy forwards the original host by default, which trips this
immediately.

```nginx
location /webhook {
    proxy_pass http://127.0.0.1:5555;

    # Without this the listener rejects the request: it is bound to
    # Host "localhost", and nginx would otherwise forward the public name.
    proxy_set_header Host localhost;

    # The signature covers the raw body; nothing may rewrite it.
    proxy_http_version 1.1;

    # nginx defaults to 1m. A 413 here looks to DocSpace like a failed
    # delivery, and enough of those disable the subscription.
    client_max_body_size 10m;
}
```

The alternative is `--host +`, which accepts any Host header. On Windows that
needs a one-off URL reservation or an elevated shell; the app prints the exact
`netsh http add urlacl` command if the binding is refused.

## Upgrading from the previous payload format

### For receivers: the payload changed

Before the webhook SDK, the publisher serialized the server's own domain objects into
`payload`. From this version on, `payload` is one of the webhook DTOs above, so several
fields were renamed or changed type. There are no aliases for the old names: a receiver
written against the old payload has to move to the new ones, and the deliveries do not
carry a payload version to branch on. What changed:

| Payload | Before | Now |
|---|---|---|
| file, folder, room | `createBy` (user id) | `createdBy` (user summary object) |
| file, folder, room | `createOn` | `created` |
| file, folder, room | `rootId` | `rootFolderId` |
| user | `contacts` (string) | `contacts` (array of `{type, value}`) |
| user | `createDate` | `registrationDate` |
| user | `terminatedDate` | `terminated` |
| group | `parent` (group object) | `parent` (group id) |
| group | `categoryID` | `category` |

Every timestamp is now whole-second UTC with its `Z`, and the server-internal parts of the
old objects, such as the share record of an entry, are gone.

The delivery log changed meaning as well. `GET api/2.0/settings/webhooks/log?eventId=X`
returns the record `X` together with its manual retries, newest first; pick the record
whose `id` is `X` to get that one alone. A manual retry is a record of its own, but the
receiver sees the `event.id` (and `x-docspace-event-id`) of the first record of the event,
so deduplicating on it keeps working across retries.

### For administrators: update the services together

Web.Studio, which sends the deliveries, and every service that publishes them (Web.Api,
People and Files among them) have to run this version together. During a rolling update:

- a Web.Studio of the previous version still sends the deliveries a new publisher writes
  as soon as their event arrives, while the new Web.Studio sends them from the schedule as
  well, so a receiver can get such an event twice (deduplication on `event.id` absorbs it);
- an event a publisher of the previous version writes carries the old payload, which held
  the full server-side objects, the password of a shared link among them. The new
  Web.Studio never sends it: the record stays in the log as not sent, with a note in its
  response, and a manual retry of it is refused, like the retry of any record written
  before this version.

So stop every Web.Studio of the previous version before the new publishers start, and
update all of them in one window.

The delivery log keeps every record unless `webhooks:journalRetentionDays` is set: then
each Web.Studio deletes the records older than that, once nothing will send them any more.
Each manual retry adds a record with a copy of the body, so set it on a busy portal.

## Known gaps

- The generator flattens `allOf`, so `FilePayload`, `FolderPayload` and
  `RoomPayload` are standalone classes that repeat the `FileEntryPayload` fields
  rather than deriving from it. `FileEntryPayload` is still emitted, but nothing
  in the trigger map points at it — do not write a receiver that matches on it.
- The room logo, watermark, lifetime, tags and chat settings are not on the wire,
  and neither are file thumbnails or form-filling roles. Each needs per-request
  work the publisher should not pay for; read them from the REST API. If a
  receiver turns out to need one of them, adding it is a contract change, not a
  serialization accident.
- Runtimes cover **verification and parsing only**, by decision. Deliberately not
  included yet: replay/idempotency helpers and per-framework adapters. The
  constraints they would encode are documented above under *Receiving* — a
  receiver has to handle them by hand for now.
- Only TypeScript and C# have reusable runtimes in `src/`. The other samples
  carry their signature check inline; that logic wants lifting into a per
  language `src/` once the shape has settled.
- Package names now follow the API SDK convention in every language; see
  *Package naming*. The same fix is outstanding in the API SDKs themselves for
  Java, PHP and Swift, where the configured `packageName` never took effect.
