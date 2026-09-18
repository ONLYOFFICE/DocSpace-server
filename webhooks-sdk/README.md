# DocSpace Webhooks SDK

Client-side libraries for **receiving** DocSpace webhooks: payload models for every
trigger, plus (in progress) a runtime that verifies the signature and narrows the
payload to the right type.

This is not the DocSpace REST API SDK. It shares no schemas with it — see
*The payloads are domain entities* below, which is the single most important thing
to understand before working here.

## Layout

```
webhooks-sdk/
├── generate.sh          regenerates every language from the contract
├── openapitools.json    pins the generator (7.25.0) for reproducible output
├── samples/             shared fixture: a real delivery, its signature, its key
├── tools/
│   └── gen-trigger-map.py   emits the trigger→payload dispatch table
└── <language>/
    ├── generated/       openapi-generator output — DO NOT EDIT, wiped on regen
    │   └── docs/        per-model Markdown reference, same as the API SDKs
    ├── src/             hand-written runtime — signature verification, parsing
    └── samples/         runnable examples (C#: samples/WebhookLogger)
```

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

DocSpace also has a second, unrelated Markdown mechanism —
`GenerateMarkdownDocsCommand` in `ASC.Api.Documentation`, which splits the
joined spec per service, renders it through a custom `my-markdown` codegen,
slices it into one file per operation and bundles the result for the API
reference site. That one is endpoint-shaped. This contract has `paths: {}`, so
it has nothing to render and is not used here.

`generated/triggers.ts` is derived from the contract's
`x-docspace-trigger-payloads` block, never hand-written: 37 triggers × 9
languages is exactly the list that rots silently. Add a trigger to
`WebhookTrigger.cs` and the contract, rerun `./generate.sh`, and it propagates.

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
the package name lives in the hand-written `typescript/package.json` instead.

The contract lives with the server code that defines it, not here:
[`common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml`](../common/ASC.Webhooks.Core/Contract/docspace-webhooks.yaml).
It sits next to `WebhookTrigger.cs` and `WebhookPayload.cs` so that a change to
either shows up in the same review as the contract change it implies.

## Regenerating

```bash
./generate.sh              # all languages
./generate.sh python go    # a subset
```

Needs `openapi-generator-cli` (`npm i -g @openapitools/openapi-generator-cli`) and a
JDK. Takes ~90 s for all nine. `generated/` is wiped and rewritten; `src/` is never
touched.

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
first-class commands to that tool — not to wire packaging into this script.

## The payloads are domain entities

DocSpace publishes webhooks with the **internal domain objects**, not the API DTOs:

| Trigger family | Payload | Serialized as |
|---|---|---|
| `user.*` | `UserPayload` | `UserInfo` |
| `group.*` | `GroupPayload` | `GroupInfo` |
| `file.*`, `folder.*`, `room.*`, `agent.*`, `form.filled.out`, `form.stopped` | `FileEntryPayload` | `FileEntry<T>` |
| `form.submit` | `FormSubmitPayload` | `SubmittedFormData<T>` |

**There is one payload shape for files and folders alike, and it is the abstract
base.** `WebhookManager` calls `PublishAsync<T1,T2>` with a static parameter type
of `FileEntry<T>`, so `T1` binds to the base and System.Text.Json serializes by
*declared* type. No `File<T>` or `Folder<T>` member — `pureTitle`, `version`,
`contentLength`, `folderType`, `filesCount`, `isRoom` — ever reaches the wire.
`fileEntryType` (**1 folder, 2 file**) is the only discriminator you get.

They are easy to mistake for DTOs — `UserPayload` and `EmployeeFullDto` share 11
field names — but they are not, and nothing maps between them. `UserController`
hands the raw `UserInfo` to the webhook and the DTO to the HTTP response two lines
apart.

Consequences worth knowing before you write a receiver:

- **`title` is always present, for files as well as folders.** `File<T>` hides
  `Title` behind `[JsonIgnore]` and exposes `pureTitle` instead, but that
  override is never reached, so `title` is what arrives and `pureTitle` never
  does. (An earlier revision of this document claimed the opposite; a captured
  production delivery settled it.)
- **Nothing is required.** The server serializes with `WhenWritingDefault`, so every
  null, `false` and `0` is omitted. Absence means "default", never "unset".
- **`security` / `securityByUsers` are on every file and folder event** — internal
  ACL maps. `securityByUsers` is initialised non-null, so it is always present, often
  as `{}`.
- **`contacts` and `contactsList` are the same data twice**, and `ldapQouta` is
  misspelled in the domain type and therefore on the wire.

## Receiving: the parts a runtime has to get right

- **Signature.** `x-docspace-signature-256: sha256=<hex>`, HMAC-SHA256 over the
  **raw body bytes**. The hex is **uppercase**, unlike GitHub's — compare
  case-insensitively and in constant time. Verify before parsing; never re-serialize
  first. This is the only authenticated header.
- **Cheap pre-filtering.** `x-docspace-event-id` and `x-docspace-event-timestamp`
  duplicate `event.id` and `event.createOn`, so a delivery can be dropped without
  parsing the body at all. They are **not** covered by the signature — reject on
  them freely, but never accept on them. A replay with its timestamp header
  rewritten to "now" still carries a valid signature, so a receiver that checks
  freshness only against the header has no replay protection whatsoever.
- **Replay.** `event.createOn` inside the signed body is the authoritative
  timestamp; check freshness against it after verifying. Comparing it with the
  header value also detects tampering for free.
- **Idempotency.** Dedupe on `event.id` **from the body**. It is stable across the
  server's automatic retries, but a *manual* retry from the admin UI creates a new
  delivery record and therefore a new id — the same logical event arrives with a
  different key.
- **Answer fast.** The server retries 5 times with exponential backoff from 1 s and
  then gives up permanently — a total budget of about 31 seconds. Acknowledge
  immediately and process out of band.

## Samples

Every sample verifies the signature, parses with the generated models and logs
the delivery. They all replay the same fixture — `samples/file.created.json`, a
real production `file.created` delivery — and print identical output.

| Language | Run from | Command | Status |
|---|---|---|---|
| C# | `csharp/samples/WebhookLogger` | `dotnet run -- --file ../../../samples/file.created.json` | tested |
| TypeScript | `typescript` | `npm run sample` | tested |
| Python | `python/samples` | `python log_webhook.py` | tested |
| Go | `go` | `go run ./samples` | tested |
| PHP | `php/samples` | `php log_webhook.php` | tested |
| Java | `java` | `mvn -q compile exec:java` | tested |
| Kotlin | `kotlin` | `mvn -q compile exec:java` | tested |
| Ruby | `ruby/samples` | `ruby log_webhook.rb` | **unverified** |
| Swift | `swift` | `swift run WebhookLogger` | **unverified** |

Kotlin needs no local `kotlinc`: the compiler comes down as a Maven artifact.

The Ruby and Swift samples have **never been executed** — neither toolchain was
available. They are written against the generated APIs rather than guessed, and
every symbol they reference was checked to exist, but expect to fix something on
first run. Each says so in its own header comment.

### `oneOf` support varies sharply by generator

`EntryId` (`integer | string`) is the one `oneOf` in the contract, and it is
where the generators differ most. This is worth knowing before trusting an id:

| Quality | Languages | Behaviour |
|---|---|---|
| Correct | Swift, Go, Ruby, TypeScript, C#, Java, Python | Value preserved — Swift's enum with associated values is the cleanest |
| **Broken** | **PHP, Kotlin** | Rendered as an empty class; the scalar is **silently dropped** |

Both samples for the broken pair read ids from the raw envelope instead, and say
why inline. Kotlin additionally needs a custom Moshi adapter or it throws, since
Moshi walks into a bare number expecting an object.

The C# sample doubles as a listener: `dotnet run -- --port 5555 --secret '<key>'`.

`samples/SECRET` holds the throwaway key the fixture is signed with, so the
signature check is exercised for real rather than skipped.

### Per-language shims

Models-only output does not compile anywhere without help. What each target
needed, all of it applied automatically by `generate.sh`:

| Language | Needed |
|---|---|
| C# | `library=httpclient` (the default wraps every property in `Option<T>`), plus `FileParameter`, `OpenAPIDateConverter`, `AbstractOpenAPISchema` in `src/` |
| TypeScript | `supportingFiles` for `runtime.ts` |
| Go | `MappedNullable`, `newStrictDecoder`, `IsNil` copied into `generated/` (Go packages are one directory) |
| PHP | `supportingFiles` for `ObjectSerializer`; too large to stand in for |
| Java | `AbstractOpenApiSchema` shim, a generated `JSON` class registering all nine type-adapter factories, and three deps: gson, jsr305, `jackson-databind-nullable` |
| Kotlin | moshi + moshi-kotlin; a Moshi adapter for the broken `EntryId`, plus `OffsetDateTime`/`UUID` adapters |
| Swift | `Package.swift` and swift-crypto — Foundation has no HMAC |
| Python, Ruby | nothing — both generate cleanly as-is |

Two languages needed no help at all: Python and Ruby. Every other target needed
a flag, a shim or both.

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

## Known gaps

- 11 enums (`FolderType`, `FileStatus`, `FileShare`, `EmployeeStatus`,
  `FilesSecurityActions`, …) are typed `integer` with the C# enum named in the
  description. Value tables not yet filled in.
- 6 nested types are `additionalProperties: true` placeholders: `Tag`,
  `FileShareRecord<T>`, `FormInfo<T>`, `WatermarkSettings`, `RoomDataLifetime`,
  `ChatSettings`.
- One captured `file.created` delivery has been checked against the contract, and
  it corrected two errors (see above). No other trigger has been conformance
  checked yet, and `user.*` especially deserves one: a capture from a clean dev
  portal is *not* sufficient evidence, because `sid`, `ssoNameId`, `ssoSessionId`
  and `ldapQouta` are null there and vanish from the JSON, but appear on LDAP/SSO
  tenants.
- Fields marked `REVIEW` in the contract — `ssoSessionId` above all — need a decision
  before this is published as a public contract. Shipping an SDK blesses the current
  shape.
- Runtimes cover **verification and parsing only**, by decision. Deliberately not
  included yet: replay/idempotency helpers and per-framework adapters. The
  constraints they would encode are documented above under *Receiving* — a
  receiver has to handle them by hand for now.
- Only TypeScript and C# have reusable runtimes in `src/`. The other samples
  carry their signature check inline; that logic wants lifting into a per
  language `src/` once the shape has settled.
- The Ruby and Swift samples are unverified — see *Samples*. What was checked
  statically: every model, property and method they call exists in the generated
  code; Ruby's `EntryId.build` returns the scalar; Swift's `EntryId` is an enum
  with associated values; both SDKs carry all 34 `FileEntryPayload` fields; and
  neither references a supporting file that models-only output omits. What was
  not checked: that either actually compiles and runs.
- All nine SDKs were checked field-by-field against the contract, and all nine
  carry the full 34 properties of `FileEntryPayload`.
- Swift renames the reserved word `Type`, so `WebhookTargetInfo.type` is typed
  `ModelType`. The property name and wire key are unaffected.
- The PHP generator renders the `EntryId` oneOf as a property-less class, so
  `getId()` returns an empty object and the value is lost. The sample reads ids
  from the raw envelope instead. Every other field maps correctly.
- Package names now follow the API SDK convention in every language; see
  *Package naming*. The same fix is outstanding in the API SDKs themselves for
  Java, PHP and Swift, where the configured `packageName` never took effect.
