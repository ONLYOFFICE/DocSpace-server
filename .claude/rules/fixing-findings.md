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
6. **Offer to check your own work on what goes to review.** This item is the one schedule of
   self-reviews, hand-over checks and re-checks; `CLAUDE.md` (Git Workflow) and `review-branch` (§0,
   §8 "Self-review", "Hand-over block") point here. Each run costs passes of the skill with its
   reviewer agents, so it is offered and the user decides; the reviewer uses the same skill, so what
   the run finds would otherwise cost a review round. The reviewer checks the Hand-over block first:
   a block whose checks fail sends the branch back unread, and a branch without a block, or with a
   declined check, is reviewed with "no hand-over check" in the report header (`review-branch` §1
   lists the checks).
   "§7" below is `review-branch` §7, the re-check with its fresh reviewer (its point 3).
   - **While working (on request):** a self-review (`review-branch` §0) is a one-pass check, cheaper
     than the hand-over check, run when the user asks for it; it is not offered. Its fix batch gets §7
     once, one more only when that found a blocker.
   - **Before the first hand-over:** offer the hand-over check (`review-branch` §0 "Hand-over") on the
     state that goes to review: the whole first review once and two independent passes of the deep
     lenses with the estimate of what is still unfound, the fixes in one batch, §7 on that batch once
     (one more only when it found a blocker; the fixes of the last one are part of the check),
     `scan.py --gate` on the final tree, and the Hand-over block in the PR description.
   - **After a review round:** offer the §7 re-check of your fixes however small the round is. On a
     yes, fix what it finds in the same round; one more §7 only when it found a blocker. Then run
     `scan.py --gate`, write the Hand-over block for the new tree, and the branch goes back to the
     reviewer: the next check is theirs.
   - **The hand-over check again, instead of §7:** when, since the last hand-over check, the branch
     gained a new kind of state or a new artifact (a cache, a lock, a fallback, a new class, an
     OpenSpec change, a refactoring that moves or rewrites code) in a round other than the own batch of a self-review or a hand-over check, whose
     §7 reads the new artifact whole: §7 reads only the round's diff. Seen in review: one
     self-review ran before four fix rounds and a spec change; about 14 of the 20 later findings sat in
     that later code, and the fresh full pass at the end found 12 after a re-check that had found 1.
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
9. **Re-read the texts the round reaches** (`claims.md` §3): remarks, comments, spec deltas, task
   lists, the PR description and the claims of earlier commit messages.
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
    goes there: it is fixed.

A fix that adds state — a mark, an "accepted" flag, a lock, a retry, a fallback — is new code and
goes through `failure-paths.md` like any other: in past rounds most new findings were in exactly
that code. The same rule covers writing new code of the kinds that are easy to get wrong: side
effects after the primary action, external calls, background loops, queues, retries and leases.
