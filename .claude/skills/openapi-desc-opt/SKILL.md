---
name: openapi-desc-opt
description: "Closing the next batch of the endpoint-description queue, end to end: refresh the queue of controllers whose published `summary`/`description`/parameter/response texts still fail `.claude/rules/openapi-endpoint-docs.md`, take the batch it plans (one document, one check, 8-10 points of edit sites), rewrite the C# sources behind it, regenerate that service's OpenAPI document and prove the declared findings have left the queue. USE FOR: which controllers still have unoptimized descriptions, build or refresh the list of unoptimized controllers, optimize the next controller or the next batch, take the first entry of the list, continue the description campaign, are there any endpoints left with thin descriptions — use it even when the user names neither the list nor the campaign, as long as they mean the quality of the published endpoint texts. DO NOT USE FOR: Spectral linter findings on the documents (openapi-lint-fix), a DTO-property campaign of its own, hand-editing `json/*_2.0.json` or `sdk/docspace-api-spec/docspace-backend.yaml`, or regenerating the SDKs (generate-sdk)."
---

# Closing the next batch of the description queue

One pass, one batch. A batch is a set of **edit sites** — the places something is actually typed —
drawn from one document and one check, worth 8 to 10 points on the weights in
`references/list-rules.md` §6.2. It is not a set of controllers, and that difference is the point:
fourteen `tautological-param` findings in `files` are two DTO properties, so fourteen rows of the
queue are two edits, while fourteen `long-summary` findings in the same document are twelve.

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
| `.claude/skills/openapi-desc-opt/scan.py` | builds the queue: indexes the C# actions, reads the documents, runs the checks, writes the list |
| `.claude/skills/openapi-desc-opt/unoptimized-controllers.md` | the list: the next batch, "Must fix" (tiers A and B) and "Recommended" (tier C), rewritten by every scan, deleted when both are empty |
| `.claude/skills/openapi-desc-opt/batch-manifest.json` | what this pass declared it would close, written by `--batch` in step 3 and verified by `--verify-batch` in step 7 |
| `.claude/skills/openapi-desc-opt/optimized-log.md` | one line per closed batch, appended by hand in step 8 |
| `.claude/skills/openapi-desc-opt/references/list-rules.md` | how the queue is defined: units, checks, tiers, filters, control figures |
| `.claude/skills/openapi-desc-opt/references/writing-traps.md` | what this codebase does to text you edit — read before the first edit |
| `.claude/rules/openapi-endpoint-docs.md` | the quality bar the rewrite has to meet |

The queue, the manifest and the log are untracked working files. Do not commit them and do not put
findings in the repository's own documentation.

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
files      controllers=29  operations=215  actions=318  unmatched=0  stale=1  must_fix=9  advisory=5
```

- `controllers` and `actions` come from the C# sources, `operations` from the document. The reference
  figures are in `references/list-rules.md` §7 — 26/29/13/1 controllers and 254/318/91/14 actions. A number that moved by itself means the index broke, not that the API
  changed, until you have found the commit that changed it.
- `unmatched` must be 0. Operations are tied to actions by their normalised title; anything unmatched
  is an operation whose findings nobody can attribute, and they are listed at the end of the file.
- `stale` counts operations whose published description no longer matches the `<remarks>` behind it.
  On freshly regenerated documents it is zero or close to it; a jump means step 1 was skipped or a
  regeneration silently failed.

Under the header the list now carries **"Batch of the next pass"**: the edit sites this pass should
take, their weights, and — below it — the rest of the planned passes, so the size of what is left is
readable as passes rather than as rows. Twelve rows of the advisory section can plan as five passes,
not twelve — which is the number worth reporting.

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

**If "Must fix" is empty but "Recommended" is not**, the pass ends here, and it ends with this
answer and nothing else:

> The mandatory findings are closed. What is left are recommendations: 20 parameters whose
> description all but repeats the name (fileId — 12, userid — 4) and 15 titles longer than six
> words (SettingsController — 7, FilesControllerCommon — 2). None of this stops anyone from using
> the API — I will take it on only if you say so.

`scan.py` prints that sentence itself when the mandatory section comes out empty, so it is copied,
not composed: plain prose rather than rule codes, because the person reading it is deciding whether
to spend an evening on it and `tautological-param — 20 findings` gives them nothing to decide with.
The examples inside it are grouped the way the fix is grouped — parameters by the parameter, since
one DTO property closes every operation that binds it, titles by controller, since there the edit is
per action. Say it, and stop.

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
sites together and sends three kinds of work into a pass of their own — a controller that came back
for the same check, a site whose blast radius leaves its document, and anything needing a behaviour
decision (`references/list-rules.md` §6.3).

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

Read `.claude/rules/openapi-endpoint-docs.md` — the whole file, not from memory — and
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
It is set equality, not an impression of the diff, and it prints one of four verdicts. **It proves
nothing unless step 6 really rewrote the document** — everything below rests on that.

1. **`the batch closed in full, the diff matched the declaration`** — what was declared closed, nothing else
   moved, the counts held. This is the outcome to report.
2. **`STILL OPEN`** — a declared finding survived. The line names it, so read what the finding now says
   before editing again: a `thin-description` that survived a rewrite usually means the text went into
   the wrong tag (a second `<remarks>`, or `<summary>`), and a parameter finding that survived usually
   means the text went on the action instead of the DTO property that feeds it. A partly closed batch
   is a reportable result, not a failure — the remainder is simply the next batch, and the manifest
   names it precisely.
3. **`CLOSED BEYOND THE DECLARATION`** — something closed that the pass never claimed. Usually a shared
   DTO property reaching further than the planner counted, which is good news and still has to be
   named in the report rather than discovered by the next pass.
4. **`COUNTER MOVED`** — `controllers`, `operations`, `actions` or `unmatched` moved, or `stale`
   grew. Stop and work out why before reporting anything: a controller that vanished because the
   parser lost it takes its findings with it and reads exactly like a fix. This is the failure mode
   the whole step exists for.

A diff against the previous list stays useful for reading what changed in prose, but it is no longer
the proof:

```bash
diff "<scratchpad>/unoptimized-controllers.prev.md" .claude/skills/openapi-desc-opt/unoptimized-controllers.md
```

Controllers of the batch keeping rows in "Recommended" is the expected outcome, not a half-closed
pass: a pass closes the findings of one check, and the other checks were never in its scope. Say so
plainly — "the N declared findings are closed, the controller still has M tier C findings under
'Recommended'" — so that nobody reads a surviving row as a failed fix.

## 8. Log the pass and report

Append one line **per controller the batch closed** to
`.claude/skills/openapi-desc-opt/optimized-log.md` (create it if this is the first pass) in exactly
this shape, because the next scan parses it — the date, the controller, and **the check names**,
which have to appear between the dash and the semicolon:

```markdown
- 2026-01-15 ExampleController — 5 operations, empty-response-text; batch api·empty-response-text (5 sites), edit in ExampleController.cs, document api_2.0.json regenerated
```

The check names are load-bearing, not decoration. The flag "came back into the queue" fires on the pair
controller + check, so a line that names no check claims every check for that controller and will
flag it on findings nobody ever closed — and then send them into passes of their own.

A controller that genuinely returns — open again on a check the log says was closed — is flagged in
the list and goes into a pass of its own. That is not an error to hide: either a merge lost the edit
or the pass closed the wrong thing, and only a person can tell which.

Then report, in the user's language and in this order: which batch was closed — the document, the check, the edit
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
  one file holds more than ten points, and a file is not split between passes. Carry it, or ask
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
  `scan.py` or its filters rather than the controller, and re-run the control figures of
  `references/list-rules.md` §7 afterwards to show the fix did not move anything else.
