---
paths:
  - "**/*Controller.cs"
---

# Endpoint documentation (OpenAPI summary, description, responses)

The published documents become MCP tools and SDK doc comments: an agent picks the operation, fills the
parameters and reads the answer from this text alone. Goal: the fewest words with no lost fact, in English
that a reader with basic English can follow. Parameters, DTO properties and enum members are
`.claude/rules/openapi-dto-docs.md`.

## 0. Where the text comes from

| OpenAPI field | Source in C# |
|---|---|
| `summary` | `<summary>` on the action — the **short title** |
| `description` | `<remarks>` on the action — the **long explanation** |
| parameter `description` | `<summary>` on the bound DTO property, or `[SwaggerPathParameter(name, description)]` |
| response descriptions | `[SwaggerResponse(code, "text", typeof(T))]` |
| `tags` | `[Tags("Files / Operations")]` |

The `<summary>`/`<remarks>` split is inverted relative to ordinary C# habit. Write `<remarks>` first and never
swap them: a swap exchanges every title and description across the contract and all the SDKs.

`<path>`, `<collection>`, `<requiresAuthorization>` are facts: keep them true (`requiresAuthorization=false`
only with `[AllowAnonymous]`).

An action or controller under `[ApiExplorerSettings(IgnoreApi = true)]` is documented to the same standard:
the attribute is a publication switch, and the text under it gets published when someone flips it.

## 1. `summary` — the title

* A noun phrase or verb+object, 2–6 words, sentence case, no trailing period: `Bulk download`.
* **Unique within the document**: it is the sidebar entry, the Postman request name and the SDK method's
  doc comment.
* Never the HTTP verb and path (`Post fileops copy`), the C# method name, or "API" / "method" / "endpoint".
* A title, not a sentence: no preconditions, permissions or errors.

## 2. `description` — the contract in prose

Before writing:

* **Start from the code of the branch**: the action and the services it calls. The current text and the
  published JSON can be wrong. Verify every fact you keep, and check each item of the list below against the
  code and the declared responses, even when the current text never had it.
* **Know what reaches the agent**: the description, the parameters and body properties with their texts,
  sometimes the schema of the 200 response. Response descriptions and headers never do. Look for each fact
  in the parameters, the body properties, their nested schemas and array items, and the 200 response:
  * a fact about the **value** — its meaning, where a valid one comes from, format, default, length, range —
    stays on the property only;
  * a fact about **behaviour** — what the call does with the value, how it compares it, which parameter
    wins, what an empty or zero value means, what a flag switches, what a repeat does — is in the
    description, even when a property states it too;
  * a fact that only a response text or a header carries goes into the description.
* **The text makes sense without the parameter texts.** State the fact or name the parameter ("the minimums
  of `quantity`"), never "minimums apply".

What stays, in this order; leave an item out only when it does not apply:

1. **What it does**, in the first sentence: a present-tense verb (GET → Returns, POST → Creates/Starts,
   PUT → Changes/Updates, DELETE → Deletes), the resource and the scope. An effect that depends on the caller
   or a mode is said here: `Changes the email address of an account, or sends a link that confirms the
   change.` Modes that define the call stay here even when a parameter describes them. A second sentence
   may explain the first. A deprecated operation says so here and names the replacement. If an older
   description is correct, start from it.
2. **Preconditions**, with the operation that satisfies them.
3. **Access**: a phrase of §2.3, then the refusal code in its own sentence: `Other callers get 403.` Check
   in the code which code a refusal really returns.
4. **Effect**: read-only, changes data, spends money, cannot be undone, or queues work. Then:
   * a change says whether it can be repeated: `You can repeat it with the same body.` / `Do not repeat it.
     Each call sends the emails again.` / `A repeated call returns 404.`;
   * queued work names the call to poll and the field that signals the end: "Repeat
     `GET api/2.0/files/fileops` until `finished` is true."; the status call itself says "This is the call to
     poll. Repeat it until `isCompleted` is true.";
   * no `idempotent` next to `read-only`.
5. **What comes back**, only what the schema does not make clear: a body that is not JSON, what `true` and
   `false` mean, the order of a list, paging and whether there is a total, what an empty result means,
   fields filled only in some states, skipped items in a list, the meaning of a field that its schema text
   does not give. When the body DTO is shared and this call reads only some fields, name them: "The call
   reads only `userId` and `email` from the body."
6. **Limits and related operations**: a limit that only a response publishes, with its code (`More calls
   return 429.`); what the operation does not do and what to use instead: "To <task>, use
   `METHOD api/2.0/…`."
7. **Errors the caller can act on**, in the forms of §2.3.

What goes: facts about the value that a property already states; reasons (`because …`), restatements and
the schema retold; side effects the caller cannot see — socket pushes, audit records, internal caches (a
cache that makes the answer stale stays); internal names — service classes, exception types, DB columns,
tracker numbers; the webhook sentence (§2.1).

### 2.1 Webhooks the operation raises

An action that causes a webhook — directly, through a service, or through a queued operation it starts —
carries `[RaisesWebhook]` (`ASC.Webhooks.Core`). A filter turns it into the closing sentence of the
description and into `x-docspace-webhooks`, and `docspace-webhooks.yaml` lists the operation from there.

* Triggers by the enum (`WebhookTrigger.FileCreated`), never by string.
* One attribute per condition; triggers raised under the same condition share one. Leave `When` out when
  the action always raises it. The whole sentence "Raises the `x` webhook <When>." has at most 20 words and
  an active verb: ``When = "only when `autosave` is true"``.
* State what a receiver cannot guess: per item of a list, per nested entry or not, on completion of a queued
  operation, when nothing changed, when the webhook is *not* raised although the data changed.
* No mention of the webhook in `<remarks>`. `IgnoreApi` actions get the attribute too.
* A new `WebhookManager.PublishAsync` / `UserWebhookManager.PublishAsync` call: annotate every action that
  reaches it (LSP `incomingCalls`). A publish no action reaches goes into `_otherSources` in
  `common/Tools/ASC.Webhooks.Contract/ContractHeader.cs`.
* After a change, re-emit the API documents (stage A of the `generate-sdk` skill), then the contract
  (`dotnet run --project common/Tools/ASC.Webhooks.Contract`): it reads the documents on disk.

### 2.2 Form

* **One paragraph**: no labels, lists, tables or HTML. Escape or avoid `<`, `>`, `&`.
* **At least 3 sentences, each at most 20 words**; aim for 8–12, and keep the average at most 12. Code in
  backticks is one word, a hyphenated word is two. Count with a tool (`check_texts.py` of the
  `openapi-desc-opt` skill), not by eye.
* **No word limit**: the facts set the length. The text is at its minimum when each sentence states a fact
  no other states, and one more shortening pass removes less than 5% of the words. Over ~250 words, move the
  details of a mode to its enum member and keep one sentence per mode.
* **Shortening pass** — never at the cost of a fact, a status code, the refusal code or the repeat
  statement: several cases of one code in one sentence after the verb (`The call returns 404 for an unknown
  top-up, a top-up without an invoice, or no billing customer.`); a precondition written only as its error;
  each fact once; a limit inside its error sentence, as `too long` when the schema publishes `maxLength`; two
  sentences with one subject merged when together they stay within 20 words; a shorter wording with the same
  meaning (`A deleted account returns 404.`).
* **One link between facts per sentence, and the link is a word.** A colon only before a list. An
  instruction and how to do it, a fact and its result, a condition and its error are two sentences. Name a
  link that is not obvious: `First`, `Then`, `So`, `After this call`.
* **Active voice** for an action: `The call skips LDAP accounts`. A state may be passive: `is turned off`.
* **Lists**: a comma before `and`/`or` from three items; the list follows the verb, never as its subject.
* **Conditions**: a short one before the action (`If no tag has the old name, the call returns 404.`), a
  list of them after the verb.
* **Avoid**: `…, or the call returns 403` meaning "otherwise" → `If …, the call returns 403.`; `unless` with
  a negation → `Only Y can delete X.`; inversion (`So does a blank name`) → `A blank name also returns
  400.`; a sentence that starts with code or a path → "To start a job, use `POST api/2.0/…`."
* **Plain words** with one direct meaning. Instead of → write: `turns true` → `becomes true`; `at once` →
  `immediately`; `goes to` (a file, a letter) → `the portal saves … in`, `sends … to`; `drops` → `deletes`
  or `no longer shows`, say which; `hand over` → `move … to another user`; `used up` → `after the portal
  spends …`; `by itself` → the doer (`the portal turns it on`); `comes back again` → what happens;
  `in place` → `has`; `after a while`, `a normal session` → the number or the exact thing, else an
  instruction; `afterwards` → `After this call`; `also the skipped ones` → `including the skipped ones`;
  `erasure` → `deletion`; `ready-to-show`, `free-form` → `a text you can show to the user`, `any`;
  `health` of a service → `if the service works`; `client` (a program) → `app`; a chain of nouns → a verb.
* **Notation**: operations in backticks, `` `GET api/2.0/files/tags` ``, and so one word; status codes as
  numbers; fields and values in backticks.

### 2.3 Phrases and terms

Access, then the refusal code (`Other callers get 403.`, or `A guest gets 403.` after "except a guest"):

* `Requires a DocSpace admin.`
* `Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.`
* `Any signed-in user can call it, except a guest.` / `Any signed-in user can call it.`
* `Anyone can call it without signing in.`
* `Sign the request with the confirmation token from the emailed link.`
* `Requires editing access to the file.` / `Requires the right to create content in the room.`
* a role that seems enough but is not: `A room admin can create tags but also gets 403 here.`

Errors, with `returns` as the only verb for a code (a caller `gets` one): `If <short condition>, the call
returns N.` / `The call returns N if <a>, <b>, or <c>.` / `It also returns N if <condition>.` / `A repeated
call returns N.`

One term per concept, in every text and as the DTOs use it:

* user types: `DocSpace admin`, `room admin`, `user`, `guest` (`[Description]` of `EmployeeType`); room
  roles: `room manager`, `content creator`, `editor`, … (`[Description]` of `FileShare`). The user type room
  admin is not the room role room manager: qualify them when both appear;
* `the portal owner`; `portal` for one DocSpace, `installation` for the server with all its portals;
* `account` for a person's record, `profile` for the returned object; `storage account`, `wallet
  sub-account` — never a bare `account` for these.

## 3. Parameters and DTO properties

Their texts are `.claude/rules/openapi-dto-docs.md`. A behavioural fact belongs to this description, never
to the property (§3.3 there); editing a shared property changes every operation that binds it (§3.5 there).

### 3.6 Responses

* One `[SwaggerResponse]` per code the operation can return, its text saying what the code *means here*:
  not "Bad request" but "The destination folder is a room the caller cannot write to".
* A declared code is a candidate, not proof: check that the code can return it. A documented code the
  handler cannot reach is a defect. Every code named in the prose has a `[SwaggerResponse]`.
* Every 4xx and 5xx code of the old text and of the declared responses is in the new text, or has a reason
  to be out: 401 for a missing sign-in, 500, 502 and 503 of the pipeline, a code a parameter's type rules
  out.

## 4. Consistency

* Tags are the namespace (`Files / Operations`); a multi-step flow names the other steps in order.
* Self-contained: no "see above", no neighbour's text, no links to UI pages or wikis.
* Not lazy (`Gets the file.` on `GET .../file/{id}`, an "Ok" response text), not a tautology of the method
  name, not untrue (a stale `<requiresAuthorization>`, an unreachable 404, the previous handler's
  behaviour): an agent trusts wrong text.

## 5. Example: `PUT api/2.0/files/tags`

> Renames a custom room tag of the portal. Every room with this tag then shows the new name. Use the old
> name exactly as `GET api/2.0/files/tags` returns it. Requires a DocSpace admin. Other callers get 403. A
> room admin can create tags but also gets 403 here. The call changes data, and a repeated call returns
> 404. It returns the new name. After the rename, a `tags` filter of `GET api/2.0/files/rooms` with the old
> name finds no rooms. If no tag has the old name, the call returns 404. The call returns 400 if a name is
> empty, has only spaces, or is too long. It also returns 400 if any tag already has the new name, even
> this tag. So you cannot merge two tags.

## 6. Definition of done

- [ ] `<summary>` is a unique 2–6 word title; `<remarks>` holds the description (not swapped).
- [ ] Every fact is checked in the code of the branch; the items of §2 that apply are there, in order.
- [ ] Access is followed by the refusal code; a change says whether it can be repeated; queued work names
      the call to poll and the field that signals the end.
- [ ] Behavioural facts are in the description; facts about a value are not repeated there.
- [ ] One paragraph, at least 3 sentences, none over 20 words by a tool's count, average at most 12.
- [ ] The shortening pass of §2.2 was run on the finished text. If it removed 5% of the words or more, the
      shorter text was kept and the pass and the counts were run again.
- [ ] Every reachable code has a `[SwaggerResponse]` with a specific text; no code was lost without a reason.
- [ ] Every webhook, under every condition, has `[RaisesWebhook]` (§2.1), and `<remarks>` does not repeat it.
- [ ] Terms of §2.3; no internal names; `<path>`, `<collection>`, `<requiresAuthorization>` are true.
- [ ] Tags are XML-well-formed and spelled correctly: a misspelled tag (`<ssummary>`) is silent in the whole
      C# toolchain.
