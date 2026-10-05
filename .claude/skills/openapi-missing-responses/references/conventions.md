# Conventions

How to judge a gap code (§1, §2, §4), how to word the attribute (§3), how to check behaviour (§5), how to edit
without damaging the file (§6) and what to do with a declared code the code cannot produce (§7). The exception → status table and the global filters of each host are the ones
`scan.py` prints on this run; the current phrasing is in the attributes of the neighbouring actions.

## Contents

1. The method's responses and the pipeline's
2. Model binding and validation → 400
3. Text conventions
4. Findings that are usually not responses
5. Checking behaviour: the local portal, a Kestrel stand
6. Editing the file safely
7. Declared codes the code cannot produce

## 1. The method's responses and the pipeline's

**The method's** — produced by the action, its controller and base classes, the services it calls, or the
contract of its own DTO: `throw`, `Demand*`, permission checks, DTO validation attributes and `required` body
members. These get attributes.

**The pipeline's** — produced the same way for many operations, whatever the action does. They belong to a
document filter, a separate task; mixing them in buries the batch's own diff:

- whatever the host's global MVC filters answer — the `global filters` line of the scanner output names them;
  open a filter to see its codes;
- the rate limiter (429);
- 401 for an anonymous caller of an action that requires authentication (an authentication exception deep in a
  service on that path is the same 401);
- tenant resolution failing before any action code runs;
- a callee that repeats, for the same caller in the same request, a check the authentication handler or a filter
  has already passed — it cannot fail where the first one did not;
- generic binding failures that any action with such a parameter has: a value of the wrong type in the route or
  the query (a non-numeric `{id}`, a GUID, a boolean, an unknown enum member), an empty or unparsable body. The
  document filter (`SwaggerCustomDocumentFilter`) gives every operation with parameters or a body a generic 400;
  how a declared 400 mentions them is in §3.

**An action with two hosts.** A project whose controllers another host also loads (`AddApplicationPart`) runs
under two pipelines, and the scanner prints an `also hosted by` line for it when the other host's exception table
or global filters differ. Today that is ApiSystem: on its own host every exception is 500 (its handler maps
nothing), while inside `ASC.Monolith` the table of `ASC.Api.Core` applies — `ArgumentException` 400,
`InvalidOperationException` 403 and so on — together with the monolith's global filters. So for such a project:

- document only the codes the action sets explicitly — `BadRequest(…)`, `StatusCode(…)`, a status returned by a
  service and passed on, a `catch` that answers with a code: they are the same in both hosts;
- an exception that reaches the handler is documented only after checking what it becomes in **both** tables; if
  they differ, name both, with the host each one belongs to ("500 on the standalone ApiSystem, 403 in the
  monolith"), rather than picking one;
- the other host's global filters are pipeline responses there, as in the list above.

Binding is split on purpose: what the DTO **declares** for this endpoint (validation attributes, `required`
members, `IValidatableObject`) is part of its contract and is documented; what binding does to any request of
that shape is not. Hence: a 400 whose only cause is "the body cannot be read" is never added on its own.

## 2. Model binding and validation → 400

ASP.NET behaviour under `[ApiController]` with the default binder providers and JSON options. If the host's
startup chain adds binder providers or changes `JsonSerializerOptions`, check the case on a stand (§5) before
relying on this list.

- Validation attributes (`[Required]`, `[Range]`, `[StringLength]`, `[EmailAddress]`, `[Url]`, custom
  `ValidationAttribute` subclasses) and `IValidatableObject` → 400 in any binding source, query included, and in
  nested DTOs. Without `[ApiController]` (or an attribute deriving from it) they do nothing unless the action
  checks `ModelState` itself.
- `[EmailAddress]`: `""` and malformed → 400; `null` or an absent property passes.
- `required` in a JSON body: an absent property → 400; `"x": null` passes unless `RespectNullableAnnotations` is
  on. `required` bound from the query or route gives no 400: an absent value arrives as the default
  (`Guid.Empty`). `required` on the `[FromBody]` property itself only restates "no body".
- Enum in the route: a case-insensitive name or a defined number passes; an undefined number or unknown name →
  400. A `[Flags]` enum accepts any combination of defined bits.
- Enum in a JSON body without `[JsonConverter]`: a name or a quoted number → 400; an undefined number passes
  into the method as is — see what the method does with it. With `JsonStringEnumConverter`: an unknown name →
  400; `"A, B"` and `"5"` are accepted.
- `Request.Form` on a request without a form Content-Type throws `InvalidOperationException` — its status is
  whatever the host's table maps it to.

## 3. Text conventions

- One line, no trailing period. After the 200 attribute, codes ascending, one attribute per code; several reasons
  for one code are joined in one sentence.
- Written from the caller's side — what is wrong with the request, the state or the caller's rights — not which
  exception is thrown. Identifiers in backticks.
- The same check gets the same words everywhere: before writing a text for a `Demand*` or a permission check,
  find an action that already documents that check and reuse its phrasing.
- An endpoint behind a confirmation link → "The account the confirmation link was issued for …".
- 400 from binding. A declared 400 replaces the generic "Bad Request." the document filter would add, so it keeps
  binding in one short opening clause and no more: "The request body cannot be read" (with "or has no `x` …" for
  `required` members) when the action takes a body, "A parameter has the wrong type" when it takes only route and
  query values and one of them is not a `string`. Then the action's own reasons.
- Never list type mismatches per parameter: not a number, not a GUID, not a boolean, not a date, an enum name or
  number the binder rejects (route and query enums, a body enum with `JsonStringEnumConverter`), a body enum sent
  as a string. The schema already states the type and the enum members. A constraint the schema does not show
  stays: a range, a length, a format such as "a date and time ending in `Z` or a UTC offset", a `string` the DTO's
  own validator requires to be a GUID ("An `ids` value is not a GUID"), and an undefined body-enum number the
  method itself rejects ("`roomType` is not a known room type"). Check the declared type before cutting.
- A 400 whose reasons are all type mismatches is not declared: the filter's generic 400 already covers it.
- A bound that comes from a shared constant (`[Range(1, ApiContext.MaxCount)]` on `count`) is not written as a
  number: "the `count` is outside its allowed range". The schema shows `minimum`/`maximum` from the attribute and
  follows the constant; a number in the text would go stale the day the constant changes. A literal bound that
  belongs to this endpoint alone ("longer than 170 characters") is still written out.
- A check with several conditions (for example a plan option and a visibility setting) names each condition that
  leads to the code.
- A bare `Exception` → 500 is documented, with what triggers it.
- An existing attribute whose code disagrees with what the host's table gives for the exception behind it is
  corrected to the table's code; a code the action sets explicitly (`CustomHttpException`, `StatusCode(…)`) is
  genuine as it is.
- 304: "… has not changed since the `Last-Modified` value sent back in `If-Modified-Since`; the body is empty", or
  the `ETag`/`If-None-Match` variant where the method compares an ETag.
- A code already declared with a narrower reason: rewrite that attribute's text to cover both; never a second
  attribute with the same code.
- An error attribute gets no response type, like its neighbours.
- An attribute the code cannot produce is removed, not kept "to be safe" — §7.

## 4. Findings that are usually not responses

| Finding | Why it may not be a response | How to tell |
|---|---|---|
| a `Demand` on an object the caller owns (their own profile, their own id) | a default ACE may grant the action to the owner role unconditionally | read the ACE defaults for that action and object; on another user's object it is usually a real 403 |
| a branch switched off by a literal argument | the call site passes a constant that skips the check | read the call site's arguments |
| a branch for a system account that never authenticates | no request can carry that identity | read how the account is created and whether authentication accepts it |
| `default:` / `_ =>` throw behind a route enum | binding already answered 400 | not for a query or body enum: an undefined number reaches the arm |
| an argument guard on an internal value | its input never comes from the request | trace the argument back to the request |
| an exception caught by a caller outside the class chain, or converted by a `catch` | the scanner checks `try/catch` only in the method that throws or calls | read the callers on the `via` path |
| `catch (...) when (...)` | the scanner treats a filtered catch as not catching | read the filter |
| a check that an earlier step of the same request already enforced | the earlier step answers first, with its own code | follow the action from its first line and see which check fails first |

A code that is reachable but exists by accident (a library failing on an edge input, a missing null check, an
exception type nobody chose on purpose) is still a response: document it as it is.

## 5. Checking behaviour

**Local portal.** The base URL is `baseUrl` in
`common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/appsettings.json`; `GET <baseUrl>/api/2.0/capabilities`
answering 200 means the portal is up. Ask the user for an account once per session and never write it into a
file. Token: `POST /api/2.0/authentication` with `{"userName": …, "password": …}` → `response.token`, then
`Authorization: Bearer <token>`. Use requests that only read or that fail before changing anything; anything that
creates or deletes data (a test user for a lower role, a room to delete) needs the user's consent first. A
portal that does not answer is not started by this skill — the user starts it.

**Kestrel stand** — for what ASP.NET decides (binding, `required`, validation, enum parsing, response shape),
never for DocSpace logic. In the session scratchpad: a `Microsoft.NET.Sdk.Web` project on the repository's target
framework, copies of the DTOs involved (keep `required` and the attributes), an `[ApiController]` controller that
echoes what it received, and the host's JSON options if they differ from the defaults. Build, run in the
background with `dotnet run --no-build --urls http://127.0.0.1:<free port>`, probe with curl, then stop the
process.

## 6. Editing the file safely

Edit with the Edit tool. `sed -i` in Git Bash rewrites the whole file to LF; a Python patch piped through a bash
heredoc can turn `\b` into a backspace. Some controller files have a UTF-8 BOM and some do not — what matters is
that the edit changes neither; `scan.py --diff-check` compares the BOM and the line endings with the snapshot.

## 7. Declared codes the code cannot produce

`.claude/rules/openapi-endpoint-docs.md` §3.6: a documented code the handler cannot reach is a defect, not
thoroughness — an agent reading the document builds a recovery branch for a case that never happens. So an
existing `[SwaggerResponse]` whose code no request to this endpoint can get is **removed**, in the same batch,
like a missing one is added.

The burden of proof is the other way round from a gap code. The scanner's `=` lines show what backs a declared
code in the action's own code, and `-` marks a declared code with no evidence there — but the scanner does not
follow services, so `-` is a lead, never a verdict. Remove only when you have traced every path that could
produce the code and can name the check that rules it out, as for a rejection:

- the exception behind it maps to another code in the host's table (`InvalidOperationException` is 403, not 409),
  and that other code is declared or added;
- a `catch` on the way converts it into something else (every `BillingException` rethrown as `Exception` → 500);
- the branch is unreachable for the reasons of §4 (a dead check, an identity that never authenticates, a check an
  earlier step already enforced).

Not removed:

- a code that is reachable but only through the pipeline (§1) — it is a real response; moving it to a document
  filter is a separate task;
- a code you could not trace to the end — say so in the report instead;
- a code the remarks of the action name: the remarks are not this skill's to change (`openapi-desc-opt`), and
  the rule requires every code named in the prose to be declared. Keep the attribute and report the contradiction.

When the removed code's triggers do happen under another code, make sure that code's text names them (the DNS
checks that once claimed 402 belong in the 500 text). Each removal goes into the report with the deciding check
and its `file:line`, so the user can undo it with one revert of that line.
