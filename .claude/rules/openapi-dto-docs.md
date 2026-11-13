---
paths:
  - "**/ApiModels/**/*.cs"
  - "**/*Dto.cs"
  - "**/*Enum.cs"
  - "**/*Enums.cs"
---

# DTO properties and enum members in the published contract

An agent fills a bound property in from its text and reads a returned property with it, without the
context a colleague has. The operation half — title, description, response codes — is
`.claude/rules/openapi-endpoint-docs.md`, with the same section numbers for what is here (§3.1–§3.6).

A DTO that only actions hidden with `[ApiExplorerSettings(IgnoreApi = true)]` bind or return is
internal and outside this rule (`openapi-endpoint-docs.md` §0). Once a published operation uses it,
every property of it is held to the rule.

| OpenAPI field | Source in C# |
|---|---|
| parameter / property `description` | `<summary>` on the property |
| property `example` | `<example>` on the property, plus the class-level `<example>` JSON block |
| enum member description | `<summary>` on the member, or `[Description]` on it |

Length follows from the facts: nothing left out, nothing the schema already prints. A property text is paid
for by every operation that references the schema.

### 3.1 What a property description must answer

1. **What the value means** in the domain, not a re-spelling of the name.
2. **Where a valid value comes from, or the format it must have.** `folderId` → "ID of the destination
   folder; obtain it from `GET api/2.0/files/@root` rather than guessing." ISO-8601, case sensitivity,
   allowed characters, "only IDs returned by ... are accepted".
3. **The behavioural consequence of the value**, where it has one: for a boolean switch, what *both* values
   do; for a bounded value, what the boundary *means*, not the number.
4. **A realistic example** (§3.2).

* **The name is part of the documentation**: `userId`, not `user`. A new property that needs a sentence to
  disambiguate its name is renamed before it is published; afterwards a rename breaks all the SDKs.
* **A date is an `ApiDateTime`, and it is not UTC.** On the wire it is an ISO-8601 timestamp with an
  explicit offset of the portal's time zone. Never write "in UTC": say that the value carries its own
  offset; on an inbound one, that a string with neither `Z` nor an offset is read in the portal's time zone.
* **Enum members each get their own `<summary>`**: what the value means, which transitions are legal, which
  one is usual. Do not re-spell the members in the property's text when `[Description]` already publishes
  them.
* **One term per concept**, as in §2.3 of the operation rule: user types by `[Description]` of
  `EmployeeType` (`DocSpace admin`, `room admin`, `user`, `guest`), room roles by `[Description]` of
  `FileShare` (`room manager`, `content creator`, …); `portal`, `installation`; `account` for a person's
  record, `profile` for the returned object.

### 3.2 Examples

* Every scalar property carries an `<example>` that looks like a real value: a real id, a real file
  extension, a real ISO timestamp — never `"string"`, `0` or `item1`.
* The class-level `<example>` JSON block and the per-property values agree. A nested or format-sensitive
  body gets the class-level block too: it shows which optional fields to include.
* On a positional `record` the `<example>` does not reach the schema: declare ordinary properties when the
  values need examples.

### 3.3 What not to write on a property

The schema already publishes:

* type, required-ness, nullability;
* the enum members, where `[Description]` on the enum reaches the schema;
* the numbers behind `[Range]` / `[StringLength]` and the default from the initializer — say what the
  boundary *means* instead;
* the field list of a nested object.

And **facts that belong to an operation**. What the value *is* — meaning, origin, format, default, length,
range — belongs to the property. What the **handler does with it** — an unknown id skipped, a duplicate
counted once, an omitted entry left where it was, a blank string treated as a reset, one parameter
disabling another — belongs to the operation's description, even on a DTO only one operation binds today:
the same field can mean something else in another operation, and a fact written in both places drifts.

### 3.4 Response properties do a different job

A response property is read after the call, and its text reaches an agent mostly as SDK doc comments. Say:

* what `null` and an empty collection **mean** here — "no avatar has been set", not "may be null";
* in which states the field is populated at all;
* the unit, and which direction is better, for a measurement;
* what a returned list is ordered by, where the order is contractual.

Not the origin-and-format rules of §3.1: nobody fills these in.

### 3.5 One property, many operations

A type from `ASC.Core.Common`, `ASC.Web.Core` or `ASC.AuditTrail` is referenced from several services, so
its text is published into several documents. Before changing a property of a shared type, find the
operations that reference it and write a sentence true of all of them.

### 3.6 The schema name

The component name is the SDK model name. It comes from `OpenApiSchemaId.Of`
(`common/ASC.Common/Utils/OpenApiSchemaId.cs`): the class name, unless the type carries
`[OpenApiSchemaName("...")]`. The naming convention is §4 of `.claude/rules/api-models.md`; never `Wrapper`
for a body — the envelope filter would invent `…ResponseWrapper`/`…WrapperWrapper`. The attribute changes
the schema name only, never the JSON, and renames the model in every SDK.

## Definition of done

- [ ] Every property and enum member answers §3.1 and repeats nothing the schema prints (§3.3).
- [ ] Every scalar property has a realistic `<example>`, and the class-level block agrees (§3.2).
- [ ] Behavioural facts are not here but in the operation's description, shared DTO or single-use (§3.3).
- [ ] Response fields say what `null` and an empty collection mean and when they are populated (§3.4).
- [ ] Before editing a shared type, the operations and documents it reaches are known (§3.5).
- [ ] Tags are XML-well-formed and spelled correctly: a misspelled tag (`<ssummary>`) is silent in the
      whole C# toolchain.
