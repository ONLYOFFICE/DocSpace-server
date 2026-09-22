---
paths:
  - "**/ApiModels/**/*.cs"
  - "**/*Dto.cs"
  - "**/*Enum.cs"
  - "**/*Enums.cs"
---

# DTO properties and enum members in the published contract

The published OpenAPI documents are loaded into agent context as tool definitions, so a bound
property's text is what an agent fills the field in from, and a returned property's text is what it
reads the answer with. Neither has the organizational context a human colleague has.

This file covers the property and the enum member. The operation half — the title, the description
and the response codes of the action — is `.claude/rules/openapi-endpoint-docs.md`, which loads with
the controller and keeps the same section numbers for what is here (§3.1–§3.5).

| OpenAPI field | Source in C# |
|---|---|
| parameter / property `description` | `<summary>` on the property |
| property `example` | `<example>` on the property, plus the class-level `<example>` JSON block |
| enum member description | `<summary>` on the member, or `[Description]` on it |

Two consequences decide everything below, and they pull against each other:

* **Brevity is not a goal.** A property sentence ends up short because the schema already prints half
  of what it would say — never because short is a virtue in itself.
* **Economy is still real.** A property description is paid for by **every** operation that
  references the schema, which is what makes a wasted clause on a shared DTO expensive and a needed
  format clause on one field cheap.

### 3.1 What a property description must answer

Four questions. Length follows from them — usually one or two sentences, more for a format-sensitive
or behaviour-switching field, and no apology is owed for the longer one.

1. **What the value means** in the domain, not a re-spelling of the name. The property name is already
   in front of the agent; a sentence that paraphrases it is, at field level, the tautology of §5 of
   `openapi-endpoint-docs.md`.
2. **Where a valid value comes from, or the format it must have.** `folderId` → "ID of the destination
   folder; obtain it from `GET api/2.0/files/@root` rather than guessing." ISO-8601, case sensitivity,
   allowed characters, "only IDs returned by ... are accepted". This is the half an agent cannot
   guess, and the half its calls fail on.
3. **The behavioural consequence, where the field has one.** For a boolean switch, what *both* values
   do. For a bounded value, what the boundary *means* — not the number, which the schema prints.
4. **A realistic example** — see §3.2.

Three notes that belong here:

* **The name is part of the documentation.** `userId`, not `user`. A new property that needs a
  sentence to disambiguate its name should be renamed instead, and this is the moment to do it:
  once the property is published, renaming it breaks the contract and all eight SDKs.
* **A date is an `ApiDateTime`, and it is not UTC.** What goes on the wire is an ISO-8601 timestamp
  carrying an explicit offset — the stored instant shifted into the portal's time zone — despite the
  `UtcTime` name inside the type. Never document such a field as "in UTC": say that the value carries
  its own offset, and on an inbound one say that a string with neither `Z` nor an offset is read in
  the portal's time zone, not in the caller's.
* **Enum members each get their own `<summary>`**: what the value means, which transitions are legal,
  which one is usual. A bare list of names is not documentation. Do not re-spell the members in the
  property's own sentence when `[Description]` on the enum already publishes them into the schema.

### 3.2 Examples

Write the `<example>` together with the property, not later.

* Every scalar property carries one, and it has to look like a real value: a real id, a real file
  extension, a real ISO timestamp. `"string"`, `0` and `item1` teach nothing — an empty or made-up
  example makes an agent guess the format, and agents pattern-match on examples aggressively.
* The class-level `<example>` JSON block and the per-property `<example>` values must agree. They
  drift silently, and the block is what an agent copies as the shape of the body.
* For a nested or format-sensitive body, write the class-level block too: it is what shows which
  optional fields to include and how the object is assembled.
* On a positional `record` the `<example>` does not reach the schema at all — declare such a DTO with
  ordinary properties when its values need examples.

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
  `description` (§2 of `openapi-endpoint-docs.md`). The dividing line is not how interesting the fact
  is but who owns it: what the
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
2xx schema maps to an MCP `outputSchema`, and not every generator emits one — so response text reaches
an agent unreliably as a schema, and reliably as SDK doc comments and as the JSON already in front of
it. Write it for the reader of a returned object:

* what `null` and an empty collection **mean** here — "no avatar has been set", not "may be null";
* in which states the field is populated at all;
* the unit, and which direction is better, where a number is a measurement;
* what a returned list is ordered by, where the order is contractual.

Do not mirror §3.1's origin-and-format rules here: nobody fills these in.

### 3.5 One property, many operations

A DTO is bound by every operation that references it, and a type declared in `ASC.Core.Common`,
`ASC.Web.Core` or `ASC.AuditTrail` is referenced from several services at once — so its text is
published into several documents, not only the one you have in mind.

Before you write or change a property on such a type, find those operations, and write a sentence
that is true of all of them. A sentence that fits the operation you happened to be looking at can be
false for the others, and §3.3's rule about operation-level facts is what keeps it from becoming
one.

## Definition of done

- [ ] Every property and enum member answers §3.1 — meaning, origin or format, behavioural
      consequence — and repeats nothing the schema already prints (§3.3).
- [ ] Every scalar property carries a realistic `<example>`, and the class-level `<example>` block
      agrees with them (§3.2).
- [ ] Cross-parameter and behavioural facts are **not** here — they sit in the operation's
      `description` (§3.3, and §2 of `openapi-endpoint-docs.md`), shared DTO or single-use one.
- [ ] Response fields say what `null` and an empty collection mean, and in which states the field is
      populated at all (§3.4).
- [ ] Before editing a shared type, the operations and documents it reaches are known (§3.5).
- [ ] Tags are XML-well-formed and spelled correctly — a doubly-misspelled tag (`<ssummary>`) is
      silent in the whole C# toolchain and only surfaces at the document level.
