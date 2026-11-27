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
   moved where a unit test reaches it (`tests.md`, "A decision the stand cannot reach").
4. **Close the class where it can be enumerated.** When the finding's `guard` names a test that
   walks every instance (every property of the webhook DTOs is filled by some fixture, every
   out-of-controller publish is listed in the contract, every backed-up column is declared), add
   that test instead of — or beside — the one for the cited place. A class closed by a guard does
   not come back in the next review; a class fixed by hand does, from the next new instance.
5. **Fix a review in one batch.** Take all findings of one review (one sha) in one round and only
   then ask for the next review. Fixing between review runs moves the sha under the review, and
   each run then finds the bugs of the previous round's fixes mixed with what it missed before.
6. **The author prepares the branch for the receiving reviewer.** Use this schedule for all file types.
   - **While working:** a one-pass self-review runs on request (`review-branch` §0).
   - **Before the first hand-over:** run the hand-over check (§0) on the finished branch. When it
     finds issues, fix them together and run §7 on that batch. Record the final gate and checked
     state in the Hand-over block (§8).
   - **After receiving a review:** keep the reviewer's Markdown report and reviewed SHA. Handle all
     items together. Under each ID record the change and evidence, or the reason for another
     disposition (§7, §11 below). For a behaviour fix, exercise the reported case and the nearest
     case that must keep working. Re-read affected claims (§9) and run the relevant tests.
     The author runs `review-branch` §7, including its reviewer without memory (§0), before returning the
     branch. Use the earlier SHA for `--since-review`; §7 covers worktree reviews and rebases.
   - **When the internal re-check finds more:** fix the issues in one batch. Re-run the checks the
     edits affect. A corrected citation needs a citation check and a read of its target. A blocker
     or a follow-up edit that changes behaviour needs another independent pass over the affected
     operation, including callers and failure paths. Read new or rewritten artifacts in full.
     Return to a full hand-over check if the affected operations cannot be bounded, for example
     after a cross-cutting refactor. Keep these iterations with the author.
   - **Ready to return:** all IDs have answers, required checks passed, the required memory-free
     passes and their scope are recorded in the Hand-over block,
     blockers and rule violations are closed, and `scan.py --gate` passed on the final state (add
     `--worktree` for local fixes). Put the returned commit or checked tree in the report and renew
     the Hand-over block. The receiving reviewer verifies the answers and gives the final verdict.
     A missing required check or unresolved product decision is an explicit open item for the
     colleague who can resolve it; identify it when handing over a request for help.
   - **The base first:** when the scan reports that the base changed rules or review tooling that apply
     to the branch (`review-branch` §1), bring the base in before the check: the reviewer judges by
     those. With uncommitted work, commit or stash it first, or ask whoever commits the branch to
     merge. Afterwards check the branch against what changed, not again in full: lens A with each
     changed rule over the branch; for a changed skill, every lens, matrix or step whose section the
     base changed, run over the whole branch (a lens S the base added means a pass of S); the scan
     re-run for a changed scan; §3 S for changed spec rules; and §7 on the merge's own conflict
     resolutions (`git show --remerge-diff <merge>`). Then the gate and a new block. Until that has
     run, no earlier check covers the branch for what the merge changed; the scan lists such merges
     with the commits before and after each ("Review tooling brought in by the branch"), and the
     reviewer, given earlier checks and no block, counts what they missed by that cause
     (`review-branch` §4 "missed by earlier checks").
   - **Every commit before the hand-over is in a check.** A commit made after the last check (a
     refactoring after the last re-check, a small fix after the hand-over check) is in none of them:
     the branch goes to review with a check that covers its final tree, or with no block and nothing
     claimed. Seen in review: a branch was handed over with a re-check on a sha three commits back,
     one of them a 1 300-line refactoring; four of the next review's 13 items were in those commits.

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
   (`caching.md`, "Which instance").
9. **Re-read the texts the round reaches and the ones it wrote, before the re-check** (`claims.md`
   §3, §6). A checklist step of every round, after the code and before §7:
   - run `scan.py` and take its "Claims with numbers" lines of the round's commits (or of the
     uncommitted batch, `--worktree`) as the list; add each absolute word and each sentence about a
     library's behaviour the round wrote in a remark, a comment, a spec text, a `verification.md` row
     or the commit message;
   - per line: the constant or the test that pins it with its one-step mutation (`claims.md` §6), the
     characterization test for a library's behaviour (`dependency-behaviour.md`), or CODE-ONLY in
     `verification.md`; a line with none is rewritten or proven before the re-check;
   - then the texts of earlier rounds the round's behaviour reaches: remarks, comments, spec deltas,
     task lists, the PR description and the claims of earlier commit messages.
   Seen in bugfix/78009: about 17 of some 45 findings over nine rounds were texts the previous round
   had written, most of them numbers and statements about the cache or the circuit breaker.
10. **A miss of the hand-over check is a gap in the tooling.** When a review lists findings under
    "Missed by the hand-over check" (`review-branch` §4), fixing the code closes the branch, not the
    gap. The review offers the user the tooling change it names (a rule section, a matrix row, a scan
    check) as a change of its own to `.claude/`; when you fix the branch, check that the offer was
    taken up or declined, and make it once more if it was lost. A miss the review marks "within the
    estimate" needs no change.
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
