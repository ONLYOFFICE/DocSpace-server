---
paths:
  - "**/*.cs"
---

# Fixing review findings: the class, the other paths, a test

A review finding names one place. Fixing that place alone leaves the same defect in its siblings,
to be found by the next review, and a fix that is checked only against the finding can break the
paths around it. Before a fix is handed back:

1. **Fix the class.** Name the pattern the finding is an instance of and find every other instance
   in the branch — `findReferences` on the member, the same call shape in sibling services (Files /
   People / Web.Api / Web.Studio), the same config/DTO/handler family. Fix them all, or say in the
   commit message which ones were left and why. For a filter that decides what a caller may see, the
   class is both tables of `visibility-filters.md` - every source of the result and every output
   channel, per caller and entry kind - filled again for the whole method, not the cited branch: on
   bug 79163 each round fixed one branch and the next review found its sibling.
2. **Walk the other paths of the changed code.** Create vs update (a field left out on update),
   the lookup the old behaviour served, rows and backup archives written before the change, the
   other database in the code (the PostgreSQL branch of a model builder or of raw SQL), the success
   path and the opposite failure. If the fix adds a retry, requeue or fallback, show where it ends.
3. **Prove it with a test** that fails without the fix — for every behavioural finding — and prove
   that it fails: run `.claude/scripts/prove-red.sh` (`tests.md`, "A test has to be able to fail") and
   put its result line in the commit message. A finding that cannot be tested or proven here
   (environment, timing, cost) says so in the commit message — after the decision behind it has been
   moved where a unit test reaches it (`failure-paths.md` §11).
4. **Close the class where it can be enumerated.** When the finding's `guard` names a test that
   walks every instance (every property of the webhook DTOs is filled by some fixture, every
   out-of-controller publish is listed in the contract, every backed-up column is declared), add
   that test instead of — or beside — the one for the cited place. A class closed by a guard does
   not come back in the next review; a class fixed by hand does, from the next new instance.
5. **Fix a review in one batch.** Take all findings of one review (one sha) in one round and only
   then ask for the next review. Fixing between review runs moves the sha under the review, and
   each run then finds the bugs of the previous round's fixes mixed with what it missed before.
6. **Check your own work; a review round ends at its fixes.** This item is the one schedule of
   self-reviews, hand-over checks and re-checks, for every branch, whatever files it changes;
   `CLAUDE.md` (Git Workflow) and `review-branch` (§0, §7, §8) point here. Ten reviews of seven
   branches after the rules of 2026-11-17 found 3 blockers in 133 findings; the rounds came from
   remarks, and a fix batch brought about 7 new findings to its next check (four checks: 7, 7, 7, 7;
   one: 14), many of them texts the batch had made untrue. So the author checks every batch cheaply,
   and a review round no longer starts another one for remarks.
   - **The batch check — every fix batch, run, not offered.** Minutes, no reviewer agents, after
     every batch: the hand-over check's, a self-review's and each review round's.
     1. `scan.py --worktree --gate --base <commit before the batch>` (`HEAD` while the batch is not
        committed): exit 0, and each hint line of the scan answered — fixed, or "not a defect: why" in
        the commit message or the block. A citation the batch corrected is also read at its target
        (point 12).
     2. Its "Texts the diff writes or reaches" section and the claims the batch wrote: point 9 —
        each number, absolute word and statement of a library's behaviour pinned by a test or marked
        CODE-ONLY, then each text re-read against the code (`claims.md` §3, §6) — remarks, comments,
        spec deltas, `verification.md` rows, the PR description.
     3. The checklists the scan prints for the applicable rules, and the class matrices over the batch
        diff: each mark, lock, fallback, catch, loop, state that ends and multi-step operation the
        batch adds walked through `failure-paths.md`.
     4. A behavioural fix proven red (`prove-red.sh`; a pinned value with `--mutate` one step on each
        side), or its commit says "not proven" and why; the reported case and the nearest case that
        must keep working are both exercised.
   - **Before the first hand-over:** offer the hand-over check (`review-branch` §0 "Hand-over"). The
     light one is the default: the whole first review once, the fixes in one batch, the batch check,
     the gate, the Hand-over block (in the PR description and the body of the last commit,
     `review-branch` §8). The full one adds a second independent pass and the estimate of
     what is still unfound; offer it for a branch of more than ~1 500 added product lines or one with a
     multi-step operation (`failure-paths.md` §10), or when the user asks. One pass finds about 55–60%
     of what is there (two passes on one branch: 22 and 20 findings, 12 in common). Each pass of the
     check runs without the author's memory (`review-branch` §0 "Review without memory"), and the
     block records how; a pass that could not be isolated is named there as pending.
   - **A review round ends at its fixes.** Keep the reviewer's Markdown report and the reviewed sha.
     Fix every finding of the review in one batch; under each ID of that report record the change and
     its evidence, or the reason for another disposition (points 7 and 11). Run the batch check, write
     the Hand-over block for the new tree, put the returned commit (or the checked tree of local fixes)
     in the report, and hand the branch back. The reviewer's next look is the re-check of that batch
     (`review-branch` §7, with `--since-review <reviewed sha>`). When a review or re-check reports no
     blocker and no rule violation, the review ends there: its remarks and questions are fixed with the
     batch check, or written under Known limitations (point 11), and no further review round runs for
     them. A check that could not run or a product decision the author cannot take is handed back as
     an open item that names who can close it, not left out.
   - **After a batch that fixed a blocker** — a review round's, a self-review's or a hand-over check's —
     offer the §7 re-check with its fresh reviewer (`review-branch` §7). What it finds is fixed in one
     more batch with the batch check; one more §7 only when it found a blocker again.
   - **A new kind of state in a batch** — a cache, a lock, a fallback, a new class, an OpenSpec change, a
     refactoring that moves or rewrites code: the batch check runs as always and reads that code whole,
     since a diff shows it only in part. After a review round's batch, also offer the light hand-over
     check again; the batch of a hand-over check or a self-review gets the batch check only, so one
     check never starts the next.
   - **The base first:** when the scan reports that the base changed rules or review tooling that apply
     to the branch (`review-branch` §1), bring the base in before the check — with uncommitted work,
     commit or stash it first, or ask whoever commits the branch to merge — and check the branch
     against what changed, not again in full: lens A with each changed rule; each lens, matrix or step
     whose section the base's skill changed or added, over the whole branch; §3 S for changed spec
     rules; the scan re-run; the batch check of the merge's own conflict resolutions
     (`git show --remerge-diff <merge>`); then the gate and a new block.
   - **Every commit is in a check.** A commit made after the last check (a refactoring after the last
     re-check, a small fix after the hand-over check) needs at least the batch check before the branch
     goes to review. Seen in review: a branch was handed over with a re-check three commits back, one
     of them a 1 300-line refactoring; four of the next review's 13 items were in those commits.

7. **Reject a finding only with the same evidence a fix needs.** Read the code for every caller, entry
   kind and path the finding names, not for the one you know best. An argument that covers part of the
   finding is not a rejection: on 79163 a correct finding about rooms was first rejected with a reason
   that held only for personal files. When unsure, say which part is right and fix that part.
8. **A suggested fix is a hint, not the fix.** The review's "how to fix" names one way, written
   without walking the other paths. Derive the fix from the class (point 1) and walk
   `failure-paths.md` — its §10 table when the code is a multi-step operation — before writing it.
   The code is yours whether it follows the suggestion or not. Seen in review: "keep the claim" closed
   one cell of a wallet renewal and opened a path where another instance pays a second time; "write
   the known answer instead of removing it" polished a cache whose instance was the real defect
   (`caching.md`, "Which instance"). Later reviews counted three more next-round findings that came
   from following the previous round's suggested fix, one of them the only blocker a fix round made.
9. **Re-read the texts the round reaches and the ones it wrote** (`claims.md` §3, §6), as step 2 of
   the batch check (point 6), after the code and before the branch goes back or to a §7 re-check:
   - run `scan.py` and take its "Claims with numbers" lines of the round's commits (or of the
     uncommitted batch, `--worktree`) as the list; add each absolute word and each sentence about a
     library's behaviour the round wrote in a remark, a comment, a spec text, a `verification.md` row
     or the commit message;
   - per line: the constant or the test that pins it with its one-step mutation (`claims.md` §6), the
     characterization test for a library's behaviour (`dependency-behaviour.md`), or CODE-ONLY in
     `verification.md`; a line with none is rewritten or proven before the branch goes back;
   - then the texts of earlier rounds the round's behaviour reaches: remarks, comments, spec deltas,
     task lists, the PR description and the claims of earlier commit messages.
   Seen in bugfix/78009: about 17 of some 45 findings over nine rounds were texts the previous round
   had written, most of them numbers and statements about the cache or the circuit breaker.
10. **A miss of the hand-over check is a gap in the tooling.** When a review lists findings under
    "Missed by the hand-over check" (`review-branch` §4), fixing the code closes the branch, not the
    gap. The review offers the user the tooling change it names (a rule section, a matrix row, a scan
    check) as a change of its own to `.claude/`; when you fix the branch, check that the offer was
    taken up or declined, and make it once more if it was lost. A miss the review marks "within the
    estimate" (a full check) or "one pass" (a light check, whose one pass finds about 55–60%) needs no
    change, unless the review names a rule, matrix row or scan check that would have caught the class.
11. **What is not fixed is a decision, written down.** A remark or a question you decide not to fix
    goes into the PR description's "Known limitations", with the decision, the reason and who
    decided; the entry states the effect as it is (`claims.md`). The reviewer does not raise it again
    without an argument it does not answer (`review-branch` §4). A blocker or a rule violation never
    goes there: it is fixed. Nor does a disclosure of behaviour that defeats the fix's purpose ("may
    keep it longer" about the state the fix bounds): that is fixed too (`failure-paths.md` §9).
12. **Line citations are regenerated, never shifted by hand.** After a fix that moves lines, the
    `verification.md` rows and other texts that cite them are written again from the code: find each
    cited member or block in the new file and take its lines, by script or by the editor's symbol
    search, never by adding an offset to the old numbers. Then run `scan.py`: its "verification.md
    citations" check (part of `--gate`) reports a range that starts on a blank line, a lone brace or
    inside a statement, or ends on a blank line, an opening brace, the next member's comment or inside
    a statement; a backticked name that is not in the cited lines; and a cited block of a row the
    branch left alone that moved. Seen in bugfix/78009: the citations were wrong or off by one in six
    rounds; run on the branch's history, the check flags five of those rounds.

A fix that adds state — a mark, an "accepted" flag, a lock, a retry, a fallback — is new code and
goes through `failure-paths.md` like any other: in past rounds most new findings were in exactly
that code. The same rule covers writing new code of the kinds that are easy to get wrong: side
effects after the primary action, external calls, background loops, queues, retries and leases.

## Checklist

- The class is fixed, not the cited line; an enumerable class is closed by a guard test (§1, §4)
- The other paths are walked: create and update, the old lookup, old rows and archives, the other DB (§2)
- Each behavioural fix has a test proven red, or its commit says why it cannot be proven here (§3)
- All findings of one review are fixed in one batch, and the batch check ran on it (§5, §6)
- The batch check: scan gate exit 0, hints answered, texts re-read, checklists walked, prove-red (§6)
- Each ID of the reviewer's report has its change and evidence, or the reason for another disposition (§6)
- A rejected finding has the evidence a fix needs, for every caller and entry kind it names (§7)
- A suggested fix was derived from its class and walked through `failure-paths.md` before it was taken (§8)
- Each number, absolute word and library behaviour the round wrote has its test with a mutation, or CODE-ONLY (§9)
- A remark or question not fixed is under Known limitations, with the decision and the reason (§11)
- Line citations the round moved are regenerated from the code, and the scan's citation check is clean (§12)
