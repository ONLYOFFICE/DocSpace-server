---
paths:
  - "**/*Controller.cs"
---

# Endpoint documentation for AI consumers (OpenAPI summary & description)

The published OpenAPI documents are no longer read only by humans. They are turned into MCP tools,
loaded into agent context as tool definitions, and used by coding assistants to write client code.
For that audience the operation's `summary` + `description` **is** the tool definition: an agent picks
the operation, fills the parameters and interprets the answer from that text alone, with none of the
organizational context a human colleague has.

Structural validity is not agent-readiness: a document can pass every schema check and still be
unusable, because what is missing is *semantics*, not *syntax*.

This file covers the **operation**: its title, its description, its response codes. The text of a
parameter, a DTO property or an enum member is the sibling rule
`.claude/rules/openapi-dto-docs.md`, which loads when you open the file that holds it.

## 0. Where the text comes from

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
   and where does it poll?). Never leave this one out: an agent that takes an asynchronous call for a
   finished one acts on a result that does not exist yet.
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

Same audience, different unit — and the division of labour between the two is load-bearing. A
parameter's *behavioural* role is documented at the operation level (§2); the property carries the
contract of the value itself — what it means, where a valid one comes from, what format it must have.

**The property half of this rule lives in `.claude/rules/openapi-dto-docs.md`** (§3.1–§3.5, keeping
these numbers), which loads with the DTO, the enum or the `ApiModels` file you open to type it. Two
things from it decide what may be written here:

* a **cross-parameter or behavioural** fact — an unknown id skipped, a parameter that silently
  disables another, a blank string treated as a reset — belongs in this operation's `description`,
  never on the property, because the same DTO is bound by other operations (§3.3 there);
* editing a **shared** property changes text in every operation that binds it, and in other documents
  when the type comes from `ASC.Core.Common` and friends (§3.5 there).

What stays here is §3.6 below: the response codes of this operation.

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
- [ ] Cross-parameter and behavioural facts sit in this operation's `description` rather than on a
      DTO property, shared or single-use (`openapi-dto-docs.md` §3.3).
- [ ] Every reachable status code has a `[SwaggerResponse]` whose text is specific to this operation.
- [ ] "When to use something else" is stated wherever a near-neighbour operation exists.
- [ ] Terminology matches the DTOs and the rest of the tag; no internal type names.
- [ ] Nothing in the text is false, including `<path>`, `<collection>` and `<requiresAuthorization>`.
- [ ] Tags are XML-well-formed and spelled correctly — a doubly-misspelled tag (`<ssummary>`) is
      silent in the whole C# toolchain and only surfaces at the document level.
