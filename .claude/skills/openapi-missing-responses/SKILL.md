---
name: openapi-missing-responses
description: "Documenting the status codes DocSpace API endpoints really return but do not declare: a queue of actions whose own code (throw, Demand*, controller helpers, DTO validation, 304) produces a code with no [SwaggerResponse]; a run settles a batch of N, adds attributes or records rejections, verifies, reports and stops. USE FOR: find undocumented / missing responses or error codes, which codes does this endpoint really return, add SwaggerResponse for the codes an action throws, continue the missing-responses campaign, take the next N endpoints, how many endpoints are left, check the last batch, show or undo rejected findings — use it whenever the user means the set of status codes an endpoint declares versus what its code returns, even without naming the campaign. DO NOT USE FOR: pipeline-wide responses that belong to a document filter (global MVC filters, the rate limiter), rewriting summaries/remarks (openapi-desc-opt), Spectral findings (openapi-lint-fix), changing server behaviour."
---

# Undocumented responses: one batch of endpoints per run

This skill adds `[SwaggerResponse(code, "text")]` for the status codes an action's code already returns and
does not declare. It documents what the code does — it never changes what the code does.

A run takes **a batch of up to N actions** from the queue (N from the user's request, 5 if none was given),
settles every gap code of each, verifies, reports, and stops. The user reviews each batch before the next one.

## How the queue works

`scan.py` (next to this file) reads the C# sources on every run and, for every documented action, compares the
codes its code produces with the codes it declares. The difference is the **gap**; an action with a gap is in the
queue. Nothing records that an action or a file was "processed": an action leaves the queue because its
attributes now cover its code, and a new or changed action enters it the moment it appears in the tree.

The first lines of the output describe the pipeline of each host, read from its startup class chain on this
run: the exception → status table of its exception handler and its global MVC filters. Take those lines as the
facts of this run; when this file and the output disagree, the output is right.

The one thing it remembers is a **rejection**: "this gap code was analysed and is not a real response"
(unreachable branch, pipeline-owned, swallowed exception). `--reject` stores it in `rejected.json` with a
fingerprint of the code it was decided on — the action body and the evidence lines. When that code changes, the
rejection expires by itself and the code is back in the queue, flagged `! … was rejected …, but the code behind
it changed since`. Only `scan.py` writes that file.

What the scanner sees (`+` counted, `?` shown as hints only):

- `throw new X(...)` in the action, mapped to a status through the exception handler of the action's host;
  subclasses resolve through the repository's class hierarchy;
- helpers of the controller and its base classes (any name, followed recursively), and `Demand*` methods
  anywhere in the repository, followed to what they throw;
- a `throw` inside a `try` whose `catch` takes it without rethrowing is not counted;
- `TryGetFromCache` → 304, `NotFound()`/`StatusCode(…)` results, `Response.StatusCode = …`;
- DTO validation → 400: validation attributes on bound properties and parameters, `required` members of the JSON
  body, `IValidatableObject` — counted only under `[ApiController]` or an attribute deriving from it.

What it does not see — your part in step 3: services called on other objects (`someService.X()`), exceptions
thrown by framework or library code, a `catch` in a caller that converts one exception into another, and whether
a branch can be reached at all.

## 0. Before the batch

`git status`. The scanner marks an action whose file has uncommitted changes `[file has uncommitted changes]`
(`[dirty]` in `--all`). These are the user's edits in progress: take such actions like any other and mark each
of them as having uncommitted changes wherever the batch is listed.

Code navigation in steps 2 and 3 goes through LSP (`goToDefinition`, `outgoingCalls`, `findReferences`) as
`.claude/rules/csharp-lsp.md` requires. If the LSP tool is not available in the session, say so once in the
report and read the callee files instead of grepping for symbols blindly.

## 1. Take the batch

```bash
python .claude/skills/openapi-missing-responses/scan.py --batch <N>
```

One run indexes the whole tree; do not repeat calls whose output you already have. The output: per host, the
exception table and the global filters; the scope and queue counts; then the batch — per action its id, verb and
route, document, declared codes, gap, and the evidence per gap code with the call path (`via`).

If the user named an endpoint or a file, use `--endpoint <id | file | file::Method>` instead — it also prints
hints (`?`) and the evidence behind already declared codes (`=`).

If the scanner stops on an exception handler ("read N of M cases", "registers several exception handlers",
"resolves to N classes"), the pipeline changed shape: read the handler and its registration by hand and tell
the user before relying on any code the scanner reports.

Start the reply with the batch: the ids you are taking, each marked if its file has uncommitted changes.

## 2. Settle every gap code

For each `+` line open the code at the reported location and follow the `via` path. Three questions:

1. **Can a real caller reach it?** Follow the conditions. The usual false positives are in
   `references/conventions.md` (below: conventions) §4. Reachable is the only test — a code that exists because
   of a bug or a library quirk is documented as it is.
2. **Is it the method's or the pipeline's?** Conventions §1. Pipeline responses are left to a document filter,
   even when the scanner reports them.
3. **What exactly triggers it?** That sentence becomes the attribute text: which input, which state, which right.

A gap code that fails 1 or 2 is rejected, with a reason someone can check without redoing the analysis — the
condition that makes the branch unreachable and where it is decided:

```bash
python .claude/skills/openapi-missing-responses/scan.py --reject "<id>" <code> --reason "<why no caller reaches it, with the deciding check>"
```

If an action needs a decision only the user can make, stop on it and ask rather than rejecting or guessing.

## 3. Walk one level deeper

For each action you are going to edit, go one level into every service it calls (`goToDefinition` on each call
in the action body) and collect what those methods throw or demand, with the conditions. Go further only through
thin wrappers that just forward the call. Map each exception through the table the scanner printed for this
action's host. Add the codes found there as well — the action is being documented now, and coming back for it
later costs a second review. Also extend the text of an already declared code when the callee adds another reason
for it.

Binding of unusual DTO shapes (form data, `[FromQuery]` complex types, enums) is decided by ASP.NET, not by
DocSpace: check it against conventions §2, and on a Kestrel stand (conventions §5) when §2 does not cover the
exact case.

## 4. Edit

Only `[SwaggerResponse]` attributes on the actions of the batch, following conventions §3. Match the
nearest analogous action already documented (the same kind of operation in a neighbouring controller) — its
attributes are the current house phrasing. Remarks, summaries and DTO descriptions stay as they are, even where
they contradict the code. Use the Edit tool — conventions §6.

Before the first edit of a file, `scan.py --snapshot <files>`: the pre-edit copy, uncommitted changes included,
is what step 5.2 compares with.

A server bug noticed on the way (wrong exception type, missing null check) is not fixed: changing what the
server answers breaks the frontend, SDKs and external clients, and is not this skill's call.

## 5. Verify

1. `scan.py --endpoint <ids>` — the gap of every action in the batch is empty, or every remaining code is rejected.
2. `scan.py --diff-check <files>` — `VERDICT: clean`: since the snapshot only `[SwaggerResponse]` lines changed,
   the BOM and the line endings are as they were. The header line must say `against snapshot`; `against HEAD`
   means no snapshot was taken, and the user's uncommitted lines then show up as problems.
3. Build the project with document generation, from the nearest `.csproj` above the controller:

   ```bash
   dotnet build <project>.csproj -p:GenerateApiDocs=true -nologo -v n > <scratchpad>/build.log 2>&1; echo "exit $?"
   grep -E "Writing document named|: (error|warning) " <scratchpad>/build.log | sort -u
   ```

   `exit 0`, no error lines, no new warnings for the edited files, and the line
   `Writing document named '2.0' to …` — the success line alone does not prove the document was written. That
   line is logged at normal verbosity: with `-v q` or `-v m` it is not printed even when the document is written.
   The build summary may be localized; the grep patterns are not.
4. `scan.py --verify-doc <ids>` — `VERDICT: documented`: every declared code of the batch is in the regenerated
   document with exactly the attribute's text. `document older than the source` means the build did not write
   it; `MISSING` with a fresh document means the operation was matched wrongly or the attribute is not compiled.
5. Behaviour. If the local portal answers (conventions §5), call each edited endpoint so that every added code
   actually comes back. Without a portal, check pipeline-level claims (binding, validation) on a Kestrel stand, and derive
   DocSpace logic (rights, settings, data) from the code. The report says, per added code, which of the three
   it rests on.

## 6. Report and stop

Reply in the user's language. Only the batch and its verification:

- the batch: ids taken, those whose file has uncommitted changes marked;
- per edited action: each code added or extended, with the evidence (`file:line`) and how it was verified
  (live call / stand / code);
- per rejected code: the reason — these are the decisions the user reviews, and `--unreject` undoes them;
- existing attributes the code cannot produce (left in place);
- verification: gap, diff-check, build, document, behaviour;
- limitations: what could not be checked.

Leave out anything unrelated to the statuses of this batch: server bugs, inaccurate remarks, style elsewhere.
The defects of the edit stay apart from findings for later. The regenerated `json/*.json` documents are part of
the diff; say so. Do not commit — git is the user's. Stop and wait for the next command.

## Other requests

- Check the last batch — redo steps 2 and 5 on it, report problems found in the edit and the limits of the
  check, and stop. Checking is not a licence to take the next batch.
- Progress, how much is left — `scan.py --batch 0` (counts only) or `scan.py --all` (the whole queue).
- Rejections — `scan.py --rejected` lists them with their state (`valid`, `expired`, `orphaned`);
  `scan.py --unreject "<id>" [code]` drops one, and the code is back in the queue on the next run.
  `orphaned` means the action or its gap code is not in the current tree — on another branch it may still be;
  `scan.py --prune-orphaned` drops them, only when the user asks.
