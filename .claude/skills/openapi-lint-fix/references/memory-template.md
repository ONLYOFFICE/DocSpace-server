# Memory template

Copy this file to `.claude/skills/openapi-lint-fix/lint-memory.md` on the first run, then fill it in.
Six sections, each answering a question the next run will otherwise have to re-derive at full cost.

Keep it in English, keep it short, and keep every claim dated. An entry that says what happened
without saying when stops being trustworthy the moment the tree moves on; an entry that says the
*cause* stays useful even when the counts are stale.

What does **not** belong here: anything already recorded elsewhere. The loop's own procedure belongs in
`SKILL.md`, rule rationale belongs in the ruleset's comments, the finding list belongs in
`lint/report.md`, and **how the generator behaves plus the traps in the tooling belong in
`project-facts.md`** — those stay true whether or not anyone ever runs the linter again, while
everything in this file is a measurement with a date on it. This file is for what a *run* learned that
none of the others can tell you.

What also does not belong here: conclusions imported from someone else's notes. This is the memory of
this skill's own runs. If a claim in here was not measured by a run of this loop, say where it came
from and treat it as a lead rather than a fact.

---

```markdown
# OpenAPI linter memory

## 1. Last measurement

- Date: YYYY-MM-DD
- TOTAL: <n> findings in lint/report.md
- Previous measurements: <n> → <n> → <n>   (oldest first, so the trend is readable at a glance)
- Ruleset: SDK/.spectral.yaml, <n> rules at error, <n> at warn, <n> off

## 2. Live debt

One row per rule with at least one finding, sorted by count descending. `severity` is the severity the
findings were *counted* at, not the rule's severity in the ruleset today — otherwise a row that gets
ratcheted mixes two points in time.

| n | severity | rule | documents | status |
|---|---|---|---|---|
| 70 | warn | path-segment-camel-case | newai 65, api 3, files 2 | blocked, see §3.x — renames public routes |

The counts must sum to §1's TOTAL. The `documents` column carries the spread; `status` carries only
what the spread cannot say — why the debt sits where it does, and what it is waiting on.

## 3. Blocked findings — do not re-derive

One subsection per finding analysed to the bottom and found not closable as documentation. This is the
section that pays for itself: the report's first row is often one of these, and without the write-up
every run rediscovers the same wall.

### 3.x `<rule>` (<n>) — <one-line reason>

- **What fires**: the construct in the document, and where.
- **Where it comes from**: the source, by file and symbol.
- **Why it is blocked**: what would move on the wire if it were fixed.
- **Ways out**: each option with its trade-off. "Impossible" is almost never the truth; "costs X, and
  X is the user's call" usually is.
- **Whose decision**: who has to agree before this can move.

## 4. Closed debt (ratchet log)

### `<rule>` — ~~<n>~~ 0 (measured YYYY-MM-DD), raised to `error`

- **Cause**: what actually produced the findings.
- **Fix**: what changed, in which file, and which documents it reached.
- **Verification**: the indicator (absent from a freshly generated report) plus which guards ran and
  the numbers they gave.
- **Did not close**: what stayed open on the same object or in the same rule.

The count chain keeps **every** measurement, increases included — `~~4~~ 0 → 1 → 0`. A rule that went
back above zero and was closed again stays in this section and gains a dated line saying what
re-opened it, how it got in, and what closed it; the severity does not move, because it was already
at `error`, and the ruleset comment gets re-dated rather than rewritten. Never flatten the chain into
a clean descent: the fact that a rule has a way of coming back is one of the more useful things this
file can tell a future reader.

## 5. Side defects

Real defects noticed while working that no rule fires on. They have no debt row and cannot get one.
Each entry: what is wrong → what proves it → what blocks the fix → whose decision it is.

A regression that reached the tree without the compiler, an analyzer or a review catching it belongs
here too, even after the finding itself is closed: the finding was the symptom, the hole in the
toolchain is the defect.

### 5.x <short title>

## 6. Stopped passes

One entry per pass that ended at step 4, 6 or 8 without closing its target. This section is what makes
the one-attempt rule pay: a pass stops precisely because it learned something that contradicted its
own analysis, and that is the one thing no other file records. Without the entry the next pass reaches
the same fix by the same reasoning and buys the same wall a second time.

An entry stays until the finding is closed, then moves into §4 as part of that rule's story — the
attempts that did not work are half of what makes the eventual cause believable.

### 6.x YYYY-MM-DD — `<rule>`, stopped at step <n>

- **Finding**: verbatim from the report — rule, message, JSON path, document.
- **Tried**: the edit, by file and symbol, and the one-sentence reasoning that led to it.
- **Contradicted by**: the reviewer's objection quoted in its own words, or the check that failed and
  the numbers it gave. Not a paraphrase — the paraphrase is filtered through the wrong hypothesis.
- **Tree state at the stop**: files modified, documents regenerated or not, report fresh or not.
- **Read first next time**: the pointer, not a plan.
```
