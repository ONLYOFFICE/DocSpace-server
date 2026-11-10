---
name: openapi-desc-opt
description: "Closing the next batch of the endpoint-description queue end to end: refresh the queue of controllers whose published summary, description, parameter and response texts fail `.claude/rules/openapi-endpoint-docs.md` or `.claude/rules/openapi-dto-docs.md` (missing, thin, or sentences too long and heavy to read, judged by Vale and vacuum where installed), take the batch it plans, rewrite the C# sources by `references/description-rules.md`, check the new texts, regenerate that service's OpenAPI document and prove the findings left the queue. USE FOR: which controllers still have unoptimized descriptions, refresh the list, optimize the next controller or batch, continue the description campaign, endpoints left with thin descriptions — even when the user names neither the list nor the campaign. DO NOT USE FOR: Spectral findings (openapi-lint-fix), a DTO-property campaign of its own, hand-editing `json/*_2.0.json` or `docspace-backend.yaml`, regenerating the SDKs (generate-sdk)."
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
| `.claude/skills/openapi-desc-opt/scan.py` | builds the queue: indexes the C# actions, reads the documents, ties operations to actions, runs the checks, writes the list and plans the batches |
| `.claude/skills/openapi-desc-opt/tools.py` | finds Vale and vacuum, runs them with the settings in `readability/`, and computes their thresholds itself where they are missing; shared by both scripts |
| `.claude/skills/openapi-desc-opt/check_texts.py` | checks the rewritten descriptions of the batch (step 4 and step 6): its own form, order, plain-word and status-code checks, then textstat, Vale and vacuum when found |
| `.claude/skills/openapi-desc-opt/readability/` | the Vale styles and the vacuum ruleset that `check_texts.py` runs |
| `.claude/skills/openapi-desc-opt/unoptimized-controllers.md` | the list: the next batch, "Must fix" (tiers A and B) and "Recommended" (tier C), rewritten by every scan, deleted when both are empty |
| `.claude/skills/openapi-desc-opt/batch-manifest.json` | what this pass declared it would close, written by `--batch` in step 3 and verified by `--verify-batch` in step 7; absent until a batch is committed, and removed by `--batch` when there is nothing left to declare |
| `.claude/skills/openapi-desc-opt/references/list-rules.md` | how the queue is defined: units, checks, tiers, filters, the axis of a batch |
| `.claude/skills/openapi-desc-opt/references/writing-traps.md` | what this codebase does to text you edit — read before the first edit |
| `.claude/skills/openapi-desc-opt/references/description-rules.md` | how an operation description and a `[RaisesWebhook]` `When` text are written: what stays, in which order, the form, the phrases, the check |
| `.claude/rules/openapi-endpoint-docs.md` | the quality bar for the operation: title, response codes, webhooks; its §2 is the short form of `description-rules.md`, which wins in detail |
| `.claude/rules/openapi-dto-docs.md` | the quality bar for a parameter, a DTO property, an enum member |

The queue and the manifest are working files of one pass: `.gitignore` keeps them out, and nothing
about them means anything on another machine. What a pass closed is recorded where the rest of this
repository's history is — in the commit that closed it. Do not put findings in the repository's own
documentation.

Scope is the public bundle: the documents that `join` in
`common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/appsettings.json` merges into the contract.
`scan.py` reads the bundle and the project behind each document from the repository (list rules
§5.5), and `--diag` prints their keys. A document outside the bundle is scanned only when `--scope`
names it, for the day that decision changes. Do not widen the scope on your own initiative: the
extra rows are texts nobody has agreed to own.

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

- `controllers` and `actions` come from the C# sources, `operations` from the document. There is no
  expected value to compare them with: the surface changes whenever someone adds an action. What
  catches an index that lost a controller is the next line — its operations stop matching.
- `unmatched` must be 0, and the scan prints an `UNMATCHED:` line when it is not. Operations are tied
  to actions by their normalised title; anything unmatched is an operation whose findings nobody can
  attribute, and they are listed at the end of the file. Either the index broke or the surface
  changed; the listed operations and the diff of their controllers say which.
- `stale` counts operations whose published description no longer matches the `<remarks>` behind it.
  On freshly regenerated documents it is zero or close to it; a jump means step 1 was skipped or a
  regeneration silently failed.

The queue finds descriptions both below the norm (`no-description`, `thin-description`) and above it
(`off-form-description`, `heavy-description`, list rules §4). The second kind is the verdict of Vale
and vacuum where they are installed and of the skill's own stand-in where they are not; the line
`FORM: judged by …` says which. The scan finds the tools the way `check_texts.py` does (`--vale`,
`--vacuum`, `VALE_BIN`, `VACUUM_BIN`, `PATH`); `--own-checks` makes it ignore them. Pass the same
choice to `--batch` and `--verify-batch` — a different judge moves form findings by itself, and step 7
reports that as a moved counter. A missing tool is not an error: name it at the end of the report, as
in step 8.

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

Read the rule the batch's check belongs to — the whole file, not from memory — and
`references/writing-traps.md` before the first edit. The rule is the bar; the traps file is what this
codebase does to text that looks fine in the editor.

- **A description** (`no-description`, `thin-description`, `off-form-description`, `heavy-description`)
  and the `When` of a `[RaisesWebhook]`:
  `references/description-rules.md`. Opening the controller with the Read tool also brings
  `.claude/rules/openapi-endpoint-docs.md` into context (a shell `cat` does not). Its §2 is the short form
  of the reference; where the two differ in detail, the reference wins. Everything else of that rule
  holds — §0, the title, the webhook mechanics, the response texts.
- **A title or a response text**: `.claude/rules/openapi-endpoint-docs.md`.
- **A parameter finding**, which is typed on a DTO property: `.claude/rules/openapi-dto-docs.md`.

The checks are a map of where to look, never a specification of what to write. `thin-description`
says the text is under three sentences; it does not say that three sentences will do. A description
has to let an agent pick the operation, fill it in and read the answer with no other context, and
§2 of the reference lists what that takes, in order. The aim is the fewest words that keep every
fact, not the shortest text: a fact missing from a short description is the failure the queue exists
to remove.

Read the handler before describing it. Every sentence is a claim about behaviour, and a confident
sentence that is wrong is worse than the thin one it replaced — an agent has no way to tell. So the
facts come before the prose: write down, for yourself, every fact the text will state next to the
line of code that proves it, and only then write the sentences. Three facts go wrong most often, and
each needs the code followed to its end rather than read at the first branch:

- **Who may call.** Follow the permission check through every branch that grants the right, not only
  the share it names: the creator of a room, the DocSpace admin bypass, a role that comes from
  elsewhere. Then write who is allowed, and which code the others get.
- **What a second call does.** Look at every state the object can be in when the call comes again —
  done, running, queued but not started, partly applied — and write what happens in each. "You can
  repeat it" is a claim about all of them; "Do not repeat it" is an equally good answer when the code
  says so.
- **Which code comes back.** The exception thrown, then the handler that maps it to a status (see
  `references/writing-traps.md`), and which check runs first when two can fail.

The integration tests of the operation (`products/*/Tests/Tests/**`, a folder per area, often a
`*PermissionsTests` class) settle the codes and the roles faster than tracing; where none exist, say
in the report that the codes rest on reading the code. When reading the handler turns up a genuine
bug, that is a normal outcome of this work: report it, do not fix it here. A documentation pass that
changes behaviour is no longer reviewable as a documentation pass.

Stay inside the batch. The temptation to fix the neighbouring controller, or the tier C finding two
lines below the one you came for, is exactly how a pass becomes unverifiable — and with a manifest it
is no longer a matter of taste: step 7 will report the extra closure as undeclared, and the pass has
to explain in its report what reached further than it said it would.

### Check the rewritten descriptions

When the batch rewrote descriptions, check them before the build, while the document still holds the
old text:

```bash
python .claude/skills/openapi-desc-opt/check_texts.py
```

It takes the description sites of the batch from the manifest (`--doc files --op "PUT api/2.0/files/tags"`
names operations by hand), reads the new text from `<remarks>` and the old one from the document, and
prints a block per operation. Section numbers here and in its output are those of
`references/description-rules.md`.

- **its own checks**, which always run: `FORM` (sentence length and count, the average), `ORDER` (the
  verb of the first sentence, the access phrase and its refusal code, the effect, the repeat statement),
  `STYLE` (colons, word order, the expressions of §5.3, read from the rules file), `REFS` (every
  `METHOD path` the text names exists in a document of the `json` folder), `CODES` (§1.5: codes of the
  old text, of the responses the action declares and of the parameters against the new text);
- **textstat** (Flesch-Kincaid grade, reading ease, fog index, difficult words), **Vale** (the
  sentence-length and grade styles of `readability/vale`, plus the Microsoft style when it is synced)
  and **vacuum** (the form rules of `readability/vacuum`) — each one only if it is on this machine.

The tools are optional. The script looks for them before it runs them: `--vale` or `VALE_BIN`,
`--vacuum` or `VACUUM_BIN`, else `PATH`; textstat is imported by the running Python, else by
`--textstat-python` or `TEXTSTAT_PYTHON`. A tool that is not found is skipped without an error, and the
last line, `MISSING TOOLS:`, names it — keep that line for the report. Do not install anything.

Read the output as a list of places to look at, not as a score. A note about content — a missing
refusal code, a missing repeat statement, a code not in the text — is a question to put to the code,
never a sentence to add: the answer may be that the fact does not apply, or that it is the opposite of
what the note's wording suggests.

- **Fix**: a sentence over 20 words (the own counter counts like `Docs.SentenceLength` and adds code
  as one word, so it is the stricter of the two), fewer than 3 sentences, a lost code without a
  reason, an operation `REFS` cannot find, a colon that is not before a list, `or` in the sense of
  otherwise, a refusal code or a repeat statement that §2 asks for and the code supports.
- **Judge**: Microsoft suggestions, passive voice, difficult words, a Flesch-Kincaid grade above 10.
  They point at a sentence worth a second look. Change it when a plainer wording exists
  (§5.3, §5.4); a domain term (`portal`, `quota`, `webhook`) and a state in the passive (`is turned
  off`, `read-only`) stay.
- **Noise**: `is read` inside "read-only", `disabled` as an account status, `My` in the name of the
  "My documents" section, `Microsoft.OxfordComma` on two verbs joined by `and` (it is no list), an
  `ORDER` note where the text states the fact in words the script does not know — the patterns are a
  heuristic, the order of §2 is the rule.

Clean output on every parameter is not the goal: a text can keep a grade of 11 when every word is
needed. Fix, run the script again, and stop when what is left is judged or noise. Then make the
shortening pass of `description-rules.md` §6, step 3, and compare the word counts the script prints.

A batch of titles, response texts or parameters rewrote no description; the script says so and there
is nothing to check.

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

When the batch rewrote descriptions, check what was published:

```bash
python .claude/skills/openapi-desc-opt/check_texts.py --from document
```

This is the text a reader gets: the generator has joined the lines and appended the sentence of each
`[RaisesWebhook]`, whose `When` follows the same form. A `FORM` line that was not there in step 4
usually points at that sentence.

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
the proof from step 7 as `--verify-batch` printed it, then, for a batch of descriptions, the check of
the texts: per operation the words before → after and the sentences, and every tool finding that was
left with the reason (judged, noise). Then where the campaign now stands, which is one of three
endings and has to be said in as many words:

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

The last line of the report names the tools `check_texts.py` did not find, from its `MISSING TOOLS:`
line: "textstat, Vale and vacuum are not installed on this machine; their checks were skipped." Say
it once, without installation advice, and leave it out when every tool ran or when the batch had no
description to check.

## When the pass stops short

This loop stops rather than improvises. Stopping with a clear account is a result; a pass that
half-closed a batch and cannot say which half is not.

- **A batch that only partly closed is not a stop.** `--verify-batch` names exactly which declared
  findings survived; report that, and the remainder becomes the next batch with no re-analysis. What
  would be a stop is reporting the batch closed when it was not.
- **The documents cannot be regenerated.** Work on the snapshot if you must, but do not report a
  finding as closed on the strength of an unregenerated document.
- **The batch is heavier than the ceiling.** The planner says so on the batch itself: it happens only
  when one site alone outweighs a pass, since a heavier file is cut into parts. Carry it, or ask
  whether to take only part of the site — and if you do take part, say which part in the report,
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
  `UNMATCHED:` line and that `controllers`, `actions` and `operations` are where they were before the
  fix, to show it did not move anything else.
- **A `check_texts.py` finding that the rules allow** — a colon before a list, a domain word Vale
  calls difficult. Leave the text as it is and name the finding in the report as judged. The script is
  a heuristic over the rules, not the rules.
