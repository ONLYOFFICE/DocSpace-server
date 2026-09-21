---
paths:
  - "**/*Controller.cs"
  - "**/ApiModels/**/*.cs"
  - "**/*Dto.cs"
  - "**/*Enum.cs"
  - "**/*Enums.cs"
---

# Endpoint documentation for AI consumers (OpenAPI summary & description)

The published OpenAPI documents are no longer read only by humans. They are turned into MCP tools,
loaded into agent context as tool definitions, and used by coding assistants to write client code.
For that audience the operation's `summary` + `description` **is** the tool definition: an agent picks
the operation, fills the parameters and interprets the answer from that text alone, with none of the
organizational context a human colleague has. Anthropic's own guidance is blunt about the stakes —
detailed descriptions are "by far the most important factor in tool performance", and the target is
"at least 3–4 sentences for each tool description, more if the tool is complex".

Structural validity is not agent-readiness: a document can pass every schema check and still be
unusable, because what is missing is *semantics*, not *syntax*.

## 0. Where the text comes from in this repository

| OpenAPI field | Source in C# |
|---|---|
| `summary` | `<summary>` on the action — the **short title** |
| `description` | `<remarks>` on the action — the **long explanation** |
| parameter `description` | `<summary>` on the bound DTO property, or `[SwaggerPathParameter(name, description)]` for an unbindable route placeholder |
| response descriptions | `[SwaggerResponse(code, "text", typeof(T))]` |
| `tags` | `[Tags("Files / Operations")]` |

The `<summary>`/`<remarks>` split is **inverted relative to ordinary C# habit** and the convention is
to write the `<remarks>` block first. Never swap them: the swap silently exchanges every operation's
title and description across the contract and all eight SDKs.

`<path>`, `<collection>`, `<requiresAuthorization>` are structured facts, not prose. Keep them true
(`requiresAuthorization=false` only with `[AllowAnonymous]`); an agent that trusts a stale value
builds a request it cannot authorize.

An action or controller carrying `[ApiExplorerSettings(IgnoreApi = true)]` is kept out of the
published documents and out of the SDKs, so nothing it says reaches an agent today. Document it to
the same standard anyway: that attribute is a publication switch, flipped by someone who will not
reread the doc comments underneath it, and until then the text is what the next person to touch the
handler reads.

## 1. `summary` — the title

* A noun phrase or verb+object, 2–6 words, sentence case, no trailing period: `Bulk download`,
  `Copy to the folder`.
* It must be **unique within the document**. It becomes the sidebar entry, the Postman request name
  and the first line of the doc comment on the generated SDK method in eight languages; duplicates
  make two different operations indistinguishable in a tool list.
* Never a restatement of the HTTP verb and path (`Post fileops copy`), never the C# method name, never
  "API" / "method" / "endpoint" as filler.
* It is a title, not a sentence: no preconditions, no permissions, no error talk. That is `description`.

## 2. `description` — the contract in prose

Minimum three sentences; typically six to nine; 150–200 words for a normal operation, with a hard
ceiling of about 200. Go under ~100 words only when most of the concerns below genuinely do not
apply — a read-only lookup with no preconditions, no asynchrony, no quota and no near-neighbour
operation. State the concerns below **in this order**, one concern per sentence, and omit a line
only when it genuinely does not apply. Fixed order matters: descriptions are truncated in
tool-search listings, so the first sentence has to carry the operation's identity on its own.

1. **What it does, in domain terms** — the intent, not the mechanics. "Creates a refund" is weak;
   "Creates a refund for a paid order" is the shape. Name the resource it acts on and the scope
   (whole portal, current room, current user).
2. **Preconditions** — what must be true before the call, and which operation to call first to make it
   true. Reference it as `GET api/2.0/files/fileops`, so an agent can find it in the same document.
3. **Access** — the role, permission or share level the caller needs, in words. An agent cannot derive
   "room admin or above" from a `security` block.
4. **Effects** — is the call read-only, mutating, or destructive; is it idempotent (may a retry be
   repeated safely?); is it **asynchronous** (does it return a queued operation the caller must poll,
   and where does it poll?). This is the single most commonly missing sentence in this repository, and
   the one whose absence most reliably produces a wrong agent action.
5. **What comes back** — the shape and the meaning of it, beyond the schema: what a returned list is
   ordered by, whether it is paginated and how to get the next page, what an empty result means,
   which fields are only populated in some states, units and time zones (`bytes`, `UTC`).
6. **Limits and boundaries** — quotas, size and count caps, rate limits, and explicitly **what the
   operation does not do**, including *when to use a different operation instead*
   ("for a single file use `.../file/{fileId}/copyas`; this operation is for batches").
7. **Failure and recovery** — the meaningful error cases and what the caller should do about them
   ("a single item the caller cannot read fails the whole operation with 403 — filter the list first").
   Every code named here must also exist as a `[SwaggerResponse]`.
8. **Deprecation** — if `[Obsolete]`/`deprecated`, say so in the first sentence and name the
   replacement operation. An agent will otherwise keep choosing it.

Make implicit knowledge explicit: specialized formats, niche internal terminology, the relationship
between two resources. Write it the way you would brief a new hire who has never seen this product.

## 3. Parameters, DTO properties and responses

Same audience, different unit — and the division of labour between the two is load-bearing.
Anthropic's detail budget ("extremely detailed", at least 3–4 sentences, more when complex) is
prescribed for the **tool description**, and the list of what that description must contain includes
"what each parameter means and how it affects the tool's behavior". So a parameter's *behavioural*
role is documented at the operation level (§2), while the property carries the contract of the value
itself. The OpenAPI-to-MCP guides state the property's half just as plainly: "what the parameter
means, valid formats, examples. The model uses this to fill in arguments correctly."

Two consequences decide everything below.

* **Brevity is not a goal.** "Humans can infer context from brief descriptions. AI agents cannot." A
  property sentence ends up short because the schema already prints half of what it would say — never
  because short is a virtue in itself.
* **Economy is still real.** Tool definitions are re-sent into context every session; an
  auto-converted 200-endpoint document runs 40–80k tokens of schema, at which point models "may not
  read the entire description". A property description is paid for by **every** operation that
  references the schema, which is what makes a wasted clause on a shared DTO expensive and a needed
  format clause on one field cheap.

### 3.1 What a property description must answer

Four questions. Length follows from them — usually one or two sentences, more for a format-sensitive
or behaviour-switching field, and no apology is owed for the longer one.

1. **What the value means** in the domain, not a re-spelling of the name. The property name is already
   in front of the agent; a sentence that paraphrases it is the §5 tautology at field level.
2. **Where a valid value comes from, or the format it must have.** `folderId` → "ID of the destination
   folder; obtain it from `GET api/2.0/files/@root` rather than guessing." ISO-8601, case sensitivity,
   allowed characters, "only IDs returned by ... are accepted". This is the half an agent cannot
   guess, and the half its calls fail on.
3. **The behavioural consequence, where the field has one.** For a boolean switch, what *both* values
   do. For a bounded value, what the boundary *means* — not the number, which the schema prints.
4. **A realistic example** — see §3.2.

Two notes that belong here:

* **The name is part of the documentation.** `userId`, not `user`; a name that needs a sentence to
  disambiguate it should be fixed instead — though renaming a bound property is a contract change, so
  a documentation pass reports it rather than doing it.
* **Enum members each get their own `<summary>`**: what the value means, which transitions are legal,
  which one is usual. A bare list of names is not documentation. Do not re-spell the members in the
  property's own sentence when `[Description]` on the enum already publishes them into the schema.

### 3.2 Examples: nearly done here, so keep them that way

Measured across the four C# documents in the public join, excluding the generated `*Wrapper`
envelopes that have no source file (2026-09-07): **2120 properties, a description on 100% of them**,
median 36 characters, **735 scalar properties of which only 12 (1.6%) carry no example**. Examples in
this contract are effectively complete — the twelve exceptions are `ErrorApiResponse` (in all four
documents), `ConnectionTestResult`, `SetAppEnabledBody` and the `ItemKeyValuePair*` instantiations.

So the rules below are about not losing that state, and the gap this contract actually has is the
subject of §3.1 and §5: in `api_2.0.json`, **128 of 1084 documented non-wrapper properties (12%) have
a description that adds not one word beyond the property name** — and that is a floor, because a
restatement phrased in different words ("The DNS suffix of the partition" for `partitionDnsSuffix`)
is not counted by any mechanical check.

* `<example>` on every scalar property, and it has to look like a real value: a real id, a real file
  extension, a real ISO timestamp. `"string"`, `0` and `item1` teach nothing. "Empty schemas force
  the model to guess parameter formats", and models "pattern-match aggressively from examples".
* The class-level `<example>` JSON block and the per-property `<example>` values must agree. They
  drift silently, and the block is what an agent copies as the shape of the body.
* For a nested or format-sensitive body the class-level block earns its keep the way Anthropic's
  `input_examples` does: it shows which optional fields to include and how the object is assembled.
* Counting examples over *all* schemas is how the 1.6% above first came out as 47%: the generated
  `*Wrapper` envelopes contribute 625 example-less scalars (`count`, `status`, `statusCode`), have no
  source file, and are out of scope — exclude them from every measurement.

### 3.3 What not to write on a property

The schema already publishes these, and repeating one costs every operation that references the
schema:

* type, required-ness, nullability;
* the enum members, where `[Description]` on the enum reaches the schema;
* the numbers behind `[Range]` / `[StringLength]` and the default from the property initializer — say
  what the boundary *means* instead;
* the field list of a nested object — it is in the referenced schema, with descriptions of its own.

And one exclusion that is not about economy at all:

* **Facts that belong to an operation.** When a parameter silently disables another, is ignored
  without a second one, or changes a matching rule, that sentence goes into the operation's
  `description` (§2). The dividing line is not how interesting the fact is but who owns it: what the
  value *is* — meaning, origin, format — belongs to the property; what the **handler does with it** —
  an unknown id skipped, a duplicate counted once, an omitted entry left where it was, a blank string
  treated as a reset — belongs to the operation. A property cannot carry the second kind and stay
  true: the same DTO is bound by other operations, where the same field means something else. That
  applies to a DTO only one operation binds today as well, and without weighing the odds that a second
  one appears: while it is genuinely single-use, a fact written in both places is a fact that will
  drift in one of them, and it is the operation's copy an agent reads when it decides what the call
  will do. This is what the property's narrow contract exists to serve, not a footnote to it.

### 3.4 Response properties do a different job

A request property is filled in *before* the call; a response property is read *after* it. Only the
2xx schema maps to an MCP `outputSchema` (added in the 2025-06-18 revision), and not every generator
emits one — so response text reaches an agent unreliably as a schema, and reliably as SDK doc comments
and as the JSON already in front of it. Write it for the reader of a returned object:

* what `null` and an empty collection **mean** here — "no avatar has been set", not "may be null";
* in which states the field is populated at all;
* the unit, and which direction is better, where a number is a measurement;
* what a returned list is ordered by, where the order is contractual.

Do not mirror §3.1's origin-and-format rules here: nobody fills these in.

### 3.5 One property, many operations

Before editing a shared DTO, know how many operations reference it — the text lands in all of them.
Of the 387 schemas in `api_2.0.json` only 128 are declared in `web/ASC.Web.Api`; the rest arrive from
`ASC.Core.Common`, `ASC.Web.Core`, `ASC.AuditTrail` and others, so editing one of those changes text
in several documents at once. Such a type is **in scope** — leaving it out would leave half the
contract unoptimised, invisibly, because it is not found by browsing the project's folders — but the
blast radius comes with a condition: before editing one, establish which documents and which
operations reference the schema, and say so in the report. A sentence that is right for the operation
you happened to be reading can be false for the other three that share the schema, and §3.3's rule
about operation-level facts is what keeps it from becoming one.

### 3.6 Responses

* One `[SwaggerResponse]` per code the operation can actually return, and its text says what that code
  *means here* — not "Bad request" but "The destination folder is a room the caller cannot write to".
* Every code named in the prose must exist as a `[SwaggerResponse]`, and a documented code the handler
  cannot reach is a defect rather than thoroughness: it buys an agent a recovery branch for a case
  that never happens.

## 4. Consistency across the document

* **One canonical term per concept.** If the same thing is called "room" in one operation and
  "workspace" in the next, agents treat them as two concepts. Match the term the DTOs use.
* Tags are the namespace (`Files / Operations`, `People / Contacts`). Related operations live under the
  same tag, and the description of a multi-step flow names the other steps in order.
* A description must be **self-contained**. No "see above", no reliance on a neighbouring operation's
  text, no links to UI pages or internal wikis.
* Escape or avoid `<`, `>`, `&` — this is XML doc. Plain sentences and `backticks` only: no HTML, no
  tables, no multi-paragraph essays. The text is re-emitted as doc comments in eight languages.

## 5. Anti-patterns

| Smell | What it looks like here |
|---|---|
| **Lazy** | `<remarks>Gets the file.</remarks>` on `GET .../file/{id}` — a paraphrase of the path that adds zero information; undocumented parameters; a generic "Ok" response text. |
| **Bloated** | The schema retyped field by field in prose; boilerplate repeated in all 40 operations of a controller; hedging and marketing language. |
| **Tangled** | Business logic, auth, error handling and pagination fused into one 60-word sentence. Split by concern, in the order of §2. |
| **Fragmented** | Half the contract in the summary, half in a response text, the precondition in another operation's remarks. |
| **Untrue** | `requiresAuthorization=false` on an authorized action; a documented 404 the code never returns; a description describing the previous version of the handler. Wrong text is worse than none — an agent trusts it. |
| **Tautology** | The description restates the method name (`BulkDownload` → "Bulk downloads."). If it can be derived from the signature, it is not documentation. |
| **Internal leakage** | `FileStorageService`, exception type names, DB column names, task-tracker numbers. Callers cannot see any of it. |

## 6. Worked example

Before — passes every presence check and still leaves an agent guessing:

```csharp
/// <remarks>
/// Starts the download process of files and folders with the IDs specified in the request.
/// </remarks>
/// <summary>Bulk download</summary>
```

After — same length class, but the asynchrony, the polling target, the two ID lists, the access rule
and the flag's effect are all now stated:

```csharp
/// <remarks>
/// Queues an asynchronous job that packs the specified files and folders into a single archive, and
/// returns the caller's current file operations including the one just created. The archive is not
/// ready when the response arrives: poll `GET api/2.0/files/fileops` until the operation's `finished`
/// is true, then take the archive link from its `url`. Items listed in `fileConvertIds` are converted
/// to the requested format before being packed, while `fileIds` are packed as they are. The caller
/// needs read access to every listed item — one unreadable item fails the whole operation with 403,
/// so filter the list beforehand. Pass `returnSingleOperation=true` to get back only the operation
/// started by this call instead of all active ones.
/// </remarks>
/// <summary>Bulk download</summary>
```

That example is 114 words — the **short end** of the band: this operation has no quota, no deprecation
and no near-neighbour operation to redirect to, so three of the §2 concerns are simply absent. An
operation where they do apply lands at 150–200.

## 7. Definition of done

Before committing a new or changed endpoint, check that:

- [ ] `<summary>` is a unique 2–6 word title; `<remarks>` holds the explanation (not swapped).
- [ ] The first sentence identifies the operation on its own, out of context.
- [ ] Preconditions, required access, and effects (mutating / destructive / idempotent / async) are stated.
- [ ] Async operations name the operation to poll and the field that signals completion.
- [ ] Every parameter, DTO property and enum member answers §3.1 — meaning, origin or format,
      behavioural consequence — and repeats nothing the schema already prints (§3.3).
- [ ] Every scalar property carries a realistic `<example>`, and the class-level `<example>` block
      agrees with them (§3.2).
- [ ] Cross-parameter and behavioural facts sit in the operation's `description`, not on a property,
      shared or single-use (§3.3); response fields say what `null` and empty mean (§3.4).
- [ ] Every reachable status code has a `[SwaggerResponse]` whose text is specific to this operation.
- [ ] "When to use something else" is stated wherever a near-neighbour operation exists.
- [ ] Terminology matches the DTOs and the rest of the tag; no internal type names.
- [ ] Nothing in the text is false, including `<path>`, `<collection>` and `<requiresAuthorization>`.
- [ ] Tags are XML-well-formed and spelled correctly — a doubly-misspelled tag (`<ssummary>`) is
      silent in the whole C# toolchain and only surfaces at the document level.

## Sources

- Anthropic, [Writing effective tools for AI agents](https://www.anthropic.com/engineering/writing-tools-for-agents)
- Anthropic, [Define tools — best practices for tool definitions](https://platform.claude.com/docs/en/agents-and-tools/tool-use/define-tools) — the 3–4 sentence budget is for the *tool* description; `input_examples` for format-sensitive inputs
- Speakeasy, [Generating MCP tools from OpenAPI](https://www.speakeasy.com/mcp/tool-design/generate-mcp-tools-from-openapi/) — "Humans can infer context from brief descriptions. AI agents cannot"
- DigitalAPI, [OpenAPI spec best practices for MCP](https://www.digitalapi.ai/blogs/openapi-spec-for-mcp-best-practices) — the parameter contract; "empty schemas force the model to guess parameter formats"
- TrueFoundry, [OpenAPI to MCP server conversion](https://www.truefoundry.com/blog/openapi-to-mcp-server-conversion) — only the 2xx schema maps to `outputSchema` (MCP revision 2025-06-18); 40–80k tokens of schema for a 200-endpoint document
- [Making OpenAPI Documentation Agent-Ready](https://arxiv.org/html/2605.14312) — the lazy / bloated / tangled / fragmented taxonomy
- LogRocket, [How to write agent-friendly API documentation](https://blog.logrocket.com/how-write-agent-friendly-api-documentation/)
- Zuplo, [The API readiness gap](https://zuplo.com/learning-center/api-readiness-gap-agent-callable-apis)
- [Designing API specs for agentic clients](https://spec-coding.dev/blog/designing-api-specs-for-agentic-clients)
- MCP tool annotations (`readOnlyHint`, `destructiveHint`, `idempotentHint`) — [MCP best practices](https://github.com/ComposioHQ/awesome-claude-skills/blob/master/mcp-builder/reference/mcp_best_practices.md)
