---
name: review-branch
description: "Review a DocSpace branch against the repo's own rules (`.claude/rules/*.md`, CLAUDE.md) and its OpenSpec specs for correctness, performance and security: scope the diff against the right base, run the mechanical scan, fan out rule/correctness/contract/performance/security/translation/OpenSpec reviewers, verify every finding in the code, check build and style, and report in a fixed format. Also the re-check after fixes: which findings are closed, which are still open, what is new; the author's self-review while working; and the hand-over check of a finished branch (one pass by default, two independent passes for a large branch, the author's batch check, scan.py --gate, the Hand-over block the reviewer checks first). USE FOR: review the branch, check my branch before review, self-review, check it before I hand it over, hand-over check, look at the changes in a branch, does the branch follow the rules, does the branch break the specs, the fixes were merged - check them, re-check once more, check the fixes against the review file, find all the findings, why does every run find something new - in any language the user writes in. DO NOT USE FOR: fixing the findings (that is a separate request, except after a self-review or a hand-over check), a single-file question, a security-only audit (claude-security), a performance-only pass (dotnet-diag:analyzing-dotnet-performance), running a suite without a review (run-tests)."
---

# Review a branch against the repo rules

The review answers two questions: **does the branch follow the rules written in this repo**, and
**does it do what it claims without breaking something else**. Everything else — style taste,
alternative designs, "I would have written it differently" — stays out unless a rule backs it.

The review is read-only. Never edit product code, never commit, never switch branches, never
touch submodule checkouts or the user's running DocSpace. Fixing is a separate request; the
exceptions are the author's self-review and hand-over check of their own branch (§0, §8 "Self-review").

Report in the user's language. Section headings below are given in English; translate them along
with the rest of the report.

## 0. Which mode

- **First review** — nothing reviewed on this branch yet in this conversation, and the user gave no
  review file.
- **Re-check** — "the fixes are merged, check", "re-check", a path to a previous review `.md`, or an
  earlier review in this conversation. Go to §7 after §1.
- **Saturation** — "find all the findings", "why does every run find something new", "review until
  nothing is left". Go to §9: two independent full passes on one frozen sha and an estimate of what
  is still unfound.
- **Self-review** — the author's one-pass check of their own branch while it is being written:
  "self-review", "check my branch". It runs when the user asks for it: a run of this skill with its
  reviewer agents is the user's call. Run it as a first review, against the base the reviewer will
  use, with `--worktree` while the work is uncommitted. Only the end differs: §8 "Self-review". A
  finished branch gets the hand-over check below instead; when each of the two, the author's batch
  check and a §7 re-check run or are offered is `.claude/rules/fixing-findings.md` §6, the one
  schedule of all of them.
- **Hand-over** — the author's check of a finished branch before it goes to the reviewer: "check it
  before I hand it over", or a yes to the hand-over check `fixing-findings.md` §6 has the author
  offer. Two depths, both ending with the self-review's end (§8 "Self-review": all findings fixed in
  one batch, the batch check on it), `scan.py --gate`, and the Hand-over block (§8):
  - **light**, the default: the whole first review once (§1–§6: every lens, the matrices A0 and M,
    the build). It costs about one review.
  - **full**, for a branch of more than ~1 500 added product lines or one with a multi-step operation
    (`failure-paths.md` §10), or when the user asks: A, A0, the matrices M, C, F, S and the build once,
    and the lenses B, D, E and G as §9's two independent passes on one frozen state, with the estimate
    of what is still unfound. It costs §9's two to three times a review: say so when offering it.
  One pass finds about 55–60% of what is there (`fixing-findings.md` §6), so the reviewer's own pass
  keeps finding what the author's skipped; on a large branch two passes and the estimate are what
  make a clean hand-over likely. Uncommitted work is
  frozen as the tree the scan prints (`tree:`, the tree `git commit -a` would record, built in a
  temporary copy of the index); the block records that tree, and a commit of exactly that tree is the
  checked state. Until it is committed, the tree object exists only in the clone that ran the check.

One review covers **one sha**. While fix rounds keep landing, a fresh first review of the whole
branch finds new things every time for two reasons that look the same in the report: bugs in the
code the fix commits just added, and old code the earlier runs did not reach. §4 tells them apart
(origin); the fix is the process, not more reviewers: a full pass on a frozen sha, all its findings
fixed in one batch, the author's batch check on it (`fixing-findings.md` §6), then §7 on the diff of
that batch only. A review or re-check with no blocker and no rule violation ends the review (§8).

Arguments the user may pass in words: a branch other than the checked-out one, a base, "including
uncommitted changes" (→ `--worktree`), "run the tests" (→ §6 runs, not just proposes).

## Dependencies — none of them is allowed to stop the review

Some tools this skill uses are optional on a given machine. A missing one degrades one step; it
never aborts the review. Every degradation is named in the report header ("performance: manual
checklist only, dotnet-diag not installed"), so a thinner review is never mistaken for a full one.

| Dependency | Where it comes from | If it is missing |
|---|---|---|
| `python` 3.9+ (`python3` on Linux/macOS) | machine | do §1 by hand: `git merge-base`, `git log --oneline`, `git diff --name-status`, `git diff --check`, and the line-ending and migration checks from §2 on the changed files; there are no rule signals, so reviewers work from the diff alone |
| LSP (`csharp-lsp` plugin) | `.claude/settings.json` `enabledPlugins` | say so once; navigate with `codegraph_explore` if `.codegraph/` exists, otherwise by reading the files. Blast-radius claims ("all call sites") are then marked unverified |
| `codegraph_explore` | user-installed, per-machine index | skip it silently, LSP only (`csharp-lsp.md`) |
| `dotnet-diag:analyzing-dotnet-performance` | `dotnet-diag` plugin, marketplace `dotnet/skills` | reviewer D walks its own checklist (§3 D.3) plus the manual list in `csharp-style.md` → Performance work |
| `claude-security` | user-level plugin, not in the repo settings | do not offer the deep scan; reviewer E's checklist is the whole security pass |
| `dotnet` SDK / a free build | machine; a running DocSpace locks DLLs | §5 reports "not built" with the reason; the review continues on reading |
| repo skills (`run-tests`, `db-migrations`, `notify-emails`) | `.claude/skills/` | always present with the repo |

How to tell: a skill is available only if it is in the session's skill list; calling an absent one
returns `Unknown skill` — treat that as "missing", not as an error to retry. A subagent may not
see the same skills or tools as you: tell reviewer D to fall back on its own when the skill call
fails, and do not rerun the reviewer because of it.

## 1. Scope the diff

```bash
git fetch origin --quiet
python .claude/skills/review-branch/scan.py            # add --base origin/develop, --worktree as needed
python .claude/skills/review-branch/scan.py --head <branch>   # a branch that is not checked out (a remote ref, a PR head)
```

`--worktree` scans the checked-out working tree only; with a `--head` that names another ref (another
branch, even at the same commit) the scan refuses it (exit 2). `scan.py` picks the base as the remote branch with the fewest commits ahead of it (newest
`origin/release/*`, then `origin/develop`, `origin/master`) and diffs from the **merge-base**, so
merges of the base into the branch do not show up as branch changes. Read its header and check:

- **The Hand-over block, first.** Reviewing someone else's branch (a first review or a re-check),
  read the Hand-over block (§8) of the PR description, or of the message that handed the branch
  over, before anything else. The author writes it into the body of the branch's last commit too:
  when the description cannot be reached (no CLI for the git host at hand), read it there
  (`git log -1 --format=%B <branch>`), and ask the user only when neither has it.
  - **No block** (none given, or its check declined): review the branch as usual. The report header
    says "no hand-over check", and the report has no "Missed by the hand-over check" section. A
    colleague who does not run this skill hands branches over this way.
  - **A block**: check it yourself, it takes a minute, on the reviewed branch `<branch>` (the remote
    ref or the PR head, not necessarily the checked-out `HEAD`):
    - `git rev-parse "<branch>^{tree}"` (quoted: PowerShell reads an unquoted `{tree}` as a script
      block) equals the block's final tree — a commit of exactly the checked tree is the checked
      state, a different tree means changes landed after the check;
    - `scan.py --gate --head <branch>` (the base's copy, see "The rules are the base's" below) prints
      its "## Gate" section and lists no gate line except "the base changed rules or review tooling":
      that one is a note for the report header, since the base may have moved after the hand-over. A
      run without the section did not run the scan (an empty copy, a wrong path), whatever its exit
      code;
    - the block reports no open blocker or rule violation.

    When one of these fails, the block promises a state the branch is not in: say so in one line and
    stop; the branch goes back to the author without a review, unless the user asks to review it
    anyway, and then the report header names the gap. This is stricter than for a branch with no
    block on purpose: a block that fails these checks claims a state the branch is not in, while a
    missing one claims nothing. What the author does depends on which check failed: for another tree,
    renew the block (§8 "Hand-over block": the batch check of the change since the final tree); for a
    failing gate or an open blocker or rule violation, fix it (`fixing-findings.md` §11)
    and write a new block. Removing the block instead is open only when nothing in it was open.

  The block holds counts and coverage, and no finding except the accepted ones under Known
  limitations (§4).

- **The base is right.** A `feature/*` cut from `develop` but scanned against `release/*` drags in
  half of develop. If the commit list contains commits that are obviously not this branch's work,
  re-run with an explicit `--base`. **0 commits** — wrong branch or already merged: stop and say so.
- **The tree is clean** (for the checked-out branch), or the user asked for `--worktree`. Uncommitted changes are not in the
  scan otherwise — say which one you reviewed.
- **No new file is left out.** `--worktree` sees tracked files only, and so does `git diff HEAD`: a
  file the branch created but nobody added is reviewed by no one. The scan lists the untracked
  files (`!! ... untracked file(s) are NOT scanned`), and `--gate` fails on them. Offer the ones that belong to the branch to the
  user as a list to `git add` — leave out what is clearly not its own, such as a folder another
  branch left behind or a saved review — and re-scan once they are added. A file the user keeps
  untracked goes under "Not verified" and stops the hand-over gate, except the review of this branch
  that this skill saved at the repo root (`<branch>-review.md` or `<branch>-self-review.md`, the
  branch name with `/` as `-`).
- **The rules are the base's.** The scan reports `!! the base changed rules or review tooling that
  apply to this branch` (and lists the files) when, since the merge-base, the base changed a rule whose
  `paths:` match a file of the branch or that has none (and `fixing-findings.md` always: `CLAUDE.md`
  applies its schedule to every branch; not `csharp-lsp.md`, which governs navigation, not what a
  branch holds), this skill or its scan, `prove-red.sh`, or
  `openspec/config.yaml` for a branch that touches `openspec/` — and the branch does not already hold
  the base's version of that file (a cherry-pick, the same edit). An author brings the base in first: a
  check on an older copy cannot see what the newer one checks (seen in review: a branch carried a copy
  without §3 S, the reviewer's copy had it, and the next review found six `[spec]` items). With
  uncommitted work, commit or stash it before the merge, or ask the user to merge when the user
  commits the branch; afterwards the branch is checked against what changed (`fixing-findings.md`
  §6), not again in full. Reviewing someone else's branch from its own checkout, the skill and the
  scan that load are the branch's copies: read the base's rules and this file as
  `git show "<base>:./<path>"`, and run the base's scan saved to the scratchpad
  (`git show "<base>:./.claude/skills/review-branch/scan.py" > <scratchpad>/scan.py`, then
  `python <scratchpad>/scan.py` from the repo root). Write the `:./` form: in Git Bash a
  `<base>:<path>` argument with a `/` on both sides of the colon is rewritten into a Windows path list,
  `git show` fails and the redirect leaves an empty file. Name the gap in the report header — for
  someone else's branch it is a note, not a finding.
- **Earlier checks of the branch.** A branch can come with earlier reviews, self-reviews or re-checks
  (saved files, a list in the PR description) and no Hand-over block. They are not a check of what you
  review: take from them only their Coverage sections (§3 "Start where the last run did not look") and
  two facts for the report header. First, the newest sha any of them covered: the branch's own
  commits after it (`git log --no-merges <sha>..<branch> ^<merge-base>`) were checked by no one, and a
  refactoring among them (code moved or rewritten, a new class) is new code. Second, the copy of the
  rules each of them ran with (its header's tooling line, §8), against the scan's "Review tooling brought
  in by the branch": a check made before one of those merges could not see what the merge added.
  Seen in review: a branch came after five re-checks; all five ran before the rules on texts and on
  multi-step operations reached it, the last covered a sha three commits back, one of them a
  1 300-line refactoring, and the next first review found 13 items, 4 of them in those commits.
- **Remember the reviewed sha** — of `HEAD`, or of `<branch>` when it is not checked out. Branches
  here move during a review (a merge of the base, a rename of a migration). Check it again before the
  report; if it moved, re-scan and say what the new commits changed.

Then read the commit messages: they are the branch's **declared intent**, the yardstick for scope
creep in §3, and claims to verify like code (§3 B "claims the branch writes"). Read the PR
description too when the user gives it or it can be reached: the next reviewer starts from it, so
one that no longer describes the branch is a finding.

## 2. The scan output is hints, not findings

Every section of `scan.py` is a cheap textual signal. Nothing from it goes into the report until it
is read in context. What each section is for:

| Section | Turns into a finding when |
|---|---|
| Rule signals | the hit really breaks the cited rule in that place (a `RemoveByTagAsync` on the *default* FusionCache is fine; on the `"memory"` one it is a bug). `[security]` and `[performance]` hits feed reviewers E and D |
| Line-ending flips | always report: a whole-file LF↔CRLF flip turns 30 real lines into a 8 000-line diff, kills blame and conflicts every parallel branch. Fix = separate normalization commit or revert in this branch. Check first that the base itself did not normalize the file |
| `git diff --check` | trailing whitespace / conflict markers in hand-written code (generated `*.Designer.cs` — ignore) |
| Migrations | §3 C |
| API contract and SDK | §3 C. A response code or an operation removed from a `json/*_2.0.json` is a contract change: a finding unless the endpoint really cannot return that code any more. A document the scan could not compare is named "not compared": read its diff by hand |
| Localized resources (.resx) | §3 F. `!! placeholder mismatch` is a finding once you have looked at the value (a tag lost or renamed in a translation renders as literal `$Tag` or as nothing); "neutral changed, translation untouched" is one only if the commits do not declare it ("English only for now") |
| Tests | §3 A3 and §6. A fix commit (its subject starts with "fix") that changes product `.cs` with neither a "RED as expected" line nor a "not proven" line is a remark when the fix changes behaviour (a typo fix is not one): the batch check proves a behavioural fix red or says why not (`fixing-findings.md` §6) |
| Changed endpoints | not a finding by itself: the rows of the cross-cutting matrix (§3 A0) |
| OpenSpec | the requirements reviewer S reads (§3 S). `!! ... openspec/ untouched` is not a finding by itself: it becomes one when a listed requirement no longer holds on the branch. The `!!` format lines are findings once read: a requirement the branch adds at 500 characters or more, one it brings to 500 or more, or one it makes longer while it is already over (`openspec/config.yaml` asks for under 500), and a ticked task that says it is blocked or not done (`claims.md` §4); with `--worktree` they cover untracked spec files too. A requirement is compared with every body it had at the merge-base under its name or the name a RENAMED pair gives — in the same file, its old path and, for a delta (active or archived), its capability's spec; REMOVED sections are not measured. A requirement with no such old body whose body equals that of a requirement gone from the same file is a rename without a pair, so unchanged; a requirement moved to another spec file is measured as new. An edit of a requirement that was already that long and did not grow is a reminder line without `!!`: not a finding, a note the author may act on. `verification.md rows citing changed files` is a list to check: each row whose lines moved or whose behaviour changed is stale; a row that cites by name (`File.cs::Method`, `claims.md` §4) is listed as stale when the file's code no longer has that name (comments do not count; a renamed file is read at its new path), and otherwise as a row to re-read when the member's behaviour changed. It finds citations by path (a `.claude/` one and a spec file included), alias, file name (marked when several files share it, so the row may cite another one) and folder alias; a row citing a changed file in another form is the reviewer's to find. The first 100 entries (a changed file in one `verification.md`) and 8 line numbers each are printed, all with `--full` |
| Review tooling brought in by the branch | not a finding: each merge of the base that brought a rule or review-tooling change applying to the branch, with the branch's own commits before and after it, and the date the scanned tree's tooling last changed. A check of the branch older than the newest such merge ran on the older copy (§1 "Earlier checks of the branch"); the author runs what the merge changed over the whole branch (`fixing-findings.md` §6 "The base first"). The tooling line goes into the report header (§8) |
| Claims with numbers | §3 B "claims the branch writes": each number with a unit, worded duration, limit and date word the scan finds in the branch's commit messages, C# comments and spec texts is checked against the constant, the timeout or the test that makes it true (`claims.md` §1). A hit whose source says otherwise is a finding; a claim worded in a form the scan does not list is still read. The first 200 lines are printed, all with `--full` |
| Texts the diff writes or reaches | not a finding: the texts the author re-reads after each fix batch (`claims.md` §3) — comments and doc comments the diff writes in `.cs`, `.ts`, `.py` and `.sh` (generated files and a new file's license header left out), the doc comments of the actions whose lines it changes, the Markdown files outside `sdk/` and `openspec/config.yaml` it writes, and which commit messages are not pushed yet (`claims.md` §4). Reviewer B reads it for §3 B "claims the branch writes"; a text whose behaviour changed outside the diff is still the reviewer's to find |
| Stateful / background processing | the files reviewer G walks (§3 G) |
| Size | how many chunks reviewers B, D, E and G are split into (§3 Coverage) |
| New projects | an `!!` line: a project in neither `ASC.Web.slnx` nor `ASC.Tests.slnx` (a tool with a solution of its own is the exception), or a test project outside `products/` that `CLAUDE.md` does not name |
| Possibly committed by accident | a review `.md`, a `.binlog`, `.env` committed by accident |
| Checklists of the applicable rules | not a finding: the `## Checklist` of each applicable rule, which the author walks over each fix batch (`fixing-findings.md` §6). For reviewer A it is a start, never the whole rule: a checklist item points at the section that decides |
| Gate | what stops a hand-over whatever the branch is: AI attribution in a commit; a base change, since the merge-base, to rules or review tooling that apply to the branch; an untracked file (`--worktree`; the branch's own saved review at the root excepted, §1); a requirement the branch adds at 500 characters or more, brings to 500 or more, or makes longer while over; a whole-file line-ending flip of what git would commit; a conflict marker; a new source file without a license header (generated files and `thirdparty/` excepted). `--gate` exits with 1 on any, 2 when the scan cannot run. In a review of someone else's branch, a gate line is reported as a finding only when it is a defect of the branch: newer base rules are a header note (§1) |

Things `scan.py` cannot see and you must look for yourself: broken indentation left by a merge,
`<summary>`/`<remarks>` swapped, a list in `CLAUDE.md` the branch leaves incomplete (services, launch
profiles; new projects are the scan's "New projects" section). Guarantee
words in comments ("never", "only", "cannot") are not scanned for: on a real branch they give about a
hundred hits, so §3 B checks the claims while reading instead.

## 3. Fan out reviewers

Load LSP first (ToolSearch if deferred); use `codegraph_explore` only if `.codegraph/` exists
(`.claude/rules/csharp-lsp.md`). Then launch the reviewers that apply **in one message** with the
Agent tool (`general-purpose`), so they run in parallel. Skip a reviewer whose area the branch does
not touch. Each prompt must be self-contained: base, merge-base, branch sha, the file list for its
area, the relevant scan hints, the rule files to read, and the return format below.

### Coverage — enumerate, do not sample

A reviewer handed 9 000 changed lines reads some of them closely and skims the rest, and the
next run skims a different part, so every run of a large branch finds something new in code the
previous one counted as covered. So:

- **Chunk the deep lenses.** B, D, E and G each get at most ~1 500 added product lines per agent
  (the scan's "Size" section gives the count). Split by subsystem, not alphabetically — the sender
  with its worker, the payload DTOs with their helpers — so a chunk holds the code that interacts.
  A chunk over the limit is split further, never handed over whole. One agent per chunk carries
  all four lenses for it (six chunks = six agents, not twenty-four); a small branch is one chunk.
  A small branch still gets that agent and its matrices: reviewing it alone, from the main context,
  is a degradation the report header names. On bug 79163 a 340-line branch was reviewed that way,
  the class matrices were skipped, and four review rounds each found the sibling of the last fix.
- **Every reviewer returns its coverage**: the files it read in full, and the ones it only skimmed
  or did not open. A skimmed file of its chunk goes to a follow-up agent, or to "Not verified" in
  the report — never silently counted as reviewed.
- **Every finding is a class, not an instance.** The reviewer names the pattern ("a side effect
  after the action can fail the action", "a config value used without a lower bound") and sweeps
  the whole diff for it, not only its chunk: `siblings` in the return format lists every other
  place with the pattern, each marked as affected or fine. The fixer then fixes the class at once;
  a fix of the one cited line leaves the same finding to come back from its siblings.
- **Start where the last run did not look.** If a previous review file of this branch has a
  `Coverage` section (§8 Saving), give its "skimmed or not opened" files to the chunk reviewers as
  read-first. Hand over the coverage only, never the previous findings — a reviewer given the list
  checks the list.

**A0. Cross-cutting matrix** — always, when the scan lists changed endpoints or the branch adds a
write path (a service method, a background job, an event handler that changes data). One row per
endpoint/write path, one column per cross-cutting check from CLAUDE.md that applies: socket event
(`socket-events.md`), `[RaisesWebhook]` and its condition (`openapi-endpoint-docs.md` §2.1), audit
`MessageAction`, every status code it can return has a `[SwaggerResponse]`, backup/restore of any
table it writes (`backup-tables.md`), DTO through Mapperly (`dto-mapping.md`), section coverage
(`files-sections.md`), search index (`full-text-search.md`), OpenSpec requirement (§3 S: the requirement
that governs the row, and whether it still holds). Every cell is filled: `yes (path:line)`,
`n/a (why)` or `missing` — a `missing` is a finding. The matrix is mechanical on purpose: a
free-form rules reviewer does not visit every endpoint, and a cross-cutting check it skips stays
missed however many times the review runs. Include
the matrix in the report only as its `missing` cells.

**M. Class matrices** — the same idea for the classes of defect that free reading keeps missing on
one run and finding on the next. A matrix enumerates **every** instance in the diff and answers one
question per row, so within its class the result is complete, not a sample. Build each one whose
rows exist in the diff; give a large one its own agent. Rows are listed by a mechanical search
(LSP `findReferences`/`documentSymbol`, or grep for non-symbol text), and the agent returns the row
count with the search it used, so a short matrix is visible as short.

| Matrix | One row per | The cell answers |
|---|---|---|
| M1 payload fill | property of every new or changed response, event or webhook DTO | which code fills it on the path that builds **this** DTO (not on another path of the same entity — a listing fills fields a plain DAO read does not); `never` is a finding |
| M2 after-action effects | call of a notification, webhook, socket, event bus or index update after the action took effect | is a failure caught and logged so the request neither fails nor rolls back; is it built from the entry in the state it describes |
| M3 config bounds | read of a configuration value (`IConfiguration`, a `*Settings` bound from config) | lower bound, upper bound, what 0 / negative / huge does, read through `ConfigurationValueExtensions`; cite `[configuration.md §N]` |
| M4 loop termination | retry, requeue, hand-back, re-schedule or polling loop | where it ends (count, age window, terminal state), and what happens when the step throws after its side effect |
| M5 catch blocks | `catch` added or changed in the diff | is the exception logged, rethrown or deliberately dropped with a comment; can a second fault replace the first |
| M6 stored columns | column added or written by the branch | backup declaration (`UserIDColumns`, `DateColumns`, `RelationInfo`), length vs column (MySQL only, §3 C) |
| M7 out-of-band sources | call site of a cross-cutting publish outside controllers (jobs, handlers, editor callbacks) | is it declared where the contract lists such sources (`_otherSources` for webhooks, `socket-events.md` recipients) |
| M8 visibility | for each filter or endpoint that decides what a caller may learn: every source of its result (each `Union` branch, each list passed in) x every invariant, and every output channel (list, total, per-row flags, filters by hidden data, search fields, profile fields) x caller (manager vs reader of the entry, each user type) x entry kind (personal file, personal folder, room, entry in a room, public room) | what that caller learns there and whether the rule the branch states allows it; cite `[visibility-filters.md §N]`. The reader column of an endpoint that needs only read access is never empty |
| M9 states that end | each place that decides on a state with an end date (a trial, a lease, a grace period, a tariff or quota in force), found by `findReferences` on the end property, and each writer that can move the end into the past | does the decision compare the end with now; what it answers when the end is later, today, yesterday; cite `[failure-paths.md §9]` |
| M10 interrupted steps | for each multi-step operation with an effect a retry cannot undo (money, an external call, a claim held across steps): every step x (the step throws, the answer is unknown, the process stops after it, another instance takes the item over) | what is on record at that moment and what the next run does; a cell that ends in a repeated payment, a lost record or a wrong letter is a finding; cite `[failure-paths.md §10]`. Built again over the whole operation after each fix of one cell |
| M11 cache instance | each value put into a FusionCache by the diff | which process obtains it and which reads it; whether a decision falls back on it; whether it must survive a restart. Obtained in one process and read in another, or a fallback for a decision, in the `"memory"` instance is a finding; an unknown answer that removes a known entry is one too; cite `[caching.md]` "Which instance" |

M2, M4, M5, M9 and M10 are the reviewer side of `.claude/rules/failure-paths.md` (§3, §8, §7, §9,
§10); a finding in them, or one of B or G that matches a section of that rule, cites it as
`[failure-paths.md §N]`, so the author finds the rule behind it. A matrix replaces neither B nor E — it closes the classes it names. When a reviewer's finding turns
out to be a class that has no matrix yet and can be enumerated, say so in the report as a candidate
row for this table (the review itself edits nothing).

**A. Rules compliance.** The set of rules to check is **not** fixed in this skill — it is the
"Applicable rules" section of the scan, which reads `.claude/rules/*.md` and matches each rule's
`paths:` frontmatter against the diff. A rule added to the repo tomorrow shows up there without any
edit here. Distribute every applicable rule to a reviewer:

- the groups below are the default bundles for the rules that exist today;
- a rule not named in any group gets **its own reviewer** (or joins the group whose files it covers,
  when it is short) — never drop it because this skill does not mention it;
- a rule listed as "always" (no `paths:`) is checked only if it constrains the code itself;
  `csharp-lsp.md` constrains how *you* navigate and is not a review criterion;
  `fixing-findings.md` constrains how findings are fixed — it is checked only in re-check mode (§7);
  `claims.md` is checked by lens B ("claims the branch writes") over code texts and by S over spec
  texts (S runs whenever the branch changes a file under `openspec/`), so it needs no reviewer of its
  own;
- the bullet lists are reminders of common misses, not the rule. The reviewer reads the
  rule file in full and checks against it — including its Definition of done, if it has one.

- **A1** — `csharp-style.md`, `logging.md`, `caching.md`, `http-clients.md` over the changed
  product `.cs` files. Includes: `using` only in `GlobalUsings.cs`, AGPL header on new files,
  `[LoggerMessage]` with camelCase parameters (the analyzer is blind to partial-method parameters),
  FusionCache with explicit duration and `tenantId` in key and tag, tags through `CacheExtention`,
  no new named `HttpClient`, JSON options cached, no sync-over-async.
- **A-TS** — `ts-ai-style.md`, `ts-ai-failure-paths.md`, `ts-ai-express.md` and `ts-ai-tests.md`,
  only if the branch changes `.ts` files of `common/ASC.AI.Chat`. Includes: a `.catch()` that answers a constant, a
  refusal of the engine sent with 200, work before the access gate, a cast on a body field, an
  answer that echoes a storage object, a new entry or a larger count in `eslint-suppressions.json`,
  a fix without a test that fails without it.
- **A2** — `openapi-endpoint-docs.md` and `openapi-dto-docs.md`, only if controllers, DTOs,
  `ApiModels` or enums changed. Walk each rule's **Definition of done** checklist per changed
  operation/property; `<summary>` = title, `<remarks>` = description (inverted!), every code named
  in prose has a `[SwaggerResponse]`, realistic `<example>`, behavioural facts on the operation, not
  the property; for a shared DTO find every operation that binds it.
- **A3** — `tests.md`, only if test files changed. One feature = one folder, an integration-suite class under ~24 cases (a unit-test class has no limit),
  SDK not raw HTTP outside the carve-outs, `ApiException` assertions, `TestContext.Current.CancellationToken`,
  deadline polling (no fixed `Task.Delay` before an assert, no timeout token inside the loop),
  access matrices that match `FileSecurity.AvailableRoomAccesses`, `[Trait("Bug")]` instead of Skip.

**B. Correctness and behaviour** — always:

- bugs on the changed lines: null paths, lost values, off-by-one, wrong cancellation, races,
  transactions, disposed readers held across awaits;
- **scope creep**: every behaviour change the commit messages do not declare (a condition widened
  in a security check, a default changed) — not necessarily wrong, but it must be named;
- **blast radius**: for every changed member whose meaning changed (`IsForm` began to mean "any
  PDF"), run `findReferences` and walk the call sites — security filters, share rights, DTO
  mapping, sockets. This is where the expensive findings are;
- **texts made untrue by the change**: the code changed and the text that describes it did not. These
  texts sit far from the change, so the diff does not show them:
  - the `<remarks>` and the DTO `<summary>` of every action that reaches a member whose behaviour
    changed (LSP `incomingCalls` up to the controllers), even when the controller is not in the diff.
    A sentence that was true on the base and is false on the branch is an `openapi-endpoint-docs.md`
    §5 "Untrue" finding of this branch ("each converted user gets a notification email" after the
    branch began to suppress repeats);
  - comments elsewhere that state the old invariant. Search the table name, the setting key or the
    entity in comments (`audit_events` led to a comment in `PortalRetentionJob` saying only login
    history is ever deleted);
- **claims the branch writes**: check every guarantee in a new or changed comment, `<remarks>`, commit
  message or PR description ("never", "always", "only", "cannot", "at most", "exactly once", "no
  longer") against the code, the same way as a finding: true, true except for a window (name it), or
  false. A commit that promises what its diff does not contain (bounds "rejected", a retry "bounded")
  is a finding, and so are two sentences of one text that disagree. Numbers too (the scan's "Claims
  with numbers"): a duration, a count or a date in a text is the one the code really hits, including
  a dependency's own limit — "a 10 s timeout" over a command that gives up after 5 s, "up to 15
  minutes" over a fail-safe copy kept for an hour. Cite `[claims.md §N]`;
- **the other path of each change**: a default added on create — what does update do when the field
  is left out; a filter narrowed for one caller — what about the caller that relied on the old
  result; a new column — rows written before it existed, archives taken before it existed; a new
  failure mode — the success path and the opposite
  failure (a refused URL vs a DNS timeout);
- **side effects after the primary action**: a notification, webhook, socket event or index update
  that runs after the action took effect must not fail the request or roll the action back — it is
  logged and dropped. One built *from* the entry must be built while the entry is in the state it
  describes (before the delete, after the move re-read);
- **values that cross a boundary**: a string written to a column — its length against the column
  (`HasMaxLength`, `varchar(N)`); a limit counted in characters where storage counts bytes; a date
  converted to UTC twice or not at all; a configuration value at 0, negative, or above a framework
  limit (`HttpClient.Timeout` is 100 s by default);
- before calling a semantic flag a defect, read its documented meaning (XML doc, commit message):
  a `Visible` flag documented as "shown in UI pickers" is not an access control. When the code
  matches the documented intent and only the intent is debatable — it is a question, not a bug.

**C. Data, contract, infrastructure** — if the branch touches migrations, contract files,
`*.csproj`/`*.props`, AppHost, submodules or `ASC.Data.Backup*`:

- migrations: which variants changed and whether that is justified (`db-migrations` skill; a
  standalone-only change in `mysql/Standalone` can be deliberate). Only MySQL is migrated until full
  PostgreSQL support is added: a migration without a `migrations/postgre` counterpart is not a
  finding, and PostgreSQL does not go under "Not verified" for the DocSpace schema. Code that talks to
  a PostgreSQL server of its own (the forms database) is reviewed as usual. `*.Designer.cs` and the
  model snapshot updated; ordering after the newest migration in the base; no edit of an
  already-applied migration; a new table is wired into backup/restore;
- contract: a renamed/removed property, a `bool` → `bool?`, a changed route is a breaking change for
  the SDKs — were the spec and `sdk/*` submodule pointers updated, or is regeneration knowingly
  postponed? Is the submodule pointer in the branch the one tests were built against?
- packages and TFM only in `Directory.Packages.props`; no `EnforceCodeStyleInBuild` committed; new
  projects in `ASC.Web.slnx` / `ASC.Tests.slnx`.

**D. Performance** — always, over the changed product `.cs` files (not tests, not migrations). Its
author-side part is `csharp-style.md` "Work per listed entry"; cite a per-entry lookup there:

1. Sort the files into **hot path** (runs per request or per listed item: security filters, DAO
   list/search queries, DTO mapping of lists, serialization, per-row loops of reports, exports,
   indexers, socket notifications) and **the rest**.
2. Hot-path files: invoke the `dotnet-diag:analyzing-dotnet-performance` skill over them and treat
   it as a reading guide, as `csharp-style.md` → Performance work prescribes — a hit count is not a
   finding; read the method around each hit. If the skill is unavailable, say so and walk the
   manual list from that section.
3. All changed files, the cheap DocSpace list: N+1 — `await` of a DAO/cache/user lookup inside a
   loop over entries; a query without a page bound or an unbounded `IN (...)`; materializing
   (`ToListAsync`) before filtering; a new query shape with no supporting index in the migration;
   a reader/`DbContext` held open across per-row awaits; per-item allocations that could be hoisted
   (`JsonSerializerOptions`, `Regex`, `Enum.GetValues`, LINQ closures); reflection per cell;
   repeated encoding of the same string; a cache read per item where one batched read would do
   (`caching.md`); a new call on a hot path that adds round-trips to every request (e.g. extra
   `GetSharedInfo` per file).
4. Severity: a cost that grows with data on a hot path is a remark (blocker only for a clear
   O(n) round-trips per request on a list endpoint); off the hot path — omit unless gross. Every
   performance finding states the scale it matters at ("5 000 queries per page of 100") and that
   it is static analysis, not a measurement.

**E. Security** — always, over the changed product files (`.cs`, `.ts`, configs, nginx). Its
author-side rule is `access-checks.md` (what a caller may do) next to `visibility-filters.md` (what
it may learn); cite a finding of authorization as `[access-checks.md §N]`:

- **authorization**: every new or changed endpoint and service method checks access on the entry
  it actually acts on (`FileSecurity.Can*Async` / the role check) **before** acting, not on its
  parent or after the fact; `[AllowAnonymous]` and `<requiresAuthorization>false` are deliberate;
  an id taken from the request is never used without that check (IDOR); a widened condition in
  `FileSecurity` / share-rights code is a security change even when the commit calls it a fix;
- **tenant isolation**: every new DAO query filters by `TenantId`; cache keys and tags carry the
  tenant (`caching.md`); background tasks restore the tenant and user context they run under;
- **anonymous, guest and public-link paths**: what a link-holder or guest can read or enumerate
  through the new code (names of fields, ids, other users' data); link tokens and passwords
  validated on every path, including the new one;
- **input**: length and count limits, `Enum.IsDefined`, `[Range]` on paging (DoS); raw SQL
  (`FromSqlRaw`/`ExecuteSqlRaw`/interpolated SQL); file names and storage paths from user input
  (path traversal, zip slip); content-type and size checks on uploads;
- **outbound requests**: a user-supplied URL goes only through `UrlValidator.PinnedHttpClient`
  (`http-clients.md`); no disabled certificate validation outside the `SslIgnore` clients;
- **injection into output**: user values reaching HTML, notification letters (`notify-emails`),
  editor config, CSV/XLSX exports (formula injection), log lines;
- **secrets and crypto**: no tokens, passwords, keys or PII in logs or response DTOs; nothing
  hardcoded; `RandomNumberGenerator` for tokens, no MD5/SHA1 for security; no
  `TypeNameHandling`/`BinaryFormatter`;
- **audit**: a security-relevant action (access change, deletion, settings change) writes an audit
  event the way its neighbours do.

A security finding states the attacker (anonymous, link-holder, guest, user of another tenant,
room member with a lower role) and what they gain. Without a concrete attacker and gain it is a
remark or a question, not a blocker. When the branch touches auth, sharing, public links or
login and the user wants more depth — and only if `claude-security` is in the skill list — offer its change scan as a
follow-up — do not start it yourself, it is a separate, heavy run.

**F. Translations** — only if the branch changes `.resx` values (the scan's "Localized resources"
section). The scan gives the keys whose neutral text changed and the mechanical facts per culture;
F judges the text itself, for those keys only, never the whole file:

- **meaning**: each culture says what the neutral text says now — nothing lost, nothing added, no
  leftover of the old wording when the neutral text was rewritten;
- **placeholders and markup**: every `$Tag`, `{0}` and textile/HTML mark of the neutral text is
  present, unchanged and not translated. Tag names are ASCII (`NVelocityPatternFormatter.DefaultPattern`),
  so a suffix glued on (`$UserName님`) is fine; a translated or misspelled tag is not;
- **terminology**: names of services, plans and units match the same entities elsewhere in that
  culture (`Resource.<culture>.resx` and siblings) — a letter that says "Business plan" must not call
  it differently from the billing page in the same language;
- **form**: the same register and form of address as the neighbouring texts of that culture, no
  machine-translation artefacts (untranslated English fragments, literal word order), grammar that
  survives the substituted values (a number or a unit inserted into a sentence that needs agreement).

F reads the translations itself. Findings in low-resource locales (`hy-AM`, `sq-AL`, `az`, `sr-*`,
`lv`, `sl`, ...) are marked lower-confidence.

Severity: a lost or broken placeholder in a value that is rendered — blocker (the customer sees
`$ServiceName` or an empty slot); a meaning that differs from the neutral text — remark, with the
neutral text, the translation and a back-translation side by side; untranslated cultures — a remark
only when the branch does not say so on purpose. Do not report wording taste.

**G. Stateful and background processing** — when the scan's "Stateful / background processing"
section lists files (queues, workers, retries, leases, locks, event handlers, retention jobs). Per
request code is reviewed for what one call does; this code has to be reviewed for what happens
over time, across instances and across failures. For each loop, job and state transition:

- **termination**: every retry, requeue and hand-back path ends — by count, by an age window or by
  a terminal state. "Failure X does not count as an attempt" without a bound is an endless loop;
- **more than one instance**: two hosts run the same loop at the same moment — is a row claimed
  atomically, can a lease expire while the work is still running (lease vs timeout), does a
  periodic upkeep job need a distributed lock;
- **shutdown and cancellation mid-step**: a claimed or leased item whose work has not started is
  handed back, not left leased until expiry; in-flight work is awaited before the host stops;
- **partial failure**: for each pair of writes, what a crash or exception between them leaves
  behind; which write must come first so the record and the counters never disagree;
- **fairness**: one slow or failing item (a dead target, a huge payload) cannot hold a batch or
  the slots of the others;
- **queries that run on a timer**: an index on every filtered column; the cost when nothing
  qualifies (a `LIMIT` stops early only when rows match — an empty result scans the table);
- **mixed versions**: rows, messages and archives produced by the previous version during a
  rolling update or before a restore — read, ignored or refused, never mis-sent.

**S. OpenSpec** — whenever `openspec/specs/` exists and the scan's "OpenSpec" section lists a requirement
or a capability, or the branch changes a file under `openspec/` (a proposal-only branch, a spec re-sync). `CLAUDE.md` ("Specs (OpenSpec)") makes the specs the statement of what the API does today:
a branch that changes a status code, a field, an access rule or a side effect is checked against the
requirements that govern it, and one that contradicts a requirement goes through `openspec-propose` with
its "Breaks current behaviour" section instead of straight into code. The other lenses judge the code
against the rules; this one judges it against the promised behaviour, and neither covers the other.

- **Which requirements**: the scan lists the requirements that name the operationId of a changed action,
  and the capabilities whose `verification.md` cites a changed file. That is a start, not the whole set:
  a service or worker change reaches operations the diff does not touch (a background job changes what
  `getPortalTariff` reports). For each changed behaviour, also search `openspec/specs/*/spec.md` for the
  operations it reaches (LSP `incomingCalls` up to the controllers, then their operationIds) and for the
  table, setting or state it writes (`start_trial`, `LicenseAccept`).
- **Per requirement**, read the `SHALL` text and every scenario, and classify it on the branch:
  **holds**, **broken** (a sentence or a scenario is false on the branch), or **extended** (the branch
  adds behaviour in the requirement's area that no sentence covers: a new outcome, a new failure, a new
  writer of the same state). Quote the sentence for a broken one.
- **Where the branch should say so**: a change under `openspec/changes/<name>/` with a "Breaks current
  behaviour" section that names the requirement, or the spec text changed in the same branch.
  `openspec/deviations.md` is for defects, not for intended changes; an intended change listed there
  is itself a finding.
- **verification.md**: a row whose cited lines or stated behaviour the branch changed is stale — name it,
  so `openspec-maintain` is not left to rediscover it. The scan lists the rows it can resolve to a
  changed file (path, alias, file name, folder alias); a citation in another form is read by hand.
- **The spec texts the branch writes** (deltas under `openspec/changes/`, `proposal.md`, `design.md`,
  `tasks.md`, edited `spec.md`) are claims (`claims.md`): each scenario value against the code and the
  test that pins it, each `SHALL` against the window where it does not hold (a silent dependency, the
  time before the first background pass), each WHEN against the conditions the code really checks, and
  the rules of `openspec/config.yaml` (the scan measures the 500-character limit and the ticked tasks
  that say they are blocked). A delta that says what the code does not is a finding of the branch, also
  when the code is the side that is right.
- **Severity**: a broken requirement that the branch neither changes in the spec nor proposes is a
  **blocker** (an undeclared contract change: the next spec run records the new behaviour as a defect,
  or a reviewer trusting the spec misreads the code). An extended requirement with no spec text is a
  **remark**; a stale `verification.md` row is a **remark**. When the code contradicts the spec and the
  commits do not say the change is intended, it may be the code that is wrong: report it as broken and
  let the author decide which side moves (a question, not a fix of the spec).
- Cite as `[openspec <capability> "<requirement>"]` with the `spec.md:line` of the requirement.

**Return format for every reviewer** (ask for exactly this):

```
- severity: blocker | rule | remark | question
  category: correctness | rule | performance | security | contract | translation | spec
  where: path:line
  rule: <rule file §section>, or [openspec <capability> "<requirement>"] (or "—" for a correctness finding)
  what: one sentence
  pattern: the class this instance belongs to, in one line
  siblings: every other place in the diff with the same pattern — path:line affected | fine (why)
  scenario: concrete input/state → wrong result (for bugs), or the rule text it breaks
  fix: one sentence, for the class
  guard: a test or check that would fail for every instance of the class, or "none" (why)
  introduced_by_branch: yes | no (exists in the merge-base already)

coverage:
  read in full: path, path, ...
  skimmed or not opened: path (why), ...
```

Tell them: read the rule file in full before judging; navigate with LSP, not grep, for symbols;
cite only code they have actually opened; do not edit anything; no praise section.

## 4. Verify before reporting

Reviewers over-report. For every finding, open the code at `where` yourself and confirm it:

- the line exists on the reviewed sha and says what the finding claims;
- the **scenario's input can reach that line**: trace the value back to where it is produced, not only
  forward to the sink. A column that cannot hold a value and a call that writes a variable into it
  prove nothing until it is shown that the variable can hold that value on the path in question. A
  blocker that has not been traced from its input is a remark;
- `introduced_by_branch: no` → check it with git before acting on it (`git log -S'<line>' <merge-base>..HEAD`,
  or the method in `git show <merge-base>:<path>`): "similar code existed" is not "this code existed",
  and a wrong `no` silently hides a real finding. Confirmed → drop it, or keep it in one line under
  "not from this branch" only when the branch makes it worse (moved it onto a hot path, widened its reach);
- `siblings` checked: open the places listed as `fine` too, and look for the pattern in the chunks
  of other reviewers. All affected places go into **one** finding, so the fix covers the class;
- a rule finding cites a rule that really says that (open the rule, check the section); a spec finding
  quotes the `SHALL` sentence or scenario it breaks, read in the spec on the reviewed sha, and shows the
  branch's code path that makes it false;
- a doubt whether the code keeps a rule the branch itself states ("Restricted access is no
  membership") is checked like a finding and reported as one. It goes to "Questions" only when the
  branch states no rule for that case. A question the author can wave away is a finding the next
  reviewer reports;
- duplicates across reviewers merged; a finding that needs a product decision moved to "Questions";
- **Known limitations**: an item the PR description lists under "Known limitations", with its decision
  and the reason, is not reported again. It is reported only with an argument the entry does not
  answer — a scenario it did not consider, a rule it breaks, a statement in the entry that is false
  (`claims.md`) — and the finding quotes the entry. A blocker or a rule violation is never accepted as
  a limitation: an entry that holds one is reported as that finding;
- **origin**: `git blame` the line (`git log -S` when blame lands on a normalization commit) and
  record the commit that introduced it. When an earlier review of this branch exists, each finding
  is either **new code** (the commit is later than that review) or **missed earlier** (the code was
  already there). The report gives both counts: the first says the fix rounds add bugs, the second
  says the earlier coverage had holes — they need different remedies, and the user will ask;
- **missed by the hand-over check**: when the branch came with a Hand-over block, every confirmed finding
  on code that existed in its final tree is a miss of that check. Name, per finding, why the check could
  not see it, reading the block's lines on matrices, lenses, rules and coverage — no rule says so; a
  rule says so but no matrix or scan check enumerates it; the matrix exists but the block shows it not
  built or short; every pass only skimmed the file (the block's coverage); the path cannot run here and
  no decision was extracted for a unit test (`failure-paths.md` §11); the check ran without a lens or on an older
  copy of the rules; the code was outside the check's scope (a re-check reads only its round's diff);
  the check found it and the author filed it under Known limitations although it is a blocker or a rule
  violation — and the change to the tooling that would have caught the class: a rule section, a matrix
  row, a scan check, or "none: within the estimate the block gave" (a full check) or "none: one pass"
  (a light check, whose pass read the code and missed it). This is how the next hand-over
  check sees what this one missed.
- **missed by earlier checks**: with no Hand-over block, but with earlier reviews of this branch (§1
  "Earlier checks of the branch"), name per "missed earlier" finding why those checks could not see
  it, from the list above and these: the commit came after the newest check; the check ran on an older
  copy of the rules (the scan's "Review tooling brought in by the branch"); it was a re-check and the
  code was outside its rounds' diffs; its fresh reviewer (§7 point 3) did not run; its Coverage lists
  the file as skimmed or not opened. Give the counts per cause in the report: it is the answer to "why
  did the earlier review not see this", and it tells a process gap (a check skipped, an old copy) from
  a tooling gap, which gets a change offered as above.

Drop what you cannot confirm. A short report of true findings beats a long one the user has to
re-check.

## 5. Build and style

Build only the projects the branch touches, narrowly — a running local DocSpace locks the shared
DLLs (`MSB3027`); report that as "not built: DLLs locked", do not stop the user's processes.

```bash
dotnet build <csproj> --no-dependencies -p:EnforceCodeStyleInBuild=true
dotnet format style <csproj> --include <changed .cs files> --verify-no-changes
```

For a branch that changes `common/ASC.AI.Chat`, run the service's own gate as well; a failure of
any of its four steps is a finding under `ts-ai-style.md` (Checks). No CI job runs either
command, so the second one is the only check that the committed generated files are the ones
the sources produce; a difference it prints is a finding under the same section:

```bash
cd common/ASC.AI.Chat && yarn check    # typecheck, lint, format:check, tests
yarn openapi && git diff --exit-code -- app/generated \
  ../Tools/ASC.Api.Documentation/ASC.Api.Documentation/json/aichat_2.0.json
```

New warnings in the changed files are findings under `csharp-style.md`. `dotnet format` is the
only check that sees naming (IDE1006); neither sees partial-method parameters of `*Logger` classes —
eyeball those.

## 6. Tests

Name the suites that cover the changed area and give the exact command (`run-tests` skill for
filter syntax). Run them only if the user asked; otherwise end the report with the command.

When running, the known traps are environmental, not the branch's: a local DocSpace holding DLLs,
a stale Aspire DCP bundle (`aspire restore`), service binaries older than the product code (build
the services, not only the test project), a submodule checkout that differs from the recorded
pointer. Tell a flake from a regression by re-running the single test and checking whether the
changed code is on its path. A red test that encodes the **old** behaviour the branch deliberately
changed is a question for the user (fix the expectation or the code), not a regression.

## 7. Re-check mode

Input: the previous findings (a `.md` path the user gave, or the last review in this
conversation). Re-scan (§1) and then:

1. For **each** previous item, read the current code at its place and classify: **Fixed**
   (how, with `path:line`), **Partially fixed**, **Not fixed**. Do not trust the commit message
   ("review fixes") — check each item.
2. Review only what changed since the previous review (`git log <prev-sha>..HEAD`, diff of those
   commits) with §3–§4 for **New** — a fix commit is where new bugs come from: a
   filter narrowed for one caller loses another caller's lookup, a default added on create changes
   every update, "this failure is not an attempt" makes a row retry for ever, a restore change fails
   on archives without the new column. For **each fix** ask, beyond §3 B:
   - which paths share the changed code and were not the subject of the finding (§3 B "the other
     path") — and does a test cover them;
   - does the fix add a loop, a requeue or a fallback — then §3 G "termination";
   - does it change what old data means (rows, archives, settings saved before it);
   - did it fix the **class** or only the cited line — run the finding's `siblings` again;
   - does it come with a test that fails without it; a behavioural fix without one is a remark. Do
     not judge that by reading the test: run `.claude/scripts/prove-red.sh --commit <fix sha>` with
     the test's filter (`--patch` with the behavioural hunk when the test calls what the fix adds).
     STILL GREEN is a finding. A run the stand does not allow goes under "Not verified";
   - run the class matrices (§3 M) over the fix diff: a fix commit adds rows like any other code.

   **New** is what the fix diff brought. Code that was there at the previous review and that it
   missed is reported only as a blocker or a rule violation (origin "missed earlier", §4); a remark or
   a question there is one unnumbered line ("missed earlier: n remarks, in <files>") or nothing, so
   the round does not reopen the whole branch for remarks.

   A commit that moves or rewrites code (a refactoring, a split into new files or classes) is not read
   as a diff: the moved code is reviewed whole, as in a first review, with the matrices over it. Its
   diff shows the move, not what the code does, and blame lands on it, so a defect that moved with the
   code reads as new and one that the move introduced reads as old.
3. Give the fix commits to one more reviewer **without** the previous findings — the code only,
   with §3 B and G, and §3 S over the spec texts they change. A reviewer handed the earlier list
   checks that list; the fresh one finds what the list did not name. This step is not skipped for a
   small round: seen in review, a re-check without it found 1 new item, and the fresh full pass that
   followed on the same sha found 12. The Coverage line says it ran; when it could not,
   the report names the gap and the closing line offers that pass first. When the round adds a new
   artifact (a new class, an OpenSpec change), the fresh reviewer reads that artifact whole, as a
   first review, not as a diff; in such a round of the author, the batch check reads it whole, and
   after a review round the light hand-over check is offered again (`fixing-findings.md` §6).
4. Re-evaluate findings that were dropped or accepted earlier ("acceptable", "not from this
   branch") when a fix changed the code around them.
5. If an earlier finding turns out to be wrong, say so plainly and withdraw it. Withdrawing needs the
   evidence a fix needs: the code read for every caller, entry kind and path the finding names
   (`fixing-findings.md` §7).
6. Run §3 S on the fix commits: a fix can break a requirement the first version kept, and a finding of
   S closed by changing the spec is checked like a code fix — the new spec text describes the branch.
7. Re-read the texts the fixes may have made stale (§3 B "texts made untrue", "claims", `claims.md`
   §3): the claims in the earlier rounds' commits and comments, the remarks of the actions the fixed
   behaviour reaches, the spec deltas and task lists, and the PR description — does it still describe
   the branch after the rounds of fixes.
8. When a fix followed the previous report's "how to fix", check it as the author's own code, against
   its whole row of the matrix it belongs to (`fixing-findings.md` §8): a suggestion written without
   walking the other paths is where the next finding comes from (seen in review: "keep the claim" in
   a wallet renewal opened a path to a second payment).
9. When the previous review ran on an older copy of the rules or this skill (its header's tooling line
   against the scan's "Review tooling brought in by the branch"), the re-check is not limited to the
   diff: each lens, matrix, rule or scan check the newer copy added or changed runs over the whole branch, as
   `fixing-findings.md` §6 "The base first" has the author do. A re-check of the diff alone on the newer
   copy sees the new classes only in the newest code.
10. **The round's verdict.** With no blocker and no rule violation left open or new, the review ends:
    say so in the report. The remaining remarks and questions are the author's to fix with the batch
    check or to write under Known limitations; no further re-check runs for them
    (`fixing-findings.md` §6).

## 8. Report

Header line: branch, base, number of commits and files, sha reviewed, what was run (build / format
/ tests) and what was **not** — "verified by reading the code, tests not run" when that is the case;
the tooling the review ran with (the scan's "tooling of the scanned tree" line, of the copy you ran:
the base's when you ran the base's scan), and, with earlier checks of the branch, the newest sha they
covered (§1 "Earlier checks of the branch").
Coverage line: how many chunks, and the files no reviewer read in full (they also go to "Not
verified"), the class matrices built with their row counts, which findings you opened yourself
in §4 versus took from a reviewer's citation, in a re-check whether the fresh reviewer of §7 point 3
ran, and whether the branch lacks rule changes of the base (§1). Origin line (§4): how many findings are in new code,
how many were missed earlier. A review that says nothing about coverage reads as complete when it
was not.

Then, omitting empty sections:

```
## 🔴 Blockers         — data loss, security hole, regression, broken contract
## 🟠 Rule violations  — each with the rule file and §: [caching.md] …
## 🟡 Remarks          — real but not blocking
## ❓ Questions        — behaviour changes and debatable semantics that need the author's decision
## Missed by the hand-over check — only with a Hand-over block (§4): per finding, why, and the tooling change
## Missed by earlier checks — no block, but earlier reviews of the branch (§4): counts per cause, the tooling change
## Not verified       — what stays unverified and what would verify it (a suite, a profile)
```

Per finding: a `[security]` / `[performance]` / `[translation]` / `[spec]` tag when that is its category, the `[path:line](path:line)` link, what is wrong, the concrete scenario, the fix — a direction for the class, not a patch; a fix that adds or keeps state (a claim, a cache entry, a fallback) is written as what must hold ("the mark is released on every failure path"), not as a code change to copy, and names the matrix row it has to be walked against (M9, M10, M11): three next-round findings came from following a suggested fix (`fixing-findings.md` §8). Code
snippets only when the line itself is the point (broken indentation, a wrong condition). No scan
tables, no "what was done well" section — at most one line if something deserves it.

Re-check report: **Fixed / Not fixed / New**, same per-item format, and the round's verdict (§7
point 10).

Do not report (ten reviews of November 2026 held 9 items outside the branch, 5 duplicates and 5
wording-only items):
- a renamed or re-numbered migration as a risk "for local DBs where the old one was applied" — the
  user drops that every time;
- formatting the generated files (`*.Designer.cs`, `json/*_2.0.json`, SDK sources);
- anything already present in the merge-base, unless the branch makes it worse (§4); when it is worth
  knowing, one unnumbered line at the end, never a numbered finding;
- an entry of the PR's "Known limitations", unless with an argument it does not answer (§4);
- a previous item that is not fixed as New: it is Not fixed, under its old number;
- the same defect twice: one item per class, its other places listed in it;
- wording: a text is a finding when it states what the code does not do (`claims.md`), not when it
  could read better;
- each claim of a commit message already pushed: one remark asks for the PR description to state it
  right (`claims.md` §4), and the pushed message stays as it is.

End with the verdict and one line offering the next step. A review or re-check with no blocker and
no rule violation ends the review: say so; its remarks and questions are the author's to fix with the
batch check or to put under Known limitations, and no further round is offered for them
(`fixing-findings.md` §6). Otherwise offer to fix the blockers, run the named suites, or save the
findings to a file. When "Missed by the hand-over check" names tooling changes, offer them to the user
too, as a change of their own to the rules, this skill or the scan: they are what keeps the next
hand-over from missing the same class. The saved review keeps that section, and so it keeps "Missed by
earlier checks".

### Saving to a file

On request ("save to md"), write the findings — no header statistics, no "Fixed"
unless asked — to `<branch-name-with-dashes>-review.md` at the repo root (or the folder the user
named). Make each item readable without this conversation: place, scenario, fix, guard. The file
stays untracked; never `git add` it.

End the file with a `Coverage` section the next run reads (§3 Coverage): the reviewed sha and date,
the tooling line of the header (§8), the matrices built with their row counts, and the files no
reviewer read in full. Every run of a
branch saves one, also when the user did not ask for the findings: offer it in the closing line,
since without it the next run cannot tell what is new from what was missed.

### Self-review

The findings go back to the author, who fixes them before anyone else reads the branch. This mode
and the hand-over check (§0), which ends the same way, are the ones followed by fixing without a
separate request: the author asked for the check in order to fix what it finds. Reviewing someone
else's branch stays read-only.

1. Fix every finding in one batch (`fixing-findings.md`), then run the batch check on it
   (`fixing-findings.md` §6), which re-reads the texts the batch reaches (`claims.md` §3) and reads code
   of a new kind the batch adds (a new class, a cache, a fallback) whole. When the batch fixed a
   blocker, offer the §7 re-check with its fresh reviewer (point 3).
   Uncommitted work has no fix commits: before fixing, save `git diff HEAD` to the
   scratchpad (it holds the new files only once they are added, §1 "No new file is left out"; a
   new file the fixes create is offered for `git add` the same way). The batch check, and a §7
   re-check when one runs, then read the files the batch touched against that copy (its fresh
   reviewer gets the difference), and a behavioural fix is proven with `prove-red.sh --patch`
   holding the fix's own hunk instead of `--commit`.
2. A finding whose fix would change behaviour or scope the user did not ask for goes to the user as
   a question, not into a silent fix.
3. Write what the self-review could not verify (a suite it could not run, a platform, a profile)
   under "Not verified" in the commit message or the PR description, or, when the author does not
   commit, in the commit message proposed to the one who does: the reviewer starts there.
4. Do not hand the findings list to the reviewer. A reviewer given a list checks that list (§7,
   point 3); the reviewer runs a first review of their own. Save the file, when asked, as
   `<branch-name-with-dashes>-self-review.md`, so that no later run takes it for a review round.
5. A remark or a question the author decides not to fix goes into the PR description's "Known
   limitations", with the decision, the reason and who decided; the entry is a claim (`claims.md`) and
   states the effect as it is. A blocker or a rule violation is fixed, never listed there.

### Hand-over block

The hand-over check (§0 "Hand-over") and every later hand-over after a review round end with this
block, written into the PR description (or the message that hands the branch over) and into the body
of the branch's last commit, the one whose tree is the final tree, so the reviewer can check it before
reading anything, also without the PR (§1). When the author does not commit, the block goes into the
commit message proposed to the one who does. It holds counts and coverage. The only findings in it are
the accepted ones under Known limitations: decisions the author asks the reviewer not to raise again,
open to a new argument (§4); the findings the check fixed stay out (§8 "Self-review" point 4).

```
Hand-over check
- checked tree: <tree id the scan printed for the passes>; final tree: <tree id after the fixes>
  (in the PR description: commit <sha> once committed; the copy in the commit body gives the tree); date; base <base> at merge-base <sha>, rules of the base: yes
- passes: <light: one pass, n = .. | full: two independent passes n1 = .., n2 = .., in common m = ..,
  estimated unfound .. | review round <date>: n = ..>; all fixed in one batch
- batch check: scan hints answered; texts re-read; prove-red: <n proven | not proven: why>;
  §7 re-check with its fresh reviewer: <.. new | not run: no blocker fixed>
- lenses run once: A (rules: ..), A0, C, F, S; matrices with rows: M2 = .., M4 = .., ...
- open: 0 blockers, 0 rule violations; remarks and questions not fixed: under Known limitations
- gate: scan.py --gate exit 0 on the final tree; built without new warnings: <projects | no .cs changed>;
  yarn check: <green | not run: no ASC.AI.Chat change>
- coverage: files no pass read in full: <list | none>
- not verified: <suites, platforms, profiles>

Known limitations
- <what>: <decision, reason, decided by>
```

When the user declines the check, the block says so in one line ("Hand-over check: declined by
<who>"); the reviewer then treats the branch as having no block (§1).

The final tree is what was checked. The fixes of the check's own batch are part of the check, so the
final tree is the one after them. A commit whose tree equals the final tree (the author's
uncommitted work, committed by the user) is the checked state. Any other change after the block is
written needs the block again: the batch check of the change since the final tree, with `--base` the
commit whose tree is the final tree (while that tree is not committed, `--base HEAD`, and `git diff
<final tree>` in the clone that ran the check names what changed since), the gate included; after a merge of the base, the batch
check of the merge's own resolutions (`git show --remerge-diff <merge>`) and a check of the branch
against what the base changed in the rules and this skill (`fixing-findings.md` §6). After a review
round, the block's passes line names the round and its count.

## 9. Saturation mode

Asked to find **all** findings, say first that reading cannot guarantee that; what it can give is
completeness inside the enumerated classes (§3 M) and a measured estimate of the rest. Then:

1. **Freeze the sha.** Every pass reviews that sha; nothing landing during the run is included. If
   the branch moves, finish on the frozen sha and leave the new commits to §7.
2. **Build every class matrix (§3 M) once.** Their rows are exhaustive, so they are not repeated
   per pass.
3. **Two independent full passes** of §3 B, D, E, G: two sets of chunk agents with different chunk
   cuts (by subsystem in one, by call path in the other), neither seeing the other's findings or any
   earlier review's findings. Verify both through §4 separately.
4. **Match** the verified findings of the two passes by class and place. With `n1` and `n2` findings
   and `m` in common, the estimated total is `n1 × n2 / m` (capture–recapture); the estimate minus
   the union is roughly what is still unfound. `m = 0`, or a tiny `m`, means the passes sample
   different code — the estimate is meaningless; run a third pass with the files both left skimmed.
5. **Stop** when the estimate is within one or two of the union, or the user stops. Report the
   union once (§8), with the two counts, `m` and the estimate in the header.

Saturation costs two to three times a normal review; say so before starting. As the first pass of a
hand-over check (§0 "Hand-over") it also runs, once, what step 3 leaves out: the rules (A), the
cross-cutting matrix (A0), C, F and S, besides the class matrices of step 2; that is the full
hand-over check, and the light one runs one pass and no estimate. The author does not stop at the
report: the union is fixed in one batch, the batch check runs on it (`fixing-findings.md` §6), and the
Hand-over block records the two counts, `m`, the estimate, the lenses and the matrices with their row
counts. "Freeze the sha" for
uncommitted work means the tree the scan prints (`--worktree`): keep the work unchanged while the
passes run.
