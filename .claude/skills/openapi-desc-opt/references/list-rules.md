# Rules for building the list of controllers whose descriptions are not optimized

This document describes **how to build the list of controllers whose published endpoint descriptions
do not meet** `.claude/rules/openapi-endpoint-docs.md`. It is the input for the loop in `SKILL.md`:
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

The C# surface as a whole: **705 actions, 75 controllers, 6 assemblies.**

| Assembly | Controllers | Rows | Document |
|---|---|---|---|
| `web/ASC.Web.Api` | 26 | 254 | `api_2.0.json` |
| `products/ASC.Files/Server` | 29 | 318 | `files_2.0.json` |
| `products/ASC.People/Server` | 13 | 91 | `people_2.0.json` |
| `products/ASC.AI/Server` | 3 | 14 | `ai_2.0.json` |
| `common/services/ASC.Data.Backup` | 1 | 14 | `backup_2.0.json` |
| `common/services/ASC.ApiSystem` | 2 | 14 | `apisystem_common.json` |

`aichat_2.0.json` (the public AI API, 103 operations) does not reach this list at all: it is written
by the Node/TS service `common/ASC.NewAi`, which has no C# controllers. If its texts are in scope
too, it needs a pass of its own — over the document, with no controllers to tie it to.

---

## 2. The order of the steps

### Step 1. Regenerate the documents — before anything else

`json/*.json` are committed artefacts. Any finding taken off a snapshot older than the sources is
false, and the gap is usually small enough to miss: an action rewritten an hour after the last
regeneration still shows its old one-sentence description in the committed document, and a check run
on that document reports a finding that was closed days ago.

If regenerating is impossible, step 5 ("the check against the source") becomes mandatory, and the
divergences are marked in the report rather than in the list.

### Step 2. Collect the population of controllers

By a walk over the sources. For every `.cs` in the project: the class declaration, a check for
`IgnoreApi` in the attribute block **above the class**, then every method whose attribute block holds
`[Http(Get|Post|Put|Delete|Patch)]`. Files listed in `<Compile Remove>` are dropped before counting.

Control: the number of visible action methods must agree with the number of operations in the
document. Reference figures: api 254 = 254, people 82 = 82, backup 14 = 14, ai 14 = 14,
apisystem 14 = 14. Files gives 216 against 215 — the discrepancy is explained in §3.

### Step 3. Tie an operation of the document to a controller

**The document does not carry the controller's name.** `tags` is a group ("Files / Settings"), and
`operationId` is the C# method name without `Async` and with a lower-case first letter, and it is the
same for `*Internal` and `*Thirdparty`.

The working link is the normalised `summary`: strip `///` line by line, remove the XML tags, collapse
the whitespace, fold the case, and match it against the `<summary>` of the action method. It works
precisely because the rule requires titles to be unique inside a document. Reference figure:
**all 593 operations** of the six C# documents match this way, `unmatched = 0`.

The second way, when titles are duplicated, is by route: `verb` plus the path with its constraints
stripped (`{fileId:int}` → `{fileId}`) and folded to lower case. Comparing both sets gives full
agreement over all six assemblies with exactly two explainable divergences (see §3).

### Step 4. Run the checks over the operations

The list of checks and thresholds is §4. Only those that concern the endpoint are counted: the
description, the title, the parameters, the texts of the response codes. The schema-level checks
(`no-property-description`, `tautological-property`, `no-example`, `placeholder-example`) do not
reach the list of controllers — their unit is different (a DTO) and so is the blast radius of the
edit.

### Step 5. Filter out the false positives

§5. Without this step the list lies in both directions.

### Step 6. Assemble the list

§6 — the format and the inclusion threshold.

---

## 3. What the document does to the routes, and why the count does not add up

* **Route constraints do not reach the document.** There is not a single path with a `:` in
  `files_2.0.json` — what is published is `/api/2.0/files/file/{fileId}`, not `{fileId:int}`. So an
  `*Internal` / `*Thirdparty` pair collapses **into one operation**: 318 actions of ASC.Files
  give 215 operations in the document. Their text is shared — it is declared once in the generic base.
  **Consequence:** one finding lands in two rows of the list but closes with one edit. The list is
  obliged to show both rows (every class that publishes the action has to be findable), and the report
  of the work is obliged to say that the edit is one.
* **A route collision: there can be fewer operations than actions.** Two methods on the same
  `[HttpDelete("favorites")]` in `TagsController` (`DeleteFavoritesFromBody` and
  `DeleteFavoritesFromQuery`) give one operation — hence 216 actions against 215 operations in Files.
* **Two attributes on one method are one operation too.** `GetRecentFolder` has `@recent` and
  `recent`; the document holds only `/api/2.0/files/recent`. The source walk sees both routes.
* **The casing of the path is not kept:** `api/2.0/settings/security/loginSettings` in the source
  against `loginsettings` in the document. Compare routes in lower case only.

---

## 4. The checks and their thresholds

The texts come from the operation in the document. Normalisation: `norm(t)` — lower case, every run
outside `[a-z0-9]` turned into a space; `content_words(t)` — the words of `norm(t)` longer than two
characters, minus the stop list:

```
a an the of to for in on by with and or is are was were this that it its be as from at all
if when which what current specified given new used use uses set get gets returns return
```

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

Counting sentences: first replace `` `code` `` with a placeholder (the dots in
`GET api/2.0/files/fileops` are not sentence ends), then cut on `[.!?]` plus a space and drop the
pieces shorter than two words.

The novelty of a description: `content_words(description) − content_words(summary) −
content_words(split_camel(operationId)) − content_words(the path without its slashes)`.

**What these checks do not see:** whether the text is true, whether the permissions and the
preconditions are named, whether asynchrony is mentioned, whether a deprecated operation names its
replacement. A clean report ≠ a finished document; the checks show where to look, not what to write.

### 4.1. A tautological operation description is a symptom, not a finding

Over all seven documents `thin` and `tautological` together come to **24 operations**, `thin` alone
to 5, **`tautological` alone to 0**. Not a single case where a description of a normal length turns
out to be a retelling of the title.

That is not a coincidence but the arithmetic of the threshold: "at most 3 novel content words beyond
the title, the `operationId` and the path" is unreachable in a text of 150–200 words — a text that
long inevitably names the permissions, the response codes and the neighbouring operations. So
`tautological-description` is a subset of `thin-description` by construction.

**The rule:** do not put it in the queue as a row of its own. Print it as a field of the
`thin-description` row: `novel words: N`. It sets the order inside the tier: `0` is a plain retelling
of the title ("Get ai agents" under the title "Get ai agents"), `3` means the text already says
something. The merge removes 24 rows without losing information: ApiSystem `PortalController` 49 →
41, `SettingsController` 11 → 8, AI `AgentsController` 21 → 13, `SettingsController` 14 → 10,
`VectorizationController` 3 → 2; the population of the list of controllers does not change.

### 4.2. A tautological parameter or property — the queue splits by owner

The converse of §4.1 does not hold: the quality of the `remarks` does not flow into the description of
a parameter. The same 43 `tautological-param` findings broken down by the length of the owning
operation's description:

| Document | Description of the operation | Parameters |
|---|---|---|
| `apisystem` | < 3 sentences | 28 |
| `people` | 3–5 sentences | 4 |
| `aichat` | 3–5 sentences | 2 |
| `files` | 6+ sentences | 7 |
| `people` | 6+ sentences | 2 |

Fifteen findings sit at descriptions that are fully rewritten. The control example is
`DELETE api/2.0/files/file/{fileId}`: `remarks` of 8 sentences and 180 words (asynchrony, Trash,
`immediately`/`deleteAfter`, permissions, destructiveness, the alternative `PUT fileops/delete`),
while `fileId` says "The file to delete." — and neither the parameter nor the `remarks` says where to
get the identifier and that a numeric value addresses a file of the portal while a string one
addresses a file on a connected third-party account.

**The rule:** print the length of the owner's description in the row of the finding, and split the
queue:

* **a parameter at a `thin` operation** is part of the work on the operation itself and is not raised
  as a row of its own (all 28 ApiSystem findings will close when the operations are rewritten);
* **a parameter at a normal description** is a short queue of its own, "common identifiers", grouped
  **by the source object and not by the operation**: `fileId` is printed into six Files operations,
  `userid` into four People operations, `culture` into two, `toolName` into two. Fifteen findings =
  about five edits.

The same split applies to `tautological-property`: 254 occurrences over 209 unique `schema.property`
pairs — group them by the object, otherwise the queue will send three different passes into one file.

---

## 5. Filters for false positives

1. **Do not count the generator's default response texts.** They are printed by a filter rather than
   by `[SwaggerResponse]`, and no controller edit closes them:
   `400 "Bad request."`, `401 "Unauthorized"`, `429 "Too many requests."`,
   `500 "Internal server error."`, `502 …`, `503 …`.
   Plus a document-local guard: any "code + text" pair that occurs more often than in
   `max(5, operations / 5)` cases is a default too.
2. **`*Wrapper` schemas are out of scope** — they have no source file.
3. **The check against the source.** For every operation, compare the normalised `description` with
   the owner's `<remarks>` (difflib, threshold 0.9). A divergence means the document is older than the
   code, not a finding. On freshly regenerated documents the count of divergences is zero or close to
   it; anything more means step 1 was skipped or a regeneration silently failed.
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
disappears from the next scan; what it closed is recorded in `optimized-log.md`, one line per
controller and check.

**The list is printed as two sections, and what is split between them are findings, not
controllers**: "Must fix" (tiers A and B) and "Recommended" (tier C). A controller with findings of
both kinds stands in both sections, each time with only its own findings and with a pointer to how
many it has left in the neighbouring one. So the row counts of the sections must not be added up;
only the findings add up.

**The threshold sets the size of the list, and it has to be chosen explicitly:**

| Threshold | Controllers (reference figures) |
|---|---|
| A only — there is no text | 1 |
| A + B — there is text, and it says nothing | 10 |
| A + B + C — plus the form of the title | 22 of 75 |

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

Over the four documents of the public bundle: **35 findings in 12 rows = 17 edit sites**. Inside
that — 14 `tautological-param` findings in `files` turn out to be **two** sites
(`fileId` and `folderId`), that is, four passes under the old rule would have edited the same DTO four
times; 14 `long-summary` findings in the same document were twelve separate sites.

## 6.2. The weight of a site and the budget of a pass

The limiting resource of this loop is not build time but careful prose: the twelfth description
written in a row in one pass is worse than the first. So the budget is counted in the weights of
sites, not in rows and not in findings.

| Check | Weight | What it is made of |
|---|---|---|
| `no-description`, `thin-description` | 4 | read the handler end to end and write 150–200 words; two of those fill a pass and a third no longer fits — which is the point |
| `no-param-description`, `tautological-param` | 2 + 1 for every consuming operation beyond the first | cheap to type and expensive to get right: the sentence has to be true for every operation that binds the property |
| `no-summary`, `empty-response-text`, `long-summary`, `duplicate-summary` | 1 | one attribute or one title; the handler is read to confirm a fact, not to be retold |

**The budget of a pass is 8–10 points.** The ceiling is what a pass may carry; the floor is not an admission threshold but a guard against fragmentation: it is there
so that splitting a group does not leave a two-point remainder that a whole pass would be wasted on.

The floor is not always reachable arithmetically: a group of 12 points under a ceiling of 10 divides
only into 7+5 or 8+4, and one of the halves is always below eight. Such a pass is marked as
underloaded rather than padded — the reader has to see that the pass is light by arithmetic and not
because something fell out of it.

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

1. a controller that **came back into the queue on the same check** — there one has to work out which
   edit was lost, and a batch is exactly the place where that question cannot be answered;
2. a finding whose fix needs a decision about behaviour (an unreachable response code, a parameter
   that does not do what its name promises) — that is not a text edit at all;
3. a shared DTO property reaching further than one document: the blast radius leaves the document
   whose counts step 7 checks. This cannot be proved mechanically — two documents sharing a parameter
   name does not yet mean one property — so the script **names such names in a list of their own**, and
   the decision stays with the pass.

The "came back into the queue" mark compares the pair **controller + check**, not the name alone. A
pass closes the findings of one section, so a controller logged for `empty-response-text` and carrying
`long-summary` today has not come back anywhere — nobody ever closed its tier C findings. Matching on
the name alone flags controllers that were never claimed for the check now open on them, and then
scatters them into passes of their own for no reason.

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

## 7. Reference figures

They serve as a regression test: on unchanged sources and unchanged documents a run is obliged to
reproduce them. A number that moves on its own means the index broke, not that the API changed.

| Assembly | Controllers | With findings | Comment |
|---|---|---|---|
| `ASC.ApiSystem` | 2 | 2 | not optimized at all; the only tier A findings (3 parameters with no description) |
| `ASC.AI` | 3 | 3 | not optimized at all: `<remarks>` of one sentence |
| `ASC.Web.Api` | 26 | 2 | `PortalController` (5 responses saying "OK"), `PaymentController` (a long title) |
| `ASC.Files` | 29 | 14 | tautological path parameters, "Status"/"Ok", long titles |
| `ASC.People` | 13 | 1 | `UserController`, 6 tautological parameters |
| `ASC.Data.Backup` | 1 | 0 | clean |

False positives left after the filters of §5 — none, beyond operations whose document is older than
its source.

---

## 8. The skeleton of a run

```python
# 1. index of action methods: file, class, method, <summary>, <remarks>, whether it is hidden
#    (walk the *.cs of the projects of the table in §1, dropping <Compile Remove> and IgnoreApi)
# 2. for every document: operations = paths[*][get|post|put|delete|patch]
# 3. the link: norm(summary of the operation) -> action method  (check that unmatched == 0)
# 4. the checks of §4 over every operation
# 5. the filters of §5, including difflib(description, remarks) >= 0.9
# 6. group the findings by controller; for Files — expand into *Internal/*Thirdparty
#    by the route with its constraints stripped
```

Related files: `.claude/rules/openapi-endpoint-docs.md` (how to write the text), `SKILL.md` (the loop
that consumes this list), `references/writing-traps.md` (what this codebase does to the text you
edit).
