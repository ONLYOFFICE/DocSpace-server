# Conventions

How to judge a gap code (§1, §2, §4), how to word the attribute (§3), how to check behaviour (§5) and how to edit
without damaging the file (§6). The exception → status table and the global filters of each host are the ones
`scan.py` prints on this run; the current phrasing is in the attributes of the neighbouring actions.

## Contents

1. The method's responses and the pipeline's
2. Model binding and validation → 400
3. Text conventions
4. Findings that are usually not responses
5. Checking behaviour: the local portal, a Kestrel stand
6. Editing the file safely

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
- generic binding failures that any action with such a parameter has: a non-numeric `{id}`, an empty or
  unparsable body. The generator already gives every operation a default 400.

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
- 400 from binding: "The request body cannot be read or has no `x` …", then the other reasons. "Sent as a string
  instead of a number" for a body enum is appended only to a 400 that has another reason.
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
- An attribute the code cannot produce is left in place and named in the report.

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
