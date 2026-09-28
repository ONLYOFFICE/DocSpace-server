# Rules for building the list of controllers whose descriptions are not optimized

This document describes **how to build the list of controllers whose published endpoint descriptions
do not meet** `.claude/rules/openapi-endpoint-docs.md` and `.claude/rules/openapi-dto-docs.md`. It is the input for the loop in `SKILL.md`:
it reads the generated OpenAPI documents, checks them against the rule and produces a list holding
only the controllers whose texts nobody has rewritten yet.

---

## 1. The unit of the list is a controller class, not a file and not an operation

The population of the list is the published C# surface, taken from the sources and not from a
generated document. The criterion for a row is:

> a public method of a class derived from `ControllerBase` that carries a route attribute and is
> **not** marked `[ApiExplorerSettings(IgnoreApi = true)]`.

Hence the unit of the list is a **class**, including the descendants of a generic base:
`EditorController`, `EditorControllerInternal` and `EditorControllerThirdparty` — three rows of the
list out of one file `EditorController.cs`. The list is obliged to keep that breakdown: a finding on
a shared action has to be visible at every class that publishes it, even though one edit closes them
all (§3).

`aichat_2.0.json` (the public AI API, 103 operations) does not reach this list at all: it is written
by the Node/TS service `common/ASC.NewAi`, which has no C# controllers. If its texts are in scope
too, it needs a pass of its own — over the document, with no controllers to tie it to.

---

## 2. How an operation is tied back to a controller

**The document does not carry the controller's name.** `tags` is a group ("Files / Settings"), and
`operationId` is the C# method name without `Async` and with a lower-case first letter — the same for
`*Internal` and `*Thirdparty`, so neither identifies the class.

The working link is the normalised `summary`, matched against the `<summary>` of the action method.
It works precisely because the rule requires titles to be unique inside a document, which is also why
a `duplicate-summary` finding is worth more than its tier suggests: a duplicated title costs the next
run its ability to attribute the findings under it. Where titles do collide, the fallback is the
route — the verb plus the path with its constraints stripped and folded to lower case.

An operation nobody could tie to an action is counted as `unmatched` and listed at the end of the
file. It is never a finding: nobody can be asked to fix text that cannot be traced to a source.

---

## 3. What the document does to the routes, and why the count does not add up

* **Route constraints do not reach the document.** What is published is
  `/api/2.0/files/file/{fileId}`, not `{fileId:int}`. So an `*Internal` / `*Thirdparty` pair collapses
  **into one operation**, which is why ASC.Files indexes far more actions than the document has
  operations. Their text is shared — it is declared once in the generic base.
  **Consequence:** one finding lands in two rows of the list but closes with one edit. The list is
  obliged to show both rows (every class that publishes the action has to be findable), and the report
  of the work is obliged to say that the edit is one.
* **A route collision: there can be fewer operations than actions.** Two methods on the same
  `[HttpDelete("favorites")]` in `TagsController` (`DeleteFavoritesFromBody` and
  `DeleteFavoritesFromQuery`) give one operation.
* **Two attributes on one method are one operation too.** `GetRecentFolder` has `@recent` and
  `recent`; the document holds only `/api/2.0/files/recent`. The source walk sees both routes.
* **The casing of the path is not kept:** `api/2.0/settings/security/loginSettings` in the source
  against `loginsettings` in the document. Compare routes in lower case only.

---

## 4. The checks and their thresholds

The texts come from the operation in the document, compared after normalisation: case folded,
punctuation dropped, and the filler words (`the`, `for`, `current`, `returns` and the like) removed,
so that "The user ID." and `userid` count as the same words. `norm()`, `content_words()` and the stop
list itself are in `scan.py`.

The conditions below are the glossary of a finding name — they mirror the checks in `scan.py`, which
is the source of truth if the two ever disagree.

| Check | Condition | Tier | What it means |
|---|---|---|---|
| `no-description` | `description` empty | A | the action has no `<remarks>` — the agent has nothing to read |
| `no-summary` | `summary` empty | A | there is no `<summary>` |
| `no-param-description` | the parameter has no `description` | A | there is nothing to fill the field with |
| `thin-description` | fewer than 3 sentences | B | below the lower bound of §2 of the rule (three sentences minimum, 6–9 and 150–200 words as the norm) |
| `tautological-description` | at most 3 novel content words | — | **not a finding of its own, a field inside `thin-description`** — see §4.1 |
| `tautological-param` | `content_words(text) − content_words(name)` ≤ 1 | C | "The user ID." at `userid`; standing on its own it improves a text that already works, so it must not outrank an empty description — the queue splits in two, see §4.2 |
| `empty-response-text` | the text of a response code is 3 words or fewer | B | "OK", "Status", "Ok" instead of what actually came back |
| `duplicate-summary` | the title occurs twice in the document | C | breaks the uniqueness requirement of §1 of the rule |
| `long-summary` | more than 6 words | C | a title has to be 2–6 words |

Two things the counting has to get right, and does: a `` `GET api/2.0/files/fileops` `` inside the
prose is not three sentences, and a description is "novel" only in the words it adds beyond the title,
the method name and the path — otherwise every operation would look documented by virtue of repeating
its own route.

**What these checks do not see:** whether the text is true, whether the permissions and the
preconditions are named, whether asynchrony is mentioned, whether a deprecated operation names its
replacement. A clean report ≠ a finished document; the checks show where to look, not what to write.

### 4.1. A tautological operation description is a symptom, not a finding

A description that retells its own title is always also a short one, and that is arithmetic rather
than luck: "adds almost nothing beyond the title, the method name and the path" is unreachable in a
text of 150–200 words, because a text that long inevitably names the permissions, the response codes
and the neighbouring operations. So a tautological description is a subset of `thin-description` by
construction, and a queue that lists both gives one operation two rows.

**The rule:** do not put it in the queue as a row of its own. Print it as a field of the
`thin-description` row: `novel words: N`. It orders the work inside the tier — `0` is a plain
retelling of the title ("Get ai agents" under the title "Get ai agents"), a higher number means the
text already says something. The population of the list does not change; the order of the walk
does.

### 4.2. A tautological parameter or property — the queue splits by owner

The converse of §4.1 does not hold: the quality of the `remarks` does not flow into the description
of a parameter. An operation can carry a fully rewritten description — asynchrony, permissions,
destructiveness, the alternative operation — and still bind a `fileId` that says "The file to
delete.", which is the one thing an agent cannot derive: where the identifier comes from, and what a
numeric value addresses as against a string one.

**The rule:** print the length of the owner's description in the row of the finding, and split the
queue:

* **a parameter at a `thin` operation** is part of the work on the operation itself and is not raised
  as a row of its own — rewriting the operation closes it;
* **a parameter at a normal description** is a short queue of its own, "common identifiers", grouped
  **by the source object and not by the operation**: one edit to `fileId` closes it in every operation
  that binds it, so a queue grouped by operation would send several passes into the same DTO.

The same split applies to a tautological property: group by the object, otherwise the queue sends
three different passes into one file.

---

## 5. Filters for false positives

1. **Do not count the generator's default response texts.** The `400`, `401`, `429`, `500`, `502` and
   `503` texts are printed by a filter rather than by `[SwaggerResponse]`, so no controller edit
   closes them. Two mechanisms drop them: a list of the known wordings, and a frequency guard for the
   ones nobody enumerated — a "code + text" pair repeated across a large share of a document's
   operations is a default by definition. If such a text ever reaches the queue, the hole is in the
   filter, not in the controller.
2. **`*Wrapper` schemas are out of scope** — they have no source file.
3. **The check against the source.** For every operation, compare the published `description` with
   the `<remarks>` behind it and treat a divergence as a stale document rather than as a finding.
   The comparison is fuzzy on purpose: the generator reflows the text, so an exact match would call
   every operation stale. On freshly regenerated documents the count of divergences is zero or close to
   it; anything more means the documents were not regenerated before the run (SKILL.md step 1).
4. **A controller with no visible operations does not reach the list**:
   class-level `IgnoreApi`, every method hidden one by one, an abstract base with no actions. A
   class-level `<summary>` does not reach the document at all.
5. **Documents outside the public bundle** (`ai_2.0.json`, `apisystem_common.json`) are scanned but
   marked apart: their descriptions only ever reach the service's Scalar UI. They are out of the
   default scope and stay one `--scope` flag away; taking them in is a decision for whoever owns the
   campaign, not for the pass.

---

## 6. The format of the list and the inclusion threshold

The structure of the list: a section per assembly, controllers inside it, sorted by the number
of findings. The row of a controller carries: the class name, the file, the number of affected
operations, the per-tier counts and the breakdown by check. A row that has been worked simply
disappears from the next scan; what closed it is the commit that closed it, and the manifest of
§6.4 is what proves the disappearance was a rewrite rather than an accident.

**The list is printed as two sections, and what is split between them are findings, not
controllers**: "Must fix" (tiers A and B) and "Recommended" (tier C). A controller with findings of
both kinds stands in both sections, each time with only its own findings and with a pointer to how
many it has left in the neighbouring one. So the row counts of the sections must not be added up;
only the findings add up.

**The threshold sets the size of the list, and it has to be chosen explicitly** — `--threshold A`,
`AB` (the default) or `ABC`. Each step widens it by a whole kind of work: A alone is the handful of
places with no text at all, B adds the ones whose text says nothing, C adds the form of a title,
which is a different order of magnitude because nearly every controller has one somewhere.

The recommendation: **A and B go into the list, C is written as a note in the row.** A title longer
than six words is a minute of work, and it must not push a controller with empty descriptions out of
the queue.

The per-tier counts are computed **after** the merge of §4.1 and the split of §4.2: otherwise one
operation with a one-sentence description gives three rows (`thin`, `tautological-description` and a
row for each of its tautological parameters) and looks three times as heavy as it is. The population
of the list does not change because of that — the order of the walk changes, and that order is exactly
what an error costs.

---

## 6.1. An edit site is the unit of work, unlike a row of the list

A row of the list is a controller (§1), because a controller is the unit of the C# surface. But
what a pass types is not a controller but an **edit site**: the thing that is physically edited. Those
two counts diverge, and in both directions.

The key of an edit site:

| Kind of finding | Key | Why |
|---|---|---|
| `tautological-param`, `no-param-description` | document + parameter name | the text lives on a DTO property; one edit closes every operation that binds it and every controller where those are declared (§4.2) |
| everything else | document + file + operation | the edit is on the action; an action from a shared base, handed to `*Internal` and `*Thirdparty`, is one site and two rows of the list |

The two counts diverge hard in practice: a dozen `tautological-param` findings in one document can be
two DTO properties — so a queue read as rows would send four passes into the same file — while a
dozen `long-summary` findings in that same document are a dozen separate titles, where a row really
is a site.

## 6.2. The weight of a site and the budget of a pass

The limiting resource of this loop is not build time but careful prose: the twelfth description
written in a row in one pass is worse than the first. So the budget is counted in the weights of
sites, not in rows and not in findings.

What each kind of site costs, and why (`SITE_WEIGHTS` and the budget band in `scan.py` hold the
numbers):

- **a description written from scratch** is the expensive one — the handler is read end to end and
  150–200 words come out of it. Two of those are a full pass, and a third no longer fits, which is
  the point of weighting them at all;
- **a DTO property** is cheap to type and expensive to get right, because the sentence has to be true
  for every operation that binds it — hence a surcharge per consuming operation beyond the first;
- **a title or a response text** is one attribute; the handler is read to confirm a fact, not to be
  retold.

The budget is a band, not a ceiling. The ceiling is what a pass may carry; the floor is not an
admission threshold but a guard against fragmentation — it keeps a split from leaving a remainder
that a whole pass would be wasted on.

The floor is not always reachable: a group slightly over the ceiling splits into two halves of which
one is always under the floor. Such a pass is marked as underloaded rather than padded — the reader
has to see that it is light by arithmetic and not because something fell out of it.

## 6.3. The axis of a batch and what falls out of it

A batch is **(document × check)**, then cut along files.

- **The document** — because the build and the regeneration are done per project, and the guarantee of
  step 7 (`controllers`, `operations`, `actions` have not moved) only reads cleanly inside one
  document.
- **The check** — because one check is one shape of edit, one section of the description rule and one
  thing to review. A diff that mixes rewritten titles with rewritten DTO properties is the same problem
  the skill already forbids when it keeps tier C out of a tier B pass.
- **A file is not split between passes**: the sites of one file travel together. A file opened by two
  passes is one diff reviewed twice, and the second pass can no longer say which of the two edits
  closed what.

Taken as a pass of its own, outside the batch:

1. a finding whose fix needs a decision about behaviour (an unreachable response code, a parameter
   that does not do what its name promises) — that is not a text edit at all;
2. a shared DTO property reaching further than one document: the blast radius leaves the document
   whose counts step 7 checks. This cannot be proved mechanically — two documents sharing a parameter
   name does not yet mean one property — so the script **names such names in a list of their own**, and
   the decision stays with the pass.

## 6.4. The manifest: what makes a batch verifiable

A batch declares the expected diff **before the first edit** — otherwise it is unverifiable. While a
pass was one row, the wording "the diff touches only the expected rows" was enough: it was checked by
reasoning. Over six sites reasoning does not work.

The manifest (`batch-manifest.json`, next to the list, also outside git) holds the declared set of
findings, a full slice of the open findings at the moment of the declaration, and the counts of the
documents. The check after the edit is set equality, with three outcomes:

- **declared and closed** — what the pass was for;
- **declared and still open** — exactly which edit did not work, and the remainder becomes the next
  batch;
- **closed but never declared** — usually good news (a shared property reached further), but it has to
  be named rather than discovered later.

Plus a separate check of the counts: a row that disappeared because the parsing broke reads exactly
like a row that was fixed, and only the immobility of
`controllers`/`operations`/`actions`/`unmatched` tells them apart.

The identity of a finding is `document|controller|operation|check`. All four parts are printed in the
list itself, so a mismatch is read with the eyes rather than debugged.

The open findings for the comparison are taken from **both** sections of the list. A finding whose fix
only lowered the tier from B to C is not closed; a set collected from the "Must fix" section alone
would have written it down as closed.

---

## Related files

- `.claude/rules/openapi-endpoint-docs.md`, `.claude/rules/openapi-dto-docs.md` — how the text itself
  has to read.
- `SKILL.md` — the loop that consumes this list.
- `references/writing-traps.md` — what this codebase does to the text you edit.
- `scan.py` — the implementation, and the source of truth for anything mechanical: the thresholds of
  §4, the filters of §5, the weights of §6.2 and the expected surface all live there as constants.
  This file explains what they are for; it does not restate them.
