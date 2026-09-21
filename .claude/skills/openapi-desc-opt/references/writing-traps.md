# Writing traps: what bites when you rewrite an endpoint's text in this repository

`.claude/rules/openapi-endpoint-docs.md` says what good text is. This file says what this codebase
does to text that looks fine in the editor — the handful of mechanisms that swallow an edit, publish
it somewhere else than intended, or turn a documentation pass into a behaviour change.

Read it in step 4, before the first edit. Add to it whenever a pass costs you an hour to work
something out, with the date and what proved it. Keep measurements out — those belong in the run
report, because they change with every run.

## The generator

**`<summary>` becomes the operation's `summary`, `<remarks>` becomes its `description`.** Inverted
relative to ordinary C# habit: the short title goes in `<summary>`, the long explanation in
`<remarks>`, and the `<remarks>` block is written first. Swapping them silently exchanges the title
and the description across the published contract and all eight SDKs.

**A second `<remarks>` block on the same action is dropped without a word.** Only the first one
reaches the document. If an action seems to ignore a rewrite, count the blocks before doubting the
build — and merging two blocks into one is a legitimate way to make the text shorter, not a loss.

**An XML `<param>` tag cannot describe a route placeholder that no DTO property binds.** Swashbuckle
matches `<param name="x">` against the *method's* parameters, and these controllers take a single DTO
argument, so the tag matches nothing, reaches no document, and costs two compiler warnings on the way
(`CS1572`, plus `CS1573` on the DTO argument). The text for such a placeholder goes in
`[SwaggerPathParameter(name, description)]` on the action. Reach for it only when the placeholder
genuinely cannot be bound: if a DTO property can carry the value, `[FromRoute]` plus an XML
`<summary>` on that property both documents and binds it, which is strictly better.

**A parameter's text comes from the bound DTO property, which many operations share.** Rewriting
`fileId` in one place improves six operations at once — that is the whole point of the "common
identifiers" queue in the list rules (§4.2) — but it also means the text must be true of every one of
them. Anything true of a single operation belongs in that operation's `<remarks>`.

**Response texts come from `[SwaggerResponse(code, "text")]`, and the generator adds its own on top.**
`400 Bad request.`, `401 Unauthorized`, `429 Too many requests.`, `500 Internal server error.`, `502`,
`503` are printed by a filter for every operation; no controller edit changes them, and `scan.py`
already filters them out. If a finding names one of these, the filter has a hole — fix the filter,
do not "fix" the controller.

**A doubly-misspelled XML tag is silent in the whole C# toolchain.** `<ssummary>` compiles, warns
about nothing, and simply never appears in the document. When an edit does not show up after a
genuine regeneration, check the spelling of the tag before anything else.

**`*Internal` and `*Thirdparty` share one declaration.** The action lives once in the generic base,
so one edit closes two rows of the list and shows up as a single operation in the document. Say so in
the report: two rows disappearing from one edit is the expected arithmetic, not a windfall.

## Truth, not just prose

**A documentation pass must not change behaviour.** Discovering that the handler is wrong is a normal
outcome of reading it closely; fixing it inside this pass is not. Describe what the code does, tell
the user what you found, and let them decide whether a behaviour change is in scope.

**Do not invent reachable status codes.** Several controllers here carry `[SwaggerResponse]` sets
that describe intent rather than what the handler can actually return — a bare `Exception` surfaces
as 500, an `InvalidOperationException` as 403, a quota refusal as 402. The `[SwaggerResponse]` set is
part of the contract and the SDKs; changing which codes are listed is a contract change, so treat the
set as frozen unless the user asked for it, and make the *texts* accurate instead.

**`ApiDateTime` is shifted twice.** Never write that such a value is "in UTC" or "in the portal's time
zone" — both readings are wrong for at least some callers, and an agent that trusts either will send
back a time that lands hours away.

**A positional `record`'s `<param>` example leaks into the description.** For positional records the
`<example>` never reaches the schema and the text ends up inside the property description instead, so
those DTOs have no example coverage to speak of. Do not chase examples there in a controller pass.

**Say where an identifier comes from.** The most common real gap behind a `tautological-param`
finding is not wording but origin: which operation hands out this ID, and what a numeric value
addresses versus a string one (portal entry versus an entry on a connected third-party account).
