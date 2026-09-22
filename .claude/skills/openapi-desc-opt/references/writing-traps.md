# Writing traps: what bites when you rewrite an endpoint's text in this repository

`.claude/rules/openapi-endpoint-docs.md` (the operation) and `.claude/rules/openapi-dto-docs.md` (the
property) say what good text is. This file says what this codebase
does to text that looks fine in the editor — the handful of mechanisms that swallow an edit, publish
it somewhere else than intended, or turn a documentation pass into a behaviour change.

Read it in step 4, before the first edit. Add to it whenever a pass costs you an hour to work
something out, with the date and what proved it. Keep measurements out — those belong in the run
report, because they change with every run.

## The generator

**The `<summary>`/`<remarks>` mapping is inverted relative to C# habit** (rule §0), and swapping
them fails silently: the build says nothing, and the title and the description trade places across
the contract and all eight SDKs.

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

**A parameter's text comes from the bound DTO property, which many operations share.** Rewriting a
common identifier once improves every operation that binds it — that is the whole point of the
"common identifiers" queue in the list rules (§4.2) — but it also means the text must be true of
every one of them. Anything true of a single operation belongs in that operation's `<remarks>`.

**Response texts come from `[SwaggerResponse(code, "text")]`, and the generator adds its own on top.**
A filter prints the standard client- and server-error texts for every operation, so no controller
edit changes them; the scan drops them before the checks run. If a finding ever names one, the filter
has a hole — fix the filter, do not "fix" the controller.

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

**One exception: adding the 200 an action already returns is not a contract change.** An action with
no `[SwaggerResponse(200, ...)]` is published with the generator's own `"OK"`, which is exactly what
`empty-response-text` fires on — and on a `Task`-returning action there is no other code to add. So
writing `[SwaggerResponse(200, "<what the caller now knows>")]` where none stood replaces a default
text with a true one and adds no code to the contract. It stops being this case the moment the code
is one the handler may not reach — then it falls back under the paragraph above.

**Say where an identifier comes from.** The real gap behind a `tautological-param` finding is
usually not the wording but the origin: which operation hands out this ID, and what a numeric value
addresses as against a string one (an entry of the portal versus one on a connected third-party
account). Rewording the sentence without answering that closes the check and not the gap.
