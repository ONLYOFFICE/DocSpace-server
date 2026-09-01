---
name: openapi-lint-fix
description: "Closing the first open Spectral finding on the DocSpace service OpenAPI documents, end to end: read the findings memory, take the top row of lint/report.md, trace it back to the generator that emits it, fix the source, check the code still builds clean, regenerate the documents, re-lint, prove that finding is gone, and record what was learned. USE FOR: fix the first linter finding, close a Spectral finding, investigate a row of lint/report.md, pay off the next piece of OpenAPI documentation debt, why does the linter complain about this endpoint — use it even when the user does not say the word Spectral, as long as they mean the OpenAPI documentation findings. DO NOT USE FOR: C# analyzer or compiler warnings, hand-editing json/*_2.0.json, the published contract sdk/docspace-api-spec/docspace-backend.yaml, or the generator input SDK/json/api-docs.json."
---

# Closing the first OpenAPI linter finding

Nine steps, and the two usually done wrong are near the end: step 6, because "the build succeeded" is
not the same as "the code is still correct", and step 8, because the finding has to be absent from a
report that was *genuinely* regenerated — a rule that quietly stopped being evaluated leaves exactly
the same empty space as one that was fixed.

The loop is worth doing carefully because each pass is meant to be permanent. The report is an
inventory of documentation debt; a finding leaves it by being paid, never by being suppressed or
quietly excluded. That is also why steps 7–8 are not ceremony: an unverified fix that turns out to be
a filtering artefact writes a false zero into the memory, and every later run trusts it.

**One pass, one attempt.** This loop stops rather than recovers. There are three places it can end
without a closed finding — step 4 when the fix would move the wire, step 6 when the clean reviewer
finds something wrong with the edit, step 8 when the regenerated report still shows the finding — and
at all three the move is the same: stop, write down what was done and what was found, hand the result
to the user. Do not repair the edit and try again in the same session.

That is a deliberate trade against the instinct to finish. An edit that failed review or failed
verification is evidence that the analysis in step 3 was wrong somewhere, and the cheapest thing to do
with that evidence is write it down while it is fresh — not spend the rest of the session iterating on
a hypothesis that has already been contradicted once, with the tree drifting further from a state
anyone can read. A pass that ends in a clear “here is what I tried, here is exactly what contradicted
it” is a useful pass: the next one starts from the answer instead of the question. A pass that ends
after three rounds of repair leaves nobody, including you, able to say what the tree now contains or
which of the three attempts each modified file belongs to.

**Working directory.** Every relative path below (`json/`, `lint/`, `SDK/.spectral.yaml`) is relative
to `common/Tools/ASC.Api.Documentation/ASC.Api.Documentation`. `cd` there before touching any of them
and stay there. Paths to sources, to this skill's own files and to `dotnet build` targets are given
from the repository root instead — those are the ones with a `common/`, `products/`, `web/` or
`.claude/` prefix.

The two roots are four levels apart, so a repository-root path invoked from the linting directory
needs `../../../../` in front of it. The snippets below already carry that prefix where it matters;
`dotnet build` is the exception — run it from the repository root, since the paths it takes are
project files rather than lint inputs.

## 1. Read the memory first

This skill keeps what it knows in two files, and the split is worth understanding before you read
either:

- **`references/project-facts.md`** — how this repository's generator behaves, and the traps in the
  tooling around it. Facts about the project, shipped with the skill, true until the project changes.
  Step 3 sends you there.
- **`.claude/skills/openapi-lint-fix/lint-memory.md`** — what previous *runs* measured: the last
  measurement, the live debt per rule, the findings analysed to the bottom and known to need a
  contract change, the closed debt, and the service defects noticed along the way that no rule fires
  on. Its section layout is `references/memory-template.md`.

The line between them is "would this still be true if nobody ever ran the linter again?" Generator
behaviour would; a count would not. Putting a count in the facts file makes it rot, and putting a
generator fact in the memory loses it the next time the memory is reset.

Read it **before** opening `lint/report.md`. The report is sorted in document order, so its first row
is an accident of alphabetics and can well be a row a previous run already analysed and found
blocked. Without the memory, such a run re-derives that conclusion from scratch, spends its budget on
an analysis already written down, and hands back nothing.

**That file is the only memory this loop uses.** Other notes about these same rules may exist in the
repository — a sibling skill's ledger, a comment in the ruleset pointing at one, a document somebody
kept by hand. They are not this skill's memory: they were written for a different purpose, nobody
keeps them in step with what this loop measures, and treating them as authority imports conclusions
you cannot verify. The two things you may trust as sources are this file and the ruleset's own
comments, which are part of the tool you are running. Everything else you derive and then write down
here yourself.

Those two rules collide in one specific place, so decide it in advance: a ruleset comment may itself
point at notes kept outside this loop. Follow such a pointer if you like, but only as far as treating
what you find as an **unverified lead** — something to confirm against the report and the documents
and then record here in your own words, marked with where it came from. Trust the comment's own claim,
because the ruleset is the tool; do not inherit the authority of whatever it links to.

If the file does not exist, this is the first run: say so, carry on, and create it in step 9. Nothing
here depends on it existing — an empty memory only means the report's first row is taken at face
value.

## 2. Take the target — the report's first row, checked against the memory

Read the first data row of `lint/report.md`. That is the target.

**If `lint/report.md` does not exist, stop and ask the user for permission to run the linter.** It is
their machine, the run needs a globally installed CLI and writes into `lint/`, and a linter run is not
what they asked for. Without permission, end the skill and say why. With permission, run it as
described in step 7, then come back here.

Before starting the analysis, check the target against the memory's blocked-findings section. If it is
recorded there, the analysis is already done: **cite that section, say what the fix would cost on the
wire, name the next row that is not blocked, and let the user choose.** Do not silently swap the
target for another one — the user asked for the first finding, and the reason it is not being fixed is
information they need. Do not restart an analysis that is already written down either; re-deriving a
known conclusion is the most common way a pass of this skill produces nothing.

**Then look up the rule's severity in the ruleset, before analysing anything.** If the rule is
already pinned to `error` with a measured-zero comment, you are not looking at debt you have just
discovered — you are looking at a regression of something previously paid for, and that changes where
you go next. Debt sends you to the generator, because the documentation was never written. A
regression sends you to whatever changed recently, which in practice means the working tree, and the
answer is often two minutes away instead of an hour. It also decides what you write at the end: the
regression branch of step 9, not a new debt row. Ask this question early — recognising it after the
generator walk means you paid for the walk twice over.

Two properties of the report change what "the first finding" means:

- **One row is one violation, not one problem.** Almost every rule fires in several documents and
  usually several times per document. Before deciding what "fixed" will mean, get the rule's full
  population:

  ```bash
  python ../../../../.claude/skills/openapi-lint-fix/count.py lint/report.json
  ```

  It prints TOTAL, the per-severity and per-document splits, one line per rule, and every Error
  finding with its JSON path. Fix the whole population of the rule, or state exactly which part you
  left open and why. A half-paid rule cannot be ratcheted and will be re-analysed next run.

- **Line and column numbers are the parser's 0-based values.** A finding reported at 4287 is on line
  4288 of the file.

## 3. Analyse: find the emitter, never the document

`json/*_2.0.json` are **build outputs**. Editing them is always wrong — the next build overwrites the
edit and the linter reports the same finding again, now with the added cost that someone believed it
was fixed. Follow the finding back to whatever generated it:

| Finding is about | The source is |
|---|---|
| `info`, `servers`, security schemes, SDK-installation extension | `common/ASC.Api.Core/Extensions/OpenApiExtension.cs` (`AddOpenApi`) |
| response envelopes, examples, nullable/3.1 rewrites, tags, rate-limit headers | the filters registered in that same `AddOpenApi` — `SwaggerSuccessApiResponseFilter`, `OpenApi31SchemaDocumentFilter`, `TagDescriptionsDocumentFilter`, the `RateLimit*Filter`s |
| a schema or property description | the `<summary>`/`<param>` XML doc on the DTO — plus `GenerateDocumentationFile=True` in the owning csproj, without which the xml is never produced and every type in that assembly comes out undescribed |
| route casing, an operation's tags or summary | the controller: `[ApiEndpoint]`, the route template, `[Tags]`, the XML doc comment |
| a path parameter with no description | whichever DTO property binds the placeholder — and if none does, `[SwaggerPathParameter]` on the action. **Not** an XML `<param>` tag: it reaches nothing here and costs two warnings, see `references/project-facts.md` |
| anything in `newai_2.0.json` | the Node service `common/ASC.NewAi` — `scripts/schema/schemaTypes.ts`, `scripts/schema/shims.d.ts`, JSDoc on the local types. No C# edit can reach this document |
| a framework or vendor type surfacing in the public API (`NoContentResult`, `KeyValuePair*`, third-party package types) | the **signature**, not the documentation |

That last row is the one most often fixed the wrong way. When a foreign type appears in the contract,
the honest options are to own it (declare and document it deliberately) or to drop it from the
declaration when it was never really part of the payload. Hardcoding a description for it in a schema
filter makes the finding disappear while leaving the leak in the contract, which is strictly worse
than the finding — it removes the only signal that the leak exists.

The table routes you to the right **file**. The finer mapping inside it — which XML doc tag becomes
which OpenAPI field, which attribute overrides which, which csproj flag has to be on for any of it to
appear — lives in `references/project-facts.md`, not here: those are facts about the project rather
than steps of the loop, each was established once at the cost of a careful reading, and keeping them
in their own document means they can be corrected without touching the procedure.

**Read `references/project-facts.md` before you start reading source.** Several of its entries are
counter-intuitive enough that a confident guess gets them backwards — one of them will tell you that
this repository uses `<summary>` and `<remarks>` the opposite way round from ordinary C# habit. When
you work something out that is not in there yet, add it, with the date and what proved it.

Then ask how far the source reaches, because that decides how many documents you must rebuild:

- `AddOpenApi` has a single call site (`common/ASC.Api.Core/Core/BaseStartup.cs`), so anything on the
  shared `OpenApiInfo` is one edit for every C# document at once. The corollary is that the text has
  to be true for all of them — service-specific wording there is a bug.
- A DTO shared by two services fixes two documents. A controller fixes one.

Do this walk with LSP (`workspaceSymbol`, `findReferences`, `goToDefinition`), not text search:
`.claude/rules/csharp-lsp.md` is a hard rule in this repository, and it is also simply the right tool
here, since the question is almost always "who else uses this" rather than "where is this string".

## 4. Decide the fix — and stop if it changes the wire

State the fix in one sentence before making it: what changes, in which file, which documents it
reaches. If you cannot say that, step 3 is not finished.

**If the fix changes what the server puts on the wire, do not make it. Present it and wait.** The
linter is a documentation tool and this loop's mandate is documentation. Renaming a route segment,
changing request binding, changing a declared response type or status code, adding or removing a
field — those are contract changes with clients on the other end, and they are the user's call, not a
side effect of paying off a linter row. Say what would move on the wire, say who it would affect, and
let them decide. If you notice you have already made such an edit, revert it, say so, and record the
finding in the memory's side-defects section instead.

A fix that only adds or corrects documentation — an XML doc comment, a description on the shared
`OpenApiInfo`, a csproj flag that makes the xml get built at all — needs no such pause.

## 5. Make the edit

Edit the source you identified, matching the surrounding code's conventions
(`.claude/rules/csharp-style.md`). Fix the whole population of the target rule that this source
covers; if the population spans several sources, either do them all or say which you left.

**Use the Edit tool, not `sed -i` or any other in-place shell rewrite.** The C# sources here are
CRLF, and GNU `sed -i` writes the file back LF: every single line then reads as changed, your
two-line fix becomes an unreadable whole-file diff, and recovering the original line endings costs
several times what the fix did. This is the most expensive avoidable mistake in the whole loop, and it
is invisible until you look at a diff and see the entire file. If it has already happened, re-normalise
the file to CRLF and confirm byte-equality against its committed version before continuing — do not
try to read the fix out of the wrecked diff.

## 6. Verify the code, not just the build

A successful build proves the file parses, which is the least interesting property it could have.
Three checks that always apply — each catches what the others miss — and then a fourth, for
documentation fixes, that is worth more than the three together:

```bash
dotnet build <the project you edited>
dotnet format style <the project you edited> --verify-no-changes
```

- **LSP diagnostics on every file you touched** — type errors and missing usings surface here before
  they cost you a build.
- **`dotnet format style … --verify-no-changes`**, because the compiler does not check naming and this
  repository enforces style separately.
- **Read your own diff.** For XML doc edits especially: an unclosed tag, or a `<param>` naming a
  parameter that does not exist, compiles perfectly and silently produces a wrong document.

For a documentation fix there is a fourth check worth more than the other three, because it tests the
one link in the chain that nothing else does — whether your text actually reached the artefact the
document generator reads. The build writes the XML doc file next to the assembly; grep it for the
member you edited:

```bash
grep -A6 'M:<Namespace>.<Class>.<Method>' $(find <project-dir>/bin/Debug -name '<Assembly>.xml')
```

(`find` rather than a literal path because the layout is not uniform here — some projects write the
xml to `bin/Debug/`, others to `bin/Debug/net10.0/`.)

If your text is not in there, the generator will never see it, and no amount of regenerating will fix
the finding. An unrecognised doc tag is precisely the case that passes the compiler, passes the style
check, passes a reading of the diff, and fails here — which is why this check is cheap insurance
against paying for a regeneration that cannot possibly work.

Fix whatever fails here. Carrying a broken edit into a regeneration wastes the expensive step and
makes the resulting document impossible to interpret.

### Have the edit reviewed by a clean agent

Everything above is mechanical: it tells you the edit compiles, is styled correctly and reached the
xml. What none of it can tell you is whether the edit is the *right* one — and you are the worst
available judge of that, because you formed a hypothesis in step 3 and have been building on it since.
An edit that follows from a wrong reading of the finding passes every check on this page.

So before paying for the regeneration, hand the diff to a **fresh subagent that has none of your
context**, and give it deliberately little:

- the finding, verbatim from the report — rule, message, JSON path, document;
- the diff of what you changed;
- the file the change is in, so it can read around the edit;
- **read-only instructions**: review, do not edit, do not build, do not run the linter.

Ask it four questions: does this edit plausibly remove that finding; is it correct C# and well-formed
XML doc; **does anything in it change what the server puts on the wire**; does it match the
conventions of the code around it. Ask for a verdict plus reasoning, not a rewrite.

Withholding your reasoning is what makes the answer worth having. A reviewer told *why* you think the
fix is right will almost always agree — it has been handed the conclusion. A reviewer given only the
finding and the diff has to re-derive the connection, and when it cannot, that is the signal you were
looking for.

Expect the payoff to arrive in one of two shapes, because they trade off against each other. On a
subtle fix, the value is the disagreement: the reviewer cannot get from the finding to your diff, and
you have just learned something before it cost you a regeneration. On a fix so self-evident that the
diff *is* the argument, the reviewer will not disagree and cannot — but it arrives with fresh eyes on
a repository you have been reading narrowly for the last hour, and it tends to come back with an
independent clearance on the wire question plus facts you had stopped looking for: how the generator
actually maps this construct, whether this exact defect has happened here before. Those are worth
recording in the memory whichever way the verdict goes. So do not skip the review on an easy finding
on the grounds that it cannot fail — on easy findings it is not a safety net, it is a second pair of
eyes, and that is a different thing worth paying for.

What to do with the verdict — and two of the three answers end the pass:

- **It flags a wire change** → stop. Present it to the user and wait, even if you are confident the
  reviewer is wrong; the reviewer disagreeing about this is itself worth the user's attention. Record
  the pass as step 9 describes.
- **It flags a correctness problem** → stop. Do not patch the edit and review again. The finding stays
  open, the pass ends here, and what you write down is the finding, the edit you made, and the
  reviewer's objection in its own words. This is the hardest of the three to obey, because the repair
  almost always looks like a two-minute job — but what the reviewer has actually told you is that the
  reading of the finding you have been building on since step 3 does not hold, and a patch aimed at
  the objection rather than at the misreading tends to produce an edit that passes the second review
  and still fails step 8. Ending here costs one pass; the next one re-reads the finding with the
  reviewer's objection already in the memory, which is a much better place to start from.
- **It has only preferences** → note them, decide for yourself, carry on. The reviewer advises; it does
  not hold a veto over a judgement you can defend. This is the only verdict the loop continues past.

The line between the second and the third is the judgement you have to make here, and it is not about
how big the objection is: an unclosed XML tag is a one-character repair and still a correctness
problem. Ask what the reviewer is claiming. That the edit is wrong, malformed, or may not remove the
finding — correctness, stop. That the edit would read better phrased differently — preference, note
it and carry on. When you genuinely cannot tell which one you are looking at, treat it as correctness:
the cost of stopping is one pass, and the cost of carrying a misread edit into step 7 is the whole
regeneration plus a report that is now hard to attribute.

If no subagent facility is available in this session, do not skip the intent of the step: re-read the
diff against those same four questions, out loud in your report, and say that no independent review
was possible. The review is cheap next to the build it protects — it also does not depend on the
build, so it can run while the build does.

## 7. Regenerate the documents, then re-lint

Per service, straight into `json/` (the csproj sets `OpenApiDocumentsDirectory`):

```bash
dotnet build web/ASC.Web.Api/ASC.Web.Api.csproj                      -p:GenerateApiDocs=true  # api_2.0.json
dotnet build products/ASC.Files/Server/ASC.Files.csproj              -p:GenerateApiDocs=true  # files_2.0.json
dotnet build products/ASC.People/Server/ASC.People.csproj            -p:GenerateApiDocs=true  # people_2.0.json
dotnet build common/services/ASC.Data.Backup/ASC.Data.Backup.csproj  -p:GenerateApiDocs=true  # backup_2.0.json
dotnet build products/ASC.AI/Server/ASC.AI.csproj                    -p:GenerateApiDocs=true  # ai_2.0.json
```

The proof a document was actually rewritten is the line `Writing document named '2.0' to
…json\<name>_2.0.json` in the output — not the build's success line, which appears just as happily
when nothing was regenerated, and which is localised, so grepping for "Build succeeded" can find
nothing even on a perfectly successful build. The `Writing document` line stays English.

- Rebuild **every** document the fix reaches, and `ai_2.0.json` along with them whenever the shared
  generator changed: it is not linted, but leaving it as the single stale C# document turns the next
  diff into a puzzle for whoever reads it.
- `newai_2.0.json` is not built by MSBuild: `yarn install --immutable && yarn openapi` in
  `common/ASC.NewAi`. Needs Node and Yarn.
- Do **not** build the `ASC.Api.Documentation` project or solution to regenerate documents. Its build
  drags in the whole service graph and then runs the Markdown/SDK targets, which need
  openapi-generator-cli, Maven, a JDK and Node. Nothing about a linter fix requires any of that.
- `sdk/docspace-api-spec/docspace-backend.yaml` and the generated SDK READMEs inherit `info` and
  descriptions, but they are a separate generation step. Do not run it here — tell the user the change
  will land there at their next SDK regeneration.

**Before anything overwrites them, copy both the old report and every document you are about to
rebuild into the scratchpad.** `--output` overwrites the report in place and the build overwrites the
document in place, so this is the only moment the previous state exists. `lint/` is gitignored, so
there is no committed copy of the report to fall back on.

```bash
cp lint/report.md "<scratchpad>/report-prev.md"
cp json/<name>_2.0.json "<scratchpad>/<name>_2.0.prev.json"
```

The report copy is what makes step 8's row-level diff possible. The document copy is what lets you
say exactly what your edit did to the contract — a good documentation fix shows up as a handful of
added lines and nothing else, and seeing that is the difference between believing your fix was
surgical and knowing it. It also catches the opposite case early: a one-line intent that moved fifty
lines of document is telling you the edit reached further than you thought.

Then re-lint. **The command lives in the header comment of `SDK/.spectral.yaml` — read it and run it
verbatim** rather than reconstructing it. That header is the single source of truth for the document
list, the three output formats and the severity flag, and it records why each part is there. Four things
it says that decide whether a run is usable at all:

- Call `spectral.cmd`, not `spectral`. On Windows the bare name resolves to npm's unsigned
  `spectral.ps1` shim, which a restrictive PowerShell execution policy refuses to run; the header
  records the reasoning.
- `lint/` must already exist. `--output` does not create directories.
- The documents are listed **explicitly**, never as a glob — a `json/*_2.0.json` glob picks up files
  that nothing references.
- `--fail-severity hint` is not optional. The markdown formatter prints only findings at or below
  failSeverity and the CLI default is `error`, so without the flag `report.md` collapses to a handful
  of errors while `report.html` stays complete — a difference easy to misread as progress.

Exit code 1 is the normal outcome, since `--fail-severity hint` makes any finding at all produce it.
Read it as "there are findings", never as "the run failed". With both `--output.*` flags set the run
prints nothing at all on success — silence is expected, not a sign it did not run, and the reports'
timestamps are what tell you it did. Do not read the exit code through a pipe either: `| tail` hands
you tail's status, not spectral's.

Finally, confirm the reports are newer than every input document before drawing any conclusion from
them. There is precedent for a report describing a tree state that no longer existed:

```bash
ls -la --time-style=+%Y-%m-%d_%H:%M json/*_2.0.json lint/report.md
```

The header command writes three files and each has one job: `report.md` is what step 8 greps and
diffs, `report.html` is its complete twin, and `report.json` is what `count.py` aggregates. None of
them is meant for showing to anyone. When the ask is to *look at* the findings rather than to close
one, render a fourth artefact from the json with `@api-common/spectral-reporter`, written out under
**Tool traps** in `references/project-facts.md`. That is an addition, never a replacement: step 8's
checks are written against the three above, so produce those as usual.

## 8. Prove the target finding is gone

**The indicator that matters is this: the finding is absent from a freshly generated report, produced
by re-linting documents that were themselves rebuilt after the fix.** That is what "fixed" means in
this loop — not that the code reads correctly, not that the diff is convincing, and emphatically not
that a different rule now sits on top of the report. Check 1 is that indicator, and it is what to lead
with when you report back.

Checks 2–4 are guards, and they exist because the indicator has exactly one failure mode: an absence
that was never a fix. A rule can leave the report because the document dropped out of the file list,
because the ruleset stopped parsing, because a severity change filtered the markdown, or because the
report you are reading predates the rebuild. Each guard rules out one of those, and the three together
cost a fraction of the regeneration you have just paid for. Run them every time — an unverified
absence becomes a zero in the memory that nobody re-examines.

One thing that is *not* evidence either way: the git diff of your fix. It can be legitimately empty —
when the finding came from a change that was never committed, your fix restores the file to its
committed content and `git diff` shows nothing. That is not a sign you changed nothing, and it is not
a reason to undo a correct fix. The report and the regenerated document are the evidence, and neither
cares what happens to be committed.

1. **The indicator — the rule is absent from the report, by name in markdown and by message text in
   html.**

   ```bash
   grep -c "<rule-name>" lint/report.md
   grep -c "<the rule's message text>" lint/report.html
   ```

   Both must be 0, and it has to be two different greps because the two formatters emit different
   things. The html formatter writes only position, severity and message — **no rule names at all** —
   so grepping html by rule name returns 0 for every rule, fixed or live. It can never fail, and
   passing it proves nothing.

   Checking html at all still matters: the markdown formatter obeys `--fail-severity` and the html one
   ignores it, so a finding present in html and absent from markdown means the markdown was filtered,
   not that the finding was fixed. Cross-check the html tally, which is exactly one match per finding:

   ```bash
   grep -o 'class="severity' lint/report.html | wc -l
   ```

   (No closing quote in the pattern — the class is `severity clr-warning` / `severity clr-error`.)

2. **Guard — the documents themselves carry the fixed construct.** This is the check that separates "fixed"
   from "no longer evaluated", and the only one written against reality rather than against the
   report. Assert the actual shape in every linted document — for a description on `info`, say:

   ```bash
   python -c "import json,glob; [print(f, bool(json.load(open(f,encoding='utf-8'))['info'].get('description','').strip())) for f in sorted(glob.glob('json/*_2.0.json'))]"
   ```

   Rewrite the predicate for your rule: a property's `description`, a path segment's casing, a
   parameter's `example`. If you cannot express the predicate, you do not yet know what you fixed.

3. **Guard — the arithmetic is exact.** From `count.py` on the fresh report: TOTAL dropped by exactly the
   number of findings the target had, and **every row that is not the target is unchanged**. A bigger
   drop means something stopped being linted — investigate before celebrating. A smaller one means
   part of the population is still open; find which document still has it.

   When one edit closes findings from several rules — a single undocumented type usually trips both a
   schema rule and a property rule — predict the split per rule *before* the run and check each
   number, not only TOTAL. A right total with a wrong split means you closed something other than what
   you aimed at. The sharpest form of this check pins down *which* findings left:

   ```bash
   diff <(cut -d'|' -f2,3 "<scratchpad>/report-prev.md") <(cut -d'|' -f2,3 lint/report.md)
   ```

   The expected diff is deletions only, exactly the target's findings, and no changed line.

4. **Guard — no new rule appeared, and the document moved only where you expected.** A regenerated
   document can introduce debt of its own; that is a new row in the memory, not something to fold into
   the row you just paid. Diff the document against the copy you took in step 7:

   ```bash
   diff "<scratchpad>/<name>_2.0.prev.json" json/<name>_2.0.json
   ```

   A documentation fix should show a handful of added lines and nothing else. Anything larger means
   the edit reached further than you intended, and you want to know that now rather than from whoever
   reads the contract next.

**If the target finding is still in the fresh report, the pass ends here.** So it does when a guard
shows that the absence was not a fix: the document does not carry the fixed construct (guard 2), the
arithmetic does not add up (guard 3), or the html still lists a finding the markdown dropped (guard
1's cross-check). Say which check failed and what it showed, record the pass as step 9 describes, and
hand it to the user. Do not adjust the edit and regenerate again in the same session.

Guard 4 is the exception, and the distinction is worth being precise about. A *new* rule appearing in
a document your fix regenerated does not mean the target failed. If the indicator and guards 1–3 hold,
the target is closed and this pass succeeded — the new rule is a new row in the memory's live debt and
a line in your report, for the next pass to take. What ends a pass is the target still being open, not
the report having grown a different problem.

A fix reported as done on a failed check is worse than no fix, because it becomes a zero in the memory
nobody re-examines. But a second attempt inside the same session is nearly as bad, for a different
reason: the tree now holds one unproven edit and one regeneration, you know the analysis went wrong
somewhere and not yet where, and every further round buries the evidence of the first attempt under
the second. The most valuable thing this pass can still produce is a precise account of an edit that
should have worked and did not — which document still carries the construct, which count moved and by
how much, what each guard said. Written down, that is most of the next pass's step 3 already done.
Iterated over, it is gone.

## 9. Record what was learned

If `.claude/skills/openapi-lint-fix/lint-memory.md` does not exist, create it from
`references/memory-template.md` first, then fill it in. Write it in English.

What goes where is in the template. The parts that are decisions rather than formatting:

- **A resolved rule keeps its history in place** — `~~103~~ 0` with the measuring date and the
  *cause*, not just the fact. The cause is the whole value of the entry; "resolved" alone tells the
  next run nothing it can act on.
- **A rule measured at zero may be ratcheted** to `error` in `SDK/.spectral.yaml`, with the measured
  count and date in a trailing comment. A rule with any remaining violations stays where it is. The
  ratchet is what makes paid debt permanent — without it a regression returns as a warning nobody
  reads. Move the memory row and the ruleset line **in the same edit**: they are one claim written in
  two places, and a stale one is worse than none. (This is the ratchet case, where a rule reaches zero
  for the first time. The regression case immediately below has no row to move.)
- **A rule that was already at zero and came back is a regression, and it is recorded differently.**
  You will meet this whenever a finding fires on a rule the ruleset already holds at `error` with a
  measured-zero comment: nothing new was discovered, something previously paid for broke. There is no
  severity to raise and no new debt row to open. What to write instead:
  - **The count chain keeps every measurement, increases included** — `~~4~~ 0 → 1 → 0`. Never erase
    the bump. A chain that only ever descends hides the fact that this rule has a way of coming back,
    which is exactly what a future reader needs to know about it.
  - **The entry stays in the closed-debt section**, gaining a dated line: what re-opened it, how it
    got in, what closed it again. The ratchet catching a regression is a success of the ratchet and
    should read as one.
  - **Re-date the ruleset comment rather than rewriting it.** The original measurement was not wrong;
    it has simply been confirmed again at a later date.
  - **Say how it got in**, because that is the transferable part. A regression that reached the tree
    without tripping the compiler, an analyzer or a review points at a hole in the toolchain, and that
    hole belongs in the side-defects section even after the finding itself is closed.
- **TOTAL belongs to the run, not to a rule.** Append it to the chain of previous measurements so the
  trend stays readable, and make sure the per-rule counts sum to it.
- **Every generator fact or tool trap you had to work out goes to `references/project-facts.md`**, not
  into the memory — the construct, the field it becomes, what proved the mapping, and the date. These
  are the cheapest entries anywhere to write and among the most reused: the next run picks them up in
  step 3 instead of reading a generator. Write one even when it felt obvious the moment you found it,
  and especially when it runs against the framework's usual convention, since that is exactly where
  the next confident guess goes wrong. If an entry already there turned out to be wrong or stale,
  correct it in place and re-date it rather than adding a contradiction.
- **A finding analysed to the bottom that you could not close goes to the blocked section**, with what
  it would cost on the wire and whose decision it is. That entry is what saves the next run from
  spending itself on the same analysis.
- **A real defect you noticed that no rule fires on goes to the side-defects section** — what is
  wrong, what proves it, what blocks the fix, whose call it is. It has no rule and cannot get a debt
  row, and it is often the most valuable thing a pass produces.

### When the pass stopped short

A pass that ended at step 4, 6 or 8 still has something to record, and it is often worth more per line
than a successful one: a closed finding leaves a fix anyone can read in the diff, while a stopped pass
leaves only what you write down. Put it in the memory's §6 and keep it to what the next pass needs.

- **The finding, verbatim**, and where the pass stopped — which step, which check.
- **What you tried**: the edit, by file and symbol, and the one-sentence statement from step 4 that
  led to it. This is the entry's whole point — without it the next pass re-derives the same fix and
  walks into the same wall at the same cost.
- **What contradicted it**: the reviewer's objection in its own words, or the check that failed and
  the numbers it gave. Quote rather than paraphrase; your paraphrase is filtered through the
  hypothesis that has just turned out to be wrong.
- **The state of the tree**: which files are modified, whether the documents were regenerated,
  whether `lint/report.md` is fresh. Expensive to reconstruct, cheap to write down now.

**Leave the edit in the working tree**, unless step 4 already told you to revert it as a wire change
or it broke the build. Deleting it destroys the evidence, and whether an unproven documentation edit
is worth keeping is the user's call rather than yours — which is exactly why the state-of-the-tree
line has to be precise enough for them to act on without re-reading the diff themselves.

If the memory has no §6 yet, add it from `references/memory-template.md`. A rule with a stopped pass
against it keeps its §2 row and its count unchanged: an attempt that failed is not a status change,
and moving the row would claim progress the report does not show.

Then report to the user. For a pass that closed the finding:

```
Target        <rule> — <n> findings, <where>
Cause         <what in the generator produced it>
Fix           <what changed, in which file>
Review        <the clean agent's verdict, and what you did about it — or that none was possible>
Verification  <the indicator first: the rule is absent from a report regenerated after the fix.
              Then TOTAL before → after, and what each guard showed>
Not closed    <the rest of the rule's population, neighbouring rules on the same object,
              downstream artifacts deliberately not regenerated>
Memory        <which sections were updated or created>
```

The "not closed" line is the one to resist trimming. Closing `info-description` leaves `info-license`
and `license-url` on the very same `info` object, and the next run should hear that from you rather
than discover it.

For a pass that stopped, lead with the fact that it stopped — a user skimming the first line must not
be able to mistake it for a success:

```
Stopped at    <step and check — "step 6, reviewer: the edit documents the wrong overload"
              / "step 8, indicator: 3 of 3 findings still present">
Target        <rule> — <n> findings, <where>
Cause         <what in the generator produced it, as far as the analysis got>
Tried         <what changed, in which file>
What failed   <the reviewer's objection, or the check and its numbers>
Tree state    <files modified, documents regenerated or not, report fresh or not>
Memory        <which sections were updated or created>
Next          <what the next pass should read first>
```

`Next` is a pointer, not a proposal — one line naming the file, the rule or the guard output worth
starting from. Working out the right fix is the next pass's step 3, done with a fresh reading of the
finding; sketching it here from the analysis that has just been contradicted is the same mistake as
repairing the edit, only cheaper for the next pass to ignore.
