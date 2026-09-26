---
name: openapi-desc-opt
description: "Closing the next batch of the endpoint-description queue, end to end: refresh the queue of controllers whose published `summary`/`description`/parameter/response texts still fail the endpoint-documentation rules (`.claude/rules/openapi-endpoint-docs.md` for the operation, `.claude/rules/openapi-dto-docs.md` for properties), take the batch it plans (one document, one check, a budgeted set of edit sites), rewrite the C# sources behind it, regenerate that service's OpenAPI document and prove the declared findings have left the queue. USE FOR: which controllers still have unoptimized descriptions, build or refresh the list of unoptimized controllers, optimize the next controller or the next batch, take the first entry of the list, continue the description campaign, are there any endpoints left with thin descriptions — use it even when the user names neither the list nor the campaign, as long as they mean the quality of the published endpoint texts. DO NOT USE FOR: Spectral linter findings on the documents (openapi-lint-fix), a DTO-property campaign of its own, hand-editing `json/*_2.0.json` or `sdk/docspace-api-spec/docspace-backend.yaml`, or regenerating the SDKs (generate-sdk)."
---

# Closing the next batch of the description queue

One pass, one batch. A batch is a set of **edit sites** — the places something is actually typed —
drawn from one document and one check, budgeted on the weights in `references/list-rules.md` §6.2.
It is not a set of controllers, and that difference is the point: a dozen `tautological-param`
findings can be two DTO properties, so a dozen rows of the queue are two edits, while a dozen
`long-summary` findings in the same document are a dozen separate titles.

The queue is an inventory of documentation debt: a finding leaves it by being rewritten, never by
being skipped, and never because a scan happened to stop seeing it. That distinction is the whole
difficulty of this loop — an absence that was never a fix looks exactly like a fix, and it writes a
false "done" into the campaign that every later pass then trusts. A batch makes that risk sharper,
which is why it declares what it will close before it edits anything (step 3) and checks the
declaration by set equality afterwards (step 7), instead of reasoning about the diff the way a
one-row pass could.

So the pass is built around two moments that are easy to do badly. Step 1, because a document older
than its controller turns finished work into findings, and the pass then rewrites text that is
already right. Step 7, because the row has to be absent from a list rebuilt from a document that was
*genuinely* regenerated after the edit — and the counts around it have to be unchanged, or the
absence is a parsing accident.

## What the loop owns

| File | What it is |
|---|---|
| `.claude/skills/openapi-desc-opt/scan.py` | builds the queue: indexes the C# actions, reads the documents, checks the surface against `EXPECTED_SURFACE`, runs the checks, writes the list and plans the batches |
| `.claude/skills/openapi-desc-opt/unoptimized-controllers.md` | the list: the next batch, "Must fix" (tiers A and B) and "Recommended" (tier C), rewritten by every scan, deleted when both are empty |
| `.claude/skills/openapi-desc-opt/batch-manifest.json` | what this pass declared it would close, written by `--batch` in step 3 and verified by `--verify-batch` in step 7; absent until a batch is committed, and removed by `--batch` when there is nothing left to declare |
| `.claude/skills/openapi-desc-opt/references/list-rules.md` | how the queue is defined: units, checks, tiers, filters, the axis of a batch |
| `.claude/skills/openapi-desc-opt/references/writing-traps.md` | what this codebase does to text you edit — read before the first edit |
| `.claude/rules/openapi-endpoint-docs.md` | the quality bar for the operation: title, description, response codes |
| `.claude/rules/openapi-dto-docs.md` | the quality bar for a parameter, a DTO property, an enum member |

The queue and the manifest are working files of one pass: `.gitignore` keeps them out, and nothing
about them means anything on another machine. What a pass closed is recorded where the rest of this
repository's history is — in the commit that closed it. Do not put findings in the repository's own
documentation.

Scope is the four documents of the public bundle — `api`, `files`, `people`, `backup`. `ai` and
`apisystem` are outside it (list rules §5.5); they stay one flag away,
`--scope api,files,people,backup,ai`, for the day that decision changes.
Do not widen the scope on your own initiative: it triples the queue and the extra rows are texts
nobody has agreed to own.

## 1. Are the documents younger than their sources?

```bash
python .claude/skills/openapi-desc-opt/scan.py --freshness
```

It compares, per document, the last commit that touched the service project against the last commit
that touched the document, and looks for uncommitted `.cs` changes that no commit date can see. Both
signals matter: the first catches a colleague's merge, the second catches your own unfinished work.

`VERDICT: safe to analyse as it stands` — go to step 2.

Otherwise rebuild the documents that came back stale, one project each, straight into `json/` (the
csproj sets `OpenApiDocumentsDirectory`):

```bash
dotnet build web/ASC.Web.Api/ASC.Web.Api.csproj                      -p:GenerateApiDocs=true  # api_2.0.json
dotnet build products/ASC.Files/Server/ASC.Files.csproj              -p:GenerateApiDocs=true  # files_2.0.json
dotnet build products/ASC.People/Server/ASC.People.csproj            -p:GenerateApiDocs=true  # people_2.0.json
dotnet build common/services/ASC.Data.Backup/ASC.Data.Backup.csproj  -p:GenerateApiDocs=true  # backup_2.0.json
```

The proof a document was actually rewritten is the line `Writing document named '2.0' to
…json\<name>_2.0.json`. The build's success line is not proof — it appears just as happily when
nothing was regenerated, and it is localised, so grepping for "Build succeeded" can come back empty
on a perfectly successful build. The `Writing document` line stays English.

Do not build `ASC.Api.Documentation` to get documents: it drags in the whole service graph and then
runs the Markdown and SDK targets, which need openapi-generator-cli, Maven, a JDK and Node. Nothing
here needs any of that.

If a document cannot be regenerated at all — no SDK, a broken build that is not yours to fix — you
can still work, because `scan.py` re-checks every operation against its `<remarks>` and moves the
drifted ones out of the queue into a section of their own. Say in the report that the pass ran on a
snapshot, and treat the drifted operations as unknown rather than clean.

## 2. Refresh the queue

```bash
python .claude/skills/openapi-desc-opt/scan.py
```

It takes about a second, so there is no reason to work from yesterday's file: the scan both creates
the list when it is missing and rewrites it when it is not. Copy the old list into the scratchpad
first if one exists — step 7 diffs against it, and this is the only moment the previous state is
still on disk.

The summary line per document is worth reading before the list itself:

```
files      controllers=..  operations=..  actions=..  unmatched=0  stale=0  must_fix=..  advisory=..
```

- `controllers` and `actions` come from the C# sources, `operations` from the document. The scan
  checks them against `EXPECTED_SURFACE` in `scan.py` itself and prints a `SURFACE:` line when they
  disagree. Such a line means the index broke, not that the API changed — stop and find the commit
  that moved it, because a controller the index lost takes its findings with it and reads exactly
  like a closed batch.
- `unmatched` must be 0. Operations are tied to actions by their normalised title; anything unmatched
  is an operation whose findings nobody can attribute, and they are listed at the end of the file.
- `stale` counts operations whose published description no longer matches the `<remarks>` behind it.
  On freshly regenerated documents it is zero or close to it; a jump means step 1 was skipped or a
  regeneration silently failed.

Under the header the list carries **"Batch of the next pass"**: the edit sites this pass should
take, their weights, and — below it — the rest of the planned passes, so the size of what is left is
readable as passes rather than as rows. A section of a dozen rows routinely plans as a handful of
passes, because sites group; the number of passes is the one worth reporting.

- `must_fix` and `advisory` count the rows of the two sections of the file. **The split is between
  findings, not between controllers**: tier A and B findings go to **"Must fix"**, tier C ones — a
  tautological parameter at an operation that is already fully written, a title longer than six words,
  a title used twice — to **"Recommended"**. A controller with both kinds appears in both sections,
  each row carrying only that section's findings and a line naming how many it has in the other. So
  the two row counts overlap, and only the finding counts in the file header add up.

  The reason for the split is that these are two different kinds of work: the first leaves an agent
  without a usable tool definition, the second improves a text that already works. The reason a
  controller is allowed in both is the same one in reverse — closing its real gaps must not quietly
  carry its cosmetics along, and reading its advisory row must not suggest that cosmetics is all it
  needs.

**If both sections are empty** the script deletes the list and says so. Report that no findings
remain in the scope, and stop — there is nothing to fix and nothing to verify.

**If "Must fix" is empty but "Recommended" is not**, the pass ends here, and it ends with the
sentence `scan.py` printed under the header — copied, not composed. It says that the mandatory
findings are closed, then what is left in plain prose: how many parameters all but repeat their
name and how many titles run long, with the worst offenders named. Rule codes are deliberately
absent, because the person reading it is deciding whether to spend an evening on this, and
`tautological-param — N findings` gives them nothing to decide with. The examples in it are
grouped the way the fix is grouped — parameters by the parameter, since one DTO property closes
every operation that binds it, titles by controller, since there the edit is per action. Say it,
and stop.

This is the milestone of the campaign and the one place
where doing more is worse than stopping: the advisory section is by construction text that already
works, so a pass nobody asked for spends a review cycle on cosmetics and, worse, makes the next
reader think the mandatory section was never empty. A recommendation becomes work only when the user
says so — then `--threshold ABC` turns tier C into the queue and the pass runs exactly as usual.

## 3. Commit to the batch

```bash
python .claude/skills/openapi-desc-opt/scan.py --batch
```

This writes the list again and, beside it, `batch-manifest.json`: the exact set of findings this pass
declares it will close, the full set of findings open right now, and the documents' counts. Write it
**before the first edit**. It is what turns step 7 from a judgement into a comparison, and a manifest
written afterwards proves nothing — it would be a description of the diff, not a prediction of it.

The batch is the one the planner put first. It holds edit sites of one document and one check,
because the build and the regeneration are per project and because a diff that mixes rewritten titles
with rewritten DTO properties cannot be reviewed as one thing. The planner already keeps a file's
sites together and sends two kinds of work into a pass of their own — a site whose blast radius
leaves its document, and anything needing a behaviour decision (`references/list-rules.md` §6.3).

**A pass works "Must fix" and nothing else.** A tier C site is never picked up because the
mandatory section ran out, because it stands in the same file, or because the controller you are
already editing also appears there. The only way a recommendation becomes this pass's target is the
user asking for it, and then `--threshold ABC` makes it the queue and the planner batches it exactly
as usual.

Then open the files the batch names. Two things about where the text actually lives:

- For `*Internal` / `*Thirdparty` pairs the action is declared **once**, in the generic base in the
  same file. The queue shows both rows because the surface report does, but there is one edit and it
  closes both — the batch already counts it as a single site, and the manifest already declares both
  findings. Expect the queue to drop by two.
- A parameter finding points at a DTO property, not at the action — the text comes from `<summary>`
  on the bound property, which several operations share, and the batch prints every operation that
  property feeds. `references/writing-traps.md` has the full map of which field feeds which part of
  the document.

## 4. Rewrite the text

Read the rule the batch's check belongs to — the whole file, not from memory:
`.claude/rules/openapi-endpoint-docs.md` for a description, a title or a response text,
`.claude/rules/openapi-dto-docs.md` for a parameter finding, which is typed on a DTO property — and
`references/writing-traps.md` before the first edit. The rule is the bar; the traps file is what this
codebase does to text that looks fine in the editor.

The checks are a map of where to look, never a specification of what to write. `thin-description`
says the text is under three sentences; it does not say that three sentences will do. What the rule
asks for is a description that lets an agent pick the operation, fill it in and read the answer with
no other context: what it does, what it needs, what it changes, what it costs, and which neighbouring
operation to use instead when this one is the wrong choice. Six to nine sentences is the normal shape
that comes out of answering those honestly.

Read the handler before describing it. Every sentence is a claim about behaviour, and a confident
sentence that is wrong is worse than the thin one it replaced — an agent has no way to tell. When
reading the handler turns up a genuine bug, that is a normal outcome of this work: report it, do not
fix it here. A documentation pass that changes behaviour is no longer reviewable as a documentation
pass.

Stay inside the batch. The temptation to fix the neighbouring controller, or the tier C finding two
lines below the one you came for, is exactly how a pass becomes unverifiable — and with a manifest it
is no longer a matter of taste: step 7 will report the extra closure as undeclared, and the pass has
to explain in its report what reached further than it said it would.

## 5. Check the code still compiles

```bash
dotnet build <the project of that controller>.csproj
```

XML documentation is compiled: an unclosed tag, a `<param>` naming an argument that is not there, or
a stray `<` in prose all produce warnings, and some of them cost the operation its text without
failing the build. Read the warnings for the file you touched, not just the exit code.

## 6. Regenerate that service's document

The same command as in step 1, for the one project you edited, and the same proof — the
`Writing document named '2.0' to …` line. Without this step the next scan reads the old document and
the row is still there, which looks exactly like a rewrite that did not work.

## 7. Prove the declared findings have left the queue

```bash
python .claude/skills/openapi-desc-opt/scan.py --verify-batch
```

This recomputes the queue from the freshly regenerated document and compares it against the manifest.
It is set equality, not an impression of the diff: it names every finding that moved, in whichever
direction, and closes with a single `VERDICT:` line. **It proves nothing unless step 6 really rewrote
the document** — everything below rests on that.

Read the named lines before the verdict, because each kind asks for something different:

- **a declared finding that is still open** — the edit did not land where the check looks. A
  `thin-description` that survived a rewrite usually means the text went into the wrong tag (a second
  `<remarks>`, or `<summary>`); a parameter finding that survived usually means the text went on the
  action instead of the DTO property that feeds it. A partly closed batch is a reportable result, not
  a failure — the remainder is simply the next batch, and the manifest names it precisely.
- **something closed that was never declared** — usually a shared DTO property reaching further than
  the planner counted. Good news, and still to be named in the report rather than discovered by the
  next pass.
- **something that appeared** — a finding that was not open when the batch was declared. The edit
  itself is the first suspect: a rewritten title that ran past six words, a new response text of two
  words. Fix it inside this pass; it is your own diff.
- **a counter that moved** — the surface or `stale` is not where the manifest left it. Stop and work
  out why before reporting anything: a controller that vanished because the parser lost it takes its
  findings with it and reads exactly like a fix. This is the failure mode the whole step exists
  for.

A diff against the previous list stays useful for reading what changed in prose, but it is no longer
the proof:

```bash
diff "<scratchpad>/unoptimized-controllers.prev.md" .claude/skills/openapi-desc-opt/unoptimized-controllers.md
```

Controllers of the batch keeping rows in "Recommended" is the expected outcome, not a half-closed
pass: a pass closes the findings of one check, and the other checks were never in its scope. Say so
plainly — "the N declared findings are closed, the controller still has M tier C findings under
'Recommended'" — so that nobody reads a surviving row as a failed fix.

## 8. Report

Report, in the user's language and in this order: which batch was closed — the document, the check, the edit
sites and the controllers they cover — what was rewritten (actions, DTO properties, response texts),
the proof from step 7 as `--verify-batch` printed it, and then where the campaign now stands, which is
one of three endings and has to be said in as many words:

- **"Must fix" still has rows** — how many findings and how many *passes* are left, and what the
  next batch is. Passes, not rows: rows are what the reader is trying to stop counting.
- **"Must fix" is empty, "Recommended" is not** — the sentence `scan.py` printed, as it printed
  it: "The mandatory findings are closed. What is left are recommendations: …". Nothing after it,
  and no starting on them in the same breath.
- **Both are empty** — the list was deleted and the scope is clean.

A controller of the batch keeping a row in "Recommended" belongs in the first or second of those,
not in a caveat: findings of the other checks were never part of this pass.

Anything you found and did not fix goes in the same report: a handler that contradicts its own
documentation, a response code the controller cannot actually return, a DTO property whose text is
wrong for one of the operations that share it. These are the findings that never reach the queue,
because no check can see them.

## When the pass stops short

This loop stops rather than improvises. Stopping with a clear account is a result; a pass that
half-closed a batch and cannot say which half is not.

- **A batch that only partly closed is not a stop.** `--verify-batch` names exactly which declared
  findings survived; report that, and the remainder becomes the next batch with no re-analysis. What
  would be a stop is reporting the batch closed when it was not.
- **The documents cannot be regenerated.** Work on the snapshot if you must, but do not report a
  finding as closed on the strength of an unregenerated document.
- **The batch is heavier than the ceiling.** The planner says so on the batch itself: it happens when
  one file holds more points than the budget allows, and a file is not split between passes. Carry it, or ask
  whether to take only part of the file — and if you do take part, say which part in the report,
  because the manifest will show the rest as surviving.
- **The row's only open findings are tier C.** Then it is not in "Must fix" at all, and the pass
  does not start on it uninvited: report that the mandatory section is empty and ask whether to work
  "Recommended" (`--threshold ABC`). Asked to do one anyway, treat it as a normal pass — the tier
  decides the order of the queue, never the care taken over the text.
- **The fix needs a behaviour change** — a response code that is documented but unreachable, a
  parameter that does not do what its name says. Describe what the code does today, report the
  mismatch, and ask before touching the handler.
- **The finding is an artefact.** A response text the generator prints everywhere, an operation whose
  title collides with another's, a controller that the index sees but the document does not. Fix
  `scan.py` or its filters rather than the controller, and check afterwards that the run prints no
  `SURFACE:` line, to show the fix did not move anything else.
